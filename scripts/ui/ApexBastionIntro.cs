using System;
using Godot;

/// <summary>
/// Skippable 90-second prologue cinematic overlay for Mission 11 ("The Praetorian Vanguard // Apex Bastion Intro").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting Axiom Force arriving at Spire Level 100 (4,500m elevation)
/// to assault the fortified fortress of the Praetorian Guard.
/// </summary>
public partial class ApexBastionIntro : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/apex_bastion_intro/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/apex_bastion_intro/";

    /// <summary>Whether finishing the cinematic automatically changes the scene to Mission 11.</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/apex_bastion_intro/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Level one hundred. Forty-five hundred meters. This is Apex Bastion, fortress of the Praetorian Guard.",
        "TITAN: Three meters of obsidian alloy. Plasma guns covering the courtyard. They have turned the summit into a killing ground.",
        "OCEANUS: The clouds are below us. Ice on the parapets, crosswinds over the rail. Stay away from those edges.",
        "MIRAGE: Heavy signatures ahead. Praetorian shock troops and cyber-samurai. The Commander's personal guard. They know we are here.",
        "AURA: Their comms are silent. No panic. They know the Nexus is gone, and they mean to hold this wall anyway.",
        "SURYA: The stairway leads to the throne room. Artillery at the perimeter gate. We will need every operative to break through.",
        "AXIOM-7: Colossal war chassis at the gatehouse. Bastion Praetor. The last mechanized guardian between us and the Commander.",
        "MIRAGE: We broke the registry, emptied the Grave, and brought their sky-shields down. We are not stopping at the front door.",
        "KAITO: Through the Guard, through the Praetor, and on to the throne. Axiom Force, blades drawn. Breach the Bastion.",
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

    private ApexBastionStage? _stage;
    private Control? _ui;
    private PanelContainer? _panel;
    private Label? _subtitle;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;
    private Button? _skip;
    private StyleBoxFlat? _panelStyle;
    private double _lastAudio;
    private double _stalled;

    /// <summary>Constructs visual stage, subtitle container, skip button, and audio streams.</summary>
    public override void _Ready()
    {
        _ownership = new CinematicOwnership(this);
        ProcessMode = ProcessModeEnum.Always;

        _stage = GetNodeOrNull<ApexBastionStage>("ApexBastionStage");
        if (_stage == null)
        {
            _stage = new ApexBastionStage
            {
                Name = "ApexBastionStage",
                ArtPath = ArtPath,
                AnchorRight = 1,
                AnchorBottom = 1
            };
            AddChild(_stage);
        }

        _ui = new Control { Name = "UI", AnchorRight = 1, AnchorBottom = 1 };
        AddChild(_ui);

        // Subtitle Panel positioned near bottom-center
        _panel = new PanelContainer
        {
            Name = "SubtitlePanel",
            CustomMinimumSize = new Vector2(1100, 110)
        };
        _panel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _panel.OffsetLeft = 360;
        _panel.OffsetRight = -360;
        _panel.OffsetTop = -140;
        _panel.OffsetBottom = -30;

        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.07f, 0.14f, 0.88f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 4,
            BorderWidthRight = 2,
            BorderColor = ColorAxiom,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        _panel.AddThemeStyleboxOverride("panel", _panelStyle);

        _subtitle = new Label
        {
            Name = "Subtitle",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _subtitle.AddThemeFontSizeOverride("font_size", 20);
        _subtitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f));
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
        InputPromptBinding.Cinematic(_skip, true);
        _ui.AddChild(_skip);

        // Audio streams
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
            var stream = GD.Load<AudioStream>(SpeechPath);
            if (stream != null)
            {
                _speech.Stream = stream;
                _speech.Play();
            }
        }

        Seek(0);
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Advances the monotonic playback clock and handles auto-completion upon reaching 90s.</summary>
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

    /// <summary>Samples any timeline second without completing, updating stage graphics and subtitle text.</summary>
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
            1 => ColorTitan,
            2 => ColorOceanus,
            3 => ColorMirage,
            4 => ColorAura,
            5 => ColorSurya,
            6 => ColorAxiom,
            7 => ColorMirage,
            8 => ColorKaito,
            _ => ColorAxiom
        };

        if (_panelStyle != null) _panelStyle.BorderColor = borderColor;
        if (_subtitle != null) _subtitle.Text = WildfireText.Get($"SUBTITLE_CINEMATIC_APEX_BASTION_INTRO_{cue + 1:D2}", Lines[Math.Min(cue, Lines.Length - 1)]);
        if (_panel != null) _panel.Visible = cue >= 0 && cue < 9 && !string.IsNullOrEmpty(Lines[cue]);
    }

    /// <summary>Handles keyboard, controller, mouse, and touch skip shortcuts.</summary>
    public override void _Input(InputEvent input)
    {
        InputPromptService.Instance?.ObserveInput(input);
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

    /// <summary>Completes the cinematic presentation, silences audio players, and transitions to Mission 11.</summary>
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
            CampaignProgress.PrepareMission(11);
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(11));
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
