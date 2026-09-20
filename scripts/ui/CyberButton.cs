using Godot;
using System;

/// <summary>
/// CyberButton is a high-fidelity 2.5D UI component.
/// It renders a 3D chamfered cyberpunk plate (cyber_button_mesh.glb) inside a transparent
/// SubViewport with real-time parallax tilt, metallic specular reflections, and neon emissions,
/// combined with a solid high-contrast cyber backing panel and floating 2D typography.
/// </summary>
[GlobalClass]
public partial class CyberButton : Control
{
	[Signal] public delegate void PressedEventHandler();

	// UI Configuration
	[Export] public string IndexText { get; set; } = "01";
	[Export] public string MainText { get; set; } = "DEPLOY MISSION";
	[Export] public string SubText { get; set; } = "// SECTOR ACCESS";
	[Export] public bool IsDisabled { get; set; } = false;

	// High-Contrast Cyber Backing Panel
	private Panel        _backingPanel;
	private StyleBoxFlat _backingStyle;

	// 3D Viewport Nodes
	private SubViewportContainer _viewportContainer;
	private SubViewport         _viewport;
	private Camera3D            _camera;
	private Node3D              _button3DRoot;
	private MeshInstance3D      _bodyMesh;
	private MeshInstance3D      _neonBorderMesh;
	private MeshInstance3D      _neonBarMesh;
	private MeshInstance3D      _cornerReticleMesh;
	private StandardMaterial3D  _bodyMat;
	private StandardMaterial3D  _neonBorderMat;
	private StandardMaterial3D  _neonBarMat;

	// 2D Typography Layer (Floating 2.5D Overlay)
	private Control _labelLayer;
	private Label   _indexLabel2D;
	private Label   _mainLabel2D;
	private Label   _subLabel2D;

	// Fonts
	private static Font _orbitronFont;
	private static Font _rajdhaniFont;

	// State tracking
	private bool  _isHovered = false;
	private bool  _isPressed = false;
	private float _parallaxIntensity = 0.12f; // Radians of max 3D tilt

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(240, 68);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		FocusMode = FocusModeEnum.All;
		MouseFilter = MouseFilterEnum.Stop;

		LoadFonts();
		SetupBackingPanel();
		Setup25DViewport();
		Setup2DLabelOverlay();

		UpdateChildLayout();

		// Mouse events
		MouseEntered += () =>
		{
			if (IsDisabled) return;
			_isHovered = true;
			AnimateHover(true);
			UISoundManager.Instance?.PlayHoverSound();
		};

		MouseExited += () =>
		{
			if (IsDisabled) return;
			_isHovered = false;
			_isPressed = false;
			AnimateHover(false);
		};

		// Gamepad / Keyboard Focus events
		FocusEntered += () =>
		{
			if (IsDisabled) return;
			_isHovered = true;
			AnimateHover(true);
			UISoundManager.Instance?.PlayHoverSound();
		};

