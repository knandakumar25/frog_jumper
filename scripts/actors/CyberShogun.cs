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
	[Signal] public delegate void OperativeProfileChangedEventHandler(OperativeProfile profile);

	// ─── Operative Profile & Synergy ─────────────────────────────────────────
	[Export] public string OperativeName { get; set; } = "Cyber Shogun";
	public OperativeProfile ActiveProfile { get; private set; } = OperativeRegistry.GetProfile("Cyber Shogun");

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
	private float _shieldRegenCooldown = 0.0f;
	private bool _isAttacking = false;
	private bool _attackPressed = false;
	private bool _qPressed = false;
	private bool _dashPressed = false;
	private Area2D? _attackArea;
	private PointLight2D? _auraLight;

	// Audio Players
	private AudioStreamPlayer2D? _slashPlayer;
	private AudioStreamPlayer2D? _clashPlayer;
	private AudioStreamPlayer2D? _dashPlayer;
	private AudioStreamPlayer2D? _shieldPlayer;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		base._Ready();
		_attackArea = GetNodeOrNull<Area2D>("AttackArea");
		SetupAudio();

		string selectedOp = OperativeName;
		if (IsInsideTree() && GetTree()?.Root?.HasNode("Global") == true)
		{
			var globalNode = GetTree().Root.GetNodeOrNull<Node>("Global");
			var globalChar = globalNode?.Get("selected_character").AsString();
			if (!string.IsNullOrWhiteSpace(globalChar))
			{
				selectedOp = globalChar;
			}
		}

		ApplyOperativeProfile(selectedOp);
		GD.Print($"=== Operative Initialized: {ActiveProfile.DisplayName} [{ActiveProfile.CivilianName}] ===");
	}

	/// <summary>
	/// Applies active operative profile stats, passives, mobility, and visual aura styling.
	/// </summary>
	public void ApplyOperativeProfile(string name)
	{
		ApplyOperativeProfile(OperativeRegistry.GetProfile(name));
	}

	/// <summary>
	/// Applies an OperativeProfile directly to the character.
	/// </summary>
	public void ApplyOperativeProfile(OperativeProfile profile)
	{
		ActiveProfile = profile ?? OperativeRegistry.GetProfile("Cyber Shogun");
		OperativeName = ActiveProfile.DisplayName;

		MaxHealth = ActiveProfile.MaxHealth;
		CurrentHealth = MaxHealth;
		MaxShield = ActiveProfile.MaxShield;
		CurrentShield = MaxShield;

		Speed = ActiveProfile.MoveSpeed;
		JumpVelocity = ActiveProfile.JumpVelocity;
		DashCooldown = ActiveProfile.DashCooldown;
		DashCooldownTimer = 0.0f;

		if (Sprite2d != null)
		{
			Sprite2d.SpriteFrames = OperativeVisuals.LoadFrames(ActiveProfile.DisplayName);
			Sprite2d.Scale = new Vector2(0.37f, 0.37f);
			Sprite2d.Modulate = Colors.White;
			if (IsInsideTree()) Sprite2d.Play("idle");
		}

		UpdateAuraLight();

		EmitSignal(SignalName.OperativeProfileChanged, ActiveProfile);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth, CurrentShield, MaxShield);
		EmitSignal(SignalName.DashCooldownUpdated, 0.0f, DashCooldown);
	}

	private void UpdateAuraLight()
	{
		if (!IsInsideTree()) return;
		if (_auraLight == null)
		{
			_auraLight = GetNodeOrNull<PointLight2D>("OperativeAuraLight");
			if (_auraLight == null)
			{
				_auraLight = new PointLight2D
				{
					Name = "OperativeAuraLight",
					Position = new Vector2(0f, 10f),
					Energy = 0.85f,
					TextureScale = 1.4f
				};

				var grad = new Gradient();
				grad.AddPoint(0.0f, new Color(1f, 1f, 1f, 1f));
				grad.AddPoint(1.0f, new Color(1f, 1f, 1f, 0f));
				var gradTex = new GradientTexture2D
				{
					Gradient = grad,
					Width = 64,
					Height = 64,
					Fill = GradientTexture2D.FillEnum.Radial
				};
				gradTex.FillFrom = new Vector2(0.5f, 0.5f);
				gradTex.FillTo = new Vector2(0.5f, 0.0f);
				_auraLight.Texture = gradTex;
				AddChild(_auraLight);
			}
		}

		if (_auraLight != null && ActiveProfile != null)
		{
			_auraLight.Color = ActiveProfile.ThemeColor;
		}
	}

	private void SetupAudio()
	{
		_slashPlayer = CreateSfxPlayer("res://assets/audio/sfx/katana_slash.wav", -2.0f);
		_clashPlayer = CreateSfxPlayer("res://assets/audio/sfx/blade_clash.wav", 0.0f);
		_dashPlayer = CreateSfxPlayer("res://assets/audio/sfx/blade_dash.wav", -1.0f);
		_shieldPlayer = CreateSfxPlayer("res://assets/audio/sfx/shield_hit.wav", 0.0f);
	}

	private AudioStreamPlayer2D? CreateSfxPlayer(string path, float volumeDb = 0f)
	{
		if (!ResourceLoader.Exists(path)) return null;
		var stream = GD.Load<AudioStream>(path);
		if (stream == null) return null;
		var player = new AudioStreamPlayer2D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			Bus = "Master"
		};
		AddChild(player);
		return player;
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

		// Passive Shield Regeneration (Operative Synergy)
		if (_shieldRegenCooldown > 0f)
		{
			_shieldRegenCooldown = Mathf.Max(0f, _shieldRegenCooldown - dt);
		}
		else if (CurrentShield < MaxShield && CurrentHealth > 0f && ActiveProfile != null)
		{
			CurrentShield = Mathf.Min(MaxShield, CurrentShield + ActiveProfile.ShieldRegenRate * dt);
			EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth, CurrentShield, MaxShield);
		}

		base._PhysicsProcess(delta);

		// Wall slide & wall jump mechanics
		if (IsInsideTree() && !IsOnFloor() && IsOnWall())
		{
			// Slide down walls at controlled speed
			if (Velocity.Y > 0f)
			{
				Velocity = Velocity with { Y = Mathf.Min(Velocity.Y, 140.0f) };
			}

			// Wall jump input
			if (Input.IsActionJustPressed("jump") || Input.IsJoyButtonPressed(0, JoyButton.A))
			{
				Vector2 normal = GetWallNormal();
				if (normal.LengthSquared() > 0.01f)
				{
					Velocity = new Vector2(normal.X * 380.0f, JumpVelocity * 0.85f);
					JumpSound?.Play();
					_dashPlayer?.Play();
				}
			}
		}
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

				float dashSpeed = ActiveProfile?.DashSpeed ?? 800f;
				var themeColor = ActiveProfile?.ThemeColor ?? new Color(0f, 0.94f, 1f, 1f);
				float primaryDmg = ActiveProfile?.MeleePrimaryDamage ?? 35f;

				float dashDir = Sprite2d.FlipH ? -1f : 1f;
				Velocity = Velocity with { X = dashDir * dashSpeed };
				Sprite2d.Play("blade_dash");
				_dashPlayer?.Play();
				SpawnSlashVfx(new Vector2(dashDir * 24f, 0f), themeColor);
				PerformMeleeHitCheck(primaryDmg * 1.15f, new Vector2(dashDir * 28f, 0f));
				_ = PlayAttackAsync();
				return true;
			}

			// 2. Sword Attack: Triggered by attack key/gamepad.
			if (_attackPressed && Sprite2d.SpriteFrames.HasAnimation("sword_attack"))
			{
				_attackPressed = false;
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("sword_attack");
				_slashPlayer?.Play();
				float slashDir = Sprite2d.FlipH ? -1f : 1f;
				var themeColor = ActiveProfile?.ThemeColor ?? new Color(0.1f, 0.85f, 1f, 0.95f);
				float primaryDmg = ActiveProfile?.MeleePrimaryDamage ?? 35f;

				SpawnSlashVfx(new Vector2(slashDir * 20f, -4f), themeColor);
				PerformMeleeHitCheck(primaryDmg, new Vector2(slashDir * 26f, 0f));
				_ = PlayAttackAsync();
				return true;
			}

			// 3. Karate Attack: Triggered by 'Q' key.
			if (_qPressed && Sprite2d.SpriteFrames.HasAnimation("karate_attack"))
			{
				_qPressed = false;
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("karate_attack");
				_slashPlayer?.Play();
				float strikeDir = Sprite2d.FlipH ? -1f : 1f;
				var themeColor = ActiveProfile?.ThemeColor.Lightened(0.2f) ?? new Color(1f, 0.2f, 0.5f, 0.9f);
				float secondaryDmg = ActiveProfile?.MeleeSecondaryDamage ?? 25f;

				SpawnSlashVfx(new Vector2(strikeDir * 16f, 0f), themeColor);
				PerformMeleeHitCheck(secondaryDmg, new Vector2(strikeDir * 20f, 0f));
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
			_clashPlayer?.Play();
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
	/// Receives incoming damage, draining shield before health, mitigated by operative armor passive.
	/// </summary>
	public void TakeDamage(float damage, Vector2 knockback = default)
	{
		if (_invulnerabilityTimer > 0f || !IsMoving || CurrentHealth <= 0f) return;

		_shieldRegenCooldown = 3.0f; // Delay shield regeneration upon receiving damage
		float mitigation = ActiveProfile?.DamageReduction ?? 0f;
		float netDamage = damage * (1.0f - mitigation);

		if (CurrentShield > 0f)
		{
			if (IsInsideTree()) _shieldPlayer?.Play();
			float absorbed = Mathf.Min(CurrentShield, netDamage);
			CurrentShield -= absorbed;
			netDamage -= absorbed;
		}

		if (netDamage > 0f)
		{
			CurrentHealth = Mathf.Max(0f, CurrentHealth - netDamage);
		}

		_invulnerabilityTimer = 0.5f;

		if (knockback.LengthSquared() > 0f)
		{
			Vector2 netKnockback = knockback * (1.0f - mitigation * 0.7f);
			Velocity += netKnockback;
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
		Color restoreColor = Colors.White;
		if (IsInsideTree())
		{
			var tween = CreateTween();
			tween?.TweenProperty(Sprite2d, "modulate", restoreColor, 0.22f);
		}
		else
		{
			Sprite2d.Modulate = restoreColor;
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
