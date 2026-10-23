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
    /// and rebuilds the roster. Returns the new session id (or a non-positive value if saving failed; play continues).
    /// </summary>
    public static async Task<int> StartNewCampaignAsync()
    {
        // A new campaign forgets progress from any earlier playthrough in this app session.
        Global?.Set("unlocked_levels", 1);
        Global?.Set("selected_character", OperativeRoster.Starter);
        PrepareMission(1);
        var sessions = SessionManager.Instance;
        return sessions == null ? -1 : await sessions.CreateNewSessionAsync(OperativeRoster.Starter);
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

    /// <summary>
    /// Records a finished mission: unlocks the next one, saves score and progress to the active session, and returns the
    /// next mission's scene path (or null after the final mission).
    /// </summary>
    public static string? CompleteMission(int completedMission, int points)
    {
        Global?.Call("unlock_next_level", completedMission);
        var sessions = SessionManager.Instance;
        if (sessions != null && sessions.CurrentSessionId > 0)
            _ = sessions.UpdateSessionAsync(sessions.CurrentSessionId, LevelCatalog.Clamp(completedMission + 1), points, 0);
        return LevelCatalog.HasNext(completedMission) ? LevelCatalog.ScenePath(completedMission + 1) : null;
    }
}
