using Godot;
using System;

/// <summary>
/// CharacterSelection manages the operative selection screen.
/// It handles the UI for choosing a character, updating a live 3D preview showcase,
/// and initializing a new game session via the SessionManager.
///
/// Unity Migration Note: In Unity, the 3D preview would be implemented using a
/// separate Camera rendering to a Render Texture, which is then displayed on a
/// RawImage in the UI. Character selection and stats would be driven by a
/// ScriptableObject-based Character Database.
/// </summary>
public partial class CharacterSelection : Control
{
	// ─── Input bindings ──────────────────────────────────────────────────────
	private const Key          RotateLeftKey   = Key.Q;
	private const Key          RotateRightKey  = Key.E;
	private const JoyButton    RotateLeftBtn   = JoyButton.LeftShoulder;   // LB
	private const JoyButton    RotateRightBtn  = JoyButton.RightShoulder;  // RB
	private const float        RotationSpeed   = 90f; // degrees per second

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

	private Control? _mainLayout;
	private Button? _upgradeOperativeBtn;
	private OperativeUpgradeScreen? _upgradeScreen;

	private string _currentPreviewAnim = "";
	private Callable _previewFinishedCallable;

	private string _currentSelectedCharacter = "Cyber Shogun";

	public override void _Ready()
	{
		// Stop the main menu music to allow character-specific audio or silence.
		if (GetTree()?.Root?.HasNode("Global") == true)
		{
			GetTree().Root.GetNodeOrNull<Node>("Global")?.Call("stop_menu_music");
		}

		EnsureUIReferences();

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
		_previewFinishedCallable = Callable.From<StringName>(OnPreviewAnimationFinished);

		_mainLayout           = GetNodeOrNull<Control>("MainLayout");
		_upgradeOperativeBtn  = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/UpgradeOperativeBtn");
		_upgradeScreen        = GetNodeOrNull<OperativeUpgradeScreen>("OperativeUpgradeScreen");

		_world3D              = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");

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

	private Node3D? _world3D;
	private Node3D _cyberShogun3DModel;

	private Node3D _oceanus3DModel;
	private Node3D _suryaPrime3DModel;
	private Node3D _mirageBlade3DModel;
	private Node3D _apexTitan3DModel;
	private Node3D _phoenicia3DModel;
	private Node3D _platform3DModel;

	// User-controlled rotation (degrees, accumulated)
	private float _charRotationY  = 0f;
	private float _glowPulseTimer = 0f;
	private float _rotateInput    = 0f;  // -1 left, +1 right, 0 idle

	private static readonly Vector3 TargetPodiumPosition = new Vector3(-0.45f, 2.69f, 0f);

	// --- Arrow button signal handlers (hold-to-rotate) ---
	public void _on_rotate_left_down()  => _rotateInput = -1f;
	public void _on_rotate_left_up()    => _rotateInput =  0f;
	public void _on_rotate_right_down() => _rotateInput =  1f;
	public void _on_rotate_right_up()   => _rotateInput =  0f;

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
	/// Updates the UI and 3D preview when a new character is selected.
	/// </summary>
	public void SelectOperative(string characterName)
	{
		EnsureUIReferences();
		_currentSelectedCharacter = characterName;
		_currentPreviewAnim = "";

		var world3D = _world3D ?? GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D != null)
		{
			HeroAbilityVFX.ClearActiveVFX(world3D);
		}

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
			if (_classTagLabel != null)     _classTagLabel.Text     = Tr("// CLASS: 3D CYBERNETIC SHOGUN WARRIOR");
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

		FixFontForLocale(this);
		UpdatePreviewShowcase(_currentSelectedCharacter);
	}

	/// <summary>
	/// Manages the visibility of 3D models in the preview viewport.
	/// Loads GLB models on demand and manages their transforms.
	/// </summary>
	private void UpdatePreviewShowcase(string characterName)
	{
		var world3D = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D == null) return;

