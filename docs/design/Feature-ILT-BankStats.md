I want you to implement a bank account tracking feature.  The goal is to let me get a sense of the output a given farming spot is producing.  To get my current bank account issue the command

```
/b
```

The server will then respond with my balances.  Here is an example of what the server responds with.
```
[BANK] Your balances are:
[BANK] Pyreals: 47,584,323
[BANK] Luminance: 230,286,228
[BANK] Legendary Keys: 246
[BANK] Mythical Keys: 0
[BANK] Enlightened Coins: 296
[BANK] Weakly Enlightened Coins: 424
```

The I want this to work is that I will type a command to turn it on.  The command will be

```
/re track
```

I can also tell my fellowship to enable tracking with the command
```
/rf track
```

Enabling tracking should act as both start record and reset recording if tracking was already enabled.  When tracking is first enabled (or reset) issue a `/b` command to get my balance.  I don't want this plugin query of my account to appear in the game chat so have the plugin hide the response from this query.
When tracking is enabled (or reset) you should record the current time.

Next implement a "report" command.  The two options should be.
`/re report` - Report for myself.  Output should go to the standard info output.
`/rf report` - Tell's the fellowship to report.  Output should go to the fellowship chat so that I can see it from my main account.

To do a report, issue another `/b` command to get the latest account balance.

In the report I want you to display the following information for each balance item. 

* Current balance
* Rate increase per hour.