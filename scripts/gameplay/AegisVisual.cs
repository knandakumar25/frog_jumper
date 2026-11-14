using Godot;

/// <summary>Explicit scale/pivot adapter for the fortress props (SpriteCook pixel art, Phase 4); missing files leave collision and objectives intact.</summary>
public static class AegisVisual
{
    /// <summary>Adds nonblocking decoration at a controlled world size. Returns no node if artwork is unavailable. Nearest filtering keeps the stretched pixel art crisp (each 512x512 prop is drawn at a non-square display size).</summary>
    public static void Add(Node2D parent, string asset, Vector2 size, Vector2 offset)
    {
        string path = $"res://assets/environment/shattered_aegis/{asset}.png";
        if (!ResourceLoader.Exists(path)) return;
        var texture = GD.Load<Texture2D>(path);
        parent.AddChild(new Sprite2D { Name = "FortressArt", Texture = texture, Position = offset,
            Scale = size / texture.GetSize(), TextureFilter = CanvasItem.TextureFilterEnum.Nearest });
    }
}
