namespace AxiomForce.Tests;

/// <summary>Engine-free mission ledger regression tests: duplicate callbacks, reward farming and extraction prerequisites.</summary>
[TestFixture]
public sealed class WildfireProgressTests
{
    /// <summary>Repeated objective events cannot count a family or spire more than once.</summary>
    [Test]
    public void RepeatedEventsAreIdempotent()
    {
        var progress = new WildfireProgress();
        progress.Complete("encounter:spire-street");
        progress.Complete("encounter:residential-route");
        progress.Complete("route:residential");
        Assert.That(progress.Complete("spire:street"), Is.True);
        Assert.That(progress.Complete("spire:street"), Is.False);
        progress.Complete("rescue:residential");
        progress.Complete("rescue:residential");
        Assert.That(progress.Spires, Is.EqualTo(1));
        Assert.That(progress.Rescued, Is.EqualTo(1));
    }

    /// <summary>Restarting an incomplete formation can never award a second kill reward for the same slot.</summary>
    [Test]
    public void RewardsSurviveIncompleteEncounterRetries()
    {
        var progress = new WildfireProgress();
        Assert.That(progress.Reward("road-parry:0"), Is.True);
        Assert.That(progress.Reward("road-parry:0"), Is.False);
        Assert.That(progress.Reward("road-parry:1"), Is.True);
        Assert.That(progress.Has("encounter:road-parry"), Is.False);
    }

    /// <summary>Each exact prerequisite matters, including the final wall and actual shelter arrival.</summary>
    [TestCase("rescue:residential")]
    [TestCase("rescue:overpass")]
    [TestCase("rescue:extraction")]
    [TestCase("spire:street")]
    [TestCase("spire:tower")]
    [TestCase("spire:courtyard")]
    [TestCase("battery:core")]
    [TestCase("barrier:final")]
    public void ExtractionRejectsEachMissingObjective(string missing)
    {
        var progress = new WildfireProgress();
        string[] required = { "rescue:residential", "rescue:overpass", "rescue:extraction", "spire:street",
            "spire:tower", "spire:courtyard", "battery:core", "barrier:final" };
        // Supply local safety first, then retry the ordered objective chain to settle dependencies.
        foreach (string id in new[] { "encounter:residential-route", "encounter:overpass-exit", "encounter:extraction-guard",
            "encounter:spire-street", "encounter:spire-tower", "encounter:spire-courtyard", "relay:west", "relay:east" }) progress.Complete(id);
        for (int retry = 0; retry < 3; retry++)
        {
            foreach (string id in required) if (id != missing) progress.Complete(id);
            foreach (string id in new[] { "route:residential", "route:overpass", "route:extraction" }) progress.Complete(id);
        }
        // Unrelated events with similar prefixes cannot substitute for an authored objective.
        progress.Complete("rescue:other");
        progress.Complete("spire:other");
        Assert.That(progress.CanExtract, Is.False);
        for (int retry = 0; retry < 3; retry++)
        {
            foreach (string id in required) progress.Complete(id);
            foreach (string id in new[] { "route:residential", "route:overpass", "route:extraction" }) progress.Complete(id);
        }
        Assert.That(progress.CanExtract, Is.True);
    }

    /// <summary>A replay/reload creates a clean ledger without altering the campaign save schema.</summary>
    [Test]
    public void FreshInstanceDoesNotInheritProgress()
    {
        var old = new WildfireProgress();
        old.Complete("battery:core");
        old.Reward("road-parry:0");
        var fresh = new WildfireProgress();
        Assert.That(fresh.Has("battery:core"), Is.False);
        Assert.That(fresh.Reward("road-parry:0"), Is.True);
        Assert.That(fresh.CanExtract, Is.False);
    }

    /// <summary>Defenders, relays and safe-route acceptance block premature objective callbacks without adding patrol requirements.</summary>
    [Test]
    public void PrematureObjectiveEventsCannotAdvanceProgress()
    {
        var p = new WildfireProgress();
        foreach (string id in new[] { "spire:street", "spire:tower", "spire:courtyard", "battery:core",
            "route:residential", "route:overpass", "route:extraction", "rescue:residential", "rescue:overpass", "rescue:extraction" })
            Assert.That(p.Complete(id), Is.False, id);
        p.Complete("encounter:residential-route");
        Assert.That(p.Complete("rescue:residential"), Is.False);
        Assert.That(p.Complete("route:residential"), Is.True);
        Assert.That(p.Complete("rescue:residential"), Is.True);
        Assert.That(p.Complete("rescue:residential"), Is.False);
        Assert.That(p.Has("encounter:road-parry"), Is.False);
    }
}
