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
	/// Latched input flag for the sword attack (Left Mouse).
	/// </summary>
	private bool _mouseClicked = false;

	/// <summary>
	/// Latched input flag for the karate attack ('Q' key).
	/// </summary>
	private bool _qPressed = false;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		base._Ready();
		GD.Print("=== Cyber Shogun Initialized ===");
	}

	// ─── Input ───────────────────────────────────────────────────────────────
	/// <summary>
	/// Captures raw input events to set attack flags.
	/// Unity Migration: Use Input Action callbacks from the Unity Input System.
	/// </summary>
	public override void _Input(InputEvent @event)
	{
		// Detect Left Mouse click for Sword attack.
		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
				_mouseClicked = true;
		}
		// Detect 'Q' key press for Karate attack.
		else if (@event is InputEventKey key)
		{
			if (key.Keycode == Key.Q && key.Pressed && !key.Echo)
				_qPressed = true;
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
			// 1. Sword Attack: Triggered by mouse click.
			if (_mouseClicked && Sprite2d.SpriteFrames.HasAnimation("sword_attack"))
			{
				_mouseClicked = false;
				// Stop horizontal movement during attack.
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("sword_attack");
				_ = PlayAttackAsync();   // Trigger the animation lock asynchronously.
				return true;
			}

			// 2. Karate Attack: Triggered by 'Q' key.
			if (_qPressed && Sprite2d.SpriteFrames.HasAnimation("karate_attack"))
			{
				_qPressed = false;
				// Stop horizontal movement during attack.
				Velocity = Velocity with { X = 0f };
				Sprite2d.Play("karate_attack");
				_ = PlayAttackAsync();
				return true;
			}
		}

		// Clean up stale input flags if they weren't consumed by an attack.
		if (_mouseClicked) _mouseClicked = false;
		if (_qPressed)     _qPressed = false;

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
}
