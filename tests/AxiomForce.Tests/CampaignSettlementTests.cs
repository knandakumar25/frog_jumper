/// <summary>
/// Mission settlement on an account save slot (2026-11-18). Runs against <see cref="InMemorySaveStore"/>,
/// which applies the same commit rules as the server (revision, receipts, schema guard); the server
/// side of those rules is tested in backend/test/gameSaves.test.js.
/// </summary>
public class CampaignSettlementTests
{
    private InMemorySaveStore _store = null!;
    private int _session;

    /// <summary>A fresh store and slot per test; no shared state and no database.</summary>
    [SetUp]
    public async Task CreateSlot()
    {
        AccountDocs.Clear();
        _store = new InMemorySaveStore();
        _session = (await _store.CreateSlotAsync("settlement-test")).Id;
    }

    /// <summary>Earlier replays cannot lower saved progress, and the finale remains resumable at Mission 12.</summary>
    [Test]
    public async Task ReplayPreservesHighestMissionAndFinaleCapsAtTwelve()
    {
        var later = await Save(11, 20, Guid.NewGuid().ToString());
        var replay = await Save(2, 5, Guid.NewGuid().ToString());
        var finale = await Save(12, 30, Guid.NewGuid().ToString());
        Assert.That(later.Status, Is.EqualTo(CompletionSaveStatus.Saved));
        Assert.That(replay.CurrentLevel, Is.EqualTo(12));
        Assert.That(finale.CurrentLevel, Is.EqualTo(12));
        Assert.That(finale.TotalScore, Is.EqualTo(55));
    }

    /// <summary>Concurrent retries and a committed answer the caller lost settle one score award.</summary>
    [Test]
    public async Task ConcurrentAndUncertainRetriesAwardExactlyOnce()
    {
        string token = Guid.NewGuid().ToString();
        await Save(6, 37, token); // Simulate a caller losing a committed answer.
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Save(6, 37, token)));
        Assert.That(results.All(result => result.Status == CompletionSaveStatus.Saved), Is.True);
        Assert.That(results.Select(result => result.TotalScore), Is.All.EqualTo(37));
        Assert.That(results.Select(result => result.CurrentLevel), Is.All.EqualTo(7));
        Assert.That((await Save(6, 99, token)).Status, Is.EqualTo(CompletionSaveStatus.Failed), "a token reused for a different result is refused");
    }

    /// <summary>Different runs settling at the same time all count (revision conflicts are recomputed, not lost).</summary>
    [Test]
    public async Task ConcurrentDifferentRunsAllCount()
    {
        var results = await Task.WhenAll(Enumerable.Range(1, 3).Select(i => Save(i, 10, Guid.NewGuid().ToString())));
        Assert.That(results.All(r => r.Status == CompletionSaveStatus.Saved), Is.True);
        Assert.That((await _store.GetSlotAsync(_session))!.Slot.TotalScore, Is.EqualTo(30));
    }

    /// <summary>Direct entry without an active campaign saves nothing, and an outage reports failure.</summary>
    [Test]
    public async Task NoCampaignAndOutageHaveExplicitResults()
    {
        Assert.That((await CampaignSettlement.SaveAsync(-1, 6, 1, Guid.NewGuid().ToString(), _store)).Status,
            Is.EqualTo(CompletionSaveStatus.NoActiveCampaign));
        _store.Offline = true;
        Assert.That((await Save(6, 1, Guid.NewGuid().ToString())).Status, Is.EqualTo(CompletionSaveStatus.Failed));
        _store.Offline = false;
        Assert.That((await CampaignSettlement.SaveAsync(9999, 6, 1, Guid.NewGuid().ToString(), _store)).Status,
            Is.EqualTo(CompletionSaveStatus.Failed), "an unknown slot is a failure, not a new save");
    }

    /// <summary>Settles one run on this test's slot.</summary>
    private Task<CompletionSaveResult> Save(int mission, int points, string token) =>
        CampaignSettlement.SaveAsync(_session, mission, points, token, _store);
}
