using Godot;

/// <summary>
/// Skippable 120-second Pre-Level 6 freight-lift descent cinematic overlay ("Into the Cyber-Grave").
/// One monotonic clock coordinates shots, subtitles, and audio.
/// Features full squad dialogue, ElevenLabs voiced performance, procedural staging, and seamless Mission 6 handoff.
/// </summary>
public partial class CyberGraveIntro : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio has stopped, for natural, skip and resource fallback exits.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed speech track; empty exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/cyber_grave_intro/dialogue.ogg";

    /// <summary>Visual resource root; missing essential artwork bypasses the cinematic safely.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_grave_intro/";

    /// <summary>When true, automatically navigates to Mission 6 upon completion or skip.</summary>
    [Export] public bool EnterMissionOnFinish { get; set; } = true;

    /// <summary>Timeline length in seconds, independent of speech availability.</summary>
    public const double Duration = 120.0;

    /// <summary>Current deterministic timeline position, also used by runtime review.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed; repeated input cannot show gameplay twice.</summary>
    public bool HasFinished { get; private set; }

    private static readonly double[] Starts = { 0, 1.0, 14.5, 27.5, 40.0, 53.5, 68.5, 82.0, 93.5, 102.0, 111.0 };

    private static readonly string[] Lines = {
        "", // Establishing ambient shot
        "AXIOM-7: Descent telemetry confirmed. We are four hundred meters beneath Downtown Core. The registry logs terminate at a subterranean facility designated Cyber-Grave.",
        "KAITO: A grave? No. They do not bury the dead here. They process the living into machine components. Look at the conduits along the shaft.",
        "AURA: The resonance down here is sickening... it’s not mechanical hum. It’s thousands of bio-signals trapped in forced synchronization. A dissonance of stolen lives.",
        "OCEANUS: They told our people the relocated citizens were sent to agricultural preserves. Cages. They put them in pressurized stasis pods. Stagnant water... drowning without an ocean.",
        "TITAN: The foundry uses geothermal siphon reactors. The pressure in those pipes could blow half the district above if the coolant fails. Heavy plating, blast doors... they knew someone would come to tear it down.",
        "MIRAGE: Secondary security grid is active. High-energy plasma conduits and chemical vapor sluices. I've marked the access shafts. We drop through the ventilation before the blast doors seal.",
        "SURYA: I'm locking our transponder signals to the orbital blind spot. Once you breach the inner vault, you will have no topside extraction. Every operative must come back together.",
        "OCEANUS: Every pod will open. Not one name left behind in this dark. Shogun, give the order.",
        "KAITO: We breach the perimeter, sever the conduits, and evacuate the captives. Axiom Force—engage!",
        "" // Final descent lock and arrival title card
    };

    private static readonly Color ColorKaito = new("f87171");    // Crimson
    private static readonly Color ColorMirage = new("34d399");   // Emerald
    private static readonly Color ColorAura = new("f472b6");     // Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Gold
    private static readonly Color ColorAxiom = new("00f0ff");     // Cyan

    private CyberGraveStage? _stage;
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

    /// <summary>Builds independent responsive presentation and starts optional audio only inside the tree.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Global owns persistent control prompt; preserve its actual state
        var global = GetNodeOrNull<Node>("/root/Global");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);

        // Procedural visual stage
        _stage = new CyberGraveStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
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
            BgColor = new Color(0.008f, 0.02f, 0.04f, 0.95f),
            BorderColor = new Color("00f0ff"),
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
            Text = Localized("CYBER_GRAVE_SKIP", "SKIP // ESC / START"),
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -270,
            OffsetRight = -28,
            OffsetTop = 24,
            OffsetBottom = 72
        };
        ui.AddChild(skip);
        _skip = skip;
        skip.Pressed += Finish;
        skip.GrabFocus();

        _speech = CreateAudio("Speech", SpeechPath, 0);
        _ambience = CreateAudio("Atmosphere", ArtPath + "atmosphere.ogg", -18);
        Seek(0);

        if (!_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Loads optional audio without emitting missing-resource errors; all players belong to this overlay.</summary>
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

    /// <summary>Advances monotonically, compensating mixer latency and recovering from missing or stalled speech.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;
        double clock = Elapsed + delta;
        if (_speech is { Playing: true })
        {
            double position = _speech.GetPlaybackPosition();
            _stalled = position <= _lastAudio ? _stalled + delta : 0;
            _lastAudio = position;
            if (_stalled < 0.5)
                clock = System.Math.Max(Elapsed, position + AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency());
        }
        Seek(clock);
        if (Elapsed >= Duration) Finish();
    }

    /// <summary>Samples every shot and subtitle without firing completion; QA can seek in either direction.</summary>
    public void Seek(double seconds)
    {
        Elapsed = System.Math.Clamp(seconds, 0, Duration);
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
            1 => ColorAxiom,
            2 or 9 => ColorKaito,
            3 => ColorAura,
            4 or 8 => ColorOceanus,
            5 => ColorTitan,
            6 => ColorMirage,
            7 => ColorSurya,
            _ => ColorAxiom
        };
        if (_panelStyle != null) _panelStyle.BorderColor = speakerColor;

        _subtitle.AddThemeFontSizeOverride("font_size", (int)System.Math.Clamp(GetViewport().GetVisibleRect().Size.Y / 45, 14, 22));
        _subtitle.Text = Localized($"CYBER_GRAVE_CUE_{cue}", Lines[cue]);
        if (_panel != null) _panel.Visible = cue > 0 && cue < 10;
    }

    /// <summary>Resumes sound at a review position without introducing another clock.</summary>
    public void SeekAudio(double seconds)
    {
        Seek(seconds);
        _lastAudio = -1;
        _stalled = 0;
        if (!IsInsideTree() || HasFinished) return;
        if (_speech?.Stream != null) _speech.Play((float)Elapsed);
        if (_ambience?.Stream != null) _ambience.Play((float)Elapsed);
    }

    /// <summary>Keyboard and controller shortcuts share the focused button's pointer/touch completion route.</summary>
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
        GetViewport().SetInputAsHandled();
        foreach (StringName action in InputMap.GetActions())
        {
            if (InputMap.EventIsAction(input, action)) Input.ActionRelease(action);
        }
        Finish();
    }

    /// <summary>Stops audio before emitting the single completion callback and cleans up.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        _speech?.Stop();
        _ambience?.Stop();
        EmitSignal(SignalName.Finished);

        if (EnterMissionOnFinish && IsInsideTree())
        {
            CampaignProgress.PrepareMission(6);
            if (LevelCatalog.Count >= 6)
            {
                GetTree().ChangeSceneToFile(LevelCatalog.ScenePath(6));
            }
            else
            {
                // Fallback to level selection if scene 6 not yet authored
                GetTree().ChangeSceneToFile("res://scenes/main_menu/level_selection.tscn");
            }
        }
    }

    /// <summary>Restores cursor and persistent prompts when leaving the tree.</summary>
    public override void _ExitTree()
    {
        Input.MouseMode = _previousMouseMode;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
    }

    /// <summary>Retrieves localized text with safe fallback.</summary>
    private static string Localized(string key, string fallback)
    {
        string localized = TranslationServer.Translate(key);
        return string.IsNullOrEmpty(localized) || localized == key ? fallback : localized;
    }
}
