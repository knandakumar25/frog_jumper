using System;
using Godot;

/// <summary>
/// Skippable 90-second post-mission cinematic epilogue overlay ("The Gates of Judgment // Mission 11 Aftermath").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting the aftermath of the Bastion Praetor's defeat and the unsealing
/// of the Grand Citadel Throne Room blast doors before Mission 12 (Judgment Day).
/// </summary>
public partial class ApexBastionAftermath : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/apex_bastion_aftermath/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/apex_bastion_aftermath/";

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/apex_bastion_aftermath/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Praetor neutralized. Battlements clear. The summit flak arrays have fallen silent.",
        "TITAN: Heavy armor, weak servos. Now those blast doors look less welcoming. Think they heard what happened to their guardian?",
        "OCEANUS: Pressure falling, temperature rising. That storm around the summit is getting tighter. This is not normal weather.",
        "SURYA: It is a power siphon. He is drawing energy straight into the throne room. My shields can feel the distortion.",
        "AURA: The dissonance behind those doors is deafening. He knows we are here. He has stopped hiding.",
        "MIRAGE: Gate relays unlocked. The Praetor key was the last barrier. Releasing the hydraulic bolts. Three. Two. One.",
        "AXIOM-7: Blast doors opening. Throne chamber confirmed. Lethal energy readings beyond the threshold. Proceed together.",
        "SURYA: From the flooded ruins to the clouds. All this way, all these people depending on us. We finish it here.",
        "KAITO: We set out to free this city. We finish together. Axiom Force, draw steel. Behind these doors is Judgment Day.",
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

    private ApexBastionAftermathStage? _stage;
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

        _stage = GetNodeOrNull<ApexBastionAftermathStage>("ApexBastionAftermathStage");
        if (_stage == null)
        {
            _stage = new ApexBastionAftermathStage
            {
                Name = "ApexBastionAftermathStage",
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
            BgColor = new Color(0.04f, 0.06f, 0.12f, 0.88f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderColor = ColorAxiom,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
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
        _panel.AddChild(_subtitle);
        _ui.AddChild(_panel);

        // Skip button positioned in top-right
        _skip = new Button
        {
            Name = "SkipButton",
            Text = "SKIP [ESC / SPACE]",
            CustomMinimumSize = new Vector2(160, 42)
        };
        _skip.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _skip.OffsetLeft = -190;
        _skip.OffsetTop = 30;
        _skip.OffsetRight = -30;
        _skip.OffsetBottom = 72;
        _skip.Pressed += Finish;
        _ui.AddChild(_skip);

        // Atmosphere stream
        string ambientPath = ArtPath.TrimEnd('/') + "/atmosphere.ogg";
        if (ResourceLoader.Exists(ambientPath))
        {
            _ambience = new AudioStreamPlayer
            {
                Name = "Atmosphere",
                Stream = GD.Load<AudioStream>(ambientPath),
                Bus = "Music",
                VolumeDb = -2f
            };
            AddChild(_ambience);
            _ambience.Play();
        }

        // Voice stream
        if (!string.IsNullOrEmpty(SpeechPath) && ResourceLoader.Exists(SpeechPath))
        {
            _speech = new AudioStreamPlayer
            {
                Name = "Speech",
                Stream = GD.Load<AudioStream>(SpeechPath),
                Bus = "SFX"
            };
            AddChild(_speech);
            _speech.Play();
        }

        Seek(0);
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Supports timeline seeking and precise positioning for tests.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw(); // Clock changes must redraw the opening gates, speaker framing and particles.
        }
        UpdateSubtitles();
    }

    /// <summary>Refreshes subtitle line and speaker accent colors based on timeline position.</summary>
    private void UpdateSubtitles()
    {
        int cue = GetCurrentCue();
        if (_subtitle != null)
        {
            _subtitle.Text = Lines[cue];
        }

        if (_panelStyle != null)
        {
            _panelStyle.BorderColor = cue switch
            {
                0 => ColorAxiom,
                1 => ColorTitan,
                2 => ColorOceanus,
                3 => ColorSurya,
                4 => ColorAura,
                5 => ColorMirage,
                6 => ColorAxiom,
                7 => ColorSurya,
                8 => ColorKaito,
                _ => ColorAxiom
            };
        }
    }

    private int GetCurrentCue()
    {
        for (int i = Starts.Length - 2; i >= 0; i--)
        {
            if (Elapsed >= Starts[i]) return i;
        }
        return 0;
    }

    /// <summary>Advances playback clock and monitors audio progress.</summary>
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

    /// <summary>Multi-input skip handling (ESC, Space, Enter, Gamepad Start/A, mouse/touch click).</summary>
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

    /// <summary>Completes playback, silences audio, and raises the Finished signal.</summary>
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
        QueueFree();
    }

    /// <summary>Stops only audio owned by this presentation on natural completion, Skip and independent removal.</summary>
    private void StopOwnedAudio()
    {
        if (GodotObject.IsInstanceValid(_speech)) _speech!.Stop();
        if (GodotObject.IsInstanceValid(_ambience)) _ambience!.Stop();
    }

    /// <summary>Safely ensures all owned audio is stopped on tree removal.</summary>
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
