using Godot;
using System.Threading.Tasks;

/// <summary>
/// Glue between the campaign rules (<see cref="LevelCatalog"/>, <see cref="OperativeRoster"/>) and the engine-side
/// state: the <c>Global</c> autoload (active operative, highest unlocked level) and <see cref="SessionManager"/> saves.
/// Every entry point that starts a mission goes through here so the squad is always rebuilt from progress.
/// </summary>
public static class CampaignProgress
{
    /// <summary>The <c>Global</c> autoload, or null when running without it (pure unit tests).</summary>
    private static Node? Global =>
        (Engine.GetMainLoop() as SceneTree)?.Root.GetNodeOrNull<Node>("Global");

    /// <summary>
    /// Starts a brand-new campaign: creates the save with the starter operative, resets progress to mission 1
    /// and rebuilds the roster. Returns the new session id (or a non-positive value if saving failed; the menu must remain open).
    /// </summary>
    public static async Task<int> StartNewCampaignAsync()
    {
        var sessions = SessionManager.Instance;
        int id = sessions == null ? -1 : await sessions.CreateNewSessionAsync(OperativeRoster.Starter);
        if (id <= 0) return id;
        // Reset runtime progress only after PostgreSQL has committed the new campaign.
        Global?.Set("unlocked_levels", 1);
        Global?.Set("selected_character", OperativeRoster.Starter);
        PrepareMission(1);
        return id;
    }

    /// <summary>
    /// Rebuilds the squad for the start of <paramref name="mission"/> and syncs <c>Global</c>. Call before loading any
    /// mission from the menus. <paramref name="preferredOperative"/> (e.g. the saved character) is honoured only if unlocked.
    /// </summary>
    public static OperativeRoster PrepareMission(int mission, string? preferredOperative = null)
    {
        int level = LevelCatalog.Clamp(mission);
        var roster = OperativeRoster.BeginLevel(level);
        var global = Global;
        // A menu launch has no explicit preference, so fall back to the operative picked on the roster screen.
        preferredOperative ??= global?.Get("selected_character").AsString();
        if (!string.IsNullOrWhiteSpace(preferredOperative)) roster.SetActive(preferredOperative);
        if (global != null)
        {
            global.Set("selected_character", roster.Active);
            // Progress is "highest mission reached"; never lower it (replaying mission 1 must not re-lock mission 3).
            int current = global.Get("unlocked_levels").AsInt32();
            if (level > current) global.Set("unlocked_levels", level);
        }
        return roster;
    }

    /// <summary>Awaits PostgreSQL settlement, then applies runtime unlocks on the engine main thread.
    /// Direct scene play without a campaign may navigate; failed active-campaign saves keep forward actions disabled.</summary>
    public static async Task<CompletionSaveResult> CompleteMissionAsync(int completedMission, int points, string runToken, int sessionId)
    {
        string operative = Global?.Get("selected_character").AsString() ?? OperativeRoster.Current.Active;
        var result = await CampaignSettlement.SaveAsync(sessionId, completedMission, points, runToken, operative: operative).ConfigureAwait(false);
        var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Callable.From(() =>
        {
            try
            {
                if (result.Status != CompletionSaveStatus.Failed && LevelCatalog.HasNext(completedMission))
                    Global?.Call("unlock_next_level", completedMission);
                applied.SetResult();
            }
            catch (System.Exception exception) { applied.SetException(exception); }
        }).CallDeferred();
        await applied.Task.ConfigureAwait(false);
        return result;
    }
}
