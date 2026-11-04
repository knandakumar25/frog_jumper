using Godot;
using System;

/// <summary>
/// Aero-Centurion is the multi-phase 2D boss mech encounter in Mission 8 (Stratos Docks).
/// Anchored to Flight Deck Bay 4, featuring twin jet thrusters, swept plasma missile barrages,
/// high-altitude strafe maneuvers, and an Overdrive Storm phase shift.
/// </summary>
public partial class AeroCenturionBoss2D : CharacterBody2D
{
    /// <summary>Emitted when health or phase state updates for HUD integration.</summary>
    [Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);

    /// <summary>Emitted when the boss is destroyed.</summary>
    [Signal] public delegate void BossDefeatedEventHandler();

    /// <summary>Maximum health across all 3 phases.</summary>
    [Export] public float MaxHealth { get; set; } = 480.0f;

    /// <summary>Patrol travel radius across the flight deck.</summary>
    [Export] public float PatrolDistance { get; set; } = 480.0f;

    /// <summary>Base strafe velocity.</summary>
    [Export] public float BaseSpeed { get; set; } = 150.0f;

    /// <summary>Point reward upon defeat.</summary>
    [Export] public int PointValue { get; set; } = 175;

    /// <summary>Current health pool.</summary>
    public float CurrentHealth { get; private set; } = 480.0f;

    /// <summary>Current combat phase (1, 2, or 3).</summary>
    public int CurrentPhase { get; private set; } = 1;

    /// <summary>Whether the boss has reached 0 HP and is in defeat sequence.</summary>
    public bool IsDying { get; private set; } = false;

    private Vector2 _originPosition;
    private float _hoverTimer;
    private float _attackCooldown;
    private PointLight2D? _coreLight;
    private Node2D? _visualRoot;

    /// <summary>Initializes boss positioning, groups, procedural visuals, and HUD telemetry.</summary>
    public override void _Ready()
    {
        AddToGroup("enemies");
        AddToGroup("boss");
        _originPosition = GlobalPosition;
        CurrentHealth = MaxHealth;

        SetupVisuals();
        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
    }

    /// <summary>Creates procedural high-tech aviation mechanoid visuals and collision shape if not pre-configured.</summary>
    private void SetupVisuals()
    {
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
        {
            var col = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new RectangleShape2D { Size = new Vector2(80, 100) }
            };
            AddChild(col);
        }

        _visualRoot = new Node2D { Name = "VisualRoot" };
        AddChild(_visualRoot);

