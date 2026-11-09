using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Kyoto, 2157: the first city outside Los Angeles and Kaito's home. A compact 32 km square of four
/// districts, each with two quests, two residents and two landmarks. Place names are real but the
/// buildings and people are invented. Fights happen at works and depots, never at temples or shrines.
/// </summary>
public static class KyotoCity
{
    /// <summary>The four district hubs in region order: Arashiyama, Higashiyama, Fushimi, Gion.</summary>
    private static readonly int[] HubStations = CityBuilder.HubStations;

    private static readonly IReadOnlyList<WorldStation> Stations = CityBuilder.Stations;

    /// <summary>The city as data. Opens as soon as Kaito, the starting operative, is on the squad.</summary>
    public static readonly WorldDefinition Definition = new(BuildContent, CityBuilder.ClearAreas)
    {
        Id = "kyoto",
        Identity = "kyoto-2157-v1",
        DisplayName = "Kyoto",
        MapTitle = "Kyoto 2157 · 32 × 32 km",
        Size = 32000,
        RegionColumns = 2,
        RegionSize = 16000,
        Regions = new[]
        {
            new WorldRegion(0, "Arashiyama Reserve", "forest"), new(1, "Higashiyama Terraces", "highland"),
            new(2, "Fushimi Foundry Docks", "foundry"), new(3, "Gion Lantern Quarter", "palace")
        },
        Stations = Stations,
        Landmarks = BuildLandmarks(),
        River = new WorldPoint[] { new(15000, 0), new(16500, 12000), new(15500, 24000), new(17000, 32000) },
        RiverHalfWidth = 60,
        UrbanRegionIds = new HashSet<int> { 2, 3 },
        PropKind = ChooseProp,
        ArtFolder = "res://assets/world/kyoto/",
        Skyport = new WorldPoint(20000, 20060),
        UnlockOperative = OperativeRoster.Starter
    };

    /// <summary>
    /// Picks art by district: Gion townhouses, teahouses and storehouses; Fushimi storehouses, foundries
    /// and cranes; maple and bamboo in Arashiyama; maple and stone gardens in Higashiyama. Uses high hash
    /// bits because the low ones already set the prop's position.
    /// </summary>
    private static int ChooseProp(int region, bool urban, uint hash)
    {
        uint pick = hash >> 8;
        return region switch
        {
            3 => (int)(pick % 3),
            2 => 2 + (int)(pick % 3),
            0 => pick % 2 == 0 ? 6 : 5,
            _ => pick % 2 == 0 ? 5 : 7
        };
    }

    /// <summary>Two landmarks at each district hub, offset along the road so stations stay clear.</summary>
    private static WorldLandmark[] BuildLandmarks() => CityBuilder.Landmarks(new[] { "Togetsu Span", "Bamboo Hush Path", "Higashiyama Funicular", "Maple Terrace", "Fushimi Stacks", "Canal Lock Gardens", "Shijo Light Bridge", "Lantern Lane" });

    /// <summary>Assembles the city's people, places and activities.</summary>
    private static CityContent BuildContent() => new(
        BuildQuests(),
        BuildResidents(),
        new[] { new InteriorDefinition("safehouse_kyoto_gion", "The Kamo Hearth (Gion Safehouse)", "kyoto_gion_hub", new WorldPoint(20025, 20015), "safehouse", new[] { "rest_heal", "squad_briefing" }) },
        new HashSet<string> { "safehouse_kyoto_gion" },
        new[] { new SideOperationDefinition("op_kyoto_kamo_locks", "The Kamo Locks", "sabotage", 2, new WorldPoint(12000, 20060), "res://scenes/gameplay/kyoto_kamo_locks.tscn", 250, 60) },
        Conversations);

