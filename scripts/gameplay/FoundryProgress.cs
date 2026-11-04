using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 7 (The Foundry Forge - Act II Finale).
/// Tracks volcanic rift ingress, cryo-coolant turbine disruptions, magnetic arc phase-throughs,
/// radiation containment breaches, the multi-phase IGNIS-COLOSSUS boss encounter,
/// and sky-shield collapse extraction without Godot engine dependencies.
/// </summary>
public sealed class FoundryProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 7.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "ingress_breach",
        "coolant_douse",
        "valve_shatter",
        "turbines_overloaded",
        "arc_phase",
        "resonance_disable",
        "conduits_severed",
        "radiation_bulwark",
        "core_overlook_breach",
        "boss_phase:1",
        "boss_phase:2",
        "boss_phase:3",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 Foundry Forge sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "ingress1", "ingress2",
        "coolant1", "coolant2", "coolant3",
        "induction1", "induction2", "induction3",
        "containment1", "containment2", "containment3",
        "boss1", "boss2", "boss3"
    });

    /// <summary>The next unmet milestone supplies active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after the boss phase 3 core collapse has occurred.</summary>
    public bool CanExtract => Has("boss_phase:3");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "ingress1" or "ingress2" => true,
        "coolant1" or "coolant2" => Has("ingress_breach"),
        "coolant3" => Has("valve_shatter"),
        "induction1" or "induction2" => Has("turbines_overloaded"),
        "induction3" => Has("resonance_disable"),
        "containment1" or "containment2" => Has("conduits_severed"),
        "containment3" => Has("radiation_bulwark"),
        "boss1" => Has("core_overlook_breach"),
        "boss2" => Has("boss_phase:1"),
        "boss3" => Has("boss_phase:2"),
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
            "ingress_breach" => Cleared("ingress1", "ingress2"),
            "coolant_douse" => Cleared("coolant1"),
            "valve_shatter" => Has("coolant_douse") && Cleared("coolant2"),
            "turbines_overloaded" => Has("valve_shatter") && Cleared("coolant3"),
            "arc_phase" => Cleared("induction1"),
            "resonance_disable" => Has("arc_phase") && Cleared("induction2"),
            "conduits_severed" => Has("resonance_disable") && Cleared("induction3"),
            "radiation_bulwark" => Cleared("containment1", "containment2"),
            "core_overlook_breach" => Has("radiation_bulwark") && Cleared("containment3"),
            "boss_phase:1" => Cleared("boss1"),
            "boss_phase:2" => Has("boss_phase:1") && Cleared("boss2"),
            "boss_phase:3" => Has("boss_phase:2") && Cleared("boss3"),
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
