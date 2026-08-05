using Godot;

/// <summary>
/// Character selection screen. Manages operative selection cards, updates live preview showcase,
/// creates a new session in SessionManager, and transitions to story intro.
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

	private string _currentSelectedCharacter = "Cyber Shogun";

	public override void _Ready()
	{
		GetNode<Node>("/root/Global").Call("stop_menu_music");

		_selectedNameLabel = GetNode<Label>("MainLayout/RightShowcasePanel/VBox/SelectedName");
		_civilianNameLabel = GetNodeOrNull<Label>("MainLayout/RightShowcasePanel/VBox/CivilianName");
		_classTagLabel     = GetNode<Label>("MainLayout/RightShowcasePanel/VBox/ClassTag");
		_previewSprite     = GetNodeOrNull<AnimatedSprite2D>("MainLayout/RightShowcasePanel/VBox/PreviewFrame/PreviewSprite");
		_speedMeter        = GetNode<SegmentedStatBar>("MainLayout/RightShowcasePanel/VBox/StatsBox/SpeedMeter");
		_agilityMeter      = GetNode<SegmentedStatBar>("MainLayout/RightShowcasePanel/VBox/StatsBox/AgilityMeter");

		TranslateUI();
		SelectOperative("Cyber Shogun");
		FixFontForLocale(this);
		UISoundManager.AutoWireButtons(this);

		var apexTitanBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan");
		if (apexTitanBtn != null)
		{
			apexTitanBtn.Pressed += () => SelectOperative("Apex Titan");
		}
		var mirageBladeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade");
		if (mirageBladeBtn != null)
		{
			mirageBladeBtn.Pressed += () => SelectOperative("Mirage Blade");
		}
		var suryaPrimeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime");
		if (suryaPrimeBtn != null)
		{
			suryaPrimeBtn.Pressed += () => SelectOperative("Surya Prime");
		}
		var oceanusBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus");
		if (oceanusBtn != null)
		{
			oceanusBtn.Pressed += () => SelectOperative("Oceanus");
		}
		var solariaBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Solaria");
		if (solariaBtn != null)
		{
			solariaBtn.Pressed += () => SelectOperative("Solaria");
		}
		var shogunBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun");
		if (shogunBtn != null)
		{
			shogunBtn.Pressed += () => SelectOperative("Cyber Shogun");
		}

		shogunBtn?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private static void FixFontForLocale(Node node)
	{
		// Native Orbitron & Rajdhani sci-fi fonts handle Arabic via Cairo font fallbacks configured in global.gd
	}

	private void TranslateUI()
	{
		var header = GetNodeOrNull<Label>("Header");
		if (header != null) header.Text = Tr("OPERATIVE SELECTION MATRIX");

		var listTitle = GetNodeOrNull<Label>("MainLayout/LeftListPanel/VBox/ListTitle");
		if (listTitle != null) listTitle.Text = Tr("AVAILABLE OPERATIVES");

		var shogunBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun");
		if (shogunBtn != null) shogunBtn.Text = $"{Tr("Cyber Shogun")} [{Tr("Kaito Kurogane")}]";

		var solariaBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Solaria");
		if (solariaBtn != null) solariaBtn.Text = $"{Tr("Solaria")} [{Tr("Valentina Cruz")}]";

		var oceanusBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus");
		if (oceanusBtn != null) oceanusBtn.Text = $"{Tr("Oceanus")} [{Tr("Kiana Vaea")}]";

		var suryaPrimeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime");
		if (suryaPrimeBtn != null) suryaPrimeBtn.Text = $"{Tr("Surya Prime")} [{Tr("Arjun Varma")}]";

		var mirageBladeBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade");
		if (mirageBladeBtn != null) mirageBladeBtn.Text = $"{Tr("Mirage Blade")} [{Tr("Zayd Al-Mansoor")}]";

		var apexTitanBtn = GetNodeOrNull<Button>("MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan");
		if (apexTitanBtn != null) apexTitanBtn.Text = $"{Tr("Apex Titan")} [{Tr("King Tariq Nzinga")}]";

		var deployBtn = GetNodeOrNull<Button>("MainLayout/RightShowcasePanel/VBox/DeployButton");
		if (deployBtn != null) deployBtn.Text = Tr("DEPLOY OPERATIVE");
	}

	private static string SafeToUpper(string text)
	{
		if (string.IsNullOrEmpty(text)) return "";
		string locale = TranslationServer.GetLocale().ToLower();
		if (locale.Contains("ar") || locale.Contains("he") || locale.Contains("ja") || 
		    locale.Contains("ko") || locale.Contains("zh"))
		{
			return text;
		}
		return text.ToUpper();
	}

	private Node3D _cyberShogun3DModel;
	private Node3D _solaria3DModel;
	private Node3D _oceanus3DModel;
	private Node3D _suryaPrime3DModel;
	private Node3D _mirageBlade3DModel;
	private Node3D _apexTitan3DModel;
	private Node3D _platform3DModel;

	// User-controlled rotation (degrees, accumulated)
	private float _charRotationY  = 0f;
	private float _glowPulseTimer = 0f;
	private float _rotateInput    = 0f;  // -1 left, +1 right, 0 idle

	// --- Arrow button signal handlers (hold-to-rotate) ---
	public void _on_rotate_left_down()  => _rotateInput = -1f;
	public void _on_rotate_left_up()    => _rotateInput =  0f;
	public void _on_rotate_right_down() => _rotateInput =  1f;
	public void _on_rotate_right_up()   => _rotateInput =  0f;

	public void _on_cyber_shogun_pressed()  => SelectOperative("Cyber Shogun");
	public void _on_solaria_pressed()       => SelectOperative("Solaria");
	public void _on_oceanus_pressed()       => SelectOperative("Oceanus");
	public void _on_surya_prime_pressed()    => SelectOperative("Surya Prime");
	public void _on_mirage_blade_pressed()   => SelectOperative("Mirage Blade");
	public void _on_apex_titan_pressed()     => SelectOperative("Apex Titan");
	public void _on_deploy_pressed()        => CreateSessionAndStart(_currentSelectedCharacter);

	private void SelectOperative(string characterName)
	{
		_currentSelectedCharacter = characterName;

		if (characterName == "Solaria")
		{
			_selectedNameLabel.Text = SafeToUpper(Tr("Solaria"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: VALENTINA CRUZ"));
			_classTagLabel.Text     = Tr("// CLASS: SOLAR FLARE MANIPULATOR");
			_speedMeter?.SetStat(Tr("SPEED"), 95f);
			_agilityMeter?.SetStat(Tr("AGILITY"), 99f);
		}
		else if (characterName == "Oceanus")
		{
			_selectedNameLabel.Text = SafeToUpper(Tr("Oceanus"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: KIANA VAEA"));
			_classTagLabel.Text     = Tr("// CLASS: HYDROKINETIC PLASMA MANIPULATOR");
			_speedMeter?.SetStat(Tr("SPEED"), 96f);
			_agilityMeter?.SetStat(Tr("AGILITY"), 97f);
		}
		else if (characterName == "Surya Prime")
		{
			_selectedNameLabel.Text = SafeToUpper(Tr("Surya Prime"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: ARJUN VARMA"));
			_classTagLabel.Text     = Tr("// CLASS: COSMIC SOLAR VANGUARD");
			_speedMeter?.SetStat(Tr("SPEED"), 97f);
			_agilityMeter?.SetStat(Tr("AGILITY"), 98f);
		}
		else if (characterName == "Mirage Blade")
		{
			_selectedNameLabel.Text = SafeToUpper(Tr("Mirage Blade"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: ZAYD AL-MANSOOR"));
			_classTagLabel.Text     = Tr("// CLASS: CRYSTALLINE PHANTOM");
			_speedMeter?.SetStat(Tr("SPEED"), 99f);
			_agilityMeter?.SetStat(Tr("AGILITY"), 99f);
		}
		else if (characterName == "Apex Titan")
		{
			_selectedNameLabel.Text = SafeToUpper(Tr("Apex Titan"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: KING TARIQ NZINGA"));
			_classTagLabel.Text     = Tr("// CLASS: ELECTRO-MAGNETIC STORM VANGUARD");
			_speedMeter?.SetStat(Tr("SPEED"), 93f);
			_agilityMeter?.SetStat(Tr("AGILITY"), 94f);
		}
		else
		{
			_selectedNameLabel.Text = SafeToUpper(Tr("Cyber Shogun"));
			if (_civilianNameLabel != null) _civilianNameLabel.Text = SafeToUpper(Tr("IDENTITY: KAITO KUROGANE"));
			_classTagLabel.Text     = Tr("// CLASS: 3D CYBERNETIC SHOGUN WARRIOR");
			_speedMeter?.SetStat(Tr("SPEED"), 98f);
			_agilityMeter?.SetStat(Tr("AGILITY"), 96f);
		}

		FixFontForLocale(this);
		UpdatePreviewShowcase(_currentSelectedCharacter);
	}

	private void UpdatePreviewShowcase(string characterName)
	{
		var world3D = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D == null) return;

		if (_cyberShogun3DModel == null)
		{
			string glbPath = "res://assets/images/Cyber Shogun/cyber_shogun.glb";
			_cyberShogun3DModel = LoadGlbModel(glbPath);
			if (_cyberShogun3DModel != null)
			{
				_cyberShogun3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_cyberShogun3DModel.Position = new Vector3(-0.45f, 3.35f, 0f);
				world3D.AddChild(_cyberShogun3DModel);
			}
		}

		if (_solaria3DModel == null)
		{
			string glbPath = "res://assets/images/Solaria/Meshy_AI_solaris_heroine_remes_0804052921_texture.glb";
			_solaria3DModel = LoadGlbModel(glbPath);
			if (_solaria3DModel != null)
			{
				_solaria3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_solaria3DModel.Position = new Vector3(-0.45f, 3.35f, 0f);
				world3D.AddChild(_solaria3DModel);
			}
		}

		if (_oceanus3DModel == null)
		{
			string glbPath = "res://assets/images/Oceanus/Meshy_AI_polynesian_superheroi_0804064602_image-to-3d-texture.glb";
			_oceanus3DModel = LoadGlbModel(glbPath);
			if (_oceanus3DModel != null)
			{
				_oceanus3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_oceanus3DModel.Position = new Vector3(-0.45f, 3.35f, 0f);
				world3D.AddChild(_oceanus3DModel);
			}
		}

		if (_suryaPrime3DModel == null)
		{
			string glbPath = "res://assets/images/Surya Prime/Meshy_AI_solar_guardian_0804150949_image-to-3d-texture.glb";
			_suryaPrime3DModel = LoadGlbModel(glbPath);
			if (_suryaPrime3DModel != null)
			{
				_suryaPrime3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_suryaPrime3DModel.Position = new Vector3(-0.45f, 3.35f, 0f);
				world3D.AddChild(_suryaPrime3DModel);
			}
		}

		if (_mirageBlade3DModel == null)
		{
			string glbPath = "res://assets/images/Mirage Blade/Meshy_AI_arab_superhero_crysta_0804155043_image-to-3d-texture.glb";
			_mirageBlade3DModel = LoadGlbModel(glbPath);
			if (_mirageBlade3DModel != null)
			{
				_mirageBlade3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_mirageBlade3DModel.Position = new Vector3(-0.45f, 3.35f, 0f);
				world3D.AddChild(_mirageBlade3DModel);
			}
		}

		if (_apexTitan3DModel == null)
		{
			string glbPath = "res://assets/images/Apex Titan/Meshy_AI_cyber_superhero_0804161932_image-to-3d-texture.glb";
			_apexTitan3DModel = LoadGlbModel(glbPath);
			if (_apexTitan3DModel != null)
			{
				_apexTitan3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_apexTitan3DModel.Position = new Vector3(-0.45f, 3.35f, 0f);
				world3D.AddChild(_apexTitan3DModel);
			}
		}

		if (_cyberShogun3DModel != null)
		{
			_cyberShogun3DModel.Visible = (characterName == "Cyber Shogun");
		}
		if (_solaria3DModel != null)
		{
			_solaria3DModel.Visible = (characterName == "Solaria");
		}
		if (_oceanus3DModel != null)
		{
			_oceanus3DModel.Visible = (characterName == "Oceanus");
		}
		if (_suryaPrime3DModel != null)
		{
			_suryaPrime3DModel.Visible = (characterName == "Surya Prime");
		}
		if (_mirageBlade3DModel != null)
		{
			_mirageBlade3DModel.Visible = (characterName == "Mirage Blade");
		}
		if (_apexTitan3DModel != null)
		{
			_apexTitan3DModel.Visible = (characterName == "Apex Titan");
		}

		// Load platform once — it stays visible always
		if (_platform3DModel == null)
		{
			string platformPath = "res://assets/images/Meshy_AI_scifi_selection_platf_0805042413_image-to-3d-texture.glb";
			_platform3DModel = LoadGlbModel(platformPath);
			if (_platform3DModel != null)
			{
				// Sit just below the hero's feet (heroes are at Y=3.2, scale=0.68)
				_platform3DModel.Scale    = new Vector3(0.55f, 0.55f, 0.55f);
				_platform3DModel.Position = new Vector3(-0.45f, 2.62f, 0f);
				world3D.AddChild(_platform3DModel);
			}
		}
	}

	private Node3D LoadGlbModel(string resPath)
	{
		try
		{
			if (ResourceLoader.Exists(resPath))
			{
				var packed = GD.Load<PackedScene>(resPath);
				if (packed != null) return packed.Instantiate<Node3D>();
			}

			string globalPath = ProjectSettings.GlobalizePath(resPath);
			if (System.IO.File.Exists(globalPath))
			{
				var doc = new GltfDocument();
				var state = new GltfState();
				if (doc.AppendFromFile(globalPath, state) == Error.Ok)
				{
					return (Node3D)doc.GenerateScene(state);
				}
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"CharacterSelection: GLB load error: {ex.Message}");
		}
		return null;
	}

	public override void _Process(double delta)
	{
		_glowPulseTimer += (float)delta;

		// --- Rotation input: arrow buttons OR Q/E keyboard OR LB/RB gamepad ---
		float rotInput = _rotateInput; // from held UI arrow buttons

		if (rotInput == 0f)
		{
			// Keyboard
			if (Input.IsKeyPressed(RotateLeftKey))       rotInput = -1f;
			else if (Input.IsKeyPressed(RotateRightKey)) rotInput =  1f;
		}

		if (rotInput == 0f)
		{
			// Gamepad
			if (Input.IsJoyButtonPressed(0, RotateLeftBtn))       rotInput = -1f;
			else if (Input.IsJoyButtonPressed(0, RotateRightBtn)) rotInput =  1f;
		}

		if (rotInput != 0f)
			_charRotationY += rotInput * RotationSpeed * (float)delta;

		var activeRotation = new Vector3(0f, _charRotationY, 0f);

		if (_cyberShogun3DModel  != null && GodotObject.IsInstanceValid(_cyberShogun3DModel)  && _cyberShogun3DModel.Visible)
			_cyberShogun3DModel.RotationDegrees  = activeRotation;
		if (_solaria3DModel      != null && GodotObject.IsInstanceValid(_solaria3DModel)      && _solaria3DModel.Visible)
			_solaria3DModel.RotationDegrees      = activeRotation;
		if (_oceanus3DModel      != null && GodotObject.IsInstanceValid(_oceanus3DModel)      && _oceanus3DModel.Visible)
			_oceanus3DModel.RotationDegrees      = activeRotation;
		if (_suryaPrime3DModel   != null && GodotObject.IsInstanceValid(_suryaPrime3DModel)   && _suryaPrime3DModel.Visible)
			_suryaPrime3DModel.RotationDegrees   = activeRotation;
		if (_mirageBlade3DModel  != null && GodotObject.IsInstanceValid(_mirageBlade3DModel)  && _mirageBlade3DModel.Visible)
			_mirageBlade3DModel.RotationDegrees  = activeRotation;
		if (_apexTitan3DModel    != null && GodotObject.IsInstanceValid(_apexTitan3DModel)    && _apexTitan3DModel.Visible)
			_apexTitan3DModel.RotationDegrees    = activeRotation;

		// --- Platform: static position, pulsing glow only ---
		if (_platform3DModel != null && GodotObject.IsInstanceValid(_platform3DModel))
		{
			// No rotation — platform stays fixed
			float pulse     = 0.4f + 1.05f * (Mathf.Sin(_glowPulseTimer * Mathf.Pi) * 0.5f + 0.5f);
			var   glowColor = new Color(0.55f, 0.85f, 1.0f, 1.0f);
			ApplyEmissivePulse(_platform3DModel, glowColor, pulse);
		}
	}

	/// <summary>
	/// Recursively applies an emissive pulse to all MeshInstance3D surfaces.
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
					// If no override yet, duplicate the base material so we don't mutate the shared resource
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

	private void CreateSessionAndStart(string characterName)
	{
		var global = GetNode<Node>("/root/Global");
		global.Set("selected_character", characterName);

		int sessionId = SessionManager.Instance.CreateNewSession(characterName);
		if (sessionId > 0)
			GD.Print("Created new session: ", sessionId, " for character: ", characterName);
		else
			GD.Print("Error creating session!");

		LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/story_intro.tscn");
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