        // Mechanoid chassis polygon
        var chassis = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(0, -45),
                new Vector2(35, -20),
                new Vector2(40, 25),
                new Vector2(20, 45),
                new Vector2(-20, 45),
                new Vector2(-40, 25),
                new Vector2(-35, -20)
            },
            Color = new Color(0.2f, 0.28f, 0.38f) // Steel titanium aviation armor
        };
        _visualRoot.AddChild(chassis);

        // Jet wings
        var leftWing = new Polygon2D
        {
            Polygon = new[] { new Vector2(-35, -10), new Vector2(-65, 10), new Vector2(-40, 30) },
            Color = new Color(0.35f, 0.45f, 0.6f)
        };
        var rightWing = new Polygon2D
        {
            Polygon = new[] { new Vector2(35, -10), new Vector2(65, 10), new Vector2(40, 30) },
            Color = new Color(0.35f, 0.45f, 0.6f)
        };
        _visualRoot.AddChild(leftWing);
        _visualRoot.AddChild(rightWing);

        // Cockpit visor / core glow
        var visor = new Polygon2D
        {
            Polygon = new[] { new Vector2(-15, -15), new Vector2(15, -15), new Vector2(10, 0), new Vector2(-10, 0) },
            Color = new Color(0.1f, 0.8f, 1.0f) // Aero cyan
        };
        _visualRoot.AddChild(visor);

        _coreLight = new PointLight2D
        {
            Name = "CoreLight",
            Color = new Color(0.2f, 0.7f, 1.0f, 0.8f),
            Energy = 1.2f,
            TextureScale = 1.5f
        };
        AddChild(_coreLight);
    }

    /// <summary>Executes hover traversal, phase-specific attacks, and projectile volleys.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (IsDying) return;

        float dt = (float)delta;
        _hoverTimer += dt;
        _attackCooldown += dt;

        // Speed multiplier based on phase
        float speedMultiplier = CurrentPhase switch
        {
            2 => 1.6f,
            3 => 2.2f,
            _ => 1.0f
        };

        float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.75f) * PatrolDistance;
        float hoverY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.4f) * 32.0f;

        Velocity = new Vector2(
            (targetX - GlobalPosition.X) * 2.4f * speedMultiplier,
            (hoverY - GlobalPosition.Y) * 2.6f
        );

        MoveAndSlide();

        if (_visualRoot != null && Math.Abs(Velocity.X) > 5f)
        {
            _visualRoot.Scale = new Vector2(Velocity.X < 0f ? -1f : 1f, 1f);
        }

        // Phase-specific attack timing
        float interval = CurrentPhase switch
        {
            2 => 1.5f,
            3 => 1.0f,
            _ => 2.2f
        };

        if (_attackCooldown >= interval)
        {
            _attackCooldown = 0.0f;
            LaunchMissileBarrage();
        }
    }

    /// <summary>Spawns high-velocity plasma missiles directed toward the player.</summary>
    private void LaunchMissileBarrage()
    {
        if (!IsInsideTree() || IsDying) return;

        Node2D? target = GetTree().GetFirstNodeInGroup("player") as Node2D;
        Vector2 targetPos = target?.GlobalPosition ?? (GlobalPosition + Vector2.Left * 400);
        Vector2 diff = targetPos - GlobalPosition;
        Vector2 dir = diff.LengthSquared() > 0 ? diff.Normalized() : Vector2.Left;

        int count = CurrentPhase == 3 ? 3 : (CurrentPhase == 2 ? 2 : 1);
        for (int i = 0; i < count; i++)
        {
            float spreadAngle = (i - (count - 1) * 0.5f) * 0.25f;
            Vector2 shotDir = dir.Rotated(spreadAngle);

            var missile = new AeroMissile2D
            {
                GlobalPosition = GlobalPosition + new Vector2(Velocity.X < 0 ? -45 : 45, 0),
                Direction = shotDir,
                OwningBoss = this,
                Speed = CurrentPhase == 3 ? 550f : (CurrentPhase == 2 ? 500f : 450f),
                Damage = CurrentPhase == 3 ? 28f : 22f
            };

            GetParent()?.AddChild(missile);
        }
    }

    /// <summary>Applies incoming damage, managing phase transitions and defeat sequence.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDying) return;

        CurrentHealth = Math.Max(0, CurrentHealth - amount);

        // Check phase transitions
        if (CurrentHealth <= MaxHealth * 0.35f)
        {
            CurrentPhase = 3;
            if (_coreLight != null) _coreLight.Color = new Color(1f, 0.15f, 0.1f, 1f); // Emergency Overdrive Red
        }
        else if (CurrentHealth <= MaxHealth * 0.70f)
        {
            CurrentPhase = 2;
            if (_coreLight != null) _coreLight.Color = new Color(1f, 0.6f, 0.1f, 1f); // Storm Amber
        }

        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>Initiates the flight-deck defeat sequence and alerts listeners.</summary>
    private void Die()
    {
        if (IsDying) return;
        IsDying = true;
        SetPhysicsProcess(false);

        // Cancel any remaining owned missiles
        if (IsInsideTree())
        {
            foreach (Node node in GetTree().GetNodesInGroup("aero_projectiles"))
            {
                if (node is AeroMissile2D missile && missile.OwningBoss == this)
                {
                    missile.Cancel();
                }
            }
        }

        EmitSignal(SignalName.BossDefeated);

        if (IsInsideTree())
        {
            var tween = CreateTween();
            tween?.TweenProperty(this, "modulate:a", 0.0f, 1.2f);
            tween?.TweenCallback(Callable.From(QueueFree));
        }
        else
        {
            QueueFree();
        }
    }
}
