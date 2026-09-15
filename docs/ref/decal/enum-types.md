# Enum Types

> Generated from the DecalDev AC protocol reference at <https://skunkworks.sourceforge.net/protocol/Protocol.php> (version 2006.08.22.1).

Enumerations and bit-mask value tables. The heading of each type notes its underlying integer size.

## AmmoType

_Underlying type: `WORD`_

The AmmoType value describes the type of ammunition a missile weapon uses.

- `0x0000` — thrown weapon (no launcher required)
- `0x0001` — arrow (for bows)
- `0x0002` — bolt (for crossbows)
- `0x0004` — dart (for atlatls)

## AnimationMask

_Underlying type: `DWORD`_

Dictates which parts of the animation packet will be present.

- `0x00000001` — Starting Stance for Animation
- `0x00000002` — Secondary Animation flowed into from previous packet
- `0x00000008` — unknown
- `0x00000020` — unknown
- `0x00000004` — unknown
- `0x00000010` — unknown
- `0x00000040` — unknown
- `0x00000080` — Primary Animation

## AnimationType

_Underlying type: `BYTE`_

The animation type defines the fields for the rest of the message

- `0x00` — General Animation
- `0x06` — Move to Object
- `0x07` — Move to Position
- `0x08` — Turn to Object
- `0x09` — Turn to Position

## ArmorHighlightMask

_Underlying type: `WORD`_

The ArmorHighlightMask selects which armor attributes highlighting is applied to.

- `0x0001` — Armor Level
- `0x0002` — Slashing Protection
- `0x0004` — Piercing Protection
- `0x0008` — Bludgeoning Protection
- `0x0010` — Cold Protection
- `0x0020` — Fire Protection
- `0x0040` — Acid Protection
- `0x0080` — Electrical Protection

## AttrID

_Underlying type: `DWORD`_

The AttrID identifies a specific Character attribute.

- `0x01` — Strength
- `0x02` — Endurance
- `0x03` — Quickness
- `0x04` — Coordination
- `0x05` — Focus
- `0x06` — Self

## AttributeHighlightMask

_Underlying type: `WORD`_

The AttributeHighlightMask selects which creature attributes highlighting is applied to.

- `0x0001` — Strength
- `0x0002` — Endurance
- `0x0004` — Quickness
- `0x0008` — Coordination
- `0x0010` — Focus
- `0x0020` — Self
- `0x0040` — Health
- `0x0080` — Stamina
- `0x0100` — Mana

## BooleanPropertyID

_Underlying type: `DWORD`_

The BooleanPropertyID identifies a specific Character or Object Boolean property.

- `0x02` — Open
- `0x03` — Locked
- `0x18` — Hook Visibility
- `0x3F` — Unlimited Uses
- `0x45` — Can be Sold
- `0x5B` — Retained
- `0x63` — Ivoryable
- `0x64` — Dyeable
- `0x6E` — Away From Keyboard

## CharacterOptions1

_Underlying type: `DWORD`_

The CharacterOptions1 word contains character options.

- `0x00000001` — unused (was Automatically Create Shortcuts)
- `0x00000002` — Automatically Repeat Attacks
- `0x00000004` — Accept Allegiance Requests (Inverted)
- `0x00000008` — Accept Fellowship Requests (Inverted)
- `0x00000010` — unused (was Invert Mouse Look Up/Down)
- `0x00000020` — unused (was Disable House Restriction Effects)
- `0x00000040` — Let Other Players Give You Items
- `0x00000080` — Automatically keep combat targets in view
- `0x00000100` — Display Tooltips
- `0x00000200` — Attempt to Deceive Other Players
- `0x00000400` — Run as Default Movement
- `0x00000800` — Stay in Chat Mode after sending a Message
- `0x00001000` — Advanced Combet Interface (No Panel)
- `0x00002000` — Auto Target
- `0x00004000` — unused (was Right-click mouselook)
- `0x00008000` — Vivid Targeting Indicator
- `0x00010000` — unused (was Disable Most Weather Effects)
- `0x00020000` — Ignore All Trade Requests
- `0x00040000` — Share Fellowship Experience
- `0x00080000` — Accept Corpse-Looting Permissions
- `0x00100000` — Share Fellowship Loot
- `0x00200000` — Stretch UI
- `0x00400000` — Show Coordinates Below The Radar
- `0x00800000` — Display Spell Durations
- `0x01000000` — unused (was Play Sounds Only When Active Application)
- `0x02000000` — Disable House Restriction Effects
- `0x04000000` — Drag Items open secure Trade
- `0x08000000` — Show Allegiance Logons
- `0x10000000` — Use Charge Attack
- `0x20000000` — Automatically Accept Fellowship Requests
- `0x40000000` — Listen to Allegiance Chat
- `0x80000000` — Use Crafting Chance of Success Dialog

## CharacterOptions2

_Underlying type: `DWORD`_

The CharacterOptions2 word contains additional character options.

- `0x00000001` — Always Daylight Outdoors
- `0x00000002` — Allow Others to See Your Date of Birth
- `0x00000004` — Allow Others to See Your Chess Rank
- `0x00000008` — Allow Others to See Your Fishing Skill
- `0x00000010` — Allow Others to See Your Number of Deaths
- `0x00000020` — Allow Others to See Your Age
- `0x00000040` — Display Timestamps
- `0x00000080` — Salvage Multiple Materials at Once
- `0x00000100` — Listen to General Chat
- `0x00000200` — Listen to Trade Chat
- `0x00000400` — Listen to LFG Chat
- `0x00000800` — Listen to Roleplaying Chat
- `0x00002000` — Allow Others to See Your Number of Titles
- `0x00004000` — Use Main Pack as Default for Picking Up Items
- `0x00008000` — Lead Missle Targets
- `0x00010000` — Use Fast Missles
- `0x00020000` — Filter Language
- `0x00040000` — Confirm use of Rare Gems

