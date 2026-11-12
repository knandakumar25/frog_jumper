using Godot;
using System.Collections.Generic;

/// <summary>
/// Isometric mission controller that reuses CyberShogun profile, shield, health and combat signals.
/// Platformer movement remains active everywhere else; this subclass substitutes twin-stick movement
/// only in the new Level 1 scene.
/// </summary>
public partial class IsometricOperative : CyberShogun
{
    private static bool _inputConfigured;
    private CircleShape2D _queryShape = null!;
    private Vector2 _aimDirection = Vector2.Right;
    private Vector2 _moveDirection;
    private float _abilityCooldownRemaining;
    private float _dashRemaining;
    private float _attackAnimationRemaining;
    private bool _usingGamepadAim;
    private float _abilityCooldown = 5.5f;

    /// <summary>Read-only attack direction, used by aiming and encounter tests.</summary>
    public Vector2 AimDirection => _aimDirection;

    /// <summary>Read-only remaining time on the selected operative's signature ability.</summary>
    public float AbilityCooldownRemaining => _abilityCooldownRemaining;

    /// <summary>Installs isometric controls, enables visible mouse aim, and prepares spatial hit queries.</summary>
    public override void _Ready()
    {
        EnsureInputActions();
        // CyberShogun's base ready method loads the operative's SpriteCook frames (or the invisible
        // placeholder while they are not made yet) through OperativeVisuals before playing idle.
        base._Ready();
        OperativeProfileChanged += OnOperativeProfileChanged;
        ApplyMissionCharacterArt(ActiveProfile);
        ExternalRespawn = true;
        _queryShape = new CircleShape2D { Radius = 52f };
        Scale = Vector2.One;
        Sprite2d.Scale = new Vector2(.48f, .48f);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        QueueRedraw();
    }

    /// <summary>Reloads the mission sprite frames when the squad swaps operatives.</summary>
    private void OnOperativeProfileChanged(OperativeProfile profile) => ApplyMissionCharacterArt(profile);

    /// <summary>
    /// Uses the operative's SpriteCook frames from <c>OperativeVisuals</c>, the same frames as every
    /// other mission. The hand-drawn Level 1 Shogun atlas was deleted on 2026-11-12.
    /// </summary>
    private void ApplyMissionCharacterArt(OperativeProfile profile)
    {
        Sprite2d.SpriteFrames = OperativeVisuals.LoadFrames(profile.DisplayName);
        Sprite2d.Play("idle");
    }

