using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One campaign mission: where it lives, what story key it uses, who joins during it and what it demands.</summary>
/// <param name="Number">1-based mission number; also the value stored in a save's <c>current_level</c>.</param>
/// <param name="Title">Display title shown on level cards and title cards.</param>
/// <param name="ScenePath">Godot resource path of the mission scene.</param>
/// <param name="NarrativeKey">Key into <c>assets/narrative/*.json</c> "levels" (kept as a string, as the JSON uses string keys).</param>
/// <param name="GrantsOperatives">Operatives (registry display names) that join the squad during this mission, in order.</param>
/// <param name="RequiredAbilities">Signature-ability tags the mission's required route needs; every one must belong to an operative unlocked by the time the gate appears.</param>
public sealed record LevelEntry(
    int Number, string Title, string ScenePath, string NarrativeKey,
    IReadOnlyList<string> GrantsOperatives, IReadOnlyList<string> RequiredAbilities);

/// <summary>
/// The single ordered list of campaign missions. Level selection, save/resume, level-to-level transitions and the
/// operative unlock schedule all read this so that adding a mission is one new row plus one scene.
/// </summary>
public static class LevelCatalog
{
    /// <summary>Ability tags for each operative's signature ability (see <see cref="OperativeRoster.AbilityTagOf"/>).</summary>
    public const string Parry = "parry", Douse = "douse", Slam = "slam", Resonate = "resonate", Phase = "phase", Bulwark = "bulwark";

    private static readonly LevelEntry[] Entries =
    {
        new(1, "Ghost in the Gridlock", "res://scenes/gameplay/level1.tscn", "1",
            Array.Empty<string>(), new[] { Parry }),
        new(2, "The Concrete Wildfire", "res://scenes/gameplay/level2.tscn", "2",
            new[] { "Oceanus", "Apex Titan" }, new[] { Parry, Douse, Slam }),
        new(3, "Neon Rust and Data Dust", "res://scenes/gameplay/level3.tscn", "3",
            new[] { "Aura Phoenicia", "Mirage Blade" }, new[] { Slam, Resonate, Phase }),
        new(4, "Shattered Aegis", "res://scenes/gameplay/level4.tscn", "4",
            new[] { "Surya Prime" }, new[] { Phase, Bulwark }),
    };

    /// <summary>All missions in play order.</summary>
    public static IReadOnlyList<LevelEntry> All => Entries;

    /// <summary>Number of playable missions.</summary>
    public static int Count => Entries.Length;

    /// <summary>Clamps any saved or requested number into the valid 1..Count range (old saves may hold 0 or retired 5-9).</summary>
    public static int Clamp(int number) => Math.Clamp(number, 1, Count);

    /// <summary>Returns the entry for a (clamped) mission number.</summary>
    public static LevelEntry Get(int number) => Entries[Clamp(number) - 1];

    /// <summary>Scene path for a (clamped) mission number.</summary>
    public static string ScenePath(int number) => Get(number).ScenePath;

    /// <summary>True when a mission follows <paramref name="number"/>.</summary>
    public static bool HasNext(int number) => number < Count;

    /// <summary>Operatives that have joined by the *start* of a mission: everyone granted in earlier missions.</summary>
    public static IEnumerable<string> GrantedBefore(int number) =>
        Entries.Where(e => e.Number < number).SelectMany(e => e.GrantsOperatives);
}
