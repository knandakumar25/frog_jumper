using Godot;
using System;

/// <summary>
/// IGNIS-COLOSSUS is the multi-phase 2D boss mech encounter in Mission 7 (The Foundry Forge).
/// Anchored to the geothermal plasma core, featuring mortar artillery volleys,
/// thermal laser sweeping, and a climactic Phase 3 Overdrive Meltdown phase shift.
/// </summary>
public partial class IgnisColossusBoss2D : CharacterBody2D
{
    /// <summary>Emitted when health or phase state updates for HUD integration.</summary>
    [Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);

    /// <summary>Emitted when the boss is destroyed.</summary>
    [Signal] public delegate void BossDefeatedEventHandler();

    /// <summary>Maximum health across all 3 phases.</summary>
    [Export] public float MaxHealth { get; set; } = 450.0f;

    /// <summary>Patrol travel radius across the core overlook.</summary>
    [Export] public float PatrolDistance { get; set; } = 480.0f;

    /// <summary>Base strafe velocity.</summary>
    [Export] public float BaseSpeed { get; set; } = 140.0f;

    /// <summary>Point reward upon defeat.</summary>
    [Export] public int PointValue { get; set; } = 150;

    /// <summary>Current health pool.</summary>
    public float CurrentHealth { get; private set; } = 450.0f;

    /// <summary>Current combat phase (1, 2, or 3).</summary>
    public int CurrentPhase { get; private set; } = 1;

    /// <summary>Whether the boss has reached 0 HP and is in defeat sequence.</summary>
    public bool IsDying { get; private set; } = false;

    private Vector2 _originPosition;
    private float _hoverTimer;
    private float _attackCooldown;
    private AnimatedSprite2D? _sprite;
    private PointLight2D? _coreLight;

    /// <summary>Initializes boss positioning, groups, and HUD telemetry.</summary>
    public override void _Ready()
    {
        AddToGroup("enemies");
        AddToGroup("boss");
        _originPosition = GlobalPosition;
        CurrentHealth = MaxHealth;
        _sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite2D");
        _coreLight = GetNodeOrNull<PointLight2D>("CoreLight");

        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
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
            2 => 1.7f,
            3 => 2.2f,
            _ => 1.0f
        };

        float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.7f) * PatrolDistance;
        float hoverY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.2f) * 28.0f;

        Velocity = new Vector2(
            (targetX - GlobalPosition.X) * 2.2f * speedMultiplier,
            (hoverY - GlobalPosition.Y) * 2.5f
        );

        MoveAndSlide();

        if (_sprite != null && Math.Abs(Velocity.X) > 5f)
        {
            _sprite.FlipH = Velocity.X < 0f;
        }

        // Phase-specific attack timing
        float interval = CurrentPhase switch
        {
            2 => 1.4f,
            3 => 0.95f,
            _ => 2.0f
        };

        if (_attackCooldown >= interval)
        {
            _attackCooldown = 0.0f;
            LaunchMortarBarrage();
        }
    }

    /// <summary>Spawns parabolic or linear molten slag projectiles directed at the player.</summary>
    private void LaunchMortarBarrage()
    {
        if (!IsInsideTree() || IsDying) return;

        Node2D? target = GetTree().GetFirstNodeInGroup("player") as Node2D;
        Vector2 targetPos = target?.GlobalPosition ?? (GlobalPosition + Vector2.Left * 400);
        Vector2 diff = targetPos - GlobalPosition;
        Vector2 dir = diff.LengthSquared() > 0 ? diff.Normalized() : Vector2.Left;

        int count = CurrentPhase == 3 ? 3 : (CurrentPhase == 2 ? 2 : 1);
        for (int i = 0; i < count; i++)
        {
            float spreadAngle = (i - (count - 1) * 0.5f) * 0.22f;
            Vector2 shotDir = dir.Rotated(spreadAngle);

            var projectile = new FoundrySlagBall2D
            {
                GlobalPosition = GlobalPosition + new Vector2(Velocity.X < 0 ? -50 : 50, -10),
                Direction = shotDir,
                OwningBoss = this,
                Speed = CurrentPhase == 3 ? 540f : 460f,
                Damage = CurrentPhase == 3 ? 26f : 20f
            };

            GetParent()?.AddChild(projectile);
        }
    }

    /// <summary>Applies incoming damage, managing phase transitions and death handling.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDying) return;

        CurrentHealth = Math.Max(0, CurrentHealth - amount);

        // Check phase transitions
        int oldPhase = CurrentPhase;
        if (CurrentHealth <= MaxHealth * 0.35f)
        {
            CurrentPhase = 3;
            if (_coreLight != null) _coreLight.Color = new Color(1f, 0.2f, 0.1f, 1f); // Blazing red overdrive
        }
        else if (CurrentHealth <= MaxHealth * 0.70f)
        {
            CurrentPhase = 2;
            if (_coreLight != null) _coreLight.Color = new Color(1f, 0.6f, 0.1f, 1f); // Hot orange
        }

        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>Initiates the core overload defeat sequence and alerts listeners.</summary>
    private void Die()
    {
        if (IsDying) return;
        IsDying = true;
        SetPhysicsProcess(false);

        // Cancel any remaining owned projectiles
        if (IsInsideTree())
        {
            foreach (Node node in GetTree().GetNodesInGroup("foundry_projectiles"))
            {
                if (node is FoundrySlagBall2D projectile && projectile.OwningBoss == this)
                {
                    projectile.Cancel();
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
