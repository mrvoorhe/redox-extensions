# Mag-Filter (`/mf`) Commands

> Source: <https://github.com/Mag-nus/Mag-Plugins/wiki/Mag%E2%80%90Filter> and the Mag-Filter source
> (<https://github.com/Mag-nus/Mag-Plugins/tree/master/Mag-Filter>) (retrieved 2026-11-09).

Chat commands accepted by the Mag-Filter Decal filter.  Unlike plugins, a filter keeps running at character select,
which is how it can log characters in.

## Notation

- `[name]` — a value you supply.

## Login next character

| Command | Description |
| --- | --- |
| `/mf lnc set [charactername]` | Logs in `[charactername]` on the next logout. |
| `/mf lnc clear` | Clears the character set with `lnc set` or `lncbi set`. |
| `/mf lncbi set [index]` | Logs in the character at `[index]` (0-10, alphabetical position in the character list) on the next logout. |
| `/mf lncbi clear` | Same as `/mf lnc clear`. |

Notes from the source:

- The name is stored lowercased and matched case-insensitively against the character list.
- It fires about 1 second after the server sends the server name (F7E1) on the way back to character select, clicks
  the character in the list and then the Enter button.  The setting is cleared once used.

## Login complete message queues

| Command | Description |
| --- | --- |
| `/mf lcmq add [message]` | Sends `[message]` 1 frame after the next login completes.  `/mf lmq add` is an older alias. |
| `/mf lcmq clear` | Clears that queue.  `/mf lmq clear` is an older alias. |
| `/mf alcmq add [message]` | Sends `[message]` a number of milliseconds (default 3000) after the next login completes.  `/mf olcmq add` is an older alias. |
| `/mf alcmq clear` | Clears that queue.  `/mf olcmq clear` is an older alias. |
| `/mf alcmq wait set [ms]` | Sets the delay used by `alcmq`.  `/mf olcwait set` is an older alias. |
| `/mf alcmq wait clear` | Resets the delay to 3000 ms.  `/mf olcwait clear` is an older alias. |

The queued messages are typed into the chat box of whichever character logs in next.

## Default login character

| Command | Description |
| --- | --- |
| `/mf dlc set` | Makes the current character the one logged in automatically on first connect (per server and account). |
| `/mf dlc clear` | Clears the default login character. |
| `/mf dlcbi set [index]` | Default login character by index (0-10). |
| `/mf dlcbi clear` | Same as `/mf dlc clear`. |
| `/mf sdlcbi set [index]` | Default login character by index for the whole server, not per account (0-10). |
| `/mf sdlcbi clear` | Clears the server defaults. |

## Other

| Command | Description |
| --- | --- |
| `/mf cssmfps [value]` | Caps the frame rate at character select (minimum 10, 0 disables). |

Automatic behavior: "One of your characters is still in the world" login failures are retried.  Esc at the
character select screen exits the client without confirmation.
