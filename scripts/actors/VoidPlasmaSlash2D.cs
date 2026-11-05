using Godot;

/// <summary>
/// Swept high-velocity crescent plasma blade projectile launched by the Void Commander during Mission 12 (Judgment Day).
/// Deflectable by Kaito's Kinetic Parry; reflection redirects the crescent slash back to the boss for heavy counter damage.
/// </summary>
public partial class VoidPlasmaSlash2D : Area2D
{
    /// <summary>Travel speed in pixels per second.</summary>
    [Export] public float Speed { get; set; } = 580f;

    /// <summary>Impact damage inflicted on player if not parried or bulwarked.</summary>
    [Export] public float Damage { get; set; } = 32f;

    /// <summary>Maximum flight time before self-terminating.</summary>
    [Export] public float Lifetime { get; set; } = 4.5f;

    /// <summary>Initial launch trajectory.</summary>
    public Vector2 Direction { get; set; } = Vector2.Left;

    /// <summary>Reference to the boss that launched this projectile.</summary>
    public VoidCommanderBoss2D? OwningBoss { get; set; }

    /// <summary>Whether this projectile has been successfully parried and redirected.</summary>
    public bool Reflected { get; private set; }

    private float _age;
    private bool _cancelled;
    private Line2D? _trail;

    /// <summary>Initializes collision configuration, trail rendering, and adds to projectile groups.</summary>
    public override void _Ready()
    {
        AddToGroup("commander_projectiles");
        Monitoring = false;
        CollisionLayer = 0;
        Rotation = Direction.LengthSquared() > 0 ? Direction.Angle() : 0;

        // Visual crescent blade body: sweeping dark matter plasma arc
        var body = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(24, 0),
                new Vector2(10, -14),
                new Vector2(-6, -24),
                new Vector2(-2, -12),
                new Vector2(6, 0),
                new Vector2(-2, 12),
                new Vector2(-6, 24),
                new Vector2(10, 14)
            },
            Color = new Color(0.9f, 0.1f, 0.35f) // High-energy crimson/magenta void blade
        };
        AddChild(body);

        var core = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(16, 0),
                new Vector2(6, -8),
                new Vector2(-2, -14),
                new Vector2(2, 0),
                new Vector2(-2, 14),
                new Vector2(6, 8)
            },
            Color = new Color(1.0f, 0.85f, 0.95f) // Superheated plasma core
        };
        AddChild(core);

        _trail = new Line2D
        {
            Width = 8f,
            DefaultColor = new Color(0.7f, 0.1f, 0.9f, 0.75f)
        };
        _trail.AddPoint(new Vector2(-4, 0));
        _trail.AddPoint(new Vector2(-36, 0));
        AddChild(_trail);
    }

    /// <summary>Invoked by player Kinetic Parry deflection branch to redirect the projectile.</summary>
    public void TakeDamage(float amount)
    {
        Reflect();
    }

    /// <summary>Redirects projectile back toward the owning boss upon a successful parry.</summary>
    public void Reflect()
    {
        if (_cancelled || !IsInstanceValid(OwningBoss) || OwningBoss!.IsDying)
        {
            Cancel();
            return;
        }
        Reflected = true;
        Speed = 800f;
        Vector2 offset = OwningBoss.GlobalPosition - GlobalPosition;
        Direction = offset.LengthSquared() > 0 ? offset.Normalized() : -Direction;
        Rotation = Direction.Angle();
        Modulate = new Color(0.1f, 0.95f, 1.0f); // Turns cyan when reflected back at Void Commander
    }

    /// <summary>Cancels and safely frees the projectile.</summary>
    public void Cancel()
    {
        if (_cancelled) return;
        _cancelled = true;
        ProcessMode = ProcessModeEnum.Disabled;
        QueueFree();
    }

    /// <summary>Deterministic swept collision to prevent tunneling and handle reflections safely.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_cancelled) return;
        _age += (float)delta;
        if (_age >= Lifetime || !IsInstanceValid(OwningBoss) || OwningBoss!.IsDying || OwningBoss.IsQueuedForDeletion())
        {
            Cancel();
            return;
        }

        if (Direction.LengthSquared() <= 0)
        {
            Cancel();
            return;
        }

        if (Reflected && IsInstanceValid(OwningBoss))
        {
            Vector2 offset = OwningBoss.GlobalPosition - GlobalPosition;
            if (offset.LengthSquared() > 0)
            {
                Direction = Direction.Lerp(offset.Normalized(), 4.5f * (float)delta);
                Rotation = Direction.Angle();
            }
        }

        Vector2 start = GlobalPosition;
        Vector2 end = start + Direction * Speed * (float)delta;

        // Swept raycast check
        var space = GetWorld2D()?.DirectSpaceState;
        if (space != null)
        {
            var query = PhysicsRayQueryParameters2D.Create(start, end, Reflected ? 4u : 1u);
            query.HitFromInside = true;
            var hit = space.IntersectRay(query);

            if (hit.Count > 0)
            {
                var collider = hit["collider"].As<Node>();
                if (Reflected && collider == OwningBoss)
                {
                    OwningBoss?.TakeDamage(Damage * 1.6f);
                    Cancel();
                    return;
                }
                if (!Reflected && collider is CyberShogun player)
                {
                    player.TakeDamage(Damage);
                    Cancel();
                    return;
                }
            }
        }

        GlobalPosition = end;
    }
}
