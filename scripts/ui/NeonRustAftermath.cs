using Godot;

/// <summary>
/// Skippable ninety-second Level 3 extraction aftermath. One clock owns shots, subtitles and audio;
/// absent speech retains timed subtitles. Owned by the frozen mission, results retain the immutable extraction snapshot.
/// </summary>
public partial class NeonRustAftermath : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio has stopped, for natural, skip and resource fallback exits.</summary>
    [Signal] public delegate void FinishedEventHandler();
    /// <summary>Optional mixed speech track; empty exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/neon_rust_aftermath/dialogue.ogg";
    /// <summary>Visual resource root; missing essential artwork bypasses the cinematic safely.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/neon_rust_aftermath/";
    /// <summary>Timeline length, independent of speech availability.</summary>
    public const double Duration = 90;
    /// <summary>Current deterministic timeline position, also used by runtime review.</summary>
    public double Elapsed { get; private set; }
    /// <summary>True once completion has been consumed; repeated input cannot show results twice.</summary>
    public bool HasFinished { get; private set; }
    private static readonly double[] Starts = { 0, 9, 18, 27, 36, 45, 54, 64, 74, 82, 88 };
    private static readonly string[] Lines = {
        "AXIOM-7: Three shipments secured. Both uplinks are down. Regional patrols have lost their shared targeting feed.",
        "AURA: Listen. No coordinates. No orders. Just a little room between the noise. I can work with that.",
        "MIRAGE: Enjoy it while it lasts. Their eyes are down, not their weapons. I’ve marked a way out.",
        "OCEANUS: Send the recovered power back toward the neighborhoods. They’ve been keeping people cold to keep these fences burning.",
        "TITAN: Put those crates on the service line. We hauled enough weight for their machines. Let it carry something useful.",
        "KAITO: They’ll come looking for this depot. Make sure the supplies leave before we do.",
        "AURA: A short signal. No names, no coordinates. Just this: the checkpoint is open, and you are not alone.",
        "MIRAGE: The route reaches the Border Fortress. I can get us to its approaches. Getting through that wall is another matter.",
        "AXIOM-7: The fortress runs on its own power. Its guns will still be waiting.",
        "KAITO: Then we go together. This city has waited long enough.",
        "" // Silent final title.
    };
    private NeonRustAftermathStage? _stage;
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
        // Global owns the persistent prompt; preserve its actual state for teardown as well as gameplay.
        var global = GetNodeOrNull<Node>("/root/Global");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);
        _stage = new NeonRustAftermathStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
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
        var skip = new Button { Name = "Skip", Text = "CITY_VOICE_SKIP",
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
        _subtitle.Text = Localized($"CITY_VOICE_CUE_{cue + 1}", Lines[cue]);
        _subtitle.GetParent<Control>().Visible = cue < 10;
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
        // mapped to this skip event so Space/A cannot remain held through gameplay handoff.
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

    /// <summary>Defers gameplay until the consumed skip event has left GUI dispatch.</summary>
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

    /// <summary>English remains readable when the selected language has no arrival translation yet.</summary>
    private string Localized(string key, string fallback)
    {
        string translated = Tr(key);
        return translated == key || string.IsNullOrWhiteSpace(translated) ? fallback : translated;
    }
}
