using System;
using Godot;

/// <summary>
/// Skippable 90-second post-mission cinematic epilogue overlay ("The Logic Collapse // Mission 10 Aftermath").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting the aftermath of the Nexus Overseer's defeat and the ascent into the Apex Bastion.
/// </summary>
public partial class CyberNexusAftermath : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/cyber_nexus_aftermath/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_nexus_aftermath/";

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/cyber_nexus_aftermath/cues.json)
    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Nexus Overseer core neutralized. All three quantum processing nodes have collapsed into terminal logic cascades. Mainframe defense grid has flatlined across the entire Citadel.",
        "MIRAGE: Look at the server towers—they're dumping petabytes of encrypted garbage! The automated reinforcement queues just zeroed out. The Citadel's robot army is dead in the water.",
        "TITAN: That Overseer thought its containment prisms could cage us. Turned its fancy laser ring into scrap metal. Look at the cooling lines—liquid helium is blowing every gasket in the room.",
        "OCEANUS: The thermal runaway is accelerating. The entire subterranean mainframe is melting down behind us. We can't linger here—the deck plating is already vibrating from reactor shockwaves.",
        "SURYA: The digital shield is broken, but look up through the gantry shaft. The Praetorian automated batteries have switched to emergency auxiliary generators. The Bastion above is locking down.",
        "AURA: I hear them... heavy hydraulic footsteps. Praetorian battle mechs and elite cyber-samurai are rallying on the perimeter ramparts above. They know their brain is dead, and they're digging in for a final stand.",
        "MIRAGE: The Apex Ascent Lift is online! I've bypassed the executive security lockout. This high-speed freight lift will take us straight through the Bastion's throat!",
        "AXIOM-7: Warning: Sensor telemetry detects extreme energy signatures atop the Spire. The Void Commander is aware of the Nexus collapse and has ordered the Bastion to fire at will.",
        "KAITO: Then we give them the fight they asked for. No more hiding behind machines and algorithms. Board the lift. Next stop: The Apex Bastion!",
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

    private CyberNexusAftermathStage? _stage;
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

        _stage = GetNodeOrNull<CyberNexusAftermathStage>("CyberNexusAftermathStage");
        if (_stage == null)
        {
            _stage = new CyberNexusAftermathStage
            {
                Name = "CyberNexusAftermathStage",
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

        // Skip Button in top-right
        _skip = new Button
        {
            Name = "SkipButton",
            Text = "SKIP [ESC / START]",
            CustomMinimumSize = new Vector2(180, 42)
        };
        _skip.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _skip.OffsetLeft = -220;
        _skip.OffsetRight = -30;
        _skip.OffsetTop = 30;
        _skip.OffsetBottom = 72;
        _skip.FocusMode = Control.FocusModeEnum.None;

        var skipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.12f, 0.22f, 0.75f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 2,
            BorderWidthRight = 1,
            BorderColor = new Color(0f, 0.94f, 1f, 0.5f),
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        _skip.AddThemeStyleboxOverride("normal", skipStyle);
        _skip.AddThemeFontSizeOverride("font_size", 14);
        _skip.Pressed += Finish;
        _ui.AddChild(_skip);

        // Ambience Player (looped procedural atmospheric audio)
        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "Master" };
        AddChild(_ambience);
        string ambPath = ArtPath.TrimEnd('/') + "/atmosphere.ogg";
        if (ResourceLoader.Exists(ambPath))
        {
            var stream = GD.Load<AudioStreamOggVorbis>(ambPath);
            if (stream != null)
            {
                stream.Loop = true;
                _ambience.Stream = stream;
                _ambience.VolumeDb = -6f;
                _ambience.Play();
            }
        }

        // Voice Player
        _speech = new AudioStreamPlayer { Name = "Speech", Bus = "Master" };
        AddChild(_speech);
        if (!string.IsNullOrEmpty(SpeechPath) && ResourceLoader.Exists(SpeechPath))
        {
            var vStream = GD.Load<AudioStream>(SpeechPath);
            if (vStream != null)
            {
                _speech.Stream = vStream;
                _speech.Play();
            }
        }

        UpdateSubtitles();
    }

    /// <summary>Advances deterministic playback clock and triggers stage redraw.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;

        if (_speech != null && _speech.Playing)
        {
            double pos = _speech.GetPlaybackPosition();
            if (Math.Abs(pos - _lastAudio) < 0.001)
            {
                _stalled += delta;
                if (_stalled > 0.5) Elapsed += delta;
            }
            else
            {
                _stalled = 0;
                Elapsed = pos;
            }
            _lastAudio = pos;
        }
        else
        {
            Elapsed += delta;
        }

        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw();
        }

        UpdateSubtitles();

        if (Elapsed >= Duration)
        {
            Finish();
        }
    }

    /// <summary>Handles ESC, Enter, Space, and gamepad skip inputs.</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (HasFinished) return;

        if (@event.IsActionPressed("ui_cancel") ||
            @event.IsActionPressed("ui_accept") ||
            (@event is InputEventKey key && key.Pressed &&
             (key.Keycode == Key.Escape || key.Keycode == Key.Space || key.Keycode == Key.Enter)) ||
            (@event is InputEventJoypadButton joy && joy.Pressed &&
             (joy.ButtonIndex == JoyButton.Start || joy.ButtonIndex == JoyButton.A)))
        {
            GetViewport()?.SetInputAsHandled();
            Finish();
        }
    }

    /// <summary>Positions timeline clock deterministically for testing or seek scrubbing.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        if (_speech != null && _speech.Playing)
        {
            _speech.Seek((float)Elapsed);
        }
        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw();
        }
        UpdateSubtitles();
    }

    /// <summary>Stops playback audio, marks cutscene finished, and raises Finished exactly once.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;

        if (_speech != null && _speech.Playing) _speech.Stop();
        if (_ambience != null && _ambience.Playing) _ambience.Stop();

        EmitSignal(SignalName.Finished);
    }

    /// <summary>Ensures audio streams are silenced if overlay is freed directly from tree.</summary>
    public override void _ExitTree()
    {
        if (_speech != null && _speech.Playing) _speech.Stop();
        if (_ambience != null && _ambience.Playing) _ambience.Stop();
        base._ExitTree();
    }

    /// <summary>Updates subtitle text and border accent color based on current elapsed seconds.</summary>
    private void UpdateSubtitles()
    {
        if (_subtitle == null || _panelStyle == null) return;

        int cueIndex = GetCurrentCueIndex();
        if (cueIndex >= 0 && cueIndex < Lines.Length)
        {
            string line = Lines[cueIndex];
            _subtitle.Text = line;
            _panel?.SetVisible(!string.IsNullOrWhiteSpace(line));

            // Apply speaker color border styling
            if (line.StartsWith("AXIOM-7")) _panelStyle.BorderColor = ColorAxiom;
            else if (line.StartsWith("MIRAGE")) _panelStyle.BorderColor = ColorMirage;
            else if (line.StartsWith("TITAN")) _panelStyle.BorderColor = ColorTitan;
            else if (line.StartsWith("OCEANUS")) _panelStyle.BorderColor = ColorOceanus;
            else if (line.StartsWith("SURYA")) _panelStyle.BorderColor = ColorSurya;
            else if (line.StartsWith("AURA")) _panelStyle.BorderColor = ColorAura;
            else if (line.StartsWith("KAITO")) _panelStyle.BorderColor = ColorKaito;
            else _panelStyle.BorderColor = ColorAxiom;
        }
    }

    /// <summary>Finds active cue index from authored start milestones.</summary>
    private int GetCurrentCueIndex()
    {
        for (int i = 0; i < Starts.Length - 1; i++)
        {
            if (Elapsed >= Starts[i] && Elapsed < Starts[i + 1])
                return i;
        }
        return -1;
    }
}
