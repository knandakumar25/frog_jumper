using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A district: its name, ground type, whether it is built up, allowed art modules and two landmarks.</summary>
public sealed record DistrictSpec(string Name, string Biome, bool Urban, int[] PropKinds, string Building, string Scenic);

/// <summary>A resident. Person N lives in district N/2; odd and even people stand on opposite sides of the hub.</summary>
public sealed record PersonSpec(string Key, string Name, string Title, string Faction, string Greeting);

/// <summary>The middle step of a quest. Vote replaces the usual fight with a saved decision.</summary>
public enum QuestShape { Repair, Investigate, Deliver, Vote }

/// <summary>
/// One quest, written as story only; <see cref="CityBuilder"/> places it. Slot 0 comes first in its district
/// and slot 1 requires it. Speaker is person District*2+Slot, who also gives the opening conversation.
/// </summary>
public sealed record QuestSpec(
    int District, int Slot, string Title, string Summary, QuestShape Shape,
    string TalkGoal, string[] TalkLines,
    string StepGoal, string StepSpeaker, string[] StepLines,
    string FinalGoal, string[]? VoteClosingLines = null,
    QuestBranchChoice[]? Choices = null, bool ForCityOperative = false);

/// <summary>Everything that differs between compact cities.</summary>
public sealed record CitySpec(
    string Id, string Prefix, string DisplayName, string Operative,
    DistrictSpec[] Districts, PersonSpec[] People, QuestSpec[] Quests,
    string SafehouseName, int SkyportDistrict,
    string OperationName, string OperationFamily, int OperationDistrict, string OperationScene,
    WorldPoint[] River, SeaEdge[] Sea);

/// <summary>
/// Turns a <see cref="CitySpec"/> into a playable city with the same layout rules everywhere: four 16 km
/// districts around central hubs, each hub's people and fights within a short walk, kept clear of props.
/// </summary>
public static class CityBuilder
{
    /// <summary>The central station of each district, in region order.</summary>
    public static readonly int[] HubStations = { 5, 6, 9, 10 };

    /// <summary>The sixteen stations of a compact city, on the shared arterial grid.</summary>
    public static readonly IReadOnlyList<WorldStation> Stations = Enumerable.Range(0, 16)
        .Select(i => new WorldStation(i, new(4000 + i % 4 * 8000, 4000 + i / 4 * 8000))).ToArray();

    /// <summary>Places two landmarks at each hub along the road, so stations stay clear. Built sites are even, scenic odd.</summary>
    public static WorldLandmark[] Landmarks(string[] names) => names.Select((name, i) =>
    {
        var hub = Stations[HubStations[i / 2]].Position;
        return new WorldLandmark(i, name, i % 2 == 0 ? "architecture" : "scenic", hub with { X = hub.X + (i % 2 + 1) * 128 });
    }).ToArray();

    /// <summary>Everywhere the player must be able to stand, kept free of generated props.</summary>
    public static IReadOnlyList<WorldPoint> ClearAreas(CityContent content) =>
        content.Quests.SelectMany(q => q.Objectives).Select(o => o.TargetLocation)
            .Concat(content.Residents.SelectMany(r => new[] { r.DayLocation, r.EveningLocation }))
            .Concat(content.Interiors.Select(i => i.EntranceLocation))
            .Concat(content.Operations.Select(o => o.TerminalLocation))
            .Distinct().ToArray();

    /// <summary>Builds the definition. Content is created on first use, like every other city.</summary>
    public static WorldDefinition Build(CitySpec spec) => new(() => Content(spec), ClearAreas)
    {
        Id = spec.Id,
        Identity = $"{spec.Id}-2157-v1",
        DisplayName = spec.DisplayName,
        MapTitle = $"{spec.DisplayName} 2157 · 32 × 32 km",
        Size = 32000,
        RegionColumns = 2,
        RegionSize = 16000,
        Regions = spec.Districts.Select((d, i) => new WorldRegion(i, d.Name, d.Biome)).ToArray(),
        Stations = Stations,
        Landmarks = Landmarks(spec.Districts.SelectMany(d => new[] { d.Building, d.Scenic }).ToArray()),
        River = spec.River,
        RiverHalfWidth = 70,
        SeaEdges = spec.Sea,
        UrbanRegionIds = spec.Districts.Select((d, i) => (d, i)).Where(x => x.d.Urban).Select(x => x.i).ToHashSet(),
        // High hash bits, because the low ones already set each prop's position.
        PropKind = (region, _, hash) => spec.Districts[region].PropKinds[(hash >> 8) % (uint)spec.Districts[region].PropKinds.Length],
        ArtFolder = $"res://assets/world/{spec.Id}/",
        Skyport = HubPoint(spec.SkyportDistrict, 0, 60),
        UnlockOperative = spec.Operative
    };

