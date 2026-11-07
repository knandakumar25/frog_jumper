using System;
using System.Linq;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit and contract acceptance tests for Neo-Kyoto World Expansion Phase 3: Natural &amp; Coastal Expansion.
/// Verifies the full completion of master targets across all 16 districts:
/// 72 Quests, 48 Challenges, 16 Side Operations, 48 Residents, 16 Safehouses, 64 Pockets,
/// and dynamic 4-state regional weather cycle.
/// </summary>
[TestFixture]
public class WorldExpansionPhase3Tests
{
    /// <summary>
    /// Verifies that exactly 72 authored quests exist across the world:
    /// - 48 District Quests (16 regions * 3)
    /// - 12 Operative Personal Episodes (6 operatives * 2)
    /// - 12 Faction Quests (4 organizations * 3)
    /// </summary>
    [Test]
    public void Catalog_ContainsExactMasterQuotaOfSeventyTwoQuests()
    {
        Assert.That(WorldContentCatalog.Quests.Count, Is.EqualTo(72), "Total quests must equal 72.");

        // Check District Quests (16 regions * 3)
        var districtQuests = WorldContentCatalog.Quests.Where(q => q.Category == QuestCategory.District).ToList();
        Assert.That(districtQuests.Count, Is.EqualTo(48), "Must have 48 district storyline quests (3 per region).");

        for (int r = 0; r < 16; r++)
        {
            var regionDistrictQuests = districtQuests.Where(q => q.RegionId == r).ToList();
            Assert.That(regionDistrictQuests.Count, Is.EqualTo(3), $"Region {r} must have exactly 3 district quests.");
            foreach (var q in regionDistrictQuests)
            {
                Assert.That(q.Objectives.Count, Is.GreaterThan(0));
                Assert.That(q.Reward.HeroSparks, Is.GreaterThan(0));
                Assert.That(q.Reward.CityCredits, Is.GreaterThan(0));
            }
        }

        // Check Operative Personal Episodes (6 operatives * 2 = 12)
        var operativeQuests = WorldContentCatalog.Quests.Where(q => q.Category == QuestCategory.Operative).ToList();
        Assert.That(operativeQuests.Count, Is.EqualTo(12), "Must have 12 operative personal episodes.");

        string[] operativeIds = { "Cyber Shogun", "Surya Prime", "Oceanus", "Mirage Blade", "Apex Titan", "Aura Phoenicia" };
        foreach (var opId in operativeIds)
        {
            var opEpisodes = operativeQuests.Where(q => q.RequiredOperative == opId).ToList();
            Assert.That(opEpisodes.Count, Is.EqualTo(2), $"Operative {opId} must have exactly 2 personal episodes.");
            foreach (var ep in opEpisodes)
            {
                Assert.That(string.IsNullOrWhiteSpace(ep.Title), Is.False);
                Assert.That(ep.Reward.HeroSparks, Is.GreaterThanOrEqualTo(100));
            }
        }

        // Check Faction Quests (4 factions * 3 = 12)
        var factionQuests = WorldContentCatalog.Quests.Where(q => q.Category == QuestCategory.Faction).ToList();
        Assert.That(factionQuests.Count, Is.EqualTo(12), "Must have 12 faction quests.");
        string[] factionIds = { FactionId.CivicRecovery, FactionId.IndependentCouriers, FactionId.ReclamationGuild, FactionId.FreeSignalCollective };
        foreach (var fac in factionIds)
        {
            var facQ = factionQuests.Where(q => q.Reward.FactionId == fac).ToList();
            Assert.That(facQ.Count, Is.EqualTo(3), $"Faction {fac} must have exactly 3 quests.");
        }
    }

    /// <summary>
    /// Verifies that exactly 48 regional challenges exist (exactly 3 per region across all 16 regions),
    /// consisting of 1 Race, 1 Traversal/Operative Puzzle, and 1 Discovery Trail each.
    /// </summary>
    [Test]
    public void Catalog_ContainsExactMasterQuotaOfFortyEightChallenges()
    {
        Assert.That(WorldContentCatalog.Challenges.Count, Is.EqualTo(48), "Total challenges must equal 48.");

        for (int r = 0; r < 16; r++)
        {
            var regionChallenges = WorldContentCatalog.Challenges.Where(c => c.RegionId == r).ToList();
            Assert.That(regionChallenges.Count, Is.EqualTo(3), $"Region {r} must have exactly 3 challenges.");
            Assert.That(regionChallenges.Any(c => c.Type == ChallengeType.Race), Is.True, $"Region {r} missing Race challenge");
            Assert.That(regionChallenges.Any(c => c.Type == ChallengeType.OperativePuzzle), Is.True, $"Region {r} missing OperativePuzzle challenge");
            Assert.That(regionChallenges.Any(c => c.Type == ChallengeType.DiscoveryTrail), Is.True, $"Region {r} missing DiscoveryTrail challenge");

            foreach (var ch in regionChallenges)
            {
                Assert.That(ch.FirstClearSparks, Is.GreaterThan(0));
                Assert.That(ch.RepeatCredits, Is.GreaterThan(0));
                Assert.That(ch.StartLocation.Distance(new WorldPoint(0, 0)), Is.GreaterThan(1000));
            }
        }
    }

