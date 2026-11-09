using System;
using System.Collections.Generic;
using System.Linq;
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

        var quest = CityRegistry.FindQuest(questId);
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

            // 3. Reject repeats and invalid requests before any reward. Reading on this transaction
            // sees the row the session lock protects, so a new token can't earn a quest twice.
            // A city's quest is checked and recorded in that city's own save row.
            var city = CityRegistry.CityOfQuest(questId);
            var worldState = await WorldStorage.ReadAsync(connection, transaction, session, city).ConfigureAwait(false)
                ?? (city == WorldCatalog.Definition ? WorldSave.AfterMission(1) : WorldSave.ArrivalIn(city, 1, OperativeRoster.Starter));
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (worldState.CompletedQuests?.ContainsKey(questId) == true)
            {
                await transaction.CommitAsync().ConfigureAwait(false);
                return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.AlreadyCommitted, now, "Quest already completed");
            }
            if (quest.PrerequisiteQuestId != null && worldState.CompletedQuests?.ContainsKey(quest.PrerequisiteQuestId) != true)
            {
                return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.PrerequisitesNotMet, now, $"Requires {quest.PrerequisiteQuestId}");
            }
            if (branchChoiceId != null && quest.BranchChoices?.Any(c => c.ChoiceId == branchChoiceId) != true)
            {
                return new TransactionReceipt(actionToken, session, "quest_completion", questId, TransactionStatus.InvalidOperation, now, "Unknown branch choice");
            }

            // 4. Load or create isolated campaign progression
            var progression = await CampaignProgressionState.LoadOrCreateAsync(session, connection, transaction).ConfigureAwait(false);

            // Grant rewards once
            progression.GlobalSparksBank += quest.Reward.HeroSparks;
            progression.CityCredits += quest.Reward.CityCredits;
            if (!string.IsNullOrEmpty(quest.Reward.FactionId) && quest.Reward.FactionReputation > 0)
                progression.AddFactionReputation(quest.Reward.FactionId, quest.Reward.FactionReputation);
            if (!string.IsNullOrEmpty(quest.Reward.BonusModuleId))
                progression.UnlockedModules.Add(quest.Reward.BonusModuleId);
            if (!string.IsNullOrEmpty(quest.Reward.BonusGadgetId))
                progression.UnlockedGadgets.Add(quest.Reward.BonusGadgetId);

            await CampaignProgressionState.SaveAsync(session, progression, connection, transaction).ConfigureAwait(false);

            // 5. Extend the world state with the completion flag
            var updatedState = worldState.WithQuestCompleted(questId, branchChoiceId);
            await WorldStorage.WriteAsync(connection, transaction, session, updatedState).ConfigureAwait(false);

            // 6. Store the immutable transaction receipt
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

    /// <summary>Paid repeat clears allowed per operation, so replaying can't become a credit farm.</summary>
    public const int MaxPaidRepeatClears = 5;

    /// <summary>Reward for one clear, given how many clears came before: Sparks once, then a limited run of credits.</summary>
    public static (int Sparks, int Credits) OperationReward(SideOperationDefinition operation, int priorClears) =>
        priorClears <= 0 ? (operation.FirstClearSparks, 0) : priorClears <= MaxPaidRepeatClears ? (0, operation.RepeatCredits) : (0, 0);

    /// <summary>
    /// Records a finished city operation and grants its reward once. Idempotent across retries using
    /// the action token. Touches only the city record and shared rewards, never campaign mission progress.
    /// </summary>
    /// <param name="operative">Operative active at the finish, kept so a swap during the operation isn't undone on return. Null leaves it unchanged.</param>
    public static async Task<TransactionReceipt> CompleteOperationAsync(int session, string operationId, string actionToken, string? operative = null, string? connectionString = null)
    {
        long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        TransactionReceipt Result(TransactionStatus status, string details) => new(actionToken, session, "operation_clear", operationId, status, Now(), details);
        var found = CityRegistry.FindOperation(operationId);
        if (session <= 0 || found == null || string.IsNullOrWhiteSpace(actionToken)) return Result(TransactionStatus.InvalidOperation, "Invalid arguments");

        await using var connection = new NpgsqlConnection(connectionString ?? PostgresStorage.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            await using (var lockCmd = new NpgsqlCommand("SELECT id FROM game_sessions WHERE id=@id FOR UPDATE", connection, transaction))
            {
                lockCmd.Parameters.AddWithValue("id", session);
                if (await lockCmd.ExecuteScalarAsync().ConfigureAwait(false) == null) return Result(TransactionStatus.InvalidOperation, "Campaign session not found");
            }

            var (city, operation) = found.Value;
            var state = await WorldStorage.ReadAsync(connection, transaction, session, city).ConfigureAwait(false);
            if (state == null) return Result(TransactionStatus.PrerequisitesNotMet, "City has not been visited");

            string receiptKey = $"receipt:{session}:{actionToken}";
            await using (var checkCmd = new NpgsqlCommand("SELECT value::text FROM game_state WHERE state_key=@key", connection, transaction))
            {
                checkCmd.Parameters.AddWithValue("key", receiptKey);
                if (await checkCmd.ExecuteScalarAsync().ConfigureAwait(false) is string existingJson && JsonSerializer.Deserialize<TransactionReceipt>(existingJson) is { } existing)
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                    return existing with { Status = TransactionStatus.AlreadyCommitted };
                }
            }

            int prior = 0;
            state.OperationClears?.TryGetValue(operationId, out prior);
            var (sparks, credits) = OperationReward(operation, prior);
            var progression = await CampaignProgressionState.LoadOrCreateAsync(session, connection, transaction).ConfigureAwait(false);
            progression.GlobalSparksBank += sparks;
            progression.CityCredits += credits;
            await CampaignProgressionState.SaveAsync(session, progression, connection, transaction).ConfigureAwait(false);

            var clears = state.OperationClears != null ? new Dictionary<string, int>(state.OperationClears) : new Dictionary<string, int>();
            clears[operationId] = prior + 1;
            await WorldStorage.WriteAsync(connection, transaction, session, state with { OperationClears = clears, Operative = operative ?? state.Operative }).ConfigureAwait(false);

            var receipt = Result(TransactionStatus.Success, $"Earned {sparks} Sparks, {credits} Credits");
            await using (var writeReceipt = new NpgsqlCommand("INSERT INTO game_state (state_key, schema_version, value) VALUES (@key, 1, @val)", connection, transaction))
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
            return Result(TransactionStatus.DatabaseError, ex.Message);
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
