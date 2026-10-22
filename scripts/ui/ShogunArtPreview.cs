using Godot;

/// <summary>Shared selection-screen art study for all six operatives; never writes campaign state.</summary>
public partial class ShogunArtPreview : Control
{
    /// <summary>Optional comparison switch; both original and hand-drawn assets are preserved.</summary>
    [Export] public bool UseOriginalDesign { get; set; } = false;
    /// <summary>Stable registry ID of the operative currently shown.</summary>
    public string ActiveOperativeId { get; private set; } = "cyber_shogun";
    /// <summary>Art resource displayed by the hand-drawn branch, exposed for integration checks.</summary>
    public string StudyTexturePath => _operativeArt?.Texture.ResourcePath ?? _atlas?.ResourcePath ?? "";
    private OperativeStudyAtlas? _operativeArt;
    private int _abilityIndex = -1;
    /// <summary>Selected loadout slot, or -1 for ordinary movement and idle.</summary>
    public int CurrentAbilitySlot => _abilityIndex;
    /// <summary>Action currently being auditioned, independent of campaign state.</summary>
    public string CurrentAction { get; private set; } = "Idle";
    private SpriteFrames? _originalFrames;
    private Texture2D? _atlas;
    private Texture2D? _locomotionAtlas;
    private float _elapsed;
    private readonly Color _cyan = new(0.25f, 0.9f, 1f);
    // Explicit art regions preserve long blades and scarf tails in the drawn sheet.
    // Generated pose sheets are not guaranteed to obey a uniform tile boundary.
    private readonly Rect2[] _regions = {
        new(40, 70, 240, 260), new(360, 70, 220, 260),
        new(680, 70, 230, 260), new(990, 70, 240, 260),
        new(20, 400, 260, 230), new(350, 400, 260, 230),
        new(660, 420, 260, 210), new(990, 390, 240, 235),
        new(20, 670, 270, 260), new(350, 700, 320, 230),
        new(690, 680, 230, 250), new(970, 680, 270, 250),
        new(20, 1010, 330, 200), new(370, 950, 270, 280),
        new(680, 950, 250, 280), new(950, 1010, 300, 244)
    };

    /// <summary>Loads both preserved art variants without modifying either resource.</summary>
    public override void _Ready()
    {
        _originalFrames = GD.Load<SpriteFrames>("res://assets/characters/rendered/cyber_shogun.tres");
        _atlas = GD.Load<Texture2D>("res://assets/characters/shogun_study/poses.png");
        _locomotionAtlas = GD.Load<Texture2D>("res://assets/characters/shogun_study/locomotion.png");
        if (_atlas != null)
        {
            using var image = _atlas.GetImage();
            for (int i = 0; i < _regions.Length; i++)
            {
                var region = _regions[i];
                using var crop = image.GetRegion(new Rect2I((Vector2I)region.Position, (Vector2I)region.Size));
                var used = crop.GetUsedRect();
                _regions[i] = new Rect2(region.Position + used.Position, used.Size);
            }
        }
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Restarts the chosen preview action from its first frame.</summary>
    public void Preview(string action)
    {
        CurrentAction = action;
        _abilityIndex = -1;
        _elapsed = 0;
        QueueRedraw();
    }

    /// <summary>Changes the preview body and resets motion without changing global selection or saves.</summary>
    public void SetOperative(string name)
    {
        string id = OperativeRegistry.GetProfile(name).Id;
        if (id != ActiveOperativeId)
        {
            ActiveOperativeId = id;
            _operativeArt = id == "cyber_shogun" ? null : OperativeStudyAtlas.Load(id);
            _originalFrames = OperativeVisuals.LoadFrames(name);
        }
        Preview("Idle");
    }

    /// <summary>Routes a named loadout ability to its primary/secondary/dash/slam pose slot.</summary>
    public void PreviewAbility(string name, int index)
    {
        if (index < 0 || index > 3) return;
        Preview(name);
        _abilityIndex = index;
    }

    /// <summary>Advances visible previews and returns one-shot actions to idle.</summary>
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _elapsed += (float)delta;
        if (_elapsed > 1.6f && CurrentAction is not ("Idle" or "Walk" or "Run"))
            Preview("Idle");
        QueueRedraw();
    }

