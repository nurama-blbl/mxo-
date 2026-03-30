using System;
using System.Collections.Generic;

namespace hds
{
    /// <summary>
    /// Combat damage and effect calculations based on the MxO combat data model.
    /// All formulas derived from client.dll combat resolution structure at 0x75DCE0+.
    ///
    /// The MxO interlock combat system uses SCORE-BASED tactic resolution:
    ///   - Each tactic has a TacticalValue score (from client.dll "TacticalValue:" at 0x756B00)
    ///   - Abilities add TacticBonus to the score
    ///   - TacticalTotalValue accumulates across rounds
    ///   - Targets have per-tactic vulnerability: DefenseVulnerability, PowerVulnerability,
    ///     SpeedVulnerability (from 0x75DE60+)
    ///   - Damage = rawDamage * Modifier - DamageAbsorbed
    ///
    /// Combat result fields (from client.dll debug strings at 0x75DD00):
    ///   DamageTaken, DamageAbsorbed, Modifier, CombatResult, OutcomeFlag,
    ///   IsAttacker, CombatRound, CombatTurn, Success, PurityLost, StabilityLost
    ///
    /// Property sources:
    ///   - StatModifier/MinValue + MaxValue: base damage range
    ///   - MinValueScaleLevel + MaxValueScaleLevel: per-level damage scaling
    ///   - AbilityInnerStrengthCost: flat IS cost (no level scaling per game data)
    ///   - ToughnessStatModifier: damage absorption (defense)
    ///   - DamageModifier: damage multiplier buff/debuff
    ///   - IsArmorPiercing: bypasses ToughnessStatModifier
    /// </summary>
    public static class CombatFormulas
    {
        private static Random rng = new Random();

