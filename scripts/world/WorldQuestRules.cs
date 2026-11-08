using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One quest step the player can act on now: the quest, its current index and that objective.</summary>
public sealed record QuestAction(QuestDefinition Quest, int ObjectiveIndex, QuestObjective Objective);

/// <summary>
/// Engine-free rules for the playable quest slice (Little Tokyo and Concrete Wards).
/// Keeping them free of Godot types lets NUnit prove every slice quest can be reached.
/// Quests outside <see cref="PlayableQuestIds"/> remain catalog data and are never offered.
/// </summary>
public static class WorldQuestRules
{
    /// <summary>The eight quests with complete runtime support. The other 64 are data only.</summary>
    public static readonly IReadOnlySet<string> PlayableQuestIds = new HashSet<string>
    {
        "quest_ok_01", "quest_ok_02", "quest_ok_03",
        "quest_cw_01", "quest_cw_02", "quest_cw_03",
        "quest_op_shogun", "quest_op_oceanus"
    };

    /// <summary>Slice quests in catalog order. Cached because the world reads this several times per frame.</summary>
    public static readonly IReadOnlyList<QuestDefinition> PlayableQuests =
        WorldContentCatalog.Quests.Where(q => PlayableQuestIds.Contains(q.Id)).ToArray();

    /// <summary>Whether the world clock is in its daytime half, which selects resident positions.</summary>
    public static bool IsDay(double worldMinutes) => (worldMinutes % 48.0) < 24.0;

    /// <summary>
    /// Decides whether a quest may be offered.
    /// <paramref name="progress"/> is the highest unlocked mission, so a quest gated on
    /// "after mission N" needs a value above N. A required operative must be recruited.
    /// </summary>
    public static bool IsAvailable(QuestDefinition quest, WorldSave state, int progress, Func<string, bool> isRecruited)
    {
        if (!PlayableQuestIds.Contains(quest.Id)) return false;
        if (state.CompletedQuests?.ContainsKey(quest.Id) == true) return false;
        if (progress <= quest.MinCampaignMission) return false;
        if (quest.PrerequisiteQuestId != null && state.CompletedQuests?.ContainsKey(quest.PrerequisiteQuestId) != true) return false;
        return quest.RequiredOperative == null || isRecruited(quest.RequiredOperative);
    }

    /// <summary>Returns the current objective of an available quest, or null when its steps are used up.</summary>
    public static QuestAction? Current(QuestDefinition quest, WorldSave state)
    {
        int index = 0;
        state.ActiveQuests?.TryGetValue(quest.Id, out index);
        return index >= 0 && index < quest.Objectives.Count ? new QuestAction(quest, index, quest.Objectives[index]) : null;
    }

    /// <summary>Resolves where an objective is right now; a resident target follows the day/evening schedule.</summary>
    public static WorldPoint ObjectivePoint(QuestObjective objective, bool isDay)
    {
        var resident = objective.TargetEntityId == null ? null : WorldContentCatalog.Residents.FirstOrDefault(r => r.Id == objective.TargetEntityId);
        return resident == null ? objective.TargetLocation : isDay ? resident.DayLocation : resident.EveningLocation;
    }

    /// <summary>
    /// Finds the first non-combat objective the player can complete by interacting at this point.
    /// A quest that requires an operative only responds while that operative is active.
    /// </summary>
    public static QuestAction? FindInteractable(WorldSave state, WorldPoint point, int progress, bool isDay, string activeOperative, Func<string, bool> isRecruited)
    {
        foreach (var action in Candidates(state, progress, activeOperative, isRecruited))
        {
            if (action.Objective.Type == ObjectiveType.DefeatHostiles) continue;
            if (ObjectivePoint(action.Objective, isDay).Distance(point) <= action.Objective.InteractionRadius) return action;
        }
        return null;
    }

    /// <summary>Finds a combat objective whose encounter should start because the player is within <paramref name="range"/> metres.</summary>
    public static QuestAction? FindCombat(WorldSave state, WorldPoint point, int progress, string activeOperative, Func<string, bool> isRecruited, double range = 40)
    {
        foreach (var action in Candidates(state, progress, activeOperative, isRecruited))
        {
            if (action.Objective.Type == ObjectiveType.DefeatHostiles && action.Objective.TargetLocation.Distance(point) <= range) return action;
        }
        return null;
    }

    /// <summary>Chooses the quest shown on the HUD: the last one advanced if still open, otherwise the first available.</summary>
    public static QuestAction? Tracked(WorldSave state, string? preferredQuestId, int progress, Func<string, bool> isRecruited)
    {
        var open = PlayableQuests.Where(q => IsAvailable(q, state, progress, isRecruited)).ToArray();
        var chosen = open.FirstOrDefault(q => q.Id == preferredQuestId) ?? open.FirstOrDefault();
        return chosen == null ? null : Current(chosen, state);
    }

    /// <summary>Lists the current step of each available quest the active operative may attempt.</summary>
    private static IEnumerable<QuestAction> Candidates(WorldSave state, int progress, string activeOperative, Func<string, bool> isRecruited)
    {
        foreach (var quest in PlayableQuests)
        {
            if (!IsAvailable(quest, state, progress, isRecruited)) continue;
            if (quest.RequiredOperative != null && quest.RequiredOperative != activeOperative) continue;
            var action = Current(quest, state);
            if (action != null) yield return action;
        }
    }
}
