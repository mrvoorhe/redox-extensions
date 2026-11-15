---
name: meta-chain-watch
description: List or change the plugin's Meta Chain Watch timeouts (`rectl metatimeout`), the per-meta limits that unstick the user's Asheron's Call VT meta chain, and read what the watcher did (`/re metachainwatch`, `[RE] MetaChainWatch` lines in the bridge journal). Use when asked about meta timeouts, the chain watchdog, why a character jumped ahead in the chain or fell back to farming, or to add, change or remove a meta's timeout or the fallback command.
---

# Meta Chain Watch

The plugin times each VT meta from its `/vt meta load`.  A meta that runs past `min(override, main)` is first
recalled (`/vt stop`, nav cleared, peace, then the Halls of Introduction Gem by exact name; 30 s per try, 2 tries,
then it carries on regardless), then sent to its chain state (`** Chain **`, `* Chain *`, `TT-chain` or `Chain`, or the entry's `chainState`).  If no other meta loads
within 1 minute, or there is no chain state, the fallback command runs (default `/re sm aFarm`).  Daily turns it on
with `/re metachainwatch on 60` and aFarm turns it off.  See `docs/design/Feature-MetaChainWatch.md`, including the
seeded table and why each value was chosen.

Run `rectl` from PowerShell, not the Bash tool: Git Bash mangles `/re ...` arguments.

## Timeouts (`rectl metatimeout`)

The table lives in `<install>\MetaChainWatch\Global.json`, with per-character overrides in `<Character>.json`.
rectl edits the files directly: the bridge doesn't need to be on, and the plugin picks changes up within about
5 seconds.

* **List:** `.\rectl.cmd metatimeout list` (global only) or `.\rectl.cmd metatimeout list -c <Character>` (merged,
  with each entry's `source`: `global` or `character`, plus `fallbackSource` and `chainStatesSource`).
* **Set:** `.\rectl.cmd [-c <Character>] metatimeout set <meta> <minutes> [--chain-state "<state>"]`.
  Without `-c` it changes `Global.json` for every character.  Minutes take decimals.
* **Remove:** `.\rectl.cmd [-c <Character>] metatimeout remove <meta>`.  Removing a character entry brings back the
  global one.
* **Fallback:** `.\rectl.cmd [-c <Character>] metatimeout fallback <command>` or `fallback --clear`.

Things to check before changing a value:

* An override only counts when it is **shorter than the main timeout** (60).  Setting a meta above 60 does nothing.
  Don't suggest lowering the main timeout below 60: KinokoPublic2.0 runs up to 48.5 minutes normally.
* Base new values on the run data in `../MetaAnalysis` (the parent repo's `meta-chain-monitor` skill): longest normal run x 1.5, rounded up
  to 5, at least 10.  For aKT metas, stay above hunt limit + turn-in overhead.  If the user raises a hunt limit in
  a meta, check its override still covers it.
* Meta names are the names used in `/vt meta load`, matched case-insensitively, without `.met`.
* `-c` must be the exact character name.  If `set` prints "Note: ... has no meta chain watch file yet", check the
  spelling with the user.
* Only change what the user asked for.  These settings affect every character's daily chain.

## In game (`/re metachainwatch`, alias `mcw`)

* `/re metachainwatch` (or `status`) shows on/off, the phase, the current meta with elapsed / limit, the fallback,
  the chain states and the merged table.
* The recall is set by `recall` in the same config files: `{ "enabled": true, "gem": "Halls of Introduction Gem",
  "landblock": "0x012F" }` (fields optional, character over global over defaults).  rectl has no command for it:
  edit the JSON, and only when the user asks.  `rectl metatimeout list` shows the merged `recall`.
* `on <minutes>`, `off`, `reload`.  On/off is saved to `<install>\MetaChainWatch\State\<Character>.json`, so after
  a crash, relog or plugin reload within 4 hours the watch turns back on by itself (`resumed after restart`), with
  the meta's run time kept but the downtime not counted.  Don't edit that file.

With the bridge on, read the status with `.\rectl.cmd -c <Character> exec "/re metachainwatch" --wait "fallback :"`.
Don't send `on` or `off` to a character that is running its chain unless the user asks.

## Reading what the watcher did

Every watcher message is a chat line starting `[RE] MetaChainWatch : `, journaled as a `plugin` event:
`.\rectl.cmd -c <Character> events --kinds plugin --max 200`, or the `meta-chain-monitor` recordings.

| Line | Meaning |
|---|---|
| `on, 60 min (meta: Daily)` / `off` / `already off` | Turned on (by Daily) or off (by aFarm). |
| `config loaded : <n> timeout overrides, fallback <cmd>` | A config file was read or changed. |
| `config file unreadable, ignoring it : <path> : <error>` | Bad JSON.  Nothing from that file is used until it's fixed. |
| `<meta> timed out after <n> min (override\|default)` | The meta hit its limit.  `default` means the main timeout. |
| `<meta> using Halls of Introduction Gem (try 1 of 2)` | The recall after a timeout.  `try 2 of 2` follows `no teleport within 30 s, trying ... again`. |
| `<meta> recalled, at 0x012F0126` | He arrived in the Halls. |
| `<meta> already at <landcell>, not using <gem>` | He was already in the Halls; no gem. |
| `<meta> no '<gem>' in inventory, carrying on without the recall` | The gem is missing.  Tell the user; the hand-off still ran from where he was. |
| `<meta> recall failed, not in 0x012F after 2 tries, carrying on` | Both gem uses failed (busy, combat, corpse open...).  The hand-off ran from where he was. |
| `<meta> -> state '<state>'` | It was sent to its chain state.  The next `/vt meta load` should follow within a minute. |
| `<meta> no chain state, running fallback : <cmd>` | No chain state found (dispatchers have none). |
| `<meta> no next meta after state '<state>', running fallback : <cmd>` | The chain state didn't load a meta within 1 minute. |

Any timeout means a meta got stuck: find out why from the VT `Meta transitioning` lines before it.  A jump skips
the meta's normal exit cleanup (see Known gaps in the design doc), e.g. aKTHoshino leaves `HoshinoRun=1`.
