# Agent Bridge

Let Claude (or any local tool) observe and drive a running acclient through RedoxExtensions, so
features can be tested end-to-end instead of only with unit tests.

## Constraints

* Only on the Windows dev machine, with Claude running on the same machine as acclient.
* Public ACE server: no server-side or admin access, and anything automated runs on a real character.
* Several acclient instances are always running at once.
* acclient is 32-bit and already close to its limits with other plugins and graphics settings, so the
  memory and address-space cost has to stay small.

## Approach

The plugin already sits inside the client with Decal's world model, decoded network messages, chat,
VT/UB/MagTools interop and a game-thread dispatcher. The bridge exposes that to a local process. Scraping
pixels or sniffing UDP from outside would be harder and less reliable.

### 1. Local control socket

* `TcpListener` on `127.0.0.1` only, newline-delimited JSON (Newtonsoft net35 is already referenced).
  This avoids the `netsh urlacl` setup `HttpListener` needs.
* Transport (socket, token, discovery file) lives in `RedoxExtensions.Wrapper` so it survives `/re reload`.
  The wrapped plugin registers request handlers on load and unregisters them on unload. During a reload
  the wrapper answers "plugin not loaded". Changing the transport needs a client restart, so it stays small.
* Requests run on the game thread via `Dispatching.Legacy/GameThreadDispatcher.cs`.
* Requests:
  * `ping`
  * `exec {text}`: `DecalProxy.DispatchChatToBoxWithPluginIntercept` (`Core/DecalProxy.cs`), so `/re`,
    `/vt`, `/ub`, `/mt` and native commands behave exactly as if typed.
  * `reload`: handled by the wrapper.
  * `state`: character, vitals, busy state, location, VT state, settings.
  * `world {filter}`: WorldFilter objects and inventory. Results are paged.
  * `events {since, kinds}`: drains the event ring buffer.
  * `wait {kind, regex, timeoutMs}`: blocks off the game thread until a matching event arrives. This turns
    "run a command and look" into an assertion.

### 2. Selecting an instance

* Each enabled client writes `%LogDirectory%\agent-bridge\<pid>.json` = `{pid, character, server, port, token, mode, startedUtc}`
  and deletes it on disable or unload. The OS picks the port (bind to port 0).
* Clients ignore files whose pid is no longer alive and clean them up.
* Selection rules for `rectl`:
  1. `-c <character>` (prefix match) or `--pid` given: use that instance.
  2. Otherwise, if exactly one instance has the agent enabled: use it.
  3. Otherwise: error and list the enabled instances.
* Agent support is enabled per client with `/re agent on`, so the usual case is "turn it on for the one
  character under test" and rule 2 applies.
* Later: `--all` to send to every enabled instance (multi-box scenarios).

### 3. Event journal

* A fixed-size ring buffer (about 5,000 entries) holding chat (text, color, channel), RE debug lines,
  command dispatch and outcomes, VT nav and state changes, and decoded network messages from an allowlist.
* Optional JSONL file per session, so `rg` can search past runs, including manual ones.

### 4. Network traffic, captured inside Decal

* AC UDP isn't encrypted, but it is fragmented, ISAAC-checksummed and sequenced, so working from a pcap is
  a large job. Decal's `ServerDispatch` and `ClientDispatch` already give reassembled messages decoded with
  `messages.xml`. These get logged to the journal by type allowlist (CreateObject, UpdatePosition, chat,
  vendor, portal and so on).
* `RedoxFilter` (an empty class today) could become a Decal filter to capture traffic from before login.
* Wireshark or pcap capture is only a passive fallback.

### 5. `rectl` CLI, then MCP

* `RedoxExtensions.AgentCli`, a net10 console app: `rectl exec`, `state`, `world`, `events`, `wait`,
  `screenshot` and `dev`, plus `-c`/`--pid`.
* Later, wrap the same client as an MCP stdio server so the tools show up natively in Claude Code.

