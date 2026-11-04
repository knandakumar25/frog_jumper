namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 8 (Stratos Docks - Act III Opening) narrative gating, formation prerequisites,
/// fuel conduit breaches, scanner array clearance, Aero-Centurion boss encounter progression,
/// and VTOL gunship extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class StratosProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("cryo_fuel_douse")]
    [TestCase("titan_clamp_shatter")]
    [TestCase("fuel_conduits_secured")]
    [TestCase("scanner_grid_phase")]
    [TestCase("radar_resonance_disable")]
    [TestCase("scanner_array_cleared")]
    [TestCase("flak_bulwark_breach")]
    [TestCase("hangar_overlook_breach")]
    [TestCase("aero_guardian_phase:1")]
    [TestCase("aero_guardian_phase:2")]
    [TestCase("aero_guardian_phase:3")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new StratosProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new StratosProgress();
        foreach (string objective in StratosProgress.Objectives)
        {
            foreach (string formation in StratosProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new StratosProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new StratosProgress();
        Assert.That(progress.Complete("perimeter_ingress"), Is.False);
        Assert.That(progress.Complete("encounter:catwalk1"), Is.True);
        Assert.That(progress.Complete("perimeter_ingress"), Is.False);
        Assert.That(progress.Complete("encounter:catwalk2"), Is.True);
        Assert.That(progress.Complete("perimeter_ingress"), Is.True);

        Assert.That(progress.Complete("cryo_fuel_douse"), Is.False);
        Assert.That(progress.Complete("encounter:fuel1"), Is.True);
        Assert.That(progress.Complete("cryo_fuel_douse"), Is.True);

        Assert.That(progress.Complete("scanner_array_cleared"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new StratosProgress();
        Assert.That(progress.Reward("catwalk1:0"), Is.True);
        Assert.That(progress.Reward("catwalk1:0"), Is.False);
        Assert.That(progress.Reward("catwalk1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("catwalk1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 8 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission8AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(8));
        var entry = LevelCatalog.Get(8);
        Assert.That(entry.Number, Is.EqualTo(8));
        Assert.That(entry.Title, Is.EqualTo("Stratos Docks"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level8.tscn"));

        var roster = OperativeRoster.ForLevelStart(8);
        Assert.That(roster.Unlocked.Count, Is.EqualTo(6));
        Assert.That(roster.Available.Count, Is.EqualTo(6));
        Assert.That(roster.IsAvailable("Surya Prime"), Is.True, "Surya should be available in Mission 8");
        Assert.That(roster.IsAvailable("Oceanus"), Is.True, "Oceanus should be available in Mission 8");
        Assert.That(roster.IsAvailable("Apex Titan"), Is.True, "Titan should be available in Mission 8");
        Assert.That(roster.IsAvailable("Mirage Blade"), Is.True, "Mirage should be available in Mission 8");
        Assert.That(roster.IsAvailable("Aura Phoenicia"), Is.True, "Aura should be available in Mission 8");
        Assert.That(roster.IsAvailable("Cyber Shogun"), Is.True, "Shogun should be available in Mission 8");
    }
}
