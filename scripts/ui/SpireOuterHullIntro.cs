using System;
using Godot;

/// <summary>
/// Skippable 90-second Pre-Level 9 cinematic prelude overlay ("Exterior Breach — Spire Outer Hull").
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
    private static readonly double[] Starts = { 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Altitude 2,500 meters. Approaching the Citadel Spire exterior flank. Wind gusts exceeding 65 knots. Automated perimeter flak batteries are tracking our trajectory.",
        "SURYA: Target lock detected on our port engine! Raising forward solar barrier to absorb their heavy flak bursts. Keep us steady in the turbulence!",
        "MIRAGE: Look at that monolithic wall of armor plating... Catwalks sighted at Gantry Level 42. Narrow maintenance rails running straight up the hull.",
        "TITAN: The shear winds out there will rip a mech clean off the wall. Magnetize your boot solenoids and check your harness tethers!",
        "OCEANUS: Thermal exhaust vents along the ascent shaft are venting sub-zero cryogenic coolant. Surfaces will be slick with black ice.",
        "AURA: Citadel security mainframe is alerting all defensive automated drone wings. Aerial interceptors are launching from exterior silo racks above us!",
        "AXIOM-7: Cargo bay depressurized. Side ramps deploying. Once you drop onto the gantry, I will take the gunship into high-orbit cloud cover to evade anti-air destruction.",
        "MIRAGE: One-way drop. We scale the Spire exterior on foot, punch through the blast doors into the Cyber-Nexus, or fall into the abyss.",
        "KAITO: Axiom Force! Weapons armed, grapples locked! Leap to the catwalks and begin the ascent! BREACH NOW!",
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
        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "Master" };
        AddChild(_ambience);
        string atmospherePath = $"{ArtPath.TrimEnd('/')}/atmosphere.ogg";
        if (ResourceLoader.Exists(atmospherePath))
        {
            _ambience.Stream = ResourceLoader.Load<AudioStream>(atmospherePath);
            _ambience.VolumeDb = -4.0f;
            _ambience.Play();
        }

        // Voice track
        _speech = new AudioStreamPlayer { Name = "Speech", Bus = "Master" };
        AddChild(_speech);
        if (ResourceLoader.Exists(SpeechPath))
        {
            _speech.Stream = ResourceLoader.Load<AudioStream>(SpeechPath);
            _speech.VolumeDb = 1.0f;
            _speech.Play();
        }

        UpdateSubtitles(0.0);
    }

    /// <summary>Advances deterministic timeline and synchronizes visual state.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;

        double target = Elapsed;
        if (_speech != null && _speech.Playing)
        {
            double pos = _speech.GetPlaybackPosition();
            if (pos > _lastAudio) { _lastAudio = pos; _stalled = 0; }
            else { _stalled += delta; }

            if (_stalled > 0.3) target += delta;
            else target = Math.Max(target, pos);
        }
        else
        {
            target += delta;
        }

        Seek(target);

        if (Elapsed >= Duration)
        {
            Finish();
        }
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

        if (_speech != null && _speech.Stream != null && Math.Abs(_speech.GetPlaybackPosition() - Elapsed) > 0.25)
        {
            if (Elapsed < Duration) _speech.Play((float)Elapsed);
            else _speech.Stop();
        }

        UpdateSubtitles(Elapsed);
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
            if (_subtitle != null) _subtitle.Text = line;
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

        if (_speech != null && IsInsideTree()) _speech.Stop();
        if (_ambience != null && IsInsideTree()) _ambience.Stop();

        EmitSignal(SignalName.Finished);

        if (EnterMissionOnFinish && !_navigating && IsInsideTree())
        {
            _navigating = true;
            string targetScene = "res://scenes/gameplay/level9.tscn";
            if (!ResourceLoader.Exists(targetScene)) targetScene = "res://scenes/main_menu/level_selection.tscn";

            if (GetTree() != null)
            {
                GetTree().Paused = false;
                GetTree().ChangeSceneToFile(targetScene);
            }
        }
    }

    /// <summary>Captures Escape, Space, Enter, Gamepad, and Click to skip playback.</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (HasFinished) return;

        if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("ui_accept") ||
            (@event is InputEventKey key && key.Pressed && (key.Keycode == Key.Space || key.Keycode == Key.Enter || key.Keycode == Key.Escape)) ||
            (@event is InputEventJoypadButton joy && joy.Pressed && (joy.ButtonIndex == JoyButton.Start || joy.ButtonIndex == JoyButton.A)))
        {
            GetViewport()?.SetInputAsHandled();
            Finish();
        }
    }

    /// <summary>Restores mouse mode and prompt visibility on exit.</summary>
    public override void _ExitTree()
    {
        Input.MouseMode = _previousMouseMode;
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Call("set_control_prompt_visible", _previousPromptVisible);
        base._ExitTree();
    }
}
