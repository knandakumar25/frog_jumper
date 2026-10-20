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
	[Export] public string CharacterName { get; set; } = "Cyber Shogun";

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
	/// Loads the same baked original-model animation used by the playable operative.
	/// </summary>
	private void LoadCharacterSprite()
	{
		SpriteFrames = OperativeVisuals.LoadFrames(CharacterName);
		Scale *= 0.37f;
		TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
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
