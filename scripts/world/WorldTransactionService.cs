using System;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

/// <summary>
/// Status codes returned by transactional world operations.
/// </summary>
public enum TransactionStatus
{
    Success,
    AlreadyCommitted,
    PrerequisitesNotMet,
    InsufficientFunds,
    InvalidOperation,
    DatabaseError
}

/// <summary>
/// Immutable receipt of a committed world transaction.
/// Prevents duplicate rewards or deductions upon retry.
/// </summary>
public sealed record TransactionReceipt(
    string ActionToken,
    int SessionId,
    string ActionType,
    string TargetId,
    TransactionStatus Status,
    long TimestampUnixSeconds,
    string? Details = null
);

/// <summary>
/// Atomic transaction service for Neo-Kyoto's Living Resistance World.
/// Commits quest advancements, economic transactions, reconstruction choices,
/// and progression changes atomically into PostgreSQL with idempotent action receipts.
/// </summary>
public static class WorldTransactionService
{
    /// <summary>
    /// Atomically completes an authored quest, granting one-time Hero Sparks, City Credits,
    /// unlocked modules/gadgets, and updating district reconstruction choices.
    /// Idempotent across retries using the action token.
    /// </summary>
    public static async Task<TransactionReceipt> CompleteQuestAsync(
        int session,
        string questId,
        string? branchChoiceId,
        string actionToken,
        string? connectionString = null)
    {
        if (session <= 0 || string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(actionToken))
        {
            return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.InvalidOperation, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "Invalid arguments");
        }

