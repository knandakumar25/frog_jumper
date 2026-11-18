using System;
using System.Threading.Tasks;
using NUnit.Framework;

/// <summary>
/// City travel on an account save slot (2026-11-18), against <see cref="InMemorySaveStore"/>
/// (same commit rules as the server). Each test owns a fresh store and slot.
/// </summary>
[TestFixture]
public class KyotoSaveTests
{
    private InMemorySaveStore _store = null!;

    /// <summary>A fresh store per test.</summary>
    [SetUp]
    public void Configure() { AccountDocs.Clear(); _store = new InMemorySaveStore(); }

    /// <summary>Creates a slot that starts in Los Angeles after Mission 2.</summary>
    private async Task<int> CreateCampaignAsync()
    {
        int id = (await _store.CreateSlotAsync("kyoto-save-fixture")).Id;
        await WorldStorage.SaveAsync(id, WorldSave.AfterMission(2), _store);
        return id;
    }

    /// <summary>The slot's shared reward totals.</summary>
    private async Task<int> SparksAsync(int session) =>
        CampaignProgressionState.FromSnapshot((await _store.GetSlotAsync(session))!).GlobalSparksBank;

    [Test]
    public async Task TravelStoresEachCitySeparatelyAndResumesInTheRightOne()
    {
        int session = await CreateCampaignAsync();
        var home = (await WorldStorage.LoadAsync(session, 3, _store))!;

        var kyoto = await WorldStorage.TravelAsync(session, home, KyotoCity.Definition, _store);
        Assert.That(kyoto.World, Is.EqualTo(KyotoCity.Definition.Identity));
        Assert.That((await WorldStorage.LoadAsync(session, 3, _store))!.CurrentCity, Is.EqualTo("kyoto"));
        Assert.That((await WorldStorage.LoadActiveAsync(session, 3, _store))!.World, Is.EqualTo(KyotoCity.Definition.Identity));
        Assert.That((await _store.GetSlotAsync(session))!.Docs.ContainsKey(WorldStorage.DocKey(KyotoCity.Definition)), Is.True);

        var receipt = await WorldTransactionService.CompleteQuestAsync(session, "kq_gion_01", null, Guid.NewGuid().ToString(), _store);
        Assert.That(receipt.Status, Is.EqualTo(TransactionStatus.Success));
        var active = (await WorldStorage.LoadActiveAsync(session, 3, _store))!;
        Assert.That(active.CompletedQuests!.ContainsKey("kq_gion_01"), Is.True);
        Assert.That((await WorldStorage.LoadAsync(session, 3, _store))!.CompletedQuests!.ContainsKey("kq_gion_01"), Is.False);

        var back = await WorldStorage.TravelAsync(session, active, WorldCatalog.Definition, _store);
        Assert.That(back.World, Is.EqualTo(WorldCatalog.Identity));
        var resumed = (await WorldStorage.LoadActiveAsync(session, 3, _store))!;
        Assert.That(resumed.World, Is.EqualTo(WorldCatalog.Identity));
        Assert.That(resumed.CurrentCity, Is.Null);

        var again = await WorldStorage.TravelAsync(session, resumed, KyotoCity.Definition, _store);
        Assert.That(again.CompletedQuests!.ContainsKey("kq_gion_01"), Is.True, "Kyoto progress must survive leaving and returning.");
    }

    [Test]
    public async Task FinishingACampaignMissionFromACityReturnsToLosAngeles()
    {
        int session = await CreateCampaignAsync();
        await WorldStorage.TravelAsync(session, (await WorldStorage.LoadAsync(session, 3, _store))!, KyotoCity.Definition, _store);
        var settled = await CampaignSettlement.SaveAsync(session, 3, 0, Guid.NewGuid().ToString(), _store);
        Assert.That(settled.Status, Is.EqualTo(CompletionSaveStatus.Saved));
        var state = (await WorldStorage.LoadActiveAsync(session, 4, _store))!;
        Assert.That(state.World, Is.EqualTo(WorldCatalog.Identity));
        Assert.That(state.CurrentCity, Is.Null);
    }

    [Test]
    public async Task OperationRewardIsPaidOncePerTokenAndLeavesCampaignLevelAlone()
    {
        int session = await CreateCampaignAsync();
        await WorldStorage.TravelAsync(session, (await WorldStorage.LoadAsync(session, 3, _store))!, KyotoCity.Definition, _store);
        int before = await SparksAsync(session);
        string token = Guid.NewGuid().ToString();

        var first = await WorldTransactionService.CompleteOperationAsync(session, "op_kyoto_kamo_locks", token, store: _store);
        var retry = await WorldTransactionService.CompleteOperationAsync(session, "op_kyoto_kamo_locks", token, store: _store);
        Assert.That(first.Status, Is.EqualTo(TransactionStatus.Success));
        Assert.That(retry.Status, Is.EqualTo(TransactionStatus.AlreadyCommitted));
        Assert.That(await SparksAsync(session), Is.EqualTo(before + 250));

        var state = (await WorldStorage.LoadActiveAsync(session, 3, _store))!;
        Assert.That(state.OperationClears!["op_kyoto_kamo_locks"], Is.EqualTo(1));
        Assert.That((await _store.GetSlotAsync(session))!.Slot.CurrentLevel, Is.EqualTo(1), "A city operation must not advance the campaign.");
    }

    [Test]
    public async Task OperationCannotBeCreditedToACityNeverVisited()
    {
        int session = await CreateCampaignAsync();
        var receipt = await WorldTransactionService.CompleteOperationAsync(session, "op_kyoto_kamo_locks", Guid.NewGuid().ToString(), store: _store);
        Assert.That(receipt.Status, Is.EqualTo(TransactionStatus.PrerequisitesNotMet));
    }
}