### 6. Screenshots

* `rectl screenshot` calls `PrintWindow` on the selected instance's HWND (found by pid) and saves a PNG
  that Claude can read. It runs in the CLI process, so acclient memory is untouched.
* No SendInput or keyboard automation.

### 7. Dev loop

* `rectl dev`: runs `copy_dlls_debug.bat`, then `exec "/re reload"`, waits for "Reload Complete", then
  optionally runs a smoke scenario.
* `deploy.bat` must not pause, so it can run non-interactively.
* Scenario scripts (exec, then wait, then assert) can be replayed, for example: record a short route and
  diff the `.nav` file.

## Safety

* Off by default. `/re agent on` enables it for the session only, and a chat line confirms enable and disable.
* Read-only by default. `exec` requires `/re agent on rw`.
* Every request needs the token from the discovery file.
* Commands that move items or currency (give, drop, sell, salvage, trade, RE give/bank commands) are
  denied unless explicitly allowed.
* `exec` is rate limited.

## Memory budget

Expected cost is on the order of **a few MB, well under 10 MB**, and nothing while the bridge is disabled.

| Item | Approx. cost |
|---|---|
| CLR, Newtonsoft | 0 extra, already loaded by RE |
| Bridge code | ~100 KB |
| Listener and connection thread | ~256 KB reserved each (set an explicit stack size, not the 1 MB default) |
| Socket buffers | tens of KB |
| Ring buffer, 5k events at ~300 B | ~1.5 MB |
| Transient JSON for `world` and `state` | <1 MB per request if paged and streamed |
| Screenshot, CLI, MCP | 0, separate processes |

Rules for keeping it there:

* Nothing is allocated or started until `/re agent on`.
* Serialize straight to the socket stream. Avoid buffers over 85 KB, which land on the LOH and fragment
  the 32-bit address space.
* Bound network-message journaling by type allowlist and the ring buffer size. Don't keep raw message blobs.
* Cap the number of connections (for example 2).

## Implementation split: Mac vs Windows

About two thirds can be built and unit-tested on the Mac, since plugin assemblies build AnyCPU and tests run on net10:

* Protocol and JSON model, token check, handler registry, ring buffer, `wait` matching and timeouts
* Discovery-file writing and instance selection rules
* The whole `rectl` CLI, tested against an in-process fake bridge server
* `/re agent` command parsing and the safety policy (allowlist, rw mode, rate limit), using
  `RedoxExtensions.Tests/Fakes/FakeDecalEventsProxy.cs`

Needs Windows and a live client:

* Wiring the wrapper and plugin lifecycle across `/re reload`
* Game-thread marshaling and Decal calls under real load
* Real WorldFilter, VT and network message data
* Firewall prompts and loopback behavior
* `PrintWindow` screenshots
* `deploy.bat` changes
* Measuring memory in acclient

## First slice (implemented)

What was built, and where it differs from the design above:

* Everything except the `state` handler lives in `RedoxExtensions.Wrapper/AgentBridge`, including `exec` and the
  event journal, so commands and history keep working through `/re reload`.  The wrapper doesn't use Newtonsoft
  (it would lock the DLL in the install directory and could load a second copy), so it has its own small `MiniJson`.
* The plugin registers `state` in `RedoxExtensions/AgentBridge/AgentHandlers.cs` when it initializes, and removes it
  in `CloseDown`.
* Event kinds: `chat` (Decal `ChatBoxMessage`, minus `[General]`, `[Allegiance]` and `[Trade]` channel chat, see
  `ChatChannelFilter`), `plugin` (text any plugin writes to chat, see "Capturing other
  plugins' chat output"), `debug` (text written to the debug chat window, plus `WrapperDebugWriter.WriteLine` in
  Release builds) and `command` (each `exec`).
* `exec` refuses `/re reload|load|unload|agent` (also when wrapped in `/ub delay`).  Use the `reload` request.
* `deploy.bat` no longer pauses, and returns exit code 1 when no install directory is found.
* `rectl.cmd` at the repo root builds the CLI quietly and runs it, so stdout is pure JSON.

