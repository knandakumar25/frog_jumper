using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using NUnit.Framework;

/// <summary>
/// Comprehensive acceptance test suite for Neo-Kyoto's Living Resistance World (Phase 0 and Phase 1).
/// Verifies content catalogs, quests, residents, vehicles, typed progression limits, and atomic transactions.
/// </summary>
public class WorldExpansionTests
{
    /// <summary>
    /// Verifies the presence of all 64 activity pockets across 16 regions, with Old Kyoto and
    /// Concrete Wards authored in full detail.
    /// </summary>
    [Test]
    public void PocketsAndInteriorsMatchSpecification()
    {
        Assert.That(WorldContentCatalog.Pockets.Count, Is.EqualTo(64), "Must have 64 activity pockets across the 16 regions.");

        var okPockets = WorldContentCatalog.Pockets.Where(p => p.RegionId == 5).ToList();
        Assert.That(okPockets.Count, Is.EqualTo(4), "Old Kyoto must have 4 pockets (arrival hub + 3 satellites).");
        Assert.That(okPockets.Any(p => p.Id == "ok_pocket_hub" && p.IsSafeZone), Is.True);
        Assert.That(okPockets.Any(p => p.Id == "ok_pocket_lantern_market" && p.IsSafeZone), Is.True);
        Assert.That(okPockets.Any(p => p.Id == "ok_pocket_palace_investigation" && !p.IsSafeZone), Is.True, "Palace perimeter is a hostile pocket.");
        Assert.That(okPockets.Any(p => p.Id == "ok_pocket_moonbridge" && p.IsSafeZone), Is.True);

        var cwPockets = WorldContentCatalog.Pockets.Where(p => p.RegionId == 9).ToList();
        Assert.That(cwPockets.Count, Is.EqualTo(4), "Concrete Wards must have 4 pockets.");
        Assert.That(cwPockets.Any(p => p.Id == "cw_pocket_hub" && p.IsSafeZone), Is.True);
        Assert.That(cwPockets.Any(p => p.Id == "cw_pocket_courtyard" && !p.IsSafeZone), Is.True);

        // Safehouses
        var okSafehouse = WorldContentCatalog.Interiors.FirstOrDefault(i => i.Id == "safehouse_old_kyoto");
        Assert.That(okSafehouse, Is.Not.Null);
        Assert.That(okSafehouse!.ProvidedServices, Does.Contain("tactical_loadout"));

        var cwSafehouse = WorldContentCatalog.Interiors.FirstOrDefault(i => i.Id == "safehouse_concrete_wards");
        Assert.That(cwSafehouse, Is.Not.Null);
        Assert.That(cwSafehouse!.ProvidedServices, Does.Contain("medical_clinic"));
    }

    /// <summary>
    /// Verifies that all 6 named residents for Old Kyoto and Concrete Wards exist with authored schedules.
    /// </summary>
    [Test]
    public void NamedResidentsHaveAuthoredSchedulesAndDialog()
    {
        Assert.That(WorldContentCatalog.Residents.Count, Is.EqualTo(6));

        string[] expected = { "npc_sora", "npc_gen", "npc_ren", "npc_maya_chen", "npc_barik", "npc_yuki" };
        foreach (var id in expected)
        {
            var resident = WorldContentCatalog.Residents.FirstOrDefault(r => r.Id == id);
            Assert.That(resident, Is.Not.Null, $"Resident {id} must exist in catalog.");
            Assert.That(resident!.GreetingDialogue, Is.Not.Empty);
            Assert.That(resident.DialogGraphId, Is.Not.Empty);
            Assert.That(WorldCatalog.InBounds(resident.DayLocation), Is.True);
            Assert.That(WorldCatalog.InBounds(resident.EveningLocation), Is.True);
        }
    }

