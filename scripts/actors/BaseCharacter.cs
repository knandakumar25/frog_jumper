using Godot;

/// <summary>
/// BaseCharacter is the foundational class for all playable characters in the game.
/// It provides shared physics, animation state management, and movement logic.
/// Subclasses (like MainCharacter or CyberShogun) override HandleSpecialActions()
/// to implement character-specific abilities such as combat or special movements.
///
/// Unity Migration Note: This will translate to a BaseCharacter class inheriting from
/// MonoBehaviour. In Unity, the physics will likely shift from CharacterBody2D to
/// a CharacterController or a Rigidbody-based system.
/// </summary>
public partial class BaseCharacter : CharacterBody2D
{
	// ─── Constants ───────────────────────────────────────────────────────────
	/// <summary>
	/// Horizontal movement speed in pixels per second.
	/// </summary>
	protected const float Speed = 400.0f;

	/// <summary>
	/// The initial upward velocity applied when jumping.
	/// </summary>
	protected const float JumpVelocity = -1000.0f;

	// ─── State ───────────────────────────────────────────────────────────────
	/// <summary>
	/// Tracks if the character is currently allowed to move.
	/// Set to false when the character reaches the finish line.
	/// </summary>
	protected bool IsMoving = true;

	// ─── Node references ─────────────────────────────────────────────────────
	/// <summary>
	/// Reference to the Sprite2D node for animation and visual updates.
	/// Unity Migration: Replace with an Animator component and a SpriteRenderer.
	/// </summary>
	protected AnimatedSprite2D Sprite2d;

	/// <summary>
	/// Audio player for the jump sound effect.
	/// Unity Migration: Use an AudioSource component.
	/// </summary>
	protected AudioStreamPlayer JumpSound;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// Initialize node references via Godot's path-based lookup.
		Sprite2d   = GetNode<AnimatedSprite2D>("Sprite2D");
		JumpSound  = GetNode<AudioStreamPlayer>("JumpSound");

		// --- Debugging Block ---
		// These print statements are used to verify the sprite and animation state on spawn.
		GD.Print("=== BaseCharacter Debug ===");
		GD.Print("Character instance created: ", Name);
		GD.Print("Sprite2D node: ", Sprite2d);

		if (Sprite2d != null)
		{
			GD.Print("Sprite frames: ", Sprite2d.SpriteFrames);
			GD.Print("Current animation: ", Sprite2d.Animation);
			GD.Print("Sprite visible: ", Sprite2d.Visible);
			GD.Print("Sprite modulate: ", Sprite2d.Modulate);

			if (Sprite2d.SpriteFrames != null)
			{
				GD.Print("Available animations: ", Sprite2d.SpriteFrames.GetAnimationNames());
				Sprite2d.Visible = true;
				Sprite2d.Modulate = new Color(1, 1, 1, 1);
				Sprite2d.Play("idle");
				GD.Print("Started playing 'idle' animation");
			}
		}

		GD.Print("Character position: ", GlobalPosition);
		GD.Print("Character scale: ", Scale);
		GD.Print("===========================");

		// Setup a periodic debug timer to monitor animation state in the console.
		var timer = new Timer();
		AddChild(timer);
		timer.Timeout += () =>
		{
			if (Sprite2d?.SpriteFrames != null)
				GD.Print("[", Time.GetTicksMsec(), "] Sprite playing: ",
					Sprite2d.IsPlaying(), " | Animation: ", Sprite2d.Animation,
					" | Frame: ", Sprite2d.Frame);
		};
		timer.WaitTime = 2.0;
		timer.Start();
	}

	// ─── Physics ─────────────────────────────────────────────────────────────
	/// <summary>
	/// Main physics loop. Handles gravity, movement, and animation state.
	/// Unity Migration: This logic moves to FixedUpdate().
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		// Reference the Global singleton to check if the level is won.
		var global = GetNode<Node>("/root/Global");
		if ((bool)global.Get("reached_finish"))
		{
			Sprite2d.Animation = "idle";
		}

		// Prevent movement if the character has finished the level.
		if (!IsMoving)
		{
			Sprite2d.Animation = "finished";
			return;
		}

		// 1. Special Actions: Check if a subclass is performing a special move (e.g., attacking).
		if (HandleSpecialActions()) return;

		// 2. Animation State: Update animation based on movement.
		var prevAnim = Sprite2d.Animation;
		// If velocity is significant, play 'running', otherwise play 'default'.
		Sprite2d.Animation = (Velocity.X > 1f || Velocity.X < -1f) ? "running" : "default";

		// 3. Gravity & Jumping:
		if (!IsOnFloor())
		{
			// Apply gravity constant to vertical velocity.
			Velocity += GetGravity() * (float)delta;
			Sprite2d.Animation = "jumping";
		}

		if (prevAnim != Sprite2d.Animation)
			GD.Print("Animation changed: ", prevAnim, " -> ", Sprite2d.Animation);

		// 4. Input Handling:
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			// Apply immediate upward force for jump.
			Velocity = Velocity with { Y = JumpVelocity };
			JumpSound.Play();
		}

		// Calculate horizontal movement based on input axis (left/right).
		float direction = Input.GetAxis("left", "right");
		if (!Mathf.IsZeroApprox(direction))
		{
			Velocity = Velocity with { X = direction * Speed };
		}
		else
		{
			// Smoothly decelerate to zero when no input is provided.
			Velocity = Velocity with { X = Mathf.MoveToward(Velocity.X, 0f, 12f) };
		}

		// Apply velocity and handle collisions.
		MoveAndSlide();

		// Flip the sprite based on movement direction.
		Sprite2d.FlipH = Velocity.X < 0;
	}

	// ─── Virtual / override point ─────────────────────────────────────────────
	/// <summary>
	/// This method is intended to be overridden in subclasses.
	/// It allows specific characters to inject their own logic (e.g., attacking) into the physics loop.
	/// </summary>
	/// <returns>True if the special action should override normal movement for this frame; otherwise false.</returns>
	protected virtual bool HandleSpecialActions() => false;

	// ─── Public API ──────────────────────────────────────────────────────────
	/// <summary>
	/// Freezes the character's movement. Typically called by the finish line logic.
	/// </summary>
	public virtual void StopMovement() => IsMoving = false;

	// GDScript-compatible alias (called by finish.gd via body.stop_movement())
	public void stop_movement() => StopMovement();
}