## ChatDisplayMask

_Underlying type: `QWORD`_

The ChatDisplayMask identifies that types of chat that are displayed in each chat window.

- `0x0000000003912021` — Gameplay (main chat window only)
- `0x000000000000c302` — Mandatory (main chat window only, cannot be disabled)
- `0x0000000000001004` — Area Chat
- `0x0000000000000018` — Tells
- `0x0000000000600040` — Combat
- `0x0000000000020080` — Magic
- `0x0000000000040c00` — Allegiance
- `0x0000000000080000` — Fellowship
- `0x0000000004000000` — Errors
- `0x0000000008000000` — General Channel
- `0x0000000010000000` — Trade Channel
- `0x0000000020000000` — LFG Channel
- `0x0000000040000000` — Roleplay Channel

## ChatFilterMask

_Underlying type: `DWORD`_

The ChatFilterMask identifies types of messages that are squelched or filtered (/messagetypes).

- `0x00000004` — Speech
- `0x00000008` — Tell
- `0x00000040` — Combat
- `0x00000080` — Magic
- `0x00001000` — Emote
- `0x00010000` — Appraisal
- `0x00020000` — Spellcasting
- `0x00040000` — Allegiance
- `0x00080000` — Fellowship
- `0x00200000` — Combat_Enemy
- `0x00400000` — Combat_Self
- `0x00800000` — Recall
- `0x01000000` — Craft
- `0x02000000` — Salvaging
- `0xFFFFFFFF` — All message types

## ChatMessageType

_Underlying type: `DWORD`_

The ChatMessageType categorizes chat window messages to control color and filtering.

- `0x00` — Broadcast (e.g. allegiance MOTD)
- `0x02` — Public Chat
- `0x03` — Private Tell
- `0x04` — Outgoing Tell (e.g. 'You tell ...')
- `0x07` — Magic Spell Results
- `0x0c` — NPC Chat
- `0x11` — Player Spellcasting
- `0x12` — Creature Chat (e.g. 'Fellow warriors, aid me!')
- `0x17` — Recall (e.g. 'Player is recalling home.')

## CompressionType

_Underlying type: `BYTE`_

The CompressionType identifies the type of data compression used.

- `0x00` — no compression
- `0x01` — zlib compression

## ConfirmationType

_Underlying type: `DWORD`_

The ConfirmationType identifies the specific confirmation panel to be displayed.

- `0x01` — <text> would like to swear allegiance to you. Do you accept? (Default is No)
- `0x02` — <text> Continue? (Default is No)
- `0x04` — <text> has invited you to join their fellowship. Do you accept? (Default is No)
- `0x05` — You determine that you have a <text> percent chance to succeed, do you wish to continue? (Default is No)

## CoverageMask

_Underlying type: `DWORD`_

The CoverageMask value describes what parts of the body an item protects.

- `0x00000002` — underwear: upper legs
- `0x00000004` — underwear: lower legs
- `0x00000008` — underwear: chest
- `0x00000010` — underwear: abdomen
- `0x00000020` — underwear: upper arms
- `0x00000040` — underwear: lower arms
- `0x00000100` — outerwear: upper legs
- `0x00000200` — outerwear: lower legs
- `0x00000400` — outerwear: chest
- `0x00000800` — outerwear: abdomen
- `0x00001000` — outerwear: upper arms
- `0x00002000` — outerwear: lower arms
- `0x00004000` — head
- `0x00008000` — hands
- `0x00010000` — feet

## CurVitalID

_Underlying type: `DWORD`_

The CurVitalID identifies a specific Character vital (secondary attribute).

- `0x02` — Current Health
- `0x04` — Current Stamina
- `0x06` — Current Mana

## DWORDPropertyID

_Underlying type: `DWORD`_

The DWORDPropertyID identifies a specific Character or Object DWORD property.

