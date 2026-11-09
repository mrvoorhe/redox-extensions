---
name: scheduled-commands
description: Schedule a chat command (e.g. `/re sm daily`) to run on one of the user's Asheron's Call characters at a set time, once or every day, with the plugin's persistent per-character schedule (`rectl schedule`). Use when asked to schedule, list, change or cancel a command at a time on a character, especially repeating ones like "every day at 6am". It keeps working after this Claude session ends.
---

# Scheduled commands

The plugin runs each character's scheduled commands from `<install>\ScheduledCommands\<Character>.json`.  You edit
that file with `rectl schedule`.  The bridge doesn't need to be on, and this Claude session doesn't need to stay
open.  The plugin picks up changes within about 5 seconds.  See `docs/design/Feature-ScheduledCommands.md`.

Run `rectl` from PowerShell, not the Bash tool: Git Bash mangles `/re ...` arguments.

## Steps

1. **Check the command exists.**  It must be documented under `docs/ref/InGameCommands` or be a `/re` command
   (`RedoxExtensionsHandler.cs`).  Don't make one up.  Ask the user if you aren't sure.
2. **Get the exact character name.**  The file is named after it, and a typo fails silently.  `rectl list` shows
   the names of clients that have the bridge on.  If `schedule add` prints "Note: ... no schedule file yet", check
   the spelling with the user.
3. **Work out the time.**  `--at` takes `HH:MM` (the next time it comes round), `"tomorrow HH:MM"`,
   `YYYY-MM-DDTHH:MM` or `+<n>m`/`+<n>h`.  Times are the PC's local time.  For "tomorrow at 6am, repeat every day":
   ```
   .\rectl.cmd -c <Character> schedule add "/re sm daily" --at "tomorrow 06:00" --repeat daily
   ```
   `--repeat` is `once` (default) or `daily`.  `--grace <min>` (default 15) is how late it may still run, e.g. if the
   character logs in at 6:10.  Later than that it's skipped, and a daily entry waits for the next day.
4. **Report back** the `id`, `nextRun` and `repeat` from the JSON it prints.  Remind the user that the character
   must be logged in, with the plugin loaded, at that time, and that the PC must be awake.  If the run is hours
   away, check `powercfg /query SCHEME_CURRENT SUB_SLEEP STANDBYIDLE` the way `meta-chain-start` does.

## Other commands

* `.\rectl.cmd schedule list` shows every character's entries, with `status` (waiting, due, done, missed or
  disabled), `lastRun` and `nextRun`.  `-c <Character>` limits it to one character.
* `.\rectl.cmd -c <Character> schedule remove|disable|enable <id>`.
* In game, `/re schedule` lists the entries and `/re schedule reload` rereads the file.
* When an entry runs, the plugin writes `[RE] Scheduled : <id> <command>` to chat.  If the bridge is on, check for
  it with `rectl -c <Character> events --kinds plugin`.

## Rules

* These are real characters on a public server.  Only schedule what the user asked for.
* To test that scheduling works, use a harmless command such as `/re help` a minute or two ahead, then remove the
  entry.
* Several characters running the same meta chain: stagger them 15 minutes apart (see `meta-chain-start`).
