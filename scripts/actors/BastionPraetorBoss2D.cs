using Godot;
using System;

/// <summary>
/// Bastion Praetor is the penultimate multi-phase 2D boss mech encounter in Mission 11 (The Apex Bastion).
/// Stationed on the high stratospheric battlements of the Grand Citadel, featuring heavy jet thrusters,
/// twin-linked swept plasma flak bolts, deployable defensive energy shields, and a catastrophic Praetor Overdrive phase shift.
/// </summary>
public partial class BastionPraetorBoss2D : CharacterBody2D
{
    /// <summary>Emitted when health or phase state updates for HUD integration.</summary>
    [Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);

    /// <summary>Emitted when the boss is destroyed.</summary>
    [Signal] public delegate void BossDefeatedEventHandler();

    /// <summary>Maximum health across all 3 phases.</summary>
    [Export] public float MaxHealth { get; set; } = 620.0f;

    /// <summary>Patrol travel radius across the fortress ramparts.</summary>
    [Export] public float PatrolDistance { get; set; } = 580.0f;

    /// <summary>Base strafe velocity.</summary>
    [Export] public float BaseSpeed { get; set; } = 190.0f;

    /// <summary>Point reward upon defeat.</summary>
    [Export] public int PointValue { get; set; } = 300;

    /// <summary>Current health pool.</summary>
    public float CurrentHealth { get; private set; } = 620.0f;

    /// <summary>Current combat phase (1, 2, or 3).</summary>
    public int CurrentPhase { get; private set; } = 1;

    /// <summary>Whether the boss has reached 0 HP and is in defeat sequence.</summary>
    public bool IsDying { get; private set; } = false;

    /// <summary>Explicit mission-owned player used for aiming; no global group lookup is required.</summary>
    public CyberShogun? Target { get; set; }
    /// <summary>Mission authority for accepting the current health boundary after its defenders are cleared.</summary>
    public Func<int, bool>? PhaseAcceptance { get; set; }
    /// <summary>True at a health boundary while supporting defenders still prevent phase advancement.</summary>
    public bool PhaseBlocked { get; private set; }

    private Vector2 _originPosition;
    private float _hoverTimer;
    private float _attackCooldown;
    private Node2D? _visualRoot;
    private Node2D? _energyShield;

    /// <summary>Initializes boss positioning, groups, SpriteCook artwork, and HUD telemetry.</summary>
    public override void _Ready()
    {
        CollisionLayer = 2;
        CollisionMask = 1;
        AddToGroup("enemies");
        AddToGroup("boss");
        _originPosition = GlobalPosition;
        CurrentHealth = MaxHealth;

        SetupVisuals();
        CampaignArtwork.AttachBoss(this, "apex_bastion");
        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
    }

    /// <summary>Creates the artwork root, combat indicators, and collision shape if not pre-configured.</summary>
    private void SetupVisuals()
    {
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
        {
            var col = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new RectangleShape2D { Size = new Vector2(110, 130) }
            };
            AddChild(col);
        }

        _visualRoot = new Node2D { Name = "VisualRoot" };
        AddChild(_visualRoot);

        // Deployable energy shield barrier
        _energyShield = new Node2D { Name = "EnergyShield", Visible = false };
        _visualRoot.AddChild(_energyShield);

