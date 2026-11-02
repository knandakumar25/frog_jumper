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
    public int Rescued => new[] { "rescue:residential", "rescue:overpass", "rescue:extraction" }.Count(Has);
    /// <summary>Number of individually destroyed biometric spires.</summary>
    public int Spires => new[] { "spire:street", "spire:tower", "spire:courtyard" }.Count(Has);
    /// <summary>Returns whether an objective, encounter, relay or permanent barrier has completed.</summary>
    public bool Has(string id) => _done.Contains(id);
    /// <summary>Records an authored event once; true tells callers to apply its one-time side effects.</summary>
    public bool Complete(string id) => !string.IsNullOrWhiteSpace(id) && CanComplete(id) && _done.Add(id);
    /// <summary>Rejects premature objective callbacks. Fires and physical shelter arrival are additionally checked by the scene controller.</summary>
    public bool CanComplete(string id) => id switch
    {
        "spire:street" => Has("encounter:spire-street"),
        "spire:tower" => Has("encounter:spire-tower"),
        "spire:courtyard" => Has("encounter:spire-courtyard"),
        "route:residential" => Has("encounter:residential-route"),
        "route:overpass" => Has("encounter:overpass-exit"),
        "route:extraction" => Has("encounter:extraction-guard") && Has("barrier:final") && Has("battery:core"),
        "rescue:residential" => Has("route:residential"),
        "rescue:overpass" => Has("route:overpass"),
        "rescue:extraction" => Has("route:extraction"),
        "battery:core" => Has("relay:west") && Has("relay:east"),
        _ => true
    };
    /// <summary>Prevents farming score when an incomplete encounter restarts after recovery.</summary>
    public bool Reward(string memberId) => _rewarded.Add(memberId);
    /// <summary>All mandatory objectives must be satisfied; merely reaching the portal never completes the mission.</summary>
    public bool CanExtract => new[] { "rescue:residential", "rescue:overpass", "rescue:extraction",
        "spire:street", "spire:tower", "spire:courtyard", "battery:core", "barrier:final" }.All(Has);
}
