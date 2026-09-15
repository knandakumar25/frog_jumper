using Godot;

/// <summary>
/// FallingCharacter is a decorative visual element used in the main menu background.
/// It represents characters tumbling down the screen with a rotating motion.
///
/// Unity Migration Note: In Unity, this would be a simple script on a GameObject
/// with a SpriteRenderer. Instead of slicing textures in code, use Unity's
/// Sprite Editor to slice the assets into animations.
/// </summary>
public partial class FallingCharacter : AnimatedSprite2D
{
	// ─── Configurable properties ─────────────────────────────────────────────
	/// <summary>
	/// Downward movement speed in pixels per second.
	/// </summary>
	[Export] public float FallSpeed     { get; set; } = 200.0f;

	/// <summary>
	/// Angular rotation speed in radians per second.
	/// </summary>
	[Export] public float RotationSpeed { get; set; } = 2.0f;

	/// <summary>
	/// The name of the character used to determine which sprite assets to load.
	/// </summary>
	[Export] public string CharacterName { get; set; } = "Ninja Frog";

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		LoadCharacterSprite();
		Play("idle");

		// Randomize the spin direction (clockwise or counter-clockwise) at spawn.
		RotationSpeed *= GD.Randf() > 0.5f ? 1f : -1f;
	}

	// ─── Sprite loading ──────────────────────────────────────────────────────
	/// <summary>
	/// Dynamically builds the SpriteFrames resource based on the character name.
	/// Similar to the logic in MainCharacter, it slices textures into animation frames.
	/// </summary>
	private void LoadCharacterSprite()
	{
		var frames = new SpriteFrames();

		if (CharacterName == "Cyber Shogun")
		{
			// Load specific high-res asset for Cyber Shogun.
			var tex = GD.Load<Texture2D>(
				"res://assets/images/A-16-bit-pixel-art-2D-side-scroller-sprite-of-a-fu-64px-9.png");
			if (tex == null) { GD.Print("Error: Cyber Shogun idle texture not found."); return; }

			frames.AddAnimation("idle");
			frames.SetAnimationLoop("idle", true);
			frames.SetAnimationSpeed("idle", 10f);

			int frameWidth = 61, frameHeight = 71, columns = 3, frameCount = 9;
			for (int i = 0; i < frameCount; i++)
			{
				var atlas = new AtlasTexture { Atlas = tex };
				atlas.Region = new Rect2((i % columns) * frameWidth, (i / columns) * frameHeight,
									          frameWidth, frameHeight);
				frames.AddFrame("idle", atlas);
			}
		}
		else
		{
			// Generic loading for standard characters.
			var basePath = "res://assets/images/" + CharacterName + "/";
			var tex = GD.Load<Texture2D>(basePath + "Idle (32x32).png");
			if (tex == null) { GD.Print("Error: Idle texture not found for ", CharacterName); return; }

			frames.AddAnimation("idle");
			frames.SetAnimationLoop("idle", true);
			frames.SetAnimationSpeed("idle", 10f);

			for (int i = 0; i < 11; i++)
			{
				var atlas = new AtlasTexture { Atlas = tex };
				atlas.Region = new Rect2(i * 32, 0, 32, 32);
				frames.AddFrame("idle", atlas);
			}
		}

		SpriteFrames = frames;
	}

	// ─── Per-frame motion ────────────────────────────────────────────────────
	/// <summary>
	/// Updates the character's position and rotation every frame.
	/// Unity Migration: Move this logic to Update().
	/// </summary>
	public override void _Process(double delta)
	{
		// Apply constant downward movement.
		Position += new Vector2(0, FallSpeed * (float)delta);

		// Apply constant rotation.
		Rotation  += RotationSpeed * (float)delta;

		// Memory management: destroy the object once it completely leaves the bottom of the viewport.
		if (Position.Y > GetViewportRect().Size.Y + 50f)
			QueueFree();
	}
}
