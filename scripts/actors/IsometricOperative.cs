using Godot;
using System.Collections.Generic;

/// <summary>
/// Isometric mission controller that reuses CyberShogun profile, shield, health and combat signals.
/// Platformer movement remains active everywhere else; this subclass substitutes twin-stick movement
/// only in the new Level 1 scene.
/// </summary>
public partial class IsometricOperative : CyberShogun
{
    private bool _primaryRequested, _abilityRequested, _dashRequested;
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
        InputActionDefaults.Initialize();
        // CyberShogun's base ready method loads the operative's SpriteCook frames (or the invisible
        // placeholder while they are not made yet) through OperativeVisuals before playing idle.
        base._Ready();
        OperativeProfileChanged += OnOperativeProfileChanged;
        ApplyMissionCharacterArt(ActiveProfile);
        ExternalRespawn = true;
        _queryShape = new CircleShape2D { Radius = 52f };
        Scale = Vector2.One;
        Sprite2d.Scale = new Vector2(.48f, .48f);
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

    /// <summary>Uses the shared deliberate-device detector for aim, independent of prompt style overrides.</summary>
    public override void _Input(InputEvent @event)
    {
        InputPromptService.Instance?.ObserveInput(@event);
        _usingGamepadAim = InputPromptService.Instance?.UsingController ?? false;
    }

    /// <summary>Queues only gameplay events that UI controls have not consumed.</summary>
    public override void _UnhandledInput(InputEvent input)
    {
        // Godot copies every touch into an emulated left click, and iso_primary_attack is bound to the
        // left mouse button on any device, so a tap on the ground would attack. On a touchscreen the
        // on-screen Attack button is the way to attack (review finding cb-M5, 2026-11-17).
        if (input is InputEventMouse && input.Device == Godot.InputEvent.DeviceIdEmulation) return;
        if (IsGameplayPress(input, "iso_primary_attack")) _primaryRequested = true;
        if (IsGameplayPress(input, "iso_special_ability")) _abilityRequested = true;
        if (IsGameplayPress(input, "iso_dash")) _dashRequested = true;
    }

    /// <summary>Accepts trigger threshold crossings once instead of repeating on every held-axis event.</summary>
    private static bool IsGameplayPress(InputEvent input, string action) => InputActionDefaults.Matches(input, action)
        && (input is not InputEventJoypadMotion || Input.IsActionJustPressed(action));
    /// <summary>Replaces gravity and jump processing with normalized top-down movement and combat.</summary>
    protected override bool HandleAlternateMovement(double delta)
    {
        float dt = (float)delta;
        _abilityCooldownRemaining = Mathf.Max(0f, _abilityCooldownRemaining - dt);
        _dashRemaining = Mathf.Max(0f, _dashRemaining - dt);
        _attackAnimationRemaining = Mathf.Max(0f, _attackAnimationRemaining - dt);
        _moveDirection = Input.GetVector("iso_move_left", "iso_move_right", "iso_move_up", "iso_move_down");
        Vector2 stickAim = ReadAimStick();
        if (_usingGamepadAim && stickAim.LengthSquared() > .09f)
        {
            _aimDirection = stickAim.Normalized();
        }
        else if (TouchControls.Active)
        {
            // Touches move the emulated mouse onto the buttons, so on-screen controls aim where the
            // operative walks, keeping the last direction while standing still.
            if (_moveDirection.LengthSquared() > .04f) _aimDirection = _moveDirection.Normalized();
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

        if (_primaryRequested)
        {
            _attackAnimationRemaining = .24f;
            if (Sprite2d.SpriteFrames?.HasAnimation("sword_attack") == true) Sprite2d.Play("sword_attack");
            PerformStrike(_aimDirection * 54f, 56f, ActiveProfile.MeleePrimaryDamage);
        }
        if (_abilityRequested) UseSignatureAbility();
        if (_dashRequested && DashCooldownTimer <= 0f)
        {
            StartDashCooldown();
            _dashRemaining = .16f;
            Velocity = _aimDirection * ActiveProfile.DashSpeed;
            if (Sprite2d.SpriteFrames?.HasAnimation("blade_dash") == true) Sprite2d.Play("blade_dash");
            EmitSignal(SignalName.ActionExecuted, "dash");
        }
        _primaryRequested = _abilityRequested = _dashRequested = false;
        QueueRedraw();
        return true;
    }

    /// <summary>Restarts mission-local action timing after a checkpoint respawn.</summary>
    public void ResetIsometricTransientState()
    {
        _primaryRequested = _abilityRequested = _dashRequested = false;
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

}
