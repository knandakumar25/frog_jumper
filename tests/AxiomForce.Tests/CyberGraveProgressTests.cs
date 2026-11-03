namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 6 (Cyber-Grave Labs) narrative gating, formation prerequisites,
/// stasis pod liberation, reward idempotency, and extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class CyberGraveProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("pod:1")]
    [TestCase("pod:2")]
    [TestCase("incubation_cleared")]
    [TestCase("coolant_override")]
    [TestCase("resonance_bypass")]
    [TestCase("grid_phase")]
    [TestCase("siphon_contain")]
    [TestCase("core_defense:1")]
    [TestCase("core_defense:2")]
    [TestCase("core_defense:3")]
    [TestCase("vault_override")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new CyberGraveProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new CyberGraveProgress();
        foreach (string objective in CyberGraveProgress.Objectives)
        {
            foreach (string formation in CyberGraveProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new CyberGraveProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new CyberGraveProgress();
        Assert.That(progress.Complete("sump_breach"), Is.False);
        Assert.That(progress.Complete("encounter:sump1"), Is.True);
        Assert.That(progress.Complete("sump_breach"), Is.False);
        Assert.That(progress.Complete("encounter:sump2"), Is.True);
        Assert.That(progress.Complete("sump_breach"), Is.True);

        Assert.That(progress.Complete("pod:1"), Is.False);
        Assert.That(progress.Complete("encounter:incubate1"), Is.True);
        Assert.That(progress.Complete("pod:1"), Is.True);

        Assert.That(progress.Complete("coolant_override"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new CyberGraveProgress();
        Assert.That(progress.Reward("sump1:0"), Is.True);
        Assert.That(progress.Reward("sump1:0"), Is.False);
        Assert.That(progress.Reward("sump1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("sump1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 6 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission6AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(6));
        var entry = LevelCatalog.Get(6);
        Assert.That(entry.Number, Is.EqualTo(6));
        Assert.That(entry.Title, Is.EqualTo("Cyber-Grave Labs"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level6.tscn"));

        var roster = OperativeRoster.ForLevelStart(6);
        Assert.That(roster.Unlocked.Count, Is.EqualTo(6));
        Assert.That(roster.Available.Count, Is.EqualTo(6));
        Assert.That(roster.IsAvailable("Surya Prime"), Is.True, "Surya should be available in Mission 6");
    }
}
