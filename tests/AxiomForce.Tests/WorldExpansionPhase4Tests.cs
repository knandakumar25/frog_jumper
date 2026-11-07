using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit tests for Neo-Kyoto Living Resistance World Phase 4: Endgame and Final Acceptance.
/// Verifies post-Mission 12 liberated state variants, reconstruction tasks, remnant incidents,
/// archive simulations, operation challenge modifiers, mastery evaluations, accessibility profiles,
/// and performance budgets.
/// </summary>
[TestFixture]
public class WorldExpansionPhase4Tests
{
    [Test]
    public void Catalog_ContainsAllSixteenRegionalReconstructionTasks()
    {
        Assert.That(EndgameContentCatalog.ReconstructionTasks.Count, Is.EqualTo(16));

        var coveredRegions = new HashSet<int>();
        foreach (var task in EndgameContentCatalog.ReconstructionTasks)
        {
            Assert.That(task.Id, Does.StartWith("recon_"));
            Assert.That(task.Name, Is.Not.Empty);
            Assert.That(task.Description, Is.Not.Empty);
            Assert.That(task.LeadFaction, Is.Not.Empty);
            Assert.That(task.SparksReward, Is.GreaterThan(0));
            Assert.That(task.CreditsReward, Is.GreaterThan(0));
            Assert.That(task.UnlockedAmenity, Does.StartWith("service_"));

            coveredRegions.Add(task.RegionId);
        }

        // Must cover all 16 regions (0 to 15)
        for (int r = 0; r < 16; r++)
        {
            Assert.That(coveredRegions.Contains(r), Is.True, $"Region {r} is missing a reconstruction task.");
        }
    }

    [Test]
    public void Catalog_ContainsEightCorpoRemnantIncidents()
    {
        Assert.That(EndgameContentCatalog.RemnantIncidents.Count, Is.EqualTo(8));

        string[] validOperatives = {
            "Cyber Shogun",
            "Surya Prime",
            "Oceanus",
            "Mirage Blade",
            "Apex Titan",
            "Aura Phoenicia"
        };

        foreach (var incident in EndgameContentCatalog.RemnantIncidents)
        {
            Assert.That(incident.Id, Does.StartWith("remnant_"));
            Assert.That(incident.Name, Is.Not.Empty);
            Assert.That(incident.ThreatLevel, Is.InRange(1, 5));
            Assert.That(incident.SparksReward, Is.GreaterThan(0));
            Assert.That(incident.CreditsReward, Is.GreaterThan(0));
            Assert.That(validOperatives, Does.Contain(incident.RecommendedOperative));
        }
    }

    [Test]
    public void Catalog_ContainsSixArchiveCombatSimulationsCoveringAllOperatives()
    {
        Assert.That(EndgameContentCatalog.ArchiveSimulations.Count, Is.EqualTo(6));

        var operativesCovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sim in EndgameContentCatalog.ArchiveSimulations)
        {
            Assert.That(sim.Id, Does.StartWith("sim_"));
            Assert.That(sim.Name, Is.Not.Empty);
            Assert.That(sim.Description, Is.Not.Empty);
            Assert.That(sim.MutatorRule, Is.Not.Empty);
            Assert.That(sim.CreditsReward, Is.GreaterThanOrEqualTo(500));

            operativesCovered.Add(sim.OperativeId);
        }

