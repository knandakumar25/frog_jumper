using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

/// <summary>Proves each compact city's authored data is reachable and consistent, and that cities can't disturb Los Angeles saves.</summary>
[TestFixture]
public class KyotoCityTests
{
    private static WorldDefinition Kyoto => KyotoCity.Definition;
    private static IEnumerable<QuestDefinition> Quests => Kyoto.Content.Quests;

    /// <summary>A point is usable if it is on the map and not in the river.</summary>
    private static void AssertStandable(WorldPoint p, string what)
    {
        Assert.That(Kyoto.InBounds(p), Is.True, $"{what} out of bounds: {p}");
        Assert.That(Kyoto.Water(p), Is.False, $"{what} in water: {p}");
    }

    [Test]
    public void GeographyIsCompactAndEveryDistrictIsPopulated()
    {
        Assert.That(Kyoto.Size, Is.EqualTo(32000));
        Assert.That(Kyoto.Regions.Select(r => r.Id), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(Kyoto.Stations, Has.Count.EqualTo(16));
        foreach (var region in Kyoto.Regions)
        {
            Assert.That(Quests.Count(q => q.RegionId == region.Id), Is.EqualTo(2), region.Name);
            Assert.That(Kyoto.Content.Residents.Count(r => r.RegionId == region.Id), Is.EqualTo(2), region.Name);
            Assert.That(Kyoto.Landmarks.Count(l => Kyoto.Region(l.Position).Id == region.Id), Is.EqualTo(2), region.Name);
        }
    }

    [Test]
    public void EveryAuthoredPointIsStandableAndInItsOwnDistrict()
    {
        foreach (var s in Kyoto.Stations) { AssertStandable(s.Position, $"station {s.Id}"); Assert.That(Kyoto.Arterial(s.Position), Is.True); }
        foreach (var l in Kyoto.Landmarks) AssertStandable(l.Position, l.Name);
        AssertStandable(Kyoto.Skyport, "skyport");
        Assert.That(Kyoto.Arterial(Kyoto.Skyport), Is.True);
        foreach (var quest in Quests)
            foreach (var objective in quest.Objectives)
            {
                AssertStandable(objective.TargetLocation, objective.Id);
                Assert.That(Kyoto.Region(objective.TargetLocation).Id, Is.EqualTo(quest.RegionId), objective.Id);
            }
        foreach (var r in Kyoto.Content.Residents)
        {
            AssertStandable(r.DayLocation, r.Id); AssertStandable(r.EveningLocation, r.Id);
            Assert.That(Kyoto.Region(r.DayLocation).Id, Is.EqualTo(r.RegionId), r.Id);
        }
        foreach (var i in Kyoto.Content.Interiors) AssertStandable(i.EntranceLocation, i.Id);
        foreach (var o in Kyoto.Content.Operations) { AssertStandable(o.TerminalLocation, o.Id); Assert.That(Kyoto.Region(o.TerminalLocation).Id, Is.EqualTo(o.RegionId), o.Id); }
    }

    [Test]
    public void GeneratedPropsNeverCoverAuthoredPlaces()
    {
        foreach (var point in Kyoto.ClearAreas)
        {
            var chunk = WorldGeneration.Generate(Kyoto, point.Chunk);
            foreach (var prop in chunk.Props)
                Assert.That(new WorldPoint(chunk.Address.X * 128 + prop.X, chunk.Address.Y * 128 + prop.Y).Distance(point), Is.GreaterThanOrEqualTo(32));
        }
    }

    [Test]
    public void GenerationIsStableAndUsesOnlyKyotoArt()
    {
        var a = new ChunkAddress(100, 100);
        Assert.That(Kyoto.Region(new WorldPoint(a.X * 128, a.Y * 128)).Id, Is.EqualTo(0));
        Assert.That(WorldGeneration.Generate(Kyoto, a).Terrain, Is.EqualTo(WorldGeneration.Generate(Kyoto, a).Terrain));
        for (int x = 20; x < 240; x += 20)
            for (int y = 20; y < 240; y += 20)
                Assert.That(WorldGeneration.Generate(Kyoto, new ChunkAddress(x, y)).Props.Select(p => p.Kind), Is.All.InRange(0, 7));
    }

    [Test]
    public void IdsAreUniqueAcrossAllCities()
    {
        var quests = WorldContentCatalog.Quests.Select(q => q.Id).Concat(Quests.Select(q => q.Id)).ToArray();
        var objectives = WorldContentCatalog.Quests.Concat(Quests).SelectMany(q => q.Objectives).Select(o => o.Id).ToArray();
        var residents = WorldContentCatalog.Residents.Select(r => r.Id).Concat(Kyoto.Content.Residents.Select(r => r.Id)).ToArray();
        var interiors = WorldContentCatalog.Interiors.Select(i => i.Id).Concat(Kyoto.Content.Interiors.Select(i => i.Id)).ToArray();
        Assert.That(quests, Is.Unique); Assert.That(objectives, Is.Unique); Assert.That(residents, Is.Unique); Assert.That(interiors, Is.Unique);
    }

    [Test]
    public void EveryNonCombatStepHasWrittenLinesAndEveryQuestEndsInReachableOrder()
    {
        foreach (var quest in Quests)
        {
            if (quest.PrerequisiteQuestId != null) Assert.That(Quests.Any(q => q.Id == quest.PrerequisiteQuestId), Is.True, quest.Id);
            foreach (var o in quest.Objectives.Where(o => o.Type != ObjectiveType.DefeatHostiles))
                Assert.That(WorldDialogueCatalog.HasAuthoredLines(o.Id), Is.True, o.Id);
        }
    }

    [Test]
    public void AllQuestsCanBePlayedThroughInOrder()
    {
        var state = WorldSave.ArrivalIn(Kyoto, 1, OperativeRoster.Starter);
        var done = new HashSet<string>();
        for (int pass = 0; pass < 16 && done.Count < Quests.Count(); pass++)
            foreach (var quest in Quests.Where(q => !done.Contains(q.Id) && WorldQuestRules.IsAvailable(q, state, 2, _ => true)).ToArray())
            {
                state = state.WithQuestCompleted(quest.Id, quest.BranchChoices?.First().ChoiceId);
                done.Add(quest.Id);
            }
        Assert.That(done, Has.Count.EqualTo(8));
        Assert.That(state.DistrictChoices![3], Is.EqualTo("choice_kyoto_pumps"));
    }

    [Test]
    public void KyotoQuestsOnlyAppearInKyoto()
    {
        var losAngeles = WorldSave.AfterMission(1);
        Assert.That(WorldQuestRules.Tracked(losAngeles, null, 3, _ => true)!.Quest.Id, Does.Not.StartWith("kq_"));
        Assert.That(WorldQuestRules.Tracked(WorldSave.ArrivalIn(Kyoto, 1, OperativeRoster.Starter), null, 3, _ => true)!.Quest.Id, Does.StartWith("kq_"));
    }

    [Test]
    public void ResolvesIdentitiesWithoutConfusingKyotoAndTheOldLosAngelesName()
    {
        Assert.That(CityRegistry.ForIdentity("neo-kyoto-v1"), Is.SameAs(WorldCatalog.Definition));
        Assert.That(CityRegistry.ForIdentity("neo-los-angeles-v1"), Is.SameAs(WorldCatalog.Definition));
        Assert.That(CityRegistry.ForIdentity(Kyoto.Identity), Is.SameAs(Kyoto));
        Assert.That(CityRegistry.ForIdentity("unknown"), Is.Null);
    }

    [Test]
    public void EachCityHasItsOwnSaveKeyAndLosAngelesKeepsTheOriginal()
    {
        Assert.That(WorldStorage.StateKey(7, null), Is.EqualTo("world:7"));
        Assert.That(WorldStorage.StateKey(7, WorldCatalog.Definition), Is.EqualTo("world:7"));
        Assert.That(WorldStorage.StateKey(7, Kyoto), Is.EqualTo("world:7:kyoto"));
    }

    [Test]
    public void OldSaveRecordsWithoutCityFieldsStillLoad()
    {
        foreach (string identity in new[] { "neo-kyoto-v1", "neo-los-angeles-v1" })
        {
            string json = JsonSerializer.Serialize(WorldSave.AfterMission(2) with { World = identity });
            var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
            node.Remove("CurrentCity"); node.Remove("OperationClears");
            var loaded = JsonSerializer.Deserialize<WorldSave>(node.ToJsonString())!;
            Assert.That(loaded.Valid(), Is.True, identity);
            Assert.That(loaded.CurrentCity, Is.Null);
        }
    }

    [Test]
    public void KyotoPositionsAreRejectedOutsideKyotoBounds()
    {
        var state = WorldSave.ArrivalIn(Kyoto, 1, OperativeRoster.Starter);
        Assert.That(state.Valid(), Is.True);
        Assert.That((state with { Position = new WorldPoint(60000, 60000) }).Valid(), Is.False);
        Assert.That((state with { CurrentCity = "atlantis" }).Valid(), Is.False);
    }

    [Test]
    public void TravelKeepsEachCitysProgressSeparate()
    {
        var home = WorldSave.AfterMission(3).WithQuestCompleted("quest_ok_01", null);
        var visited = WorldSave.ArrivalIn(Kyoto, 3, OperativeRoster.Starter).WithQuestCompleted("kq_gion_01", null);

        var inKyoto = WorldStorage.TravelArrival(home, home, Kyoto, visited);
        Assert.That(inKyoto.World, Is.EqualTo(Kyoto.Identity));
        Assert.That(inKyoto.Position, Is.EqualTo(Kyoto.Skyport));
        Assert.That(inKyoto.CompletedQuests!.Keys, Is.EquivalentTo(new[] { "kq_gion_01" }));

        var back = WorldStorage.TravelArrival(home with { CurrentCity = "kyoto" }, inKyoto, WorldCatalog.Definition, null);
        Assert.That(back.World, Is.EqualTo(WorldCatalog.Identity));
        Assert.That(back.CurrentCity, Is.Null);
        Assert.That(back.Position, Is.EqualTo(WorldCatalog.Definition.Skyport));
        Assert.That(back.CompletedQuests!.Keys, Is.EquivalentTo(new[] { "quest_ok_01" }));
    }

    [Test]
    public void FlyingKeepsTheDefeatReturnPointOfAnAlreadyVisitedCity()
    {
        var safehouse = new WorldPoint(44025, 76015);
        var home = WorldSave.AfterMission(3) with { LastExtraction = safehouse, CurrentCity = "kyoto" };
        var kyoto = WorldSave.ArrivalIn(Kyoto, 3, OperativeRoster.Starter);
        Assert.That(WorldStorage.TravelArrival(home, kyoto, WorldCatalog.Definition, null).LastExtraction, Is.EqualTo(safehouse));
        Assert.That(kyoto.LastExtraction, Is.EqualTo(Kyoto.Skyport), "A first arrival returns to the Skyport.");
    }

    [Test]
    public void BothSkyportsAreOnRoadsAndClearOfMissionTerminals()
    {
        var la = WorldCatalog.Definition;
        Assert.That(la.Arterial(la.Skyport), Is.True);
        Assert.That(la.Water(la.Skyport), Is.False);
        Assert.That(la.NearbyEntrance(la.Skyport, 20), Is.Null);
        Assert.That(la.Content.Interiors.Min(i => i.EntranceLocation.Distance(la.Skyport)), Is.GreaterThan(14));
        Assert.That(la.Content.Residents.Min(r => r.DayLocation.Distance(la.Skyport)), Is.GreaterThan(14));
    }

    [Test]
    public void OperationRewardsAreOneTimeThenBounded()
    {
        var operation = Kyoto.Content.Operations.Single();
        Assert.That(WorldTransactionService.OperationReward(operation, 0), Is.EqualTo((250, 0)));
        Assert.That(WorldTransactionService.OperationReward(operation, 1), Is.EqualTo((0, 60)));
        Assert.That(WorldTransactionService.OperationReward(operation, WorldTransactionService.MaxPaidRepeatClears), Is.EqualTo((0, 60)));
        Assert.That(WorldTransactionService.OperationReward(operation, WorldTransactionService.MaxPaidRepeatClears + 1), Is.EqualTo((0, 0)));
    }

    [Test]
    public void KyotoHasNoCampaignMissions()
    {
        Assert.That(Kyoto.Entrances, Is.Empty);
        Assert.That(Kyoto.Elevators, Is.Empty);
        Assert.That(Kyoto.UnlockOperative, Is.EqualTo(OperativeRoster.Starter));
        Assert.That(Kyoto.InBounds(new WorldPoint(100, 100, 1)), Is.False);
    }
}

/// <summary>Runs the authored-data rules over every city outside Los Angeles, so a new city can't skip them.</summary>
[TestFixture]
public class AllCitiesTests
{
    /// <summary>Every compact city.</summary>
    private static IEnumerable<WorldDefinition> Cities => CityRegistry.All.Where(c => c != WorldCatalog.Definition);
    /// <summary>Words naming places of worship or memorials, which must never be combat sites.</summary>
    private static readonly string[] Protected = { "temple", "shrine", "mosque", "church", "cathedral", "synagogue", "memorial", "heiau" };

