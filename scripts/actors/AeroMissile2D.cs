using Godot;

/// <summary>
/// Swept high-velocity plasma missile launched by Aero-Centurion during Mission 8 (Stratos Docks).
/// Deflectable by Kaito's katana parry; reflection redirects the missile back to the boss.
/// </summary>
public partial class AeroMissile2D : Area2D, ICampaignProjectile
{
    /// <summary>Travel speed in pixels per second.</summary>
    [Export] public float Speed { get; set; } = 480f;

    /// <summary>Impact damage inflicted on player if not parried or bulwarked.</summary>
    [Export] public float Damage { get; set; } = 22f;

    /// <summary>Maximum flight time before self-terminating.</summary>
    [Export] public float Lifetime { get; set; } = 4.5f;

    /// <summary>Initial launch trajectory.</summary>
    public Vector2 Direction { get; set; } = Vector2.Left;

    /// <summary>Reference to the boss that launched this projectile.</summary>
    public AeroCenturionBoss2D? OwningBoss { get; set; }

    /// <summary>Whether this projectile has been successfully parried and redirected.</summary>
    public bool Reflected { get; private set; }

    private CyberShogun? _reflector;
    private float _age;
    private bool _cancelled;
    private Line2D? _trail;

    /// <summary>Initializes collision configuration, trail rendering, and adds to projectile groups.</summary>
    public override void _Ready()
    {
        AddToGroup("aero_projectiles");
        Monitoring = false;
        CollisionLayer = 0;
        Rotation = Direction.LengthSquared() > 0 ? Direction.Angle() : 0;

        // Visual missile body
        var body = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(14, 0),
                new Vector2(-10, -5),
                new Vector2(-6, 0),
                new Vector2(-10, 5)
            },
            Color = new Color(1.0f, 0.45f, 0.1f) // Aviation amber/orange
        };
        AddChild(body);

        _trail = new Line2D
        {
            Width = 3f,
            DefaultColor = new Color(1f, 0.6f, 0.2f, 0.7f)
        };
        _trail.AddPoint(new Vector2(-6, 0));
        _trail.AddPoint(new Vector2(-26, 0));
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
        _age = 0;
        Vector2 offset = OwningBoss.GlobalPosition - GlobalPosition;
        Direction = offset.LengthSquared() > 0 ? offset.Normalized() : -Direction;
        Rotation = Direction.Angle();
        Modulate = new Color("#00f0ff"); // Turns cyan when parried
    }

    /// <summary>Cancels and safely frees the projectile.</summary>
    public void Cancel()
    {
        _cancelled = true;
        ProcessMode = ProcessModeEnum.Disabled;
        QueueFree();
    }

    /// <summary>Deterministic swept collision to prevent tunneling and handle reflections safely.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_cancelled) return;
        _age += (float)delta;
        if (_age >= Lifetime || !IsInstanceValid(OwningBoss) || OwningBoss!.IsDying || OwningBoss.IsQueuedForDeletion() || Direction.LengthSquared() <= 0)
        { Cancel(); return; }
        if (Reflected)
        {
            Vector2 offset = OwningBoss.GlobalPosition - GlobalPosition;
            if (offset.LengthSquared() > 0) Direction = offset.Normalized();
        }
        Vector2 end = GlobalPosition + Direction * Speed * (float)delta;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, end, Reflected ? 2u : 1u);
        query.HitFromInside = true;
        if (Reflected)
        {
            // Return fire belongs only to its launching boss; other enemies and the reflector cannot consume it.
            var excluded = new Godot.Collections.Array<Rid>();
            if (IsInstanceValid(_reflector)) excluded.Add(_reflector!.GetRid());
            foreach (Node enemy in GetTree().GetNodesInGroup("enemies"))
                if (enemy is CollisionObject2D body && body != OwningBoss) excluded.Add(body.GetRid());
            query.Exclude = excluded;
        }
        var hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count > 0)
        {
            var collider = hit["collider"].AsGodotObject();
            if (!Reflected && collider is CyberShogun player)
            {
                _reflector = player;
                GlobalPosition = hit["position"].AsVector2();
                player.TakeDamageFrom(Damage, Direction * 160f, this);
                // Parry changes this very projectile synchronously. Keep its new return trajectory alive.
                if (Reflected) { GlobalPosition += Direction * 4; return; }
                Cancel(); return;
            }
            if (Reflected && collider == OwningBoss)
            {
                OwningBoss.TakeDamage(Damage * 1.5f);
                Cancel(); return;
            }
            if (!Reflected && collider is StaticBody2D) { Cancel(); return; }
        }
        GlobalPosition = end;
        Rotation = Direction.Angle();
    }
}
