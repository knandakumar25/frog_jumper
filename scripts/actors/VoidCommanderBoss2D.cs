using Godot;
using System;

/// <summary>
/// The Void Commander is the ultimate multi-phase 2D boss encounter in Mission 12 (Judgment Day).
/// Enthroned at the Spire Summit inside the Void Singularity Chamber, commanding twin dual-phase energy katanas,
/// dark-matter blink teleportation, sweeping reflectable plasma slash projectiles, and Singularity Overdrive.
/// </summary>
public partial class VoidCommanderBoss2D : CharacterBody2D
{
    /// <summary>Emitted when health or phase state updates for HUD integration.</summary>
    [Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);

    /// <summary>Emitted when the boss is destroyed.</summary>
    [Signal] public delegate void BossDefeatedEventHandler();

    /// <summary>Maximum health across all 3 phases.</summary>
    [Export] public float MaxHealth { get; set; } = 750.0f;

    /// <summary>Patrol travel radius across the Grand Throne Dais.</summary>
    [Export] public float PatrolDistance { get; set; } = 640.0f;

    /// <summary>Base strafe velocity.</summary>
    [Export] public float BaseSpeed { get; set; } = 220.0f;

    /// <summary>Point reward upon defeat.</summary>
    [Export] public int PointValue { get; set; } = 500;

    /// <summary>Current health pool.</summary>
    public float CurrentHealth { get; private set; } = 750.0f;

    /// <summary>Current combat phase (1, 2, or 3).</summary>
    public int CurrentPhase { get; private set; } = 1;

    /// <summary>Whether the boss has reached 0 HP and is in defeat sequence.</summary>
    public bool IsDying { get; private set; } = false;

    private Vector2 _originPosition;
    private float _hoverTimer;
    private float _attackCooldown;
    private Node2D? _visualRoot;
    private Polygon2D? _visor;
    private Node2D? _darkAura;
    private Line2D? _bladeRight;
    private Line2D? _bladeLeft;

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

    /// <summary>Creates procedural high-tech cybernetic warlord visuals and collision shape if not pre-configured.</summary>
    private void SetupVisuals()
    {
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
        {
            var col = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new RectangleShape2D { Size = new Vector2(90, 140) }
            };
            AddChild(col);
        }

        _visualRoot = new Node2D { Name = "VisualRoot" };
        AddChild(_visualRoot);

        // Dark matter gravitational aura ring
        _darkAura = new Node2D { Name = "DarkAura", Visible = true };
        _visualRoot.AddChild(_darkAura);

        for (int i = 0; i < 8; i++)
        {
            float a1 = i * Mathf.Pi / 4.0f;
            float a2 = (i + 1) * Mathf.Pi / 4.0f;
            var auraSegment = new Line2D
            {
                Points = new[]
                {
                    new Vector2(Mathf.Cos(a1) * 80f, Mathf.Sin(a1) * 80f),
                    new Vector2(Mathf.Cos(a2) * 80f, Mathf.Sin(a2) * 80f)
                },
                Width = 3.5f,
                DefaultColor = new Color(0.6f, 0.1f, 0.85f, 0.65f)
            };
            _darkAura.AddChild(auraSegment);
        }

