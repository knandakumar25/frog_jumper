using Godot;

/// <summary>Mission 5's skippable ninety-second epilogue about civilians reclaiming their names after extraction.</summary>
public partial class NamesWithoutNumbersAftermath : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();
    /// <summary>Optional mixed dialogue; absent speech leaves the complete timed subtitle sequence available.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/names_without_numbers/dialogue.ogg";
    /// <summary>Root for original district art and the independently authored atmosphere track.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/names_without_numbers/";
    /// <summary>Fixed presentation length, independent of speech resource availability.</summary>
    public const double Duration = 90;
    /// <summary>Current deterministic timeline position for shot and subtitle review.</summary>
    public double Elapsed { get; private set; }
    /// <summary>Prevents duplicate result handoffs from repeated input or teardown.</summary>
    public bool HasFinished { get; private set; }

    private static readonly double[] Starts = { 0, 12, 22, 31, 40, 49, 58, 68, 82 };
    private static readonly string[] Lines = {
        "CIVILIAN AMARA: My name is Amara. I choose to keep it.",
        "CIVILIANS: I am Elias. My name is Nia. I remember mine.",
        "MIRAGE: No one asked them to prove it. They just said it.",
        "AURA: The records are theirs. So is the next word.",
        "OCEANUS: Water, shelter, transit. The gates are answering to people again.",
        "TITAN: They carried those names through every locked door.",
        "SURYA // COMMS: The queue is moving. They are choosing where to go.",
        "KAITO: We helped make the choice possible. What comes next is theirs to decide.",
        ""
    };

    private NamesWithoutNumbersAftermathStage? _stage;
    private Label? _subtitle;
    private AudioStreamPlayer? _speech;
    private AudioStreamPlayer? _ambience;
    private Button? _skip;
    private Input.MouseModeEnum _previousMouseMode;
    private bool _previousPromptVisible = true;
    private double _lastAudio = -1;
    private double _stalled;

    /// <summary>Creates responsive presentation controls and starts owned audio when the overlay enters the tree.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Node? prompt = GetNodeOrNull<Node>("/root/Global")?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);

        _stage = new NamesWithoutNumbersAftermathStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var ui = new Control { Name = "UI", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(ui);
        ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { Name = "SubtitlePanel", AnchorLeft = .08f, AnchorRight = .92f, AnchorTop = .77f, AnchorBottom = .96f };
        ui.AddChild(panel);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(.008f, .02f, .04f, .95f), BorderColor = new Color("66d9e8"), BorderWidthTop = 2, ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 14, ContentMarginBottom = 14 });
        _subtitle = new Label { Name = "Subtitle", AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _subtitle.AddThemeFontSizeOverride("font_size", 22);
        panel.AddChild(_subtitle);
        _skip = new Button { Name = "Skip", Text = "SKIP // ESC / START", AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -270, OffsetRight = -28, OffsetTop = 24, OffsetBottom = 72 };
        ui.AddChild(_skip);
        _skip.Pressed += Finish;
        _skip.GrabFocus();
        _speech = CreateAudio("Speech", SpeechPath, 0);
        _ambience = CreateAudio("Atmosphere", ArtPath + "atmosphere.ogg", -18);
        Seek(0);
        if (!_stage.ResourcesAvailable) Callable.From(Finish).CallDeferred();
    }

    /// <summary>Loads an optional overlay-owned stream without asking Godot to open a missing resource.</summary>
    private AudioStreamPlayer CreateAudio(string name, string path, float volume)
    {
        var player = new AudioStreamPlayer { Name = name, Bus = "SFX", VolumeDb = volume };
        AddChild(player);
        if (ResourceLoader.Exists(path)) player.Stream = GD.Load<AudioStream>(path);
        if (player.Stream != null && IsInsideTree()) player.Play();
        return player;
    }

    /// <summary>Uses compensated speech playback while it advances and a monotonic wall clock if speech stalls.</summary>
    public override void _Process(double delta)
    {
        if (HasFinished) return;
        double clock = Elapsed + delta;
        if (_speech is { Playing: true })
        {
            double position = _speech.GetPlaybackPosition();
            _stalled = position <= _lastAudio ? _stalled + delta : 0;
            _lastAudio = position;
            if (_stalled < .5) clock = System.Math.Max(Elapsed, position + AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency());
        }
        Seek(clock);
        if (Elapsed >= Duration) Finish();
    }

    /// <summary>Samples shot, civilian choice, subtitle and stage animation from one seekable timeline.</summary>
    public void Seek(double seconds)
    {
        Elapsed = System.Math.Clamp(seconds, 0, Duration);
        if (_stage == null || _subtitle == null) return;
        _stage.Seconds = (float)Elapsed;
        _stage.QueueRedraw();
        int cue = 0;
        for (int i = 1; i < Starts.Length; i++) if (Elapsed >= Starts[i]) cue = i;
        _subtitle.AddThemeFontSizeOverride("font_size", (int)System.Math.Clamp(GetViewport().GetVisibleRect().Size.Y / 45, 14, 22));
        _subtitle.Text = Lines[cue];
        _subtitle.GetParent<Control>().Visible = cue < Lines.Length - 1;
    }

    /// <summary>Aligns audio and visuals after a review seek without introducing a second clock.</summary>
    public void SeekAudio(double seconds)
    {
        Seek(seconds);
        _lastAudio = -1;
        _stalled = 0;
        if (!IsInsideTree() || HasFinished) return;
        if (_speech?.Stream != null) _speech.Play((float)Elapsed);
        if (_ambience?.Stream != null) _ambience.Play((float)Elapsed);
    }

    /// <summary>Routes keyboard, controller, pointer and touch Skip through the same idempotent completion.</summary>
    public override void _Input(InputEvent input)
    {
        if (HasFinished || !IsInsideTree()) return;
        bool skip = input is InputEventKey { Pressed: true, Echo: false } key && key.Keycode is Key.Escape or Key.Space or Key.Enter;
        skip |= input is InputEventJoypadButton { Pressed: true } joy && joy.ButtonIndex is JoyButton.Start or JoyButton.A;
        skip |= input is InputEventScreenTouch { Pressed: true } touch && _skip != null && _skip.GetGlobalRect().HasPoint(touch.Position);
        skip |= input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse && _skip != null && _skip.GetGlobalRect().HasPoint(mouse.Position);
        if (!skip) return;
        GetViewport().SetInputAsHandled();
        foreach (StringName action in InputMap.GetActions()) if (InputMap.EventIsAction(input, action)) Input.ActionRelease(action);
        Finish();
    }

    /// <summary>Consumes completion once, stops both tracks and defers the result handoff beyond input dispatch.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        StopAudio();
        if (IsInsideTree())
        {
            Input.MouseMode = _previousMouseMode;
            GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
            Callable.From(DeliverResults).CallDeferred();
        }
        else EmitSignal(SignalName.Finished);
    }

    /// <summary>Emits the single results signal only while the owning mission remains in the active tree.</summary>
    private void DeliverResults()
    {
        if (!IsInsideTree()) return;
        EmitSignal(SignalName.Finished);
        QueueFree();
    }

    /// <summary>Stops audio on independent removal and restores cursor/prompt state if completion was not consumed.</summary>
    public override void _ExitTree()
    {
        StopAudio();
        if (!HasFinished)
        {
            // An independently removed overlay cannot leave the frozen mission stranded without its results screen.
            HasFinished = true;
            if (IsInsideTree()) EmitSignal(SignalName.Finished);
            Input.MouseMode = _previousMouseMode;
            GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
        }
    }

    /// <summary>Stops all tracks owned by this overlay; repeated calls are safe.</summary>
    private void StopAudio() { _speech?.Stop(); _ambience?.Stop(); }
}
