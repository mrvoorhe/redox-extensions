This document describes a scheme for create per-character chains of metas to run.  This way each character can have a customized set of dailies to run.  Although we need not limit this to dailies.  We can imagine a scheme for creating arbitrary chains of metas.

To initiate a run, use the command

`/re runchain <chain name>`


Starting sequence.

- Quit Fellow
- Create Fellow
- Configure VT States
    - Profile `Normal`
    - Disable boot nav priority
    - Disable Disable loot
    - Disable combat
    - Enable buff
    - Disable nav
    - Enable Meta

- Load Meta Chain Information
    - In RE Directory Search for, match first found
        - MetaChainList-<charcter name>-<chain name>.txt
        - MetaChainList-<chain name>.txt
    - Each line in the file will be the name of a VTank meta file

- Load first meta
    - Pop the first meta off the chain list.
- Start VT
- say '/f !start'


Then we let the meta run.  We have to somehow detect when the meta is complete.  This could be tricky.  For now let's assume we need to detect this using heuristics.  Assume an individual meta is done when none of the following have happened for 60 seconds.
    - Character has not moved
    - Chracter has not attacked


Once a meta has been determined to be complete,
    - Pop the next meta file off the chain list
    - say '/f !start'


Crash Recovery.  The game will crash on a somewhat regular basis.  Other tools handle starting the game again and will start VT again.  However, we need to somehow resume our Meta Chain state. 
    - Persist the MetaChain state in a file.  
    - Reuse the same format.
    - Call it 'MetaChainList-<charcter name>-Current.txt'.  
    - We need to auto resume from this list if the file exists.
    - Delete this file when the chain completes



When Howard the Mad Ends, the chat output is
```

[VTank] Meta executing action: All: {Embedded Nav "[None]" (0pts), Set Meta State: * USE CHAIN *}
[VTank] Meta transitioning to state: * USE CHAIN *
```

However, I don't think we can consist on this output being the same for all meta's.