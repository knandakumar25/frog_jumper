using Godot;
using System;

/// <summary>
/// CyberButton is a high-fidelity 2D UI component.
/// It renders pre-rendered Blender PBR 9-slice textures (normal, hover, pressed)
/// with crisp 2D typography overlays, responsive scaling, hover/press animations,
/// and full keyboard/gamepad navigation support.
/// </summary>
[GlobalClass]
public partial class CyberButton : Control
{
	[Signal] public delegate void PressedEventHandler();

	private string _indexText = "01";
	private string _mainText = "DEPLOY MISSION";
	private string _subText = "// SECTOR ACCESS";

	// UI Configuration
	[Export] public string IndexText
	{
		get => _indexText;
		set
		{
			_indexText = value;
			if (_indexLabel2D != null) _indexLabel2D.Text = _indexText;
		}
	}

	[Export] public string MainText
	{
		get => _mainText;
		set
		{
			_mainText = value;
			if (_mainLabel2D != null)
			{
				_mainLabel2D.Text = SafeToUpper(TranslateText(_mainText));
			}
		}
	}

	[Export] public string SubText
	{
		get => _subText;
		set
		{
			_subText = value;
			if (_subLabel2D != null)
			{
				_subLabel2D.Text = TranslateText(_subText);
			}
		}
	}

	private bool _isDisabled;
	[Export] public bool IsDisabled
	{
		get => _isDisabled;
		set
		{
			_isDisabled = value;
			if (value) _isPressed = false;
			if (IsInsideTree() && _ninePatch != null) AnimatePress(false);
		}
	}

	// 2D NinePatch Background
	private NinePatchRect _ninePatch;
	private static Texture2D? _texNormal;
	private static Texture2D? _texHover;
	private static Texture2D? _texPressed;

	// 2D Typography Layer
	private Control _labelLayer;
	private Label   _indexLabel2D;
	private Label   _mainLabel2D;
	private Label   _subLabel2D;

	// Fonts
	private static Font _orbitronFont;
	private static Font _rajdhaniFont;

	// State tracking
	private bool _isHovered = false;
	private bool _isPressed = false;
	private bool _pointerInside;
	private Tween _scaleTween;
	private Tween _colorTween;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(240, 68);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		FocusMode = FocusModeEnum.All;
		MouseFilter = MouseFilterEnum.Stop;

		LoadStaticResources();
		SetupNinePatch();
		Setup2DLabelOverlay();

