using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

/// <summary>
/// Stat modifier types supported by the tactical progression engine.
/// Replaces legacy string-matching on display labels with strictly typed mathematical effects.
/// </summary>
public enum StatModifierType
{
    MaxHealth,
    ShieldCapacity,
    SlashDamage,
    CounterForce,
    AbilityDamage,
    CooldownReduction,
    MoveSpeed
}

/// <summary>
/// A typed gameplay modifier with a specific magnitude and target stat.
/// </summary>
public sealed record TypedStatModifier(StatModifierType Type, float Value, string Source);

/// <summary>
/// Tactical module equipment slot items that offer balanced tradeoffs rather than unbounded bonuses.
/// </summary>
public sealed record TacticalModule(string Id, string Name, string Description, StatModifierType BonusType, float BonusValue, StatModifierType PenaltyType, float PenaltyValue, int CostCredits);

/// <summary>
/// Tactical gadget equipment items spanning the four canonical families:
/// Reconnaissance, Decoy, Smoke, and Localized Disruption.
/// </summary>
public sealed record TacticalGadget(string Id, string Name, string Family, string Description, float Cooldown, float Radius, int CostCredits);

/// <summary>
/// Tactical loadout configuration for an operative, containing two module slots and two gadget slots.
/// </summary>
public sealed record OperativeLoadout
{
    public string? ModuleSlot1 { get; set; }
    public string? ModuleSlot2 { get; set; }
    public string? GadgetSlot1 { get; set; }
    public string? GadgetSlot2 { get; set; }
}

/// <summary>
/// Per-operative progression state owned exclusively by a single campaign.
/// Supports up to Level 10, three augmentation tracks (Ranks 0-5), and equipped loadouts.
/// </summary>
public sealed class CampaignHeroProgression
{
    public int Level { get; set; } = 1;
    public int CurrentSparks { get; set; } = 0;
    public int SkillPoints { get; set; } = 0;
    public int[] UpgradeRanks { get; set; } = new int[3];
    public OperativeLoadout Loadout { get; set; } = new();
}

/// <summary>
/// Campaign-owned progression state document persisted in PostgreSQL table 'game_state'
/// under the key 'progression:{session}'. Fully isolated between campaigns.
/// </summary>
public sealed class CampaignProgressionState
{
    public const int Version = 1;
    public const int MaxLevel = 10;
    public const int MaxUpgradeRank = 5;

    /// <summary>
    /// Strict 20% passive bonus ceiling across health, shield, damage, and cooldown reduction.
    /// Preserves operative base stat distinctions and prevents trivializing encounters.
    /// </summary>
    public const float PassiveStatCeiling = 0.20f;

