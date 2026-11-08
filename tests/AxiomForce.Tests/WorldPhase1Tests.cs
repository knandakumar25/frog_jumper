using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Data-level proof that the eight playable slice quests can actually be finished:
/// each one is reachable through its prerequisites, its targets are on walkable ground,
/// its text exists and its rewards point at real items.
/// </summary>
[TestFixture]
public class WorldPhase1Tests
{
    private static readonly Func<string, bool> Everyone = _ => true;

    /// <summary>Walks the prerequisite graph the way play does, completing whatever is open.</summary>
    [Test]
    public void AllSliceQuestsBecomeAvailableInOrder()
    {
        var state = WorldSave.AfterMission(1);
        var finished = new HashSet<string>();
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var quest in WorldQuestRules.PlayableQuests.Where(q => WorldQuestRules.IsAvailable(q, state, 12, Everyone)).ToArray())
            {
                state = state.WithQuestCompleted(quest.Id, quest.BranchChoices?.First().ChoiceId);
                finished.Add(quest.Id);
                changed = true;
            }
        }
        Assert.That(finished, Is.EquivalentTo(WorldQuestRules.PlayableQuestIds));
    }

    /// <summary>"After mission N" must mean mission N is finished, so the unlocked level has to exceed N.</summary>
    [Test]
    public void MissionGateRequiresTheMissionToBeFinished()
    {
        var quest = WorldContentCatalog.Quests.Single(q => q.Id == "quest_ok_01");
        var state = WorldSave.AfterMission(1);
        Assert.That(WorldQuestRules.IsAvailable(quest, state, 1, Everyone), Is.False);
        Assert.That(WorldQuestRules.IsAvailable(quest, state, 2, Everyone), Is.True);
    }

    /// <summary>Missing prerequisites and unrecruited operatives keep a quest closed.</summary>
    [Test]
    public void PrerequisitesAndRecruitmentGateQuests()
    {
        var state = WorldSave.AfterMission(1);
        Assert.That(WorldQuestRules.IsAvailable(WorldContentCatalog.Quests.Single(q => q.Id == "quest_ok_02"), state, 12, Everyone), Is.False);
        Assert.That(WorldQuestRules.IsAvailable(WorldContentCatalog.Quests.Single(q => q.Id == "quest_op_oceanus"), state, 12, _ => false), Is.False);
    }

    /// <summary>Every target is in the world, with walkable ground inside its interaction radius.</summary>
    [Test]
    public void EveryObjectiveTargetIsReachableOnFoot()
    {
        foreach (var quest in WorldQuestRules.PlayableQuests)
        foreach (var objective in quest.Objectives)
        foreach (bool isDay in new[] { true, false })
        {
            var point = WorldQuestRules.ObjectivePoint(objective, isDay);
            Assert.That(WorldCatalog.InBounds(point), Is.True, objective.Id);
            Assert.That(HasWalkableCellNear(point, objective.InteractionRadius), Is.True, $"{objective.Id} is blocked (day={isDay})");
        }
    }

    /// <summary>Named targets exist, and every conversation is written.</summary>
    [Test]
    public void ObjectiveTargetsAndConversationsExist()
    {
        foreach (var quest in WorldQuestRules.PlayableQuests)
        foreach (var objective in quest.Objectives)
        {
            if (objective.TargetEntityId != null)
                Assert.That(WorldContentCatalog.Residents.Any(r => r.Id == objective.TargetEntityId), Is.True, objective.Id);
            if (objective.Type != ObjectiveType.DefeatHostiles)
                Assert.That(WorldDialogueCatalog.HasAuthoredLines(objective.Id), Is.True, objective.Id);
        }
    }

    /// <summary>Combat objectives sit in hostile pockets, so no fight starts inside a safe space.</summary>
    [Test]
    public void CombatObjectivesAreOutsideSafePockets()
    {
        foreach (var objective in WorldQuestRules.PlayableQuests.SelectMany(q => q.Objectives).Where(o => o.Type == ObjectiveType.DefeatHostiles))
        {
            bool safe = WorldContentCatalog.Pockets.Any(p => p.IsSafeZone && p.Center.Distance(objective.TargetLocation) <= p.RadiusMetres);
            Assert.That(safe, Is.False, objective.Id);
        }
    }

    /// <summary>Encounter waves fit the shared hostile budget.</summary>
    [Test]
    public void WavesFitTheHostileBudget()
    {
        foreach (var objective in WorldQuestRules.PlayableQuests.SelectMany(q => q.Objectives).Where(o => o.Type == ObjectiveType.DefeatHostiles))
            Assert.That(WorldPerformanceBudget.IsWithinBudget(0, objective.RequiredCount, 0), Is.True, objective.Id);
    }

    /// <summary>All 72 quests, not only the slice, reward items that exist.</summary>
    [Test]
    public void EveryQuestRewardResolvesToARealItem()
    {
        var modules = CampaignProgressionState.ModuleCatalog.Select(m => m.Id).ToHashSet();
        var gadgets = CampaignProgressionState.GadgetCatalog.Select(g => g.Id).ToHashSet();
        foreach (var quest in WorldContentCatalog.Quests)
        {
            if (quest.Reward.BonusModuleId != null) Assert.That(modules, Does.Contain(quest.Reward.BonusModuleId), quest.Id);
            if (quest.Reward.BonusGadgetId != null) Assert.That(gadgets, Does.Contain(quest.Reward.BonusGadgetId), quest.Id);
        }
    }

    /// <summary>A fight must happen in its own quest's region, including quests that are still data only.</summary>
    [Test]
    public void EveryCombatObjectiveIsInItsQuestRegion()
    {
        var misplaced = WorldContentCatalog.Quests
            .SelectMany(q => q.Objectives.Where(o => o.Type == ObjectiveType.DefeatHostiles && WorldCatalog.Region(o.TargetLocation).Id != q.RegionId).Select(o => $"{q.Id}/{o.Id}"))
            .ToArray();
        Assert.That(misplaced, Is.Empty);
    }

    /// <summary>The Free Signal specialty comes only from that faction, so no later grant silently adds nothing.</summary>
    [Test]
    public void FreeSignalModuleIsAwardedOnce()
    {
        Assert.That(WorldContentCatalog.Quests.Count(q => q.Reward.BonusModuleId == "mod_free_signal_tuner"), Is.EqualTo(1));
    }

    /// <summary>Checks the 8 m terrain cells around a point. Terrain values of 2 and above are solid.</summary>
    private static bool HasWalkableCellNear(WorldPoint point, double radius)
    {
        for (double dx = -radius; dx <= radius; dx += 4)
        for (double dy = -radius; dy <= radius; dy += 4)
        {
            if (dx * dx + dy * dy > radius * radius) continue;
            var probe = new WorldPoint(point.X + dx, point.Y + dy, point.Layer);
            var chunk = WorldGeneration.Generate(probe.Chunk);
            int cellX = Math.Clamp((int)((probe.X - probe.Chunk.X * 128) / 8), 0, 15);
            int cellY = Math.Clamp((int)((probe.Y - probe.Chunk.Y * 128) / 8), 0, 15);
            if (chunk.Terrain[cellY * 16 + cellX] < 2) return true;
        }
        return false;
    }
}
