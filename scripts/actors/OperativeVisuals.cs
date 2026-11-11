using Godot;

/// <summary>
/// Resources for the operative and enemy sprite animations in <c>assets/characters/rendered</c>.
/// </summary>
/// <remarks>
/// The Blender-made sprites were deleted on 2026-11-11 and are waiting to be remade with
/// SpriteCook (see <c>docs/ART_REGENERATION.md</c>). Until a file comes back, the loaders here
/// return either null or an invisible placeholder, so characters leave a gap instead of logging
/// load errors. A regenerated <c>.tres</c> at the same path is picked up with no code change.
/// </remarks>
public static class OperativeVisuals
{
    /// <summary>
    /// Every animation name that actor scripts play or assign. The placeholder must define all of
    /// them, because <c>AnimatedSprite2D.Play</c> reports an error for an unknown animation.
    /// </summary>
    private static readonly string[] AnimationNames =
    {
        "default", "idle", "running", "jumping", "falling", "double_jump", "hit",
        "blade_dash", "sword_attack", "karate_attack", "finished",
    };

    /// <summary>
    /// Animations that play once, as in the original art. Callers such as <c>MainCharacter</c> wait
    /// for <c>animation_finished</c> after an attack, which never fires for a looping animation.
    /// </summary>
    private static readonly string[] OneShot = { "blade_dash", "sword_attack", "karate_attack", "hit" };

    private static SpriteFrames? _placeholder;

    /// <summary>Path of the SpriteFrames resource for the named operative.</summary>
    public static string ResourcePath(string name) =>
        $"res://assets/characters/rendered/{OperativeRegistry.GetProfile(name).Id}.tres";

    /// <summary>Path of the selection-card portrait for the named operative.</summary>
    public static string PortraitPath(string name) =>
        $"res://assets/characters/rendered/{OperativeRegistry.GetProfile(name).Id}_portrait.png";

    /// <summary>
    /// Loads the named operative's animations, or the invisible placeholder when the art is missing.
    /// Use this for actors that call <c>Play</c>; use <see cref="TryLoadFrames"/> when the caller
    /// draws frame textures itself.
    /// </summary>
    public static SpriteFrames LoadFrames(string name) => LoadFramesAt(ResourcePath(name));

    /// <summary>
    /// Loads a SpriteFrames resource by path, or the invisible placeholder when the file is missing.
    /// Enemy scenes use this with <c>vanguard.tres</c> and <c>heavy.tres</c>.
    /// </summary>
    public static SpriteFrames LoadFramesAt(string path) => TryLoadAt(path) ?? Placeholder;

    /// <summary>Loads the named operative's animations, or null when the art is missing.</summary>
    public static SpriteFrames? TryLoadFrames(string name) => TryLoadAt(ResourcePath(name));

    /// <summary>
    /// Shared placeholder: every expected animation holds one frame with no texture.
    /// <c>AnimatedSprite2D</c> draws nothing for a null frame texture, so the character is invisible
    /// while its collision, hit boxes and scripts keep working.
    /// </summary>
    public static SpriteFrames Placeholder => _placeholder ??= BuildPlaceholder();

    private static SpriteFrames? TryLoadAt(string path) =>
        ResourceLoader.Exists(path) ? GD.Load<SpriteFrames>(path) : null;

    private static SpriteFrames BuildPlaceholder()
    {
        var frames = new SpriteFrames();
        foreach (string name in AnimationNames)
        {
            if (!frames.HasAnimation(name)) frames.AddAnimation(name);
            frames.SetAnimationLoop(name, System.Array.IndexOf(OneShot, name) < 0);
            frames.AddFrame(name, null!);
        }
        return frames;
    }
}