- `0x02` — Species
- `0x05` — Burden
- `0x0A` — Equipped Slots
- `0x11` — Rare ID
- `0x13` — Value
- `0x14` — Total Pyreals
- `0x18` — Skill Credits Available
- `0x19` — Creature Level
- `0x1A` — Restricted to AC:ToD Purchasers
- `0x1C` — Armor Level
- `0x1E` — Rank
- `0x21` — Bonded
- `0x23` — Number of Followers
- `0x24` — Unenchantable
- `0x26` — Lockpick Difficulty
- `0x2B` — Deaths
- `0x2D` — Wand Elemental Damage Bonus: DamageType
- `0x56` — Minimum Level Restriction
- `0x57` — Maximum Level Restriction
- `0x58` — Lockpick Skill Bonus
- `0x59` — Affects Vital: VitalID
- `0x5A` — Affects Vital: Amount (also Healing Kit Skill Bonus)
- `0x5B` — Uses Total
- `0x5C` — Uses Remaining
- `0x62` — Date of Birth
- `0x69` — Workmanship
- `0x6A` — Spellcraft
- `0x6B` — Current Mana
- `0x6C` — Maximum Mana
- `0x6D` — Activation Requirement - Arcane Lore (Difficulty)
- `0x6E` — Activation Requirement - Rank
- `0x6F` — Portal Restriction Flags
- `0x71` — Gender
- `0x72` — Attuned
- `0x73` — Activation Requirement - Skill Level
- `0x75` — Mana Cost
- `0x7D` — Age (seconds)
- `0x81` — XP needed for next point of Vitae Penalty reduction
- `0x83` — Material
- `0x9E` — Wield Requirement - Type
- `0x9F` — Wield Requirement - Attribute
- `0xA0` — Wield Requirement - Value
- `0xA6` — Slayer Species
- `0xAA` — Number of Items Salvaged From
- `0xAB` — Number of Times Tinkered
- `0xAC` — Description Format
- `0xAE` — Pages Used
- `0xAF` — Pages Total
- `0xB0` — Activation Requirement - Skill ID
- `0xB1` — Gemstone Setting Quantity
- `0xB2` — Gemstone Setting Type
- `0xB3` — Imbued
- `0xB5` — Chess Rank
- `0xBC` — Heritage
- `0xC0` — Fishing Skill
- `0xC1` — Keys Held
- `0xCC` — Elemental Damage Bonus
- `0xDA` — Augmentation: Reinforcement of the Lugians
- `0xDB` — Augmentation: Bleeargh's Fortitude
- `0xDC` — Augmentation: Oswald's Enhancement
- `0xDD` — Augmentation: Siraluun's Blessing
- `0xDE` — Augmentation: Enduring Calm
- `0xDF` — Augmentation: Steadfast Will
- `0xE0` — Augmentation: Ciandra's Essence
- `0xE1` — Augmentation: Yoshi's Essence
- `0xE2` — Augmentation: Jibril's Essence
- `0xE3` — Augmentation: Celdiseth's Essence
- `0xE4` — Augmentation: Koga's Essence
- `0xE5` — Augmentation: Shadow of the Seventh Mule
- `0xE6` — Augmentation: Might of the Seventh Mule
- `0xE7` — Augmentation: Clutch of the Miser
- `0xE8` — Augmentation: Enduring Enchantment
- `0xE9` — Augmentation: Critical Protection
- `0xEA` — Augmentation: Quick Learner
- `0xEB` — Augmentation: Ciandra's Fortune
- `0xEC` — Augmentation: Charmed Smith
- `0xED` — Augmentation: Innate Renewal
- `0xEE` — Augmentation: Archmage's Endurance
- `0xF0` — Augmentation: Enchancement of the Blade Turner
- `0xF1` — Augmentation: Enchancement of the Arrow Turner
- `0xF2` — Augmentation: Enchancement of the Mace Turner
- `0xF3` — Augmentation: Caustic Enhancement
- `0xF4` — Augmentation: Fiery Enchancement
- `0xF5` — Augmentation: Icy Enchancement
- `0xF6` — Augmentation: Storm's Enhancement
- `0x106` — Titles Earned

## DamageLocation

_Underlying type: `DWORD`_

The DamageLocation indicates where damage was done.

- `0x00` — Head
- `0x01` — Chest
- `0x02` — Abdomen
- `0x03` — Upper Arm
- `0x04` — Lower Arm
- `0x05` — Hand
- `0x06` — Upper Leg
- `0x07` — Lower Leg
- `0x08` — Foot

## DamageType

_Underlying type: `DWORD`_

The DamageType identifies the type of damage.

- `0x01` — Slashing
- `0x02` — Piercing
- `0x04` — Bludgeoning
- `0x08` — Cold
- `0x10` — Fire
- `0x20` — Acid
- `0x40` — Electric

## DoublePropertyID

_Underlying type: `DWORD`_

The DoublePropertyID identifies a specific Character or Object Double property.

- `0x05` — Mana Rate of Change (points per second)
- `0x1D` — Melee Defense Bonus (multiplier)
- `0x57` — Mana Transfer Efficiency
- `0x64` — Healing Kit Restoration Bonus
- `0x89` — Mana Stone Chance of Destruction
- `0x90` — Mana Conversion Bonus (percentage to add)
- `0x95` — Missile Defense Bonus (multiplier)
- `0x96` — Magic Defense Bonus (multiplier)
- `0x98` — Elemental Damage Bonus vs. Monsters (multiplier)

## Effect

_Underlying type: `DWORD`_

Audio/Visual Effect ID

