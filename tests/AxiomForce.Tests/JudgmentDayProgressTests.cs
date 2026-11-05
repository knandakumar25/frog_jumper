using System;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 12 (Judgment Day - Citadel Summit &amp; Void Singularity Chamber) narrative gating, formation prerequisites,
/// power siphon coolant dousing, dark-matter siphon clamp demolition, siphon grid neutralization,
/// high-voltage laser curtain phasing, cosmic radiation vent absorption, barrier corridor clearance,
/// harmonic acoustic lock resonance, vertical cathedral vault scaling, Void Commander boss progression,
/// core containment initiation, and campaign extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class JudgmentDayProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("siphon_coolant_doused")]
    [TestCase("siphon_clamp_shattered")]
    [TestCase("siphon_grid_neutralized")]
    [TestCase("laser_curtain_phased")]
    [TestCase("cosmic_vent_absorbed")]
    [TestCase("barrier_corridor_cleared")]
    [TestCase("harmonic_lock_resonated")]
    [TestCase("cathedral_vault_scaled")]
    [TestCase("void_commander_phase:1")]
    [TestCase("void_commander_phase:2")]
    [TestCase("void_commander_phase:3")]
    [TestCase("core_containment_initiated")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new JudgmentDayProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new JudgmentDayProgress();
        foreach (string objective in JudgmentDayProgress.Objectives)
        {
            foreach (string formation in JudgmentDayProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new JudgmentDayProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new JudgmentDayProgress();
        Assert.That(progress.Complete("ingress_secured"), Is.False);
        Assert.That(progress.Complete("encounter:ingress1"), Is.True);
        Assert.That(progress.Complete("ingress_secured"), Is.False);
        Assert.That(progress.Complete("encounter:ingress2"), Is.True);
        Assert.That(progress.Complete("ingress_secured"), Is.True);

        Assert.That(progress.Complete("siphon_coolant_doused"), Is.True);
        Assert.That(progress.Complete("siphon_clamp_shattered"), Is.True);
        Assert.That(progress.Complete("siphon_grid_neutralized"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new JudgmentDayProgress();
        Assert.That(progress.Reward("ingress1:0"), Is.True);
        Assert.That(progress.Reward("ingress1:0"), Is.False);
        Assert.That(progress.Reward("ingress1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("ingress1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 12 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission12AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(12));
        var entry = LevelCatalog.Get(12);
        Assert.That(entry.Number, Is.EqualTo(12));
        Assert.That(entry.Title, Is.EqualTo("Judgment Day"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level12.tscn"));

        var roster = OperativeRoster.ForLevelStart(12);
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
