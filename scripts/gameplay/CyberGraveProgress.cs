using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 6 (Cyber-Grave Labs - Underground Facility).
/// Tracks sector progression, stasis pod liberations, coolant overrides, barrier bypasses,
/// and core security encounters without Godot engine dependencies so it can be verified in pure NUnit.
/// </summary>
public sealed class CyberGraveProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 6.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "sump_breach",
        "pod:1",
        "pod:2",
        "incubation_cleared",
        "coolant_override",
        "resonance_bypass",
        "grid_phase",
        "siphon_contain",
        "core_defense:1",
        "core_defense:2",
        "core_defense:3",
        "vault_override",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 subterranean sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "sump1", "sump2",
        "incubate1", "incubate2", "incubate3",
        "coolant1", "coolant2",
        "resonance1", "resonance2", "laser1",
        "siphon1", "siphon2",
        "core1", "core2", "core3"
    });

    /// <summary>The next unmet milestone supplies active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after the vault override is engaged.</summary>
    public bool CanExtract => Has("vault_override");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "sump1" or "sump2" => true,
        "incubate1" or "incubate2" or "incubate3" => Has("sump_breach"),
        "coolant1" or "coolant2" => Has("incubation_cleared"),
        "resonance1" or "resonance2" => Has("coolant_override"),
        "laser1" => Has("resonance_bypass"),
        "siphon1" or "siphon2" => Has("grid_phase"),
        "core1" => Has("siphon_contain"),
        "core2" => Has("core_defense:1"),
        "core3" => Has("core_defense:2"),
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
            "sump_breach" => Cleared("sump1", "sump2"),
            "pod:1" => Cleared("incubate1"),
            "pod:2" => Cleared("incubate2"),
            "incubation_cleared" => Has("pod:1") && Has("pod:2") && Cleared("incubate3"),
            "coolant_override" => Cleared("coolant1", "coolant2"),
            "resonance_bypass" => Cleared("resonance1", "resonance2"),
            "grid_phase" => Cleared("laser1"),
            "siphon_contain" => Cleared("siphon1", "siphon2"),
            "core_defense:1" => Cleared("core1"),
            "core_defense:2" => Cleared("core2"),
            "core_defense:3" => Cleared("core3"),
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
