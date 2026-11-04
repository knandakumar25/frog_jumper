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

    private Vector2 _originPosition;
    private float _hoverTimer;
    private float _attackCooldown;
    private Node2D? _visualRoot;
    private Node2D? _orbitalRing;
    private Polygon2D? _coreSphere;

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

    /// <summary>Creates procedural high-tech Nexus guardian visuals and collision shape if not pre-configured.</summary>
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

        // Heavy aerodynamic Overseer chassis
        var chassis = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(0, -60),
                new Vector2(45, -30),
                new Vector2(50, 25),
                new Vector2(30, 55),
                new Vector2(-30, 55),
                new Vector2(-50, 25),
                new Vector2(-45, -30)
            },
            Color = new Color(0.08f, 0.12f, 0.22f) // Deep obsidian cyber armor
        };
        _visualRoot.AddChild(chassis);

        // Stabilizer wings
        var leftWing = new Polygon2D
        {
            Polygon = new[] { new Vector2(-45, -20), new Vector2(-85, 0), new Vector2(-50, 40) },
            Color = new Color(0.15f, 0.25f, 0.45f)
        };
        var rightWing = new Polygon2D
        {
            Polygon = new[] { new Vector2(45, -20), new Vector2(85, 0), new Vector2(50, 40) },
            Color = new Color(0.15f, 0.25f, 0.45f)
        };
        _visualRoot.AddChild(leftWing);
        _visualRoot.AddChild(rightWing);

        // Central Quantum Core Sphere
        _coreSphere = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(0, -22),
                new Vector2(16, -16),
                new Vector2(22, 0),
                new Vector2(16, 16),
                new Vector2(0, 22),
                new Vector2(-16, 16),
                new Vector2(-22, 0),
                new Vector2(-16, -16)
            },
            Color = new Color(0.0f, 0.95f, 1.0f) // Cyan quantum energy
        };
        _visualRoot.AddChild(_coreSphere);

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
        if (IsDying) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        // Phase transitions at 66% and 33% health
        if (CurrentPhase == 1 && CurrentHealth <= MaxHealth * 0.66f)
        {
            CurrentPhase = 2;
            OnPhaseShift(2);
        }
        else if (CurrentPhase == 2 && CurrentHealth <= MaxHealth * 0.33f)
        {
            CurrentPhase = 3;
            OnPhaseShift(3);
        }

        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);

        if (CurrentHealth <= 0 && !IsDying)
        {
            IsDying = true;
            OnDefeated();
        }
    }

    private void OnPhaseShift(int newPhase)
    {
        if (_coreSphere != null)
        {
            _coreSphere.Color = newPhase switch
            {
                2 => new Color(1.0f, 0.7f, 0.1f), // Amber Overdrive
                _ => new Color(1.0f, 0.2f, 0.3f)  // Red/Meltdown Overload
            };
        }
    }

    private void OnDefeated()
    {
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

    private void ExecuteAttackPattern()
    {
        if (!IsInsideTree() || IsDying) return;

        Node2D? target = GetTree()?.GetFirstNodeInGroup("player") as Node2D;
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
