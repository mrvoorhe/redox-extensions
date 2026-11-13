# Monster list

`/re monsters` (alias `/re mon`) reads and edits the monster list of the loaded VTank settings profile, the list on
VTank's Monsters tab.

| Command | Does |
|---|---|
| `/re monsters [list]` | Lists every entry with its priority, `<DEFAULT>` first, and names the profile. |
| `/re monsters get <name>` | Shows one entry's priority.  For a name with no entry it says so and gives `<DEFAULT>`'s priority. |
| `/re monsters set <priority> <name>` | Changes an entry's priority.  `<DEFAULT>` can be set too. |
| `/re monsters add <priority> <name>` | Adds an entry with the Monsters tab's defaults and the given priority, at the end of the list. |
| `/re monsters remove\|rm <name>` | Removes an entry.  `<DEFAULT>` can't be removed. |

The name is the rest of the line, spaces included (`/re monsters set 3 Drudge Prowler`).  It is the entry's text,
which can be a VTank match rule rather than a plain monster name.  Names match ignoring case, as VTank's own lookup
does; `set` keeps the entry's spelling.  Priorities are -1 to 4, the values the Monsters tab cycles through.

Output lines start with `[RE] Monsters : `, so `rectl exec ... --wait "\[RE\] Monsters"` works.

## How it works

VTank has no public API for the monster list, and its external interface is gated by Virindi-signed keys.  The
plugin reaches VTank's internals by reflection (`VirindiInterop/VTMonsterListMembers.cs`) and calls the same methods
the Monsters tab does, so an edit behaves exactly like one made in the VTank window:

1. Update the entry in VTank's in-memory monster list.  This also clears VTank's monster-match cache, so the change
   applies the next time VTank picks a target.  The current target is not dropped.
2. Save the settings profile to disk (what the Monsters tab does after every edit).
3. Redraw the Monsters tab.

Nothing is written to the profile file from outside VTank, so there is no race with VTank's own saves and no risk of
VTank replacing an unreadable profile with its defaults.

The commands run on the game thread (typed, sent by a meta, or sent through the agent bridge).  VTank has no
locking, so don't call `VTMonsterList` from a background thread.

## VTank build

The obfuscated names are those of the 2016 VTank build (assembly version 1.0.0.0), the one Utility Belt patches.
`External/Virindi/utank2-i.dll` is that build.  The older 0.3.1.148 build has different names and is refused.

| Member | What it is |
|---|---|
| `PluginCore.dz` | VTank's state, created at login |
| `PluginCore.dt` | VTank has finished loading the character; saves are skipped until then |
| `PluginCore.p()` | Redraws the Monsters tab list |
| `dz.e` (`d1`) | The monster list |
| `d1+a` | An entry: `u` name, `a` priority, `v` is `<DEFAULT>` |
| `d1.d(name)` / `d1.c(name)` | Whether an entry exists / a copy of it |
| `d1.a(name, entry)` / `d1.a(name)` / `d1.f()` | Add or update / remove / a new entry with defaults |
| `dz.m` (`da`) | The loaded profiles: `m` settings profile file name, `i()` saves it |

Every member is checked by name, signature and type the first time a command runs.  On a VTank build that doesn't
match, the commands say "This VTank build isn't supported" and change nothing.  `VTMonsterListMembersTests` checks
the names against `External/Virindi/utank2-i.dll`, including that a monster row's first two columns land in the
entry's name and priority fields.

## Caveats

* A profile shared by several characters (one without `--Character_World` in its name) is held in memory by each
  client.  The others keep the old list, and the next settings save on any of them writes its old copy over the
  edit.  Edit character profiles, or run the same command on every client using the profile.
* With no settings profile loaded the save writes nothing, so the edit lasts only until the profile changes.
