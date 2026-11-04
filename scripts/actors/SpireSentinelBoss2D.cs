using Godot;
using System;

/// <summary>
/// Spire Sentinel V-9 is the multi-phase 2D boss mech encounter in Mission 9 (Spire Outer Hull).
/// Anchored to the Summit Catwalk Arena at 3,500m elevation, featuring high-altitude hover thrusters,
/// swept plasma bolt barrages, strafing laser sweeps, and an Overdrive Kinetic dive-bomb phase shift.
/// </summary>
public partial class SpireSentinelBoss2D : CharacterBody2D
{
    /// <summary>Emitted when health or phase state updates for HUD integration.</summary>
    [Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);

    /// <summary>Emitted when the boss is destroyed.</summary>
    [Signal] public delegate void BossDefeatedEventHandler();

    /// <summary>Maximum health across all 3 phases.</summary>
    [Export] public float MaxHealth { get; set; } = 520.0f;

    /// <summary>Patrol travel radius across the summit arena.</summary>
    [Export] public float PatrolDistance { get; set; } = 520.0f;

    /// <summary>Base strafe velocity.</summary>
    [Export] public float BaseSpeed { get; set; } = 170.0f;

    /// <summary>Point reward upon defeat.</summary>
    [Export] public int PointValue { get; set; } = 200;

    /// <summary>Current health pool.</summary>
    public float CurrentHealth { get; private set; } = 520.0f;

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

    /// <summary>Creates procedural high-tech Spire guardian visuals and collision shape if not pre-configured.</summary>
    private void SetupVisuals()
    {
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
        {
            var col = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new RectangleShape2D { Size = new Vector2(90, 110) }
            };
            AddChild(col);
        }

        _visualRoot = new Node2D { Name = "VisualRoot" };
        AddChild(_visualRoot);

        // Heavy aerodynamic Spire Sentinel armored torso
        var chassis = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(0, -50),
                new Vector2(40, -25),
                new Vector2(45, 20),
                new Vector2(25, 50),
                new Vector2(-25, 50),
                new Vector2(-45, 20),
                new Vector2(-40, -25)
            },
            Color = new Color(0.16f, 0.22f, 0.32f) // Deep stratospheric titanium armor
        };
        _visualRoot.AddChild(chassis);

        // Swept vertical stabilizers
        var leftFin = new Polygon2D
        {
            Polygon = new[] { new Vector2(-40, -15), new Vector2(-75, 5), new Vector2(-45, 35) },
            Color = new Color(0.28f, 0.38f, 0.52f)
        };
        var rightFin = new Polygon2D
        {
            Polygon = new[] { new Vector2(40, -15), new Vector2(75, 5), new Vector2(45, 35) },
            Color = new Color(0.28f, 0.38f, 0.52f)
        };
        _visualRoot.AddChild(leftFin);
        _visualRoot.AddChild(rightFin);

        // Central optic sensor lens (magenta/violet)
        var sensor = new Polygon2D
        {
            Polygon = new[] { new Vector2(-18, -18), new Vector2(18, -18), new Vector2(12, 2), new Vector2(-12, 2) },
            Color = new Color(0.95f, 0.2f, 0.6f)
        };
        _visualRoot.AddChild(sensor);

        _coreLight = new PointLight2D
        {
            Name = "CoreLight",
            Color = new Color(0.9f, 0.2f, 0.7f, 0.85f),
            Energy = 1.3f,
            TextureScale = 1.6f
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

        float speedMultiplier = CurrentPhase switch
        {
            2 => 1.7f,
            3 => 2.3f,
            _ => 1.0f
        };

        float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.8f) * PatrolDistance;
        float hoverY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.6f) * 36.0f;

        Velocity = new Vector2(
            (targetX - GlobalPosition.X) * 2.5f * speedMultiplier,
            (hoverY - GlobalPosition.Y) * 2.8f
        );

        MoveAndSlide();

        if (_visualRoot != null && Math.Abs(Velocity.X) > 5f)
        {
            _visualRoot.Scale = new Vector2(Velocity.X < 0f ? -1f : 1f, 1f);
        }

        float interval = CurrentPhase switch
        {
            2 => 1.4f,
            3 => 0.95f,
            _ => 2.0f
        };

        if (_attackCooldown >= interval)
        {
            _attackCooldown = 0.0f;
            LaunchPlasmaBarrage();
        }
    }

    /// <summary>Spawns high-velocity swept plasma bolts directed toward the player.</summary>
    private void LaunchPlasmaBarrage()
    {
        if (!IsInsideTree() || IsDying) return;

        Node2D? target = GetTree().GetFirstNodeInGroup("player") as Node2D;
        Vector2 targetPos = target?.GlobalPosition ?? (GlobalPosition + Vector2.Left * 400);
        Vector2 diff = targetPos - GlobalPosition;
        Vector2 dir = diff.LengthSquared() > 0 ? diff.Normalized() : Vector2.Left;

        int count = CurrentPhase == 3 ? 3 : (CurrentPhase == 2 ? 2 : 1);
        for (int i = 0; i < count; i++)
        {
            float spreadAngle = (i - (count - 1) * 0.5f) * 0.28f;
            Vector2 shotDir = dir.Rotated(spreadAngle);

            var bolt = new SpirePlasmaBolt2D
            {
                GlobalPosition = GlobalPosition + new Vector2(Velocity.X < 0 ? -48 : 48, 0),
                Direction = shotDir,
                OwningBoss = this,
                Speed = CurrentPhase == 3 ? 580f : (CurrentPhase == 2 ? 520f : 460f),
                Damage = CurrentPhase == 3 ? 30f : 25f
            };

            GetParent()?.AddChild(bolt);
        }
    }

    /// <summary>Applies incoming damage, managing phase transitions and defeat sequence.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDying) return;

        CurrentHealth = Math.Max(0, CurrentHealth - amount);

        if (CurrentHealth <= MaxHealth * 0.35f)
        {
            CurrentPhase = 3;
            if (_coreLight != null) _coreLight.Color = new Color(1f, 0.1f, 0.1f, 1f); // Overdrive Crimson
        }
        else if (CurrentHealth <= MaxHealth * 0.70f)
        {
            CurrentPhase = 2;
            if (_coreLight != null) _coreLight.Color = new Color(1f, 0.5f, 0.1f, 1f); // Overdrive Amber
        }

        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>Initiates the summit defeat sequence and alerts listeners.</summary>
    private void Die()
    {
        if (IsDying) return;
        IsDying = true;
        SetPhysicsProcess(false);

        if (IsInsideTree())
        {
            foreach (Node node in GetTree().GetNodesInGroup("spire_projectiles"))
            {
                if (node is SpirePlasmaBolt2D bolt && bolt.OwningBoss == this)
                {
                    bolt.Cancel();
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
