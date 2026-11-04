using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 9 (Spire Outer Hull - High-Mobility Vertical Ascent).
/// Tracks high-altitude catwalk infiltration, cryogenic exhaust conduit neutralization,
/// perimeter laser grid phase-bypass, flak artillery corridor defense, vertical elevator shaft ascent,
/// the multi-phase Spire Sentinel mechanoid boss encounter, and Cyber-Nexus airlock extraction
/// without Godot engine dependencies.
/// </summary>
public sealed class SpireProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 9.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "gantry_ingress",
        "coolant_shaft_douse",
        "clamp_brace_shatter",
        "exhaust_conduits_cleared",
        "laser_grid_phase",
        "flak_barrier_deflect",
        "battery_deck_cleared",
        "relay_resonance_disable",
        "elevator_ascent_breach",
        "spire_sentinel_phase:1",
        "spire_sentinel_phase:2",
        "spire_sentinel_phase:3",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 Spire Outer Hull sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "gantry1", "gantry2",
        "exhaust1", "exhaust2", "exhaust3",
        "flak1", "flak2", "flak3",
        "shaft1", "shaft2", "shaft3",
        "summit1", "summit2", "summit3"
    });

    /// <summary>The next unmet milestone supplies active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after the Spire Sentinel phase 3 defeat has occurred.</summary>
    public bool CanExtract => Has("spire_sentinel_phase:3");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "gantry1" or "gantry2" => true,
        "exhaust1" or "exhaust2" => Has("gantry_ingress"),
        "exhaust3" => Has("clamp_brace_shatter"),
        "flak1" or "flak2" => Has("exhaust_conduits_cleared"),
        "flak3" => Has("flak_barrier_deflect"),
        "shaft1" or "shaft2" => Has("battery_deck_cleared"),
        "shaft3" => Has("relay_resonance_disable"),
        "summit1" => Has("elevator_ascent_breach"),
        "summit2" => Has("spire_sentinel_phase:1"),
        "summit3" => Has("spire_sentinel_phase:2"),
        _ => false
    });

    /// <summary>Verifies strict sequential ordering and formation prerequisite resolution before completing an objective.</summary>
    public bool CanComplete(string id)
    {
        if (Has(id)) return false;
        if (id.StartsWith("encounter:", StringComparison.Ordinal)) return CanStartFormation(id[10..]);
        int index = Objectives.ToList().IndexOf(id);
        if (index < 0 || Objectives.Take(index).Any(p => !Has(p))) return false;

        return id switch
        {
            "gantry_ingress" => Cleared("gantry1", "gantry2"),
            "coolant_shaft_douse" => Cleared("exhaust1"),
            "clamp_brace_shatter" => Has("coolant_shaft_douse") && Cleared("exhaust2"),
            "exhaust_conduits_cleared" => Has("clamp_brace_shatter") && Cleared("exhaust3"),
            "laser_grid_phase" => Cleared("flak1"),
            "flak_barrier_deflect" => Has("laser_grid_phase") && Cleared("flak2"),
            "battery_deck_cleared" => Has("flak_barrier_deflect") && Cleared("flak3"),
            "relay_resonance_disable" => Cleared("shaft1", "shaft2"),
            "elevator_ascent_breach" => Has("relay_resonance_disable") && Cleared("shaft3"),
            "spire_sentinel_phase:1" => Cleared("summit1"),
            "spire_sentinel_phase:2" => Has("spire_sentinel_phase:1") && Cleared("summit2"),
            "spire_sentinel_phase:3" => Has("spire_sentinel_phase:2") && Cleared("summit3"),
            _ => true
        };
    }

    /// <summary>Records an objective or encounter as completed if eligible.</summary>
    public bool Complete(string id)
    {
        if (!CanComplete(id)) return false;
        _done.Add(id);
        return true;
    }

    /// <summary>Marks an authored combat formation as cleared.</summary>
    public bool CompleteFormation(string id) => Complete("encounter:" + id);

    /// <summary>Awards points for a designated enemy member once, preserving idempotency across checkpoint restarts.</summary>
    public bool Reward(string id) => Formations.Any(f => Enumerable.Range(0, 4).Any(slot => id == f + ":" + slot)) && _rewards.Add(id);

    /// <summary>Awards combat scores and credits once per event.</summary>
    public int ClaimReward(string id, int value)
    {
        if (_rewards.Contains(id)) return 0;
        _rewards.Add(id);
        return value;
    }

    private bool Cleared(params string[] ids) => ids.All(id => Has("encounter:" + id));
}