    /// <summary>Draws the selected art on the shared floor; both panel dimensions constrain its size.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#101b2c"));
        float ground = Size.Y - 28;
        DrawLine(new Vector2(12, ground), new Vector2(Size.X - 12, ground), _cyan.Darkened(.6f), 2);
        if (UseOriginalDesign && _originalFrames != null)
        {
            DrawOriginalDesign(ground);
            return;
        }
        if (_operativeArt != null)
        {
            DrawOperativeStudy(ground);
            return;
        }
        if (_atlas == null) return;
        if (CurrentAction is "Walk" or "Run" && _locomotionAtlas != null)
        {
            DrawLocomotion(ground);
            return;
        }
        int pose = 0;
        float x = 0, lift = 0;
        float t = Mathf.Clamp(_elapsed / 1.2f, 0, 1);
        switch (CurrentAction)
        {
            case "Jump":
            case "Double jump":
                pose = t < .15f ? 6 : 7;
                lift = Mathf.Sin(t * Mathf.Pi) * 90;
                if (CurrentAction == "Double jump") lift += Mathf.Abs(Mathf.Sin(t * Mathf.Tau)) * 35;
                break;
            case "Katana": pose = _elapsed < .5f ? 8 : 9; break;
            case "Martial strike": pose = _elapsed < .5f ? 10 : 11; break;
            case "Shogun's Flash":
            case "Cyber Dash":
                pose = 12;
                x = (Mathf.Clamp(_elapsed * 3, 0, 1) - .5f) * Size.X * .35f;
                break;
            case "Kinetic Parry": pose = 13; break;
            case "Ground Slam":
                pose = _elapsed < .6f ? 14 : 15;
                lift = _elapsed < .6f ? Mathf.Sin(_elapsed / .6f * Mathf.Pi) * 90 : 0;
                if (_elapsed >= .6f)
                    DrawArc(new Vector2(Size.X / 2, ground - 5), 20 + (_elapsed - .6f) * 90,
                        Mathf.Pi, Mathf.Tau, 32, new Color(_cyan, Mathf.Max(0, 1.5f - _elapsed)), 3, true);
                break;
            default: pose = (int)(_elapsed * 2) % 2; break;
        }
        float side = Mathf.Min(Size.X * .76f, Mathf.Min(440, Size.Y * .80f));
        Rect2 source = _regions[pose];
        Vector2 drawnSize = source.Size * (side / 320);
        var destination = new Rect2(new Vector2((Size.X - drawnSize.X) / 2 + x,
            ground - drawnSize.Y - lift), drawnSize);
        DrawTextureRectRegion(_atlas, destination, source);
        // Keep the flat-preview parry effect when auditioning the newer drawing.
        if (CurrentAction == "Kinetic Parry")
            DrawArc(new Vector2(Size.X / 2 + side * .12f, ground - side * .38f), side * .26f,
                -Mathf.Pi / 2, Mathf.Pi / 2, 32, _cyan, 4, true);
    }

    /// <summary>
    /// Plays each new operative's distinct poses using the existing preview's
    /// duration, jump trajectories and dash displacement. Loadout slots select
    /// character-specific primary/secondary art rather than Shogun's katana.
    /// </summary>
    private void DrawOperativeStudy(float ground)
    {
        var art = _operativeArt!;
        int pose = 0;
        float lift = 0, offset = 0;
        float t = Mathf.Clamp(_elapsed / 1.2f, 0, 1);
        if (CurrentAction is "Walk" or "Run")
        {
            // Four key poses span the same full-stride duration as the eight
            // original frames: walk 8/6 seconds, run 8/12 seconds.
            bool running = CurrentAction == "Run";
            pose = (running ? 5 : 1) + (int)(_elapsed * (running ? 6 : 3)) % 4;
        }
        else if (CurrentAction is "Jump" or "Double jump")
        {
            pose = 9;
            lift = Mathf.Sin(t * Mathf.Pi) * 90;
            if (CurrentAction == "Double jump") lift += Mathf.Abs(Mathf.Sin(t * Mathf.Tau)) * 35;
        }
        else if (_abilityIndex == 0) pose = _elapsed < .5f ? 10 : 11;
        else if (_abilityIndex == 1) pose = 12;
        else if (_abilityIndex == 2)
        {
            pose = 13;
            offset = (Mathf.Clamp(_elapsed * 3, 0, 1) - .5f) * Size.X * .35f;
        }
        else if (_abilityIndex == 3)
        {
            pose = _elapsed < .6f ? 14 : 15;
            lift = _elapsed < .6f ? Mathf.Sin(_elapsed / .6f * Mathf.Pi) * 90 : 0;
            if (_elapsed >= .6f)
            {
                var color = OperativeRegistry.GetProfile(ActiveOperativeId.Replace('_', ' ')).ThemeColor;
                DrawArc(new Vector2(Size.X / 2, ground - 5), 20 + (_elapsed - .6f) * 90,
                    Mathf.Pi, Mathf.Tau, 32, new Color(color, Mathf.Max(0, 1.5f - _elapsed)), 3, true);
            }
        }
        Rect2 source = art.Regions[pose];
        float standingHeight = Mathf.Min(Size.Y * .64f, Size.X * .54f);
        float scale = standingHeight / art.StandingHeight;
        float anchorX = (pose % 4 + .5f) * art.Texture.GetWidth() / 4f;
        var destination = new Rect2(
            new Vector2(Size.X / 2 + offset + (source.Position.X - anchorX) * scale,
                ground - lift + (source.Position.Y - art.Baselines[pose / 4]) * scale), source.Size * scale);
        DrawTextureRectRegion(art.Texture, destination, source);
    }

    /// <summary>
    /// Auditions the original flat sprites with the same action choices. Walking
    /// reuses the stride cycle at a slower cadence; parry and slam remain visual
    /// prototypes built from existing poses plus shield/impact effects.
    /// </summary>
    private void DrawOriginalDesign(float ground)
    {
        // The comparison branch also follows non-Shogun loadout buttons without
        // assuming their display names match Cyber Shogun's ability names.
        string motion = ActiveOperativeId == "cyber_shogun" ? CurrentAction : _abilityIndex switch
        {
            0 => "Katana", 1 => "Kinetic Parry", 2 => "Cyber Dash", 3 => "Ground Slam", _ => CurrentAction
        };
        string animation = motion switch
        {
            "Walk" or "Run" => "running",
            "Jump" or "Double jump" => "jumping",
            "Katana" or "Kinetic Parry" => "sword_attack",
            "Martial strike" => "karate_attack",
            "Shogun's Flash" or "Cyber Dash" => "blade_dash",
            "Ground Slam" => _elapsed < .6f ? "jumping" : "karate_attack",
            _ => "idle"
        };
        float fps = CurrentAction == "Walk" ? 6f : CurrentAction == "Run" ? 12f : 8f;
        int count = _originalFrames!.GetFrameCount(animation);
        bool loop = CurrentAction is "Idle" or "Walk" or "Run";
        int frame = loop ? (int)(_elapsed * fps) % count : Mathf.Min((int)(_elapsed * fps), count - 1);
        // Hold the raised blade while the parry barrier is active.
        if (motion == "Kinetic Parry") frame = 0;
        float lift = 0, offset = 0;
        float t = Mathf.Clamp(_elapsed / 1.2f, 0, 1);
        if (CurrentAction is "Jump" or "Double jump")
        {
            lift = Mathf.Sin(t * Mathf.Pi) * 90;
            if (CurrentAction == "Double jump") lift += Mathf.Abs(Mathf.Sin(t * Mathf.Tau)) * 35;
        }
        if (motion is "Shogun's Flash" or "Cyber Dash")
            offset = (Mathf.Clamp(_elapsed * 3, 0, 1) - .5f) * Size.X * .35f;
        if (motion == "Ground Slam")
        {
            lift = _elapsed < .6f ? Mathf.Sin(_elapsed / .6f * Mathf.Pi) * 90 : 0;
            if (_elapsed >= .6f)
                DrawArc(new Vector2(Size.X / 2, ground - 5), 20 + (_elapsed - .6f) * 90,
                    Mathf.Pi, Mathf.Tau, 32, new Color(_cyan, Mathf.Max(0, 1.5f - _elapsed)), 3, true);
        }
        // All original frames share a 256px canvas and a foot baseline at y=224.
        // Keep that anchor fixed so changing silhouettes cannot recenter the body.
        float side = Mathf.Min(Size.X * .70f, Mathf.Min(410, Size.Y * .85f));
        DrawTextureRect(_originalFrames.GetFrameTexture(animation, frame),
            new Rect2((Size.X - side) / 2 + offset, ground - side * .875f - lift, side, side), false);
        if (motion == "Kinetic Parry")
            DrawArc(new Vector2(Size.X / 2 + side * .12f, ground - side * .38f), side * .26f,
                -Mathf.Pi / 2, Mathf.Pi / 2, 32, _cyan, 4, true);
    }

    /// <summary>Plays the preserved hand-drawn eight-frame walk/run cycles in place.</summary>
    private void DrawLocomotion(float ground)
    {
        bool running = CurrentAction == "Run";
        // A complete left/right stride, with a stable canvas anchor. Do not
        // recenter each silhouette or add sinusoidal drift/bob on top of the art:
        // either would make planted feet slide and the body pop between frames.
        int frame = (int)(_elapsed * (running ? 12f : 6f)) % 8;
        int tile = frame + (running ? 8 : 0);
        // The illustrated rows have different gutters. Keep fixed row baselines
        // rather than cropping every frame to its (changing) opaque silhouette.
        int row = tile / 4;
        float[] tops = { 40, 370, 700, 975 };
        float[] heights = { 310, 310, 270, 279 };
        float[] baselines = { 300, 300, 260, 245 };
        float cellWidth = _locomotionAtlas!.GetWidth() / 4f;
        float side = Mathf.Min(Size.X * .76f, Mathf.Min(440, Size.Y * .80f)) * .84f;
        float scale = side / cellWidth;
        var destination = new Rect2((Size.X - side) / 2,
            ground - baselines[row] * scale, side, heights[row] * scale);
        DrawTextureRectRegion(_locomotionAtlas, destination,
            new Rect2((tile % 4) * cellWidth, tops[row], cellWidth, heights[row]));
    }
}
