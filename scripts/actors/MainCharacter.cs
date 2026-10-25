using Godot;

/// <summary>
/// MainCharacter is the primary playable entity. It features dynamic sprite loading
/// based on the selected character from the Global state and implements a basic
/// combat system with "Sword" and "Karate" attack patterns.
///
/// Unity Migration Note: In Unity, instead of manually loading textures and building
/// SpriteFrames in code, it is recommended to use Animator Override Controllers
/// or distinct Prefabs for each character. Input should be migrated to the
/// Unity Input System (Input Action assets) for cleaner gamepad/keyboard support.
/// </summary>
public partial class MainCharacter : CharacterBody2D
{
	// ─── Constants ───────────────────────────────────────────────────────────
	private const float Speed         = 400.0f;
	private const float JumpVelocity  = -1000.0f;

	// ─── State ───────────────────────────────────────────────────────────────
	private bool _isMoving    = true;
	private bool _isAttacking = false;
	private bool _attackPressed = false;
	private bool _qPressed    = false;

	// ─── Node references ─────────────────────────────────────────────────────
	private AnimatedSprite2D _sprite2d;
	private AudioStreamPlayer _jumpSound;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// Prevent the UI from stealing focus on spawn.
		GetViewport()?.GuiReleaseFocus();
		_sprite2d  = GetNode<AnimatedSprite2D>("Sprite2D");
		_jumpSound = GetNode<AudioStreamPlayer>("JumpSound");
		LoadCharacterSprites();
	}

	// ─── Input Handling ──────────────────────────────────────────────────────
	/// <summary>
	/// Captures raw input events to trigger attack flags.
	/// </summary>
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouse) return;

		// Handle 'F' key or attack action for Sword Attack.
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.F || @event.IsActionPressed("attack"))
				_attackPressed = true;
			else if (key.Keycode == Key.Q)
				_qPressed = true;
		}
		// Handle Gamepad buttons for attacks.
		else if (@event is InputEventJoypadButton jb && jb.Pressed)
		{
			if (jb.ButtonIndex == JoyButton.X || jb.ButtonIndex == JoyButton.B || jb.ButtonIndex == JoyButton.RightShoulder)
				_attackPressed = true;
			else if (jb.ButtonIndex == JoyButton.Y || jb.ButtonIndex == JoyButton.LeftShoulder)
				_qPressed = true;
		}
	}

	// ─── Sprite loading & Animation Assembly ──────────────────────────────────
	/// <summary>
	/// Loads the selected operative's original-model animation atlas.
	/// </summary>
	private void LoadCharacterSprites()
	{
		var character = (string)GetNode<Node>("/root/Global").Get("selected_character");
		_sprite2d.SpriteFrames = OperativeVisuals.LoadFrames(character);
		_sprite2d.Scale = new Vector2(0.16f, 0.16f);
		_sprite2d.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		_sprite2d.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
		if (IsInsideTree()) _sprite2d.Play("idle");
	}

	// ─── Physics ─────────────────────────────────────────────────────────────
	public override void _PhysicsProcess(double delta)
	{
		var global = GetNode<Node>("/root/Global");
		if ((bool)global.Get("reached_finish"))
			_sprite2d.Animation = "idle";

		if (!_isMoving)
		{
			_sprite2d.Animation = "finished";
			return;
		}

		// 1. Attack Logic: Priority over movement.
		// Attack logic available grounded or airborne
		if (!_isAttacking)
		{
			if (_attackPressed && _sprite2d.SpriteFrames.HasAnimation("sword_attack"))
			{
				_attackPressed = false;
				if (IsOnFloor()) Velocity = Velocity with { X = 0f };
				_sprite2d.Play("sword_attack");
				_ = PlayAttackAsync();
				return;
			}
			if (_qPressed && _sprite2d.SpriteFrames.HasAnimation("karate_attack"))
			{
				_qPressed = false;
				if (IsOnFloor()) Velocity = Velocity with { X = 0f };
				_sprite2d.Play("karate_attack");
				_ = PlayAttackAsync();
				return;
			}
		}

		// Reset attack flags if not used.
		if (_attackPressed) _attackPressed = false;
		if (_qPressed)      _qPressed = false;
		if (_isAttacking)  return;

		// 2. Movement Animation:
		_sprite2d.Animation = (Velocity.X > 1f || Velocity.X < -1f) ? "running" : "default";

		if (!IsOnFloor())
		{
			Velocity += GetGravity() * (float)delta;
			_sprite2d.Animation = "jumping";
		}

		// 3. Jump Logic:
		bool isJumpPressed = Input.IsActionJustPressed("jump");
		if (!isJumpPressed)
		{
			// Fallback for gamepad buttons.
			isJumpPressed = Input.IsJoyButtonPressed(0, JoyButton.A) || Input.IsJoyButtonPressed(0, JoyButton.DpadUp);
		}

		if (isJumpPressed && IsOnFloor())
		{
			Velocity = Velocity with { Y = JumpVelocity };
			_jumpSound?.Play();
		}

		// 4. Horizontal Movement:
		float direction = Input.GetAxis("left", "right");
		if (Mathf.IsZeroApprox(direction))
		{
			// Handle analog stick input if digital keys are not pressed.
			float joyX = Input.GetJoyAxis(0, JoyAxis.LeftX);
			if (Mathf.Abs(joyX) > 0.25f)
			{
				direction = joyX;
			}
			else if (Input.IsJoyButtonPressed(0, JoyButton.DpadLeft))
			{
				direction = -1.0f;
			}
			else if (Input.IsJoyButtonPressed(0, JoyButton.DpadRight))
			{
				direction = 1.0f;
			}
		}

		Velocity = !Mathf.IsZeroApprox(direction)
			? Velocity with { X = direction * Speed }
			: Velocity with { X = Mathf.MoveToward(Velocity.X, 0f, 12f) };

		MoveAndSlide();
		_sprite2d.FlipH = Velocity.X < 0;
	}

	// ─── Attack helper ───────────────────────────────────────────────────────
	/// <summary>
	/// Asynchronous helper to lock movement until the attack animation completes.
	/// </summary>
	private async System.Threading.Tasks.Task PlayAttackAsync()
	{
		_isAttacking = true;
		// Wait for the 'AnimationFinished' signal from the AnimatedSprite2D.
		await ToSignal(_sprite2d, AnimatedSprite2D.SignalName.AnimationFinished);
		_isAttacking = false;
	}

	// ─── Public API ──────────────────────────────────────────────────────────
	public void StopMovement() => _isMoving = false;

	// GDScript-callable alias (finish.gd uses body.stop_movement())
	public void stop_movement() => StopMovement();
}
