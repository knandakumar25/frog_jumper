using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A civic reconstruction project available in a liberated district following Mission 12.
/// </summary>
public sealed record ReconstructionTaskDefinition(
    string Id,
    string Name,
    int RegionId,
    string Description,
    string LeadFaction,
    int SparksReward,
    int CreditsReward,
    string UnlockedAmenity
);

/// <summary>
/// A localized corporate remnant threat appearing in post-campaign liberated sectors.
/// </summary>
public sealed record RemnantIncidentDefinition(
    string Id,
    string Name,
    int RegionId,
    int ThreatLevel,
    string RecommendedOperative,
    int SparksReward,
    int CreditsReward
);

/// <summary>
/// An explicitly labeled tactical simulation reproducing pivotal historical battle encounters.
/// </summary>
public sealed record ArchiveSimulationDefinition(
    string Id,
    string Name,
    string OperativeId,
    string Description,
    string MutatorRule,
    int CreditsReward
);

/// <summary>
/// A gameplay challenge mutator for replayable side-scrolling operations.
/// </summary>
public sealed record OperationModifierDefinition(
    string Id,
    string Name,
    string Description,
    float ScoreMultiplier
);

/// <summary>
/// Mastery rank achieved on a side-scrolling resistance operation.
/// </summary>
public enum OperationMasteryRank
{
    None,
    Bronze,
    Silver,
    Gold,
    Platinum
}

/// <summary>
/// Result of evaluating an operation clear with active modifiers and performance stats.
/// </summary>
public sealed record OperationMasteryResult(
    string OperationId,
    OperationMasteryRank Rank,
    int BaseScore,
    int FinalScore,
    int CreditsAwarded,
    int FactionReputationAwarded,
    string? UnlockedCosmetic
);

/// <summary>
/// Central registry of Phase 4 endgame content: reconstruction projects, remnant incidents,
/// archive simulations, and operation challenge modifiers.
/// </summary>
public static class EndgameContentCatalog
{
    // ─── Operation Challenge Modifiers ─────────────────────────────────────────
    public static readonly OperationModifierDefinition ModIronman = new(
        "op_mod_ironman",
        "Ironman Protocol",
        "Single life: defeat immediately terminates the operation without checkpoints.",
        1.50f
    );

    public static readonly OperationModifierDefinition ModOverclocked = new(
        "op_mod_overclocked",
        "Overclocked Mechanoids",
        "All enemy movement, attack frequency, and projectile speeds increased by 25%.",
        1.35f
    );

    public static readonly OperationModifierDefinition ModGlassVanguard = new(
        "op_mod_glass_vanguard",
        "Glass Vanguard",
        "Operative shield capacity reduced by 50%; mobility dash cooldown reduced by 25%.",
        1.30f
    );

    public static readonly OperationModifierDefinition ModEmpAtmosphere = new(
        "op_mod_emp_atmosphere",
        "EMP Atmosphere",
        "Tactical gadgets take double the normal time to recharge.",
        1.20f
    );

    public static readonly OperationModifierDefinition ModAxiomProtocol = new(
        "op_mod_axiom_protocol",
        "Axiom Protocol",
        "Combines Ironman, Overclocked, and Glass Vanguard for ultimate operational mastery.",
        2.00f
    );

    public static readonly IReadOnlyList<OperationModifierDefinition> Modifiers = new[]
    {
        ModIronman,
        ModOverclocked,
        ModGlassVanguard,
        ModEmpAtmosphere,
        ModAxiomProtocol
    };

