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
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Altitude twelve hundred meters. Wind shear forty-five knots. Commercial transit is grounded. Stratos Docks is under lockdown.",
        "MIRAGE: Three gunships on the upper pads. But Bay Four has a heavy transport fueled and idling. That is our ride.",
        "TITAN: Magnetic mooring clamps. Reinforced locks. Get me to the release valves, and I will free that bird.",
        "OCEANUS: Those fuel lines carry liquid hydrogen. One spark could take the deck with it. We keep this clean.",
        "AURA: Air traffic control is calling in interceptors. The flight grid is trying to pin us on the catwalks.",
        "SURYA: I can cover the flak crossing with my solar barrier. Stay close until we are off the flight line.",
        "MIRAGE: Dual-phase lasers across the maintenance route. I will cross first and open a path through the security grid.",
        "AXIOM-7: Flight path ready. Once we have the transport, we climb toward the Spire upper command ring.",
        "KAITO: Weapons live. Breach the flight deck, seize that gunship, and take this fight upstairs. Axiom Force, move out.",
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
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
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
            CampaignProgress.PrepareMission(8);
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(8));
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