    public int SchemaVersion { get; set; } = Version;
    public int GlobalSparksBank { get; set; } = 250;
    public int CityCredits { get; set; } = 500;
    public Dictionary<string, CampaignHeroProgression> Operatives { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> UnlockedModules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> UnlockedGadgets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Available catalog of tactical modules offering balanced tradeoffs.
    /// </summary>
    public static readonly IReadOnlyList<TacticalModule> ModuleCatalog = new[]
    {
        new TacticalModule("mod_kinetic_coil", "Kinetic Flux Coil", "Increases melee slash damage by +10% at the cost of -5% max shield capacity.", StatModifierType.SlashDamage, 0.10f, StatModifierType.ShieldCapacity, -0.05f, 200),
        new TacticalModule("mod_aegis_plating", "Aegis Nano-Plating", "Reinforces shield capacity by +10% while reducing dash cooldown recovery by -5%.", StatModifierType.ShieldCapacity, 0.10f, StatModifierType.CooldownReduction, -0.05f, 200),
        new TacticalModule("mod_thermal_sink", "Thermal Siphon Heatsink", "Accelerates ability cooldowns by +8% with a -5% reduction in melee impact damage.", StatModifierType.CooldownReduction, 0.08f, StatModifierType.SlashDamage, -0.05f, 250),
        new TacticalModule("mod_hull_reinforce", "Composite Hull Lattice", "Expands operative maximum health by +12% at the cost of -4% movement speed.", StatModifierType.MaxHealth, 0.12f, StatModifierType.MoveSpeed, -0.04f, 250)
    };

    /// <summary>
    /// Available catalog of tactical gadgets across the four canonical families.
    /// </summary>
    public static readonly IReadOnlyList<TacticalGadget> GadgetCatalog = new[]
    {
        new TacticalGadget("gadget_sonar_pulse", "Echo-Sonar Beacon", "Reconnaissance", "Emits an ultrasonic pulse that tags all hostiles through cover within 25 metres.", 12.0f, 25.0f, 150),
        new TacticalGadget("gadget_decoy_emitter", "Prism Decoy Projector", "Decoy", "Projects a holographic double that draws enemy gunfire and sensor locks for 6 seconds.", 18.0f, 15.0f, 200),
        new TacticalGadget("gadget_smoke_screen", "Nanite Aerosol Shroud", "Smoke", "Deploys a dense infrared-opaque smoke cloud granting instant evasion and stealth.", 15.0f, 12.0f, 150),
        new TacticalGadget("gadget_emp_disruptor", "Static EMP Mine", "Localized Disruption", "Detonates an electromagnetic burst disabling security cameras and robotic shields for 5 seconds.", 20.0f, 10.0f, 250)
    };

    /// <summary>
    /// Retrieves progression for an operative, creating default Level 1 stats if missing.
    /// </summary>
    public CampaignHeroProgression GetHero(string operativeName)
    {
        string normalized = OperativeRoster.Normalize(operativeName);
        if (!Operatives.TryGetValue(normalized, out var hero))
        {
            hero = new CampaignHeroProgression();
            Operatives[normalized] = hero;
        }
        return hero;
    }

    /// <summary>
    /// Calculates required Hero Sparks to reach next level from current level.
    /// </summary>
    public static int GetRequiredSparks(int level)
    {
        if (level >= MaxLevel) return 0;
        return 100 + (level - 1) * 140 + (level - 1) * (level - 1) * 10;
    }

    /// <summary>
    /// Computes effective modifiers for an operative, enforcing the strict 20% passive bonus ceiling
    /// across health, shield, damage, and cooldown reduction.
    /// </summary>
    public EffectiveTacticalModifiers GetEffectiveModifiers(string operativeName)
    {
        var hero = GetHero(operativeName);
        var defs = CharacterProgressionManager.GetUpgradeDefs(operativeName);

        // Baseline level scaling: +1.5% health, +1.0% damage per level above 1
        int bonusLevels = Math.Max(0, hero.Level - 1);
        float healthFromLevel = bonusLevels * 0.015f;
        float damageFromLevel = bonusLevels * 0.010f;

        float healthBonus = healthFromLevel;
        float shieldBonus = 0f;
        float damageBonus = damageFromLevel;
        float cooldownReduction = 0f;

        // Apply augmentation ranks with typed mappings
        for (int i = 0; i < 3 && i < defs.Length; i++)
        {
            int rank = hero.UpgradeRanks[i];
            if (rank <= 0) continue;

            float bonus = rank * defs[i].StatBonusPerRank * 0.5f; // Normalized scaling
            string stat = defs[i].StatLabel;

            if (stat.Contains("DMG", StringComparison.OrdinalIgnoreCase) || stat.Contains("Damage", StringComparison.OrdinalIgnoreCase))
                damageBonus += bonus;
            else if (stat.Contains("Cooldown", StringComparison.OrdinalIgnoreCase))
                cooldownReduction += bonus;
            else if (stat.Contains("HP", StringComparison.OrdinalIgnoreCase) || stat.Contains("Shield", StringComparison.OrdinalIgnoreCase))
                shieldBonus += bonus;
            else if (stat.Contains("Resistance", StringComparison.OrdinalIgnoreCase))
                healthBonus += bonus;
            else
                damageBonus += bonus * 0.5f;
        }

        // Apply equipped module modifiers
        ApplyModuleModifiers(hero.Loadout.ModuleSlot1, ref healthBonus, ref shieldBonus, ref damageBonus, ref cooldownReduction);
        ApplyModuleModifiers(hero.Loadout.ModuleSlot2, ref healthBonus, ref shieldBonus, ref damageBonus, ref cooldownReduction);

        // Enforce the strict 20% passive bonus ceiling
        return new EffectiveTacticalModifiers(
            Math.Clamp(healthBonus, -0.20f, PassiveStatCeiling),
            Math.Clamp(shieldBonus, -0.20f, PassiveStatCeiling),
            Math.Clamp(damageBonus, -0.20f, PassiveStatCeiling),
            Math.Clamp(cooldownReduction, 0.0f, PassiveStatCeiling)
        );
    }

    private static void ApplyModuleModifiers(string? moduleId, ref float health, ref float shield, ref float dmg, ref float cdr)
    {
        if (string.IsNullOrEmpty(moduleId)) return;
        var mod = ModuleCatalog.FirstOrDefault(m => m.Id == moduleId);
        if (mod == null) return;

        ApplySingleStat(mod.BonusType, mod.BonusValue, ref health, ref shield, ref dmg, ref cdr);
        ApplySingleStat(mod.PenaltyType, mod.PenaltyValue, ref health, ref shield, ref dmg, ref cdr);
    }

    private static void ApplySingleStat(StatModifierType type, float value, ref float health, ref float shield, ref float dmg, ref float cdr)
    {
        switch (type)
        {
            case StatModifierType.MaxHealth: health += value; break;
            case StatModifierType.ShieldCapacity: shield += value; break;
            case StatModifierType.SlashDamage:
            case StatModifierType.AbilityDamage: dmg += value; break;
            case StatModifierType.CooldownReduction: cdr += value; break;
        }
    }

    /// <summary>
    /// Loads campaign progression from PostgreSQL. If absent, performs a safe legacy migration
    /// by copying the legacy global state into this campaign's isolated store.
    /// </summary>
    public static async Task<CampaignProgressionState> LoadOrCreateAsync(int session, NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        if (session <= 0) return new CampaignProgressionState();

        await using var cmd = new NpgsqlCommand("SELECT value::text FROM game_state WHERE state_key=@key", connection, transaction);
        cmd.Parameters.AddWithValue("key", $"progression:{session}");
        var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);

        if (result is string json && !string.IsNullOrWhiteSpace(json))
        {
            var loaded = JsonSerializer.Deserialize<CampaignProgressionState>(json);
            if (loaded != null) return loaded;
        }

        // Migrate from legacy global store 'operative_progression' if available
        var migrated = await MigrateFromLegacyAsync(connection, transaction).ConfigureAwait(false);
        await SaveAsync(session, migrated, connection, transaction).ConfigureAwait(false);
        return migrated;
    }

