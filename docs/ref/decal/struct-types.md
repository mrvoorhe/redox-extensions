# Struct Types

> Generated from the DecalDev AC protocol reference at <https://skunkworks.sourceforge.net/protocol/Protocol.php> (version 2006.08.22.1).

Reusable data structures referenced by protocol messages and by other structures.

## AttributeData

The AttributeData structure contains information about a character primary.

- **raised** `DWORD` — points raised
- **innate** `DWORD` — innate points
- **xp** `DWORD` — XP spent on this attribute

## CharacterOptionData

The CharacterOptionData structure contains character options.

- **flags** `DWORD`
- **options** [`CharacterOptions1`](enum-types.md#characteroptions1) — The options in the Character tab (F11 by default)
- _Choose valid sections by masking against flags:_
  - **0x00000001**:
    - **shortcutCount** `DWORD` — Number of shortcut items.
    - **position** `DWORD` — Position
    - **target** `ObjectID` — Object ID
    - **unknown3** `DWORD`
- **tab1Count** `DWORD` — Number of spells in the first spelltab.
- **spell** `DWORD` — The Spell's ID
- _Choose valid sections by masking against flags:_
  - **0x00000010**:
    - **tab2Count** `DWORD` — Number of spells in the second spelltab.
    - **spell** `DWORD` — The Spell's ID
    - **tab3Count** `DWORD` — Number of spells in the third spelltab.
    - **spell** `DWORD` — The Spell's ID
    - **tab4Count** `DWORD` — Number of spells in the fourth spelltab.
    - **spell** `DWORD` — The Spell's ID
    - **tab5Count** `DWORD` — Number of spells in the fifth spelltab.
    - **spell** `DWORD` — The Spell's ID
    - **tab6Count** `DWORD` — Number of spells in the fifth spelltab.
    - **spell** `DWORD` — The Spell's ID
    - **tab7Count** `DWORD` — Number of spells in the fifth spelltab.
    - **spell** `DWORD` — The Spell's ID
  - **0x00000008**:
    - **fillcompsCount** `WORD` — Number of components being tracked
    - **fillcompsUnknown** `WORD`
    - **component** `DWORD` — Component ID
    - **count** `DWORD` — Number of this component for compbuyer to refill to
  - **0x00000020**:
    - **unk20mask3** `DWORD` — Unknown mask value
  - **0x00000040**:
    - **optionFlags** [`CharacterOptions2`](enum-types.md#characteroptions2) — Character options
  - **0x00000100**:
    - **unknown100_1** `DWORD`
    - **optionStringCount** `WORD`
    - **optionStringUnknown** `WORD`
    - **key** `DWORD`
    - **value** `String`
  - **0x00000200**:
    - **unknown200_1** `DWORD`
    - **unknown200_2** `BYTE`
    - **optionPropertyCount** `BYTE`
    - **type** [`PropertyType`](enum-types.md#propertytype)
    - _Select one section based on the value of type:_
      - **0x1000008c**:
        - _window property lists for each window_
        - **unknown** `DWORD`
        - **windowCount** `DWORD`
        - **windows: vector of length windowCount**
          - **type** [`PropertyType`](enum-types.md#propertytype)
          - _Select one section based on the value of type:_
            - **0x1000008b**:
              - _a window property list_
              - **unknown** `BYTE`
              - **propertyCount** `BYTE`
              - **key** [`PropertyType`](enum-types.md#propertytype)
              - _Select one section based on the value of key:_
                - **0x1000008d**:
                  - _chat window title_
                  - **unknown** `DWORD`
                  - **titleSource** [`SourceType`](enum-types.md#sourcetype)
                  - _Select one section based on the value of titleSource:_
                    - **0x00**:
                      - **stringID** `DWORD`
                      - **fileID** `DWORD`
                    - **0x01**:
                      - **value** `WString`
                  - **unknown_1b** `DWORD`
                  - **unknown_1c** `WORD`
                - **0x1000008a**:
                  - _chat window enabled_
                  - **unknown** `DWORD`
                  - **value** `BYTE`
                - **0x10000089**:
                  - _chat window size (y)_
                  - **unknown** `DWORD`
                  - **value** `DWORD`
                - **0x10000088**:
                  - _chat window size (x)_
                  - **unknown** `DWORD`
                  - **value** `DWORD`
                - **0x10000087**:
                  - _chat window position (y)_
                  - **unknown** `DWORD`
                  - **value** `DWORD`
                - **0x10000086**:
                  - _chat window position (x)_
                  - **unknown** `DWORD`
                  - **value** `DWORD`
                - **0x1000007F**:
                  - _chat window display mask_
                  - **unknown** `DWORD`
                  - **value** [`ChatDisplayMask`](enum-types.md#chatdisplaymask)
      - **0x10000081**:
        - _active window opacity_
        - **unknown** `DWORD`
        - **activeOpacity** `float`
      - **0x10000080**:
        - _inactive window opacity_
        - **unknown** `DWORD`
        - **inactiveOpacity** `float`

## CharacterPropertyData

The CharacterPropertyData structure contains character properties.

- **flags** `DWORD` — determines which property types appear in the message
- **unknown1** `DWORD` — Unknown - always 0x0A
- _Choose valid sections by masking against flags:_
  - **0x00000001**:
    - **dwordCount** `WORD` — number of DWORD properties
    - **dwordUnknown** `WORD` — unknown
    - **key** [`DWORDPropertyID`](enum-types.md#dwordpropertyid) — the property ID
    - **value** `DWORD` — the value
  - **0x00000080**:
    - **qwordCount** `WORD` — number of QWORD properties
    - **qwordUnknown** `WORD` — unknown
    - **key** [`QWORDPropertyID`](enum-types.md#qwordpropertyid) — the property ID
    - **value** `QWORD` — the value
  - **0x00000002**:
    - **booleanCount** `WORD` — number of Boolean properties
    - **booleanUnknown** `WORD` — unknown
    - **key** [`BooleanPropertyID`](enum-types.md#booleanpropertyid) — the property ID
    - **value** `Boolean` — Boolean property value (0=False, 1=True)
  - **0x00000004**:
    - **doubleCount** `WORD` — number of Double properties
    - **doubleUnknown** `WORD` — unknown
    - **key** [`DoublePropertyID`](enum-types.md#doublepropertyid) — the property ID
    - **value** `double` — the value
  - **0x00000010**:
    - **stringCount** `WORD` — number of String properties
    - **stringUnknown** `WORD` — unknown
    - **key** [`StringPropertyID`](enum-types.md#stringpropertyid) — the property ID
    - **value** `String` — the value
  - **0x00000040**:
    - **resourceCount** `WORD` — number of Resource properties
    - **resourceUnknown** `WORD` — unknown
    - **key** [`ResourcePropertyID`](enum-types.md#resourcepropertyid) — the property ID
    - **value** `ResourceID` — the value
  - **0x00000008**:
    - **linkCount** `WORD` — number of Link properties
    - **linkUnknown** `WORD` — unknown
    - **key** [`LinkPropertyID`](enum-types.md#linkpropertyid) — the property ID
    - **value** `ObjectID` — the value
  - **0x00000020**:
    - **positionCount** `WORD` — number of Position properties
    - **positionUnknown** `WORD` — unknown
    - **key** [`PositionPropertyID`](enum-types.md#positionpropertyid) — the property ID
    - **value** [`Position0`](struct-types.md#position0) — the value

## CharacterVectorData

The CharacterVectorData structure contains character property lists.

- **flags** `DWORD` — determines which property vector types appear in the message
- **unknown2** `DWORD` — Unknown - always 1.
- _Choose valid sections by masking against flags:_
  - **0x00000001**:
    - **attributeFlags** `DWORD` — The attributes included in the character description - this is always 0x1FF
    - _Choose valid sections by masking against attributeFlags:_
      - **0x00000001**:
        - **strength** [`AttributeData`](struct-types.md#attributedata) — strength attribute information
      - **0x00000002**:
        - **endurance** [`AttributeData`](struct-types.md#attributedata) — endurance attribute information
      - **0x00000004**:
        - **quickness** [`AttributeData`](struct-types.md#attributedata) — quickness attribute information
      - **0x00000008**:
        - **coordination** [`AttributeData`](struct-types.md#attributedata) — coordination attribute information
      - **0x00000010**:
        - **focus** [`AttributeData`](struct-types.md#attributedata) — focus attribute information
      - **0x00000020**:
        - **self** [`AttributeData`](struct-types.md#attributedata) — self attribute information
      - **0x00000040**:
        - **health** [`VitalData`](struct-types.md#vitaldata) — health vital information
      - **0x00000080**:
        - **stamina** [`VitalData`](struct-types.md#vitaldata) — stamina vital information
      - **0x00000100**:
        - **mana** [`VitalData`](struct-types.md#vitaldata) — mana vital information
  - **0x00000002**:
    - **skillCount** `WORD`
    - **skillUnknown** `WORD`
    - **key** [`SkillID`](enum-types.md#skillid) — skill ID
    - **value** [`SkillData`](struct-types.md#skilldata) — skill information
  - **0x00000100**:
    - **spellbookCount** `WORD` — The number of spells in your Spellbook
    - **spellbookUnknown** `WORD` — Probably indicates what type of data is in this section
    - **spell** `DWORD` — The Spell ID.
    - **charge** `float` — The spell's charge. Ranges from 0.0 to 1.0
  - **0x00000200**:
    - **enchantmentMask** `DWORD` — Enchantment mask.
    - _Choose valid sections by masking against enchantmentMask:_
      - **0x0001**:
        - **lifeSpellCount** `DWORD` — Number of Life Magic enchantments in effect.
        - **enchantment** [`Enchantment`](struct-types.md#enchantment) — Information about the spell.
      - **0x0002**:
        - **creatureSpellCount** `DWORD` — Number of Creature Magic enchantments in effect.
        - **enchantment** [`Enchantment`](struct-types.md#enchantment) — Information about the spell.
      - **0x0004**:
        - **vitae** [`Enchantment`](struct-types.md#enchantment) — Vitae Penalty.

## DwellingACL

The DwellingACL contains the access control list for a dwelling object.

- **flags** `DWORD` — believed to be flags that control the size and content of this structure
- **open** `DWORD` — 0 = private dwelling, 1 = open to public
- **allegiance** `ObjectID` — allegiance monarch (if allegiance access granted)
- **guestCount** `WORD` — number of guests on list
- **guestLimit** `WORD` — Maximum number of guests on guest list (cottage is 32)
- **guest** `ObjectID` — the ID of the guest
- **storage** `Boolean` — 0 = dwelling access only, 1 = storage access also

## DwellingItem

The DwellingItem structure contains information about a dwelling purchas or maintenance item.

- **quantityRequired** `DWORD` — the quantity required
- **quantityPaid** `DWORD` — the quantity paid
- **type** `DWORD` — the item's object type
- **name** `String` — the name of this item
- **pluralName** `String` — the plural name of this item (if not specified, use <name> followed by 's' or 'es')

## Enchantment

The Enchantment structure describes an active enchantment.

- **spell** `SpellID` — the spell ID of the enchantment
- **layer** `WORD` — identifies the specific enchantment, if the same spell is applied more than once
- **family** `WORD` — the family of related spells this enchantment belongs to
- **unknown0** `WORD` — unknown
- **difficulty** `DWORD` — the difficulty of the spell
- **elapsedTime** `double` — the amount of time this enchantment has been active
- **duration** `double` — the duration of the spell
- **caster** `ObjectID` — the object ID of the creature or item that cast this enchantment
- **unknown1** `DWORD` — unknown
- **unknown2** `DWORD` — unknown
- **startTime** `double` — the time when this enchantment was cast
- **flags** `DWORD` — flags that indicate the type of effect the spell has
- **key** `DWORD` — along with flags, indicates which attribute is affected by the spell
- **value** `float` — the effect value/amount
- **unknown3** `DWORD` — unknown

## FellowInfo

The FellowInfo structure contains information about a fellowship member.

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

## GameData

The GameData structure defines an object's game behavior.

- **flags1** `DWORD` — game data flags
- **name** `String` — object name
- **type** `PackedDWORD` — object type
- **icon** `PackedDWORD` — icon ResourceID (minus 0x06000000)
- **category** [`ObjectCategoryFlags`](enum-types.md#objectcategoryflags) — object categories
- **behavior** [`ObjectBehaviorFlags`](enum-types.md#objectbehaviorflags) — object behaviors
- _Choose valid sections by masking against behavior:_
  - **0x04000000**:
    - **flags2** `DWORD` — additional game data flags
- _Choose valid sections by masking against flags1:_
  - **0x00000001**:
    - **namePlural** `String` — plural object name (if not specified, use <name> followed by 's' or 'es')
  - **0x00000002**:
    - **itemSlots** `BYTE` — number of item slots
  - **0x00000004**:
    - **packSlots** `BYTE` — number of pack slots (a pack slot is a slot that may hold a pack or a foci)
  - **0x00000100**:
    - **ammunition** [`AmmoType`](enum-types.md#ammotype) — missile ammunition type
  - **0x00000008**:
    - **value** `DWORD` — object value
  - **0x00000010**:
    - **unknown10** `DWORD`
  - **0x00000020**:
    - **approachDistance** `float` — distance a player will walk to pick up the object
  - **0x00080000**:
    - **usableOn** `DWORD` — the object categories this object may be used on
  - **0x00000080**:
    - **iconHighlight** [`IconHighlight`](enum-types.md#iconhighlight) — the type of highlight (outline) applied to the object's icon
  - **0x00000200**:
    - **wieldType** [`WieldType`](enum-types.md#wieldtype) — the type of wieldable item this is
  - **0x00000400**:
    - **uses** `WORD` — the number of uses remaining for this item (also salvage quantity)
  - **0x00000800**:
    - **usesLimit** `WORD` — the maximum number of uses possible for this item (also maximum salvage quantity)
  - **0x00001000**:
    - **stack** `WORD` — the number of items in this stack of objects
  - **0x00002000**:
    - **stackLimit** `WORD` — the maximum number of items possible in this stack of objects
  - **0x00004000**:
    - **container** `ObjectID` — the ID of the container holding this object
  - **0x00008000**:
    - **equipper** `ObjectID` — the ID of the creature equipping this object
  - **0x00010000**:
    - **equipPossible** [`EquipMask`](enum-types.md#equipmask) — the potential equipment slots this object may be placed in
  - **0x00020000**:
    - **equipActual** [`EquipMask`](enum-types.md#equipmask) — the actual equipment slots this object is currently placed in
  - **0x00040000**:
    - **coverage** [`CoverageMask`](enum-types.md#coveragemask) — the parts of the body this object protects
  - **0x00100000**:
    - **unknown100000** `BYTE`
  - **0x00800000**:
    - **unknown800000** `BYTE`
  - **0x08000000**:
    - **unknown8000000** `WORD`
  - **0x01000000**:
    - **workmanship** `float` — object workmanship
  - **0x00200000**:
    - **burden** `WORD` — total burden of this object
  - **0x00400000**:
    - **spell** `SpellID` — the spell cast by this object
  - **0x02000000**:
    - **owner** `ObjectID` — the owner of this object
  - **0x04000000**:
    - **acl** [`DwellingACL`](struct-types.md#dwellingacl) — the access control list for this dwelling object
  - **0x20000000**:
    - **hookTypeUnknown** `WORD` — always -1
    - **hookType** [`HookType`](enum-types.md#hooktype) — what type of dwelling hook is this
  - **0x00000040**:
    - **monarch** `ObjectID` — this player's monarch
  - **0x10000000**:
    - **hookableOn** [`HookType`](enum-types.md#hooktype) — the types of hooks this object may be placed on (-1 for hooks)
  - **0x40000000**:
    - **iconOverlay** `PackedDWORD` — icon overlay ResourceID (minus 0x06000000)
- _Choose valid sections by masking against behavior:_
  - **0x04000000**:
    - **iconUnderlay** `PackedDWORD` — icon underlay ResourceID (minus 0x06000000)
- _Choose valid sections by masking against flags1:_
  - **0x80000000**:
    - **material** [`MaterialType`](enum-types.md#materialtype) — the type of material this object is made of

## GameData1a

- _Choose valid sections by masking against flags1:_
  - **0x00000001**:
    - **namePlural** `String` — plural object name (if not specified, use <name> followed by 's' or 'es')
  - **0x00000002**:
    - **itemSlots** `BYTE` — number of item slots
  - **0x00000004**:
    - **packSlots** `BYTE` — number of pack slots (a pack slot is a slot that may hold a pack or a foci)
  - **0x00000100**:
    - **ammunition** [`AmmoType`](enum-types.md#ammotype) — missile ammunition type
  - **0x00000008**:
    - **value** `DWORD` — object value
  - **0x00000010**:
    - **unknown10** `DWORD`
  - **0x00000020**:
    - **approachDistance** `float` — distance a player will walk to pick up the object
  - **0x00080000**:
    - **usableOn** `DWORD` — the object categories this object may be used on
  - **0x00000080**:
    - **iconHighlight** [`IconHighlight`](enum-types.md#iconhighlight) — the type of highlight (outline) applied to the object's icon
  - **0x00000200**:
    - **wieldType** [`WieldType`](enum-types.md#wieldtype) — the type of wieldable item this is
  - **0x00000400**:
    - **uses** `WORD` — the number of uses remaining for this item (also salvage quantity)
  - **0x00000800**:
    - **usesLimit** `WORD` — the maximum number of uses possible for this item (also maximum salvage quantity)
  - **0x00001000**:
    - **stack** `WORD` — the number of items in this stack of objects
  - **0x00002000**:
    - **stackLimit** `WORD` — the maximum number of items possible in this stack of objects
  - **0x00004000**:
    - **container** `ObjectID` — the ID of the container holding this object
  - **0x00008000**:
    - **equipper** `ObjectID` — the ID of the creature equipping this object
  - **0x00010000**:
    - **equipPossible** [`EquipMask`](enum-types.md#equipmask) — the potential equipment slots this object may be placed in
  - **0x00020000**:
    - **equipActual** [`EquipMask`](enum-types.md#equipmask) — the actual equipment slots this object is currently placed in
  - **0x00040000**:
    - **coverage** [`CoverageMask`](enum-types.md#coveragemask) — the parts of the body this object protects
  - **0x00100000**:
    - **unknown100000** `BYTE`
  - **0x00800000**:
    - **unknown800000** `BYTE`
  - **0x08000000**:
    - **unknown8000000** `WORD`
  - **0x01000000**:
    - **workmanship** `float` — object workmanship
  - **0x00200000**:
    - **burden** `WORD` — total burden of this object
  - **0x00400000**:
    - **spell** `SpellID` — the spell cast by this object
  - **0x02000000**:
    - **owner** `ObjectID` — the owner of this object
  - **0x04000000**:
    - **acl** [`DwellingACL`](struct-types.md#dwellingacl) — the access control list for this dwelling object
  - **0x20000000**:
    - **hookTypeUnknown** `WORD` — always -1
    - **hookType** [`HookType`](enum-types.md#hooktype) — what type of dwelling hook is this
  - **0x00000040**:
    - **monarch** `ObjectID` — this player's monarch
  - **0x10000000**:
    - **hookableOn** [`HookType`](enum-types.md#hooktype) — the types of hooks this object may be placed on (-1 for hooks)
  - **0x40000000**:
    - **iconOverlay** `PackedDWORD` — icon overlay ResourceID (minus 0x06000000)

## GameData1b

- _Choose valid sections by masking against flags1:_
  - **0x80000000**:
    - **material** [`MaterialType`](enum-types.md#materialtype) — the type of material this object is made of

## GameData2a

- _Choose valid sections by masking against behavior:_
  - **0x04000000**:
    - **iconUnderlay** `PackedDWORD` — icon underlay ResourceID (minus 0x06000000)

## ModelData

The ModelData structure defines an object's visual appearance.

- **eleven** `BYTE` — always 0x11
- **paletteCount** `BYTE` — the number of palettes associated with this object
- **textureCount** `BYTE` — the number of textures associated with this object
- **modelCount** `BYTE` — the number of models associated with this object
- _Choose valid sections by masking against paletteCount:_
  - **0xFF**:
    - **palette** `PackedDWORD` — palette ResourceID (minus 0x04000000)
- **palette** `PackedDWORD` — palette ResourceID (minus 0x04000000)
- **offset** `BYTE` — The number of palette entries to skip
- **length** `BYTE` — The number of palette entries to copy
- **index** `BYTE` — the index of the model we are replacing the texture in
- **old** `PackedDWORD` — texture ResourceID (minus 0x05000000)
- **new** `PackedDWORD` — texture ResourceID (minus 0x05000000)
- **index** `BYTE` — The index of the model
- **model** `PackedDWORD` — model ResourceID (minus 0x01000000)

## PhysicsData

The PhysicsData structure defines an object's physical behavior.

- **flags** `DWORD` — physics data flags
- **unknown** `DWORD`
- _Choose valid sections by masking against flags:_
  - **0x00010000**:
    - **byteCount** `DWORD` — the number of BYTEs that follow
    - **byte** `BYTE`
    - **unknown10000** `DWORD`
  - **0x00020000**:
    - **unknown20000** `DWORD`
  - **0x00008000**:
    - **position** [`Position0`](struct-types.md#position0) — object position
  - **0x00000002**:
    - **animations** `ResourceID` — animation set ResourceID
  - **0x00000800**:
    - **sounds** `ResourceID` — sound set ResourceID
  - **0x00001000**:
    - **unknown1000** `ResourceID` — unknown ResourceID
  - **0x00000001**:
    - **model** `ResourceID` — model ResourceID
  - **0x00000020**:
    - **equipper** `ObjectID` — the creature equipping this object
    - **equipperSlot** [`EquipMask`](enum-types.md#equipmask) — the slot in which this object is equipped
  - **0x00000040**:
    - **equippedCount** `DWORD` — the number of items equipped by this creature
    - **item** `ObjectID`
    - **slot** [`EquipMask`](enum-types.md#equipmask)
  - **0x00000080**:
    - **scale** `float` — the size of this object
  - **0x00000100**:
    - **unknown100** `DWORD`
  - **0x00000200**:
    - **unknown200** `DWORD`
  - **0x00040000**:
    - **unknown40000** `float`
  - **0x00000004**:
    - **dx** `float` — velocity vector x component
    - **dy** `float` — velocity vector y component
    - **dz** `float` — velocity vector z component
  - **0x00000008**:
    - **unknown8_1** `float`
    - **unknown8_2** `float`
    - **unknown8_3** `float`
  - **0x00000010**:
    - **rx** `float` — rotation vector x component
    - **ry** `float` — rotation vector y component
    - **rz** `float` — rotation vector z component
  - **0x00002000**:
    - **unknown2000** `DWORD`
  - **0x00004000**:
    - **unknown4000** `DWORD`
- **unknown1** `WORD`
- **unknown2** `WORD`
- **unknown3** `WORD`
- **unknown4** `WORD`
- **unknown5** `WORD`
- **unknown6** `WORD`
- **unknown7** `WORD`
- **unknown8** `WORD`
- **unknown9** `WORD`

## Position

The Position structure defines an object's position, orientation and motion.

- **flags** [`PositionFlags`](enum-types.md#positionflags)
- **landcell** `DWORD` — the landcell in which the object is located
- **displacement: a vector describing the object's position within the landblock containing the landcell**
  - **x** `float`
  - **y** `float`
  - **z** `float`
- **orientation: a quaternion describing the object's orientation**
  - _Choose valid sections by masking against flags xor 0x00000078:_
    - **0x00000008**:
      - **wQuat** `float`
    - **0x00000010**:
      - **xQuat** `float`
    - **0x00000020**:
      - **yQuat** `float`
    - **0x00000040**:
      - **zQuat** `float`
- _Choose valid sections by masking against flags:_
  - **0x00000001**:
    - **velocity: a vector describing the object's velocity**
      - **dx** `float`
      - **dy** `float`
      - **dz** `float`
  - **0x000002**:
    - **unknown** `DWORD`

## Position0

A Position structure with an implied flags value of 0.

- **landcell** `DWORD` — the landcell in which the object is located
- **displacement: a vector describing the object's position within the landblock containing the landcell**
  - **x** `float`
  - **y** `float`
  - **z** `float`
- **orientation: a quaternion describing the object's orientation**
  - **wQuat** `float`
  - **xQuat** `float`
  - **yQuat** `float`
  - **zQuat** `float`

## SkillData

The SkillData structure contains information about a character skill.

- **raised** `WORD` — points raised
- **unknown1** `WORD`
- **state** [`SkillState`](enum-types.md#skillstate) — skill state
- **xp** `DWORD` — XP spent on this skill
- **bonus** `DWORD` — bonus points applied to this skill
- **diff** `DWORD` — task difficulty
- **unknown2** `double`

## VitalData

The VitalData structure contains information about a character vital.

- **raised** `DWORD` — points raised
- **unknown** `DWORD`
- **xp** `DWORD` — XP spent on this attribute
- **current** `DWORD` — current value