		UpdateChildLayout();

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
			_isHovered = _pointerInside;
			_isPressed = false;
			AnimateHover(_isHovered && !IsDisabled);
		};
		MouseEntered += () =>
		{
			_pointerInside = true;
			if (IsDisabled) return;
			bool wasHovered = _isHovered;
			_isHovered = true;
			AnimateHover(true);
			if (_isPressed) AnimatePress(true);
			if (!wasHovered) UISoundManager.Instance?.PlayHoverSound();
		};
		MouseExited += () =>
		{
			_pointerInside = false;
			_isHovered = HasFocus();
			AnimateHover(_isHovered && !IsDisabled);
		};
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized)
		{
			UpdateChildLayout();
		}
		else if (what == NotificationTranslationChanged)
		{
			UpdateLocalization();
		}
		else if (what == NotificationVisibilityChanged && !IsVisibleInTree())
		{
			_isPressed = false;
			_pointerInside = false;
		}
	}

	/// <summary>
	/// Loads the shared button textures and fonts once. The three button images were deleted on
	/// 2026-11-12 and await SpriteCook regeneration (<c>docs/ART_REGENERATION.md</c>); until they
	/// return, <c>OptionalArt</c> yields null, the nine-patch draws nothing and the button shows its
	/// text only. A regenerated file at the same path is used with no code change.
	/// </summary>
	private static void LoadStaticResources()
	{
		_texNormal ??= OptionalArt.Texture("res://assets/images/cyber_button_normal.png");
		_texHover ??= OptionalArt.Texture("res://assets/images/cyber_button_hover.png");
		_texPressed ??= OptionalArt.Texture("res://assets/images/cyber_button_pressed.png");

		if (_orbitronFont == null)
		{
			_orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		}
		if (_rajdhaniFont == null)
		{
			_rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");
		}
	}

	private void SetupNinePatch()
	{
		_ninePatch = new NinePatchRect
		{
			Name = "NinePatchBackground",
			Texture = _texNormal,
			PatchMarginLeft = 70,
			PatchMarginRight = 70,
			PatchMarginTop = 16,
			PatchMarginBottom = 16,
			AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Stretch,
			AxisStretchVertical = NinePatchRect.AxisStretchMode.Stretch,
			MouseFilter = MouseFilterEnum.Ignore
		};

		AddChild(_ninePatch);
		_ninePatch.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	private void Setup2DLabelOverlay()
	{
		_labelLayer = new Control
		{
			Name = "LabelOverlay",
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_labelLayer);
		_labelLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Number Callout (e.g. "01")
		_indexLabel2D = new Label
		{
			Name = "IndexLabel",
			Text = IndexText,
			Position = new Vector2(28, 22),
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_orbitronFont != null) _indexLabel2D.AddThemeFontOverride("font", _orbitronFont);
		_indexLabel2D.AddThemeFontSizeOverride("font_size", 15);
		_indexLabel2D.AddThemeColorOverride("font_color", new Color(0f, 0.52f, 0.65f, 1f));
		_indexLabel2D.AddThemeColorOverride("font_outline_color", Colors.Black);
		_indexLabel2D.AddThemeConstantOverride("outline_size", 4);
		_labelLayer.AddChild(_indexLabel2D);

		// Main Title (e.g. PLAY, DEPLOY MISSION)
		_mainLabel2D = new Label
		{
			Name = "MainLabel",
			Text = SafeToUpper(TranslateText(MainText)),
			Position = new Vector2(72, 12),
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_rajdhaniFont != null) _mainLabel2D.AddThemeFontOverride("font", _rajdhaniFont);
		_mainLabel2D.AddThemeFontSizeOverride("font_size", 22);
		_mainLabel2D.AddThemeColorOverride("font_color", new Color(0.62f, 0.70f, 0.78f, 1f));
		_mainLabel2D.AddThemeColorOverride("font_outline_color", Colors.Black);
		_mainLabel2D.AddThemeConstantOverride("outline_size", 5);
		_labelLayer.AddChild(_mainLabel2D);

		// Subtitle (e.g. // DEPLOY MISSION)
		_subLabel2D = new Label
		{
			Name = "SubLabel",
			Text = TranslateText(SubText),
			Position = new Vector2(72, 38),
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_rajdhaniFont != null) _subLabel2D.AddThemeFontOverride("font", _rajdhaniFont);
		_subLabel2D.AddThemeFontSizeOverride("font_size", 13);
		_subLabel2D.AddThemeColorOverride("font_color", new Color(0f, 0.48f, 0.60f, 0.90f));
		_subLabel2D.AddThemeColorOverride("font_outline_color", Colors.Black);
		_subLabel2D.AddThemeConstantOverride("outline_size", 3);
		_labelLayer.AddChild(_subLabel2D);
	}

	private void UpdateChildLayout()
	{
		Vector2 size = Size;
		if (size.X <= 0 || size.Y <= 0)
		{
			size = CustomMinimumSize;
		}

		PivotOffset = size / 2f;

		float maxLabelWidth = Mathf.Max(size.X - 110, 100);
		if (_mainLabel2D != null)
		{
			_mainLabel2D.Size = new Vector2(maxLabelWidth, 28);
		}
		if (_subLabel2D != null)
		{
			_subLabel2D.Size = new Vector2(maxLabelWidth, 20);
		}
	}

	private void AnimateHover(bool hovering)
	{
		if (_ninePatch != null)
		{
			_ninePatch.Texture = hovering ? (_texHover ?? _texNormal) : (_texNormal ?? _texNormal);
		}

		_scaleTween?.Kill();
		_scaleTween = CreateTween();
		_scaleTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		Vector2 targetScale = hovering ? new Vector2(1.025f, 1.025f) : Vector2.One;
		_scaleTween.TweenProperty(this, "scale", targetScale, 0.18);

		_colorTween?.Kill();
		_colorTween = CreateTween().SetParallel(true);
		_colorTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		Color mainColor = hovering ? new Color(0.85f, 0.90f, 0.95f, 1f) : new Color(0.62f, 0.70f, 0.78f, 1f);
		Color indexColor = hovering ? new Color(0.75f, 0f, 0.38f, 1f) : new Color(0f, 0.52f, 0.65f, 1f);

		if (_mainLabel2D != null)
		{
			_colorTween.TweenProperty(_mainLabel2D, "theme_override_colors/font_color", mainColor, 0.18);
		}
		if (_indexLabel2D != null)
		{
			_colorTween.TweenProperty(_indexLabel2D, "theme_override_colors/font_color", indexColor, 0.18);
		}
	}

	private void AnimatePress(bool pressed)
	{
		if (_ninePatch != null)
		{
			if (pressed)
				_ninePatch.Texture = _texPressed ?? _texHover ?? _texNormal;
			else
				_ninePatch.Texture = _isHovered ? (_texHover ?? _texNormal) : (_texNormal ?? _texNormal);
		}

		_scaleTween?.Kill();
		_scaleTween = CreateTween();
		_scaleTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		Vector2 targetScale = pressed ? new Vector2(0.97f, 0.97f) : (_isHovered ? new Vector2(1.025f, 1.025f) : Vector2.One);
		_scaleTween.TweenProperty(this, "scale", targetScale, 0.1);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsDisabled || !HasFocus()) return;

		bool isAccept = @event.IsActionPressed("ui_accept") ||
			(@event is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == JoyButton.A) ||
			(@event is InputEventKey k && k.Pressed && !k.Echo && (k.Keycode == Key.Enter || k.Keycode == Key.Space));

		if (isAccept)
		{
			UISoundManager.Instance?.PlayClickSound();
			EmitSignal(SignalName.Pressed);
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		InputPromptService.Instance?.ObserveInput(@event);
		if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse) return;
		// Disabled controls still own their click region, preventing gameplay beneath them from firing.
		AcceptEvent();
		if (IsDisabled) { _isPressed = false; return; }
		if (mouse.Pressed)
		{
			GrabFocus();
			_isPressed = true;
			AnimatePress(true);
			return;
		}
		bool activate = _isPressed && new Rect2(Vector2.Zero, Size).HasPoint(mouse.Position);
		_isPressed = false;
		AnimatePress(false);
		if (!activate) return;
		UISoundManager.Instance?.PlayClickSound();
		EmitSignal(SignalName.Pressed);
	}

	public void UpdateLocalization()
	{
		if (_mainLabel2D != null && !string.IsNullOrEmpty(_mainText))
		{
			_mainLabel2D.Text = SafeToUpper(TranslateText(_mainText));
		}
		if (_subLabel2D != null && !string.IsNullOrEmpty(_subText))
		{
			_subLabel2D.Text = TranslateText(_subText);
		}
	}

	private string TranslateText(string input)
	{
		if (string.IsNullOrEmpty(input)) return "";

		// 1. Direct translation
		string translated = Tr(input);
		if (translated != input) return translated;

		// 2. If it's a "// SUBTEXT" formatted string, try translating the inner string
		string trimmed = input.Trim();
		if (trimmed.StartsWith("//"))
		{
			string inner = trimmed.Substring(2).Trim();
			bool hasTrailing = inner.EndsWith("//");
			if (hasTrailing) inner = inner.Substring(0, inner.Length - 2).Trim();

			string trInner = Tr(inner);
			if (trInner != inner)
			{
				return hasTrailing ? $"// {trInner} //" : $"// {trInner}";
			}
		}

		// 3. Fallback for common button uppercase keys: PLAY -> Play, QUIT -> Quit, SETTINGS -> Settings
		if (trimmed.Equals("PLAY", StringComparison.OrdinalIgnoreCase))
		{
			string trVal = Tr("Play");
			if (trVal != "Play") return trVal;
		}
		else if (trimmed.Equals("SETTINGS", StringComparison.OrdinalIgnoreCase))
		{
			string trVal = Tr("Settings");
			if (trVal != "Settings") return trVal;
		}
		else if (trimmed.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
		{
			string trVal = Tr("Quit");
			if (trVal != "Quit") return trVal;
		}
		else if (trimmed.Equals("CONTINUE", StringComparison.OrdinalIgnoreCase))
		{
			string trVal = Tr("Continue");
			if (trVal != "Continue") return trVal;
		}
		else if (trimmed.Equals("HOW TO PLAY", StringComparison.OrdinalIgnoreCase))
		{
			string trVal = Tr("How to Play");
			if (trVal != "How to Play") return trVal;
		}

		return input;
	}

	/// <summary>Preserves authored casing, including Turkish dotted I and scripts without letter case.</summary>
	private static string SafeToUpper(string text) => text ?? "";
	private string Tr(string text) => MainMenu.Tr(text);
}
