Implement character switching functionality.

Implement `/re switch <character name> <(optional) cmd>`.  It should log out of the current character and log in the named character.  More details on the options accepted 
* <character name> is another character on the same account.
* <(optional) cmd> is an optional argument.  It can be a command that the other character should run once it has logged in.  The idea here is to give one character a way to hand off to the next character.  For example, loading a meta.


Implement support for the rectl to list the other characters on the same account as a given character.

Stage the implementation with separate commits when appropriate
## Usage

| Command | Effect |
|---|---|
| `/re switch <name>` | Log out and log in `<name>`, another character on the same account |
| `/re switch <name> <command>` | The same, then run `<command>` on `<name>` about 5 s after it logs in |

Examples:

```
/re switch Woodford
/re switch Bob Smith /re startmeta daily
/re switch wood /vt start
```

* `<name>` ignores case and can contain spaces.  The longest run of leading words that is a character name wins, so
  with both `Bob` and `Bob Smith` on the account, `/re switch Bob Smith ...` picks `Bob Smith`.  Otherwise the first
  word can be a unique prefix (`wood` for `Woodford`).  Everything after the name is the command.
* An unknown name lists the account's other characters.  An ambiguous prefix lists its matches.

From a terminal, `rectl [-c <name>] characters` shows the account, server, current character and the account's
characters (name, id, index), for a client with the bridge enabled.

## Design

* **Logging in.**  A Decal plugin can't act at character select, but Mag-Filter (a Decal filter, which keeps running
  there) can.  `/re switch` sends `/mf lnc set <name>` (see `docs/ref/InGameCommands/MagTools/mf-commands.md`), then
  logs out 1 s later.  On the way back to character select Mag-Filter clicks that character and Enter.  Mag-Filter must
  be installed and enabled.
* **The handoff command.**  Saved before logout to `<install>\Logs\switch\<server>-<account>-<character>.txt`
  (lowercased) with a timestamp.  When the plugin initializes on login it takes the file for the character that just
  logged in, deletes it and runs the command 5 s later through the normal chat dispatch (so `/re`, `/vt`, `/ub`, `/mt`
  and game commands all work).  Only the named character runs it, at most once, and a file older than 30 minutes is
  discarded, so a switch that failed doesn't fire on a later login.  `/re switch <name>` without a command removes
  any file left for `<name>`.
* The character list comes from Decal's `CharacterFilter.Characters` and `AccountName`.  The matching logic is
  `Core.Utilities/AccountCharacters.cs` (unit-tested), the handoff file `Core/SwitchHandoff.cs`.

## Caveats

* The agent bridge is per session and stops on logout.  Run `/re agent on|rw` on the new character to use `rectl`
  with it.
* `/re switch` isn't in `AgentPolicy`'s blocked list, so an agent in `rw` mode can switch characters (and loses the
  bridge when it does).
