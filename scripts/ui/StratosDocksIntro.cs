using System;
using Godot;

/// <summary>
/// Skippable 90-second Pre-Level 8 cinematic prelude overlay ("Infiltration at Stratos Docks").
/// Coordinates high-altitude weather staging, multi-speaker dialogue subtitles, and aviation telemetry
/// across nine authored beats establishing the infiltration into Mission 8 to commandeer the heavy VTOL gunship.
/// </summary>
public partial class StratosDocksIntro : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio stops, for natural, skip, and resource fallback exits.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue track; absent speech exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/stratos_docks_intro/dialogue.ogg";

    /// <summary>Visual and audio resource directory root.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/stratos_docks_intro/";

    /// <summary>When true, automatically navigates to Mission 8 upon completion or skip.</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/stratos_docks_intro/cues.json)
    private static readonly double[] Starts = { 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Altitude 1,200 meters. Wind shear 45 knots. High Command has grounded all commercial transit and initiated priority lockdown across Stratos Docks.",
        "MIRAGE: Thermal scan shows three patrol gunships on the upper pads, but the heavy VTOL transport in Bay 4 is fueled and idling. That's our ticket up the Spire.",
        "TITAN: Mooring clamps are reinforced magnetic titan-locks. I'll need to slam the primary release valves before that bird can lift off.",
        "OCEANUS: The fueling lines are pressurized liquid hydrogen. One rogue spark and the whole flight deck goes up. We keep this clean.",
        "AURA: I'm tracking air traffic control communications. The automated flight grid is scrambling drone interceptors to bottle us up on the catwalks.",
        "SURYA: My solar barrier can deflect their anti-aircraft flak batteries along the open gantry. Stick close to my shield when we cross the flight line.",
        "MIRAGE: The security grid uses dual-phase motion lasers across the maintenance gantries. I'll phase through first and disable the local tripwires.",
        "AXIOM-7: Flight path calculated. Once the VTOL is secured, we will punch directly through the Spire's mid-tier intake vents into the upper command ring.",
        "KAITO: Weapons live, squad. We breach the flight deck, seize that gunship, and take this fight to the Spire. Move out!",
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

    private StratosDocksStage? _stage;
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

        _stage = new StratosDocksStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
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
            AnchorBottom = 0.95f
        };
        ui.AddChild(_panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.01f, 0.02f, 0.04f, 0.95f),
            BorderColor = ColorAxiom,
            BorderWidthTop = 3,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        _panel.AddThemeStyleboxOverride("panel", style);

        _subtitle = new Label
        {
            Name = "Subtitle",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _subtitle.AddThemeFontSizeOverride("font_size", 20);
        _panel.AddChild(_subtitle);

        var skip = new Button
        {
            Name = "Skip",
            Text = "SKIP // ESC / START",
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -260,
            OffsetRight = -24,
            OffsetTop = 24,
            OffsetBottom = 68
        };
        var skipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.05f, 0.1f, 0.85f),
            BorderColor = new Color("00f0ff"),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        skip.AddThemeStyleboxOverride("normal", skipStyle);
        var skipHover = (StyleBoxFlat)skipStyle.Duplicate();
        skipHover.BorderColor = Colors.White;
        skip.AddThemeStyleboxOverride("hover", skipHover);
        skip.AddThemeFontSizeOverride("font_size", 12);
        skip.AddThemeColorOverride("font_color", Colors.White);
        ui.AddChild(skip);
        _skip = skip;
        skip.Pressed += Finish;

        if (IsInsideTree())
        {
            Callable.From(() => { if (IsInstanceValid(this) && IsInsideTree() && IsInstanceValid(skip)) skip.GrabFocus(); }).CallDeferred();
        }

        _speech = CreateAudio("Speech", SpeechPath, 0);
        _ambience = CreateAudio("Atmosphere", ArtPath + "atmosphere.ogg", -16);

        Seek(0);
        if (!_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Creates non-crashing audio player inside tree.</summary>
    private AudioStreamPlayer CreateAudio(string name, string path, float volume)
    {
        var player = new AudioStreamPlayer { Name = name, Bus = "SFX", VolumeDb = volume };
        AddChild(player);
        if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path)) player.Stream = GD.Load<AudioStream>(path);
        if (player.Stream != null && IsInsideTree()) player.Play();
        return player;
    }

    /// <summary>Advances playback timeline each frame.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;

        double audioTime = -1;
        if (_speech?.Stream != null && _speech.Playing)
        {
            audioTime = _speech.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency();
        }

        if (audioTime >= 0)
        {
            if (Math.Abs(audioTime - _lastAudio) < 0.0001) _stalled += delta;
            else { _stalled = 0; _lastAudio = audioTime; }
            if (_stalled < 0.25) Elapsed = Math.Max(Elapsed, audioTime);
            else Elapsed += delta;
        }
        else
        {
            Elapsed += delta;
        }

        Seek(Elapsed);
        if (Elapsed >= Duration) Finish();
    }

    /// <summary>Positions timeline at arbitrary timestamp in seconds.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw();
        }

        int cue = -1;
        for (int i = 0; i < Starts.Length - 1; i++)
        {
            if (Elapsed >= Starts[i] && Elapsed < Starts[i + 1]) { cue = i; break; }
        }

        if (cue >= 0 && cue < Lines.Length && !string.IsNullOrEmpty(Lines[cue]))
        {
            if (_subtitle != null) _subtitle.Text = Lines[cue];
            if (_panel != null)
            {
                _panel.Visible = true;
                UpdatePanelBorder(Lines[cue]);
            }
        }
        else
        {
            if (_panel != null) _panel.Visible = false;
        }
    }

    /// <summary>Colors subtitle border to match the speaking operative.</summary>
    private void UpdatePanelBorder(string line)
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
            string targetScene = "res://scenes/gameplay/level8.tscn";
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
