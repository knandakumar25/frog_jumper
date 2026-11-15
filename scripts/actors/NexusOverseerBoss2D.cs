using Godot;
using System;

/// <summary>
/// Nexus Overseer V-10 is the multi-phase 2D boss mech encounter in Mission 10 (The Cyber-Nexus).
/// Anchored to the central Quantum Core Arena in Sector 5, featuring magnetic hover levitation,
/// swept quantum pulse bolt barrages, rotating orbital containment prisms, and a catastrophic Core Overdrive phase shift.
/// </summary>
public partial class NexusOverseerBoss2D : CharacterBody2D
{
    /// <summary>Emitted when health or phase state updates for HUD integration.</summary>
    [Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);

    /// <summary>Emitted when the boss is destroyed.</summary>
    [Signal] public delegate void BossDefeatedEventHandler();

    /// <summary>Maximum health across all 3 phases.</summary>
    [Export] public float MaxHealth { get; set; } = 560.0f;

    /// <summary>Patrol travel radius across the core arena.</summary>
    [Export] public float PatrolDistance { get; set; } = 560.0f;

    /// <summary>Base strafe velocity.</summary>
    [Export] public float BaseSpeed { get; set; } = 180.0f;

    /// <summary>Point reward upon defeat.</summary>
    [Export] public int PointValue { get; set; } = 250;

    /// <summary>Current health pool.</summary>
    public float CurrentHealth { get; private set; } = 560.0f;

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
    private Node2D? _orbitalRing;

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
        CampaignArtwork.AttachBoss(this, "cyber_nexus");
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
                Shape = new RectangleShape2D { Size = new Vector2(96, 120) }
            };
            AddChild(col);
        }

        _visualRoot = new Node2D { Name = "VisualRoot" };
        AddChild(_visualRoot);

        // Orbital Ring with Prisms
        _orbitalRing = new Node2D { Name = "OrbitalRing" };
        _visualRoot.AddChild(_orbitalRing);

        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.Pi * 0.5f;
            var prism = new Polygon2D
            {
                Position = new Vector2(Mathf.Cos(angle) * 55f, Mathf.Sin(angle) * 55f),
                Polygon = new[]
                {
                    new Vector2(0, -8),
                    new Vector2(6, 0),
                    new Vector2(0, 8),
                    new Vector2(-6, 0)
                },
                Color = new Color(1.0f, 0.85f, 0.2f) // Gold optical prism
            };
            _orbitalRing.AddChild(prism);
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
        }
        if (amount > 0 || oldPhase != CurrentPhase || wasBlocked != PhaseBlocked)
            EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
        if (CurrentHealth <= 0 && !PhaseBlocked)
        {
            IsDying = true; OnDefeated();
        }
    }

    /// <summary>Damage stops at the current authored phase threshold; excess damage never skips defenders or a phase.</summary>
    private float PhaseFloor => CurrentPhase switch { 1 => MaxHealth * 0.66f, 2 => MaxHealth * 0.33f, _ => 0 };

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
            foreach (Node node in tree.GetNodesInGroup("nexus_projectiles"))
            {
                if (node is NexusPulseBolt2D bolt) bolt.Cancel();
            }
        }

        // Defeat shrink tween
        if (IsInsideTree())
        {
            var tween = CreateTween();
            tween?.TweenProperty(this, "scale", Vector2.Zero, 0.65f);
            tween?.TweenCallback(Callable.From(QueueFree));
        }
        else
        {
            QueueFree();
        }
    }

    /// <summary>Drives boss levitation, orbital ring rotation, and phase-scaled projectile combat.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (IsDying) return;
        if (CurrentHealth <= PhaseFloor) TakeDamage(0);
        if (IsDying) return;

        _hoverTimer += (float)delta;
        if (_orbitalRing != null)
        {
            _orbitalRing.Rotation += (float)delta * (CurrentPhase * 1.5f);
        }

        // Horizontal sinusoidal patrol and vertical hover
        float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.8f) * PatrolDistance;
        float targetY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.2f) * 50f;
        GlobalPosition = new Vector2(targetX, targetY);

        // Combat attack cadence
        _attackCooldown -= (float)delta;
        if (_attackCooldown <= 0f)
        {
            ExecuteAttackPattern();
            _attackCooldown = CurrentPhase switch
            {
                1 => 2.4f,
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
                SpawnBolt(dir);
                SpawnBolt(dir.Rotated(0.18f));
                break;
            case 3:
                SpawnBolt(dir);
                SpawnBolt(dir.Rotated(0.22f));
                SpawnBolt(dir.Rotated(-0.22f));
                break;
        }
    }

    /// <summary>Creates a projectile owned by this arena attempt.</summary>
    private void SpawnBolt(Vector2 direction)
    {
        var parent = GetParent();
        if (parent == null) return;

        var bolt = new NexusPulseBolt2D
        {
            GlobalPosition = GlobalPosition + direction * 40f,
            Direction = direction,
            OwningBoss = this
        };
        parent.AddChild(bolt);
    }
}
