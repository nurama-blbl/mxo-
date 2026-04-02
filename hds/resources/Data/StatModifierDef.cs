using System;

namespace hds
{
    /// <summary>
    /// Defines a stat modification effect (buff, debuff, damage-over-time, heal-over-time).
    /// Schema extracted from gameobjects.pkb GOTypeDescriptor table at offset 0x1168000+.
    /// Property paths: StatModifier/MinValue, StatModifier/MaxValue, StatModifier/DamageType, etc.
    ///
    /// There are 36,517 StatModifier instances across all game objects in the MxO data.
    /// Abilities can have multiple StatModifiers (StatModifier, StatModifier 2, 3, 4).
    /// </summary>
    public class StatModifierDef
    {
        // Core values
        private float minValue;
        private float maxValue;
        private DamageType damageType;
        private bool isArmorPiercing;

        // Duration
        private float duration;
        private DurationType durationType;

        // Targeting
        private UInt16 targetAttribute;    // StatModifier/TargetAttribute9
        private byte targetState;          // StatModifier/TargetState8
        private UInt16 targetAbility;      // Ability applied to target
        private UInt16 targetAbilityGrant; // Ability granted to target

        // Proc system
        private float procTriggerChance;   // 0.0 - 1.0 probability

        // Value scaling
        private string valueFunction;      // Formula name for dynamic calculation
        private float valueScale;          // Multiplier for level scaling

        // Activation condition
        private string whenEnabled;        // Condition string

        // Visual/audio
        private UInt32 activationFX;       // FX played on proc

        // Scripting hooks (function names called by the engine)
        private string statModInitFunc;
        private string statModTermFunc;
        private string statModUpdateFunc;
        private string statModHealthChangedFunc;
        private string statModLevelChangedFunc;
        private string statModInnerstrengthChangedFunc;

        // Update ticking
        private float updateFuncInterval;

        // Notification strings
        private string activationStringOnSelf;
        private string activationStringSubject;
        private string activationStringTarget;
        private string updateString;

        // Scripting
        private string activationScript;

        public StatModifierDef()
        {
            this.damageType = DamageType.None;
            this.durationType = DurationType.Instant;
            this.procTriggerChance = 1.0f;
            this.valueScale = 1.0f;
        }

        // --- Core Values ---

        public void setMinValue(float val) { this.minValue = val; }
        public float getMinValue() { return this.minValue; }

        public void setMaxValue(float val) { this.maxValue = val; }
        public float getMaxValue() { return this.maxValue; }

        public void setDamageType(DamageType type) { this.damageType = type; }
        public DamageType getDamageType() { return this.damageType; }

        public void setIsArmorPiercing(bool val) { this.isArmorPiercing = val; }
        public bool getIsArmorPiercing() { return this.isArmorPiercing; }

        // --- Duration ---

        public void setDuration(float val) { this.duration = val; }
        public float getDuration() { return this.duration; }

        public void setDurationType(DurationType type) { this.durationType = type; }
        public DurationType getDurationType() { return this.durationType; }

        // --- Targeting ---

        public void setTargetAttribute(UInt16 val) { this.targetAttribute = val; }
        public UInt16 getTargetAttribute() { return this.targetAttribute; }

        public void setTargetState(byte val) { this.targetState = val; }
        public byte getTargetState() { return this.targetState; }

        public void setTargetAbility(UInt16 val) { this.targetAbility = val; }
        public UInt16 getTargetAbility() { return this.targetAbility; }

        public void setTargetAbilityGrant(UInt16 val) { this.targetAbilityGrant = val; }
        public UInt16 getTargetAbilityGrant() { return this.targetAbilityGrant; }

        // --- Proc ---

        public void setProcTriggerChance(float val) { this.procTriggerChance = val; }
        public float getProcTriggerChance() { return this.procTriggerChance; }

        // --- Value Scaling ---

        public void setValueFunction(string val) { this.valueFunction = val; }
        public string getValueFunction() { return this.valueFunction; }

        public void setValueScale(float val) { this.valueScale = val; }
        public float getValueScale() { return this.valueScale; }

        public void setWhenEnabled(string val) { this.whenEnabled = val; }
        public string getWhenEnabled() { return this.whenEnabled; }

        // --- FX ---

        public void setActivationFX(UInt32 val) { this.activationFX = val; }
        public UInt32 getActivationFX() { return this.activationFX; }

        // --- Scripting Hooks ---

        public void setStatModInitFunc(string val) { this.statModInitFunc = val; }
        public string getStatModInitFunc() { return this.statModInitFunc; }

        public void setStatModTermFunc(string val) { this.statModTermFunc = val; }
        public string getStatModTermFunc() { return this.statModTermFunc; }

        public void setStatModUpdateFunc(string val) { this.statModUpdateFunc = val; }
        public string getStatModUpdateFunc() { return this.statModUpdateFunc; }

        public void setStatModHealthChangedFunc(string val) { this.statModHealthChangedFunc = val; }
        public string getStatModHealthChangedFunc() { return this.statModHealthChangedFunc; }

        public void setStatModLevelChangedFunc(string val) { this.statModLevelChangedFunc = val; }
        public string getStatModLevelChangedFunc() { return this.statModLevelChangedFunc; }

        public void setStatModInnerstrengthChangedFunc(string val) { this.statModInnerstrengthChangedFunc = val; }
        public string getStatModInnerstrengthChangedFunc() { return this.statModInnerstrengthChangedFunc; }

        // --- Update Ticking ---

        public void setUpdateFuncInterval(float val) { this.updateFuncInterval = val; }
        public float getUpdateFuncInterval() { return this.updateFuncInterval; }

        // --- Notification ---

        public void setActivationStringOnSelf(string val) { this.activationStringOnSelf = val; }
        public string getActivationStringOnSelf() { return this.activationStringOnSelf; }

        public void setActivationStringSubject(string val) { this.activationStringSubject = val; }
        public string getActivationStringSubject() { return this.activationStringSubject; }

        public void setActivationStringTarget(string val) { this.activationStringTarget = val; }
        public string getActivationStringTarget() { return this.activationStringTarget; }

        public void setUpdateString(string val) { this.updateString = val; }
        public string getUpdateString() { return this.updateString; }

        public void setActivationScript(string val) { this.activationScript = val; }
        public string getActivationScript() { return this.activationScript; }

        /// <summary>
        /// Calculate the actual effect value, applying scaling and randomization.
        /// </summary>
        public float rollValue(Random rng)
        {
            if (minValue == maxValue)
                return minValue;
            return minValue + (float)(rng.NextDouble() * (maxValue - minValue));
        }
    }
}
