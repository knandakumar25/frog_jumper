using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// An authored activity pocket within a region, providing a dense cluster of residents,
/// services, interiors, and nearby activities.
/// </summary>
public sealed record ActivityPocket(
    string Id,
    string Name,
    int RegionId,
    WorldPoint Center,
    double RadiusMetres,
    bool IsSafeZone,
    IReadOnlyList<string> ResidentIds,
    IReadOnlyList<string> InteriorIds,
    IReadOnlyList<string> ActivityIds
);

/// <summary>
/// A named resident living in Neo-Kyoto with authored daily routines and faction ties.
/// </summary>
public sealed record NamedResident(
    string Id,
    string Name,
    string Title,
    string Faction,
    int RegionId,
    string HomePocketId,
    WorldPoint DayLocation,
    WorldPoint EveningLocation,
    string GreetingDialogue,
    string DialogGraphId
);

/// <summary>
/// An enterable building interior such as a resistance safehouse, medical clinic, or workshop.
/// </summary>
public sealed record InteriorDefinition(
    string Id,
    string Name,
    string PocketId,
    WorldPoint EntranceLocation,
    string InteriorKind,
    IReadOnlyList<string> ProvidedServices
);

/// <summary>
/// A traversal vehicle specification.
/// </summary>
public sealed record VehicleDefinition(
    string Id,
    string Name,
    float MaxSpeedMetresPerSec,
    float Acceleration,
    float Handling,
    bool CanEnterWater,
    int UnlockCostCredits
);

/// <summary>
/// Regional challenge types spanning races, operative traversal puzzles, and discovery trails.
/// </summary>
public enum ChallengeType
{
    Race,
    OperativePuzzle,
    DiscoveryTrail
}

/// <summary>
/// Authored regional challenge with time thresholds and repeatable rewards.
/// </summary>
public sealed record RegionalChallenge(
    string Id,
    string Name,
    int RegionId,
    ChallengeType Type,
    WorldPoint StartLocation,
    float GoldTimeSeconds,
    float SilverTimeSeconds,
    int FirstClearSparks,
    int RepeatCredits
);

/// <summary>
/// Replayable side-scrolling resistance operation.
/// </summary>
public sealed record SideOperationDefinition(
    string Id,
    string Name,
    string OperationFamily,
    int RegionId,
    WorldPoint TerminalLocation,
    string ScenePath,
    int FirstClearSparks,
    int RepeatCredits
);

/// <summary>
/// Central registry of all authored content for Neo-Kyoto's Living Resistance World.
/// Contains pockets, residents, safehouses, vehicles, quests, challenges, and operations.
/// </summary>
public static class WorldContentCatalog
{
    // ─── Vehicles ────────────────────────────────────────────────────────────
    public static readonly VehicleDefinition StarterBike = new(
        "veh_starter_bike",
        "Axiom Echo-Rider (Electric Bike)",
        20.0f, // 20 m/s (~72 km/h) on suitable roads
        8.0f,
        1.2f,
        false,
        0 // Free loan / starter orientation reward
    );

    public static readonly VehicleDefinition UtilityBoat = new(
        "veh_utility_boat",
        "Nami-Runner Utility Boat",
        14.0f, // 14 m/s on waterways
        6.0f,
        1.0f,
        true,
        400
    );

    public static readonly IReadOnlyList<VehicleDefinition> Vehicles = new[]
    {
        StarterBike,
        UtilityBoat
    };

    // ─── Interiors & Safehouses (Phase 0, 1 & 2) ─────────────────────────────
    public static readonly IReadOnlyList<InteriorDefinition> Interiors = new[]
    {
        // Phase 1 Safehouses
        new InteriorDefinition(
            "safehouse_old_kyoto",
            "The Lantern Hearth (Old Kyoto Safehouse)",
            "ok_pocket_hub",
            new WorldPoint(44020, 44015),
            "safehouse",
            new[] { "rest_heal", "tactical_loadout", "squad_briefing", "garage_bay" }
        ),
        new InteriorDefinition(
            "safehouse_concrete_wards",
            "Sub-Viaduct Shelter 04 (Concrete Wards Safehouse)",
            "cw_pocket_hub",
            new WorldPoint(44025, 76015),
            "safehouse",
            new[] { "medical_clinic", "generator_workbench", "transit_terminal" }
        ),
        new InteriorDefinition(
            "interior_palace_archives",
            "Kurogane Ancient Archives",
            "ok_pocket_palace_investigation",
            new WorldPoint(44260, 43910),
            "archive",
            new[] { "historical_codex", "relic_study" }
        ),
        new InteriorDefinition(
            "interior_viaduct_clinic",
            "District 9 Emergency Clinic",
            "cw_pocket_hub",
            new WorldPoint(43990, 76030),
            "clinic",
            new[] { "first_aid", "serum_synthesis" }
        ),

        // Phase 2 Urban Safehouses
        new InteriorDefinition(
            "safehouse_downtown",
            "Underground Terminal 01 (Downtown Safehouse)",
            "dt_pocket_hub",
            new WorldPoint(76020, 44015, 1),
            "safehouse",
            new[] { "rest_heal", "surveillance_tap", "signal_scramble", "transit_terminal" }
        ),
        new InteriorDefinition(
            "safehouse_harbor",
            "Old Customs Drydock (Harbor Safehouse)",
            "hb_pocket_hub",
            new WorldPoint(108025, 44020),
            "safehouse",
            new[] { "rest_heal", "boat_dock", "smuggling_cache", "cargo_terminal" }
        ),
        new InteriorDefinition(
            "safehouse_citadel",
            "Forward Outpost Delta (Citadel Safehouse)",
            "ct_pocket_hub",
            new WorldPoint(76020, 76015),
            "safehouse",
            new[] { "rest_heal", "aegis_recalibration", "tactical_briefing", "covert_entry" }
        ),
        new InteriorDefinition(
            "safehouse_rustrail",
            "Depot Maintenance Bunker (Rustrail Safehouse)",
            "rr_pocket_hub",
            new WorldPoint(44020, 108015),
            "safehouse",
            new[] { "rest_heal", "rail_car_workshop", "scrap_refinery", "heavy_storage" }
        ),
        new InteriorDefinition(
            "safehouse_foundry",
            "Sub-Furnace Refuge (Foundry Safehouse)",
            "fd_pocket_hub",
            new WorldPoint(76025, 108020, 2),
            "safehouse",
            new[] { "rest_heal", "thermal_shielding", "forge_workbench", "coolant_terminal" }
        )
    };

