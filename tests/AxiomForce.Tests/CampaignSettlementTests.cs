using Npgsql;

/// <summary>Exercises real PostgreSQL settlement using only explicitly owned temporary campaigns and receipts.</summary>
[NonParallelizable]
public class CampaignSettlementTests
{
    private int _session;
    private string _character = "", _connection = "";

    /// <summary>Creates an isolated campaign without reading or modifying existing player saves.</summary>
    [SetUp]
    public async Task CreateOwnedCampaign()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "project.godot"))) root = root.Parent;
        string previous = Directory.GetCurrentDirectory();
        try { Directory.SetCurrentDirectory(root!.FullName); _connection = PostgresStorage.ConnectionString; }
        finally { Directory.SetCurrentDirectory(previous); }
        _character = "repair-test:" + Guid.NewGuid();
        await using var connection = new NpgsqlConnection(_connection); await connection.OpenAsync();
        await using var schema = new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS game_sessions (
            id SERIAL PRIMARY KEY, character VARCHAR(100) NOT NULL, current_level INT NOT NULL DEFAULT 1,
            total_score INT NOT NULL DEFAULT 0, collectables INT NOT NULL DEFAULT 0,
            created_at VARCHAR(100) NOT NULL, last_played VARCHAR(100) NOT NULL)", connection);
        await schema.ExecuteNonQueryAsync();
        // The outage-only test must also be independently runnable against an otherwise fresh local database.
        await using var receipts = new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS game_state (
            state_key text PRIMARY KEY, schema_version integer NOT NULL DEFAULT 1, value jsonb NOT NULL)", connection);
        await receipts.ExecuteNonQueryAsync();
        await using var command = new NpgsqlCommand(@"INSERT INTO game_sessions(character,created_at,last_played)
            VALUES (@character,'fixture','fixture') RETURNING id", connection);
        command.Parameters.AddWithValue("character", _character);
        _session = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Removes only this fixture's exact campaign and its namespaced completion receipts.</summary>
    [TearDown]
    public async Task RemoveOwnedCampaign()
    {
        if (_session <= 0) return;
        await using var connection = new NpgsqlConnection(_connection); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(@"DELETE FROM game_state WHERE state_key LIKE @prefix;
            DELETE FROM game_sessions WHERE id=@id AND character=@character", connection);
        command.Parameters.AddWithValue("prefix", $"mission-completion:{_session}:%");
        command.Parameters.AddWithValue("id", _session); command.Parameters.AddWithValue("character", _character);
        await command.ExecuteNonQueryAsync(); _session = 0;
    }

    /// <summary>Earlier replays cannot lower persisted progress, and the finale remains resumable at Mission 12.</summary>
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

    /// <summary>Concurrent retries and a committed response discarded by the caller settle one score award.</summary>
    [Test]
    public async Task ConcurrentAndUncertainRetriesAwardExactlyOnce()
    {
        string token = Guid.NewGuid().ToString();
        await Save(6, 37, token); // Simulate a caller losing a committed response.
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Save(6, 37, token)));
        Assert.That(results.All(result => result.Status == CompletionSaveStatus.Saved), Is.True);
        Assert.That(results.Select(result => result.TotalScore), Is.All.EqualTo(37));
        Assert.That(results.Select(result => result.CurrentLevel), Is.All.EqualTo(7));
        Assert.That((await Save(6, 99, token)).Status, Is.EqualTo(CompletionSaveStatus.Failed));
    }

    /// <summary>Direct entry without an active campaign never creates persistence, and an outage reports failure.</summary>
    [Test]
    public async Task NoCampaignAndOutageHaveExplicitResults()
    {
        Assert.That((await CampaignSettlement.SaveAsync(-1, 6, 1, Guid.NewGuid().ToString())).Status,
            Is.EqualTo(CompletionSaveStatus.NoActiveCampaign));
        var unavailable = new NpgsqlConnectionStringBuilder(_connection) { Host = "127.0.0.1", Port = 1, Timeout = 1 };
        Assert.That((await CampaignSettlement.SaveAsync(_session, 6, 1, Guid.NewGuid().ToString(), unavailable.ConnectionString)).Status,
            Is.EqualTo(CompletionSaveStatus.Failed));
    }

    /// <summary>Uses the captured fixture connection so environment mutations in other tests cannot redirect settlement.</summary>
    private Task<CompletionSaveResult> Save(int mission, int points, string token) =>
        CampaignSettlement.SaveAsync(_session, mission, points, token, _connection);
}