        // Billowing dark matter cape
        var cape = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(-28, -50),
                new Vector2(28, -50),
                new Vector2(50, 65),
                new Vector2(-50, 65)
            },
            Color = new Color(0.04f, 0.01f, 0.08f, 0.95f)
        };
        _visualRoot.AddChild(cape);

        // Heavy cyber-exosuit torso chassis
        var chassis = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(0, -70),
                new Vector2(32, -50),
                new Vector2(40, -10),
                new Vector2(30, 45),
                new Vector2(16, 65),
                new Vector2(-16, 65),
                new Vector2(-30, 45),
                new Vector2(-40, -10),
                new Vector2(-32, -50)
            },
            Color = new Color(0.08f, 0.04f, 0.14f) // Obsidian warlord armor
        };
        _visualRoot.AddChild(chassis);

        // Shoulder pauldrons
        var leftShoulder = new Polygon2D
        {
            Polygon = new[] { new Vector2(-32, -50), new Vector2(-60, -35), new Vector2(-45, -5), new Vector2(-25, -20) },
            Color = new Color(0.18f, 0.08f, 0.28f)
        };
        var rightShoulder = new Polygon2D
        {
            Polygon = new[] { new Vector2(32, -50), new Vector2(60, -35), new Vector2(45, -5), new Vector2(25, -20) },
            Color = new Color(0.18f, 0.08f, 0.28f)
        };
        _visualRoot.AddChild(leftShoulder);
        _visualRoot.AddChild(rightShoulder);

        // Crimson command ocular visor
        _visor = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(-20, -35),
                new Vector2(20, -35),
                new Vector2(14, -26),
                new Vector2(-14, -26)
            },
            Color = new Color(1.0f, 0.1f, 0.3f) // Glowing crimson visor
        };
        _visualRoot.AddChild(_visor);

        // Twin dual-phase energy blades
        _bladeRight = new Line2D
        {
            Points = new[] { new Vector2(35, -20), new Vector2(75, -50) },
            Width = 4.5f,
            DefaultColor = new Color(1.0f, 0.15f, 0.35f, 0.95f)
        };
        _bladeLeft = new Line2D
        {
            Points = new[] { new Vector2(-35, -20), new Vector2(-75, -50) },
            Width = 4.5f,
            DefaultColor = new Color(0.8f, 0.15f, 1.0f, 0.95f)
        };
        _visualRoot.AddChild(_bladeRight);
        _visualRoot.AddChild(_bladeLeft);
    }

    /// <summary>Applies damage to health pool and drives phase transitions.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDying) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        // Phase transitions at 70% and 35% health
        if (CurrentPhase == 1 && CurrentHealth <= MaxHealth * 0.70f)
        {
            CurrentPhase = 2;
            OnPhaseShift(2);
        }
        else if (CurrentPhase == 2 && CurrentHealth <= MaxHealth * 0.35f)
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
        if (_visor != null)
        {
            _visor.Color = newPhase switch
            {
                2 => new Color(1.0f, 0.75f, 0.1f), // Coronal gold targeting
                _ => new Color(1.0f, 0.0f, 0.35f)  // Singularity Overdrive blazing crimson
            };
        }

        if (_bladeRight != null && _bladeLeft != null && newPhase == 3)
        {
            _bladeRight.Width = 6.5f;
            _bladeLeft.Width = 6.5f;
            _bladeRight.DefaultColor = new Color(1.0f, 0.0f, 0.4f);
            _bladeLeft.DefaultColor = new Color(0.2f, 0.9f, 1.0f);
        }
    }

    private void OnDefeated()
    {
        EmitSignal(SignalName.BossDefeated);

        // Clear active hostile projectiles
        var tree = GetTree();
        if (tree != null)
        {
            foreach (Node node in tree.GetNodesInGroup("commander_projectiles"))
            {
                if (node is VoidPlasmaSlash2D slash) slash.Cancel();
            }
        }

        // Defeat shrink and particle fade tween
        if (IsInsideTree())
        {
            var tween = CreateTween();
            tween?.TweenProperty(this, "scale", Vector2.Zero, 0.8f);
            tween?.TweenCallback(Callable.From(QueueFree));
        }
        else
        {
            QueueFree();
        }
    }

    /// <summary>Drives boss thruster hover, dais strafing, and phase-scaled projectile combat.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (IsDying) return;

        _hoverTimer += (float)delta;
        if (_darkAura != null && _darkAura.Visible)
        {
            _darkAura.Rotation += (float)delta * (CurrentPhase * 2.2f);
        }

        // Horizontal sinusoidal patrol and vertical hover
        float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.85f) * PatrolDistance;
        float targetY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.2f) * 40f;
        GlobalPosition = new Vector2(targetX, targetY);

        // Combat attack cadence
        _attackCooldown -= (float)delta;
        if (_attackCooldown <= 0f)
        {
            ExecuteAttackPattern();
            _attackCooldown = CurrentPhase switch
            {
                1 => 2.4f,
                2 => 1.7f,
                _ => 1.1f
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
                SpawnSlash(dir);
                break;
            case 2:
                SpawnSlash(dir.Rotated(-0.16f));
                SpawnSlash(dir.Rotated(0.16f));
                break;
            case 3:
                SpawnSlash(dir);
                SpawnSlash(dir.Rotated(0.28f));
                SpawnSlash(dir.Rotated(-0.28f));
                break;
        }
    }

    private void SpawnSlash(Vector2 direction)
    {
        var parent = GetParent();
        if (parent == null) return;

        var slash = new VoidPlasmaSlash2D
        {
            GlobalPosition = GlobalPosition + direction * 50f,
            Direction = direction,
            OwningBoss = this
        };
        parent.AddChild(slash);
    }
}