		// Lazy-load GLB models.
		if (_cyberShogun3DModel == null)
		{
			string glbPath = "res://assets/models/hq/cyber_shogun.glb";
			_cyberShogun3DModel = LoadGlbModel(glbPath);
			if (_cyberShogun3DModel != null)
			{
				_cyberShogun3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_cyberShogun3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_cyberShogun3DModel);
				PlayStandingAnimation(_cyberShogun3DModel, "Cyber Shogun");
			}
		}

		if (_oceanus3DModel == null)
		{
			string glbPath = "res://assets/models/hq/oceanus.glb";
			_oceanus3DModel = LoadGlbModel(glbPath);
			if (_oceanus3DModel != null)
			{
				_oceanus3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_oceanus3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_oceanus3DModel);
				PlayStandingAnimation(_oceanus3DModel, "Oceanus");
			}
		}

		if (_suryaPrime3DModel == null)
		{
			string glbPath = "res://assets/models/hq/surya_prime.glb";
			_suryaPrime3DModel = LoadGlbModel(glbPath);
			if (_suryaPrime3DModel != null)
			{
				_suryaPrime3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_suryaPrime3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_suryaPrime3DModel);
				PlayStandingAnimation(_suryaPrime3DModel, "Surya Prime");
			}
		}

		if (_mirageBlade3DModel == null)
		{
			string glbPath = "res://assets/models/hq/mirage_blade.glb";
			_mirageBlade3DModel = LoadGlbModel(glbPath);
			if (_mirageBlade3DModel != null)
			{
				_mirageBlade3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_mirageBlade3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_mirageBlade3DModel);
				PlayStandingAnimation(_mirageBlade3DModel, "Mirage Blade");
			}
		}

		if (_apexTitan3DModel == null)
		{
			string glbPath = "res://assets/models/hq/apex_titan.glb";
			_apexTitan3DModel = LoadGlbModel(glbPath);
			if (_apexTitan3DModel != null)
			{
				_apexTitan3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_apexTitan3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_apexTitan3DModel);
				PlayStandingAnimation(_apexTitan3DModel, "Apex Titan");
			}
		}

		if (_phoenicia3DModel == null)
		{
			string glbPath = "res://assets/models/hq/phoenicia.glb";
			_phoenicia3DModel = LoadGlbModel(glbPath);
			if (_phoenicia3DModel != null)
			{
				_phoenicia3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_phoenicia3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_phoenicia3DModel);
				PlayStandingAnimation(_phoenicia3DModel, "Aura Phoenicia");
			}
		}

		// Toggle visibility based on selection.
		if (_cyberShogun3DModel != null) _cyberShogun3DModel.Visible = (characterName == "Cyber Shogun");
		if (_oceanus3DModel != null) _oceanus3DModel.Visible = (characterName == "Oceanus");
		if (_suryaPrime3DModel != null) _suryaPrime3DModel.Visible = (characterName == "Surya Prime");
		if (_mirageBlade3DModel != null) _mirageBlade3DModel.Visible = (characterName == "Mirage Blade");
		if (_apexTitan3DModel != null) _apexTitan3DModel.Visible = (characterName == "Apex Titan");
		if (_phoenicia3DModel != null) _phoenicia3DModel.Visible = (characterName == "Aura Phoenicia" || characterName == "Phoenicia");

		// Ensure the platform is loaded and visible.
		if (_platform3DModel == null)
		{
			string platformPath = "res://assets/models/hq/Meshy_AI_scifi_selection_platf_0805042413_image-to-3d-texture.glb";
			_platform3DModel = LoadGlbModel(platformPath);
			if (_platform3DModel != null)
			{
				_platform3DModel.Scale    = new Vector3(0.55f, 0.55f, 0.55f);
				_platform3DModel.Position = new Vector3(-0.45f, 2.62f, 0f);
				world3D.AddChild(_platform3DModel);
			}
		}

		// Identify active model
		Node3D activeModel = GetActiveModel();

		if (activeModel != null && GodotObject.IsInstanceValid(activeModel))
		{
			activeModel.Position = TargetPodiumPosition;
			activeModel.RotationDegrees = new Vector3(0f, _charRotationY, 0f);
			PlayStandingAnimation(activeModel, _currentSelectedCharacter);
		}
	}

	public Node3D GetActiveModel()
	{
		return _currentSelectedCharacter switch
		{
			"Oceanus" => _oceanus3DModel,
			"Surya Prime" => _suryaPrime3DModel,
			"Mirage Blade" => _mirageBlade3DModel,
			"Apex Titan" => _apexTitan3DModel,
			"Aura Phoenicia" or "Phoenicia" => _phoenicia3DModel,
			_ => _cyberShogun3DModel
		};
	}

	/// <summary>
	/// Previews an ability animation on the active 3D model and updates the tactical UI display.
	/// Automatically reverts to the Idle animation once the ability finishes.
	/// </summary>
	public void PreviewAbility(int abilityIndex)
	{
		var abilities = GetOperativeAbilities(_currentSelectedCharacter);
		if (abilityIndex < 0 || abilityIndex >= abilities.Length) return;

		var ability = abilities[abilityIndex];
		UpdateAbilityDisplay(ability, isPreviewing: true);

		Node3D activeModel = GetActiveModel();
		if (activeModel == null || !GodotObject.IsInstanceValid(activeModel)) return;
		activeModel.Position = TargetPodiumPosition;

		var player = FindNodeRecursive<AnimationPlayer>(activeModel);
		if (player == null) return;

		string targetAnim = ResolveAnimationName(player, ability.AnimName);
		if (string.IsNullOrEmpty(targetAnim))
		{
			GD.PrintErr($"CharacterSelection: Animation '{ability.AnimName}' not found on {activeModel.Name}");
			return;
		}

		var anim = player.GetAnimation(targetAnim);
		if (anim != null)
		{
			anim.LoopMode = Animation.LoopModeEnum.None;
		}

		_currentPreviewAnim = targetAnim;

		if (!player.IsConnected(AnimationMixer.SignalName.AnimationFinished, _previewFinishedCallable))
		{
			player.Connect(AnimationMixer.SignalName.AnimationFinished, _previewFinishedCallable);
		}

		player.Stop();
		player.Play(targetAnim, customBlend: 0.18f);

		// Trigger High-Fidelity Elemental Hero Ability VFX (Water streams, Fire blasts, Phase slashes, etc.)
		var world3D = _world3D ?? GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D") ?? activeModel.GetParent<Node3D>();
		if (world3D != null)
		{
			Vector3 forward = activeModel.GlobalTransform.Basis * new Vector3(0, 0, 1);
			if (forward.LengthSquared() > 0.001f) forward = forward.Normalized();
			else forward = new Vector3(0, 0, 1);

			HeroAbilityVFX.TriggerAbilityVFX(world3D, activeModel, _currentSelectedCharacter, ability.AnimName, forward);
		}
	}

	public void OnPreviewAnimationFinished(StringName animName)
	{
		if (_currentPreviewAnim == animName.ToString())
		{
			_currentPreviewAnim = "";
			Node3D activeModel = GetActiveModel();
			if (activeModel != null && GodotObject.IsInstanceValid(activeModel))
			{
				activeModel.Position = TargetPodiumPosition;
				PlayStandingAnimation(activeModel, _currentSelectedCharacter);
			}

			var world3D = _world3D ?? GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
			if (world3D != null)
			{
				HeroAbilityVFX.ClearActiveVFX(world3D);
			}

			if (_abilityStatusLabel != null && GodotObject.IsInstanceValid(_abilityStatusLabel))
			{
				_abilityStatusLabel.Text = Tr("✔ READY // SELECT ABILITY TO PREVIEW");
				_abilityStatusLabel.Modulate = new Color(0.2f, 1f, 0.5f, 0.9f);
			}
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

	private static string ResolveAnimationName(AnimationPlayer player, string baseName)
	{
		if (player == null || string.IsNullOrEmpty(baseName)) return null;
		var list = player.GetAnimationList();

		foreach (var name in list)
		{
			if (string.Equals(name, baseName, StringComparison.OrdinalIgnoreCase))
				return name;
		}

		foreach (var name in list)
		{
			string n = name.ToString();
			if (n.EndsWith(baseName, StringComparison.OrdinalIgnoreCase) ||
				n.Contains(baseName, StringComparison.OrdinalIgnoreCase))
			{
				return name;
			}
		}

		return null;
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

	/// <summary>
	/// Loads a GLB model from a resource path.
	/// If PackedScene loading fails, it uses the GltfDocument for raw file loading.
	/// Automatically strips redundant static meshes from secondary armatures.
	/// </summary>
	private Node3D LoadGlbModel(string resPath)
	{
		try
		{
			Node3D loaded = null;
			if (ResourceLoader.Exists(resPath))
			{
				var packed = GD.Load<PackedScene>(resPath);
				if (packed != null) loaded = packed.Instantiate<Node3D>();
			}

			if (loaded == null)
			{
				string globalPath = ProjectSettings.GlobalizePath(resPath);
				if (System.IO.File.Exists(globalPath))
				{
					var doc = new GltfDocument();
					var state = new GltfState();
					if (doc.AppendFromFile(globalPath, state) == Error.Ok)
					{
						// Raw GLB loading bypasses the editor's material import script.
						foreach (var material in state.GetMaterials())
						{
							if (material is BaseMaterial3D baseMaterial)
							{
								baseMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
								baseMaterial.SpecularMode = BaseMaterial3D.SpecularModeEnum.SchlickGgx;
								baseMaterial.MetallicSpecular = Mathf.Max(baseMaterial.MetallicSpecular, 0.5f);
							}
						}
						loaded = (Node3D)doc.GenerateScene(state);
					}
				}
			}

			if (loaded != null)
			{
				CleanDuplicateMeshes(loaded);
				if (!resPath.Contains("selection_platf"))
				{
					string charKey = "";
					if (resPath.Contains("oceanus")) charKey = "Oceanus";
					else if (resPath.Contains("surya")) charKey = "Surya Prime";
					else if (resPath.Contains("mirage")) charKey = "Mirage Blade";
					else if (resPath.Contains("apex")) charKey = "Apex Titan";
					else if (resPath.Contains("phoenicia")) charKey = "Aura Phoenicia";
					else if (resPath.Contains("shogun")) charKey = "Cyber Shogun";

					ApplyToonShading(loaded, charKey);
				}
				return loaded;
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"CharacterSelection: GLB load error: {ex.Message}");
		}
		return null;
	}

	private static Shader? _toonShader;
	private static Shader? _outlineShader;

	/// <summary>
	/// Applies high-energy anime cel-shading and inverted-hull ink outline to all meshes in the model.
	/// Customizes rim lighting and shadow tints per operative signature theme while eliminating harsh white specular glare.
	/// </summary>
	public static void ApplyToonShading(Node root, string characterName = "")
	{
		if (root == null) return;

		try
		{
			if (_toonShader == null)
			{
				_toonShader = GD.Load<Shader>("res://assets/shaders/toon_character.gdshader");
			}
			if (_outlineShader == null)
			{
				_outlineShader = GD.Load<Shader>("res://assets/shaders/toon_outline.gdshader");
			}

			if (_toonShader == null) return;

			Color rimColor = new Color(0.0f, 0.90f, 1.0f, 1.0f);
			Color shadowTint = new Color(0.48f, 0.52f, 0.65f, 1.0f);
			float outlineWidth = 2.0f;
			Color outlineColor = new Color(0.02f, 0.03f, 0.06f, 1.0f);
			float emissionBoost = 2.2f;
			float emissionThreshold = 0.75f;
			float emissionSaturation = 0.30f;

			switch (characterName)
			{
				case "Oceanus":
					rimColor = new Color(0.0f, 0.95f, 0.85f, 1.0f); // Bioluminescent Aqua
					shadowTint = new Color(0.38f, 0.50f, 0.62f, 1.0f); // Abyssal deep blue
					break;
				case "Surya Prime":
					rimColor = new Color(1.0f, 0.72f, 0.28f, 1.0f); // Cosmic Solar Amber
					shadowTint = new Color(0.55f, 0.42f, 0.48f, 1.0f); // Cosmic bronze
					break;
				case "Mirage Blade":
					rimColor = new Color(0.20f, 0.95f, 0.65f, 1.0f); // Crystalline Phantom Emerald
					shadowTint = new Color(0.42f, 0.46f, 0.52f, 1.0f); // Phantom slate
					break;
				case "Apex Titan":
					rimColor = new Color(0.35f, 0.75f, 1.0f, 1.0f); // Electro-Magnetic Azure
					shadowTint = new Color(0.45f, 0.46f, 0.55f, 1.0f); // Heavy metal steel
					break;
				case "Aura Phoenicia":
				case "Phoenicia":
					rimColor = new Color(0.55f, 0.15f, 0.95f, 1.0f); // Imperial Tyrian Purple
					shadowTint = new Color(0.40f, 0.35f, 0.50f, 1.0f); // Mediterranean Twilight
					emissionThreshold = 0.88f; // Ensure only cyan acoustic crystals and cybernetic eyes emit light
					break;
				case "Cyber Shogun":
				default:
					rimColor = new Color(0.0f, 0.90f, 1.0f, 1.0f); // Neon Cyan
					shadowTint = new Color(0.48f, 0.52f, 0.65f, 1.0f); // Cool navy
					break;
			}

			foreach (var node in root.FindChildren("*", "MeshInstance3D"))
			{
				if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
				{
					string nodeName = meshInstance.Name.ToString();
					// Remove stray untextured bone helper shapes (e.g. Blender Icosphere custom bone shapes)
					if (nodeName.StartsWith("Icosphere", StringComparison.OrdinalIgnoreCase))
					{
						meshInstance.Visible = false;
						meshInstance.QueueFree();
						continue;
					}

					float matEmissionBoost = emissionBoost;
					float matEmissionThreshold = emissionThreshold;
					float matEmissionSaturation = emissionSaturation;

					// Dedicated hardlight halo & wing energy accessories glow brightly
					if (nodeName.Contains("Halo", StringComparison.OrdinalIgnoreCase) ||
					    nodeName.Contains("Wing", StringComparison.OrdinalIgnoreCase))
					{
						matEmissionBoost = 2.4f;
						matEmissionThreshold = 0.50f;
						matEmissionSaturation = 0.20f;
					}

					int surfaceCount = meshInstance.Mesh.GetSurfaceCount();
					for (int s = 0; s < surfaceCount; s++)
					{
						Texture2D albedoTex = null;
						Color albedoColor = Colors.White;

						var origMat = meshInstance.GetSurfaceOverrideMaterial(s) ?? meshInstance.Mesh.SurfaceGetMaterial(s);
						if (origMat is BaseMaterial3D baseMat)
						{
							albedoTex = baseMat.AlbedoTexture;
							albedoColor = baseMat.AlbedoColor;
						}

						var toonMat = new ShaderMaterial { Shader = _toonShader };
						if (albedoTex != null)
						{
							toonMat.SetShaderParameter("albedo_texture", albedoTex);
						}
						toonMat.SetShaderParameter("albedo_tint", albedoColor);
						toonMat.SetShaderParameter("rim_color", rimColor);
						toonMat.SetShaderParameter("shadow_tint", shadowTint);
						toonMat.SetShaderParameter("emission_boost", matEmissionBoost);
						toonMat.SetShaderParameter("emission_threshold", matEmissionThreshold);
						toonMat.SetShaderParameter("emission_saturation", matEmissionSaturation);

						if (_outlineShader != null)
						{
							var outlineMat = new ShaderMaterial { Shader = _outlineShader };
							outlineMat.SetShaderParameter("outline_width", outlineWidth);
							outlineMat.SetShaderParameter("outline_color", outlineColor);
							toonMat.NextPass = outlineMat;
						}

						meshInstance.SetSurfaceOverrideMaterial(s, toonMat);
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"CharacterSelection: Error applying toon shading: {ex.Message}");
		}
	}

	/// <summary>
	/// Strips redundant static MeshInstance3Ds from batch-exported armatures (Armature_001..Armature_042),
	/// leaving only the single active animated Armature mesh.
	/// Preserves the Skeleton3D hierarchy under Armature_* so AnimationPlayer tracks resolve cleanly without warnings.
	/// </summary>
	public static void CleanDuplicateMeshes(Node3D model)
	{
		if (model == null) return;

		foreach (Node child in model.GetChildren())
		{
			string name = child.Name.ToString();
			if (name.StartsWith("Armature_"))
			{
				RemoveMeshesRecursive(child);
				if (child is Node3D n3d)
				{
					n3d.Visible = false;
				}
			}
			else if (name.StartsWith("Icosphere", StringComparison.OrdinalIgnoreCase) ||
			         name.StartsWith("Cube", StringComparison.OrdinalIgnoreCase) ||
			         name.StartsWith("Sphere", StringComparison.OrdinalIgnoreCase) ||
			         name.StartsWith("Plane", StringComparison.OrdinalIgnoreCase))
			{
				model.RemoveChild(child);
				child.QueueFree();
			}
		}
	}

	private static void RemoveMeshesRecursive(Node parent)
	{
		var toRemove = new System.Collections.Generic.List<MeshInstance3D>();
		foreach (Node child in parent.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				toRemove.Add(mesh);
			}
			else
			{
				RemoveMeshesRecursive(child);
			}
		}
		foreach (var mesh in toRemove)
		{
			parent.RemoveChild(mesh);
			mesh.QueueFree();
		}
	}

	/// <summary>
	/// Recursively finds an AnimationPlayer and starts the operative's optimal heroic idle animation on loop.
	/// "Standing" now houses the dynamic heroic breathing animation across all operative models.
	/// </summary>
	private static void PlayStandingAnimation(Node root, string characterName = "")
	{
		var player = FindNodeRecursive<AnimationPlayer>(root);
		if (player == null) return;

		var list = player.GetAnimationList();
		if (list.Length == 0) return;

		string clip = null;

		// Prioritize "Standing", which contains the heroic breathing idle across all operatives
		foreach (var name in list)
		{
			if (string.Equals(name, "Standing", StringComparison.OrdinalIgnoreCase))
			{
				clip = name;
				break;
			}
		}

		if (clip == null)
		{
			foreach (var name in list)
			{
				if (name.ToString().ToLowerInvariant().Contains("standing"))
				{
					clip = name;
					break;
				}
			}
		}

		if (clip == null)
		{
			foreach (var name in list)
			{
				if (string.Equals(name, "Armature|Idle_02|baselayer", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(name, "Idle_02", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(name, "Idle", StringComparison.OrdinalIgnoreCase))
				{
					clip = name;
					break;
				}
			}
		}

		if (clip == null)
		{
			foreach (var name in list)
			{
				if (name.ToString().ToLowerInvariant().Contains("idle"))
				{
					clip = name;
					break;
				}
			}
		}

		if (clip == null && list.Length > 0)
		{
			clip = list[0];
		}

		var anim = player.GetAnimation(clip);
		if (anim != null)
			anim.LoopMode = Animation.LoopModeEnum.Linear;

		player.Play(clip, customBlend: 0.20f);
	}

	/// <summary>
	/// Helper to recursively search for a node of type T.
	/// </summary>
	private static T FindNodeRecursive<T>(Node node) where T : Node
	{
		if (node is T match) return match;
		foreach (Node child in node.GetChildren())
		{
			var result = FindNodeRecursive<T>(child);
			if (result != null) return result;
		}
		return null;
	}

	public override void _Process(double delta)
	{
		_glowPulseTimer += (float)delta;

		// Determine rotation input from UI buttons, Keyboard, or Gamepad.
		float rotInput = _rotateInput;

		if (rotInput == 0f)
		{
			if (Input.IsKeyPressed(RotateLeftKey))       rotInput = -1f;
			else if (Input.IsKeyPressed(RotateRightKey)) rotInput =  1f;
		}

		if (rotInput == 0f)
		{
			if (Input.IsJoyButtonPressed(0, RotateLeftBtn))       rotInput = -1f;
			else if (Input.IsJoyButtonPressed(0, RotateRightBtn)) rotInput =  1f;
		}

		// Gamepad Right Stick axis support for rotating character preview
		if (rotInput == 0f)
		{
			float joyStickX = Input.GetJoyAxis(0, JoyAxis.RightX);
			if (Mathf.Abs(joyStickX) > 0.2f)
			{
				rotInput = Mathf.Sign(joyStickX);
			}
		}

		if (rotInput != 0f)
			_charRotationY += rotInput * RotationSpeed * (float)delta;

		var activeRotation = new Vector3(0f, _charRotationY, 0f);

		// Apply rotation to the currently visible 3D model.
		if (_cyberShogun3DModel  != null && GodotObject.IsInstanceValid(_cyberShogun3DModel)  && _cyberShogun3DModel.Visible)
			_cyberShogun3DModel.RotationDegrees  = activeRotation;
		if (_oceanus3DModel      != null && GodotObject.IsInstanceValid(_oceanus3DModel)      && _oceanus3DModel.Visible)
			_oceanus3DModel.RotationDegrees      = activeRotation;
		if (_suryaPrime3DModel   != null && GodotObject.IsInstanceValid(_suryaPrime3DModel)   && _suryaPrime3DModel.Visible)
			_suryaPrime3DModel.RotationDegrees   = activeRotation;
		if (_mirageBlade3DModel  != null && GodotObject.IsInstanceValid(_mirageBlade3DModel)  && _mirageBlade3DModel.Visible)
			_mirageBlade3DModel.RotationDegrees  = activeRotation;
		if (_apexTitan3DModel    != null && GodotObject.IsInstanceValid(_apexTitan3DModel)    && _apexTitan3DModel.Visible)
			_apexTitan3DModel.RotationDegrees    = activeRotation;
		if (_phoenicia3DModel    != null && GodotObject.IsInstanceValid(_phoenicia3DModel)    && _phoenicia3DModel.Visible)
			_phoenicia3DModel.RotationDegrees    = activeRotation;

		// Apply a pulsing emissive glow to the selection platform.
		if (_platform3DModel != null && GodotObject.IsInstanceValid(_platform3DModel))
		{
			float pulse     = 0.4f + 1.05f * (Mathf.Sin(_glowPulseTimer * Mathf.Pi) * 0.5f + 0.5f);
			var   glowColor = new Color(0.55f, 0.85f, 1.0f, 1.0f);
			ApplyEmissivePulse(_platform3DModel, glowColor, pulse);
		}
	}

	/// <summary>
	/// Recursively applies an emissive pulse to all MeshInstance3D surfaces to create a "glowing" effect.
	/// </summary>
	private static void ApplyEmissivePulse(Node node, Color color, float energy)
	{
		if (node is MeshInstance3D mesh)
		{
			for (int s = 0; s < mesh.GetSurfaceOverrideMaterialCount(); s++)
			{
				var mat = mesh.GetSurfaceOverrideMaterial(s) as StandardMaterial3D;
				if (mat == null)
				{
					// Duplicate the base material to prevent mutating the shared resource.
					var baseMat = mesh.Mesh?.SurfaceGetMaterial(s) as StandardMaterial3D;
					if (baseMat != null)
					{
						mat = (StandardMaterial3D)baseMat.Duplicate();
						mesh.SetSurfaceOverrideMaterial(s, mat);
					}
				}
				if (mat != null)
				{
					mat.EmissionEnabled = true;
					mat.Emission        = color;
					mat.EmissionEnergyMultiplier = energy;
				}
			}
		}
		foreach (Node child in node.GetChildren())
			ApplyEmissivePulse(child, color, energy);
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

		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/3d_story_intro.tscn");
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
}