- `0x04` — War Launch
- `0x05` — War Land
- `0x06` — Red Clouds Rising (Strength/Health Buff)
- `0x07` — Red Clouds Falling (Strength/Health Debuff)
- `0x08` — Orange Clouds Rising (Coordination Buff)
- `0x09` — Orange Clouds Falling (Coordination Debuff)
- `0x0A` — Yellow Clouds Rising (Endurance Buff)
- `0x0B` — Yellow Clouds Falling (Endurance Debuff)
- `0x0C` — Green Clouds Rising (Quickness Buff)
- `0x0D` — Green Clouds Falling (Quickness Debuff)
- `0x0E` — Cyan Clouds Rising (Self Buff, Lifestone Recall/Tie)
- `0x0F` — Cyan Clouds Falling (Self Debuff)
- `0x10` — Purple Clouds Rising (Focus Buff, Portal Recall/Summon/Tie)
- `0x11` — Purple Clouds Falling (Focus Debuff)
- `0x12` — Red Bubbles Rising (Weapon Skill Buff)
- `0x13` — Red Bubbles Falling (Weapon Skill Debuff)
- `0x14` — Orange Bubbles Rising (Allegiance/Crafting Skill Buff)
- `0x15` — Orange Bubbles Falling (Allegiance/Crafting Skill Debuff)
- `0x16` — Yellow Bubbles Rising (Defense Skill Buff)
- `0x17` — Yellow Bubbles Falling (Defense Skill Debuff)
- `0x18` — Green Bubbles Rising (Run/Jump Skill Buff)
- `0x19` — Green Bubbles Falling (Run/Jump Skill Debuff)
- `0x1A` — Cyan Bubbles Rising (Magic/Alchemy Skill Buff)
- `0x1B` — Cyan Bubbles Falling (Magic/Alchemy Skill Debuff)
- `0x1C` — Purple Bubbles Rising (Assessment/Tinkering Skill Buff, Learn Spell)
- `0x1D` — Purple Bubbles Falling (Assessment/Tinkering Skill Debuff)
- `0x1E` — Red Stars In (Heal, Infuse Health)
- `0x1F` — Red Stars Out (Harm, Drain Health)
- `0x20` — Blue Stars In (Mana Boost, Infuse Mana)
- `0x21` — Blue Stars Out (Mana Drain, Drain Mana)
- `0x22` — Yellow Stars In (Revitalize, Infuse Stamina)
- `0x23` — Yellow Stars Out (Enfeeble, Drain Stamina)
- `0x24` — Red Stars Rotating Out (Regeneration)
- `0x25` — Red Stars Rotating In (Fester)
- `0x26` — Blue Stars Rotating Out (Mana Renewal)
- `0x27` — Blue Stars Rotating In (Mana Depletion)
- `0x28` — Yellow Stars Rotating In (Rejuvenation)
- `0x29` — Yellow Stars Rotating Out (Exhaustion)
- `0x2A` — Red Shield Rising (Fire Protection)
- `0x2B` — Red Shield Falling (Fire Vulnerability)
- `0x2C` — Orange Shield Rising (Piercing Protection)
- `0x2D` — Orange Shield Falling (Piercing Vulnerability)
- `0x2E` — Yellow Shield Rising (Blade Protection)
- `0x2F` — Yellow Shield Falling (Blade Vulnerability)
- `0x30` — Green Shield Rising (Acid Protection)
- `0x31` — Green Shield Falling (Acid Vulnerability)
- `0x32` — Cyan Shield Rising (Cold Protection)
- `0x33` — Cyan Shield Falling (Cold Vulnerability)
- `0x34` — Purple Shield Rising (Lightning Protection)
- `0x35` — Purple Shield Falling (Lightning Vulnerability)
- `0x36` — Black Shield Rising (Bludgeon Protection, Armor)
- `0x37` — Black Shield Falling (Bludgeon Vulnerability, Imperil)
- `0x38` — Red/White Sparks (Flame Bane, Blood Drinker)
- `0x39` — Red/Black Sparks (Flame Lure, Blood Loather)
- `0x3A` — Orange/White Sparks (Piercing Bane, Heart Seeker, Strengthen Lock)
- `0x3B` — Orange/Black Sparks (Piercing Lure, Turn Blade, Weaken Lock)
- `0x3C` — Yellow/White Sparks (Blade Bane, Defender)
- `0x3D` — Yellow/Black Sparks (Blade Lure, Lure Blade)
- `0x3E` — Green/White Sparks (Acid Bane, Swift Killer)
- `0x3F` — Green/Black Sparks (Acid Lure, Leaden Weapon)
- `0x40` — Cyan/White Sparks (Frost Bane)
- `0x41` — Cyan/Black Sparks (Frost Lure)
- `0x42` — Purple/White Sparks (Bludgeon/Lightning Bane, Hermetic Link)
- `0x43` — Purple/Black Sparks (Bludgeon/Lightning Lure, Hermetic Void, Dispel)
- `0x48` — Red Stars Out / Yellow Stars In (Health to Stamina)
- `0x49` — Red Stars Out / Blue Stars In (Health to Mana)
- `0x4A` — Yellow Stars Out / Red Stars In (Stamina to Health)
- `0x4B` — Yellow Stars Out / Blue Stars In (Stamina to Mana)
- `0x4C` — Blue Stars Out / Red Stars In (Mana to Health)
- `0x4D` — Blue Stars Out / Yellow Stars In (Mana to Stamina)
- `0x50` — Fizzle
- `0x57` — Idle Emote
- `0x58` — Item Dissolve
- `0x73` — Portal Bubbles
- `0x76` — Raise Attribute or Skill
- `0x77` — Equip Item
- `0x78` — Unequip Item
- `0x79` — Give Item
- `0x7A` — Pick Up Item
- `0x7B` — Drop Item
- `0x7E` — Unlock Item
- `0x81` — Enchantment Expiration
- `0x82` — Item Out of Mana
- `0x89` — Gain Level
- `0x8D` — White/White Sparks (Impenetrability)
- `0x8E` — White/Black Sparks (Brittlemail)
- `0x91` — White/Purple Clouds (Life Dispel)
- `0x92` — White/Cyan Clouds (Creature Dispel)

## EndTradeReason

_Underlying type: `DWORD`_

The EndTradeReason identifies the reason trading was ended.

- `0x00` — trade ended normally
- `0x02` — party entered combat mode
- `0x51` — party moved out of range or cancelled trade

## EquipMask

_Underlying type: `DWORD`_

