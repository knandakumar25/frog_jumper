using Godot;
using System;

/// <summary>
/// CharacterSelection manages the operative selection screen.
/// It handles 2D operative cards, tactical loadouts, and new game sessions.
/// </summary>
public partial class CharacterSelection : Control
{
	// ─── Node refs ───────────────────────────────────────────────────────────
	private Label _selectedNameLabel;
	private Label _civilianNameLabel;
	private Label _classTagLabel;
	private AnimatedSprite2D _previewSprite;
	private SegmentedStatBar _speedMeter;
	private SegmentedStatBar _agilityMeter;

	private Label _abilitiesHeaderLabel;
	private Button _primaryAbilityBtn;
	private Button _secondaryAbilityBtn;
	private Button _dashAbilityBtn;
	private Button _slamAbilityBtn;
	private Label _abilityBadgeLabel;
	private Label _abilityDescLabel;
	private Label _abilityStatusLabel;

	private TextureRect? _operativeCardImage;
	private Label? _cardHeaderLabel;
	private Label? _cardStatusLabel;

	private Control? _mainLayout;
	private Button? _upgradeOperativeBtn;
	private OperativeUpgradeScreen? _upgradeScreen;
	private VBoxContainer? _artStudy;
	private ShogunArtPreview? _artPreview;
	private Label? _artStatus;
	// Shared action buttons are relabeled per operative; the original portrait remains available as an asset.
	private readonly System.Collections.Generic.List<Button> _artButtons = new();


	private string _currentSelectedCharacter = "Cyber Shogun";
	/// <summary>Squad the player has earned so far (derived from the highest mission reached). Locked operatives are previewable but cannot be set active.</summary>
	private OperativeRoster _roster = OperativeRoster.ForLevelStart(1);

	/// <summary>Builds the unified dossier, initializes selection, and wires roster navigation.</summary>
	public override void _Ready()
	{
		// Stop the main menu music to allow character-specific audio or silence.
		if (GetTree()?.Root?.HasNode("Global") == true)
		{
			GetTree().Root.GetNodeOrNull<Node>("Global")?.Call("stop_menu_music");
		}

		EnsureUIReferences();
		BuildArtStudy();

		Input.MouseMode = Input.MouseModeEnum.Hidden;

		var global = GetNodeOrNull<Node>("/root/Global");
		int reached = global != null ? global.Get("unlocked_levels").AsInt32() : 1;
		_roster = OperativeRoster.ForLevelStart(reached);
		string active = OperativeRoster.Normalize(global?.Get("selected_character").AsString());
		if (!_roster.IsUnlocked(active)) active = OperativeRoster.Starter;

		TranslateUI();
		SelectOperative(active);
		FixFontForLocale(this);
		UISoundManager.AutoWireButtons(this);

		// Setup button event handlers for the character grid (both Pressed and FocusEntered for keyboard/gamepad navigation).
		var phoeniciaBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Phoenicia");
		if (phoeniciaBtn != null)
		{
			phoeniciaBtn.Pressed += () => SelectOperative("Aura Phoenicia");
			phoeniciaBtn.FocusEntered += () => SelectOperative("Aura Phoenicia");
		}
		var apexTitanBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan");
		if (apexTitanBtn != null)
		{
			apexTitanBtn.Pressed += () => SelectOperative("Apex Titan");
			apexTitanBtn.FocusEntered += () => SelectOperative("Apex Titan");
		}
		var mirageBladeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade");
		if (mirageBladeBtn != null)
		{
			mirageBladeBtn.Pressed += () => SelectOperative("Mirage Blade");
			mirageBladeBtn.FocusEntered += () => SelectOperative("Mirage Blade");
		}
		var suryaPrimeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime");
		if (suryaPrimeBtn != null)
		{
			suryaPrimeBtn.Pressed += () => SelectOperative("Surya Prime");
			suryaPrimeBtn.FocusEntered += () => SelectOperative("Surya Prime");
		}
		var oceanusBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus");
		if (oceanusBtn != null)
		{
			oceanusBtn.Pressed += () => SelectOperative("Oceanus");
			oceanusBtn.FocusEntered += () => SelectOperative("Oceanus");
		}
		var shogunBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun");
		if (shogunBtn != null)
		{
			shogunBtn.Pressed += () => SelectOperative("Cyber Shogun");
			shogunBtn.FocusEntered += () => SelectOperative("Cyber Shogun");
		}

		shogunBtn?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public override void _Input(InputEvent @event)
	{
		// Escape / B returns to the main menu without changing the active operative.
		if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("escape"))
		{
			ReturnToMenu();
			GetViewport()?.SetInputAsHandled();
			return;
		}
		// Completely eliminate mouse cursor compatibility
		if (@event is InputEventMouse)
		{
			GetViewport()?.SetInputAsHandled();
			return;
		}
	}

