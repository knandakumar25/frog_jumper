using System;
using System.Collections.Generic;

/// <summary>
/// Campaign era representing the historical and geopolitical status of a district.
/// </summary>
public enum DistrictEra
{
    PreMission,
    Aftermath,
    Liberated
}

/// <summary>
/// Relationship tiers with resistance organizations unlocking optional services, dialogue, and sidegrades.
/// </summary>
public enum FactionTier
{
    Contact,
    Trusted,
    Partner
}

/// <summary>
/// Canonical identifiers for the four primary resistance factions operating across Neo-Kyoto.
/// </summary>
public static class FactionId
{
    public const string CivicRecovery = "civic_recovery";
    public const string IndependentCouriers = "independent_couriers";
    public const string ReclamationGuild = "reclamation_guild";
    public const string FreeSignalCollective = "free_signal_collective";

    public static FactionTier CalculateTier(int points) => points switch
    {
        >= 250 => FactionTier.Partner,
        >= 100 => FactionTier.Trusted,
        _ => FactionTier.Contact
    };
}

/// <summary>
/// Authored amenities for customizing the Old Kyoto Resistance Headquarters.
/// </summary>
public static class HqAmenities
{
    public const string TacticalHoloTable = "hq_tactical_holo_table";
    public const string SquadQuarters = "hq_squad_quarters";
    public const string MemorialShrine = "hq_memorial_shrine";
    public const string ReclamationWorkbench = "hq_reclamation_workbench";
    public const string SignalArray = "hq_signal_array";

    public static readonly IReadOnlyList<string> All = new[]
    {
        TacticalHoloTable,
        SquadQuarters,
        MemorialShrine,
        ReclamationWorkbench,
        SignalArray
    };
}

/// <summary>
/// State of a single district within a campaign, tracking its era, reconstruction projects,
/// and available civilian services.
/// </summary>
public sealed class DistrictState
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = "";
    public DistrictEra Era { get; set; } = DistrictEra.PreMission;
    public string? ActiveReconstructionChoice { get; set; }
    public HashSet<string> UnlockedServices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Computes the appropriate district era based on campaign mission progression.
    /// After Mission 12, all districts transition into the Liberated reconstruction era.
    /// </summary>
    public static DistrictEra DetermineEra(int regionId, int unlockedCampaignMission)
    {
        if (unlockedCampaignMission > 12) return DistrictEra.Liberated;

        return regionId switch
        {
            5 => unlockedCampaignMission > 1 ? DistrictEra.Aftermath : DistrictEra.PreMission,   // Old Kyoto (Mission 1)
            9 => unlockedCampaignMission > 2 ? DistrictEra.Aftermath : DistrictEra.PreMission,   // Concrete Wards (Mission 2)
            13 => unlockedCampaignMission > 3 ? DistrictEra.Aftermath : DistrictEra.PreMission,  // Rustrail Belt (Mission 3)
            10 => unlockedCampaignMission > 4 ? DistrictEra.Aftermath : DistrictEra.PreMission,  // Citadel Ring (Mission 4)
            6 => unlockedCampaignMission > 6 ? DistrictEra.Aftermath : DistrictEra.PreMission,   // Downtown Core (Mission 5/6)
            14 => unlockedCampaignMission > 7 ? DistrictEra.Aftermath : DistrictEra.PreMission,  // Foundry Basin (Mission 7)
            7 => unlockedCampaignMission > 8 ? DistrictEra.Aftermath : DistrictEra.PreMission,   // Eastlight Harbor (Mission 8)
            4 => unlockedCampaignMission > 8 ? DistrictEra.Aftermath : DistrictEra.PreMission,   // Cedarwild Preserve
            0 => unlockedCampaignMission > 9 ? DistrictEra.Aftermath : DistrictEra.PreMission,   // Cedarcrest Highlands (Mission 9)
            2 => unlockedCampaignMission > 10 ? DistrictEra.Aftermath : DistrictEra.PreMission,  // Stormbreak Peaks (Mission 10)
            12 => unlockedCampaignMission > 11 ? DistrictEra.Aftermath : DistrictEra.PreMission, // Kurogane Escarpment (Mission 11)
            _ => unlockedCampaignMission > 1 ? DistrictEra.Aftermath : DistrictEra.PreMission
        };
    }
}
