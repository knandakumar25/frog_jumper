using Godot;

/// <summary>
/// Swept molten mortar projectile launched by IGNIS-COLOSSUS during Mission 7.
/// Deflectable by Kaito's katana parry; reflection redirects the superheated magma back to the boss.
/// </summary>
public partial class FoundrySlagBall2D : Area2D, ICampaignProjectile
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
                OwningBoss.TakeDamage(40f);
                Cancel(); return;
            }
            if (!Reflected && collider is StaticBody2D) { Cancel(); return; }
        }
        GlobalPosition = end;
        Rotation = Direction.Angle();
    }
    /// <summary>Readable molten core and hot trail for the code-built IGNIS artillery.</summary>
    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 13, new Color("ff6622"));
        DrawCircle(Vector2.Zero, 6, new Color("fff0ac"));
        DrawLine(new Vector2(-32, 0), Vector2.Zero, new Color("ff9a3388"), 9);
    }

}
