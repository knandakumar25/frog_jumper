using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
/// Atomic world actions on the account's save slot (2026-11-18; previously PostgreSQL transactions):
/// quest completion, city operation clears and purchases. Each action is one commit that carries its
/// action token, so a retry after a lost answer reports <see cref="TransactionStatus.AlreadyCommitted"/>
/// instead of rewarding or charging twice. The receipt value stored with the token describes the
/// action (not its timestamp), so the same action always matches its own receipt.
/// </summary>
public static class WorldTransactionService
{
    /// <summary>Paid repeat clears allowed per operation, so replaying can't become a credit farm.</summary>
    public const int MaxPaidRepeatClears = 5;

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>Maps a failed commit to a receipt status; the details are player-safe.</summary>
    private static TransactionReceipt Failure(CommitResult result, Func<TransactionStatus, string, TransactionReceipt> make) => result.Outcome switch
    {
        CommitOutcome.NotFound => make(TransactionStatus.InvalidOperation, "Campaign session not found"),
        CommitOutcome.ReceiptMismatch => make(TransactionStatus.InvalidOperation, "Action token was already used for a different action"),
        _ => make(TransactionStatus.DatabaseError, string.IsNullOrEmpty(result.Error) ? "The save could not be confirmed" : result.Error),
    };

    /// <summary>
    /// Completes an authored quest, granting one-time Hero Sparks, City Credits, faction reputation and
    /// any bonus module or gadget, and recording the completion (and branch choice) in the quest's city.
    /// </summary>
    public static async Task<TransactionReceipt> CompleteQuestAsync(int session, string questId, string? branchChoiceId,
        string actionToken, ISaveStore? store = null)
    {
        TransactionReceipt Make(TransactionStatus status, string details) => new(actionToken, session, "quest_completion", questId, status, Now(), details);
        if (session <= 0 || string.IsNullOrWhiteSpace(questId) || !Guid.TryParse(actionToken, out _)) return Make(TransactionStatus.InvalidOperation, "Invalid arguments");
        var quest = CityRegistry.FindQuest(questId);
        if (quest == null) return Make(TransactionStatus.InvalidOperation, "Quest definition not found");
        string receiptValue = JsonSerializer.Serialize(new { questId, branch = branchChoiceId });

        TransactionReceipt? early = null;
        try
        {
            var result = await SlotTransaction.RunAsync(store ?? SaveStore.Current, session, snapshot =>
            {
                early = null;
                // Repeats and invalid requests are refused before any reward; a city's quest lives in that city's record.
                var city = CityRegistry.CityOfQuest(questId);
                var world = WorldStorage.Read(snapshot, city)
                    ?? (city == WorldCatalog.Definition ? WorldSave.AfterMission(1) : WorldSave.ArrivalIn(city, 1, OperativeRoster.Starter));
                if (world.CompletedQuests?.ContainsKey(questId) == true) { early = Make(TransactionStatus.AlreadyCommitted, "Quest already completed"); return null; }
                if (quest.PrerequisiteQuestId != null && world.CompletedQuests?.ContainsKey(quest.PrerequisiteQuestId) != true)
                { early = Make(TransactionStatus.PrerequisitesNotMet, $"Requires {quest.PrerequisiteQuestId}"); return null; }
                if (branchChoiceId != null && quest.BranchChoices?.Any(c => c.ChoiceId == branchChoiceId) != true)
                { early = Make(TransactionStatus.InvalidOperation, "Unknown branch choice"); return null; }

                var progression = CampaignProgressionState.FromSnapshot(snapshot);
                progression.GlobalSparksBank += quest.Reward.HeroSparks;
                progression.CityCredits += quest.Reward.CityCredits;
                if (!string.IsNullOrEmpty(quest.Reward.FactionId) && quest.Reward.FactionReputation > 0)
                    progression.AddFactionReputation(quest.Reward.FactionId, quest.Reward.FactionReputation);
                if (!string.IsNullOrEmpty(quest.Reward.BonusModuleId)) progression.UnlockedModules.Add(quest.Reward.BonusModuleId);
                if (!string.IsNullOrEmpty(quest.Reward.BonusGadgetId)) progression.UnlockedGadgets.Add(quest.Reward.BonusGadgetId);
                return new SlotCommit(snapshot.Slot.Revision, actionToken, "quest_completion", receiptValue,
                    Docs: new[] { progression.ToDoc(), WorldStorage.Write(world.WithQuestCompleted(questId, branchChoiceId)) });
            }).ConfigureAwait(false);
            if (early != null) return early;
            return result.Outcome switch
            {
                CommitOutcome.Committed => Make(TransactionStatus.Success, $"Earned {quest.Reward.HeroSparks} Sparks, {quest.Reward.CityCredits} Credits"),
                CommitOutcome.AlreadyCommitted => Make(TransactionStatus.AlreadyCommitted, "Quest already completed"),
                _ => Failure(result, Make),
            };
        }
        catch (InvalidOperationException error) { return Make(TransactionStatus.DatabaseError, error.Message); }
    }

    /// <summary>Reward for one clear, given how many clears came before: Sparks once, then a limited run of credits.</summary>
    public static (int Sparks, int Credits) OperationReward(SideOperationDefinition operation, int priorClears) =>
        priorClears <= 0 ? (operation.FirstClearSparks, 0) : priorClears <= MaxPaidRepeatClears ? (0, operation.RepeatCredits) : (0, 0);

