using Godot;

/// <summary>
/// CyberShogun is a specialized playable character that extends BaseCharacter.
/// It implements combat abilities (Sword and Karate attacks, Blade Dash),
/// shields, health management, combo tracking, and melee hitboxes.
/// </summary>
public partial class CyberShogun : BaseCharacter
{
	/// <summary>Reports an executed move; a hit signal is emitted separately after collision checks.</summary>
    [Signal] public delegate void ActionExecutedEventHandler(string action);
    /// <summary>Reports the attack type that actually damaged a target.</summary>
    [Signal] public delegate void AttackLandedEventHandler(string action);
    /// <summary>Training owns checkpoint recovery instead of campaign scene reloads.</summary>
    [Export] public bool ExternalRespawn { get; set; }
    /// <summary>Current attack identity for visuals and collision-confirmed lessons.</summary>
    public string ActiveAction { get; private set; } = "idle";
    private float _actionRemaining;
    private float _wallPushRemaining;
    private float _wallPushVelocity;
    private readonly System.Collections.Generic.HashSet<ulong> _dashHits = new();

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
	/// <summary>Initializes combat, audio, and the selected operative profile.</summary>
	public override void _Ready()
	{
		base._Ready();
        // Platformer wall sliding must not inherit the floor-block-on-wall latch.
        FloorBlockOnWall = false;
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

	/// <summary>Creates or recolors the operative aura inside an active scene.</summary>
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

	/// <summary>Loads the shared local combat sound effects.</summary>
	private void SetupAudio()
	{
		_slashPlayer = CreateSfxPlayer("res://assets/audio/sfx/katana_slash.wav", -2.0f);
		_clashPlayer = CreateSfxPlayer("res://assets/audio/sfx/blade_clash.wav", 0.0f);
		_dashPlayer = CreateSfxPlayer("res://assets/audio/sfx/blade_dash.wav", -1.0f);
		_shieldPlayer = CreateSfxPlayer("res://assets/audio/sfx/shield_hit.wav", 0.0f);
	}

	/// <summary>Creates a child sound player only when its resource exists.</summary>
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
	/// <summary>Queues keyboard and controller combat requests for the physics tick.</summary>
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

	/// <summary>Updates combat timers, shield recovery, physics, and wall mobility.</summary>
	public override void _PhysicsProcess(double delta)
	{
		// Update cooldowns & timers
		float dt = (float)delta;
        if (_actionRemaining > 0f)
        {
            _actionRemaining -= dt;
            if (_actionRemaining <= 0f)
            {
                _isAttacking = false;
                ActiveAction = "idle";
                Velocity = Velocity with { X = 0 };
            }
        }
        _wallPushRemaining = Mathf.Max(0, _wallPushRemaining - dt);

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

		bool wasGrounded = IsOnFloor();
        base._PhysicsProcess(delta);
        if (wasGrounded && Velocity.Y < 0) EmitSignal(SignalName.ActionExecuted, "jump");
        if (ActiveAction == "dash")
            PerformMeleeHitCheck(ActiveProfile.MeleePrimaryDamage * 1.15f,
                new Vector2(Sprite2d.FlipH ? -28 : 28, 0));

		// Wall slide & wall jump mechanics
		if (IsInsideTree() && !IsOnFloor() && IsOnWall())
		{
			// Slide down walls at controlled speed
			if (Velocity.Y > 0f)
			{
				Velocity = Velocity with { Y = Mathf.Min(Velocity.Y, 140.0f) };
                EmitSignal(SignalName.ActionExecuted, "wall_slide");
			}

			// Wall jump input
			if (Input.IsActionJustPressed("jump"))
			{
				Vector2 normal = GetWallNormal();
				if (normal.LengthSquared() > 0.01f)
				{
					Velocity = new Vector2(normal.X * 380.0f, JumpVelocity * 0.85f);
					_wallPushRemaining = .18f;
                    _wallPushVelocity = normal.X * 380f;
                    EmitSignal(SignalName.ActionExecuted, "wall_jump");
                    JumpSound?.Play();
					_dashPlayer?.Play();
				}
			}
		}
	}

	// ─── Override: special actions ───────────────────────────────────────────
	/// <summary>Starts grounded actions, retaining a short outward impulse after wall jumps.</summary>
	protected override bool HandleSpecialActions()
	{
		if (_wallPushRemaining > 0f)
        {
            Velocity = Velocity with { X = _wallPushVelocity };
            return true;
        }
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
				StartAction("dash", .24f);
				Sprite2d.Play("blade_dash");
				_dashPlayer?.Play();
				SpawnSlashVfx(new Vector2(dashDir * 24f, 0f), themeColor);
				PerformMeleeHitCheck(primaryDmg * 1.15f, new Vector2(dashDir * 28f, 0f));
				return true;
			}

			// 2. Sword Attack: Triggered by attack key/gamepad.
			if (_attackPressed && Sprite2d.SpriteFrames.HasAnimation("sword_attack"))
			{
				_attackPressed = false;
				Velocity = Velocity with { X = 0f };
				StartAction("primary", .36f);
				Sprite2d.Play("sword_attack");
				_slashPlayer?.Play();
				float slashDir = Sprite2d.FlipH ? -1f : 1f;
				var themeColor = ActiveProfile?.ThemeColor ?? new Color(0.1f, 0.85f, 1f, 0.95f);
				float primaryDmg = ActiveProfile?.MeleePrimaryDamage ?? 35f;

				SpawnSlashVfx(new Vector2(slashDir * 20f, -4f), themeColor);
				PerformMeleeHitCheck(primaryDmg, new Vector2(slashDir * 26f, 0f));
				return true;
			}

			// 3. Karate Attack: Triggered by 'Q' key.
			if (_qPressed && Sprite2d.SpriteFrames.HasAnimation("karate_attack"))
			{
				_qPressed = false;
				Velocity = Velocity with { X = 0f };
				StartAction("secondary", .36f);
				Sprite2d.Play("karate_attack");
				_slashPlayer?.Play();
				float strikeDir = Sprite2d.FlipH ? -1f : 1f;
				var themeColor = ActiveProfile?.ThemeColor.Lightened(0.2f) ?? new Color(1f, 0.2f, 0.5f, 0.9f);
				float secondaryDmg = ActiveProfile?.MeleeSecondaryDamage ?? 25f;

				SpawnSlashVfx(new Vector2(strikeDir * 16f, 0f), themeColor);
				PerformMeleeHitCheck(secondaryDmg, new Vector2(strikeDir * 20f, 0f));
				return true;
			}
		}

