I want you to implement a command that will initialize some state, load a meta, and then start the meta.

`/re startmeta <name>` where `<name>` is the name of the meta file without it's `.met` file extension.

Here is the sequence of things to initialize before loading the meta.

- Load the profile called `Normal`
- Stop VT - We don't want it running while we are tweaking with things.  We'll start it later
- Disable VT nav
- Disable VT combat
- Disable VT loot
- Enable VT Buff
- Disable VT boost nav priority
- Disable peace mode when idle
- Load the loot profile `QuestLoot.utl`
- Leave the current fellowship
- Create a new fellowship called `X`
- Set the attack range to 20
- Load the specified VT meta
- Enable VT meta
- Start VT
- say `/f !start`