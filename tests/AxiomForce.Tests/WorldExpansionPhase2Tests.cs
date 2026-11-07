using System;
using System.Linq;
using NUnit.Framework;

namespace AxiomForce.Tests;

[TestFixture]
public class WorldExpansionPhase2Tests
{
    [Test]
    public void Catalog_ContainsAllThirtyFiveAuthoredQuestsWithValidStructure()
    {
        Assert.That(WorldContentCatalog.Quests.Count, Is.EqualTo(35));

        // Check Phase 2 District Chains
        string[] expectedDistrictQuests = {
            "quest_dt_01", "quest_dt_02", "quest_dt_03",
            "quest_hb_01", "quest_hb_02", "quest_hb_03",
            "quest_ct_01", "quest_ct_02", "quest_ct_03",
            "quest_rr_01", "quest_rr_02", "quest_rr_03",
            "quest_fd_01", "quest_fd_02", "quest_fd_03"
        };
        foreach (var qid in expectedDistrictQuests)
        {
            var q = WorldContentCatalog.Quests.FirstOrDefault(x => x.Id == qid);
            Assert.That(q, Is.Not.Null, $"Missing expected district quest {qid}");
            Assert.That(q!.Objectives.Count, Is.GreaterThan(0));
            Assert.That(q.Reward.HeroSparks, Is.GreaterThan(0));
            Assert.That(q.Reward.CityCredits, Is.GreaterThan(0));
        }

        // Check Phase 2 Faction Quests (12 Total: 3 per faction)
        string[] expectedFactionQuests = {
            "quest_crn_01", "quest_crn_02", "quest_crn_03",
            "quest_cou_01", "quest_cou_02", "quest_cou_03",
            "quest_rec_01", "quest_rec_02", "quest_rec_03",
            "quest_fsc_01", "quest_fsc_02", "quest_fsc_03"
        };
        foreach (var qid in expectedFactionQuests)
        {
            var q = WorldContentCatalog.Quests.FirstOrDefault(x => x.Id == qid);
            Assert.That(q, Is.Not.Null, $"Missing expected faction quest {qid}");
            Assert.That(q!.Category, Is.EqualTo(QuestCategory.Faction));
            Assert.That(q.Reward.FactionId, Is.Not.Null);
            Assert.That(q.Reward.FactionReputation, Is.GreaterThan(0));
        }
    }

    [Test]
    public void Catalog_ContainsAllTwentyOneRegionalChallenges()
    {
        Assert.That(WorldContentCatalog.Challenges.Count, Is.EqualTo(21));

        int[] urbanRegions = { 6, 7, 10, 13, 14 };
        foreach (var r in urbanRegions)
        {
            var challenges = WorldContentCatalog.Challenges.Where(c => c.RegionId == r).ToList();
            Assert.That(challenges.Count, Is.EqualTo(3), $"Region {r} should have exactly 3 challenges");
            Assert.That(challenges.Any(c => c.Type == ChallengeType.Race), Is.True);
            Assert.That(challenges.Any(c => c.Type == ChallengeType.OperativePuzzle), Is.True);
            Assert.That(challenges.Any(c => c.Type == ChallengeType.DiscoveryTrail), Is.True);
        }
    }

    [Test]
    public void Catalog_ContainsAllSevenSideOperations()
    {
        Assert.That(WorldContentCatalog.SideOperations.Count, Is.EqualTo(7));

        string[] expectedOps = {
            "side_op_palace_vault",
            "side_op_viaduct_defense",
            "side_op_data_haven",
            "side_op_harbor_slipway",
            "side_op_citadel_perimeter",
            "side_op_freight_intercept",
            "side_op_smelting_core"
        };
        foreach (var opId in expectedOps)
        {
            var op = WorldContentCatalog.SideOperations.FirstOrDefault(o => o.Id == opId);
            Assert.That(op, Is.Not.Null, $"Missing side operation {opId}");
            Assert.That(op!.ScenePath, Does.StartWith("res://scenes/world/side_ops/"));
        }
    }

    [Test]
    public void Catalog_ContainsAllTwentyOneResidentsWithSchedules()
    {
        Assert.That(WorldContentCatalog.Residents.Count, Is.EqualTo(21));

        foreach (var resident in WorldContentCatalog.Residents)
        {
            Assert.That(resident.DayLocation.Distance(new WorldPoint(0, 0)), Is.GreaterThan(1000));
            Assert.That(resident.EveningLocation.Distance(new WorldPoint(0, 0)), Is.GreaterThan(1000));
            Assert.That(string.IsNullOrWhiteSpace(resident.Name), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(resident.Faction), Is.False);
        }
    }

