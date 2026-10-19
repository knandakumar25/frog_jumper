using Godot;

/// <summary>
/// CyberShogun is a specialized playable character that extends BaseCharacter.
/// It implements combat abilities (Sword and Karate attacks, Blade Dash),
/// shields, health management, combo tracking, and melee hitboxes.
/// </summary>
public partial class CyberShogun : BaseCharacter
{
	// ─── Signals ─────────────────────────────────────────────────────────────
	[Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth, float currentShield, float maxShield);
	[Signal] public delegate void DashCooldownUpdatedEventHandler(float remaining, float maxDuration);
	[Signal] public delegate void ComboChangedEventHandler(int comboCount);
	[Signal] public delegate void PlayerDefeatedEventHandler();

	// ─── Health & Shield ─────────────────────────────────────────────────────
	[Export] public float MaxHealth { get; set; } = 100.0f;
	[Export] public float MaxShield { get; set; } = 50.0f;
	[Export] public float DashCooldown { get; set; } = 1.0f;

	public float CurrentHealth { get; private set; } = 100.0f;
	public float CurrentShield { get; private set; } = 50.0f;
	public float DashCooldownTimer { get; private set; } = 0.0f;
	public int ComboCount { get; private set; } = 0;
	public float ComboTimer { get; private set; } = 0.0f;
	public const float ComboWindow = 2.0f;