        Assert.That(operativesCovered.Contains("Cyber Shogun"), Is.True);
        Assert.That(operativesCovered.Contains("Surya Prime"), Is.True);
        Assert.That(operativesCovered.Contains("Oceanus"), Is.True);
        Assert.That(operativesCovered.Contains("Mirage Blade"), Is.True);
        Assert.That(operativesCovered.Contains("Apex Titan"), Is.True);
        Assert.That(operativesCovered.Contains("Aura Phoenicia"), Is.True);
    }

    [Test]
    public void Catalog_ContainsFiveOperationChallengeModifiers()
    {
        Assert.That(EndgameContentCatalog.Modifiers.Count, Is.EqualTo(5));

        string[] expectedIds = {
            "op_mod_ironman",
            "op_mod_overclocked",
            "op_mod_glass_vanguard",
            "op_mod_emp_atmosphere",
            "op_mod_axiom_protocol"
        };

        foreach (var id in expectedIds)
        {
            var mod = EndgameContentCatalog.Modifiers.FirstOrDefault(m => m.Id == id);
            Assert.That(mod, Is.Not.Null, $"Modifier {id} missing.");
            Assert.That(mod!.ScoreMultiplier, Is.GreaterThan(1.0f));
            Assert.That(mod.Description, Is.Not.Empty);
        }
    }

    [Test]
    public void OperationMastery_EvaluatesRanksAndMultipliersCorrectly()
    {
        // Bronze Rank (slow clear, no modifiers)
        var bronzeResult = EndgameContentCatalog.EvaluateMastery(
            "side_op_harbor_slipway",
            clearTimeSeconds: 300f,
            targetGoldTimeSeconds: 120f,
            activeModifierIds: Array.Empty<string>(),
            tookDamage: true
        );
        Assert.That(bronzeResult.Rank, Is.EqualTo(OperationMasteryRank.Bronze));
        Assert.That(bronzeResult.CreditsAwarded, Is.EqualTo(300));
        Assert.That(bronzeResult.UnlockedCosmetic, Is.Null);

        // Silver Rank (cleared within 1.5x gold time)
        var silverResult = EndgameContentCatalog.EvaluateMastery(
            "side_op_harbor_slipway",
            clearTimeSeconds: 150f,
            targetGoldTimeSeconds: 120f,
            activeModifierIds: Array.Empty<string>(),
            tookDamage: true
        );
        Assert.That(silverResult.Rank, Is.EqualTo(OperationMasteryRank.Silver));
        Assert.That(silverResult.CreditsAwarded, Is.EqualTo(500));

        // Gold Rank (cleared within gold time with 1 modifier)
        var goldResult = EndgameContentCatalog.EvaluateMastery(
            "side_op_harbor_slipway",
            clearTimeSeconds: 110f,
            targetGoldTimeSeconds: 120f,
            activeModifierIds: new[] { "op_mod_overclocked" },
            tookDamage: true
        );
        Assert.That(goldResult.Rank, Is.EqualTo(OperationMasteryRank.Gold));
        Assert.That(goldResult.CreditsAwarded, Is.EqualTo(800));
        Assert.That(goldResult.UnlockedCosmetic, Does.StartWith("livery_gold_"));
        Assert.That(goldResult.FinalScore, Is.GreaterThan(goldResult.BaseScore));

        // Platinum Rank (cleared within gold time with 2+ modifiers and no damage taken)
        var platinumResult = EndgameContentCatalog.EvaluateMastery(
            "side_op_harbor_slipway",
            clearTimeSeconds: 100f,
            targetGoldTimeSeconds: 120f,
            activeModifierIds: new[] { "op_mod_ironman", "op_mod_glass_vanguard" },
            tookDamage: false
        );
        Assert.That(platinumResult.Rank, Is.EqualTo(OperationMasteryRank.Platinum));
        Assert.That(platinumResult.CreditsAwarded, Is.EqualTo(1200));
        Assert.That(platinumResult.UnlockedCosmetic, Does.StartWith("skin_platinum_"));
        Assert.That(platinumResult.FinalScore, Is.GreaterThan(goldResult.FinalScore));
    }

    [Test]
    public void AccessibilityProfile_ClampsAndCalculatesScalingCorrectly()
    {
        var profile = new AccessibilityProfile();

        // Check defaults
        Assert.That(profile.HighContrastTelegraphs, Is.False);
        Assert.That(profile.ParryTimingWindowScale, Is.EqualTo(1.0f));
        Assert.That(profile.ScreenShakeScale, Is.EqualTo(1.0f));
        Assert.That(profile.PuzzleAssist, Is.EqualTo(PuzzleAssistance.Standard));
        Assert.That(profile.GlyphStyle, Is.EqualTo(ControllerGlyphStyle.AutoDetect));

        // Clamping parry scale (1.0 to 2.5)
        profile.ParryTimingWindowScale = 0.5f;
        Assert.That(profile.ParryTimingWindowScale, Is.EqualTo(1.0f));
        profile.ParryTimingWindowScale = 3.0f;
        Assert.That(profile.ParryTimingWindowScale, Is.EqualTo(2.5f));
        Assert.That(profile.CalculateParryWindow(0.2f), Is.EqualTo(0.5f).Within(0.001f));

        // Clamping screen shake scale (0.0 to 1.0)
        profile.ScreenShakeScale = -0.5f;
        Assert.That(profile.ScreenShakeScale, Is.EqualTo(0.0f));
        profile.ScreenShakeScale = 1.5f;
        Assert.That(profile.ScreenShakeScale, Is.EqualTo(1.0f));
        profile.ScreenShakeScale = 0.0f;
        Assert.That(profile.CalculateScreenShake(10.0f), Is.EqualTo(0.0f));
    }

    [Test]
    public void WorldPerformanceBudget_ValidatesEntityAndFrameIntervalThresholds()
    {
        // Within budget
        Assert.That(WorldPerformanceBudget.IsWithinBudget(16, 8, 4), Is.True);
        Assert.That(WorldPerformanceBudget.IsWithinBudget(10, 5, 2), Is.True);

        // Exceeding civilian limit
        Assert.That(WorldPerformanceBudget.IsWithinBudget(17, 8, 4), Is.False);

        // Exceeding hostile combatant limit
        Assert.That(WorldPerformanceBudget.IsWithinBudget(16, 9, 4), Is.False);

        // Exceeding moving vehicle limit
        Assert.That(WorldPerformanceBudget.IsWithinBudget(16, 8, 5), Is.False);

        // Frame interval thresholds
        Assert.That(WorldPerformanceBudget.IsAcceptableFrameInterval(16.6f), Is.True);
        Assert.That(WorldPerformanceBudget.IsAcceptableFrameInterval(33.0f), Is.True);
        Assert.That(WorldPerformanceBudget.IsAcceptableFrameInterval(35.0f), Is.False);
    }

    [Test]
    public void DistrictState_AllSixteenRegionsTransitionToLiberatedPostMissionTwelve()
    {
        for (int r = 0; r < 16; r++)
        {
            var postFinaleEra = DistrictState.DetermineEra(r, 13);
            Assert.That(postFinaleEra, Is.EqualTo(DistrictEra.Liberated), $"Region {r} should be Liberated after Mission 12.");
        }
    }
}
