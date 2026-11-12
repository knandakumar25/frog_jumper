using Godot;

/// <summary>
/// Selection-screen animation preview for all six operatives; never writes campaign state.
/// It plays the operative's SpriteCook frames (<c>assets/characters/rendered/&lt;id&gt;.tres</c>, loaded
/// through <c>OperativeVisuals</c>) and shows an empty stage while those are not made yet. The old
/// hand-drawn study art was deleted on 2026-11-12. The class keeps its historical name because the
/// selection scene and tests refer to it.
/// </summary>
public partial class ShogunArtPreview : Control
{
    /// <summary>Stable registry ID of the operative currently shown.</summary>
    public string ActiveOperativeId { get; private set; } = "cyber_shogun";
    /// <summary>True when the shown operative has SpriteCook frames to play.</summary>
    public bool HasArt => _frames != null;
    private int _abilityIndex = -1;
    /// <summary>Selected loadout slot, or -1 for ordinary movement and idle.</summary>
    public int CurrentAbilitySlot => _abilityIndex;
    /// <summary>Action currently being auditioned, independent of campaign state.</summary>
    public string CurrentAction { get; private set; } = "Idle";
    private SpriteFrames? _frames;
    private float _elapsed;
    private readonly Color _cyan = new(0.25f, 0.9f, 1f);

    /// <summary>Loads Cyber Shogun's frames, or null while they are not made yet.</summary>
    public override void _Ready()
    {
        _frames = OperativeVisuals.TryLoadFrames("Cyber Shogun");
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
            _frames = OperativeVisuals.TryLoadFrames(name);
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

    /// <summary>Draws the floor line and, when the operative has frames, the chosen action on it.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#101b2c"));
        float ground = Size.Y - 28;
        DrawLine(new Vector2(12, ground), new Vector2(Size.X - 12, ground), _cyan.Darkened(.6f), 2);
        if (_frames != null) DrawFrames(ground);
    }

    /// <summary>
    /// Plays the operative's frames for the chosen action. Walking reuses the run cycle at a
    /// slower cadence; parry and slam are visual prototypes built from existing animations plus
    /// shield and impact effects. Jumps and dashes add the same lift and travel as the campaign.
    /// </summary>
    private void DrawFrames(float ground)
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
        int count = _frames!.GetFrameCount(animation);
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
        // All frames share a 256px canvas and a foot baseline at y=224.
        // Keep that anchor fixed so changing silhouettes cannot recenter the body.
        float side = Mathf.Min(Size.X * .70f, Mathf.Min(410, Size.Y * .85f));
        DrawTextureRect(_frames.GetFrameTexture(animation, frame),
            new Rect2((Size.X - side) / 2 + offset, ground - side * .875f - lift, side, side), false);
        if (motion == "Kinetic Parry")
            DrawArc(new Vector2(Size.X / 2 + side * .12f, ground - side * .38f), side * .26f,
                -Mathf.Pi / 2, Mathf.Pi / 2, 32, _cyan, 4, true);
    }
}
