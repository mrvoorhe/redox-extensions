# Mag-Tools (`/mt`) Commands

> Source: <https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Tools-Command-Line> (last modified 2026-09-01; retrieved 2026-10-20).

Chat commands accepted by the Mag-Tools plugin. Type them in the in-game chat box.

## Notation

- `[a,b]` — choose exactly one of the listed literal values.
- `[name]` — a value you supply (item name, target, option name, etc.).
- `[p]` — optional `p` suffix on the command name enables partial name matching on `[item]` (and `[target]`). Example: `/mt usep` instead of `/mt use`.
- `[on [target]]` — optional. When present, `[item]` is searched for in inventory only.
- Commands with no arguments are used as-is.

## Client

| Command | Description |
| --- | --- |
| `/mt logoff` | Logs out. `/mt logout` is the same command. |
| `/mt quit` | Quits the client. `/mt exit` is the same command. |
| `/mt client minimize` | Minimizes the client window. |

## Input

| Command | Description |
| --- | --- |
| `/mt send [enter,pause,space]` | Sends the given key. |
| `/mt send cntrl+[C]` | Sends Ctrl plus the given key. |
| `/mt send [f4,f12]` | Sends the given function key. |
| `/mt send [del]` | Sends the Delete key. |
| `/mt send msg [keys to press]` | Types the given keys. |
| `/mt click [ok,yes,no]` | Clicks the given button on an open dialog. |
| `/mt click [X] [Y]` | Clicks at the given window coordinates. |
| `/mt get xy` | Reports the current cursor X/Y coordinates. Useful for finding values for `/mt click [X] [Y]`. |

All `/mt click` commands cause the client window to partially regain focus without taking your screen focus or cursor. The client then runs at its normal focused CPU usage, even when the Mag-Tools FPS Manager is in use (observable in Windows Task Manager). Clicking into the client and back out again re-engages the Mag-Tools idle FPS manager (or AC's default 10 FPS when unfocused).

## Movement

| Command | Description |
| --- | --- |
| `/mt face [degrees]` | Turns to face the given heading. |
| `/mt [s]jump[w,z,x,c] [milliseconds]` | Jumps, holding the jump key for `milliseconds`. `s` prefix holds Shift. The optional suffix holds a movement key during the jump (`w` is forward). Examples: `/mt jump 500`, `/mt sjumpw 300`. |
| `/mt movement [movement_key] [milliseconds_to_hold_key]` | Holds the given movement key for the given duration. |

## Fellowship

| Command | Description |
| --- | --- |
| `/mt fellow create [name]` | Creates a fellowship with the given name. |
| `/mt fellow [open,close,disband,quit]` | Opens, closes, disbands, or quits the current fellowship. |
| `/mt fellow recruit [name]` | Recruits the named player into the fellowship. |

## Magic and Combat

| Command | Description |
| --- | --- |
| `/mt cast[p] [#,name] [on [target]]` | Casts a spell by id or name, optionally on a target. |
| `/mt combatstate [magic,melee,missile,peace]` | Switches to the given combat mode. |
| `/mt attack_melee [closest]` | Melee attacks the closest target. |

## Items

| Command | Description |
| --- | --- |
| `/mt select[p] [item]` | Selects an item. |
| `/mt use[p] [item] [on [target]]` | Uses an item, optionally on a target. |
| `/mt use [closestnpc,closestvendor,closestportal]` | Uses the closest NPC, vendor, or portal. |
| `/mt usei[p] [item] [on [target]]` | Like `use`, but searches inventory items only. |
| `/mt usel[p] [item] [on [target]]` | Like `use`, but searches landscape items only. |
| `/mt give[p] [item] to [target]` | Gives an item to a target. |
| `/mt loot[p] [item]` | Loots an item. |
| `/mt drop[p] [item]` | Drops an item. |
| `/mt equip[p] [item]` | Equips an item. |
| `/mt dequip[p] [item]` | Unequips an item. |
| `/mt autopack` | Runs Mag-Tools inventory auto-packing. |

Examples:

```
/mt useip mote on mote
/mt uselp key on door
/mt usel Colloseum Key on Colosseum Chest
```

## Trade

| Command | Description |
| --- | --- |
| `/mt trade add[p] [item]` | Adds an item to the open trade window. |
| `/mt trade [accept,decline,reset,end]` | Accepts, declines, resets, or ends the current trade. |

## Vendor

| Command | Description |
| --- | --- |
| `/mt vendor addbuy[p] [item] [count]` | Adds `count` of an item to the buy list. |
| `/mt vendor addsell[p] [item]` | Adds an item to the sell list. |
| `/mt vendor [buy,clearbuy,sell,clearsell]` | Completes or clears the buy or sell list. |

## Game Info

| Command | Description |
| --- | --- |
| `/mt dumpspells` | Dumps the spell table to `c:\mt spelldump.txt`. |

## Options

| Command | Description |
| --- | --- |
| `/mt opt list` | Lists all options. |
| `/mt opt get [option]` | Shows an option's current value. |
| `/mt opt set [option] [value]` | Sets an option. |
| `/mt opt remember [option]` | Saves an option's current value. |
| `/mt opt restore [option]` | Restores an option to the value saved with `remember`. |

`remember`/`restore` allow temporarily changing an option and putting it back afterwards:

```
/mt opt remember InventoryManagement.KeyRinger
/mt opt set InventoryManagement.KeyRinger false
/mt opt restore InventoryManagement.KeyRinger
```

## Related wiki pages

- [Mag-Tools](https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Tools) — plugin overview.
- [Mag-Tools Misc](https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Tools-Misc), [Tools](https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Tools-Tools), [Trackers](https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Tools-Trackers), [Loggers](https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Tools-Loggers) — feature pages, including settings controlled by `/mt opt`.
