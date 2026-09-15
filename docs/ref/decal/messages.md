# Protocol Messages

> Generated from the DecalDev AC protocol reference at <https://skunkworks.sourceforge.net/protocol/Protocol.php> (version 2006.08.22.1).

Top-level messages, ordered by opcode. Retired messages are kept for reference but are no longer used by current clients.

## 0024: Destroy Object

Sent every time an object you are aware of ceases to exist. Merely running out of range does not generate this message - in that case, the client just automatically stops tracking it after receiving no updates for a while (which I presume is a very short while).

- **object** `ObjectID` — The object that ceases to exist.

## 0037: Local Chat

**Retired.**

Contains a message, type, person's name and source person ID. If this is local chat or spellcasting, the radar filtering is done on the client (so actually hear talking within 2 landblocks).

- **text** `String` — The message text.
- **senderName** `String` — Character name of the speaker.
- **sender** `ObjectID` — Character ID of the speaker.
- **type** `DWORD` — Type of text.

## 005E: Attack

**Retired.**

A player has attacked a monster (or player).

- **target** `ObjectID` — Object ID of the monster (or player) being attacked.
- **unknown** `DWORD` — unknown - 0x00000019
- **attacker** `ObjectID` — Object ID of the attacking player.

## 0197: Adjust Stack Size

For stackable items, this changes the number of items in the stack.

- **sequence** `BYTE` — Seems to be a sequence number of some sort
- **item** `ObjectID` — Item getting it's stack adjusted.
- **count** `DWORD` — New number of items in the stack.
- **value** `DWORD` — New value for the item.

## 019E: Player Killed

A Player Kill occurred nearby (also sent for suicides). This could be interesting to monitor for tournements.

- **text** `String` — The death message (is blank for suicides causing a blank line on your scrolling window).
- **killed** `ObjectID` — The ID of the departed.
- **killer** `ObjectID` — The ID of the character doing the killing.

## 01B5: Broadcast Text

**Retired.**

Used for leather crafter and collector advertisements.

- **message** `String` — The text for display.
- **senderName** `String` — Name of the speaker.
- **sender** `ObjectID` — Character ID of the sender - used for squelch and radar filtering.
- **unknown1** `float` — Unknown - Usually 20.0.
- **color** `DWORD` — Color the client uses for displaying the text.

## 01E0: Indirect Text

Indirect '/e' text.

- **sender** `ObjectID` — The ID of the character performing the emote - used for squelch/radar filtering.
- **senderName** `String` — Name of the character performing the emote.
- **text** `String` — Text representation of the emote.

## 01E2: Emote Text

Contains the text associated with an emote action.

- **sender** `ObjectID` — The ID of the character performing the emote - used for squelch/radar filtering.
- **senderName** `String` — Name of the character performing the emote.
- **text** `String` — Text representation of the emote.

## 0229: Set Coverage

**Retired.**

Sets the coverage of an object

- **sequence** `BYTE` — Count of times this object has been equipped
- **object** `ObjectID` — The ID of the object this message refers to.
- **unknown** `DWORD` — Unknown, was always 0x0A (10) in testing
- **coverage** `DWORD` — If this item can be worn or wielded, the mask of slots it occupies.

## 022C: Set Character Flag

**Retired.**

Set a flag for a character.

