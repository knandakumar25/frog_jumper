using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Categorization of quests across the Neo-Kyoto Living Resistance world.
/// </summary>
public enum QuestCategory
{
    District,
    Operative,
    Faction
}

/// <summary>
/// Status state machine for an authored quest.
/// </summary>
public enum QuestStatus
{
    NotStarted,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed
}

/// <summary>
/// Distinct objective types within a quest chain.
/// </summary>
public enum ObjectiveType
{
    Interact,
    Investigate,
    DefeatHostiles,
    RepairConduit,
    DeliverPayload,
    MakeConsequentialChoice
}

/// <summary>
/// A single authored objective step within a quest.
/// </summary>
public sealed record QuestObjective(
    string Id,
    string Description,
    ObjectiveType Type,
    WorldPoint TargetLocation,
    double InteractionRadius = 16.0,
    string? TargetEntityId = null
);

/// <summary>
/// A consequential narrative and gameplay branch choice offering visible tradeoffs.
/// </summary>
public sealed record QuestBranchChoice(
    string ChoiceId,
    string Title,
    string Description,
    string ConsequenceSummary,
    string UnlockedServiceId
);

/// <summary>
/// One-time completion rewards awarded upon committing a quest turn-in.
/// </summary>
public sealed record QuestReward(
    int HeroSparks,
    int CityCredits,
    string? BonusModuleId = null,
    string? BonusGadgetId = null
);

/// <summary>
/// Complete authored specification for a quest in Neo-Kyoto.
/// </summary>
public sealed record QuestDefinition(
    string Id,
    string Title,
    string Summary,
    int RegionId,
    QuestCategory Category,
    int MinCampaignMission,
    string? PrerequisiteQuestId,
    string? RequiredOperative,
    IReadOnlyList<QuestObjective> Objectives,
    IReadOnlyList<QuestBranchChoice>? BranchChoices,
    QuestReward Reward
);

/// <summary>
/// Runtime progress state for a single quest within a campaign.
/// </summary>
public sealed class QuestProgressState
{
    public string QuestId { get; set; } = "";
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;
    public int CurrentObjectiveIndex { get; set; } = 0;
    public string? ChosenBranchId { get; set; }
    public string? CompletionReceiptToken { get; set; }
    public long CompletedAtUnixSeconds { get; set; }

    public bool IsComplete => Status == QuestStatus.Completed;
}
