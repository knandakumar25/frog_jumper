using Godot;

/// <summary>
/// Loads art that may be absent. The Blender-made images were deleted on 2026-11-11 and are being
/// remade with SpriteCook (see <c>docs/ART_REGENERATION.md</c>); until each file comes back, callers
/// get null and leave a gap instead of logging a load error. A regenerated file at the same path is
/// used with no code change.
/// </summary>
public static class OptionalArt
{
    /// <summary>Returns the texture at <paramref name="path"/>, or null when the file does not exist.</summary>
    public static Texture2D? Texture(string path) =>
        ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
}
