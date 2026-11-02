using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Exact scene-local fortress objectives. Recovery retains this ledger; loading a scene creates a fresh one.</summary>
public sealed class AegisProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);
    /// <summary>Fourteen finite formations; only designated objective defenders are mandatory.</summary>
    public static IEnumerable<string> Encounters => new[] { 3, 2, 3, 3, 3 }.SelectMany((n, s) =>
        Enumerable.Range(1, n).Select(g => $"encounter:s{s + 1}-{g}"));
    /// <summary>Ordered critical path; the two gun approaches share one accepted controller event.</summary>
    public static IReadOnlyList<string> Objectives { get; } = Array.AsReadOnly(new[] {
        "interlock:approach", "recruit:surya", "lesson:bulwark", "crossing:bulwark", "coolant:intake",
        "guns:western", "lesson:blink", "curtain:inner", "boss:voss", "crossing:open" });
    /// <summary>Extraction needs the full objective chain, never every ordinary kill.</summary>
    public bool CanExtract => Objectives.All(Has);
    /// <summary>First unmet objective ID, suitable for localized HUD guidance.</summary>
    public string CurrentObjective => Objectives.FirstOrDefault(id => !Has(id)) ?? "extract";
    /// <summary>Exact event query: unknown prefixes cannot substitute for authored IDs.</summary>
    public bool Has(string id) => _done.Contains(id);
    /// <summary>Rejects duplicate, unknown and premature triggers without granting rewards.</summary>
    public bool Complete(string id) => CanComplete(id) && _done.Add(id);
    /// <summary>Prerequisites combine the ordered mission chain with selected local defense formations.</summary>
    public bool CanComplete(string id)
    {
        if (Has(id)) return false;
        if (Encounters.Contains(id, StringComparer.Ordinal)) return true;
        int index = Objectives.ToList().IndexOf(id);
        if (index < 0 || Objectives.Take(index).Any(prerequisite => !Has(prerequisite))) return false;
        return id switch {
            "interlock:approach" => Has("encounter:s1-3"),
            "recruit:surya" => Has("encounter:s2-1") && Has("encounter:s2-2"),
            "coolant:intake" => Has("encounter:s3-3"),
            "guns:western" => Has("encounter:s4-3"),
            "curtain:inner" => Has("encounter:s5-3"),
            _ => true
        };
    }
    /// <summary>Rewards only authored enemy slots and Voss, once across checkpoint retries.</summary>
    public bool Reward(string id)
    {
        bool known = id == "boss:voss" || Encounters.Any(e =>
            Enumerable.Range(0, 3).Any(slot => id == e[10..] + ":" + slot));
        return known && _rewards.Add(id);
    }
}
