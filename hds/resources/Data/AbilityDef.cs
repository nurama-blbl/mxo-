using System;
using System.Collections.Generic;

namespace hds
{
    /// <summary>
    /// Complete ability definition matching the MxO game data model.
    /// Schema extracted from gameobjects.pkb GOTypeDescriptor table (Ability/* property paths).
    ///
    /// This replaces/extends the existing AbilityItem with all 80+ real game properties.
    /// The existing AbilityItem fields map to:
    ///   AbilityID -> abilityID
    ///   GOID -> goid
    ///   AbilityName -> abilityName
    ///   isCastable -> derived from castingTime > 0
    ///   CastingTime -> castingTime
    ///   ActivationFX -> executionFX
    ///   valueFrom/valueTo -> statModifiers[0].minValue/maxValue
    ///   isBuff -> statModifiers[0].durationType != Instant
    ///   buffTime -> statModifiers[0].duration
    ///
    /// Data sources:
    ///   - 672 unique abilities in gameobjects.pkb
    ///   - StartingCharacterData.xml for starting ability IDs and names
    ///   - Abilities.ilmb for combat rule trees (MHAB/MTAC/AGGR)
    ///   - Cookbook.cbb for category and crafting relationships
    /// </summary>
    public class AbilityDef
    {
        // === Identity ===
        private UInt16 abilityID;
        private Int32 goid;
        private string abilityName;
        private AbilityClass abilityClass;
        private byte abilityRank;
        private byte abilityLevelCap;
        private string disciplineBelongsTo;

        // === Casting ===
        private float castingTime;
        private float recastTime;               // Cooldown after use
        private string familyRecastTimer;        // Shared cooldown group name
        private float selectionRange;            // Max targeting distance
        private float radiusOfEffect;            // AoE radius (0 = single target)
        private float angleOfEffect;             // AoE cone angle (0 = full circle)
        private float pulseInterval;             // Tick interval for pulse abilities
        private ValidTarget castValidTarget;     // Valid target bitmask
        private bool doesNOTRequireLOS;          // Line-of-sight bypass
        private bool isInterruptable;
        private bool programLauncherRequired;    // Needs tool equipped
        private bool requiresTool;

        // === Combat Properties ===
        private FightingStyle combatFightingStyle;
        private FightingStyle requiredFightingStyle;
        private TacticType requiredTacticalSetting;
        private string combatSpecialMove;        // Special move animation category
        private float tacticBonus;               // Tactic score modifier
        private float innerStrengthCost;         // IS cost to use
        private float aiAbilityInnerStrengthCost; // AI version

        // === Aggro ===
        private byte aggroClass;
        private float aggroMultiplier;
        private float aggroThreatLevel;

        // === Flags ===
        private bool isCombatExclusive;          // Only usable in combat
        private bool nonCombatOnlyAbility;       // Only usable outside combat
        private bool isAToggleAbility;
        private bool isDiscipline;               // Passive discipline tree ability
        private bool isExclusive;
        private bool doShowInCharacterSheet;
        private bool hideFromBuffedUI;

        // === Deflection/Success interlock ===
        private bool hasDeflectionInterlockMove;
        private bool hasSuccessInterlockMove;
        private string grabDistruptedBuff;

        // === FX and Animations ===
        private UInt32 castingFX;
        private UInt32 executionFX;
        private UInt32 selectionFX;
        private UInt32 layoutRezID;              // UI layout resource

        // === Animation data (from existing AbilityItem) ===
        private byte[] castAnimStart;
        private byte[] castAnimMid;
        private byte[] castAnimEnd;

        // === Level Scaling ===
        private float levelingCost;              // Cost to level up
        private float upgradeCostModifier;
        private float upgradeCostMultiplier;
        private float inventoryMemCost;          // Memory slot cost
        private float minValueScaleLevel;        // Min damage/heal per level
        private float maxValueScaleLevel;        // Max damage/heal per level

        // === Mastery ===
        private UInt16 masteryAbility;           // Mastery version ability ID
        private UInt16 masteryBaseAbility;       // Base ability for this mastery

        // === Buff/Debuff properties (from StatModifier/ValueTypeB) ===
        private byte valueType;
        private float buffBonusCapScale;
        private byte stackingPriority;
        private StackingType stackingType;

        // === Scripting ===
        private string castingScript;
        private bool castingScriptIsAggroStance;
        private string testFunction;
        private string stringInit;
        private string stringTerm;

        // === Result Strings ===
        private string successCasterString;
        private string successTargetString;
        private string failureCasterString;
        private string failureTargetString;
        private string deflectionCasterString;
        private string deflectionTargetString;

        // === Detection ===
        private float detectionDifficultyChange;

        // === Stat Modifiers (buffs/debuffs applied by this ability) ===
        private List<StatModifierDef> statModifiers;

        // === Status ===
        private string statusCategory;

        public AbilityDef()
        {
            this.statModifiers = new List<StatModifierDef>();
            this.castValidTarget = ValidTarget.EnemyNPC | ValidTarget.EnemyPlayer;
            this.castingTime = 0f;
            this.recastTime = 0f;
            this.abilityLevelCap = 4;
            this.isInterruptable = true;
            this.doShowInCharacterSheet = true;
            this.aggroMultiplier = 1.0f;
        }

