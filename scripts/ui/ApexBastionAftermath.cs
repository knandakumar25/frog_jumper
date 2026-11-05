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
    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Bastion Praetor core neutralized. The high fortress battlements are cleared. Flak artillery arrays across the stratospheric summit have gone completely silent.",
        "TITAN: That was their supreme guardian? Heavy armor, but its servos couldn't take my kinetic hammer. Now look at those blast doors... thick enough to withstand orbital bombardment.",
        "OCEANUS: Look at the atmospheric sensors. Barometric pressure is plummeting, but the temperature is surging. The entire summit is wrapped in a localized void hurricane.",
        "SURYA: It is not weather. The Void Commander is siphoning raw energy from the upper atmosphere straight into the Throne Room. My bulwark shields are picking up intense electromagnetic distortion.",
        "AURA: The dissonance... it's deafening now. An overwhelming psychic frequency echoing from behind those doors. He knows we broke through his army, and he is waiting.",
        "MIRAGE: I've sliced into the gate relays. The Praetor's biometric master key was the only lock left. Disengaging hydraulic security bolts... three... two... one.",
        "AXIOM-7: Warning: Blast doors unsealing. Sensor telemetry beyond this threshold confirms the Void Commander's throne chamber. Extreme lethal power readings detected.",
        "SURYA: Twelve sectors. We fought from the flooded ruins of Old Tokyo, through the underground bio-labs, up the Spire hull, and across these clouds. This is the end of the line.",
        "KAITO: We set out to free this city, and we finish it together. Axiom Force, draw steel. Behind these doors is Judgment Day!",
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
                Bus = "Dialogue"
            };
            AddChild(_speech);
            _speech.Play();
        }

        Seek(0);
    }

    /// <summary>Supports timeline seeking and precise positioning for tests.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        if (_stage != null) _stage.Seconds = (float)Elapsed;
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

        if (_speech != null && _speech.Playing)
        {
            double pos = _speech.GetPlaybackPosition();
            if (Math.Abs(pos - _lastAudio) < 0.0001)
            {
                _stalled += delta;
                if (_stalled > 0.5) Elapsed += delta;
            }
            else
            {
                _stalled = 0;
                _lastAudio = pos;
                Elapsed = pos;
            }
        }
        else
        {
            Elapsed += delta;
        }

        if (_stage != null) _stage.Seconds = (float)Elapsed;
        UpdateSubtitles();

        if (Elapsed >= Duration)
        {
            Finish();
        }
    }

    /// <summary>Multi-input skip handling (ESC, Space, Enter, Gamepad Start/A, mouse/touch click).</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (HasFinished) return;

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode is Key.Escape or Key.Space or Key.Enter or Key.KpEnter)
            {
                GetViewport()?.SetInputAsHandled();
                Finish();
            }
        }
        else if (@event is InputEventJoypadButton joy && joy.Pressed)
        {
            if (joy.ButtonIndex is JoyButton.Start or JoyButton.A)
            {
                GetViewport()?.SetInputAsHandled();
                Finish();
            }
        }
        else if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            GetViewport()?.SetInputAsHandled();
            Finish();
        }
    }

    /// <summary>Completes playback, silences audio, and raises the Finished signal.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;

        if (_speech != null && _speech.Playing) _speech.Stop();
        if (_ambience != null && _ambience.Playing) _ambience.Stop();

        EmitSignal(SignalName.Finished);
    }

    /// <summary>Safely ensures all owned audio is stopped on tree removal.</summary>
    public override void _ExitTree()
    {
        if (_speech != null && _speech.Playing) _speech.Stop();
        if (_ambience != null && _ambience.Playing) _ambience.Stop();
        base._ExitTree();
    }
}
