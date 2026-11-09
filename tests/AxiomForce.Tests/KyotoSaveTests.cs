using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Npgsql;

/// <summary>
/// Real PostgreSQL round trips for city travel. Each test owns a throwaway campaign, and cleanup
/// matches that exact campaign number so it can never touch another campaign's rows.
/// </summary>
[TestFixture]
public class KyotoSaveTests
{
    private readonly List<int> _owned = new();
    private string _connection = "";

    /// <summary>Reads the same connection settings the game uses, which need the project root as working directory.</summary>
    [SetUp]
    public void Configure()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "project.godot"))) root = root.Parent;
        string previous = Directory.GetCurrentDirectory();
        try { Directory.SetCurrentDirectory(root!.FullName); _connection = PostgresStorage.ConnectionString; }
        finally { Directory.SetCurrentDirectory(previous); }
    }

    /// <summary>Creates a fixture campaign that starts in Los Angeles.</summary>
    private async Task<int> CreateCampaignAsync()
    {
        await using var c = new NpgsqlConnection(_connection);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO game_sessions(character,created_at,last_played) VALUES ('kyoto-save-fixture','fixture','fixture') RETURNING id", c);
        int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _owned.Add(id);
        await WorldStorage.SaveAsync(id, WorldSave.AfterMission(2), _connection);
        return id;
    }

    /// <summary>Deletes only keys whose campaign number is exactly this fixture's.</summary>
    [TearDown]
    public async Task CleanupAsync()
    {
        await using var c = new NpgsqlConnection(_connection);
        await c.OpenAsync();
        foreach (int id in _owned)
        {
            await using var cmd = new NpgsqlCommand("DELETE FROM game_state WHERE state_key ~ @pattern; DELETE FROM game_sessions WHERE id=@id AND created_at='fixture'", c);
            cmd.Parameters.AddWithValue("pattern", $"^[a-z_]+:{id}(:|$)");
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        _owned.Clear();
    }

    /// <summary>Reads the shared reward totals for a campaign.</summary>
    private async Task<int> SparksAsync(int session)
    {
        await using var c = new NpgsqlConnection(_connection);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        return (await CampaignProgressionState.LoadOrCreateAsync(session, c, tx)).GlobalSparksBank;
    }

    [Test]
    public async Task TravelStoresEachCitySeparatelyAndResumesInTheRightOne()
    {
        int session = await CreateCampaignAsync();
        var home = (await WorldStorage.LoadAsync(session, 3, _connection))!;

        var kyoto = await WorldStorage.TravelAsync(session, home, KyotoCity.Definition, _connection);
        Assert.That(kyoto.World, Is.EqualTo(KyotoCity.Definition.Identity));
        Assert.That((await WorldStorage.LoadAsync(session, 3, _connection))!.CurrentCity, Is.EqualTo("kyoto"));
        Assert.That((await WorldStorage.LoadActiveAsync(session, 3, _connection))!.World, Is.EqualTo(KyotoCity.Definition.Identity));

        var receipt = await WorldTransactionService.CompleteQuestAsync(session, "kq_gion_01", null, Guid.NewGuid().ToString(), _connection);
        Assert.That(receipt.Status, Is.EqualTo(TransactionStatus.Success));
        var active = (await WorldStorage.LoadActiveAsync(session, 3, _connection))!;
        Assert.That(active.CompletedQuests!.ContainsKey("kq_gion_01"), Is.True);
        Assert.That((await WorldStorage.LoadAsync(session, 3, _connection))!.CompletedQuests!.ContainsKey("kq_gion_01"), Is.False);

        var back = await WorldStorage.TravelAsync(session, active, WorldCatalog.Definition, _connection);
        Assert.That(back.World, Is.EqualTo(WorldCatalog.Identity));
        var resumed = (await WorldStorage.LoadActiveAsync(session, 3, _connection))!;
        Assert.That(resumed.World, Is.EqualTo(WorldCatalog.Identity));
        Assert.That(resumed.CurrentCity, Is.Null);

        var again = await WorldStorage.TravelAsync(session, resumed, KyotoCity.Definition, _connection);
        Assert.That(again.CompletedQuests!.ContainsKey("kq_gion_01"), Is.True, "Kyoto progress must survive leaving and returning.");
    }

    [Test]
    public async Task FinishingACampaignMissionFromACityReturnsToLosAngeles()
    {
        int session = await CreateCampaignAsync();
        await WorldStorage.TravelAsync(session, (await WorldStorage.LoadAsync(session, 3, _connection))!, KyotoCity.Definition, _connection);
        await using (var c = new NpgsqlConnection(_connection))
        {
            await c.OpenAsync();
            await using var tx = await c.BeginTransactionAsync();
            await WorldStorage.SettleReturnAsync(c, tx, session, 3, OperativeRoster.Starter);
            await tx.CommitAsync();
        }
        var state = (await WorldStorage.LoadActiveAsync(session, 4, _connection))!;
        Assert.That(state.World, Is.EqualTo(WorldCatalog.Identity));
        Assert.That(state.CurrentCity, Is.Null);
    }

    [Test]
    public async Task OperationRewardIsPaidOncePerTokenAndLeavesCampaignLevelAlone()
    {
        int session = await CreateCampaignAsync();
        await WorldStorage.TravelAsync(session, (await WorldStorage.LoadAsync(session, 3, _connection))!, KyotoCity.Definition, _connection);
        int before = await SparksAsync(session);
        string token = Guid.NewGuid().ToString();

        var first = await WorldTransactionService.CompleteOperationAsync(session, "op_kyoto_kamo_locks", token, connectionString: _connection);
        var retry = await WorldTransactionService.CompleteOperationAsync(session, "op_kyoto_kamo_locks", token, connectionString: _connection);
        Assert.That(first.Status, Is.EqualTo(TransactionStatus.Success));
        Assert.That(retry.Status, Is.EqualTo(TransactionStatus.AlreadyCommitted));
        Assert.That(await SparksAsync(session), Is.EqualTo(before + 250));

        var state = (await WorldStorage.LoadActiveAsync(session, 3, _connection))!;
        Assert.That(state.OperationClears!["op_kyoto_kamo_locks"], Is.EqualTo(1));

        await using var c = new NpgsqlConnection(_connection);
        await c.OpenAsync();
        await using var level = new NpgsqlCommand("SELECT current_level FROM game_sessions WHERE id=@id", c);
        level.Parameters.AddWithValue("id", session);
        Assert.That(Convert.ToInt32(await level.ExecuteScalarAsync()), Is.EqualTo(1), "A city operation must not advance the campaign.");
    }

    [Test]
    public async Task OperationCannotBeCreditedToACityNeverVisited()
    {
        int session = await CreateCampaignAsync();
        var receipt = await WorldTransactionService.CompleteOperationAsync(session, "op_kyoto_kamo_locks", Guid.NewGuid().ToString(), connectionString: _connection);
        Assert.That(receipt.Status, Is.EqualTo(TransactionStatus.PrerequisitesNotMet));
    }
}
