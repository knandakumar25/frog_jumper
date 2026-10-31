namespace AxiomForce.Tests;

/// <summary>Exact dependency, idempotency and fresh-run tests without a running Godot engine.</summary>
[TestFixture]
public sealed class NeonRustProgressTests
{
    /// <summary>Orders all events as the authored route does; this also documents the valid progression contract.</summary>
    private static IEnumerable<string> Route()
    {
        yield return "barrier:insertion";
        for (int s = 1; s <= 8; s++)
        {
            for (int g = 1; g <= NeonRustProgress.SectorGroups[s - 1]; g++)
                yield return $"encounter:s{s}-{g}";
            foreach (string id in s switch
            {
                2 => new[] { "shipment:freight" },
                3 => new[] { "shipment:runoff", "recruit:aura", "lesson:aura" },
                5 => new[] { "shipment:harmonic", "uplink:west" },
                6 => new[] { "uplink:east" },
                7 => new[] { "coupler:west", "coupler:east", "relay:central", "recruit:mirage", "lesson:mirage" },
                _ => Array.Empty<string>()
            }) yield return id;
        }
    }
    /// <summary>Data cases reject extraction with any mandatory event absent, including every finite formation.</summary>
    public static IEnumerable<string> Required => Route();
    /// <summary>Unknown names, wrong capitalization and prefix lookalikes cannot inflate counters or unlock the relay.</summary>
    [Test]
    public void UnknownOrPrematureEventsAreRejected()
    {
        var p = new NeonRustProgress();
        foreach (string id in new[] { "shipment:fake", "Shipment:freight", "uplink:fake", "relay:central", "recruit:mirage", "lesson:aura" })
            Assert.That(p.Complete(id), Is.False, id);
        Assert.That(p.Shipments, Is.Zero);
        Assert.That(p.Uplinks, Is.Zero);
        Assert.That(p.CommsBlackedOut, Is.False);
        Assert.That(NeonRustProgress.Encounters.Count(), Is.EqualTo(30));
    }
    /// <summary>Missing an exact prerequisite must reject completion, regardless of other completed namespaces.</summary>
    [TestCaseSource(nameof(Required))]
    public void ExtractionRejectsEachMissingEvent(string missing)
    {
        var p = new NeonRustProgress();
        foreach (string id in Route()) if (id != missing) p.Complete(id);
        Assert.That(p.CanExtract, Is.False, missing);
        // Retry the full route after restoring the missing event: dependencies that were premature now become valid.
        foreach (string id in Route()) p.Complete(id);
        Assert.That(p.CanExtract, Is.True);
        Assert.That(p.Shipments, Is.EqualTo(3));
        Assert.That(p.Uplinks, Is.EqualTo(2));
    }
    /// <summary>Repeated signals and recovery restarts retain completion and prevent score farming; Replay resets both.</summary>
    [Test]
    public void RepeatedSignalsAndRewardsAreIdempotent()
    {
        var p = new NeonRustProgress();
        foreach (string id in Route()) { Assert.That(p.Complete(id), Is.True, id); Assert.That(p.Complete(id), Is.False); }
        Assert.That(p.Reward("s1-1:0"), Is.True);
        Assert.That(p.Reward("s1-1:0"), Is.False);
        var replay = new NeonRustProgress();
        Assert.That(replay.Reward("s1-1:0"), Is.True);
        Assert.That(replay.CanExtract, Is.False);
        Assert.That(replay.Has("lesson:mirage"), Is.False);
    }
    /// <summary>Guidance is derived from event state, with a stable localization key and speaker-labelled fallback.</summary>
    [Test]
    public void ObjectiveFollowsDependencies()
    {
        var p = new NeonRustProgress();
        Assert.That(p.Objective.Key, Is.EqualTo("BREACH"));
        p.Complete("barrier:insertion");
        Assert.That(p.Objective.Key, Is.EqualTo("FREIGHT"));
        Assert.That(p.Objective.Text, Does.StartWith("AXIOM-7:"));
    }
}