    private static void AssertStandable(WorldDefinition city, WorldPoint p, string what)
    {
        Assert.That(city.InBounds(p), Is.True, $"{city.Id} {what} out of bounds: {p}");
        Assert.That(city.Water(p), Is.False, $"{city.Id} {what} in water: {p}");
    }

    [TestCaseSource(nameof(Cities))]
    public void EveryDistrictIsPopulatedAndAuthoredPointsAreStandable(WorldDefinition city)
    {
        Assert.That(city.Size, Is.EqualTo(32000));
        Assert.That(city.Stations, Has.Count.EqualTo(16));
        foreach (var region in city.Regions)
        {
            Assert.That(city.Content.Quests.Count(q => q.RegionId == region.Id), Is.EqualTo(2), region.Name);
            Assert.That(city.Content.Residents.Count(r => r.RegionId == region.Id), Is.EqualTo(2), region.Name);
            Assert.That(city.Landmarks.Count(l => city.Region(l.Position).Id == region.Id), Is.EqualTo(2), region.Name);
        }
        AssertStandable(city, city.Skyport, "skyport");
        Assert.That(city.Arterial(city.Skyport), Is.True);
        foreach (var s in city.Stations) AssertStandable(city, s.Position, $"station {s.Id}");
        foreach (var l in city.Landmarks)
        {
            AssertStandable(city, l.Position, l.Name);
            Assert.That(Protected.Any(w => l.Name.Contains(w, System.StringComparison.OrdinalIgnoreCase)), Is.False, $"{l.Name}: landmarks stand beside fights, so none may be a place of worship or memorial");
        }
        foreach (var quest in city.Content.Quests)
            foreach (var o in quest.Objectives)
            {
                AssertStandable(city, o.TargetLocation, o.Id);
                Assert.That(city.Region(o.TargetLocation).Id, Is.EqualTo(quest.RegionId), o.Id);
                if (o.Type != ObjectiveType.DefeatHostiles) Assert.That(WorldDialogueCatalog.HasAuthoredLines(o.Id), Is.True, o.Id);
                else Assert.That(Protected.Any(w => o.Description.Contains(w, System.StringComparison.OrdinalIgnoreCase)), Is.False, o.Id);
            }
        foreach (var r in city.Content.Residents) { AssertStandable(city, r.DayLocation, r.Id); AssertStandable(city, r.EveningLocation, r.Id); }
        foreach (var i in city.Content.Interiors) AssertStandable(city, i.EntranceLocation, i.Id);
        foreach (var o in city.Content.Operations) { AssertStandable(city, o.TerminalLocation, o.Id); Assert.That(city.Region(o.TerminalLocation).Id, Is.EqualTo(o.RegionId), o.Id); }
    }

