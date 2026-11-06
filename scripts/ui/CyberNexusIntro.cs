using System;
using Godot;

/// <summary>
/// Skippable 90-second prologue cinematic overlay ("The Heart of the Machine // Cyber-Nexus Intro").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting Axiom Force's ingress into the Citadel's core mainframe architecture.
/// </summary>
public partial class CyberNexusIntro : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/cyber_nexus_intro/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_nexus_intro/";

    /// <summary>Whether to automatically load Mission 10 upon completion (disabled in isolated unit tests).</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/cyber_nexus_intro/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Ingress confirmed. We are inside the Spire core. Establishing a hardline connection to the Cyber-Nexus.",
        "MIRAGE: Glass, silicon, not a trace of dust. It feels like walking into something that has never needed a human.",
        "TITAN: Every rack could power a city block. All that energy, wasted on watching people and building machines to kill them.",
        "OCEANUS: Liquid helium under the deck. Disrupt the cooling manifold, and these processors will overheat on their own.",
        "AURA: The matrix is awake. It is probing our signatures. The Overseer has found us, and security is closing in.",
        "SURYA: Biometric firewalls on the bridges. I can absorb the surges. Stay behind the barrier while we cross.",
        "AXIOM-7: Three quantum nodes regulate the Spire defense grid. Override all three to stop its automated reinforcements.",
        "MIRAGE: Without this core, the Bastion loses its machine army. We break the chain here, before the final climb.",
        "KAITO: Silence the Overseer. Override the nodes. We finish this together. Axiom Force, advance.",
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

    private CyberNexusStage? _stage;
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

        _stage = GetNodeOrNull<CyberNexusStage>("CyberNexusStage");
        if (_stage == null)
        {
            _stage = new CyberNexusStage
            {
                Name = "CyberNexusStage",
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
            BgColor = new Color(0.04f, 0.07f, 0.14f, 0.90f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderColor = ColorAxiom,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
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
        _ui.AddChild(_skip);

        // Audio streams
        _speech = new AudioStreamPlayer { Name = "SpeechPlayer", Bus = "SFX" };
        AddChild(_speech);

        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "Music", VolumeDb = -2f };
        AddChild(_ambience);

        TreeExiting += OnTreeExiting;
        StartPlayback();
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    private void StartPlayback()
    {
        if (ResourceLoader.Exists(SpeechPath))
        {
            _speech!.Stream = GD.Load<AudioStream>(SpeechPath);
            if (IsInsideTree()) _speech.Play();
        }

        string atmospherePath = ArtPath.TrimEnd('/') + "/atmosphere.ogg";
        if (ResourceLoader.Exists(atmospherePath))
        {
            _ambience!.Stream = GD.Load<AudioStream>(atmospherePath);
            if (IsInsideTree()) _ambience.Play();
        }

        Seek(0);
    }

    private void OnTreeExiting()
    {
        _speech?.Stop();
        _ambience?.Stop();
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
            1 => ColorMirage,
            2 => ColorTitan,
            3 => ColorOceanus,
            4 => ColorAura,
            5 => ColorSurya,
            6 => ColorAxiom,
            7 => ColorMirage,
            8 => ColorKaito,
            _ => ColorAxiom
        };

        if (_panelStyle != null) _panelStyle.BorderColor = borderColor;
        if (_subtitle != null) _subtitle.Text = WildfireText.Get($"SUBTITLE_CINEMATIC_CYBER_NEXUS_INTRO_{cue + 1:D2}", Lines[Math.Min(cue, Lines.Length - 1)]);
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

    /// <summary>Completes the cinematic presentation, silences audio players, and transitions to Mission 10.</summary>
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
            CampaignProgress.PrepareMission(10);
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(10));
        }
        else QueueFree();
    }

    /// <summary>Stops only audio owned by this presentation on natural completion, Skip and independent removal.</summary>
    private void StopOwnedAudio()
    {
        if (GodotObject.IsInstanceValid(_speech)) _speech!.Stop();
        if (GodotObject.IsInstanceValid(_ambience)) _ambience!.Stop();
    }
    /// <summary>Independent removal restores ownership and releases the retained mission exactly once.</summary>
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
