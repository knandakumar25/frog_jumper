using Godot;

/// <summary>
/// Standalone 120-second Act II preview. One clock owns shots, subtitles and optional audio.
/// Completion remains local: no campaign, roster, score, session or save API is called.
/// </summary>
public partial class ActIIOpening : CanvasLayer
{
    /// <summary>Emitted once after all cinematic audio has stopped, for natural, skip and resource fallback completion.</summary>
    [Signal] public delegate void FinishedEventHandler();
    /// <summary>Optional mixed speech track; empty exercises the timed subtitle fallback.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/act_ii_opening/dialogue.ogg";
    /// <summary>Visual resource root; missing essential artwork bypasses the cinematic safely.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/act_ii_opening/";
    /// <summary>Timeline length, independent of speech availability.</summary>
    public const double Duration = 120;
    /// <summary>Current deterministic timeline position, also used by runtime review.</summary>
    public double Elapsed { get; private set; }
    /// <summary>True once completion has been consumed; repeated input cannot complete twice.</summary>
    public bool HasFinished { get; private set; }
    private static readonly double[] Starts = { 0, 8, 20, 32, 44, 56, 68, 80, 92, 104, 110, 114 };
    private static readonly string[] Lines = {
        "",
        "SURYA: The crossing is open. I thought that meant they could finally go home. Why is no one moving?",
        "AXIOM-7: Their identities are marked invalid. Transit, food distribution, shelter access—all refused. The district recognizes their bodies, but denies that they belong here.",
        "OCEANUS: They survived the wall. Now they have to prove they exist before someone will give them water. Stay with us. We’ll find another way.",
        "TITAN: No chains. No prison doors. Just a light that says no, and a city trained to look away.",
        "AURA: The scanners aren’t losing their names. Something upstream is removing them. Every rejection carries the same command signature.",
        "MIRAGE: There’s a service passage beneath the processing hall. No public entrance, no queue. That’s usually where they hide the useful doors.",
        "AXIOM-7: District identity registry. It controls the exclusion list and holds the original records. Destroying it could erase the only evidence these people have left.",
        "KAITO: Then we keep the records intact. Get inside, recover their identities, and stop the registry from locking them out again.",
        "SURYA: I’ll keep this queue safe. Bring their names back.",
        "KAITO: All of them. Squad—move.",
        ""
    };
    private ActIIStage? _stage;
    private Label? _subtitle;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;
    private Button? _skip;
    private Input.MouseModeEnum _previousMouseMode;
    private bool _previousPromptVisible = true;
    private Control? _ui;
    private Control? _complete;
    private bool _navigating;
    private double _lastAudio = -1;
    private double _stalled;