        for (int i = 0; i < 6; i++)
        {
            float a1 = i * Mathf.Pi / 3.0f;
            float a2 = (i + 1) * Mathf.Pi / 3.0f;
            var shieldSegment = new Line2D
            {
                Points = new[]
                {
                    new Vector2(Mathf.Cos(a1) * 75f, Mathf.Sin(a1) * 75f),
                    new Vector2(Mathf.Cos(a2) * 75f, Mathf.Sin(a2) * 75f)
                },
                Width = 4f,
                DefaultColor = new Color(1.0f, 0.3f, 0.1f, 0.8f)
            };
            _energyShield.AddChild(shieldSegment);
        }
    }

    /// <summary>Applies damage to health pool and drives phase transitions.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDying || amount < 0 || !float.IsFinite(amount)) return;
        float boundary = PhaseFloor;
        int oldPhase = CurrentPhase;
        bool wasBlocked = PhaseBlocked;
        CurrentHealth = Math.Max(boundary, CurrentHealth - amount);
        PhaseBlocked = CurrentHealth <= boundary && PhaseAcceptance?.Invoke(CurrentPhase) == false;
        if (CurrentHealth <= boundary && !PhaseBlocked && CurrentPhase < 3)
        {
            ++CurrentPhase;
            OnPhaseShift(CurrentPhase);
        }
        if (amount > 0 || oldPhase != CurrentPhase || wasBlocked != PhaseBlocked)
            EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
        if (CurrentHealth <= 0 && !PhaseBlocked)
        {
            IsDying = true; OnDefeated();
        }
    }

    /// <summary>Damage stops at the current authored phase threshold; excess damage never skips defenders or a phase.</summary>
    private float PhaseFloor => CurrentPhase switch { 1 => MaxHealth * 0.70f, 2 => MaxHealth * 0.35f, _ => 0 };

    /// <summary>Updates the phase-specific energy shield indicator.</summary>
    private void OnPhaseShift(int newPhase)
    {
        if (_energyShield != null)
        {
            _energyShield.Visible = newPhase >= 2;
        }
    }

    /// <summary>Stops owned attacks and emits the accepted defeat before visual teardown.</summary>
    private void OnDefeated()
    {
        // Accepted defeat immediately removes the hurtbox; visual fade may outlive checkpoint recovery.
        SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);
        EmitSignal(SignalName.BossDefeated);

        // Clear active hostile projectiles
        var tree = GetTree();
        if (tree != null)
        {
            foreach (Node node in tree.GetNodesInGroup("praetor_projectiles"))
            {
                if (node is PraetorPlasmaBolt2D bolt) bolt.Cancel();
            }
        }

        // Defeat shrink tween
        if (IsInsideTree())
        {
            var tween = CreateTween();
            tween?.TweenProperty(this, "scale", Vector2.Zero, 0.7f);
            tween?.TweenCallback(Callable.From(QueueFree));
        }
        else
        {
            QueueFree();
        }
    }

    /// <summary>Drives boss thruster hover, fortress strafing, and phase-scaled projectile combat.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (IsDying) return;
        if (CurrentHealth <= PhaseFloor) TakeDamage(0);
        if (IsDying) return;

        _hoverTimer += (float)delta;
        if (_energyShield != null && _energyShield.Visible)
        {
            _energyShield.Rotation += (float)delta * (CurrentPhase * 1.8f);
        }

        // Horizontal sinusoidal patrol and vertical hover
        float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.75f) * PatrolDistance;
        float targetY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.0f) * 45f;
        GlobalPosition = new Vector2(targetX, targetY);

        // Combat attack cadence
        _attackCooldown -= (float)delta;
        if (_attackCooldown <= 0f)
        {
            ExecuteAttackPattern();
            _attackCooldown = CurrentPhase switch
            {
                1 => 2.5f,
                2 => 1.8f,
                _ => 1.2f
            };
        }
    }

    /// <summary>Aims the current phase attack at the explicit mission player.</summary>
    private void ExecuteAttackPattern()
    {
        if (!IsInsideTree() || IsDying) return;

        Node2D? target = IsInstanceValid(Target) ? Target : null;
        Vector2 targetPos = target?.GlobalPosition ?? (GlobalPosition + Vector2.Left * 400);
        Vector2 diff = targetPos - GlobalPosition;
        Vector2 dir = diff.LengthSquared() > 0 ? diff.Normalized() : Vector2.Left;

        switch (CurrentPhase)
        {
            case 1:
                SpawnBolt(dir);
                break;
            case 2:
                SpawnBolt(dir.Rotated(-0.14f));
                SpawnBolt(dir.Rotated(0.14f));
                break;
            case 3:
                SpawnBolt(dir);
                SpawnBolt(dir.Rotated(0.24f));
                SpawnBolt(dir.Rotated(-0.24f));
                break;
        }
    }

    /// <summary>Creates a projectile owned by this arena attempt.</summary>
    private void SpawnBolt(Vector2 direction)
    {
        var parent = GetParent();
        if (parent == null) return;

        var bolt = new PraetorPlasmaBolt2D
        {
            GlobalPosition = GlobalPosition + direction * 45f,
            Direction = direction,
            OwningBoss = this
        };
        parent.AddChild(bolt);
    }
}