    [Test]
    public void Catalog_ContainsAllNineInteriorsIncludingFiveUrbanSafehouses()
    {
        Assert.That(WorldContentCatalog.Interiors.Count, Is.EqualTo(9));

        string[] urbanSafehouses = {
            "safehouse_downtown",
            "safehouse_harbor",
            "safehouse_citadel",
            "safehouse_rustrail",
            "safehouse_foundry"
        };

        foreach (var sh in urbanSafehouses)
        {
            var interior = WorldContentCatalog.Interiors.FirstOrDefault(i => i.Id == sh);
            Assert.That(interior, Is.Not.Null, $"Missing urban safehouse {sh}");
            Assert.That(interior!.InteriorKind, Is.EqualTo("safehouse"));
            Assert.That(interior.ProvidedServices.Contains("rest_heal"), Is.True);
        }
    }

    [Test]
    public void Traversal_UtilityBoatConfigurationAndRestrictions()
    {
        var boat = WorldContentCatalog.Vehicles.FirstOrDefault(v => v.Id == "veh_utility_boat");
        Assert.That(boat, Is.Not.Null);
        Assert.That(boat!.CanEnterWater, Is.True);
        Assert.That(boat.MaxSpeedMetresPerSec, Is.EqualTo(14.0f));
        Assert.That(boat.UnlockCostCredits, Is.EqualTo(400));

        // Water location: X > 124000 is defined as water in WorldCatalog.Water
        var waterPoint = new WorldPoint(126000, 50000);
        Assert.That(WorldCatalog.Water(waterPoint), Is.True);

        // Dry land location is not water
        var landPoint = new WorldPoint(44000, 44000);
        Assert.That(WorldCatalog.Water(landPoint), Is.False);
    }

    [Test]
    public void Factions_ReputationThresholdsAndProgressionTiers()
    {
        var prog = new CampaignProgressionState();

        Assert.That(prog.GetFactionReputation(FactionId.CivicRecovery), Is.EqualTo(0));
        Assert.That(prog.GetFactionTier(FactionId.CivicRecovery), Is.EqualTo(FactionTier.Contact));

        // Add 120 points -> Trusted tier
        prog.AddFactionReputation(FactionId.CivicRecovery, 120);
        Assert.That(prog.GetFactionReputation(FactionId.CivicRecovery), Is.EqualTo(120));
        Assert.That(prog.GetFactionTier(FactionId.CivicRecovery), Is.EqualTo(FactionTier.Trusted));

        // Add 150 points (total 270) -> Partner tier
        prog.AddFactionReputation(FactionId.CivicRecovery, 150);
        Assert.That(prog.GetFactionReputation(FactionId.CivicRecovery), Is.EqualTo(270));
        Assert.That(prog.GetFactionTier(FactionId.CivicRecovery), Is.EqualTo(FactionTier.Partner));
    }

    [Test]
    public void Headquarters_AmenitiesUnlockingAndEconomics()
    {
        var prog = new CampaignProgressionState { CityCredits = 400 };

        Assert.That(prog.UnlockedHqAmenities.Count, Is.EqualTo(0));

        // Unlock first amenity (costs 250)
        bool success1 = prog.UnlockHqAmenity(HqAmenities.TacticalHoloTable, 250);
        Assert.That(success1, Is.True);
        Assert.That(prog.CityCredits, Is.EqualTo(150));
        Assert.That(prog.UnlockedHqAmenities.Contains(HqAmenities.TacticalHoloTable), Is.True);

        // Attempt second amenity without sufficient credits (costs 250, has 150)
        bool success2 = prog.UnlockHqAmenity(HqAmenities.SquadQuarters, 250);
        Assert.That(success2, Is.False);
        Assert.That(prog.CityCredits, Is.EqualTo(150));

        // Duplicate unlock must fail
        prog.CityCredits = 500;
        bool duplicate = prog.UnlockHqAmenity(HqAmenities.TacticalHoloTable, 250);
        Assert.That(duplicate, Is.False);
    }

    [Test]
    public void FactionModules_AdhereToStrictPassiveCeiling()
    {
        var state = new CampaignProgressionState();
        var hero = state.GetHero("Cyber Shogun");

        // Equip faction modules
        hero.Loadout.ModuleSlot1 = "mod_courier_booster"; // +10% move speed, -5% shield
        hero.Loadout.ModuleSlot2 = "mod_reclamation_mesh"; // +10% shield, -5% slash damage

        var mods = state.GetEffectiveModifiers("Cyber Shogun");

        // Verify ceiling capped at 20%
        Assert.That(mods.HealthBonusPct, Is.LessThanOrEqualTo(0.20f));
        Assert.That(mods.ShieldBonusPct, Is.LessThanOrEqualTo(0.20f));
        Assert.That(mods.DamageBonusPct, Is.LessThanOrEqualTo(0.20f));
        Assert.That(mods.CooldownReductionPct, Is.LessThanOrEqualTo(0.20f));
    }
}
