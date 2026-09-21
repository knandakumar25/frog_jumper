using Godot;
using System;

/// <summary>
/// ThreeDMenuExperience is a specialized extension of MainMenu that transforms the static menu into a
/// cinematic 3D experience. It manages the rendering of the "Axiom Force HQ Citadel" 3D environment
/// and coordinates a smooth camera zoom transition that acts as the "entry" into the game's main interface.
///
/// Unity Migration Note:
/// 1. Scene Setup: Instead of programmatically loading GLB models in _Ready, use a Unity Scene with
///    Prefabs already placed. Use a 'MenuManager' script to handle the logic.
/// 2. Camera Transition: Use Cinemachine with a "Virtual Camera" for the High View and another for the
///    Plaza View, then use CinemachineBrain to blend between them.
/// 3. Background Animation: Use simple C# scripts on the vehicle GameObjects to handle the lane patrol
///    logic (Sines/Cosines) rather than managing them centrally in the menu class.
/// </summary>
public partial class ThreeDMenuExperience : MainMenu
{
	/// <summary>
	/// Defines the high-level states of the 3D menu experience.
	/// Home: The cinematic starting screen with the logo and prompt.
	/// Transitioning: The active phase where the camera zooms in.
	/// MainMenuState: The final state where the standard menu buttons are interactive.
	/// </summary>
	private enum MenuState { Home, Transitioning, MainMenuState }
	private MenuState _currentState = MenuState.Home;

	// ─── 3D World References ──────────────────────────────────────────────────
	private Node3D      _hqModel;
	private Camera3D    _camera3D;
	private Control     _homeOverlay;
	private Label       _promptLabel;
	private TextureRect _titleLogo;
	private Control     _titleContainer;
	private Control     _menuButtonsContainer;
	private ColorRect   _fadeOverlay;
	private float       _pulseTimer = 0f;

	// ─── Camera Transform Constants ──────────────────────────────────────────────
	/// <summary>
	/// Start position for the cinematic zoom: a wide view of the city canyon.
	/// </summary>
	private static readonly Vector3 HighCamPos = new Vector3(0f, 10f, 26f);
	private static readonly Vector3 HighCamRot = new Vector3(-6f, 0f, 0f);

	/// <summary>
	/// End position for the cinematic zoom: a tight view of the HQ Plaza entrance.
	/// </summary>
	private static readonly Vector3 PlazaCamPos = new Vector3(0f, 3.8f, 15f);
	private static readonly Vector3 PlazaCamRot = new Vector3(2f, 0f, 0f);

	// ─── Ambient Vehicle References ─────────────────────────────────────────────
	private Node3D _spinnerVehicle;
	private Node3D _cargoBargeVehicle;
	private Node3D _vtolGunshipVehicle;
	private Node3D _shieldEmblemNode;

	// Patrol path data for ambient vehicles
	private float _spinnerX   = -60f;
	private float _cargoX     =  65f;
	private float _hoverTimer =  0f;

