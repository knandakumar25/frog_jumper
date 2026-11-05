using System;
using Godot;

/// <summary>
/// Skippable 90-second Pre-Level 7 cinematic prelude overlay ("The Molten Heart // The Foundry Forge").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric geothermal audio
/// across nine authored beats establishing the infiltration into Act II's climax facility.
/// </summary>
public partial class FoundryForgeIntro : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio stops, for natural, skip and resource fallback exits.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue track; absent speech exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/foundry_forge_intro/dialogue.ogg";

    /// <summary>Visual and audio resource directory root.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/foundry_forge_intro/";

    /// <summary>When true, automatically navigates to Mission 7 upon completion or skip.</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/foundry_forge_intro/cues.json)
    private CinematicOwnership? _ownership;
    private bool _completionDelivered;

    private static readonly double[] Starts = { 0, 8.0, 18.0, 28.0, 38.0, 48.0, 58.0, 68.0, 78.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Thermal telemetry online. Four thousand Kelvin at the source. We have reached the Foundry Forge.",
        "KAITO: Those conduits feed the Citadel sky-shields. Shut this reactor down, and their fortress loses its cover.",
        "TITAN: Forced labor built this place. Heavy coils, thick walls. They planned for a meltdown. They did not plan for us.",
        "OCEANUS: Three aquifers drained to cool their turbines. Break the valves, and the river can take its course again.",
        "AURA: That turbine is deafening. But beneath the grind, I can hear a fault in the containment lattice.",
        "MIRAGE: Turrets on the catwalks. Slag below. I have a route over the coolant lines. Watch your footing.",
        "SURYA // COMMS: My bulwark will cover the radiation crossing. Once we breach the core, we move straight to extraction.",
        "TITAN: When that reactor goes, their shield goes with it. Let us see how brave they are without it.",
        "KAITO: Sever the coolant lines. Overload the reactor. Break their sky-shields. Axiom Force, drop in.",
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

    private FoundryForgeStage? _stage;
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

    /// <summary>Constructs procedural stage, subtitle overlay, skip button, and starts audio playback.</summary>
    public override void _Ready()
    {
        _ownership = new CinematicOwnership(this);
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Preserve and hide persistent control prompt from Global
        var global = GetNodeOrNull<Node>("/root/Global");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);

        // Procedural visual stage
        _stage = new FoundryForgeStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // UI overlay with Subtitle panel and Skip button
        var ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(ui);
        ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var panel = new PanelContainer
        {
            Name = "SubtitlePanel",
            AnchorLeft = 0.08f,
            AnchorRight = 0.92f,
            AnchorTop = 0.77f,
            AnchorBottom = 0.96f
        };
        ui.AddChild(panel);
        _panel = panel;

        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.012f, 0.008f, 0.005f, 0.95f),
            BorderColor = ColorAxiom,
            BorderWidthTop = 3,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        panel.AddThemeStyleboxOverride("panel", _panelStyle);

        _subtitle = new Label
        {
            Name = "Subtitle",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _subtitle.AddThemeFontSizeOverride("font_size", 22);
        panel.AddChild(_subtitle);

        var skip = new Button
        {
            Name = "Skip",
            Text = Localized("FOUNDRY_FORGE_SKIP", "SKIP // ESC / START"),
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -270,
            OffsetRight = -28,
            OffsetTop = 24,
            OffsetBottom = 72
        };
        var skipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.01f, 0.008f, 0.88f),
            BorderColor = new Color("f97316"),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        skip.AddThemeStyleboxOverride("normal", skipStyle);
        skip.AddThemeStyleboxOverride("hover", skipStyle);
        skip.AddThemeStyleboxOverride("focus", skipStyle);
        ui.AddChild(skip);
        _skip = skip;
        skip.Pressed += Finish;
        skip.GrabFocus();

        _speech = CreateAudio("Speech", SpeechPath, 0);
        _ambience = CreateAudio("Atmosphere", ArtPath + "atmosphere.ogg", -18);
        Seek(0);

        if (!_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
            if (_speech != null) _speech.Bus = "SFX";
        if (_ambience != null) { _ambience.Bus = "Music"; _ambience.VolumeDb = -6; }
        CinematicOwnership.LayoutSubtitles(_panel);
        if (_stage != null && !_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Creates an AudioStreamPlayer node with safe missing-resource handling.</summary>
    private AudioStreamPlayer CreateAudio(string name, string path, float volume)
    {
        var player = new AudioStreamPlayer { Name = name, Bus = "SFX", VolumeDb = volume };
        AddChild(player);
        if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path))
            player.Stream = GD.Load<AudioStream>(path);
        if (player.Stream != null && IsInsideTree())
            player.Play();
        return player;
    }

    /// <summary>Monotonically advances the timeline, syncing audio playback position and ending at Duration.</summary>
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

    /// <summary>Samples any timeline second without completing, updating stage graphics and subtitle text.</summary>
    public void Seek(double seconds)
    {
        Elapsed = Math.Clamp(seconds, 0, Duration);
        if (_stage == null || _subtitle == null) return;
        _stage.Seconds = (float)Elapsed;
        _stage.QueueRedraw();

        int cue = 0;
        for (int i = 1; i < Starts.Length; i++)
        {
            if (Elapsed >= Starts[i]) cue = i;
        }

        // Color-code the subtitle border based on speaker
        Color speakerColor = cue switch
        {
            0 => ColorAxiom,
            1 or 8 => ColorKaito,
            2 or 7 => ColorTitan,
            3 => ColorOceanus,
            4 => ColorAura,
            5 => ColorMirage,
            6 => ColorSurya,
            _ => ColorAxiom
        };
        if (_panelStyle != null) _panelStyle.BorderColor = speakerColor;

        _subtitle.AddThemeFontSizeOverride("font_size", CinematicOwnership.SubtitleFont(_subtitle!));
        _subtitle.Text = Localized($"FOUNDRY_FORGE_CUE_{cue}", Lines[Math.Min(cue, Lines.Length - 1)]);
        if (_panel != null) _panel.Visible = cue >= 0 && cue < 9 && !string.IsNullOrEmpty(Lines[cue]);
    }

    /// <summary>Handles keyboard, controller, mouse, and touch skip shortcuts.</summary>
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

    /// <summary>Stops audio and emits Finished, automatically loading Mission 7 if enabled.</summary>
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
            CampaignProgress.PrepareMission(7);
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(7));
        }
        else QueueFree();
    }

    /// <summary>Stops only audio owned by this presentation on natural completion, Skip and independent removal.</summary>
    private void StopOwnedAudio()
    {
        if (GodotObject.IsInstanceValid(_speech)) _speech!.Stop();
        if (GodotObject.IsInstanceValid(_ambience)) _ambience!.Stop();
    }

    /// <summary>Restores persistent prompt visibility and mouse cursor mode when leaving scene tree.</summary>
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

    /// <summary>Retrieves localized text with safe fallback.</summary>
    private static string Localized(string key, string fallback)
    {
        string localized = TranslationServer.Translate(key);
        return string.IsNullOrEmpty(localized) || localized == key ? fallback : localized;
    }
}
