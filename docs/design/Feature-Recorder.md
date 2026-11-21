# Session recorder

The plugin can record a character's event log (chat, plugin lines, 30 s `state` records) to a `.jsonl` file with no
agent connected.  `metachain.py analyze` reads the files unchanged.

## Commands

`/re recorder` (alias `/re rec`):

* `start [timeout]` - start a session (default `5h`).  A running session is closed with reason `restarted`.
* `stop [delay]` - stop now, or at now + delay.  Prints `not recording` when nothing is running.  A stop never
  extends a session past its timeout.
* `status` - file, start time, timeout, pending stop and last seq.

Durations are a number plus `s`, `m` or `h` (`30s`, `5m`, `4.5h`, `.5h`).  A bare number, `0m`, `1h30m` and other
units print usage.

## Files

* Recordings: `<outputRoot>\<YYYY-MM-DD>\<Char>.jsonl`, then `<Char>-2.jsonl` and so on.  The installed `.met` files
  are copied to `<outputRoot>\<YYYY-MM-DD>\metas\`.
* Session state (survives a crash or `/re reload`): `<install>\Recorder\State\<Char>.json`.  After a relog the
  session resumes while its deadline is ahead; otherwise it is closed with `expired-while-down`.
* Config: `<install>\Recorder\Global.json` with `outputRoot` (default `<install>\Recordings`) and an optional
  `afterStop` `{ "program", "args" }`.  `args` may use `{file}`, `{dir}`, `{name}`, `{char}` and `{date}`.  The
  program is started without a shell and is not waited for.

The event log now runs from login to logoff on its own; the agent bridge reads the same journal.
