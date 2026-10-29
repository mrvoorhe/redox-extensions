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
* Event kinds: `chat` (Decal `ChatBoxMessage`), `plugin` (text RE and the wrapper write to chat), `debug` (every
  `WrapperDebugWriter.WriteLine`, which includes the plugin's `DebugWriter`) and `command` (each `exec`).
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
rectl exec /re recordroute start --wait "recording" --kinds plugin,chat
rectl events --since 120 --kinds chat
rectl wait "Route saved" --timeout 30000
rectl dev
```

The tool finds the install directory from `REDOX_EXTENSIONS_INSTALL`, then the same known paths `deploy.bat` checks.
Use `--dir` or `REDOX_AGENT_DIR` to point it at a discovery directory directly.

Exit codes: 0 means success, 1 means the request failed or a wait timed out, 2 means a usage error or no
single client could be picked.

## Still to check on Windows

* `CharFilterVitalType` and `CharacterFilter.Health/Stamina/Mana` return what the `state` handler assumes (current
  vs. max).
* Whether `Host.Actions.AddChatText` also raises `ChatBoxMessage`.  If it does, RE output shows up as both `plugin`
  and `chat` events.
* `GetVTOptions` inside `state` works when called from RenderFrame and doesn't spam chat.
* A Windows Firewall prompt on the first enable (loopback-only binding shouldn't trigger one).
* `reload` finishes within the 10 second game-thread timeout.  `rectl reload` polls `ping` if it doesn't.

## Verification

* Unit tests (net10): `dotnet test RedoxExtensions.slnx --filter "FullyQualifiedName~AgentBridge"`.  87 tests in
  `RedoxExtensions.Tests/AgentBridge/`, all passing as of `db32452`.  `CliTests` run the real CLI against a real
  loopback server with a fake game host.  The full suite has 27 failures that predate this feature (stale tests).
* On Windows, with two clients logged in:
  * `/re agent rw` on one client, then `rectl state` should pick it automatically.
  * Enable on the second client: `rectl state` should error and list both, and `rectl -c <name> state` should work.
  * `rectl exec /re help --wait <some text from the help output>` should complete a round trip.
  * `rectl dev` should survive the reload.
  * `/re agent off` should make `rectl` refuse.
  * Compare acclient private bytes before and after enabling.

## Handoff notes

Notes for picking this up on the Windows machine, where there's no history of the Mac session that built it.

### State as of `db32452` (branch `gamedata`)

* The design doc (`e05be91`) and first slice (`db32452`) are committed.  All of it was written and unit-tested on
  macOS.  None of it has run inside acclient yet.
* Wrapper code is in `RedoxExtensions.Wrapper/AgentBridge/`:
  * `AgentBridgeSession`: the Decal-bound part
  * `AgentRequestProcessor`: request routing
  * `AgentBridgeServer`: TCP
  * `AgentPolicy`: modes, blocked commands, rate limit
  * `EventJournal`, `GameThreadQueue`, `AgentDiscovery`, `MiniJson`, `AgentHandlerRegistry`
  * `AgentBridgeService`: the static entry point the plugin and debug writers call
* Hooks into existing code:
  * `REWrapperPlugin.SetAgentMode` / `ReloadWrappedPlugin`
  * `WrapperCommandListener.HandleAgentCommand`
  * `WrapperDebugWriter.WriteLine`, which publishes `debug` events
  * `WriteToChat` in both `REWrapperPlugin` and `REPlugin`, which publishes `plugin` events
  * `REPlugin.Initialize` / `CloseDown`, which register and unregister `AgentHandlers`
* The CLI is `RedoxExtensions.AgentCli` (assembly `rectl`).  `rectl.cmd` runs the AnyCPU build from
  `bin\Debug\net10.0`.  The x86 deploy build puts its copy under `bin\x86`, and that copy isn't used.

### First Windows session, in order

1. Run `copy_dlls_debug.bat`, then restart the client.  The wrapper changed, and `/re reload` doesn't reload the wrapper.
2. In game, run `/re agent rw`.  Expect "Agent bridge rw on port N" in chat and
   `<install>\Logs\agent-bridge\<pid>.json` on disk.
3. Run `rectl.cmd ping`, then `rectl.cmd state`.  Check the vitals and Virindi values against the game
   (see "Still to check on Windows").
4. Run `rectl.cmd events --kinds chat,plugin`.  Check whether RE output appears twice.  If it does, decide whether to
   drop `plugin` or filter `chat`.
5. Run `rectl.cmd exec /re help --wait <text from the help output>`, then `rectl.cmd dev` after a trivial plugin
   change.
6. Enable a second client and check the selection rules: no flag should list both clients, `-c` and `--pid` should
   each pick one.
7. Record acclient private bytes with the bridge off, then after `/re agent rw` and a few minutes of play.
   Update "Memory budget" with the real numbers.

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
* `VTNavRouteTests` was switched to `ClassicAssert` because it no longer compiled under NUnit 5 and blocked the
  whole test project.

### Next steps after the Windows check

* `world` handler: WorldFilter objects and inventory, paged
* Network message journaling from `ServerDispatch` / `ClientDispatch`, limited by an allowlist of message types
* `rectl screenshot` using `PrintWindow`.  It should encode PNG itself with `ZLibStream`, to avoid needing
  System.Drawing.
* An MCP stdio wrapper around the same client
* Optionally, a JSONL file for the journal
