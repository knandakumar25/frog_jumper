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
            _ => unlockedCampaignMission > 1 ? DistrictEra.Aftermath : DistrictEra.PreMission
        };
    }
}
