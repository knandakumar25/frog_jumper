using Godot;

/// <summary>Stationary low-profile gunner: lock aim, fire three parryable cyan rounds, then visibly recover.</summary>
public partial class WildfireGunner : CharacterBody2D
{
    /// <summary>Player supplied by the owning encounter, never discovered across scene boundaries.</summary>
    public CyberShogun Target { get; set; } = null!;
    /// <summary>Mission owns transient bullets and recovery cancellation.</summary>
    public IFiniteCombatHost Mission { get; set; } = null!;
    /// <summary>Single death notification for the finite encounter.</summary>
    [Signal] public delegate void DefeatedEventHandler();
    /// <summary>Normal sentry-equivalent integrity; difficulty comes from composition, not health inflation.</summary>
    public float Health { get; private set; } = 70;
    /// <summary>Readable state exposed for diagnostics.</summary>
    public string State { get; private set; } = "recovery";
    /// <summary>Time the locked aim remains visible before the three-shot burst begins.</summary>
    public float WarningDuration { get; set; } = 1.1f;
    /// <summary>Time the gunner rests after its finite burst before it can aim again.</summary>
    public float RecoveryDuration { get; set; } = 2.1f;
    private float _clock = 1.2f;
    private int _shots;
    private Vector2 _aim = Vector2.Left;

    /// <summary>Builds a layer-two melee hurtbox and installs the matching generated sprite when available.</summary>
    public override void _Ready()
    {
        CollisionLayer = 2;
        CollisionMask = 0;
        AddToGroup("enemies");
        AddChild(new CollisionShape2D { Position = new Vector2(0, -36),
            Shape = new RectangleShape2D { Size = new Vector2(64, 72) } });
        WildfireVisual.AddSprite(this, "gunner", new Vector2(110, 100), new Vector2(0, -44));
    }

    /// <summary>Locks a clear firing line for the configured warning; bullets never home after warning.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Health <= 0 || Mission.GameplaySuspended || !IsInstanceValid(Target)) return;
        _clock -= (float)delta;
        Vector2 muzzle = GlobalPosition + new Vector2(0, -45);
        Vector2 difference = Target.GlobalPosition - muzzle;
        if (_clock <= 0)
        {
            if (State == "aim") { State = "burst"; _shots = 0; }
            if (State == "burst")
            {
                Mission.LaunchRound(muzzle, _aim, this);
                _shots++;
                _clock = .22f;
                if (_shots == 3) { State = "recovery"; _clock = RecoveryDuration; }
            }
            else if (difference.LengthSquared() < 650 * 650 && Target.CurrentHealth > 0)
            {
                var hit = GetWorld2D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters2D.Create(muzzle, Target.GlobalPosition, 1));
                if (hit.Count > 0 && hit["collider"].AsGodotObject() == Target)
                {
                    _aim = difference.LengthSquared() > .001f ? difference.Normalized() : Vector2.Left;
                    State = "aim";
                    _clock = WarningDuration;
                }
            }
        }
        QueueRedraw();
    }

    /// <summary>Supports both the player's melee query and the existing ranged parry reflection.</summary>
    public void TakeDamage(float damage)
    {
        if (Health <= 0 || damage <= 0) return;
        Health = Mathf.Max(0, Health - damage);
        if (Health > 0) return;
        EmitSignal(SignalName.Defeated);
        QueueFree();
    }

    /// <summary>State markers remain legible over the sprite: thin cyan aim, bright muzzle, gray recovery.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(-32, -12, 64, 12), new Color("#263748"));
        DrawCircle(new Vector2(0, -47), 7, State == "recovery" ? new Color("#83929e") : new Color("#77efff"));
        if (State == "aim") DrawLine(new Vector2(0, -45), new Vector2(0, -45) + _aim * 620,
            new Color(.3f, .9f, 1f, .55f), 2);
    }
}