    /// <summary>Eight residents, two per district, each moving a short way between day and evening.</summary>
    private static NamedResident[] BuildResidents() => new NamedResident[]
    {
        new("knpc_hana", "Hana", "Lantern Row Keeper", "Civic Recovery Network", 3, "kyoto_gion_hub", new WorldPoint(20140, 20110), new WorldPoint(20110, 20090), "The lanterns here were lit by hand long before anyone wired them.", "dialog_knpc_hana"),
        new("knpc_tomo", "Tomo", "Bridge Council Clerk", "Civic Recovery Network", 3, "kyoto_gion_hub", new WorldPoint(19860, 20110), new WorldPoint(19890, 20090), "Every argument in Gion ends up on this bridge eventually.", "dialog_knpc_tomo"),
        new("knpc_rin", "Rin", "Terrace Archivist", "Free Signal Collective", 1, "kyoto_higashiyama_hub", new WorldPoint(20120, 11880), new WorldPoint(20090, 11900), "Paper survives power cuts. That is why they keep burning it.", "dialog_knpc_rin"),
        new("knpc_sensei_oda", "Oda", "Retired Sword Instructor", "Independent Couriers", 1, "kyoto_higashiyama_hub", new WorldPoint(19880, 11700), new WorldPoint(19910, 11720), "Kaito still stands too far forward. Tell him I said so.", "dialog_knpc_oda"),
        new("knpc_goro", "Goro", "Furnace Shift Lead", "Reclamation Guild", 2, "kyoto_fushimi_hub", new WorldPoint(12120, 20130), new WorldPoint(12090, 20110), "Cold furnaces mean hungry families. Simple as that.", "dialog_knpc_goro"),
        new("knpc_ume", "Ume", "Barge Pilot", "Independent Couriers", 2, "kyoto_fushimi_hub", new WorldPoint(11860, 20140), new WorldPoint(11890, 20120), "The canal only looks slow. Ask anyone who has fallen in.", "dialog_knpc_ume"),
        new("knpc_yuki", "Yuki", "Reserve Warden", "Reclamation Guild", 0, "kyoto_arashiyama_hub", new WorldPoint(12120, 11880), new WorldPoint(12090, 11900), "Listen. The bamboo tells you when someone is walking through it.", "dialog_knpc_yuki"),
        new("knpc_kenji", "Kenji", "Seed Bank Courier", "Civic Recovery Network", 0, "kyoto_arashiyama_hub", new WorldPoint(11860, 11880), new WorldPoint(11890, 11900), "Every crate I carry is someone's next ten harvests.", "dialog_knpc_kenji")
    };

