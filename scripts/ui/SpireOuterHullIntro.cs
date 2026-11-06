using System;
using Godot;

/// <summary>
/// Skippable 90-second Pre-Level 9 cinematic prelude overlay ("Exterior Breach â€” Spire Outer Hull").
/// Coordinates stratospheric weather staging, multi-speaker dialogue subtitles, and combat telemetry
/// across nine authored beats establishing the high-altitude insertion onto the exterior catwalks of the Citadel Spire.
/// </summary>
public partial class SpireOuterHullIntro : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio stops, for natural, skip, and resource fallback exits.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue track; absent speech exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/spire_outer_hull_intro/dialogue.ogg";

    /// <summary>Visual and audio resource directory root.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/spire_outer_hull_intro/";

    /// <summary>When true, automatically navigates to Mission 9 upon completion or skip.</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/spire_outer_hull_intro/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Altitude twenty-five hundred meters. Wind gusts above sixty-five knots. Perimeter flak is tracking our approach.",
        "SURYA: Port engine lock. Raising the forward barrier. Keep her steady while I take the flak.",
        "MIRAGE: Catwalks at Gantry Forty-Two. Narrow rails, but they run straight up the armor. I have our landing.",
        "TITAN: That wind could peel a mech off the wall. Magnetize your boots. Check every tether twice.",
        "OCEANUS: Cryogenic exhaust along the shaft. Expect black ice on the rails. Test your footing before you jump.",
        "AURA: Drone wings are launching from the silos above. The mainframe is sending everything it has down this wall.",
        "AXIOM-7: Cargo bay depressurized. Ramps deploying. After insertion, I will take the transport into cloud cover.",
        "MIRAGE: One-way drop. We climb to the Cyber-Nexus airlock. Stay on the hull, and we have a way through.",
        "KAITO: Weapons armed. Harnesses locked. Axiom Force, onto the catwalks. Begin the ascent.",
        ""
    };

    // Speaker color coding for subtitle border accents
    private static readonly Color ColorKaito = new("00f0ff");    // Cyan
    private static readonly Color ColorMirage = new("aa44ff");   // Violet
    private static readonly Color ColorAura = new("ff00aa");     // Pink
    private static readonly Color ColorOceanus = new("00d4ff");  // Aqua
    private static readonly Color ColorTitan = new("ffcc00");    // Gold
    private static readonly Color ColorSurya = new("ffaa00");    // Amber
    private static readonly Color ColorAxiom = new("00f0ff");     // Cyan

    private SpireOuterHullStage? _stage;
    private PanelContainer? _panel;
    private Label? _subtitle;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;
    private Button? _skip;
    private Input.MouseModeEnum _previousMouseMode;
    private bool _previousPromptVisible = true;
    private Control? _ui;
    private bool _navigating;
    private double _lastAudio = -1;
    private double _stalled;

    /// <summary>Constructs seekable visual stage and responsive UI elements.</summary>
    public override void _Ready()
    {
        _ownership = new CinematicOwnership(this);
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Call("stop_menu_music");

        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        global?.Call("set_control_prompt_visible", false);

        _stage = new SpireOuterHullStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(ui);
        _ui = ui;
        ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _panel = new PanelContainer
        {
            Name = "SubtitlePanel",
            AnchorLeft = 0.08f,
            AnchorRight = 0.92f,
            AnchorTop = 0.76f,
            AnchorBottom = 0.92f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.06f, 0.10f, 0.92f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 6,
            BorderWidthRight = 2,
            BorderColor = ColorAxiom,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        ui.AddChild(_panel);

        _subtitle = new Label
        {
            Name = "Subtitle",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        _subtitle.AddThemeFontSizeOverride("font_size", 20);
        _subtitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.97f, 1f));
        _panel.AddChild(_subtitle);

        _skip = new Button
        {
            Name = "SkipButton",
            Text = "SKIP CINEMATIC [ESC / SPACE]",
            AnchorLeft = 0.78f,
            AnchorRight = 0.95f,
            AnchorTop = 0.04f,
            AnchorBottom = 0.09f,
            FocusMode = Control.FocusModeEnum.None
        };
        var btnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.12f, 0.20f, 0.85f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = new Color(0f, 0.94f, 1f, 0.6f),
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        _skip.AddThemeStyleboxOverride("normal", btnStyle);
        _skip.AddThemeColorOverride("font_color", new Color(0.8f, 0.92f, 1f));
        _skip.AddThemeFontSizeOverride("font_size", 14);
        _skip.Pressed += Finish;
        ui.AddChild(_skip);

        // Ambient high-altitude atmosphere audio
        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "SFX" };
        AddChild(_ambience);
        string atmospherePath = $"{ArtPath.TrimEnd('/')}/atmosphere.ogg";
        if (ResourceLoader.Exists(atmospherePath))
        {
            _ambience.Stream = ResourceLoader.Load<AudioStream>(atmospherePath);
            _ambience.VolumeDb = -4.0f;
            _ambience.Play();
        }

        // Voice track
        _speech = new AudioStreamPlayer { Name = "Speech", Bus = "SFX" };
        AddChild(_speech);
        if (ResourceLoader.Exists(SpeechPath))
        {
            _speech.Stream = ResourceLoader.Load<AudioStream>(SpeechPath);
            _speech.VolumeDb = 1.0f;
            _speech.Play();
        }

        UpdateSubtitles(0.0);
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Advances deterministic timeline and synchronizes visual state.</summary>
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

    /// <summary>Jumps timeline to specified position and updates subtitles and stage.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0.0, Duration);
        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw();
        }

        UpdateSubtitles(Elapsed);
    }

    /// <summary>Scrubs owned audio only on an explicit request, keeping natural playback and missing-speech fallback uninterrupted.</summary>
    public void SeekPlayback(double seconds)
    {
        Seek(seconds);
        _lastAudio = -1; _stalled = 0;
        if (!IsInsideTree() || HasFinished) return;
        if (_speech?.Stream != null) _speech.Play((float)Elapsed);
        if (_ambience?.Stream != null) _ambience.Play((float)Elapsed);
    }

    private void UpdateSubtitles(double time)
    {
        int cue = -1;
        for (int i = 0; i < Starts.Length - 1; i++)
        {
            if (time >= Starts[i] && time < Starts[i + 1])
            {
                cue = i;
                break;
            }
        }

        if (cue >= 0 && cue < Lines.Length)
        {
            string line = Lines[cue];
            if (_subtitle != null) _subtitle.Text = WildfireText.Get($"SUBTITLE_CINEMATIC_SPIRE_OUTER_HULL_INTRO_{cue + 1:D2}", Lines[cue]);
            UpdateBorderColor(line);
        }
        else
        {
            if (_subtitle != null) _subtitle.Text = "";
        }
    }

    private void UpdateBorderColor(string line)
    {
        if (_panel == null) return;
        Color accent = ColorAxiom;
        if (line.StartsWith("KAITO")) accent = ColorKaito;
        else if (line.StartsWith("MIRAGE")) accent = ColorMirage;
        else if (line.StartsWith("TITAN")) accent = ColorTitan;
        else if (line.StartsWith("OCEANUS")) accent = ColorOceanus;
        else if (line.StartsWith("AURA")) accent = ColorAura;
        else if (line.StartsWith("SURYA")) accent = ColorSurya;
        else if (line.StartsWith("AXIOM-7")) accent = ColorAxiom;

        var style = _panel.GetThemeStylebox("panel") as StyleBoxFlat;
        if (style != null) style.BorderColor = accent;
    }

    /// <summary>Gracefully finishes playback, releases inputs, and navigates.</summary>
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
            CampaignProgress.PrepareMission(9);
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(9));
        }
        else QueueFree();
    }

    /// <summary>Stops only audio owned by this presentation on natural completion, Skip and independent removal.</summary>
    private void StopOwnedAudio()
    {
        if (GodotObject.IsInstanceValid(_speech)) _speech!.Stop();
        if (GodotObject.IsInstanceValid(_ambience)) _ambience!.Stop();
    }

    /// <summary>Captures Escape, Space, Enter, Gamepad, and Click to skip playback.</summary>
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

    /// <summary>Restores mouse mode and prompt visibility on exit.</summary>
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
