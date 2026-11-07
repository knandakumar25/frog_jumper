using System;

/// <summary>
/// Level of puzzle assistance available for environmental and operative exploration puzzles.
/// </summary>
public enum PuzzleAssistance
{
    Standard,
    ExtendedClues,
    AutoSolveAllowed
}

/// <summary>
/// Controller and input glyph style for on-screen prompt displays.
/// </summary>
public enum ControllerGlyphStyle
{
    AutoDetect,
    Xbox,
    PlayStation,
    NintendoSwitch,
    Keyboard
}

/// <summary>
/// User accessibility and gameplay assist configuration for Neo-Kyoto exploration and combat.
/// Provides motor, visual, and cognitive assists while preserving challenge integrity.
/// </summary>
public sealed class AccessibilityProfile
{
    private float _parryTimingWindowScale = 1.0f;
    private float _screenShakeScale = 1.0f;

    /// <summary>
    /// When true, renders high-contrast bright outlines on enemy attack telegraphs and hazards.
    /// </summary>
    public bool HighContrastTelegraphs { get; set; } = false;

    /// <summary>
    /// Scaling multiplier for defensive parry and dodge success windows (clamped between 1.0 and 2.5).
    /// </summary>
    public float ParryTimingWindowScale
    {
        get => _parryTimingWindowScale;
        set => _parryTimingWindowScale = Math.Clamp(value, 1.0f, 2.5f);
    }

    /// <summary>
    /// Level of puzzle assistance enabled for exploration challenges and traversal gates.
    /// </summary>
    public PuzzleAssistance PuzzleAssist { get; set; } = PuzzleAssistance.Standard;

    /// <summary>
    /// Scaling multiplier for screen shake intensity (clamped between 0.0 and 1.0).
    /// 0.0 disables screen shake entirely for photosensitive or motion-sensitive players.
    /// </summary>
    public float ScreenShakeScale
    {
        get => _screenShakeScale;
        set => _screenShakeScale = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Preferred controller glyph prompt presentation.
    /// </summary>
    public ControllerGlyphStyle GlyphStyle { get; set; } = ControllerGlyphStyle.AutoDetect;

    /// <summary>
    /// Grants extended invulnerability frames following operative recovery from knockdown or heavy hit.
    /// </summary>
    public bool ExtendedRecoveryInvulnerability { get; set; } = false;

    /// <summary>
    /// Calculates effective parry window in seconds given a base window duration.
    /// </summary>
    public float CalculateParryWindow(float baseWindowSeconds)
    {
        return MathF.Max(0.05f, baseWindowSeconds * ParryTimingWindowScale);
    }

    /// <summary>
    /// Calculates effective screen shake intensity given a base intensity value.
    /// </summary>
    public float CalculateScreenShake(float baseIntensity)
    {
        return MathF.Max(0.0f, baseIntensity * ScreenShakeScale);
    }

    /// <summary>
    /// Creates a default baseline accessibility profile.
    /// </summary>
    public static AccessibilityProfile Default => new();
}