    /// <summary>Two quests per district. The second of each pair follows the first. Kaito's own story is in Higashiyama.</summary>
    private static QuestDefinition[] BuildQuests() => new QuestDefinition[]
    {
        new("kq_gion_01", "Dark Row", "Restore the power that keeps Gion's lantern row lit and drive off the drones mapping it.", 3, QuestCategory.District, 1, null, null,
            new QuestObjective[]
            {
                new("kobj_gion01_1", "Speak with Hana on Lantern Row.", ObjectiveType.Interact, new WorldPoint(20140, 20110), 14, "knpc_hana"),
                new("kobj_gion01_2", "Repair the lantern row relay.", ObjectiveType.RepairConduit, new WorldPoint(20190, 20140), 12),
                new("kobj_gion01_3", "Destroy the mapping drones at the depot.", ObjectiveType.DefeatHostiles, new WorldPoint(20340, 20260), 20)
            }, null, new QuestReward(110, 150)),
        new("kq_gion_02", "The Vote on Shijo Bridge", "Help the quarter decide what its one shipment of parts should repair.", 3, QuestCategory.District, 1, "kq_gion_01", null,
            new QuestObjective[]
            {
                new("kobj_gion02_1", "Meet Tomo at the bridge council.", ObjectiveType.Interact, new WorldPoint(19860, 20110), 14, "knpc_tomo"),
                new("kobj_gion02_2", "Decide where the parts go.", ObjectiveType.MakeConsequentialChoice, new WorldPoint(19820, 20140), 14),
                new("kobj_gion02_3", "Tell the council your decision.", ObjectiveType.Interact, new WorldPoint(19820, 20140), 14)
            },
            new QuestBranchChoice[]
            {
                new("choice_kyoto_pumps", "Repair the River Pumps", "Sends the parts to the Kamo flood pumps.", "The river crews get this shipment. The lantern grid waits.", "service_kyoto_pumps"),
                new("choice_kyoto_grid", "Repair the Lantern Grid", "Sends the parts to the quarter's power grid.", "The grid crews get this shipment. The pumps wait.", "service_kyoto_grid")
            }, new QuestReward(180, 260)),
        new("kq_higashi_01", "Terrace Records", "Find out who has been copying the terrace archive's records.", 1, QuestCategory.District, 1, null, null,
            new QuestObjective[]
            {
                new("kobj_higa01_1", "Speak with Rin at the terrace archive.", ObjectiveType.Interact, new WorldPoint(20120, 11880), 14, "knpc_rin"),
                new("kobj_higa01_2", "Examine the tap on the archive line.", ObjectiveType.Investigate, new WorldPoint(20170, 11840), 12),
                new("kobj_higa01_3", "Destroy the relay drones at the funicular yard.", ObjectiveType.DefeatHostiles, new WorldPoint(20340, 11720), 20)
            }, null, new QuestReward(110, 160)),
        new("kq_higashi_02", "Too Far Forward", "Kaito returns to the teacher who trained him.", 1, QuestCategory.Operative, 1, "kq_higashi_01", OperativeRoster.Starter,
            new QuestObjective[]
            {
                new("kobj_higa02_1", "Visit Oda.", ObjectiveType.Interact, new WorldPoint(19880, 11700), 14, "knpc_sensei_oda"),
                new("kobj_higa02_2", "Recover the training records from the old hall's cellar.", ObjectiveType.Investigate, new WorldPoint(19840, 11660), 12),
                new("kobj_higa02_3", "Stop the salvage drones at the cable station.", ObjectiveType.DefeatHostiles, new WorldPoint(19700, 11540), 20)
            }, null, new QuestReward(200, 200)),
        new("kq_fushimi_01", "Cold Furnaces", "Get a stalled foundry shift running again.", 2, QuestCategory.District, 1, null, null,
            new QuestObjective[]
            {
                new("kobj_fush01_1", "Speak with Goro at the foundry gate.", ObjectiveType.Interact, new WorldPoint(12120, 20130), 14, "knpc_goro"),
                new("kobj_fush01_2", "Repair the furnace feed line.", ObjectiveType.RepairConduit, new WorldPoint(12170, 20170), 12),
                new("kobj_fush01_3", "Clear the enforcer drones from the coal yard.", ObjectiveType.DefeatHostiles, new WorldPoint(12330, 20280), 20)
            }, null, new QuestReward(120, 170)),
        new("kq_fushimi_02", "The Barge Line", "Move a cargo of medicine past the blockade on the canal.", 2, QuestCategory.District, 1, "kq_fushimi_01", null,
            new QuestObjective[]
            {
                new("kobj_fush02_1", "Meet Ume at the canal steps.", ObjectiveType.Interact, new WorldPoint(11860, 20140), 14, "knpc_ume"),
                new("kobj_fush02_2", "Deliver the medicine to the lock keeper's shed.", ObjectiveType.DeliverPayload, new WorldPoint(11780, 20220), 12),
                new("kobj_fush02_3", "Break the drone blockade at the barge pens.", ObjectiveType.DefeatHostiles, new WorldPoint(11660, 20330), 20)
            }, null, new QuestReward(160, 240)),
        new("kq_arashi_01", "Listening Post", "Find out why the reserve's sensor line has gone quiet.", 0, QuestCategory.District, 1, null, null,
            new QuestObjective[]
            {
                new("kobj_aras01_1", "Speak with Yuki at the warden's hut.", ObjectiveType.Interact, new WorldPoint(12120, 11880), 14, "knpc_yuki"),
                new("kobj_aras01_2", "Repair the buried sensor line.", ObjectiveType.RepairConduit, new WorldPoint(12170, 11830), 12),
                new("kobj_aras01_3", "Destroy the drones jamming the reserve.", ObjectiveType.DefeatHostiles, new WorldPoint(12340, 11710), 20)
            }, null, new QuestReward(110, 150)),
        new("kq_arashi_02", "Seed Road", "See a seed bank shipment safely out of the reserve.", 0, QuestCategory.District, 1, "kq_arashi_01", null,
            new QuestObjective[]
            {
                new("kobj_aras02_1", "Meet Kenji at the road head.", ObjectiveType.Interact, new WorldPoint(11860, 11880), 14, "knpc_kenji"),
                new("kobj_aras02_2", "Carry the seed crate to the ford.", ObjectiveType.DeliverPayload, new WorldPoint(11780, 11800), 12),
                new("kobj_aras02_3", "Drive off the drones raiding the ford road.", ObjectiveType.DefeatHostiles, new WorldPoint(11660, 11680), 20)
            }, null, new QuestReward(160, 230))
    };