	public override void _Ready()
	{
		base._Ready();

		// Resolve UI overlay and fade references.
		_homeOverlay          = GetNodeOrNull<Control>("HomeOverlay");
		_promptLabel          = GetNodeOrNull<Label>("HomeOverlay/CenterContainer/VBox/PromptLabel");
		_titleLogo            = GetNodeOrNull<TextureRect>("HomeOverlay/CenterContainer/VBox/TitleLogo");
		_menuButtonsContainer = GetNodeOrNull<Control>("ButtonContainer");
		_fadeOverlay          = GetNodeOrNull<ColorRect>("FadeOverlay");

		// Setup the 3D camera to the starting 'High City' position.
		_camera3D = GetNodeOrNull<Camera3D>("SubViewportContainer/SubViewport3D/World3D/Camera3D");
		if (_camera3D != null)
		{
			_camera3D.Position = HighCamPos;
			_camera3D.RotationDegrees = HighCamRot;
		}

		// Load the architectural assets and the localized title logo.
		LoadHQModel();
		LoadHomeTitleLogo();

		// Audio: Trigger the atmospheric "Home" music loop.
		GetNodeOrNull<Node>("/root/Global")?.Call("play_home_music");

		if (_homeOverlay != null)          _homeOverlay.Visible = true;
		_titleContainer       = GetNodeOrNull<Control>("TitleContainer");
		if (_titleContainer != null)       _titleContainer.Modulate = new Color(1f, 1f, 1f, 0f);
		if (_menuButtonsContainer != null) _menuButtonsContainer.Modulate = new Color(1f, 1f, 1f, 0f);

		// Initial scene fade-in from black to avoid a jarring jump on load.
		if (_fadeOverlay != null)
		{
			_fadeOverlay.Visible = true;
			_fadeOverlay.Modulate = Colors.Black;
			var tween = CreateTween();
			tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, 0.6f);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_hoverTimer += dt;

		// Pulse the "Press Any Key" prompt using a sine wave for an organic breathe effect.
		if (_currentState == MenuState.Home && _promptLabel != null)
		{
			_pulseTimer += dt;
			float alpha = 0.35f + 0.65f * (Mathf.Sin(_pulseTimer * 3.5f) * 0.5f + 0.5f);
			_promptLabel.Modulate = new Color(1f, 1f, 1f, alpha);
		}

		// Ambient Background Animations:
		// These create a "living city" feel by animating vehicles in the 3D background.

		// 1. Police Spinner: Patrols the lower sky-highway lane with a gentle vertical sine-wave.
		if (_spinnerVehicle != null)
		{
			_spinnerX += 16f * dt;
			if (_spinnerX > 65f) _spinnerX = -65f;
			_spinnerVehicle.Position = new Vector3(_spinnerX, 15f + Mathf.Sin(_spinnerX * 0.25f) * 1.2f, -18f);
		}

		// 2. Cargo Barge: Moves slower along the upper freight lane.
		if (_cargoBargeVehicle != null)
		{
			_cargoX -= 10f * dt;
			if (_cargoX < -65f) _cargoX = 65f;
			_cargoBargeVehicle.Position = new Vector3(_cargoX, 26f + Mathf.Cos(_cargoX * 0.18f) * 1.5f, -28f);
		}

		// 3. VTOL Gunship: Performs a slow hovering sway above the landing pad.
		if (_vtolGunshipVehicle != null)
		{
			float hoverY = 1.6f + Mathf.Sin(_hoverTimer * 2.2f) * 0.35f;
			_vtolGunshipVehicle.Position = new Vector3(22f, hoverY, 6f);
		}

		// 4. Resistance Shield Emblem: Subtle rotation to imply a holographic projection.
		if (_shieldEmblemNode != null)
		{
			_shieldEmblemNode.RotationDegrees = new Vector3(0f, Mathf.Sin(_hoverTimer * 1.5f) * 6f, 0f);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Only trigger the zoom transition if we are currently in the Home state.
		if (_currentState == MenuState.Home)
		{
			// Detection for any form of input (Keyboard, Mouse, or Gamepad).
			bool isGamepad = (@event is InputEventJoypadButton jb && jb.Pressed) ||
							 (@event is InputEventJoypadMotion jm && Mathf.Abs(jm.AxisValue) > 0.5f);

			if ((@event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo) ||
				(@event is InputEventMouseButton mouseEv && mouseEv.Pressed) ||
				isGamepad)
			{
				StartZoomTransition();
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		base._Input(@event);
	}

	/// <summary>
	/// Orchestrates the cinematic transition from the high-level city view to the
	/// functional main menu. This includes camera interpolation, UI fading, and music shifts.
	/// </summary>
	private async void StartZoomTransition()
	{
		if (_currentState != MenuState.Home) return;
		_currentState = MenuState.Transitioning;

		// Audio feedback for the "System Initialization" event.
		UISoundManager.Instance?.PlayClickSound();
		UISoundManager.Instance?.PlayTransitionSound();

		if (_promptLabel != null)
		{
			_promptLabel.Modulate = new Color(1f, 0f, 0.5f, 1f);
			_promptLabel.Text = Tr("// SYSTEM INITIALIZED //");
		}

		// Shift music from "Home" atmospheric loop to the primary "Menu" loop.
		GetNodeOrNull<Node>("/root/Global")?.Call("play_menu_music");

		var tween = CreateTween().SetParallel(true);

		// Fade out the Home UI overlay (Logo and Prompt).
		if (_homeOverlay != null)
		{
			tween.TweenProperty(_homeOverlay, "modulate:a", 0.0f, 0.5f);
		}

		// Smoothly interpolate 3D Camera from High view to Ground Plaza view using a Cubic ease.
		if (_camera3D != null)
		{
			tween.TweenProperty(_camera3D, "position", PlazaCamPos, 2.0f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
			tween.TweenProperty(_camera3D, "rotation_degrees", PlazaCamRot, 2.0f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
		}

		// Fade in 2D Title and Main Menu buttons with a slight delay to allow the camera to settle.
		if (_titleContainer != null)
		{
			_titleContainer.Visible = true;
			tween.TweenProperty(_titleContainer, "modulate:a", 1.0f, 0.8f).SetDelay(1.0f);
		}

		if (_menuButtonsContainer != null)
		{
			_menuButtonsContainer.Visible = true;
			tween.TweenProperty(_menuButtonsContainer, "modulate:a", 1.0f, 0.8f).SetDelay(1.0f);
		}

		await ToSignal(tween, Tween.SignalName.Finished);

		_currentState = MenuState.MainMenuState;
		if (_homeOverlay != null) _homeOverlay.Visible = false;

		// Enable interaction for the main menu and focus the 'Play' button for gamepad support.
		if (_menuButtonsContainer != null)
		{
			_menuButtonsContainer.MouseFilter = MouseFilterEnum.Stop;
			var playBtn = _menuButtonsContainer.GetNodeOrNull<Control>("Play");
			playBtn?.CallDeferred(Control.MethodName.GrabFocus);
		}
	}

	/// <summary>
	/// Programmatically constructs the 3D environment by loading and positioning
	/// the various architectural and vehicle GLB models generated via Meshy AI.
	/// </summary>
	private void LoadHQModel()
	{
		var world3D = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D == null) return;

		// 0. City Skyline Background: A high-res image on a large quad to simulate a distant backdrop.
		string bgImgPath = ProjectSettings.GlobalizePath("res://assets/images/Gemini_Generated_Image_t8s2p4t8s2p4t8s2.png");
		if (System.IO.File.Exists(bgImgPath))
		{
			var img = Image.LoadFromFile(bgImgPath);
			if (img != null)
			{
				var bgTex = ImageTexture.CreateFromImage(img);
				var bgMesh = new QuadMesh { Size = new Vector2(160f, 90f) };
				var bgMat = new StandardMaterial3D
				{
					AlbedoTexture = bgTex,
					ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
					AlbedoColor = new Color(0.7f, 0.75f, 0.9f)
				};
				var bgInstance = new MeshInstance3D
				{
					Mesh = bgMesh,
					MaterialOverride = bgMat,
					Position = new Vector3(0f, 25f, -40f)
				};
				world3D.AddChild(bgInstance);
			}
		}

		// The following load and place 3D models for the Citadel complex.
		// Unity Migration Note: Replace this entire block with a pre-authored Unity Scene.

		// 1. PLAZA_01: The metallic floor grid forming the base of the plaza.
		string floorPath = "res://assets/models/hq/Meshy_AI_Metallic_Floor_Grid_0807073249_image-to-3d-texture.glb";
		var floorModel = LoadGlbModel(floorPath);
		if (floorModel != null)
		{
			floorModel.Scale = new Vector3(14.0f, 1.0f, 14.0f);
			floorModel.Position = new Vector3(0f, 0f, 0f);
			world3D.AddChild(floorModel);
		}

		// 2. ARCH_01: The centerpiece Axiom Force HQ Citadel building.
		string hqPath = "res://assets/models/hq/Meshy_AI_Axiom_Force_HQ_Buildi_0806151721_image-to-3d-texture.glb";
		_hqModel = LoadGlbModel(hqPath);
		if (_hqModel != null)
		{
			_hqModel.Scale = new Vector3(17.0f, 17.0f, 17.0f);
			_hqModel.Position = new Vector3(0f, 6.2f, -2f);
			world3D.AddChild(_hqModel);
		}

		// 3. ARCH_02: Bio-Gen Corporate Spire (skyline decoration).
		string biogenPath = "res://assets/models/hq/Meshy_AI_Bio_Gen_Corporate_Spi_0807054611_image-to-3d-texture.glb";
		var biogenModel = LoadGlbModel(biogenPath);
		if (biogenModel != null)
		{
			biogenModel.Scale = new Vector3(11.0f, 11.0f, 11.0f);
			biogenModel.Position = new Vector3(-17f, 4.2f, -8f);
			world3D.AddChild(biogenModel);
		}

		// 4. ARCH_03: Neuro-Link Communication Tower.
		string commTowerPath = "res://assets/models/hq/Meshy_AI_Neuro_Link_Comm_Tower_0807055157_image-to-3d-texture.glb";
		var commModel = LoadGlbModel(commTowerPath);
		if (commModel != null)
		{
			commModel.Scale = new Vector3(9.5f, 9.5f, 9.5f);
			commModel.Position = new Vector3(18f, 4.5f, -10f);
			world3D.AddChild(commModel);
		}

		// 5. ARCH_04: Syndicate Hab-Block (foreground urban detail).
		string habBlockPath = "res://assets/models/hq/Meshy_AI_Syndicate_Hab_Block_0807060633_image-to-3d-texture.glb";
		var habModel = LoadGlbModel(habBlockPath);
		if (habModel != null)
		{
			habModel.Scale = new Vector3(8.5f, 8.5f, 8.5f);
			habModel.Position = new Vector3(-19f, 2.0f, 4f);
			world3D.AddChild(habModel);
		}

		// 6. ARCH_05: Aegis Defense Bastion (fortified wing).
		string aegisPath = "res://assets/models/hq/Meshy_AI_Aegis_Defense_Bastion_0807061844_image-to-3d-texture.glb";
		var aegisModel = LoadGlbModel(aegisPath);
		if (aegisModel != null)
		{
			aegisModel.Scale = new Vector3(8.0f, 8.0f, 8.0f);
			aegisModel.Position = new Vector3(15f, 1.6f, -2f);
			world3D.AddChild(aegisModel);
		}

		// 7. INFRA_01: The elevated sky-highway span.
		string highwayPath = "res://assets/models/hq/Meshy_AI_Elevated_Sky_Highway__0807062447_image-to-3d-texture.glb";
		var highwayModel = LoadGlbModel(highwayPath);
		if (highwayModel != null)
		{
			highwayModel.Scale = new Vector3(16.0f, 7.0f, 10.0f);
			highwayModel.Position = new Vector3(0f, 15f, -16f);
			world3D.AddChild(highwayModel);
		}

		// 8. INFRA_02: Monorail Support Gantry.
		string monorailPath = "res://assets/models/hq/Meshy_AI_Monorail_Support_Gant_0807063542_image-to-3d-texture.glb";
		var monorailModel = LoadGlbModel(monorailPath);
		if (monorailModel != null)
		{
			monorailModel.Scale = new Vector3(6.5f, 10.0f, 6.5f);
			monorailModel.Position = new Vector3(-18f, 4.5f, -8f);
			world3D.AddChild(monorailModel);
		}

		// 9. INFRA_03: The gunship landing pad in the plaza.
		string padPath = "res://assets/models/hq/Meshy_AI_HQ_Gunship_Landing_Pa_0807064955_image-to-3d-texture.glb";
		var padModel = LoadGlbModel(padPath);
		if (padModel != null)
		{
			padModel.Scale = new Vector3(4.0f, 1.5f, 4.0f);
			padModel.Position = new Vector3(16f, 0.1f, 8f);
			world3D.AddChild(padModel);
		}

		// 10. VEH_01: Axiom Resistance VTOL Gunship (stationary hovering above pad).
		string vtolPath = "res://assets/models/hq/Meshy_AI_Axiom_Resistance_VTOL_0807065619_image-to-3d-texture.glb";
		_vtolGunshipVehicle = LoadGlbModel(vtolPath);
		if (_vtolGunshipVehicle != null)
		{
			_vtolGunshipVehicle.Scale = new Vector3(3.0f, 3.0f, 3.0f);
			_vtolGunshipVehicle.Position = new Vector3(16f, 1.5f, 8f);
			_vtolGunshipVehicle.RotationDegrees = new Vector3(0f, -25f, 0f);
			world3D.AddChild(_vtolGunshipVehicle);
		}

		// 11. VEH_02: Cyberpunk Police Spinner (patrol craft).
		string spinnerPath = "res://assets/models/hq/Meshy_AI_Cyberpunk_Police_Spin_0807070257_image-to-3d-texture.glb";
		_spinnerVehicle = LoadGlbModel(spinnerPath);
		if (_spinnerVehicle != null)
		{
			_spinnerVehicle.Scale = new Vector3(2.2f, 2.2f, 2.2f);
			_spinnerVehicle.Position = new Vector3(-35f, 14f, -14f);
			_spinnerVehicle.RotationDegrees = new Vector3(0f, 90f, 0f);
			world3D.AddChild(_spinnerVehicle);
		}

		// 12. VEH_03: Cargo Transport Hover Barge (freight craft).
		string cargoPath = "res://assets/models/hq/Meshy_AI_Cargo_Transport_Hover_0807071108_image-to-3d-texture.glb";
		_cargoBargeVehicle = LoadGlbModel(cargoPath);
		if (_cargoBargeVehicle != null)
		{
			_cargoBargeVehicle.Scale = new Vector3(3.5f, 3.5f, 3.5f);
			_cargoBargeVehicle.Position = new Vector3(35f, 22f, -24f);
			_cargoBargeVehicle.RotationDegrees = new Vector3(0f, -90f, 0f);
			world3D.AddChild(_cargoBargeVehicle);
		}

		// 13. PLAZA_02: Energy Shield Pylons (entrance decoration).
		string pylonPath = "res://assets/models/hq/Meshy_AI_Energy_Shield_Pylon_0807073859_image-to-3d-texture.glb";
		var pylonLeft = LoadGlbModel(pylonPath);
		if (pylonLeft != null)
		{
			pylonLeft.Scale = new Vector3(1.8f, 3.0f, 1.8f);
			pylonLeft.Position = new Vector3(-7.5f, 0.5f, 13f);
			world3D.AddChild(pylonLeft);
		}
		var pylonRight = LoadGlbModel(pylonPath);
		if (pylonRight != null)
		{
			pylonRight.Scale = new Vector3(1.8f, 3.0f, 1.8f);
			pylonRight.Position = new Vector3(7.5f, 0.5f, 13f);
			world3D.AddChild(pylonRight);
		}

		// 14. PLAZA_03: Armored Perimeter Barriers.
		string barrierPath = "res://assets/models/hq/Meshy_AI_Armored_Perimeter_Bar_0807074359_image-to-3d-texture.glb";
		var barrierLeft = LoadGlbModel(barrierPath);
		if (barrierLeft != null)
		{
			barrierLeft.Scale = new Vector3(2.2f, 2.2f, 2.2f);
			barrierLeft.Position = new Vector3(-12f, 0.2f, 16f);
			world3D.AddChild(barrierLeft);
		}
		var barrierRight = LoadGlbModel(barrierPath);
		if (barrierRight != null)
		{
			barrierRight.Scale = new Vector3(2.2f, 2.2f, 2.2f);
			barrierRight.Position = new Vector3(12f, 0.2f, 16f);
			barrierRight.Position = new Vector3(12f, 0.2f, 16f);
			world3D.AddChild(barrierRight);
		}

		// 15. PLAZA_04: Automated Defense Turrets.
		string turretPath = "res://assets/models/hq/Meshy_AI_Automated_Defense_Tur_0807075515_image-to-3d-texture.glb";
		var turretLeft = LoadGlbModel(turretPath);
		if (turretLeft != null)
		{
			turretLeft.Scale = new Vector3(1.6f, 1.6f, 1.6f);
			turretLeft.Position = new Vector3(-12f, 2.4f, 16f);
			turretLeft.RotationDegrees = new Vector3(0f, 15f, 0f);
			world3D.AddChild(turretLeft);
		}
		var turretRight = LoadGlbModel(turretPath);
		if (turretRight != null)
		{
			turretRight.Scale = new Vector3(1.6f, 1.6f, 1.6f);
			turretRight.Position = new Vector3(12f, 2.4f, 16f);
			turretRight.RotationDegrees = new Vector3(0f, -15f, 0f);
			world3D.AddChild(turretRight);
		}

		// 16. PLAZA_05: Holo-Projector Pedestals.
		string pedestalPath = "res://assets/models/hq/Meshy_AI_Holo_Projector_Pedest_0807080445_image-to-3d-texture.glb";
		var pedestalModel = LoadGlbModel(pedestalPath);
		if (pedestalModel != null)
		{
			pedestalModel.Scale = new Vector3(2.0f, 1.0f, 2.0f);
			pedestalModel.Position = new Vector3(0f, 0.1f, 15f);
			world3D.AddChild(pedestalModel);
		}

		// 17. TECH_01: 3D Resistance Shield Emblem (floating focal point).
		string emblemPath = "res://assets/models/hq/Meshy_AI_Resistance_Shield_Emb_0807081351_image-to-3d-texture.glb";
		_shieldEmblemNode = LoadGlbModel(emblemPath);
		if (_shieldEmblemNode != null)
		{
			_shieldEmblemNode.Scale = new Vector3(3.2f, 3.2f, 3.2f);
			_shieldEmblemNode.Position = new Vector3(0f, 12.8f, 2.5f);
			world3D.AddChild(_shieldEmblemNode);
		}

		// 18. TECH_02: Cyber Billboard Frame.
		string billboardPath = "res://assets/models/hq/Meshy_AI_Cyber_Billboard_Frame_0807082322_image-to-3d-texture.glb";
		var billboardModel = LoadGlbModel(billboardPath);
		if (billboardModel != null)
		{
			billboardModel.Scale = new Vector3(4.5f, 4.5f, 4.5f);
			billboardModel.Position = new Vector3(-16f, 16f, -10f);
			billboardModel.RotationDegrees = new Vector3(0f, 20f, 0f);
			world3D.AddChild(billboardModel);
		}

		// 19. TECH_03: Terminal Kiosk Node (plaza info terminal).
		string kioskPath = "res://assets/models/hq/Meshy_AI_Terminal_Kiosk_Node_0807083122_image-to-3d-texture.glb";
		var kioskModel = LoadGlbModel(kioskPath);
		if (kioskModel != null)
		{
			kioskModel.Scale = new Vector3(1.6f, 1.6f, 1.6f);
			kioskModel.Position = new Vector3(-3.5f, 0.2f, 15.5f);
			kioskModel.RotationDegrees = new Vector3(0f, 30f, 0f);
			world3D.AddChild(kioskModel);
		}
	}

	/// <summary>
	/// Loads the high-fidelity 2D title logo (rendered via Blender) into the HomeOverlay.
	/// </summary>
	private void LoadHomeTitleLogo()
	{
		var titleTex = GD.Load<Texture2D>("res://assets/images/axiom_force_title_2d.png");
		if (titleTex == null)
		{
			titleTex = GD.Load<Texture2D>("res://assets/images/axiom_force_logo_clean.png");
		}

		if (_titleLogo != null)
		{
			_titleLogo.Texture = titleTex;
			_titleLogo.Visible = true;
			_titleLogo.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			_titleLogo.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			_titleLogo.CustomMinimumSize = new Vector2(750, 230);
		}
	}

	/// <summary>
	/// Utility method to instantiate a 3D model from a .glb file.
	/// Attempts loading via Godot's ResourceLoader first, then falls back to GltfDocument for raw file loading.
	/// </summary>
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
					return (Node3D)doc.GenerateScene(state);
				}
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"ThreeDMenuExperience: GLB load error: {ex.Message}");
		}
		return null;
	}
}
