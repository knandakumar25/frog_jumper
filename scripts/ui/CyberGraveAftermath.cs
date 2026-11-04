using System;
using Godot;

/// <summary>
/// Skippable 90-second post-mission cinematic epilogue overlay ("Ascent from the Cyber-Grave").
/// Coordinates camera choreography, multi-speaker dialogue subtitles, and atmospheric audio across
/// nine authored story beats depicting the freight lift ascent with the liberated captives.
/// </summary>
public partial class CyberGraveAftermath : CanvasLayer
{
    /// <summary>Raised once after cinematic audio stops, regardless of natural finish, Skip or missing resources.</summary>
    [Signal] public delegate void FinishedEventHandler();

    /// <summary>Mastered mixed dialogue audio stream; absent speech leaves the complete timed subtitle sequence intact.</summary>
    [Export] public string SpeechPath { get; set; } = "res://assets/cutscenes/cyber_grave_aftermath/dialogue.ogg";

    /// <summary>Asset directory containing cues and atmospheric background stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_grave_aftermath/";

    /// <summary>Deterministic timeline duration in seconds.</summary>
    public const double Duration = 90.0;

    /// <summary>Current deterministic timeline position in seconds.</summary>
    public double Elapsed { get; private set; }

    /// <summary>True once completion has been consumed, preventing double handoffs.</summary>
    public bool HasFinished { get; private set; }

    // Dialogue timing cue start points (matching assets/cutscenes/cyber_grave_aftermath/cues.json)
    private static readonly double[] Starts = { 0, 8.0, 18.0, 28.0, 38.0, 48.0, 58.0, 68.0, 78.0, 90.0 };

    // Dialogue text per cue
    private static readonly string[] Lines = {
        "AXIOM-7: Ascent telemetry locked. Freight lift rising from sub-level six. All seventeen liberated captives secured on platform.",
        "OCEANUS: Their vital signs are stabilizing. The stasis fluid in their lungs is clearing... breathe slowly, you're safe now. No one will ever cage you again.",
        "AURA: The scream in the network... it's fading. For miles beneath the concrete, the forced resonance has broken. Their minds belong to them again.",
        "TITAN: Reactor pressure is venting safely into the geothermal sinks. The containment walls held. The foundation beneath Downtown Core won't collapse.",
        "MIRAGE: Take a look at the data disk I pulled from the vault terminal. This wasn't an isolated rogue lab. Every pod order was authorized directly from the Citadel Spire.",
        "SURYA // COMMS: Kaito! Extraction corridor is clear topside. Emergency medics and underground transit convoys are standing by at the maintenance junction.",
        "SURYA // COMMS: Warning: Citadel high command has detected the breach. Heavy orbital defense arrays are rotating to lock down the upper sectors.",
        "KAITO: They turned our people into machine components and buried them in the dark. But the Cyber-Grave is empty today. Every captive returns with us.",
        "KAITO: Tend to the wounded. Rest for one hour. When we reach the surface, we take the fight straight to the Citadel Spire. Axiom Force—ascend!",
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

    private CyberGraveAftermathStage? _stage;
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

    /// <summary>Constructs procedural stage, subtitle panel, skip button, and starts audio playback.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _previousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Preserve and hide persistent control prompts from Global
        var global = GetNodeOrNull<Node>("/root/Global");
        Node? prompt = global?.Get("_prompt_label").AsGodotObject() as Node;
        while (prompt != null && prompt is not CanvasLayer) prompt = prompt.GetParent();
        if (prompt is CanvasLayer promptLayer) _previousPromptVisible = promptLayer.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);

        // Procedural visual stage
        _stage = new CyberGraveAftermathStage { Name = "Stage", ArtPath = ArtPath, MouseFilter = Control.MouseFilterEnum.Ignore };
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
            Text = Localized("CYBER_GRAVE_SKIP", "SKIP // ESC / START"),
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -270,
            OffsetRight = -28,
            OffsetTop = 24,
            OffsetBottom = 72
        };
        var skipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.01f, 0.03f, 0.05f, 0.85f),
            BorderColor = new Color("00f0ff"),
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
            if (_stalled < 0.5)
                clock = Math.Max(Elapsed, position + AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency());
        }
        Seek(clock);
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
            1 => ColorOceanus,
            2 => ColorAura,
            3 => ColorTitan,
            4 => ColorMirage,
            5 or 6 => ColorSurya,
            7 or 8 => ColorKaito,
            _ => ColorAxiom
        };
        if (_panelStyle != null) _panelStyle.BorderColor = speakerColor;

        _subtitle.AddThemeFontSizeOverride("font_size", (int)Math.Clamp(GetViewport().GetVisibleRect().Size.Y / 45, 14, 22));
        _subtitle.Text = Localized($"CYBER_GRAVE_AFTERMATH_CUE_{cue}", Lines[Math.Min(cue, Lines.Length - 1)]);
        if (_panel != null) _panel.Visible = cue >= 0 && cue < 9 && !string.IsNullOrEmpty(Lines[cue]);
    }

    /// <summary>Handles keyboard, controller, mouse, and touch skip shortcuts.</summary>
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

    /// <summary>Stops active audio streams and emits the single Finished signal callback.</summary>
    public void Finish()
    {
        if (HasFinished) return;
        HasFinished = true;
        _speech?.Stop();
        _ambience?.Stop();
        EmitSignal(SignalName.Finished);
    }

    /// <summary>Restores persistent prompt visibility and mouse cursor mode when leaving scene tree.</summary>
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