        var quest = Array.Find(WorldContentCatalog.Quests as QuestDefinition[] ?? System.Array.Empty<QuestDefinition>(), q => q.Id == questId);
        if (quest == null)
        {
            return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.InvalidOperation, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "Quest definition not found");
        }

        await using var connection = new NpgsqlConnection(connectionString ?? PostgresStorage.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            // 1. Lock the session
            await using (var lockCmd = new NpgsqlCommand("SELECT id FROM game_sessions WHERE id=@id FOR UPDATE", connection, transaction))
            {
                lockCmd.Parameters.AddWithValue("id", session);
                if (await lockCmd.ExecuteScalarAsync().ConfigureAwait(false) == null)
                {
                    return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.InvalidOperation, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "Campaign session not found");
                }
            }

            // 2. Check for existing receipt (idempotency)
            string receiptKey = $"receipt:{session}:{actionToken}";
            await using (var checkCmd = new NpgsqlCommand("SELECT value::text FROM game_state WHERE state_key=@key", connection, transaction))
            {
                checkCmd.Parameters.AddWithValue("key", receiptKey);
                if (await checkCmd.ExecuteScalarAsync().ConfigureAwait(false) is string existingJson)
                {
                    var existingReceipt = JsonSerializer.Deserialize<TransactionReceipt>(existingJson);
                    if (existingReceipt != null)
                    {
                        await transaction.CommitAsync().ConfigureAwait(false);
                        return existingReceipt with { Status = TransactionStatus.AlreadyCommitted };
                    }
                }
            }

            // 3. Load or create isolated campaign progression
            var progression = await CampaignProgressionState.LoadOrCreateAsync(session, connection, transaction).ConfigureAwait(false);

            // Grant rewards once
            progression.GlobalSparksBank += quest.Reward.HeroSparks;
            progression.CityCredits += quest.Reward.CityCredits;
            if (!string.IsNullOrEmpty(quest.Reward.BonusModuleId))
                progression.UnlockedModules.Add(quest.Reward.BonusModuleId);
            if (!string.IsNullOrEmpty(quest.Reward.BonusGadgetId))
                progression.UnlockedGadgets.Add(quest.Reward.BonusGadgetId);

            await CampaignProgressionState.SaveAsync(session, progression, connection, transaction).ConfigureAwait(false);

            // 4. Update the world save state with quest completion
            var worldState = await WorldStorage.LoadAsync(session, 1, connectionString ?? PostgresStorage.ConnectionString).ConfigureAwait(false)
                ?? WorldSave.AfterMission(1);

            // Extend world state dictionary with quest completion flag
            var updatedState = worldState.WithQuestCompleted(questId, branchChoiceId);
            await WorldStorage.WriteAsync(connection, transaction, session, updatedState).ConfigureAwait(false);

            // 5. Store the immutable transaction receipt
            var receipt = new TransactionReceipt(
                actionToken,
                session,
                "quest_completion",
                questId,
                TransactionStatus.Success,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                $"Earned {quest.Reward.HeroSparks} Sparks, {quest.Reward.CityCredits} Credits"
            );

            await using (var writeReceipt = new NpgsqlCommand(
                "INSERT INTO game_state (state_key, schema_version, value) VALUES (@key, 1, @val)",
                connection, transaction))
            {
                writeReceipt.Parameters.AddWithValue("key", receiptKey);
                writeReceipt.Parameters.AddWithValue("val", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(receipt));
                await writeReceipt.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            return receipt;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.DatabaseError, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ex.Message);
        }
    }

    /// <summary>
    /// Atomically purchases an item (module, gadget, vehicle) using City Credits.
    /// Idempotent across retries using the action token.
    /// </summary>
    public static async Task<TransactionReceipt> PurchaseItemAsync(
        int session,
        string itemType,
        string itemId,
        int costCredits,
        string actionToken,
        string? connectionString = null)
    {
        if (session <= 0 || costCredits < 0 || string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(actionToken))
        {
            return new TransactionReceipt(actionToken, session, "purchase", itemId, TransactionStatus.InvalidOperation, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "Invalid arguments");
        }

        await using var connection = new NpgsqlConnection(connectionString ?? PostgresStorage.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            await using (var lockCmd = new NpgsqlCommand("SELECT id FROM game_sessions WHERE id=@id FOR UPDATE", connection, transaction))
            {
                lockCmd.Parameters.AddWithValue("id", session);
                if (await lockCmd.ExecuteScalarAsync().ConfigureAwait(false) == null)
                {
                    return new TransactionReceipt(actionToken, session, "purchase", itemId, TransactionStatus.InvalidOperation, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "Campaign session not found");
                }
            }

            string receiptKey = $"receipt:{session}:{actionToken}";
            await using (var checkCmd = new NpgsqlCommand("SELECT value::text FROM game_state WHERE state_key=@key", connection, transaction))
            {
                checkCmd.Parameters.AddWithValue("key", receiptKey);
                if (await checkCmd.ExecuteScalarAsync().ConfigureAwait(false) is string existingJson)
                {
                    var existingReceipt = JsonSerializer.Deserialize<TransactionReceipt>(existingJson);
                    if (existingReceipt != null)
                    {
                        await transaction.CommitAsync().ConfigureAwait(false);
                        return existingReceipt with { Status = TransactionStatus.AlreadyCommitted };
                    }
                }
            }

            var progression = await CampaignProgressionState.LoadOrCreateAsync(session, connection, transaction).ConfigureAwait(false);
            if (progression.CityCredits < costCredits)
            {
                return new TransactionReceipt(actionToken, session, "purchase", itemId, TransactionStatus.InsufficientFunds, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), $"Required: {costCredits}, Available: {progression.CityCredits}");
            }

            progression.CityCredits -= costCredits;
            if (itemType == "module") progression.UnlockedModules.Add(itemId);
            else if (itemType == "gadget") progression.UnlockedGadgets.Add(itemId);

            await CampaignProgressionState.SaveAsync(session, progression, connection, transaction).ConfigureAwait(false);

            var receipt = new TransactionReceipt(
                actionToken,
                session,
                "purchase",
                itemId,
                TransactionStatus.Success,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                $"Purchased {itemId} for {costCredits} credits"
            );

            await using (var writeReceipt = new NpgsqlCommand(
                "INSERT INTO game_state (state_key, schema_version, value) VALUES (@key, 1, @val)",
                connection, transaction))
            {
                writeReceipt.Parameters.AddWithValue("key", receiptKey);
                writeReceipt.Parameters.AddWithValue("val", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(receipt));
                await writeReceipt.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            return receipt;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            return new TransactionReceipt(actionToken, session, "purchase", itemId, TransactionStatus.DatabaseError, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ex.Message);
        }
    }
}
