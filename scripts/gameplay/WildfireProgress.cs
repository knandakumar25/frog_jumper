using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission-instance ledger for the evacuation. Stable authored identifiers make repeated physics callbacks harmless.
/// This deliberately has no save serialization: loading/replaying the scene constructs a fresh ledger.
/// </summary>
public sealed class WildfireProgress
{
    private readonly HashSet<string> _done = new();
    private readonly HashSet<string> _rewarded = new();
    /// <summary>Number of civilian groups that have actually arrived in their shelters.</summary>
    public int Rescued => Count("rescue:");
    /// <summary>Number of individually destroyed biometric spires.</summary>
    public int Spires => Count("spire:");
    /// <summary>Returns whether an objective, encounter, relay or permanent barrier has completed.</summary>
    public bool Has(string id) => _done.Contains(id);
    /// <summary>Records an authored event once; true tells callers to apply its one-time side effects.</summary>
    public bool Complete(string id) => !string.IsNullOrWhiteSpace(id) && _done.Add(id);
    /// <summary>Prevents farming score when an incomplete encounter restarts after recovery.</summary>
    public bool Reward(string memberId) => _rewarded.Add(memberId);
    /// <summary>All mandatory objectives must be satisfied; merely reaching the portal never completes the mission.</summary>
    public bool CanExtract => new[] { "rescue:residential", "rescue:overpass", "rescue:extraction",
        "spire:street", "spire:tower", "spire:courtyard", "battery:core", "barrier:final" }.All(Has);
    /// <summary>Counts only events in the specified namespace.</summary>
    private int Count(string prefix) => _done.Count(id => id.StartsWith(prefix, System.StringComparison.Ordinal));
}