- **unknown** `BYTE` — Possibly a sequence number, or it stays as is every time.
- **character** `ObjectID` — The character getting it's flags changed (including yourself).
- **flag** [`BooleanPropertyID`](enum-types.md#booleanpropertyid) — The getting changed, this may be a mask into an actual mask somewhere.
- **set** `DWORD` — 1 to set the flag, 0 to clear the flag.

## 022D: Set Wielder/Container

**Retired.**

Sets the wielder/container of an object, used in equipping

- **sequence** `BYTE` — Number of equips of this object
- **object** `ObjectID` — The ID of the object this message refers to.
- **equipType** `DWORD` — 3 = Set Wielder, 2 = Set Container
- **container** `ObjectID` — The object that the wielder or container being being set to

## 022E: Set Object Resource

**Retired.**

set a resource for a character.

- **sequence** `BYTE` — seems to be a sequence number of some sort
- **object** `ObjectID` — The ID of the object this message refers to.
- **unknown1** `DWORD` — Unknown
- **unknown2** `DWORD` — Unknown

## 0237: Update Statistic

**Retired.**

Record a stat changing

- **sequence** `BYTE` — Seems to be a sequence number of some sort
- **statistic** `DWORD` — Statistic being set.
- **value** `DWORD` — Replacement value.

## 023B: Update Last Attacker

**Retired.**

Sent whenever the last attacker changes

- **sequence** `BYTE` — Seems to be a sequence number of some sort
- **unknown** `DWORD` — An unknown value (0x0000000B in testing). (this seems to be a key, indicating what ObjectID is, with 0x0B being Last attacker)
- **objectID** `ObjectID` — Previous attackers ObjectID

## 023D: Update Last Corpse Location

**Retired.**

Sent whenever you leave a corpse (wasn't tested in a dungeon).

- **unknown** `DWORD` — Unknown - 0x0000000E
- **position** [`Position0`](struct-types.md#position0) — Location of the (outdoor) corpse you just left.

## 023E: Skill Experience

**Retired.**

Experience directly applied to your skill

- **skill** `WORD` — Your skill that's receiving XP
- **unknown** `BYTE` — Unknown filler. Always 0 for me.
- **sequence** `BYTE` — Seems to be a sequence number of some sort
- **skillOffset** `WORD` — The offset from the attribute base.
- **unknown1** `WORD` — Sept unknown added, always 0x0001?
- **skillTrained** `DWORD` — Skill disposition
- **appliedXP** `DWORD` — experience applied towards this skill so far
- **bonusPoints** `DWORD` — Bonus points given during character creation
- **difficulty** `DWORD` — probably used in the xp timer calculation
- **unknown2** `DWORD` — unknown
- **unknown3** `DWORD` — unknown

## 0240: Train Skill

**Retired.**

Change training state of a skill

- **sequence** `BYTE` — Seems to be a sequence number of some sort.
- **skillID** `WORD` — ID of the skill that is changing
- **trained** `DWORD` — new training disposition

## 0241: Update Attribute

**Retired.**

Sent every time you upgrade one of the 6 primary attributes.

- **Attribute** `WORD` — Atribute being set.
- **SpaceHolder** `BYTE` — Space holder for byte alignment.
- **sequence** `BYTE` — Sequence number.
- **NewIncrement** `DWORD` — What this Attribute's increment is now
- **StartingValue** `DWORD` — What this Attribute's value was on creation.
- **TotalAppliedXP** `DWORD` — How much XP has been applied directly to this attribute.

## 0243: Update Secondary Attribute

**Retired.**

Sent every time you upgrade one of the 3 secondary attributes.

- **Attribute** `WORD` — Attribute being set.
- **SpaceHolder** `BYTE` — Space holder for byte alignment.
- **sequence** `BYTE` — Sequence number.
- **PointsAdded** `DWORD` — Number of total points added to this attribute.
- **Unknown** `DWORD` — Not enough data yet.
- **TotalAppliedXP** `DWORD` — How much XP has been applied directly to this attribute.
- **NewValue** `DWORD` — The new value of this attribute.

## 0244: Vital Statistic Update

**Retired.**

Sent whenever the current value of a vital statistic changes

- **sequence** `BYTE` — Seems to be a sequence number of some sort
- **vital** `DWORD` — The Vital Statistic being updated.
- **value** `DWORD` — The new value.

## 02BB: Creature Message

A message to be displayed in the chat window, spoken by a nearby player, NPC or creature

- **text** `String` — message text
- **senderName** `String` — sender name
- **sender** `ObjectID` — sender ID
- **type** [`ChatMessageType`](enum-types.md#chatmessagetype) — message type

## 02BC: Creature Message (Ranged)

A message to be displayed in the chat window, spoken by a nearby player, NPC or creature

- **text** `String` — message text
- **senderName** `String` — sender name
- **sender** `ObjectID` — sender ID
- **range** `float` — broadcast range
- **type** [`ChatMessageType`](enum-types.md#chatmessagetype) — message type

## 02CD: Set Character DWORD

Set or update a Character DWORD property value

- **sequence** `BYTE` — sequence number
- **key** [`DWORDPropertyID`](enum-types.md#dwordpropertyid) — DWORD property ID
- **value** `DWORD` — DWORD property value

## 02CE: Set Object DWORD

Set or update an Object DWORD property value

- **sequence** `BYTE` — sequence number
- **object** `ObjectID` — object ID
- **key** [`DWORDPropertyID`](enum-types.md#dwordpropertyid) — DWORD property ID
- **value** `DWORD` — DWORD property value

## 02CF: Set Character QWORD

Set or update a Character QWORD property value

- **sequence** `BYTE` — sequence number
- **key** [`QWORDPropertyID`](enum-types.md#qwordpropertyid) — QWORD property ID
- **value** `QWORD` — QWORD property value

## 02D1: Set Character Boolean

Set or update a Character Boolean property value

- **sequence** `BYTE` — sequence number
- **key** [`BooleanPropertyID`](enum-types.md#booleanpropertyid) — Boolean property ID
- **value** `Boolean` — Boolean property value (0=False, 1=True)

## 02D2: Set Object Boolean

Set or update an Object Boolean property value

- **sequence** `BYTE` — sequence number
- **object** `ObjectID` — object ID
- **key** [`BooleanPropertyID`](enum-types.md#booleanpropertyid) — Boolean property ID
- **value** `Boolean` — Boolean property value (0=False, 1=True)

## 02D6: Set Object String

Set or update an Object String property value

- **sequence** `BYTE` — sequence number
- **key** [`StringPropertyID`](enum-types.md#stringpropertyid) — String property ID
- **object** `ObjectID` — object ID
- **value** `String` — String property value

## 02D8: Set Object Resource

Set or update an Object Resource property value

- **sequence** `BYTE` — sequence number
- **object** `ObjectID` — object ID
- **key** [`ResourcePropertyID`](enum-types.md#resourcepropertyid) — Resource property ID
- **value** `DWORD` — Resource property value

## 02D9: Set Character Link

Set or update a Character Link property value

- **sequence** `BYTE` — sequence number
- **key** [`LinkPropertyID`](enum-types.md#linkpropertyid) — Link property ID
- **value** `ObjectID` — Link property value

## 02DA: Set Object Link

Set or update an Object Link property value

- **sequence** `BYTE` — sequence number
- **object** `ObjectID` — object ID
- **key** [`LinkPropertyID`](enum-types.md#linkpropertyid) — Link property ID
- **value** `ObjectID` — Link property value

## 02DB: Set Character Position

Set or update a Character Position property value

- **sequence** `BYTE` — sequence number
- **key** [`PositionPropertyID`](enum-types.md#positionpropertyid) — Position property ID
- **value** [`Position0`](struct-types.md#position0) — Position property value

## 02DD: Set Character Skill Level

Set or update a Character Skill value

- **sequence** `BYTE` — sequence number
- **key** [`SkillID`](enum-types.md#skillid) — skill ID
- **value** [`SkillData`](struct-types.md#skilldata) — skill information

## 02E1: Set Character Skill State

Set or update a Character Skill state

- **sequence** `BYTE` — sequence number
- **key** [`SkillID`](enum-types.md#skillid) — skill ID
- **value** [`SkillState`](enum-types.md#skillstate) — skill state

## 02E3: Set Character Attribute

Set or update a Character Attribute value

- **sequence** `BYTE` — sequence number
- **key** [`AttrID`](enum-types.md#attrid) — attribute ID
- **value** [`AttributeData`](struct-types.md#attributedata) — attribute information

## 02E7: Set Character Vital

Set or update a Character Vital value

- **sequence** `BYTE` — sequence number
- **key** [`VitalID`](enum-types.md#vitalid) — vital ID
- **value** [`VitalData`](struct-types.md#vitaldata) — vital information

## 02E9: Set Character Current Vital

Set or update a Character Vital value

- **sequence** `BYTE` — sequence number
- **key** [`CurVitalID`](enum-types.md#curvitalid) — vital ID
- **value** `DWORD` — current value

## F619: Lifestone Materialize

Sent when a character rematerializes at the lifestone after death.

- **object** `ObjectID` — ObjectID of the character doing the animation
- **position** [`Position`](struct-types.md#position)
- **unknown2** `DWORD` — Unknown
- **unknown3** `DWORD` — Unknown
- **unknown4** `DWORD` — Unknown
- **unknown5** `DWORD` — Unknown

## F625: Change Model

Sent whenever a character changes their clothes. It contains the entire description of what their wearing (and possibly their facial features as well). This message is only sent for changes, when the character is first created, the body of this message is included inside the creation message.

- **object** `ObjectID` — The ID of character changing their clothing.
- **model** [`ModelData`](struct-types.md#modeldata)
- **modelSequenceType** `WORD` — Unknown stream number. Used to sequence model changes on an object.
- **modelSequence** `WORD` — Increments for every model change.

## F62C: Server Text

**Retired.**

Contains the text and then a number for the type (which I would expect just translates to a color).

- **text** `String` — The message for display.
- **color** `DWORD` — The color for displaying th message.

## F643: Char Creation Initilisation

Uncracked - Character creation screen initilised.

_No fields._

## F653: End 3D Mode

Instructs the client to return to 2D mode - the character list.

_No fields._

## F655: Char Deletion

A character was marked for delete.

_No fields._

## F657: Request Login

The character to log in.

- **character** `ObjectID` — The character ID of the character to log in
- **zonename** `String` — The account name associated with the character

## F658: Character List

The list of characters on the current account.

- **unknown1** `DWORD`
- **characterCount** `DWORD` — The number of characters in the list. Characters appear in the list ordered most-recently-used first, but are displayed alphabetically.
- **characters: vector of length characterCount**
  - **character** `ObjectID` — The character ID for this entry.
  - **name** `String` — The name of this character.
  - **deleteTimeout** `DWORD` — When 0, this character is not being deleted (not shown crossed out). Otherwise, it's a countdown timer in the number of seconds until the character is submitted for deletion.
- **unknown2** `DWORD`
- **slotCount** `DWORD` — The total count of character slots.
- **zonename** `String` — The zonename for this account.
- **turbineChatEnabled** `DWORD` — Whether or not Turbine Chat (Allegiance chat) enabled.
- **unknown3** `DWORD`

## F659: Character Login Failure

Failure to log in

- **reason** `DWORD` — 0x0d = Character still in World

## F65A: Message of the Day

**Retired.**

The message of the day during logon. There are 2 strings, one for the number of clients connected, the second is the message of the day.

- **connections** `String` — The number of connections: 'Currently xxx clients connected.
- **message** `String` — The remaining message of the day text. As far as I can tell, the 2 strings are concatenanted (with a line break) and thrown into the text box on the login screen.

## F745: Create Object

Create an object somewhere in the world

- **object** `ObjectID` — object ID
- **model** [`ModelData`](struct-types.md#modeldata)
- **physics** [`PhysicsData`](struct-types.md#physicsdata)
- **game** [`GameData`](struct-types.md#gamedata)

## F746: Login Character

- **character** `ObjectID` — ID of the character logging on - should be you.

## F747: Remove Item

Sent whenever an object is removed from the scene.

- **object** `ObjectID` — The character or monster who was recently erased.
- **unknown** `DWORD` — Unknown

## F748: Set Position and Motion

Set position - the server pathologically sends these after every actions - sometimes more than once. If has options for setting a fixed velocity or an arc for thrown weapons and arrows.

- **object** `ObjectID` — The object with the position changing.
- **position** [`Position`](struct-types.md#position) — The current or starting location.
- **logins** `WORD` — logins
- **sequence** `WORD` — A sequence number of some sort
- **portals** `WORD` — number of portals
- **adjustments** `WORD` — Adjustments to position

## F749: Wield Object

Multipurpose message. So far object wielding has been decoded. Lots of unknowns

- **owner** `ObjectID` — id of the owner of this object
- **object** `ObjectID` — id of the object
- **unknown1** `DWORD` — Unknown, always 1 in investigations
- **unknown2** `DWORD` — Unknown, always 1 in investigations
- **unknown3** `DWORD` — Unknown, Some sort of an equip counter, formula used to generate it: 0x00254 | (0x40000 + 0x30000*((ConnUser[UserParsing].EquipCount - 1) / 2))

## F74A: Move object into inventory.

- **object** `ObjectID`
- **unknown** `WORD` — unknown, was 0335 during testing
- **unknown1** `WORD` — unknown, appears to be a sequence number of some kind

## F74B: Toggle Object Visibility

Signals your client to end the portal animation for you or another char and also is fired when war spells dissapear as they hit an object blocking their path.

- **object** `ObjectID` — The character exiting portal animation or the object that just dissapeared/appeared.
- **portalType** `WORD` — Type of portal user is exiting. For begin it's 0x4410, for end it's 0x0408, figure it out for war spell objects! :)
- **unknown_1** `WORD` — Unknown word - Always 0x40 (64)
- **logins** `WORD` — Total times this user has logged in AC, seems to be something else for war spell objects
- **loginPortals** `WORD` — Number of portals user has entered during this login

## F74C: Animation

These are animations. Whenever a human, monster or object moves - one of these little messages is sent. Even idle emotes (like head scratching and nodding) are sent in this manner.

- **object** `ObjectID` — ID of the character moving
- **logins** `WORD` — Number of User Logins
- **sequence** `WORD` — Number of animations this login for the object
- **index** `WORD` — Number of animations, increased by one every time another f74c call is sent for that object
- **activity** `WORD` — 0x0 - idle, 0x1 - active
- **animation_type** [`AnimationType`](enum-types.md#animationtype) — Determines the type of animation that follows
- **type_flags** `BYTE` — 0x01 = has target (attack animation)
- **stance** [`StanceMode`](enum-types.md#stancemode) — Stance or animation.
- _Select one section based on the value of animation_type:_
  - **0x0000**:
    - **flags** `DWORD`
    - _Choose valid sections by masking against flags:_
      - **0x00000001**:
        - **stance2** [`StanceMode`](enum-types.md#stancemode)
      - **0x00000002**:
        - **animation_1** `WORD` — Animation 1, needs more investigation
      - **0x00000008**:
        - **animation_2** `WORD` — Animation 2, needs more investigation
      - **0x00000020**:
        - **animation_3** `WORD` — Animation 3, needs more investigation
      - **0x00000004**:
        - **float_1** `float` — Related to animation_1, maybe speed?
      - **0x00000010**:
        - **float_2** `float` — Related to animation_2, maybe speed?
      - **0x00000040**:
        - **float_3** `float` — Related to animation_3, maybe speed?
    - **animations: vector of length flags**
      - **animation** `WORD` — Animation ID
      - **sequence** `WORD` — The order in which this animation should be processed
      - **animation_speed** `float` — Animation Speed
  - **0x0006**:
    - **target** `ObjectID` — The object that's being moved to
    - **landblock** `DWORD` — Looks like a standard coordinate
    - **xOffset** `float` — Looks like a standard coordinate
    - **yOffset** `float` — Looks like a standard coordinate
    - **zOffset** `float` — Looks like a standard coordinate
    - **flags_2** `DWORD` — This looks like a bitfield
    - **float_1** `float` — Unknown float
    - **float_2** `float` — Unknown float
    - **unknown_3** `DWORD` — Unknown DWORD
    - **animation_speed** `float` — Looks like speed
    - **float_4** `float` — Unknown float
    - **heading** `float` — Appears to be the heading the object is turning to
    - **unknown_value** `DWORD` — ???
  - **0x0007**:
    - **landblock** `DWORD` — Looks like a standard coordinate
    - **xOffset** `float` — Looks like a standard coordinate
    - **yOffset** `float` — Looks like a standard coordinate
    - **zOffset** `float` — Looks like a standard coordinate
    - **flags_2** `DWORD` — This looks like a bitfield
    - **float_1** `float` — Unknown float
    - **float_2** `float` — Unknown float
    - **unknown_3** `DWORD` — Unknown DWORD
    - **animation_speed** `float` — Looks like speed
    - **float_4** `float` — Unknown float
    - **heading** `float` — Appears to be the heading the object is turning to
    - **unknown_value** `DWORD` — ???
  - **0x0008**:
    - **target** `ObjectID` — The object that's being faced
    - **unknown_value** `DWORD` — ???
    - **flags_2** `DWORD` — This looks like a bitfield
    - **animation_speed** `float` — Looks like speed
    - **heading** `float` — Appears to be the heading the object is turning to
  - **0x0009**:
    - **flags_2** `DWORD` — This looks like a bitfield
    - **animation_speed** `float` — Looks like speed
    - **heading** `float` — Appears to be the heading the object is turning to
- _Choose valid sections by masking against type_flags:_
  - **0x01**:
    - **targetid** `ObjectID` — The target that's being attacked
  - **0x02**:
    - _This section seems to be related to jumping, but has no data that I've seen_

## F74E: Jumping

An object has jumped

- **object** `ObjectID` — ID of the object jumping
- **unknown_1** `DWORD` — Always zero
- **heading** `float` — Direction you are jumping. Zero for stationary jump
- **height** `float` — How high the object jumped
- **unknown_3** `DWORD` — Always zero
- **unknown_4** `DWORD` — Always zero
- **unknown_5** `DWORD` — Always zero
- **logins** `WORD` — Number of times you've logged into AC
- **sequence** `WORD` — Number of times you've jumped since logging into AC

## F750: Apply Sound Effect

Applies a sound effect.

- **object** `ObjectID` — ID of the object from which the effect originates. Can be you, another char/npc or an item.
- **effect** [`Effect`](enum-types.md#effect) — The particle effect ID.
- **parameter** `float` — Some sort of parameter to the effect, possibly speed or color.

## F751: Enter Portal Mode

Instructs the client to show the portal graphic.

- **sequence** `DWORD` — Increases by 1 or 2 each time you portal

## F755: Apply Visual/Sound Effect

Applies an effect with visual and sound.

- **object** `ObjectID` — ID of the object from which the effect originates. Can be you, another char/npc or an item.
- **effect** [`Effect`](enum-types.md#effect) — The particle effect ID.
- **speed** `float` — Speed to play the particle effect at. 1.0 is default, lower for slower, higher for faster.

## F7B0: Game Event

Game Events are messages that are sequenced.

Common header fields:

- **character** `ObjectID` — the object ID of the message recipient (should be you)
- **sequence** `DWORD` — sequence number
- **event** [`GameEvent`](enum-types.md#gameevent) — the (sequenced) message type

The message body is one of many sub-messages selected by the `event` field. See **[game-events.md](game-events.md)** for the full list.

## F7B1: Game Action

Game Actions are outgoing messages that are sequenced.

Common header fields:

- **sequence** `DWORD` — message sequence number
- **action** [`GameAction`](enum-types.md#gameaction) — sequenced message type

The message body is one of many sub-messages selected by the `action` field. See **[game-actions.md](game-actions.md)** for the full list.

## F7C7: Start 3D Mode

**Retired.**

Sent to instruct the client to login. Client changes video mode and enter's portal mode.

_No fields._

## F7C8: Enter Game

The user has clicked 'Enter'. This message does not contain the ID of the character logging on; that comes later.

_No fields._

## F7DB: Update Object

Update an existing object's data.

- **object** `ObjectID` — the object being updated
- **model** [`ModelData`](struct-types.md#modeldata) — updated model data
- **physics** [`PhysicsData`](struct-types.md#physicsdata) — updated physics data
- **game** [`GameData`](struct-types.md#gamedata) — updated game data

## F7DE: Turbine Chat

Send or receive a message using Turbine Chat.

- **size** `DWORD` — the number of bytes that follow after this DWORD
- **type** [`TurbineChatType`](enum-types.md#turbinechattype) — the type of data contained in this message
- **unknown1** `DWORD`
- **unknown2** `DWORD`
- **unknown3** `DWORD`
- **unknown4** `DWORD`
- **unknown5** `DWORD`
- **unknown6** `DWORD`
- **payload** `DWORD` — the number of bytes that follow after this DWORD
- _Select one section based on the value of type:_
  - **0x01**:
    - _inbound message_
    - **channel** `DWORD` — the channel number of the message
    - **senderName** `WString` — the name of the player sending the message
    - **text** `WString` — the message text
    - **unknown01_1** `DWORD`
    - **sender** `ObjectID` — the object ID of the player sending the message
    - **unknown01_2** `DWORD`
    - **unknown01_3** `DWORD`
  - **0x03**:
    - _outbound message_
    - **unknown03_1** `DWORD`
    - **unknown03_2** `DWORD`
    - **unknown03_3** `DWORD`
    - **outChannel** `DWORD` — the channel number of the message
    - **outText** `WString` — the message text
    - **unknown03_4** `DWORD`
    - **outSender** `ObjectID` — the object ID of the player sending the message (should be you)
    - **unknown03_5** `DWORD`
    - **unknown03_6** `DWORD`
  - **0x05**:
    - _inbound acknowledgement of outbound message_
    - **unknown05_1** `DWORD`
    - **unknown05_2** `DWORD`
    - **unknown05_3** `DWORD`
    - **unknown05_4** `DWORD`

## F7DF: Start 3D Mode

Switch from the character display to the game display.

_No fields._

## F7E0: Server Message

Display a message in the chat window.

- **text** `String` — the message text
- **type** [`ChatMessageType`](enum-types.md#chatmessagetype) — the message type, controls color and @filter processing

## F7E1: Server Name

The name of the current world.

- **players** `DWORD` — the number of players connected
- **unknown** `DWORD` — unknown
- **server** `String` — the name of the current world

## F7E2: Update Resource

Add or update a dat file Resource.

- **unknown1** `DWORD` — unknown
- **type** [`ResourceType`](enum-types.md#resourcetype) — which dat file should store this resource
- **unknown3** `DWORD` — unknown
- **resource** `ResourceID` — the resource ID number
- **version** `DWORD` — the file version number
- **compression** [`CompressionType`](enum-types.md#compressiontype) — the type of compression used
- **unknown4** `DWORD` — unknown
- **dataSize** `DWORD` — the number of bytes required for the remainder of this message, including this DWORD
- _Select one section based on the value of compression:_
  - **0x00**:
    - **data: vector of length dataSize**
      - **byte** `BYTE` — (dataSize-4) bytes of uncompressed file data
  - **0x01**:
    - **fileSize** `DWORD` — the size of the uncompressed file
    - **data: vector of length dataSize**
      - **byte** `BYTE` — (dataSize-8) bytes of zlib compressed file data

## F7E7: Dat File Patch List

A list of dat files that need to be patched

- **size** `DWORD` — Total size of all revisions
- **revisionCount** `DWORD` — Total number of revisions to follow
- **revisions: vector of length revisionCount**
  - **header_0150** `DWORD` — Dat File header offset 0x0150
  - **header_014C** `DWORD` — Dat File header offset 0x014C
  - **revision** `DWORD` — The corresponding Dat file revision for this patch set
  - **portalCount** `DWORD` — Number of portal resources in this revision
  - **resource** `ResourceID` — the resource ID number
  - **cellCount** `DWORD` — Number of cell resources in this revision
  - **resource** `ResourceID` — the resource ID number
