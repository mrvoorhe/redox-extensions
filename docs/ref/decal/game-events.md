# Game Events (`F7B0`)

> Generated from the DecalDev AC protocol reference at <https://skunkworks.sourceforge.net/protocol/Protocol.php> (version 2006.08.22.1).

Game Events are sequenced messages sent from the server to a specific character. Each event shares the common header below, followed by an event-specific body selected by the `event` field.

## Common header

- **character** `ObjectID` — the object ID of the message recipient (should be you)
- **sequence** `DWORD` — sequence number
- **event** [`GameEvent`](enum-types.md#gameevent) — the (sequenced) message type

## 0x0004: Message Box

Display a message in a popup message window.

- **text** `String` — the message text

## 0x0013: Login Character

Information describing your character.

- **properties** [`CharacterPropertyData`](struct-types.md#characterpropertydata)
- **vectors** [`CharacterVectorData`](struct-types.md#charactervectordata)
- **options** [`CharacterOptionData`](struct-types.md#characteroptiondata)
- **inventoryCount** `DWORD` — Number of items in your main pack.
- **inventory: vector of length inventoryCount**
  - **item** `ObjectID`
  - **type** `DWORD` — Whether or not this object is a container. 1=unlocked container, 2=foci
- **equippedCount** `DWORD` — Number of items currently equipped.
- **equipped: vector of length equippedCount**
  - **item** `ObjectID`
  - **slot** [`EquipMask`](enum-types.md#equipmask)
  - **unknown3** `DWORD`

## 0x0016: Transaction Message

**Retired.**

Trade text/Chat Partner not Available

- **text** `String` — Text detailing parts of the trade, or that trades aren't being accepted - Also is used to tell you that somebody isn't available for chat (direct/fellow/monarch/patron/vassal etc).

## 0x0020: Allegiance Info

Returns info related to your monarch, patron and vassals.

- **unknown0** `DWORD`
- **allegianceSize** `DWORD` — The number of allegiance members.
- **followers** `DWORD` — Your personal number of followers.
- **recordCount** `WORD` — Number of character allegiance records.
- **unknown1** `WORD` — 0030
- **unknown2** `DWORD` — unknown
- **unknown2a** `DWORD` — 00000000
- **unknown3** `DWORD` — 00000000
- **unknown4** `DWORD` — 00000000
- **unknown5** `DWORD` — 00000000
- **unknown6** `DWORD` — 00000000
- **unknown7** `DWORD` — 00000000
- **unknown8** `DWORD` — 00000000
- **unknown9** `DWORD` — allegiance chat channel number
- **unknown10** `DWORD` — 00000000
- **unknown11** `DWORD` — 00000000
- **unknown12** `DWORD` — 00000000
- **unknown13** `DWORD` — 00000000
- **unknown14** `float` — 1.0
- **unknown15** `DWORD` — 00000000
- **unknown16** `DWORD` — 00000000
- **unknown16a** `DWORD` — 00000000
- **allegianceName** `String` — The name of the allegiance.
- **unknown16c** `DWORD` — unknown
- **unknown16d** `DWORD` — 00000000
- _Select one section based on the value of recordCount:_
  - **0x0000**:
    - **unknown17** `DWORD`
- **records: vector of length recordCount**
  - **treeParent** `ObjectID` — The Object ID for the parent character to this character. Used by the client to decide how to build the display in the Allegiance tab. 1 is the monarch.
  - **character** `ObjectID` — Character ID
  - **pendingXP** `DWORD` — XP gained while logged off
  - **exp** `QWORD` — Total allegiance XP contribution.
  - **gender** `BYTE` — The gender of the character (for determining title).
  - **race** `BYTE` — The heritage of the character (for determining title).
  - **rank** `BYTE` — The numerical rank (1 is lowest).
  - **online** `Boolean` — online: 0=no, 1=yes
  - **loyalty** `WORD` — Character loyalty.
  - **leadership** `WORD` — Character leadership.
  - **unknown** `double`
  - **name** `String`

## 0x0021: Friends List Update

Friends list update

- **friendsCount** `DWORD` — The number of friends in the list
- **friends: vector of length friendsCount**
  - **friend** `ObjectID` — Friend's ID
  - **online** `Boolean` — Whether this friend is online
  - **unknown1** `DWORD` — unknown1, seems to be 0
  - **name** `String` — Name of the friend
  - **outFriendsCount** `DWORD` — The number of people on this player's friends list
  - **outFriend** `ObjectID` — The ID of a character on this player's friends list
  - **inFriendsCount** `DWORD` — The number of people who have this player on their friends list
  - **inFriend** `ObjectID` — The ID of a character who has this player on their friends list
- **type** [`FriendsUpdateType`](enum-types.md#friendsupdatetype) — The type of the update

## 0x0022: Insert Inventory Item

Store an item in a container.

- **item** `ObjectID` — the object ID of the item being stored
- **container** `ObjectID` — the object ID of the container the item is being stored in
- **slot** `DWORD` — the item slot within the container where the item is being placed (0-based)
- **type** [`ItemType`](enum-types.md#itemtype) — the type of item being stored (pack, foci or regular item)

## 0x0023: Wear Item

Equip an item.

- **item** `ObjectID` — the object ID of the item being equipped
- **slot** [`EquipMask`](enum-types.md#equipmask) — the slot(s) the item uses

## 0x0029: Title List

Titles for the current character.

- **unknown** `DWORD`
- **current** `DWORD` — the title ID of the currently active title
- **count** `DWORD` — the number of available titles
- **titles: vector of length count**
  - **title** `DWORD` — the title ID of an available title

## 0x002b: Set Title

Set a title for the current character.

- **title** `DWORD` — the title ID of the new title
- **active** `Boolean` — true if the title should be made the current title, false if it should just be added to the title list

## 0x0038: Direct Chat

**Retired.**

Received when someone sends you an @tell.

- **text** `String` — The message text.
- **sourceName** `String` — The name of the person sending you the message.
- **source** `ObjectID` — The character ID for the person sending you the message.
- **destination** `ObjectID` — The person receiving the message - which should always be you.
- **color** `DWORD` — The color of the message - should always be 0x04 for normal tells, but may change if a VIP is talking to you.

## 0x004C: Add Spell to Spellbook / Cast Spell

**Retired.**

Adds a spell to the spellbook. Also indicates you have cast a spell - a separate message contains the details on the spell.

- **spell** `SpellID` — The ID of the spell

## 0x004D: Delete Spell from Spellbook

**Retired.**

Deletes a spell from the spellbook.

- **spell** `SpellID` — The ID of the spell

## 0x004E: Add Enchantment

**Retired.**

Contains the details for spells cast of your character.

- **enchantment** [`Enchantment`](struct-types.md#enchantment) — enchantment info

## 0x004F: Remove Enchantment

**Retired.**

An active enchantment has expired. Sent to update the client list and display a message.

- **spell** `SpellID` — The spell copy that's begin removed.
- **layer** `WORD` — The index of this exact spell on the on the player/item. It will increment one each time the spell is cast if there is one already on the player/item.

## 0x0052: Close Container

Close Container - Only sent when explicitly closed

- **object** `ObjectID` — Chest or corpse being closed

## 0x0062: Approach Vendor

Open the buy/sell panel for a merchant.

- **merchant** `ObjectID` — the object ID of the merchant
- **buyCategories** `DWORD` — the categories of items the merchant will buy
- **unknown1** `DWORD`
- **buyValue** `DWORD` — the highest value of an item the merchant will buy
- **unknown2** `DWORD`
- **buyRate** `float` — the merchant's buy rate
- **sellRate** `float` — the merchant's sell rate
- **itemCount** `DWORD` — the number of items the merchant is selling
- **items: vector of length itemCount**
  - **count** `WORD` — the number of items for sale (-1 for an unlimited supply)
  - **flags** `WORD`
  - **object** `ObjectID` — the object ID of the item
  - **game** [`GameData`](struct-types.md#gamedata) — details about the item

## 0x009C: End Portal Storm

**Retired.**

End Portal Storm - 'The Portal Storm has subsided' message

_No fields._

## 0x009D: Mild Portal Storm

**Retired.**

Portal Storm warning - Level 1 of 3

- **severity** `float` — Could be severity - was 0.40 during testing

## 0x009E: Heavy Portal Storm

**Retired.**

Portal Storm warning - Level 2 of 3

- **severity** `float` — Could be severity - was 0.60 during testing

## 0x009F: Portal Stormed

**Retired.**

Character was portal stormed

- **desc** `String` — Portal storm description

## 0x00A0: Failure to Give Item

Failure to give an item

- **item** `ObjectID` — Item that could not be given
- **reason** `DWORD` — Unknown, was 0x3EF when I tested (Zyrca: appears to be a failure reason ID)

## 0x00A3: Fellowship Member Quit

Member left fellowship

- **fellow** `ObjectID` — Person who quit the fellowship

## 0x00A4: Fellowship Member Dismissed

Member dismissed from fellowship

- **fellow** `ObjectID` — Person who was dismissed from the fellowship

## 0x00A7: Quit Fellowship

**Retired.**

Fellowship quit

- **fellow** `ObjectID` — Person who quit the fellowship

## 0x00AF: Create Fellowship

**Retired.**

Create new fellowship

- **fellowCount** `WORD` — Number of current members in Fellowship
- **unknown2** `WORD` — unknown2, seems to be 16 (0x0010) always, could be a 'set SharePhatLoot mask' entry
- **fellows: vector of length fellowCount**
  - **fellow** `ObjectID` — Fellowship member
  - **unknown1** `WORD` — unknown1, seems to be 0
  - **level** `WORD` — level of member
  - **maxHealth** `DWORD` — Maximum Health
  - **maxStam** `DWORD` — Maximum Stamina
  - **maxMana** `DWORD` — Maximum Mana
  - **curHealth** `DWORD` — Current Health
  - **curStam** `DWORD` — Current Stamina
  - **curMana** `DWORD` — Current Mana
  - **shareLoot** `DWORD` — if 0 then noSharePhatLoot, if 16 (0x0010) then sharePhatLoot
  - **name** `String` — Name of Member
- **fellowship** `String` — Fellowship Name
- **leader** `ObjectID` — Fellowship Leader
- **shareExp1** `DWORD` — Seems to match ShareExp
- **shareExp** `DWORD` — if value==0 then share==0, if value==1 then share==percentage based on level and numberOfFellows
- **openfellow** `DWORD` — Open fellowship
- **unknown3** `DWORD` — unknown

## 0x00B0: Recruit Member

**Retired.**

Fellowship recruit member

- **fellow** `ObjectID` — New fellowship member
- **unknown1** `WORD` — unknown1
- **level** `WORD` — Level of recruited member
- **maxHealth** `DWORD` — Maximum Health
- **maxStam** `DWORD` — Maximum Stamina
- **maxMana** `DWORD` — Maximum Mana
- **curHealth** `DWORD` — Current Health
- **curStam** `DWORD` — Current Stamina
- **curMana** `DWORD` — Current Mana
- **shareLoot** `DWORD` — if 0 then noSharePhatLoot, if 16 (0x0010) then sharePhatLoot
- **name** `String` — Name of Member
- **unknown** `DWORD` — ShareXPS?

## 0x00B1: Dismiss Member

**Retired.**

Fellowship dismissal

- **fellow** `ObjectID` — Person who was dismissed from the fellowship

## 0x00B3: Disband Fellowship

**Retired.**

Fellowship was disbanded

_No fields._

## 0x00B4: Read Table of Contents

Sent when you first open a book, contains the entire table of contents.

- **object** `ObjectID` — The readable object you have just opened.
- **totalPages** `DWORD` — The total number of pages in the book.
- **contentsPages** `DWORD` — The number of pages that appear in the contents.
- **unknown1** `DWORD` — Unknown - Always 0x3E8, may be some sort of media type.
- **usedPages** `DWORD` — The number of used pages, and the number of content records.
- **pages: vector of length usedPages**
  - **author** `ObjectID` — The character ID of the author. For preauthored books, this value is 0xFFFFFFFF.
  - **authorName** `String` — The character name of the author. Preauthored books often use this as a table of contents instead.
  - **zoneAccount** `String` — The zone account name of the author (expect this to be remove in the near future - Cibo, October 20, 2000). For prewritten books, this is always 'prewritten'.
  - **unknown1** `DWORD` — Unknown - Always 0
- **comment** `String` — The inscription comment and the book title.
- **commentAuthor** `ObjectID` — The author of the inscription (and not coincidentally, the book title).
- **commentAuthorName** `String` — The name of the inscription author.

## 0x00B8: Read Page

Contains the text of a single page of a book, parchment or sign.

- **object** `ObjectID` — The object id for the readable object.
- **page** `DWORD` — The 0-based index of the page you are currently viewing.
- **author** `ObjectID` — The character ID of the author. For preauthored books, this value is 0xFFFFFFFF.
- **authorName** `String` — The character name of the author. Preauthored books often use this as a table of contents instead.
- **zoneAccount** `String` — The zone account name of the author (expect this to be remove in the near future - Cibo, October 20, 2000). For prewritten books, this is always 'prewritten'.
- **unknown3** `DWORD` — unknown
- **unknown1** `DWORD` — Unknown - Always 1
- **unknown2** `DWORD` — unknown
- **text** `String` — The text of the page.

## 0x00C9: Identify Object

The result of an attempt to assess an item or creature.

- **object** `ObjectID` — the object ID of the item or creature being assessed
- **flags** `DWORD`
- **success** `Boolean` — assessment successful: 0=no, 1=yes
- _Choose valid sections by masking against flags:_
  - **0x00000001**:
    - **dwordCount** `WORD`
    - **dwordUnknown** `WORD`
    - **dwords: vector of length dwordCount**
      - **key** [`DWORDPropertyID`](enum-types.md#dwordpropertyid)
      - **value** `DWORD`
  - **0x00002000**:
    - **qwordCount** `WORD`
    - **qwordUnknown** `WORD`
    - **qwords: vector of length qwordCount**
      - **key** [`QWORDPropertyID`](enum-types.md#qwordpropertyid)
      - **value** `QWORD`
  - **0x00000002**:
    - **booleanCount** `WORD`
    - **booleanUnknown** `WORD`
    - **booleans: vector of length booleanCount**
      - **key** [`BooleanPropertyID`](enum-types.md#booleanpropertyid)
      - **value** `Boolean` — Boolean property value (0=False, 1=True)
  - **0x00000004**:
    - **doubleCount** `WORD`
    - **doubleUnknown** `WORD`
    - **doubles: vector of length doubleCount**
      - **key** [`DoublePropertyID`](enum-types.md#doublepropertyid)
      - **value** `double`
  - **0x00000008**:
    - **stringCount** `WORD`
    - **stringUnknown** `WORD`
    - **strings: vector of length stringCount**
      - **key** [`StringPropertyID`](enum-types.md#stringpropertyid)
      - **value** `String`
  - **0x00001000**:
    - **resourceCount** `WORD`
    - **resourceUnknown** `WORD`
    - **resources: vector of length resourceCount**
      - **key** [`ResourcePropertyID`](enum-types.md#resourcepropertyid)
      - **value** `ResourceID`
  - **0x00000010**:
    - **spellCount** `WORD`
    - **spellUnknown** `WORD`
    - **spells: vector of length spellCount**
      - **spell** `SpellID`
      - **flags** `WORD`
  - **0x00000080**:
    - **protSlashing** `float` — relative protection against slashing damage (multiply by AL for actual protection)
    - **protPiercing** `float` — relative protection against piercing damage (multiply by AL for actual protection)
    - **protBludgeoning** `float` — relative protection against bludgeoning damage (multiply by AL for actual protection)
    - **protCold** `float` — relative protection against cold damage (multiply by AL for actual protection)
    - **protFire** `float` — relative protection against fire damage (multiply by AL for actual protection)
    - **protAcid** `float` — relative protection against acid damage (multiply by AL for actual protection)
    - **protLightning** `float` — relative protection against lightning damage (multiply by AL for actual protection)
  - **0x00000100**:
    - **flags1** `DWORD` — These Flags indication which fields will be available for assess.
    - **health** `DWORD` — current health
    - **healthMax** `DWORD` — maximum health
    - _Choose valid sections by masking against flags1:_
      - **0x00000008**:
        - **strength** `DWORD` — strength
        - **endurance** `DWORD` — endurance
        - **quickness** `DWORD` — quickness
        - **coordination** `DWORD` — coordination
        - **focus** `DWORD` — focus
        - **self** `DWORD` — self
        - **stamina** `DWORD` — current stamina
        - **mana** `DWORD` — current mana
        - **staminaMax** `DWORD` — maximum stamina
        - **manaMax** `DWORD` — maximum mana
      - **0x00000001**:
        - **attrHighlight** [`AttributeHighlightMask`](enum-types.md#attributehighlightmask) — highlight enable bitmask: 0=no, 1=yes
        - **attrColor** [`AttributeHighlightMask`](enum-types.md#attributehighlightmask) — highlight color bitmask: 0=red, 1=green
  - **0x00000020**:
    - **weapType** [`DamageType`](enum-types.md#damagetype) — the type of damage done by the weapon
    - **weapSpeed** `DWORD` — the speed of the weapon
    - **weapSkill** [`SkillID`](enum-types.md#skillid) — the skill used by the weapon (-1 if none)
    - **weapDamage** `DWORD` — the maximum damage done by the weapon
    - **weapVariance** `double` — the maximum damage variance of the weapon
    - **weapModifier** `double` — the damage modifier of the weapon
    - **weapUnknown1** `double`
    - **weapPower** `double` — the power of the weapon (this affects range)
    - **weapAttack** `double` — the attack skill bonus of the weapon
    - **weapUnknown3** `DWORD`
  - **0x00000040**:
    - **unknown40_1** `DWORD`
    - **unknown40_2** `DWORD`
    - **unknown40_3** `DWORD`
  - **0x00000200**:
    - **protHighlight** [`ArmorHighlightMask`](enum-types.md#armorhighlightmask) — highlight enable bitmask: 0=no, 1=yes
    - **protColor** [`ArmorHighlightMask`](enum-types.md#armorhighlightmask) — highlight color bitmask: 0=red, 1=green
  - **0x00000800**:
    - **weapHighlight** [`WeaponHighlightMask`](enum-types.md#weaponhighlightmask) — highlight enable bitmask: 0=no, 1=yes
    - **weapColor** [`WeaponHighlightMask`](enum-types.md#weaponhighlightmask) — highlight color bitmask: 0=red, 1=green
  - **0x00000400**:
    - **wandHighlight** [`WandHighlightMask`](enum-types.md#wandhighlightmask) — highlight enable bitmask: 0=no, 1=yes
    - **wandColor** [`WandHighlightMask`](enum-types.md#wandhighlightmask) — highlight color bitmask: 0=red, 1=green

## 0x0147: Group Chat

Group Chat

- **type** [`GroupChatType`](enum-types.md#groupchattype) — the message type
- **senderName** `String` — the name of the player sending the message
- **text** `String` — the message text.

## 0x014A: Group Chat

**Retired.**

Allegiance and Fellowship chats come on these messages.

- **group** `DWORD` — The type of group chat.
- **senderName** `String` — The name of the character sending you the message.
- **text** `String` — The text of the message.

## 0x0196: Set Pack Contents

Set Pack Contents

- **container** `ObjectID` — The pack we are setting the contents of. This pack objects and the contained objects may be created before or after the message.
- **itemCount** `DWORD` — Number of items in the pack (does not reflect the capacity - see the pack creation message).
- **items: vector of length itemCount**
  - **item** `ObjectID` — An item for the pack.
  - **type** [`ItemType`](enum-types.md#itemtype) — The type of this inventory. In this message it will always be 0 since there are no subpacks currently.

## 0x019A: Drop from Inventory

Removes an item from inventory (when you place it on the ground or give it away)

- **item** `ObjectID` — The item leaving your inventory.

## 0x01A4: Remove Enchantment (Silent)

**Retired.**

An enchantment was removed (via magic item).

- **spell** `SpellID` — The ID of the spell being removed.
- **layers** `WORD` — Index of this spell (for layers).

## 0x01A6: Remove Multiple Enchantments

**Retired.**

Removes a bunch of enchantments to the list (when multiple spells expire simultaneously).

- **count** `DWORD` — The number of enchantments
- **enchantments: vector of length count**
  - **spell** `SpellID` — The spell being removed.
  - **layer** `WORD` — Index of the spell being removed, if this index is 1 the enchantment is entirely removed.

## 0x01A7: Attack Completed

Melee attack completed

- **number** `DWORD` — Number of user attacks

## 0x01A8: Delete Spell from Spellbook

Delete a spell from your spellbook.

- **spell** `SpellID` — The spell being deleted
- **unknown** `WORD`

## 0x01AC: Your death.

You just died.

- **text** `String` — Your (typically mocking) death message.

## 0x01AD: Kill/Death Message

Message for a death, something you killed or your own death message.

- **text** `String` — The text of the nearby or present death message.

## 0x01AE: Remove Multiple Enchantments

**Retired.**

Remove a bunch of enchantments in a single shot.

- **count** `DWORD` — The number of enchantments
- **enchantments: vector of length count**
  - **spell** `SpellID` — The spell being removed.
  - **layer** `WORD` — The layer being removed.

## 0x01B1: Inflict Melee Damage

You have hit your target with a melee attack.

- **target** `String` — the name of your target
- **type** [`DamageType`](enum-types.md#damagetype) — the type of damage done
- **severity** `double` — the severity of the attack, scaled from 0.0 (low) to 1.0 (high)
- **damage** `DWORD` — the amount of damage done
- **critical** `Boolean` — critical hit: 0=no, 1=yes
- **unknown** `DWORD`

## 0x01B2: Receive Melee Damage

You have been hit by a creature's melee attack.

- **attacker** `String` — the name of the creature
- **type** [`DamageType`](enum-types.md#damagetype) — the type of damage done
- **severity** `double` — the severity of the attack, scaled from 0.0 (low) to 1.0 (high)
- **damage** `DWORD` — the amount of damage done
- **location** [`DamageLocation`](enum-types.md#damagelocation) — the location of the damage done
- **critical** `Boolean` — critical hit: 0=no, 1=yes
- **unknown** `DWORD`

## 0x01B3: Other Melee Evade

Your target has evaded your melee attack.

- **target** `String` — the name of your target

## 0x01B4: Self Melee Evade

You have evaded a creature's melee attack.

- **attacker** `String` — the name of the creature

## 0x01B8: Start Melee Attack

Start melee attack

_No fields._

## 0x01C0: Update Health

Update a creature's health bar.

- **object** `ObjectID` — the object ID of the creature
- **health** `float` — the amount of health remaining, scaled from 0.0 (none) to 1.0 (full)

## 0x01C3: Age Command Result

Age Command Result - happens when you do /age in the game

- **unknown** `String` — Unknown - always seems to be a null string
- **age** `String` — Your age in the format 1mo 1d 1h 1m 1s

## 0x01C7: Ready. Previous action complete

Ready. Previous action complete

- **unknown** `DWORD`

## 0x01C8: Update Allegiance Info

Update Allegiance info, sent when allegiance panel is open

- **unknown** `DWORD`

## 0x01CB: Close Assess Panel

Close Assess Panel

- **unknown** `DWORD`

## 0x01EA: Ping Reply

Ping Reply

_No fields._

## 0x01F4: Squelched Users List

Squelch and Filter List

- **unknown1** `DWORD` — Unknown Always 0
- **squelchCount** `WORD` — The number of squelched users.
- **squelchUnknown** `WORD` — Unknown flags, sometimes 0x0020 is set
- **users: vector of length squelchCount**
  - **user** `ObjectID` — the object ID of the squelched player.
  - **squelchMaskCount** `DWORD` — The number of squelch masks (1=specific types, 4=all types)
  - **squelchMask** [`ChatFilterMask`](enum-types.md#chatfiltermask) — One of the squelch masks
  - **name** `String` — the name of the squelched player
  - **account** `Boolean` — Whether this squelch applies to the entire account
- **filterMaskCount** `DWORD` — The number of global filter masks (1=specific types, 4=all types)
- **filterMasks: vector of length filterMaskCount**
  - **filterMask** [`ChatFilterMask`](enum-types.md#chatfiltermask) — One of the global filter masks
- **unknown2** `DWORD`
- **unknown3** `DWORD`

## 0x01FD: Enter Trade

Send to begin a trade and display the trade window

- **trader** `ObjectID` — Person initiating the trade
- **tradee** `ObjectID` — Person receiving the trade
- **unknown1** `DWORD` — unknown
- **unknown2** `DWORD` — unknown

## 0x01FF: End Trade

End trading

- **reason** [`EndTradeReason`](enum-types.md#endtradereason) — Reason trade was cancelled

## 0x0200: Add Trade Item

Item was added to trade window - you will receive a Create Object (0xF745) with details of the item

- **item** `ObjectID` — The item being dropped into trade window
- **side** `DWORD` — Side of the trade window object was inserted (1 or 2)
- **unknown1** `DWORD` — unknown, was zero when testing

## 0x0202: Accept Trade

The trade was accepted

- **trader** `ObjectID` — Person who accepted the trade

## 0x0203: Un-Accept Trade

The trade was un-accepted

- **trader** `ObjectID` — Person who un-accepted the trade

## 0x0205: Reset Trade

The trade window was reset

- **trader** `ObjectID` — Person who cleared the window

## 0x0207: Failure to add a trade item

Failure to add a trade item

- **item** `ObjectID` — Item that could not be added to trade window
- **reason** `DWORD` — The numeric reason it couldn't be traded.

## 0x0208: Failure to complete a trade

Failure to complete a trade

_No fields._

## 0x021D: Display Dwelling Purchase/Maintenance Panel

Buy a dwelling or pay maintenance

- **object** `ObjectID` — the object ID of the dwelling's covenant crystal
- **dwellingID** `DWORD` — the number associated with this dwelling
- **owner** `ObjectID` — the object ID of the the current owner
- **unknown1** `DWORD`
- **levelReq** `DWORD` — the level requirement to purchase this dwelling (-1 if no requirement)
- **unknown2** `DWORD`
- **rankReq** `DWORD` — the rank requirement to purchase this dwelling (-1 if no requirement)
- **unknown3** `DWORD`
- **unknown4** `DWORD`
- **dwellingType** `DWORD` — the type of dwelling (1=cottage, 2=villa, 3=mansion, 4=apartment)
- **ownerName** `String` — the name of the current owner
- **purchaseCount** `DWORD` — the number of items required to purchase this dwelling
- **purchaseItems: vector of length purchaseCount**
  - **item** [`DwellingItem`](struct-types.md#dwellingitem)
- **maintenanceCount** `DWORD` — the number of items required to pay the maintenance cost for this dwelling
- **maintenanceItems: vector of length maintenanceCount**
  - **item** [`DwellingItem`](struct-types.md#dwellingitem)

## 0x0225: House Information for Owners

House panel information for owners.

- **purchaseDate** `DWORD` — when the dwelling was purchased (Unix timestamp)
- **maintenanceDate** `DWORD` — when the current maintenance period begain (Unix timestamp)
- **dwellingType** `DWORD` — the type of dwelling (1=cottage, 2=villa, 3=mansion, 4=apartment)
- **unknown1** `DWORD`
- **purchaseCount** `DWORD` — the number of items required to purchase this dwelling
- **purchaseItems: vector of length purchaseCount**
  - **item** [`DwellingItem`](struct-types.md#dwellingitem)
- **maintenanceCount** `DWORD` — the number of items required to pay the maintenance cost for this dwelling
- **maintenanceItems: vector of length maintenanceCount**
  - **item** [`DwellingItem`](struct-types.md#dwellingitem)
- **position** [`Position0`](struct-types.md#position0) — object position

## 0x0226: House Information for Non-Owners

House panel information for non-owners.

_No fields._

## 0x0257: House Guest List

House Guest List, Sent in response to asking for one.

- **unknown** `DWORD` — so far always 0x10000001
- **public** `DWORD` — 0 is private house, 1 = open to public
- **monarch** `ObjectID` — populated when any allegiance access is specified
- **guestCount** `WORD` — number of guests on list
- **guestLimit** `WORD` — Maximum number of guests on guest list (cottage is 32)
- **guestList: vector of length guestCount**
  - **guest** `ObjectID` — ID of the guest
  - **access** `DWORD` — 0 is just access to house, 1 = access to storage
  - **guestName** `String` — Name of the guest

## 0x0264: Update Item Mana Bar

Update an item's mana bar.

- **item** `ObjectID` — the object ID of the item
- **mana** `float` — the amount of mana remaining, scaled from 0.0 (none) to 1.0 (full)
- **show** `Boolean` — show mana bar: 0=no, 1=yes

## 0x0271: Houses Available

Display a list of available dwellings in the chat window.

- **dwellingType** `DWORD` — The type of dwelling (1=cottage, 2=villa, 3=mansion, 4=apartment)
- **landcellCount** `DWORD` — The number of dwelling locations returned
- **landcells: vector of length landcellCount**
  - **landcell** `DWORD`
- **availableCount** `DWORD` — The total number of dwellings of this type available

## 0x0274: Confirmation Panel

Display a confirmation panel.

- **type** [`ConfirmationType`](enum-types.md#confirmationtype) — the type of confirmation panel to display
- **number** `DWORD` — sequence number
- **text** `String` — text to be included in the confirmation panel message

## 0x0276: Confirmation Panel Closed

A player has closed your confirmation panel.

- **unknown** `DWORD`
- **number** `DWORD` — sequence number

## 0x027A: Allegiance Member Login/out

Display an allegiance login/logout message in the chat window.

- **member** `ObjectID` — the object ID of the player logging in or out
- **logon** `Boolean` — 0=logout, 1=login

## 0x028A: Display Status Message

Display a status message in the chat window.

- **type** [`StatusMessageType1`](enum-types.md#statusmessagetype1) — the type of status message to display

## 0x028B: Display Parameterized Status Message

Display a parameterized status message in the chat window.

- **type** [`StatusMessageType2`](enum-types.md#statusmessagetype2) — the type of status message to display
- **text** `String` — text to be included in the status message

## 0x0295: Set Turbine Chat Channels

Set Turbine Chat channel numbers.

- **allegiance** `DWORD` — the channel number of the allegiance channel
- **general** `DWORD` — the channel number of the general channel
- **trade** `DWORD` — the channel number of the trade channel
- **lfg** `DWORD` — the channel number of the looking-for-group channel
- **roleplay** `DWORD` — the channel number of the roleplay channel

## 0x02BD: Tell

Someone has sent you a @tell.

- **text** `String` — the message text
- **senderName** `String` — the name of the creature sending you the message
- **sender** `ObjectID` — the object ID of the creature sending you the message
- **target** `ObjectID` — the object ID of the recipient of the message (should be you)
- **type** [`ChatMessageType`](enum-types.md#chatmessagetype) — the message type, controls color and @filter processing

## 0x02BE: Create Fellowship

Create or join a fellowship

- **fellowCount** `WORD` — the current number of fellowship members
- **fellowUnknown** `WORD`
- **fellows: vector of length fellowCount**
  - **fellow** [`FellowInfo`](struct-types.md#fellowinfo)
- **name** `String` — the fellowship name
- **leader** `ObjectID` — the object ID of the fellowship leader
- **unknown1** `DWORD`
- **shareXP** `Boolean` — XP sharing: 0=no, 1=yes
- **open** `Boolean` — open fellowship: 0=no, 1=yes
- **unknown2** `DWORD`
- **unknown3** `DWORD`
- **unknown4** `DWORD`

## 0x02BF: Disband Fellowship

Disband your fellowship.

_No fields._

## 0x02C0: Add Fellowship Member

Add a member to your fellowship.

- **fellow** [`FellowInfo`](struct-types.md#fellowinfo)
- **unknown** `DWORD`

## 0x02C1: Add Spell to Spellbook

Add a spell to your spellbook.

- **spell** `SpellID` — the spell ID of the new spell
- **unknown** `WORD`

## 0x02C2: Add Character Enchantment

Apply an enchantment to your character.

- **enchantment** [`Enchantment`](struct-types.md#enchantment)

## 0x02C3: Remove Character Enchantment

Remove an enchantment from your character.

- **spell** `SpellID` — the spell ID of the enchantment to be removed
- **layer** `WORD` — identifies the specific enchantment, if the same spell is applied more than once

## 0x02C5: Remove Multiple Character Enchantments

Remove multiple enchantments from your character.

- **count** `DWORD` — the number of enchantments to be removed
- **enchantments: vector of length count**
  - **spell** `SpellID` — the spell ID of the enchantment to be removed
  - **layer** `WORD` — identifies the specific enchantment, if the same spell is applied more than once

## 0x02C6: Remove All Character Enchantments (Silent)

Silently remove all enchantments from your character, e.g. when you die (no message in the chat window).

_No fields._

## 0x02C7: Remove Character Enchantment (Silent)

Silently remove An enchantment from your character.

- **spell** `SpellID` — the spell ID of the enchantment to be removed
- **layer** `WORD` — identifies the specific enchantment, if the same spell is applied more than once

## 0x02C8: Remove Multiple Character Enchantments (Silent)

Silently remove multiple enchantments from your character (no message in the chat window).

- **count** `DWORD` — the number of enchantments to be removed
- **enchantments: vector of length count**
  - **spell** `SpellID` — the spell ID of the enchantment to be removed
  - **layer** `WORD` — identifies the specific enchantment, if the same spell is applied more than once

## 0x02C9: Mild Portal Storm

A portal storm is brewing.

- **severity** `float`

## 0x02CA: Heavy Portal Storm

A portal storm is imminent.

- **severity** `float`

## 0x02CB: Portal Stormed

You have been portal stormed.

_No fields._

## 0x02CC: End Portal Storm

The portal storm has subsided.

_No fields._

## 0x02EB: Status Message

Display a status message on the Action Viewscreen (the red text overlaid on the 3D area).

- **text** `String` — the message text
