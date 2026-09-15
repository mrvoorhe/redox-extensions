# Game Actions (`F7B1`)

> Generated from the DecalDev AC protocol reference at <https://skunkworks.sourceforge.net/protocol/Protocol.php> (version 2006.08.22.1).

Game Actions are sequenced messages sent from the client to the server. Each action shares the common header below, followed by an action-specific body selected by the `action` field.

## Common header

- **sequence** `DWORD` — message sequence number
- **action** [`GameAction`](enum-types.md#gameaction) — sequenced message type

## 0x0005: Set Single Character Option

Set a single character option.

- **key** [`OptionPropertyID`](enum-types.md#optionpropertyid) — the option being set
- **value** `Boolean` — the value of the option

## 0x0010: Set AFK Message

Set AFK message.

- **text** `String` — The message text

## 0x0019: Store Item

Store an item in a container.

- **item** `ObjectID` — The item being stored
- **container** `ObjectID` — The container the item is being stored in
- **slot** `DWORD` — The position in the container where the item is being placed

## 0x001A: Equip Item

Equip an item.

- **item** `ObjectID` — The item being equipped
- **slot** [`EquipMask`](enum-types.md#equipmask) — The position in the container where the item is being placed

## 0x001B: Drop Item

Drop an item.

- **object** `ObjectID` — The item being dropped

## 0x0036: Use Item

Attempt to use an item.

- **object** `ObjectID` — The item being used

## 0x0044: Raise Vital

Spend XP to raise a vital.

- **vital** `DWORD` — The ID of the vital
- **xp** `DWORD` — The amount of XP being spent

## 0x0045: Raise Attribute

Spend XP to raise an attribute.

- **attr** `DWORD` — The ID of the attribute
- **xp** `DWORD` — The amount of XP being spent

## 0x0046: Raise Skill

Spend XP to raise a skill.

- **skill** `DWORD` — The ID of the skill
- **xp** `DWORD` — The amount of XP being spent

## 0x0047: Train Skill

Spend skill credits to train a skill.

- **skill** `DWORD` — The ID of the skill
- **credits** `DWORD` — The number of skill credits being spent

## 0x0048: Cast Spell

Cast a spell.

- **spell** `DWORD` — The ID of the spell

## 0x004A: Cast Spell on Object

Cast a spell.

- **target** `ObjectID` — The target of the spell
- **spell** `DWORD` — The ID of the spell

## 0x00A1: Materialize

The client is ready for the character to materialize after portalling or logging on.

_No fields._

## 0x00CD: Give Item

Give an item to someone.

- **target** `ObjectID` — The recipient of the item
- **item** `ObjectID` — The item being given
- **unknown** `DWORD`

## 0x019C: Make Shortcut

Add an item to the shortcut bar.

- **position** `DWORD` — Position on the shortcut bar (0-8) where the item is to be added
- **target** `ObjectID` — Object ID
- **unknown3** `DWORD`

## 0x019D: Remove Shortcut

Remove an item from the shortcut bar.

- **position** `DWORD` — Position on the shortcut bar (0-8) of the item to be removed

## 0x01A1: Set Character Options

Set multiple character options.

- **options** [`CharacterOptionData`](struct-types.md#characteroptiondata)

## 0x01E3: Add Spell to Spellbar

Add a spell to a spell bar.

- **spell** `DWORD` — The spell's ID
- **position** `DWORD` — Position on the spell bar where the spell is to be added
- **spellbar** `DWORD` — The spell bar number

## 0x01E4: Remove Spell from Spellbar

Remove a spell from a spell bar.

- **spell** `DWORD` — The spell's ID
- **spellbar** `DWORD` — The spell bar number
