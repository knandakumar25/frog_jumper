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

	/// <summary>Raised when the signature ability fires; the argument is its tag (see <see cref="LevelCatalog"/>).</summary>
	[Signal] public delegate void AbilityUsedEventHandler(string tag);
	/// <summary>Raised when a parry deflects an attack; the argument is the attacker that was hit back.</summary>
	[Signal] public delegate void ParryDeflectedEventHandler(Node2D source);

	/// <summary>Tag of the active operative's signature ability (parry, douse, slam, resonate, phase, bulwark).</summary>
	public string AbilityTag => OperativeRoster.AbilityTagOf(ActiveProfile.DisplayName) ?? "";
	/// <summary>+1 when facing right, -1 when facing left.</summary>
	public float FacingSign => Sprite2d != null && Sprite2d.FlipH ? -1f : 1f;
	/// <summary>Seconds until the signature ability can fire again (the melee strike itself is never gated).</summary>
	public float AbilityCooldownTimer { get; private set; }
	/// <summary>True while Kinetic Parry would deflect an incoming attack.</summary>
	public bool IsParrying => _parryRemaining > 0f;
	/// <summary>True during Phase Blink: hazards and beams pass through the operative.</summary>
	public bool IsPhased => _phaseRemaining > 0f;
	/// <summary>True during Solar Bulwark: hazards and ranged attacks are absorbed.</summary>
	public bool IsBulwarked => _bulwarkRemaining > 0f;
	private float _parryRemaining, _phaseRemaining, _bulwarkRemaining;

	/// <summary>Seconds a swap locks out further swaps, so swapping can't be used to dodge or reset cooldowns.</summary>
	public const float SwapCooldownSeconds = 1.0f;
	/// <summary>Remaining swap lockout in seconds.</summary>
	public float SwapCooldownTimer { get; private set; }

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

		// Squad swapping: Tab / Shift+Tab cycles unlocked operatives and 1-6 picks one directly. On gamepad D-pad Down is
		// "next" and Back/Select is "previous" (D-pad up/left/right are already jump and movement).
		if (@event is InputEventKey swapKey && swapKey.Pressed && !swapKey.Echo)
		{
			if (swapKey.Keycode == Key.Tab) { CycleOperative(swapKey.ShiftPressed ? -1 : 1); return; }
			int slot = (int)(swapKey.Keycode - Key.Key1);
			if (slot >= 0 && slot < OperativeRoster.RosterOrder.Count) { SwapOperative(OperativeRoster.RosterOrder[slot]); return; }
		}
		else if (@event is InputEventJoypadButton swapPad && swapPad.Pressed)
		{
			if (swapPad.ButtonIndex == JoyButton.DpadDown) { CycleOperative(1); return; }
			if (swapPad.ButtonIndex == JoyButton.Back) { CycleOperative(-1); return; }
		}

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
		if (SwapCooldownTimer > 0f) SwapCooldownTimer = Mathf.Max(0f, SwapCooldownTimer - dt);
		if (AbilityCooldownTimer > 0f) AbilityCooldownTimer = Mathf.Max(0f, AbilityCooldownTimer - dt);
		if (_parryRemaining > 0f) _parryRemaining -= dt;
		if (_phaseRemaining > 0f) { _phaseRemaining -= dt; if (_phaseRemaining <= 0f) SetGhostLook(false); }
		if (_bulwarkRemaining > 0f) { _bulwarkRemaining -= dt; if (_bulwarkRemaining <= 0f) SetGhostLook(false); }
        if (_actionRemaining > 0f)
        {
            _actionRemaining -= dt;
            if (_actionRemaining <= 0f)
            {
                _isAttacking = false;
                ActiveAction = "idle";
                // Only zero out horizontal velocity when grounded; in the air, preserve momentum for smooth jump arcs.
                if (IsOnFloor()) Velocity = Velocity with { X = 0 };
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

		// Alternate mission controllers reuse this class's health, shield and profile systems.
		// Returning here keeps their movement independent from platform gravity and wall logic.
		if (HandleAlternateMovement(delta)) return;

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

	/// <summary>Allows a specialized mission controller to replace platform movement for this tick.</summary>
	protected virtual bool HandleAlternateMovement(double delta) => false;

	/// <summary>Starts the profile's dash cooldown and notifies HUD listeners.</summary>
	protected void StartDashCooldown()
	{
		DashCooldownTimer = DashCooldown;
		EmitSignal(SignalName.DashCooldownUpdated, DashCooldownTimer, DashCooldown);
	}

	// ─── Override: special actions ───────────────────────────────────────────
	/// <summary>
	/// Initiates combat actions (blade dash, primary sword strike, secondary signature ability) both on the ground
	/// and while airborne, retaining a short outward impulse after wall jumps.
	/// </summary>
	/// <returns>True if an action or wall push is currently executing and handling physics movement; false otherwise.</returns>
	protected override bool HandleSpecialActions()

	{

		if (_wallPushRemaining > 0f)

        {

            Velocity = Velocity with { X = _wallPushVelocity };

            return true;

        }



        if (Sprite2d == null) Sprite2d = GetNodeOrNull<AnimatedSprite2D>("Sprite2D");



        // Allow attacks and dashes both grounded and airborne, as long as another action is not already executing.

		if (!_isAttacking)

		{

			// 1. Blade Dash: Triggered by Shift / C / Gamepad RB with cooldown check.

			if (_dashPressed && DashCooldownTimer <= 0f && (Sprite2d?.SpriteFrames == null || Sprite2d.SpriteFrames.HasAnimation("blade_dash")))

			{

				_dashPressed = false;

				DashCooldownTimer = DashCooldown;

				EmitSignal(SignalName.DashCooldownUpdated, DashCooldownTimer, DashCooldown);



				float dashSpeed = ActiveProfile?.DashSpeed ?? 800f;

				var themeColor = ActiveProfile?.ThemeColor ?? new Color(0f, 0.94f, 1f, 1f);

				float primaryDmg = ActiveProfile?.MeleePrimaryDamage ?? 35f;



				float dashDir = (Sprite2d != null && Sprite2d.FlipH) ? -1f : 1f;

				// In air, suspend downward falling momentum at dash initiation for snappy, responsive horizontal air dashing.

				Velocity = new Vector2(dashDir * dashSpeed, IsOnFloor() ? Velocity.Y : 0f);

				StartAction("dash", .24f);

				if (IsInsideTree()) Sprite2d?.Play("blade_dash");

				if (IsInsideTree()) _dashPlayer?.Play();

				SpawnSlashVfx(new Vector2(dashDir * 24f, 0f), themeColor);

				PerformMeleeHitCheck(primaryDmg * 1.15f, new Vector2(dashDir * 28f, 0f));

				return true;

			}



			// 2. Sword Attack: Triggered by attack key/gamepad.

			if (_attackPressed && (Sprite2d?.SpriteFrames == null || Sprite2d.SpriteFrames.HasAnimation("sword_attack")))

			{

				_attackPressed = false;

				// Grounded swings anchor the operative; aerial swings preserve mid-air jump momentum.

				if (IsOnFloor()) Velocity = Velocity with { X = 0f };

				StartAction("primary", .36f);

				if (IsInsideTree()) Sprite2d?.Play("sword_attack");

				if (IsInsideTree()) _slashPlayer?.Play();

				float slashDir = (Sprite2d != null && Sprite2d.FlipH) ? -1f : 1f;

				var themeColor = ActiveProfile?.ThemeColor ?? new Color(0.1f, 0.85f, 1f, 0.95f);

				float primaryDmg = ActiveProfile?.MeleePrimaryDamage ?? 35f;



				SpawnSlashVfx(new Vector2(slashDir * 20f, -4f), themeColor);

				PerformMeleeHitCheck(primaryDmg, new Vector2(slashDir * 26f, 0f));

				return true;

			}



			// 3. Karate Attack: Triggered by 'Q' key.

			if (_qPressed && (Sprite2d?.SpriteFrames == null || Sprite2d.SpriteFrames.HasAnimation("karate_attack")))

			{

				_qPressed = false;

				// Grounded strikes anchor the operative; aerial strikes preserve mid-air jump momentum.

				if (IsOnFloor()) Velocity = Velocity with { X = 0f };

				StartAction("secondary", .36f);

				if (IsInsideTree()) Sprite2d?.Play("karate_attack");

				if (IsInsideTree()) _slashPlayer?.Play();

				float strikeDir = (Sprite2d != null && Sprite2d.FlipH) ? -1f : 1f;

				var themeColor = ActiveProfile?.ThemeColor.Lightened(0.2f) ?? new Color(1f, 0.2f, 0.5f, 0.9f);

				float secondaryDmg = ActiveProfile?.MeleeSecondaryDamage ?? 25f;



				SpawnSlashVfx(new Vector2(strikeDir * 16f, 0f), themeColor);

				PerformMeleeHitCheck(secondaryDmg, new Vector2(strikeDir * 20f, 0f));

				TriggerSignatureAbility();

				return true;

			}

		}



		// Clean up stale input flags if not consumed
		if (_dashPressed)   _dashPressed = false;
		if (_attackPressed) _attackPressed = false;
		if (_qPressed)      _qPressed = false;

		return _isAttacking;

	}



	/// <summary>Simulates a primary katana strike input, allowing tests and scripted sequences to trigger sword attacks.</summary>

	public void PerformPrimaryAttack()

	{

		_attackPressed = true;

		HandleSpecialActions();

	}



	/// <summary>Simulates a secondary karate / signature strike input for testing and scripted actions.</summary>

	public void PerformSecondaryAttack()

	{

		_qPressed = true;

		HandleSpecialActions();

	}



	/// <summary>Simulates a blade dash input for testing and scripted dashes.</summary>

	public void PerformBladeDash()

	{

		_dashPressed = true;

		HandleSpecialActions();

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

    /// <summary>
    /// Fires the active operative's signature ability (bound to Q together with the secondary strike). Starts the
    /// operative-specific effect, notifies matching <see cref="AbilityGate"/>s in range, and sets a cooldown.
    /// Does nothing while on cooldown or if the operative has no ability tag.
    /// </summary>
    public void TriggerSignatureAbility()
    {
        string tag = AbilityTag;
        if (tag == "" || AbilityCooldownTimer > 0f) return;
        AbilityCooldownTimer = tag switch
        {
            LevelCatalog.Parry => 1.2f, LevelCatalog.Douse => 2.0f, LevelCatalog.Slam => 2.5f,
            LevelCatalog.Resonate => 2.5f, LevelCatalog.Phase => 2.0f, _ => 3.0f
        };
        switch (tag)
        {
            case LevelCatalog.Parry: _parryRemaining = 0.45f; break;
            case LevelCatalog.Phase:
                _phaseRemaining = 0.8f;
                SetGhostLook(true);
                BlinkForward(180f);
                break;
            case LevelCatalog.Bulwark:
                _bulwarkRemaining = 1.4f;
                SetGhostLook(true);
                break;
        }
        // Gates only care about the ability's tag and where/which way it was used.

        if (IsInsideTree())

        {

            foreach (Node node in GetTree()?.GetNodesInGroup("ability_gates") ?? new Godot.Collections.Array<Node>())

                if (node is AbilityGate gate) gate.ReceiveAbility(tag, GlobalPosition, FacingSign);

        }
        EmitSignal(SignalName.AbilityUsed, tag);
    }

    /// <summary>Teleports up to <paramref name="distance"/> px forward, stopping short of walls (Phase Blink).</summary>
    private void BlinkForward(float distance)
    {
        for (float d = distance; d >= 24f; d -= 24f)
        {
            Vector2 motion = new(FacingSign * d, 0f);
            if (!TestMove(GlobalTransform, motion)) { GlobalPosition += motion; return; }
        }
    }

    /// <summary>Tints the sprite translucent while an intangible/shielded ability window is active.</summary>
    private void SetGhostLook(bool on)
    {
        if (Sprite2d == null) return;
        Sprite2d.Modulate = on ? new Color(.6f, 1f, 1f, .55f) : Colors.White;
    }

    /// <summary>Cycles mission-available operatives, excluding deployed support such as Surya at the Mission 5 queue; returns false when unchanged.</summary>
    public bool CycleOperative(int direction)
    {
        var roster = OperativeRoster.Current;
        var list = roster.Available;
        if (list.Count <= 1) return false;
        int index = System.Linq.Enumerable.ToList(list).IndexOf(ActiveProfile.DisplayName);
        int next = ((index + System.Math.Sign(direction)) % list.Count + list.Count) % list.Count;
        return SwapOperative(list[next]);
    }

    /// <summary>
    /// Switches the controlled operative to an unlocked, mission-available squad member without interrupting movement. The squad shares one
    /// health and shield *ratio* (so swapping never heals), a running dash cooldown carries over, and a short lockout applies.
    /// Refused while locked out, attacking, defeated or when the operative is locked/already active.
    /// </summary>
    /// <returns>True when the swap happened.</returns>
    public bool SwapOperative(string name)
    {
        var roster = OperativeRoster.Current;
        string target = OperativeRoster.Normalize(name);
        if (target == ActiveProfile.DisplayName || !roster.IsAvailable(target)) return false;
        if (SwapCooldownTimer > 0f || _isAttacking || CurrentHealth <= 0f || !IsMoving) return false;

        float healthRatio = MaxHealth > 0f ? CurrentHealth / MaxHealth : 1f;
        float shieldRatio = MaxShield > 0f ? CurrentShield / MaxShield : 1f;
        float dashRemaining = DashCooldownTimer;
        roster.SetActive(target);
        ApplyOperativeProfile(target);
        // ApplyOperativeProfile fills health/shield and clears the dash cooldown; restore the carried-over values.
        CurrentHealth = Mathf.Max(1f, healthRatio * MaxHealth);
        CurrentShield = shieldRatio * MaxShield;
        DashCooldownTimer = Mathf.Min(dashRemaining, DashCooldown);
        SwapCooldownTimer = SwapCooldownSeconds;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth, CurrentShield, MaxShield);
        EmitSignal(SignalName.DashCooldownUpdated, DashCooldownTimer, DashCooldown);
        GetNodeOrNull<Node>("/root/Global")?.Set("selected_character", target);
        if (IsInsideTree() && Sprite2d != null)
        {
            // A quick bright flash sells the swap and hides the sprite change.
            Sprite2d.Modulate = new Color(2.2f, 2.2f, 2.6f, 1f);
            CreateTween().TweenProperty(Sprite2d, "modulate", Colors.White, 0.25f);
        }
        return true;
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
	public void TakeDamage(float damage, Vector2 knockback = default) => TakeDamageFrom(damage, knockback, null);

	/// <summary>
	/// Damage entry point for attacks that know their attacker (sentry beams, missiles). Kept separate from
	/// <see cref="TakeDamage"/> because GDScript cannot call a C# method with fewer arguments than it declares.
	/// A non-null <paramref name="source"/> marks the hit as ranged, which Kinetic Parry can reflect and Phase Blink cannot ignore.
	/// </summary>
	public void TakeDamageFrom(float damage, Vector2 knockback, Node? source)
	{
		if (_invulnerabilityTimer > 0f || !IsMoving || CurrentHealth <= 0f) return;
		// Signature-ability windows. Attacks that name a source (sentry beams, missiles) are "ranged"; sourceless damage
		// comes from environmental hazards (lasers, surge floors, vents).
		bool ranged = source != null;
		if (IsPhased && !ranged) return;                       // Phase Blink slips through hazards
		if (IsBulwarked) return;                                // Solar Bulwark absorbs hazards and ranged fire
		if (IsParrying && ranged)                               // Kinetic Parry throws ranged attacks back
		{
			if (source is CyberBossMissile2D missile)
            {
                // Reflect the owned projectile itself so counterfire travels back to its launching mech.
                missile.Reflect();
                EmitSignal(SignalName.ParryDeflected, missile);
            }
            else if (source is Node2D attacker)
			{
				if (attacker.HasMethod("TakeDamage")) attacker.Call("TakeDamage", 35f);
				EmitSignal(SignalName.ParryDeflected, attacker);
			}
			_invulnerabilityTimer = 0.2f;
			return;
		}

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

		if (!IsInsideTree()) return;

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

		float dir = (Sprite2d != null && Sprite2d.FlipH) ? -1f : 1f;

		particles.Direction = new Vector2(dir, -0.15f);

		AddChild(particles);

		particles.Emitting = true;



		var timer = GetTree()?.CreateTimer(0.35);

		if (timer != null)

		{

			// Checkpoint changes can free the owner before this short-lived timer fires.

			timer.Timeout += () => { if (GodotObject.IsInstanceValid(this) && GodotObject.IsInstanceValid(particles) && particles.IsInsideTree()) particles.QueueFree(); };

		}

	}

}

