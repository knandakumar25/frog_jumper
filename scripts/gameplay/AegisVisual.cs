using Godot;

/// <summary>Explicit scale/pivot adapter for original fortress art; missing files leave collision and objectives intact.</summary>
public static class AegisVisual
{
    /// <summary>Adds nonblocking decoration at a controlled world size. Returns no node if artwork is unavailable.</summary>
    public static void Add(Node2D parent, string asset, Vector2 size, Vector2 offset)
    {
        string path = $"res://assets/environment/shattered_aegis/{asset}.png";
        if (!ResourceLoader.Exists(path)) return;
        var texture = GD.Load<Texture2D>(path);
        parent.AddChild(new Sprite2D { Name = "FortressArt", Texture = texture, Position = offset,
            Scale = size / texture.GetSize(), TextureFilter = CanvasItem.TextureFilterEnum.Linear });
    }
}