    /// <summary>
    /// Verifies the 8 authored quests: prerequisites, objectives, and branch tradeoffs.
    /// </summary>
    [Test]
    public void QuestsAndBranchesMatchNarrativeChain()
    {
        Assert.That(WorldContentCatalog.Quests.Count, Is.EqualTo(8));

        // Old Kyoto Chain
        var qOk1 = WorldContentCatalog.Quests.First(q => q.Id == "quest_ok_01");
        var qOk2 = WorldContentCatalog.Quests.First(q => q.Id == "quest_ok_02");
        var qOk3 = WorldContentCatalog.Quests.First(q => q.Id == "quest_ok_03");

        Assert.That(qOk1.MinCampaignMission, Is.EqualTo(1));
        Assert.That(qOk2.PrerequisiteQuestId, Is.EqualTo("quest_ok_01"));
        Assert.That(qOk3.PrerequisiteQuestId, Is.EqualTo("quest_ok_02"));
        Assert.That(qOk3.BranchChoices?.Count, Is.EqualTo(2), "The Moonbridge Promise must offer 2 consequential choices.");
        Assert.That(qOk3.BranchChoices?.Select(c => c.ChoiceId), Is.EquivalentTo(new[] { "choice_artisan_guild", "choice_wall_defenses" }));

        // Concrete Wards Chain
        var qCw1 = WorldContentCatalog.Quests.First(q => q.Id == "quest_cw_01");
        var qCw2 = WorldContentCatalog.Quests.First(q => q.Id == "quest_cw_02");
        var qCw3 = WorldContentCatalog.Quests.First(q => q.Id == "quest_cw_03");

        Assert.That(qCw1.MinCampaignMission, Is.EqualTo(2));
        Assert.That(qCw2.PrerequisiteQuestId, Is.EqualTo("quest_cw_01"));
        Assert.That(qCw3.PrerequisiteQuestId, Is.EqualTo("quest_cw_02"));
        Assert.That(qCw3.BranchChoices?.Count, Is.EqualTo(2));
        Assert.That(qCw3.BranchChoices?.Select(c => c.ChoiceId), Is.EquivalentTo(new[] { "choice_medical_clinic", "choice_workshop_expansion" }));

        // Operative Personal Episodes
        var qShogun = WorldContentCatalog.Quests.First(q => q.Id == "quest_op_shogun");
        Assert.That(qShogun.RequiredOperative, Is.EqualTo("Cyber Shogun"));

        var qOceanus = WorldContentCatalog.Quests.First(q => q.Id == "quest_op_oceanus");
        Assert.That(qOceanus.RequiredOperative, Is.EqualTo("Oceanus"));
    }

    /// <summary>
    /// Verifies regional challenges and replayable side operations.
    /// </summary>
    [Test]
    public void ChallengesAndSideOperationsAreRegistered()
    {
        Assert.That(WorldContentCatalog.Challenges.Count, Is.EqualTo(6));
        Assert.That(WorldContentCatalog.Challenges.Count(c => c.RegionId == 5), Is.EqualTo(3));
        Assert.That(WorldContentCatalog.Challenges.Count(c => c.RegionId == 9), Is.EqualTo(3));

        Assert.That(WorldContentCatalog.SideOperations.Count, Is.EqualTo(2));
        Assert.That(WorldContentCatalog.SideOperations.Any(s => s.Id == "side_op_palace_vault" && s.RegionId == 5), Is.True);
        Assert.That(WorldContentCatalog.SideOperations.Any(s => s.Id == "side_op_viaduct_defense" && s.RegionId == 9), Is.True);
    }

