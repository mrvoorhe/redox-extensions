# Virindi Tank Advanced Options

> Source: <http://www.virindi.net/wiki/index.php/Virindi_Tank_Advanced_Options> (wiki revision 1615, last modified 2017-07-09; retrieved 2026-10-20). Content available under the GNU Free Documentation License 1.2.

The Advanced Options tweak specific macro behaviors. Most can be read and changed in-game with `/vt opt` (see [vtank-commands.md](vtank-commands.md#profiles)).

## Usage

```
/vt opt list
/vt opt get [option]
/vt opt set [option] [value]
/vt opt setinall [option] [value]
```

- Do not type the brackets. Examples: `/vt opt set DoJiggle true`, `/vt opt set AttackDistance 0.0208333`.
- `setinall` changes the option in ALL profiles.
- **Range units:** the options UI shows ranges in yards, but `/vt opt` takes `yards / 240`. A range of 5 yards is `0.0208333`. Options measured this way are marked _(range)_ below.
- Several options appear on more than one tab in the in-game Advanced Options panel. They are the same setting; changing it in one place changes it everywhere. When viewing all tabs at once, duplicates are hidden. This reference repeats them under each tab, matching the UI.

### Value notation

- `true/false` — boolean.
- `##` — a number.
- `1/2/3` etc. — an enumerated value; meanings are listed in the description.

## Misc

| Option | Value | Description |
| --- | --- | --- |
| `AutoCram` | `true/false` | Moves items from the main pack to side packs. |
| `AutoStack` | `true/false` | Stacks items towards the bottom-most item in the pack, and into side packs. The partial stack of an item is always the top-most one in inventory, which is the order the game burns components. Avoids stacking after every spell cast and prevents stacking from pulling items out of side packs. |
| `RandomHelperBuffs` | `true/false` | Casts random buffs on nearby players. |
| `RandomHelperIntervalSeconds` | `##` | Timer for `RandomHelperBuffs`, in seconds. |
| `IdlePeaceMode` | `true/false` | Loots, navigates, and salvages in peace mode. |
| `StopMacroOnDeath` | `true/false` | Disables the macro when the character dies. |
| `RefillWornMana` | `true/false` | Uses the mana charge types listed in the Consumables panel to refill worn items. |
| `RefillWornMana-Item-ManaPercent` | `##` | Minimum percentage of mana left on an item at which to refill it. |
| `ManaStoneLootCount` | `##` | Maximum number of usable mana stones to keep in inventory when looting. |
| `ManaTankMinimumMana` | `##` | Minimum mana an item must have to be used to fill mana stones. |
| `ManaChargesWhenOff` | `true/false` | Keeps equipped mana filled with charges/stones even while the macro is disabled. |
| `AutoFellowManagement` | `true/false` | Automatically recruits players on request, and manages waiting lists and votes when fellowship leader. See [Virindi Tank Fellowship Commands](http://www.virindi.net/wiki/index.php/Virindi_Tank_Fellowship_Commands). |
| `DeleteGhostMonsters` | `true/false` | Detects monsters that appear present but don't exist, and tells the client to delete them. |
| `GhostMonsterSpellAttemptCount` | `##` | Number of spell attempts on a monster, without the spell starting, before it is marked as a ghost. |
| `WhoYouGonnaCall` | `true/false` | No function. |
| `BlacklistMonsterAttemptCount` | `##` | Number of successful attacks that must miss a target before it is blacklisted. |
| `BlacklistMonsterTimeoutSeconds` | `##` | How long an unhittable monster stays blacklisted, in seconds. |
| `DeleteGhostMonstersByHPTracker` | `true/false` | Alternate ghost-monster deletion method that works for melee, missile, and magic attacks. |
| `GhostDeleteHPTrackerSeconds` | `##` | Seconds with no health updates for a target before it is considered a ghost. |
| `GoToPeaceModeToUseKits` | `true/false` | Drops to peace mode before using a healing kit. |
| `EnableMeta` | `true/false` | Enables the meta system. |
| `DropToPeaceModeRetryCount` | `##` | When switching weapons, how many times to retry dropping to peace mode before assuming the client is bugged and trying a wand. Default `34`. |
| `BlacklistCorpseOpenAttemptCount` | `##` | Number of attempts to open a corpse before it is blacklisted. |
| `BlacklistCorpseOpenTimeoutSeconds` | `##` | How long an unopenable corpse stays blacklisted, in seconds. |

## Recharge

| Option | Value | Description |
| --- | --- | --- |
| `Recharge-Norm-HitP` | `##` | Health percentage at which to heal during combat. |
| `Recharge-Norm-Stam` | `##` | Stamina percentage at which to restam during combat. |
| `Recharge-Norm-Mana` | `##` | Mana percentage at which to remana during combat. |
| `Recharge-NoTarg-HitP` | `##` | Health percentage to heal up to when idle. |
| `Recharge-NoTarg-Stam` | `##` | Stamina percentage to restam up to when idle. |
| `Recharge-NoTarg-Mana` | `##` | Mana percentage to remana up to when idle. |
| `Recharge-Helper-HitP` | `##` | Health percentage at which to heal a fellow. |
| `Recharge-Helper-Stam` | `##` | Stamina percentage at which to give stamina to a fellow. |
| `Recharge-Helper-Mana` | `##` | Mana percentage at which to give mana to a fellow. |
| `DoHelp` | `true/false` | Heals/restams/remanas allies. The fellowship window must be open to help fellows (or be in a [Virindi Integrator 2](http://www.virindi.net/wiki/index.php/Virindi_Integrator_2) fellowship). |
| `HelperDistanceHitP` | `##` _(range)_ | Maximum distance at which to heal fellows. |
| `HelperDistanceStam` | `##` _(range)_ | Maximum distance at which to restam fellows. |
| `HelperDistanceMana` | `##` _(range)_ | Maximum distance at which to give mana to fellows. |
| `CastDispelSelf` | `true/false` | Casts Dispel Life Magic Self VII when the character has a vuln on it. |
| `UseDispelItems` | `true/false` | Uses a Gem of Stillness or Condensed Dispel Potion when the character has a vuln on it. |
| `UseDispelDrum` | `true/false` | _(undocumented on wiki)_ |
| `UseHealersHeart` | `true/false` | Uses a Healer's Heart for helping, if one is in the item list. |
| `JumpOutWandCasting` | `true/false` | For Healer's Heart: jumps right after using the heart. The heal goes out immediately, but the character must stand idle until the timer is up. |
| `RechargeBoostTimeSeconds` | `##` | Maximum duration of the recharge boost effect. Boost makes the macro prefer recharging once it has switched to magic mode to recharge. |
| `RechargeBoostAmount` | `##` | Number of health/stamina/mana points used for recharge boost. |
| `MinimumHealKitSuccessChance` | `##` | Lowest heal-kit success chance, in percent, the macro will accept. |
| `UseKitsInMagicMode` | `true/false` | Uses healing kits while in magic mode, with higher priority than spells. |
| `StaminaToHealthMultiplier` | `##` | How much better a Stamina to Health spell must be than a normal heal before it is used. |
| `ManaToHealthMultiplier` | `##` | How much better a Mana to Health spell must be than a normal heal before it is used. |
| `GoToPeaceModeToUseKits` | `true/false` | Drops to peace mode before using a healing kit. |
| `ClearLevelBoostFlagOnCast` | `true/false` | Recharge boost applies only to the first spell cast after switching to magic mode. |
| `IdleCraftCount_HealthKits` | `##` | Health kits of each type to craft up to when idle. |
| `IdleCraftCount_StamKits` | `##` | Stamina kits of each type to craft up to when idle. |
| `IdleCraftCount_ManaKits` | `##` | Mana kits of each type to craft up to when idle. |
| `IdleCraftCount_HealthFood` | `##` | Health foods/potions of each type to craft up to when idle. |
| `IdleCraftCount_StamFood` | `##` | Stamina foods/potions of each type to craft up to when idle. |
| `IdleCraftCount_ManaFood` | `##` | Mana foods/potions of each type to craft up to when idle. |

## Melee Combat

| Option | Value | Description |
| --- | --- | --- |
| `EnableCombat` | `true/false` | Enables/disables combat. |
| `DefaultMeleeAttackHeight` | `1/2/3` | Default melee attack height. `1` = high, `2` = mid, `3` = low. |
| `TargetLock` | `true/false` | Attacks the selected monster first, if it is a valid target. Priority doesn't take over until it is dead. |
| `TargetSelectMethod` | `1/2/3` | How targets are preferred. `1` = by range, `2` = by angle, `3` = both (nearby targets by angle, distant targets by distance). |
| `TargetSelectAngleRange` | `##` _(range)_ | With `TargetSelectMethod` = both, targets beyond this range are selected by range. |
| `DebuffEachFirst` | `1/2/3` | Debuffs valid targets before attacking. `1` = One (debuff one monster, then attack it), `2` = Priority (debuff all monsters of the same priority, then attack), `3` = All (debuff all monsters regardless of priority, then attack). |
| `AutoAttackPower` | `true/false` | Sets melee attack power automatically based on weapon damage type. |
| `DebuffSelectionMethod` | `1/2` | What to prefer when choosing how to debuff. `1` = spell level, `2` = skill. Use skill against monsters with high magic defense. |
| `DeleteGhostMonstersByHPTracker` | `true/false` | Alternate ghost-monster deletion method that works for melee, missile, and magic attacks.¹ |
| `GhostDeleteHPTrackerSeconds` | `##` | Seconds with no health updates for a target before it is considered a ghost. |
| `UseRecklessness` | `true/false` | Attacks using Recklessness if available. |

## Spell Combat

| Option | Value | Description |
| --- | --- | --- |
| `EnableCombat` | `true/false` | Enables/disables combat. |
| `SpellDiffExcessThreshold-Hunt` | `##` | Adjusts the skill required to cast combat spells. Positive raises it, negative lowers it; use a negative number to attempt higher-level spells at low skill. See the [VTank FAQ](http://www.virindi.net/wiki/index.php/Virindi_Tank_FAQ#How_does_Virindi_Tank_know_what_level_of_spells_to_use,_and_how_can_I_change_it?). |
| `RingDistance` | `##` _(range)_ | Distance within which targets count toward ring spells. |
| `MinimumRingTargets` | `##` | Minimum number of targets within `RingDistance` before casting ring spells. |
| `SwitchWandToDebuff` | `true/false` | Switches to the monster's configured wand even just to debuff it. Otherwise wands are only switched when a war attack is needed. |
| `JumpOutWandCasting` | `true/false` | For Healer's Heart: jumps right after using the heart. See [Recharge](#recharge). |
| `DoJiggle` | `true/false` | "Secret spell dodge mode." |
| `TargetLock` | `true/false` | Attacks the selected monster first, if it is a valid target. Priority doesn't take over until it is dead. |
| `UseArcs` | `true/false` | Uses arc war spells instead of bolts when both are known at the best castable level. |
| `ArcRange` | `##` _(range)_ | Minimum range for casting arc spells. |
| `TargetSelectMethod` | `1/2/3` | How targets are preferred. `1` = by range, `2` = by angle, `3` = both. |
| `TargetSelectAngleRange` | `##` _(range)_ | With `TargetSelectMethod` = both, targets beyond this range are selected by range. |
| `DebuffEachFirst` | `1/2/3` | Debuff ordering. `1` = One, `2` = Priority, `3` = All. See [Melee Combat](#melee-combat). |
| `DebuffSelectionMethod` | `1/2` | `1` = spell level, `2` = skill. |
| `DeleteGhostMonstersByHPTracker` | `true/false` | Alternate ghost-monster deletion method that works for melee, missile, and magic attacks. |
| `GhostDeleteHPTrackerSeconds` | `##` | Seconds with no health updates for a target before it is considered a ghost. |
| `DebuffPrecastSeconds` | `##` | Seconds before a target's debuff expires that the macro will recast it. Does not apply to DoT spells. |
| `DropToPeaceModeRetryCount` | `##` | Peace-mode retries before trying a wand. Default `34`. See [Misc](#misc). |
| `SummonPets` | `true/false` | Summons pets if available and listed in items. |
| `PetRangeMode` | `0/1` | Which monster range is used when summoning a combat pet. `0` = `AttackDistance`, `1` = `PetCustomRange`. |
| `PetCustomRange` | `##` _(range)_ | Custom pet range, used when `PetRangeMode` = `1`. |
| `PetRefillCount-Idle` | `##` | Refill a pet summon item when idle if it has this many charges or fewer. |
| `PetRefillCount-Normal` | `##` | Refill a pet summon item in combat if it has this many charges or fewer. |
| `PetMonsterDensity` | `##` | Minimum number of monsters (configured for summon) within pet range before a pet is summoned. |

## Ranges

All options on this tab are ranges; use `yards / 240` with `/vt opt`.

| Option | Description |
| --- | --- |
| `AttackDistance` | Maximum attack distance. Controlled by Monster Range on the [Standard Options](http://www.virindi.net/wiki/index.php/Virindi_Tank_Standard_Options). |
| `AttackMinimumDistance` | Minimum attack distance. Default `0`. |
| `ApproachDistance` | Maximum approach distance. |
| `RingDistance` | Distance within which targets count toward ring spells. |
| `CorpseApproachRange-Max` | Maximum approach distance for corpse looting. |
| `CorpseApproachRange-Min` | Minimum approach distance for corpse looting.² |
| `NavCloseStopRange` | Follow/nav minimum distance. |
| `NavFarStopRange` | Disables navigation if the macro runs too far off course. |
| `UsePortalDistance` | _(undocumented on wiki)_ |
| `HelperDistanceHitP` | Maximum distance at which to heal fellows. |
| `HelperDistanceStam` | Maximum distance at which to restam fellows. |
| `HelperDistanceMana` | Maximum distance at which to give mana to fellows. |
| `ArcRange` | Minimum range for casting arc spells. |
| `TargetSelectAngleRange` | With `TargetSelectMethod` = both, targets beyond this range are selected by range. |
| `DoorIDRange` | Minimum distance at which the macro will ID a door. |
| `DoorOpenRange` | Distance at which the macro attempts to open doors. |

## Navigation

| Option | Value | Description |
| --- | --- | --- |
| `EnableNav` | `true/false` | Enables/disables navigation. |
| `ApproachDistance` | `##` _(range)_ | Maximum approach distance. |
| `CorpseApproachRange-Max` | `##` _(range)_ | Maximum approach distance for corpse looting. |
| `CorpseApproachRange-Min` | `##` _(range)_ | Minimum approach distance for corpse looting. |
| `NavCloseStopRange` | `##` _(range)_ | Follow/nav minimum distance. |
| `NavFarStopRange` | `##` _(range)_ | Disables navigation if the macro runs too far off course. |
| `UsePortalDistance` | `##` _(range)_ | _(undocumented on wiki)_ |
| `CorpseCacheTimeoutMinutes` | `##` | Minutes after which a corpse is removed from the corpse cache. |
| `OpenDoors` | `true/false` | Opens doors within range. |
| `DoorIDRange` | `##` _(range)_ | Minimum distance at which the macro will ID a door. |
| `DoorLockpickDiffExcessThreshold` | `##` | Lockpick skill points above a door's pick difficulty required for the door to be considered pickable. |
| `NavPriorityBoost` | `true/false` | Navigates before attacking. |
| `FollowAroundCorners` | `true/false` | When following a character: `true` runs the leader's exact route, `false` runs directly at the leader. |

## Buffing

| Option | Value | Description |
| --- | --- | --- |
| `EnableBuffing` | `true/false` | Enables buffing. |
| `SpellDiffExcessThreshold-Buff` | `##` | Adjusts the skill required to cast buffs. See the [VTank FAQ](http://www.virindi.net/wiki/index.php/Virindi_Tank_FAQ#How_does_Virindi_Tank_know_what_level_of_spells_to_use,_and_how_can_I_change_it?). |
| `IdleBuffTopoff` | `true/false` | Recasts buffs when idle.² |
| `IdleBuffTopoffTimeSeconds` | `##` | Time left on a buff at which an idle top-off begins. |
| `RebuffTimeRemainingSeconds` | `##` | Time left on a buff at which to rebuff normally. |
| `BuffProfile_Prots` | _(profile)_ | Which self prots are cast. See the [VTank FAQ](http://www.virindi.net/wiki/index.php/Virindi_Tank_FAQ#How_do_I_choose_which_spells_are_used_to_buff.3F). |
| `BuffProfile_Banes` | _(profile)_ | Which self banes are cast. See the [VTank FAQ](http://www.virindi.net/wiki/index.php/Virindi_Tank_FAQ#How_do_I_choose_which_spells_are_used_to_buff.3F). |
| `BuffCastRecast_Seconds` | `##` | After casting a single buff, the rebuff time for every other buff is reduced by this many seconds, for `BuffCastRecastReset_Seconds`. |
| `BuffCastRecastReset_Seconds` | `##` | How long the `BuffCastRecast_Seconds` reduction lasts. |

## Crafting

| Option | Value | Description |
| --- | --- | --- |
| `ArrowheadFletchDiffExcessTheshold` | `##` | _(undocumented on wiki)_² |
| `AutoCraftItems` | `true/false` | Crafts known components into items listed in the Consumables panel when the character has none. E.g. makes blue kits from bobo comps if "Plentiful Healing Kit" is listed. |
| `SplitPeas` | `true/false` | Automatically splits peas. Add the components you want kept supplied to the Consumables panel. |
| `SpellCompMin-Critical` | `##` | Minimum count of each spell component before splitting a pea. Critical splitting is higher priority than healing. |
| `SpellCompMin-Normal` | `##` | Minimum count of each spell component before splitting a pea. Normal splitting is lower priority than normal buffing. |
| `SpellCompMin-Idle` | `##` | Minimum count of each spell component before splitting a pea. Only done when idle. |
| `UseSpecialAmmo` | `0/1/2/3` | Crafts and uses special ammo. `0` = none, `1` = Raider (Raider Lightning ammo), `2` = SCurrency (ammo from Colosseum, Graveyard, and Paradox-touched Olthoi Infested Area), `3` = both. |
| `DoorLockpickDiffExcessThreshold` | `##` | Lockpick skill points above a door's pick difficulty required for the door to be considered pickable. |
| `IdleCraftCount_HealthKits` | `##` | Health kits of each type to craft up to when idle. |
| `IdleCraftCount_StamKits` | `##` | Stamina kits of each type to craft up to when idle. |
| `IdleCraftCount_ManaKits` | `##` | Mana kits of each type to craft up to when idle. |
| `IdleCraftCount_HealthFood` | `##` | Health foods/potions of each type to craft up to when idle. |
| `IdleCraftCount_StamFood` | `##` | Stamina foods/potions of each type to craft up to when idle. |
| `IdleCraftCount_ManaFood` | `##` | Mana foods/potions of each type to craft up to when idle. |

## Looting

| Option | Value | Description |
| --- | --- | --- |
| `EnableLooting` | `true/false` | Enables/disables looting. |
| `ReadUnknownScrolls` | `true/false` | Loots and reads unknown scrolls (looting must be enabled). See the [VTank FAQ](http://www.virindi.net/wiki/index.php/Virindi_Tank_FAQ#Why_are_some_unknown_scrolls_looted_and_not_others?). |
| `LootAllCorpses` | `true/false` | Loots corpses killed by neither you nor your fellows. |
| `LootFellowCorpses` | `true/false` | Loots corpses killed by your fellows. |
| `LootPriorityBoost` | `true/false` | Loots corpses before attacking monsters. |
| `CorpseItemAppearanceTimeoutSeconds` | `##` | Seconds to wait for all items on a corpse to appear. |
| `CorpseItemIDTimeoutSeconds` | `##` | Seconds to wait for all needed items on a corpse to be ID'd. |
| `ManaStoneLootCount` | `##` | Maximum number of usable mana stones to keep in inventory when looting. |
| `ManaTankMinimumMana` | `##` | Minimum mana an item must have to be used to fill mana stones. |
| `CombineSalvage` | `true/false` | Combines salvage bags according to the ranges in the current loot profile (looting must be enabled). |
| `LootOnlyRareCorpses` | `true/false` | Loots only rare-generating corpses. |
| `CorpseOpenTimeoutSeconds` | `##` | Seconds after trying to open a corpse before retrying. |
| `CorpseLootItemMaxAttempts` | `##` | Attempts to loot an item before blacklisting it. |

## Notes

1. The wiki spells this `DeleteGhostMonsterByHPTracker` on the Melee Combat tab and `DeleteGhostMonstersByHPTracker` elsewhere; the latter is used here.
2. Spelling differs from or is suspect on the wiki: `CorpeApproachRange-Min` (Ranges tab) and `IdleBuffTOpoff` were normalized here; `ArrowheadFletchDiffExcessTheshold` is kept as written. Confirm exact names with `/vt opt list`.
