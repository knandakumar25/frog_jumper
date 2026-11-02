using Godot;

/// <summary>Invulnerable nonblocking crossing presentation. No collision, escort logic or timed rescue requirement.</summary>
public partial class AegisCivilian : Node2D
{
    /// <summary>Walks the fixed safe road at a frame-independent speed and waits inside the open border.</summary>
    public override void _PhysicsProcess(double delta) => Position = new Vector2(Mathf.Min(Position.X + 78 * (float)delta, 27300),600);
    /// <summary>Readable fallback silhouettes keep the liberated crossing inhabited even if optional artwork is missing.</summary>
    public override void _Draw()
    {
        if (ResourceLoader.Exists("res://assets/environment/shattered_aegis/civilians.png")) return;
        DrawCircle(new Vector2(0,-73),12,new Color("#e5c7ac"));
        DrawRect(new Rect2(-13,-60,26,40),new Color("#a18d7c"));
        DrawLine(new Vector2(-6,-20),new Vector2(-8,0),new Color("#72838b"),7);
        DrawLine(new Vector2(6,-20),new Vector2(8,0),new Color("#72838b"),7);
    }
}
