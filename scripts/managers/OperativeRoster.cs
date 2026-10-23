using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks which operatives the player has earned, which one is active, and which ability lessons have been shown.
/// <para>
/// The roster is deliberately *derived* from campaign progress instead of being stored separately: the squad at the
/// start of mission N is the starter plus everyone granted by missions before N (<see cref="LevelCatalog"/>). Joins
/// that happen mid-mission are applied live with <see cref="Unlock"/> and simply replay if the mission is reloaded, so
/// nothing extra needs to be saved and old saves keep working.
/// </para>
/// This class has no engine dependencies so it can be unit tested directly.
/// </summary>
public sealed class OperativeRoster
{
    /// <summary>The operative every new campaign begins with (the story's lead).</summary>
    public const string Starter = "Cyber Shogun";

    /// <summary>Canonical roster order, used for cycling and for listing locked operatives.</summary>
    public static readonly IReadOnlyList<string> RosterOrder = new[]
    {
        "Cyber Shogun", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade", "Surya Prime"
    };

    private static readonly Dictionary<string, string> AbilityTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cyber Shogun"] = LevelCatalog.Parry,
        ["Oceanus"] = LevelCatalog.Douse,
        ["Apex Titan"] = LevelCatalog.Slam,
        ["Aura Phoenicia"] = LevelCatalog.Resonate,
        ["Mirage Blade"] = LevelCatalog.Phase,
        ["Surya Prime"] = LevelCatalog.Bulwark,
    };

    private readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _lessonsSeen = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The roster used by whatever mission is currently loaded.</summary>
    public static OperativeRoster Current { get; private set; } = ForLevelStart(1);

    /// <summary>Raised after an operative is newly unlocked, with the operative's display name.</summary>
    public event Action<string>? OperativeUnlocked;

    /// <summary>Raised after the active operative changes, with the new operative's display name.</summary>
    public event Action<string>? ActiveChanged;

    /// <summary>Display name of the operative the player is controlling.</summary>
    public string Active { get; private set; } = Starter;

    /// <summary>Unlocked operatives in canonical roster order.</summary>
    public IReadOnlyList<string> Unlocked => RosterOrder.Where(_unlocked.Contains).ToList();

    /// <summary>Signature-ability tag of an operative (see <see cref="LevelCatalog"/> constants), or null if unknown.</summary>
    public static string? AbilityTagOf(string operative) =>
        AbilityTags.TryGetValue(Normalize(operative), out var tag) ? tag : null;

    /// <summary>Builds the roster a player has at the start of mission <paramref name="level"/> (earlier grants only).</summary>
    public static OperativeRoster ForLevelStart(int level)
    {
        var roster = new OperativeRoster();
        roster._unlocked.Add(Starter);
        foreach (string name in LevelCatalog.GrantedBefore(level))
            roster._unlocked.Add(Normalize(name));
        // Anyone who joined in an earlier mission already had their lesson there; the starter's lesson belongs to mission 1.
        foreach (string name in roster._unlocked)
            if (level > 1 || name != Starter) roster._lessonsSeen.Add(name);
        return roster;
    }

    /// <summary>Replaces <see cref="Current"/> with the roster for the start of <paramref name="level"/> and returns it.</summary>
    public static OperativeRoster BeginLevel(int level) => Current = ForLevelStart(level);

    /// <summary>
    /// Maps any alias ("titan", "Kaito", a civilian name...) to the operative's display name; unknown names become the
    /// starter. Mirrors <c>OperativeRegistry.GetProfile</c>'s matching but stays engine-free: the registry's profiles
    /// use Godot types that cannot load inside the plain NUnit host.
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Starter;
        string trimmed = name.Trim();
        foreach (string known in RosterOrder)
            if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase)) return known;
        bool Has(string part) => trimmed.Contains(part, StringComparison.OrdinalIgnoreCase);
        if (Has("surya") || Has("arjun")) return "Surya Prime";
        if (Has("oceanus") || Has("kiana")) return "Oceanus";
        if (Has("mirage") || Has("zayd")) return "Mirage Blade";
        if (Has("titan") || Has("tariq") || Has("apex")) return "Apex Titan";
        if (Has("phoenicia") || Has("maya") || Has("aura")) return "Aura Phoenicia";
        return Starter;
    }

    /// <summary>True when the operative has joined the squad.</summary>
    public bool IsUnlocked(string name) => _unlocked.Contains(Normalize(name));

    /// <summary>True when the squad contains an operative whose signature ability has this tag.</summary>
    public bool HasAbility(string tag) => _unlocked.Any(n => AbilityTagOf(n) == tag);

    /// <summary>Adds an operative to the squad. Returns true only the first time (so recruit beats fire once).</summary>
    public bool Unlock(string name)
    {
        string normalized = Normalize(name);
        if (!_unlocked.Add(normalized)) return false;
        OperativeUnlocked?.Invoke(normalized);
        return true;
    }

    /// <summary>Makes an unlocked operative active. Locked operatives are refused so a locked hero can never be fielded.</summary>
    public bool SetActive(string name)
    {
        string normalized = Normalize(name);
        if (!_unlocked.Contains(normalized)) return false;
        if (Active != normalized)
        {
            Active = normalized;
            ActiveChanged?.Invoke(normalized);
        }
        return true;
    }

    /// <summary>Steps to the next (+1) or previous (-1) unlocked operative in roster order, wrapping; returns the new active name.</summary>
    public string Cycle(int direction)
    {
        var list = Unlocked;
        if (list.Count <= 1) return Active;
        int index = list.ToList().IndexOf(Active);
        int next = ((index + Math.Sign(direction)) % list.Count + list.Count) % list.Count;
        SetActive(list[next]);
        return Active;
    }

    /// <summary>True once the operative's ability lesson has been shown (or was shown in an earlier mission).</summary>
    public bool LessonSeen(string operative) => _lessonsSeen.Contains(Normalize(operative));

    /// <summary>Records that the operative's lesson is complete so it is never shown again this session.</summary>
    public void MarkLessonSeen(string operative) => _lessonsSeen.Add(Normalize(operative));
}