	private float _invulnerabilityTimer = 0.0f;
	private bool _isAttacking = false;
	private bool _attackPressed = false;
	private bool _qPressed = false;
	private bool _dashPressed = false;
	private Area2D? _attackArea;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		base._Ready();
		CurrentHealth = MaxHealth;
		CurrentShield = MaxShield;
		_attackArea = GetNodeOrNull<Area2D>("AttackArea");
		GD.Print("=== Cyber Shogun Initialized ===");
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth, CurrentShield, MaxShield);
		EmitSignal(SignalName.DashCooldownUpdated, 0.0f, DashCooldown);
	}

	// ─── Input ───────────────────────────────────────────────────────────────
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouse) return;

		// Detect 'F' key or 'attack' action for Sword attack.
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.F || @event.IsActionPressed("attack"))
				_attackPressed = true;
			else if (key.Keycode == Key.Q)
				_qPressed = true;
			else if (key.Keycode == Key.Shift || key.Keycode == Key.C)
				_dashPressed = true;
		}
		else if (@event is InputEventJoypadButton jb && jb.Pressed)
		{
			if (jb.ButtonIndex == JoyButton.X || jb.ButtonIndex == JoyButton.B)
				_attackPressed = true;
			else if (jb.ButtonIndex == JoyButton.Y || jb.ButtonIndex == JoyButton.LeftShoulder)
				_qPressed = true;
			else if (jb.ButtonIndex == JoyButton.RightShoulder)
				_dashPressed = true;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Update cooldowns & timers
		float dt = (float)delta;

		if (DashCooldownTimer > 0f)
		{
			DashCooldownTimer = Mathf.Max(0f, DashCooldownTimer - dt);
			EmitSignal(SignalName.DashCooldownUpdated, DashCooldownTimer, DashCooldown);
		}

		if (_invulnerabilityTimer > 0f)
		{
			_invulnerabilityTimer = Mathf.Max(0f, _invulnerabilityTimer - dt);
		}

		if (ComboTimer > 0f)
		{
			ComboTimer = Mathf.Max(0f, ComboTimer - dt);
			if (ComboTimer <= 0f && ComboCount > 0)
			{
				ComboCount = 0;
				EmitSignal(SignalName.ComboChanged, 0);
			}
		}

		base._PhysicsProcess(delta);
	}

	// ─── Override: special actions ───────────────────────────────────────────
	protected override bool HandleSpecialActions()
	{
		// Only allow attacks when grounded and not already attacking.
		if (IsOnFloor() && !_isAttacking)
		{
			// 1. Blade Dash: Triggered by Shift / C / Gamepad RB with cooldown check.
			if (_dashPressed && DashCooldownTimer <= 0f && Sprite2d.SpriteFrames.HasAnimation("blade_dash"))
			{
				_dashPressed = false;
				DashCooldownTimer = DashCooldown;
				EmitSignal(SignalName.DashCooldownUpdated, DashCooldownTimer, DashCooldown);

				float dashDir = Sprite2d.FlipH ? -1f : 1f;
				Velocity = Velocity with { X = dashDir * 800f };
				Sprite2d.Play("blade_dash");
				SpawnSlashVfx(new Vector2(dashDir * 24f, 0f), new Color(0f, 0.94f, 1f, 1f));
				PerformMeleeHitCheck(40.0f, new Vector2(dashDir * 28f, 0f));
				_ = PlayAttackAsync();
				return true;
			}

			// 2. Sword Attack: Triggered by attack key/gamepad.
			if (_attackPressed && Sprite2d.SpriteFrames.HasAnimation("sword_attack"))
			{
				_attackPressed = false;
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("sword_attack");
				float slashDir = Sprite2d.FlipH ? -1f : 1f;
				SpawnSlashVfx(new Vector2(slashDir * 20f, -4f), new Color(0.1f, 0.85f, 1f, 0.95f));
				PerformMeleeHitCheck(35.0f, new Vector2(slashDir * 26f, 0f));
				_ = PlayAttackAsync();
				return true;
			}

			// 3. Karate Attack: Triggered by 'Q' key.
			if (_qPressed && Sprite2d.SpriteFrames.HasAnimation("karate_attack"))
			{
				_qPressed = false;
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("karate_attack");
				float strikeDir = Sprite2d.FlipH ? -1f : 1f;
				SpawnSlashVfx(new Vector2(strikeDir * 16f, 0f), new Color(1f, 0.2f, 0.5f, 0.9f));
				PerformMeleeHitCheck(25.0f, new Vector2(strikeDir * 20f, 0f));
				_ = PlayAttackAsync();
				return true;
			}
		}

		// Clean up stale input flags if not consumed
		if (_dashPressed)   _dashPressed = false;
		if (_attackPressed) _attackPressed = false;
		if (_qPressed)      _qPressed = false;

		return _isAttacking;
	}

	private async System.Threading.Tasks.Task PlayAttackAsync()
	{
		_isAttacking = true;
		await ToSignal(Sprite2d, AnimatedSprite2D.SignalName.AnimationFinished);
		_isAttacking = false;
	}

	/// <summary>
	/// Checks for enemies in the melee hitbox area and applies damage.
	/// </summary>
	private void PerformMeleeHitCheck(float damage, Vector2 offset)
	{
		if (_attackArea == null) return;
		_attackArea.Position = offset;

		bool hitAny = false;

		// Check overlapping physics bodies
		foreach (var body in _attackArea.GetOverlappingBodies())
		{
			if (body == this) continue;
			if (body is CyberDrone2D drone)
			{
				drone.TakeDamage(damage);
				hitAny = true;
			}
			else if (body.HasMethod("TakeDamage"))
			{
				body.Call("TakeDamage", damage);
				hitAny = true;
			}
		}

		// Check overlapping areas
		foreach (var area in _attackArea.GetOverlappingAreas())
		{
			var parent = area.GetParent();
			if (parent is CyberDrone2D drone)
			{
				drone.TakeDamage(damage);
				hitAny = true;
			}
			else if (parent != null && parent.HasMethod("TakeDamage"))
			{
				parent.Call("TakeDamage", damage);
				hitAny = true;
			}
		}

		if (hitAny)
		{
			RegisterHit();
		}
	}

	/// <summary>
	/// Registers a successful melee strike, incrementing the combo counter.
	/// </summary>
	public void RegisterHit()
	{
		ComboCount++;
		ComboTimer = ComboWindow;
		EmitSignal(SignalName.ComboChanged, ComboCount);
	}

	/// <summary>
	/// Receives incoming damage, draining shield before health.
	/// </summary>
	public void TakeDamage(float damage, Vector2 knockback = default)
	{
		if (_invulnerabilityTimer > 0f || !IsMoving || CurrentHealth <= 0f) return;

		if (CurrentShield > 0f)
		{
			float absorbed = Mathf.Min(CurrentShield, damage);
			CurrentShield -= absorbed;
			damage -= absorbed;
		}

		if (damage > 0f)
		{
			CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
		}

		_invulnerabilityTimer = 0.5f;

		if (knockback.LengthSquared() > 0f)
		{
			Velocity += knockback;
		}

		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth, CurrentShield, MaxShield);
		FlashHurtVfx();

		if (CurrentHealth <= 0f)
		{
			Die();
		}
	}

	/// <summary>
	/// Heals player health and shield.
	/// </summary>
	public void Heal(float healthAmount, float shieldAmount = 0f)
	{
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healthAmount);
		CurrentShield = Mathf.Min(MaxShield, CurrentShield + shieldAmount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth, CurrentShield, MaxShield);
	}

	private void FlashHurtVfx()
	{
		if (Sprite2d == null) return;
		Sprite2d.Modulate = new Color(3.0f, 0.3f, 0.3f, 1f);
		if (IsInsideTree())
		{
			var tween = CreateTween();
			tween?.TweenProperty(Sprite2d, "modulate", new Color(1f, 1f, 1f, 1f), 0.22f);
		}
		else
		{
			Sprite2d.Modulate = new Color(1f, 1f, 1f, 1f);
		}
	}

	private void Die()
	{
		StopMovement();
		EmitSignal(SignalName.PlayerDefeated);
		var timer = GetTree()?.CreateTimer(0.8);
		if (timer != null)
		{
			timer.Timeout += () => GetTree()?.ReloadCurrentScene();
		}
	}

	/// <summary>
	/// Spawns a lightweight 2D neon plasma slash particle burst for attacks and dashes.
	/// </summary>
	private void SpawnSlashVfx(Vector2 offset, Color color)
	{
		var particles = new CpuParticles2D
		{
			Position = offset,
			Amount = 14,
			Lifetime = 0.25,
			OneShot = true,
			Explosiveness = 0.92f,
			Spread = 45.0f,
			InitialVelocityMin = 140.0f,
			InitialVelocityMax = 280.0f,
			Color = color,
			Gravity = new Vector2(0, 50),
			Scale = new Vector2(1.5f, 1.5f)
		};
		float dir = Sprite2d.FlipH ? -1f : 1f;
		particles.Direction = new Vector2(dir, -0.15f);
		AddChild(particles);
		particles.Emitting = true;

		var timer = GetTree()?.CreateTimer(0.35);
		if (timer != null)
		{
			timer.Timeout += () => particles.QueueFree();
		}
	}
}
