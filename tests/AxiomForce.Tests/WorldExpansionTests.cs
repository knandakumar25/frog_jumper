using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
        Assert.That(WorldContentCatalog.Residents.Count, Is.GreaterThanOrEqualTo(6));

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
        Assert.That(WorldContentCatalog.Quests.Count, Is.GreaterThanOrEqualTo(8));

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
        Assert.That(WorldContentCatalog.Challenges.Count, Is.GreaterThanOrEqualTo(6));
        Assert.That(WorldContentCatalog.Challenges.Count(c => c.RegionId == 5), Is.EqualTo(3));
        Assert.That(WorldContentCatalog.Challenges.Count(c => c.RegionId == 9), Is.EqualTo(3));

        Assert.That(WorldContentCatalog.SideOperations.Count, Is.GreaterThanOrEqualTo(2));
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
/// World actions (quests, purchases, settlement) on an account save slot (2026-11-18), against
/// <see cref="InMemorySaveStore"/>: all-or-nothing commits and action-token receipts.
/// </summary>
public class WorldTransactionTests
{
    private InMemorySaveStore _store = null!;

    /// <summary>A fresh store per test.</summary>
    [SetUp]
    public void Configure() { AccountDocs.Clear(); _store = new InMemorySaveStore(); }

    private async Task<int> CreateCampaignAsync() => (await _store.CreateSlotAsync("world-tx-fixture")).Id;

    /// <summary>The slot's progression as stored.</summary>
    private async Task<CampaignProgressionState> ProgressionAsync(int session) =>
        CampaignProgressionState.FromSnapshot((await _store.GetSlotAsync(session))!);

