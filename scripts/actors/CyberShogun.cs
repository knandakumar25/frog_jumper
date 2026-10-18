using Godot;

/// <summary>
/// CyberShogun is a specialized playable character that extends the BaseCharacter.
/// It implements specific combat abilities (Sword and Karate attacks) by overriding
/// the HandleSpecialActions method, allowing it to bypass standard movement
/// and enter attack states.
///
/// Unity Migration Note: This will be a subclass of BaseCharacter. In Unity, these
/// specific attack animations and triggers would be handled via an Animator Controller
/// with "Attack" triggers or states.
/// </summary>
public partial class CyberShogun : BaseCharacter
{
	// ─── Attack state ────────────────────────────────────────────────────────
	/// <summary>
	/// Flag indicating if the character is currently locked in an attack animation.
	/// </summary>
	private bool _isAttacking = false;

	/// <summary>
	/// Latched input flag for the sword attack ('F' key / Gamepad X).
	/// </summary>
	private bool _attackPressed = false;

	/// <summary>
	/// Latched input flag for the karate attack ('Q' key).
	/// </summary>
	private bool _qPressed = false;

	/// <summary>
	/// Latched input flag for the blade dash ('Shift' / 'C' key / Gamepad RB).
	/// </summary>
	private bool _dashPressed = false;

	private Area2D? _attackArea;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		base._Ready();
		_attackArea = GetNodeOrNull<Area2D>("AttackArea");
		GD.Print("=== Cyber Shogun Initialized ===");
	}

	// ─── Input ───────────────────────────────────────────────────────────────
	/// <summary>
	/// Captures raw input events to set attack flags.
	/// </summary>
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

	// ─── Override: special actions ───────────────────────────────────────────
	/// <summary>
	/// Implements character-specific combat logic.
	/// This method is called by BaseCharacter during the physics loop.
	/// </summary>
	/// <returns>True if the character is attacking and normal movement should be suppressed.</returns>
	protected override bool HandleSpecialActions()
	{
		// Only allow attacks when grounded and not already attacking.
		if (IsOnFloor() && !_isAttacking)
		{
			// 1. Blade Dash: Triggered by Shift / C / Gamepad RB.
			if (_dashPressed && Sprite2d.SpriteFrames.HasAnimation("blade_dash"))
			{
				_dashPressed = false;
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
				// Stop horizontal movement during attack.
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("sword_attack");
				float slashDir = Sprite2d.FlipH ? -1f : 1f;
				SpawnSlashVfx(new Vector2(slashDir * 20f, -4f), new Color(0.1f, 0.85f, 1f, 0.95f));
				PerformMeleeHitCheck(35.0f, new Vector2(slashDir * 26f, 0f));
				_ = PlayAttackAsync();   // Trigger the animation lock asynchronously.
				return true;
			}

			// 3. Karate Attack: Triggered by 'Q' key.
			if (_qPressed && Sprite2d.SpriteFrames.HasAnimation("karate_attack"))
			{
				_qPressed = false;
				// Stop horizontal movement during attack.
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("karate_attack");
				float strikeDir = Sprite2d.FlipH ? -1f : 1f;
				SpawnSlashVfx(new Vector2(strikeDir * 16f, 0f), new Color(1f, 0.2f, 0.5f, 0.9f));
				PerformMeleeHitCheck(25.0f, new Vector2(strikeDir * 20f, 0f));
				_ = PlayAttackAsync();
				return true;
			}
		}

		// Clean up stale input flags if they weren't consumed by an attack.
		if (_dashPressed)   _dashPressed = false;
		if (_attackPressed) _attackPressed = false;
		if (_qPressed)      _qPressed = false;

		// Return true if the character is currently in the middle of an attack animation.
		return _isAttacking;
	}

	// ─── Async attack helper ─────────────────────────────────────────────────
	/// <summary>
	/// Locks the character's state until the current attack animation finishes.
	/// </summary>
	private async System.Threading.Tasks.Task PlayAttackAsync()
	{
		_isAttacking = true;
		// Wait for the 'AnimationFinished' signal from the AnimatedSprite2D.
		await ToSignal(Sprite2d, AnimatedSprite2D.SignalName.AnimationFinished);
		_isAttacking = false;
	}

	/// <summary>
	/// Checks for enemies or destructibles in the melee attack hitbox area and applies damage.
	/// </summary>
	private void PerformMeleeHitCheck(float damage, Vector2 offset)
	{
		if (_attackArea == null) return;
		_attackArea.Position = offset;

		// Check overlapping physics bodies
		foreach (var body in _attackArea.GetOverlappingBodies())
		{
			if (body == this) continue;
			if (body is CyberDrone2D drone)
			{
				drone.TakeDamage(damage);
			}
			else if (body.HasMethod("TakeDamage"))
			{
				body.Call("TakeDamage", damage);
			}
		}

		// Check overlapping areas
		foreach (var area in _attackArea.GetOverlappingAreas())
		{
			var parent = area.GetParent();
			if (parent is CyberDrone2D drone)
			{
				drone.TakeDamage(damage);
			}
			else if (parent != null && parent.HasMethod("TakeDamage"))
			{
				parent.Call("TakeDamage", damage);
			}
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
