---
name: vt-meta
description: Build, read and test Virindi Tank (VT) metas (.met) for the user's Asheron's Call characters, including metas with embedded navs. Use when asked to make or change a VT meta, explain what a meta does, or test one in game.
---

# VT metas

Tools are in `tools/vt` (Python 3; run with `py`).  For navs (recording, building, the nav format, the in-game
test loop) see the `vt-nav` skill; a meta that travels usually needs a nav built that way first.

The VT folder is the user's VirindiTank directory (`py tools/vt/rectl.py vt-dir` prints it).  Read existing metas there before designing one:
the `aKT*` / `aQD*` metas are the user's own and show their conventions.  Never overwrite a file without asking.

```
py tools/vt/vtmeta.py dump <file.met>      # [State] IF <condition>  DO <action>, one line per rule
py tools/vt/vtmeta.py check "<glob>"       # read + write gives identical bytes (all 111+ of the user's metas do)
```

## Building

Write a small Python script (scratchpad, or next to the example if the user wants to keep it) that lists rules as
`(state, condition, action)` and calls `vtmeta.save(path, rules)`.  `tools/vt/examples/aFJamBridge.py` is a tested
meta: load a settings profile, travel with an embedded nav and combat off, then hold a spot and fight.

```python
import os, rectl, vtmeta as m
vt = rectl.vt_dir()
rules = [
    ('Default', m.always(), m.chat('/vt settings loadchar Farm')),
    ('Default', m.all_c(m.seconds_in_state(2), m.landcell(0x01A30153)), m.set_state('Farm')),
    ('Farm', m.always(), m.all_a(m.embed_nav(os.path.join(vt, 'JamBridge.nav')), m.opt('EnableCombat', True))),
    ('Farm', m.not_c(m.landblock(0x01A30000)), m.set_state('Default')),
]
m.save(os.path.join(vt, '<Name>.met'), rules)
```

Then `vtmeta.py dump` the result and read it back before testing.

Conditions: `never always all_c any_c chat_message seconds_in_state nav_empty died monster_count no_monsters_within
landblock landcell portal_enter portal_exit not_c seconds_in_state_persist expr_c`.
Actions: `none set_state chat all_a call_state return_from_call expr_a watchdog clear_watchdog opt embed_nav`.
Each is `(type number, encoded data)`; `vtmeta.COND` / `vtmeta.ACT` name the numbers.  For anything not covered,
find a meta in the VT folder that uses it, `vtmeta.load` it and copy the encoding; don't guess.

## Encoding notes (all taken from the user's metas)

* Top table columns `CType AType CData AData State`, one row per rule, every value typed (`i`, `s`, `d`, `b`, `ba`,
  `TABLE`).  Lists of child conditions/actions (All, Any, Not) are `TABLE` with columns `K V` (K = type number);
  option/expression data is `TABLE` with `k v`.
* `SetVTOption`: `{o: name, v: "True"/"False"/value}` as strings.
* `LandCellE` is the landcell as a signed int; `LandBlockE` is the landblock in the high word (`0x01A30000`).
* `EmbedNav` is a byte blob: nav name, `\r\n`, node count, `\r\n`, then the whole nav file.  The nav is copied in, so
  the meta doesn't need the .nav file at runtime; rebuild the meta after changing a nav.
* No-data conditions/actions (Always, Died, ...) store `i 0`.

## VT behaviour worth knowing

* A rule fires when its condition becomes true, so `Always` runs once each time the state is entered.  Put
  "on entering this state" setup there.
* VT logs every meta step to chat: `[VTank] Meta executing action: ...` and `[VTank] Meta transitioning to state: X`.
  They show up as `plugin` events in the bridge journal, which is how to check a meta in a test.
* Settings profiles: per-character profiles (`--<Char>_<Server>_<Name>.usd`) load with
  `/vt settings loadchar <Name>`.  `/vt settings load <Name>` doesn't find them: it silently creates a new shared
  `<Name>.usd` full of defaults and loads it (EnableMeta goes off).  If that happens, `loadchar` the right one and
  delete the stray file.
* A profile load changes options (EnableCombat, EnableNav, ...).  Wait a couple of seconds after it
  (`seconds_in_state(2)`) before rules that set options, or the profile can undo them.
* A profile that turns nav on while waiting makes the character walk toward whatever nav was loaded before (seen:
  20 m the wrong way in Abandoned Mines).  Turn nav off on entering the state and again at `seconds_in_state(1)`,
  after the profile has loaded; the next state turns it on with its own nav.
* Don't use one `LandCellE` to mean "arrived at the spot": a spot on a cell boundary can end the route in the
  next cell, and the meta never leaves its travel state (combat stays off).  Use a distance expression instead, as
  in `tools/vt/examples/aFJamPillar.py`: squared flat distance in nav units from `coordinategetwe/ns[getplayercoordinates[]]`
  plus a height check with `coordinategetz` (nav units, z / 240).  Needs `/ub opt set VTank.PatchExpressionEngine true`.
* Test expressions with `/ub mexec <expr>` (UB prints `[UB] Result: [type] value`).  `/vt mexec` always uses VT's
  own engine, even with the patch on, so UB functions (`getquestktprogress`, `setpvar`, `sqrt`,
  `getplayerlandblock`, ...) report "unknown function" there, and `<-0.4` and backtick strings fail to parse.  Under
  `/ub mexec` all of these work.  The patch covers expressions inside metas, so a meta can use them.
* Kill tasks: `getquestktprogress[flag]` / `getquestktrequired[flag]` read `/myquests` data, which doesn't update as
  you kill: run `/ub myquests` (hidden output, "[UB] Quest data updated.") to refresh.  A flag that doesn't exist
  gives required 0.  Find flag names by diffing `/myquests` output before and after talking to the NPCs.
* `/vt setattackbar <0..1>` sets attack power.  The Farm profile has `AutoAttackPower` False; with it True, VT probably
  manages the bar itself (not tested).
* Only use commands documented under `docs/ref/InGameCommands` in chat actions (AGENTS.md).

## Testing in game

1. Reset the character to a start point (ask the user how, if it isn't clear).  `/vt stop` first if it's fighting.
2. `/vt meta load <Name>` (starts at Default), make sure `EnableMeta` is True, `/vt start`.
3. Follow with `py tools/vt/vtfollow.py <Character> --nav <destination nav>` and read the meta log lines from the
   journal (`rectl -c <Char> events --since <seq> --kinds plugin`).  Check the options the meta should have set with
   `rectl.vt_option`.
4. Test each path you can reach safely (e.g. starting away from the spot and starting at it).  Death and similar
   branches usually can't be tested: say so.
5. Leave the character as the user wants it (usually: their meta loaded and running), and say what you changed.
