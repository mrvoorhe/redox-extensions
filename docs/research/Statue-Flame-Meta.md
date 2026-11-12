# Research: statue flame meta

**Built 2026-11-11 as `aKTViridian`** (`tools/vt/examples/aKTViridian.py`, navs `aKTViridian-*.spec/.nav` next to
it).  Findings from testing it on Woodford:

* The Deru gem drops you at **42.70S 42.85E** (nav 42.849167, -42.701667), about 85 m SW of Brellegia, not next to
  her.  The user had walked to her.
* Cooldown replies, given every time when a task is on cooldown: the plaques say "You may complete this quest again
  in 19h ..."; Brellegia "Return tomorrow, and I will have more work for you."; Aangesilus "Thank you again for your
  assistance. Return later, and I may have more work for you."; Swift "Tomorrow! More work!".
* VT talk nodes for the four center NPCs take about 85-150 s in total.
* QuestLoot.utl now keeps `^Viridian Rise Deru Portal Sending Gem$` and `^Essence of (?!the Viridian Rise)`
  (backup `QuestLoot.utl.bak-2026-11-11`).  After two runs Woodford had gone from 2 Deru gems to 4.
* Laps: each zone loop takes about 6.5 minutes.  Right after a teleport the meta's position checks briefly see the old
  position, so lap counting ignores the first minute in a zone.
* Giving the Third Portal key to the entrance statue doesn't work: "You allow Ancient Statue of the Viridian Rise to
  examine your Viridian Key of the Third Portal."  How the keys are meant to be used is still unknown.
* Flame positions seen while testing: zone 1 53035 (17:18Z), 53033 (17:31Z); zone 2 53051 (17:19Z and 17:38Z).
* **The keys do work at the entrance**: giving the Third Portal key to statue 53130 ("You allow ... to examine")
  was followed by a teleport into zone 3, and the key wasn't used up.  So a key-holder can go straight to a later
  zone.  The meta doesn't use this (it does zones 1, 2, 3 in order, as asked).
* Stuck case: VT stood still for 15 minutes trying to loot a boss essence from a Corpse of Wind Fury, and the server
  refused every 3 s with "You may complete this quest again in 2h ..." (the boss essence pickup is on a ~2 h timer
  per character).  The meta's watchdog (5 m in 120 s) now walks on with combat and looting off for 20 s.  The user
  is disabling the boss essence loot rule for now, to make validating the meta simpler (2026-11-11).
* The center route uses waypoints and stops about 2 m in front of each NPC, in peace mode (talk nodes from a
  distance shuffled in combat mode, user).  It now takes about 35 s.

Cowhead end-to-end test (2026-11-11, 18:27Z on):

