using System;
using Godot;

/// <summary>
/// ActIIIStartScreen provides a stylized cyberpunk title sequence ushering in
/// Act III: The Citadel Spire and Final Liberation (Missions 8–12).
/// It displays mission intel, operative readiness, and launches the Pre-Level 8 prologue.
/// </summary>
public partial class ActIIIStartScreen : CanvasLayer
{
    /// <summary>Emitted when the operation is launched.</summary>
    [Signal]
    public delegate void OperationStartedEventHandler();

    /// <summary>Emitted when mission selection is requested.</summary>
    [Signal]
    public delegate void MissionSelectRequestedEventHandler();

    /// <summary>Target scene for the Pre-Level 8 prologue cutscene.</summary>
    [Export]
    public string StratosDocksIntroScenePath { get; set; } = "res://scenes/cutscenes/stratos_docks_intro.tscn";

    /// <summary>Whether activating the start button automatically triggers scene transition.</summary>
    [Export]
    public bool AutoNavigate { get; set; } = true;

    private Control? _root;
    private Button? _startBtn;
    private Button? _selectBtn;
    private bool _navigating;

    /// <summary>Initializes responsive layout, operative strip, and input bindings.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _root = new Control
        {
            Name = "RootControl",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        BuildBackground(_root);
        BuildContent(_root);

        if (IsInsideTree())
        {
            Callable.From(() => { if (IsInstanceValid(this) && IsInsideTree() && IsInstanceValid(_startBtn)) _startBtn?.GrabFocus(); }).CallDeferred();
        }
    }

