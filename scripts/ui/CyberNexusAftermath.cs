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
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Overseer neutralized. All three processing nodes are down. The Citadel automated defense grid has flatlined.",
        "MIRAGE: The reinforcement queues just emptied. No fresh drones, no replacement squads. Their machine army has nowhere to go.",
        "TITAN: Containment prisms. Fancy name for a cage. Now look at them. Scrap metal, and coolant leaking through every seam.",
        "OCEANUS: Thermal runaway is accelerating. Feel that vibration? We cannot stay on this deck. Keep moving toward the lift.",
        "SURYA: The Bastion has switched to auxiliary power. We broke its link to the Nexus, but its local guns still work.",
        "AURA: Heavy footsteps above us. Praetorian mechs and cyber-samurai. They know the network is dead. They are making their stand.",
        "MIRAGE: Ascent lift online. Executive lockout bypassed. Everyone aboard. This takes us straight into the Bastion.",
        "AXIOM-7: Extreme energy readings at the summit. The Commander has detected the Nexus collapse. Bastion batteries are cleared to fire.",
        "KAITO: Then we meet them face to face. No more hiding behind machines. Board the lift. Next stop, Apex Bastion.",
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
        _ownership = new CinematicOwnership(this);
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
        _ambience = new AudioStreamPlayer { Name = "Atmosphere", Bus = "SFX" };
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
        _speech = new AudioStreamPlayer { Name = "Speech", Bus = "SFX" };
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
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Advances deterministic playback clock and triggers stage redraw.</summary>
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

    /// <summary>Handles ESC, Enter, Space, and gamepad skip inputs.</summary>
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

    /// <summary>Positions timeline clock deterministically for testing or seek scrubbing.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        // Natural playback calls Seek every frame; sampling visuals must never restart the speech decoder.
        if (_stage != null)
        {
            _stage.Seconds = (float)Elapsed;
            _stage.QueueRedraw();
        }
        UpdateSubtitles();
    }

    /// <summary>Explicit playback scrubbing is separate from ordinary per-frame visual timeline sampling.</summary>
    public void SeekPlayback(double seconds)
    {
        Seek(seconds);
        _lastAudio = -1; _stalled = 0;
        if (!IsInsideTree() || HasFinished) return;
        if (_speech?.Stream != null) _speech.Play((float)Elapsed);
        if (_ambience?.Stream != null) _ambience.Play((float)Elapsed);
    }

    /// <summary>Stops playback audio, marks cutscene finished, and raises Finished exactly once.</summary>
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

    /// <summary>Ensures audio streams are silenced if overlay is freed directly from tree.</summary>
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

    /// <summary>Updates subtitle text and border accent color based on current elapsed seconds.</summary>
    private void UpdateSubtitles()
    {
        if (_subtitle == null || _panelStyle == null) return;

        int cueIndex = GetCurrentCueIndex();
        if (cueIndex >= 0 && cueIndex < Lines.Length)
        {
            string line = Lines[cueIndex];
            _subtitle.Text = WildfireText.Get($"SUBTITLE_CINEMATIC_CYBER_NEXUS_AFTERMATH_{cueIndex + 1:D2}", Lines[cueIndex]);
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