    /// <summary>A point relative to a district's hub, with no mirroring.</summary>
    private static WorldPoint HubPoint(int district, double dx, double dy) => Stations[HubStations[district]].Position is var hub
        ? new WorldPoint(hub.X + dx, hub.Y + dy)
        : default;

    /// <summary>Adds an offset to a point; north-side districts flip Y so people stay on their half of the hub.</summary>
    private static WorldPoint Near(int district, double dx, double dy) => HubPoint(district, dx, district < 2 ? -dy : dy);

    /// <summary>Generates IDs and positions for the people, quests and conversations.</summary>
    private static CityContent Content(CitySpec spec)
    {
        string p = spec.Prefix;
        var talks = new Dictionary<string, WorldDialogueCatalog.Conversation>();
        WorldPoint Person(int i) => Near(i / 2, i % 2 == 0 ? 130 : -140, 110);

        var residents = spec.People.Select((who, i) =>
        {
            var day = Person(i);
            var evening = Near(i / 2, i % 2 == 0 ? 100 : -110, 90);
            return new NamedResident($"{p}npc_{who.Key}", who.Name, who.Title, who.Faction, i / 2, $"{spec.Id}_hub_{i / 2}", day, evening, who.Greeting, $"dialog_{p}npc_{who.Key}");
        }).ToArray();

        var quests = spec.Quests.Select(q =>
        {
            int person = q.District * 2 + q.Slot;
            int side = q.Slot == 0 ? 1 : -1;
            string Id(int step) => $"{p}obj_{q.District}{q.Slot}_{step}";
            var talkAt = Person(person);
            var stepAt = Near(q.District, side * 190, 150);
            var fightAt = Near(q.District, side * 340, 260);
            var talk = new QuestObjective(Id(1), q.TalkGoal, ObjectiveType.Interact, talkAt, 14, $"{p}npc_{spec.People[person].Key}");
            talks[Id(1)] = new(spec.People[person].Name, q.TalkLines);
            talks[Id(2)] = new(q.StepSpeaker, q.StepLines);
            QuestObjective[] steps;
            if (q.Shape == QuestShape.Vote)
            {
                talks[Id(3)] = new(q.StepSpeaker, q.VoteClosingLines ?? Array.Empty<string>());
                steps = new[] { talk, new QuestObjective(Id(2), q.StepGoal, ObjectiveType.MakeConsequentialChoice, stepAt, 14), new QuestObjective(Id(3), q.FinalGoal, ObjectiveType.Interact, stepAt, 14) };
            }
            else
            {
                var type = q.Shape switch { QuestShape.Repair => ObjectiveType.RepairConduit, QuestShape.Deliver => ObjectiveType.DeliverPayload, _ => ObjectiveType.Investigate };
                steps = new[] { talk, new QuestObjective(Id(2), q.StepGoal, type, stepAt, 12), new QuestObjective(Id(3), q.FinalGoal, ObjectiveType.DefeatHostiles, fightAt, 20) };
            }
            var reward = q.ForCityOperative ? new QuestReward(200, 200) : q.Slot == 0 ? new QuestReward(110, 150) : new QuestReward(170, 240);
            return new QuestDefinition($"{p}q_{q.District}_{q.Slot}", q.Title, q.Summary, q.District,
                q.ForCityOperative ? QuestCategory.Operative : QuestCategory.District, 1,
                q.Slot == 1 ? $"{p}q_{q.District}_0" : null, q.ForCityOperative ? spec.Operative : null, steps, q.Choices, reward);
        }).ToArray();

        string safehouse = $"safehouse_{spec.Id}";
        return new CityContent(quests, residents,
            new[] { new InteriorDefinition(safehouse, spec.SafehouseName, $"{spec.Id}_hub_{spec.SkyportDistrict}", HubPoint(spec.SkyportDistrict, 25, 15), "safehouse", new[] { "rest_heal", "squad_briefing" }) },
            new HashSet<string> { safehouse },
            new[] { new SideOperationDefinition($"op_{spec.Id}", spec.OperationName, spec.OperationFamily, spec.OperationDistrict, HubPoint(spec.OperationDistrict, 0, 60), spec.OperationScene, 250, 60) },
            talks);
    }
}
