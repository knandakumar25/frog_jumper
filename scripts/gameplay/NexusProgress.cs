using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-local mission ledger for Mission 10 (The Cyber-Nexus - Mainframe Core Infiltration).
/// Tracks interior core ingress, liquid helium manifold dousing, server bracket destruction,
/// optical laser firewall phasing, biometric scanner defense, neural matrix cipher resonance bypass,
/// the multi-phase Nexus Overseer boss encounter, quantum processing nodes override, and express ascent lift extraction
/// without Godot engine dependencies.
/// </summary>
public sealed class NexusProgress : ILaterMissionProgress
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

    /// <summary>Exact ordered narrative and tactical objective milestones for Mission 10.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "ingress_confirmed",
        "helium_manifold_doused",
        "server_bracket_shattered",
        "cooling_vaults_secured",
        "laser_firewall_phased",
        "biometric_firewall_absorbed",
        "optical_conduits_secured",
        "cipher_resonance_bypassed",
        "neural_substation_breached",
        "nexus_overseer_phase:1",
        "nexus_overseer_phase:2",
        "nexus_overseer_phase:3",
        "quantum_nodes_overridden",
        "extract"
    });

    /// <summary>Finite authored combat formations across all 6 Cyber-Nexus sectors.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "ingress1", "ingress2",
        "coolant1", "coolant2", "coolant3",
        "data1", "data2", "data3",
        "neural1", "neural2", "neural3",
        "core1", "core2", "core3"
    });

    /// <summary>The next unmet milestone supplies active HUD guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";

    /// <summary>Extraction is authorized only after all three quantum nodes are overridden following boss defeat.</summary>
    public bool CanExtract => Has("quantum_nodes_overridden");

    /// <summary>True if the exact event or objective has been recorded.</summary>
    public bool Has(string id) => _done.Contains(id);

    /// <summary>Maps each combat formation to its required progress gate.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch
    {
        "ingress1" or "ingress2" => true,
        "coolant1" or "coolant2" => Has("ingress_confirmed"),
        "coolant3" => Has("server_bracket_shattered"),
        "data1" or "data2" => Has("cooling_vaults_secured"),
        "data3" => Has("biometric_firewall_absorbed"),
        "neural1" or "neural2" => Has("optical_conduits_secured"),
        "neural3" => Has("cipher_resonance_bypassed"),
        "core1" => Has("neural_substation_breached"),
        "core2" => Has("nexus_overseer_phase:1"),
        "core3" => Has("nexus_overseer_phase:2"),
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
            "ingress_confirmed" => Cleared("ingress1", "ingress2"),
            "helium_manifold_doused" => Cleared("coolant1"),
            "server_bracket_shattered" => Has("helium_manifold_doused") && Cleared("coolant2"),
            "cooling_vaults_secured" => Has("server_bracket_shattered") && Cleared("coolant3"),
            "laser_firewall_phased" => Cleared("data1"),
            "biometric_firewall_absorbed" => Has("laser_firewall_phased") && Cleared("data2"),
            "optical_conduits_secured" => Has("biometric_firewall_absorbed") && Cleared("data3"),
            "cipher_resonance_bypassed" => Cleared("neural1", "neural2"),
            "neural_substation_breached" => Has("cipher_resonance_bypassed") && Cleared("neural3"),
            "nexus_overseer_phase:1" => Cleared("core1"),
            "nexus_overseer_phase:2" => Has("nexus_overseer_phase:1") && Cleared("core2"),
            "nexus_overseer_phase:3" => Has("nexus_overseer_phase:2") && Cleared("core3"),
            "quantum_nodes_overridden" => Has("nexus_overseer_phase:3"),
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