        /// <summary>
        /// Calculate tactic damage modifier using the vulnerability system.
        /// From client.dll at 0x75DE60: DefenseVulnerability, PowerVulnerability, SpeedVulnerability.
        ///
        /// The attacker's tactic type is checked against the target's vulnerability for that tactic.
        /// Higher vulnerability = more damage taken. The vulnerability values are per-character stats
        /// modified by buffs, debuffs, fighting style, and equipment.
        /// </summary>
        public static float getTacticModifier(
            TacticType attackerTactic,
            float targetDefenseVuln,
            float targetPowerVuln,
            float targetSpeedVuln)
        {
            switch (attackerTactic)
            {
                case TacticType.Defense:
                case TacticType.Retaliate:
                    return 1.0f + targetDefenseVuln;
                case TacticType.Power:
                    return 1.0f + targetPowerVuln;
                case TacticType.Speed:
                case TacticType.Burst:
                    return 1.0f + targetSpeedVuln;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Calculate ability damage using the confirmed MxO damage formula.
        ///
        /// From client.dll combat resolution structure (0x75DD00):
        ///   rawDamage = random(scaledMin, scaledMax)
        ///   Modifier = tacticModifier * (1 + DamageModifier) + DamageMod
        ///   DamageAbsorbed = toughness (unless IsArmorPiercing)
        ///   DamageTaken = rawDamage * Modifier - DamageAbsorbed
        ///
        /// Level scaling (from gameobjects.pkb property schema):
        ///   scaledMin = MinValue + (MinValueScaleLevel * level)
        ///   scaledMax = MaxValue + (MaxValueScaleLevel * level)
        /// </summary>
        public static CombatResult calculateAbilityEffect(
            AbilityDef ability,
            uint casterLevel,
            TacticType casterTactic,
            float targetDefenseVuln,
            float targetPowerVuln,
            float targetSpeedVuln,
            float targetToughness,
            float damageModifier,
            float damageMod)
        {
            CombatResult result = new CombatResult();

            if (ability.getStatModifiers().Count == 0)
            {
                result.damageTaken = 0;
                return result;
            }

            StatModifierDef primaryMod = ability.getStatModifier(0);

            // Roll base value from min-max range with level scaling
            float scaledMin = ability.getScaledMinValue(casterLevel);
            float scaledMax = ability.getScaledMaxValue(casterLevel);
            float rawDamage = scaledMin + (float)(rng.NextDouble() * (scaledMax - scaledMin));

            // Apply tactic vulnerability modifier
            float tacticMod = getTacticModifier(casterTactic,
                targetDefenseVuln, targetPowerVuln, targetSpeedVuln);

            // Apply damage modifier buffs/debuffs
            float modifier = tacticMod * (1.0f + damageModifier) + damageMod;

            // Apply tactic bonus from ability definition
            float tacticBonus = ability.getTacticBonus();
            modifier += tacticBonus;

            // Calculate damage absorbed (toughness/armor)
            float damageAbsorbed = 0f;
            if (!primaryMod.getIsArmorPiercing() && targetToughness > 0)
            {
                damageAbsorbed = targetToughness;
            }

            // Final damage: rawDamage * Modifier - DamageAbsorbed
            int damageTaken = Math.Max(0, (int)(rawDamage * modifier - damageAbsorbed));

            result.rawDamage = rawDamage;
            result.modifier = modifier;
            result.damageAbsorbed = damageAbsorbed;
            result.damageTaken = damageTaken;
            result.damageType = primaryMod.getDamageType();
            result.isArmorPiercing = primaryMod.getIsArmorPiercing();
            result.threatGenerated = ability.getAggroThreatLevel() * ability.getAggroMultiplier();
            result.tacticBonus = tacticBonus;

            return result;
        }

        /// <summary>
        /// Get the inner strength cost for using an ability.
        /// From gameobjects.pkb: AbilityInnerStrengthCost is a flat value per ability.
        /// No per-level scaling exists in the property schema — cost is constant across levels.
        /// Modified at runtime by InnerstrengthCostModifierAbility and TacticISCostModifierAbility.
        /// </summary>
        public static float getISCost(AbilityDef ability)
        {
            return ability.getInnerStrengthCost();
        }

        /// <summary>
        /// Check if a player can use an ability based on their current state.
        /// Returns null if allowed, or an error string matching LTCLIENT_ABILITY_* codes
        /// from client.dll at 0x74E0E0-0x74E3D0.
        ///
        /// Validation order matches the string order in client.dll binary:
        ///   1. CANT_TARGET_SELF
        ///   2. NOT_STANDING
        ///   3. MUST_BE_INCOMBAT / MUST_BE_OUTSIDECOMBAT
        ///   4. NOT_A_FRIENDLY_TARGETED / NOT_AN_ENEMY_TARGETED
        ///   5. NOT_A_GROUPMEMBER_TARGETED
        ///   6. NOT_OBJECT_TARGETED
        ///   7. NOT_CAST_IN_INVENTORY
        ///   8. MISSING_PROGRAMLAUNCHER
        ///   9. WAITING_RECAST_TIMER
        ///   10. NOT_AN_DEAD_FRIENDLY/ENEMY_TARGETED
        ///   11. NOT_IN_RANGE
        ///   12. QUEUE_IS_FULL
        ///   13. INSUFFICIENT_INNER_STRENGTH
        ///   14. INSUFFICIENT_TACTICAL_POINTS
        /// </summary>
        public static string validateAbilityUse(
            AbilityDef ability,
            bool isInCombat,
            float currentIS,
            bool isStanding,
            FightingStyle currentStyle,
            TacticType currentTactic,
            bool hasTarget,
            bool targetIsSelf,
            bool targetIsEnemy,
            bool targetIsDead,
            bool targetIsGroupMember,
            bool hasProgramLauncher,
            bool isRecastReady,
            float targetDistance)
        {
            // 1. Self-target check
            if (targetIsSelf && (ability.getCastValidTarget() & ValidTarget.Self) == 0)
                return "CLIENT_ABILITY_CANT_TARGET_SELF";

            // 2. Standing check
            if (!isStanding)
                return "CLIENT_ABILITY_NOT_STANDING";

            // 3. Combat state requirements
            if (ability.getIsCombatExclusive() && !isInCombat)
                return "CLIENT_ABILITY_MUST_BE_INCOMBAT";

            if (ability.getNonCombatOnlyAbility() && isInCombat)
                return "CLIENT_ABILITY_MUST_BE_OUTSIDECOMBAT";

            // 4-5. Target type checks
            ValidTarget validTargets = ability.getCastValidTarget();

            if (hasTarget && !targetIsSelf)
            {
                if (targetIsEnemy &&
                    (validTargets & (ValidTarget.EnemyNPC | ValidTarget.EnemyPlayer)) == 0)
                    return "CLIENT_ABILITY_NOT_AN_ENEMY_TARGETED";

                if (!targetIsEnemy &&
                    (validTargets & (ValidTarget.FriendlyPlayer | ValidTarget.FriendlyNPC)) == 0)
                    return "CLIENT_ABILITY_NOT_A_FRIENDLY_TARGETED";
            }

            if (!hasTarget && validTargets != ValidTarget.None &&
                (validTargets & ValidTarget.Self) == 0)
                return "CLIENT_ABILITY_NO_TARGET_SELECTED";

            // 8. Program launcher check
            if (ability.getProgramLauncherRequired() && !hasProgramLauncher)
                return "CLIENT_ABILITY_MISSING_PROGRAMLAUNCHER";

            // 9. Recast timer check
            if (!isRecastReady)
                return "CLIENT_ABILITY_WAITING_RECAST_TIMER";

            // 10. Dead target checks
            if (hasTarget && targetIsDead)
            {
                if (targetIsEnemy && (validTargets & ValidTarget.DeadEnemy) == 0)
                    return "CLIENT_ABILITY_NOT_AN_DEAD_ENEMY_TARGETED";
                if (!targetIsEnemy && (validTargets & ValidTarget.DeadFriendly) == 0)
                    return "CLIENT_ABILITY_NOT_AN_DEAD_FRIENDLY_TARGETED";
            }

            // 11. Range check
            if (hasTarget && !targetIsSelf && ability.getSelectionRange() > 0 &&
                targetDistance > ability.getSelectionRange())
                return "CLIENT_ABILITY_NOT_IN_RANGE";

            // 13. IS cost check (flat cost, no level scaling)
            if (currentIS < ability.getInnerStrengthCost())
                return "CLIENT_ABILITY_INSUFFICIENT_INNER_STRENGTH";

            // Fighting style requirement
            if (ability.getRequiredFightingStyle() != FightingStyle.None &&
                ability.getRequiredFightingStyle() != currentStyle)
                return "CLIENT_ABILITY_NOT_COMBAT_ABILITY";

            // Tactic requirement
            if (ability.getRequiredTacticalSetting() != TacticType.Normal &&
                ability.getRequiredTacticalSetting() != currentTactic)
                return "CLIENT_ABILITY_NOT_COMBAT_ABILITY";

            return null; // Ability use is valid
        }
    }

    /// <summary>
    /// Result of a combat calculation. Field names match client.dll combat resolution
    /// structure at 0x75DD00 (DamageTaken, DamageAbsorbed, Modifier, etc.)
    /// </summary>
    public class CombatResult
    {
        public float rawDamage;           // random(scaledMin, scaledMax)
        public float modifier;            // tacticMod * (1 + damageModifier) + damageMod + tacticBonus
        public float damageAbsorbed;      // ToughnessStatModifier value (0 if armor-piercing)
        public int damageTaken;           // max(0, rawDamage * modifier - damageAbsorbed)
        public DamageType damageType;     // StatModifier/DamageType
        public bool isArmorPiercing;      // StatModifier/IsArmorPiercing
        public float threatGenerated;     // AggroThreatLevel * AggroMultiplier
        public float tacticBonus;         // Ability/TacticBonus contribution
    }
}
