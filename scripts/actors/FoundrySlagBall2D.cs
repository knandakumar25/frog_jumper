using Godot;

/// <summary>
/// Swept molten mortar projectile launched by IGNIS-COLOSSUS during Mission 7.
/// Deflectable by Kaito's katana parry; reflection redirects the superheated magma back to the boss.
/// </summary>
public partial class FoundrySlagBall2D : Area2D
{
    /// <summary>Travel speed in pixels per second.</summary>
    [Export] public float Speed { get; set; } = 460f;

    /// <summary>Impact damage inflicted on player if not parried or bulwarked.</summary>
    [Export] public float Damage { get; set; } = 22f;

    /// <summary>Maximum flight time before self-terminating.</summary>
    [Export] public float Lifetime { get; set; } = 4.5f;

    /// <summary>Initial launch trajectory.</summary>
    public Vector2 Direction { get; set; } = Vector2.Left;

    /// <summary>Reference to the boss that launched this projectile.</summary>
    public IgnisColossusBoss2D? OwningBoss { get; set; }

    /// <summary>Whether this projectile has been successfully parried and redirected.</summary>
    public bool Reflected { get; private set; }

    private float _age;
    private bool _cancelled;
    private CyberShogun? _reflector;

    /// <summary>Initializes collision configuration and adds to projectile groups.</summary>
    public override void _Ready()
    {
        AddToGroup("foundry_projectiles");
        Monitoring = false;
        CollisionLayer = 0;
        Rotation = Direction.LengthSquared() > 0 ? Direction.Angle() : 0;
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
            if (offset.LengthSquared() > 0) Direction = offset.Normalized();
        }

        Vector2 end = GlobalPosition + Direction * Speed * (float)delta;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, end, Reflected ? 5u : 1u);
        query.HitFromInside = true;

        if (Reflected)
        {
            var excluded = new Godot.Collections.Array<Rid>();
            if (IsInsideTree())
            {
                foreach (Node node in GetTree().GetNodesInGroup("player"))
                    if (node is CollisionObject2D body) excluded.Add(body.GetRid());
            }
            if (IsInstanceValid(_reflector)) excluded.Add(_reflector!.GetRid());
            query.Exclude = excluded;
        }

        var hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count > 0)
        {
            var body = hit["collider"].AsGodotObject();
            if (body is CyberShogun player && !Reflected)
            {
                _reflector = player;
                bool parrying = player.IsParrying && !player.IsBulwarked;
                GlobalPosition = end;
                player.TakeDamageFrom(Damage, Direction * 280 + new Vector2(0, -90), this);
                if (parrying && Reflected) return;
            }
            else if (Reflected && body == OwningBoss)
            {
                OwningBoss.TakeDamage(40f);
            }
            Cancel();
            return;
        }

        GlobalPosition = end;
        Rotation = Direction.Angle();
    }
}
