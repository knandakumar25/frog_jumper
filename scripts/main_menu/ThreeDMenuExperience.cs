using Godot;
using System;
using System.Collections.Generic;
using PhantomCamera;
using PhantomCamera.Noise;

/// <summary>
/// ThreeDMenuExperience is a specialized extension of MainMenu that transforms the static menu into a
/// cinematic 3D experience. It manages the rendering of the "Axiom Force HQ Citadel" 3D environment
/// in a SubViewport while keeping the title logo and menu buttons strictly as 2-D UI Control elements.
///
/// Architecture Design:
/// 1. 2-D UI Elements: The TitleLogo (TextureRect in HomeOverlay) and the five menu buttons (CyberButton
///    instances in ButtonContainer) are pure 2-D UI controls rendered directly onto the root Canvas,
///    enhanced with GoHUD chamfered and bracketed sci-fi panels.
/// 2. Full 3-D Viewport Staging: The entire world—including the distant 3-D megacity skyline, volumetric
///    sky atmosphere, Citadel architecture, ambient vehicles, lighting, and holographic projectors—lives
///    natively in 3-D inside SubViewportContainer/SubViewport3D/World3D (no 2-D background matte image).
/// 3. Phantom Camera Director: Dual PhantomCamera3D rigs (HomePhantomCam & MenuPhantomCam) with procedural
///    camera breathing noise drive the Camera3D via PhantomCameraHost.
/// </summary>
public partial class ThreeDMenuExperience : MainMenu
{
	private enum MenuState { Home, Transitioning, MainMenuState }
	private MenuState _currentState = MenuState.Home;

	// ─── 3D World & Phantom Camera References ─────────────────────────────────
	private Node3D          _hqModel;
	private Camera3D        _camera3D;
	private PhantomCamera3D _homePhantomCam;
	private PhantomCamera3D _menuPhantomCam;
	private Control         _homeOverlay;
	private Label           _promptLabel;
	private PanelContainer  _promptCutPanel;
	private TextureRect     _titleLogo;
	private Control         _titleContainer;
	private Control         _menuButtonsContainer;
	private Control         _navPromptFooter;
	private Control         _profileContainer;
	private Control         _statusTicker;
	private ColorRect       _fadeOverlay;
	private float           _pulseTimer = 0f;

	// ─── Camera Transform Constants ──────────────────────────────────────────────
	private static readonly Vector3 HighCamPos = new Vector3(0f, 10f, 26f);
	private static readonly Vector3 HighCamRot = new Vector3(-6f, 0f, 0f);

	private static readonly Vector3 PlazaCamPos = new Vector3(0f, 3.8f, 15f);
	private static readonly Vector3 PlazaCamRot = new Vector3(2f, 0f, 0f);

	// ─── Ambient Vehicle & Scene Node References ────────────────────────────────
	private Node3D _spinnerVehicle;
	private Node3D _cargoBargeVehicle;
	private Node3D _vtolGunshipVehicle;
	private Node3D _patrolDroneVehicle;
	private Node3D _shieldEmblemNode;
	private Node3D _holoRingsNode;
	private Node3D _operativeShogunNode;
	private readonly List<OmniLight3D> _skylineBeaconLights = new();

	// Patrol path data for ambient vehicles
	private float _spinnerX   = -60f;
	private float _cargoX     =  65f;
	private float _hoverTimer =  0f;
	private float _droneAngle =  0f;

	public override void _Ready()
	{
		base._Ready();

		// Resolve UI overlay and fade references.
		_homeOverlay          = GetNodeOrNull<Control>("HomeOverlay");
		_promptLabel          = GetNodeOrNull<Label>("HomeOverlay/CenterContainer/VBox/PromptLabel");
		_titleLogo            = GetNodeOrNull<TextureRect>("HomeOverlay/CenterContainer/VBox/TitleLogo");
		_menuButtonsContainer = GetNodeOrNull<Control>("ButtonContainer");
		_fadeOverlay          = GetNodeOrNull<ColorRect>("FadeOverlay");
		_navPromptFooter      = GetNodeOrNull<Control>("NavPromptFooter");
		_profileContainer     = GetNodeOrNull<Control>("ProfileContainer");
		_statusTicker         = GetNodeOrNull<Control>("StatusTicker");

		// Hide secondary Main Menu HUD elements while on the Home title screen so it stays clean and cinematic
		if (_navPromptFooter != null)
		{
			_navPromptFooter.Visible = false;
			_navPromptFooter.Modulate = new Color(1f, 1f, 1f, 0f);
		}
		if (_profileContainer != null)
		{
			_profileContainer.Visible = false;
			_profileContainer.Modulate = new Color(1f, 1f, 1f, 0f);
		}
		if (_statusTicker != null)
		{
			_statusTicker.Visible = false;
			_statusTicker.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		// Setup the 3D camera and Phantom Camera director rig.
		_camera3D = GetNodeOrNull<Camera3D>("SubViewportContainer/SubViewport3D/World3D/Camera3D");
		if (_camera3D != null)
		{
			_camera3D.Position = HighCamPos;
			_camera3D.RotationDegrees = HighCamRot;
		}

		_holoRingsNode = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D/HoloProjector/HoloFocusRings");

		var world3D = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D != null)
		{
			SetupPhantomCameraRig(world3D);
		}

		// Load the 3D architectural skyline, plaza assets, and localized title logo.
		LoadHQModel();
		if (world3D != null)
		{
			ApplyPbrAtmosphereTuning(world3D);
		}

		StyleHomeOverlayWithGoHud();
		UpdateLocalizedTitle();

		// Audio: Trigger the atmospheric "Home" music loop.
		GetNodeOrNull<Node>("/root/Global")?.Call("play_home_music");

		if (_homeOverlay != null)          _homeOverlay.Visible = true;
		_titleContainer       = GetNodeOrNull<Control>("TitleContainer");
		if (_titleContainer != null)
		{
			_titleContainer.AnchorLeft   = 0.03f;
			_titleContainer.AnchorTop    = 0.06f;
			_titleContainer.AnchorRight  = 0.38f;
			_titleContainer.AnchorBottom = 0.23f;
			_titleContainer.Modulate     = new Color(1f, 1f, 1f, 0f);
		}
		if (_profileContainer != null)
		{
			_profileContainer.AnchorLeft   = 0.70f;
			_profileContainer.AnchorTop    = 0.065f;
			_profileContainer.AnchorRight  = 0.96f;
			_profileContainer.AnchorBottom = 0.18f;
		}
		if (_menuButtonsContainer != null) _menuButtonsContainer.Modulate = new Color(1f, 1f, 1f, 0f);

		// Initial scene fade-in from black to avoid a jarring jump on load.
		if (_fadeOverlay != null)
		{
			_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			_fadeOverlay.Visible = true;
			_fadeOverlay.Modulate = Colors.Black;
			var tween = CreateTween();
			tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, 0.6f);
			tween.TweenCallback(Callable.From(() =>
			{
				if (_fadeOverlay != null)
				{
					_fadeOverlay.Visible = false;
					_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
				}
			}));
		}

