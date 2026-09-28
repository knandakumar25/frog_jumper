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

	private string _currentSelectedCharacter = "Cyber Shogun";

	public override void _Ready()
	{
		// Stop the main menu music to allow character-specific audio or silence.
		GetNode<Node>("/root/Global").Call("stop_menu_music");

		// Initialize UI references.
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

		// Setup button event handlers for the character grid.
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

	/// <summary>
	/// Converts text to uppercase, but avoids doing so for scripts (Arabic, Hebrew, CJK) where uppercase doesn't exist.
	/// </summary>
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

	private static readonly Vector3 TargetPodiumPosition = new Vector3(-0.45f, 2.69f, 0f);

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

	/// <summary>
	/// Updates the UI and 3D preview when a new character is selected.
	/// </summary>
	private void SelectOperative(string characterName)
	{
		_currentSelectedCharacter = characterName;

		// Update labels and stat meters based on the selected operative.
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
				PlayIdleAnimation(_cyberShogun3DModel);
			}
		}

		if (_solaria3DModel == null)
		{
			string glbPath = "res://assets/models/hq/solaria.glb";
			_solaria3DModel = LoadGlbModel(glbPath);
			if (_solaria3DModel != null)
			{
				_solaria3DModel.Scale    = new Vector3(0.68f, 0.68f, 0.68f);
				_solaria3DModel.Position = new Vector3(-0.45f, 2.69f, 0f);
				world3D.AddChild(_solaria3DModel);
				PlayIdleAnimation(_solaria3DModel);
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
				PlayIdleAnimation(_oceanus3DModel);
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
				PlayIdleAnimation(_suryaPrime3DModel);
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
				PlayIdleAnimation(_mirageBlade3DModel);
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
				PlayIdleAnimation(_apexTitan3DModel);
			}
		}

		// Toggle visibility based on selection.
		if (_cyberShogun3DModel != null) _cyberShogun3DModel.Visible = (characterName == "Cyber Shogun");
		if (_solaria3DModel != null) _solaria3DModel.Visible = (characterName == "Solaria");
		if (_oceanus3DModel != null) _oceanus3DModel.Visible = (characterName == "Oceanus");
		if (_suryaPrime3DModel != null) _suryaPrime3DModel.Visible = (characterName == "Surya Prime");
		if (_mirageBlade3DModel != null) _mirageBlade3DModel.Visible = (characterName == "Mirage Blade");
		if (_apexTitan3DModel != null) _apexTitan3DModel.Visible = (characterName == "Apex Titan");

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

		// Identify active model and trigger high-altitude thruster landing fly-in
		Node3D activeModel = characterName switch
		{
			"Solaria" => _solaria3DModel,
			"Oceanus" => _oceanus3DModel,
			"Surya Prime" => _suryaPrime3DModel,
			"Mirage Blade" => _mirageBlade3DModel,
			"Apex Titan" => _apexTitan3DModel,
			_ => _cyberShogun3DModel
		};

		if (activeModel != null && GodotObject.IsInstanceValid(activeModel))
		{
			activeModel.Position = TargetPodiumPosition;
			activeModel.RotationDegrees = new Vector3(0f, _charRotationY, 0f);
			PlayIdleAnimation(activeModel);
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
				return loaded;
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"CharacterSelection: GLB load error: {ex.Message}");
		}
		return null;
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
			if (child.Name.ToString().StartsWith("Armature_"))
			{
				RemoveMeshesRecursive(child);
				if (child is Node3D n3d)
				{
					n3d.Visible = false;
				}
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
	/// Recursively finds an AnimationPlayer and starts the first matching "idle" animation.
	/// </summary>
	private static void PlayIdleAnimation(Node root)
	{
		var player = FindNodeRecursive<AnimationPlayer>(root);
		if (player == null) return;

		var list = player.GetAnimationList();
		if (list.Length == 0) return;

		string clip = list[0];
		foreach (var name in list)
		{
			if (name.ToString().ToLowerInvariant().Contains("idle"))
			{
				clip = name;
				break;
			}
		}

		var anim = player.GetAnimation(clip);
		if (anim != null)
			anim.LoopMode = Animation.LoopModeEnum.Linear;

		player.Play(clip);
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

		if (rotInput != 0f)
			_charRotationY += rotInput * RotationSpeed * (float)delta;

		var activeRotation = new Vector3(0f, _charRotationY, 0f);

		// Apply rotation to the currently visible 3D model.
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
		var global = GetNode<Node>("/root/Global");
		global.Set("selected_character", characterName);

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
