namespace AxiomForce.Tests;

/// <summary>Exact dependency, idempotency and fresh-run tests without a running Godot engine.</summary>
[TestFixture]
public sealed class NeonRustProgressTests
{
    /// <summary>Orders all events as the authored route does; this also documents the valid progression contract.</summary>
    private static IEnumerable<string> Route()
    {
        yield return "barrier:insertion";
        foreach (string id in new[] { "encounter:s2-4", "shipment:freight", "encounter:s3-3", "shipment:runoff",
            "recruit:aura", "lesson:aura", "encounter:s5-4", "shipment:harmonic", "uplink:west", "encounter:s6-5",
            "uplink:east", "encounter:s7-1", "encounter:s7-2", "coupler:west", "encounter:s7-3", "coupler:east",
            "encounter:s7-4", "encounter:s7-5", "relay:central", "recruit:mirage", "lesson:mirage" }) yield return id;
    }
    /// <summary>Data cases reject extraction with any mandatory event absent, including each local defense.</summary>
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

    /// <summary>All thirty formations remain authored, but every optional survivor may be bypassed without blocking extraction.</summary>
    [Test]
    public void OptionalFormationsNeverBlockExtraction()
    {
        var p = new NeonRustProgress();
        foreach (string id in Route()) Assert.That(p.Complete(id), Is.True, id);
        Assert.That(NeonRustProgress.RequiredEncounters, Has.Count.EqualTo(9));
        Assert.That(NeonRustProgress.Encounters.Except(NeonRustProgress.RequiredEncounters).All(id => !p.Has(id)), Is.True);
        Assert.That(p.CanExtract, Is.True);
        Assert.That(p.Objective.Text, Does.Contain("optional"));
        Assert.That(p.Complete("encounter:s8-1"), Is.True);
        Assert.That(p.Reward("s8-1:0"), Is.True);
        Assert.That(p.Reward("s8-1:0"), Is.False);
    }

    /// <summary>Reinforcement callbacks cannot jump ahead of couplers or of the first wave.</summary>
    [Test]
    public void ReinforcementsRejectPrematureCompletion()
    {
        var p = new NeonRustProgress();
        Assert.That(p.Complete("encounter:s7-4"), Is.False);
        Assert.That(p.Complete("encounter:s7-5"), Is.False);
        Assert.That(p.CanExtract, Is.False);
    }

    /// <summary>Missing local defenses reject their exact target even when every unrelated formation and prerequisite is offered.</summary>
    [TestCase("encounter:s2-4", "shipment:freight")]
    [TestCase("encounter:s3-3", "shipment:runoff")]
    [TestCase("encounter:s5-4", "shipment:harmonic")]
    [TestCase("encounter:s6-5", "uplink:east")]
    [TestCase("encounter:s7-1", "coupler:west")]
    [TestCase("encounter:s7-2", "coupler:west")]
    [TestCase("encounter:s7-3", "coupler:east")]
    [TestCase("encounter:s7-4", "relay:central")]
    [TestCase("encounter:s7-5", "relay:central")]
    public void LocalDefendersBlockTheirObjective(string defense, string target)
    {
        var p = new NeonRustProgress();
        foreach (string id in NeonRustProgress.Encounters.Except(new[] { defense, "encounter:s7-4", "encounter:s7-5" })) p.Complete(id);
        foreach (string id in Route()) if (id != defense) p.Complete(id);
        Assert.That(p.Has(target), Is.False);
        Assert.That(p.CanComplete(target), Is.False);
    }
}
