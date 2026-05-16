using Godot;

/// <summary>
/// Character selection screen. Builds sprite animations, creates a new session
/// in SessionManager, then transitions to the story intro via LoadingScreen.
/// Converted from character_selection.gd.
/// </summary>
public partial class CharacterSelection : Control
{
    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        GetNode<Node>("/root/Global").Call("play_menu_music");
        SetupCharacterAnimations();
    }

    // ─── Button callbacks ────────────────────────────────────────────────────
    public void _on_ninja_frog_pressed()    => CreateSessionAndStart("Ninja Frog");
    public void _on_mask_dude_pressed()     => CreateSessionAndStart("Mask Dude");
    public void _on_pink_man_pressed()      => CreateSessionAndStart("Pink Man");
    public void _on_virtual_guy_pressed()   => CreateSessionAndStart("Virtual Guy");

    // ─── Session creation ────────────────────────────────────────────────────
    private void CreateSessionAndStart(string characterName)
    {
        var global = GetNode<Node>("/root/Global");
        global.Set("selected_character", characterName);

        int sessionId = SessionManager.Instance.CreateNewSession(characterName);
        if (sessionId > 0)
            GD.Print("Created new session: ", sessionId, " for character: ", characterName);
        else
            GD.Print("Error creating session!");

        // Always proceed to story intro
        LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/story_intro.tscn");
    }

    // ─── Sprite animation setup ──────────────────────────────────────────────
    private void SetupCharacterAnimations()
    {
        var characters = new[]
        {
            ("Ninja Frog",   GetNode<AnimatedSprite2D>("VBoxContainer/CharacterGrid/NinjaFrog/CharacterButton/AnimatedSprite")),
            ("Mask Dude",    GetNode<AnimatedSprite2D>("VBoxContainer/CharacterGrid/MaskDude/CharacterButton/AnimatedSprite")),
            ("Pink Man",     GetNode<AnimatedSprite2D>("VBoxContainer/CharacterGrid/PinkMan/CharacterButton/AnimatedSprite")),
            ("Virtual Guy",  GetNode<AnimatedSprite2D>("VBoxContainer/CharacterGrid/VirtualGuy/CharacterButton/AnimatedSprite")),
        };

        foreach (var (name, sprite) in characters)
        {
            var frames = new SpriteFrames();
            var tex    = GD.Load<Texture2D>("res://assets/images/" + name + "/Idle (32x32).png");
            if (tex == null) { GD.Print("Error: Idle texture not found for ", name); continue; }

            AddAnimationFrames(frames, "idle", tex, 11, 10f);
            sprite.SpriteFrames = frames;
            sprite.Play("idle");
        }
    }

    // ─── Atlas helpers ───────────────────────────────────────────────────────
    private static void AddAnimationFrames(
        SpriteFrames frames, string animName, Texture2D texture,
        int frameCount, float speed)
    {
        frames.AddAnimation(animName);
        frames.SetAnimationLoop(animName, true);
        frames.SetAnimationSpeed(animName, speed);

        for (int i = 0; i < frameCount; i++)
        {
            var atlas = new AtlasTexture { Atlas = texture };
            atlas.Region = new Rect2(i * 32, 0, 32, 32);
            frames.AddFrame(animName, atlas);
        }
    }

    private static void AddGridAnimationFrames(
        SpriteFrames frames, string animName, Texture2D texture,
        Vector2I frameSize, int columns, int frameCount, float speed)
    {
        frames.AddAnimation(animName);
        frames.SetAnimationLoop(animName, true);
        frames.SetAnimationSpeed(animName, speed);

        for (int i = 0; i < frameCount; i++)
        {
            var atlas = new AtlasTexture { Atlas = texture };
            float x = (i % columns) * frameSize.X;
            float y = (i / columns) * frameSize.Y;
            atlas.Region = new Rect2(x, y, frameSize.X, frameSize.Y);
            frames.AddFrame(animName, atlas);
        }
    }
}

