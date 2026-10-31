using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Engine-independent, scene-local sabotage ledger. Reloading constructs a clean run; recovery retains this instance.</summary>
public sealed class NeonRustProgress
{
    private readonly HashSet<string> _done = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rewards = new(StringComparer.Ordinal);
    /// <summary>Finite group counts in the eight authored sectors.</summary>
    public static IReadOnlyList<int> SectorGroups { get; } = Array.AsReadOnly(new[] { 3, 4, 3, 2, 4, 5, 5, 4 });
    /// <summary>All exact encounter identifiers, including the two relay reinforcement groups.</summary>
    public static IEnumerable<string> Encounters => SectorGroups.SelectMany((count, sector) =>
        Enumerable.Range(1, count).Select(group => $"encounter:s{sector + 1}-{group}"));
    /// <summary>Distinct hijacked shipments; similar prefixes cannot substitute for authored IDs.</summary>
    public int Shipments => new[] { "shipment:freight", "shipment:runoff", "shipment:harmonic" }.Count(Has);
    /// <summary>Distinct disabled uplinks.</summary>
    public int Uplinks => new[] { "uplink:west", "uplink:east" }.Count(Has);
    /// <summary>Compatibility state derived solely from the accepted central relay event.</summary>
    public bool CommsBlackedOut => Has("relay:central");
    /// <summary>Every prerequisite must still be present; a cached relay flag alone cannot enable extraction.</summary>
    public bool CanExtract => Has("barrier:insertion") && RelayReady && CommsBlackedOut && Has("recruit:mirage") && Has("lesson:mirage") && Encounters.All(Has);
    /// <summary>Central relay shielding requires all shipments, both uplinks, two couplers and finite reinforcements.</summary>
    public bool RelayReady => new[] { "shipment:freight", "shipment:runoff", "shipment:harmonic", "uplink:west",
        "uplink:east", "coupler:west", "coupler:east", "encounter:s7-4", "encounter:s7-5" }.All(Has);
    /// <summary>Exact event query, independent of world position.</summary>
    public bool Has(string id) => _done.Contains(id);
    /// <summary>Accepts known events only after their dependencies; repeated or premature signals have no side effects.</summary>
    public bool Complete(string id) => !_done.Contains(id) && CanComplete(id) && _done.Add(id);
    /// <summary>One score reward per member slot even when its incomplete group restarts.</summary>
    public bool Reward(string id) => !string.IsNullOrWhiteSpace(id) && _rewards.Add(id);
    /// <summary>Dependency contract shared by gate acceptance and objective tests.</summary>
    public bool CanComplete(string id) => id switch
    {
        "barrier:insertion" => true,
        "shipment:freight" => SectorDone(2),
        "shipment:runoff" => SectorDone(3) && Has("shipment:freight"),
        "recruit:aura" => Has("shipment:runoff"),
        "lesson:aura" => Has("recruit:aura"),
        "shipment:harmonic" => SectorDone(5) && Has("lesson:aura"),
        "uplink:west" => Has("shipment:harmonic"),
        "uplink:east" => SectorDone(6) && Has("uplink:west"),
        "coupler:west" => Has("uplink:east") && Has("encounter:s7-2"),
        "coupler:east" => Has("coupler:west") && Has("encounter:s7-3"),
        "relay:central" => RelayReady,
        "recruit:mirage" => CommsBlackedOut,
        "lesson:mirage" => Has("recruit:mirage"),
        _ => Encounters.Contains(id, StringComparer.Ordinal)
    };
    /// <summary>All finite defenders in a sector must be cleared before its shipment can be hijacked.</summary>
    private bool SectorDone(int sector) => Enumerable.Range(1, SectorGroups[sector - 1]).All(group => Has($"encounter:s{sector}-{group}"));
    /// <summary>First unmet instruction key and English fallback; guidance never depends on the player's coordinates.</summary>
    public (string Key, string Text) Objective => !Has("barrier:insertion") ? ("BREACH", "TITAN: Slam the collapsed wall; secure the scrapyard.") :
        !Has("shipment:freight") ? ("FREIGHT", "AXIOM-7: Clear freight defenders; Titan must release shipment one.") :
        !Has("shipment:runoff") ? ("RUNOFF", "OCEANUS: Clear maintenance; douse shipment two's vent.") :
        !Has("recruit:aura") ? ("AURA", "AXIOM-7: Meet Aura in harmonic access.") :
        !Has("lesson:aura") ? ("PRACTICE", "AURA: Open the practice sonic lock with Harmonic Pulse.") :
        !Has("shipment:harmonic") ? ("CARGO", "AURA: Clear the west gantries; pulse shipment three's restraint.") :
        !Has("uplink:west") ? ("WEST", "AXIOM-7: Pulse the defended west uplink.") :
        !Has("uplink:east") ? ("EAST", "AXIOM-7: Secure the tower ledges; pulse the east uplink.") :
        !Has("coupler:west") ? ("COUPLER_W", "AURA: Clear the approach and west coupler defenders; pulse the coupler.") :
        !Has("coupler:east") ? ("COUPLER_E", "AURA: Clear east coupler defenders; pulse the second coupler.") :
        !RelayReady ? ("REINFORCE", "AXIOM-7: Defeat both relay reinforcement groups.") :
        !CommsBlackedOut ? ("RELAY", "AURA: Pulse the exposed central relay. Black out the grid.") :
        !Has("recruit:mirage") ? ("MIRAGE", "AXIOM-7: Meet Mirage beyond the relay.") :
        !Has("lesson:mirage") ? ("PHASE", "MIRAGE: Face right beside the blue practice beam and use Phase Blink.") :
        ("ESCAPE", "MIRAGE: Clear the blind-grid formations; phase through separated curtains to extraction.");
}