    // ─── 16 Regional Reconstruction Tasks (1 per Region across all 16 Regions) ─
    public static readonly IReadOnlyList<ReconstructionTaskDefinition> ReconstructionTasks = new ReconstructionTaskDefinition[]
    {
        new("recon_cedarcrest_monastery", "Angeles Crest Monastery Restoration", 0, "Restore mountain radio relays and pilgrim shelters across the high ridges.", FactionId.CivicRecovery, 3, 450, "service_mountain_transit"),
        new("recon_cloudstep_sluice", "Castaic Aqueduct Sluice Modernization", 1, "Automate reservoir spillways to supply clean drinking water downstream.", FactionId.ReclamationGuild, 3, 450, "service_automated_boat_charging"),
        new("recon_stormbreak_lightning", "San Gabriel Grid Grounding", 2, "Repair high-altitude lightning grounding pylons to safeguard alpine communities.", FactionId.ReclamationGuild, 3, 450, "service_weather_radar"),
        new("recon_northlight_beacon", "Malibu Coastal Beacon Lighthouse", 3, "Reconstruct maritime lighthouse beacon to guide coastal civilian supply boats.", FactionId.IndependentCouriers, 3, 450, "service_coastal_ferry"),
        new("recon_cedarwild_sanctuary", "Topanga Canyon Flora Nursery", 4, "Establish native botanical nursery to heal canyon tracts damaged by machine fires.", FactionId.CivicRecovery, 3, 450, "service_herbal_refinery"),
        new("recon_old_kyoto_lanterns", "Little Tokyo Lantern Preservation", 5, "Restore the historic street lanterns and rebuild the community artisan pavilion.", FactionId.CivicRecovery, 3, 500, "service_lantern_sanctuary"),
        new("recon_downtown_transit", "Underworks Subway Reopening", 6, "Reopen the central transit concourse connecting underground safe zones to the street.", FactionId.IndependentCouriers, 3, 500, "service_subway_express"),
        new("recon_harbor_slipways", "San Pedro Slipway Renewal", 7, "Refit automated cranes to handle humanitarian food and medical cargo shipments.", FactionId.IndependentCouriers, 3, 500, "service_deepwater_freight"),
        new("recon_solar_belt_harvest", "Photovoltaic Array Alignment", 8, "Recalibrate two square kilometers of solar collectors to power rural homes.", FactionId.ReclamationGuild, 3, 450, "service_solar_rapid_charge"),
        new("recon_concrete_wards_clinic", "LA River Viaduct Trauma Center", 9, "Expand Sub-Viaduct Shelter 04 into a full-scale regional surgical hospital.", FactionId.CivicRecovery, 3, 500, "service_advanced_prosthetics"),
        new("recon_citadel_reclamation", "Century Citadel Academy", 10, "Transform the former corporate military perimeter into a free public university.", FactionId.FreeSignalCollective, 3, 500, "service_cyber_research_lab"),
        new("recon_tideglass_desalination", "Santa Monica Desalination Plant", 11, "Construct passive solar desalination tanks using reclaimed titanium plates.", FactionId.ReclamationGuild, 3, 450, "service_water_barter_depot"),
        new("recon_kurogane_forge", "Ancestral Blade Monument", 12, "Erect a memorial to fallen resistance fighters and reopen traditional sword forges.", FactionId.FreeSignalCollective, 3, 450, "service_master_sharpening"),
        new("recon_rustrail_commuter", "Alameda Passenger Locomotive", 13, "Convert armored corporate cargo trains into high-speed civilian passenger rail.", FactionId.IndependentCouriers, 3, 500, "service_intercity_passenger_rail"),
        new("recon_foundry_geothermal", "Vernon Geothermal Turbine", 14, "Convert molten smelting furnaces into closed-loop clean geothermal energy plants.", FactionId.ReclamationGuild, 3, 500, "service_foundry_heat_shields"),
        new("recon_wetlands_filtration", "Ballona Bio-Filtration Marsh", 15, "Expand cedar boardwalks and bio-filtration reeds to cleanse the Ballona wetlands delta.", FactionId.CivicRecovery, 3, 450, "service_wetland_nature_reserve")
    };

    // ─── 8 Corpo Remnant Tactical Incidents ────────────────────────────────────
    public static readonly IReadOnlyList<RemnantIncidentDefinition> RemnantIncidents = new RemnantIncidentDefinition[]
    {
        new("remnant_highlands_convoy", "Highland Mercenary Convoy", 0, 3, "Mirage Blade", 2, 350),
        new("remnant_peaks_relay", "San Gabriel Jamming Node", 2, 4, "Aura Phoenicia", 2, 400),
        new("remnant_downtown_blackout", "Underworks Rogue AI Core", 6, 5, "Cyber Shogun", 3, 500),
        new("remnant_harbor_torpedoes", "Automated Minefield Patrol", 7, 4, "Oceanus", 2, 400),
        new("remnant_citadel_stragglers", "Citadel Straggler Garrison", 10, 5, "Surya Prime", 3, 550),
        new("remnant_rustrail_hijackers", "Armored Rail Car Hijack", 13, 4, "Apex Titan", 2, 450),
        new("remnant_foundry_saboteurs", "Foundry Demolition Automata", 14, 5, "Apex Titan", 3, 500),
        new("remnant_wetlands_dumping", "Toxic Siphon Droid Unit", 15, 3, "Oceanus", 2, 350)
    };

