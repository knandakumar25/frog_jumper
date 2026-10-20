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

		TranslateUI();
		SelectOperative("Cyber Shogun");
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
		// Completely eliminate mouse cursor compatibility
		if (@event is InputEventMouse)
		{
			GetViewport()?.SetInputAsHandled();
			return;
		}
	}

	private static void FixFontForLocale(Node node)
	{
		// Native Orbitron & Rajdhani sci-fi fonts handle Arabic via Cairo font fallbacks configured in global.gd.
	}

	/// <summary>Builds the shared, keyboard-accessible animation study without changing campaign controls.</summary>
	private void BuildArtStudy()
	{
		var center = GetNodeOrNull<VBoxContainer>("MainLayout/CenterShowcasePanel/VBox");
		if (center == null || _artStudy != null) return;
		_artStudy = new VBoxContainer { Name = "ShogunArtStudy", SizeFlagsVertical = SizeFlags.ExpandFill };
		center.AddChild(_artStudy);
		center.MoveChild(_artStudy, 1);
		_artPreview = new ShogunArtPreview {
			Name = "ArtPreview", CustomMinimumSize = new Vector2(0, 430),
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_artStudy.AddChild(_artPreview);
		_artStatus = new Label {
			Text = "Idle • animation study", HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_artStatus.AddThemeFontSizeOverride("font_size", 16);
		_artStudy.AddChild(_artStatus);
		var grid = new GridContainer { Name = "Actions", Columns = 2 };
		_artStudy.AddChild(grid);
		foreach (string action in new[] { "Idle", "Walk", "Run", "Jump", "Double jump", "Katana",
			"Martial strike", "Shogun's Flash", "Kinetic Parry", "Cyber Dash", "Ground Slam" })
		{
			var button = new Button {
				Text = action, CustomMinimumSize = new Vector2(0, 34),
				SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore
			};
			button.AddThemeFontSizeOverride("font_size", 15);
			button.Pressed += () => PreviewStudyAction(button.Text);
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
			Text = "Arrows / Tab: choose • Enter: preview\nArt test only • campaign unchanged",
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
			_artStatus.Text = action + (abilityPreview ? " • visual ability preview" : " • animation study");
	}

	/// <summary>Refreshes art, loadout labels, and return focus when selecting an operative.</summary>
	private void ConfigureArtStudy(string characterName)
	{
		if (_artPreview == null) return;
		_artPreview.SetOperative(characterName);
		var abilities = GetOperativeAbilities(characterName);
		for (int i = 0; i < 4; i++) _artButtons[i + 7].Text = abilities[i].Name;
		_artButtons[5].Visible = _artButtons[6].Visible = characterName == "Cyber Shogun";
		string rosterNode = OperativeRegistry.GetProfile(characterName).Id switch {
			"surya_prime" => "SuryaPrime", "oceanus" => "Oceanus", "mirage_blade" => "MirageBlade",
			"apex_titan" => "ApexTitan", "phoenicia" => "Phoenicia", _ => "CyberShogun"
		};
		var selectedButton = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/" + rosterNode);
		if (selectedButton != null)
			foreach (var button in _artButtons) button.FocusNeighborLeft = button.GetPathTo(selectedButton);
		if (_artStatus != null) _artStatus.Text = "Idle • animation study";
	}

	/// <summary>
	/// Localizes the UI text based on the current language settings.
	/// </summary>
	private void TranslateUI()
	{
		var header = GetNodeOrNull<Label>("Header");
		if (header != null) header.Text = Tr("OPERATIVE SELECTION MATRIX");

		var listTitle = GetNodeOrNull<Label>("MainLayout/LeftListPanel/VBox/ListTitle");
		if (listTitle != null) listTitle.Text = Tr("AVAILABLE OPERATIVES");

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

		var deployBtn = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/DeployButton");
		if (deployBtn != null) deployBtn.Text = Tr("DEPLOY OPERATIVE");

		if (_abilitiesHeaderLabel != null)
			_abilitiesHeaderLabel.Text = Tr("OPERATIVE TACTICAL LOADOUT");
	}

	/// <summary>
	/// Converts text to uppercase, but avoids doing so for scripts (Arabic, Hebrew, CJK) where uppercase doesn't exist.
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

		_operativeCardImage   = GetNodeOrNull<TextureRect>("MainLayout/CenterShowcasePanel/VBox/CardImage");
		_cardHeaderLabel      = GetNodeOrNull<Label>("MainLayout/CenterShowcasePanel/VBox/CardHeader");
		_cardStatusLabel      = GetNodeOrNull<Label>("MainLayout/CenterShowcasePanel/VBox/StatusFooter");

		_mainLayout           = GetNodeOrNull<Control>("MainLayout");
		_upgradeOperativeBtn  = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/UpgradeOperativeBtn");
		_upgradeScreen        = GetNodeOrNull<OperativeUpgradeScreen>("OperativeUpgradeScreen");


		if (_upgradeScreen != null)
		{
			_upgradeScreen.Closed += OnUpgradeScreenClosed;
			_upgradeScreen.UpgradesChanged += OnUpgradesChanged;
		}
	}

	/// <summary>
	/// Converts text to uppercase, but avoids doing so for scripts (Arabic, Hebrew, CJK) where uppercase doesn't exist.
	/// </summary>
	private static string SafeToUpper(string text)
	{
		if (string.IsNullOrEmpty(text)) return "";
		string locale = TranslationServer.GetLocale()?.ToLower() ?? "";
		if (locale.Contains("ar") || locale.Contains("he") || locale.Contains("ja") ||
			locale.Contains("ko") || locale.Contains("zh"))
		{
			return text;
		}
		return text.ToUpper();
	}

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
				? $"⚡ UPGRADE OPERATIVE [LVL {lvl}] (!)"
				: $"⚡ UPGRADE OPERATIVE [LVL {lvl}]";
		}
		else
		{
			_upgradeOperativeBtn.Text = $"⚡ UPGRADE OPERATIVE [LVL {lvl}]";
		}
	}

	/// <summary>
	/// Updates the UI and operative card when a new character is selected.
	/// </summary>
	public void SelectOperative(string characterName)
	{
		EnsureUIReferences();
		_currentSelectedCharacter = characterName;
		var centerPanel = GetNodeOrNull<Control>("MainLayout/CenterShowcasePanel");
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
			if (_selectedNameLabel != null) _selectedNameLabel.Text = SafeToUpper(Tr("Oceanus"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: KIANA VAEA"));
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: HYDROKINETIC PLASMA MANIPULATOR");
		}
		else if (characterName == "Surya Prime")
		{
			baseSpeed = 97f;
			baseAgility = 98f;
			if (_selectedNameLabel != null) _selectedNameLabel.Text = SafeToUpper(Tr("Surya Prime"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: ARJUN VARMA"));
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: COSMIC SOLAR VANGUARD");
		}
		else if (characterName == "Mirage Blade")
		{
			baseSpeed = 99f;
			baseAgility = 99f;
			if (_selectedNameLabel != null) _selectedNameLabel.Text = SafeToUpper(Tr("Mirage Blade"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: ZAYD AL-MANSOOR"));
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: CRYSTALLINE PHANTOM");
		}
		else if (characterName == "Apex Titan")
		{
			baseSpeed = 93f;
			baseAgility = 94f;
			if (_selectedNameLabel != null) _selectedNameLabel.Text = SafeToUpper(Tr("Apex Titan"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: KING TARIQ NZINGA"));
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: ELECTRO-MAGNETIC STORM VANGUARD");
		}
		else if (characterName == "Aura Phoenicia" || characterName == "Phoenicia")
		{
			baseSpeed = 96f;
			baseAgility = 98f;
			if (_selectedNameLabel != null) _selectedNameLabel.Text = SafeToUpper(Tr("Aura Phoenicia"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: MAYA KHOURY"));
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: ULTRASONIC PRISM WEAVER");
		}
		else
		{
			baseSpeed = 98f;
			baseAgility = 96f;
			if (_selectedNameLabel != null) _selectedNameLabel.Text = SafeToUpper(Tr("Cyber Shogun"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: KAITO KUROGANE"));
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: CYBERNETIC SHOGUN WARRIOR");
		}

		if (CharacterProgressionManager.Instance != null)
		{
			var mods = CharacterProgressionManager.Instance.GetEffectiveModifiers(characterName);
			baseSpeed = Mathf.Clamp(baseSpeed * (1.0f + mods.speedBonusPct), 0f, 100f);
			baseAgility = Mathf.Clamp(baseAgility * (1.0f + mods.speedBonusPct * 0.5f), 0f, 100f);
		}

		_speedMeter?.SetStat(Tr("SPEED"), baseSpeed);
		_agilityMeter?.SetStat(Tr("AGILITY"), baseAgility);
		UpdateUpgradeButtonText();

		// Update ability preview buttons and details
		var abilities = GetOperativeAbilities(characterName);
		if (_primaryAbilityBtn != null && abilities.Length > 0)
			_primaryAbilityBtn.Text = $"⚡ {SafeToUpper(Tr(abilities[0].Name))}";
		if (_secondaryAbilityBtn != null && abilities.Length > 1)
			_secondaryAbilityBtn.Text = $"🛡️ {SafeToUpper(Tr(abilities[1].Name))}";
		if (_dashAbilityBtn != null && abilities.Length > 2)
			_dashAbilityBtn.Text = $"💨 {SafeToUpper(Tr(abilities[2].Name))}";
		if (_slamAbilityBtn != null && abilities.Length > 3)
			_slamAbilityBtn.Text = $"💥 {SafeToUpper(Tr(abilities[3].Name))}";

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
			_cardStatusLabel.Text = Tr("● 2D TACTICAL OPERATIVE READY");
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
			_cardStatusLabel.Text = $"▶ EXECUTING 2D TACTICAL: {SafeToUpper(Tr(ability.Name))}";
			_cardStatusLabel.Modulate = new Color(0f, 0.94f, 1f, 1f);
		}

	}

	private void UpdateAbilityDisplay(OperativeAbilityData ability, bool isPreviewing)
	{
		if (_abilityBadgeLabel != null)
		{
			string dmgText = ability.Damage > 0 ? $"DMG: {ability.Damage:F0}" : "TACTICAL";
			_abilityBadgeLabel.Text = $"[{ability.Category}] {SafeToUpper(Tr(ability.Name))} • CD: {ability.Cooldown:F1}s • {dmgText}";
		}
		if (_abilityDescLabel != null)
		{
			_abilityDescLabel.Text = Tr(ability.Description);
		}
		if (_abilityStatusLabel != null)
		{
			if (isPreviewing)
			{
				_abilityStatusLabel.Text = $"▶ PREVIEWING: {SafeToUpper(Tr(ability.Name))}...";
				_abilityStatusLabel.Modulate = new Color(0f, 0.94f, 1f, 1f);
			}
			else
			{
				_abilityStatusLabel.Text = Tr("✔ READY // SELECT ABILITY TO PREVIEW");
				_abilityStatusLabel.Modulate = new Color(0.2f, 1f, 0.5f, 0.9f);
			}
		}
	}

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

	private void _on_deploy_pressed()
	{
		CreateSessionAndStart(_currentSelectedCharacter);
	}

	/// <summary>
	/// Finalizes the selection, creates a new session in SessionManager, and transitions to the story intro.
	/// </summary>
	private async void CreateSessionAndStart(string characterName)
	{
		if (GetTree()?.Root?.HasNode("Global") == true)
		{
			GetTree().Root.GetNodeOrNull<Node>("Global")?.Set("selected_character", characterName);
		}

		int sessionId = await SessionManager.Instance.CreateNewSessionAsync(characterName);
		if (sessionId > 0)
			GD.Print("Created new session: ", sessionId, " for character: ", characterName);
		else
			GD.Print("Error creating session!");

		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/story_intro.tscn");
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