        /// <summary>
        /// Create an AbilityDef from an existing AbilityItem for backwards compatibility.
        /// </summary>
        public static AbilityDef fromAbilityItem(AbilityItem item)
        {
            AbilityDef def = new AbilityDef();
            def.abilityID = item.getAbilityID();
            def.goid = item.getGOID();
            def.abilityName = item.getAbilityName();
            def.castingTime = item.getCastingTime();
            def.castAnimStart = item.getCastAnimStart();
            def.castAnimMid = item.getCastAnimMid();
            def.castAnimEnd = item.getCastAnimEnd();
            def.executionFX = item.getActivationFX();

            if (item.getValueFrom() != 0 || item.getValueTo() != 0)
            {
                StatModifierDef mod = new StatModifierDef();
                mod.setMinValue(item.getValueFrom());
                mod.setMaxValue(item.getValueTo());
                if (item.getIsBuff())
                {
                    mod.setDuration(item.getBuffTime());
                    mod.setDurationType(DurationType.Timed);
                }
                def.statModifiers.Add(mod);
            }
            return def;
        }

        // === Identity Accessors ===

        public void setAbilityID(UInt16 val) { this.abilityID = val; }
        public UInt16 getAbilityID() { return this.abilityID; }

        public void setGOID(Int32 val) { this.goid = val; }
        public Int32 getGOID() { return this.goid; }

        public void setAbilityName(string val) { this.abilityName = val; }
        public string getAbilityName() { return this.abilityName; }

        public void setAbilityClass(AbilityClass val) { this.abilityClass = val; }
        public AbilityClass getAbilityClass() { return this.abilityClass; }

        public void setAbilityRank(byte val) { this.abilityRank = val; }
        public byte getAbilityRank() { return this.abilityRank; }

        public void setAbilityLevelCap(byte val) { this.abilityLevelCap = val; }
        public byte getAbilityLevelCap() { return this.abilityLevelCap; }

        public void setDisciplineBelongsTo(string val) { this.disciplineBelongsTo = val; }
        public string getDisciplineBelongsTo() { return this.disciplineBelongsTo; }

        // === Casting Accessors ===

        public void setCastingTime(float val) { this.castingTime = val; }
        public float getCastingTime() { return this.castingTime; }

        public void setRecastTime(float val) { this.recastTime = val; }
        public float getRecastTime() { return this.recastTime; }

        public void setFamilyRecastTimer(string val) { this.familyRecastTimer = val; }
        public string getFamilyRecastTimer() { return this.familyRecastTimer; }

        public void setSelectionRange(float val) { this.selectionRange = val; }
        public float getSelectionRange() { return this.selectionRange; }

        public void setRadiusOfEffect(float val) { this.radiusOfEffect = val; }
        public float getRadiusOfEffect() { return this.radiusOfEffect; }

        public void setAngleOfEffect(float val) { this.angleOfEffect = val; }
        public float getAngleOfEffect() { return this.angleOfEffect; }

        public void setPulseInterval(float val) { this.pulseInterval = val; }
        public float getPulseInterval() { return this.pulseInterval; }

        public void setCastValidTarget(ValidTarget val) { this.castValidTarget = val; }
        public ValidTarget getCastValidTarget() { return this.castValidTarget; }

        public void setDoesNOTRequireLOS(bool val) { this.doesNOTRequireLOS = val; }
        public bool getDoesNOTRequireLOS() { return this.doesNOTRequireLOS; }

        public void setIsInterruptable(bool val) { this.isInterruptable = val; }
        public bool getIsInterruptable() { return this.isInterruptable; }

        public void setProgramLauncherRequired(bool val) { this.programLauncherRequired = val; }
        public bool getProgramLauncherRequired() { return this.programLauncherRequired; }

        public void setRequiresTool(bool val) { this.requiresTool = val; }
        public bool getRequiresTool() { return this.requiresTool; }

        // === Combat Accessors ===

        public void setCombatFightingStyle(FightingStyle val) { this.combatFightingStyle = val; }
        public FightingStyle getCombatFightingStyle() { return this.combatFightingStyle; }

        public void setRequiredFightingStyle(FightingStyle val) { this.requiredFightingStyle = val; }
        public FightingStyle getRequiredFightingStyle() { return this.requiredFightingStyle; }

        public void setRequiredTacticalSetting(TacticType val) { this.requiredTacticalSetting = val; }
        public TacticType getRequiredTacticalSetting() { return this.requiredTacticalSetting; }

        public void setCombatSpecialMove(string val) { this.combatSpecialMove = val; }
        public string getCombatSpecialMove() { return this.combatSpecialMove; }

        public void setTacticBonus(float val) { this.tacticBonus = val; }
        public float getTacticBonus() { return this.tacticBonus; }

        public void setInnerStrengthCost(float val) { this.innerStrengthCost = val; }
        public float getInnerStrengthCost() { return this.innerStrengthCost; }