    /// <summary>
    /// Records a finished city operation and grants its reward once. Touches only the city record and
    /// shared rewards, never campaign mission progress.
    /// </summary>
    /// <param name="operative">Operative active at the finish, kept so a swap during the operation isn't undone on return. Null leaves it unchanged.</param>
    public static async Task<TransactionReceipt> CompleteOperationAsync(int session, string operationId, string actionToken,
        string? operative = null, ISaveStore? store = null)
    {
        TransactionReceipt Make(TransactionStatus status, string details) => new(actionToken, session, "operation_clear", operationId, status, Now(), details);
        var found = CityRegistry.FindOperation(operationId);
        if (session <= 0 || found == null || !Guid.TryParse(actionToken, out _)) return Make(TransactionStatus.InvalidOperation, "Invalid arguments");
        var (city, operation) = found.Value;
        string receiptValue = JsonSerializer.Serialize(new { operationId });
        TransactionReceipt? early = null;
        (int Sparks, int Credits) reward = (0, 0);
        try
        {
            var result = await SlotTransaction.RunAsync(store ?? SaveStore.Current, session, snapshot =>
            {
                early = null;
                var state = WorldStorage.Read(snapshot, city);
                if (state == null) { early = Make(TransactionStatus.PrerequisitesNotMet, "City has not been visited"); return null; }
                int prior = 0;
                state.OperationClears?.TryGetValue(operationId, out prior);
                reward = OperationReward(operation, prior);
                var progression = CampaignProgressionState.FromSnapshot(snapshot);
                progression.GlobalSparksBank += reward.Sparks;
                progression.CityCredits += reward.Credits;
                var clears = state.OperationClears != null ? new Dictionary<string, int>(state.OperationClears) : new Dictionary<string, int>();
                clears[operationId] = prior + 1;
                return new SlotCommit(snapshot.Slot.Revision, actionToken, "operation_clear", receiptValue,
                    Docs: new[] { progression.ToDoc(), WorldStorage.Write(state with { OperationClears = clears, Operative = operative ?? state.Operative }) });
            }).ConfigureAwait(false);
            if (early != null) return early;
            return result.Outcome switch
            {
                CommitOutcome.Committed => Make(TransactionStatus.Success, $"Earned {reward.Sparks} Sparks, {reward.Credits} Credits"),
                CommitOutcome.AlreadyCommitted => Make(TransactionStatus.AlreadyCommitted, "Operation already recorded"),
                _ => Failure(result, Make),
            };
        }
        catch (InvalidOperationException error) { return Make(TransactionStatus.DatabaseError, error.Message); }
    }

    /// <summary>
    /// The catalog price of a module or gadget, or null for an unknown item. Prices come from game
    /// data, never from the caller (2026-11-18 review of the old purchase path).
    /// </summary>
    public static int? ItemPrice(string itemType, string itemId) => itemType switch
    {
        "module" => CampaignProgressionState.ModuleCatalog.FirstOrDefault(m => m.Id == itemId)?.CostCredits,
        "gadget" => CampaignProgressionState.GadgetCatalog.FirstOrDefault(g => g.Id == itemId)?.CostCredits,
        _ => null,
    };

    /// <summary>Buys a catalog module or gadget with City Credits, once per action token.</summary>
    public static async Task<TransactionReceipt> PurchaseItemAsync(int session, string itemType, string itemId, string actionToken, ISaveStore? store = null)
    {
        TransactionReceipt Make(TransactionStatus status, string details) => new(actionToken, session, "purchase", itemId, status, Now(), details);
        int? price = ItemPrice(itemType, itemId);
        if (session <= 0 || price == null || !Guid.TryParse(actionToken, out _)) return Make(TransactionStatus.InvalidOperation, "Invalid arguments");
        string receiptValue = JsonSerializer.Serialize(new { itemType, itemId, price });
        TransactionReceipt? early = null;
        try
        {
            var result = await SlotTransaction.RunAsync(store ?? SaveStore.Current, session, snapshot =>
            {
                early = null;
                var progression = CampaignProgressionState.FromSnapshot(snapshot);
                if (progression.CityCredits < price) { early = Make(TransactionStatus.InsufficientFunds, $"Required: {price}, Available: {progression.CityCredits}"); return null; }
                progression.CityCredits -= price.Value;
                if (itemType == "module") progression.UnlockedModules.Add(itemId);
                else progression.UnlockedGadgets.Add(itemId);
                return new SlotCommit(snapshot.Slot.Revision, actionToken, "purchase", receiptValue, Docs: new[] { progression.ToDoc() });
            }).ConfigureAwait(false);
            if (early != null) return early;
            return result.Outcome switch
            {
                CommitOutcome.Committed => Make(TransactionStatus.Success, $"Purchased {itemId} for {price} credits"),
                CommitOutcome.AlreadyCommitted => Make(TransactionStatus.AlreadyCommitted, "Purchase already recorded"),
                _ => Failure(result, Make),
            };
        }
        catch (InvalidOperationException error) { return Make(TransactionStatus.DatabaseError, error.Message); }
    }
}