    /// <summary>
    /// Saves campaign progression to PostgreSQL game_state under 'progression:{session}'.
    /// </summary>
    public static async Task SaveAsync(int session, CampaignProgressionState state, NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        if (session <= 0) return;
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO game_state (state_key, schema_version, value) VALUES (@key, @ver, @val) " +
            "ON CONFLICT (state_key) DO UPDATE SET value=EXCLUDED.value, schema_version=EXCLUDED.schema_version",
            connection, transaction);
        cmd.Parameters.AddWithValue("key", $"progression:{session}");
        cmd.Parameters.AddWithValue("ver", Version);
        cmd.Parameters.AddWithValue("val", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(state));
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Copies legacy shared progression as an immutable starting baseline for existing campaigns.
    /// </summary>
    private static async Task<CampaignProgressionState> MigrateFromLegacyAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        var newState = new CampaignProgressionState();
        try
        {
            await using var cmd = new NpgsqlCommand("SELECT value::text FROM game_state WHERE state_key='operative_progression'", connection, transaction);
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            if (result is string json && !string.IsNullOrWhiteSpace(json))
            {
                var legacy = JsonSerializer.Deserialize<CharacterProgressionManager.SaveDataModel>(json);
                if (legacy != null)
                {
                    newState.GlobalSparksBank = Math.Max(250, legacy.GlobalSparksBank);
                    foreach (var (heroName, data) in legacy.Operatives)
                    {
                        string norm = OperativeRoster.Normalize(heroName);
                        newState.Operatives[norm] = new CampaignHeroProgression
                        {
                            Level = Math.Clamp(data.Level, 1, MaxLevel),
                            CurrentSparks = Math.Max(0, data.CurrentSparks),
                            SkillPoints = Math.Max(0, data.SkillPoints),
                            UpgradeRanks = data.UpgradeRanks?.Take(3).Select(r => Math.Clamp(r, 0, MaxUpgradeRank)).ToArray() ?? new int[3]
                        };
                    }
                }
            }
        }
        catch
        {
            // Fall back to clean defaults on any legacy read error
        }
        return newState;
    }
}

/// <summary>
/// Result of evaluating effective modifiers with strict passive limits.
/// </summary>
public readonly record struct EffectiveTacticalModifiers(float HealthBonusPct, float ShieldBonusPct, float DamageBonusPct, float CooldownReductionPct);