        // === Aggro Accessors ===

        public void setAggroClass(byte val) { this.aggroClass = val; }
        public byte getAggroClass() { return this.aggroClass; }

        public void setAggroMultiplier(float val) { this.aggroMultiplier = val; }
        public float getAggroMultiplier() { return this.aggroMultiplier; }

        public void setAggroThreatLevel(float val) { this.aggroThreatLevel = val; }
        public float getAggroThreatLevel() { return this.aggroThreatLevel; }

        // === Flag Accessors ===

        public void setIsCombatExclusive(bool val) { this.isCombatExclusive = val; }
        public bool getIsCombatExclusive() { return this.isCombatExclusive; }

        public void setNonCombatOnlyAbility(bool val) { this.nonCombatOnlyAbility = val; }
        public bool getNonCombatOnlyAbility() { return this.nonCombatOnlyAbility; }

        public void setIsAToggleAbility(bool val) { this.isAToggleAbility = val; }
        public bool getIsAToggleAbility() { return this.isAToggleAbility; }

        public void setIsDiscipline(bool val) { this.isDiscipline = val; }
        public bool getIsDiscipline() { return this.isDiscipline; }

        // === FX Accessors ===

        public void setCastingFX(UInt32 val) { this.castingFX = val; }
        public UInt32 getCastingFX() { return this.castingFX; }

        public void setExecutionFX(UInt32 val) { this.executionFX = val; }
        public UInt32 getExecutionFX() { return this.executionFX; }

        public void setCastAnimStart(byte[] val) { this.castAnimStart = val; }
        public byte[] getCastAnimStart() { return this.castAnimStart; }

        public void setCastAnimMid(byte[] val) { this.castAnimMid = val; }
        public byte[] getCastAnimMid() { return this.castAnimMid; }

        public void setCastAnimEnd(byte[] val) { this.castAnimEnd = val; }
        public byte[] getCastAnimEnd() { return this.castAnimEnd; }

        // === Level Scaling Accessors ===

        public void setLevelingCost(float val) { this.levelingCost = val; }
        public float getLevelingCost() { return this.levelingCost; }

        public void setInventoryMemCost(float val) { this.inventoryMemCost = val; }
        public float getInventoryMemCost() { return this.inventoryMemCost; }

        public void setMinValueScaleLevel(float val) { this.minValueScaleLevel = val; }
        public float getMinValueScaleLevel() { return this.minValueScaleLevel; }

        public void setMaxValueScaleLevel(float val) { this.maxValueScaleLevel = val; }
        public float getMaxValueScaleLevel() { return this.maxValueScaleLevel; }

        // === StatModifier Management ===

        public void addStatModifier(StatModifierDef mod) { this.statModifiers.Add(mod); }
        public List<StatModifierDef> getStatModifiers() { return this.statModifiers; }
        public StatModifierDef getStatModifier(int index)
        {
            if (index < statModifiers.Count)
                return statModifiers[index];
            return null;
        }

        // === Status ===

        public void setStatusCategory(string val) { this.statusCategory = val; }
        public string getStatusCategory() { return this.statusCategory; }

        // === Utility Methods ===

        /// <summary>
        /// Returns true if this ability can target the given target type.
        /// </summary>
        public bool canTarget(ValidTarget targetType)
        {
            return (castValidTarget & targetType) != 0;
        }

        /// <summary>
        /// Returns true if this ability has a cast time (is not instant).
        /// </summary>
        public bool isCastable()
        {
            return castingTime > 0f;
        }

        /// <summary>
        /// Returns true if this ability has area-of-effect.
        /// </summary>
        public bool isAoE()
        {
            return radiusOfEffect > 0f;
        }

        /// <summary>
        /// Returns true if this ability applies any buffs or debuffs.
        /// </summary>
        public bool hasStatModifiers()
        {
            return statModifiers.Count > 0;
        }

        /// <summary>
        /// Returns the primary damage type from the first stat modifier, or None.
        /// </summary>
        public DamageType getPrimaryDamageType()
        {
            if (statModifiers.Count > 0)
                return statModifiers[0].getDamageType();
            return DamageType.None;
        }

        /// <summary>
        /// Calculate the scaled damage/heal value for a given ability level.
        /// From gameobjects.pkb property schema:
        ///   scaledMin = StatModifier/MinValue + (MinValueScaleLevel * level)
        ///   scaledMax = StatModifier/MaxValue + (MaxValueScaleLevel * level)
        /// </summary>
        public float getScaledMinValue(uint level)
        {
            if (statModifiers.Count == 0) return 0f;
            return statModifiers[0].getMinValue() + (minValueScaleLevel * level);
        }

        public float getScaledMaxValue(uint level)
        {
            if (statModifiers.Count == 0) return 0f;
            return statModifiers[0].getMaxValue() + (maxValueScaleLevel * level);
        }

        // Note: getInnerStrengthCost() is defined in the Combat Accessors section above.
        // AbilityInnerStrengthCost is a flat value per ability — no per-level scaling.
        // Modified at runtime by InnerstrengthCostModifierAbility buffs.
    }
}
