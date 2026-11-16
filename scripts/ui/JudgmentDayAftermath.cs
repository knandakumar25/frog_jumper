using System;
using Godot;

/// <summary>
/// Skippable 90-second post-mission cinematic epilogue overlay ("Dawn of Axiom // Mission 12 Aftermath").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting the aftermath of the Void Commander's defeat, the collapse of
/// the Singularity Core into a dawn aurora, the restoration of Neo-Los Angeles's power grid, and the liberation of the world.
/// </summary>
public partial class JudgmentDayAftermath : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/judgment_day_aftermath/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/judgment_day_aftermath/";

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/judgment_day_aftermath/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Singularity contained. Reactor signature zero. The Commander's control network is down across every relay.",
        "MIRAGE: The firewalls are down. Broadcast is open to everyone. Look below us. The quarantine barriers are dropping.",
        "TITAN: Ten million people kept in the dark. Now the lights are coming on, from the sumps to the cloudline.",
        "OCEANUS: The scrubbers are reversing. That is ocean air coming through the broken windows. The storm is finally passing.",
        "SURYA: Actual sunlight. After fifty years, my cells are drinking it in. I had forgotten it could feel this warm.",
        "AURA: I hear voices. Singing, laughing, crying. No suppression signal. No one telling them to be quiet. Just people.",
        "AXIOM-7: Imperial control erased. Identities restored to their owners in all twelve districts. Mission complete.",
        "KAITO: Castaways. Rogues. Ghosts in the gridlock. Today we stood together, and gave them their tomorrow back.",
        "KAITO: The dawn belongs to all of us. Axiom Force, stand down. Our city is free.",
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

    private JudgmentDayAftermathStage? _stage;
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

        _stage = GetNodeOrNull<JudgmentDayAftermathStage>("JudgmentDayAftermathStage");
        if (_stage == null)
        {
            _stage = new JudgmentDayAftermathStage
            {
                Name = "JudgmentDayAftermathStage",
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
            BgColor = new Color(0.04f, 0.02f, 0.08f, 0.90f),
            BorderColor = ColorAxiom,
            BorderWidthLeft = 3,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
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
        _subtitle.AddThemeFontSizeOverride("font_size", 19);
        _subtitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.96f, 0.98f));
        _panel.AddChild(_subtitle);
        _ui.AddChild(_panel);

        // Skip Button top right
        _skip = new Button
        {
            Name = "SkipButton",
            Text = "SKIP [ESC / START]",
            CustomMinimumSize = new Vector2(160, 40)
        };
        _skip.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _skip.OffsetLeft = -190;
        _skip.OffsetRight = -30;
        _skip.OffsetTop = 26;
        _skip.OffsetBottom = 66;

        var skipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.04f, 0.14f, 0.8f),
            BorderColor = new Color(0.95f, 0.65f, 0.2f, 0.7f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _skip.AddThemeStyleboxOverride("normal", skipStyle);
        _skip.AddThemeFontSizeOverride("font_size", 14);
        _skip.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.3f));
        _skip.Pressed += Finish;
        InputPromptBinding.Cinematic(_skip, true);
        _ui.AddChild(_skip);

        // Atmosphere audio
        _ambience = new AudioStreamPlayer
        {
            Name = "Atmosphere",
            Bus = "Music",
            VolumeDb = -6f
        };
        AddChild(_ambience);

        string atmoPath = ArtPath.TrimEnd('/') + "/atmosphere.ogg";
        if (ResourceLoader.Exists(atmoPath))
        {
            _ambience.Stream = GD.Load<AudioStream>(atmoPath);
            _ambience.Play();
        }

        // Voice track (optional)
        _speech = new AudioStreamPlayer
        {
            Name = "Speech",
            Bus = "SFX"
        };
        AddChild(_speech);

        if (!string.IsNullOrEmpty(SpeechPath) && ResourceLoader.Exists(SpeechPath))
        {
            _speech.Stream = GD.Load<AudioStream>(SpeechPath);
            _speech.Play();
        }

        UpdateCue(0.0);
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
            _stage.QueueRedraw(); // Redraw the sunrise and city liberation throughout the existing 90-second schedule.
        }
        UpdateCue(Elapsed);
    }

    /// <summary>Advances the deterministic timeline, checks audio stalls, and updates subtitles.</summary>
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

    private void UpdateCue(double elapsed)
    {
        int index = GetCueIndex(elapsed);
        if (_subtitle != null && index >= 0 && index < Lines.Length)
        {
            _subtitle.Text = WildfireText.Get($"SUBTITLE_CINEMATIC_JUDGMENT_DAY_AFTERMATH_{index + 1:D2}", Lines[index]);
            UpdateSpeakerStyle(Lines[index]);
        }
    }

    private int GetCueIndex(double elapsed)
    {
        for (int i = 0; i < Starts.Length - 1; i++)
        {
            if (elapsed >= Starts[i] && elapsed < Starts[i + 1])
                return i;
        }
        return Starts.Length - 2;
    }

    private void UpdateSpeakerStyle(string line)
    {
        if (_panelStyle == null) return;

        if (line.StartsWith("KAITO:", StringComparison.OrdinalIgnoreCase))
            _panelStyle.BorderColor = ColorKaito;
        else if (line.StartsWith("MIRAGE:", StringComparison.OrdinalIgnoreCase))
            _panelStyle.BorderColor = ColorMirage;
        else if (line.StartsWith("AURA:", StringComparison.OrdinalIgnoreCase))
            _panelStyle.BorderColor = ColorAura;
        else if (line.StartsWith("OCEANUS:", StringComparison.OrdinalIgnoreCase))
            _panelStyle.BorderColor = ColorOceanus;
        else if (line.StartsWith("TITAN:", StringComparison.OrdinalIgnoreCase))
            _panelStyle.BorderColor = ColorTitan;
        else if (line.StartsWith("SURYA:", StringComparison.OrdinalIgnoreCase))
            _panelStyle.BorderColor = ColorSurya;
        else
            _panelStyle.BorderColor = ColorAxiom;
    }

    /// <summary>Handles skip shortcuts on key and gamepad input.</summary>
    public override void _Input(InputEvent input)
    {
        InputPromptService.Instance?.ObserveInput(input);
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

    /// <summary>Completes the cutscene once, silences audio, and raises Finished.</summary>
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
