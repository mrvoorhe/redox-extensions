# Asheron's Call Base Game Commands

> Source: Microsoft KnowledgeBase article Q243622, "Asheron's Call: List of Chat Commands" (last modified 2002-05-16; retrieved 2026-10-20), archived at <https://jeffpar.github.io/kbarchive/kb/243/Q243622/>. Applies to Asheron's Call 1.0 and Dark Majesty.

Chat commands built into the game. Press Enter to open the chat box, type the command, and press Enter again (or click Send). You can type `@` in place of `/` in any of these commands, for example `@loc`.

The source is a retail-era list. Emulator servers such as ACE support most of these commands, but some may behave differently or be missing.

## Notation

- `<value>`: a required value you supply.
- `[value]`: an optional value.
- `a|b`: choose exactly one of the listed values.

## Talking and Tells

| Command | Description |
| --- | --- |
| `/say <text>` | Says the text to everyone around you. |
| `/tell <character>, <text>` | Sends a long-distance message to a character. |
| `/reply <text>` | Sends the text to the last person who sent you a tell. |
| `/r <text>` | Same as `/reply`. |
| `/rp <text>` | Same as `/reply`. |
| `/retell <text>` | Sends the text to the last person you sent a tell to. |
| `/rt <text>` | Same as `/retell`. |
| `/notell` | Toggles whether you receive tells. |
| `/chat` | Sets whether you receive normal chat. |

## Group Channels

| Command | Description |
| --- | --- |
| `/f <text>` | Broadcasts to your fellowship. |
| `/a <text>` | Broadcasts to your allegiance. |
| `/m <text>` | Broadcasts to your monarch. |
| `/p <text>` | Broadcasts to your patron. |
| `/v <text>` | Broadcasts to your vassals. |
| `/c <text>` | Broadcasts to your co-vassals. |

## Emotes

| Command | Description |
| --- | --- |
| `/emote <text>` | Emotes the text. |
| `/em <text>` | Same as `/emote`. |
| `/e <text>` | Same as `/emote`. |
| `/me <text>` | Same as `/emote`. |

## Filters

Message types you can filter: `Speech`, `Tell`, `Combat`, `Magic`, `Emote`, `Appraisal`, `Spellcasting`, `Allegiance`, `Fellowship`, `Combat_Enemy`, `Combat_Self`.

| Command | Description |
| --- | --- |
| `/filter` | Lists the filters currently in place. |
| `/filter -<message_type>` | Filters out incoming messages of the given type. Example: `/filter -Combat`. |
| `/filter -all` | Filters out incoming messages of all types. |
| `/unfilter -<message_type>` | Removes the filter on the given message type. |
| `/unfilter -all` | Removes the filters on all message types. |

## Squelch

| Command | Description |
| --- | --- |
| `/squelch` | Lists the characters you have squelched (up to 32). |
| `/squelch <character>` | Squelches all messages from the character. |
| `/squelch -account <character>` | Squelches all messages from the character, and also blocks all non-public chat from that player's other characters. |
| `/unsquelch <character>` | Changes the squelch settings for the character. |

## Friends

| Command | Description |
| --- | --- |
| `/friends` | Tells you when friends log in. |
| `/friends_add <character>` | Adds the character to your friends list. |
| `/friends_remove <character>` | Removes the character from your friends list. |

## Allegiance

| Command | Description |
| --- | --- |
| `/motd` | Shows your allegiance's message of the day. |
| `/speaker` | Tells you who your allegiance speaker is. |

## Death and Corpses

| Command | Description |
| --- | --- |
| `/corpse` | Shows where you last died outdoors. |
| `/consent` | Shows and manages your corpse-looting consent list. |
| `/permit <character>` | Gives or revokes another player's permission to loot your corpse. |

## Recalls

| Command | Description |
| --- | --- |
| `/lifestone` | Recalls you to your lifestone. |
| `/ls` | Same as `/lifestone`. |
| `/marketplace` | Recalls you to the Marketplace. |
| `/mp` | Same as `/marketplace`. |

## Housing

| Command | Description |
| --- | --- |
| `/house recall` | Teleports you to your house. |
| `/house open` | Makes your house an open house. |
| `/house close` | Closes your house. |
| `/house hooks on\|off` | Shows or hides the hooks in your house. |
| `/house boot <name>` | Removes a player from your house. |
| `/house boot all` | Removes everyone from your house. |

### Guest list

| Command | Description |
| --- | --- |
| `/house guest list` | Shows the current guest list. |
| `/house guest add <name>` | Adds a player to your guest list. |
| `/house guest remove <name>` | Removes a player from your guest list. |
| `/house guest remove_all` | Removes everyone from your guest list. |
| `/house guest add_allegiance` | Adds your allegiance to the guest list. |
| `/house guest remove_allegiance` | Removes your allegiance from the guest list. |

### Storage permissions

| Command | Description |
| --- | --- |
| `/house storage add <name>` | Lets a player use your house storage. |
| `/house storage remove <name>` | Removes a player's permission to use your house storage. |
| `/house storage remove_all` | Removes storage permission from all guests. |

## Character Info

| Command | Description |
| --- | --- |
| `/age` | Shows your total play time. |
| `/birth` | Shows when your character was created. |
| `/loc` | Shows your current position. |

## Client and Utility

| Command | Description |
| --- | --- |
| `/help` | Lists the available commands. |
| `/log` | Echoes chat text to a log file. |
| `/fillcomps` | Helps you buy spell components. |
| `/render radius <5-25>` | Sets the landscape draw radius. |
| `/render fov <10-160>` | Sets the field of view. |
| `/render texpage on\|off` | Toggles the 3dfx texture paging system. |

## Source notes

The source page has some gaps and inconsistencies. This document handles them as follows:

- The source calls `/` a "backslash". It means the forward slash.
- The source only gives the bare command name for most entries. The `<text>` and `<character>` arguments shown above follow normal in-game usage.
- `/unsquelch` is described only as "modifies the squelch information for this character". It most likely just removes the squelch.
- `/consent` and `/permit` subcommands are not listed in the source.
