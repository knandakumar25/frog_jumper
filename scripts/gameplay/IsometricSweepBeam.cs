using Godot;

/// <summary>Non-lethal visual sweep cue that damages operatives crossing the Terra-Walker's beam.</summary>
public partial class IsometricSweepBeam : Node2D
{
    /// <summary>Horizontal screen-space span of the terraforming beam.</summary>
    [Export] public float BeamLength { get; set; } = 880f;
    /// <summary>Vertical travel rate while sweeping the demolition yard.</summary>
    [Export] public float SweepSpeed { get; set; } = 150f;
    /// <summary>Raised when the beacon is disabled, stopping the active terraforming effect.</summary>
    public bool Disabled { get; set; }

    private CyberShogun? _player;
    private float _elapsed;
    private float _damageCooldown;
    private const float SweepSpan = 285f;

    /// <summary>Finds the shared operative and starts the visual hazard animation.</summary>
    public override void _Ready() => _player = GetTree().Root.FindChild("CyberShogun", true, false) as CyberShogun;

    /// <summary>Moves the beam and applies periodic damage when the player touches its visible lane.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Disabled) return;
        _elapsed += (float)delta;
        _damageCooldown = Mathf.Max(0f, _damageCooldown - (float)delta);
        Position = new Vector2(Position.X, 400f + Mathf.PingPong(_elapsed * SweepSpeed, SweepSpan) - SweepSpan * .5f);
        if (_player != null && GodotObject.IsInstanceValid(_player))
        {
            float horizontalDistance = Mathf.Abs(_player.GlobalPosition.X - GlobalPosition.X);
            float verticalDistance = Mathf.Abs(_player.GlobalPosition.Y - GlobalPosition.Y);
            if (horizontalDistance < BeamLength * .5f && verticalDistance < 36f && _damageCooldown <= 0f)
            {
                _player.TakeDamage(10f);
                _damageCooldown = 1.2f;
            }
        }
        QueueRedraw();
    }

    /// <summary>Renders the violet projected beam with a soft outline and bright core.</summary>
    public override void _Draw()
    {
        if (Disabled) return;
        DrawLine(new Vector2(-BeamLength * .5f, 0), new Vector2(BeamLength * .5f, 0), new Color(0.67f, 0.34f, 0.93f, .32f), 16f, true);
        DrawLine(new Vector2(-BeamLength * .5f, 0), new Vector2(BeamLength * .5f, 0), new Color("#d79cff"), 3f, true);
    }
}