    // ─── 6 Archive Historical Combat Simulations ──────────────────────────────
    public static readonly IReadOnlyList<ArchiveSimulationDefinition> ArchiveSimulations = new ArchiveSimulationDefinition[]
    {
        new("sim_shogun_defense", "Historic Gate Defense", "Cyber Shogun", "Defend the Little Tokyo shrine approach against wave upon wave of security mechs.", "Pure Katana: Blade Dash charges instantly on parry.", 600),
        new("sim_surya_solar_siege", "Siege of Array Eight", "Surya Prime", "Hold the solar array central terminal under continuous mortar bombardment.", "Fortified Aegis: Shield absorption converts 20% damage to energy.", 600),
        new("sim_oceanus_abyssal_tide", "Operation Abyssal Tide", "Oceanus", "Infiltrate flooded industrial sluice gates against naval torpedo drones.", "Hydro Surge: Waterway torrents continuously restore shields.", 600),
        new("sim_mirage_shadow_breach", "Citadel Infiltration Run", "Mirage Blade", "Sprint through corporate laser corridors without triggering central alarm grid.", "Phase Drift: Dagger strikes grant temporary cloaking.", 600),
        new("sim_apex_furnace_stand", "The Smelting Core Stand", "Apex Titan", "Duel heavy chassis automata in the molten furnace basin.", "Kinetic Dynamo: Heavy kinetic hammer blows generate shockwaves.", 600),
        new("sim_aura_harmonic_resonance", "Prism Harmonic Pulse", "Aura Phoenicia", "Weave sonic resonance waves to disable drone clusters atop the high spires.", "Harmonic Loop: Resonance pulses trigger secondary chain detonations.", 600)
    };

    /// <summary>
    /// Evaluates operation performance, calculating score, rank, credits, and cosmetic rewards.
    /// </summary>
    public static OperationMasteryResult EvaluateMastery(
        string operationId,
        float clearTimeSeconds,
        float targetGoldTimeSeconds,
        IReadOnlyList<string> activeModifierIds,
        bool tookDamage
    )
    {
        int baseScore = 1000;
        float timeRatio = targetGoldTimeSeconds / MathF.Max(1.0f, clearTimeSeconds);
        int timeBonus = (int)MathF.Max(0.0f, (timeRatio - 1.0f) * 500f);

        // Sum modifier score multipliers
        float multiplier = 1.0f;
        foreach (var modId in activeModifierIds)
        {
            var def = Modifiers.FirstOrDefault(m => m.Id.Equals(modId, StringComparison.OrdinalIgnoreCase));
            if (def != null)
            {
                multiplier += (def.ScoreMultiplier - 1.0f);
            }
        }

        bool hasAxiomProtocol = activeModifierIds.Any(m => m.Equals("op_mod_axiom_protocol", StringComparison.OrdinalIgnoreCase));
        int finalScore = (int)((baseScore + timeBonus) * multiplier);

        OperationMasteryRank rank = OperationMasteryRank.Bronze;
        if (clearTimeSeconds <= targetGoldTimeSeconds * 1.5f) rank = OperationMasteryRank.Silver;
        if (clearTimeSeconds <= targetGoldTimeSeconds && activeModifierIds.Count >= 1) rank = OperationMasteryRank.Gold;
        if (clearTimeSeconds <= targetGoldTimeSeconds && (activeModifierIds.Count >= 2 || hasAxiomProtocol) && !tookDamage)
        {
            rank = OperationMasteryRank.Platinum;
        }

        int creditsAwarded = rank switch
        {
            OperationMasteryRank.Platinum => 1200,
            OperationMasteryRank.Gold => 800,
            OperationMasteryRank.Silver => 500,
            _ => 300
        };

        int factionRep = rank switch
        {
            OperationMasteryRank.Platinum => 75,
            OperationMasteryRank.Gold => 50,
            OperationMasteryRank.Silver => 30,
            _ => 15
        };

        string? cosmetic = rank switch
        {
            OperationMasteryRank.Platinum => $"skin_platinum_{operationId}",
            OperationMasteryRank.Gold => $"livery_gold_{operationId}",
            _ => null
        };

        return new OperationMasteryResult(
            operationId,
            rank,
            baseScore + timeBonus,
            finalScore,
            creditsAwarded,
            factionRep,
            cosmetic
        );
    }
}
