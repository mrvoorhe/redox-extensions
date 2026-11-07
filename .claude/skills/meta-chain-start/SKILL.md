---
name: meta-chain-start
description: Start a VT meta chain (`/re startmeta <chain>`, e.g. the `daily` chain) on one or more of the user's Asheron's Call characters at a set time, 15 minutes apart so they don't overlap in game. Use when asked to start, schedule or kick off a meta chain or the dailies at a time, for one or several characters.
---

# Start a meta chain at a set time

`tools/vt/metachain.py start` sends `/re startmeta <chain>` once to each character through the agent bridge.  The
first character goes at the given time, and each later one 15 minutes after the one before it.  The stagger keeps
them out of each other's way in game: the chain metas share NPCs, portals and hunt spots.

* **Time:** 07:00 by default, the next time it comes round.  `--at HH:MM` or `--at YYYY-MM-DDTHH:MM` sets it.
* **Order:** characters start in the order the user named them.
* **Chain:** `daily` by default if the user doesn't name one.  `/re startmeta` is documented in
  `docs/design/Feature-StartMeta.md`.

The characters are real, on a public server.  This sends exactly one command per character and nothing else.

## Steps

1. **Check the bridge.**  `rectl list` must show every character, each in mode `rw`.  If one is missing or read-only,
   tell the user to run `/re agent rw` on it (or to log it in).  Don't ask for `full`.
   * A character can be offline when you schedule, if the user says it will be logged in by then.  The script
     retries every 30 s for 15 minutes after its time.
   * If the user set up the agent to turn on at login, a client that crashes and relogs is fine.
2. **Check the PC stays awake.**  Run `powercfg /query SCHEME_CURRENT SUB_SLEEP STANDBYIDLE` in PowerShell (not
   bash).  If AC sleep isn't 0 and the start is hours away, tell the user the PC may sleep first.
3. **Show the plan and confirm it.**
   ```
   py tools/vt/metachain.py start <chain> <Char1> <Char2> ... [--at 07:00] [--stagger 15] --dry-run
   ```
   This prints each character's start time.  Tell the user the times and wait for their OK, unless they already
   gave exact times and characters.
4. **Arm it.**  Run this with the Bash tool's `run_in_background`, and keep its log in the scratchpad:
   ```
   py tools/vt/metachain.py start <chain> <Char1> <Char2> ... [--at ...] --log <scratch>/start.jsonl
   ```
   * It sleeps until each time, sends the command, and waits for the plugin's `[RE] StartMeta : <chain>` line.
   * It exits after the last character, writing `{"what": "done"}`.
   * It runs inside this Claude session, so the session must stay open until then.  Say so.
5. **Report the outcome** when the background job finishes: read the log.
   * `"accepted": true` with `wait.matched` means the chain started.
   * `accepted` without a match means the command was sent but the confirmation was missed.  Check with
     `rectl -c <Char> events --kinds plugin --max 50` for `StartMeta` and `Meta transitioning`.
   * `accepted: false` after 15 minutes means the client never answered.  Say which character, and don't resend
     on your own: a chain must start at most once.

## Rules

* Once a chain has started, don't send anything else to that character unless the user asks.  In particular, don't
  "help" a meta that looks stuck.
* To watch the chain and collect timing and kill-task data, use the `meta-chain-monitor` skill.  Start its
  recorders before the first start time.
* A full `daily` chain took about 85 minutes per character on 2026-11-07 (Daily → Attributes4.0 → HowardTheMad →
  LaLaRing → 5 kill-task metas → aQD9ELerRhan → farming).  With a 15-minute stagger, the characters are in different
  parts of the chain at any time.