    /// <summary>Written conversations. Combat objectives have none. Not voiced, and shown in English until translated.</summary>
    private static readonly Dictionary<string, WorldDialogueCatalog.Conversation> Conversations = new()
    {
        ["kobj_gion01_1"] = new("Hana", new[] { "Half the row went dark at dusk. The relay behind the tea stalls has been cut, not worn through.", "Mend it, please. Then look at the depot; something up there has been mapping every door on this street." }),
        ["kobj_gion01_2"] = new("Field Note", new[] { "The cut ends splice cleanly. Lanterns come back along the row, one house at a time." }),
        ["kobj_gion02_1"] = new("Tomo", new[] { "One shipment of parts came through. The council has argued for three nights and got nowhere.", "They asked for someone from outside the quarter to decide. Kaito's people will do." }),
        ["kobj_gion02_2"] = new("Bridge Council", new[] { "The pumps or the grid. Whichever you name, the other waits until spring." }),
        ["kobj_gion02_3"] = new("Bridge Council", new[] { "It is written down. The crews will start in the morning." }),
        ["kobj_higa01_1"] = new("Rin", new[] { "Somebody is reading our catalogue as fast as we write it. Birth rolls, land records, all of it.", "There is a tap on the line somewhere near the east wall. Find it for me." }),
        ["kobj_higa01_2"] = new("Field Note", new[] { "The tap is recent and neat. Its signal runs uphill, toward the old funicular yard." }),
        ["kobj_higa02_1"] = new("Oda", new[] { "You still stand too far forward, Kaito. You did at twelve, too.", "Your first training records are in the old hall's cellar. Salvagers have been sniffing around. Fetch them before they do." }),
        ["kobj_higa02_2"] = new("Kaito", new[] { "Every drill, in his handwriting. He marked the same mistake forty times." }),
        ["kobj_fush01_1"] = new("Goro", new[] { "No feed, no heat. No heat, no shift. And there are two hundred people on this shift.", "The feed line is split near the east wall. Once it holds, those drones in the coal yard will come looking." }),
        ["kobj_fush01_2"] = new("Field Note", new[] { "The line takes pressure. Somewhere behind the wall, the first furnace catches with a low thump." }),
        ["kobj_fush02_1"] = new("Ume", new[] { "The blockade checks every hull. It does not check a person walking the towpath with a crate.", "Get this to the lock keeper. I will draw their eyes down the canal." }),
        ["kobj_fush02_2"] = new("Field Note", new[] { "The keeper signs for the medicine and points at the barge pens without saying a word." }),
        ["kobj_aras01_1"] = new("Yuki", new[] { "The sensor line stopped reporting at noon. Nothing in the reserve goes quiet by accident.", "The break should be just past the hut. Be careful what you step on." }),
        ["kobj_aras01_2"] = new("Field Note", new[] { "The line wakes up, and at once reports a jamming source to the northeast." }),
        ["kobj_aras02_1"] = new("Kenji", new[] { "Forty years of seed stock in one crate. I would rather not be the man who dropped it.", "Help me get it to the ford. They have been raiding that road all month." }),
        ["kobj_aras02_2"] = new("Field Note", new[] { "The crate is handed across at the ford. Then a shape moves between the trees on the road behind you." }),
    };
}