	/// <summary>Refreshes presentation only; selecting or replaying the operative would reset an active preview.</summary>
	public override void _Notification(int what)
	{
		if (what != NotificationTranslationChanged || !IsNodeReady()) return;
		TranslateUI();
		UpdateRosterButtons();
		UpdateUpgradeButtonText();
		_speedMeter?.QueueRedraw();
		_agilityMeter?.QueueRedraw();
	}

	/// <summary>Retains the historical hook; LocaleService owns the shared font and direction policy.</summary>
	private static void FixFontForLocale(Node node)
	{
		// Native Orbitron & Rajdhani sci-fi fonts handle Arabic via Cairo font fallbacks configured in global.gd.
	}

	/// <summary>Builds the shared, keyboard-accessible animation study without changing campaign controls.</summary>
	private void BuildArtStudy()
	{
		var center = GetNodeOrNull<VBoxContainer>("MainLayout/RightShowcasePanel/VBox");
		if (center == null || _artStudy != null) return;
		_artStudy = new VBoxContainer { Name = "ShogunArtStudy", SizeFlagsVertical = SizeFlags.ExpandFill };
		center.AddChild(_artStudy);
		center.MoveChild(_artStudy, center.GetNode<Control>("AbilitiesBox").GetIndex());
		_artPreview = new ShogunArtPreview {
			Name = "ArtPreview", CustomMinimumSize = new Vector2(0, 250),
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_artStudy.AddChild(_artPreview);
		_artStatus = new Label {
			Text = "Idle • character preview", HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_artStatus.AddThemeFontSizeOverride("font_size", 16);
		_artStudy.AddChild(_artStatus);
		var grid = new GridContainer { Name = "Actions", Columns = 7 };
		_artStudy.AddChild(grid);
		foreach (string action in new[] { "Idle", "Walk", "Run", "Jump", "Double jump", "Katana",
			"Martial strike" })
		{
			var button = new Button {
				Text = action, CustomMinimumSize = new Vector2(0, 34),
				SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore
			};
			button.AddThemeFontSizeOverride("font_size", 15);
			// Display text may change language; animation dispatch always uses the canonical action.
			WildfireText.SetText(button, "Text", "UI_SCHEMA_TEXT", $"{MovementDisplay(action)}");
			button.Pressed += () => PreviewStudyAction(action);
			grid.AddChild(button);
			_artButtons.Add(button);
		}
		var roster = GetNodeOrNull<Control>("MainLayout/LeftListPanel/VBox/CharacterGrid");
		if (roster != null)
		{
			foreach (Node child in roster.GetChildren())
				if (child is Button rosterButton)
					rosterButton.FocusNeighborRight = rosterButton.GetPathTo(grid.GetChild<Button>(0));
		}
		var note = new Label {
			Text = "Arrows / D-pad: choose • Enter / A: preview",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		note.AddThemeFontSizeOverride("font_size", 13);
		_artStudy.AddChild(note);
	}

	/// <summary>Auditions movement or the selected operative's named loadout ability.</summary>
	public void PreviewStudyAction(string action)
	{
		if (_artPreview == null) return;
		_artPreview.Preview(action);
		var abilities = GetOperativeAbilities(_currentSelectedCharacter);
		bool abilityPreview = false;
		for (int i = 0; i < abilities.Length; i++)
		{
			if (abilities[i].Name != action) continue;
			_artPreview.PreviewAbility(action, i);
			UpdateAbilityDisplay(abilities[i], isPreviewing: true);
			abilityPreview = true;
			break;
		}
		if (_artStatus != null)
		{
			string suffix = abilityPreview ? " • visual ability preview" : " • character preview";
			WildfireText.SetText(_artStatus, "Text", "UI_PREVIEW_STATUS", $"{MovementDisplay(action)}{WildfireText.DisplayValue(suffix)}");
		}
	}

	/// <summary>Refreshes artwork and visible movement controls, then rebuilds focus for the merged dossier.</summary>
	private void ConfigureArtStudy(string characterName)
	{
		if (_artPreview == null) return;
		_artPreview.SetOperative(characterName);

		_artButtons[5].Visible = _artButtons[6].Visible = characterName == "Cyber Shogun";
		string rosterNode = OperativeRegistry.GetProfile(characterName).Id switch {
			"surya_prime" => "SuryaPrime", "oceanus" => "Oceanus", "mirage_blade" => "MirageBlade",
			"apex_titan" => "ApexTitan", "phoenicia" => "Phoenicia", _ => "CyberShogun"
		};
		var selectedButton = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/" + rosterNode);
		if (selectedButton != null) ConfigureMergedFocus(selectedButton);
		if (_artStatus != null) BindSchemaText(_artStatus, "Idle • character preview");
	}

    /// <summary>Connects visible movement, ability, and footer rows; hidden melee buttons never receive focus.</summary>
    private void ConfigureMergedFocus(Button selected)
    {
        var rows = new System.Collections.Generic.List<System.Collections.Generic.List<Button>>();
        var visible = _artButtons.FindAll(button => button.Visible);
        for (int start = 0; start < visible.Count; start += 7)
            rows.Add(visible.GetRange(start, Math.Min(7, visible.Count-start)));
        rows.Add(new() { _primaryAbilityBtn, _secondaryAbilityBtn, _dashAbilityBtn, _slamAbilityBtn });
        rows.Add(new() { _upgradeOperativeBtn!, GetNode<Button>("MainLayout/RightShowcasePanel/VBox/Footer/DeployButton") });
        var ordered = new System.Collections.Generic.List<Button>();
        foreach (var row in rows) ordered.AddRange(row);
        for (int r=0; r<rows.Count; r++)
            for (int c=0; c<rows[r].Count; c++)
            {
                var button=rows[r][c];
                var above=rows[Math.Max(0,r-1)]; var below=rows[Math.Min(rows.Count-1,r+1)];
                button.FocusNeighborLeft=button.GetPathTo(c==0?selected:rows[r][c-1]);
                button.FocusNeighborRight=button.GetPathTo(rows[r][Math.Min(c+1,rows[r].Count-1)]);
                button.FocusNeighborTop=button.GetPathTo(above[Math.Min(c,above.Count-1)]);
                button.FocusNeighborBottom=button.GetPathTo(below[Math.Min(c,below.Count-1)]);
                int index=ordered.IndexOf(button);
                button.FocusNext=button.GetPathTo(index+1<ordered.Count?ordered[index+1]:selected);
                button.FocusPrevious=button.GetPathTo(index>0?ordered[index-1]:selected);
            }
        var roster=GetNode<Control>("MainLayout/LeftListPanel/VBox/CharacterGrid");
        for(int i=0;i<roster.GetChildCount();i++)
        {
            var button=roster.GetChild<Button>(i);
            button.FocusNeighborRight=button.GetPathTo(ordered[0]);
            button.FocusNeighborTop=button.GetPathTo(roster.GetChild((i+roster.GetChildCount()-1)%roster.GetChildCount()));
            button.FocusNeighborBottom=button.GetPathTo(roster.GetChild((i+1)%roster.GetChildCount()));
        }
    }

	/// <summary>
	/// Localizes the UI text based on the current language settings.
	/// </summary>
	private void TranslateUI()
	{
		var header = GetNodeOrNull<Label>("Header");
		if (header != null) header.Text = Tr("SQUAD ROSTER");

		var listTitle = GetNodeOrNull<Label>("MainLayout/LeftListPanel/VBox/ListTitle");
		if (listTitle != null) listTitle.Text = $"{Tr("OPERATIVES")}  {_roster.Unlocked.Count}/{OperativeRoster.RosterOrder.Count} {Tr("UNLOCKED")}";

		var shogunBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun");
		if (shogunBtn != null) shogunBtn.Text = $"{Tr("Cyber Shogun")} [{Tr("Kaito Kurogane")}]";

		var oceanusBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus");
		if (oceanusBtn != null) oceanusBtn.Text = $"{Tr("Oceanus")} [{Tr("Kiana Vaea")}]";

		var suryaPrimeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime");
		if (suryaPrimeBtn != null) suryaPrimeBtn.Text = $"{Tr("Surya Prime")} [{Tr("Arjun Varma")}]";

		var mirageBladeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade");
		if (mirageBladeBtn != null) mirageBladeBtn.Text = $"{Tr("Mirage Blade")} [{Tr("Zayd Al-Mansoor")}]";

		var apexTitanBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan");
		if (apexTitanBtn != null) apexTitanBtn.Text = $"{Tr("Apex Titan")} [{Tr("King Tariq Nzinga")}]";

		var phoeniciaBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Phoenicia");
		if (phoeniciaBtn != null) phoeniciaBtn.Text = $"{Tr("Aura Phoenicia")} [{Tr("Maya Khoury")}]";

		var deployBtn = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/Footer/DeployButton");
		if (deployBtn != null) deployBtn.Text = Tr("SET ACTIVE");

		if (_abilitiesHeaderLabel != null)
			_abilitiesHeaderLabel.Text = Tr("OPERATIVE TACTICAL LOADOUT");
	}

	/// <summary>
	/// Resolves the merged dossier controls and connects the existing upgrade screen.
	/// </summary>
	private void EnsureUIReferences()
	{
		if (_selectedNameLabel != null) return;
		_selectedNameLabel    = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/SelectedName");
		_civilianNameLabel    = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/CivilianName");
		_classTagLabel        = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/ClassTag");
		_previewSprite        = GetNodeOrNull<AnimatedSprite2D>("MainLayout/RightShowcasePanel/VBox/PreviewFrame/PreviewSprite");
		_speedMeter           = GetNodeOrNull<SegmentedStatBar>("MainLayout/RightShowcasePanel/VBox/StatsBox/SpeedMeter");
		_agilityMeter         = GetNodeOrNull<SegmentedStatBar>("MainLayout/RightShowcasePanel/VBox/StatsBox/AgilityMeter");

		_abilitiesHeaderLabel = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilitiesHeader");
		_primaryAbilityBtn    = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/PrimaryAbilityBtn");
		_secondaryAbilityBtn  = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/SecondaryAbilityBtn");
		_dashAbilityBtn       = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/DashAbilityBtn");
		_slamAbilityBtn       = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/SlamAbilityBtn");
		_abilityBadgeLabel    = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityBadgeLabel");
		_abilityDescLabel     = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityDescLabel");

		_abilityStatusLabel = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityStatusLabel");

		_operativeCardImage   = GetNodeOrNull<TextureRect>("MainLayout/RightShowcasePanel/VBox/CardImage");
		_cardHeaderLabel      = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/CardHeader");
		_cardStatusLabel      = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/StatusFooter");

		_mainLayout           = GetNodeOrNull<Control>("MainLayout");
		_upgradeOperativeBtn  = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/Footer/UpgradeOperativeBtn");
		_upgradeScreen        = GetNodeOrNull<OperativeUpgradeScreen>("OperativeUpgradeScreen");


		if (_upgradeScreen != null)
		{
			_upgradeScreen.Closed += OnUpgradeScreenClosed;
			_upgradeScreen.UpgradesChanged += OnUpgradesChanged;
		}
	}

	/// <summary>Uses the original English tactical styling and preserves translated casing, including Turkish dotted I.</summary>
	private static string SafeToUpper(string text) => WildfireText.EnglishUppercase(text ?? "");
	public void _on_cyber_shogun_pressed()  => SelectOperative("Cyber Shogun");
	public void _on_oceanus_pressed()       => SelectOperative("Oceanus");
	public void _on_surya_prime_pressed()    => SelectOperative("Surya Prime");
	public void _on_mirage_blade_pressed()   => SelectOperative("Mirage Blade");
	public void _on_apex_titan_pressed()     => SelectOperative("Apex Titan");
	public void _on_phoenicia_pressed()      => SelectOperative("Aura Phoenicia");
	public void _on_primary_ability_pressed()   => PreviewAbility(0);
	public void _on_secondary_ability_pressed() => PreviewAbility(1);
	public void _on_dash_ability_pressed()      => PreviewAbility(2);
	public void _on_slam_ability_pressed()      => PreviewAbility(3);
	public void _on_upgrade_operative_pressed()
	{
		if (_upgradeScreen == null)
		{
			_upgradeScreen = GetNodeOrNull<OperativeUpgradeScreen>("OperativeUpgradeScreen");
		}
		if (_upgradeScreen == null) return;

		if (_mainLayout != null) _mainLayout.Visible = false;
		_upgradeScreen.OpenForOperative(_currentSelectedCharacter);
	}

	private void OnUpgradeScreenClosed()
	{
		if (_mainLayout != null) _mainLayout.Visible = true;
		SelectOperative(_currentSelectedCharacter);
		_upgradeOperativeBtn?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void OnUpgradesChanged(string opName)
	{
		UpdateUpgradeButtonText();
		SelectOperative(_currentSelectedCharacter);
	}

	/// <summary>Refreshes the upgrade cost indicator without changing progression or spending sparks.</summary>
	private void UpdateUpgradeButtonText()
	{
		if (_upgradeOperativeBtn == null) return;
		int lvl = 1;
		if (CharacterProgressionManager.Instance != null)
		{
			var data = CharacterProgressionManager.Instance.GetOperativeData(_currentSelectedCharacter);
			lvl = data.Level;
			bool canLevel = CharacterProgressionManager.Instance.CanLevelUp(_currentSelectedCharacter);
			_upgradeOperativeBtn.Text = canLevel
				? WildfireText.Interpolate("UI_CHARACTERSELECTION_UPDATEUPGRADEBUTTONTEXT_7270D91F", $"⚡ UPGRADE OPERATIVE [LVL {lvl}] (!)")
				: WildfireText.Interpolate("UI_CHARACTERSELECTION_UPDATEUPGRADEBUTTONTEXT_EE25105F", $"⚡ UPGRADE OPERATIVE [LVL {lvl}]");
		}
		else
		{
			WildfireText.SetText(_upgradeOperativeBtn, "Text", "UI_CHARACTERSELECTION_UPDATEUPGRADEBUTTONTEXT_EE25105F", $"⚡ UPGRADE OPERATIVE [LVL {lvl}]");
		}
	}

	/// <summary>
	/// Updates the UI and operative card when a new character is selected.
	/// </summary>
	public void SelectOperative(string characterName)
	{
		EnsureUIReferences();
		_currentSelectedCharacter = characterName;
		var centerPanel = GetNodeOrNull<Control>("MainLayout/RightShowcasePanel");
		if (centerPanel != null)
			centerPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		if (_artStudy != null) _artStudy.Visible = true;
		if (_operativeCardImage != null) _operativeCardImage.Visible = _artStudy == null;
		ConfigureArtStudy(characterName);

		float baseSpeed = 98f;
		float baseAgility = 96f;

		// Update labels and stat meters based on the selected operative.
		if (characterName == "Oceanus")
		{
			baseSpeed = 96f;
			baseAgility = 97f;
			if (_selectedNameLabel != null) BindSchemaText(_selectedNameLabel, "Oceanus", true);
			if (_civilianNameLabel != null) BindSchemaText(_civilianNameLabel, "IDENTITY: KIANA VAEA", true);
			if (_classTagLabel != null)     BindSchemaText(_classTagLabel, "// CLASS: HYDROKINETIC PLASMA MANIPULATOR");
		}
		else if (characterName == "Surya Prime")
		{
			baseSpeed = 97f;
			baseAgility = 98f;
			if (_selectedNameLabel != null) BindSchemaText(_selectedNameLabel, "Surya Prime", true);
			if (_civilianNameLabel != null) BindSchemaText(_civilianNameLabel, "IDENTITY: ARJUN VARMA", true);
			if (_classTagLabel != null)     BindSchemaText(_classTagLabel, "// CLASS: COSMIC SOLAR VANGUARD");
		}
		else if (characterName == "Mirage Blade")
		{
			baseSpeed = 99f;
			baseAgility = 99f;
			if (_selectedNameLabel != null) BindSchemaText(_selectedNameLabel, "Mirage Blade", true);
			if (_civilianNameLabel != null) BindSchemaText(_civilianNameLabel, "IDENTITY: ZAYD AL-MANSOOR", true);
			if (_classTagLabel != null)     BindSchemaText(_classTagLabel, "// CLASS: CRYSTALLINE PHANTOM");
		}
		else if (characterName == "Apex Titan")
		{
			baseSpeed = 93f;
			baseAgility = 94f;
			if (_selectedNameLabel != null) BindSchemaText(_selectedNameLabel, "Apex Titan", true);
			if (_civilianNameLabel != null) BindSchemaText(_civilianNameLabel, "IDENTITY: KING TARIQ NZINGA", true);
			if (_classTagLabel != null)     BindSchemaText(_classTagLabel, "// CLASS: ELECTRO-MAGNETIC STORM VANGUARD");
		}
		else if (characterName == "Aura Phoenicia" || characterName == "Phoenicia")
		{
			baseSpeed = 96f;
			baseAgility = 98f;
			if (_selectedNameLabel != null) BindSchemaText(_selectedNameLabel, "Aura Phoenicia", true);
			if (_civilianNameLabel != null) BindSchemaText(_civilianNameLabel, "IDENTITY: MAYA KHOURY", true);
			if (_classTagLabel != null)     BindSchemaText(_classTagLabel, "// CLASS: ULTRASONIC PRISM WEAVER");
		}
		else
		{
			baseSpeed = 98f;
			baseAgility = 96f;
			if (_selectedNameLabel != null) BindSchemaText(_selectedNameLabel, "Cyber Shogun", true);
			if (_civilianNameLabel != null) BindSchemaText(_civilianNameLabel, "IDENTITY: KAITO KUROGANE", true);
			if (_classTagLabel != null)     BindSchemaText(_classTagLabel, "// CLASS: CYBERNETIC SHOGUN WARRIOR");
		}

		if (CharacterProgressionManager.Instance != null)
		{
			var mods = CharacterProgressionManager.Instance.GetEffectiveModifiers(characterName);
			baseSpeed = Mathf.Clamp(baseSpeed * (1.0f + mods.speedBonusPct), 0f, 100f);
			baseAgility = Mathf.Clamp(baseAgility * (1.0f + mods.speedBonusPct * 0.5f), 0f, 100f);
		}

		// The custom meters translate during drawing and must retain their source identities.
		_speedMeter?.SetStat("SPEED", baseSpeed);
		_agilityMeter?.SetStat("AGILITY", baseAgility);
		UpdateUpgradeButtonText();
		UpdateRosterButtons();

		// Update ability preview buttons and details
		var abilities = GetOperativeAbilities(characterName);
		BindAbilityButton(_primaryAbilityBtn, "⚡", abilities, 0);
		BindAbilityButton(_secondaryAbilityBtn, "🛡️", abilities, 1);
		BindAbilityButton(_dashAbilityBtn, "💨", abilities, 2);
		BindAbilityButton(_slamAbilityBtn, "💥", abilities, 3);

		if (abilities.Length > 0)
		{
			UpdateAbilityDisplay(abilities[0], isPreviewing: false);
		}

		// Update 2D High-Fidelity Operative Card
		string cardPath = OperativeVisuals.PortraitPath(characterName);
		if (_operativeCardImage != null)
		{
			_operativeCardImage.Texture = LoadOperativeTexture(cardPath);
		}
		if (_cardStatusLabel != null)
		{
			BindSchemaText(_cardStatusLabel, "● 2D TACTICAL OPERATIVE READY");
			_cardStatusLabel.Modulate = new Color(0.2f, 0.9f, 0.5f, 0.85f);
		}

		FixFontForLocale(this);
	}

	/// <summary>Routes a tactical-loadout button into the same art preview as the center controls.</summary>
	public void PreviewAbility(int abilityIndex)
	{
		var abilities = GetOperativeAbilities(_currentSelectedCharacter);
		if (abilityIndex < 0 || abilityIndex >= abilities.Length) return;

		var ability = abilities[abilityIndex];
		UpdateAbilityDisplay(ability, isPreviewing: true);
		if (_artPreview != null)
		{
			PreviewStudyAction(ability.Name);
			return;
		}

		// Pulse 2D Operative Card with holographic tactical flash
		if (_operativeCardImage != null && IsInsideTree())
		{
			var tween = CreateTween();
			_operativeCardImage.Modulate = new Color(1.3f, 1.3f, 1.6f, 1f);
			tween?.TweenProperty(_operativeCardImage, "modulate", new Color(1f, 1f, 1f, 1f), 0.35f);
		}
		if (_cardStatusLabel != null)
		{
			WildfireText.SetText(_cardStatusLabel, "Text", "UI_CHARACTERSELECTION_PREVIEWABILITY_89B104C4", $"▶ EXECUTING 2D TACTICAL: {WildfireText.DisplayValue(ability.Name, englishUppercase: true)}");
			_cardStatusLabel.Modulate = new Color(0f, 0.94f, 1f, 1f);
		}

	}

	/// <summary>Shows schema-owned ability fields as refreshable translations without changing preview actions or animation IDs.</summary>
	private void UpdateAbilityDisplay(OperativeAbilityData ability, bool isPreviewing)
	{
		if (_abilityBadgeLabel != null)
		{
			object damage = ability.Damage > 0
				? WildfireText.TemplateValue("ABILITY_DAMAGE", $"DMG: {ability.Damage:F0}")
				: WildfireText.KeyValue("ABILITY_TACTICAL", "TACTICAL");
			WildfireText.SetText(_abilityBadgeLabel, "Text", "UI_CHARACTERSELECTION_UPDATEABILITYDISPLAY_F0B3BF3A", $"[{AbilityCategoryText(ability.Category)}] {WildfireText.DisplayValue(ability.Name, englishUppercase: true)} • CD: {ability.Cooldown:F1}s • {damage}");
			_abilityBadgeLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		}
		if (_abilityDescLabel != null)
		{
			BindSchemaText(_abilityDescLabel, ability.Description);
		}
		if (_abilityStatusLabel != null)
		{
			if (isPreviewing)
			{
				WildfireText.SetText(_abilityStatusLabel, "Text", "UI_CHARACTERSELECTION_UPDATEABILITYDISPLAY_8ADF05DF", $"▶ PREVIEWING: {WildfireText.DisplayValue(ability.Name, englishUppercase: true)}...");
				_abilityStatusLabel.Modulate = new Color(0f, 0.94f, 1f, 1f);
			}
			else
			{
				BindSchemaText(_abilityStatusLabel, "✔ READY // SELECT ABILITY TO PREVIEW");
				_abilityStatusLabel.Modulate = new Color(0.2f, 1f, 0.5f, 0.9f);
			}
		}
	}

	/// <summary>Binds schema-owned text while retaining the English source for future locale changes.</summary>
	private static void BindSchemaText(Label? label, string source, bool englishUppercase = false)
	{
		if (label != null) WildfireText.SetText(label, "Text", "UI_SCHEMA_TEXT", $"{WildfireText.DisplayValue(source, englishUppercase)}");
	}

	/// <summary>Keeps a loadout button's icon and canonical ability name separate from its translated display.</summary>
	private static void BindAbilityButton(Button? button, string icon, OperativeAbilityData[] abilities, int index)
	{
		if (button != null && index < abilities.Length)
			WildfireText.SetText(button, "Text", "UI_DISPLAY_PAIR", $"{icon} {WildfireText.DisplayValue(abilities[index].Name, englishUppercase: true)}");
	}

	/// <summary>Maps stable ability-category IDs to semantic display keys; these IDs still select the same gameplay slots.</summary>
	private static object AbilityCategoryText(string category) => category switch
	{
		"PRIMARY" => WildfireText.KeyValue("ABILITY_CATEGORY_PRIMARY", "PRIMARY"),
		"SECONDARY" => WildfireText.KeyValue("ABILITY_CATEGORY_SECONDARY", "SECONDARY"),
		"MOBILITY" => WildfireText.KeyValue("ABILITY_CATEGORY_MOBILITY", "MOBILITY"),
		"SLAM" => WildfireText.KeyValue("ABILITY_CATEGORY_SLAM", "SLAM"),
		_ => WildfireText.DisplayValue(category)
	};

	/// <summary>Translates movement captions while preserving their stable animation dispatch names.</summary>
	private static object MovementDisplay(string action) => action switch
	{
		"Idle" => WildfireText.KeyValue("PREVIEW_IDLE", "Idle"),
		"Walk" => WildfireText.KeyValue("PREVIEW_WALK", "Walk"),
		"Run" => WildfireText.KeyValue("PREVIEW_RUN", "Run"),
		"Katana" => WildfireText.KeyValue("PREVIEW_KATANA", "Katana"),
		_ => WildfireText.DisplayValue(action)
	};

	private static OperativeAbilityData[] GetOperativeAbilities(string characterName)
	{
		return characterName switch
		{
			"Oceanus" => new[]
			{
				new OperativeAbilityData("Tidal Torrent", "Torrent_Blast", "PRIMARY", "Hydro Blast", 4.0f, 28f,
					"High-pressure hydrokinetic plasma surge propelling hostiles backward."),
				new OperativeAbilityData("Maelstrom Vortex", "Vortex_Cast", "SECONDARY", "Gravity Vortex", 6.0f, 20f,
					"Swirling aquatic gravity well pulling enemies toward the center."),
				new OperativeAbilityData("Aqua Jet", "Dash", "MOBILITY", "Hydro Slide", 2.5f, 0f,
					"Frictionless hydro-propulsion slide granting rapid evasion."),
				new OperativeAbilityData("Tidal Crash", "Ground_Slam", "SLAM", "Hydrostatic Crash", 5.0f, 30f,
					"Crashing downward drop releasing an expanding shockwave of water pressure.")
			},
			"Surya Prime" => new[]
			{
				new OperativeAbilityData("Solar Shield Bash", "Shield_Bash", "PRIMARY", "Vanguard Rush", 4.5f, 35f,
					"Heavy armored charge ramming enemies with reinforced solar bulwark."),
				new OperativeAbilityData("Aegis of the Sun", "Shield_Guard", "SECONDARY", "Hardlight Barrier", 5.5f, 15f,
					"Cosmic barrier absorbing incoming fire and shielding the operative."),
				new OperativeAbilityData("Vanguard Dash", "Dash", "MOBILITY", "Powered Sprint", 2.5f, 0f,
					"Exoskeleton-assisted burst breaking through enemy formations."),
				new OperativeAbilityData("Cosmic Slam", "Ground_Slam", "SLAM", "Bulwark Stomp", 5.0f, 35f,
					"Fortress-class seismic stomp destabilizing the ground beneath foes.")
			},
			"Mirage Blade" => new[]
			{
				new OperativeAbilityData("Phase Blink", "Phase_Dash", "PRIMARY", "Teleport Slash", 3.5f, 25f,
					"Dimensional phase shift instantly blinking behind the target."),
				new OperativeAbilityData("Holographic Decoy", "Decoy_Deploy", "SECONDARY", "Decoy Illusion", 6.0f, 15f,
					"Deploys a crystalline holographic clone attracting enemy fire before bursting."),
				new OperativeAbilityData("Phase Dash", "Dash", "MOBILITY", "Phase Slip", 2.5f, 0f,
					"Semi-material phase slide slipping through hostile fire unharmed."),
				new OperativeAbilityData("Mirage Drop", "Ground_Slam", "SLAM", "Crystalline Dive", 5.0f, 28f,
					"Acrobatic plunge creating a resonating crystalline shockwave.")
			},
			"Apex Titan" => new[]
			{
				new OperativeAbilityData("Tectonic Slam", "Tectonic_Slam", "PRIMARY", "Seismic Crater", 4.5f, 45f,
					"Shatters the ground with devastating seismic force, launching targets."),
				new OperativeAbilityData("Magnetic Tether", "Magnetic_Pull", "SECONDARY", "Magnetic Pull", 5.5f, 25f,
					"Electromagnetic pulse dragging distant enemies into crushing range."),
				new OperativeAbilityData("Titan Surge", "Dash", "MOBILITY", "Rocket Charge", 2.5f, 0f,
					"Heavy rocket-boosted charge plowing past obstacles."),
				new OperativeAbilityData("Seismic Drop", "Ground_Slam", "SLAM", "Gravitational Drop", 5.0f, 40f,
					"Gravitational drop generating destructive radial shockwaves.")
			},
			"Aura Phoenicia" or "Phoenicia" => new[]
			{
				new OperativeAbilityData("Harmonic Pulse", "Harmonic_Pulse", "PRIMARY", "Acoustic Shockwave", 4.0f, 32f,
					"Emits a high-frequency ultrasonic shockwave that staggers targets and deflects incoming energy."),
				new OperativeAbilityData("Prism Reverberation", "Prism_Reverberation", "SECONDARY", "Resonant Field", 5.5f, 22f,
					"Deploys a shimmering prismatic acoustic field dealing resonant periodic damage."),
				new OperativeAbilityData("Wave Glide", "Dash", "MOBILITY", "Harmonic Glide", 2.5f, 0f,
					"Surges forward on a wave of sound and light for evasive repositioning."),
				new OperativeAbilityData("Cedars' Echo", "Ground_Slam", "SLAM", "Resonant Impact", 5.0f, 32f,
					"Acoustic downward strike sending concentric sonic rings reverberating outward.")
			},
			_ => new[] // Cyber Shogun
			{
				new OperativeAbilityData("Shogun's Flash", "Blade_Dash", "PRIMARY", "Dash / Melee", 4.0f, 35f,
					"High-speed katana dash cutting through enemies with nanite plasma edge."),
				new OperativeAbilityData("Kinetic Parry", "Parry_Stance", "SECONDARY", "Deflect / Stance", 5.5f, 20f,
					"Raises dual blade matrix absorbing attacks and counter-blasting shockwaves."),
				new OperativeAbilityData("Cyber Dash", "Dash", "MOBILITY", "Thruster Burst", 2.5f, 0f,
					"Directional thruster burst for rapid evasive repositioning."),
				new OperativeAbilityData("Ground Slam", "Ground_Slam", "SLAM", "Kinetic Impact", 5.0f, 30f,
					"Downward kinetic drop creating an area shockwave on impact.")
			}
		};
	}

	public class OperativeAbilityData
	{
		public string Name { get; set; }
		public string AnimName { get; set; }
		public string Category { get; set; }
		public string TypeName { get; set; }
		public float Cooldown { get; set; }
		public float Damage { get; set; }
		public string Description { get; set; }

		public OperativeAbilityData(string name, string animName, string category, string typeName, float cooldown, float damage, string description)
		{
			Name = name;
			AnimName = animName;
			Category = category;
			TypeName = typeName;
			Cooldown = cooldown;
			Damage = damage;
			Description = description;
		}
	}

	/// <summary>
	/// "Set Active": makes the highlighted operative the one fielded in the next mission, then returns to the main menu.
	/// Locked operatives are refused. Missions rebuild the squad from progress and honour this choice if it is unlocked.
	/// </summary>
	private void _on_deploy_pressed()
	{
		if (!_roster.SetActive(_currentSelectedCharacter)) return;
		GetNodeOrNull<Node>("/root/Global")?.Set("selected_character", _roster.Active);
		ReturnToMenu();
	}

	/// <summary>Leaves the roster for the main menu.</summary>
	private void ReturnToMenu() => LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/main_menu.tscn");

	/// <summary>Mission number in which an operative joins the squad, or 0 for the starter.</summary>
	private static int JoinMission(string operative)
	{
		string normalized = OperativeRoster.Normalize(operative);
		foreach (var level in LevelCatalog.All)
			foreach (string granted in level.GrantsOperatives)
				if (OperativeRoster.Normalize(granted) == normalized) return level.Number;
		return 0;
	}

	/// <summary>
	/// Marks locked operatives in the roster grid ("LOCKED") and turns the footer button into "Set Active" for unlocked
	/// operatives or a disabled "Locked - joins in Mission N" for locked ones.
	/// </summary>
	private void UpdateRosterButtons()
	{
		foreach (var (path, name) in new[] {
			("CyberShogun", "Cyber Shogun"), ("Oceanus", "Oceanus"), ("SuryaPrime", "Surya Prime"),
			("MirageBlade", "Mirage Blade"), ("ApexTitan", "Apex Titan"), ("Phoenicia", "Aura Phoenicia") })
		{
			var button = GetNodeOrNull<Button>($"MainLayout/LeftListPanel/VBox/CharacterGrid/{path}");
			if (button == null) continue;
			bool unlocked = _roster.IsUnlocked(name);
			string baseText = button.Text.Replace($"  {Tr("LOCKED")}", "");
			button.Text = unlocked ? baseText : $"{baseText}  {Tr("LOCKED")}";
			button.Modulate = unlocked ? Colors.White : new Color(.6f, .6f, .7f, .85f);
		}
		var deploy = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/Footer/DeployButton");
		if (deploy == null) return;
		bool selectedUnlocked = _roster.IsUnlocked(_currentSelectedCharacter);
		deploy.Disabled = !selectedUnlocked;
		deploy.Text = selectedUnlocked
			? Tr("SET ACTIVE")
			: $"{Tr("LOCKED")} - {Tr("JOINS IN MISSION")} {JoinMission(_currentSelectedCharacter)}";
	}

	private static void AddAnimationFrames(
		SpriteFrames frames, string animName, Texture2D texture,
		int frameCount, float speed)
	{
		frames.AddAnimation(animName);
		frames.SetAnimationLoop(animName, true);
		frames.SetAnimationSpeed(animName, speed);

		for (int i = 0; i < frameCount; i++)
		{
			var atlas = new AtlasTexture { Atlas = texture };
			atlas.Region = new Rect2(i * 32, 0, 32, 32);
			frames.AddFrame(animName, atlas);
		}
	}

	/// <summary>
	/// Loads high-fidelity 2D operative card texture from resource path with filesystem fallback.
	/// </summary>
	private static Texture2D? LoadOperativeTexture(string resPath)
	{
		if (ResourceLoader.Exists(resPath))
		{
			var tex = GD.Load<Texture2D>(resPath);
			if (tex != null) return tex;
		}

		string globalPath = ProjectSettings.GlobalizePath(resPath);
		if (System.IO.File.Exists(globalPath))
		{
			var img = Image.LoadFromFile(globalPath);
			if (img != null)
			{
				return ImageTexture.CreateFromImage(img);
			}
		}

		return null;
	}
}