    /// <summary>
    /// Verifies the starter electric bike traversal specifications and vehicle limitations.
    /// </summary>
    [Test]
    public void StarterBikeMeetsTraversalRequirements()
    {
        var bike = WorldContentCatalog.StarterBike;
        Assert.That(bike.MaxSpeedMetresPerSec, Is.EqualTo(20.0f));
        Assert.That(bike.CanEnterWater, Is.False);
        Assert.That(bike.UnlockCostCredits, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that typed tactical modifiers enforce the strict 20% passive bonus ceiling
    /// across health, shield, damage, and cooldown reduction.
    /// </summary>
    [Test]
    public void ProgressionEnforcesTwentyPercentPassiveCeiling()
    {
        var progression = new CampaignProgressionState();
        var hero = progression.GetHero("Cyber Shogun");

        // Max out all upgrades to rank 5
        hero.Level = 10;
        hero.UpgradeRanks[0] = 5;
        hero.UpgradeRanks[1] = 5;
        hero.UpgradeRanks[2] = 5;

        // Equip modules
        hero.Loadout.ModuleSlot1 = "mod_kinetic_coil";
        hero.Loadout.ModuleSlot2 = "mod_thermal_sink";

        var modifiers = progression.GetEffectiveModifiers("Cyber Shogun");

        Assert.That(modifiers.HealthBonusPct, Is.LessThanOrEqualTo(CampaignProgressionState.PassiveStatCeiling + 0.0001f));
        Assert.That(modifiers.ShieldBonusPct, Is.LessThanOrEqualTo(CampaignProgressionState.PassiveStatCeiling + 0.0001f));
        Assert.That(modifiers.DamageBonusPct, Is.LessThanOrEqualTo(CampaignProgressionState.PassiveStatCeiling + 0.0001f));
        Assert.That(modifiers.CooldownReductionPct, Is.LessThanOrEqualTo(CampaignProgressionState.PassiveStatCeiling + 0.0001f));
    }

    /// <summary>
    /// Verifies that district era transitions correctly reflect campaign progress.
    /// </summary>
    [TestCase(5, 1, DistrictEra.PreMission)]
    [TestCase(5, 2, DistrictEra.Aftermath)]
    [TestCase(5, 13, DistrictEra.Liberated)]
    [TestCase(9, 2, DistrictEra.PreMission)]
    [TestCase(9, 3, DistrictEra.Aftermath)]
    [TestCase(9, 13, DistrictEra.Liberated)]
    public void DistrictEraTransitionsCorrectly(int regionId, int unlockedMission, DistrictEra expectedEra)
    {
        Assert.That(DistrictState.DetermineEra(regionId, unlockedMission), Is.EqualTo(expectedEra));
    }
}

/// <summary>
/// Integration tests verifying PostgreSQL atomic transactions and action receipt idempotency.
/// </summary>
[NonParallelizable]
public class WorldTransactionTests
{
    private readonly List<int> _owned = new();
    private string _connection = "";

    [SetUp]
    public void Configure()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "project.godot"))) root = root.Parent;
        string previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(root!.FullName);
            _connection = PostgresStorage.ConnectionString;
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    private async Task<int> CreateCampaignAsync()
    {
        await using var c = new NpgsqlConnection(_connection);
        await c.OpenAsync();
        await using (var schema = new NpgsqlCommand(
            "CREATE TABLE IF NOT EXISTS game_sessions (id SERIAL PRIMARY KEY,character VARCHAR(100) NOT NULL,current_level INT NOT NULL DEFAULT 1,total_score INT NOT NULL DEFAULT 0,collectables INT NOT NULL DEFAULT 0,created_at VARCHAR(100) NOT NULL,last_played VARCHAR(100) NOT NULL)", c))
            await schema.ExecuteNonQueryAsync();

        await using var cmd = new NpgsqlCommand("INSERT INTO game_sessions(character,created_at,last_played) VALUES ('world-tx-fixture','fixture','fixture') RETURNING id", c);
        int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _owned.Add(id);
        return id;
    }

    [TearDown]
    public async Task CleanupAsync()
    {
        await using var c = new NpgsqlConnection(_connection);
        await c.OpenAsync();
        foreach (int id in _owned)
        {
            await using var cmd = new NpgsqlCommand("DELETE FROM game_state WHERE state_key LIKE @prefix; DELETE FROM game_sessions WHERE id=@id AND created_at='fixture'", c);
            cmd.Parameters.AddWithValue("prefix", $"%:{id}%");
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        _owned.Clear();
    }

    /// <summary>
    /// Verifies that quest completion commits rewards and receipt atomically, and retrying the same
    /// action token is idempotent without duplicating sparks or credits.
    /// </summary>
    [Test]
    public async Task QuestCompletionIsAtomicAndIdempotent()
    {
        int session = await CreateCampaignAsync();
        string token = Guid.NewGuid().ToString();

        // Initial completion
        var receipt1 = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, token, _connection);
        Assert.That(receipt1.Status, Is.EqualTo(TransactionStatus.Success));

        await using (var conn = new NpgsqlConnection(_connection))
        {
            await conn.OpenAsync();
            var prog = await CampaignProgressionState.LoadOrCreateAsync(session, conn);
            Assert.That(prog.GlobalSparksBank, Is.EqualTo(350), "Starter 250 + 100 reward = 350");
            Assert.That(prog.CityCredits, Is.EqualTo(650), "Starter 500 + 150 reward = 650");
        }

        // Retry with same token
        var receipt2 = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, token, _connection);
        Assert.That(receipt2.Status, Is.EqualTo(TransactionStatus.AlreadyCommitted));

        // Balances must not have increased twice
        await using (var conn = new NpgsqlConnection(_connection))
        {
            await conn.OpenAsync();
            var prog = await CampaignProgressionState.LoadOrCreateAsync(session, conn);
            Assert.That(prog.GlobalSparksBank, Is.EqualTo(350), "Sparks must not duplicate on retry.");
            Assert.That(prog.CityCredits, Is.EqualTo(650), "Credits must not duplicate on retry.");
        }
    }

    /// <summary>
    /// Verifies purchasing an item with credits, deducting cost and unlocking the module.
    /// </summary>
    [Test]
    public async Task PurchaseItemDeductsCreditsAndEnforcesBalance()
    {
        int session = await CreateCampaignAsync();
        string token1 = Guid.NewGuid().ToString();

        // Purchase module for 200 credits
        var receipt1 = await WorldTransactionService.PurchaseItemAsync(session, "module", "mod_kinetic_coil", 200, token1, _connection);
        Assert.That(receipt1.Status, Is.EqualTo(TransactionStatus.Success));

        await using (var conn = new NpgsqlConnection(_connection))
        {
            await conn.OpenAsync();
            var prog = await CampaignProgressionState.LoadOrCreateAsync(session, conn);
            Assert.That(prog.CityCredits, Is.EqualTo(300), "500 - 200 = 300");
            Assert.That(prog.UnlockedModules, Does.Contain("mod_kinetic_coil"));
        }

        // Attempting to purchase something that costs 1000 credits fails with InsufficientFunds
        string token2 = Guid.NewGuid().ToString();
        var receipt2 = await WorldTransactionService.PurchaseItemAsync(session, "module", "mod_expensive", 1000, token2, _connection);
        Assert.That(receipt2.Status, Is.EqualTo(TransactionStatus.InsufficientFunds));
    }
}
