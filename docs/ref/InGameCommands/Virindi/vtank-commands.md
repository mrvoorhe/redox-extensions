# Virindi Tank (`/vt`) Commands

> Source: <http://www.virindi.net/wiki/index.php/Virindi_Tank_Commands> (wiki revision 1601, last modified 2016-12-10; retrieved 2026-10-16). Content available under the GNU Free Documentation License 1.2.

Chat commands accepted by the Virindi Tank (VTank) plugin. Type them in the in-game chat box.

## Notation

- `[a/b]` — choose exactly one of the listed literal values.
- `[name]` — a value you supply (filename, heading, state name, etc.).
- Commands with no arguments are used as-is.

## Profiles

| Command | Description |
| --- | --- |
| `/vt settings [save/load/savechar/loadchar] [filename]` | Saves or loads character setting profiles. `savechar`/`loadchar` apply to MineOnly profiles. |
| `/vt nav [save/load] [filename]` | Saves or loads a navigation profile. |
| `/vt loot [save/load] [filename]` | Saves or loads a loot profile. |
| `/vt meta [save/load] [filename]` | Saves or loads a meta profile. |
| `/vt opt [list/get/set/setinall]` | Lists, gets, or sets an advanced option. `setinall` affects ALL profiles. See [Options List](http://www.virindi.net/wiki/index.php/Options_List). |
| `/vt testitem` | Checks the currently selected item against the current loot rules and reports what the action would be if the item were found on a corpse. |
| `/vt propertydump` | Displays the raw game properties for the currently selected item. |
| `/vt refresh` | Refreshes the UI. Also updates the list of profile files in the profile dropdowns. |
| `/vt getdb` | Attempts to redownload the latest game information database. |
| `/vt addnavpt [coords]` | Adds a navigation waypoint at the specified coordinates to the end of the current navigation profile. |
| `/vt addnavjump [heading] [shift: true/false] [milliseconds]` | Adds a jump to the current route. Example: `/vt addnavjump 360 true 500` |
| `/vt addnavcheckpoint` | Adds a checkpoint to the current route. Checkpoints are like normal waypoints, except the plugin waits for the server to verify you reached the location before continuing. |

## Actions

| Command | Description |
| --- | --- |
| `/vt start` | Turns the macro on. |
| `/vt stop` | Turns the macro off. |
| `/vt forcebuff` | Initiates a forcebuff. |
| `/vt cancelforcebuff` | Cancels a forcebuff. |
| `/vt setmetastate [statename]` | Sets the meta state to `statename`. |
| `/vt fakedeath` | Fires the meta Character Death trigger for testing, without actually dying. |
| `/vt deletemonster` | Forces the client to delete the currently selected monster. |
| `/vt reverseroute [true/false]` | Reverses the currently loaded nav route. |
| `/vt reverseroutequery` | Returns the current value of `/vt reverseroute` (`true`/`false`). |
| `/vt equipitemsfor [monstername]` | Performs one step of equipping for the given monster (e.g. going to peace mode, equipping a weapon). Multiple calls are usually required to finish. Rules that contain operators will not work. |
| `/vt mexec [expression]` | Evaluates an expression. Used to test expressions before using them in a meta. Setting variables this way is the same as setting them in a meta. |
| `/vt echo [text]` | Outputs the given text back to the user using a custom VCS preset. Makes it easier to color meta feedback messages. |
| `/vt tapjump` | Tap-jumps. |
| `/vt jump [heading] [shift: true/false] [milliseconds]` | Issues a single jump. Example: `/vt jump 360 true 500` |
| `/vt setattackbar [0 to 1]` | Sets the attack bar slider position. |

## Game Info

| Command | Description |
| --- | --- |
| `/vt dumpspells` | Dumps the client spell table to `C:\spelldump.txt`. |
| `/vt dumpspecies` | Dumps the client species table to `C:\speciesdump.txt`. |
| `/vt dumpmats` | Dumps the client material table to `C:\matdump.txt`. |
| `/vt dumpskills` | Dumps the client skill table to `C:\skilldump.txt`. |

## Debug

| Command | Description |
| --- | --- |
| `/vt log` | Displays the current logging state. |
| `/vt log [type] [on/off]` | Enables or disables logging of a particular type. Valid types: `ActiveRule`, `SalvageList`, `SpellCast`, `RuleInfo`, `Timers`, `CastInfo`, `DebuffChoice`, `Loot`, `CharProps`, `Misc`. |
| `/vt testmonster` | Evaluates the currently selected monster against your Monster List match rules. |
| `/vt lockdump` | _(undocumented on wiki)_ |
| `/vt dumptracker` | _(undocumented on wiki)_ |
| `/vt clearlocks` | _(undocumented on wiki)_ |
| `/vt clearbusy` | _(undocumented on wiki)_ |
| `/vt listmonstervariables` | Lists the supported monster expression variables: `true`, `false`, `name`, `typeid`, `species`, `maxhp`, `range`, `hasshield`, `metastate`. |
| `/vt dumpmetavars` | Lists the currently set meta variables. |
| `/vt listmetafunctions` | Lists all [Meta Functions](http://www.virindi.net/wiki/index.php/Meta_Functions). |
| `/vt metafunchelp [function]` | Gives detailed information for the given meta function. |
| `/vt fakeimp` | Marks a monster as imped. |
| `/vt pscount` | Portal debugging info. |
| `/vt testspell [spellid]` | Tests whether you know a spell and whether you can cast it based on your threshold mods. Example: `/vt testspell 567` |
| `/vt testpet` | Tests whether a pet can be summoned in the current location. |

## Related wiki pages

- [Virindi Tank Advanced Options](http://www.virindi.net/wiki/index.php/Virindi_Tank_Advanced_Options) / [Options List](http://www.virindi.net/wiki/index.php/Options_List) — option names for `/vt opt`.
- [Meta Functions](http://www.virindi.net/wiki/index.php/Meta_Functions) — functions usable in `/vt mexec` and listed by `/vt listmetafunctions`.
