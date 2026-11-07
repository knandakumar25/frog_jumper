using System;

/// <summary>
/// Enforces and monitors runtime simulation budgets, memory footprints, and frame intervals
/// for Neo-Kyoto's streamed 128 km x 128 km world.
/// </summary>
public static class WorldPerformanceBudget
{
    /// <summary>
    /// Maximum simultaneous active civilian NPCs in the nearby simulation radius.
    /// </summary>
    public const int MaxNearbyCivilians = 16;

    /// <summary>
    /// Maximum simultaneous active hostile combatants in an active combat pocket.
    /// </summary>
    public const int MaxHostileCombatants = 8;

    /// <summary>
    /// Maximum simultaneous moving road/waterway traffic vehicles.
    /// </summary>
    public const int MaxMovingTrafficVehicles = 4;

    /// <summary>
    /// Number of active resident spatial chunks held in live simulation (3x3 grid around player).
    /// </summary>
    public const int ActiveResidentChunkCount = 9;

    /// <summary>
    /// Total number of cached chunks maintained in memory (5x5 grid around player).
    /// </summary>
    public const int MaxCachedChunkCount = 25;

    /// <summary>
    /// Target frame time interval in milliseconds for sustained 1080p/60fps playback (16.67 ms).
    /// </summary>
    public const float TargetFrameIntervalMs = 16.6667f;

    /// <summary>
    /// Maximum allowable 95th percentile (p95) frame time interval in milliseconds.
    /// </summary>
    public const float MaxP95FrameIntervalMs = 18.0f;

    /// <summary>
    /// Maximum allowable single-frame streaming stall duration in milliseconds before logging a degradation.
    /// </summary>
    public const float MaxStreamingStallThresholdMs = 33.333f;

    /// <summary>
    /// Validates whether a candidate population count fits within the authored simulation budget.
    /// </summary>
    public static bool IsWithinBudget(int civilians, int hostiles, int vehicles)
    {
        return civilians <= MaxNearbyCivilians &&
               hostiles <= MaxHostileCombatants &&
               vehicles <= MaxMovingTrafficVehicles;
    }

    /// <summary>
    /// Validates whether a frame interval conforms to the smooth 60fps performance budget.
    /// </summary>
    public static bool IsAcceptableFrameInterval(float frameIntervalMs)
    {
        return frameIntervalMs <= MaxStreamingStallThresholdMs;
    }
}