    /// <summary>Creates dark cybernetic backdrop with subtle vignette.</summary>
    private void BuildBackground(Control parent)
    {
        var bg = new ColorRect
        {
            Name = "Background",
            Color = new Color(0.01f, 0.015f, 0.035f, 0.98f)
        };
        parent.AddChild(bg);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Constructs the act title banner, narrative briefing, operative readiness, and action button.</summary>
    private void BuildContent(Control parent)
    {
        var margin = new MarginContainer
        {
            Name = "ContentMargin",
            AnchorLeft = 0.08f,
            AnchorRight = 0.92f,
            AnchorTop = 0.08f,
            AnchorBottom = 0.92f
        };
        parent.AddChild(margin);

        var vbox = new VBoxContainer
        {
            Name = "MainVBox",
            Alignment = BoxContainer.AlignmentMode.Center
        };
        vbox.AddThemeConstantOverride("separation", 20);
        margin.AddChild(vbox);

        // Act Title
        var actTitle = new Label
        {
            Name = "ActTitle",
            Text = "ACT III",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        actTitle.AddThemeFontSizeOverride("font_size", 54);
        actTitle.AddThemeColorOverride("font_color", new Color("00f0ff"));
        vbox.AddChild(actTitle);

        // Subtitle
        var subtitle = new Label
        {
            Name = "ActSubtitle",
            Text = "THE CITADEL SPIRE & FINAL LIBERATION",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 22);
        subtitle.AddThemeColorOverride("font_color", new Color("ffaa00"));
        vbox.AddChild(subtitle);

        // Scope Tag
        var scopeLabel = new Label
        {
            Name = "ScopeTag",
            Text = "CAMPAIGN MISSIONS 08 \u2013 12 // FINAL THEATER OF WAR",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        scopeLabel.AddThemeFontSizeOverride("font_size", 13);
        scopeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.75f, 0.9f));
        vbox.AddChild(scopeLabel);

        // Divider
        var divider = new ColorRect
        {
            Name = "Divider",
            CustomMinimumSize = new Vector2(0, 2),
            Color = new Color("00f0ff")
        };
        vbox.AddChild(divider);

        // Narrative Briefing Card
        var briefPanel = new PanelContainer
        {
            Name = "BriefingPanel",
            CustomMinimumSize = new Vector2(0, 110)
        };
        var briefStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.04f, 0.08f, 0.92f),
            BorderColor = new Color("00f0ff"),
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        briefPanel.AddThemeStyleboxOverride("panel", briefStyle);
        vbox.AddChild(briefPanel);

        var briefText = new Label
        {
            Name = "BriefingText",
            Text = "The planetary sky-shield has fallen. Neo-Los Angeles is waking up.\n" +
                   "High Command has locked down the Citadel Spire and scrambled interceptor drone wings across all sectors.\n" +
                   "With ground and subterranean transit sealed, the only insertion vector upward is through the clouds.\n" +
                   "Infiltrate Stratos Docks, commandeer a heavy military VTOL gunship, and scale the Spire.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        briefText.AddThemeFontSizeOverride("font_size", 13);
        briefText.AddThemeColorOverride("font_color", new Color(0.85f, 0.92f, 1.0f));
        briefPanel.AddChild(briefText);

        // Squad Readiness Strip
        var squadRow = new HBoxContainer
        {
            Name = "SquadRow",
            Alignment = BoxContainer.AlignmentMode.Center
        };
        squadRow.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(squadRow);

        AddOperativeBadge(squadRow, "CYBER SHOGUN", "Kaito Kurogane", "Parry", new Color("00f0ff"));
        AddOperativeBadge(squadRow, "SURYA PRIME", "Arjun Varma", "Bulwark", new Color("ffaa00"));
        AddOperativeBadge(squadRow, "OCEANUS", "Kiana Vaea", "Douse", new Color("00d4ff"));
        AddOperativeBadge(squadRow, "MIRAGE BLADE", "Zayd Al-Mansoor", "Phase", new Color("aa44ff"));
        AddOperativeBadge(squadRow, "APEX TITAN", "Tariq Nzinga", "Slam", new Color("ffcc00"));
        AddOperativeBadge(squadRow, "AURA PHOENICIA", "Maya Khoury", "Resonate", new Color("ff00aa"));

        // Action Buttons Row
        var btnRow = new HBoxContainer
        {
            Name = "ButtonRow",
            Alignment = BoxContainer.AlignmentMode.Center
        };
        btnRow.AddThemeConstantOverride("separation", 24);
        vbox.AddChild(btnRow);

        _startBtn = new Button
        {
            Name = "StartButton",
            Text = "BEGIN OPERATION // INFILTRATE STRATOS DOCKS  \u25B6",
            CustomMinimumSize = new Vector2(340, 52)
        };
        var startStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.2f, 0.35f, 0.95f),
            BorderColor = new Color("00f0ff"),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        _startBtn.AddThemeStyleboxOverride("normal", startStyle);
        var startHover = (StyleBoxFlat)startStyle.Duplicate();
        startHover.BgColor = new Color(0.1f, 0.32f, 0.55f, 1.0f);
        startHover.BorderColor = Colors.White;
        _startBtn.AddThemeStyleboxOverride("hover", startHover);
        var startFocus = (StyleBoxFlat)startStyle.Duplicate();
        startFocus.BorderColor = new Color("00f0ff");
        startFocus.SetBorderWidthAll(3);
        _startBtn.AddThemeStyleboxOverride("focus", startFocus);
        _startBtn.AddThemeFontSizeOverride("font_size", 14);
        _startBtn.AddThemeColorOverride("font_color", Colors.White);
        _startBtn.Pressed += OnStartPressed;
        btnRow.AddChild(_startBtn);

        _selectBtn = new Button
        {
            Name = "SelectButton",
            Text = "MISSION SELECT",
            CustomMinimumSize = new Vector2(160, 52)
        };
        var selectStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.05f, 0.1f, 0.8f),
            BorderColor = new Color(0.5f, 0.6f, 0.7f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        _selectBtn.AddThemeStyleboxOverride("normal", selectStyle);
        var selectHover = (StyleBoxFlat)selectStyle.Duplicate();
        selectHover.BorderColor = Colors.White;
        _selectBtn.AddThemeStyleboxOverride("hover", selectHover);
        _selectBtn.AddThemeFontSizeOverride("font_size", 12);
        _selectBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 0.9f));
        _selectBtn.Pressed += OnSelectPressed;
        btnRow.AddChild(_selectBtn);
    }

    /// <summary>Adds a compact operative badge indicating full squad readiness.</summary>
    private void AddOperativeBadge(HBoxContainer parent, string operative, string civilian, string ability, Color accent)
    {
        var badge = new PanelContainer
        {
            Name = "Badge_" + operative.Replace(" ", "_"),
            CustomMinimumSize = new Vector2(120, 54)
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.015f, 0.03f, 0.06f, 0.85f),
            BorderColor = accent,
            BorderWidthTop = 2,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        badge.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(badge);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        badge.AddChild(vbox);

        var title = new Label
        {
            Text = operative,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 9);
        title.AddThemeColorOverride("font_color", accent);
        vbox.AddChild(title);

        var abLabel = new Label
        {
            Text = ability.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        abLabel.AddThemeFontSizeOverride("font_size", 11);
        abLabel.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(abLabel);

        var status = new Label
        {
            Text = "DEPLOYED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        status.AddThemeFontSizeOverride("font_size", 8);
        status.AddThemeColorOverride("font_color", new Color(0.3f, 1.0f, 0.5f));
        vbox.AddChild(status);
    }

    /// <summary>Starts Operation and navigates to Pre-Level 8 prologue.</summary>
    private void OnStartPressed()
    {
        EmitSignal(SignalName.OperationStarted);
        if (AutoNavigate) Navigate(StratosDocksIntroScenePath);
    }

    /// <summary>Returns to Level Selection screen.</summary>
    private void OnSelectPressed()
    {
        EmitSignal(SignalName.MissionSelectRequested);
        if (AutoNavigate) Navigate("res://scenes/main_menu/level_selection.tscn");
    }

    /// <summary>Safe navigation helper checking tree state and releasing pause.</summary>
    private void Navigate(string path)
    {
        if (!IsInsideTree() || _navigating) return;
        _navigating = true;
        if (GetTree() != null)
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(path);
        }
    }

    /// <summary>Accepts Space, Enter, or Escape shortcuts to advance or exit.</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsInsideTree() || _navigating) return;

        if (@event.IsActionPressed("ui_accept"))
        {
            GetViewport()?.SetInputAsHandled();
            OnStartPressed();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport()?.SetInputAsHandled();
            OnSelectPressed();
        }
    }
}
