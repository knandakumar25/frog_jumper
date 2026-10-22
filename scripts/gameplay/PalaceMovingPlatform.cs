using Godot;

/// <summary>
/// A timber platform that glides back and forth between its spawn point and spawn point + <see cref="Travel"/>.
/// Built on <see cref="AnimatableBody2D"/> with physics sync so a standing CharacterBody2D is carried with it.
/// </summary>
public partial class PalaceMovingPlatform : AnimatableBody2D
{
    /// <summary>Platform size in world pixels; Position is the top-left corner at the start of the cycle.</summary>
    public Vector2 Size { get; set; } = new(180f, 28f);
    /// <summary>Offset from the start position to the far end of the route.</summary>
    public Vector2 Travel { get; set; } = new(280f, 0f);
    /// <summary>Seconds for one full out-and-back cycle.</summary>
    public float Period { get; set; } = 5f;
    /// <summary>Initial phase in seconds, so several platforms can be staggered.</summary>
    public float PhaseOffset { get; set; }

    private Vector2 _origin;
    private float _time;

    /// <summary>Installs a one-way collision rectangle and remembers the route start.</summary>
    public override void _Ready()
    {
        _origin = Position;
        _time = PhaseOffset;
        SyncToPhysics = true;
        CollisionLayer = 1;
        CollisionMask = 0;
        AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = Size },
            Position = Size * .5f,
            OneWayCollision = true,
            OneWayCollisionMargin = 10f
        });
    }

    /// <summary>Eases between the two route ends (cosine easing) so riders get a gentle slow-down at each stop.</summary>
    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        float t = (1f - Mathf.Cos(_time / Period * Mathf.Tau)) * .5f;
        Position = _origin + Travel * t;
    }

    /// <summary>Draws the timber deck plus glowing underside thrusters that explain why it floats.</summary>
    public override void _Draw()
    {
        PalacePlatform.DrawSurface(this, Size, PalaceSurface.Wood, true, 0f);
        for (float x = 28f; x < Size.X; x += 62f)
            DrawLine(new Vector2(x, Size.Y), new Vector2(x, Size.Y + 8f + Mathf.Sin(_time * 12f + x) * 3f), new Color("#81e3e6"), 4f, true);
    }

    /// <summary>Requests a redraw so the thruster flicker animates.</summary>
    public override void _Process(double delta) => QueueRedraw();
}
