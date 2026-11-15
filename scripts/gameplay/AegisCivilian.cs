using Godot;

/// <summary>Invulnerable nonblocking crossing presentation. No collision, escort logic or timed rescue requirement.</summary>
public partial class AegisCivilian : Node2D
{
    /// <summary>Walks the fixed safe road at a frame-independent speed and waits inside the open border.</summary>
    public override void _PhysicsProcess(double delta) => Position = new Vector2(Mathf.Min(Position.X + 78 * (float)delta, 27300),600);
}
