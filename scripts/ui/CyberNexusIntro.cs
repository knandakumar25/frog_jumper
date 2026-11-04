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
    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Ingress confirmed. We have crossed into the Citadel Spire interior core. Establishing hardline link into the Cyber-Nexus mainframe architecture.",
        "MIRAGE: Look at the scale of this place... It’s like stepping inside the belly of a leviathan made of glowing glass and cold silicon. Not a speck of dust.",
        "TITAN: Listen to that hum. The whole tower is vibrating. Every server rack in here could power an entire city block, and they're using it to run death machines.",
        "OCEANUS: High-pressure liquid helium conduits are running beneath the deck plating. If we rupture the manifold cooling lines, the quantum processors will melt themselves down.",
        "AURA: The neural matrix is awake! It’s probing our bio-signatures. The Nexus Overseer knows we’re here, and it’s rerouting automated security drones to our sector.",
        "SURYA: High-frequency defensive lasers and biometric firewalls are locking down the bridges ahead. My solar barrier can absorb their electromagnetic surges—stay behind me!",
        "AXIOM-7: Mainframe telemetry analyzed: Three quantum processing nodes regulate the Spire's defense grid. Neutralizing all three will cause a fatal cascading logic failure.",
        "MIRAGE: Once the Nexus falls, the Praetorian guards at the Apex Bastion will lose their automated reinforcements. This is where we break their spine.",
        "KAITO: No retreats, no hesitations. We strike the processing nodes, silence the Overseer, and tear the Citadel's digital throne to pieces. Axiom Force—advance!",
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
        _speech = new AudioStreamPlayer { Name = "SpeechPlayer", Bus = "Master" };
        AddChild(_speech);

        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "Music", VolumeDb = -2f };
        AddChild(_ambience);

        TreeExiting += OnTreeExiting;
        StartPlayback();
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
    public override void _UnhandledInput(InputEvent @event)
    {
        if (HasFinished) return;

        if (@event.IsActionPressed("ui_cancel") ||
            @event.IsActionPressed("ui_accept") ||
            (@event is InputEventKey k && k.Pressed && !k.Echo &&
             (k.Keycode == Key.Escape || k.Keycode == Key.Space || k.Keycode == Key.Enter)) ||
            (@event is InputEventJoypadButton j && j.Pressed &&
             (j.ButtonIndex == JoyButton.Start || j.ButtonIndex == JoyButton.A)) ||
            (@event is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left))
        {
            Finish();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Completes the cinematic presentation, silences audio players, and transitions to Mission 10.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        _speech?.Stop();
        _ambience?.Stop();
        EmitSignal(SignalName.Finished);

        if (EnterMissionOnFinish && IsInsideTree())
        {
            string nextScene = ResourceLoader.Exists("res://scenes/gameplay/level10.tscn")
                ? "res://scenes/gameplay/level10.tscn"
                : "res://scenes/main_menu/level_selection.tscn";
            GetTree().ChangeSceneToFile(nextScene);
        }
        else
        {
            QueueFree();
        }
    }
}