    /// <summary>
    /// Verifies that all 16 regions have exactly one replayable Side Operation.
    /// </summary>
    [Test]
    public void Catalog_ContainsExactMasterQuotaOfSixteenSideOperations()
    {
        Assert.That(WorldContentCatalog.SideOperations.Count, Is.EqualTo(16), "Total side operations must equal 16.");

        for (int r = 0; r < 16; r++)
        {
            var op = WorldContentCatalog.SideOperations.FirstOrDefault(o => o.RegionId == r);
            Assert.That(op, Is.Not.Null, $"Region {r} must have an authored side operation.");
            Assert.That(op!.ScenePath, Does.StartWith("res://scenes/world/side_ops/"));
            Assert.That(string.IsNullOrWhiteSpace(op.Name), Is.False);
            Assert.That(op.FirstClearSparks, Is.GreaterThan(0));
            Assert.That(op.RepeatCredits, Is.GreaterThan(0));
        }
    }

    /// <summary>
    /// Verifies that all 48 named residents exist (3 per region) with authored day and evening schedules.
    /// </summary>
    [Test]
    public void Catalog_ContainsExactMasterQuotaOfFortyEightResidentsWithAuthoredSchedules()
    {
        Assert.That(WorldContentCatalog.Residents.Count, Is.EqualTo(48), "Total named residents must equal 48.");

        for (int r = 0; r < 16; r++)
        {
            var residentsInRegion = WorldContentCatalog.Residents.Where(res => res.RegionId == r).ToList();
            Assert.That(residentsInRegion.Count, Is.EqualTo(3), $"Region {r} must have exactly 3 named residents.");

            foreach (var resident in residentsInRegion)
            {
                Assert.That(string.IsNullOrWhiteSpace(resident.Name), Is.False);
                Assert.That(string.IsNullOrWhiteSpace(resident.Faction), Is.False);
                Assert.That(resident.DayLocation.Distance(new WorldPoint(0, 0)), Is.GreaterThan(1000));
                Assert.That(resident.EveningLocation.Distance(new WorldPoint(0, 0)), Is.GreaterThan(1000));
            }
        }
    }

    /// <summary>
    /// Verifies that all 16 districts have a safehouse interior providing vitals recovery and tactical services.
    /// </summary>
    [Test]
    public void Catalog_ContainsExactMasterQuotaOfSixteenDistrictSafehouses()
    {
        var safehouses = WorldContentCatalog.Interiors.Where(i => i.InteriorKind == "safehouse").ToList();
        Assert.That(safehouses.Count, Is.EqualTo(16), "Total district safehouses must equal 16.");

        foreach (var safehouse in safehouses)
        {
            var pocket = WorldContentCatalog.Pockets.FirstOrDefault(p => p.Id == safehouse.PocketId);
            Assert.That(pocket, Is.Not.Null, $"Safehouse {safehouse.Id} must link to a valid pocket");
            Assert.That(safehouse.ProvidedServices, Does.Contain("rest_heal").Or.Contain("medical_clinic").Or.Contain("tactical_loadout"));
            Assert.That(safehouse.EntranceLocation.Distance(new WorldPoint(0, 0)), Is.GreaterThan(1000));
        }
    }

    /// <summary>
    /// Verifies that all 64 activity pockets exist (exactly 4 per region).
    /// </summary>
    [Test]
    public void Catalog_ContainsExactMasterQuotaOfSixtyFourPockets()
    {
        Assert.That(WorldContentCatalog.Pockets.Count, Is.EqualTo(64), "Total pockets must equal 64.");

        for (int r = 0; r < 16; r++)
        {
            var pockets = WorldContentCatalog.Pockets.Where(p => p.RegionId == r).ToList();
            Assert.That(pockets.Count, Is.EqualTo(4), $"Region {r} must have exactly 4 activity pockets.");
        }
    }

    /// <summary>
    /// Verifies the 4-state regional dynamic weather cycle in NeoKyotoWorld:
    /// Clear (0-24m), Rain (24-32m), Fog (32-40m), RegionalStorm (40-48m).
    /// </summary>
    [Test]
    public void WeatherState_CyclesPredictablyAcrossFortyEightMinutePeriod()
    {
        // 48 minutes total cycle
        Assert.That(NeoKyotoWorld.GetWeatherForTime(0), Is.EqualTo(NeoKyotoWorld.WeatherState.Clear));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(12), Is.EqualTo(NeoKyotoWorld.WeatherState.Clear));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(24), Is.EqualTo(NeoKyotoWorld.WeatherState.Rain));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(30), Is.EqualTo(NeoKyotoWorld.WeatherState.Rain));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(32), Is.EqualTo(NeoKyotoWorld.WeatherState.Fog));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(38), Is.EqualTo(NeoKyotoWorld.WeatherState.Fog));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(40), Is.EqualTo(NeoKyotoWorld.WeatherState.RegionalStorm));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(46), Is.EqualTo(NeoKyotoWorld.WeatherState.RegionalStorm));
        Assert.That(NeoKyotoWorld.GetWeatherForTime(48), Is.EqualTo(NeoKyotoWorld.WeatherState.Clear)); // wraps to 0 min
    }

    /// <summary>
    /// Verifies era progression across natural, coastal, and wilderness regions.
    /// </summary>
    [TestCase(0, 0, DistrictEra.PreMission)]
    [TestCase(0, 10, DistrictEra.Aftermath)]
    [TestCase(0, 13, DistrictEra.Liberated)]
    [TestCase(12, 0, DistrictEra.PreMission)]
    [TestCase(12, 12, DistrictEra.Aftermath)]
    [TestCase(12, 13, DistrictEra.Liberated)]
    [TestCase(15, 0, DistrictEra.PreMission)]
    [TestCase(15, 2, DistrictEra.Aftermath)]
    [TestCase(15, 13, DistrictEra.Liberated)]
    public void DistrictEra_ProgressesAccuratelyAcrossNaturalAndCoastalRegions(int regionId, int missionProgress, DistrictEra expectedEra)
    {
        var era = DistrictState.DetermineEra(regionId, missionProgress);
        Assert.That(era, Is.EqualTo(expectedEra));
    }
}