    /// <summary>Routes keyboard, mouse, and right-stick motion to the current aim source.</summary>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            _usingGamepadAim = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else if (@event is InputEventJoypadMotion)
        {
            Vector2 stick = ReadAimStick();
            if (stick.LengthSquared() > .09f) _usingGamepadAim = true;
        }
        else if (@event is InputEventKey or InputEventMouseButton)
        {
            _usingGamepadAim = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    /// <summary>Replaces gravity and jump processing with normalized top-down movement and combat.</summary>
    protected override bool HandleAlternateMovement(double delta)
    {
        float dt = (float)delta;
        _abilityCooldownRemaining = Mathf.Max(0f, _abilityCooldownRemaining - dt);
        _dashRemaining = Mathf.Max(0f, _dashRemaining - dt);
        _attackAnimationRemaining = Mathf.Max(0f, _attackAnimationRemaining - dt);
        _moveDirection = Input.GetVector("iso_move_left", "iso_move_right", "iso_move_up", "iso_move_down");
        Vector2 stickAim = ReadAimStick();
        if (stickAim.LengthSquared() > .09f)
        {
            _aimDirection = stickAim.Normalized();
            _usingGamepadAim = true;
        }
        else if (!_usingGamepadAim)
        {
            Vector2 mouseDirection = GetGlobalMousePosition() - GlobalPosition;
            if (mouseDirection.LengthSquared() > 16f) _aimDirection = mouseDirection.Normalized();
        }

        if (_dashRemaining > 0f)
            Velocity = _aimDirection * ActiveProfile.DashSpeed;
        else
            Velocity = _moveDirection * ActiveProfile.MoveSpeed;
        MoveAndSlide();

        if (Sprite2d.SpriteFrames != null && _dashRemaining <= 0f && _attackAnimationRemaining <= 0f)
        {
            string animation = _moveDirection.LengthSquared() > .02f ? "running" : "default";
            if (Sprite2d.SpriteFrames.HasAnimation(animation) && Sprite2d.Animation != animation)
                Sprite2d.Play(animation);
            Sprite2d.FlipH = _aimDirection.X < -.15f;
        }

        if (Input.IsActionJustPressed("iso_primary_attack"))
        {
            _attackAnimationRemaining = .24f;
            if (Sprite2d.SpriteFrames?.HasAnimation("sword_attack") == true) Sprite2d.Play("sword_attack");
            PerformStrike(_aimDirection * 54f, 56f, ActiveProfile.MeleePrimaryDamage);
        }
        if (Input.IsActionJustPressed("iso_special_ability")) UseSignatureAbility();
        if (Input.IsActionJustPressed("iso_dash") && DashCooldownTimer <= 0f)
        {
            StartDashCooldown();
            _dashRemaining = .16f;
            Velocity = _aimDirection * ActiveProfile.DashSpeed;
            if (Sprite2d.SpriteFrames?.HasAnimation("blade_dash") == true) Sprite2d.Play("blade_dash");
            EmitSignal(SignalName.ActionExecuted, "dash");
        }
        QueueRedraw();
        return true;
    }

    /// <summary>Restarts mission-local action timing after a checkpoint respawn.</summary>
    public void ResetIsometricTransientState()
    {
        _abilityCooldownRemaining = 0f;
        _dashRemaining = 0f;
        _attackAnimationRemaining = 0f;
        _aimDirection = Vector2.Right;
        Velocity = Vector2.Zero;
        QueueRedraw();
    }

    /// <summary>Draws a subtle directional reticle that makes mouse and stick aim readable.</summary>
    public override void _Draw()
    {
        Color color = ActiveProfile?.ThemeColor ?? new Color("#40edff");
        DrawLine(Vector2.Zero, _aimDirection * 74f, new Color(color, .65f), 3f, true);
        DrawArc(_aimDirection * 88f, 8f, 0f, Mathf.Tau, 20, color, 2f, true);
    }

    /// <summary>Runs the profile's named signature ability with a distinct shape and effect.</summary>
    private void UseSignatureAbility()
    {
        if (_abilityCooldownRemaining > 0f) return;
        _abilityCooldownRemaining = _abilityCooldown;
        _attackAnimationRemaining = .3f;
        if (Sprite2d.SpriteFrames.HasAnimation("karate_attack")) Sprite2d.Play("karate_attack");
        string operative = ActiveProfile.DisplayName;
        if (operative == "Mirage Blade")
        {
            GlobalPosition += _aimDirection * 145f;
            PerformStrike(_aimDirection * 58f, 116f, ActiveProfile.MeleePrimaryDamage * 1.8f);
        }
        else if (operative is "Apex Titan" or "Aura Phoenicia")
        {
            PerformStrike(Vector2.Zero, operative == "Apex Titan" ? 150f : 170f,
                ActiveProfile.MeleeSecondaryDamage * 1.8f);
        }
        else if (operative == "Oceanus")
        {
            PerformStrike(_aimDirection * 112f, 82f, ActiveProfile.MeleePrimaryDamage * 1.9f);
        }
        else
        {
            PerformStrike(_aimDirection * 72f, operative == "Surya Prime" ? 106f : 82f,
                ActiveProfile.MeleePrimaryDamage * 1.8f);
        }
        EmitSignal(SignalName.ActionExecuted, ActiveProfile.PrimaryAbilityName);
    }

    /// <summary>Applies one hit to each enemy or destructible objective within an aimed circle.</summary>
    private void PerformStrike(Vector2 offset, float radius, float damage)
    {
        if (!IsInsideTree()) return;
        _queryShape.Radius = radius;
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = _queryShape,
            Transform = new Transform2D(0f, GlobalPosition + offset),
            CollisionMask = 4,
            CollideWithAreas = true,
            CollideWithBodies = true,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() }
        };
        var hit = new HashSet<ulong>();
        foreach (var result in GetWorld2D().DirectSpaceState.IntersectShape(query, 24))
        {
            Node? target = result["collider"].AsGodotObject() as Node;
            if (target is Area2D) target = target.GetParent();
            if (target == null || target == this || !target.HasMethod("TakeDamage")) continue;
            if (!hit.Add(target.GetInstanceId())) continue;
            target.Call("TakeDamage", damage);
            if (target is CharacterBody2D body)
            {
                Vector2 knockback = body.GlobalPosition - GlobalPosition;
                if (knockback.LengthSquared() > .001f) body.Velocity += knockback.Normalized() * 125f;
            }
            RegisterHit();
            EmitSignal(SignalName.AttackLanded, "primary");
        }
    }

    /// <summary>Reads the gamepad right stick through its four signed action strengths.</summary>
    private static Vector2 ReadAimStick() => Input.GetVector("iso_aim_left", "iso_aim_right", "iso_aim_up", "iso_aim_down");

    /// <summary>Creates the mission's keyboard, mouse, and gamepad bindings only when absent.</summary>
    private static void EnsureInputActions()
    {
        if (_inputConfigured) return;
        AddAxis("iso_move_left", Key.A, JoyAxis.LeftX, -1f);
        AddAxis("iso_move_right", Key.D, JoyAxis.LeftX, 1f);
        AddAxis("iso_move_up", Key.W, JoyAxis.LeftY, -1f);
        AddAxis("iso_move_down", Key.S, JoyAxis.LeftY, 1f);
        AddAxis("iso_aim_left", Key.None, JoyAxis.RightX, -1f);
        AddAxis("iso_aim_right", Key.None, JoyAxis.RightX, 1f);
        AddAxis("iso_aim_up", Key.None, JoyAxis.RightY, -1f);
        AddAxis("iso_aim_down", Key.None, JoyAxis.RightY, 1f);
        AddButton("iso_primary_attack", Key.F, MouseButton.Left, JoyAxis.TriggerLeft);
        AddButton("iso_special_ability", Key.Q, MouseButton.Right, JoyAxis.TriggerRight);
        AddButton("iso_dash", Key.Shift, MouseButton.None, JoyAxis.Invalid, JoyButton.RightShoulder);
        AddButton("iso_interact", Key.E, MouseButton.None, JoyAxis.Invalid, JoyButton.A);
        AddButton("iso_pause", Key.Escape, MouseButton.None, JoyAxis.Invalid, JoyButton.Start);
        _inputConfigured = true;
    }

    /// <summary>Adds one digital and one analog directional binding without duplicating existing events.</summary>
    private static void AddAxis(string action, Key key, JoyAxis axis, float direction)
    {
        EnsureAction(action);
        if (key != Key.None) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = axis, AxisValue = direction });
    }

    /// <summary>Adds keyboard, optional mouse, trigger-axis, and optional button bindings.</summary>
    private static void AddButton(string action, Key key, MouseButton mouse, JoyAxis trigger, JoyButton pad = JoyButton.Invalid)
    {
        EnsureAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        if (mouse != MouseButton.None) InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = mouse });
        if (trigger != JoyAxis.Invalid) InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = trigger, AxisValue = 1f });
        if (pad != JoyButton.Invalid) InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = pad });
    }

    /// <summary>Ensures runtime bindings exist without changing controls used by other levels.</summary>
    private static void EnsureAction(string action)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action, .25f);
    }
}