The EquipMask value describes the equipment slots an item uses.

- `0x00000001` — head
- `0x00000002` — underwear: chest
- `0x00000004` — underwear: abdomen
- `0x00000008` — underwear: upper arms
- `0x00000010` — underwear: lower arms
- `0x00000020` — hands
- `0x00000040` — underwear: upper legs
- `0x00000080` — underwear: lower legs
- `0x00000100` — feet
- `0x00000200` — outerwear: chest
- `0x00000400` — outerwear: abdomen
- `0x00000800` — outerwear: upper arms
- `0x00001000` — outerwear: lower arms
- `0x00002000` — outerwear: upper legs
- `0x00004000` — outerwear: lower legs
- `0x00008000` — necklace
- `0x00010000` — bracelet (right)
- `0x00020000` — bracelet (left)
- `0x00040000` — ring (right)
- `0x00080000` — ring (left)
- `0x00100000` — melee weapon
- `0x00200000` — shield
- `0x00400000` — missile weapon
- `0x00800000` — ammunition
- `0x01000000` — wand

## FriendsUpdateType

_Underlying type: `DWORD`_

The type of the friend change event.

- `0x0000` — Full friends list (at log in)
- `0x0001` — Friend added
- `0x0002` — Friend removed
- `0x0004` — Friend logged in or out

## GameAction

_Underlying type: `DWORD`_

The GameAction identifies the meaning of the rest of the message.

- `0x0005` — Set Single Character Option
- `0x0010` — Set AFK Message
- `0x0019` — Store Item
- `0x001A` — Equip Item
- `0x001B` — Drop Item
- `0x0036` — Use Item
- `0x0044` — Raise Vital
- `0x0045` — Raise Attribute
- `0x0046` — Raise Skill
- `0x0047` — Train Skill
- `0x0048` — Cast Spell
- `0x004A` — Cast Spell on Object
- `0x00A1` — Materialize
- `0x00CD` — Give Item
- `0x019C` — Make Shortcut
- `0x019D` — Remove Shortcut
- `0x01A1` — Set Character Options
- `0x01E3` — Add Spell to Spellbar
- `0x01E4` — Remove Spell from Spellbar

## GameEvent

_Underlying type: `DWORD`_

The GameEvent identifies the meaning of the rest of the message.

- `0x0004` — Message Box
- `0x0013` — Login Character
- `0x0016` — Retired - Transaction Message
- `0x0020` — Allegiance Info
- `0x0021` — Friends List Update
- `0x0022` — Insert Inventory Item
- `0x0023` — Wear Item
- `0x0029` — Title List
- `0x002b` — Set Title
- `0x0038` — Retired - Direct Chat
- `0x004C` — Retired - Add Spell to Spellbook / Cast Spell
- `0x004D` — Retired - Delete Spell from Spellbook
- `0x004E` — Retired - Add Enchantment
- `0x004F` — Retired - Remove Enchantment
- `0x0052` — Close Container
- `0x0062` — Approach Vendor
- `0x009C` — Retired - End Portal Storm
- `0x009D` — Retired - Mild Portal Storm
- `0x009E` — Retired - Heavy Portal Storm
- `0x009F` — Retired - Portal Stormed
- `0x00A0` — Failure to Give Item
- `0x00A3` — Fellowship Member Quit
- `0x00A4` — Fellowship Member Dismissed
- `0x00A7` — Retired - Quit Fellowship
- `0x00AF` — Retired - Create Fellowship
- `0x00B0` — Retired - Recruit Member
- `0x00B1` — Retired - Dismiss Member
- `0x00B3` — Retired - Disband Fellowship
- `0x00B4` — Read Table of Contents
- `0x00B8` — Read Page
- `0x00C9` — Identify Object
- `0x0147` — Group Chat
- `0x014A` — Retired - Group Chat
- `0x0196` — Set Pack Contents
- `0x019A` — Drop from Inventory
- `0x01A4` — Retired - Remove Enchantment (Silent)
- `0x01A6` — Retired - Remove Multiple Enchantments
- `0x01A7` — Attack Completed
- `0x01A8` — Delete Spell from Spellbook
- `0x01AC` — Your death.
- `0x01AD` — Kill/Death Message
- `0x01AE` — Retired - Remove Multiple Enchantments
- `0x01B1` — Inflict Melee Damage
- `0x01B2` — Receive Melee Damage
- `0x01B3` — Other Melee Evade
- `0x01B4` — Self Melee Evade
- `0x01B8` — Start Melee Attack
- `0x01C0` — Update Health
- `0x01C3` — Age Command Result
- `0x01C7` — Ready. Previous action complete
- `0x01C8` — Update Allegiance Info
- `0x01CB` — Close Assess Panel
- `0x01EA` — Ping Reply
- `0x01F4` — Squelched Users List
- `0x01FD` — Enter Trade
- `0x01FF` — End Trade
- `0x0200` — Add Trade Item
- `0x0202` — Accept Trade
- `0x0203` — Un-Accept Trade
- `0x0205` — Reset Trade
- `0x0207` — Failure to add a trade item
- `0x0208` — Failure to complete a trade
- `0x021D` — Display Dwelling Purchase/Maintenance Panel
- `0x0225` — House Information for Owners
- `0x0226` — House Information for Non-Owners
- `0x0257` — House Guest List
- `0x0264` — Update Item Mana Bar
- `0x0271` — Houses Available
- `0x0274` — Confirmation Panel
- `0x0276` — Confirmation Panel Closed
- `0x027A` — Allegiance Member Login/out
- `0x028A` — Display Status Message
- `0x028B` — Display Parameterized Status Message
- `0x0295` — Set Turbine Chat Channels
- `0x02BD` — Tell
- `0x02BE` — Create Fellowship
- `0x02BF` — Disband Fellowship
- `0x02C0` — Add Fellowship Member
- `0x02C1` — Add Spell to Spellbook
- `0x02C2` — Add Character Enchantment
- `0x02C3` — Remove Character Enchantment
- `0x02C5` — Remove Multiple Character Enchantments
- `0x02C6` — Remove All Character Enchantments (Silent)
- `0x02C7` — Remove Character Enchantment (Silent)
- `0x02C8` — Remove Multiple Character Enchantments (Silent)
- `0x02C9` — Mild Portal Storm
- `0x02CA` — Heavy Portal Storm
- `0x02CB` — Portal Stormed
- `0x02CC` — End Portal Storm
- `0x02EB` — Status Message