    [TestCaseSource(nameof(Cities))]
    public void GeneratedPropsNeverCoverAuthoredPlaces(WorldDefinition city)
    {
        foreach (var point in city.ClearAreas)
        {
            var chunk = WorldGeneration.Generate(city, point.Chunk);
            foreach (var prop in chunk.Props)
                Assert.That(new WorldPoint(chunk.Address.X * 128 + prop.X, chunk.Address.Y * 128 + prop.Y).Distance(point), Is.GreaterThanOrEqualTo(32), city.Id);
        }
    }

    [TestCaseSource(nameof(Cities))]
    public void AllQuestsCanBeCompletedInOrder(WorldDefinition city)
    {
        var state = WorldSave.ArrivalIn(city, 3, OperativeRoster.Starter);
        var done = new HashSet<string>();
        for (int pass = 0; pass < 16 && done.Count < 8; pass++)
            foreach (var quest in city.Content.Quests.Where(q => !done.Contains(q.Id) && WorldQuestRules.IsAvailable(q, state, 5, _ => true)).ToArray())
            {
                state = state.WithQuestCompleted(quest.Id, quest.BranchChoices?.First().ChoiceId);
                done.Add(quest.Id);
            }
        Assert.That(done, Has.Count.EqualTo(8), city.Id);
        Assert.That(city.Content.Quests.Count(q => q.RequiredOperative == city.UnlockOperative), Is.EqualTo(1), $"{city.Id} needs one story for its operative");
    }

