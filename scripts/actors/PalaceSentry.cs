using Godot;

/// <summary>SpriteCook vanguard sentry with readable patrols, a telegraphed beam and cover-aware line-of-sight.</summary>
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
    private AnimatedSprite2D? _body;

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
        // The 256-pixel SpriteCook cell's body spans roughly y=65..224. Centre that body on
        // the retained hovering hurtbox; visual changes do not move the beam or collision.
        _body = new AnimatedSprite2D
        {
            Name = "SpriteCookBody",
            SpriteFrames = OperativeVisuals.LoadFramesAt("res://assets/characters/rendered/vanguard.tres"),
            Animation = "idle", Scale = Vector2.One * .42f, Offset = new Vector2(0, -16),
            TextureFilter = TextureFilterEnum.Nearest, ShowBehindParent = true,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        AddChild(_body);
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
            int frames = _body.SpriteFrames.GetFrameCount("idle");
            if (frames > 0) _body.Frame = (int)(_time * _body.SpriteFrames.GetAnimationSpeed("idle")) % frames;
            _body.FlipH = difference.X < 0;
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

    /// <summary>Draws only the beam telegraph and damage bar over the SpriteCook body.</summary>
    public override void _Draw()
    {
        if (_windup > 0 || _beam > 0)
            DrawLine(Vector2.Zero, _aim, _beam > 0 ? new Color("#ffad73") : new Color(1, .39f, .36f, .3f), _beam > 0 ? 7 : 1.5f, true);
        if (CurrentHealth < MaxHealth)
        {
            DrawLine(new Vector2(-30,-44),new Vector2(30,-44),new Color("#16222d"),4);
            DrawLine(new Vector2(-30,-44),new Vector2(-30 + 60 * CurrentHealth / MaxHealth,-44),new Color("#e889a2"),4);
        }
    }

}