* Tasks were all available (cooldowns ~13 days old).  Plaques handed out all three in 24 s.  `/ub myquests` then
  showed **`killtaskviridiananek`, `killtaskviridianwasp` and `killtaskviridianzefir` at 0 / 10**, so the A'nekshen
  counter name is confirmed.  With an open task the center NPCs repeat the task text (e.g. Brellegia "The A'nekshen
  in the Viridian rise are driven to madness ...").
* The entrance portal took Cowhead's 11 Viridian Essence on the way in, as expected.
* Essence gives were broken.  The single give rule (retried on a stopwatch) fired once, in the same second as the
  talk node's use of the Essence NPC, and the server dropped that give.  The rule never fired again: its condition
  stayed true, and VT only fires on a change.  Fixed with two give states that hand over to each other every 2 s
  after the route has finished.  The "none left" check is only made at the 2 s handover, because straight after a
  give the count briefly read 0 and the meta left with one essence still in the pack.  Tested at the NPC (gave
  Essence of Zerzelikyr).  `actiontrygiveitem` itself works from /ub mexec.

Not tested yet: a run with real open kill tasks (Woodford's were all on cooldown, so the counters and the 15 minute
final hunt only ran through their "already done" paths), the death path, and `killtaskviridiananek` (the A'nekshen
counter name is a guess from its `...wait` flag).  Not in the meta chain yet (CHAIN = None).

Notes for a future VT meta that finds the statue with the blue flame in a zone, gets some items, then uses that
statue.  There are 2-3 more zones with their own statues to work out after this one.  Status: research, nothing built.

## Open questions

* Zone names, and how to get into each zone.
* Which items are needed first, and where they come from.
* What "use the statue" involves (the description mentions Lockpick).
* The exact rotation interval (the user thinks 15-20 minutes).  A logger is running, see [Rotation timing](#rotation-timing).
* Whether the flame is ever on no statue, or on two at once.

## Zone 1: Viridian Rise statues

Researched on Woodford, 2026-11-11, around landblock 0xB349 / 0xB34A (about 42.5S 41.7E).

### Statues

All share name `Ancient Statue of the Viridian Rise`, model 33558954, icon 25015, class Npc, and this description:
"An ancient statue infused with the magic of the Viridian Rise. There are ancient mechanisms that might be manipulated
with the Lockpick skill."  The only reliable way to tell them apart is the template type (Decal `Type`, UB
`wobjectgettemplatetype`), which is fixed per statue.  **Object ids are not stable, even within a session.**  When
Woodford came back in range at 16:09Z, Statue 5 had a new id (-261071774 became -265545113), and so did the flame.
Always look statues up by template type.

| Template | Id (2026-11-11) | Coords (ns, ew) | Nav `[ew, ns, z]` | Heading | Label |
|---|---|---|---|---|---|
| 53033 | -260161018 | 42.70S 41.77E | `[41.772500, -42.696667, 0.479583]` | 90 | Statue 1 (user's order) |
| 53037 | -259096898 | 42.46S 42.04E | `[42.038333, -42.457083, 0.490694]` | 270 | Statue 2 |
| 53035 | -265989615 | 42.29S 41.70E | `[41.695833, -42.294583, 0.469167]` | 135 | Statue 3 |
| 53036 | -266281212 | 41.99S 41.78E | `[41.785000, -41.987500, 0.464583]` | 135 | Statue 4 |
| 53034 | -261071774 | 42.52S 41.68E | `[41.676667, -42.516667, 0.473750]` | 45 | Statue 5, not shown by the user, but the flame moved here.  About halfway between Statue 1 and Statue 3, a little west |

Nav is the VT nav "use" node position (z = raw Z / 240), from `rectl world` / `rectl object`.

Other objects with similar names that are not part of the set (as far as we know):

* Two more `Ancient Statue of the Viridian Rise`: -267519988 (41.74S 42.28E) and -266397471 (41.63S 42.08E).
  Template types not checked yet.
* A far one, template 53130 (id 2067042424, 43.23S 41.60E, z much lower).
* Three `Ancient Statue of the Viridian Root` (2068091271/3/4, about 42.8S 42.5E).

### The blue flame

The flame is a separate object, `Fiery Remains`, class Npc, template **53026**, model 33560191, icon 4198, placed at
the statue's exact ns/ew (0.0 m flat) and about 2.5 m higher.  Decal reports its `Scale` as 0.

* Confirmed by the user: at 14:40Z it was on Statue 1 (53033), and only that statue had an object on it.
* At 15:30Z it was on 53034.  The new Fiery Remains had a **new id** (-266436630 before, -266218922 after), so the
  server deletes it and creates a new one.  Look it up by template type, never by id.

Things checked that did **not** show the glow:

* Nothing on the statue itself changes.  Name, description, model, icon and the Decal long/double/string values are
  the same for every statue.  None have the physics DefaultScript bit (0x2000).  `PhysicsDataFlags` is 0x1980F on
  four statues and 0x19803 on 53037.  The difference is only the velocity and acceleration bits, so it isn't related
  to the glow.
* Selecting a statue only produces the normal appraisal reply (F7B0 event 201), with no extra data.
* No PlayScript (F755) or other effect message was sent to a statue while it was being selected.  The F755s in the
  capture were spawn/despawn effects on monsters and combat effects on the player.

### Detecting it from a meta (tested)

UtilityBelt expression, tested with `/ub mexec` through the agent bridge.  It returns the template type of the statue
under the flame:

```
setvar[fr,wobjectfindnearestbytemplatetype[53026]];wobjectgettemplatetype[listgetitem[listfilter[wobjectfindalllandscapebynamerx[`Ancient Statue of the Viridian Rise`],`coordinatedistanceflat[wobjectgetphysicscoordinates[$1],wobjectgetphysicscoordinates[getvar[fr]]]<1`],0]]
```

Results: `53033` at 15:25Z, `53034` at 15:30Z.  It works with the window minimized, no screenshot needed.

Still to handle in the meta:

* No flame in range: `wobjectfindnearestbytemplatetype` returns 0 and the statue list is empty, so check
  `getobjectinternaltype[...]==7` first (same guard as `farmspot.py`'s door check).
* The client has to know about the objects.  From Woodford's spot every statue was known, up to about 170 m.
* The flame's statue can be used directly, e.g. `actiontryuseitem[<statue wobject>]`.  How to use it is still to
  be worked out.

### Rotation timing

`F:\ac-files\decaldev\MetaAnalysis\2026-11-11\flamelog.py` polls Woodford every 20 s and appends a line to
`flamelog.csv` (same folder) each time the flame's id or statue changes.

Times here are UTC; the CSV uses local time (UTC-5).

| Time (UTC) | Statue template | Notes |
|---|---|---|
| ~14:40Z to 15:25Z | 53033 | Seen at 14:40 and 15:25.  It didn't move for at least 45 minutes, so either the interval is longer than 15-20 minutes or the flame can stay on the same statue |
| by 15:30Z | 53034 | User saw it move |
| 15:42Z to 16:09Z | ? | Woodford away at the center, nothing seen |
| 16:09Z | 53034 | Still (or again) on Statue 5.  New ids: flame -262750644, statue -265545113 |
| 16:31:43Z | 53034 | Still on Statue 5 after more than 60 minutes, even though Woodford used it at 16:12Z |

Zone 2 and zone 3 (same logger):

| Time (UTC) | Zone 2 | Zone 3 | Notes |
|---|---|---|---|
| 16:13:26Z | 53049 | (out of range) | |
| 16:22:57Z | 53049 | 53099 | Woodford used 53049 at 16:22:08Z.  The flame didn't move right away |
| 16:28:25Z | (out of range) | 53099 | |
| 16:29:32Z | **53055** | 53099 | Zone 2's flame had moved (new flame id -265203511) |

The logger was stopped at about 16:45Z.  What we know: using a flame statue doesn't move its flame.  Zone 1's
flame stayed on one statue for over 60 minutes, and zone 2's moved within about 6.5 minutes of 16:22:57Z.  So the
interval is either long and per zone, or irregular.  Either way **the meta has to check where the flame is each time
it gets to a zone**, never remember it from an earlier run.

### Getting to and using the flame statue

Seen in the tracker log at 16:08Z, before the user explained it:

```
16:08:55 use: Ancient Statue of the Viridian Rise (template 53130, 2067042424, 43.23S 41.60E)
         "The mark of the Viridian Guardians briefly burns with blue flame."
16:08:58 use: Viridian Portal (template 53060, -258688975, 43.23S 41.59E)
         "You hand over all of your Viridian Essences."
```

* Statue **53130** is the "start" statue near the task plaques.  It's at a much lower z (0.263 vs about 0.47) and its
  id is in the static range.  Nav `[41.595833, -43.230833, 0.263333]`.  The flame never sits on it.
* Using it brings up a **Viridian Portal** (template 53060) next to it.  The portal had gone a few minutes later (the
  id was no longer known), so it's temporary.
* Using the portal takes **all** your `Viridian Essence` (Woodford had 18).  The next activity was fights and
  corpse loots at Statue 5's position (about 42.51S 41.69E), which is where the flame was.  So the portal seems to
  take you to the flame statue (to confirm).
* **Using the flame statue needs at least 5 `Viridian Essence`** (user, 16:12Z).  Woodford had 7 at that point,
  collected after the portal.  So you get them from kills near the flame statue.

**Correction (16:12Z run, user narrated):** the portal is made by the *flame* statue, and it gives you a key, it
doesn't teleport you:

```
16:12:15 use: Ancient Statue (template 53034 = Statue 5, under the flame)
         "The Viridian Essences you have gathered flare up and empower the portal. The amount required was reduced by your Lockpick skill."
         "You hand over all of your Viridian Essences."
16:12:18 use: Viridian Portal (template 53061, -265400065, at the statue)
         "Viridian Portal gives you Viridian Key of the Second Portal."
         "Viridian Portal gives you 8 Infused Amber Shards."
```

* Lockpick lowers the number of essences needed (5 for Woodford).  It still takes all of them.
* The portal's template differs between runs: 53060 at 16:08Z (made by statue 53130), 53061 at 16:12Z (made by
  53034).  So the 16:08Z use of 53130 was probably a different step, e.g. handing in an earlier key or entering a
  zone.  To be confirmed.
* The key is **`Viridian Key of the Second Portal`**, template **53480**, class Misc, bonded and attuned.  "Hand this
  to the entrance statue of the Viridian Rise to enter the second area of the Viridian Rise."  So the entrance
  statue is presumably 53130.  The portal stays up for a short while (still there 20 s later, gone after a few
  minutes).

Not known yet: whether the portal needs a minimum number of essences, and what the 16:08Z 53130 use and portal did.

### Items needed

* **`Viridian Essence`**, template **52970**, stacks to 100, bonded and attuned, burden 4.  "A mote of Deru life magic
  used to power the gates in the Viridian Rise."  The portal at 53130 takes all of them.  The flame statue needs at
  least 5.

* **`Viridian Rise Deru Portal Sending Gem`**, template **53450**, from `Viridian Mite` corpses.
  * Looted by the user on Woodford, 2026-11-11, from the second Mite (corpse -259770662 at 42.01S 42.43E).  The first
    Mite (-267110028, 42.23S 41.95E) had none, so it's a chance drop.  Drop rate unknown.
  * "Double Click on this portal gem to transport yourself to the Deru tree in the Viridian Rise."  Casts spell 6322.
  * Stacks to 25, burden 5, mana 70/70, Arcane Lore 40, not bonded or attuned (so it may be tradeable between
    characters, untested).  Description: "A glowing green gem."
  * In a meta: `wobjectfindininventorybytemplatetype[53450]` (0 when there is none), or by exact name with
    `wobjectfindininventorybyname`.
  * The client didn't show the corpse's contents through `rectl world --container`, even after the user opened it.
    Spotting a gem in a corpse before looting it needs another method (for example VT loot rules on the name).

### Kill task givers

Tracked with `F:\ac-files\decaldev\MetaAnalysis\2026-11-11\tasktrack.py` (logs each UseItem, game action 54, plus
the chat that follows) into `tasktrack.log`.

| # | Giver | Template | Id | Coords | Task text |
|---|---|---|---|---|---|
| 1 | `Brellegia's Marker` (class Foci) | 53486 | 2067042423 | 43.21S 41.97E | "End the suffering of 10 A'nekshen within the Viridian Rise and I will reward you for your efforts. If you complete this task then find me in the center of the Viridian Rise. Brellegia." |
| 2 | `Aangesilus' Marker` (class Npc) | 53488 | 2067042426 | 43.09S 41.88E | "Cull the wasps and wasp hives within the Viridian Rise and I will reward you for your efforts. If you complete this task then find me in the center of the Viridian Rise. Aangesilus." |
| 3 | `Swift's Marker` (class Npc) | 53487 | 2067042354 | 43.02S 41.90E | "Go! Go! Kill 10 Zefir! I reward you for this! Find me in the center! Near tree! Swift." |

All three are plaques within about 50 m of each other, about 43.1S 41.9E.  Each only puts its plaque text in chat
(there's no separate "task started" message), which starts with `A plaque on this statue reads`.  All three say
to come back to "the center of the Viridian Rise", near the tree.  That's presumably where the Deru gem
goes ("the Deru tree in the Viridian Rise").

### Essence turn-in (tested)

At 16:02:44Z the user gave `Essence of Night Brier`, then `Essence of Norshuntyr`, to the `Essence of the Viridian
Rise` NPC (game action 205, give).  So that NPC accepts them, even though the item text names Kervim'telek.  Each
give produced:

```
You give Essence of the Viridian Rise Essence of Night Brier.
You've stamped VRRareEssenceCounter!
Essence of the Viridian Rise tells you, "Please find me more rare bosses of the Viridian Rise and I will reward you when you've turned in 25 essences."
```

So in the meta, give every `^Essence of ` item to the NPC.  There's no reward until the counter reaches 25.

### Quest flags (`/myquests`, 2026-11-11 16:03Z)

The meta can read these with UB `getquestktprogress[flag]`, `getquestktrequired[flag]`, `getqueststatus[flag]` and
`testquestflag[flag]`.  UB only refreshes them on `/myquests` (or `/ub myquests`), so run that before checking.
Format: `flag - solves (last stamp) "description" maxSolves timerSeconds`.

| Flag | Value at 16:03Z | Meaning |
|---|---|---|
| `killtaskviridiananekwait` | 2 solves, stamped 15:42:21Z, timer 72000 | Brellegia (A'nekshen) cooldown, 20 h.  Stamped when the task was turned in |
| `killtaskviridianwasp` | 0 / 10, started 15:43:33Z | Aangesilus wasp kill counter (10 kills; "wasps and hives") |
| `killtaskviridianwaspwait` | 2 solves (old), timer 72000 | Aangesilus cooldown, 20 h |
| `killtaskviridianzefir` | 0 / 10, started 15:44:10Z | Swift Zefir kill counter |
| `vrrareessencecounter` | 2 / 25 | Essence turn-in counter |
| `viridian_mite_legbone_found` | 1 solve, timer 72000 | Woodford has a `Viridian Mite Legbone` (-260394483).  Another Mite drop, purpose unknown |
| `foundrarezefirscrystal` | 1 solve | Unknown, probably a rare Zefir drop |
| `viridianrecharge` | 16 solves, last 15:15:23Z, timer 20 | 20-second timer, unknown (maybe a statue or the gem?) |

The A'nekshen counter flag (probably `killtaskviridiananek`) wasn't listed after the turn-in.  It looks like the
counter is removed when the task is handed in, and only the `...wait` timer is left.  Presumably a Zefir wait flag
(`killtaskviridianzefirwait`) will show up after Swift's turn-in.

So for each kill task the meta can:
* skip the giver if `getqueststatus[<task>wait]` shows the timer is still running (not tested how it reports that);
* go to the turn-in when `getquestktprogress[<task>] >= getquestktrequired[<task>]`.

### Turn-in NPCs (center of the Viridian Rise)

The Deru gem takes you there.  At 15:41:59Z the user used `Viridian Rise Deru Portal Sending Gem` (-265915894),
chat said "You have been teleported.", and the gem was used up (the stack was 1 and it's no longer in inventory).
Woodford arrived next to Brellegia, at landcell 0xB549xxxx (-1253441524).

| Turn-in | Class | Template | Id (2026-11-11) | Coords | Nav `[ew, ns, z]` |
|---|---|---|---|---|---|
| `Brellegia` (turn-in for Brellegia's Marker, task 1) | Npc | 53280 | -266502574 | 42.37S 42.98E | `[42.977917, -42.370833, 0.491688]` |
| `Aangesilus` (gives the wasp task too, task 2) | Npc | 53281 | -260107610 | 42.38S 43.25E | `[43.252083, -42.377083, 0.491688]` |
| `Swift` (turn-in for Swift's Marker, task 3) | Npc | 53283 | -266004999 | 42.34S 43.47E | `[43.470833, -42.339583, 0.491708]` |

Swift at 15:44:09Z (task not done yet, so these lines just repeat the task):

```
Swift tells you, "You! Strong! Yes! My kin! Zefir! Corrupted! End suffering! Go!"
Swift tells you, "Ten! At least ten! Yes! Rewards! Yes! End corruption!"
```

Task 4, `Essence of the Viridian Rise` (Npc, template **64454007**, id 2069143592, 42.31S 43.50E,
`[43.499225, -42.306721, 0.491698]`, right next to Swift).  The template number is far outside the normal range, so
it's probably a custom server object, and its id is in the static range (2069...), so the id may stay the same.
At 15:44:38Z:

```
Essence of the Viridian Rise tells you, "The creatures of the Viridian Rise have been corrupted. Please end the suffering of 25 of the bosses of the area and return to me their essences."
```

So: kill 25 bosses and bring back their essences.  It's the same NPC that gives the task and takes the turn-in.

**Optional in the meta.**  Keep it in the list, but don't require it on every run: turn in whatever essences we have,
and don't farm bosses just for this task (user, 2026-11-11).

The items are the `Essence of <boss>` drops (user).  Woodford had two from an earlier trip:

| Item | Template | Notes |
|---|---|---|
| `Essence of Night Brier` | 53364 | bonded, attuned, burden 1, doesn't stack |
| `Essence of Norshuntyr` | 53362 | same |
| `Essence of Wind Fury` | 53366 | Looted by the user in zone 2 at 16:19Z, near zone 2 statue 4 (about 41.2S 42.8E).  Looting it stamped `pickupWindFuryEssence`, so each boss essence probably has its own pickup flag, maybe on a cooldown |

To spot a boss essence pickup in chat, watch for `You've stamped pickup<Boss>Essence!`.

Both say "Kervim'telek of the Virindian Rise would be interested in this essence." (sic, "Virindian").  So they may go
to an NPC named Kervim'telek rather than, or as well as, the Essence of the Viridian Rise NPC.  Check which NPC
accepts them.  They're attuned, so each character has to collect its own.  The 18 `Viridian Essence`
(-259817924) is a different item.  Match by name `^Essence of ` and exclude `Essence of the Viridian Rise`, which is
the NPC.  Other boss essence templates are probably near 53362-53364.

Other NPC used here, not a kill task: `Dridge` (Npc, template 53306, -266018405, 42.34S 43.32E,
`[43.316667, -42.343750, 0.491167]`).  He's the Cooking/mead NPC: "Give me one Infused Amber and I will give you five
jars of honey and the recipe to make the meads."  The meads need a Brew Kettle, water, the amber honey and
Nightbloom.  Brellegia's reward includes 10 Infused Amber Shards, which may be what he wants (not checked).

Aangesilus (the NPC, not the marker) gave the wasp task at 15:43:32Z.  The user had already taken it from the
marker, so this was probably the NPC repeating it, or giving it again.  Not tested whether the NPC alone is enough
without the marker.  It's in two `tells you` lines:

```
Aangesilus tells you, "As the great Deru tree fights off the corruption which attacks it the land is infused with its life energies. This has caused the phyntos wasps and hives of the area to grow wildly. We need to cull these before they grow out of control."
Aangesilus tells you, "Destroy wasps and wasp hives within the Viridian Rise and I will reward you for your efforts."
```

Brellegia's turn-in at 15:42:18Z (UseItem on Brellegia):

```
Brellegia tells you, "I can feel the spirits of those you have freed rising towards the tree. The cycle begins anew. Here is your reward."
Brellegia gives you Aged Legendary Key.   (x5)
Brellegia gives you Trade Note (250,000).
Brellegia gives you 10 Infused Amber Shards.
You've earned 1,492,479,859 experience.
Brellegia gives you 10 Enlightened Coins.
```

#### Turn-in run (user demo, 16:37Z)

From zone 1: `Viridian Rise Deru Portal Sending Gem` (template 53450), "You have been teleported.", then:

```
16:37:58 use: Brellegia (53280)
         Brellegia tells you, "Return tomorrow, and I will have more work for you."
         You may complete this quest again in 19h 4m 22s.
16:38:06 use: Aangesilus (53281)
         Aangesilus tells you, "Excellent. That should be enough for the moment. Here is your reward."
         Aangesilus gives you Aged Legendary Key.   (x5)
         Aangesilus gives you Trade Note (250,000).
         Aangesilus gives you 10 Infused Amber Shards.
         You've earned 1,496,211,059 experience.
         Aangesilus gives you 10 Enlightened Coins.
```

* Using a giver whose task is on cooldown prints `Return tomorrow...` plus `You may complete this quest again in ...`.
  That's harmless, so the meta can use every turn-in NPC every time without checking flags first.
* Turn-in rewards start with `<NPC> tells you, "...Here is your reward."`.  Aangesilus' reward is the same as
  Brellegia's.
* **Woodford's client crashed right after this** (16:38Z), before Swift.  The wasp task was already complete by
  16:32Z ("You have killed 10 ... Wasps! Your task is complete!").
* Swift, after the relog (16:39:57Z):

  ```
  Swift tells you, "Good! Good! Corruption defeated! Here! Here! Take!"
  Swift gives you Aged Legendary Key.   (x5)
  Swift gives you Trade Note (250,000).
  Swift gives you 10 Infused Amber Shards.
  You've earned 1,496,211,059 experience.
  Swift gives you 10 Enlightened Coins.
  ```

  Swift doesn't say "Here is your reward", so to detect any of the three turn-ins, match
  `^(Brellegia|Aangesilus|Swift) gives you Aged Legendary Key\.`.  All three give the same reward.

The turn-ins for Aangesilus and Swift are presumably also at the center.  Not found yet.  Other NPCs within 60 m of
Brellegia: `Doriathazaar` (-259769626, 42.42S 43.08E) with 7 `Doriathazaar's Horde`, and `V'exallia`
(2069143596, 42.26S 43.18E).

## Zone 2 (second area of the Viridian Rise)

16:13Z: the user says Woodford is in zone 2, at landcell 0xB34B0031 (about 41.91S 41.88E), 30 m from Statue 4
(53036), and still holding the key (-258631433).

**How to get in (user):** use the zone 1 flame statue, then use the portal it makes.  So the 16:12Z sequence (flame
statue, then Viridian Portal 53061) both took Woodford into zone 2 and gave the key.  The chat didn't include
"You have been teleported."  So the meta should check for arrival by position (or by seeing zone 2 objects), not
by chat.  The key is presumably for coming back in later through the entrance statue (53130) without redoing
zone 1.  The 16:08Z use of 53130 plus portal 53060 fits that: an earlier key taking Woodford back into zone 1.
Both to be confirmed.

Zone 2 statues (same name `Ancient Statue of the Viridian Rise`), the ones known so far:

| Template | Id (16:2xZ) | Coords | Nav `[ew, ns, z]` | Notes |
|---|---|---|---|---|
| 53049 | -266397471 | 41.63S 42.08E | `[42.077083, -41.634583, 0.465382]` | First zone 2 statue the user visited.  **Flame here** at about 16:20Z (user confirmed) |
| 53051 | -266053491 | 41.29S 42.28E | `[42.281250, -41.287500, 0.472917]` | Zone 2 statue 2 (user's order) |
| 53053 | -267519988 | 41.74S 42.28E | `[42.279167, -41.737500, 0.483750]` | Zone 2 statue 5 (user's order) |
| 53055 | -266346627 | 41.51S 42.44E | `[42.435000, -41.505417, 0.483958]` | Zone 2 statue 3 (user's order) |
| 53057 | -266532488 | 41.17S 42.75E | `[42.747083, -41.165833, 0.483750]` | Zone 2 statue 4 (user's order) |

### Zone 2 hunt loop nav: `VRMetaZone2.nav`

Recorded by the user, in the VT folder.  Circular, 96 points, about 1,010 m around, starting at about 41.84S 41.93E
(node 0, near where the zone 1 portal drops you; node 95 is 20 m from the 16:13Z arrival point).  It passes within
2.3 m of every zone 2 statue, in this order:

| Statue | Nearest node | Distance |
|---|---|---|
| 53049 | 6 | 1.8 m |
| 53051 | 15 | 2.3 m |
| 53055 | 20 | 1.8 m |
| 53057 | 35 | 0.9 m |
| 53053 | 63 | 2.3 m |

53076 is 39 m from the closest node (45), which fits it not being part of zone 2.  The nav has no use nodes.  Using the
flame statue would be done by the meta (for example `actiontryuseitem`) once it's in range.

Also seen from statue 4: **53076** (-259668803, 41.32S 42.86E, `[42.857917, -41.323333, 0.483750]`), 45 m southeast.
Its template is well outside 53049-53057, so it may belong to zone 3.  To be confirmed.

The templates seen so far are odd numbers.  There are probably more (53050/52/54/56?) still out of range.  The user is
visiting them, so fill this in.

**Zone 2's flame is also `Fiery Remains`, template 53026**, sitting 0.0 m flat and 2.5 m above the statue, the same as
zone 1.  From inside zone 2 both zones' flames are in range (the zone 1 one was on 53034 at the same time), so the
nearest-flame expression could pick the wrong one near the border.  This version lists every flame's statue
(tested 16:2xZ, gave `[53034,53049]`).  Nested backticks have to be escaped as `` \` ``:

```
listmap[wobjectfindallbytemplatetype[53026],`setvar[f,$1];wobjectgettemplatetype[listgetitem[listfilter[wobjectfindalllandscapebynamerx[\`Ancient Statue of the Viridian Rise\`],\`coordinatedistanceflat[wobjectgetphysicscoordinates[$1],wobjectgetphysicscoordinates[getvar[f]]]<1\`],0]]`]
```

The meta can then pick the entry that's in the current zone's template set (zone 1: 53033-53037, zone 2: 53049+).

Other statue-like objects far off (static ids): `Ancient Statue of the Viridian Rise` 53114 (2069139522, 43.07S 42.90E,
near the center), and six `Ancient Statue of the Viridian Root` 53263-53269 (2068091270-5, about 42.9S 42.6E).
Those may belong to zone 3.

## Zone 3 (third area of the Viridian Rise)

Getting in from zone 2 works the same way as getting into zone 2 from zone 1 (tracker log, 16:22Z):

```
16:22:08 use: Ancient Statue (template 53049 = zone 2 flame statue)
         "The Viridian Essences you have gathered flare up and empower the portal. The amount required was reduced by your Lockpick skill."
         "You hand over all of your Viridian Essences."
16:22:12 use: Viridian Portal (template 53062, -259589059, 41.63S 42.09E)
         "Viridian Portal gives you Viridian Key of the Third Portal."
         "Viridian Portal gives you 10 Infused Amber Shards."
```

* Portal templates go up by one per zone: 53061 (zone 1 to 2), 53062 (zone 2 to 3).  53060 was the entrance statue's.
* Key: **`Viridian Key of the Third Portal`**, template **53481**.  "Hand this to the entrance statue of the Viridian
  Rise to enter the third area of the Viridian Rise."  (Second Portal key = 53480.)  Woodford still has the Second
  Portal key too, so the keys aren't used up when you go through the portals.
* Arrival: landcell 0xB54B0001 (9.01, 7.85, 113.51), about 41.92S 42.89E.  No teleport message again.
* Woodford also has 3 `Viridian Rise Deru Portal Sending Gem` now (more Mite drops).

Statues known from the arrival point that aren't in zone 1's or zone 2's sets (probably zone 3's):

| Template | Id (16:2xZ) | Coords | Nav `[ew, ns, z]` | Notes |
|---|---|---|---|---|
| 53076 | -259668803 | 41.32S 42.86E | `[42.857917, -41.323333, 0.483750]` | Zone 3 statue 1 (user's order) |
| 53099 | -264995432 | 41.17S 43.23E | `[43.229167, -41.170833, 0.483750]` | Zone 3 statue 2 (user's order).  **Flame here** at about 16:25Z (Fiery Remains -266267995) |
| 53100 | -265168810 | 41.54S 43.15E | `[43.154167, -41.537500, 0.483750]` | Zone 3 statue 3 (user's order) |
| 53101 | -265529615 | 41.55S 43.63E | `[43.633333, -41.551250, 0.483750]` | Zone 3 statue 5 (user's order) |
| 53102 | -265325572 | 41.18S 43.57E | `[43.571250, -41.175417, 0.471250]` | Zone 3 statue 4 (user's order) |

Zone 3's templates aren't consecutive like zones 1 and 2 (53076, 53099, 53100 so far).  So the meta needs an
explicit list of templates for each zone, not a range.  The flame is Fiery Remains 53026 again.  Zone 3 set
(all five visited by the user): 53076, 53099, 53100, 53101, 53102.

### Zone 3 hunt loop nav: `VRMetaZone3.nav`

Recorded by the user, in the VT folder.  Circular, 137 points (all plain waypoints), about 1,270 m around.  Node 0
is 5 m from where the zone 2 portal drops you (41.90S 42.89E).  It passes within 1.8 m of every zone 3 statue:

| Statue | Nearest node | Distance |
|---|---|---|
| 53076 | 15 | 1.8 m |
| 53099 | 26 | 0.9 m |
| 53100 | 35 | 1.3 m |
| 53102 | 89 | 1.5 m |
| 53101 | 100 | 1.4 m |

### Zone 1 hunt loop nav: `VRMetaZone1.nav`

Recorded by the user, in the VT folder.  Circular, 138 points (all plain waypoints), about 1,240 m around.  Node 0 is
at 43.16S 41.93E, where the entrance portal (53060) drops you.  The user entered zone 1 that way just before
recording (16:30:41Z: entrance statue 53130, "The mark of the Viridian Guardians briefly burns with blue flame.",
then 16:30:44Z portal 53060, "You hand over all of your Viridian Essences.").  So the entrance portal also takes
all of your essences.  It passes within 1.8 m of every zone 1 statue:

| Statue | Nearest node | Distance |
|---|---|---|
| 53033 | 13 | 0.9 m |
| 53037 | 24 | 1.2 m |
| 53034 | 35 | 1.5 m |
| 53035 | 41 | 1.8 m |
| 53036 | 58 | 0.9 m |

### Getting to the Viridian Rise (user demo, 16:41Z)

Recorded with `tools/vt/vtrecord.py` into `F:\ac-files\decaldev\MetaAnalysis\2026-11-11\goto-vr` (positions,
actions and chat logs, plus a draft `route.spec` for `vtnav.py build`.  The draft hasn't been built or tested).

1. **`Halls of Introduction Gem`** (template 19851000, inventory) takes you to the Halls of Introduction
   (cell 0x012F0126).  It was used before the recorder started.  The tracker logged it at 16:41:38Z.  The spec adds
   `chat /ub usei Halls of Introduction Gem`.
2. Walk 11 waypoints inside the Halls (0x012F...) to the **`Daily Portals`** portal (class Portal, template 19842018,
   nav `[-101.143034, -64.474066, -0.000262]`).  "A helpful area where one can organize their killtasks."
3. That takes you to the Daily Portals hub (0x6444...).  Walk about 9 waypoints to the **`Base of the Viridian Rise`**
   portal (template 694200177, nav `[-21.450383, -48.841137, -0.100262]`).  Destination "Base of the Viridian Rise
   (43.3S, 41.5E)".
4. You arrive at 0xB349001B (73.47, 71.72, 60.33), a few meters from the entrance statue 53130.
5. Then the entrance statue and Viridian Portal 53060 take you into zone 1 (0xB3490034 164.61 91.34 112.01, the
   zone 1 loop's node 0).

The spec uses `talk` nodes for the statue and the Viridian Portal, because they're class Npc, not Portal.  The Viridian
Portal only exists for a short time after the statue is used, so test that VT can use it from a nav, or do these two
steps from the meta (`actiontryuseitem`) instead.  Navs that help the builder go in `tools/vt/examples`, not the VT
folder.

### Returning to the entrance (same from every zone, user)

Shown by the user at 16:35Z, from zone 1:

```
16:35:55 use: Portal Recall Gem (-258615552, template 86753076, inventory)
16:36:06 use: Ancient Statue (53130, entrance)  "The mark of the Viridian Guardians briefly burns with blue flame."
16:36:08 use: Viridian Portal (53060)            (no "hand over" line, because Woodford had no essences)
```

* **`Portal Recall Gem`** (template 86753076, a custom template) brings you back to the entrance.  It recalls to the
  last portal you used, and the user says it works the same from every zone.  No chat line was logged for the
  recall itself.
* The gem **isn't used up**.  The same item (-258615552, stack 1) was still in inventory afterwards, so one gem is
  enough.  The meta can use it by name, e.g. `/ub usei Portal Recall Gem`, the way other metas use gems.
* The statue and portal after it were the user going back into zone 1.  Woodford ended up at the zone 1 loop's node 0
  (0xB3490034 164.61 91.34 112.01, about 43.17S 41.90E).  The entrance portal only prints "You hand over all of your
  Viridian Essences." when you have some.

### Statue sets, all zones

| Zone | Statue templates | Portal made by the flame statue | Key it gives |
|---|---|---|---|
| 1 | 53033, 53034, 53035, 53036, 53037 | 53061 | Viridian Key of the Second Portal (53480) |
| 2 | 53049, 53051, 53053, 53055, 53057 | 53062 | Viridian Key of the Third Portal (53481) |
| 3 | 53076, 53099, 53100, 53101, 53102 | ? | ? |
| Entrance | 53130 (near the task plaques) | 53060 | n/a |

## Zone 4+

Not started (the user said 2-3 more zones, so there may be one more).

## Method notes

* `rectl net --server all --client all` while the user selected each statue, then `rectl object` on each.  The flame
  wasn't in the network capture because every object had been created before capture started.  It was found by
  looking for objects at the same coordinates as each statue (`rectl world`, every object within 6 m of a statue).
* `rectl exec` with `--wait "A|B"` breaks in cmd, so use a single word.