    [TestCaseSource(nameof(Cities))]
    public void OpensExactlyWhenItsOperativeIsRecruited(WorldDefinition city)
    {
        Assert.That(CityRegistry.IsOpen(city, name => name == city.UnlockOperative), Is.True, city.Id);
        Assert.That(CityRegistry.IsOpen(city, _ => false), Is.False, city.Id);
        Assert.That(OperativeRoster.RosterOrder, Does.Contain(city.UnlockOperative));
    }

    [TestCaseSource(nameof(Cities))]
    public void SaveRecordsAreValidAndUseTheirOwnRow(WorldDefinition city)
    {
        Assert.That(WorldSave.ArrivalIn(city, 3, OperativeRoster.Starter).Valid(), Is.True, city.Id);
        Assert.That(WorldStorage.StateKey(9, city), Is.EqualTo($"world:9:{city.Id}"));
        Assert.That(CityRegistry.ForIdentity(city.Identity), Is.SameAs(city));
    }

    [Test]
    public void IdsAreUniqueAcrossEveryCity()
    {
        var quests = WorldContentCatalog.Quests.Concat(Cities.SelectMany(c => c.Content.Quests)).ToArray();
        Assert.That(quests.Select(q => q.Id), Is.Unique);
        Assert.That(quests.SelectMany(q => q.Objectives).Select(o => o.Id), Is.Unique);
        Assert.That(WorldContentCatalog.Residents.Concat(Cities.SelectMany(c => c.Content.Residents)).Select(r => r.Id), Is.Unique);
        Assert.That(WorldContentCatalog.Interiors.Concat(Cities.SelectMany(c => c.Content.Interiors)).Select(i => i.Id), Is.Unique);
        Assert.That(Cities.SelectMany(c => c.Content.Operations).Select(o => o.Id), Is.Unique);
        Assert.That(CityRegistry.All.Select(c => c.Id), Is.Unique);
    }

    [Test]
    public void EachOperativeHasExactlyOneHomeCity() =>
        Assert.That(Cities.Select(c => c.UnlockOperative), Is.EquivalentTo(OperativeRoster.RosterOrder));

    [Test]
    public void SeaEdgesCoverTheStatedSideOnly()
    {
        Assert.That(MumbaiCity.Definition.Water(new WorldPoint(1000, 1000)), Is.True);
        Assert.That(MumbaiCity.Definition.Water(new WorldPoint(31000, 1000)), Is.False);
        Assert.That(HonoluluCity.Definition.Water(new WorldPoint(1000, 31000)), Is.True);
        Assert.That(DubaiCity.Definition.Water(new WorldPoint(9000, 1000)), Is.True);
    }
}
