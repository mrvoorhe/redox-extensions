I want you to extend BankingTracker.cs with new functionality.  I want you to implement a scheme to automatically deposit things into my bank.

This functionality should trigger on a timed basis.  By default it should be enabled and trigger every 30 minutes.

The commands to control this should be

`/re autobank off` - turns it off
`/re autobank 0` - also turns it off
`/re autobank 60` - sets the interval to every 60 minutes.

Implement `/rf` variations of the same command so that slave accounts can be controlled from the master.

When the auto bank interval goes off you should do a sequence of things.

1) Deposit legendary keys.  To do this run the command
```
/b d e
```

2) Deposit peas
```
/b d ps
```

3) Craft coins.
```
/clap all
```


There are a few requirements before executing each command.

* You must wait 5s between issue each of the commands

* You must make sure the character is in the idle peace mode state.