using System;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

/// <summary>Explicit extraction persistence result; direct scene play can finish without creating a campaign.</summary>
public enum CompletionSaveStatus { Saved, Failed, NoActiveCampaign }

/// <summary>Engine-free result safe to return from database worker continuations without touching Godot nodes.</summary>
public sealed record CompletionSaveResult(CompletionSaveStatus Status, int CurrentLevel = 0, int TotalScore = 0,
    string Error = "");

/// <summary>PostgreSQL extraction settlement. A stable per-run receipt and the score/progress update commit in one
/// transaction; retrying a lost response or concurrently settling the same token cannot award twice.</summary>
public static class CampaignSettlement
{
    /// <summary>Claims one run in the existing game_state schema. Locks only the selected campaign and never lowers progress.
    /// Optional connection override is for owned integration fixtures and outage tests, never a fallback store.</summary>
    public static async Task<CompletionSaveResult> SaveAsync(int sessionId, int mission, int score, string runToken,
        string? connectionString = null)
    {
        if (sessionId <= 0) return new(CompletionSaveStatus.NoActiveCampaign);
        if (mission < 1 || mission > LevelCatalog.Count || score < 0 || !Guid.TryParse(runToken, out _))
            return new(CompletionSaveStatus.Failed, Error: "Invalid mission completion receipt.");
        try
        {
            await using var connection = new NpgsqlConnection(connectionString ?? PostgresStorage.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
            // Row locking serializes different runs as well as concurrent retries of the same run.
            await using var selected = new NpgsqlCommand(
                "SELECT current_level, total_score FROM game_sessions WHERE id=@id FOR UPDATE", connection, transaction);
            selected.Parameters.AddWithValue("id", sessionId);
            int level, total;
            await using (var reader = await selected.ExecuteReaderAsync().ConfigureAwait(false))
            {
                if (!await reader.ReadAsync().ConfigureAwait(false))
                    return new(CompletionSaveStatus.Failed, Error: "The selected campaign no longer exists.");
                level = reader.GetInt32(0); total = reader.GetInt32(1);
            }
            await using var schema = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS game_state (state_key text PRIMARY KEY, schema_version integer NOT NULL DEFAULT 1, value jsonb NOT NULL)", connection, transaction);
            await schema.ExecuteNonQueryAsync().ConfigureAwait(false);
            string key = $"mission-completion:{sessionId}:{runToken}";
            await using var receipt = new NpgsqlCommand(
                "INSERT INTO game_state(state_key,value) VALUES (@key,@value) ON CONFLICT(state_key) DO NOTHING", connection, transaction);
            receipt.Parameters.AddWithValue("key", key);
            receipt.Parameters.AddWithValue("value", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(new { mission, score }));
            if (await receipt.ExecuteNonQueryAsync().ConfigureAwait(false) == 1)
            {
                await using var update = new NpgsqlCommand(@"UPDATE game_sessions
                    SET current_level=GREATEST(current_level,@level), total_score=total_score+@score, last_played=@last
                    WHERE id=@id RETURNING current_level,total_score", connection, transaction);
                update.Parameters.AddWithValue("id", sessionId);
                update.Parameters.AddWithValue("level", LevelCatalog.Clamp(mission + 1));
                update.Parameters.AddWithValue("score", score);
                update.Parameters.AddWithValue("last", DateTime.UtcNow.ToString("O"));
                await using var reader = await update.ExecuteReaderAsync().ConfigureAwait(false);
                await reader.ReadAsync().ConfigureAwait(false);
                level = reader.GetInt32(0); total = reader.GetInt32(1);
            }
            else
            {
                // Reusing a token for a different snapshot is an error rather than a second award or silent success.
                await using var verify = new NpgsqlCommand(
                    "SELECT schema_version=1 AND value=@value FROM game_state WHERE state_key=@key", connection, transaction);
                verify.Parameters.AddWithValue("key", key);
                verify.Parameters.AddWithValue("value", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(new { mission, score }));
                if (await verify.ExecuteScalarAsync().ConfigureAwait(false) is not true)
                    return new(CompletionSaveStatus.Failed, Error: "Completion receipt does not match this run.");
            }
            await transaction.CommitAsync().ConfigureAwait(false);
            return new(CompletionSaveStatus.Saved, level, total);
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            // No credentials, SQL payload or unrelated campaign details reach the results screen.
            return new(CompletionSaveStatus.Failed, Error: "Campaign save could not be confirmed. Retry Save uses the same receipt.");
        }
    }
}
