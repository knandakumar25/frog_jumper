using System.Collections.Generic;

/// <summary>Common scene-local ledger operations used by the later campaign's physical interaction and retry boundary.</summary>
public interface ILaterMissionProgress
{
    /// <summary>Authored milestone order; action readiness excludes defender checks to avoid locked-gate deadlocks.</summary>
    IReadOnlyList<string> ObjectiveIds { get; }
    /// <summary>Whether an exact event has already been accepted.</summary>
    bool Has(string id);
    /// <summary>Whether defender and milestone prerequisites currently permit completion.</summary>
    bool CanComplete(string id);
    /// <summary>Forgets only unfinished arena phases/support formations, preserving pre-arena progress and rewards.</summary>
    void ResetArena(string phasePrefix, string[] formations);
}