    /// <summary>Builds independent responsive presentation and starts optional audio only inside the tree.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        // Global owns the persistent prompt; restore its actual visibility when this scene exits.
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Call("stop_menu_music");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);
        _stage = new ActIIStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(ui);
        _ui = ui;
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
        var skip = new Button { Name = "Skip", Text = Localized("ACT_II_SKIP", "SKIP // ESC / START"),
            AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -270, OffsetRight = -28, OffsetTop = 24, OffsetBottom = 72 };
        ui.AddChild(skip);
        _skip = skip;
        skip.Pressed += Finish;
        skip.GrabFocus();
        _speech = CreateAudio("Speech", SpeechPath, 0);
        _ambience = CreateAudio("Atmosphere", ArtPath + "atmosphere.ogg", -18);
        BuildCompletion();
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
        _subtitle.Text = Localized($"ACT_II_CUE_{cue + 1}", Lines[cue]);
        _subtitle.GetParent<Control>().Visible = cue > 0 && cue < 11;
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
        if (!IsInsideTree() || _navigating) return;
        if (HasFinished)
        {
            // Global suppresses legacy mouse input, so route completion clicks before GUI dispatch.
            Vector2? point = input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click ? click.Position :
                input is InputEventScreenTouch { Pressed: true } tap ? tap.Position : null;
            if (point.HasValue && _complete is { Visible: true })
                foreach (var child in _complete.GetChildren())
                    if (child is Button button && button.GetGlobalRect().HasPoint(point.Value))
                    {
                        GetViewport().SetInputAsHandled();
                        button.EmitSignal(Button.SignalName.Pressed);
                        return;
                    }
            return;
        }
        bool skip = input is InputEventKey { Pressed: true, Echo: false } key &&
            key.Keycode is Key.Escape or Key.Space or Key.Enter;
        skip |= input is InputEventJoypadButton { Pressed: true } joy && joy.ButtonIndex is JoyButton.Start or JoyButton.A;
        skip |= input is InputEventScreenTouch { Pressed: true } touch && _skip != null && _skip.GetGlobalRect().HasPoint(touch.Position);
        skip |= input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse &&
            _skip != null && _skip.GetGlobalRect().HasPoint(mouse.Position);
        if (!skip) return;
        GetViewport().SetInputAsHandled();
        // Handling the event does not clear Input's action state. Release only actions
        // mapped to this Skip event so Space/A cannot remain held through completion focus.
        foreach (StringName action in InputMap.GetActions())
            if (InputMap.EventIsAction(input, action)) Input.ActionRelease(action);
        Finish();
    }

    /// <summary>Stops audio before emitting the single completion signal and reveals the preview ending.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        StopAudio();
        if (_ui != null) _ui.Visible = false;
        if (_stage != null) _stage.Visible = false;
        // Defer focus so the Skip key cannot activate Replay in the same GUI dispatch.
        if (IsInsideTree()) Callable.From(ShowCompletion).CallDeferred();
        EmitSignal(SignalName.Finished);
    }

    /// <summary>Expose completion after input dispatch, keeping playback and persistence entirely separate.</summary>
    private void ShowCompletion()
    {
        if (!IsInsideTree() || _complete == null) return;
        _complete.Visible = true;
        _complete.GetNode<Button>("Replay").GrabFocus();
    }

    /// <summary>Build a scene-local ending with keyboard/gamepad focus and no mission preparation.</summary>
    private void BuildCompletion()
    {
        var background = new ColorRect { Name = "CompleteBackground", Color = new Color("050b12"), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // Behind both presentation and completion: the cinematic stage already draws its own background.
        MoveChild(background, 0);
        var box = new VBoxContainer { Name = "Complete", Visible = false, AnchorLeft = .15f, AnchorRight = .85f, AnchorTop = .3f, AnchorBottom = .7f };
        AddChild(box);
        _complete = box;
        box.AddThemeConstantOverride("separation", 24);
        var title = new Label { Text = Localized("ACT_II_PREVIEW_COMPLETE", "ACT II PREVIEW COMPLETE"), HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        title.AddThemeFontSizeOverride("font_size", 32);
        box.AddChild(title);
        box.AddChild(new Label { Text = Localized("ACT_II_UNAVAILABLE", "Level 5 gameplay is not available yet."), HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var replay = new Button { Name = "Replay", Text = Localized("ACT_II_REPLAY", "REPLAY PREVIEW"), CustomMinimumSize = new Vector2(0,56) };
        var menu = new Button { Name = "MainMenu", Text = Localized("ACT_II_MAIN_MENU", "MAIN MENU"), CustomMinimumSize = new Vector2(0,56) };
        box.AddChild(replay);
        box.AddChild(menu);
        replay.FocusNeighborTop = replay.GetPathTo(menu);
        replay.FocusNeighborBottom = replay.GetPathTo(menu);
        menu.FocusNeighborTop = menu.GetPathTo(replay);
        menu.FocusNeighborBottom = menu.GetPathTo(replay);
        replay.Pressed += () => Navigate("res://scenes/cutscenes/act_ii_opening.tscn");
        menu.Pressed += () => Navigate("res://scenes/main_menu/main_menu.tscn");
    }

    /// <summary>Consume navigation once and stop owned sound before deferred scene replacement.</summary>
    private void Navigate(string path)
    {
        if (!IsInsideTree() || _navigating) return;
        _navigating = true;
        StopAudio();
        GetViewport().SetInputAsHandled();
        GetTree().ChangeSceneToFile(path);
    }

    /// <summary>Scene replacement also stops sound; no callbacks survive scene teardown.</summary>
    public override void _ExitTree()
    {
        StopAudio();
        Input.MouseMode = _previousMouseMode;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
    }

    /// <summary>Centralized audio cleanup is safe for missing streams and repeated calls.</summary>
    private void StopAudio()
    {
        _speech?.Stop();
        _ambience?.Stop();
    }

    /// <summary>English remains readable when the selected language has no preview translation yet.</summary>
    private string Localized(string key, string fallback)
    {
        string translated = Tr(key);
        return translated == key || string.IsNullOrWhiteSpace(translated) ? fallback : translated;
    }
}
