# Asheron's Call Network Protocol Reference

> Generated from the DecalDev AC protocol reference at <https://skunkworks.sourceforge.net/protocol/Protocol.php> (version 2006.08.22.1).

This is a Markdown rendering of the DecalDev protocol documentation describing the network messages exchanged between the Asheron's Call client and server. It is provided as reference material for the Redox extensions.

## Contents

| File | Description |
| --- | --- |
| [messages.md](messages.md) | Top-level protocol messages (each identified by a message opcode). |
| [game-events.md](game-events.md) | `F7B0` Game Event sub-messages (sequenced server events). |
| [game-actions.md](game-actions.md) | `F7B1` Game Action sub-messages (sequenced client actions). |
| [struct-types.md](struct-types.md) | Reusable data structures referenced by messages. |
| [enum-types.md](enum-types.md) | Enumerations and bit-mask value tables. |

## How messages are laid out

Every message begins with a message-type opcode (a `DWORD`), followed by an ordered sequence of fields. Fields are read in the order listed. Notation used throughout these documents:

- **field** `Type` — a fixed field of the given type.
- **name: vector of length N** — a repeated block; the preceding field `N` gives the count.
- _Select one section based on the value of X:_ — a discriminated union; exactly one of the listed cases applies, chosen by the value of field `X`.
- _Choose valid sections by masking against X:_ — a set of optional sections; each case value is a bit flag, and a section is present when its bit is set in field `X`.

Multi-byte integers are little-endian.

## Primitive & common types

| Type | Size | Meaning |
| --- | --- | --- |
| `BYTE` | 8-bit | Unsigned 8-bit integer. |
| `WORD` | 16-bit | Unsigned 16-bit integer (little-endian). |
| `DWORD` | 32-bit | Unsigned 32-bit integer (little-endian). |
| `QWORD` | 64-bit | Unsigned 64-bit integer (little-endian). |
| `float` | 32-bit | IEEE-754 single-precision floating point. |
| `double` | 64-bit | IEEE-754 double-precision floating point. |
| `Boolean` | 32-bit | A DWORD used as a boolean: 0 = false, non-zero (1) = true. |
| `String` | variable | A length-prefixed ASCII string (WORD length, then bytes), padded to a DWORD boundary. |
| `WString` | variable | A length-prefixed wide/Unicode string. |
| `ObjectID` | 32-bit | A DWORD identifying an in-world object (GUID). |
| `ResourceID` | 32-bit | A DWORD identifying a resource/portal.dat entry. |
| `SpellID` | 16-bit | A WORD identifying a spell. |
| `PackedDWORD` | variable | A DWORD stored in a compact, variable-length form. |

## Message index