		// Configure Operative Podium model & bind Universal/Ultimate idle animations
		SetupOperativePodiumGuard();
	}

	/// <summary>
	/// Initializes the PhantomCameraHost on Camera3D and creates two PhantomCamera3D nodes
	/// (HomePhantomCam and MenuPhantomCam) with procedural camera breathing noise.
	/// </summary>
	private void SetupPhantomCameraRig(Node3D world3D)
	{
		if (_camera3D == null) return;

		var hostScript = GD.Load<GDScript>("res://addons/phantom_camera/scripts/phantom_camera_host/phantom_camera_host.gd");
		var pcamScript = GD.Load<GDScript>("res://addons/phantom_camera/scripts/phantom_camera/phantom_camera_3d.gd");
		var tweenResScript = GD.Load<GDScript>("res://addons/phantom_camera/scripts/resources/tween_resource.gd");
		var noiseResScript = GD.Load<GDScript>("res://addons/phantom_camera/scripts/resources/phantom_camera_noise_3d.gd");

		if (hostScript == null || pcamScript == null) return;

		if (_camera3D.GetNodeOrNull<Node>("PhantomCameraHost") == null)
		{
			var hostNode = hostScript.New().As<Node>();
			if (hostNode != null)
			{
				hostNode.Name = "PhantomCameraHost";
				_camera3D.AddChild(hostNode);
			}
		}

		var homeNode = pcamScript.New().As<Node3D>();
		if (homeNode != null)
		{
			homeNode.Name = "HomePhantomCam";
			homeNode.Position = HighCamPos;
			homeNode.RotationDegrees = HighCamRot;
			world3D.AddChild(homeNode);
			_homePhantomCam = homeNode.AsPhantomCamera3D();
			_homePhantomCam.Priority = 20;

			if (noiseResScript != null)
			{
				var homeNoiseObj = noiseResScript.New().As<Resource>();
				if (homeNoiseObj != null)
				{
					var homeNoise = homeNoiseObj.AsPhantomCameraNoise3D();
					homeNoise.Amplitude = 0.42f;
					homeNoise.Frequency = 0.16f;
					homeNoise.RotationalNoise = true;
					homeNoise.PositionalNoise = true;
					homeNoise.RotationalMultiplierX = 0.25f;
					homeNoise.RotationalMultiplierY = 0.35f;
					homeNoise.RotationalMultiplierZ = 0.08f;
					homeNoise.PositionalMultiplierX = 0.20f;
					homeNoise.PositionalMultiplierY = 0.15f;
					homeNoise.PositionalMultiplierZ = 0.10f;
					_homePhantomCam.Noise = homeNoise;
				}
			}
		}

		var menuNode = pcamScript.New().As<Node3D>();
		if (menuNode != null)
		{
			menuNode.Name = "MenuPhantomCam";
			menuNode.Position = PlazaCamPos;
			menuNode.RotationDegrees = PlazaCamRot;
			world3D.AddChild(menuNode);
			_menuPhantomCam = menuNode.AsPhantomCamera3D();
			_menuPhantomCam.Priority = 10;

			if (tweenResScript != null)
			{
				var tweenObj = tweenResScript.New().As<Resource>();
				if (tweenObj != null)
				{
					var pcamTween = new PhantomCameraTween(tweenObj)
					{
						Duration = 2.0f,
						Transition = PhantomCamera.TransitionType.Cubic,
						Ease = PhantomCamera.EaseType.EaseInOut
					};
					_menuPhantomCam.TweenResource = pcamTween;
				}
			}

			if (noiseResScript != null)
			{
				var menuNoiseObj = noiseResScript.New().As<Resource>();
				if (menuNoiseObj != null)
				{
					var menuNoise = menuNoiseObj.AsPhantomCameraNoise3D();
					menuNoise.Amplitude = 0.16f;
					menuNoise.Frequency = 0.12f;
					menuNoise.RotationalNoise = true;
					menuNoise.PositionalNoise = false;
					menuNoise.RotationalMultiplierX = 0.18f;
					menuNoise.RotationalMultiplierY = 0.22f;
					menuNoise.RotationalMultiplierZ = 0.05f;
					_menuPhantomCam.Noise = menuNoise;
				}
			}
		}
	}

	/// <summary>
	/// Upgrades the HomeOverlay UI elements with a clean GoHUD chamfered sci-fi panel (GoStyleBoxCut)
	/// around the initialization prompt without overlapping text or cluttering the title logo.
	/// </summary>
	private void StyleHomeOverlayWithGoHud()
	{
		var vbox = GetNodeOrNull<VBoxContainer>("HomeOverlay/CenterContainer/VBox");
		if (vbox == null) return;

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var cutScript = GD.Load<GDScript>("res://addons/gohud/widgets/go_stylebox_cut.gd");

		// Wrap PromptLabel in a clean GoHUD Chamfered Tactical Call-To-Action Panel with generous padding
		if (_promptLabel != null && _promptLabel.GetParent() == vbox)
		{
			int promptIdx = _promptLabel.GetIndex();
			vbox.RemoveChild(_promptLabel);

			_promptCutPanel = new PanelContainer
			{
				Name = "PromptHudPanel",
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};

			if (cutScript != null)
			{
				var promptCut = cutScript.New().As<StyleBox>();
				if (promptCut != null)
				{
					promptCut.Set("bg_color", new Color(0.015f, 0.045f, 0.11f, 0.90f));
					promptCut.Set("border_color", new Color(0f, 0.90f, 1f, 0.85f));
					promptCut.Set("border_width", 1.8f);
					promptCut.Set("cut", 12.0f);
					promptCut.Set("cut_corners", 5); // DIAGONAL
					promptCut.Set("edge_color", new Color(1.0f, 0.14f, 0.58f, 0.95f));
					promptCut.Set("edge_width", 2.5f);
					promptCut.Set("edge_side", 1); // SIDE_TOP
					promptCut.Set("glow_color", new Color(0f, 0.92f, 1f, 0.25f));
					promptCut.Set("glow_size", 6.0f);
					promptCut.ContentMarginLeft = 40;
					promptCut.ContentMarginRight = 40;
					promptCut.ContentMarginTop = 14;
					promptCut.ContentMarginBottom = 14;
					_promptCutPanel.AddThemeStyleboxOverride("panel", promptCut);
				}
			}

			if (orbitronFont != null) _promptLabel.AddThemeFontOverride("font", orbitronFont);
			_promptLabel.AddThemeFontSizeOverride("font_size", 16);
			_promptLabel.AddThemeColorOverride("font_color", new Color(0.94f, 0.99f, 1f, 1f));
			_promptCutPanel.AddChild(_promptLabel);

			vbox.AddChild(_promptCutPanel);
			vbox.MoveChild(_promptCutPanel, Mathf.Clamp(promptIdx, 0, vbox.GetChildCount() - 1));
		}

		// Style VersionLabel at bottom of HomeOverlay
		var versionLbl = GetNodeOrNull<Label>("HomeOverlay/BottomMargin/VersionLabel");
		if (versionLbl != null)
		{
			if (orbitronFont != null) versionLbl.AddThemeFontOverride("font", orbitronFont);
			versionLbl.AddThemeFontSizeOverride("font_size", 13);
			versionLbl.AddThemeColorOverride("font_color", new Color(0.65f, 0.88f, 0.98f, 0.88f));
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_hoverTimer += dt;

		// Pulse the "Press Enter or Space" prompt using a smooth sine wave.
		if (_currentState == MenuState.Home && _promptLabel != null)
		{
			_pulseTimer += dt;
			float wave = Mathf.Sin(_pulseTimer * 3.2f) * 0.5f + 0.5f;
			float alpha = 0.45f + 0.55f * wave;
			_promptLabel.Modulate = new Color(1f, 1f, 1f, alpha);
		}

		// Ambient 3D World Animations:

		// 1. Police Spinner: Patrols the lower sky-highway lane with a gentle vertical sine-wave.
		if (_spinnerVehicle != null)
		{
			_spinnerX += 16f * dt;
			if (_spinnerX > 65f) _spinnerX = -65f;
			_spinnerVehicle.Position = new Vector3(_spinnerX, 15f + Mathf.Sin(_spinnerX * 0.25f) * 1.2f, -22f);
		}

		// 2. Cargo Barge: Moves slower along the upper freight lane.
		if (_cargoBargeVehicle != null)
		{
			_cargoX -= 10f * dt;
			if (_cargoX < -65f) _cargoX = 65f;
			_cargoBargeVehicle.Position = new Vector3(_cargoX, 24f + Mathf.Cos(_cargoX * 0.18f) * 1.5f, -30f);
		}

		// 3. VTOL Gunship: Performs a slow hovering sway above the landing pad.
		if (_vtolGunshipVehicle != null)
		{
			float hoverY = 1.6f + Mathf.Sin(_hoverTimer * 2.2f) * 0.35f;
			_vtolGunshipVehicle.Position = new Vector3(18f, hoverY, 6f);
		}

		// 4. Resistance Shield Emblem: Subtle rotation to imply a holographic projection.
		if (_shieldEmblemNode != null)
		{
			_shieldEmblemNode.RotationDegrees = new Vector3(0f, Mathf.Sin(_hoverTimer * 1.5f) * 8f, 0f);
		}

		// 5. Patrol Surveillance Drone: Sweeps a figure-8 patrol flight path above the plaza with banking
		if (_patrolDroneVehicle != null)
		{
			_droneAngle += 0.45f * dt;
			float droneX = Mathf.Sin(_droneAngle) * 20f;
			float droneZ = Mathf.Cos(_droneAngle * 0.5f) * 12f + 4f;
			float droneY = 13.5f + Mathf.Sin(_droneAngle * 2.0f) * 1.2f;
			_patrolDroneVehicle.Position = new Vector3(droneX, droneY, droneZ);

			float bankZ = -Mathf.Cos(_droneAngle) * 18f;
			float yawY = Mathf.RadToDeg(Mathf.Atan2(Mathf.Cos(_droneAngle) * 20f, -Mathf.Sin(_droneAngle * 0.5f) * 6f));
			_patrolDroneVehicle.RotationDegrees = new Vector3(0f, yawY, bankZ);
		}

		// 6. Holographic Focus Rings: Continuous slow rotation above the projector
		if (_holoRingsNode != null)
		{
			_holoRingsNode.RotateY(0.75f * dt);
		}

		// 7. Operative Podium Guard: Subtle tactical breathing & horizon scan facing the plaza camera
		if (_operativeShogunNode != null)
		{
			float breatheY = 0.35f + Mathf.Sin(_hoverTimer * 2.4f) * 0.025f;
			float scanYaw = -18.0f + Mathf.Sin(_hoverTimer * 0.85f) * 7.0f;
			_operativeShogunNode.Position = new Vector3(0f, breatheY, 0f);
			_operativeShogunNode.RotationDegrees = new Vector3(0f, scanYaw, 0f);
		}

		// 8. Distant 3D Skyline Aviation Warning Beacons: Asynchronous pulse
		for (int i = 0; i < _skylineBeaconLights.Count; i++)
		{
			var beacon = _skylineBeaconLights[i];
			if (beacon != null && IsInstanceValid(beacon))
			{
				beacon.LightEnergy = 1.4f + Mathf.Sin(_hoverTimer * 3.0f + i * 1.3f) * 1.0f;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_currentState == MenuState.Home)
		{
			bool isGamepad = (@event is InputEventJoypadButton jb && jb.Pressed) ||
							 (@event is InputEventJoypadMotion jm && Mathf.Abs(jm.AxisValue) > 0.5f);

			if ((@event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo) ||
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
	/// functional main menu via PhantomCamera3D priority switching, UI fading, and music shifts.
	/// </summary>
	private async void StartZoomTransition()
	{
		if (_currentState != MenuState.Home) return;
		_currentState = MenuState.Transitioning;

		UISoundManager.Instance?.PlayClickSound();
		UISoundManager.Instance?.PlayTransitionSound();

		if (_promptLabel != null)
		{
			_promptLabel.Modulate = new Color(1f, 0f, 0.5f, 1f);
			_promptLabel.Text = Tr("// SYSTEM INITIALIZED //");
		}

		GetNodeOrNull<Node>("/root/Global")?.Call("play_menu_music");

		// Trigger PhantomCamera3D priority transition from HomePhantomCam (20) to MenuPhantomCam (30)
		if (_menuPhantomCam != null)
		{
			_menuPhantomCam.Priority = 30;
		}

		var tween = CreateTween().SetParallel(true);

		if (_homeOverlay != null)
		{
			tween.TweenProperty(_homeOverlay, "modulate:a", 0.0f, 0.5f);
		}

		if (_camera3D != null)
		{
			tween.TweenProperty(_camera3D, "position", PlazaCamPos, 2.0f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
			tween.TweenProperty(_camera3D, "rotation_degrees", PlazaCamRot, 2.0f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
		}

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

		if (_navPromptFooter != null)
		{
			_navPromptFooter.Visible = true;
			tween.TweenProperty(_navPromptFooter, "modulate:a", 1.0f, 0.8f).SetDelay(1.0f);
		}

		if (_profileContainer != null)
		{
			_profileContainer.Visible = true;
			tween.TweenProperty(_profileContainer, "modulate:a", 1.0f, 0.8f).SetDelay(1.0f);
		}

		if (_statusTicker != null)
		{
			_statusTicker.Visible = true;
			tween.TweenProperty(_statusTicker, "modulate:a", 1.0f, 0.8f).SetDelay(1.0f);
		}

		await ToSignal(tween, Tween.SignalName.Finished);

		_currentState = MenuState.MainMenuState;
		if (_homeOverlay != null) _homeOverlay.Visible = false;

		if (_menuButtonsContainer != null)
		{
			_menuButtonsContainer.MouseFilter = MouseFilterEnum.Stop;
			var playBtn = _menuButtonsContainer.GetNodeOrNull<Control>("Play");
			playBtn?.CallDeferred(Control.MethodName.GrabFocus);
		}
	}

	/// <summary>
	/// Programmatically constructs the full-3D Cyber Command Center and distant 3D Megacity Skyline.
	/// Completely eliminates the legacy 2D background image quad in favor of multi-layered 3D architecture.
	/// </summary>
	private void LoadHQModel()
	{
		var world3D = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D == null) return;

		// Reposition HoloProjector into the central courtyard (Z = 5.0, aligned with TerminalKiosk & OperativePodium)
		// and upgrade its 2D quad to a 3D Axiom Orb
		var holoProjector = world3D.GetNodeOrNull<Node3D>("HoloProjector");
		if (holoProjector != null)
		{
			holoProjector.Position = new Vector3(0f, 0.05f, 5.0f);
			var flatHoloQuad = holoProjector.GetNodeOrNull<MeshInstance3D>("HoloProjection");
			if (flatHoloQuad != null)
			{
				flatHoloQuad.Visible = false;
			}
			var coreLens = holoProjector.FindChild("Core_Lens", true, false) as MeshInstance3D;
			if (coreLens != null && coreLens.Mesh != null && coreLens.Mesh.GetSurfaceCount() > 0)
			{
				if (coreLens.Mesh.SurfaceGetMaterial(0) is StandardMaterial3D lensMat)
				{
					var tonedMat = (StandardMaterial3D)lensMat.Duplicate();
					tonedMat.EmissionEnergyMultiplier = 0.65f;
					coreLens.SetSurfaceOverrideMaterial(0, tonedMat);
				}
			}
			var holoOrb = LoadGlbModel("res://assets/models/web/axiom_orb.glb");
			if (holoOrb != null)
			{
				holoOrb.Name = "HoloAxiomOrb";
				holoOrb.Scale = new Vector3(1.25f, 1.25f, 1.25f);
				holoOrb.Position = new Vector3(0f, 1.45f, 0f);
				if (_holoRingsNode != null)
				{
					_holoRingsNode.AddChild(holoOrb);
					holoOrb.Position = new Vector3(0f, 0.68f, 0f);
				}
				else
				{
					holoProjector.AddChild(holoOrb);
				}
			}
		}

		// 0. FULL 3D MEGACITY SKYLINE (Replaces legacy 2D backdrop quad):
		// Note: cyber_city_skyline.glb is authored with towers at local Y = -15..0, Z = -25..-58, X = -22..+22.
		// Using Scale = (1.8, 1.8, 1.0) and Position = (0, 27, -6) aligns the tower bases cleanly to Y = 0
		// at world Z = -31 to -64!
		string skylinePath = "res://assets/models/menu/cyber_city_skyline.glb";
		var skylineCenter = LoadGlbModel(skylinePath);
		if (skylineCenter != null)
		{
			skylineCenter.Name = "SkylineSectorCenter";
			skylineCenter.Scale = new Vector3(1.8f, 1.8f, 1.0f);
			skylineCenter.Position = new Vector3(0f, 27.0f, -6.0f);
			world3D.AddChild(skylineCenter);
		}

		var skylineLeft = LoadGlbModel(skylinePath);
		if (skylineLeft != null)
		{
			skylineLeft.Name = "SkylineSectorLeft";
			skylineLeft.Scale = new Vector3(1.5f, 1.6f, 1.0f);
			skylineLeft.Position = new Vector3(-44f, 24.0f, -10.0f);
			skylineLeft.RotationDegrees = new Vector3(0f, 12f, 0f);
			world3D.AddChild(skylineLeft);
		}

		var skylineRight = LoadGlbModel(skylinePath);
		if (skylineRight != null)
		{
			skylineRight.Name = "SkylineSectorRight";
			skylineRight.Scale = new Vector3(1.5f, 1.6f, 1.0f);
			skylineRight.Position = new Vector3(44f, 24.0f, -10.0f);
			skylineRight.RotationDegrees = new Vector3(0f, -12f, 0f);
			world3D.AddChild(skylineRight);
		}

		// Layer 0B: Ground Deck, Axiom Beacon Monuments & Architectural Fill Lighting.
		BuildProcedural3DSkylineAndDeck(world3D);

		// 1. PLAZA_01: The metallic floor grid forming the base of the plaza.
		string floorPath = "res://assets/models/hq/Meshy_AI_Metallic_Floor_Grid_0807073249_image-to-3d-texture.glb";
		var floorModel = LoadGlbModel(floorPath);
		if (floorModel != null)
		{
			floorModel.Scale = new Vector3(14.0f, 1.0f, 14.0f);
			floorModel.Position = new Vector3(0f, 0f, 0f);
			world3D.AddChild(floorModel);
		}

		// 2. ARCH_01: The centerpiece Axiom Force HQ Citadel building (set back so plaza courtyard is open).
		string hqPath = "res://assets/models/hq/Meshy_AI_Axiom_Force_HQ_Buildi_0806151721_image-to-3d-texture.glb";
		_hqModel = LoadGlbModel(hqPath);
		if (_hqModel != null)
		{
			_hqModel.Scale = new Vector3(15.0f, 15.0f, 15.0f);
			_hqModel.Position = new Vector3(0f, 6.2f, -14f);
			world3D.AddChild(_hqModel);
		}

		// 3. ARCH_02: Bio-Gen Corporate Spire (left canyon framing tower).
		string biogenPath = "res://assets/models/hq/Meshy_AI_Bio_Gen_Corporate_Spi_0807054611_image-to-3d-texture.glb";
		var biogenModel = LoadGlbModel(biogenPath);
		if (biogenModel != null)
		{
			biogenModel.Scale = new Vector3(11.0f, 11.0f, 11.0f);
			biogenModel.Position = new Vector3(-22f, 4.5f, -16f);
			world3D.AddChild(biogenModel);
		}

		// 4. ARCH_03: Neuro-Link Communication Tower (right canyon framing tower).
		string commTowerPath = "res://assets/models/hq/Meshy_AI_Neuro_Link_Comm_Tower_0807055157_image-to-3d-texture.glb";
		var commModel = LoadGlbModel(commTowerPath);
		if (commModel != null)
		{
			commModel.Scale = new Vector3(9.5f, 9.5f, 9.5f);
			commModel.Position = new Vector3(22f, 4.5f, -16f);
			world3D.AddChild(commModel);
		}

		// 5. ARCH_04: Syndicate Hab-Block (left mid-ground wing).
		string habBlockPath = "res://assets/models/hq/Meshy_AI_Syndicate_Hab_Block_0807060633_image-to-3d-texture.glb";
		var habModel = LoadGlbModel(habBlockPath);
		if (habModel != null)
		{
			habModel.Scale = new Vector3(8.0f, 8.0f, 8.0f);
			habModel.Position = new Vector3(-24f, 2.0f, -2f);
			world3D.AddChild(habModel);
		}

		// 6. ARCH_05: Aegis Defense Bastion (right mid-ground wing).
		string aegisPath = "res://assets/models/hq/Meshy_AI_Aegis_Defense_Bastion_0807061844_image-to-3d-texture.glb";
		var aegisModel = LoadGlbModel(aegisPath);
		if (aegisModel != null)
		{
			aegisModel.Scale = new Vector3(8.0f, 8.0f, 8.0f);
			aegisModel.Position = new Vector3(22f, 1.6f, -4f);
			world3D.AddChild(aegisModel);
		}

		// 7. INFRA_03: The gunship landing pad in the plaza.
		string padPath = "res://assets/models/hq/Meshy_AI_HQ_Gunship_Landing_Pa_0807064955_image-to-3d-texture.glb";
		var padModel = LoadGlbModel(padPath);
		if (padModel != null)
		{
			padModel.Scale = new Vector3(4.0f, 1.5f, 4.0f);
			padModel.Position = new Vector3(18f, 0.1f, 6f);
			world3D.AddChild(padModel);
		}

		// 8. VEH_01: Axiom Resistance VTOL Gunship (stationary hovering above pad).
		string vtolPath = "res://assets/models/hq/Meshy_AI_Axiom_Resistance_VTOL_0807065619_image-to-3d-texture.glb";
		_vtolGunshipVehicle = LoadGlbModel(vtolPath);
		if (_vtolGunshipVehicle != null)
		{
			_vtolGunshipVehicle.Scale = new Vector3(3.0f, 3.0f, 3.0f);
			_vtolGunshipVehicle.Position = new Vector3(18f, 1.5f, 6f);
			_vtolGunshipVehicle.RotationDegrees = new Vector3(0f, -25f, 0f);
			world3D.AddChild(_vtolGunshipVehicle);
		}

		// 9. VEH_02: Cyberpunk Police Spinner (patrol craft).
		string spinnerPath = "res://assets/models/hq/Meshy_AI_Cyberpunk_Police_Spin_0807070257_image-to-3d-texture.glb";
		_spinnerVehicle = LoadGlbModel(spinnerPath);
		if (_spinnerVehicle != null)
		{
			_spinnerVehicle.Scale = new Vector3(2.2f, 2.2f, 2.2f);
			_spinnerVehicle.Position = new Vector3(-35f, 15f, -22f);
			_spinnerVehicle.RotationDegrees = new Vector3(0f, 90f, 0f);
			world3D.AddChild(_spinnerVehicle);
		}

		// 10. VEH_03: Cargo Transport Hover Barge (freight craft).
		string cargoPath = "res://assets/models/hq/Meshy_AI_Cargo_Transport_Hover_0807071108_image-to-3d-texture.glb";
		_cargoBargeVehicle = LoadGlbModel(cargoPath);
		if (_cargoBargeVehicle != null)
		{
			_cargoBargeVehicle.Scale = new Vector3(3.5f, 3.5f, 3.5f);
			_cargoBargeVehicle.Position = new Vector3(35f, 24f, -30f);
			_cargoBargeVehicle.RotationDegrees = new Vector3(0f, -90f, 0f);
			world3D.AddChild(_cargoBargeVehicle);
		}

		// 11. PLAZA_02: Energy Shield Pylons (procedural hard-surface columns).
		string pylonPath = "res://assets/models/menu/shield_pylon_column.glb";
		var pylonLeft = LoadGlbModel(pylonPath);
		if (pylonLeft != null)
		{
			pylonLeft.Scale = new Vector3(1.2f, 1.2f, 1.2f);
			pylonLeft.Position = new Vector3(-7.5f, 0.0f, 13f);
			world3D.AddChild(pylonLeft);
		}
		var pylonRight = LoadGlbModel(pylonPath);
		if (pylonRight != null)
		{
			pylonRight.Scale = new Vector3(1.2f, 1.2f, 1.2f);
			pylonRight.Position = new Vector3(7.5f, 0.0f, 13f);
			world3D.AddChild(pylonRight);
		}

		// 12. PLAZA_03: Armored Perimeter Barriers.
		string barrierPath = "res://assets/models/menu/perimeter_blast_barrier.glb";
		var barrierLeft = LoadGlbModel(barrierPath);
		if (barrierLeft != null)
		{
			barrierLeft.Scale = new Vector3(1.2f, 1.2f, 1.2f);
			barrierLeft.Position = new Vector3(-12f, 0.0f, 16f);
			world3D.AddChild(barrierLeft);
		}
		var barrierRight = LoadGlbModel(barrierPath);
		if (barrierRight != null)
		{
			barrierRight.Scale = new Vector3(1.2f, 1.2f, 1.2f);
			barrierRight.Position = new Vector3(12f, 0.0f, 16f);
			world3D.AddChild(barrierRight);
		}

		// 13. PLAZA_04: Automated Plasma Defense Turrets.
		string turretPath = "res://assets/models/menu/plasma_defense_turret.glb";
		var turretLeft = LoadGlbModel(turretPath);
		if (turretLeft != null)
		{
			turretLeft.Scale = new Vector3(1.3f, 1.3f, 1.3f);
			turretLeft.Position = new Vector3(-12f, 1.3f, 16f);
			turretLeft.RotationDegrees = new Vector3(0f, 20f, 0f);
			world3D.AddChild(turretLeft);
		}
		var turretRight = LoadGlbModel(turretPath);
		if (turretRight != null)
		{
			turretRight.Scale = new Vector3(1.3f, 1.3f, 1.3f);
			turretRight.Position = new Vector3(12f, 1.3f, 16f);
			turretRight.RotationDegrees = new Vector3(0f, -20f, 0f);
			world3D.AddChild(turretRight);
		}

		// 14. PLAZA_05: Holo-Projector Pedestals (guard against duplicate if instanced in .tscn).
		if (!world3D.HasNode("HoloProjector") && !world3D.HasNode("HoloProjector2"))
		{
			string pedestalPath = "res://assets/models/menu/holo_projector_pedestal.glb";
			var pedestalModel = LoadGlbModel(pedestalPath);
			if (pedestalModel != null)
			{
				pedestalModel.Scale = new Vector3(1.2f, 1.2f, 1.2f);
				pedestalModel.Position = new Vector3(0f, 0.05f, 6.5f);
				world3D.AddChild(pedestalModel);
			}
		}

		// 15. TECH_01: 3D Resistance Shield Emblem (floating focal point above plaza).
		string emblemPath = "res://assets/models/menu/resistance_crest_emblem.glb";
		_shieldEmblemNode = LoadGlbModel(emblemPath);
		if (_shieldEmblemNode != null)
		{
			_shieldEmblemNode.Scale = new Vector3(2.2f, 2.2f, 2.2f);
			_shieldEmblemNode.Position = new Vector3(0f, 13.5f, -6.0f);
			world3D.AddChild(_shieldEmblemNode);
		}

		// 16. TECH_03: Terminal Kiosk Node (guard against duplicate if instanced in .tscn).
		if (!world3D.HasNode("TerminalKiosk") && !world3D.HasNode("TerminalKiosk2"))
		{
			string kioskPath = "res://assets/models/menu/command_terminal_kiosk.glb";
			var kioskModel = LoadGlbModel(kioskPath);
			if (kioskModel != null)
			{
				kioskModel.Scale = new Vector3(1.3f, 1.3f, 1.3f);
				kioskModel.Position = new Vector3(-3.5f, 0.0f, 15.5f);
				kioskModel.RotationDegrees = new Vector3(0f, 30f, 0f);
				world3D.AddChild(kioskModel);
			}
		}

		// 17. TECH_04: DataCore Server Towers (flanking the command plaza wings).
		string serverPath = "res://assets/models/menu/datacore_server_tower.glb";
		var serverLeft = LoadGlbModel(serverPath);
		if (serverLeft != null)
		{
			serverLeft.Scale = new Vector3(1.1f, 1.1f, 1.1f);
			serverLeft.Position = new Vector3(-13.5f, 0f, 6f);
			serverLeft.RotationDegrees = new Vector3(0f, 25f, 0f);
			world3D.AddChild(serverLeft);
		}
		var serverRight = LoadGlbModel(serverPath);
		if (serverRight != null)
		{
			serverRight.Scale = new Vector3(1.1f, 1.1f, 1.1f);
			serverRight.Position = new Vector3(13.5f, 0f, 6f);
			serverRight.RotationDegrees = new Vector3(0f, -25f, 0f);
			world3D.AddChild(serverRight);
		}

		// 18. TECH_05: Tactical Comm Array (atop the communications gantry).
		string commArrayPath = "res://assets/models/menu/tactical_comm_array.glb";
		var commArray = LoadGlbModel(commArrayPath);
		if (commArray != null)
		{
			commArray.Scale = new Vector3(1.3f, 1.3f, 1.3f);
			commArray.Position = new Vector3(22f, 9.5f, -16f);
			commArray.RotationDegrees = new Vector3(0f, -35f, 0f);
			world3D.AddChild(commArray);
		}

		// 19. VEH_04: Patrol Surveillance Drone (patrolling flight path above plaza).
		string dronePath = "res://assets/models/menu/patrol_surveillance_drone.glb";
		_patrolDroneVehicle = LoadGlbModel(dronePath);
		if (_patrolDroneVehicle != null)
		{
			_patrolDroneVehicle.Scale = new Vector3(1.8f, 1.8f, 1.8f);
			_patrolDroneVehicle.Position = new Vector3(0f, 13.5f, 8f);
			world3D.AddChild(_patrolDroneVehicle);
		}

		// 20. Attach 3D Thruster Plumes to VTOL Gunship for kinetic realism.
		string plumePath = "res://assets/models/menu/thruster_plume.glb";
		if (_vtolGunshipVehicle != null)
		{
			var vtolPlume = LoadGlbModel(plumePath);
			if (vtolPlume != null)
			{
				vtolPlume.Scale = new Vector3(0.6f, 0.6f, 0.6f);
				vtolPlume.Position = new Vector3(0f, -0.35f, 0f);
				_vtolGunshipVehicle.AddChild(vtolPlume);
			}
		}
	}

	/// <summary>
	/// Constructs the ground deck plane, Axiom Beacon monuments, pulsing skyline aviation warning beacons,
	/// and architectural fill lights, completely replacing any flat 2D backdrop.
	/// </summary>
	private void BuildProcedural3DSkylineAndDeck(Node3D world3D)
	{
		var skylineRoot = new Node3D { Name = "Procedural3DSkyline" };
		world3D.AddChild(skylineRoot);

		// 1. Continuous Dark Obsidian Ground Deck beneath Plaza & Skyline (prevents any horizon floor gap)
		var groundDeckMat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.015f, 0.022f, 0.038f, 1f),
			Metallic = 0.82f,
			Roughness = 0.42f
		};
		var groundDeckInst = new MeshInstance3D
		{
			Name = "CitadelGroundPlane",
			Mesh = new BoxMesh { Size = new Vector3(240f, 0.2f, 180f) },
			MaterialOverride = groundDeckMat,
			Position = new Vector3(0f, -0.15f, -35f)
		};
		skylineRoot.AddChild(groundDeckInst);

		// 2. Flanking Axiom Beacon Obelisk Monuments in the Plaza
		string beaconPath = "res://assets/models/web/axiom_beacon.glb";
		var beaconLeft = LoadGlbModel(beaconPath);
		if (beaconLeft != null)
		{
			beaconLeft.Scale = new Vector3(1.35f, 1.35f, 1.35f);
			beaconLeft.Position = new Vector3(-9.5f, 0.0f, 8.5f);
			skylineRoot.AddChild(beaconLeft);
		}
		var beaconRight = LoadGlbModel(beaconPath);
		if (beaconRight != null)
		{
			beaconRight.Scale = new Vector3(1.35f, 1.35f, 1.35f);
			beaconRight.Position = new Vector3(9.5f, 0.0f, 8.5f);
			skylineRoot.AddChild(beaconRight);
		}

		// 3. Pulsing Skyline Aviation Warning Beacon Lights across the Distant Horizon
		(float x, float y, float z, bool isCyan)[] beaconCoords =
		{
			(-62f, 32f, -46f, true),
			(-44f, 36f, -42f, false),
			(-26f, 34f, -40f, true),
			( -8f, 38f, -44f, false),
			(  8f, 38f, -44f, true),
			( 26f, 34f, -40f, false),
			( 44f, 36f, -42f, true),
			( 62f, 32f, -46f, false)
		};

		_skylineBeaconLights.Clear();
		foreach (var b in beaconCoords)
		{
			var beaconLight = new OmniLight3D
			{
				LightColor = b.isCyan ? new Color(0f, 0.95f, 1f, 1f) : new Color(1f, 0.15f, 0.55f, 1f),
				LightEnergy = 1.6f,
				OmniRange = 22.0f,
				Position = new Vector3(b.x, b.y, b.z)
			};
			skylineRoot.AddChild(beaconLight);
			_skylineBeaconLights.Add(beaconLight);
		}

		// 4. Architectural Fill, Operative Podium Key & Horizon Lighting Rig
		var citadelFrontLight = new OmniLight3D
		{
			Name = "CitadelFrontKeyLight",
			LightColor = new Color(0.35f, 0.88f, 1.0f, 1.0f),
			LightEnergy = 2.6f,
			OmniRange = 36.0f,
			OmniAttenuation = 1.1f,
			Position = new Vector3(0f, 14f, 2f)
		};
		skylineRoot.AddChild(citadelFrontLight);

		var leftWingFillLight = new OmniLight3D
		{
			Name = "LeftWingFillLight",
			LightColor = new Color(0.0f, 0.85f, 1.0f, 1.0f),
			LightEnergy = 2.2f,
			OmniRange = 32.0f,
			OmniAttenuation = 1.15f,
			Position = new Vector3(-18f, 12f, -4f)
		};
		skylineRoot.AddChild(leftWingFillLight);

		var rightWingFillLight = new OmniLight3D
		{
			Name = "RightWingFillLight",
			LightColor = new Color(1.0f, 0.22f, 0.62f, 1.0f),
			LightEnergy = 2.2f,
			OmniRange = 32.0f,
			OmniAttenuation = 1.15f,
			Position = new Vector3(18f, 12f, -4f)
		};
		skylineRoot.AddChild(rightWingFillLight);

		var operativeKeyLight = new OmniLight3D
		{
			Name = "OperativePodiumKeyLight",
			LightColor = new Color(0.75f, 0.92f, 1.0f, 1.0f),
			LightEnergy = 1.85f,
			OmniRange = 8.0f,
			OmniAttenuation = 1.1f,
			Position = new Vector3(5.5f, 2.8f, 7.2f)
		};
		skylineRoot.AddChild(operativeKeyLight);

		var horizonBacklight = new OmniLight3D
		{
			Name = "SkylineHorizonBacklight",
			LightColor = new Color(0.0f, 0.85f, 1.0f, 1.0f),
			LightEnergy = 2.8f,
			OmniRange = 95.0f,
			OmniAttenuation = 1.1f,
			Position = new Vector3(0f, 22f, -38f)
		};
		skylineRoot.AddChild(horizonBacklight);
	}

	/// <summary>
	/// Returns the resource path to the high-contrast 2D title logo matching the active locale.
	/// </summary>
	public static string GetLocalizedTitle2DPath()
	{
		string locale = TranslationServer.GetLocale().ToLower().Replace("-", "_");
		string langName = "english";

		if (locale is "arabic" || locale.StartsWith("ar_") || locale == "ar") langName = "arabic";
		else if (locale is "hebrew" || locale.StartsWith("he_") || locale == "he" || locale.StartsWith("iw_") || locale == "iw") langName = "hebrew";
		else if (locale is "japanese" || locale.StartsWith("ja_") || locale == "ja") langName = "japanese";
		else if (locale is "korean" || locale.StartsWith("ko_") || locale == "ko") langName = "korean";
		else if (locale is "mandarin" || locale == "zh_cn" || locale.StartsWith("zh_cn_") || locale.StartsWith("zh_hans")) langName = "mandarin";
		else if (locale is "traditionalchinese" || locale == "zh_tw" || locale == "zh_hk" || locale.StartsWith("zh_tw_") || locale.StartsWith("zh_hk_") || locale.StartsWith("zh_hant") || locale == "zh") langName = "traditionalchinese";
		else if (locale is "german" || locale.StartsWith("de_") || locale == "de") langName = "german";
		else if (locale is "eurofrench" || locale is "french" || locale.StartsWith("fr_") || locale == "fr") langName = "eurofrench";
		else if (locale is "italian" || locale.StartsWith("it_") || locale == "it") langName = "italian";
		else if (locale is "polish" || locale.StartsWith("pl_") || locale == "pl") langName = "polish";
		else if (locale is "brazilianportuguese" || locale is "portuguese" || locale.StartsWith("pt_") || locale == "pt") langName = "brazilianportuguese";
		else if (locale is "spanish" || locale.StartsWith("es_") || locale == "es") langName = "spanish";
		else if (locale is "thai" || locale.StartsWith("th_") || locale == "th") langName = "thai";
		else if (locale is "turkish" || locale.StartsWith("tr_") || locale == "tr") langName = "turkish";
		else if (locale is "vietnamese" || locale.StartsWith("vi_") || locale == "vi") langName = "vietnamese";
		else if (locale is "indonesian" || locale.StartsWith("id_") || locale == "id" || locale.StartsWith("in_") || locale == "in") langName = "indonesian";

		if (langName == "english")
		{
			return "res://assets/images/axiom_force_title_2d.png";
		}

		string candidate = $"res://assets/images/axiom_force_title_2d_{langName}.png";
		if (ResourceLoader.Exists(candidate) || System.IO.File.Exists(ProjectSettings.GlobalizePath(candidate)))
		{
			return candidate;
		}

		return "res://assets/images/axiom_force_title_2d.png";
	}

	/// <summary>
	/// Loads the high-fidelity 2D title logo (rendered via Blender) into the HomeOverlay and TitleContainer.
	/// </summary>
	private void LoadHomeTitleLogo()
	{
		string path = GetLocalizedTitle2DPath();
		var titleTex = GD.Load<Texture2D>(path);
		if (titleTex == null)
		{
			titleTex = GD.Load<Texture2D>("res://assets/images/axiom_force_title_2d.png");
		}
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

		var menuTitleLogo = GetNodeOrNull<TextureRect>("TitleContainer/MenuTitleLogo");
		if (menuTitleLogo != null && titleTex != null)
		{
			menuTitleLogo.Texture = titleTex;
		}
	}

	public override void UpdateLocalizedTitle()
	{
		LoadHomeTitleLogo();

		if (_promptLabel != null)
		{
			var globalNode = GetNodeOrNull<Node>("/root/Global");
			if (globalNode != null && globalNode.HasMethod("get_home_prompt"))
			{
				_promptLabel.Text = (string)globalNode.Call("get_home_prompt");
			}
			else
			{
				_promptLabel.Text = Tr("// PRESS ANY KEY OR CLICK TO INITIALIZE SYSTEM //");
			}
		}

		var versionLbl = GetNodeOrNull<Label>("HomeOverlay/BottomMargin/VersionLabel");
		if (versionLbl != null)
		{
			versionLbl.Text = $"v1.0.4  |  {Tr("RESISTANCE PROTOCOL ACTIVE")}  |  {Tr("AXIOM FORCE")}";
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

	private static void ApplyPbrAtmosphereTuning(Node node)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			int surfaceCount = meshInstance.Mesh.GetSurfaceCount();
			for (int s = 0; s < surfaceCount; s++)
			{
				var mat = meshInstance.GetSurfaceOverrideMaterial(s) ?? meshInstance.Mesh.SurfaceGetMaterial(s);
				if (mat is BaseMaterial3D baseMat && baseMat.ShadingMode != BaseMaterial3D.ShadingModeEnum.Unshaded)
				{
					var dup = (BaseMaterial3D)baseMat.Duplicate();
					dup.Roughness = Mathf.Clamp(dup.Roughness, 0.35f, 0.95f);
					dup.SpecularMode = BaseMaterial3D.SpecularModeEnum.SchlickGgx;
					dup.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Burley;
					meshInstance.SetSurfaceOverrideMaterial(s, dup);
				}
			}
		}

		foreach (Node child in node.GetChildren())
		{
			ApplyPbrAtmosphereTuning(child);
		}
	}

	private void SetupOperativePodiumGuard()
	{
		var world3D = GetNodeOrNull<Node3D>("SubViewportContainer/SubViewport3D/World3D");
		if (world3D == null) return;

		var podium = world3D.GetNodeOrNull<Node3D>("OperativePodium") ?? world3D.FindChild("OperativePodium", true, false) as Node3D;
		var shogun = podium?.GetNodeOrNull<Node3D>("CyberShogun") ?? world3D.FindChild("CyberShogun", true, false) as Node3D;
		if (shogun != null)
		{
			_operativeShogunNode = shogun;
			CharacterSelection.CleanDuplicateMeshes(shogun);
			shogun.Position = new Vector3(0f, 0.35f, 0f);
			shogun.RotationDegrees = Vector3.Zero;
			BindUniversalIdleAnimations(shogun);
			PlayIdleAnimation(shogun);
		}
	}

	/// <summary>
	/// Attempts to enrich the operative's AnimationPlayer with idle animations from the
	/// Universal Animation Library if the operative does not already have a dedicated idle clip.
	/// </summary>
	private void BindUniversalIdleAnimations(Node root)
	{
		var player = FindNodeRecursive<AnimationPlayer>(root);
		if (player == null) return;

		foreach (var animName in player.GetAnimationList())
		{
			if (animName.ToString().ToLowerInvariant().Contains("idle"))
				return; // Already has native idle clip
		}

		const string univLibPath = "res://assets/animations/universal_animation_library/Godot/AnimationLibrary_Godot_Standard.glb";
		if (!ResourceLoader.Exists(univLibPath)) return;

		var libNode = LoadGlbModel(univLibPath);
		if (libNode == null) return;

		var sourcePlayer = FindNodeRecursive<AnimationPlayer>(libNode);
		if (sourcePlayer != null)
		{
			foreach (var libKey in sourcePlayer.GetAnimationLibraryList())
			{
				var lib = sourcePlayer.GetAnimationLibrary(libKey);
				if (lib != null && !player.HasAnimationLibrary("universal"))
				{
					player.AddAnimationLibrary("universal", lib);
					break;
				}
			}
		}
		libNode.QueueFree();
	}

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

	private static T? FindNodeRecursive<T>(Node node) where T : Node
	{
		if (node is T match) return match;
		foreach (Node child in node.GetChildren())
		{
			var result = FindNodeRecursive<T>(child);
			if (result != null) return result;
		}
		return null;
	}
}
