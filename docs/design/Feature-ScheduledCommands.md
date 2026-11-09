Implement support for scheduled commands.  The goal is to allow a specified command to be scheduled to run on a specific time.  Support repeat frequency.  For now just implement "once" and "daily".

These should be configured per character.

Persistance is important.  Use a per character settings file dedicated to scheduled commands in the redox extensions directory.

The primary interface for adding or removing scheduled tasks will be you.  An out of game cli that modifies that settings file is adequate for now.  Examples of what I want to be able to tell you.

```
Tomorrow at 6am schedule the command "/re sm daily" to run on <character name>.  The command should repeat every day at 6am
```


In order to test your changes you can connect to one of the available characters and have them issue a simple command once time for a short duration into the future.  Make sure the command does not mess with what they are doing.

# Design

## Overview

Each character has a schedule file, `<install>\ScheduledCommands\<Character>.json`, in the RedoxExtensions install
directory (the wrapper's directory, not the `TmpDlls` shadow copy).  The plugin
(`Listeners.Monitors/CommandScheduler.cs`) watches the file and runs the commands that come due.  `rectl schedule`
edits the file from outside the game.  The agent bridge isn't needed for either.

## File format

```json
{
  "entries": [
    {
      "id": "6ab2eb",
      "command": "/re sm daily",
      "at": "2026-11-09T06:00:00",
      "repeat": "daily",
      "graceMinutes": 15,
      "enabled": true,
      "lastRun": "2026-11-09T06:00:03"
    }
  ]
}
```

* All times are the PC's local time, with no offset.
* `repeat` is `once` or `daily`.  A `daily` entry runs at `at`'s time of day every day, starting on `at`'s date.
* `lastRun` is written only by the plugin.  rectl keeps it when it rewrites the file.
* Both sides write to a temp file and swap it in, so neither side ever reads half a file.

The model and the due rule are in `RedoxExtensions/Scheduling/ScheduledCommand.cs`.  The plugin and rectl share that
one file (it's a linked source in `RedoxExtensions.AgentCli`).

## When an entry runs

The latest occurrence is the most recent scheduled time at or before now (for a `once` entry, `at` itself).  An entry
is due if it's enabled, it has a latest occurrence, `lastRun` is older than that occurrence, and the occurrence is no
more than `graceMinutes` old.

* **Late runs.**  If the character logs in or the plugin loads up to `graceMinutes` after the time, the entry still
  runs.  Later than that it's skipped: a `once` entry counts as missed, and a `daily` entry waits for the next day.
* **At most once.**  The plugin saves `lastRun` before it queues the command.  A reload or crash right after can't
  run the entry again, and if the save fails nothing runs.  The plugin also remembers what it ran this session, so a
  rectl write based on an older read can't bring an entry back.
* **Portal space.**  Due entries wait while the character is in portal space, and run when it lands if they're
  still within the grace window.
* **Threads.**  The file is checked every 5 s on a timer thread, which does all of the file IO.  Due commands are
  queued to the game thread and run like typed commands (`ACUtilities.ProcessArbitraryCommand`: other plugins first,
  then the game's chat parser).
* **Bad file.**  If the file can't be parsed, the plugin schedules nothing, writes
  `[RE] Schedule file unreadable ...` once, and waits for the file to change.

Chat output (journaled by the agent bridge as `plugin` events):

* `[RE] Schedule loaded : <n> entries` when the file is first read or changes.
* `[RE] Scheduled : <id> <command>` right before a command runs.

## In-game commands

* `/re schedule` (or `/re sched`) lists the entries, with their status and next run.
* `/re schedule reload` rereads the file now.

## rectl

```
rectl -c <Character> schedule add <command> --at <time> [--repeat once|daily] [--grace <min>]
rectl schedule list [-c <Character>]
rectl -c <Character> schedule remove|enable|disable <id>
```

* `-c` is the exact character name.  It isn't matched against running clients, because the character may be
  offline.  `add` prints a note if the character has no schedule file yet and isn't running with the bridge on.
* `--at` takes `HH:MM` (the next time it comes round), `"tomorrow HH:MM"`, `YYYY-MM-DDTHH:MM`,
  `"YYYY-MM-DD HH:MM"`, or `+<n>m`/`+<n>h` from now.  A time in the past is rejected.
* The output is JSON, and each entry includes its `status` (`waiting`, `due`, `done`, `missed`, `disabled`) and
  `nextRun`.

The `scheduled-commands` skill (`.claude/skills/scheduled-commands`) says how Claude turns requests into these
commands.