## GroupChatType

_Underlying type: `DWORD`_

The GroupChatType identifies the type of group chat message.

- `0x00000800` — @f - Tell Fellowship
- `0x00001000` — @v - Tell Vassals
- `0x00002000` — @p - Tell Patron
- `0x00004000` — @m - Tell Monarch
- `0x01000000` — @c - Tell Co-Vassals
- `0x02000000` — @allegiance broadcast - Tell All Allegiance Members

## HookType

_Underlying type: `WORD`_

The HookType identifies the types of dwelling hooks.

- `0x0001` — floor hook
- `0x0002` — wall hook
- `0x0004` — ceiling hook
- `0x0008` — yard hook
- `0x0010` — roof hook

## IconHighlight

_Underlying type: `DWORD`_

The IconHighlight value describes the type of highlight (outline) applied to an icon.

- `0x00000001` — enchanted item (blue)
- `0x00000004` — healing foods (red)
- `0x00000008` — mana foods (blue)
- `0x00000010` — hearty stamina foods (yellow)
- `0x00000020` — fire weapon/ammo (orange)
- `0x00000040` — lightning weapon/ammo (purple)
- `0x00000080` — cold weapon/ammo (white)
- `0x00000100` — acid weapon/ammo (green)

## ItemType

_Underlying type: `DWORD`_

The ItemType specifies whether an object is a pack, a foci, or a regular item.

- `0x00` — item
- `0x01` — pack
- `0x02` — foci

## LinkPropertyID

_Underlying type: `DWORD`_

The LinkPropertyID identifies a specific Character or Object Link property.

- `0x02` — Container
- `0x03` — Equipped By
- `0x0B` — Last Attacker
- `0x18` — Allegiance Object
- `0x19` — Patron
- `0x1A` — Monarch
- `0x20` — Owned By

## MaterialType

_Underlying type: `DWORD`_

The MaterialType identifies the material an object is made of.

- `0x00000001` — Ceramic
- `0x00000002` — Porcelain
- `0x00000004` — Linen
- `0x00000005` — Satin
- `0x00000006` — Silk
- `0x00000007` — Velvet
- `0x00000008` — Wool
- `0x0000000A` — Agate
- `0x0000000B` — Amber
- `0x0000000C` — Amethyst
- `0x0000000D` — Aquamarine
- `0x0000000E` — Azurite
- `0x0000000F` — Black Garnet
- `0x00000010` — Black Opal
- `0x00000011` — Bloodstone
- `0x00000012` — Carnelian
- `0x00000013` — Citrine
- `0x00000014` — Diamond
- `0x00000015` — Emerald
- `0x00000016` — Fire Opal
- `0x00000017` — Green Garnet
- `0x00000018` — Green Jade
- `0x00000019` — Hematite
- `0x0000001A` — Imperial Topaz
- `0x0000001B` — Jet
- `0x0000001C` — Lapis Lazuli
- `0x0000001D` — Lavender Jade
- `0x0000001E` — Malachite
- `0x0000001F` — Moonstone
- `0x00000020` — Onyx
- `0x00000021` — Opal
- `0x00000022` — Peridot
- `0x00000023` — Red Garnet
- `0x00000024` — Red Jade
- `0x00000025` — Rose Quartz
- `0x00000026` — Ruby
- `0x00000027` — Sapphire
- `0x00000028` — Smokey Quartz
- `0x00000029` — Sunstone
- `0x0000002A` — Tiger Eye
- `0x0000002B` — Tourmaline
- `0x0000002C` — Turquoise
- `0x0000002D` — White Jade
- `0x0000002E` — White Quartz
- `0x0000002F` — White Sapphire
- `0x00000030` — Yellow Garnet
- `0x00000031` — Yellow Topaz
- `0x00000032` — Zircon
- `0x00000033` — Ivory
- `0x00000034` — Leather
- `0x00000035` — Armoredillo Hide
- `0x00000036` — Gromnie Hide
- `0x00000037` — Reed Shark Hide
- `0x00000039` — Brass
- `0x0000003A` — Bronze
- `0x0000003B` — Copper
- `0x0000003C` — Gold
- `0x0000003D` — Iron
- `0x0000003E` — Pyreal
- `0x0000003F` — Silver
- `0x00000040` — Steel
- `0x00000042` — Alabaster
- `0x00000043` — Granite
- `0x00000044` — Marble
- `0x00000045` — Obsidian
- `0x00000046` — Sandstone
- `0x00000047` — Serpentine
- `0x00000049` — Ebony
- `0x0000004A` — Mahogany
- `0x0000004B` — Oak
- `0x0000004C` — Pine
- `0x0000004D` — Teak

