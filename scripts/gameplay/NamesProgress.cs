using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Scene-local identity recovery ledger. Checkpoints retain completed events and reward slots; replay constructs a fresh ledger.</summary>
public sealed class NamesProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);
    /// <summary>Exact ordered recovery chain; protected records are events, never damageable actors.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "phase", "signal", "coolant", "maintenance", "record:1", "record:2", "record:3",
        "isolate", "defense:1", "defense:2", "defense:3", "commit", "extract" });
    /// <summary>Finite authored formations: two service, three processing, three utility, six archive and three core stages.</summary>
    public static IReadOnlyList<string> Formations { get; } = Array.AsReadOnly(new[] {
        "service1", "service2", "hall1", "hall2", "hall3", "utility1", "utility2", "utility3",
        "archive1a", "archive1b", "archive2a", "archive2b", "archive3a", "archive3b", "core1", "core2", "core3" });
    /// <summary>The next unmet story event supplies objective guidance.</summary>
    public string Current => Objectives.FirstOrDefault(id => !Has(id)) ?? "complete";
    /// <summary>Extraction becomes available only after all three sequential defenses and the civilian commit.</summary>
    public bool CanExtract => Has("commit");
    /// <summary>Exact queries reject prefixes and unknown identifiers.</summary>
    public bool Has(string id) => _done.Contains(id);
    /// <summary>Maps each formation to its durable prerequisite; incomplete stages restart after recovery.</summary>
    public bool CanStartFormation(string id) => Formations.Contains(id) && !Has("encounter:" + id) && (id switch {
        "service1" or "service2" => true,
        "hall1" or "hall2" or "hall3" => Has("phase"),
        "utility1" or "utility2" or "utility3" => Has("signal"),
        "archive1a" or "archive1b" => Has("maintenance"),
        "archive2a" or "archive2b" => Has("record:1"),
        "archive3a" or "archive3b" => Has("record:2"),
        "core1" => Has("isolate"), "core2" => Has("defense:1"), "core3" => Has("defense:2"), _ => false });
    /// <summary>Requires the entire preceding chain plus designated defenders; arbitrary and repeated events cannot advance recovery.</summary>
    public bool CanComplete(string id)
    {
        if (Has(id)) return false;
        if (id.StartsWith("encounter:", StringComparison.Ordinal)) return CanStartFormation(id[10..]);
        int index = Objectives.ToList().IndexOf(id);
        if (index < 0 || Objectives.Take(index).Any(p => !Has(p))) return false;
        return id switch {
            "phase" => Cleared("service1", "service2"),
            "signal" => Cleared("hall1", "hall2", "hall3"),
            "coolant" => Cleared("utility1", "utility2"),
            "maintenance" => Cleared("utility3"),
            "record:1" => Cleared("archive1a", "archive1b"),
            "record:2" => Cleared("archive2a", "archive2b"),
            "record:3" => Cleared("archive3a", "archive3b"),
            "defense:1" => Cleared("core1"), "defense:2" => Cleared("core2"), "defense:3" => Cleared("core3"),
            _ => true };
    }
    /// <summary>Commits one legal transition; no score is awarded for rejected or repeated input.</summary>
    public bool Complete(string id) => CanComplete(id) && _done.Add(id);
    /// <summary>Awards each authored member once even when an incomplete formation is rebuilt.</summary>
    public bool Reward(string id) => Formations.Any(f => Enumerable.Range(0, 3).Any(slot => id == f + ":" + slot)) && _rewards.Add(id);
    /// <summary>Checks designated defenders rather than a kill-count approximation.</summary>
    private bool Cleared(params string[] ids) => ids.All(id => Has("encounter:" + id));
}
