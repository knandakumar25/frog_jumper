using Godot;

/// <summary>Demolition saw-drone with readable patrols, a telegraphed beam and cover-aware line-of-sight.</summary>
public partial class PalaceSentry : CharacterBody2D
{
    /// <summary>Initial integrity; reinforcement drones can be configured before entering the tree.</summary>
    public float MaxHealth { get; set; } = 70;
    /// <summary>Remaining integrity for damage feedback and encounter checks.</summary>
    public float CurrentHealth { get; private set; }
    /// <summary>Telegraph length before the locked beam fires; mission encounters may tune it without changing damage.</summary>
    public float WarningDuration { get; set; } = .8f;
    /// <summary>Target is assigned by the mission so enemies never bind to another scene's player.</summary>
    public CyberShogun Target { get; set; } = null!;
    /// <summary>Emitted once, before the drone is freed, to award mission score.</summary>
    [Signal] public delegate void DefeatedEventHandler();
    private Vector2 _origin, _aim;
    private float _time, _cooldown = 1.8f, _windup, _beam, _flash, _contact;
    private bool _dead;
    private AudioStreamPlayer2D? _beamSound;
    private Sprite2D? _body;

    /// <summary>Installs the layer-two hurtbox used by the platform controller's real melee queries.</summary>
    public override void _Ready()
    {
        _origin = Position;
        CurrentHealth = MaxHealth;
        CollisionLayer = 2;
        CollisionMask = 0;
        AddToGroup("enemies");
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 34 } });
        _beamSound = StandInAssets.AddPlayer(this, StandInAssets.BeamShot, -9);
        // The drone fills only about 15 of its frame's 32 pixels, so 4.5x matches the 34-radius hurtbox.
        _body = StandInAssets.AddSheet(this, StandInAssets.DroneIdle, 8, 1, 4.5f, Vector2.Zero, TextureFilterEnum.Nearest);
    }

    /// <summary>Patrols a bounded hover lane, warns before firing, and damages only a player visible past world collision.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_dead || !GodotObject.IsInstanceValid(Target)) return;
        float dt = (float)delta;
        _time += dt;
        _cooldown -= dt;
        _contact -= dt;
        _flash = Mathf.Max(0, _flash - dt);
        _beam = Mathf.Max(0, _beam - dt);
        if (_windup <= 0)
            Position = _origin + new Vector2(Mathf.Sin(_time * .65f) * 74, Mathf.Sin(_time * 2.2f) * 12);
        Vector2 difference = Target.GlobalPosition - GlobalPosition;
        if (difference.LengthSquared() < 70 * 70 && _contact <= 0)
        {
            Target.TakeDamageFrom(14, new Vector2(difference.X < 0 ? -180 : 180, -100), this);
            _contact = 1.1f;
        }
        if (_windup > 0)
        {
            _windup -= dt;
            if (_windup <= 0)
            {
                _beam = .13f;
                StandInAssets.Play(_beamSound);
                var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + _aim, 1);
                var result = GetWorld2D().DirectSpaceState.IntersectRay(query);
                if (result.Count > 0 && result["collider"].AsGodotObject() is CyberShogun player)
                    player.TakeDamageFrom(18, default, this);
                _cooldown = 2.8f;
            }
        }
        else if (_cooldown <= 0 && difference.LengthSquared() < 500 * 500 && Target.CurrentHealth > 0)
        {
            // Aim is locked during the warning so players can jump or dash out of the beam.
            _aim = difference.LengthSquared() > .001f ? difference.Normalized() * 550 : Vector2.Left * 550;
            _windup = WarningDuration;
        }
        if (_body != null)
        {
            _body.Frame = (int)(_time * 10) % 8;
            // Values above 1 brighten the sprite, standing in for the hull's drawn hit flash.
            _body.Modulate = _flash > 0 ? new Color(2.2f, 2.2f, 2.2f) : Colors.White;
        }
        QueueRedraw();
    }

    /// <summary>Receives melee damage and emits a single defeat event at zero integrity.</summary>
    public void TakeDamage(float damage)
    {
        if (_dead || damage <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        _flash = .15f;
        if (CurrentHealth <= 0)
        {
            _dead = true;
            StandInAssets.PlayDetached(this, StandInAssets.Explosion, -8);
            EmitSignal(SignalName.Defeated);
            QueueFree();
        }
        QueueRedraw();
    }

    /// <summary>Draws armored saw housings, scanning optics, exhaust and the warning/fire state.</summary>
    public override void _Draw()
    {
        if (_windup > 0 || _beam > 0)
            DrawLine(Vector2.Zero, _aim, _beam > 0 ? new Color("#ffad73") : new Color(1, .39f, .36f, .3f), _beam > 0 ? 7 : 1.5f, true);
        if (_body == null) DrawHull();
        if (CurrentHealth < MaxHealth)
        {
            DrawLine(new Vector2(-30,-44),new Vector2(30,-44),new Color("#16222d"),4);
            DrawLine(new Vector2(-30,-44),new Vector2(-30 + 60 * CurrentHealth / MaxHealth,-44),new Color("#e889a2"),4);
        }
    }

    /// <summary>Draws the original code-based saw housings and hull, used when the stand-in sprite is absent.</summary>
    private void DrawHull()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 center = new(side * 31, 9);
            DrawCircle(center, 22, new Color("#0d1725"));
            for (int tooth = 0; tooth < 10; tooth++)
            {
                float a = tooth * Mathf.Tau / 10 + _time * side * 3;
                DrawLine(center + Vector2.FromAngle(a) * 16, center + Vector2.FromAngle(a + .1f) * 23, new Color("#6e8593"), 4, true);
            }
            DrawArc(center, 12, 0, Mathf.Tau, 24, new Color("#ba6479"), 2, true);
            DrawLine(new Vector2(side * 18, 26), new Vector2(side * 18, 36 + Mathf.Sin(_time * 14) * 5), new Color("#81e3e6"), 5, true);
        }
        DrawColoredPolygon(new Vector2[] { new(-36,-11),new(-22,-28),new(22,-28),new(36,-11),new(25,22),new(-25,22) }, _flash > 0 ? new Color("#ffe9db") : new Color("#263748"));
        DrawPolyline(new Vector2[] { new(-32,-9),new(-20,-23),new(20,-23),new(32,-9) }, new Color("#9a9ca8"), 3, true);
        DrawLine(new Vector2(-20, -5), new Vector2(20, -5), new Color("#08131e"), 14, true);
        DrawLine(new Vector2(-14, -6), new Vector2(14, -6), new Color(_windup > 0 ? "#ff9d79" : "#a0f0ee"), 4, true);
        DrawLine(new Vector2(-8,14), new Vector2(8,14), new Color("#b27672"), 3, true);
    }
}