    // ─── Named Residents (Phase 1: 6 Residents + Phase 2: 15 Residents = 21 Total) ─────────
    public static readonly IReadOnlyList<NamedResident> Residents = new[]
    {
        // Old Kyoto Residents (Region 5)
        new NamedResident(
            "npc_sora", "Sora", "Lantern Artisan & Civic Organizer",
            "Civic Recovery Network", 5, "ok_pocket_lantern_market",
            new WorldPoint(43820, 44210), new WorldPoint(44025, 44018),
            "Welcome back to Old Kyoto. The lanterns still burn, so long as we keep the power lines unbroken.", "dialog_sora"
        ),
        new NamedResident(
            "npc_gen", "Master Gen", "Historic Archivist & Memory Keeper",
            "Free Signal Collective", 5, "ok_pocket_palace_investigation",
            new WorldPoint(44255, 43905), new WorldPoint(44155, 44355),
            "The machine occupiers tried to burn our names from the record. But stone remembers what circuits forget.", "dialog_gen"
        ),
        new NamedResident(
            "npc_ren", "Ren", "Independent Courier & Master Mechanic",
            "Independent Couriers", 5, "ok_pocket_hub",
            new WorldPoint(44010, 44005), new WorldPoint(43830, 44220),
            "Need a tune-up on that Echo-Rider? The arterial routes are open if your reflexes are sharp enough.", "dialog_ren"
        ),

        // Concrete Wards Residents (Region 9)
        new NamedResident(
            "npc_maya_chen", "Dr. Maya Chen", "Trauma Surgeon & Shelter Director",
            "Civic Recovery Network", 9, "cw_pocket_hub",
            new WorldPoint(43995, 76035), new WorldPoint(44020, 76010),
            "Every day the viaduct stands is another day our patients can sleep without fear of ceiling collapse.", "dialog_maya"
        ),
        new NamedResident(
            "npc_barik", "Barik", "Chief Viaduct Engineer",
            "Reclamation Guild", 9, "cw_pocket_viaduct_station",
            new WorldPoint(43760, 76210), new WorldPoint(44015, 76020),
            "Heavy conduit alignment isn't delicate work, operative. Watch your hands around the magnetic couplings.", "dialog_barik"
        ),
        new NamedResident(
            "npc_yuki", "Yuki", "Signal Runner & Surveillance Scout",
            "Free Signal Collective", 9, "cw_pocket_courtyard",
            new WorldPoint(44310, 75915), new WorldPoint(44160, 76360),
            "They've got pulse sensor drones buzzing over the viaduct gap. Give me a distraction and I'll slice their feeds.", "dialog_yuki"
        ),

        // Downtown Core Residents (Region 6)
        new NamedResident(
            "npc_elena_vance", "Dr. Elena Vance", "Civic Recovery Director",
            "Civic Recovery Network", 6, "dt_pocket_hub",
            new WorldPoint(76010, 44010, 1), new WorldPoint(76020, 44015, 1),
            "The downtown towers look clean from above, but half the population survives down here in the underworks.", "dialog_elena"
        ),
        new NamedResident(
            "npc_kazuya", "Kazuya", "Cyber-Infiltration Specialist",
            "Free Signal Collective", 6, "dt_pocket_grid_relay",
            new WorldPoint(75850, 43900), new WorldPoint(76120, 44100),
            "Every camera in this sector reports to the Citadel mainframes. Let's show them a loop of empty asphalt.", "dialog_kazuya"
        ),
        new NamedResident(
            "npc_aoi", "Aoi", "Neon District Information Broker",
            "Independent Couriers", 6, "dt_pocket_market",
            new WorldPoint(76200, 44150), new WorldPoint(76020, 44015, 1),
            "Looking for high-priority courier routes? Keep your head low and your tires warm.", "dialog_aoi"
        ),

        // Eastlight Harbor Residents (Region 7)
        new NamedResident(
            "npc_captain_maro", "Captain Maro", "Waterfront Courier Master",
            "Independent Couriers", 7, "hb_pocket_hub",
            new WorldPoint(108010, 44010), new WorldPoint(108025, 44020),
            "The waterways are the only roads the machines can't easily pave over. Take the Nami-Runner out for a spin.", "dialog_maro"
        ),
        new NamedResident(
            "npc_nami", "Nami", "Boat Mechanic & Sluice Operator",
            "Reclamation Guild", 7, "hb_pocket_boatyard",
            new WorldPoint(108150, 43850), new WorldPoint(108025, 44020),
            "Mind the turbine intake valves when you're throttling up along the canal lock.", "dialog_nami"
        ),
        new NamedResident(
            "npc_daichi", "Daichi", "Dockworkers Union Steward",
            "Civic Recovery Network", 7, "hb_pocket_docks",
            new WorldPoint(107900, 44200), new WorldPoint(108025, 44020),
            "Our cargo loaders are ready to move medical supplies the moment the harbor blockade is disrupted.", "dialog_daichi"
        ),

        // Citadel Ring Residents (Region 10)
        new NamedResident(
            "npc_commander_locke", "Commander Locke", "Garrison Defector & Strategist",
            "Civic Recovery Network", 10, "ct_pocket_hub",
            new WorldPoint(76010, 76010), new WorldPoint(76020, 76015),
            "The machines built this ring to lock mankind out of the Spire. We know its weak points down to the millimeter.", "dialog_locke"
        ),
        new NamedResident(
            "npc_lyra", "Lyra", "Cryptographic Signal Weaver",
            "Free Signal Collective", 10, "ct_pocket_spire_lookout",
            new WorldPoint(76250, 75850), new WorldPoint(76020, 76015),
            "I'm decrypting Citadel patrol vectors in real-time. Give me time at the radar terminal and I'll blind their outer eyes.", "dialog_lyra"
        ),
        new NamedResident(
            "npc_kaelen", "Kaelen", "Security Pylon Saboteur",
            "Reclamation Guild", 10, "ct_pocket_perimeter_gate",
            new WorldPoint(75800, 76150), new WorldPoint(76020, 76015),
            "These high-voltage barrier pylons can be overridden if we short their grounding coils simultaneously.", "dialog_kaelen"
        ),

        // Rustrail Belt Residents (Region 13)
        new NamedResident(
            "npc_foreman_briggs", "Foreman Briggs", "Reclamation Guild Railmaster",
            "Reclamation Guild", 13, "rr_pocket_hub",
            new WorldPoint(44010, 108010), new WorldPoint(44020, 108015),
            "Rail lines are the veins of the city. We get these locomotives moving, and the resistance eats tonight.", "dialog_briggs"
        ),
        new NamedResident(
            "npc_spike", "Spike", "High-Velocity Rail Runner",
            "Independent Couriers", 13, "rr_pocket_switchyard",
            new WorldPoint(43850, 108200), new WorldPoint(44020, 108015),
            "Riding alongside a moving freight train at 20 meters a second? That's just my Tuesday morning commute.", "dialog_spike"
        ),
        new NamedResident(
            "npc_vex", "Vex", "Heavy Armature Welder",
            "Civic Recovery Network", 13, "rr_pocket_salvage_depot",
            new WorldPoint(44180, 107900), new WorldPoint(44020, 108015),
            "Give me ten tons of train chassis and I can reinforce every civilian bunker from here to Old Kyoto.", "dialog_vex"
        ),

        // Foundry Basin Residents (Region 14)
        new NamedResident(
            "npc_master_cho", "Master Cho", "Chief Metallurgist & Guild Elder",
            "Reclamation Guild", 14, "fd_pocket_hub",
            new WorldPoint(76010, 108010, 2), new WorldPoint(76025, 108020, 2),
            "The heat down here will melt inferior steel and inferior resolve alike. Stand firm before the furnace.", "dialog_cho"
        ),
        new NamedResident(
            "npc_talia", "Talia", "Geothermal Coolant Specialist",
            "Free Signal Collective", 14, "fd_pocket_pump_station",
            new WorldPoint(75850, 107900), new WorldPoint(76025, 108020, 2),
            "The geothermal vents are fluctuating dangerously. Help me bleed the excess steam before the pressure valves pop.", "dialog_talia"
        ),
        new NamedResident(
            "npc_goro", "Goro", "Heavy Armor Fabricator",
            "Civic Recovery Network", 14, "fd_pocket_smelting_bay",
            new WorldPoint(76200, 108150), new WorldPoint(76025, 108020, 2),
            "When the machines bring their heavy mechs, our operatives need armor that laughs at plasma fire.", "dialog_goro"
        )
    };

    // ─── Activity Pockets (64 Total across 16 Regions) ─────────────────────────
    public static readonly IReadOnlyList<ActivityPocket> Pockets = BuildPockets();