		FocusExited += () =>
		{
			if (IsDisabled) return;
			_isHovered = false;
			_isPressed = false;
			AnimateHover(false);
		};
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized)
		{
			UpdateChildLayout();
		}
	}

	private static void LoadFonts()
	{
		if (_orbitronFont == null)
		{
			_orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		}
		if (_rajdhaniFont == null)
		{
			_rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");
		}
	}

	private void SetupBackingPanel()
	{
		_backingPanel = new Panel
		{
			Name = "BackingPanel",
			MouseFilter = MouseFilterEnum.Ignore
		};

		_backingStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.03f, 0.06f, 0.11f, 0.92f),
			BorderColor = new Color(0f, 0.94f, 1f, 0.50f),
			BorderWidthLeft = 4,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusBottomRight = 10,
			CornerDetail = 4
		};

		_backingPanel.AddThemeStyleboxOverride("panel", _backingStyle);
		AddChild(_backingPanel);
		_backingPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	private void Setup25DViewport()
	{
		// 1. Viewport container covering the button rect
		_viewportContainer = new SubViewportContainer
		{
			Name = "ViewportContainer",
			Stretch = true,
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_viewportContainer);
		_viewportContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// 2. SubViewport with OwnWorld3D = TRUE to isolate from Citadel world
		_viewport = new SubViewport
		{
			Name = "SubViewport",
			OwnWorld3D = true,
			TransparentBg = true,
			Size = new Vector2I(380, 80),
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			HandleInputLocally = false
		};
		_viewportContainer.AddChild(_viewport);

		// 3. Isolated Environment with Glow and Ambient Light
		var env = new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.ClearColor,
				AmbientLightColor = new Color(0.35f, 0.45f, 0.60f),
				AmbientLightEnergy = 1.2f,
				GlowEnabled = true,
				GlowBloom = 0.35f,
				GlowIntensity = 1.5f,
				GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive
			}
		};
		_viewport.AddChild(env);

		// 4. Directional key light angled for sharp metallic specular highlights
		var keyLight = new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-35, 35, 0),
			LightEnergy = 2.5f,
			LightColor = new Color(0.9f, 0.95f, 1.0f)
		};
		_viewport.AddChild(keyLight);

		// Secondary fill light for cyber ambient glow
		var fillLight = new DirectionalLight3D
		{
			RotationDegrees = new Vector3(30, -140, 0),
			LightEnergy = 1.0f,
			LightColor = new Color(0f, 0.8f, 1.0f)
		};
		_viewport.AddChild(fillLight);

		// 5. Camera looking straight at the 2.5D plate
		_camera = new Camera3D
		{
			Position = new Vector3(0, 0, 1.35f),
			Fov = 34f,
			Current = true
		};
		_viewport.AddChild(_camera);

		// 6. 3D Button Mesh Root
		_button3DRoot = new Node3D { Name = "Button3DRoot" };
		_viewport.AddChild(_button3DRoot);

		// 7. Load 3D model with fallback to raw GLTF loader
		Node3D modelInstance = LoadButtonModel();
		if (modelInstance != null)
		{
			_button3DRoot.AddChild(modelInstance);

			// Discover specific child meshes
			_bodyMesh = modelInstance.GetNodeOrNull<MeshInstance3D>("CyberButton");
			_neonBorderMesh = modelInstance.GetNodeOrNull<MeshInstance3D>("NeonBorder");
			_neonBarMesh = modelInstance.GetNodeOrNull<MeshInstance3D>("NeonBar");
			_cornerReticleMesh = modelInstance.GetNodeOrNull<MeshInstance3D>("CornerReticle");

			// Fallback search by iteration
			foreach (var child in modelInstance.FindChildren("*", "MeshInstance3D"))
			{
				if (child is MeshInstance3D mi)
				{
					if (mi.Name == "CyberButton" || (_bodyMesh == null && !mi.Name.ToString().Contains("Neon") && !mi.Name.ToString().Contains("Reticle")))
						_bodyMesh = mi;
					else if (mi.Name == "NeonBorder")
						_neonBorderMesh = mi;
					else if (mi.Name == "NeonBar")
						_neonBarMesh = mi;
					else if (mi.Name == "CornerReticle")
						_cornerReticleMesh = mi;
				}
			}
		}
		else
		{
			GD.PrintErr("CyberButton: Could not load cyber_button_mesh.glb, generating procedural 2.5D mesh.");
			_bodyMesh = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(3.2f, 0.7f, 0.12f) }
			};
			_button3DRoot.AddChild(_bodyMesh);
		}

		// 8. Apply High-Fidelity Cyber Materials
		_bodyMat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.09f, 0.14f, 0.22f),
			Metallic = 0.75f,
			Roughness = 0.25f,
			RimEnabled = true,
			Rim = 0.8f,
			RimTint = 0.5f
		};
		if (_bodyMesh != null) _bodyMesh.MaterialOverride = _bodyMat;

		_neonBorderMat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.0f, 0.94f, 1.0f),
			EmissionEnabled = true,
			Emission = new Color(0.0f, 0.94f, 1.0f),
			EmissionEnergyMultiplier = 3.0f,
			Roughness = 0.1f
		};
		if (_neonBorderMesh != null) _neonBorderMesh.MaterialOverride = _neonBorderMat;
		if (_cornerReticleMesh != null) _cornerReticleMesh.MaterialOverride = _neonBorderMat;

		_neonBarMat = new StandardMaterial3D
		{
			AlbedoColor = new Color(1.0f, 0.0f, 0.55f),
			EmissionEnabled = true,
			Emission = new Color(1.0f, 0.0f, 0.55f),
			EmissionEnergyMultiplier = 3.5f,
			Roughness = 0.1f
		};
		if (_neonBarMesh != null) _neonBarMesh.MaterialOverride = _neonBarMat;
	}

	private Node3D LoadButtonModel()
	{
		string resPath = "res://assets/models/hq/cyber_button_mesh.glb";
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
		catch (Exception ex)
		{
			GD.PrintErr($"CyberButton: Error loading GLB model: {ex.Message}");
		}
		return null;
	}

	private void UpdateChildLayout()
	{
		Vector2 size = Size;
		if (size.X <= 0 || size.Y <= 0)
		{
			size = CustomMinimumSize;
		}

		if (_backingPanel != null)
		{
			_backingPanel.Position = Vector2.Zero;
			_backingPanel.Size = size;
		}

		if (_viewportContainer != null)
		{
			_viewportContainer.Position = Vector2.Zero;
			_viewportContainer.Size = size;
		}

		if (_labelLayer != null)
		{
			_labelLayer.Position = Vector2.Zero;
			_labelLayer.Size = size;
		}

		if (_viewport != null)
		{
			_viewport.Size = new Vector2I((int)Mathf.Max(size.X, 100), (int)Mathf.Max(size.Y, 40));
		}

		if (_button3DRoot != null && size.Y > 0)
		{
			float aspect = size.X / size.Y;
			float baseAspect = 3.2f / 0.7f; // ~4.57
			_button3DRoot.Scale = new Vector3(aspect / baseAspect, 1f, 1f);
		}
	}

	private void Setup2DLabelOverlay()
	{
		_labelLayer = new Control
		{
			Name = "LabelLayer",
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_labelLayer);
		_labelLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Index Badge [01]
		_indexLabel2D = new Label
		{
			Name = "IndexLabel",
			Text = IndexText,
			Position = new Vector2(24, 22),
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_orbitronFont != null) _indexLabel2D.AddThemeFontOverride("font", _orbitronFont);
		_indexLabel2D.AddThemeFontSizeOverride("font_size", 16);
		_indexLabel2D.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 1f));
		_indexLabel2D.AddThemeColorOverride("font_outline_color", Colors.Black);
		_indexLabel2D.AddThemeConstantOverride("outline_size", 4);
		_labelLayer.AddChild(_indexLabel2D);

		// Main Title (e.g. DEPLOY MISSION)
		_mainLabel2D = new Label
		{
			Name = "MainLabel",
			Text = SafeToUpper(TranslateText(MainText)),
			Position = new Vector2(68, 12),
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_rajdhaniFont != null) _mainLabel2D.AddThemeFontOverride("font", _rajdhaniFont);
		_mainLabel2D.AddThemeFontSizeOverride("font_size", 21);
		_mainLabel2D.AddThemeColorOverride("font_color", new Color(0.98f, 0.99f, 1f, 1f));
		_mainLabel2D.AddThemeColorOverride("font_outline_color", Colors.Black);
		_mainLabel2D.AddThemeConstantOverride("outline_size", 5);
		_labelLayer.AddChild(_mainLabel2D);

		// Subtitle (e.g. // SECTOR ACCESS)
		_subLabel2D = new Label
		{
			Name = "SubLabel",
			Text = TranslateText(SubText),
			Position = new Vector2(68, 38),
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_rajdhaniFont != null) _subLabel2D.AddThemeFontOverride("font", _rajdhaniFont);
		_subLabel2D.AddThemeFontSizeOverride("font_size", 13);
		_subLabel2D.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.85f));
		_subLabel2D.AddThemeColorOverride("font_outline_color", Colors.Black);
		_subLabel2D.AddThemeConstantOverride("outline_size", 3);
		_labelLayer.AddChild(_subLabel2D);
	}

	public override void _Process(double delta)
	{
		if (IsDisabled || _camera == null) return;

		// 2.5D Parallax Tilting based on mouse offset from button center
		Vector2 mousePos = GetLocalMousePosition();
		Vector2 center = Size / 2f;
		Vector2 offset = Vector2.Zero;
		if (center.X > 0 && center.Y > 0)
		{
			offset = (mousePos - center) / center;
			offset = new Vector2(Mathf.Clamp(offset.X, -1.2f, 1.2f), Mathf.Clamp(offset.Y, -1.2f, 1.2f));
		}

		if (_isHovered)
		{
			float targetRotX = -offset.Y * _parallaxIntensity;
			float targetRotY = offset.X * _parallaxIntensity;
			_camera.Rotation = _camera.Rotation.Lerp(new Vector3(targetRotX, targetRotY, 0), (float)delta * 12f);

			// Subtle 2D label parallax offset for depth separation
			if (_labelLayer != null)
			{
				Vector2 targetOffset = new Vector2(offset.X * 3.5f, offset.Y * 2.0f);
				_labelLayer.Position = _labelLayer.Position.Lerp(targetOffset, (float)delta * 10f);
			}
		}
		else
		{
			_camera.Rotation = _camera.Rotation.Lerp(Vector3.Zero, (float)delta * 8f);
			if (_labelLayer != null)
			{
				_labelLayer.Position = _labelLayer.Position.Lerp(Vector2.Zero, (float)delta * 8f);
			}
		}
	}

	private void AnimateHover(bool hovering)
	{
		var tween = CreateTween().SetParallel(true);
		tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		float targetZ = hovering ? 0.12f : 0f;
		float borderEnergy = hovering ? 4.5f : 3.0f;
		float barEnergy = hovering ? 5.0f : 3.5f;
		Color mainColor = hovering ? Colors.White : new Color(0.98f, 0.99f, 1f, 1f);
		Color indexColor = hovering ? new Color(1f, 0f, 0.55f, 1f) : new Color(0f, 0.94f, 1f, 1f);

		Color targetBg = hovering ? new Color(0.06f, 0.14f, 0.24f, 0.96f) : new Color(0.03f, 0.06f, 0.11f, 0.92f);
		Color targetBorder = hovering ? new Color(0f, 0.98f, 1f, 1f) : new Color(0f, 0.94f, 1f, 0.50f);
		int targetBorderLeft = hovering ? 6 : 4;

		if (_backingStyle != null)
		{
			tween.TweenProperty(_backingStyle, "bg_color", targetBg, 0.22);
			tween.TweenProperty(_backingStyle, "border_color", targetBorder, 0.22);
			_backingStyle.BorderWidthLeft = targetBorderLeft;
		}
		if (_button3DRoot != null)
		{
			tween.TweenProperty(_button3DRoot, "position:z", targetZ, 0.22);
		}
		if (_neonBorderMat != null)
		{
			tween.TweenProperty(_neonBorderMat, "emission_energy_multiplier", borderEnergy, 0.22);
		}
		if (_neonBarMat != null)
		{
			tween.TweenProperty(_neonBarMat, "emission_energy_multiplier", barEnergy, 0.22);
		}
		if (_mainLabel2D != null)
		{
			tween.TweenProperty(_mainLabel2D, "theme_override_colors/font_color", mainColor, 0.2);
		}
		if (_indexLabel2D != null)
		{
			tween.TweenProperty(_indexLabel2D, "theme_override_colors/font_color", indexColor, 0.2);
		}
	}

	private void AnimatePress(bool pressed)
	{
		var tween = CreateTween().SetParallel(true);
		tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		float targetScale = pressed ? 0.96f : (_isHovered ? 1.0f : 1.0f);
		float targetZ = pressed ? -0.06f : (_isHovered ? 0.12f : 0f);

		if (_backingStyle != null)
		{
			Color pressBg = pressed ? new Color(0.01f, 0.03f, 0.06f, 0.98f) : (_isHovered ? new Color(0.06f, 0.14f, 0.24f, 0.96f) : new Color(0.03f, 0.06f, 0.11f, 0.92f));
			Color pressBorder = pressed ? new Color(1f, 0f, 0.55f, 1f) : (_isHovered ? new Color(0f, 0.98f, 1f, 1f) : new Color(0f, 0.94f, 1f, 0.50f));
			_backingStyle.BgColor = pressBg;
			_backingStyle.BorderColor = pressBorder;
		}
		if (_button3DRoot != null)
		{
			tween.TweenProperty(_button3DRoot, "scale", Vector3.One * targetScale, 0.1);
			tween.TweenProperty(_button3DRoot, "position:z", targetZ, 0.1);
		}
		if (_labelLayer != null)
		{
			tween.TweenProperty(_labelLayer, "scale", Vector2.One * targetScale, 0.1);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsDisabled || !HasFocus()) return;

		bool isAccept = @event.IsActionPressed("ui_accept") ||
			(@event is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == JoyButton.A);

		if (isAccept)
		{
			UISoundManager.Instance?.PlayClickSound();
			EmitSignal(SignalName.Pressed);
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (IsDisabled) return;

		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			if (mb.Pressed)
			{
				_isPressed = true;
				AnimatePress(true);
			}
			else if (_isPressed)
			{
				_isPressed = false;
				AnimatePress(false);
				if (_isHovered || GetGlobalRect().HasPoint(mb.GlobalPosition))
				{
					UISoundManager.Instance?.PlayClickSound();
					EmitSignal(SignalName.Pressed);
				}
			}
		}
	}

	private string TranslateText(string input)
	{
		if (string.IsNullOrEmpty(input)) return "";
		return Tr(input);
	}

	private static string SafeToUpper(string text)
	{
		if (string.IsNullOrEmpty(text)) return "";
		return text.ToUpper();
	}

	private string Tr(string text) => MainMenu.Tr(text);
}
