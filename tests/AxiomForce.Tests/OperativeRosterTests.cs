namespace AxiomForce.Tests;

/// <summary>Verifies the progression rules: who is unlocked when, swapping, lessons and the level catalog.</summary>
[TestFixture]
public class OperativeRosterTests
{
    /// <summary>A new campaign starts with only the story's lead.</summary>
    [Test]
    public void MissionOne_StartsWithOnlyTheStarter()
    {
        var roster = OperativeRoster.ForLevelStart(1);
        Assert.That(roster.Unlocked, Is.EqualTo(new[] { "Cyber Shogun" }));
        Assert.That(roster.Active, Is.EqualTo("Cyber Shogun"));
    }

    /// <summary>Operatives join on the schedule in the catalog and are never lost later.</summary>
    [TestCase(2, 1)]
    [TestCase(3, 3)]
    [TestCase(4, 5)]
    [TestCase(5, 6)]
    public void SquadGrowsWithMissionNumber(int mission, int expectedCount) =>
        Assert.That(OperativeRoster.ForLevelStart(mission).Unlocked, Has.Count.EqualTo(expectedCount));

    /// <summary>Unlock fires once, aliases resolve to the registry name, and the event reports it.</summary>
    [Test]
    public void Unlock_IsIdempotentAndRaisesOnce()
    {
        var roster = OperativeRoster.ForLevelStart(2);
        int raised = 0;
        roster.OperativeUnlocked += _ => raised++;
        Assert.That(roster.Unlock("oceanus"), Is.True);
        Assert.That(roster.Unlock("Oceanus"), Is.False);
        Assert.That(raised, Is.EqualTo(1));
        Assert.That(roster.IsUnlocked("Kiana"), Is.True);
    }

    /// <summary>A locked operative can never become active.</summary>
    [Test]
    public void SetActive_RefusesLockedOperatives()
    {
        var roster = OperativeRoster.ForLevelStart(1);
        Assert.That(roster.SetActive("Apex Titan"), Is.False);
        Assert.That(roster.Active, Is.EqualTo("Cyber Shogun"));
    }

    /// <summary>Cycling wraps around only the unlocked operatives, in roster order, both directions.</summary>
    [Test]
    public void Cycle_WrapsThroughUnlockedOnly()
    {
        var roster = OperativeRoster.ForLevelStart(3); // Shogun, Oceanus, Titan
        Assert.That(roster.Cycle(1), Is.EqualTo("Oceanus"));
        Assert.That(roster.Cycle(1), Is.EqualTo("Apex Titan"));
        Assert.That(roster.Cycle(1), Is.EqualTo("Cyber Shogun"));
        Assert.That(roster.Cycle(-1), Is.EqualTo("Apex Titan"));
    }

    /// <summary>With a single operative there is nothing to swap to.</summary>
    [Test]
    public void Cycle_WithOneOperativeStaysPut() =>
        Assert.That(OperativeRoster.ForLevelStart(1).Cycle(1), Is.EqualTo("Cyber Shogun"));

    /// <summary>Lessons for earlier joiners count as seen; the starter's lesson belongs to mission 1.</summary>
    [Test]
    public void Lessons_AreSeenOnlyForEarlierJoiners()
    {
        Assert.That(OperativeRoster.ForLevelStart(1).LessonSeen("Cyber Shogun"), Is.False);
        var later = OperativeRoster.ForLevelStart(3);
        Assert.That(later.LessonSeen("Cyber Shogun"), Is.True);
        Assert.That(later.LessonSeen("Oceanus"), Is.True);
        later.Unlock("Aura Phoenicia");
        Assert.That(later.LessonSeen("Aura Phoenicia"), Is.False);
        later.MarkLessonSeen("Aura Phoenicia");
        Assert.That(later.LessonSeen("Aura Phoenicia"), Is.True);
    }

    /// <summary>Each operative owns exactly one distinct signature-ability tag.</summary>
    [Test]
    public void EveryOperativeHasADistinctAbilityTag()
    {
        var tags = OperativeRoster.RosterOrder.Select(OperativeRoster.AbilityTagOf).ToList();
        Assert.That(tags, Has.None.Null);
        Assert.That(tags.Distinct().Count(), Is.EqualTo(6));
    }

    /// <summary>
    /// The core design rule: every ability a mission's required route needs belongs to an operative who has joined by
    /// the end of that mission (mid-mission joins are taught before their gate).
    /// </summary>
    [Test]
    public void RequiredAbilitiesAreAvailableByTheirMission()
    {
        foreach (var level in LevelCatalog.All)
        {
            var squad = OperativeRoster.ForLevelStart(level.Number);
            foreach (string name in level.GrantsOperatives) squad.Unlock(name);
            foreach (string tag in level.RequiredAbilities)
                Assert.That(squad.HasAbility(tag), Is.True, $"Mission {level.Number} requires '{tag}' but nobody owns it by its end");
        }
    }

    /// <summary>The catalog is contiguous, clamps legacy values and links missions in order.</summary>
    [Test]
    public void Catalog_IsContiguousAndClamps()
    {
        for (int i = 0; i < LevelCatalog.Count; i++) Assert.That(LevelCatalog.All[i].Number, Is.EqualTo(i + 1));
        Assert.That(LevelCatalog.Clamp(0), Is.EqualTo(1));
        Assert.That(LevelCatalog.Clamp(11), Is.EqualTo(LevelCatalog.Count));
        Assert.That(LevelCatalog.HasNext(LevelCatalog.Count), Is.False);
        Assert.That(LevelCatalog.ScenePath(1), Does.EndWith("level1.tscn"));
    }

    /// <summary>Only the intended operatives are granted, and each exactly once across the campaign.</summary>
    [Test]
    public void EveryNonStarterOperativeIsGrantedExactlyOnce()
    {
        var granted = LevelCatalog.All.SelectMany(l => l.GrantsOperatives).ToList();
        Assert.That(granted, Is.Unique);
        Assert.That(granted.Select(OperativeRoster.Normalize).OrderBy(x => x),
            Is.EqualTo(OperativeRoster.RosterOrder.Where(n => n != OperativeRoster.Starter).OrderBy(x => x)));
    }
}
