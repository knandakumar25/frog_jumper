namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 7 (The Foundry Forge - Act II Finale) narrative gating, formation prerequisites,
/// turbine overrides, arc phase clearance, boss encounter progression, and extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class FoundryProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("coolant_douse")]
    [TestCase("valve_shatter")]
    [TestCase("turbines_overloaded")]
    [TestCase("arc_phase")]
    [TestCase("resonance_disable")]
    [TestCase("conduits_severed")]
    [TestCase("radiation_bulwark")]
    [TestCase("core_overlook_breach")]
    [TestCase("boss_phase:1")]
    [TestCase("boss_phase:2")]
    [TestCase("boss_phase:3")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new FoundryProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new FoundryProgress();
        foreach (string objective in FoundryProgress.Objectives)
        {
            foreach (string formation in FoundryProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new FoundryProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new FoundryProgress();
        Assert.That(progress.Complete("ingress_breach"), Is.False);
        Assert.That(progress.Complete("encounter:ingress1"), Is.True);
        Assert.That(progress.Complete("ingress_breach"), Is.False);
        Assert.That(progress.Complete("encounter:ingress2"), Is.True);
        Assert.That(progress.Complete("ingress_breach"), Is.True);

        Assert.That(progress.Complete("coolant_douse"), Is.False);
        Assert.That(progress.Complete("encounter:coolant1"), Is.True);
        Assert.That(progress.Complete("coolant_douse"), Is.True);

        Assert.That(progress.Complete("conduits_severed"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new FoundryProgress();
        Assert.That(progress.Reward("ingress1:0"), Is.True);
        Assert.That(progress.Reward("ingress1:0"), Is.False);
        Assert.That(progress.Reward("ingress1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("ingress1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 7 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission7AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(7));
        var entry = LevelCatalog.Get(7);
        Assert.That(entry.Number, Is.EqualTo(7));
        Assert.That(entry.Title, Is.EqualTo("The Foundry Forge"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level7.tscn"));

        var roster = OperativeRoster.ForLevelStart(7);
        Assert.That(roster.Unlocked.Count, Is.EqualTo(6));
        Assert.That(roster.Available.Count, Is.EqualTo(6));
        Assert.That(roster.IsAvailable("Surya Prime"), Is.True, "Surya should be available in Mission 7");
        Assert.That(roster.IsAvailable("Oceanus"), Is.True, "Oceanus should be available in Mission 7");
        Assert.That(roster.IsAvailable("Apex Titan"), Is.True, "Titan should be available in Mission 7");
    }
}