    private static ActivityPocket[] BuildPockets()
    {
        var list = new List<ActivityPocket>
        {
            // Old Kyoto Pockets (Region 5)
            new("ok_pocket_hub", "Old Kyoto Arrival Plaza & Hearth HQ", 5, new WorldPoint(44000, 44000), 96.0, true, new[] { "npc_ren" }, new[] { "safehouse_old_kyoto" }, new[] { "chal_ok_race" }),
            new("ok_pocket_lantern_market", "Lantern Market & Artisan Guild", 5, new WorldPoint(43800, 44200), 80.0, true, new[] { "npc_sora" }, Array.Empty<string>(), new[] { "quest_ok_01" }),
            new("ok_pocket_palace_investigation", "Kurogane Palace Walls & Archives", 5, new WorldPoint(44250, 43900), 90.0, false, new[] { "npc_gen" }, new[] { "interior_palace_archives" }, new[] { "quest_ok_02", "chal_ok_puzzle", "side_op_palace_vault" }),
            new("ok_pocket_moonbridge", "Moonbridge Gardens & Shrines", 5, new WorldPoint(44150, 44350), 85.0, true, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_ok_03", "chal_ok_trail" }),

            // Concrete Wards Pockets (Region 9)
            new("cw_pocket_hub", "Concrete Junction & Shelter 04", 9, new WorldPoint(44000, 76000), 96.0, true, new[] { "npc_maya_chen" }, new[] { "safehouse_concrete_wards", "interior_viaduct_clinic" }, new[] { "quest_cw_03" }),
            new("cw_pocket_viaduct_station", "Rainline Viaduct & Generator Station", 9, new WorldPoint(43750, 76200), 85.0, false, new[] { "npc_barik" }, Array.Empty<string>(), new[] { "quest_cw_01", "chal_cw_race", "side_op_viaduct_defense" }),
            new("cw_pocket_courtyard", "Contested Courtyard & Drone Perimeter", 9, new WorldPoint(44300, 75900), 80.0, false, new[] { "npc_yuki" }, Array.Empty<string>(), new[] { "quest_cw_02", "chal_cw_puzzle" }),
            new("cw_pocket_suspended_forum", "Suspended Forum & Girders", 9, new WorldPoint(44150, 76350), 90.0, true, Array.Empty<string>(), Array.Empty<string>(), new[] { "chal_cw_trail" }),

            // Downtown Core Pockets (Region 6)
            new("dt_pocket_hub", "Downtown Plaza & Underworks Bunker", 6, new WorldPoint(76000, 44000, 1), 96.0, true, new[] { "npc_elena_vance" }, new[] { "safehouse_downtown" }, new[] { "chal_dt_race" }),
            new("dt_pocket_grid_relay", "Registry Needle & Surveillance Node", 6, new WorldPoint(75850, 43900), 85.0, false, new[] { "npc_kazuya" }, Array.Empty<string>(), new[] { "quest_dt_01", "side_op_data_haven" }),
            new("dt_pocket_market", "Neon Bazaar & Skyline Promenade", 6, new WorldPoint(76200, 44150), 85.0, true, new[] { "npc_aoi" }, Array.Empty<string>(), new[] { "quest_dt_02", "chal_dt_puzzle" }),
            new("dt_pocket_blackout_terminal", "Commercial Grid Power Substation", 6, new WorldPoint(76100, 43800), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_dt_03", "chal_dt_trail" }),

            // Eastlight Harbor Pockets (Region 7)
            new("hb_pocket_hub", "Eastlight Customs & Drydock Safehouse", 7, new WorldPoint(108000, 44000), 96.0, true, new[] { "npc_captain_maro" }, new[] { "safehouse_harbor" }, new[] { "chal_hb_race" }),
            new("hb_pocket_boatyard", "Canal Locks & Nami Boatyard", 7, new WorldPoint(108150, 43850), 85.0, true, new[] { "npc_nami" }, Array.Empty<string>(), new[] { "quest_hb_01" }),
            new("hb_pocket_docks", "Cargo Container Yard 09", 7, new WorldPoint(107900, 44200), 90.0, false, new[] { "npc_daichi" }, Array.Empty<string>(), new[] { "quest_hb_02", "side_op_harbor_slipway" }),
            new("hb_pocket_waterfront_pier", "Tidal Crane Pier & Defense Bastion", 7, new WorldPoint(108250, 44100), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_hb_03", "chal_hb_puzzle", "chal_hb_trail" }),

            // Citadel Ring Pockets (Region 10)
            new("ct_pocket_hub", "Citadel Forecourt & Outpost Delta", 10, new WorldPoint(76000, 76000), 96.0, true, new[] { "npc_commander_locke" }, new[] { "safehouse_citadel" }, new[] { "chal_ct_race" }),
            new("ct_pocket_spire_lookout", "Spire Lookout & Telemetry Array", 10, new WorldPoint(76250, 75850), 85.0, true, new[] { "npc_lyra" }, Array.Empty<string>(), new[] { "quest_ct_02", "chal_ct_trail" }),
            new("ct_pocket_perimeter_gate", "Fortified Inner Barricade & Pylons", 10, new WorldPoint(75800, 76150), 90.0, false, new[] { "npc_kaelen" }, Array.Empty<string>(), new[] { "quest_ct_01", "chal_ct_puzzle" }),
            new("ct_pocket_ramparts", "Silent Ramparts & Garrison Breach", 10, new WorldPoint(76150, 76300), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_ct_03", "side_op_citadel_perimeter" }),

            // Rustrail Belt Pockets (Region 13)
            new("rr_pocket_hub", "Rustrail Concourse & Maintenance Bunker", 13, new WorldPoint(44000, 108000), 96.0, true, new[] { "npc_foreman_briggs" }, new[] { "safehouse_rustrail" }, new[] { "chal_rr_race" }),
            new("rr_pocket_switchyard", "Central Shunting Yard & Signal Tower", 13, new WorldPoint(43850, 108200), 85.0, true, new[] { "npc_spike" }, Array.Empty<string>(), new[] { "quest_rr_01", "chal_rr_puzzle" }),
            new("rr_pocket_salvage_depot", "Iron Memory Depot & Freight Yard", 13, new WorldPoint(44180, 107900), 90.0, false, new[] { "npc_vex" }, Array.Empty<string>(), new[] { "quest_rr_02", "side_op_freight_intercept" }),
            new("rr_pocket_ember_cut", "Ember Railcut & Defended Spur", 13, new WorldPoint(44300, 108150), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_rr_03", "chal_rr_trail" }),

            // Foundry Basin Pockets (Region 14)
            new("fd_pocket_hub", "Foundry Exchange & Sub-Furnace Refuge", 14, new WorldPoint(76000, 108000, 2), 96.0, true, new[] { "npc_master_cho" }, new[] { "safehouse_foundry" }, new[] { "chal_fd_race" }),
            new("fd_pocket_pump_station", "Geothermal Coolant Valve Station", 14, new WorldPoint(75850, 107900), 85.0, true, new[] { "npc_talia" }, Array.Empty<string>(), new[] { "quest_fd_01", "chal_fd_puzzle" }),
            new("fd_pocket_smelting_bay", "Vulcan Smelting Core & Slag Gates", 14, new WorldPoint(76200, 108150), 90.0, false, new[] { "npc_goro" }, Array.Empty<string>(), new[] { "quest_fd_02", "side_op_smelting_core" }),
            new("fd_pocket_cinderwater", "Cinderwater Basin & Quench Channel", 14, new WorldPoint(76100, 107800), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_fd_03", "chal_fd_trail" })
        };

        // Synthesize satellite pockets for the remaining 9 regions so the world contract of exactly 64 pockets is stable
        foreach (var region in WorldCatalog.Regions)
        {
            if (region.Id is 5 or 9 or 6 or 7 or 10 or 13 or 14) continue;
            int r = region.Id / 4, c = region.Id % 4;
            double baseX = c * 32000 + 16000, baseY = r * 32000 + 16000;
            list.Add(new($"pocket_r{region.Id}_hub", $"{region.Name} Arrival Hub", region.Id, new WorldPoint(baseX, baseY), 96.0, true, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
            list.Add(new($"pocket_r{region.Id}_sat1", $"{region.Name} North Satellite", region.Id, new WorldPoint(baseX, baseY - 8000), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
            list.Add(new($"pocket_r{region.Id}_sat2", $"{region.Name} East Satellite", region.Id, new WorldPoint(baseX + 8000, baseY), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
            list.Add(new($"pocket_r{region.Id}_sat3", $"{region.Name} South Satellite", region.Id, new WorldPoint(baseX, baseY + 8000), 80.0, true, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        }

        return list.ToArray();
    }

    // ─── Authored Quests (Phase 1: 8 Quests + Phase 2: 27 Quests = 35 Total) ───
    public static readonly IReadOnlyList<QuestDefinition> Quests = BuildQuests();

    private static QuestDefinition[] BuildQuests()
    {
        return new QuestDefinition[]
        {
            // ─── Phase 1: Old Kyoto Chain (3) ─────────────────────────────────
            new(
                "quest_ok_01", "The Lantern Circuit",
                "Restore broken energy relays to light the emergency lanterns throughout Old Kyoto.",
                5, QuestCategory.District, 1, null, null,
                new QuestObjective[]
                {
                    new("obj_ok01_1", "Speak with Sora at the Lantern Market.", ObjectiveType.Interact, new WorldPoint(43820, 44210), 12, "npc_sora"),
                    new("obj_ok01_2", "Repair the damaged South Lantern Conduit.", ObjectiveType.RepairConduit, new WorldPoint(43850, 44150), 10),
                    new("obj_ok01_3", "Repair the Western Overloaded Relay.", ObjectiveType.RepairConduit, new WorldPoint(43760, 44240), 10),
                    new("obj_ok01_4", "Return to Sora at the market.", ObjectiveType.Interact, new WorldPoint(43820, 44210), 12, "npc_sora")
                },
                null,
                new QuestReward(100, 150)
            ),
            new(
                "quest_ok_02", "Names Behind the Walls",
                "Investigate Kurogane Palace outer perimeter and decrypt hidden resistance civilian registries.",
                5, QuestCategory.District, 1, "quest_ok_01", null,
                new QuestObjective[]
                {
                    new("obj_ok02_1", "Meet Master Gen at the Kurogane Palace Archives.", ObjectiveType.Interact, new WorldPoint(44255, 43905), 12, "npc_gen"),
                    new("obj_ok02_2", "Scan the ancient etched stele along the outer wall.", ObjectiveType.Investigate, new WorldPoint(44290, 43860), 12),
                    new("obj_ok02_3", "Neutralize the patrolling machine purge squad.", ObjectiveType.DefeatHostiles, new WorldPoint(44320, 43920), 20),
                    new("obj_ok02_4", "Deliver the decrypted memory core back to Master Gen.", ObjectiveType.Interact, new WorldPoint(44255, 43905), 12, "npc_gen")
                },
                null,
                new QuestReward(150, 200, "mod_kinetic_coil")
            ),
            new(
                "quest_ok_03", "The Moonbridge Promise",
                "Help the community leaders resolve the reconstruction priority for the Moonbridge district.",
                5, QuestCategory.District, 1, "quest_ok_02", null,
                new QuestObjective[]
                {
                    new("obj_ok03_1", "Convene with Sora and Ren at the Moonbridge Pavilion.", ObjectiveType.Interact, new WorldPoint(44150, 44350), 16),
                    new("obj_ok03_2", "Choose how to direct the reconstruction resources.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44150, 44350), 16),
                    new("obj_ok03_3", "Confirm the reconstruction order.", ObjectiveType.Interact, new WorldPoint(44150, 44350), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_artisan_guild", "Prioritize the Artisan Guild & Public Kitchens", "Restores civilian trade and living quarters; provides community item discounts.", "The Artisan Guild is rebuilt, filling the market with artisans and lowering upgrade costs.", "service_artisan_guild"),
                    new("choice_wall_defenses", "Reinforce the Kurogane Wall Defenses", "Installs defensive sensor barriers and tactical support turrets around the district perimeter.", "Perimeter defense nodes are armed, providing automated defensive fire against machine incursions.", "service_wall_defenses")
                },
                new QuestReward(200, 300)
            ),

            // ─── Phase 1: Concrete Wards Chain (3) ─────────────────────────────
            new(
                "quest_cw_01", "The Broken Line",
                "Re-align fractured viaduct power cables to restore emergency lighting to Sub-Viaduct Shelter 04.",
                9, QuestCategory.District, 2, null, null,
                new QuestObjective[]
                {
                    new("obj_cw01_1", "Consult Chief Engineer Barik at the Viaduct Station.", ObjectiveType.Interact, new WorldPoint(43760, 76210), 12, "npc_barik"),
                    new("obj_cw01_2", "Repair High-Voltage Cable Junction Alpha.", ObjectiveType.RepairConduit, new WorldPoint(43720, 76180), 10),
                    new("obj_cw01_3", "Reset Transformer Breaker Beta.", ObjectiveType.RepairConduit, new WorldPoint(43810, 76260), 10),
                    new("obj_cw01_4", "Return to Barik to confirm circuit integrity.", ObjectiveType.Interact, new WorldPoint(43760, 76210), 12, "npc_barik")
                },
                null,
                new QuestReward(100, 150)
            ),
            new(
                "quest_cw_02", "Signal Under Rain",
                "Assist scout Yuki in eliminating machine surveillance relays pinning down ward inhabitants.",
                9, QuestCategory.District, 2, "quest_cw_01", null,
                new QuestObjective[]
                {
                    new("obj_cw02_1", "Meet Yuki at the Contested Courtyard surveillance perch.", ObjectiveType.Interact, new WorldPoint(44310, 75915), 12, "npc_yuki"),
                    new("obj_cw02_2", "Disable Sentinel Drone Relay 01.", ObjectiveType.Investigate, new WorldPoint(44340, 75880), 12),
                    new("obj_cw02_3", "Eliminate the sentinel patrol responding to the alarm.", ObjectiveType.DefeatHostiles, new WorldPoint(44280, 75940), 20),
                    new("obj_cw02_4", "Hand the recovered surveillance records to Yuki.", ObjectiveType.Interact, new WorldPoint(44310, 75915), 12, "npc_yuki")
                },
                null,
                new QuestReward(150, 200, null, "gadget_emp_disruptor")
            ),
            new(
                "quest_cw_03", "Shelter and Steel",
                "Determine the vital resource allocation between emergency medical care and heavy fabrication.",
                9, QuestCategory.District, 2, "quest_cw_02", null,
                new QuestObjective[]
                {
                    new("obj_cw03_1", "Convene with Dr. Maya Chen and Barik at Shelter 04.", ObjectiveType.Interact, new WorldPoint(44020, 76010), 16),
                    new("obj_cw03_2", "Select the ward recovery initiative.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44020, 76010), 16),
                    new("obj_cw03_3", "Ratify the allocation order.", ObjectiveType.Interact, new WorldPoint(44020, 76010), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_medical_clinic", "Establish Emergency Trauma Clinic", "Expands ICU beds and rapid field triage; unlocks instant health regeneration in shelter.", "Dr. Chen opens a state-of-the-art triage facility, granting rapid field healing at safehouses.", "service_trauma_clinic"),
                    new("choice_workshop_expansion", "Expand Reclamation Heavy Workshop", "Installs industrial metal presses and salvage recycling; grants bonus credits from salvage.", "Barik completes the heavy workshop expansion, increasing credit yields from world exploration.", "service_salvage_press")
                },
                new QuestReward(200, 300)
            ),

            // ─── Phase 1: Operative Episodes (2) ──────────────────────────────
            new(
                "quest_op_shogun", "Blade of Kurogane",
                "Kaito Kurogane visits his family's ancestral shrine to recover a sealed tactical combat protocol.",
                5, QuestCategory.Operative, 1, null, "Cyber Shogun",
                new QuestObjective[]
                {
                    new("obj_shogun_1", "Visit the Kurogane Ancestral Shrine at the palace overlook.", ObjectiveType.Interact, new WorldPoint(44220, 43980), 12),
                    new("obj_shogun_2", "Defend the shrine altar against scavengers.", ObjectiveType.DefeatHostiles, new WorldPoint(44220, 43980), 18),
                    new("obj_shogun_3", "Synchronize with the Kurogane Legacy Datacore.", ObjectiveType.Investigate, new WorldPoint(44220, 43980), 10)
                },
                null,
                new QuestReward(150, 250, "mod_thermal_sink")
            ),
            new(
                "quest_op_oceanus", "Tides of Vaea",
                "Kiana Vaea uses her hydrokinetic mastery to purge industrial contaminants from the ancient canal gate.",
                5, QuestCategory.Operative, 2, null, "Oceanus",
                new QuestObjective[]
                {
                    new("obj_oceanus_1", "Locate the jammed Canal Floodgate in East Old Kyoto.", ObjectiveType.Interact, new WorldPoint(44350, 44100), 12),
                    new("obj_oceanus_2", "Use Hydro Flush to purge the toxic sludge buildup.", ObjectiveType.RepairConduit, new WorldPoint(44360, 44120), 10),
                    new("obj_oceanus_3", "Open the canal sluice valve and restore clean water flow.", ObjectiveType.Interact, new WorldPoint(44350, 44100), 12)
                },
                null,
                new QuestReward(150, 250, "mod_aegis_plating")
            ),

            // ─── Phase 2: Downtown Core District Chain (3) ─────────────────────
            new(
                "quest_dt_01", "The Glass Archive",
                "Infiltrate an automated surveillance terminal in the commercial high-rises to retrieve wiped civilian logs.",
                6, QuestCategory.District, 5, null, null,
                new QuestObjective[]
                {
                    new("obj_dt01_1", "Confer with Kazuya near the Registry Needle.", ObjectiveType.Interact, new WorldPoint(75850, 43900), 12, "npc_kazuya"),
                    new("obj_dt01_2", "Slice the encrypted glass optic junction.", ObjectiveType.Investigate, new WorldPoint(75820, 43880), 10),
                    new("obj_dt01_3", "Neutralize the corporate security enforcers.", ObjectiveType.DefeatHostiles, new WorldPoint(75880, 43920), 18),
                    new("obj_dt01_4", "Return the decrypted index to Kazuya.", ObjectiveType.Interact, new WorldPoint(75850, 43900), 12, "npc_kazuya")
                },
                null,
                new QuestReward(150, 200, "mod_hull_reinforce", null, FactionId.FreeSignalCollective, 50)
            ),
            new(
                "quest_dt_02", "Ghosts in the Wire",
                "Track a ghosted signal bouncing through the neon advertising marquees down into the Underworks.",
                6, QuestCategory.District, 5, "quest_dt_01", null,
                new QuestObjective[]
                {
                    new("obj_dt02_1", "Meet Aoi in the Neon Bazaar.", ObjectiveType.Interact, new WorldPoint(76200, 44150), 12, "npc_aoi"),
                    new("obj_dt02_2", "Trace the beacon atop Skyline Terrace.", ObjectiveType.Investigate, new WorldPoint(76240, 44180), 12),
                    new("obj_dt02_3", "Follow the decrypted signal down to Underworks Terminal 01.", ObjectiveType.Interact, new WorldPoint(76020, 44015, 1), 16, "npc_elena_vance")
                },
                null,
                new QuestReward(175, 250, null, "gadget_sonar_pulse", FactionId.CivicRecovery, 50)
            ),
            new(
                "quest_dt_03", "Grid Blackout Choice",
                "Decide whether to crash the ward surveillance grid permanently or re-route its power into civilian trauma centers.",
                6, QuestCategory.District, 5, "quest_dt_02", null,
                new QuestObjective[]
                {
                    new("obj_dt03_1", "Convene with Dr. Vance and Kazuya at the Commercial Grid Substation.", ObjectiveType.Interact, new WorldPoint(76100, 43800), 16),
                    new("obj_dt03_2", "Direct the high-voltage breaker routing.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(76100, 43800), 16),
                    new("obj_dt03_3", "Execute the power routing sequence.", ObjectiveType.Interact, new WorldPoint(76100, 43800), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_blackout_net", "Crash the Downtown Machine Surveillance Grid", "Permanently disables scanner drones and lowers alert build-up rate in Downtown.", "Surveillance towers go dark across downtown, granting reduced machine detection.", "service_downtown_blackout"),
                    new("choice_clinic_grid", "Re-Route Mega-Watt Power to Underworks Clinics", "Guarantees continuous life-support power and unlocks advanced clinic buffs in Downtown Underworks.", "Underground medical facilities receive uninterrupted auxiliary power.", "service_underworks_icu")
                },
                new QuestReward(250, 350, null, null, FactionId.CivicRecovery, 100)
            ),

            // ─── Phase 2: Eastlight Harbor District Chain (3) ──────────────────
            new(
                "quest_hb_01", "Dead in the Water",
                "Reopen clogged drainage valves and boat sluice gates to allow resistance utility craft through the canals.",
                7, QuestCategory.District, 7, null, null,
                new QuestObjective[]
                {
                    new("obj_hb01_1", "Speak with Nami at the boatyard locks.", ObjectiveType.Interact, new WorldPoint(108150, 43850), 12, "npc_nami"),
                    new("obj_hb01_2", "Clear the jammed turbine gear in Canal Lock Alpha.", ObjectiveType.RepairConduit, new WorldPoint(108180, 43820), 10),
                    new("obj_hb01_3", "Test the canal water flood gate.", ObjectiveType.Interact, new WorldPoint(108150, 43850), 12, "npc_nami")
                },
                null,
                new QuestReward(150, 200, null, null, FactionId.IndependentCouriers, 50)
            ),
            new(
                "quest_hb_02", "Smuggler's Horizon",
                "Secure contraband medical crates hidden in Cargo Yard 09 before automated port droids incinerate them.",
                7, QuestCategory.District, 7, "quest_hb_01", null,
                new QuestObjective[]
                {
                    new("obj_hb02_1", "Meet Daichi in the container yard.", ObjectiveType.Interact, new WorldPoint(107900, 44200), 12, "npc_daichi"),
                    new("obj_hb02_2", "Recover 3 tagged medical pallets among the red freight stacks.", ObjectiveType.Investigate, new WorldPoint(107930, 44230), 15),
                    new("obj_hb02_3", "Defend the extraction forklift from harbor security drones.", ObjectiveType.DefeatHostiles, new WorldPoint(107900, 44200), 20),
                    new("obj_hb02_4", "Deliver the cargo manifest to Captain Maro.", ObjectiveType.Interact, new WorldPoint(108010, 44010), 12, "npc_captain_maro")
                },
                null,
                new QuestReward(175, 250, "mod_courier_booster", null, FactionId.IndependentCouriers, 75)
            ),
            new(
                "quest_hb_03", "The Waterfront Choice",
                "Decide whether to militarize the dockworkers' union with heavy armaments or establish a covert boat smuggling fleet.",
                7, QuestCategory.District, 7, "quest_hb_02", null,
                new QuestObjective[]
                {
                    new("obj_hb03_1", "Convene with Captain Maro and Daichi on Tidal Crane Pier.", ObjectiveType.Interact, new WorldPoint(108250, 44100), 16),
                    new("obj_hb03_2", "Choose the waterfront tactical orientation.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(108250, 44100), 16),
                    new("obj_hb03_3", "Confirm harbor resistance directive.", ObjectiveType.Interact, new WorldPoint(108250, 44100), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_harbor_militia", "Arm the Harbor Dockworkers Union", "Equips dockworkers with heavy kinetic rifles, placing armed defense sentries on piers.", "The waterfront is garrisoned by armed dockworkers repelling coastal patrols.", "service_harbor_militia"),
                    new("choice_smuggling_flotilla", "Commission Stealth Flotilla Network", "Unlocks discounted marine upgrades and boat speed enhancements across all canals.", "A fast courier flotilla is established, accelerating all water transit.", "service_boat_network")
                },
                new QuestReward(250, 350, null, null, FactionId.IndependentCouriers, 100)
            ),

            // ─── Phase 2: Citadel Ring District Chain (3) ─────────────────────
            new(
                "quest_ct_01", "Perimeter Bleed",
                "Disable external automated sentry pylons surrounding the outer Citadel perimeter wall.",
                10, QuestCategory.District, 4, null, null,
                new QuestObjective[]
                {
                    new("obj_ct01_1", "Meet Kaelen near the fortified barricade.", ObjectiveType.Interact, new WorldPoint(75800, 76150), 12, "npc_kaelen"),
                    new("obj_ct01_2", "Short-circuit Pylon Relay 1.", ObjectiveType.RepairConduit, new WorldPoint(75830, 76180), 10),
                    new("obj_ct01_3", "Short-circuit Pylon Relay 2.", ObjectiveType.RepairConduit, new WorldPoint(75770, 76120), 10),
                    new("obj_ct01_4", "Return to Kaelen.", ObjectiveType.Interact, new WorldPoint(75800, 76150), 12, "npc_kaelen")
                },
                null,
                new QuestReward(150, 200, null, null, FactionId.ReclamationGuild, 50)
            ),
            new(
                "quest_ct_02", "Captive Telemetry",
                "Extract encrypted machine deployment telemetry from the Spire Lookout listening post.",
                10, QuestCategory.District, 4, "quest_ct_01", null,
                new QuestObjective[]
                {
                    new("obj_ct02_1", "Ascend to the Spire Lookout and speak with Lyra.", ObjectiveType.Interact, new WorldPoint(76250, 75850), 12, "npc_lyra"),
                    new("obj_ct02_2", "Align the telemetry dish toward the central Spire.", ObjectiveType.Investigate, new WorldPoint(76280, 75820), 10),
                    new("obj_ct02_3", "Defend Lyra against inbound airborne assault probes.", ObjectiveType.DefeatHostiles, new WorldPoint(76250, 75850), 20),
                    new("obj_ct02_4", "Extract telemetry drive to Commander Locke.", ObjectiveType.Interact, new WorldPoint(76010, 76010), 12, "npc_commander_locke")
                },
                null,
                new QuestReward(175, 250, "mod_free_signal_tuner", null, FactionId.FreeSignalCollective, 75)
            ),
            new(
                "quest_ct_03", "The Barricade Choice",
                "Breach the inner garrison gate to divert machine patrols away from civilian wards or reinforce local blast doors.",
                10, QuestCategory.District, 4, "quest_ct_02", null,
                new QuestObjective[]
                {
                    new("obj_ct03_1", "Confer with Commander Locke and Lyra at the Silent Ramparts.", ObjectiveType.Interact, new WorldPoint(76150, 76300), 16),
                    new("obj_ct03_2", "Determine the defensive breach strategy.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(76150, 76300), 16),
                    new("obj_ct03_3", "Commit the strategic demolition.", ObjectiveType.Interact, new WorldPoint(76150, 76300), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_breach_garrison", "Demolish Garrison Outer Wall", "Draws machine forces into a choke-point, permanently reducing hostile density in adjacent wards.", "Citadel defensive lines fall into disarray, easing pressure on surrounding districts.", "service_garrison_choke"),
                    new("choice_reinforce_blastdoors", "Seal Civilian Blast-Doors", "Strengthens civilian bunkers against assault, unlocking safehouse defensive upgrades.", "Heavy blast-doors lock in place, ensuring civilian security throughout the ring.", "service_blastdoor_security")
                },
                new QuestReward(250, 350, null, null, FactionId.CivicRecovery, 100)
            ),

            // ─── Phase 2: Rustrail Belt District Chain (3) ─────────────────────
            new(
                "quest_rr_01", "Sparks on the Track",
                "Clear collapsed rail wreckage to reactivate the western freight siding for civilian food convoys.",
                13, QuestCategory.District, 3, null, null,
                new QuestObjective[]
                {
                    new("obj_rr01_1", "Consult Foreman Briggs at the Rustrail Concourse.", ObjectiveType.Interact, new WorldPoint(44010, 108010), 12, "npc_foreman_briggs"),
                    new("obj_rr01_2", "Clear magnetic debris from Switch 04.", ObjectiveType.RepairConduit, new WorldPoint(43850, 108200), 10),
                    new("obj_rr01_3", "Repel scavengers stripping locomotive coils.", ObjectiveType.DefeatHostiles, new WorldPoint(43820, 108230), 18),
                    new("obj_rr01_4", "Signal the track clear to Briggs.", ObjectiveType.Interact, new WorldPoint(44010, 108010), 12, "npc_foreman_briggs")
                },
                null,
                new QuestReward(150, 200, "mod_reclamation_mesh", null, FactionId.ReclamationGuild, 50)
            ),
            new(
                "quest_rr_02", "The Iron Caravan",
                "Ride escort alongside an automated salvage train across the Iron Memory freight corridor.",
                13, QuestCategory.District, 3, "quest_rr_01", null,
                new QuestObjective[]
                {
                    new("obj_rr02_1", "Meet Spike at the switchyard.", ObjectiveType.Interact, new WorldPoint(43850, 108200), 12, "npc_spike"),
                    new("obj_rr02_2", "Escort the train through Ember Railcut, clearing drone mines.", ObjectiveType.Investigate, new WorldPoint(44180, 107900), 16),
                    new("obj_rr02_3", "Eliminate the pursuit combat skiff.", ObjectiveType.DefeatHostiles, new WorldPoint(44220, 107930), 20),
                    new("obj_rr02_4", "Deliver the salvage payload to Vex.", ObjectiveType.Interact, new WorldPoint(44180, 107900), 12, "npc_vex")
                },
                null,
                new QuestReward(175, 250, null, "gadget_decoy_emitter", FactionId.IndependentCouriers, 75)
            ),
            new(
                "quest_rr_03", "The Line Choice",
                "Choose whether to dedicate the restored rail line to food and medical transport or armored resistance combat cars.",
                13, QuestCategory.District, 3, "quest_rr_02", null,
                new QuestObjective[]
                {
                    new("obj_rr03_1", "Convene with Briggs and Vex at Ember Railcut.", ObjectiveType.Interact, new WorldPoint(44300, 108150), 16),
                    new("obj_rr03_2", "Select the locomotive operational profile.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44300, 108150), 16),
                    new("obj_rr03_3", "Ratify rail operations schedule.", ObjectiveType.Interact, new WorldPoint(44300, 108150), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_humanitarian_train", "Dedicate Line to Food & Medical Logistics", "Provides continuous rations to settlements, generating passive credit income in safehouses.", "Freight trains run regular civilian relief schedules across the industrial belt.", "service_relief_train"),
                    new("choice_armored_dreadnought", "Deploy Armored Resistance Rail Dreadnought", "Mounts heavy rail guns along the tracks, granting orbital fire-support in combat.", "An armored combat locomotive patrols the rails, providing heavy artillery support.", "service_armored_train")
                },
                new QuestReward(250, 350, null, null, FactionId.ReclamationGuild, 100)
            ),

            // ─── Phase 2: Foundry Basin District Chain (3) ────────────────────
            new(
                "quest_fd_01", "Quench and Temper",
                "Cool an overheating geothermal pump to prevent molten slag from overflowing into residential quarters.",
                14, QuestCategory.District, 7, null, null,
                new QuestObjective[]
                {
                    new("obj_fd01_1", "Report to Talia at the Geothermal Pump Station.", ObjectiveType.Interact, new WorldPoint(75850, 107900), 12, "npc_talia"),
                    new("obj_fd01_2", "Manually vent Overheated Relief Valve A.", ObjectiveType.RepairConduit, new WorldPoint(75820, 107870), 10),
                    new("obj_fd01_3", "Manually vent Overheated Relief Valve B.", ObjectiveType.RepairConduit, new WorldPoint(75880, 107930), 10),
                    new("obj_fd01_4", "Confirm pressure normalization with Talia.", ObjectiveType.Interact, new WorldPoint(75850, 107900), 12, "npc_talia")
                },
                null,
                new QuestReward(150, 200, null, null, FactionId.FreeSignalCollective, 50)
            ),
            new(
                "quest_fd_02", "The Stoker's Strike",
                "Re-route steam manifolds to shield escaping factory laborers from machine enforcers.",
                14, QuestCategory.District, 7, "quest_fd_01", null,
                new QuestObjective[]
                {
                    new("obj_fd02_1", "Speak with Master Cho in the Foundry Exchange.", ObjectiveType.Interact, new WorldPoint(76010, 108010, 2), 12, "npc_master_cho"),
                    new("obj_fd02_2", "Divert high-pressure steam manifold in Smelting Bay 3.", ObjectiveType.RepairConduit, new WorldPoint(76200, 108150), 12),
                    new("obj_fd02_3", "Destroy the slag gate enforcer droids.", ObjectiveType.DefeatHostiles, new WorldPoint(76230, 108170), 20),
                    new("obj_fd02_4", "Regroup with Goro to confirm worker evacuation.", ObjectiveType.Interact, new WorldPoint(76200, 108150), 12, "npc_goro")
                },
                null,
                new QuestReward(175, 250, "mod_civic_nanobound", null, FactionId.CivicRecovery, 75)
            ),
            new(
                "quest_fd_03", "The Forge Choice",
                "Recommission the ancient Vulcan smelting core for heavy armor plating or high-yield energy conduits.",
                14, QuestCategory.District, 7, "quest_fd_02", null,
                new QuestObjective[]
                {
                    new("obj_fd03_1", "Convene with Master Cho and Talia at Cinderwater Basin.", ObjectiveType.Interact, new WorldPoint(76100, 107800), 16),
                    new("obj_fd03_2", "Direct the core smelting re-calibration.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(76100, 107800), 16),
                    new("obj_fd03_3", "Ignite the Vulcan crucible.", ObjectiveType.Interact, new WorldPoint(76100, 107800), 16)
                },
                new QuestBranchChoice[]
                {
                    new("choice_heavy_plating", "Forge Heavy Kinetic Nanite Armor", "Supplies all safehouses with advanced armor reinforcement plating; boosts max armor.", "The foundry pours tons of dense plating, shielding resistance operations across the city.", "service_heavy_plating"),
                    new("choice_energy_conduits", "Fabricate Super-Conductor Energy Conduits", "Accelerates ability recharge speeds and powers high-output laser defense nodes.", "Super-conductors are routed throughout the grid, accelerating operative ability regeneration.", "service_energy_conduits")
                },
                new QuestReward(250, 350, null, null, FactionId.ReclamationGuild, 100)
            ),

            // ─── Phase 2: Civic Recovery Network Faction Quests (3) ────────────
            new(
                "quest_crn_01", "Field Dressing",
                "Supply emergency field triage kits to civilian shelters operating along the Concrete Viaduct.",
                9, QuestCategory.Faction, 2, null, null,
                new QuestObjective[]
                {
                    new("obj_crn01_1", "Meet Dr. Maya Chen at Shelter 04.", ObjectiveType.Interact, new WorldPoint(43995, 76035), 12, "npc_maya_chen"),
                    new("obj_crn01_2", "Deliver trauma serum crate to North Shelter Siding.", ObjectiveType.DeliverPayload, new WorldPoint(44150, 76350), 15),
                    new("obj_crn01_3", "Report delivery back to Dr. Chen.", ObjectiveType.Interact, new WorldPoint(43995, 76035), 12, "npc_maya_chen")
                },
                null,
                new QuestReward(125, 200, null, null, FactionId.CivicRecovery, 60)
            ),
            new(
                "quest_crn_02", "Structural Integrity",
                "Anchor cracked load-bearing foundation pillars in Downtown Underworks to prevent cave-ins.",
                6, QuestCategory.Faction, 5, "quest_crn_01", null,
                new QuestObjective[]
                {
                    new("obj_crn02_1", "Confer with Dr. Vance in Underworks Terminal 01.", ObjectiveType.Interact, new WorldPoint(76010, 44010, 1), 12, "npc_elena_vance"),
                    new("obj_crn02_2", "Weld hydraulic support jack at Pillar Gamma.", ObjectiveType.RepairConduit, new WorldPoint(75950, 43950, 1), 10),
                    new("obj_crn02_3", "Weld hydraulic support jack at Pillar Delta.", ObjectiveType.RepairConduit, new WorldPoint(76050, 44050, 1), 10),
                    new("obj_crn02_4", "Verify seismic stability with Dr. Vance.", ObjectiveType.Interact, new WorldPoint(76010, 44010, 1), 12, "npc_elena_vance")
                },
                null,
                new QuestReward(150, 250, null, null, FactionId.CivicRecovery, 80)
            ),
            new(
                "quest_crn_03", "Sanctuary Charter",
                "Formally ratify and connect all regional safehouses into a cohesive civilian emergency sanctuary network.",
                5, QuestCategory.Faction, 5, "quest_crn_02", null,
                new QuestObjective[]
                {
                    new("obj_crn03_1", "Assemble Civic Recovery delegates at The Lantern Hearth in Old Kyoto.", ObjectiveType.Interact, new WorldPoint(44020, 44015), 16),
                    new("obj_crn03_2", "Sign the Unified Sanctuary Charter.", ObjectiveType.Interact, new WorldPoint(44020, 44015), 12)
                },
                null,
                new QuestReward(200, 350, "mod_civic_nanobound", null, FactionId.CivicRecovery, 120)
            ),

            // ─── Phase 2: Independent Couriers Faction Quests (3) ──────────────
            new(
                "quest_cou_01", "Express Run",
                "Complete a high-speed express delivery run across the Rustrail industrial arterial corridor in under 90 seconds.",
                13, QuestCategory.Faction, 3, null, null,
                new QuestObjective[]
                {
                    new("obj_cou01_1", "Pick up express package from Spike at the switchyard.", ObjectiveType.Interact, new WorldPoint(43850, 108200), 12, "npc_spike"),
                    new("obj_cou01_2", "Deliver package to Foreman Briggs at the concourse.", ObjectiveType.DeliverPayload, new WorldPoint(44010, 108010), 14, "npc_foreman_briggs")
                },
                null,
                new QuestReward(125, 200, null, null, FactionId.IndependentCouriers, 60)
            ),
            new(
                "quest_cou_02", "Signal Beacons",
                "Deploy waypoint marker beacons along the Eastlight Harbor canal route to guide nocturnal courier boats.",
                7, QuestCategory.Faction, 7, "quest_cou_01", null,
                new QuestObjective[]
                {
                    new("obj_cou02_1", "Take the beacon array from Captain Maro at the docks.", ObjectiveType.Interact, new WorldPoint(108010, 44010), 12, "npc_captain_maro"),
                    new("obj_cou02_2", "Plant Navigation Beacon 1 on Channel Pylon North.", ObjectiveType.Investigate, new WorldPoint(108100, 43900), 12),
                    new("obj_cou02_3", "Plant Navigation Beacon 2 on Channel Pylon South.", ObjectiveType.Investigate, new WorldPoint(108200, 44150), 12),
                    new("obj_cou02_4", "Return to Maro to confirm telemetry uplink.", ObjectiveType.Interact, new WorldPoint(108010, 44010), 12, "npc_captain_maro")
                },
                null,
                new QuestReward(150, 250, null, null, FactionId.IndependentCouriers, 80)
            ),
            new(
                "quest_cou_03", "The Open Road",
                "Assault and dismantle an automated machine security tollgate blocking the coastal viaduct.",
                7, QuestCategory.Faction, 7, "quest_cou_02", null,
                new QuestObjective[]
                {
                    new("obj_cou03_1", "Meet Captain Maro at Tidal Crane Pier.", ObjectiveType.Interact, new WorldPoint(108250, 44100), 12, "npc_captain_maro"),
                    new("obj_cou03_2", "Destroy the tollgate power capacitor.", ObjectiveType.RepairConduit, new WorldPoint(108300, 44120), 10),
                    new("obj_cou03_3", "Eliminate the automated barrier response drones.", ObjectiveType.DefeatHostiles, new WorldPoint(108280, 44100), 20),
                    new("obj_cou03_4", "Celebrate the liberated arterial highway with Maro.", ObjectiveType.Interact, new WorldPoint(108250, 44100), 12, "npc_captain_maro")
                },
                null,
                new QuestReward(200, 350, "mod_courier_booster", null, FactionId.IndependentCouriers, 120)
            ),

            // ─── Phase 2: Reclamation Guild Faction Quests (3) ─────────────────
            new(
                "quest_rec_01", "Scrap Tithe",
                "Recover 5 intact heavy servo-actuators from the Foundry slag disposal channels.",
                14, QuestCategory.Faction, 7, null, null,
                new QuestObjective[]
                {
                    new("obj_rec01_1", "Check in with Master Cho at the Foundry Exchange.", ObjectiveType.Interact, new WorldPoint(76010, 108010, 2), 12, "npc_master_cho"),
                    new("obj_rec01_2", "Extract servo parts from 3 slag heaps in the quench channel.", ObjectiveType.Investigate, new WorldPoint(76100, 107800), 16),
                    new("obj_rec01_3", "Deliver salvage components back to Master Cho.", ObjectiveType.Interact, new WorldPoint(76010, 108010, 2), 12, "npc_master_cho")
                },
                null,
                new QuestReward(125, 200, null, null, FactionId.ReclamationGuild, 60)
            ),
            new(
                "quest_rec_02", "Reverse Engineered",
                "Calibrate the Guild's modular component workbench at the Rustrail salvage depot.",
                13, QuestCategory.Faction, 3, "quest_rec_01", null,
                new QuestObjective[]
                {
                    new("obj_rec02_1", "Speak with Vex at the Rustrail Salvage Depot.", ObjectiveType.Interact, new WorldPoint(44180, 107900), 12, "npc_vex"),
                    new("obj_rec02_2", "Calibrate high-frequency laser cutter.", ObjectiveType.RepairConduit, new WorldPoint(44200, 107920), 10),
                    new("obj_rec02_3", "Test component stress tolerances.", ObjectiveType.Investigate, new WorldPoint(44180, 107900), 12)
                },
                null,
                new QuestReward(150, 250, null, null, FactionId.ReclamationGuild, 80)
            ),
            new(
                "quest_rec_03", "Master Fabricator",
                "Forge an advanced resistance composite exoskeleton component at the Vulcan crucible.",
                14, QuestCategory.Faction, 7, "quest_rec_02", null,
                new QuestObjective[]
                {
                    new("obj_rec03_1", "Confer with Master Cho and Barik in the Foundry Molten Refuge.", ObjectiveType.Interact, new WorldPoint(76025, 108020, 2), 16),
                    new("obj_rec03_2", "Pour the molten composite alloy into the forge mold.", ObjectiveType.Interact, new WorldPoint(76025, 108020, 2), 12)
                },
                null,
                new QuestReward(200, 350, "mod_reclamation_mesh", null, FactionId.ReclamationGuild, 120)
            ),

            // ─── Phase 2: Free Signal Collective Faction Quests (3) ────────────
            new(
                "quest_fsc_01", "Whisper Network",
                "Tap into an encrypted Citadel surveillance trunk line and redirect raw video feeds to the Collective.",
                10, QuestCategory.Faction, 4, null, null,
                new QuestObjective[]
                {
                    new("obj_fsc01_1", "Meet Lyra at the Spire Lookout.", ObjectiveType.Interact, new WorldPoint(76250, 75850), 12, "npc_lyra"),
                    new("obj_fsc01_2", "Splice fiber-optic tap into Citadel Trunk 09.", ObjectiveType.RepairConduit, new WorldPoint(76210, 75870), 10),
                    new("obj_fsc01_3", "Confirm data packet reception with Lyra.", ObjectiveType.Interact, new WorldPoint(76250, 75850), 12, "npc_lyra")
                },
                null,
                new QuestReward(125, 200, null, null, FactionId.FreeSignalCollective, 60)
            ),
            new(
                "quest_fsc_02", "Static Shield",
                "Deploy portable jamming units around the Downtown Core to blind machine drone telemetry sweeps.",
                6, QuestCategory.Faction, 5, "quest_fsc_01", null,
                new QuestObjective[]
                {
                    new("obj_fsc02_1", "Meet Kazuya near the Registry Needle.", ObjectiveType.Interact, new WorldPoint(75850, 43900), 12, "npc_kazuya"),
                    new("obj_fsc02_2", "Activate Jammer Alpha on Skyline Marquee.", ObjectiveType.Investigate, new WorldPoint(75880, 43920), 12),
                    new("obj_fsc02_3", "Activate Jammer Beta on Underworks Exhaust.", ObjectiveType.Investigate, new WorldPoint(75820, 43880), 12),
                    new("obj_fsc02_4", "Confirm sensor distortion with Kazuya.", ObjectiveType.Interact, new WorldPoint(75850, 43900), 12, "npc_kazuya")
                },
                null,
                new QuestReward(150, 250, null, "gadget_smoke_screen", FactionId.FreeSignalCollective, 80)
            ),
            new(
                "quest_fsc_03", "Free Frequency",
                "Broadcast the unredacted truth of the resistance across every holographic billboard in Downtown.",
                6, QuestCategory.Faction, 5, "quest_fsc_02", null,
                new QuestObjective[]
                {
                    new("obj_fsc03_1", "Convene with Lyra and Kazuya at the Commercial Grid Substation.", ObjectiveType.Interact, new WorldPoint(76100, 43800), 16),
                    new("obj_fsc03_2", "Overpower the central broadcast transmitter.", ObjectiveType.RepairConduit, new WorldPoint(76120, 43820), 10),
                    new("obj_fsc03_3", "Defend transmitter antenna against aerial purge drones.", ObjectiveType.DefeatHostiles, new WorldPoint(76100, 43800), 22),
                    new("obj_fsc03_4", "Trigger the citywide unredacted transmission.", ObjectiveType.Interact, new WorldPoint(76100, 43800), 12)
                },
                null,
                new QuestReward(200, 350, "mod_free_signal_tuner", null, FactionId.FreeSignalCollective, 120)
            )
        };
    }

    // ─── Regional Challenges (Phase 1: 6 + Phase 2: 15 = 21 Total) ─────────────
    public static readonly IReadOnlyList<RegionalChallenge> Challenges = new[]
    {
        // Old Kyoto (5)
        new RegionalChallenge("chal_ok_race", "Lantern Circuit Time Trial", 5, ChallengeType.Race, new WorldPoint(44020, 44030), 45.0f, 60.0f, 100, 150),
        new RegionalChallenge("chal_ok_puzzle", "Palace Rooftops Precision Dash", 5, ChallengeType.OperativePuzzle, new WorldPoint(44240, 43880), 40.0f, 55.0f, 100, 150),
        new RegionalChallenge("chal_ok_trail", "Moonbridge Inscriptions Trail", 5, ChallengeType.DiscoveryTrail, new WorldPoint(44140, 44340), 90.0f, 120.0f, 100, 150),

        // Concrete Wards (9)
        new RegionalChallenge("chal_cw_race", "Rainline Viaduct Sprint", 9, ChallengeType.Race, new WorldPoint(43760, 76220), 35.0f, 50.0f, 100, 150),
        new RegionalChallenge("chal_cw_puzzle", "Magnetic Conduit Coupling", 9, ChallengeType.OperativePuzzle, new WorldPoint(44290, 75920), 45.0f, 65.0f, 100, 150),
        new RegionalChallenge("chal_cw_trail", "Echoes of the Wards Trail", 9, ChallengeType.DiscoveryTrail, new WorldPoint(44140, 76340), 90.0f, 120.0f, 100, 150),

        // Downtown Core (6)
        new RegionalChallenge("chal_dt_race", "Skyline Highway Sprint", 6, ChallengeType.Race, new WorldPoint(76020, 44030), 40.0f, 55.0f, 100, 150),
        new RegionalChallenge("chal_dt_puzzle", "Underworks Circuit Alignment", 6, ChallengeType.OperativePuzzle, new WorldPoint(75860, 43920), 45.0f, 60.0f, 100, 150),
        new RegionalChallenge("chal_dt_trail", "Neon Ghost Trail", 6, ChallengeType.DiscoveryTrail, new WorldPoint(76120, 43820), 90.0f, 120.0f, 100, 150),

        // Eastlight Harbor (7)
        new RegionalChallenge("chal_hb_race", "Harbor Canal Boat Slalom", 7, ChallengeType.Race, new WorldPoint(108160, 43860), 45.0f, 60.0f, 100, 150),
        new RegionalChallenge("chal_hb_puzzle", "Tidal Crane Girders", 7, ChallengeType.OperativePuzzle, new WorldPoint(108260, 44120), 40.0f, 55.0f, 100, 150),
        new RegionalChallenge("chal_hb_trail", "Sunken Cargo Log", 7, ChallengeType.DiscoveryTrail, new WorldPoint(107920, 44220), 90.0f, 120.0f, 100, 150),

        // Citadel Ring (10)
        new RegionalChallenge("chal_ct_race", "Perimeter Barricade Run", 10, ChallengeType.Race, new WorldPoint(76020, 76030), 40.0f, 55.0f, 100, 150),
        new RegionalChallenge("chal_ct_puzzle", "Aegis Sensor Override", 10, ChallengeType.OperativePuzzle, new WorldPoint(75820, 76170), 50.0f, 70.0f, 100, 150),
        new RegionalChallenge("chal_ct_trail", "Silent Ramparts Observation", 10, ChallengeType.DiscoveryTrail, new WorldPoint(76160, 76320), 90.0f, 120.0f, 100, 150),

        // Rustrail Belt (13)
        new RegionalChallenge("chal_rr_race", "Locomotive Siding Time Trial", 13, ChallengeType.Race, new WorldPoint(44020, 108030), 42.0f, 58.0f, 100, 150),
        new RegionalChallenge("chal_rr_puzzle", "Switchyard Turntable Puzzle", 13, ChallengeType.OperativePuzzle, new WorldPoint(43860, 108220), 45.0f, 65.0f, 100, 150),
        new RegionalChallenge("chal_rr_trail", "Iron Memory Archive Trail", 13, ChallengeType.DiscoveryTrail, new WorldPoint(44310, 108170), 90.0f, 120.0f, 100, 150),

        // Foundry Basin (14)
        new RegionalChallenge("chal_fd_race", "Thermal Vent Scramble", 14, ChallengeType.Race, new WorldPoint(76020, 108030), 38.0f, 52.0f, 100, 150),
        new RegionalChallenge("chal_fd_puzzle", "Molten Siphon Alignment", 14, ChallengeType.OperativePuzzle, new WorldPoint(75860, 107920), 45.0f, 60.0f, 100, 150),
        new RegionalChallenge("chal_fd_trail", "Crucible Inscription Trail", 14, ChallengeType.DiscoveryTrail, new WorldPoint(76120, 107820), 90.0f, 120.0f, 100, 150)
    };

    // ─── Replayable Side Operations (Phase 1: 2 + Phase 2: 5 = 7 Total) ────────
    public static readonly IReadOnlyList<SideOperationDefinition> SideOperations = new[]
    {
        // Phase 1 Operations
        new SideOperationDefinition("side_op_palace_vault", "Palace Vault Reclamation", "Infiltration & Recovery", 5, new WorldPoint(44270, 43930), "res://scenes/world/side_ops/palace_vault_op.tscn", 150, 200),
        new SideOperationDefinition("side_op_viaduct_defense", "Viaduct Grid Defense", "Sabotage & Defense", 9, new WorldPoint(43780, 76240), "res://scenes/world/side_ops/viaduct_defense_op.tscn", 150, 200),

        // Phase 2 Urban Operations
        new SideOperationDefinition("side_op_data_haven", "Data Haven Infiltration", "Cyber-Infiltration & Data Extraction", 6, new WorldPoint(75870, 43910), "res://scenes/world/side_ops/data_haven_op.tscn", 175, 250),
        new SideOperationDefinition("side_op_harbor_slipway", "Harbor Slipway Sabotage", "Amphibious Assault & Demolition", 7, new WorldPoint(107920, 44220), "res://scenes/world/side_ops/harbor_slipway_op.tscn", 175, 250),
        new SideOperationDefinition("side_op_citadel_perimeter", "Citadel Perimeter Breach", "Heavy Assault & Radar Sabotage", 10, new WorldPoint(76170, 76320), "res://scenes/world/side_ops/citadel_perimeter_op.tscn", 175, 250),
        new SideOperationDefinition("side_op_freight_intercept", "Freight Line Interception", "Moving Locomotive Battle & Recovery", 13, new WorldPoint(44200, 107920), "res://scenes/world/side_ops/freight_intercept_op.tscn", 175, 250),
        new SideOperationDefinition("side_op_smelting_core", "Smelting Core Shutdown", "Hazard Navigation & Drone Factory Sabotage", 14, new WorldPoint(76220, 108170), "res://scenes/world/side_ops/smelting_core_op.tscn", 175, 250)
    };
}
