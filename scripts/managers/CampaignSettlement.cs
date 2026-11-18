using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Explicit extraction persistence result; direct scene play can finish without creating a campaign.</summary>
public enum CompletionSaveStatus { Saved, Failed, NoActiveCampaign }

/// <summary>Engine-free result safe to return from worker continuations without touching Godot nodes.</summary>
public sealed record CompletionSaveResult(CompletionSaveStatus Status, int CurrentLevel = 0, int TotalScore = 0,
    string Error = "");

/// <summary>
/// Mission settlement on the account's save slot (2026-11-18; previously a PostgreSQL transaction).
/// One commit carries the run's receipt (its stable per-run token), the progress and score update,
/// and the post-mission return to Los Angeles. Retrying after a lost answer, or settling the same
/// token concurrently, cannot award twice: the server answers "already committed".
/// </summary>
public static class CampaignSettlement
{
    /// <summary>Receipt kind stored with each settled run.</summary>
    public const string ReceiptKind = "mission_completion";

    /// <summary>
    /// Settles one run. Never lowers progress. Reusing a token for a different result is refused.
    /// The optional store is for tests; the game uses <see cref="SaveStore.Current"/>.
    /// </summary>
    public static async Task<CompletionSaveResult> SaveAsync(int sessionId, int mission, int score, string runToken,
        ISaveStore? store = null, string operative = OperativeRoster.Starter)
    {
        if (sessionId <= 0) return new(CompletionSaveStatus.NoActiveCampaign);
        if (mission < 1 || mission > LevelCatalog.Count || score < 0 || !Guid.TryParse(runToken, out _))
            return new(CompletionSaveStatus.Failed, Error: "Invalid mission completion receipt.");
        string receipt = JsonSerializer.Serialize(new { mission, score });
        try
        {
            var result = await SlotTransaction.RunAsync(store ?? SaveStore.Current, sessionId, snapshot =>
                new SlotCommit(snapshot.Slot.Revision, runToken, ReceiptKind, receipt,
                    Character: operative, LevelAtLeast: LevelCatalog.Clamp(mission + 1), ScoreAdd: score,
                    // A lost settlement answer must never reapply the return over later transit or a new
                    // mission entry; the receipt makes a retry a no-op, so this write happens once.
                    Docs: new[] { WorldStorage.Write(WorldStorage.SettleReturn(snapshot, mission, operative)) })).ConfigureAwait(false);
            return result.Outcome switch
            {
                CommitOutcome.Committed or CommitOutcome.AlreadyCommitted => new(CompletionSaveStatus.Saved,
                    result.Snapshot!.Slot.CurrentLevel, (int)Math.Min(int.MaxValue, result.Snapshot.Slot.TotalScore)),
                CommitOutcome.ReceiptMismatch => new(CompletionSaveStatus.Failed, Error: "Completion receipt does not match this run."),
                CommitOutcome.NotFound => new(CompletionSaveStatus.Failed, Error: "The selected campaign no longer exists."),
                CommitOutcome.NotSignedIn => new(CompletionSaveStatus.Failed, Error: "Sign in to save. Retry Save uses the same receipt."),
                _ => new(CompletionSaveStatus.Failed, Error: "Campaign save could not be confirmed. Retry Save uses the same receipt."),
            };
        }
        catch (InvalidOperationException)
        {
            // An invalid world record cannot be settled; no detail reaches the results screen.
            return new(CompletionSaveStatus.Failed, Error: "Campaign save could not be confirmed. Retry Save uses the same receipt.");
        }
    }
}
