using Godot;

/// <summary>Resources for original flat 2D operative drawings and animation.</summary>
public static class OperativeVisuals
{
    public static string ResourcePath(string name) =>
        $"res://assets/characters/rendered/{OperativeRegistry.GetProfile(name).Id}.tres";

    public static string PortraitPath(string name) =>
        $"res://assets/characters/rendered/{OperativeRegistry.GetProfile(name).Id}_portrait.png";

    public static SpriteFrames LoadFrames(string name) =>
        GD.Load<SpriteFrames>(ResourcePath(name));
}
