/// <summary>
/// The game-side save layer (2026-11-18): the in-memory store's commit rules match the server's
/// (backend/test/gameSaves.test.js), and <see cref="SlotTransaction"/> recomputes on a revision
/// conflict instead of losing a write.
/// </summary>
public class SaveStoreTests
{
    private InMemorySaveStore _store = null!;
    private int _slot;

    /// <summary>A fresh store and slot per test.</summary>
    [SetUp]
    public async Task Create()
    {
        AccountDocs.Clear();
        _store = new InMemorySaveStore();
        _slot = (await _store.CreateSlotAsync("store-test")).Id;
    }

    /// <summary>Receipts are checked before the revision, a mismatched receipt is refused, and a stale revision conflicts.</summary>
    [Test]
    public async Task CommitRulesMatchTheServer()
    {
        string token = Guid.NewGuid().ToString();
        var first = await _store.CommitAsync(_slot, new SlotCommit(0, token, "quest", "{\"q\":1}", ScoreAdd: 5));
        Assert.That(first.Outcome, Is.EqualTo(CommitOutcome.Committed));
        Assert.That(first.Snapshot!.Slot.Revision, Is.EqualTo(1));
        var retry = await _store.CommitAsync(_slot, new SlotCommit(0, token, "quest", "{ \"q\": 1 }", ScoreAdd: 5));
        Assert.That(retry.Outcome, Is.EqualTo(CommitOutcome.AlreadyCommitted), "same token and same receipt (spacing ignored) is a no-op");
        Assert.That(retry.Snapshot!.Slot.TotalScore, Is.EqualTo(5));
        Assert.That((await _store.CommitAsync(_slot, new SlotCommit(1, token, "quest", "{\"q\":2}"))).Outcome, Is.EqualTo(CommitOutcome.ReceiptMismatch));
        var stale = await _store.CommitAsync(_slot, new SlotCommit(0, ScoreAdd: 1));
        Assert.That(stale.Outcome, Is.EqualTo(CommitOutcome.RevisionConflict));
        Assert.That(stale.Snapshot!.Slot.Revision, Is.EqualTo(1), "a conflict returns the current slot");
        Assert.That((await _store.CommitAsync(_slot, new SlotCommit(1, LevelAtLeast: 4))).Snapshot!.Slot.CurrentLevel, Is.EqualTo(4));
        Assert.That((await _store.CommitAsync(_slot, new SlotCommit(2, LevelAtLeast: 2))).Snapshot!.Slot.CurrentLevel, Is.EqualTo(4), "levels never go down");
        Assert.That((await _store.CommitAsync(9999, new SlotCommit(0))).Outcome, Is.EqualTo(CommitOutcome.NotFound));
    }

    /// <summary>Two different saves racing on one slot both land: the loser reloads and recomputes.</summary>
    [Test]
    public async Task ConflictingWritesAreRecomputedNotLost()
    {
        var la = WorldSave.AfterMission(2);
        await WorldStorage.SaveAsync(_slot, la, _store);
        var results = await Task.WhenAll(
            SlotTransaction.RunAsync(_store, _slot, s => new SlotCommit(s.Slot.Revision, ScoreAdd: 10)),
            SlotTransaction.RunAsync(_store, _slot, s => new SlotCommit(s.Slot.Revision, ScoreAdd: 20)),
            SlotTransaction.RunAsync(_store, _slot, s => new SlotCommit(s.Slot.Revision, CollectablesAdd: 1)));
        Assert.That(results.All(r => r.Outcome == CommitOutcome.Committed), Is.True);
        var slot = (await _store.GetSlotAsync(_slot))!.Slot;
        Assert.That(slot.TotalScore, Is.EqualTo(30));
        Assert.That(slot.Collectables, Is.EqualTo(1));
        Assert.That(await WorldStorage.LoadAsync(_slot, 3, _store), Is.EqualTo(la), "unrelated documents survive");
    }

    /// <summary>A plan that decides not to write returns without committing anything.</summary>
    [Test]
    public async Task APlanCanStopWithoutWriting()
    {
        var result = await SlotTransaction.RunAsync(_store, _slot, _ => null);
        Assert.That(result.Outcome, Is.EqualTo(CommitOutcome.Committed));
        Assert.That((await _store.GetSlotAsync(_slot))!.Slot.Revision, Is.EqualTo(0));
    }

    /// <summary>A new campaign's progression starts from the account's operative progression, then stays per slot.</summary>
    [Test]
    public async Task NewCampaignProgressionStartsFromTheAccount()
    {
        AccountDocs.ReplaceAll(new Dictionary<string, string> { ["operative_progression"] = "{\"GlobalSparksBank\":900,\"Operatives\":{}}" });
        var fresh = CampaignProgressionState.FromSnapshot((await _store.GetSlotAsync(_slot))!);
        Assert.That(fresh.GlobalSparksBank, Is.EqualTo(900));
        await _store.CommitAsync(_slot, new SlotCommit(0, Docs: new[] { new CampaignProgressionState { GlobalSparksBank = 10 }.ToDoc() }));
        Assert.That(CampaignProgressionState.FromSnapshot((await _store.GetSlotAsync(_slot))!).GlobalSparksBank, Is.EqualTo(10));
        AccountDocs.Clear();
    }

    /// <summary>An outage fails reads with a player-safe exception and commits with a Failed outcome.</summary>
    [Test]
    public async Task OutagesAreReportedNotHidden()
    {
        _store.Offline = true;
        Assert.ThrowsAsync<SaveStoreException>(async () => await _store.ListSlotsAsync());
        Assert.That((await SlotTransaction.RunAsync(_store, _slot, s => new SlotCommit(s.Slot.Revision))).Outcome, Is.EqualTo(CommitOutcome.Failed));
    }
}
