using Godot;

/// <summary>Shared scale adapter for the Wildfire mission props (SpriteCook pixel art, Phase 4).</summary>
public static class WildfireVisual
{
    /// <summary>Adds a noninteractive sprite at an explicit world size; missing art keeps gameplay functional. Nearest filtering keeps the stretched pixel art crisp (each 512x512 prop is drawn at a non-square display size).</summary>
    public static void AddSprite(Node2D parent, string asset, Vector2 size, Vector2 offset)
    {
        string path = $"res://assets/environment/wildfire/{asset}.png";
        if (!ResourceLoader.Exists(path)) return;
        var texture = GD.Load<Texture2D>(path);
        parent.AddChild(new Sprite2D { Name = "Art", Texture = texture, Position = offset,
            Scale = size / texture.GetSize(), TextureFilter = CanvasItem.TextureFilterEnum.Nearest });
    }
}
