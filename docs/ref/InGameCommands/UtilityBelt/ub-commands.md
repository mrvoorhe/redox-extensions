# UtilityBelt (`/ub`) Commands

> Source: <https://utilitybelt.gitlab.io/docs/command-line/> (docs build 0.2.7, commit 6edb6c0, 2022-07-28; retrieved 2026-10-20).

Chat commands accepted by the UtilityBelt plugin. Type them in the in-game chat box. `/ub help [command]` prints usage for any command in game.

## Notation

- `<value>` — required value you supply.
- `[value]` — optional value.
- `a|b` or `(a|b)` — choose exactly one of the listed values.
- `{a | b <x>}` — choose exactly one of the listed sub-commands.
- Suffix letters in brackets modify the command name and can be combined:
  - `p` — partial name match (e.g. `/ub portalp Port`).
  - `l` — search landscape objects only.
  - `i` — search inventory only.
  - `q` — quiet (suppresses chat output).
- Many movement/interaction commands have built-in VTank pausing and retries, so they can be used safely while VTank is running.

## General

| Command | Description |
| --- | --- |
| `/ub` | Prints the current build version to chat. |
| `/ub help [command]` | With no argument, lists all UB commands. With a command name, prints help and usage for it. Example: `/ub help printcolors`. |
| `/ub opt list` | Lists all available settings. |
| `/ub opt get <option>` | Shows a setting's current value. Example: `/ub opt get Plugin.Debug`. |
| `/ub opt set <option> <newValue>` | Sets a setting. Example: `/ub opt set Plugin.Debug true`. |
| `/ub opt toggle <option>` | Toggles a boolean setting. |
| `/ub date[utc] [format]` | Prints the current local (or UTC) date/time, optionally formatted with a [.NET custom date and time format string](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings). Examples: `/ub date hh:mm:ss tt` → `06:09:01 PM`; `/ub dateutc dddd dd MMMM` → `Friday 29 August`. |
| `/ub delay <millisecondDelay> <command>` | Runs `command` after the given delay. Example: `/ub delay 5000 /say hello` runs `/say hello` after 5 seconds. |
| `/ub mexec <expression>` | Evaluates a meta [expression](https://utilitybelt.gitlab.io/docs/expressions/). |
| `/ub printcolors` | Prints all available chat colors. |
| `/ub playsound [volume] <filepath>` | Plays a sound file. Relative paths resolve against the UB plugin storage directory. Examples: `/ub playsound 100 C:\test.wav`, `/ub playsound 50 test.wav`. |
| `/ub vitae` | Thinks to yourself with your current vitae percentage. |

## Movement and Navigation

| Command | Description |
| --- | --- |
| `/ub closestportal` | Uses the closest portal. |
| `/ub portal[p] <portalName>` | Uses the named portal, with VTank pausing. Examples: `/ub portal Gateway` (exact), `/ub portalp Portal` (partial). |
| `/ub follow[p] <name>` | Sets a VTank nav route to follow the named character. Examples: `/ub follow Zero Cool`, `/ub followp Zero`. |
| `/ub face <heading>` | Faces the given heading in degrees, with VTank pausing and retries. Example: `/ub face 180` (south). |
| `/ub jump[swzxc] [heading] [holdtime]` | Jumps, with VTank pausing and retries. `s` holds Shift; `w`/`z`/`x`/`c` hold the matching movement key during the jump. `holdtime` is jump power out of 1000. Examples: `/ub jump` (tap), `/ub jumpsw 180 500` (face south, jump forward at 500/1000), `/ub jumpsx 300` (jump backward at 300/1000). |
| `/ub arrow point <coordinates>` | Points the on-screen arrow at the given coordinates. Example: `/ub arrow point 32.7N, 45.5E`. |
| `/ub arrow face` | Turns your character to face the direction the arrow is pointing. |
| `/ub getmotion` | Reports the motion of the currently selected object. |
| `/ub setmotion <Forward\|Backward\|TurnRight\|TurnLeft\|StrafeRight\|StrafeLeft\|Walk> <0\|1>` | Sets (`1`) or clears (`0`) a wanted motion in the client. Example: `/ub setmotion Forward 1` runs forward indefinitely. |
| `/ub clearmotion` | Clears all wanted motions in the client. |
| `/ub translateroute <startLandblock> <routeToLoad> <endLandblock> <routeToSaveAs> [force]` | Translates a VTank nav route from one landblock to another and saves it under a new name. `force` overwrites an existing output file. All points are translated, regardless of whether they are indoors or outdoors. Example: `/ub translateroute 0x00640371 eo-east.nav 0x002B0371 eo-main.nav`. |

## Items and Inventory

| Command | Description |
| --- | --- |
| `/ub use[li][p] <item> [on <target>]` | Uses an item, optionally on another item/target. Examples: `/ub use Cake`, `/ub usepi splitting on gold pea`, `/ub uselp plant`, `/ub usei Stamina Elixer`. |
| `/ub select[li][p] <item>` | Selects an item. Examples: `/ub select Cake`, `/ub selectpi gold`, `/ub selectlp plant`. |
| `/ub close corpse` | Closes the open corpse, if any. |
| `/ub give[p{P\|r}] [itemCount] <itemName> to <target>` | Gives items matching the name to a player. `p` partially matches the item name; `P` partially matches the target name; `r` treats the item name as a regex. Omit `itemCount` to give all matches. Examples: `/ub givep 10 Prismatic to Zero Cool`, `/ub giveP 10 Prismatic Tapers to Zero`, `/ub give Hero Token to Zero Cool`, `/ub giver Hero.* to Zero Cool`. |
| `/ub ig[p] <lootProfile> to <target>` | Gives all items matching Keep rules in a VTank loot profile to a player. `p` partially matches the target name. Example: `/ub ig muledItems.utl to Zero Cool`. |
| `/ub count item <name> [debug] [think]` | Counts inventory items by name. Example: `/ub count item Prismatic Taper`. |
| `/ub count profile <lootProfile> [debug] [think]` | Counts inventory items matching a loot profile. `think` reports the result as a think-to-self. Example: `/ub count profile recomp.utl think`. |
| `/ub count player <range> [debug] [think]` | Counts players within `range`. |
| `/ub autostack` | Stacks items in your inventory. |
| `/ub autocram` | Crams items into side packs. |
| `/ub unstack` | Unstacks all stackable items. |
| `/ub clearbugged` | IDs everything in your inventory and removes bugged items. |
| `/ub equip list` | Lists available equipment profiles. |
| `/ub equip load [lootProfile]` | Equips all items matching the profile. Example: `/ub equip load profile.utl`. |
| `/ub equip test [lootProfile]` | Tests equipping the profile without equipping. |
| `/ub equip create [lootProfile]` | Creates an equipment profile. |
| `/ub calcdamage` | Calculates the buffed damage of the selected weapon. Only cantrip buffs are included. |

## Trade and Vendors

| Command | Description |
| --- | --- |
| `/ub autotrade [lootProfile]` | Adds all items matching a VTank loot profile to the open trade window. With no profile, uses `<TradePartnerName>.utl`. Example: `/ub autotrade mfk.utl`. |
| `/ub autotrade autoaccept add[gs] <namePattern>` | Adds a regex name pattern to the auto-accept list. No suffix: current character. `s`: all your characters on this server. `g`: all your characters on any server. Example: `/ub autotrade autoaccept add Shen-.*`. |
| `/ub autotrade autoaccept remove[gs] <namePattern>` | Removes a pattern from the auto-accept list (same suffixes as `add`). Example: `/ub autotrade autoaccept removes Sunnuj`. |
| `/ub autotrade autoaccept list` | Lists all auto-accept name patterns. |
| `/ub autovendor [lootProfile]` | Starts AutoVendor buy/sell with the open vendor. With no profile, uses `<VendorName>.utl`. Example: `/ub autovendor recomp.utl`. |
| `/ub autovendor cancel` | Cancels the current AutoVendor session. |
| `/ub vendor open[p] <vendorname\|vendorid\|vendorhex>` | Opens a vendor, with VTank pausing. Example: `/ub vendor open Tunlok Weapons Master`. |
| `/ub vendor opencancel` | Quietly cancels the last `/ub vendor open*` command. |
| `/ub vendor buyall` | Buys everything on the buy list. |
| `/ub vendor sellall` | Sells everything on the sell list. |
| `/ub vendor clearbuy` | Clears the buy list. |
| `/ub vendor clearsell` | Clears the sell list. |

## Automation

| Command | Description |
| --- | --- |
| `/ub autosalvage` | Adds inventory items matching a Salvage rule in the loaded VTank loot profile to the salvage window. |
| `/ub autosalvage force` | Same as above, and clicks Salvage. |
| `/ub getjob` | Shows tinker jobs currently in the AutoTinker queue. |
| `/ub tinkcalc` | Shows the best iron/granite combination for the selected item. |
| `/ub xp` | Displays the weights of the current XP spending policy. |
| `/ub xp test` | Shows how current experience would be spent under the policy. |
| `/ub xp level` | Starts (or halts) spending experience quickly, in chunks up to `MaxXpChunk`. |
| `/ub xp slow` | Starts (or halts) spending experience one level at a time. |
| `/ub professor <creature\|item\|life\|war\|void> <level>` | Talks to a nearby spell professor to learn the given school and level of spells. Example: `/ub professor void 7`. |
| `/ub professor cancel` | Cancels all current professor attempts. |
| `/ub prepclick yes <secondsToWatch>` | Clicks Yes on the first message box that appears within the given time. Example: `/ub prepclick yes 10`. |
| `/ub prepclick no <secondsToWatch>` | Clicks No on the first message box that appears within the given time. |
| `/ub prepclick stop` | Stops watching for a message box. |

## Character, Allegiance, and Fellowship

| Command | Description |
| --- | --- |
| `/ub combatstate (peace\|melee\|missile\|magic)` | Sets the combat state. |
| `/ub swearallegiance[p] [name\|id\|selected]` | Swears allegiance to the named, id'd, or selected character. With no argument, uses the closest character. Examples: `/ub swearallegiance Yonneh`, `/ub swearallegiancep Yo`, `/ub swearallegiance selected`. |
| `/ub breakallegiance[p] [name\|id\|selected]` | Breaks allegiance from the named, id'd, or selected character. With no argument, uses the closest character. Only visible characters are scanned (not the allegiance hierarchy). |
| `/ub fellow create <Name>` | Creates a fellowship. |
| `/ub fellow quit\|disband\|open\|close\|status` | Quits, disbands, opens, closes, or shows status of the current fellowship. |
| `/ub fellow recruit[p] [Name]` | Recruits a character into the fellowship. |
| `/ub fellow dismiss[p] [Name]` | Dismisses a character from the fellowship. |
| `/ub fellow leader[p] [Name]` | Passes fellowship leadership to a character. |
| `/ub playeroption list` | Lists AC client player options. |
| `/ub playeroption <option> (on\|true\|off\|false)` | Turns an AC client player option on or off. Example: `/ub playeroption AutoRepeatAttack on`. |
| `/ub quests check <questFlag>` | Thinks to yourself with the status of quest flags matching `questFlag`. Click an entry in the quest tracker to print its flag name. After completing a quest, run `/myquests` (or `/ub myquests`) first. Example: `/ub quests check blankaug`. |
| `/ub myquests` | Refreshes quest flags via `/myquests`, hiding the output. |
| `/ub logout` | Logs out of the current character. |
| `/ub quit` | Closes the client. |

## Client and UI

| Command | Description |
| --- | --- |
| `/ub getui` | Prints the client window positions encoded as a base64 string. |
| `/ub setui UI:<string from getui>` | Restores client window positions from a `getui` string. |
| `/ub resolution <width> <height>` | Sets the client resolution immediately. Does not change the settings page or persist across relogs. Example: `/ub resolution 640 600`. |
| `/ub textures <landscape[0-4]> <landscapeDetail[0-1]> <environment[0-4]> <environmentDetail[0-1]> <sceneryDraw[1-25]> <landscapeDraw[1-25]>` | Sets client texture options immediately. Does not change the settings page or persist across relogs. Examples: `/ub textures 0 1 0 1 25 25` (max), `/ub textures 4 0 4 0 1 1` (min). |
| `/ub element[q] [element [x y width height]]` | Lists AC UI elements, shows one element's position/size, or moves and resizes it. `q` suppresses chat output. Examples: `/ub element`, `/ub element sbox`, `/ub elementq sbox 0 0 800 600`. |
| `/ub videopatch {enable \| disable \| toggle}` | Enables, disables, or toggles the video patch, which stops rendering the 3D world to save CPU. |
| `/ub globalframelimit <frameRate>` | Limits the client frame rate. `0` disables the limiter. |
| `/ub bgframelimit <frameRate>` | Limits the frame rate while the client is not focused. `0` disables the limiter. |

## Networking and Broadcast

Client tags are configured with the `Networking.Tags` setting.

| Command | Description |
| --- | --- |
| `/ub bc [millisecondDelay] <command>` | Runs `command` on every connected client, with an optional delay between each. Examples: `/ub bc /say hello`, `/ub bc 5000 /say hello`. |
| `/ub bct <tags> [millisecondDelay] <command>` | Runs `command` on every client with any of the comma-separated tags (no spaces; quote tags that contain spaces). Examples: `/ub bct one,two 5000 /say hello`, `/ub bct "some tag","another tag" /say hello`. |
| `/ub netclients [tag]` | Lists clients on the UB network, optionally only those with `tag`. |

## Debugging and Info

| Command | Description |
| --- | --- |
| `/ub pos` | Prints position information for the selected object. |
| `/ub id` | Prints the object id of the selected object. |
| `/ub propertydump` | Prints property information for the selected object. |
| `/ub listvars` | Prints all defined variables. |
| `/ub listpvars` | Prints all persistent variables for this character. |
| `/ub listgvars` | Prints all global variables for this server. |
| `/ub dumpskills` | Prints all skills and training levels from `Login_PlayerDesc` (0x0013). |
| `/ub pcap enable [bufferDepth]` | Enables packet capture into an in-memory buffer. Nothing is saved until `print`. |
| `/ub pcap disable` | Disables packet capture. |
| `/ub pcap print` | Saves the current capture buffer to a new file in the plugin storage directory. |
| `/ub lsdlb <landblock>` | Downloads and caches landblock spawns from Lifestoned. Example: `/ub lsdlb 00070000` (Town Network). |
| `/ub lsdclearcache` | Clears all cached Lifestoned data. |

## Source discrepancies

The upstream page has a few copy/paste errors; this document uses the evident intent:

- `/ub delay` is described upstream as "Thinks to yourself with your current vitae percentage" and its example says 5000 ms is "3 seconds".
- `/ub count` examples upstream omit the `item`/`profile` keyword from the syntax.
- `/ub face` is listed twice, and an unnamed "Development Test." entry is omitted here.

## Related docs

- [Overview](https://utilitybelt.gitlab.io/docs/overview/)
- [Plugin Settings](https://utilitybelt.gitlab.io/docs/plugin-settings/) — settings controlled by `/ub opt`.
- [Expressions](https://utilitybelt.gitlab.io/docs/expressions/) — used by `/ub mexec` and VTank metas.
- [Config Files](https://utilitybelt.gitlab.io/docs/config-files/)
