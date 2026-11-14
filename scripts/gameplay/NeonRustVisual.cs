using Godot;

/// <summary>Explicit scale adapter for the Neon Rust depot props (SpriteCook pixel art, Phase 4); decoration never supplies collision.</summary>
public static class NeonRustVisual
{
    /// <summary>Adds a noninteractive centered sprite; missing imported artwork leaves the validated blockout playable. Nearest filtering keeps the stretched pixel art crisp (each 512x512 prop is drawn at a non-square display size).</summary>
    public static void AddSprite(Node2D parent, string asset, Vector2 size, Vector2 offset)
    {
        string path = $"res://assets/environment/neon_rust/{asset}.png";
        if (!ResourceLoader.Exists(path)) return;
        var texture = GD.Load<Texture2D>(path);
        parent.AddChild(new Sprite2D { Name = "DepotArt", Texture = texture, Position = offset,
            Scale = size / texture.GetSize(), TextureFilter = CanvasItem.TextureFilterEnum.Nearest });
    }
}
