using System;
using Godot;

/// <summary>
/// ActIIEndScreen provides a tactical campaign debrief celebrating the liberation
/// of the three Act II infrastructure sectors (Downtown Civic District, Cyber-Grave Labs,
/// and The Foundry Forge), and confirming the total collapse of the Citadel's planetary sky-shield.
/// It bridges the Act II scorecard into Act III.
/// </summary>
public partial class ActIIEndScreen : CanvasLayer
{
    /// <summary>Emitted when the player proceeds to Act III.</summary>
    [Signal]
    public delegate void ProceedToActIIIEventHandler();

    /// <summary>Emitted when the player chooses to return to level selection.</summary>
    [Signal]
    public delegate void LevelSelectRequestedEventHandler();

    /// <summary>Emitted when the player chooses to return to the main menu.</summary>
    [Signal]
    public delegate void MainMenuRequestedEventHandler();

    /// <summary>Whether activating the proceed button performs scene transition automatically.</summary>
    [Export]
    public bool AutoNavigate { get; set; } = true;

    /// <summary>Target scene for Act III start.</summary>
    [Export]
    public string ActIIIStartScenePath { get; set; } = "res://scenes/ui/act_iii_start_screen.tscn";

    private Control? _root;
    private Button? _proceedBtn;
    private Button? _levelSelectBtn;
    private Button? _mainMenuBtn;
    private bool _navigating;

