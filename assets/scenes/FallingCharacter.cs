using Godot;

/// <summary>
/// Decorative falling character used on the main-menu background.
/// Loads the correct sprite sheet at runtime and tumbles down the screen.
/// Converted from falling_character.gd (and replaces the near-identical falling_turtle.gd).
/// </summary>
public partial class FallingCharacter : AnimatedSprite2D
{
    // ─── Configurable properties ─────────────────────────────────────────────
    [Export] public float FallSpeed     { get; set; } = 200.0f;
    [Export] public float RotationSpeed { get; set; } = 2.0f;
    [Export] public string CharacterName { get; set; } = "Ninja Frog";

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        LoadCharacterSprite();
        Play("idle");
        // Randomise spin direction
        RotationSpeed *= GD.Randf() > 0.5f ? 1f : -1f;
    }

    // ─── Sprite loading ──────────────────────────────────────────────────────
    private void LoadCharacterSprite()
    {
        var frames = new SpriteFrames();

        if (CharacterName == "Cyber Shogun")
        {
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
    public override void _Process(double delta)
    {
        Position += new Vector2(0, FallSpeed * (float)delta);
        Rotation  += RotationSpeed * (float)delta;

        if (Position.Y > GetViewportRect().Size.Y + 50f)
            QueueFree();
    }
}

