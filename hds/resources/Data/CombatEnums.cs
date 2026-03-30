using System;

namespace hds
{
    /// <summary>
    /// Damage types used by the MxO combat system.
    /// Extracted from client.dll StatModifier/DamageType field definitions.
    /// </summary>
    public enum DamageType : byte
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Viral = 3,
        Ballistic = 4,
        Hacking = 5
    }

    /// <summary>
    /// Combat tactic types. Values confirmed from client.dll string table at 0x756F6C
    /// (sequential null-terminated strings in binary order = enum value) and
    /// cross-referenced with ILDB Abilities.ilmb MFAC block values (ASCII '0'-'7').
    ///
    /// Interlock button mapping:
    ///   Block_Button  -> Defense (3)
    ///   Grab_Button   -> Retaliate (0)
    ///   Power_Button  -> Power (4)
    ///   Speed_Button  -> Speed (5)
    ///   Withdraw      -> exits combat
    /// </summary>
    public enum TacticType : byte
    {
        Retaliate = 0,
        AimedShot = 1,
        Burst = 2,
        Defense = 3,
        Power = 4,
        Speed = 5,
        Precise = 6,
        Energized = 7,
        Normal = 8        // Default / no tactic selected
    }

    /// <summary>
    /// Character classes / ability archetypes.
    /// From client.dll CS_Abilities_Option_* strings at 0x750574.
    /// </summary>
    public enum AbilityClass : byte
    {
        Awakened = 0,
        Operative = 1,
        Coder = 2,
        Hacker = 3
    }

    /// <summary>
    /// Game object categories from gocategories.gcb.
    /// Top-level ability categories used in the Cookbook.cbb system.
    /// </summary>
    public enum AbilityCategory : ushort
    {
        // Character Ability categories (CA00 tree)
        AbilityHackerBlock = 0x4148,     // AHB
        AbilityHackerDefense = 0x4148,   // AHD
        AbilityHackerPower = 0x4148,     // AHP
        AbilityHackerOther = 0x4148,     // AHO
        AbilityOperativeOther = 0x414F,  // AOO
        AbilityOperativePower = 0x414F,  // AOP
        AbilityOperativeBlock = 0x414F,  // AOB
        AbilityOperativeUpgrade = 0x414F,// AOU
        AbilityCoderBlock = 0x4143,      // ACB
        AbilityCoderPower = 0x4143,      // ACP
        AbilityCoderUpgrade = 0x4143,    // ACU
        AbilityCoderOther = 0x4143,      // ACO

        // Interlock Ability categories (IA00 tree)
        InterlockShot = 0x4941,          // IASh - 8397 entries
        InterlockOwn = 0x4941,           // IAOw - 8098 entries
        InterlockPowerSpecial = 0x4941,  // IAPS - 2695 entries
        InterlockForward = 0x4941,       // IAFw - 1184 entries
        InterlockEvade = 0x4941,         // IAEw - 81 entries
        InterlockHit = 0x4941,           // IAHt - 19 entries
        InterlockGrapple = 0x4941,       // IAGl - 12 entries
    }

    /// <summary>
    /// Fighting styles available in the interlock (choreographed combat) system.
    /// From client.dll StatusCombat_FightingStyleText/Icon at 0x75A560.
    /// </summary>
    public enum FightingStyle : byte
    {
        None = 0,
        Aikido = 1,
        KungFu = 2,
        Karate = 3
    }

    /// <summary>
    /// Buff/debuff duration types from StatModifier/DurationType property.
    /// </summary>
    public enum DurationType : byte
    {
        Instant = 0,
        Timed = 1,
        Permanent = 2,
        Toggle = 3,
        Pulse = 4
    }

    /// <summary>
    /// Stacking behavior for buffs/debuffs from StatModifier StackingType4 property.
    /// </summary>
    public enum StackingType : byte
    {
        None = 0,
        Replace = 1,
        Stack = 2,
        Refresh = 3,
        Highest = 4
    }

    /// <summary>
    /// Valid target types for abilities.
    /// Derived from client.dll LTCLIENT_ABILITY_* error strings at 0x74E118-0x74E3D0:
    ///   CANT_TARGET_SELF, NOT_A_FRIENDLY_TARGETED, NOT_AN_ENEMY_TARGETED,
    ///   NOT_A_GROUPMEMBER_TARGETED, NOT_OBJECT_TARGETED,
    ///   NOT_AN_DEAD_FRIENDLY_TARGETED, NOT_AN_DEAD_ENEMY_TARGETED
    ///
    /// The CastValidTarget/ property in gameobjects.pkb is a compound type
    /// (trailing slash = sub-object reference), meaning target rules are defined
    /// as named TargetObjectRule objects rather than raw bitmasks. These flag
    /// values are a server-side abstraction for implementing the same checks.
    ///
    /// NOTE: Flag bit assignments are server-side convention, not from client binary.
    /// </summary>
    [Flags]
    public enum ValidTarget : ushort
    {
        None = 0,
        Self = 0x01,
        FriendlyPlayer = 0x02,
        EnemyPlayer = 0x04,
        FriendlyNPC = 0x08,
        EnemyNPC = 0x10,
        DeadFriendly = 0x20,
        DeadEnemy = 0x40,
        Object = 0x80,
        GroupMember = 0x100
    }
}