    /// <summary>Builds responsive cyber debrief interface and grabs button focus.</summary>
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
            Callable.From(() => { if (IsInstanceValid(this) && IsInsideTree() && IsInstanceValid(_proceedBtn)) _proceedBtn?.GrabFocus(); }).CallDeferred();
        }
    }

    /// <summary>Renders deep blue dark background with glowing cyan and amber cyber accent lines.</summary>
    private void BuildBackground(Control parent)
    {
        var bg = new ColorRect
        {
            Name = "BackgroundDim",
            Color = new Color(0.01f, 0.02f, 0.05f, 0.96f)
        };
        parent.AddChild(bg);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Assembles the header, sector debrief cards, telemetry summary, and action bar.</summary>
    private void BuildContent(Control parent)
    {
        var margin = new MarginContainer
        {
            Name = "MainMargin",
            AnchorLeft = 0.05f,
            AnchorRight = 0.95f,
            AnchorTop = 0.04f,
            AnchorBottom = 0.96f
        };
        parent.AddChild(margin);

        var vbox = new VBoxContainer
        {
            Name = "VBoxMain",
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        vbox.AddThemeConstantOverride("separation", 16);
        margin.AddChild(vbox);

        // Header Title
        var headerLabel = new Label
        {
            Name = "HeaderTitle",
            Text = "ACT II CONCLUDED // THE INFRASTRUCTURE COLLAPSE",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        headerLabel.AddThemeFontSizeOverride("font_size", 28);
        headerLabel.AddThemeColorOverride("font_color", new Color("00f0ff"));
        vbox.AddChild(headerLabel);

        var subheaderLabel = new Label
        {
            Name = "SubheaderTitle",
            Text = "ALL THREE PRIMARY OCCUPATION INFRASTRUCTURE SYSTEMS SEVERED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subheaderLabel.AddThemeFontSizeOverride("font_size", 14);
        subheaderLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        vbox.AddChild(subheaderLabel);

        // Separator
        var divider = new ColorRect
        {
            Name = "HeaderDivider",
            CustomMinimumSize = new Vector2(0, 2),
            Color = new Color("00f0ff")
        };
        vbox.AddChild(divider);

        // Three Sector Debrief Cards
        var cardsHBox = new HBoxContainer
        {
            Name = "SectorCardsRow",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        cardsHBox.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(cardsHBox);

        AddSectorCard(cardsHBox, "SEC.05 // DOWNTOWN CORE", "NAMES WITHOUT NUMBERS",
            "Biometric surveillance registry breached and neutralized.\nCivilian identities decrypted and restored across all municipal terminals.",
            new Color(0.2f, 0.8f, 1.0f));

        AddSectorCard(cardsHBox, "SEC.06 // CYBER-GRAVE LABS", "SUBTERRANEAN FOUNDRY",
            "Bio-synthetic conversion tanks compromised.\n17 captive operatives and civilians rescued from cryogenic neural stasis.",
            new Color(0.3f, 1.0f, 0.5f));

        AddSectorCard(cardsHBox, "SEC.07 // THE FOUNDRY FORGE", "GEOTHERMAL CORE CRITICAL",
            "Geothermal plasma reactor overloaded.\nIGNIS-COLOSSUS guardian mech defeated. Planetary hexagonal sky-shield completely collapsed.",
            new Color(1.0f, 0.5f, 0.2f));

        // Telemetry Summary Panel
        var statusPanel = new PanelContainer
        {
            Name = "TelemetryPanel",
            CustomMinimumSize = new Vector2(0, 70)
        };
        var statusStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.05f, 0.1f, 0.85f),
            BorderColor = new Color("ffaa00"),
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
        statusPanel.AddThemeStyleboxOverride("panel", statusStyle);
        vbox.AddChild(statusPanel);

        var statusVBox = new VBoxContainer();
        statusPanel.AddChild(statusVBox);

        var statusText1 = new Label
        {
            Name = "StatusLine1",
            Text = "TACTICAL STATUS: PLANETARY SKY-SHIELD OFFLINE (0.0% INTEGRITY) // SPIRE DEFENSES EXPOSED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        statusText1.AddThemeFontSizeOverride("font_size", 13);
        statusText1.AddThemeColorOverride("font_color", new Color("ffaa00"));
        statusVBox.AddChild(statusText1);

        var statusText2 = new Label
        {
            Name = "StatusLine2",
            Text = "NEXT OBJECTIVE: INFILTRATE STRATOS DOCKS // SECURE HEAVY VTOL GUNSHIP // ASCEND TO SPIRE",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        statusText2.AddThemeFontSizeOverride("font_size", 12);
        statusText2.AddThemeColorOverride("font_color", Colors.White);
        statusVBox.AddChild(statusText2);

        // Action Buttons Row
        var btnRow = new HBoxContainer
        {
            Name = "ButtonRow",
            Alignment = BoxContainer.AlignmentMode.Center
        };
        btnRow.AddThemeConstantOverride("separation", 24);
        vbox.AddChild(btnRow);

        _proceedBtn = CreateCyberButton("ProceedButton", "PROCEED TO ACT III  \u25B6", new Color("00f0ff"), true);
        _proceedBtn.Pressed += OnProceedPressed;
        btnRow.AddChild(_proceedBtn);

        _levelSelectBtn = CreateCyberButton("LevelSelectButton", "MISSION SELECT", new Color(0.7f, 0.8f, 0.9f), false);
        _levelSelectBtn.Pressed += OnLevelSelectPressed;
        btnRow.AddChild(_levelSelectBtn);

        _mainMenuBtn = CreateCyberButton("MainMenuButton", "MAIN MENU", new Color(0.6f, 0.6f, 0.7f), false);
        _mainMenuBtn.Pressed += OnMainMenuPressed;
        btnRow.AddChild(_mainMenuBtn);
    }

    /// <summary>Helper building an individual sector debrief panel.</summary>
    private void AddSectorCard(HBoxContainer parent, string code, string name, string desc, Color accent)
    {
        var panel = new PanelContainer
        {
            Name = "Card_" + code.Replace(" ", "_").Replace(".", "_").Replace("/", "_"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.015f, 0.035f, 0.08f, 0.9f),
            BorderColor = accent,
            BorderWidthTop = 3,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        panel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(panel);

        var cardVBox = new VBoxContainer();
        cardVBox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(cardVBox);

        var codeLabel = new Label
        {
            Text = code,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        codeLabel.AddThemeFontSizeOverride("font_size", 11);
        codeLabel.AddThemeColorOverride("font_color", accent);
        cardVBox.AddChild(codeLabel);

        var nameLabel = new Label
        {
            Text = name,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 15);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        cardVBox.AddChild(nameLabel);

        var statusTag = new Label
        {
            Text = "\u2714 SECTOR LIBERATED",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        statusTag.AddThemeFontSizeOverride("font_size", 11);
        statusTag.AddThemeColorOverride("font_color", new Color(0.3f, 1.0f, 0.4f));
        cardVBox.AddChild(statusTag);

        var descLabel = new Label
        {
            Text = desc,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLabel.AddThemeFontSizeOverride("font_size", 12);
        descLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.85f, 0.95f));
        cardVBox.AddChild(descLabel);
    }

    /// <summary>Constructs a styled cyberpunk button with focus border.</summary>
    private Button CreateCyberButton(string name, string text, Color accent, bool primary)
    {
        var btn = new Button
        {
            Name = name,
            Text = text,
            CustomMinimumSize = new Vector2(primary ? 220 : 160, 48)
        };

        var normal = new StyleBoxFlat
        {
            BgColor = primary ? new Color(0.04f, 0.15f, 0.28f, 0.9f) : new Color(0.02f, 0.05f, 0.1f, 0.8f),
            BorderColor = accent,
            BorderWidthLeft = primary ? 2 : 1,
            BorderWidthRight = primary ? 2 : 1,
            BorderWidthTop = primary ? 2 : 1,
            BorderWidthBottom = primary ? 2 : 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = primary ? new Color(0.08f, 0.25f, 0.45f, 0.95f) : new Color(0.05f, 0.1f, 0.18f, 0.9f);
        hover.BorderColor = Colors.White;
        btn.AddThemeStyleboxOverride("hover", hover);

        var focus = (StyleBoxFlat)normal.Duplicate();
        focus.BorderColor = new Color("00f0ff");
        focus.SetBorderWidthAll(3);
        btn.AddThemeStyleboxOverride("focus", focus);

        btn.AddThemeFontSizeOverride("font_size", primary ? 14 : 12);
        btn.AddThemeColorOverride("font_color", Colors.White);

        return btn;
    }

    /// <summary>Handles proceed button activation, advancing into Act III.</summary>
    private void OnProceedPressed()
    {
        EmitSignal(SignalName.ProceedToActIII);
        if (AutoNavigate) Navigate(ActIIIStartScenePath);
    }

    /// <summary>Navigates back to the Level Selection screen.</summary>
    private void OnLevelSelectPressed()
    {
        EmitSignal(SignalName.LevelSelectRequested);
        if (AutoNavigate) Navigate("res://scenes/main_menu/level_selection.tscn");
    }

    /// <summary>Navigates back to the Main Menu.</summary>
    private void OnMainMenuPressed()
    {
        EmitSignal(SignalName.MainMenuRequested);
        if (AutoNavigate) Navigate("res://scenes/main_menu/main_menu.tscn");
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

    /// <summary>Handles keyboard and gamepad cancellation or advance shortcuts.</summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsInsideTree() || _navigating) return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport()?.SetInputAsHandled();
            OnLevelSelectPressed();
        }
    }
}
