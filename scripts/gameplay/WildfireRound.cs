using Godot;

/// <summary>Finite swept cyan bullet. First world collision consumes it; source-aware damage uses the existing parry.</summary>
public partial class WildfireRound : Node2D
{
    /// <summary>Locked normalized heading and owning gunner.</summary>
    public Vector2 Heading { get; set; }
    /// <summary>Reflection damages this attacker; an invalid source consumes the bullet without damage.</summary>
    public WildfireGunner Source { get; set; } = null!;
    private float _remaining = 2.2f;
    /// <summary>Allows the mission to remove all transient rounds before recovery.</summary>
    public override void _Ready() => AddToGroup("wildfire_rounds");
    /// <summary>Sweeps the full distance each physics tick so thin cover cannot be tunneled through.</summary>
    public override void _PhysicsProcess(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0 || !IsInstanceValid(Source) || Source.IsQueuedForDeletion()) { QueueFree(); return; }
        Vector2 end = GlobalPosition + Heading * 480 * (float)delta;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, end, 1);
        // Close-range rounds can begin inside the player's body; consume that overlap instead of shooting through it.
        query.HitFromInside = true;
        var hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count > 0)
        {
            if (hit["collider"].AsGodotObject() is CyberShogun player) player.TakeDamageFrom(12, default, Source);
            QueueFree();
            return;
        }
        GlobalPosition = end;
    }
    /// <summary>Cyan tracers are visually distinct from orange ground artillery markers.</summary>
    public override void _Draw() => DrawLine(-Heading * 15, Heading * 4, new Color("#77efff"), 4, true);
}
