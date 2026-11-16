using Godot;

/// <summary>
/// Skippable seventy-five-second Level 2 extraction epilogue. One clock owns shots, subtitles and audio;
/// absent speech never delays results. Owned by the frozen mission, every fresh launch has the same handoff.
/// </summary>
public partial class WildfireAftermath : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio has stopped, for natural, skip and resource fallback exits.</summary>
    [Signal] public delegate void FinishedEventHandler();
    /// <summary>Optional mixed speech track; empty exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/wildfire_aftermath/dialogue.ogg";
    /// <summary>Visual resource root; missing essential artwork bypasses the cinematic safely.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/wildfire_aftermath/";
    /// <summary>Timeline length, independent of speech availability.</summary>
    public const double Duration = 75;
    /// <summary>Current deterministic timeline position, also used by runtime review.</summary>
    public double Elapsed { get; private set; }
    /// <summary>True once completion has been consumed; repeated input cannot show results twice.</summary>
    public bool HasFinished { get; private set; }
    private static readonly double[] Starts = { 0, 8, 17, 26, 34, 43, 54, 64, 72 };
    private static readonly string[] Lines = {
        "AXIOM-7: Battery silent. All three shelter groups are clear. The evacuation corridor is secure.",
        "OCEANUS: They drew a line through these families. Look at them now. No scanners. No walls. Just each other.",
        "TITAN: Easy, little ones. There’s room for everyone. Take your time. Nobody’s closing this road behind you.",
        "KAITO: We opened one checkpoint. How many more are still waiting for us?",
        "OCEANUS: Then we make this more than an escape. Keep the corridor open. Let them choose where they go.",
        "AXIOM-7: Recovered shipment manifest decoded. The siege battery drew power through a regional transit depot. Its communication towers coordinate the patrol network.",
        "TITAN: They used those rails to bring the cages here. Let’s make sure the next train carries something better.",
        "KAITO: Oceanus, Titan—you’re with me. We follow the rails. Then we take their eyes off this city.",
        "NEXT: NEON RUST AND DATA DUST"
    };
    private WildfireAftermathStage? _stage;
    private Label? _subtitle;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;
    private Button? _skip;
    private Input.MouseModeEnum _previousMouseMode;
    private bool _previousPromptVisible = true;
    private double _lastAudio = -1;
    private double _stalled;

    /// <summary>Builds independent responsive presentation and starts optional audio only inside the tree.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        // Global owns the persistent prompt; preserve its actual state for teardown as well as results.
        var global = GetNodeOrNull<Node>("/root/Global");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);
        _stage = new WildfireAftermathStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(ui);
        ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { Name = "SubtitlePanel", AnchorLeft = .08f, AnchorRight = .92f,
            AnchorTop = .77f, AnchorBottom = .96f };
        ui.AddChild(panel);
        var style = new StyleBoxFlat { BgColor = new Color(.008f, .02f, .04f, .95f),
            BorderColor = new Color("66d9e8"), BorderWidthTop = 2,
            ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 14, ContentMarginBottom = 14 };
        panel.AddThemeStyleboxOverride("panel", style);
        _subtitle = new Label { Name = "Subtitle", AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _subtitle.AddThemeFontSizeOverride("font_size", 22);
        panel.AddChild(_subtitle);
        var skip = new Button { Name = "Skip", Text = "BREACH_SKIP",
            AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -270, OffsetRight = -28, OffsetTop = 24, OffsetBottom = 72 };
        ui.AddChild(skip);
        _skip = skip;
        InputPromptBinding.Cinematic(_skip, false);
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
        if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path)) player.Stream = GD.Load<AudioStream>(path);
        if (player.Stream != null && IsInsideTree()) player.Play();
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
            if (_stalled < .5)
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
        for (int i = 1; i < Starts.Length; i++) if (Elapsed >= Starts[i]) cue = i;
        // Scale down within the safe subtitle panel on small windows and wide aspect ratios.
        _subtitle.AddThemeFontSizeOverride("font_size", (int)System.Math.Clamp(GetViewport().GetVisibleRect().Size.Y / 45, 14, 22));
        _subtitle.Text = Localized($"BREACH_CUE_{cue + 1}", Lines[cue]);
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
        InputPromptService.Instance?.ObserveInput(input);
        if (HasFinished || !IsInsideTree()) return;
        bool skip = input is InputEventKey { Pressed: true, Echo: false } key &&
            key.Keycode is Key.Escape or Key.Space or Key.Enter;
        skip |= input is InputEventJoypadButton { Pressed: true } joy && joy.ButtonIndex is JoyButton.Start or JoyButton.A;
        skip |= input is InputEventScreenTouch { Pressed: true } touch && _skip != null && _skip.GetGlobalRect().HasPoint(touch.Position);
        skip |= input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse &&
            _skip != null && _skip.GetGlobalRect().HasPoint(mouse.Position);
        if (!skip) return;
        GetViewport().SetInputAsHandled();
        // Handling the event does not clear Input's action state. Release only actions
        // mapped to this skip event so Space/A cannot remain held through results navigation.
        foreach (StringName action in InputMap.GetActions())
            if (InputMap.EventIsAction(input, action)) Input.ActionRelease(action);
        Finish();
    }

    /// <summary>Stops audio before emitting the single results callback and removes all owned UI/effects.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        StopAudio();
        if (IsInsideTree())
        {
            Input.MouseMode = _previousMouseMode;
            GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
        }
        if (IsInsideTree()) Callable.From(DeliverResults).CallDeferred();
        else EmitSignal(SignalName.Finished);
    }

    /// <summary>Defers results until the consumed skip event has left GUI dispatch.</summary>
    private void DeliverResults()
    {
        if (!IsInsideTree()) return;
        EmitSignal(SignalName.Finished);
        QueueFree();
    }

    /// <summary>Scene replacement also stops sound; no callbacks survive scene teardown.</summary>
    public override void _ExitTree()
    {
        StopAudio();
        if (!HasFinished)
        {
            Input.MouseMode = _previousMouseMode;
            GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
        }
    }

    /// <summary>Centralized audio cleanup is safe for missing streams and repeated calls.</summary>
    private void StopAudio()
    {
        _speech?.Stop();
        _ambience?.Stop();
    }

    /// <summary>English remains readable when the selected language has no aftermath translation yet.</summary>
    private string Localized(string key, string fallback)
    {
        string translated = Tr(key);
        return translated == key || string.IsNullOrWhiteSpace(translated) ? fallback : translated;
    }
}
