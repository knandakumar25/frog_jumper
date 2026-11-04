using System;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Verifies Mission 10 (The Cyber-Nexus - Mainframe Core Infiltration) narrative gating, formation prerequisites,
/// liquid helium cooling vaults clearance, optical firewall bypass, neural matrix cipher resonance,
/// Nexus Overseer boss progression, quantum nodes override, and express ascent lift extraction conditions in pure NUnit.
/// </summary>
[TestFixture]
public class NexusProgressTests
{
    /// <summary>Premature objectives and unknown events must be rejected on a fresh ledger.</summary>
    [TestCase("helium_manifold_doused")]
    [TestCase("server_bracket_shattered")]
    [TestCase("cooling_vaults_secured")]
    [TestCase("laser_firewall_phased")]
    [TestCase("biometric_firewall_absorbed")]
    [TestCase("optical_conduits_secured")]
    [TestCase("cipher_resonance_bypassed")]
    [TestCase("neural_substation_breached")]
    [TestCase("nexus_overseer_phase:1")]
    [TestCase("nexus_overseer_phase:2")]
    [TestCase("nexus_overseer_phase:3")]
    [TestCase("quantum_nodes_overridden")]
    [TestCase("extract")]
    [TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new NexusProgress().Complete(id), Is.False);

    /// <summary>The full objective chain succeeds in order when prerequisites and encounters are resolved.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress = new NexusProgress();
        foreach (string objective in NexusProgress.Objectives)
        {
            foreach (string formation in NexusProgress.Formations)
            {
                if (progress.CanStartFormation(formation))
                    Assert.That(progress.Complete("encounter:" + formation), Is.True);
            }
            Assert.That(progress.Complete(objective), Is.True, $"Failed to complete {objective}");
            Assert.That(progress.Complete(objective), Is.False, $"Should reject duplicate {objective}");
        }
        Assert.That(progress.CanExtract, Is.True);
        Assert.That(new NexusProgress().CanExtract, Is.False, "Fresh ledger should not permit extraction");
    }

    /// <summary>Formation clearance cannot be skipped.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress = new NexusProgress();
        Assert.That(progress.Complete("ingress_confirmed"), Is.False);
        Assert.That(progress.Complete("encounter:ingress1"), Is.True);
        Assert.That(progress.Complete("ingress_confirmed"), Is.False);
        Assert.That(progress.Complete("encounter:ingress2"), Is.True);
        Assert.That(progress.Complete("ingress_confirmed"), Is.True);

        Assert.That(progress.Complete("helium_manifold_doused"), Is.False);
        Assert.That(progress.Complete("encounter:coolant1"), Is.True);
        Assert.That(progress.Complete("helium_manifold_doused"), Is.True);

        Assert.That(progress.Complete("optical_conduits_secured"), Is.False);
    }

    /// <summary>Rewards for slot kills are awarded exactly once per authored slot.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress = new NexusProgress();
        Assert.That(progress.Reward("ingress1:0"), Is.True);
        Assert.That(progress.Reward("ingress1:0"), Is.False);
        Assert.That(progress.Reward("ingress1:4"), Is.False);
        Assert.That(progress.Reward("unknown:0"), Is.False);
        Assert.That(progress.Reward("ingress1:1"), Is.True);
    }

    /// <summary>Confirms LevelCatalog registers Mission 10 and all 6 operatives are available.</summary>
    [Test]
    public void LevelCatalogRegistersMission10AndEnablesAllOperatives()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(10));
        var entry = LevelCatalog.Get(10);
        Assert.That(entry.Number, Is.EqualTo(10));
        Assert.That(entry.Title, Is.EqualTo("The Cyber-Nexus"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level10.tscn"));

        var roster = OperativeRoster.ForLevelStart(10);
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