    /// <summary>A damaged world record is refused, not replaced by defaults that would erase the campaign.</summary>
    [Test]
    public async Task QuestCompletionRefusesToOverwriteDamagedWorldSave()
    {
        int session = await CreateCampaignAsync();
        var damaged = WorldSave.AfterMission(1) with { Position = new WorldPoint(-5000, -5000) };
        await _store.CommitAsync(session, new SlotCommit(0, Docs: new[] { new SlotDocWrite("world", JsonSerializer.Serialize(damaged)) }));
        var receipt = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, Guid.NewGuid().ToString(), _store);
        Assert.That(receipt.Status, Is.EqualTo(TransactionStatus.DatabaseError));
        var stored = JsonSerializer.Deserialize<WorldSave>((await _store.GetSlotAsync(session))!.Json("world")!);
        Assert.That(stored!.Position, Is.EqualTo(damaged.Position));
    }

    /// <summary>A second completion with a new token, as after a restart, must not pay twice.</summary>
    [Test]
    public async Task QuestRewardsCannotBeEarnedTwiceWithNewTokens()
    {
        int session = await CreateCampaignAsync();
        var first = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, Guid.NewGuid().ToString(), _store);
        var second = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, Guid.NewGuid().ToString(), _store);
        Assert.That(first.Status, Is.EqualTo(TransactionStatus.Success));
        Assert.That(second.Status, Is.EqualTo(TransactionStatus.AlreadyCommitted));
        Assert.That((await ProgressionAsync(session)).CityCredits,
            Is.EqualTo(WorldContentCatalog.Quests.Single(q => q.Id == "quest_ok_01").Reward.CityCredits + new CampaignProgressionState().CityCredits));
    }

    /// <summary>The service enforces the quest chain and rejects branches the quest doesn't offer.</summary>
    [Test]
    public async Task QuestCompletionEnforcesPrerequisitesAndBranches()
    {
        int session = await CreateCampaignAsync();
        var blocked = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_02", null, Guid.NewGuid().ToString(), _store);
        var badBranch = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", "not_a_choice", Guid.NewGuid().ToString(), _store);
        Assert.That(blocked.Status, Is.EqualTo(TransactionStatus.PrerequisitesNotMet));
        Assert.That(badBranch.Status, Is.EqualTo(TransactionStatus.InvalidOperation));
    }

    /// <summary>Finishing a mission moves the player without erasing quests, choices or the clock.</summary>
    [Test]
    public async Task MissionSettlementKeepsQuestProgress()
    {
        int session = await CreateCampaignAsync();
        var before = WorldSave.AfterMission(1) with { ActiveQuests = new() { ["quest_ok_02"] = 1 }, DistrictChoices = new() { [5] = "choice_artisan_guild" }, WorldTimeMinutes = 12 };
        await WorldStorage.SaveAsync(session, before.WithQuestCompleted("quest_ok_01", null), _store);
        await CampaignSettlement.SaveAsync(session, 2, 10, Guid.NewGuid().ToString(), _store, "Cyber Shogun");
        var after = await WorldStorage.LoadAsync(session, 3, _store);
        Assert.That(after!.CompletedQuests, Does.ContainKey("quest_ok_01"));
        Assert.That(after.ActiveQuests!["quest_ok_02"], Is.EqualTo(1));
        Assert.That(after.DistrictChoices![5], Is.EqualTo("choice_artisan_guild"));
        Assert.That(after.WorldTimeMinutes, Is.EqualTo(12));
        Assert.That(after.Position, Is.EqualTo(WorldSave.AfterMission(2).Position));
    }

    /// <summary>Quest completion commits rewards and receipt together; retrying the same token adds nothing.</summary>
    [Test]
    public async Task QuestCompletionIsAtomicAndIdempotent()
    {
        int session = await CreateCampaignAsync();
        string token = Guid.NewGuid().ToString();
        var receipt1 = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, token, _store);
        Assert.That(receipt1.Status, Is.EqualTo(TransactionStatus.Success));
        var prog = await ProgressionAsync(session);
        Assert.That(prog.GlobalSparksBank, Is.EqualTo(350), "Starter 250 + 100 reward = 350");
        Assert.That(prog.CityCredits, Is.EqualTo(650), "Starter 500 + 150 reward = 650");

        var receipt2 = await WorldTransactionService.CompleteQuestAsync(session, "quest_ok_01", null, token, _store);
        Assert.That(receipt2.Status, Is.EqualTo(TransactionStatus.AlreadyCommitted));
        prog = await ProgressionAsync(session);
        Assert.That(prog.GlobalSparksBank, Is.EqualTo(350), "Sparks must not duplicate on retry.");
        Assert.That(prog.CityCredits, Is.EqualTo(650), "Credits must not duplicate on retry.");
    }

    /// <summary>Purchases charge the catalog price (never a caller's number), unlock the item and enforce the balance.</summary>
    [Test]
    public async Task PurchaseItemDeductsCreditsAndEnforcesBalance()
    {
        int session = await CreateCampaignAsync();
        Assert.That(WorldTransactionService.ItemPrice("module", "mod_kinetic_coil"), Is.EqualTo(200));
        string token1 = Guid.NewGuid().ToString();
        var receipt1 = await WorldTransactionService.PurchaseItemAsync(session, "module", "mod_kinetic_coil", token1, _store);
        Assert.That(receipt1.Status, Is.EqualTo(TransactionStatus.Success));
        var prog = await ProgressionAsync(session);
        Assert.That(prog.CityCredits, Is.EqualTo(300), "500 - 200 = 300");
        Assert.That(prog.UnlockedModules, Does.Contain("mod_kinetic_coil"));
        Assert.That((await WorldTransactionService.PurchaseItemAsync(session, "module", "mod_kinetic_coil", token1, _store)).Status,
            Is.EqualTo(TransactionStatus.AlreadyCommitted), "a retried purchase is not charged twice");

        // 300 credits left: the 150 smoke screen is affordable, leaving 150, so a 300 module is not.
        Assert.That((await WorldTransactionService.PurchaseItemAsync(session, "gadget", "gadget_smoke_screen", Guid.NewGuid().ToString(), _store)).Status, Is.EqualTo(TransactionStatus.Success));
        var poor = await WorldTransactionService.PurchaseItemAsync(session, "module", "mod_civic_nanobound", Guid.NewGuid().ToString(), _store);
        Assert.That(poor.Status, Is.EqualTo(TransactionStatus.InsufficientFunds));
        Assert.That((await WorldTransactionService.PurchaseItemAsync(session, "module", "mod_not_in_catalog", Guid.NewGuid().ToString(), _store)).Status,
            Is.EqualTo(TransactionStatus.InvalidOperation), "unknown items have no price and cannot be bought");
    }
}