		// Clean up stale input flags if not consumed
		if (_dashPressed)   _dashPressed = false;
		if (_attackPressed) _attackPressed = false;
		if (_qPressed)      _qPressed = false;

		return _isAttacking;
	}

    /// <summary>Starts a physics-timed action independently of artwork frame counts.</summary>
    private void StartAction(string action, float duration)
    {
        ActiveAction = action;
        _isAttacking = true;
        _actionRemaining = duration;
        _dashHits.Clear();
        EmitSignal(SignalName.ActionExecuted, action);
    }

    /// <summary>Queries the current hitbox transform, avoiding last-frame overlap caches and duplicate hits.</summary>
    private void PerformMeleeHitCheck(float damage, Vector2 offset)
    {
        if (_attackArea == null || !IsInsideTree()) return;
        _attackArea.Position = offset;
        var shape = _attackArea.GetNode<CollisionShape2D>("CollisionShape2D");
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape.Shape, Transform = shape.GlobalTransform,
            CollideWithAreas = true, CollideWithBodies = true,
            CollisionMask = _attackArea.CollisionMask,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() }
        };
        var seen = new System.Collections.Generic.HashSet<ulong>();
        bool hit = false;
        foreach (var result in GetWorld2D().DirectSpaceState.IntersectShape(query))
        {
            var target = result["collider"].AsGodotObject() as Node;
            if (target is Area2D) target = target.GetParent();
            if (target == null || target == this || !target.HasMethod("TakeDamage")) continue;
            ulong id = target.GetInstanceId();
            if (!seen.Add(id) || (ActiveAction == "dash" && !_dashHits.Add(id))) continue;
            target.Call("TakeDamage", damage);
            hit = true;
        }
        if (hit)
        {
            _clashPlayer?.Play();
            RegisterHit();
            EmitSignal(SignalName.AttackLanded, ActiveAction);
        }
    }

    /// <summary>Resets all transient combat state at a training checkpoint without changing profile or saves.</summary>
    public void ResetAtCheckpoint(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        IsMoving = true;
        _isAttacking = _attackPressed = _qPressed = _dashPressed = false;
        _actionRemaining = _wallPushRemaining = _invulnerabilityTimer = _shieldRegenCooldown = 0;
        DashCooldownTimer = ComboTimer = 0;
        ComboCount = 0;
        ActiveAction = "idle";
        _dashHits.Clear();
        Heal(MaxHealth, MaxShield);
        Sprite2d.FlipH = false;
        Sprite2d.Modulate = Colors.White;
        if (IsInsideTree()) Sprite2d.Play("idle");
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

	/// <summary>Briefly highlights shield or health damage on the character.</summary>
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

	/// <summary>Signals defeat and lets training recover locally; campaign keeps its reload behavior.</summary>
	private void Die()
	{
		StopMovement();
		EmitSignal(SignalName.PlayerDefeated);
		if (ExternalRespawn || !IsInsideTree()) return;
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
			// Checkpoint changes can free the owner before this short-lived timer fires.
			timer.Timeout += () => { if (GodotObject.IsInstanceValid(particles)) particles.QueueFree(); };
		}
	}
}
