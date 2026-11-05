using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 8 (Stratos Docks - Aviation Hub).
/// Tracks high-altitude catwalk infiltration, cryogenic fuel conduit breaches,
/// dual-phase radar scanner grid disruptions, flak artillery corridor survival,
/// the multi-phase Aero-Centurion flight-deck mechanoid encounter, and VTOL gunship extraction
/// without Godot engine dependencies.
/// </summary>
public sealed class StratosProgress : ILaterMissionProgress
{
    /// <inheritdoc/>
    public IReadOnlyList<string> ObjectiveIds => Objectives;

    /// <inheritdoc/>
    public void ResetArena(string phasePrefix, string[] formations)
    {
        if (Has(phasePrefix + ":3")) return;
        _done.RemoveWhere(id => id.StartsWith(phasePrefix + ":", StringComparison.Ordinal) ||
            formations.Any(f => id == "encounter:" + f));
        // Slot reward receipts deliberately survive an abandoned arena attempt.
    }

    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 8.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "perimeter_ingress",
        "cryo_fuel_douse",
        "titan_clamp_shatter",
        "fuel_conduits_secured",
        "scanner_grid_phase",
        "radar_resonance_disable",
        "scanner_array_cleared",
        "flak_bulwark_breach",
        "hangar_overlook_breach",
        "aero_guardian_phase:1",
        "aero_guardian_phase:2",
        "aero_guardian_phase:3",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 Stratos Docks sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "catwalk1", "catwalk2",
        "fuel1", "fuel2", "fuel3",
        "scanner1", "scanner2", "scanner3",
        "gantry1", "gantry2", "gantry3",
        "hangar1", "hangar2", "hangar3"
    });

    /// <summary>The next unmet milestone supplies active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after the Aero-Centurion phase 3 defeat has occurred.</summary>
    public bool CanExtract => Has("aero_guardian_phase:3");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "catwalk1" or "catwalk2" => true,
        "fuel1" or "fuel2" => Has("perimeter_ingress"),
        "fuel3" => Has("titan_clamp_shatter"),
        "scanner1" or "scanner2" => Has("fuel_conduits_secured"),
        "scanner3" => Has("radar_resonance_disable"),
        "gantry1" or "gantry2" => Has("scanner_array_cleared"),
        "gantry3" => Has("flak_bulwark_breach"),
        "hangar1" => Has("hangar_overlook_breach"),
        "hangar2" => Has("aero_guardian_phase:1"),
        "hangar3" => Has("aero_guardian_phase:2"),
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
            "perimeter_ingress" => Cleared("catwalk1", "catwalk2"),
            "cryo_fuel_douse" => Cleared("fuel1"),
            "titan_clamp_shatter" => Has("cryo_fuel_douse") && Cleared("fuel2"),
            "fuel_conduits_secured" => Has("titan_clamp_shatter") && Cleared("fuel3"),
            "scanner_grid_phase" => Cleared("scanner1"),
            "radar_resonance_disable" => Has("scanner_grid_phase") && Cleared("scanner2"),
            "scanner_array_cleared" => Has("radar_resonance_disable") && Cleared("scanner3"),
            "flak_bulwark_breach" => Cleared("gantry1", "gantry2"),
            "hangar_overlook_breach" => Has("flak_bulwark_breach") && Cleared("gantry3"),
            "aero_guardian_phase:1" => Cleared("hangar1"),
            "aero_guardian_phase:2" => Has("aero_guardian_phase:1") && Cleared("hangar2"),
            "aero_guardian_phase:3" => Has("aero_guardian_phase:2") && Cleared("hangar3"),
            _ => true
        };
    }

    /// <summary>Commits an objective or combat event to the ledger.</summary>
    public bool Complete(string id) => CanComplete(id) && _done.Add(id);

    /// <summary>Awards points for a designated enemy member once, preserving idempotency across checkpoint restarts.</summary>
    public bool Reward(string id) => Formations.Any(f => Enumerable.Range(0, 4).Any(slot => id == f + ":" + slot)) && _rewards.Add(id);

    /// <summary>True only if all specified combat encounters are recorded in the ledger.</summary>
    private bool Cleared(params string[] ids) => ids.All(id => Has("encounter:" + id));
}
