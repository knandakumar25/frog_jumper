using Godot;

/// <summary>Owned, bounded, swept missile. Parry redirects the actual projectile to its launching Voss.</summary>
public partial class CyberBossMissile2D : Area2D
{
    /// <summary>Travel speed in pixels per second.</summary>
    [Export] public float Speed { get; set; } = 420;
    /// <summary>Source-aware impact damage; Bulwark absorbs it and Blink does not grant ranged immunity.</summary>
    [Export] public float Damage { get; set; } = 18;
    /// <summary>Maximum life, including the reflected return journey.</summary>
    [Export] public float Lifetime { get; set; } = 4;
    /// <summary>Locked launch heading, replaced on reflection.</summary>
    public Vector2 Direction { get; set; } = Vector2.Left;
    /// <summary>Explicit owner; removing or defeating it immediately invalidates this projectile.</summary>
    public CyberBossMech2D? OwningBoss { get; set; }
    /// <summary>True after an actual player parry.</summary>
    public bool Reflected { get; private set; }
    private float _age;
    private bool _cancelled;
    private CyberShogun? _reflector;
    /// <summary>Disables area monitoring in favor of deterministic swept collision, retaining the existing missile visuals.</summary>
    public override void _Ready()
    {
        AddToGroup("aegis_projectiles");
        Monitoring = false;
        CollisionLayer = 0;
        Rotation = Direction.Angle();
    }
    /// <summary>Returns toward the live owner; a missing owner consumes the shot without fabricating damage.</summary>
    public void Reflect()
    {
        if (_cancelled || !IsInstanceValid(OwningBoss) || OwningBoss!.IsDying) { Cancel(); return; }
        Reflected = true;
        Vector2 offset = OwningBoss.GlobalPosition - GlobalPosition;
        Direction = offset.LengthSquared() > 0 ? offset.Normalized() : -Direction;
        Rotation = Direction.Angle();
        Modulate = new Color("#77efff");
    }
    /// <summary>Consumes the shot synchronously; deferred deletion is safe during a parry or physics callback.</summary>
    public void Cancel() { _cancelled = true; ProcessMode = ProcessModeEnum.Disabled; QueueFree(); }
    /// <summary>Sweeps thin cover and the player/owner, avoiding tunneling and zero-vector normalization.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_cancelled) return;
        _age += (float)delta;
        if (_age >= Lifetime || !IsInstanceValid(OwningBoss) || OwningBoss!.IsDying || OwningBoss.IsQueuedForDeletion()) { Cancel(); return; }
        if (OwningBoss.FortressController?.Mission.GameplaySuspended == true) { Cancel(); return; }
        if (Direction.LengthSquared() <= 0) { Cancel(); return; }
        if (Reflected)
        {
            Vector2 offset = OwningBoss.GlobalPosition - GlobalPosition;
            if (offset.LengthSquared() > 0) Direction = offset.Normalized();
        }
        Vector2 end = GlobalPosition + Direction * Speed * (float)delta;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, end, Reflected ? 5u : 1u);
        query.HitFromInside = true;
        if (Reflected)
        {
            // Player and world share layer 1; exclude the player so counterfire can leave its collider.
            var excluded = new Godot.Collections.Array<Rid>();
            foreach (Node node in GetTree().GetNodesInGroup("player"))
                if (node is CollisionObject2D body) excluded.Add(body.GetRid());
            var player = OwningBoss.FortressController?.Mission.Player;
            if (player != null) excluded.Add(player.GetRid());
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
                player.TakeDamageFrom(Damage, Direction * 260 + new Vector2(0, -80), this);
                if (parrying && Reflected) return;
            }
            else if (Reflected && body == OwningBoss)
            {
                if (OwningBoss.FortressController != null) OwningBoss.TakeReflectedDamage(35);
                else OwningBoss.TakeDamage(35);
            }
            Cancel();
            return;
        }
        GlobalPosition = end;
        Rotation = Direction.Angle();
    }
}