| Opcode | Name |
| --- | --- |
| `0024` | [Destroy Object](messages.md#0024-destroy-object) |
| `0037` | [Local Chat](messages.md#0037-local-chat) _(Retired)_ |
| `005E` | [Attack](messages.md#005e-attack) _(Retired)_ |
| `0197` | [Adjust Stack Size](messages.md#0197-adjust-stack-size) |
| `019E` | [Player Killed](messages.md#019e-player-killed) |
| `01B5` | [Broadcast Text](messages.md#01b5-broadcast-text) _(Retired)_ |
| `01E0` | [Indirect Text](messages.md#01e0-indirect-text) |
| `01E2` | [Emote Text](messages.md#01e2-emote-text) |
| `0229` | [Set Coverage](messages.md#0229-set-coverage) _(Retired)_ |
| `022C` | [Set Character Flag](messages.md#022c-set-character-flag) _(Retired)_ |
| `022D` | [Set Wielder/Container](messages.md#022d-set-wieldercontainer) _(Retired)_ |
| `022E` | [Set Object Resource](messages.md#022e-set-object-resource) _(Retired)_ |
| `0237` | [Update Statistic](messages.md#0237-update-statistic) _(Retired)_ |
| `023B` | [Update Last Attacker](messages.md#023b-update-last-attacker) _(Retired)_ |
| `023D` | [Update Last Corpse Location](messages.md#023d-update-last-corpse-location) _(Retired)_ |
| `023E` | [Skill Experience](messages.md#023e-skill-experience) _(Retired)_ |
| `0240` | [Train Skill](messages.md#0240-train-skill) _(Retired)_ |
| `0241` | [Update Attribute](messages.md#0241-update-attribute) _(Retired)_ |
| `0243` | [Update Secondary Attribute](messages.md#0243-update-secondary-attribute) _(Retired)_ |
| `0244` | [Vital Statistic Update](messages.md#0244-vital-statistic-update) _(Retired)_ |
| `02BB` | [Creature Message](messages.md#02bb-creature-message) |
| `02BC` | [Creature Message (Ranged)](messages.md#02bc-creature-message-ranged) |
| `02CD` | [Set Character DWORD](messages.md#02cd-set-character-dword) |
| `02CE` | [Set Object DWORD](messages.md#02ce-set-object-dword) |
| `02CF` | [Set Character QWORD](messages.md#02cf-set-character-qword) |
| `02D1` | [Set Character Boolean](messages.md#02d1-set-character-boolean) |
| `02D2` | [Set Object Boolean](messages.md#02d2-set-object-boolean) |
| `02D6` | [Set Object String](messages.md#02d6-set-object-string) |
| `02D8` | [Set Object Resource](messages.md#02d8-set-object-resource) |
| `02D9` | [Set Character Link](messages.md#02d9-set-character-link) |
| `02DA` | [Set Object Link](messages.md#02da-set-object-link) |
| `02DB` | [Set Character Position](messages.md#02db-set-character-position) |
| `02DD` | [Set Character Skill Level](messages.md#02dd-set-character-skill-level) |
| `02E1` | [Set Character Skill State](messages.md#02e1-set-character-skill-state) |
| `02E3` | [Set Character Attribute](messages.md#02e3-set-character-attribute) |
| `02E7` | [Set Character Vital](messages.md#02e7-set-character-vital) |
| `02E9` | [Set Character Current Vital](messages.md#02e9-set-character-current-vital) |
| `F619` | [Lifestone Materialize](messages.md#f619-lifestone-materialize) |
| `F625` | [Change Model](messages.md#f625-change-model) |
| `F62C` | [Server Text](messages.md#f62c-server-text) _(Retired)_ |
| `F643` | [Char Creation Initilisation](messages.md#f643-char-creation-initilisation) |
| `F653` | [End 3D Mode](messages.md#f653-end-3d-mode) |
| `F655` | [Char Deletion](messages.md#f655-char-deletion) |
| `F657` | [Request Login](messages.md#f657-request-login) |
| `F658` | [Character List](messages.md#f658-character-list) |
| `F659` | [Character Login Failure](messages.md#f659-character-login-failure) |
| `F65A` | [Message of the Day](messages.md#f65a-message-of-the-day) _(Retired)_ |
| `F745` | [Create Object](messages.md#f745-create-object) |
| `F746` | [Login Character](messages.md#f746-login-character) |
| `F747` | [Remove Item](messages.md#f747-remove-item) |
| `F748` | [Set Position and Motion](messages.md#f748-set-position-and-motion) |
| `F749` | [Wield Object](messages.md#f749-wield-object) |
| `F74A` | [Move object into inventory.](messages.md#f74a-move-object-into-inventory) |
| `F74B` | [Toggle Object Visibility](messages.md#f74b-toggle-object-visibility) |
| `F74C` | [Animation](messages.md#f74c-animation) |
| `F74E` | [Jumping](messages.md#f74e-jumping) |
| `F750` | [Apply Sound Effect](messages.md#f750-apply-sound-effect) |
| `F751` | [Enter Portal Mode](messages.md#f751-enter-portal-mode) |
| `F755` | [Apply Visual/Sound Effect](messages.md#f755-apply-visualsound-effect) |
| `F7B0` | [Game Event](messages.md#f7b0-game-event) |
| `F7B1` | [Game Action](messages.md#f7b1-game-action) |
| `F7C7` | [Start 3D Mode](messages.md#f7c7-start-3d-mode) _(Retired)_ |
| `F7C8` | [Enter Game](messages.md#f7c8-enter-game) |
| `F7DB` | [Update Object](messages.md#f7db-update-object) |
| `F7DE` | [Turbine Chat](messages.md#f7de-turbine-chat) |
| `F7DF` | [Start 3D Mode](messages.md#f7df-start-3d-mode) |
| `F7E0` | [Server Message](messages.md#f7e0-server-message) |
| `F7E1` | [Server Name](messages.md#f7e1-server-name) |
| `F7E2` | [Update Resource](messages.md#f7e2-update-resource) |
| `F7E7` | [Dat File Patch List](messages.md#f7e7-dat-file-patch-list) |

## Game event index (`F7B0`)

| Event | Name |
| --- | --- |
| `0x0004` | [Message Box](game-events.md#0x0004-message-box) |
| `0x0013` | [Login Character](game-events.md#0x0013-login-character) |
| `0x0016` | [Transaction Message](game-events.md#0x0016-transaction-message) _(Retired)_ |
| `0x0020` | [Allegiance Info](game-events.md#0x0020-allegiance-info) |
| `0x0021` | [Friends List Update](game-events.md#0x0021-friends-list-update) |
| `0x0022` | [Insert Inventory Item](game-events.md#0x0022-insert-inventory-item) |
| `0x0023` | [Wear Item](game-events.md#0x0023-wear-item) |
| `0x0029` | [Title List](game-events.md#0x0029-title-list) |
| `0x002b` | [Set Title](game-events.md#0x002b-set-title) |
| `0x0038` | [Direct Chat](game-events.md#0x0038-direct-chat) _(Retired)_ |
| `0x004C` | [Add Spell to Spellbook / Cast Spell](game-events.md#0x004c-add-spell-to-spellbook-cast-spell) _(Retired)_ |
| `0x004D` | [Delete Spell from Spellbook](game-events.md#0x004d-delete-spell-from-spellbook) _(Retired)_ |
| `0x004E` | [Add Enchantment](game-events.md#0x004e-add-enchantment) _(Retired)_ |
| `0x004F` | [Remove Enchantment](game-events.md#0x004f-remove-enchantment) _(Retired)_ |
| `0x0052` | [Close Container](game-events.md#0x0052-close-container) |
| `0x0062` | [Approach Vendor](game-events.md#0x0062-approach-vendor) |
| `0x009C` | [End Portal Storm](game-events.md#0x009c-end-portal-storm) _(Retired)_ |
| `0x009D` | [Mild Portal Storm](game-events.md#0x009d-mild-portal-storm) _(Retired)_ |
| `0x009E` | [Heavy Portal Storm](game-events.md#0x009e-heavy-portal-storm) _(Retired)_ |
| `0x009F` | [Portal Stormed](game-events.md#0x009f-portal-stormed) _(Retired)_ |
| `0x00A0` | [Failure to Give Item](game-events.md#0x00a0-failure-to-give-item) |
| `0x00A3` | [Fellowship Member Quit](game-events.md#0x00a3-fellowship-member-quit) |
| `0x00A4` | [Fellowship Member Dismissed](game-events.md#0x00a4-fellowship-member-dismissed) |
| `0x00A7` | [Quit Fellowship](game-events.md#0x00a7-quit-fellowship) _(Retired)_ |
| `0x00AF` | [Create Fellowship](game-events.md#0x00af-create-fellowship) _(Retired)_ |
| `0x00B0` | [Recruit Member](game-events.md#0x00b0-recruit-member) _(Retired)_ |
| `0x00B1` | [Dismiss Member](game-events.md#0x00b1-dismiss-member) _(Retired)_ |
| `0x00B3` | [Disband Fellowship](game-events.md#0x00b3-disband-fellowship) _(Retired)_ |
| `0x00B4` | [Read Table of Contents](game-events.md#0x00b4-read-table-of-contents) |
| `0x00B8` | [Read Page](game-events.md#0x00b8-read-page) |
| `0x00C9` | [Identify Object](game-events.md#0x00c9-identify-object) |
| `0x0147` | [Group Chat](game-events.md#0x0147-group-chat) |
| `0x014A` | [Group Chat](game-events.md#0x014a-group-chat) _(Retired)_ |
| `0x0196` | [Set Pack Contents](game-events.md#0x0196-set-pack-contents) |
| `0x019A` | [Drop from Inventory](game-events.md#0x019a-drop-from-inventory) |
| `0x01A4` | [Remove Enchantment (Silent)](game-events.md#0x01a4-remove-enchantment-silent) _(Retired)_ |
| `0x01A6` | [Remove Multiple Enchantments](game-events.md#0x01a6-remove-multiple-enchantments) _(Retired)_ |
| `0x01A7` | [Attack Completed](game-events.md#0x01a7-attack-completed) |
| `0x01A8` | [Delete Spell from Spellbook](game-events.md#0x01a8-delete-spell-from-spellbook) |
| `0x01AC` | [Your death.](game-events.md#0x01ac-your-death) |
| `0x01AD` | [Kill/Death Message](game-events.md#0x01ad-killdeath-message) |
| `0x01AE` | [Remove Multiple Enchantments](game-events.md#0x01ae-remove-multiple-enchantments) _(Retired)_ |
| `0x01B1` | [Inflict Melee Damage](game-events.md#0x01b1-inflict-melee-damage) |
| `0x01B2` | [Receive Melee Damage](game-events.md#0x01b2-receive-melee-damage) |
| `0x01B3` | [Other Melee Evade](game-events.md#0x01b3-other-melee-evade) |
| `0x01B4` | [Self Melee Evade](game-events.md#0x01b4-self-melee-evade) |
| `0x01B8` | [Start Melee Attack](game-events.md#0x01b8-start-melee-attack) |
| `0x01C0` | [Update Health](game-events.md#0x01c0-update-health) |
| `0x01C3` | [Age Command Result](game-events.md#0x01c3-age-command-result) |
| `0x01C7` | [Ready. Previous action complete](game-events.md#0x01c7-ready-previous-action-complete) |
| `0x01C8` | [Update Allegiance Info](game-events.md#0x01c8-update-allegiance-info) |
| `0x01CB` | [Close Assess Panel](game-events.md#0x01cb-close-assess-panel) |
| `0x01EA` | [Ping Reply](game-events.md#0x01ea-ping-reply) |
| `0x01F4` | [Squelched Users List](game-events.md#0x01f4-squelched-users-list) |
| `0x01FD` | [Enter Trade](game-events.md#0x01fd-enter-trade) |
| `0x01FF` | [End Trade](game-events.md#0x01ff-end-trade) |
| `0x0200` | [Add Trade Item](game-events.md#0x0200-add-trade-item) |
| `0x0202` | [Accept Trade](game-events.md#0x0202-accept-trade) |
| `0x0203` | [Un-Accept Trade](game-events.md#0x0203-un-accept-trade) |
| `0x0205` | [Reset Trade](game-events.md#0x0205-reset-trade) |
| `0x0207` | [Failure to add a trade item](game-events.md#0x0207-failure-to-add-a-trade-item) |
| `0x0208` | [Failure to complete a trade](game-events.md#0x0208-failure-to-complete-a-trade) |
| `0x021D` | [Display Dwelling Purchase/Maintenance Panel](game-events.md#0x021d-display-dwelling-purchasemaintenance-panel) |
| `0x0225` | [House Information for Owners](game-events.md#0x0225-house-information-for-owners) |
| `0x0226` | [House Information for Non-Owners](game-events.md#0x0226-house-information-for-non-owners) |
| `0x0257` | [House Guest List](game-events.md#0x0257-house-guest-list) |
| `0x0264` | [Update Item Mana Bar](game-events.md#0x0264-update-item-mana-bar) |
| `0x0271` | [Houses Available](game-events.md#0x0271-houses-available) |
| `0x0274` | [Confirmation Panel](game-events.md#0x0274-confirmation-panel) |
| `0x0276` | [Confirmation Panel Closed](game-events.md#0x0276-confirmation-panel-closed) |
| `0x027A` | [Allegiance Member Login/out](game-events.md#0x027a-allegiance-member-loginout) |
| `0x028A` | [Display Status Message](game-events.md#0x028a-display-status-message) |
| `0x028B` | [Display Parameterized Status Message](game-events.md#0x028b-display-parameterized-status-message) |
| `0x0295` | [Set Turbine Chat Channels](game-events.md#0x0295-set-turbine-chat-channels) |
| `0x02BD` | [Tell](game-events.md#0x02bd-tell) |
| `0x02BE` | [Create Fellowship](game-events.md#0x02be-create-fellowship) |
| `0x02BF` | [Disband Fellowship](game-events.md#0x02bf-disband-fellowship) |
| `0x02C0` | [Add Fellowship Member](game-events.md#0x02c0-add-fellowship-member) |
| `0x02C1` | [Add Spell to Spellbook](game-events.md#0x02c1-add-spell-to-spellbook) |
| `0x02C2` | [Add Character Enchantment](game-events.md#0x02c2-add-character-enchantment) |
| `0x02C3` | [Remove Character Enchantment](game-events.md#0x02c3-remove-character-enchantment) |
| `0x02C5` | [Remove Multiple Character Enchantments](game-events.md#0x02c5-remove-multiple-character-enchantments) |
| `0x02C6` | [Remove All Character Enchantments (Silent)](game-events.md#0x02c6-remove-all-character-enchantments-silent) |
| `0x02C7` | [Remove Character Enchantment (Silent)](game-events.md#0x02c7-remove-character-enchantment-silent) |
| `0x02C8` | [Remove Multiple Character Enchantments (Silent)](game-events.md#0x02c8-remove-multiple-character-enchantments-silent) |
| `0x02C9` | [Mild Portal Storm](game-events.md#0x02c9-mild-portal-storm) |
| `0x02CA` | [Heavy Portal Storm](game-events.md#0x02ca-heavy-portal-storm) |
| `0x02CB` | [Portal Stormed](game-events.md#0x02cb-portal-stormed) |
| `0x02CC` | [End Portal Storm](game-events.md#0x02cc-end-portal-storm) |
| `0x02EB` | [Status Message](game-events.md#0x02eb-status-message) |

## Game action index (`F7B1`)

| Action | Name |
| --- | --- |
| `0x0005` | [Set Single Character Option](game-actions.md#0x0005-set-single-character-option) |
| `0x0010` | [Set AFK Message](game-actions.md#0x0010-set-afk-message) |
| `0x0019` | [Store Item](game-actions.md#0x0019-store-item) |
| `0x001A` | [Equip Item](game-actions.md#0x001a-equip-item) |
| `0x001B` | [Drop Item](game-actions.md#0x001b-drop-item) |
| `0x0036` | [Use Item](game-actions.md#0x0036-use-item) |
| `0x0044` | [Raise Vital](game-actions.md#0x0044-raise-vital) |
| `0x0045` | [Raise Attribute](game-actions.md#0x0045-raise-attribute) |
| `0x0046` | [Raise Skill](game-actions.md#0x0046-raise-skill) |
| `0x0047` | [Train Skill](game-actions.md#0x0047-train-skill) |
| `0x0048` | [Cast Spell](game-actions.md#0x0048-cast-spell) |
| `0x004A` | [Cast Spell on Object](game-actions.md#0x004a-cast-spell-on-object) |
| `0x00A1` | [Materialize](game-actions.md#0x00a1-materialize) |
| `0x00CD` | [Give Item](game-actions.md#0x00cd-give-item) |
| `0x019C` | [Make Shortcut](game-actions.md#0x019c-make-shortcut) |
| `0x019D` | [Remove Shortcut](game-actions.md#0x019d-remove-shortcut) |
| `0x01A1` | [Set Character Options](game-actions.md#0x01a1-set-character-options) |
| `0x01E3` | [Add Spell to Spellbar](game-actions.md#0x01e3-add-spell-to-spellbar) |
| `0x01E4` | [Remove Spell from Spellbar](game-actions.md#0x01e4-remove-spell-from-spellbar) |

## Type index

**Structs:** [AttributeData](struct-types.md#attributedata), [CharacterOptionData](struct-types.md#characteroptiondata), [CharacterPropertyData](struct-types.md#characterpropertydata), [CharacterVectorData](struct-types.md#charactervectordata), [DwellingACL](struct-types.md#dwellingacl), [DwellingItem](struct-types.md#dwellingitem), [Enchantment](struct-types.md#enchantment), [FellowInfo](struct-types.md#fellowinfo), [GameData](struct-types.md#gamedata), [GameData1a](struct-types.md#gamedata1a), [GameData1b](struct-types.md#gamedata1b), [GameData2a](struct-types.md#gamedata2a), [ModelData](struct-types.md#modeldata), [PhysicsData](struct-types.md#physicsdata), [Position](struct-types.md#position), [Position0](struct-types.md#position0), [SkillData](struct-types.md#skilldata), [VitalData](struct-types.md#vitaldata)

**Enums:** [AmmoType](enum-types.md#ammotype), [AnimationMask](enum-types.md#animationmask), [AnimationType](enum-types.md#animationtype), [ArmorHighlightMask](enum-types.md#armorhighlightmask), [AttrID](enum-types.md#attrid), [AttributeHighlightMask](enum-types.md#attributehighlightmask), [BooleanPropertyID](enum-types.md#booleanpropertyid), [CharacterOptions1](enum-types.md#characteroptions1), [CharacterOptions2](enum-types.md#characteroptions2), [ChatDisplayMask](enum-types.md#chatdisplaymask), [ChatFilterMask](enum-types.md#chatfiltermask), [ChatMessageType](enum-types.md#chatmessagetype), [CompressionType](enum-types.md#compressiontype), [ConfirmationType](enum-types.md#confirmationtype), [CoverageMask](enum-types.md#coveragemask), [CurVitalID](enum-types.md#curvitalid), [DWORDPropertyID](enum-types.md#dwordpropertyid), [DamageLocation](enum-types.md#damagelocation), [DamageType](enum-types.md#damagetype), [DoublePropertyID](enum-types.md#doublepropertyid), [Effect](enum-types.md#effect), [EndTradeReason](enum-types.md#endtradereason), [EquipMask](enum-types.md#equipmask), [FriendsUpdateType](enum-types.md#friendsupdatetype), [GameAction](enum-types.md#gameaction), [GameEvent](enum-types.md#gameevent), [GroupChatType](enum-types.md#groupchattype), [HookType](enum-types.md#hooktype), [IconHighlight](enum-types.md#iconhighlight), [ItemType](enum-types.md#itemtype), [LinkPropertyID](enum-types.md#linkpropertyid), [MaterialType](enum-types.md#materialtype), [ObjectBehaviorFlags](enum-types.md#objectbehaviorflags), [ObjectCategoryFlags](enum-types.md#objectcategoryflags), [OptionPropertyID](enum-types.md#optionpropertyid), [PositionFlags](enum-types.md#positionflags), [PositionPropertyID](enum-types.md#positionpropertyid), [PropertyType](enum-types.md#propertytype), [QWORDPropertyID](enum-types.md#qwordpropertyid), [ResourcePropertyID](enum-types.md#resourcepropertyid), [ResourceType](enum-types.md#resourcetype), [SkillID](enum-types.md#skillid), [SkillState](enum-types.md#skillstate), [SourceType](enum-types.md#sourcetype), [StanceMode](enum-types.md#stancemode), [StatusMessageType1](enum-types.md#statusmessagetype1), [StatusMessageType2](enum-types.md#statusmessagetype2), [StringPropertyID](enum-types.md#stringpropertyid), [TurbineChatType](enum-types.md#turbinechattype), [VitalID](enum-types.md#vitalid), [WandHighlightMask](enum-types.md#wandhighlightmask), [WeaponHighlightMask](enum-types.md#weaponhighlightmask), [WieldType](enum-types.md#wieldtype)
