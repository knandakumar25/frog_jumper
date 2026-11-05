using System;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 11 (The Apex Bastion - Stratospheric Summit Fortress) narrative gating, formation prerequisites,
/// plasma flak coolant dousing, titan mooring clamp demolition, perimeter flak battery neutralization,
/// high-voltage laser curtain phasing, thermal vent absorption, lower redoubt clearance,
/// acoustic security lock resonance, vertical hydraulic elevator scaling, Bastion Praetor boss progression,
/// citadel blast doors breach, and throne room extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class ApexBastionProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("flak_coolant_doused")]
    [TestCase("titan_clamp_shattered")]
    [TestCase("flak_battery_neutralized")]
    [TestCase("laser_curtain_phased")]
    [TestCase("thermal_vent_absorbed")]
    [TestCase("lower_redoubt_cleared")]
    [TestCase("acoustic_lock_resonated")]
    [TestCase("elevator_shaft_scaled")]
    [TestCase("bastion_praetor_phase:1")]
    [TestCase("bastion_praetor_phase:2")]
    [TestCase("bastion_praetor_phase:3")]
    [TestCase("citadel_blast_doors_breached")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new ApexBastionProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new ApexBastionProgress();
        foreach (string objective in ApexBastionProgress.Objectives)
        {
            foreach (string formation in ApexBastionProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new ApexBastionProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new ApexBastionProgress();
        Assert.That(progress.Complete("staging_secured"), Is.False);
        Assert.That(progress.Complete("encounter:summit1"), Is.True);
        Assert.That(progress.Complete("staging_secured"), Is.False);
        Assert.That(progress.Complete("encounter:summit2"), Is.True);
        Assert.That(progress.Complete("staging_secured"), Is.True);

        Assert.That(progress.Complete("flak_coolant_doused"), Is.False);
        Assert.That(progress.Complete("encounter:flak1"), Is.True);
        Assert.That(progress.Complete("flak_coolant_doused"), Is.True);

        Assert.That(progress.Complete("lower_redoubt_cleared"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new ApexBastionProgress();
        Assert.That(progress.Reward("summit1:0"), Is.True);
        Assert.That(progress.Reward("summit1:0"), Is.False);
        Assert.That(progress.Reward("summit1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("summit1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 11 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission11AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(11));
        var entry = LevelCatalog.Get(11);
        Assert.That(entry.Number, Is.EqualTo(11));
        Assert.That(entry.Title, Is.EqualTo("The Apex Bastion"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level11.tscn"));

        var roster = OperativeRoster.ForLevelStart(11);
        Assert.That(roster.Unlocked.Count, Is.EqualTo(6));
        Assert.That(roster.Available.Count, Is.EqualTo(6));
        Assert.That(roster.IsAvailable("Cyber Shogun"), Is.True);
        Assert.That(roster.IsAvailable("Surya Prime"), Is.True);
        Assert.That(roster.IsAvailable("Oceanus"), Is.True);
        Assert.That(roster.IsAvailable("Apex Titan"), Is.True);
        Assert.That(roster.IsAvailable("Aura Phoenicia"), Is.True);
        Assert.That(roster.IsAvailable("Mirage Blade"), Is.True);
    }
}