## ObjectBehaviorFlags

_Underlying type: `DWORD`_

Flags related to the use of the item.

- `0x00000001` — can be opened (false if locked)
- `0x00000002` — can be inscribed
- `0x00000004` — cannot be picked up
- `0x00000008` — is a player
- `0x00000010` — is not an npc
- `0x00000020` — unknown
- `0x00000040` — unknown
- `0x00000080` — cannot be selected
- `0x00000100` — can be read
- `0x00000200` — is a merchant
- `0x00000400` — is a pk altar
- `0x00000800` — is an npk altar
- `0x00001000` — is a door
- `0x00002000` — is a corpse
- `0x00004000` — can be attuned to (lifestone)
- `0x00008000` — adds to health, stamina or mana
- `0x00010000` — is a healing kit
- `0x00020000` — is a lockpick
- `0x00040000` — is a portal
- `0x00800000` — is a foci
- `0x04000000` — has an extra flags DWORD

## ObjectCategoryFlags

_Underlying type: `DWORD`_

Part one of an object's flags

- `0x00000001` — Melee Weapon
- `0x00000002` — Armor
- `0x00000004` — Clothing
- `0x00000008` — Jewelry
- `0x00000010` — Creature (Player/NPC/Monster)
- `0x00000020` — Food
- `0x00000040` — Pyreals
- `0x00000080` — Miscellaneous
- `0x00000100` — Missile Weapons/Ammunition
- `0x00000200` — Containers
- `0x00000400` — Wrapped Fletching Supplies, House Decorations
- `0x00000800` — Gems, Pack dolls, Decorative Statues
- `0x00001000` — Spell Components
- `0x00002000` — Books, Parchment, Scrolls, Signs, Statues
- `0x00004000` — Keys, Lockpicks
- `0x00008000` — Casting Item (wand, orb, staff)
- `0x00010000` — Portal
- `0x00020000` — Lockable
- `0x00040000` — Trade Notes
- `0x00080000` — Mana Stones, Mana Charges
- `0x00100000` — Services
- `0x00200000` — unknown (no longer plants)
- `0x00400000` — Cooking Ingredients and Supplies, Plants, Dye Pots
- `0x00800000` — Loose Fletching Supplies
- `0x01000000` — unknown
- `0x02000000` — unknown
- `0x04000000` — Alchemy Ingredients and Supplies, Oils, Dye Vials
- `0x08000000` — unknown
- `0x10000000` — Lifestone
- `0x20000000` — Ust
- `0x40000000` — Salvage
- `0x80000000` — unknown

## OptionPropertyID

_Underlying type: `DWORD`_

The OptionPropertyID identifies a specific character option.

- `0x00` — Automatically Repeat Attacks
- `0x01` — Ignore Allegiance Requests
- `0x02` — Ignore Fellowship Requests
- `0x0F` — Share Fellowship Experience
- `0x10` — Accept Corpse-Looting Permissions
- `0x11` — Share Fellowship Loot
- `0x12` — Automatically Accept Fellowship Requests
- `0x19` — Use Charge Attack
- `0x1B` — Listen to Allegiance Chat
- `0x23` — Listen to General Chat
- `0x24` — Listen to Trade Chat
- `0x25` — Listen to LFG Chat
- `0x26` — Listen to Roleplaying Chat
- `0x2A` — Lead Missle Targets
- `0x2B` — Use Fast Missles

## PositionFlags

_Underlying type: `DWORD`_

The PositionFlags value defines the fields present in the Position structure.

- `0x00000001` — velocity vector is present
- `0x00000002` — unknown DWORD is present
- `0x00000004` — object is grounded
- `0x00000008` — orientation quaternion has no w component
- `0x00000010` — orientation quaternion has no x component
- `0x00000020` — orientation quaternion has no y component
- `0x00000040` — orientation quaternion has no z component

## PositionPropertyID

_Underlying type: `DWORD`_

The PositionPropertyID identifies a specific Character or Object Position property.

- `0x0E` — Last Corpse Location

## PropertyType

_Underlying type: `DWORD`_

The PropertyType value defines the structure and content of a property.

- `0x1000007F` — chat window display mask
- `0x10000080` — inactive window opacity
- `0x10000081` — active window opacity
- `0x10000086` — chat window position (x)
- `0x10000087` — chat window position (y)
- `0x10000088` — chat window size (x)
- `0x10000089` — chat window size (y)
- `0x1000008A` — chat window enabled
- `0x1000008B` — a window property list
- `0x1000008C` — a vector of window property lists
- `0x1000008D` — chat window title

## QWORDPropertyID

_Underlying type: `DWORD`_

The QWORDPropertyID identifies a specific Character or Object QWORD property.

- `0x01` — Total Experience
- `0x02` — Unassigned Experience

## ResourcePropertyID

_Underlying type: `DWORD`_

The ResourcePropertyID identifies a specific Character or Object Resource property.

- `0x08` — Icon

## ResourceType

_Underlying type: `DWORD`_

The ResourceType identifies the dat file to be used.

- `0x01` — client_portal.dat
- `0x02` — client_cell_1.dat
- `0x03` — client_local_English.dat

## SkillID

_Underlying type: `DWORD`_

