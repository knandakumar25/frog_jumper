using System;
using Godot;

/// <summary>
/// Skippable 90-second post-mission cinematic epilogue overlay ("Airborne Ascension // Mission 8 Aftermath").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting the hijacked VTOL gunship taking flight toward the Citadel Spire Outer Hull.
/// </summary>
public partial class StratosDocksAftermath : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/stratos_docks_aftermath/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/stratos_docks_aftermath/";

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/stratos_docks_aftermath/cues.json)
    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: VTOL flight systems online. Avionics decoupled from Citadel air traffic control. Dual ion-thrusters at 100% output.",
        "MIRAGE: Look at those flight decks shrink behind us. The fleet is grounded, their interceptors crippled in their berths. The sky is ours.",
        "TITAN: Feels good to fly something built like a fortress. Hydraulic clamps didn't stand a chance. This bird can take a pounding.",
        "OCEANUS: Look through the storm squall below. The city lights are shining without the sky-shield dome. Three municipal sectors breathing free tonight.",
        "AURA: I can hear the Citadel's distress broadcast. High command is in full panic. They're scrambling all exterior hull automated defense batteries.",
        "SURYA: Sensor lock dead ahead! We've broken through the storm ceiling... Look at the Spire. Kilometers of armored exterior plating.",
        "AXIOM-7: Telemetry warning: Outer Spire heavy anti-air batteries detecting our radar signature. Direct flight to the summit is blocked by automated hull turrets.",
        "MIRAGE: Then we don't land at the top. We insert directly onto the exterior maintenance catwalks. We scale the Spire outer hull on foot!",
        "KAITO: Agreed. Secure your harnesses. We breach the Spire's outer skin and carve our way to the summit. Operatives—prepare for exterior insertion!",
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

    private StratosDocksAftermathStage? _stage;
    private PanelContainer? _panel;
    private Label? _subtitle;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;
    private Button? _skip;
    private Input.MouseModeEnum _previousMouseMode;
    private bool _previousPromptVisible = true;
    private double _lastAudio = -1;
    private double _stalled;
    private StyleBoxFlat? _panelStyle;

    /// <summary>Constructs procedural stage, subtitle panel, skip button, and starts audio playback.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Preserve and hide persistent control prompts from Global
        var global = GetNodeOrNull<Node>("/root/Global");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);

        // Procedural visual stage
        _stage = new StratosDocksAftermathStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // UI overlay with Subtitle panel and Skip button
        var ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(ui);
        ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _panel = new PanelContainer { Name = "SubtitlePanel" };
        ui.AddChild(_panel);
        _panel.AnchorLeft = 0.1f;
        _panel.AnchorRight = 0.9f;
        _panel.AnchorTop = 0.82f;
        _panel.AnchorBottom = 0.94f;
        _panel.OffsetLeft = 0;
        _panel.OffsetRight = 0;
        _panel.OffsetTop = 0;
        _panel.OffsetBottom = 0;

        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.08f, 0.88f),
            BorderWidthTop = 3,
            BorderColor = ColorAxiom,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        _panel.AddThemeStyleboxOverride("panel", _panelStyle);

        _subtitle = new Label
        {
            Name = "Subtitle",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _subtitle.AddThemeFontSizeOverride("font_size", 20);
        _subtitle.AddThemeColorOverride("font_color", Colors.White);
        _panel.AddChild(_subtitle);

        _skip = new Button
        {
            Name = "SkipButton",
            Text = "SKIP [ESC / SPACE]",
            CustomMinimumSize = new Vector2(160, 42)
        };
        ui.AddChild(_skip);
        _skip.AnchorLeft = 0.88f;
        _skip.AnchorRight = 0.98f;
        _skip.AnchorTop = 0.04f;
        _skip.AnchorBottom = 0.09f;
        _skip.Pressed += Finish;

        // Audio streams
        _speech = new AudioStreamPlayer { Name = "SpeechPlayer", Bus = "Master" };
        AddChild(_speech);

        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "Master", VolumeDb = -4f };
        AddChild(_ambience);

        StartPlayback();
    }

    /// <summary>Initiates atmosphere playback and optional mastered speech dialogue.</summary>
    private void StartPlayback()
    {
        string atmospherePath = ArtPath + "atmosphere.ogg";
        if (ResourceLoader.Exists(atmospherePath))
        {
            _ambience!.Stream = GD.Load<AudioStream>(atmospherePath);
            _ambience.Play();
        }

        if (ResourceLoader.Exists(SpeechPath))
        {
            _speech!.Stream = GD.Load<AudioStream>(SpeechPath);
            _speech.Play();
        }
    }

    /// <summary>Advances the deterministic timeline, drives procedural stage sampling, and updates subtitles.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;

        double current;
        if (_speech != null && _speech.Playing)
        {
            current = _speech.GetPlaybackPosition();
            if (Math.Abs(current - _lastAudio) < 0.0001)
            {
                _stalled += delta;
                if (_stalled > 0.5) current = Elapsed + delta;
            }
            else
            {
                _stalled = 0;
                _lastAudio = current;
            }
        }
        else
        {
            current = Elapsed + delta;
        }

        Elapsed = Math.Max(Elapsed, current);

        if (Elapsed >= Duration)
        {
            Finish();
            return;
        }

        Seek(Elapsed);
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
        if (_subtitle != null) _subtitle.Text = Lines[Math.Min(cue, Lines.Length - 1)];
        if (_panel != null) _panel.Visible = cue >= 0 && cue < 9 && !string.IsNullOrEmpty(Lines[cue]);
    }

    /// <summary>Handles keyboard, controller, mouse, and touch skip shortcuts.</summary>
    public override void _Input(InputEvent input)
    {
        if (HasFinished || !IsInsideTree()) return;
        bool skip = input is InputEventKey { Pressed: true, Echo: false } key &&
            key.Keycode is Key.Escape or Key.Space or Key.Enter;
        skip |= input is InputEventJoypadButton { Pressed: true } joy && joy.ButtonIndex is JoyButton.Start or JoyButton.A;
        skip |= input is InputEventScreenTouch { Pressed: true } touch && _skip != null && _skip.GetGlobalRect().HasPoint(touch.Position);
        skip |= input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse &&
            _skip != null && _skip.GetGlobalRect().HasPoint(mouse.Position);
        if (!skip) return;
        GetViewport()?.SetInputAsHandled();
        foreach (StringName action in InputMap.GetActions())
        {
            if (InputMap.EventIsAction(input, action)) Input.ActionRelease(action);
        }
        Finish();
    }

    /// <summary>Stops active audio streams and emits the single Finished signal callback.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;

        _speech?.Stop();
        _ambience?.Stop();

        EmitSignal(SignalName.Finished);
    }

    /// <summary>Restores persistent prompt visibility and mouse cursor mode when leaving scene tree.</summary>
    public override void _ExitTree()
    {
        Input.MouseMode = _previousMouseMode;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
    }
}
