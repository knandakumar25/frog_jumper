using Godot;

/// <summary>Shared import-scale adapter for the Docker/Blender authored Wildfire gameplay asset family.</summary>
public static class WildfireVisual
{
    /// <summary>Adds a noninteractive sprite at an explicit world size; missing art keeps gameplay functional.</summary>
    public static void AddSprite(Node2D parent, string asset, Vector2 size, Vector2 offset)
    {
        string path = $"res://assets/environment/wildfire/{asset}.png";
        if (!ResourceLoader.Exists(path)) return;
        var texture = GD.Load<Texture2D>(path);
        parent.AddChild(new Sprite2D { Name = "Art", Texture = texture, Position = offset,
            Scale = size / texture.GetSize(), TextureFilter = CanvasItem.TextureFilterEnum.Linear });
    }
}