## Usage

In game:

| Command | Effect |
|---|---|
| `/re agent` | Show status (mode, port, connections) |
| `/re agent on` | Enable read-only: `ping`, `state`, `events`, `wait` |
| `/re agent rw` | Also allow `exec` and `reload`, except the blocked commands in `AgentPolicy` |
| `/re agent full` | Also allow the blocked commands |
| `/re agent off` | Stop the server, free the journal, delete the discovery file |

From a terminal on the same machine (`rectl help` for everything):

```
rectl list
rectl state
rectl world --class Monster --near 60
rectl inventory "^Aetheria$" --limit 20
rectl object -263546213
rectl exec /re recordroute start --wait "recording" --kinds plugin,chat
rectl events --since 120 --kinds chat
rectl wait "Route saved" --timeout 30000
rectl dev
```

The tool finds the install directory from `REDOX_EXTENSIONS_INSTALL`, then the same known paths `deploy.bat` checks.
`rectl paths` prints it, the discovery directory and the VirindiTank folder (`VT_DIR`, else
`<install>\..\VirindiPlugins\VirindiTank`, the plugin's default), so tools don't need hard-coded paths.
Use `--dir` or `REDOX_AGENT_DIR` to point it at a discovery directory directly.

Exit codes: 0 means success, 1 means the request failed or a wait timed out, 2 means a usage error or no
single client could be picked.

### Network messages

`rectl net --server F7B0,F74E --client F7B1` picks the decoded message types to journal as `net` events (`rectl net`
shows the selection, `rectl net off` stops it, `all` takes every type in that direction).  Nothing is captured until
then, and the selection is plugin state, so it resets on `/re reload`.  `NetCapture` is called from the plugin's
`ServerDispatch` / `ClientDispatch` handlers, so it's a plugin change, not a wrapper one.

Each message is written generically from Decal's decoded fields (`messages.xml` names), nested structs in braces up to
two levels, cut at 400 characters:

```
S F7B0 character=1342207843 sequence=3271 event=436 attacker=Ancient Honey Bee
C F7B1 sequence=3480 action=63315
S F7B0 ... event=201 object=-262189320 flags=261 success=0 dwordCount=5 dwords={ 0={ key=2 value=9 } ... }
```

Values are as Decal decodes them, so codes like `event` and `action` are decimal (201 = 0x00C9).  Game events
(F7B0) arrive several times a second in combat, so turn capture off when done or it pushes older events out of the
journal.

### Motion

`rectl motion on` starts the motion monitor, which journals `motion` events so an agent can wait for the character to
get stuck instead of polling `state` or reading screenshots:

```
rectl motion on [--stuck 2.5] [--min-speed 0.5] [--min-move 0.5]
rectl wait "^stuck" --kinds motion --timeout 60000
```

* It samples the position about 10 times a second on `RenderFrame` (`MotionMonitor`) and reads what the client is
  trying to do from its MoveToState game action (F7B1 / F61C), which the client sends whenever the movement keys
  change, VT's included (`MoveToStateParser`, from `Message.RawData`).
* States (`MotionAnalyzer`, no Decal dependency, unit-tested):
  * `stuck`: trying to move (forward, backward or sidestep) for `--stuck` seconds and less than `--min-move` meters
    from any position in that time.  Before the first MoveToState it uses "VT is running" as the intent instead
    (`intent=unknown nav=true`), which also catches VT standing still on purpose, so turn the monitor on early.
  * `moving`: at least `--min-speed` m/s over the last second.
  * `idle`: neither.  Trying to move but not yet long enough for `stuck` keeps the previous state, so it doesn't
    flicker through `idle`.
  * `portal`: in portal space and for 5 s after.  A jump of more than 20 m between samples (recall) restarts the
    window.
* Only changes are journaled, plus a repeat every 10 s while stuck, so a `wait` started late still sees one.  Each
  event has the cell, position, heading, speed, intent and VT state, and the previous state with how long it lasted:
  `stuck for=2.6s cell=01A30153 x=50.21 y=-64.52 z=0.00 heading=271 speed=0.0 intent=forward,run nav=true was=moving after=14.2s`
* `rectl motion` (no arguments) shows the state and the last 5 positions one second apart.  `state` gets a `motion`
  block while it's on.
* Plugin state, so it resets on `/re reload`, and changing it only needs `rectl dev`.  The handler only reads, so
  mode `r` is enough.
* `tools/vt/vtfollow.py` uses it and falls back to polling on builds without it.

Why not hook rendering?  acclient draws with Direct3D 9 and Decal ships `D3DService`, so hooking `Present` or the
draw calls is possible, but it yields pixels and triangles, not "the character is trying to run forward and not
getting anywhere".  Reading the depth buffer back in D3D9 needs format tricks, stalls the GPU and costs memory in a
client that's short of address space.  If stuck events turn out not to be enough and the agent needs to know what's
in front of the character, the better source is the collision geometry in `cell.dat` / `portal.dat` (Decal's file
service), raycast along the heading.

Not checked in game yet:

* That `Message.RawData` starts with the F7B1 type (the parser also accepts it without), and that MoveToState
  arrives on `ClientDispatch` when VT moves the character.  If it doesn't, events show `intent=unknown` and the VT
  fallback is used.
* The thresholds against real runs: a VT route into a wall, normal walking, portals and recalls.

### Screenshots

`rectl screenshot [--out <path>] [--scale <n>]` captures the selected client's window (main window of its pid) with
`PrintWindow(PW_CLIENTONLY | PW_RENDERFULLCONTENT)`, which gets the Direct3D scene and every overlay (VVS views, VT and
UB HUDs, chat).  It runs in the CLI process, so nothing changes in acclient, and encodes PNG itself with `ZLibStream`.

* The window can be behind other windows, but not minimized.  Minimized windows aren't drawn, and the command says so.
* At 2560x1431, full size is about 6 MB and `--scale 2` (block averaging) about 1.7 MB.  At half size chat and large
  panels are readable, small HUD text isn't.
* Default path: `%TEMP%\rectl\<character>-<time>.png`.  The JSON output has the path and size.

### World and inventory

`world` and `object` are plugin handlers (`RedoxExtensions/AgentBridge/AgentHandlers.cs`), so changing them only needs
`rectl dev`.  The filtering and paging is in `WorldQuery`, which has no Decal dependency and is unit-tested.

* `world` request: `scope` (`landscape`, the default, or `inventory`), `name` (case-insensitive regex), `class`
  (`ObjectClass` names, list or comma-separated), `container` (id), `maxDistance` (meters), `offset`, `limit` (default
  50, max 200, which keeps a response under the 85 KB large object heap threshold).
* Landscape is sorted by distance, closest first.  Inventory (which includes equipped items) is sorted by name.
* Each object has `id`, `name` and `class`, and `container`, `wielder`, `stack`, `hasIdData`, `distance` (meters), `ns`
  and `ew` when they apply.  The character itself and anything in a container have no distance.
* Every object also has `classId` (the numeric `ObjectClass`, e.g. Portal 14, Npc 37).  Landscape objects have `nav`,
  `[ew, ns, z]` at full precision with z = raw Z / 240, which is what a VT nav "use" node stores for the object (see
  `RouteRecorder.AddPortalNpc` and `tools/vt`).
* The response has `total` (matches before paging) and `nextOffset` when there are more.
* `object` request: `objectId` (not `id`, which is the request id).  Returns the summary fields plus `category`,
  `behavior`, `type` and every `long`, `double`, `string` and `bool` value Decal has, keyed by `*ValueKey` name.

`rectl move <name-regex> --to <container id>` moves matching inventory items into the main pack (the character's id) or
a side pack.  The plugin's `move` handler sends one move and refuses anything outside the character's own containers,
equipped items and packs.  The CLI confirms each move (up to 15 s) before sending the next and stops at the first
failure.  Stacks can merge into a stack already in the destination: the moved item disappears, or the server tops up
the destination stack and leaves the rest behind with a new stack size.  Both count as `merged`, and the CLI re-scans
and moves what's left (up to 5 passes).  Found on Rockdown, where a 5 s timeout and no merge handling stopped the run
about a dozen times while sorting 160 items.  `move` needs `rw`.  The wrapper only checks read access for plugin handlers, so `move` reads the mode from
the discovery file itself (`RequireWriteMode`).  That check belongs in `AgentRequestProcessor` at the next wrapper
change.

* `Actions.MoveItem(id, dest)` (two arguments, `MoveItemEx`) sends nothing on this client.  The four-argument
  `MoveItem(id, pack, 0, false)` sends game action 25 (put item in container) and works.
* Side packs on this server hold 102 items (Decal's `ItemSlots`), not the usual 24.
* `--to main` is the main pack and `--to pack<N>` the side pack at slot N (0 = first in the inventory window).  The
  order comes from `Values(LongValueKey.Slot)`, which Decal computes on request, so it's not in `LongKeys` and the
  `object` dump doesn't show it.  `world`/`inventory` report it as `slot` for anything in a container.  Found by
  decompiling mudsort, whose `/ms set dest pack N` picks the pack with `Slot + 1 == N`; checked on Virmar against the
  pack the user clicked as pack 0.  (`OpenedContainer` stays 0 for your own packs.)
* Checked on Virmar: 41 Titan Mana Charges moved into a side pack in 7 seconds.

Checked in game on Virmar: nearby doors, a player and a monster with distances and coordinates; 297 inventory items
paged; equipped bow and arrows show `wielder`; `object` returns the key dump; a bad id or `--near far` gives an error and
exit code 1.

## Windows checks

Checked on 2026-11-05 with one client (Virmar):

* Vitals: `CharacterFilter.Health/Stamina/Mana` are current values and `EffectiveVital[...]` are max, as assumed.
* `Host.Actions.AddChatText` does not raise `ChatBoxMessage`, so RE output is only a `plugin` event, never also `chat`.
  The same means other plugins' `AddChatText` output (VT, UB, MagTools) never reaches the journal.
* No firewall prompt on enable.
* `rectl dev` deploys, reloads and reconnects well within the timeout.
* `exec /re help --wait ...` round trip works.

Fixed along the way:

* `/re agent ...` also printed "RT Unknown Command".  Decal raises `CommandLineText` for every subscriber regardless of
  `Eat`, so the plugin's `/re` handler also saw it, and it returned `false`, which reset the wrapper's `Eat`.  The
  plugin now claims `agent` as a no-op.  (`load`/`unload`/`reload` dodge this only because the plugin isn't
  subscribed at that moment.)
* `state` no longer includes `vt`.  `VTUtilities.GetVTOptions` waits for VT's `[VTank] Option ...` reply on
  `CommandLineText`, which never arrives (no "Captured VT Option" line in any character's log), so it always
  returned `{}`.  This affects existing callers too (`UnstuckActions`, `VTStateScope`, `VTSettingsSnapshot`).
* Text written to the debug chat window is journaled as `debug`, not `plugin`.  `WrapperDebugWriter.WriteLine`
  publishes its own `debug` event only in Release builds, where it doesn't write to chat.  (This wrapper part
  needs a client restart.  Until then wrapper debug lines show up as both `plugin` and `debug`.)
* `rectl dev` ran `copy_dlls_debug.bat` by name, which fails when `NoDefaultCurrentDirectoryInExePath` is set (it is
  in Claude Code's shell).  It now uses the full path.
* `deploy.bat` failed with a sharing violation on `RedoxLib.dll`, which running clients keep locked.  It now copies
  `RedoxLib`, `RedoxFilter` and the wrapper with `xcopy /D`, so files that weren't rebuilt are skipped.  Debug builds
  aren't deterministic (full PDBs), so a rebuilt but unchanged `RedoxLib` still looks new.  A locked `RedoxLib` or
  `RedoxFilter` is now a warning, like the wrapper.  If `RedoxLib` really changed, the plugin reload fails to load
  and the clients need a restart.

Still open:

* VT state (see "What's left" under "Handoff notes").  `utank2-i.dll` also exposes
  `uTank2.PluginCore.PC.GetExternalInterface()` returning `cExternalInterfaceTrustedRelay` (`GetSetting`,
  `MacroEnabled`, `GetNavProfile`, `GetSettingsProfile`, `NavCurrent`, ...).  Not tried, because it's unknown whether
  VT shows a permission prompt.
* Memory (see "What's left").  With Virmar and Woodford both enabled, private bytes were 635 and 620 MB, but that can't
  be compared with the bridge-off reading from earlier (different session and uptime), so it doesn't isolate the
  bridge's cost.

Selection rules, checked with Virmar and Woodford both in `rw`: no flag lists both and exits 2, `-c wood` (prefix,
case-insensitive), `-c Virmar` and `--pid` each pick the right client, and an unknown `-c` lists both and exits 2.
Each client installs its own chat capture (`chatCapture: true` on both).

Removed: `CreateObject` debug logging.  It ignored `DebugLevel`, filled the journal (about 1,300 events in 3 minutes of
combat) and grew `RT_Debug_<char>.txt` to 1.6 GB.

## Capturing other plugins' chat output

Plugins write with Decal's `IACHooks.AddChatText` / `AddChatTextRaw`.  That draws straight into the chat UI and doesn't
raise `ChatBoxMessage`, so VT, UB, MagTools and RE output never reached the journal through Decal events.  UB's chat
logger has the same limit (it's built on `ChatTextInterceptEventArgs`), and VCS only exposes `FilterOutputText`, a way to
send text, not to see it.

`RedoxExtensions.Wrapper/AgentBridge/ChatTextCapture` patches those two slots of the hooks object's COM vtable:

* Every plugin uses Decal's one hooks object, so one patch sees all of them, RE included.
* Slots come from Decal's real `IACHooks` type at runtime (metadata order after the 7 IUnknown/IDispatch slots:
  `AddChatText` = 11, `AddChatTextRaw` = 12), cross-checked against `Marshal.GetComSlotForMethodInfo`.  If anything
  doesn't match, nothing is patched and a `debug` event says why.
* The hook reports the text, then calls whatever was in the slot before, so another plugin hooking the same slot (before
  or after us) keeps working.
* Installed on the first `/re agent on` and never removed.  Removing it could cut out a plugin that hooked after us.
  With the bridge off it only forwards the call.
* Text to the debug chat window (target 4) is journaled as `debug`, everything else as `plugin`.  There's no source
  plugin field.  Plugins usually prefix their output (`[VTank]`, `[UB]`, `RT`).
* `ping` reports `chatCapture: true` when it's installed.  Without it, only the wrapper's own chat output is journaled.
* Not seen: calls made late-bound through `IDispatch::Invoke`, and anything a plugin draws into its own window (VVS
  consoles) without going through `AddChatText`.

Verified in game on 2026-11-05: `rectl ping` shows `chatCapture: true`, and
`rectl exec "/vt opt get EnableCombat" --wait "EnableCombat" --kinds plugin` matched `[VTank] Option EnableCombat = True`.
VT's startup messages were captured too, and RE's own output showed up once.

* Target 4 is just the window RE uses for debug output.  Another plugin writing there (VT writes some messages to
  several windows) is also tagged `debug`.
* Changing the wrapper needs every client closed, since the wrapper DLL is locked while any client is running.

## Verification

* Unit tests (net10): `dotnet test RedoxExtensions.slnx --filter "FullyQualifiedName~AgentBridge"`.  112 tests in
  `RedoxExtensions.Tests/AgentBridge/`, all passing as of `79d2c8d`.  `CliTests` run the real CLI against a real
  loopback server with a fake game host.  `ChatTextCaptureTests` patch a fake vtable in unmanaged memory.  The full
  suite has failures that predate this feature (stale tests, `PipelineDispatcherTests`).
* In game, everything in "Windows checks", "Capturing other plugins' chat output", "Network messages", "Screenshots"
  and "World and inventory" was checked on 2026-11-05 with Virmar and Woodford.

## Handoff notes

### Where it stands (2026-11-05, branch `gamedata`, last commit `79d2c8d`, not pushed)

Implementation is paused here.  Everything below is committed and the working tree is clean.

Done and checked in game:

* Bridge, discovery, selection rules (`-c` prefix, `--pid`, refusal with several clients), `exec` + `wait`, `rectl dev`.
* Chat capture of every plugin through the `AddChatText` vtable hook (wrapper; needed a client restart, done).
* `world` / `inventory` / `object` (plugin handlers).
* `net` capture of chosen message types (plugin).
* `rectl screenshot` (CLI only).

Live state when paused: Virmar (pid 57256) and Woodford (pid 12384) were both in `rw` mode on the current build, net
capture off.  The bridge is per session, so it's off after a client restart until `/re agent on|rw` is run again.

Commits this session, oldest first:

| Commit | What |
|---|---|
| `5c844ca` | Plugin claims `/re agent` so it doesn't print "Unknown Command" |
| `98a94ab` | `rectl dev` runs `copy_dlls_debug.bat` by full path |
| `ab146dc` | `deploy.bat` skips unchanged locked DLLs (`xcopy /D`) |
| `9ab86cc` | `state` drops the always-empty `vt` field |
| `3f0aaed` | Debug chat window output journaled as `debug` |
| `d16a928` | Doc: first Windows checks |
| `cc6e1cd` | Remove per-object `CreateObject` debug logging (1.6 GB logs) |
| `4318196` | Chat capture of all plugins (`ChatTextCapture`, `VTableSlotPatch`) |
| `9a3add0`, `b02df03` | Doc: capture and two-client checks |
| `decf796` | `deploy.bat` warns instead of failing on a locked `RedoxLib` / `RedoxFilter` |
| `88dadec` | `world`, `inventory`, `object` |
| `a2adc29` | `net` capture |
| `79d2c8d` | `rectl screenshot` |

### What's left, and what each needs

| Item | Needs a client restart? | Notes |
|---|---|---|
| Fix `VTUtilities.GetVTOptions` / `VTCaptureContext` | Yes (wrapper) | VT's `/vt opt get` replies come through `AddChatText`, which only the wrapper's `ChatTextCapture` sees.  The plugin needs a way to subscribe, e.g. a public multi-listener event on `AgentBridgeService` that works even with the bridge off (install the hook at wrapper startup, or on first subscription).  Then `GetVTOptions` can wait for the `[VTank] Option` lines, `state` can get `vt` back, and `UnstuckActions`, `VTStateScope` and `VTSettingsSnapshot` start working.  Alternative without the hook: `uTank2.PluginCore.PC.GetExternalInterface()` (unknown whether VT prompts for permission). |
| Memory numbers | Yes (wrapper) | Add `GC.GetTotalMemory(false)` and the journal count to `ping`, then compare before and after `/re agent rw` on one client.  Process private bytes are too noisy (475-635 MB between clients) to show a few MB. |
| MCP stdio server | No (CLI only) | Wrap the same client code as MCP tools.  Best done now that the command set has settled. |
| JSONL journal file | Yes if in the wrapper | Optional. |

Batch the two wrapper items into one restart.

### Things that will trip up the next session

* **Wrapper changes need every client closed.**  The wrapper DLL is locked while any client is running, so
  `deploy.bat` can't copy it (it only warns).  Plugin changes only need `rectl -c <name> dev`.  With several clients
  enabled, `dev` and `reload` need `-c`, and each client has to be reloaded separately to pick up a plugin change.
* **Plugin code must not call new wrapper APIs until the wrapper is redeployed and clients restarted.**  A plugin built
  against a newer wrapper runs against the old one after `rectl dev` and fails with `MissingMethodException`.
* **`NoDefaultCurrentDirectoryInExePath=1` is set in Claude Code's shell**, so `cmd /c something.bat` doesn't find
  scripts in the current directory.  Use full paths (`& "F:\...\copy_dlls_debug.bat"`).
* **`deploy.bat` warnings about `RedoxLib` / `RedoxExtensions.Wrapper` not being copied are normal** when clients are
  running and those projects were only rebuilt (Debug builds aren't byte-identical).
* **Screenshots need a window that isn't minimized.**  The user minimizes clients, so ask them to restore one.
* **net35 limits**: no `ValueTuple` in plugin or wrapper code, no `Marshal.GetComSlotForMethodInfo` on .NET 10 (tests).
* **Request field `id` is the request id**, echoed in the response.  Handler parameters need other names (`objectId`).
* **Python isn't installed.**  Use PowerShell, the Bash tool with standard tools, or the edit tools.

### Code map

* Wrapper (`RedoxExtensions.Wrapper/AgentBridge/`, restart to change): `AgentBridgeSession` (Decal-bound, starts chat
  capture), `AgentRequestProcessor` (routing; unknown commands go to plugin handlers), `AgentBridgeServer` (TCP),
  `AgentPolicy`, `EventJournal`, `GameThreadQueue`, `AgentDiscovery`, `MiniJson`, `AgentHandlerRegistry`,
  `AgentBridgeService` (static entry point), `ChatTextCapture` + `VTableSlotPatch` (the `AddChatText` hook).
* Plugin (`RedoxExtensions/AgentBridge/`, `rectl dev` to change): `AgentHandlers` (`state`, `world`, `object`, `net`),
  `WorldQuery`, `NetCapture`.  `NetCapture` is called from `Core/DecalEventsProxy.cs`.
* CLI (`RedoxExtensions.AgentCli`, assembly `rectl`, no client change): `Cli`, `InstanceLocator`, `BridgeClient`,
  `ScreenCapture`.  `rectl.cmd` builds it quietly and runs the AnyCPU build from `bin\Debug\net10.0`.

### Decisions worth keeping

* The transport stays in the wrapper so it survives `/re reload`.  Keep it small and stable, because every
  wrapper change costs a client restart.  New features should be plugin handlers registered through
  `AgentBridgeService.Handlers`.  They run on the game thread and receive and return plain
  `Dictionary<string, object>` / `List<object>` / primitives, since no types are shared across the reload boundary.
* No Newtonsoft in the wrapper (see "First slice").
* The blocked-command list in `AgentPolicy` only uses commands documented under `docs/ref/InGameCommands`, per
  AGENTS.md.  Add new entries from those docs, not from memory.
* `exec` returns `seq`, the journal position before the command ran.  Pass it as `since` to `wait` so output that
  arrives before the wait starts isn't missed.  `rectl exec --wait` already does this.
* Reading the character name or server from a connection thread isn't safe.  `AgentBridgeSession` caches them when
  it starts.  Anything else that touches Decal has to go through `InvokeOnGameThread`.
* The chat hook is never removed once installed (see "Capturing other plugins' chat output").
* Pure logic (filtering, formatting, parsing) is kept free of Decal types so it can be unit-tested on net10:
  `WorldQuery`, `NetCapture.Format` via `INetFields`, `ScreenCapture.WritePng`.
* `VTNavRouteTests` was switched to `ClassicAssert` because it no longer compiled under NUnit 5 and blocked the
  whole test project.
