using System;
using Godot;

/// <summary>
/// Skippable 90-second prologue cinematic overlay for Mission 12 ("The Final Reckoning // Judgment Day Intro").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting Axiom Force arriving inside the Citadel Summit Throne Room
/// and Void Singularity Chamber at 5,000m elevation to confront the Void Commander in the final battle.
/// </summary>
public partial class JudgmentDayIntro : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/judgment_day_intro/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/judgment_day_intro/";

    /// <summary>Whether finishing the cinematic automatically changes the scene to Mission 12.</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/judgment_day_intro/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;
    private double _lastAudio = -1, _stalled;

    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Summit reached. Five thousand meters. Atmospheric containment failing. We are inside the Void Singularity Chamber.",
        "SURYA: That event horizon is bending the light around the dais. Stay behind my bulwark. The radiation is climbing.",
        "OCEANUS: He is draining the planetary geothermal grid into that core. One command, and he could purge the civilian population.",
        "MIRAGE: No guards. No drone army. Just him, waiting on that throne. He knew we would make it this far.",
        "TITAN: We broke his tanks, his labs, and his fortress. Let him wait. That crown is coming off today.",
        "AURA: Beneath all that noise, I can hear them. Millions of people down below, still holding on. Still singing for the dawn.",
        "VOID COMMANDER: You crawled out of the scrap heaps to kneel before me. Your insurrection ends here. Axiom Force dies tonight.",
        "MIRAGE: Phase blades ready. Shields synchronized. Every scar brought us here. We finish this together.",
        "KAITO: For every stolen name, every life the machine took. Axiom Force, draw your weapons. Strike down the Commander.",
        ""
    };

    // Speaker color coding for subtitle border accents
    private static readonly Color ColorKaito = new("f87171");    // Crimson
    private static readonly Color ColorMirage = new("34d399");   // Emerald
    private static readonly Color ColorAura = new("f472b6");     // Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Gold
    private static readonly Color ColorAxiom = new("00f0ff");     // Cyan
    private static readonly Color ColorCommander = new("ff2255"); // Critical Crimson

    private JudgmentDayStage? _stage;
    private Control? _ui;
    private Label? _subtitle;
    private PanelContainer? _panel;
    private StyleBoxFlat? _panelStyle;
    private Button? _skip;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;

    /// <summary>
    /// Configures the canvas layer ordering, creates the procedural visual stage, instantiates subtitle chrome,
    /// and mounts optional audio streams.
    /// </summary>
    public override void _Ready()
    {
        _ownership = new CinematicOwnership(this);
        Layer = 40;

        _stage = new JudgmentDayStage
        {
            Name = "Stage",
            ArtPath = ArtPath
        };
        _stage.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_stage);

        BuildUi();
        MountAudio();
        Seek(0);
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Constructs the responsive HUD overlay for dialogue subtitle captions and skip button.</summary>
    private void BuildUi()
    {
        _ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Ignore };
        _ui.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        _panel = new PanelContainer { Name = "SubtitlePanel" };
        _panel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _panel.OffsetLeft = 140;
        _panel.OffsetRight = -140;
        _panel.OffsetBottom = -64;
        _panel.OffsetTop = -160;

        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.04f, 0.08f, 0.88f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderColor = ColorAxiom,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        _panel.AddThemeStyleboxOverride("panel", _panelStyle);

        _subtitle = new Label
        {
            Name = "Subtitle",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _subtitle.AddThemeFontSizeOverride("font_size", 21);
        _subtitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f));
        _subtitle.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        _subtitle.AddThemeConstantOverride("shadow_offset_x", 1);
        _subtitle.AddThemeConstantOverride("shadow_offset_y", 2);

        _panel.AddChild(_subtitle);
        _ui.AddChild(_panel);

        // Skip prompt button in top right
        _skip = new Button
        {
            Name = "SkipButton",
            Text = "SKIP [ESC / SPACE]",
            CustomMinimumSize = new Vector2(170, 40)
        };
        _skip.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _skip.OffsetLeft = -200;
        _skip.OffsetRight = -30;
        _skip.OffsetTop = 24;
        _skip.OffsetBottom = 64;

        var skipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.16f, 0.75f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = new Color(0.3f, 0.8f, 1.0f, 0.5f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _skip.AddThemeStyleboxOverride("normal", skipStyle);
        _skip.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1.0f, 0.8f));
        _skip.AddThemeFontSizeOverride("font_size", 14);
        _skip.Pressed += Finish;
        _ui.AddChild(_skip);

        AddChild(_ui);
    }

    /// <summary>Mounts voiceover audio and looping void storm atmosphere stream if present on disk.</summary>
    private void MountAudio()
    {
        _speech = new AudioStreamPlayer { Name = "SpeechPlayer", Bus = "SFX" };
        AddChild(_speech);

        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "Music", VolumeDb = -2f };
        AddChild(_ambience);

        string ambPath = ArtPath.TrimEnd('/') + "/atmosphere.ogg";
        if (ResourceLoader.Exists(ambPath))
        {
            var stream = GD.Load<AudioStreamOggVorbis>(ambPath);
            if (stream != null)
            {
                stream.Loop = true;
                _ambience.Stream = stream;
                _ambience.Play();
            }
        }

        if (!string.IsNullOrEmpty(SpeechPath) && ResourceLoader.Exists(SpeechPath))
        {
            var speechStream = GD.Load<AudioStream>(SpeechPath);
            if (speechStream != null)
            {
                _speech.Stream = speechStream;
                _speech.Play();
            }
        }
    }

    /// <summary>Advances the deterministic timeline and triggers visual updates on the stage.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;
        double clock = Elapsed + delta;
        if (_speech is { Playing: true })
        {
            double position = _speech.GetPlaybackPosition();
            _stalled = position <= _lastAudio ? _stalled + delta : 0;
            _lastAudio = position;
            if (_stalled < .5)
                clock = System.Math.Max(Elapsed, position + AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency());
        }
        Seek(clock);
        _subtitle?.AddThemeFontSizeOverride("font_size", CinematicOwnership.SubtitleFont(_subtitle!));
        if (Elapsed >= Duration) Finish();
    }

    /// <summary>Positions the cutscene at an arbitrary timeline offset and synchronizes visuals.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw();
        }

        int cue = 0;
        for (int i = 1; i < Starts.Length; i++)
        {
            if (Elapsed >= Starts[i]) cue = i;
        }

        Color borderColor = cue switch
        {
            0 => ColorAxiom,
            1 => ColorSurya,
            2 => ColorOceanus,
            3 => ColorMirage,
            4 => ColorTitan,
            5 => ColorAura,
            6 => ColorCommander,
            7 => ColorMirage,
            8 => ColorKaito,
            _ => ColorAxiom
        };

        if (_panelStyle != null) _panelStyle.BorderColor = borderColor;
        if (_subtitle != null) _subtitle.Text = WildfireText.Get($"SUBTITLE_CINEMATIC_JUDGMENT_DAY_INTRO_{cue + 1:D2}", Lines[Math.Min(cue, Lines.Length - 1)]);
        if (_panel != null) _panel.Visible = cue >= 0 && cue < 9 && !string.IsNullOrEmpty(Lines[cue]);
    }

    /// <summary>Handles keyboard, controller, mouse, and touch skip shortcuts.</summary>
    public override void _Input(InputEvent input)
    {
        if (HasFinished || !IsInsideTree()) return;
        bool skip = input is InputEventKey { Pressed: true, Echo: false } key && key.Keycode is Key.Escape or Key.Space or Key.Enter;
        skip |= input is InputEventJoypadButton { Pressed: true } joy && joy.ButtonIndex is JoyButton.Start or JoyButton.A or JoyButton.B;
        skip |= input is InputEventScreenTouch { Pressed: true } touch && _skip != null && _skip.GetGlobalRect().HasPoint(touch.Position);
        skip |= input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse && _skip != null && _skip.GetGlobalRect().HasPoint(mouse.Position);
        if (!skip) return;
        GetViewport().SetInputAsHandled();
        foreach (StringName action in InputMap.GetActions()) if (InputMap.EventIsAction(input, action)) Input.ActionRelease(action);
        Finish();
    }

    /// <summary>Completes the cinematic presentation, silences audio players, and transitions to Mission 12.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        StopOwnedAudio();
        foreach (StringName action in InputMap.GetActions()) if (Input.IsActionPressed(action)) Input.ActionRelease(action);
        if (IsInsideTree()) Callable.From(DeliverCompletion).CallDeferred();
        else { _completionDelivered = true; EmitSignal(SignalName.Finished); }
    }

    /// <summary>Delivers completion once beyond input dispatch, then removes the overlay or enters the prepared mission.</summary>
    private void DeliverCompletion()
    {
        if (_completionDelivered || !IsInsideTree()) return;
        _completionDelivered = true;
        _ownership?.Restore();
        EmitSignal(SignalName.Finished);
        if (EnterMissionOnFinish && IsInsideTree())
        {
            CampaignProgress.PrepareMission(12);
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(12));
        }
        else QueueFree();
    }

    /// <summary>Stops only audio owned by this presentation on natural completion, Skip and independent removal.</summary>
    private void StopOwnedAudio()
    {
        if (GodotObject.IsInstanceValid(_speech)) _speech!.Stop();
        if (GodotObject.IsInstanceValid(_ambience)) _ambience!.Stop();
    }

    /// <summary>Ensures audio streams are silenced if overlay is freed directly from tree.</summary>
    public override void _ExitTree()
    {
        StopOwnedAudio();
        _ownership?.Restore();
        if (!_completionDelivered)
        {
            HasFinished = _completionDelivered = true;
            EmitSignal(SignalName.Finished);
        }
    }
}
