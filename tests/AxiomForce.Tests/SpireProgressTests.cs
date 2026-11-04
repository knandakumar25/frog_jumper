namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 9 (Spire Outer Hull - High-Mobility Vertical Ascent) narrative gating, formation prerequisites,
/// high-altitude catwalk breaches, pylon ascent clearance, Spire Sentinel aerial mechanoid boss progression,
/// and core airlock extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class SpireProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("coolant_shaft_douse")]
    [TestCase("clamp_brace_shatter")]
    [TestCase("exhaust_conduits_cleared")]
    [TestCase("laser_grid_phase")]
    [TestCase("flak_barrier_deflect")]
    [TestCase("battery_deck_cleared")]
    [TestCase("relay_resonance_disable")]
    [TestCase("elevator_ascent_breach")]
    [TestCase("spire_sentinel_phase:1")]
    [TestCase("spire_sentinel_phase:2")]
    [TestCase("spire_sentinel_phase:3")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new SpireProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new SpireProgress();
        foreach (string objective in SpireProgress.Objectives)
        {
            foreach (string formation in SpireProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new SpireProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new SpireProgress();
        Assert.That(progress.Complete("gantry_ingress"), Is.False);
        Assert.That(progress.Complete("encounter:gantry1"), Is.True);
        Assert.That(progress.Complete("gantry_ingress"), Is.False);
        Assert.That(progress.Complete("encounter:gantry2"), Is.True);
        Assert.That(progress.Complete("gantry_ingress"), Is.True);

        Assert.That(progress.Complete("coolant_shaft_douse"), Is.False);
        Assert.That(progress.Complete("encounter:exhaust1"), Is.True);
        Assert.That(progress.Complete("coolant_shaft_douse"), Is.True);

        Assert.That(progress.Complete("battery_deck_cleared"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new SpireProgress();
        Assert.That(progress.Reward("gantry1:0"), Is.True);
        Assert.That(progress.Reward("gantry1:0"), Is.False);
        Assert.That(progress.Reward("gantry1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("gantry1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 9 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission9AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(9));
        var entry = LevelCatalog.Get(9);
        Assert.That(entry.Number, Is.EqualTo(9));
        Assert.That(entry.Title, Is.EqualTo("Spire Outer Hull"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level9.tscn"));

        var roster = OperativeRoster.ForLevelStart(9);
        Assert.That(roster.Unlocked.Count, Is.EqualTo(6));
        Assert.That(roster.Available.Count, Is.EqualTo(6));
        Assert.That(roster.IsAvailable("Surya Prime"), Is.True, "Surya should be available in Mission 9");
        Assert.That(roster.IsAvailable("Oceanus"), Is.True, "Oceanus should be available in Mission 9");
        Assert.That(roster.IsAvailable("Apex Titan"), Is.True, "Titan should be available in Mission 9");
        Assert.That(roster.IsAvailable("Mirage Blade"), Is.True, "Mirage should be available in Mission 9");
        Assert.That(roster.IsAvailable("Aura Phoenicia"), Is.True, "Aura should be available in Mission 9");
        Assert.That(roster.IsAvailable("Cyber Shogun"), Is.True, "Shogun should be available in Mission 9");
    }
}
