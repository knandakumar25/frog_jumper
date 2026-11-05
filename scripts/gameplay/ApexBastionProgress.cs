using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 11 (The Apex Bastion - Stratospheric Summit Fortress).
/// Tracks arrival gantry clearance, plasma flak coolant dousing, titan mooring clamp demolition,
/// perimeter flak battery neutralization, high-voltage laser curtain phasing, thermal vent absorption,
/// lower redoubt clearance, acoustic security lock resonance, vertical hydraulic elevator scaling,
/// the multi-phase Bastion Praetor boss encounter, citadel blast doors breach, and throne room extraction
/// without Godot engine dependencies.
/// </summary>
public sealed class ApexBastionProgress : ILaterMissionProgress
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

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 11.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "staging_secured",
        "flak_coolant_doused",
        "titan_clamp_shattered",
        "flak_battery_neutralized",
        "laser_curtain_phased",
        "thermal_vent_absorbed",
        "lower_redoubt_cleared",
        "acoustic_lock_resonated",
        "elevator_shaft_scaled",
        "bastion_praetor_phase:1",
        "bastion_praetor_phase:2",
        "bastion_praetor_phase:3",
        "citadel_blast_doors_breached",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 Apex Bastion sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "summit1", "summit2",
        "flak1", "flak2", "flak3",
        "redoubt1", "redoubt2", "redoubt3",
        "praetorian1", "praetorian2",
        "arena1", "arena2", "arena3",
        "breach1"
    });

    /// <summary>The next unmet milestone supplying active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after the Citadel blast doors are breached following Praetor defeat.</summary>
    public bool CanExtract => Has("citadel_blast_doors_breached");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "summit1" or "summit2" => true,
        "flak1" or "flak2" => Has("staging_secured"),
        "flak3" => Has("titan_clamp_shattered"),
        "redoubt1" or "redoubt2" => Has("flak_battery_neutralized"),
        "redoubt3" => Has("thermal_vent_absorbed"),
        "praetorian1" => Has("lower_redoubt_cleared"),
        "praetorian2" => Has("acoustic_lock_resonated"),
        "arena1" => Has("elevator_shaft_scaled"),
        "arena2" => Has("bastion_praetor_phase:1"),
        "arena3" => Has("bastion_praetor_phase:2"),
        "breach1" => Has("bastion_praetor_phase:3"),
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
            "staging_secured" => Cleared("summit1", "summit2"),
            "flak_coolant_doused" => Cleared("flak1"),
            "titan_clamp_shattered" => Has("flak_coolant_doused") && Cleared("flak2"),
            "flak_battery_neutralized" => Has("titan_clamp_shattered") && Cleared("flak3"),
            "laser_curtain_phased" => Cleared("redoubt1"),
            "thermal_vent_absorbed" => Has("laser_curtain_phased") && Cleared("redoubt2"),
            "lower_redoubt_cleared" => Has("thermal_vent_absorbed") && Cleared("redoubt3"),
            "acoustic_lock_resonated" => Cleared("praetorian1"),
            "elevator_shaft_scaled" => Has("acoustic_lock_resonated") && Cleared("praetorian2"),
            "bastion_praetor_phase:1" => Cleared("arena1"),
            "bastion_praetor_phase:2" => Has("bastion_praetor_phase:1") && Cleared("arena2"),
            "bastion_praetor_phase:3" => Has("bastion_praetor_phase:2") && Cleared("arena3"),
            "citadel_blast_doors_breached" => Has("bastion_praetor_phase:3") && Cleared("breach1"),
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

    /// <summary>Awards combat score points for enemy slot defeats within an authored formation.</summary>
    public bool Reward(string slotId)
    {
        int colon = slotId.IndexOf(':');
        if (colon <= 0 || !Formations.Contains(slotId[..colon])) return false;
        if (!int.TryParse(slotId[(colon + 1)..], out int member) || member < 0 || member >= 4) return false;
        return _rewards.Add(slotId);
    }

    private bool Cleared(params string[] formations) =>
        formations.All(f => Has("encounter:" + f));
}
