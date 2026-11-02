using Godot;

/// <summary>Mission boundary for finite encounters; keeps reusable enemies independent of any campaign level.</summary>
public interface IFiniteCombatHost
{
    /// <summary>Controlled body used for proximity and aiming.</summary>
    CyberShogun CombatPlayer { get; }
    /// <summary>Arrival, recovery and results reject activation and projectiles.</summary>
    bool GameplaySuspended { get; }
    /// <summary>Horizontal distance from a formation's authored center at which an incomplete attempt retires; zero disables retirement.</summary>
    float EncounterRetirementDistance => 0;
    /// <summary>Queries an exact scene-local event identifier.</summary>
    bool HasCombatEvent(string id);
    /// <summary>Records one completed finite formation.</summary>
    void CompleteCombatEvent(string id);
    /// <summary>Checks the mission's concurrent actor budget before spawning a group.</summary>
    bool CanActivateEncounter(int members);
    /// <summary>Awards a member slot once across recovery attempts.</summary>
    void RewardMember(string id);
    /// <summary>Creates a mission-owned transient round.</summary>
    void LaunchRound(Vector2 origin, Vector2 heading, WildfireGunner source);
}
