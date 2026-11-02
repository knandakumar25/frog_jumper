using Godot;

/// <summary>Alternating floor heat with a harmless practice variant. Bulwark lessons observe the real protection window.</summary>
public partial class AegisVent : Node2D
{
    /// <summary>Player and suspension state are supplied by the owning mission.</summary>
    public ShatteredAegisMission Mission { get; set; } = null!;
    /// <summary>Hot footprint; 140 pixels leaves ample margin within Surya's 1.4 seconds at 300px/s.</summary>
    public float Width { get; set; } = 140;
    /// <summary>Practice vents flash without dealing damage.</summary>
    public bool Practice { get; set; }
    /// <summary>Thermal section vents become permanently cold with the coolant intake.</summary>
    public bool Thermal { get; set; }
    /// <summary>Per-vent offset makes adjacent heat pulses alternate.</summary>
    public float Offset { get; set; }
    /// <summary>Current visible heat state.</summary>
    public bool Hot => !Offline && (_clock + Offset) % 4 < 2;
    /// <summary>Permanent mission state drives both art and damage.</summary>
    public bool Offline => Mission.Progress.Has("crossing:open") || Thermal && Mission.Progress.Has("coolant:intake");
    private float _clock, _hurt;
    /// <summary>Applies only environmental heat; never processes damage during arrival, recovery, pause or results.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Mission.GameplaySuspended) return;
        _clock += (float)delta;
        _hurt -= (float)delta;
        Vector2 local = Mission.Player.GlobalPosition - GlobalPosition;
        if (!Practice && Hot && _hurt <= 0 && Mathf.Abs(local.X) < Width / 2 + 18 && local.Y > -170 && local.Y < 20)
        {
            Mission.Player.TakeDamage(30);
            _hurt = .45f;
        }
        QueueRedraw();
    }
    /// <summary>Cool strips stay visible after shutdown; hot columns communicate a safe wait window.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(-Width / 2, -8, Width, 8), new Color(Offline ? "#77efff" : "#ffbc69"));
        if (Hot) for (int i = 0; i < 5; i++)
            DrawRect(new Rect2(-Width / 2 + i * Width / 5, -140, Width / 8, 132), new Color(1, .4f, .1f, Practice ? .2f : .5f));
    }
}
