using Godot;

/// <summary>A close-range Xelthara demolition drone that hunts the active operative in the dojo.</summary>
public partial class IsometricPurgeDrone : CharacterBody2D
{
    /// <summary>Maximum damage the drone can sustain before it is destroyed.</summary>
    [Export] public float MaxHealth { get; set; } = 100f;
    /// <summary>Movement speed toward the operative when the drone enters its detection radius.</summary>
    [Export] public float HuntSpeed { get; set; } = 105f;
    /// <summary>Theme tint that differentiates demolition crew roles.</summary>
    [Export] public Color DroneColor { get; set; } = new("#8f5de0");

    /// <summary>Raised when this drone is removed from the encounter.</summary>
    [Signal] public delegate void DefeatedEventHandler();

    /// <summary>Current remaining drone integrity.</summary>
    public float CurrentHealth { get; private set; }
    private CyberShogun? _player;
    private float _attackCooldown;
    private float _bob;
    private bool _dying;

    /// <summary>Sets combat layers, integrity, and a compact circular body collider.</summary>
    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        CollisionLayer = 4;
        CollisionMask = 1;
        AddToGroup("enemies");
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
            AddChild(new CollisionShape2D { Name = "CollisionShape2D", Shape = new CircleShape2D { Radius = 27f } });
        SetPhysicsProcess(true);
    }

    /// <summary>Approaches the operative and deals contact damage on a guarded cadence.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_dying) return;
        _attackCooldown = Mathf.Max(0f, _attackCooldown - (float)delta);
        _player ??= GetTree().Root.FindChild("CyberShogun", true, false) as CyberShogun;
        if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
        Vector2 difference = _player.GlobalPosition - GlobalPosition;
        if (difference.Length() <= 560f && difference.LengthSquared() > 1f)
        {
            Velocity = difference.Normalized() * HuntSpeed;
            MoveAndSlide();
            if (difference.Length() < 48f && _attackCooldown <= 0f)
            {
                _player.TakeDamage(12f, difference.Normalized() * 95f);
                _attackCooldown = .9f;
            }
        }
        else Velocity = Velocity.MoveToward(Vector2.Zero, HuntSpeed * (float)delta * 4f);
        _bob += (float)delta * 3f;
        QueueRedraw();
    }

    /// <summary>Receives a single hit from the operative's aimed melee or signature ability.</summary>
    public void TakeDamage(float amount)
    {
        if (_dying || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth <= 0f)
        {
            _dying = true;
            EmitSignal(SignalName.Defeated);
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    /// <summary>Draws the hovering saw-drone body, rotating blade, and remaining integrity.</summary>
    public override void _Draw()
    {
        DrawEllipse(new Vector2(0, 18), new Vector2(32, 11), new Color("#0a0d18"));
        DrawCircle(new Vector2(0, Mathf.Sin(_bob) * 3f), 24f, new Color("#242a3c"));
        DrawArc(Vector2.Zero, 25f, _bob, _bob + Mathf.Tau * .72f, 20, DroneColor, 5f, true);
        DrawColoredPolygon(new Vector2[] {
            new(-18, -1), new(-10, -16), new(0, -20), new(10, -16), new(18, -1),
            new(10, 10), new(0, 14), new(-10, 10)
        }, new Color("#66758d"));
        DrawCircle(new Vector2(0, -3), 6f, new Color("#dffcff"));
        DrawLine(new Vector2(-9, 23), new Vector2(-9 + 18f * CurrentHealth / MaxHealth, 23), DroneColor, 3f, true);
    }

    /// <summary>Draws a filled ellipse using a polygon because CanvasItem has no ellipse primitive.</summary>
    private void DrawEllipse(Vector2 center, Vector2 radii, Color color)
    {
        var points = new Vector2[20];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = index * Mathf.Tau / points.Length;
            points[index] = center + new Vector2(Mathf.Cos(angle) * radii.X, Mathf.Sin(angle) * radii.Y);
        }
        DrawColoredPolygon(points, color);
    }
}
