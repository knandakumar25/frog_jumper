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
        20.0f,
        8.0f,
        1.2f,
        false,
        0
    );

    public static readonly VehicleDefinition UtilityBoat = new(
        "veh_utility_boat",
        "Nami-Runner Utility Boat",
        14.0f,
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

    // ─── Interiors & Safehouses (16 Regional Safehouses + 2 Secondary Facilities = 18 Total) ─
    public static readonly IReadOnlyList<InteriorDefinition> Interiors = BuildInteriors();

    private static InteriorDefinition[] BuildInteriors()
    {
        return new InteriorDefinition[]
        {
            // All 16 Regional Safehouses
            new("safehouse_cedarcrest", "Monastery High Refuge (Angeles Crest Safehouse)", "pocket_r0_hub", new WorldPoint(12020, 12015), "safehouse", new[] { "rest_heal", "mountain_shrine", "meditation_mat" }),
            new("safehouse_cloudstep", "Floodgate Lookout Cabin (Castaic Reservoir Safehouse)", "pocket_r1_hub", new WorldPoint(44025, 12020), "safehouse", new[] { "rest_heal", "boat_dock", "sluice_controls" }),
            new("safehouse_stormbreak", "Storm Saddle Shelter (San Gabriel Peaks Safehouse)", "pocket_r2_hub", new WorldPoint(76020, 12015), "safehouse", new[] { "rest_heal", "lightning_grounding", "high_altitude_triage" }),
            new("safehouse_northlight", "Northlight Beacon Hold (Malibu Coast Safehouse)", "pocket_r3_hub", new WorldPoint(108025, 12020), "safehouse", new[] { "rest_heal", "coastal_radio", "boat_dock" }),
            new("safehouse_cedarwild", "Cedarwild Canopy Blind (Topanga Canyon Safehouse)", "pocket_r4_hub", new WorldPoint(12020, 44015), "safehouse", new[] { "rest_heal", "herbal_apothecary", "canopy_lookout" }),
            new("safehouse_old_kyoto", "The Lantern Hearth (Little Tokyo Safehouse)", "ok_pocket_hub", new WorldPoint(44020, 44015), "safehouse", new[] { "rest_heal", "tactical_loadout", "squad_briefing", "garage_bay" }),
            new("safehouse_downtown", "Underground Terminal 01 (Downtown LA Safehouse)", "dt_pocket_hub", new WorldPoint(76020, 44015, 1), "safehouse", new[] { "rest_heal", "surveillance_tap", "signal_scramble", "transit_terminal" }),
            new("safehouse_harbor", "Old Customs Drydock (San Pedro Harbor Safehouse)", "hb_pocket_hub", new WorldPoint(108025, 44020), "safehouse", new[] { "rest_heal", "boat_dock", "smuggling_cache", "cargo_terminal" }),
            new("safehouse_solar_belt", "Solar Array Station 08 (Mojave Solar Safehouse)", "pocket_r8_hub", new WorldPoint(12020, 76015), "safehouse", new[] { "rest_heal", "solar_cell_charge", "battery_bank" }),
            new("safehouse_concrete_wards", "Sub-Viaduct Shelter 04 (LA River Concrete Safehouse)", "cw_pocket_hub", new WorldPoint(44025, 76015), "safehouse", new[] { "medical_clinic", "generator_workbench", "transit_terminal" }),
            new("safehouse_citadel", "Forward Outpost Delta (Century Citadel Safehouse)", "ct_pocket_hub", new WorldPoint(76020, 76015), "safehouse", new[] { "rest_heal", "aegis_recalibration", "tactical_briefing", "covert_entry" }),
            new("safehouse_tideglass", "Tidepool Harbor Shack (Santa Monica Coast Safehouse)", "pocket_r11_hub", new WorldPoint(108025, 76020), "safehouse", new[] { "rest_heal", "marine_salvage", "boat_dock" }),
            new("safehouse_kurogane", "Blackstone Overlook Hermitage (Kurogane Ridge Safehouse)", "pocket_r12_hub", new WorldPoint(12020, 108015), "safehouse", new[] { "rest_heal", "sword_polishing", "ancestral_altar" }),
            new("safehouse_rustrail", "Depot Maintenance Bunker (Alameda Rustrail Safehouse)", "rr_pocket_hub", new WorldPoint(44020, 108015), "safehouse", new[] { "rest_heal", "rail_car_workshop", "scrap_refinery", "heavy_storage" }),
            new("safehouse_foundry", "Sub-Furnace Refuge (Vernon Foundry Safehouse)", "fd_pocket_hub", new WorldPoint(76025, 108020, 2), "safehouse", new[] { "rest_heal", "thermal_shielding", "forge_workbench", "coolant_terminal" }),
            new("safehouse_wetlands", "Wetlands Research Stilt-House (Ballona Wetlands Safehouse)", "pocket_r15_hub", new WorldPoint(108020, 108015), "safehouse", new[] { "rest_heal", "water_filtration", "botanical_press" }),

            // Secondary Story Facilities
            new("interior_palace_archives", "Kurogane Ancient Archives", "ok_pocket_palace_investigation", new WorldPoint(44260, 43910), "archive", new[] { "historical_codex", "relic_study" }),
            new("interior_viaduct_clinic", "District 9 Emergency Clinic", "cw_pocket_hub", new WorldPoint(43990, 76030), "clinic", new[] { "first_aid", "serum_synthesis" })
        };
    }

    // ─── Named Residents (48 Residents: 3 per Region × 16 Regions) ───────────
    public static readonly IReadOnlyList<NamedResident> Residents = BuildResidents();

    private static NamedResident[] BuildResidents()
    {
        return new NamedResident[]
        {
            // Region 0: Cedarcrest Highlands
            new("npc_jin", "Abbot Jin", "Highland Monastery Elder", "Civic Recovery Network", 0, "pocket_r0_hub", new WorldPoint(12010, 12010), new WorldPoint(12020, 12015), "May the mountain wind calm your blade, operative.", "dialog_jin"),
            new("npc_kira", "Kira", "Highland Lookout Scout", "Free Signal Collective", 0, "pocket_r0_sat1", new WorldPoint(12150, 11850), new WorldPoint(12020, 12015), "From up here, we see machine convoys three valleys away.", "dialog_kira"),
            new("npc_dan", "Ronin Dan", "Master Trail Guide", "Independent Couriers", 0, "pocket_r0_sat2", new WorldPoint(11900, 12200), new WorldPoint(12020, 12015), "These switchbacks will test your balance and your machine.", "dialog_dan"),

            // Region 1: Cloudstep Reservoir
            new("npc_moria", "Overseer Moria", "Reservoir Gate Master", "Reclamation Guild", 1, "pocket_r1_hub", new WorldPoint(44010, 12010), new WorldPoint(44025, 12020), "One wrong pressure valve and half the lower valley floods.", "dialog_moria"),
            new("npc_finn", "Finn", "Hydro-Turbine Technician", "Reclamation Guild", 1, "pocket_r1_sat1", new WorldPoint(44150, 11850), new WorldPoint(44025, 12020), "Keep the silt off the generator coils or we lose all grid power.", "dialog_finn"),
            new("npc_mira", "Mira", "Sunken History Diver", "Free Signal Collective", 1, "pocket_r1_sat2", new WorldPoint(43900, 12200), new WorldPoint(44025, 12020), "There are pre-war server vaults sleeping beneath forty fathoms of water.", "dialog_mira"),

            // Region 2: Stormbreak Peaks
            new("npc_vance", "Ranger Vance", "Weather Observatory Director", "Civic Recovery Network", 2, "pocket_r2_hub", new WorldPoint(76010, 12010), new WorldPoint(76020, 12015), "The thunder here isn't weather; it's machine atmospheric terraforming.", "dialog_vance"),
            new("npc_tesla", "Tesla", "High-Voltage Coil Engineer", "Reclamation Guild", 2, "pocket_r2_sat1", new WorldPoint(76150, 11850), new WorldPoint(76020, 12015), "Grounding arrays are holding, but only just. Watch the lightning arcs.", "dialog_tesla"),
            new("npc_ania", "Ania", "Alpine Climber & Courier", "Independent Couriers", 2, "pocket_r2_sat2", new WorldPoint(75900, 12200), new WorldPoint(76020, 12015), "Frostbite and laser sentries — just another day on the peaks.", "dialog_ania"),

            // Region 3: Northlight Coast
            new("npc_nils", "Keeper Nils", "Beacon Lightmaster", "Civic Recovery Network", 3, "pocket_r3_hub", new WorldPoint(108010, 12010), new WorldPoint(108025, 12020), "This light guided fishing skiffs before the machines ever landed.", "dialog_nils"),
            new("npc_isla", "Isla", "Coastal Smuggler", "Independent Couriers", 3, "pocket_r3_sat1", new WorldPoint(108150, 11850), new WorldPoint(108025, 12020), "Coast patrol sweeps every twenty minutes. We move in the blind intervals.", "dialog_isla"),
            new("npc_soren", "Soren", "Marine Salvage Scavenger", "Reclamation Guild", 3, "pocket_r3_sat2", new WorldPoint(107900, 12200), new WorldPoint(108025, 12020), "Found a machine patrol craft grounded on the shoals. Ripe for stripping.", "dialog_soren"),

            // Region 4: Cedarwild Preserve
            new("npc_ray", "Warden Ray", "Ancient Forest Ranger", "Civic Recovery Network", 4, "pocket_r4_hub", new WorldPoint(12010, 44010), new WorldPoint(12020, 44015), "Nature is reclaiming these ruins faster than the machines can pave them.", "dialog_ray"),
            new("npc_tatsu", "Tatsu", "Wildlife Tracker", "Free Signal Collective", 4, "pocket_r4_sat1", new WorldPoint(12150, 43850), new WorldPoint(12020, 44015), "Even the wolves know to steer clear of the red sensor eyes.", "dialog_tatsu"),
            new("npc_botan", "Botan", "Herbal Bio-Chemist", "Civic Recovery Network", 4, "pocket_r4_sat2", new WorldPoint(11900, 44200), new WorldPoint(12020, 44015), "Forest canopy roots filter out the industrial chemical poisons.", "dialog_botan"),

            // Region 5: Little Tokyo
            new("npc_sora", "Sora", "Lantern Artisan & Civic Organizer", "Civic Recovery Network", 5, "ok_pocket_lantern_market", new WorldPoint(43820, 44210), new WorldPoint(44025, 44018), "Welcome back to Little Tokyo. The lanterns still burn.", "dialog_sora"),
            new("npc_gen", "Master Gen", "Historic Archivist & Memory Keeper", "Free Signal Collective", 5, "ok_pocket_palace_investigation", new WorldPoint(44255, 43905), new WorldPoint(44155, 44355), "Stone remembers what circuits forget.", "dialog_gen"),
            new("npc_ren", "Ren", "Independent Courier & Master Mechanic", "Independent Couriers", 5, "ok_pocket_hub", new WorldPoint(44010, 44005), new WorldPoint(43830, 44220), "Need a tune-up on that Echo-Rider?", "dialog_ren"),

            // Region 6: Downtown Core
            new("npc_elena_vance", "Dr. Elena Vance", "Civic Recovery Director", "Civic Recovery Network", 6, "dt_pocket_hub", new WorldPoint(76010, 44010, 1), new WorldPoint(76020, 44015, 1), "Half our people survive down in these underworks.", "dialog_elena"),
            new("npc_kazuya", "Kazuya", "Cyber-Infiltration Specialist", "Free Signal Collective", 6, "dt_pocket_grid_relay", new WorldPoint(75850, 43900), new WorldPoint(76120, 44100), "Every camera here reports to Citadel mainframes.", "dialog_kazuya"),
            new("npc_aoi", "Aoi", "Neon District Information Broker", "Independent Couriers", 6, "dt_pocket_market", new WorldPoint(76200, 44150), new WorldPoint(76020, 44015, 1), "Keep your head low and your tires warm.", "dialog_aoi"),

            // Region 7: Eastlight Harbor
            new("npc_captain_maro", "Captain Maro", "Waterfront Courier Master", "Independent Couriers", 7, "hb_pocket_hub", new WorldPoint(108010, 44010), new WorldPoint(108025, 44020), "Waterways are the only roads machines can't pave over.", "dialog_maro"),
            new("npc_nami", "Nami", "Boat Mechanic & Sluice Operator", "Reclamation Guild", 7, "hb_pocket_boatyard", new WorldPoint(108150, 43850), new WorldPoint(108025, 44020), "Mind the turbine intake valves when throttling up.", "dialog_nami"),
            new("npc_daichi", "Daichi", "Dockworkers Union Steward", "Civic Recovery Network", 7, "hb_pocket_docks", new WorldPoint(107900, 44200), new WorldPoint(108025, 44020), "Our cargo loaders are ready to move medical supplies.", "dialog_daichi"),

            // Region 8: Solar Belt
            new("npc_amara", "Elder Amara", "Solar Commune Matriarch", "Civic Recovery Network", 8, "pocket_r8_hub", new WorldPoint(12010, 76010), new WorldPoint(12020, 76015), "The sun shines equally on machines and men. We capture its grace.", "dialog_amara"),
            new("npc_ravi", "Ravi", "Photovoltaic Array Mechanic", "Reclamation Guild", 8, "pocket_r8_sat1", new WorldPoint(12150, 75850), new WorldPoint(12020, 76015), "Dust storms cut array efficiency by half. We clean them by hand.", "dialog_ravi"),
            new("npc_zuri", "Zuri", "Desert Speed Trialist", "Independent Couriers", 8, "pocket_r8_sat2", new WorldPoint(11900, 76200), new WorldPoint(12020, 76015), "Wide open roads, clear skies, and twenty meters per second.", "dialog_zuri"),

            // Region 9: Concrete Wards
            new("npc_maya_chen", "Dr. Maya Chen", "Trauma Surgeon & Shelter Director", "Civic Recovery Network", 9, "cw_pocket_hub", new WorldPoint(43995, 76035), new WorldPoint(44020, 76010), "Every day the viaduct stands is another day our patients can sleep safely.", "dialog_maya"),
            new("npc_barik", "Barik", "Chief Viaduct Engineer", "Reclamation Guild", 9, "cw_pocket_viaduct_station", new WorldPoint(43760, 76210), new WorldPoint(44015, 76020), "Heavy conduit alignment isn't delicate work, operative.", "dialog_barik"),
            new("npc_yuki", "Yuki", "Signal Runner & Surveillance Scout", "Free Signal Collective", 9, "cw_pocket_courtyard", new WorldPoint(44310, 75915), new WorldPoint(44160, 76360), "Give me a distraction and I'll slice their surveillance feeds.", "dialog_yuki"),

            // Region 10: Citadel Ring
            new("npc_commander_locke", "Commander Locke", "Garrison Defector & Strategist", "Civic Recovery Network", 10, "ct_pocket_hub", new WorldPoint(76010, 76010), new WorldPoint(76020, 76015), "We know this machine ring's weak points down to the millimeter.", "dialog_locke"),
            new("npc_lyra", "Lyra", "Cryptographic Signal Weaver", "Free Signal Collective", 10, "ct_pocket_spire_lookout", new WorldPoint(76250, 75850), new WorldPoint(76020, 76015), "Give me time at the radar terminal and I'll blind their outer eyes.", "dialog_lyra"),
            new("npc_kaelen", "Kaelen", "Security Pylon Saboteur", "Reclamation Guild", 10, "ct_pocket_perimeter_gate", new WorldPoint(75800, 76150), new WorldPoint(76020, 76015), "Short their grounding coils and the laser fence collapses.", "dialog_kaelen"),

            // Region 11: Tideglass Coast
            new("npc_kai", "Elder Kai", "Coastal Fisher & Tide Master", "Civic Recovery Network", 11, "pocket_r11_hub", new WorldPoint(108010, 76010), new WorldPoint(108025, 76020), "The sea gave us food before machines ruled, and it will after they rust.", "dialog_kai"),
            new("npc_coral", "Coral", "Marine Biologist & Chemist", "Free Signal Collective", 11, "pocket_r11_sat1", new WorldPoint(108150, 75850), new WorldPoint(108025, 76020), "Tidepools here concentrate rare minerals that neutralize machine armor.", "dialog_coral"),
            new("npc_jax", "Jax", "Nautical Rigging Specialist", "Independent Couriers", 11, "pocket_r11_sat2", new WorldPoint(107900, 76200), new WorldPoint(108025, 76020), "Rigging boats to out-turn automated torpedo drones is an art form.", "dialog_jax"),

            // Region 12: Kurogane Escarpment
            new("npc_riku", "Shrinekeeper Riku", "Ancestral Blade Keeper", "Free Signal Collective", 12, "pocket_r12_hub", new WorldPoint(12010, 108010), new WorldPoint(12020, 108015), "The spirit of the blade honors only those who respect discipline.", "dialog_riku"),
            new("npc_hanzo", "Hanzo", "Master Weaponsmith", "Reclamation Guild", 12, "pocket_r12_sat1", new WorldPoint(12150, 107850), new WorldPoint(12020, 108015), "Folded high-carbon steel can cleave titanium chassis if tempered right.", "dialog_hanzo"),
            new("npc_kasumi", "Kasumi", "Shadow Path Infiltrator", "Independent Couriers", 12, "pocket_r12_sat2", new WorldPoint(11900, 108200), new WorldPoint(12020, 108015), "These razor cliffs have secret footholds known only to our clan.", "dialog_kasumi"),

            // Region 13: Rustrail Belt
            new("npc_foreman_briggs", "Foreman Briggs", "Reclamation Guild Railmaster", "Reclamation Guild", 13, "rr_pocket_hub", new WorldPoint(44010, 108010), new WorldPoint(44020, 108015), "We get these locomotives moving, and the resistance eats tonight.", "dialog_briggs"),
            new("npc_spike", "Spike", "High-Velocity Rail Runner", "Independent Couriers", 13, "rr_pocket_switchyard", new WorldPoint(43850, 108200), new WorldPoint(44020, 108015), "Riding alongside freight trains at 20 meters a second is my passion.", "dialog_spike"),
            new("npc_vex", "Vex", "Heavy Armature Welder", "Civic Recovery Network", 13, "rr_pocket_salvage_depot", new WorldPoint(44180, 107900), new WorldPoint(44020, 108015), "Give me ten tons of chassis and I will fortify every bunker.", "dialog_vex"),

            // Region 14: Foundry Basin
            new("npc_master_cho", "Master Cho", "Chief Metallurgist & Guild Elder", "Reclamation Guild", 14, "fd_pocket_hub", new WorldPoint(76010, 108010, 2), new WorldPoint(76025, 108020, 2), "The heat here melts inferior steel and resolve alike.", "dialog_cho"),
            new("npc_talia", "Talia", "Geothermal Coolant Specialist", "Free Signal Collective", 14, "fd_pocket_pump_station", new WorldPoint(75850, 107900), new WorldPoint(76025, 108020, 2), "Help me bleed this excess steam before the pressure valves pop.", "dialog_talia"),
            new("npc_goro", "Goro", "Heavy Armor Fabricator", "Civic Recovery Network", 14, "fd_pocket_smelting_bay", new WorldPoint(76200, 108150), new WorldPoint(76025, 108020, 2), "Our operatives need armor that laughs at plasma fire.", "dialog_goro"),

            // Region 15: Reclamation Wetlands
            new("npc_silt", "Dr. Silt", "Ecological Restoration Specialist", "Civic Recovery Network", 15, "pocket_r15_hub", new WorldPoint(108010, 108010), new WorldPoint(108020, 108015), "The wetlands filter heavy metals and machine waste out of the river.", "dialog_silt"),
            new("npc_reed", "Reed", "Boardwalk Scout & Courier", "Independent Couriers", 15, "pocket_r15_sat1", new WorldPoint(108150, 107850), new WorldPoint(108020, 108015), "Stick to the reinforced cedar boardwalks; the marshes swallow bikes whole.", "dialog_reed"),
            new("npc_lin", "Dr. Lin", "Water Chemistry Pioneer", "Reclamation Guild", 15, "pocket_r15_sat2", new WorldPoint(107900, 108200), new WorldPoint(108020, 108015), "Our bio-filters are restoring drinking water for two million civilians.", "dialog_lin")
        };
    }

    // ─── Activity Pockets (Exactly 64 Pockets: 4 per Region × 16 Regions) ────
    public static readonly IReadOnlyList<ActivityPocket> Pockets = BuildPockets();

    private static ActivityPocket[] BuildPockets()
    {
        var list = new List<ActivityPocket>();

        // Phase 1: Little Tokyo (5)
        list.Add(new("ok_pocket_hub", "Little Tokyo Arrival Hub & Lantern Hearth", 5, new WorldPoint(44000, 44000), 96.0, true, new[] { "npc_ren" }, new[] { "safehouse_old_kyoto" }, new[] { "chal_ok_race" }));
        list.Add(new("ok_pocket_lantern_market", "Lantern Market & Artisan Quarter", 5, new WorldPoint(43800, 44200), 80.0, true, new[] { "npc_sora" }, Array.Empty<string>(), new[] { "quest_ok_01", "chal_ok_puzzle" }));
        list.Add(new("ok_pocket_palace_investigation", "Palace Walls & Historic Underway", 5, new WorldPoint(44250, 43900), 90.0, false, new[] { "npc_gen" }, new[] { "interior_palace_archives" }, new[] { "quest_ok_02", "side_op_palace_vault" }));
        list.Add(new("ok_pocket_moonbridge", "Moonbridge Overlook & Shrine Gardens", 5, new WorldPoint(44150, 44350), 75.0, true, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_ok_03", "chal_ok_trail" }));

        // Phase 1: Concrete Wards (9)
        list.Add(new("cw_pocket_hub", "Viaduct Sanctuary & Shelter 04", 9, new WorldPoint(44000, 76000), 96.0, true, new[] { "npc_maya_chen" }, new[] { "safehouse_concrete_wards", "interior_viaduct_clinic" }, new[] { "chal_cw_race" }));
        list.Add(new("cw_pocket_courtyard", "Sub-Viaduct Residential Courtyard", 9, new WorldPoint(44300, 75900), 85.0, false, new[] { "npc_yuki" }, Array.Empty<string>(), new[] { "quest_cw_01", "chal_cw_puzzle" }));
        list.Add(new("cw_pocket_viaduct_station", "Viaduct Station & Heavy Transit Line", 9, new WorldPoint(43750, 76200), 80.0, false, new[] { "npc_barik" }, Array.Empty<string>(), new[] { "quest_cw_02", "side_op_viaduct_defense" }));
        list.Add(new("cw_pocket_substructure", "Foundations & Drainage Caverns", 9, new WorldPoint(44150, 76350), 75.0, true, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_cw_03", "chal_cw_trail" }));

        // Phase 2: Downtown Core (6)
        list.Add(new("dt_pocket_hub", "Downtown Central Underworks Hub", 6, new WorldPoint(76000, 44000, 1), 96.0, true, new[] { "npc_elena_vance" }, new[] { "safehouse_downtown" }, new[] { "chal_dt_race" }));
        list.Add(new("dt_pocket_grid_relay", "Neon Grid Surveillance Relay", 6, new WorldPoint(75850, 43900), 85.0, false, new[] { "npc_kazuya" }, Array.Empty<string>(), new[] { "quest_dt_01", "chal_dt_puzzle" }));
        list.Add(new("dt_pocket_market", "Lower Tier Night Market", 6, new WorldPoint(76200, 44150), 80.0, true, new[] { "npc_aoi" }, Array.Empty<string>(), new[] { "quest_dt_02", "side_op_data_haven" }));
        list.Add(new("dt_pocket_transit", "Overpass Concourse & Data Tap", 6, new WorldPoint(76100, 43800, 1), 75.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_dt_03", "chal_dt_trail" }));

        // Phase 2: Eastlight Harbor (7)
        list.Add(new("hb_pocket_hub", "Customs Pier & Marina Hub", 7, new WorldPoint(108000, 44000), 96.0, true, new[] { "npc_captain_maro" }, new[] { "safehouse_harbor" }, new[] { "chal_hb_race" }));
        list.Add(new("hb_pocket_boatyard", "Tidal Drydock & Boat Basin", 7, new WorldPoint(108150, 43850), 85.0, true, new[] { "npc_nami" }, Array.Empty<string>(), new[] { "quest_hb_01", "chal_hb_puzzle" }));
        list.Add(new("hb_pocket_docks", "Container Logistics Gantry", 7, new WorldPoint(107900, 44200), 85.0, false, new[] { "npc_daichi" }, Array.Empty<string>(), new[] { "quest_hb_02", "side_op_harbor_slipway" }));
        list.Add(new("hb_pocket_warehouse", "Sunken Slipway Warehouses", 7, new WorldPoint(108250, 44100), 75.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_hb_03", "chal_hb_trail" }));

        // Phase 2: Citadel Ring (10)
        list.Add(new("ct_pocket_hub", "Forward Outpost Delta & Radar Bunker", 10, new WorldPoint(76000, 76000), 96.0, true, new[] { "npc_commander_locke" }, new[] { "safehouse_citadel" }, new[] { "chal_ct_race" }));
        list.Add(new("ct_pocket_spire_lookout", "Spire Surveillance Gantry", 10, new WorldPoint(76250, 75850), 85.0, false, new[] { "npc_lyra" }, Array.Empty<string>(), new[] { "quest_ct_01", "chal_ct_puzzle" }));
        list.Add(new("ct_pocket_perimeter_gate", "Perimeter Breach Point", 10, new WorldPoint(75800, 76150), 85.0, false, new[] { "npc_kaelen" }, Array.Empty<string>(), new[] { "quest_ct_02", "side_op_citadel_perimeter" }));
        list.Add(new("ct_pocket_garrison", "Automated Bastion Approach", 10, new WorldPoint(76150, 76300), 75.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_ct_03", "chal_ct_trail" }));

        // Phase 2: Rustrail Belt (13)
        list.Add(new("rr_pocket_hub", "Rustrail Central Switchyard Concourse", 13, new WorldPoint(44000, 108000), 96.0, true, new[] { "npc_foreman_briggs" }, new[] { "safehouse_rustrail" }, new[] { "chal_rr_race" }));
        list.Add(new("rr_pocket_switchyard", "Heavy Locomotive Turntable", 13, new WorldPoint(43850, 108200), 85.0, false, new[] { "npc_spike" }, Array.Empty<string>(), new[] { "quest_rr_01", "chal_rr_puzzle" }));
        list.Add(new("rr_pocket_salvage_depot", "Railcar Salvage Yard", 13, new WorldPoint(44180, 107900), 85.0, true, new[] { "npc_vex" }, Array.Empty<string>(), new[] { "quest_rr_02", "side_op_freight_intercept" }));
        list.Add(new("rr_pocket_freight", "Abandoned Freight Cut", 13, new WorldPoint(44300, 108150), 75.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_rr_03", "chal_rr_trail" }));

        // Phase 2: Foundry Basin (14)
        list.Add(new("fd_pocket_hub", "Foundry Exchange & Thermal Slag Hub", 14, new WorldPoint(76000, 108000, 2), 96.0, true, new[] { "npc_master_cho" }, new[] { "safehouse_foundry" }, new[] { "chal_fd_race" }));
        list.Add(new("fd_pocket_pump_station", "Geothermal Coolant Conduit", 14, new WorldPoint(75850, 107900), 85.0, false, new[] { "npc_talia" }, Array.Empty<string>(), new[] { "quest_fd_01", "chal_fd_puzzle" }));
        list.Add(new("fd_pocket_smelting_bay", "Heavy Armor Smelting Bay", 14, new WorldPoint(76200, 108150), 85.0, true, new[] { "npc_goro" }, Array.Empty<string>(), new[] { "quest_fd_02", "side_op_smelting_core" }));
        list.Add(new("fd_pocket_crucible", "Automated Crucible Incline", 14, new WorldPoint(76100, 107800), 75.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { "quest_fd_03", "chal_fd_trail" }));

        // Phase 3 Natural & Coastal Regions (0, 1, 2, 3, 4, 8, 11, 12, 15)
        int[] naturalRegions = { 0, 1, 2, 3, 4, 8, 11, 12, 15 };
        foreach (int r in naturalRegions)
        {
            var region = WorldCatalog.Regions[r];
            int row = r / 4, col = r % 4;
            double bx = col * 32000 + 16000, by = row * 32000 + 16000;

            list.Add(new($"pocket_r{r}_hub", $"{region.Name} Sanctuary Hub", r, new WorldPoint(bx, by), 96.0, true, Array.Empty<string>(), new[] { $"safehouse_{region.Name.ToLowerInvariant().Replace(" ", "_")}" }, new[] { $"chal_r{r}_race" }));
            list.Add(new($"pocket_r{r}_sat1", $"{region.Name} Outpost Alpha", r, new WorldPoint(bx, by - 6000), 85.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { $"quest_r{r}_01", $"chal_r{r}_puzzle" }));
            list.Add(new($"pocket_r{r}_sat2", $"{region.Name} Relay Beta", r, new WorldPoint(bx + 6000, by), 85.0, false, Array.Empty<string>(), Array.Empty<string>(), new[] { $"quest_r{r}_02", $"side_op_r{r}" }));
            list.Add(new($"pocket_r{r}_sat3", $"{region.Name} Overlook Gamma", r, new WorldPoint(bx, by + 6000), 80.0, true, Array.Empty<string>(), Array.Empty<string>(), new[] { $"quest_r{r}_03", $"chal_r{r}_trail" }));
        }

        return list.ToArray();
    }

    // ─── Authored Quests (Exactly 72 Quests: 48 District + 12 Operative + 12 Faction) ─
    public static readonly IReadOnlyList<QuestDefinition> Quests = BuildQuests();

    private static QuestDefinition[] BuildQuests()
    {
        var list = new List<QuestDefinition>();

        // ── Phase 1 District Quests ───────────────────────────────────────────
        // Little Tokyo (5)
        list.Add(new(
            "quest_ok_01", "Lanterns in the Shadow",
            "Restore ancient surveillance scramblers in the Lantern Market to shield resistance couriers from corporate scans.",
            5, QuestCategory.District, 1, null, null,
            new QuestObjective[]
            {
                new("obj_ok01_1", "Speak with Sora at the Lantern Market.", ObjectiveType.Interact, new WorldPoint(43820, 44210), 14, "npc_sora"),
                new("obj_ok01_2", "Repair the damaged market power conduit.", ObjectiveType.RepairConduit, new WorldPoint(43860, 44240), 12),
                new("obj_ok01_3", "Neutralize the surveillance probes at the palace wall.", ObjectiveType.DefeatHostiles, new WorldPoint(44200, 43950), 20)
            },
            null,
            new QuestReward(100, 150)
        ));
        list.Add(new(
            "quest_ok_02", "Ancestral Echoes",
            "Investigate corporate excavation probes along the historic palace perimeter.",
            5, QuestCategory.District, 1, "quest_ok_01", null,
            new QuestObjective[]
            {
                new("obj_ok02_1", "Consult Master Gen near the Palace Archives.", ObjectiveType.Interact, new WorldPoint(44255, 43905), 14, "npc_gen"),
                new("obj_ok02_2", "Investigate excavated cipher shrine.", ObjectiveType.Investigate, new WorldPoint(44280, 43870), 12),
                new("obj_ok02_3", "Defend Master Gen from extraction drones.", ObjectiveType.DefeatHostiles, new WorldPoint(44250, 43900), 22)
            },
            null,
            new QuestReward(150, 200, "mod_kinetic_coil")
        ));
        list.Add(new(
            "quest_ok_03", "The Crossroads of Little Tokyo",
            "Help the residents decide whether to fortify the historic wall bastions or support the artisan guild.",
            5, QuestCategory.District, 1, "quest_ok_02", null,
            new QuestObjective[]
            {
                new("obj_ok03_1", "Convene the district council at Moonbridge.", ObjectiveType.Interact, new WorldPoint(44150, 44350), 16),
                new("obj_ok03_2", "Make the consequential district reconstruction choice.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44150, 44350), 16),
                new("obj_ok03_3", "Affirm the choice with district elders.", ObjectiveType.Interact, new WorldPoint(44150, 44350), 16)
            },
            new QuestBranchChoice[]
            {
                new("choice_artisan_guild", "Support the Artisan Guild & Market Expansion", "Funds stalls and workshops so the street artisans can grow the market.", "Sora and the Lantern Artisans get this season's materials. The wall repairs wait.", "service_artisan_guild"),
                new("choice_wall_defenses", "Fortify Historic Wall Bastions", "Repairs the old wall bastions and posts lookouts over the palace grounds.", "The wall crews get this season's materials. The market expansion waits.", "service_wall_defenses")
            },
            new QuestReward(200, 300)
        ));

        // Concrete Wards (9)
        list.Add(new(
            "quest_cw_01", "Lifeblood of District 9",
            "Protect the civilian medical supply lines running beneath the rainline viaduct.",
            9, QuestCategory.District, 2, null, null,
            new QuestObjective[]
            {
                new("obj_cw01_1", "Check in with Dr. Maya Chen at Shelter 04.", ObjectiveType.Interact, new WorldPoint(43995, 76035), 14, "npc_maya_chen"),
                new("obj_cw01_2", "Deliver trauma kits to the residential courtyard.", ObjectiveType.DeliverPayload, new WorldPoint(44310, 75915), 16),
                new("obj_cw01_3", "Repel Cronos security enforcers.", ObjectiveType.DefeatHostiles, new WorldPoint(44300, 75900), 22)
            },
            null,
            new QuestReward(100, 150)
        ));
        list.Add(new(
            "quest_cw_02", "Under the Iron Canopy",
            "Assist chief engineer Barik in stabilizing the vibrating viaduct transit pillars.",
            9, QuestCategory.District, 2, "quest_cw_01", null,
            new QuestObjective[]
            {
                new("obj_cw02_1", "Meet Barik at Viaduct Station.", ObjectiveType.Interact, new WorldPoint(43760, 76210), 14, "npc_barik"),
                new("obj_cw02_2", "Realign seismic dampener #1.", ObjectiveType.RepairConduit, new WorldPoint(43730, 76240), 12),
                new("obj_cw02_3", "Realign seismic dampener #2.", ObjectiveType.RepairConduit, new WorldPoint(43790, 76180), 12)
            },
            null,
            new QuestReward(150, 200, null, "gadget_sonar_pulse")
        ));
        list.Add(new(
            "quest_cw_03", "A City That Bleeds",
            "Determine the reconstruction path of Concrete Wards: rebuild emergency medical hubs or power heavy defense generators.",
            9, QuestCategory.District, 2, "quest_cw_02", null,
            new QuestObjective[]
            {
                new("obj_cw03_1", "Assemble the ward wardens at the drainage cavern overlook.", ObjectiveType.Interact, new WorldPoint(44150, 76350), 16),
                new("obj_cw03_2", "Choose the reconstruction directive.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44150, 76350), 16),
                new("obj_cw03_3", "Ratify the reconstruction project.", ObjectiveType.Interact, new WorldPoint(44150, 76350), 16)
            },
            new QuestBranchChoice[]
            {
                new("choice_medical_clinic", "Establish Emergency Trauma Clinic", "Turns the back rooms of Shelter 04 into a trauma clinic.", "Dr. Chen gets the ward's crews and supplies. The workshop expansion waits.", "service_trauma_clinic"),
                new("choice_workshop_expansion", "Expand Reclamation Heavy Workshop", "Expands the salvage workshop with metal presses and recycling lines.", "Barik gets the ward's crews and supplies. The clinic waits.", "service_salvage_press")
            },
            new QuestReward(200, 300)
        ));

        // ── Phase 2 Urban District Quests ─────────────────────────────────────
        // Downtown Core (6)
        list.Add(new("quest_dt_01", "Neon Heartbeat", "Tap into the high-voltage commercial power spine to re-energize the Underworks.", 6, QuestCategory.District, 5, null, null, new QuestObjective[] { new("obj_dt01_1", "Speak with Dr. Elena Vance in the Underworks.", ObjectiveType.Interact, new WorldPoint(76010, 44010, 1), 14), new("obj_dt01_2", "Splice commercial conduit.", ObjectiveType.RepairConduit, new WorldPoint(76080, 44040, 1), 12), new("obj_dt01_3", "Defend the splice team from Cronos patrol drones.", ObjectiveType.DefeatHostiles, new WorldPoint(76050, 44020, 1), 22) }, null, new QuestReward(120, 180)));
        list.Add(new("quest_dt_02", "Shadows on the Glass", "Track corporate data runners through the neon-lit commercial skyways.", 6, QuestCategory.District, 5, "quest_dt_01", null, new QuestObjective[] { new("obj_dt02_1", "Intercept courier route at Neon Market.", ObjectiveType.Investigate, new WorldPoint(76200, 44150), 14), new("obj_dt02_2", "Extract encrypted flight ledger.", ObjectiveType.Interact, new WorldPoint(76180, 44120), 12), new("obj_dt02_3", "Evade corporate enforcers.", ObjectiveType.DefeatHostiles, new WorldPoint(76220, 44180), 20) }, null, new QuestReward(160, 240, null, "gadget_emp_disruptor")));
        list.Add(new("quest_dt_03", "The High-Rise Pact", "Broker an alliance between civilian shelters and rooftop black market syndicates.", 6, QuestCategory.District, 5, "quest_dt_02", null, new QuestObjective[] { new("obj_dt03_1", "Convene summit on Skyline Terrace.", ObjectiveType.Interact, new WorldPoint(76100, 43800, 1), 16), new("obj_dt03_2", "Decide the civic resource priority.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(76100, 43800, 1), 16), new("obj_dt03_3", "Seal the charter.", ObjectiveType.Interact, new WorldPoint(76100, 43800, 1), 16) }, new QuestBranchChoice[] { new("choice_dt_shelters", "Fund Civilian Underworks Shelters", "Rebuilds drainage refuges into reinforced civilian habitats.", "Underworks shelters gain automated medical terminals.", "service_underworks_habs"), new("choice_dt_blackmarket", "Empower Skyline Black Market", "Permits syndicates to run black-market trade across the upper catwalks.", "Skyline merchants stock rare operative upgrade matrices.", "service_skyline_bazaar") }, new QuestReward(220, 320)));

        // Eastlight Harbor (7)
        list.Add(new("quest_hb_01", "Salt and Silt", "Clear naval mines blocking civilian fishing trawlers in the outer harbor.", 7, QuestCategory.District, 7, null, null, new QuestObjective[] { new("obj_hb01_1", "Consult Captain Maro at the customs pier.", ObjectiveType.Interact, new WorldPoint(108010, 44010), 14), new("obj_hb01_2", "Disarm floating magnetic mine.", ObjectiveType.RepairConduit, new WorldPoint(108150, 43850), 16), new("obj_hb01_3", "Destroy torpedo drones.", ObjectiveType.DefeatHostiles, new WorldPoint(108180, 43880), 24) }, null, new QuestReward(120, 180)));
        list.Add(new("quest_hb_02", "Gantry Infiltration", "Reprogram cargo cranes to move containerized medical supplies.", 7, QuestCategory.District, 7, "quest_hb_01", null, new QuestObjective[] { new("obj_hb02_1", "Ascend cargo gantry controls.", ObjectiveType.Interact, new WorldPoint(107900, 44200), 14), new("obj_hb02_2", "Override container sorting logic.", ObjectiveType.Investigate, new WorldPoint(107920, 44230), 12), new("obj_hb02_3", "Repel wharf security guards.", ObjectiveType.DefeatHostiles, new WorldPoint(107880, 44180), 20) }, null, new QuestReward(160, 240, "mod_courier_booster")));
        list.Add(new("quest_hb_03", "Tide of Change", "Determine harbor reconstruction: restore the public ferry network or construct fortified sea bastions.", 7, QuestCategory.District, 7, "quest_hb_02", null, new QuestObjective[] { new("obj_hb03_1", "Meet dockworker delegation at crane pier.", ObjectiveType.Interact, new WorldPoint(108250, 44100), 16), new("obj_hb03_2", "Choose the harbor development plan.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(108250, 44100), 16), new("obj_hb03_3", "Ratify maritime agreement.", ObjectiveType.Interact, new WorldPoint(108250, 44100), 16) }, new QuestBranchChoice[] { new("choice_hb_ferry", "Restore Civilian Ferry Network", "Deploys high-speed civilian ferries connecting coastal districts.", "Water transit speed increased by 25%.", "service_public_ferry"), new("choice_hb_bastion", "Build Naval Gun Bastions", "Mounts automated coastal artillery to deny corporate gunboats.", "Coastal artillery shells hostile gunboats automatically.", "service_naval_bastion") }, new QuestReward(220, 320)));

        // Citadel Ring (10)
        list.Add(new("quest_ct_01", "Breaching the Aegis", "Disable automated scanner pylons protecting the outer perimeter of the Citadel.", 10, QuestCategory.District, 4, null, null, new QuestObjective[] { new("obj_ct01_1", "Report to Commander Locke at Outpost Delta.", ObjectiveType.Interact, new WorldPoint(76010, 76010), 14), new("obj_ct01_2", "Sever sensor feed to radar pylon.", ObjectiveType.RepairConduit, new WorldPoint(76250, 75850), 12), new("obj_ct01_3", "Neutralize automated sentry turrets.", ObjectiveType.DefeatHostiles, new WorldPoint(76220, 75820), 22) }, null, new QuestReward(120, 180)));
        list.Add(new("quest_ct_02", "Signal in the Static", "Transmit decrypted rebel propaganda from the high broadcast array.", 10, QuestCategory.District, 4, "quest_ct_01", null, new QuestObjective[] { new("obj_ct02_1", "Meet Lyra at the spire lookout.", ObjectiveType.Interact, new WorldPoint(76250, 75850), 14), new("obj_ct02_2", "Align broadcast transmitter dish.", ObjectiveType.Investigate, new WorldPoint(76270, 75870), 12), new("obj_ct02_3", "Defend broadcast until uplink completes.", ObjectiveType.DefeatHostiles, new WorldPoint(76240, 75840), 24) }, null, new QuestReward(160, 240, null, "gadget_decoy_emitter")));
        list.Add(new("quest_ct_03", "Fallen Crown", "Decide the fate of the captured military garrison: convert to hospital or repurpose as weapons testing bunker.", 10, QuestCategory.District, 4, "quest_ct_02", null, new QuestObjective[] { new("obj_ct03_1", "Inspect garrison grounds with Locke.", ObjectiveType.Interact, new WorldPoint(76150, 76300), 16), new("obj_ct03_2", "Determine garrison charter.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(76150, 76300), 16), new("obj_ct03_3", "Execute directive.", ObjectiveType.Interact, new WorldPoint(76150, 76300), 16) }, new QuestBranchChoice[] { new("choice_ct_hospital", "Convert to Resistance Trauma Center", "Creates a massive medical staging hospital.", "Hero spark generation rate increased by 10%.", "service_garrison_hospital"), new("choice_ct_armory", "Repurpose as Ordnance Foundry", "Converts heavy armor bays into custom weapon modification stations.", "Unlocks military prototype weapon attachments.", "service_ordnance_foundry") }, new QuestReward(220, 320)));

        // Rustrail Belt (13)
        list.Add(new("quest_rr_01", "Sparks on the Track", "Clear rusted locomotives blocking vital supply transit through the Rustrail concourse.", 13, QuestCategory.District, 3, null, null, new QuestObjective[] { new("obj_rr01_1", "Confer with Foreman Briggs at the switchyard.", ObjectiveType.Interact, new WorldPoint(44010, 108010), 14), new("obj_rr01_2", "Release manual track brakes.", ObjectiveType.RepairConduit, new WorldPoint(43850, 108200), 14), new("obj_rr01_3", "Eliminate scrap scavenger ambush.", ObjectiveType.DefeatHostiles, new WorldPoint(43880, 108220), 20) }, null, new QuestReward(120, 180)));
        list.Add(new("quest_rr_02", "Heavy Freight", "Escort an armored reclamation train carrying vital reactor shielding.", 13, QuestCategory.District, 3, "quest_rr_01", null, new QuestObjective[] { new("obj_rr02_1", "Mount the freight engine platform.", ObjectiveType.Interact, new WorldPoint(44180, 107900), 14), new("obj_rr02_2", "Deliver shielding core to maintenance bay.", ObjectiveType.DeliverPayload, new WorldPoint(44220, 107940), 16), new("obj_rr02_3", "Repel corporate rail-raiders.", ObjectiveType.DefeatHostiles, new WorldPoint(44190, 107910), 22) }, null, new QuestReward(160, 240, "mod_hull_reinforce")));
        list.Add(new("quest_rr_03", "Iron Arteries", "Decide Rustrail reconstruction: prioritize high-speed armored transit or expand civilian trade trains.", 13, QuestCategory.District, 3, "quest_rr_02", null, new QuestObjective[] { new("obj_rr03_1", "Gather rail union heads at the roundhouse.", ObjectiveType.Interact, new WorldPoint(44300, 108150), 16), new("obj_rr03_2", "Select the transit expansion policy.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44300, 108150), 16), new("obj_rr03_3", "Sign the union agreement.", ObjectiveType.Interact, new WorldPoint(44300, 108150), 16) }, new QuestBranchChoice[] { new("choice_rr_armored", "Armored Rapid Strike Trains", "Deploys high-speed trains that suppress enemy road patrols.", "Road patrols along rail lines are disabled.", "service_armored_express"), new("choice_rr_freight", "Civilian Cargo Caravans", "Opens trade cars for inter-district merchants.", "Increases city credits earned from all regional challenges by 20%.", "service_merchant_freight") }, new QuestReward(220, 320)));

        // Foundry Basin (14)
        list.Add(new("quest_fd_01", "Molten Metal", "Reopen the geothermal coolant bypass valves to prevent a critical slag explosion.", 14, QuestCategory.District, 7, null, null, new QuestObjective[] { new("obj_fd01_1", "Meet Master Cho at the Foundry Hub.", ObjectiveType.Interact, new WorldPoint(76010, 108010, 2), 14), new("obj_fd01_2", "Vent primary steam exhaust stack.", ObjectiveType.RepairConduit, new WorldPoint(75850, 107900), 14), new("obj_fd01_3", "Destroy automated coolant drones.", ObjectiveType.DefeatHostiles, new WorldPoint(75870, 107930), 20) }, null, new QuestReward(120, 180)));
        list.Add(new("quest_fd_02", "Tempered Will", "Forge high-tensile armor plates for resistance field operatives.", 14, QuestCategory.District, 7, "quest_fd_01", null, new QuestObjective[] { new("obj_fd02_1", "Deliver rare alloy ingots to crucible.", ObjectiveType.DeliverPayload, new WorldPoint(76200, 108150), 14), new("obj_fd02_2", "Operate hydraulic forging press.", ObjectiveType.Interact, new WorldPoint(76220, 108170), 12), new("obj_fd02_3", "Defend crucible from saboteurs.", ObjectiveType.DefeatHostiles, new WorldPoint(76130, 107830), 24) }, null, new QuestReward(160, 240, "mod_thermal_sink")));
        list.Add(new("quest_fd_03", "Fire and Slag", "Decide Foundry reconstruction: dedicate furnaces to armor fabrication or convert thermal vents to clean geothermal power.", 14, QuestCategory.District, 7, "quest_fd_02", null, new QuestObjective[] { new("obj_fd03_1", "Convene with smelter guildmasters.", ObjectiveType.Interact, new WorldPoint(76100, 107800), 16), new("obj_fd03_2", "Decide the thermal production directive.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(76100, 107800), 16), new("obj_fd03_3", "Authorize furnace conversion.", ObjectiveType.Interact, new WorldPoint(76100, 107800), 16) }, new QuestBranchChoice[] { new("choice_fd_armor", "Dedicated Heavy Armor Plant", "Maximizes military armor synthesis.", "Operative shield maximum capacity increased by 10%.", "service_heavy_armor_works"), new("choice_fd_power", "Geothermal Clean Energy Grid", "Supplies free geothermal power to residential districts.", "Reduces fast travel recharge cost to 0 credits.", "service_clean_power_grid") }, new QuestReward(220, 320)));

        // ── Phase 3 Natural & Coastal District Quests (27 Quests: 9 Regions × 3) ───
        int[] naturalRegions = { 0, 1, 2, 3, 4, 8, 11, 12, 15 };
        foreach (int r in naturalRegions)
        {
            var region = WorldCatalog.Regions[r];
            int row = r / 4, col = r % 4;
            double bx = col * 32000 + 16000, by = row * 32000 + 16000;
            int minMission = Math.Clamp(r, 1, 11);

            // Q1
            list.Add(new(
                $"quest_r{r}_01", $"{region.Name}: First Awakening",
                $"Reactivate vital resistance infrastructure and clear sentinel relays across {region.Name}.",
                r, QuestCategory.District, minMission, null, null,
                new QuestObjective[]
                {
                    new($"obj_r{r}01_1", $"Inspect relay node in {region.Name}.", ObjectiveType.Interact, new WorldPoint(bx, by - 6000), 14),
                    new($"obj_r{r}01_2", $"Repair damaged power conduit.", ObjectiveType.RepairConduit, new WorldPoint(bx + 50, by - 5950), 12),
                    new($"obj_r{r}01_3", $"Neutralize sentinel security drones.", ObjectiveType.DefeatHostiles, new WorldPoint(bx, by - 6000), 20)
                },
                null,
                new QuestReward(120, 180)
            ));

            // Q2
            list.Add(new(
                $"quest_r{r}_02", $"{region.Name}: Deep Discovery",
                $"Uncover hidden civilian archival telemetry and defend extraction points in {region.Name}.",
                r, QuestCategory.District, minMission, $"quest_r{r}_01", null,
                new QuestObjective[]
                {
                    new($"obj_r{r}02_1", $"Meet local resistance contact.", ObjectiveType.Interact, new WorldPoint(bx + 6000, by), 14),
                    new($"obj_r{r}02_2", $"Recover encrypted data drive.", ObjectiveType.Investigate, new WorldPoint(bx + 6050, by + 50), 12),
                    new($"obj_r{r}02_3", $"Repel machine reinforcement skiff.", ObjectiveType.DefeatHostiles, new WorldPoint(bx + 6000, by), 22)
                },
                null,
                new QuestReward(160, 240, "mod_kinetic_coil")
            ));

            // Q3
            list.Add(new(
                $"quest_r{r}_03", $"{region.Name}: Consequential Path",
                $"Gather local community elders to decide the lasting reconstruction charter of {region.Name}.",
                r, QuestCategory.District, minMission, $"quest_r{r}_02", null,
                new QuestObjective[]
                {
                    new($"obj_r{r}03_1", $"Assemble local leaders at South Promontory.", ObjectiveType.Interact, new WorldPoint(bx, by + 6000), 16),
                    new($"obj_r{r}03_2", $"Choose district reconstruction priority.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(bx, by + 6000), 16),
                    new($"obj_r{r}03_3", $"Confirm the regional reconstruction directive.", ObjectiveType.Interact, new WorldPoint(bx, by + 6000), 16)
                },
                new QuestBranchChoice[]
                {
                    new($"choice_r{r}_civic", $"Civilian Care & Infrastructure in {region.Name}", "Restores clinics and living shelters; grants regional service discounts.", "Civilian infrastructure is restored, lowering vendor costs.", $"service_r{r}_civic"),
                    new($"choice_r{r}_military", $"Fortified Defense Perimeter in {region.Name}", "Deploys automated barrier turrets and shock sentries to repel incursions.", "Fortified defenses repel hostile patrols across the district.", $"service_r{r}_defense")
                },
                new QuestReward(220, 320)
            ));
        }

        // ── 12 Operative Personal Episodes (6 Operatives × 2 Episodes) ─────────
        string[] operativeIds = { "Cyber Shogun", "Surya Prime", "Oceanus", "Mirage Blade", "Apex Titan", "Aura Phoenicia" };
        string[] opShortTags = { "shogun", "surya", "oceanus", "mirage", "apex", "aura" };
        string[] ep1Titles = { "Blade of Kurogane", "Solar Bulwark", "Tides of Vaea", "Phantom Daggers", "Kinetic Breaker", "Prismatic Resonance" };
        string[] ep2Titles = { "Honor Among Cinders", "Dawn's Radiance", "Tides of Memory", "Veil of Sand", "Iron Sovereign", "Harmonic Chorus" };
        int[] epRegions = { 5, 8, 5, 6, 13, 0 };

        for (int i = 0; i < 6; i++)
        {
            string hero = operativeIds[i];
            string tag = opShortTags[i];
            int region = epRegions[i];
            int row = region / 4, col = region % 4;
            double bx = col * 32000 + 16000, by = row * 32000 + 16000;
            string regionName = WorldCatalog.Regions[region].Name;
            // Slice episodes sit inside the hostile palace pocket; the generic anchor is outside every pocket.
            WorldPoint episodeAnchor = i == 0 ? new WorldPoint(44300, 43850) : i == 2 ? new WorldPoint(44210, 43840) : new WorldPoint(bx + 200, by - 200);

            // Episode 1
            list.Add(new(
                $"quest_op_{tag}", $"{hero}: {ep1Titles[i]}",
                $"{hero} undertakes a personal mission to recover lost operative battle telemetry in {regionName}.",
                region, QuestCategory.Operative, 1 + (i % 3), null, hero,
                new QuestObjective[]
                {
                    new($"obj_op_{i}_1", $"Visit the operative memorial shrine.", ObjectiveType.Interact, episodeAnchor, 14),
                    new($"obj_op_{i}_2", $"Defeat hostile scavengers targeting the site.", ObjectiveType.DefeatHostiles, episodeAnchor, 20),
                    new($"obj_op_{i}_3", $"Synchronize with personal legacy datacore.", ObjectiveType.Investigate, episodeAnchor, 12)
                },
                null,
                new QuestReward(150, 250, "mod_thermal_sink")
            ));

            // Episode 2
            list.Add(new(
                $"quest_op_{tag}_2", $"{hero}: {ep2Titles[i]}",
                $"{hero} masters their signature technique and defends civilian survivors in Region {region}.",
                region, QuestCategory.Operative, 3 + (i % 3), $"quest_op_{tag}", hero,
                new QuestObjective[]
                {
                    new($"obj_op2_{i}_1", $"Locate the embattled survivor camp.", ObjectiveType.Interact, new WorldPoint(bx - 300, by + 300), 14),
                    new($"obj_op2_{i}_2", $"Overcome elite machine purge hunters.", ObjectiveType.DefeatHostiles, new WorldPoint(bx - 300, by + 300), 24),
                    new($"obj_op2_{i}_3", $"Seal the defensive gateway.", ObjectiveType.RepairConduit, new WorldPoint(bx - 300, by + 300), 12)
                },
                null,
                new QuestReward(200, 300, "mod_aegis_plating")
            ));
        }

        // ── 12 Faction Quests (4 Organizations × 3 Quests) ─────────────────────
        // Civic Recovery Network
        list.Add(new("quest_crn_01", "Field Dressing", "Supply field triage kits to civilian shelters.", 9, QuestCategory.Faction, 2, null, null, new QuestObjective[] { new("obj_crn1", "Deliver medical supplies.", ObjectiveType.DeliverPayload, new WorldPoint(44000, 76000), 14) }, null, new QuestReward(120, 180, null, null, FactionId.CivicRecovery, 60)));
        list.Add(new("quest_crn_02", "Structural Integrity", "Reinforce residential foundation pillars in Underworks.", 6, QuestCategory.Faction, 5, "quest_crn_01", null, new QuestObjective[] { new("obj_crn2", "Weld support jacks.", ObjectiveType.RepairConduit, new WorldPoint(76000, 44000, 1), 14) }, null, new QuestReward(150, 250, null, null, FactionId.CivicRecovery, 80)));
        list.Add(new("quest_crn_03", "Sanctuary Charter", "Unify all regional safehouses into a protective sanctuary network.", 5, QuestCategory.Faction, 5, "quest_crn_02", null, new QuestObjective[] { new("obj_crn3", "Ratify sanctuary charter.", ObjectiveType.Interact, new WorldPoint(44020, 44015), 14) }, null, new QuestReward(200, 350, "mod_civic_nanobound", null, FactionId.CivicRecovery, 120)));

        // Independent Couriers
        list.Add(new("quest_cou_01", "Express Run", "Complete high-speed courier delivery across the industrial arterial corridor.", 13, QuestCategory.Faction, 3, null, null, new QuestObjective[] { new("obj_cou1", "Deliver parcel in under 90s.", ObjectiveType.DeliverPayload, new WorldPoint(44000, 108000), 14) }, null, new QuestReward(120, 180, null, null, FactionId.IndependentCouriers, 60)));
        list.Add(new("quest_cou_02", "Signal Beacons", "Deploy waypoint beacons along the harbor waterways.", 7, QuestCategory.Faction, 7, "quest_cou_01", null, new QuestObjective[] { new("obj_cou2", "Plant navigation beacons.", ObjectiveType.Investigate, new WorldPoint(108000, 44000), 14) }, null, new QuestReward(150, 250, null, null, FactionId.IndependentCouriers, 80)));
        list.Add(new("quest_cou_03", "The Open Road", "Dismantle automated security tollgate on the coastal expressway.", 7, QuestCategory.Faction, 7, "quest_cou_02", null, new QuestObjective[] { new("obj_cou3", "Destroy barrier capacitor.", ObjectiveType.RepairConduit, new WorldPoint(108250, 44100), 14) }, null, new QuestReward(200, 350, "mod_courier_booster", null, FactionId.IndependentCouriers, 120)));

        // Reclamation Guild
        list.Add(new("quest_rec_01", "Scrap Tithe", "Recover intact servo-actuators from the Foundry slag channels.", 14, QuestCategory.Faction, 7, null, null, new QuestObjective[] { new("obj_rec1", "Recover servo components.", ObjectiveType.Investigate, new WorldPoint(76000, 108000, 2), 14) }, null, new QuestReward(120, 180, null, null, FactionId.ReclamationGuild, 60)));
        list.Add(new("quest_rec_02", "Reverse Engineered", "Calibrate modular component workbench at Rustrail depot.", 13, QuestCategory.Faction, 3, "quest_rec_01", null, new QuestObjective[] { new("obj_rec2", "Calibrate laser workbench.", ObjectiveType.RepairConduit, new WorldPoint(44180, 107900), 14) }, null, new QuestReward(150, 250, null, null, FactionId.ReclamationGuild, 80)));
        list.Add(new("quest_rec_03", "Heavy Reclamation", "Reclaim corporate smelting core blueprints and install them in the Foundry.", 14, QuestCategory.Faction, 7, "quest_rec_02", null, new QuestObjective[] { new("obj_rec3", "Secure smelting schematics.", ObjectiveType.Investigate, new WorldPoint(76220, 108170), 14) }, null, new QuestReward(200, 350, "mod_reclamation_mesh", null, FactionId.ReclamationGuild, 120)));

        // Free Signal Collective
        list.Add(new("quest_fsc_01", "Ghost Frequency", "Broadcast cryptographic counter-signals through the Citadel outer array.", 10, QuestCategory.Faction, 4, null, null, new QuestObjective[] { new("obj_fsc1", "Deploy relay transmitter.", ObjectiveType.RepairConduit, new WorldPoint(76000, 76000), 14) }, null, new QuestReward(120, 180, null, null, FactionId.FreeSignalCollective, 60)));
        list.Add(new("quest_fsc_02", "Memory Leak", "Recover erased history data drives from Little Tokyo archives.", 5, QuestCategory.Faction, 2, "quest_fsc_01", null, new QuestObjective[] { new("obj_fsc2", "Extract erased memory block.", ObjectiveType.Investigate, new WorldPoint(44260, 43910), 14) }, null, new QuestReward(150, 250, null, null, FactionId.FreeSignalCollective, 80)));
        list.Add(new("quest_fsc_03", "Axiom Breaker", "Transmit master liberation sequence across the central Downtown uplink.", 6, QuestCategory.Faction, 6, "quest_fsc_02", null, new QuestObjective[] { new("obj_fsc3", "Broadcast liberation code.", ObjectiveType.Interact, new WorldPoint(76100, 43800, 1), 14) }, null, new QuestReward(200, 350, "mod_free_signal_tuner", null, FactionId.FreeSignalCollective, 120)));

        return list.ToArray();
    }

    // ─── Regional Challenges (Exactly 48 Challenges: 3 per Region × 16 Regions) ─
    public static readonly IReadOnlyList<RegionalChallenge> Challenges = BuildChallenges();

    private static RegionalChallenge[] BuildChallenges()
    {
        var list = new List<RegionalChallenge>();

        // Phase 1 & 2 Authored Challenges
        // Little Tokyo (5)
        list.Add(new("chal_ok_race", "Lantern Mile Sprint", 5, ChallengeType.Race, new WorldPoint(44020, 44030), 45.0f, 60.0f, 100, 150));
        list.Add(new("chal_ok_puzzle", "Palace Wall Ascension", 5, ChallengeType.OperativePuzzle, new WorldPoint(44260, 43920), 40.0f, 55.0f, 100, 150));
        list.Add(new("chal_ok_trail", "Shrine Garden Circuit", 5, ChallengeType.DiscoveryTrail, new WorldPoint(44160, 44360), 90.0f, 120.0f, 100, 150));

        // Concrete Wards (9)
        list.Add(new("chal_cw_race", "Rainline Viaduct Sprint", 9, ChallengeType.Race, new WorldPoint(43760, 76220), 35.0f, 50.0f, 100, 150));
        list.Add(new("chal_cw_puzzle", "Magnetic Conduit Coupling", 9, ChallengeType.OperativePuzzle, new WorldPoint(44290, 75920), 45.0f, 65.0f, 100, 150));
        list.Add(new("chal_cw_trail", "Echoes of the Wards Trail", 9, ChallengeType.DiscoveryTrail, new WorldPoint(44140, 76340), 90.0f, 120.0f, 100, 150));

        // Downtown Core (6)
        list.Add(new("chal_dt_race", "Skyline Highway Sprint", 6, ChallengeType.Race, new WorldPoint(76020, 44030), 40.0f, 55.0f, 100, 150));
        list.Add(new("chal_dt_puzzle", "Underworks Circuit Alignment", 6, ChallengeType.OperativePuzzle, new WorldPoint(75860, 43920), 45.0f, 60.0f, 100, 150));
        list.Add(new("chal_dt_trail", "Neon Ghost Trail", 6, ChallengeType.DiscoveryTrail, new WorldPoint(76120, 43820), 90.0f, 120.0f, 100, 150));

        // Eastlight Harbor (7)
        list.Add(new("chal_hb_race", "Harbor Canal Boat Slalom", 7, ChallengeType.Race, new WorldPoint(108160, 43860), 45.0f, 60.0f, 100, 150));
        list.Add(new("chal_hb_puzzle", "Tidal Crane Girders", 7, ChallengeType.OperativePuzzle, new WorldPoint(108260, 44120), 40.0f, 55.0f, 100, 150));
        list.Add(new("chal_hb_trail", "Sunken Cargo Log", 7, ChallengeType.DiscoveryTrail, new WorldPoint(107920, 44220), 90.0f, 120.0f, 100, 150));

        // Citadel Ring (10)
        list.Add(new("chal_ct_race", "Perimeter Barricade Run", 10, ChallengeType.Race, new WorldPoint(76020, 76030), 40.0f, 55.0f, 100, 150));
        list.Add(new("chal_ct_puzzle", "Aegis Sensor Override", 10, ChallengeType.OperativePuzzle, new WorldPoint(75820, 76170), 50.0f, 70.0f, 100, 150));
        list.Add(new("chal_ct_trail", "Silent Ramparts Observation", 10, ChallengeType.DiscoveryTrail, new WorldPoint(76160, 76320), 90.0f, 120.0f, 100, 150));

        // Rustrail Belt (13)
        list.Add(new("chal_rr_race", "Locomotive Siding Time Trial", 13, ChallengeType.Race, new WorldPoint(44020, 108030), 42.0f, 58.0f, 100, 150));
        list.Add(new("chal_rr_puzzle", "Switchyard Turntable Puzzle", 13, ChallengeType.OperativePuzzle, new WorldPoint(43860, 108220), 45.0f, 65.0f, 100, 150));
        list.Add(new("chal_rr_trail", "Iron Memory Archive Trail", 13, ChallengeType.DiscoveryTrail, new WorldPoint(44310, 108170), 90.0f, 120.0f, 100, 150));

        // Foundry Basin (14)
        list.Add(new("chal_fd_race", "Thermal Vent Scramble", 14, ChallengeType.Race, new WorldPoint(76020, 108030), 38.0f, 52.0f, 100, 150));
        list.Add(new("chal_fd_puzzle", "Molten Siphon Alignment", 14, ChallengeType.OperativePuzzle, new WorldPoint(75860, 107920), 45.0f, 60.0f, 100, 150));
        list.Add(new("chal_fd_trail", "Crucible Inscription Trail", 14, ChallengeType.DiscoveryTrail, new WorldPoint(76120, 107820), 90.0f, 120.0f, 100, 150));

        // Natural & Coastal Regions (0, 1, 2, 3, 4, 8, 11, 12, 15)
        int[] naturalRegions = { 0, 1, 2, 3, 4, 8, 11, 12, 15 };
        foreach (int r in naturalRegions)
        {
            var region = WorldCatalog.Regions[r];
            int row = r / 4, col = r % 4;
            double bx = col * 32000 + 16000, by = row * 32000 + 16000;

            list.Add(new($"chal_r{r}_race", $"{region.Name} Speed Trial", r, ChallengeType.Race, new WorldPoint(bx, by), 40.0f, 55.0f, 100, 150));
            list.Add(new($"chal_r{r}_puzzle", $"{region.Name} Traversal Puzzle", r, ChallengeType.OperativePuzzle, new WorldPoint(bx, by - 6000), 45.0f, 65.0f, 100, 150));
            list.Add(new($"chal_r{r}_trail", $"{region.Name} Discovery Trail", r, ChallengeType.DiscoveryTrail, new WorldPoint(bx, by + 6000), 90.0f, 120.0f, 100, 150));
        }

        return list.ToArray();
    }

    // ─── Replayable Side Operations (Exactly 16 Operations: 1 per Region) ──────
    public static readonly IReadOnlyList<SideOperationDefinition> SideOperations = BuildSideOperations();

    private static SideOperationDefinition[] BuildSideOperations()
    {
        return new SideOperationDefinition[]
        {
            new("side_op_highland_monastery", "Highland Monastery Breach", "Infiltration & Defense", 0, new WorldPoint(12020, 12015), "res://scenes/world/side_ops/highland_monastery_op.tscn", 150, 200),
            new("side_op_floodgate_sluice", "Reservoir Sluice Defense", "Sabotage & Defense", 1, new WorldPoint(44025, 12020), "res://scenes/world/side_ops/floodgate_sluice_op.tscn", 150, 200),
            new("side_op_storm_observatory", "Storm Observatory Raid", "High-Altitude Assault", 2, new WorldPoint(76020, 12015), "res://scenes/world/side_ops/storm_observatory_op.tscn", 150, 200),
            new("side_op_beacon_shoals", "Beacon Shoals Intercept", "Coastal Amphibious Assault", 3, new WorldPoint(108025, 12020), "res://scenes/world/side_ops/beacon_shoals_op.tscn", 150, 200),
            new("side_op_canopy_ambush", "Preserve Canopy Ambush", "Wilderness Guerrilla Strike", 4, new WorldPoint(12020, 44015), "res://scenes/world/side_ops/canopy_ambush_op.tscn", 150, 200),
            new("side_op_palace_vault", "Palace Vault Reclamation", "Infiltration & Recovery", 5, new WorldPoint(44270, 43930), "res://scenes/world/side_ops/palace_vault_op.tscn", 150, 200),
            new("side_op_data_haven", "Data Haven Infiltration", "Cyber-Infiltration & Data Extraction", 6, new WorldPoint(75870, 43910), "res://scenes/world/side_ops/data_haven_op.tscn", 175, 250),
            new("side_op_harbor_slipway", "Harbor Slipway Sabotage", "Amphibious Assault & Demolition", 7, new WorldPoint(107920, 44220), "res://scenes/world/side_ops/harbor_slipway_op.tscn", 175, 250),
            new("side_op_solar_station_purge", "Solar Substation Purge", "Automated Drone Defense", 8, new WorldPoint(12020, 76015), "res://scenes/world/side_ops/solar_station_purge_op.tscn", 150, 200),
            new("side_op_viaduct_defense", "Viaduct Grid Defense", "Sabotage & Defense", 9, new WorldPoint(43780, 76240), "res://scenes/world/side_ops/viaduct_defense_op.tscn", 150, 200),
            new("side_op_citadel_perimeter", "Citadel Perimeter Breach", "Heavy Assault & Radar Sabotage", 10, new WorldPoint(76170, 76320), "res://scenes/world/side_ops/citadel_perimeter_op.tscn", 175, 250),
            new("side_op_tideglass_salvage", "Tideglass Salvage Extraction", "Marine Wreckage Scramble", 11, new WorldPoint(108025, 76020), "res://scenes/world/side_ops/tideglass_salvage_op.tscn", 150, 200),
            new("side_op_escarpment_blade_test", "Escarpment Trial of Steel", "Martial Duel & Trial", 12, new WorldPoint(12020, 108015), "res://scenes/world/side_ops/escarpment_blade_test_op.tscn", 150, 200),
            new("side_op_freight_intercept", "Freight Line Interception", "Moving Locomotive Battle & Recovery", 13, new WorldPoint(44200, 107920), "res://scenes/world/side_ops/freight_intercept_op.tscn", 175, 250),
            new("side_op_smelting_core", "Smelting Core Shutdown", "Hazard Navigation & Drone Factory Sabotage", 14, new WorldPoint(76220, 108170), "res://scenes/world/side_ops/smelting_core_op.tscn", 175, 250),
            new("side_op_marsh_filtration_defense", "Wetlands Filter Station Defense", "Ecological Defense & Swarm Repulsion", 15, new WorldPoint(108020, 108015), "res://scenes/world/side_ops/marsh_filtration_defense_op.tscn", 150, 200)
        };
    }
}
