using Godot;

/// <summary>
/// Lets GDScript (level selection, menus) read the C# <see cref="LevelCatalog"/> without static-call support:
/// <c>var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()</c>.
/// </summary>
public partial class LevelCatalogBridge : RefCounted
{
    private bool _entranceSelectionPending;
    /// <summary>Routes mission cards to a saved world entrance waypoint, preserving direct Mission 1 opening for new campaigns.</summary>
    public async void SelectEntrance(Node owner,int number)
    {
        if(_entranceSelectionPending) return;
        _entranceSelectionPending=true;
        try { await WorldNavigation.SelectEntrance(owner,number); }
        finally { _entranceSelectionPending=false; }
    }
    /// <summary>Number of playable missions.</summary>
    public int Count() => LevelCatalog.Count;

    /// <summary>Scene path for a mission number (clamped into range).</summary>
    public string ScenePath(int number) => LevelCatalog.ScenePath(number);

    /// <summary>Display title for a mission number (clamped into range).</summary>
    public string Title(int number) => LevelCatalog.Get(number).Title;

    /// <summary>Rebuilds the squad for a mission about to be launched from a menu, syncing <c>Global</c>.</summary>
    public void PrepareMission(int number) => CampaignProgress.PrepareMission(number);

    /// <summary>Adds an operative to the live squad (as a story recruit beat does); true only the first time.</summary>
    public bool UnlockOperative(string name) => OperativeRoster.Current.Unlock(name);

    /// <summary>True when the operative has joined the live squad.</summary>
    public bool IsOperativeUnlocked(string name) => OperativeRoster.Current.IsUnlocked(name);

    /// <summary>Display name of the operative currently being controlled.</summary>
    public string ActiveOperative() => OperativeRoster.Current.Active;

    /// <summary>How many operatives the live squad contains.</summary>
    public int UnlockedCount() => OperativeRoster.Current.Unlocked.Count;
}
