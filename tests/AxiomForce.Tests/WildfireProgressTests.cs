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
        foreach (string id in required)
            if (id != missing) progress.Complete(id);
        // Unrelated events with similar prefixes cannot substitute for an authored objective.
        progress.Complete("rescue:other");
        progress.Complete("spire:other");
        Assert.That(progress.CanExtract, Is.False);
        progress.Complete(missing);
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
}
