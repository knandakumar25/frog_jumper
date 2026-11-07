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

    public static readonly IReadOnlyList<VehicleDefinition> Vehicles = new[]
    {
        StarterBike,
        new VehicleDefinition("veh_utility_boat", "Nami-Runner Utility Boat", 14.0f, 6.0f, 1.0f, true, 400)
    };

    // ─── Interiors & Safehouses ──────────────────────────────────────────────
    public static readonly IReadOnlyList<InteriorDefinition> Interiors = new[]
    {
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
        )
    };

    // ─── Named Residents (Phase 1: 6 Residents) ──────────────────────────────
    public static readonly IReadOnlyList<NamedResident> Residents = new[]
    {
        // Old Kyoto Residents
        new NamedResident(
            "npc_sora",
            "Sora",
            "Lantern Artisan & Civic Organizer",
            "Civic Recovery Network",
            5,
            "ok_pocket_lantern_market",
            new WorldPoint(43820, 44210), // Day: Lantern Market stall
            new WorldPoint(44025, 44018), // Evening: Hearth safehouse
            "Welcome back to Old Kyoto. The lanterns still burn, so long as we keep the power lines unbroken.",
            "dialog_sora"
        ),
        new NamedResident(
            "npc_gen",
            "Master Gen",
            "Historic Archivist & Memory Keeper",
            "Free Signal Collective",
            5,
            "ok_pocket_palace_investigation",
            new WorldPoint(44255, 43905), // Day: Palace archive desk
            new WorldPoint(44155, 44355), // Evening: Moonbridge tea pavilion
            "The machine occupiers tried to burn our names from the record. But stone remembers what circuits forget.",
            "dialog_gen"
        ),
        new NamedResident(
            "npc_ren",
            "Ren",
            "Independent Courier & Master Mechanic",
            "Independent Couriers",
            5,
            "ok_pocket_hub",
            new WorldPoint(44010, 44005), // Day: Station 85 garage bay
            new WorldPoint(43830, 44220), // Evening: Lantern Market plaza
            "Need a tune-up on that Echo-Rider? The arterial routes are open if your reflexes are sharp enough.",
            "dialog_ren"
        ),
        // Concrete Wards Residents
        new NamedResident(
            "npc_maya_chen",
            "Dr. Maya Chen",
            "Trauma Surgeon & Shelter Director",
            "Civic Recovery Network",
            9,
            "cw_pocket_hub",
            new WorldPoint(43995, 76035), // Day: Shelter triage clinic
            new WorldPoint(44020, 76010), // Evening: Shelter common
            "Every day the viaduct stands is another day our patients can sleep without fear of ceiling collapse.",
            "dialog_maya"
        ),
        new NamedResident(
            "npc_barik",
            "Barik",
            "Chief Viaduct Engineer",
            "Reclamation Guild",
            9,
            "cw_pocket_viaduct_station",
            new WorldPoint(43760, 76210), // Day: Viaduct power conduits
            new WorldPoint(44015, 76020), // Evening: Shelter maintenance bay
            "Heavy conduit alignment isn't delicate work, operative. Watch your hands around the magnetic couplings.",
            "dialog_barik"
        ),
        new NamedResident(
            "npc_yuki",
            "Yuki",
            "Signal Runner & Surveillance Scout",
            "Free Signal Collective",
            9,
            "cw_pocket_courtyard",
            new WorldPoint(44310, 75915), // Day: Contested Courtyard surveillance perch
            new WorldPoint(44160, 76360), // Evening: Suspended Forum relay
            "They've got pulse sensor drones buzzing over the viaduct gap. Give me a distraction and I'll slice their feeds.",
            "dialog_yuki"
        )
    };

    // ─── Activity Pockets ────────────────────────────────────────────────────
    public static readonly IReadOnlyList<ActivityPocket> Pockets = BuildPockets();

    private static ActivityPocket[] BuildPockets()
    {
        var list = new List<ActivityPocket>
        {
            // Old Kyoto Pockets
            new(
                "ok_pocket_hub",
                "Old Kyoto Arrival Plaza & Hearth HQ",
                5,
                new WorldPoint(44000, 44000),
                96.0,
                true, // Safe zone
                new[] { "npc_ren" },
                new[] { "safehouse_old_kyoto" },
                new[] { "chal_ok_race" }
            ),
            new(
                "ok_pocket_lantern_market",
                "Lantern Market & Artisan Guild",
                5,
                new WorldPoint(43800, 44200),
                80.0,
                true, // Safe zone
                new[] { "npc_sora" },
                Array.Empty<string>(),
                new[] { "quest_ok_01" }
            ),
            new(
                "ok_pocket_palace_investigation",
                "Kurogane Palace Walls & Archives",
                5,
                new WorldPoint(44250, 43900),
                90.0,
                false, // Hostile perimeter
                new[] { "npc_gen" },
                new[] { "interior_palace_archives" },
                new[] { "quest_ok_02", "chal_ok_puzzle", "side_op_palace_vault" }
            ),
            new(
                "ok_pocket_moonbridge",
                "Moonbridge Gardens & Shrines",
                5,
                new WorldPoint(44150, 44350),
                85.0,
                true, // Safe zone
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { "quest_ok_03", "chal_ok_trail" }
            ),

            // Concrete Wards Pockets
            new(
                "cw_pocket_hub",
                "Concrete Junction & Shelter 04",
                9,
                new WorldPoint(44000, 76000),
                96.0,
                true, // Safe zone
                new[] { "npc_maya_chen" },
                new[] { "safehouse_concrete_wards", "interior_viaduct_clinic" },
                new[] { "quest_cw_03" }
            ),
            new(
                "cw_pocket_viaduct_station",
                "Rainline Viaduct & Generator Station",
                9,
                new WorldPoint(43750, 76200),
                85.0,
                false, // Hostile machine patrols
                new[] { "npc_barik" },
                Array.Empty<string>(),
                new[] { "quest_cw_01", "chal_cw_race", "side_op_viaduct_defense" }
            ),
            new(
                "cw_pocket_courtyard",
                "Contested Courtyard & Drone Perimeter",
                9,
                new WorldPoint(44300, 75900),
                80.0,
                false, // Hostile
                new[] { "npc_yuki" },
                Array.Empty<string>(),
                new[] { "quest_cw_02", "chal_cw_puzzle" }
            ),
            new(
                "cw_pocket_suspended_forum",
                "Suspended Forum & Girders",
                9,
                new WorldPoint(44150, 76350),
                90.0,
                true, // Safe zone
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { "chal_cw_trail" }
            )
        };

        // Synthesize satellite pockets for the remaining 14 regions so the world contract of 64 pockets is stable
        foreach (var region in WorldCatalog.Regions)
        {
            if (region.Id is 5 or 9) continue;
            int r = region.Id / 4, c = region.Id % 4;
            double baseX = c * 32000 + 16000, baseY = r * 32000 + 16000;
            list.Add(new($"pocket_r{region.Id}_hub", $"{region.Name} Arrival Hub", region.Id, new WorldPoint(baseX, baseY), 96.0, true, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
            list.Add(new($"pocket_r{region.Id}_sat1", $"{region.Name} North Satellite", region.Id, new WorldPoint(baseX, baseY - 8000), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
            list.Add(new($"pocket_r{region.Id}_sat2", $"{region.Name} East Satellite", region.Id, new WorldPoint(baseX + 8000, baseY), 80.0, false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
            list.Add(new($"pocket_r{region.Id}_sat3", $"{region.Name} South Satellite", region.Id, new WorldPoint(baseX, baseY + 8000), 80.0, true, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        }

        return list.ToArray();
    }

    // ─── Authored Quests (Phase 1: 8 Quests) ──────────────────────────────────
    public static readonly IReadOnlyList<QuestDefinition> Quests = new[]
    {
        // 1. Old Kyoto Chain: The Lantern Circuit
        new QuestDefinition(
            "quest_ok_01",
            "The Lantern Circuit",
            "Restore broken energy relays to light the emergency lanterns throughout Old Kyoto.",
            5,
            QuestCategory.District,
            1, // Unlocked after Mission 1
            null,
            null,
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

        // 2. Old Kyoto Chain: Names Behind the Walls
        new QuestDefinition(
            "quest_ok_02",
            "Names Behind the Walls",
            "Investigate Kurogane Palace outer perimeter and decrypt hidden resistance civilian registries.",
            5,
            QuestCategory.District,
            1,
            "quest_ok_01",
            null,
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

        // 3. Old Kyoto Chain: The Moonbridge Promise
        new QuestDefinition(
            "quest_ok_03",
            "The Moonbridge Promise",
            "Help the community leaders resolve the reconstruction priority for the Moonbridge district.",
            5,
            QuestCategory.District,
            1,
            "quest_ok_02",
            null,
            new QuestObjective[]
            {
                new("obj_ok03_1", "Convene with Sora and Ren at the Moonbridge Pavilion.", ObjectiveType.Interact, new WorldPoint(44150, 44350), 16),
                new("obj_ok03_2", "Choose how to direct the reconstruction resources.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44150, 44350), 16),
                new("obj_ok03_3", "Confirm the reconstruction order.", ObjectiveType.Interact, new WorldPoint(44150, 44350), 16)
            },
            new QuestBranchChoice[]
            {
                new(
                    "choice_artisan_guild",
                    "Prioritize the Artisan Guild & Public Kitchens",
                    "Restores civilian trade and living quarters; provides community item discounts.",
                    "The Artisan Guild is rebuilt, filling the market with artisans and lowering upgrade costs.",
                    "service_artisan_guild"
                ),
                new(
                    "choice_wall_defenses",
                    "Reinforce the Kurogane Wall Defenses",
                    "Installs defensive sensor barriers and tactical support turrets around the district perimeter.",
                    "Perimeter defense nodes are armed, providing automated defensive fire against machine incursions.",
                    "service_wall_defenses"
                )
            },
            new QuestReward(200, 300)
        ),

        // 4. Concrete Wards Chain: The Broken Line
        new QuestDefinition(
            "quest_cw_01",
            "The Broken Line",
            "Re-align fractured viaduct power cables to restore emergency lighting to Sub-Viaduct Shelter 04.",
            9,
            QuestCategory.District,
            2, // Unlocked after Mission 2
            null,
            null,
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

        // 5. Concrete Wards Chain: Signal Under Rain
        new QuestDefinition(
            "quest_cw_02",
            "Signal Under Rain",
            "Assist scout Yuki in eliminating machine surveillance relays pinning down ward inhabitants.",
            9,
            QuestCategory.District,
            2,
            "quest_cw_01",
            null,
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

        // 6. Concrete Wards Chain: Shelter and Steel
        new QuestDefinition(
            "quest_cw_03",
            "Shelter and Steel",
            "Determine the vital resource allocation between emergency medical care and heavy fabrication.",
            9,
            QuestCategory.District,
            2,
            "quest_cw_02",
            null,
            new QuestObjective[]
            {
                new("obj_cw03_1", "Convene with Dr. Maya Chen and Barik at Shelter 04.", ObjectiveType.Interact, new WorldPoint(44020, 76010), 16),
                new("obj_cw03_2", "Select the ward recovery initiative.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(44020, 76010), 16),
                new("obj_cw03_3", "Ratify the allocation order.", ObjectiveType.Interact, new WorldPoint(44020, 76010), 16)
            },
            new QuestBranchChoice[]
            {
                new(
                    "choice_medical_clinic",
                    "Establish Emergency Trauma Clinic",
                    "Expands ICU beds and rapid field triage; unlocks instant health regeneration in shelter.",
                    "Dr. Chen opens a state-of-the-art triage facility, granting rapid field healing at safehouses.",
                    "service_trauma_clinic"
                ),
                new(
                    "choice_workshop_expansion",
                    "Expand Reclamation Heavy Workshop",
                    "Installs industrial metal presses and salvage recycling; grants bonus credits from salvage.",
                    "Barik completes the heavy workshop expansion, increasing credit yields from world exploration.",
                    "service_salvage_press"
                )
            },
            new QuestReward(200, 300)
        ),

        // 7. Operative Personal Episode: Cyber Shogun (Blade of Kurogane)
        new QuestDefinition(
            "quest_op_shogun",
            "Blade of Kurogane",
            "Kaito Kurogane visits his family's ancestral shrine to recover a sealed tactical combat protocol.",
            5,
            QuestCategory.Operative,
            1,
            null,
            "Cyber Shogun",
            new QuestObjective[]
            {
                new("obj_shogun_1", "Visit the Kurogane Ancestral Shrine at the palace overlook.", ObjectiveType.Interact, new WorldPoint(44220, 43980), 12),
                new("obj_shogun_2", "Defend the shrine altar against scavengers.", ObjectiveType.DefeatHostiles, new WorldPoint(44220, 43980), 18),
                new("obj_shogun_3", "Synchronize with the Kurogane Legacy Datacore.", ObjectiveType.Investigate, new WorldPoint(44220, 43980), 10)
            },
            null,
            new QuestReward(150, 250, "mod_thermal_sink")
        ),

        // 8. Operative Personal Episode: Oceanus (Tides of Vaea)
        new QuestDefinition(
            "quest_op_oceanus",
            "Tides of Vaea",
            "Kiana Vaea uses her hydrokinetic mastery to purge industrial contaminants from the ancient canal gate.",
            5,
            QuestCategory.Operative,
            2,
            null,
            "Oceanus",
            new QuestObjective[]
            {
                new("obj_oceanus_1", "Locate the jammed Canal Floodgate in East Old Kyoto.", ObjectiveType.Interact, new WorldPoint(44350, 44100), 12),
                new("obj_oceanus_2", "Use Hydro Flush to purge the toxic sludge buildup.", ObjectiveType.RepairConduit, new WorldPoint(44360, 44120), 10),
                new("obj_oceanus_3", "Open the canal sluice valve and restore clean water flow.", ObjectiveType.Interact, new WorldPoint(44350, 44100), 12)
            },
            null,
            new QuestReward(150, 250, "mod_aegis_plating")
        )
    };

    // ─── Regional Challenges (Phase 1: 6 Challenges) ─────────────────────────
    public static readonly IReadOnlyList<RegionalChallenge> Challenges = new[]
    {
        // Old Kyoto Challenges
        new RegionalChallenge("chal_ok_race", "Lantern Circuit Time Trial", 5, ChallengeType.Race, new WorldPoint(44020, 44030), 45.0f, 60.0f, 100, 150),
        new RegionalChallenge("chal_ok_puzzle", "Palace Rooftops Precision Dash", 5, ChallengeType.OperativePuzzle, new WorldPoint(44240, 43880), 40.0f, 55.0f, 100, 150),
        new RegionalChallenge("chal_ok_trail", "Moonbridge Inscriptions Trail", 5, ChallengeType.DiscoveryTrail, new WorldPoint(44140, 44340), 90.0f, 120.0f, 100, 150),

        // Concrete Wards Challenges
        new RegionalChallenge("chal_cw_race", "Rainline Viaduct Sprint", 9, ChallengeType.Race, new WorldPoint(43760, 76220), 35.0f, 50.0f, 100, 150),
        new RegionalChallenge("chal_cw_puzzle", "Magnetic Conduit Coupling", 9, ChallengeType.OperativePuzzle, new WorldPoint(44290, 75920), 45.0f, 65.0f, 100, 150),
        new RegionalChallenge("chal_cw_trail", "Echoes of the Wards Trail", 9, ChallengeType.DiscoveryTrail, new WorldPoint(44140, 76340), 90.0f, 120.0f, 100, 150)
    };

    // ─── Replayable Side Operations (Phase 1: 2 Operations) ──────────────────
    public static readonly IReadOnlyList<SideOperationDefinition> SideOperations = new[]
    {
        new SideOperationDefinition(
            "side_op_palace_vault",
            "Palace Vault Reclamation",
            "Infiltration & Recovery",
            5,
            new WorldPoint(44270, 43930),
            "res://scenes/world/side_ops/palace_vault_op.tscn",
            150,
            200
        ),
        new SideOperationDefinition(
            "side_op_viaduct_defense",
            "Viaduct Grid Defense",
            "Sabotage & Defense",
            9,
            new WorldPoint(43780, 76240),
            "res://scenes/world/side_ops/viaduct_defense_op.tscn",
            150,
            200
        )
    };
}