The SkillID identifies a specific Character skill.

- `0x01` — Axe
- `0x02` — Bow
- `0x03` — Crossbow
- `0x04` — Dagger
- `0x05` — Mace
- `0x06` — Melee Defense
- `0x07` — Missile Defense
- `0x09` — Spear
- `0x0A` — Staff
- `0x0B` — Sword
- `0x0C` — Thrown Weapons
- `0x0D` — Unarmed Combat
- `0x0E` — Arcane Lore
- `0x0F` — Magic Defense
- `0x10` — Mana Conversion
- `0x12` — Item Tinkering
- `0x13` — Assess Person
- `0x14` — Deception
- `0x15` — Healing
- `0x16` — Jump
- `0x17` — Lockpick
- `0x18` — Run
- `0x1B` — Assess Creature
- `0x1C` — Weapon Tinkering
- `0x1D` — Armor Tinkering
- `0x1E` — Magic Item Tinkering
- `0x1F` — Creature Enchantment
- `0x20` — Item Enchantment
- `0x21` — Life Magic
- `0x22` — War Magic
- `0x23` — Leadership
- `0x24` — Loyalty
- `0x25` — Fletching
- `0x26` — Alchemy
- `0x27` — Cooking
- `0x28` — Salvaging

## SkillState

_Underlying type: `DWORD`_

The SkillState identifies whether a skill is untrained, trained or specialized.

- `0x01` — Untrained
- `0x02` — Trained
- `0x03` — Specialized

## SourceType

_Underlying type: `BYTE`_

Chat window string source

- `0x00` — String is Loaded from the localization Dat
- `0x01` — String value follows inline

## StanceMode

_Underlying type: `WORD`_

The stance for a character or monster.

- `0x3C` — melee UA weapon with no shield in attack stance
- `0x3D` — Standing
- `0x3E` — melee weapon with no shield in attack stance
- `0x3F` — bow in attack stance
- `0x40` — melee weapon with shield in attack stance
- `0x49` — Spellcasting

## StatusMessageType1

_Underlying type: `DWORD`_

The StatusMessageType1 identifies the specific message to be displayed in the chat window.

- `0x001D` — You're too busy!
- `0x03F7` — You are too fatigued to attack!
- `0x03F8` — You are out of ammunition!
- `0x03F9` — Your missile attack misfired!
- `0x03FA` — You've attempted an impossible spell path!
- `0X03FE` — You don't know that spell!
- `0X03FF` — Incorrect target type
- `0x0400` — You don't have all the components for this spell.
- `0x0401` — You don't have enough Mana to cast this spell.
- `0x0402` — Your spell fizzled.
- `0x0403` — Your spell's target is missing!
- `0x0404` — Your projectile spell mislaunched!
- `0x043E` — You have solved this quest too recently!
- `0x043F` — You have solved this quest too many times!
- `0x051B` — You have entered your allegiance chat room.
- `0x051C` — You have left an allegiance chat room.
- `0x051D` — Turbine Chat is enabled.

## StatusMessageType2

_Underlying type: `DWORD`_

The StatusMessageType2 identifies the specific message to be displayed in the chat window.

- `0x001E` — <text> is too busy to accept gifts right now.
- `0x002B` — <text> cannot carry anymore.
- `0x03EF` — <text> is not accepting gifts right now.
- `0x046A` — <text> doesn't know what to do with that.
- `0x04D6` — You have succeeded in specializing your <text> skill!
- `0x04D7` — You have succeeded in lowering your <text> skill from specialized to trained!
- `0x04D8` — You have succeeded in untraining your <text> skill!
- `0x04D9` — Although you cannot untrain your <text> skill, you have succeeded in recovering all the experience you had invested in it.

## StringPropertyID

_Underlying type: `DWORD`_

The StringPropertyID identifies a specific Character or Object String property.

- `0x01` — Name
- `0x05` — Title
- `0x07` — Inscription
- `0x08` — Inscribed By
- `0x0A` — Fellowship Name
- `0x0E` — Usage Instructions
- `0x0F` — Simple Description
- `0x10` — Full Description
- `0x15` — Monarch
- `0x19` — Can Only Be Activated By
- `0x23` — Patron
- `0x26` — Portal Destination
- `0x27` — Last Tinkered By
- `0x28` — Imbued By
- `0x2B` — Date of Birth

## TurbineChatType

_Underlying type: `DWORD`_

The TurbineChatType identifies the type of Turbine Chat message.

- `0x01` — inbound message
- `0x03` — outbound message
- `0x05` — outbound message acknowledgement

## VitalID

_Underlying type: `DWORD`_

The VitalID identifies a specific Character vital (secondary attribute).

- `0x01` — Maximum Health
- `0x03` — Maximum Stamina
- `0x05` — Maximum Mana

## WandHighlightMask

_Underlying type: `WORD`_

The WandHighlightMask selects which wand attributes highlighting is applied to.

- `0x1000` — Mana Conversion Bonus

## WeaponHighlightMask

_Underlying type: `WORD`_

The WeaponHighlightMask selects which weapon attributes highlighting is applied to.

- `0x0001` — Bonus to Attack Skill
- `0x0002` — Bonus to Melee Defense
- `0x0004` — Speed
- `0x0008` — Damage

## WieldType

_Underlying type: `BYTE`_

The WieldType value describes a wieldable item's type.

- `0x01` — melee weapon
- `0x02` — missile weapon
- `0x03` — missile ammunition
- `0x04` — shield
