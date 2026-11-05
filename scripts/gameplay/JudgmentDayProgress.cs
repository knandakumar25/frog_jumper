using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 12 (Judgment Day - Citadel Summit &amp; Void Singularity Chamber).
/// Tracks ingress gantry clearance, power siphon coolant dousing, dark-matter siphon clamp demolition,
/// siphon grid neutralization, high-voltage laser curtain phasing, cosmic radiation vent absorption,
/// barrier corridor clearance, harmonic acoustic lock resonance, vertical cathedral vault scaling,
/// the multi-phase Void Commander boss encounter, singularity core containment initiation, and final campaign extraction
/// without Godot engine dependencies.
/// </summary>
public sealed class JudgmentDayProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 12.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "ingress_secured",
        "siphon_coolant_doused",
        "siphon_clamp_shattered",
        "siphon_grid_neutralized",
        "laser_curtain_phased",
        "cosmic_vent_absorbed",
        "barrier_corridor_cleared",
        "harmonic_lock_resonated",
        "cathedral_vault_scaled",
        "void_commander_phase:1",
        "void_commander_phase:2",
        "void_commander_phase:3",
        "core_containment_initiated",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 Citadel Summit sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "ingress1", "ingress2",
        "siphon1", "siphon2", "siphon3",
        "barrier1", "barrier2", "barrier3",
        "vault1", "vault2",
        "boss1", "boss2", "boss3",
        "citadel_remnant1"
    });

    /// <summary>The next unmet milestone supplying active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after the Singularity Core containment is initiated following Commander defeat.</summary>
    public bool CanExtract => Has("core_containment_initiated");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "ingress1" or "ingress2" => true,
        "siphon1" or "siphon2" => Has("ingress_secured"),
        "siphon3" => Has("siphon_clamp_shattered"),
        "barrier1" or "barrier2" => Has("siphon_grid_neutralized"),
        "barrier3" => Has("cosmic_vent_absorbed"),
        "vault1" or "vault2" => Has("barrier_corridor_cleared"),
        "boss1" => Has("cathedral_vault_scaled"),
        "boss2" => Has("void_commander_phase:1"),
        "boss3" => Has("void_commander_phase:2"),
        "citadel_remnant1" => Has("void_commander_phase:3"),
        _ => false
    });

    /// <summary>
    /// Evaluates whether an objective or encounter milestone can be completed based on strict prerequisites.
    /// </summary>
    public bool CanComplete(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || Has(id)) return false;

        if (id.StartsWith("encounter:", StringComparison.Ordinal))
        {
            string formation = id["encounter:".Length..];
            return CanStartFormation(formation);
        }

        return id switch
        {
            "ingress_secured" => Has("encounter:ingress1") && Has("encounter:ingress2"),
            "siphon_coolant_doused" => Has("ingress_secured"),
            "siphon_clamp_shattered" => Has("siphon_coolant_doused"),
            "siphon_grid_neutralized" => Has("siphon_clamp_shattered") && Has("encounter:siphon1") && Has("encounter:siphon2") && Has("encounter:siphon3"),
            "laser_curtain_phased" => Has("siphon_grid_neutralized"),
            "cosmic_vent_absorbed" => Has("laser_curtain_phased"),
            "barrier_corridor_cleared" => Has("cosmic_vent_absorbed") && Has("encounter:barrier1") && Has("encounter:barrier2") && Has("encounter:barrier3"),
            "harmonic_lock_resonated" => Has("barrier_corridor_cleared"),
            "cathedral_vault_scaled" => Has("harmonic_lock_resonated") && Has("encounter:vault1") && Has("encounter:vault2"),
            "void_commander_phase:1" => Has("cathedral_vault_scaled") && Has("encounter:boss1"),
            "void_commander_phase:2" => Has("void_commander_phase:1") && Has("encounter:boss2"),
            "void_commander_phase:3" => Has("void_commander_phase:2") && Has("encounter:boss3"),
            "core_containment_initiated" => Has("void_commander_phase:3") && Has("encounter:citadel_remnant1"),
            "extract" => CanExtract,
            _ => false
        };
    }

    /// <summary>
    /// Records completion of an objective or combat encounter. Rejects premature, duplicate, or unauthored events.
    /// </summary>
    /// <param name="id">Event or objective milestone identifier.</param>
    /// <returns>True if the event was accepted and registered.</returns>
    public bool Complete(string id) => !string.IsNullOrWhiteSpace(id) && CanComplete(id) && _done.Add(id);

    /// <summary>
    /// Rewards an enemy kill within a formation slot once per mission attempt.
    /// </summary>
    /// <param name="slotId">Format: "{formation}:{index}" (e.g. "ingress1:0").</param>
    /// <returns>True if the kill reward was accepted.</returns>
    public bool Reward(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId) || _rewards.Contains(slotId)) return false;

        string[] parts = slotId.Split(':');
        if (parts.Length != 2 || !Formations.Contains(parts[0])) return false;
        if (!int.TryParse(parts[1], out int index) || index < 0 || index >= 3) return false;

        _rewards.Add(slotId);
        return true;
    }
}
