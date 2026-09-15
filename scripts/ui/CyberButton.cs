using Godot;
using System;

/// <summary>
/// CyberButton is a high-fidelity, custom-drawn UI component that implements a
/// "Cyberpunk" aesthetic. It features asymmetric 45° chamfered corners,
/// dynamic neon color interpolation, and a programmatic slide animation on hover.
///
/// Unity Migration Note:
/// 1. Visuals: Instead of DrawPolygon in _Draw(), use a custom UI Mesh or a
///    Sliced Sprite (9-slice) with a custom mask to achieve the chamfered corners.
/// 2. Animations: Replace Godot Tweens with DOTween or LeanTween for the slide
///    and color lerping effects.
/// 3. Text: Use TextMeshPro (TMP) for all labels. The complex translation logic
///    should be moved to a dedicated 'LocalizationUtility' class.
/// 4. Interaction: Implement the IPointerEnterHandler, IPointerExitHandler, and
///    IPointerClickHandler interfaces from UnityEngine.EventSystems.
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

	// Animation and State tracking
	private bool _isHovered = false;
	private bool _isPressed = false;
	private float _slideOffset = 0f;
	private float _hoverProgress = 0f; // Normalized value (0.0 to 1.0) for smooth lerping

	// Global font resources for consistent sci-fi branding.
	private static readonly FontFile OrbitronFont = GD.Load<FontFile>("res://assets/fonts/Orbitron-VariableFont.ttf");
	private static readonly FontFile RajdhaniFont = GD.Load<FontFile>("res://assets/fonts/Rajdhani-Bold.ttf");

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(0, 68);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		FocusMode = FocusModeEnum.All;
		MouseFilter = MouseFilterEnum.Stop;

		// Hover Logic: Mouse Entry
		MouseEntered += () =>
		{
			if (IsDisabled) return;
			_isHovered = true;
			AnimateHover(true);
			UISoundManager.Instance?.PlayHoverSound();
		};

		// Hover Logic: Mouse Exit
		MouseExited += () =>
		{
			if (IsDisabled) return;
			_isHovered = false;
			_isPressed = false;
			AnimateHover(false);
		};

		// Gamepad Navigation: Focus Entry
		FocusEntered += () =>
		{
			if (IsDisabled) return;
			_isHovered = true;
			AnimateHover(true);
			UISoundManager.Instance?.PlayHoverSound();
		};

		// Gamepad Navigation: Focus Exit
		FocusExited += () =>
		{
			if (IsDisabled) return;
			_isHovered = false;
			_isPressed = false;
			AnimateHover(false);
		};
	}

	/// <summary>
	/// Handles global input events, specifically ensuring gamepad "Accept" (A button)
	/// triggers the button action when focused.
	/// </summary>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsDisabled) return;
		if (!HasFocus()) return;

		bool isAccept = @event.IsActionPressed("ui_accept") ||
			(@event is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == JoyButton.A);

		if (isAccept)
		{
			UISoundManager.Instance?.PlayClickSound();
			EmitSignal(SignalName.Pressed);
			GetViewport()?.SetInputAsHandled();
		}
	}

	/// <summary>
	/// Handles direct UI input, including mouse clicks and gamepad interactions.
	/// </summary>
	public override void _GuiInput(InputEvent @event)
	{
		if (IsDisabled) return;

		bool isAccept = @event.IsActionPressed("ui_accept") ||
			(@event is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == JoyButton.A);

		if (isAccept)
		{
			UISoundManager.Instance?.PlayClickSound();
			EmitSignal(SignalName.Pressed);
			GetViewport()?.SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			if (mb.Pressed)
			{
				_isPressed = true;
				QueueRedraw();
			}
			else if (_isPressed)
			{
				_isPressed = false;
				QueueRedraw();
				// Trigger action only if the mouse is still over the button upon release.
				if (_isHovered || GetGlobalRect().HasPoint(mb.GlobalPosition))
				{
					UISoundManager.Instance?.PlayClickSound();
					EmitSignal(SignalName.Pressed);
				}
			}
		}
	}

	/// <summary>
	/// Coordinates the smooth transition of the button's visual state (Slide and Color).
	/// </summary>
	private void AnimateHover(bool hovering)
	{
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		float targetOffset = hovering ? 14f : 0f;
		float targetProgress = hovering ? 1.0f : 0.0f;

		tween.TweenProperty(this, "_slideOffset", targetOffset, 0.2);
		tween.TweenProperty(this, "_hoverProgress", targetProgress, 0.2);
		tween.TweenCallback(Callable.From(QueueRedraw));
	}

	public override void _Process(double delta)
	{
		// Ensure the button redraws while the hover transition is active.
		if (_hoverProgress > 0f && _hoverProgress < 1f)
		{
			QueueRedraw();
		}
	}

	/// <summary>
	/// Implements a multi-stage translation attempt to ensure that the localized
	/// text maintains the intended "Cyber-Caps" styling regardless of the source format.
	/// </summary>
	private string TranslateText(string input)
	{
		if (string.IsNullOrEmpty(input)) return "";
		string clean = input.Trim();

		// Handle leading "// " subtitle tags as a special case.
		string prefix = "";
		if (clean.StartsWith("// "))
		{
			prefix = "// ";
			clean = clean.Substring(3).Trim();
		}

		// Stage 1: Direct translation check.
		string trans = Tr(clean);
		if (trans != clean) return prefix + trans;

		// Stage 2: Explicit match for specific high-priority labels.
		if (clean.Equals("HOW TO PLAY", StringComparison.OrdinalIgnoreCase))
		{
			trans = Tr("How to Play");
			if (trans != "How to Play") return prefix + trans;
		}

		// Stage 3: Attempt translation via Title Case.
		string titleCase = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean.ToLower());
		trans = Tr(titleCase);
		if (trans != titleCase) return prefix + trans;

		// Stage 4: Attempt translation via Title Case with a lowercase preposition ("To").
		string titleCaseLowerTo = titleCase.Replace(" To ", " to ");
		trans = Tr(titleCaseLowerTo);
		if (trans != titleCaseLowerTo) return prefix + trans;

		// Stage 5: Attempt translation via Capitalized first letter.
		string cap = clean.Length > 1 ? char.ToUpper(clean[0]) + clean.Substring(1).ToLower() : clean;
		trans = Tr(cap);
		if (trans != cap) return prefix + trans;

		return prefix + clean;
	}

	/// <summary>
	/// Converts text to uppercase only for locales that support the concept of casing.
	/// Prevents corruption of scripts like Arabic, Japanese, Korean, or Chinese.
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

	/// <summary>
	/// Selects the appropriate font based on the current locale to ensure glyph support.
	/// </summary>
	private static Font GetActiveFont(Font defaultFont)
	{
		string locale = TranslationServer.GetLocale().ToLower();
		if (locale.Contains("ar"))
		{
			return GD.Load<Font>("res://assets/fonts/Cairo.ttf") ?? defaultFont;
		}
		if (locale.Contains("he") || locale.Contains("ja") || locale.Contains("ko") || locale.Contains("zh"))
		{
			return GD.Load<Font>("res://assets/fonts/NotoSans-Regular.ttf") ?? defaultFont;
		}
		return defaultFont;
	}

	/// <summary>
	/// Performs the low-level vector drawing of the button's cyberpunk frame and labels.
	/// </summary>
	public override void _Draw()
	{
		Vector2 size = Size;
		if (size.X <= 0 || size.Y <= 0) return;

		float cut = 14f; // Size of the 45° chamfer cut at corners.
		float xOffset = _slideOffset;

		// 1. Polygon Points for Chamfered Cut Corners (Top-Right and Bottom-Left).
		Vector2[] points = new Vector2[]
		{
			new Vector2(xOffset, 0),
			new Vector2(xOffset + size.X - cut, 0),
			new Vector2(xOffset + size.X, cut),
			new Vector2(xOffset + size.X, size.Y),
			new Vector2(xOffset + cut, size.Y),
			new Vector2(xOffset, size.Y - cut)
		};

		// 2. Define the Color Palette based on state.
		Color normalBg = new Color(0.04f, 0.06f, 0.14f, 0.88f);
		Color hoverBg  = new Color(0.08f, 0.12f, 0.26f, 0.95f);
		Color disabledBg = new Color(0.02f, 0.03f, 0.07f, 0.5f);

		Color normalBorder = new Color(0f, 0.94f, 1f, 0.85f); // Cyber Cyan
		Color hoverBorder  = new Color(1f, 0f, 0.5f, 1f);     // Neon Pink
		Color disabledBorder = new Color(0.3f, 0.35f, 0.45f, 0.4f);

		Color activeBg = IsDisabled ? disabledBg : normalBg.Lerp(hoverBg, _hoverProgress);
		Color activeBorder = IsDisabled ? disabledBorder : normalBorder.Lerp(hoverBorder, _hoverProgress);

		// 3. Render the main body polygon.
		DrawPolygon(points, new Color[] { activeBg, activeBg, activeBg, activeBg, activeBg, activeBg });

		// 4. Render the border lines around the polygon.
		for (int i = 0; i < points.Length; i++)
		{
			Vector2 p1 = points[i];
			Vector2 p2 = points[(i + 1) % points.Length];
			DrawLine(p1, p2, activeBorder, IsDisabled ? 1.5f : 2.5f, true);
		}

		// 5. Render the Left Indicator Bar (appears on hover).
		if (_hoverProgress > 0.01f && !IsDisabled)
		{
			Color barColor = new Color(1f, 0f, 0.5f, _hoverProgress);
			DrawRect(new Rect2(xOffset + 2, 6, 6, size.Y - 12), barColor);
		}

		// 6. Render the Text Elements.
		float contentX = xOffset + 24f;

		// Index Badge [01]
		Color indexColor = IsDisabled ? new Color(0.4f, 0.45f, 0.55f, 0.6f) : (new Color(0f, 0.94f, 1f, 0.9f)).Lerp(new Color(1f, 0f, 0.5f, 1f), _hoverProgress);
		DrawString(OrbitronFont, new Vector2(contentX, size.Y / 2f + 6f), IndexText, HorizontalAlignment.Left, -1, 18, indexColor);

		// Main Label (Caps-lock styling).
		float mainX = contentX + 44f;
		Color mainColor = IsDisabled ? new Color(0.5f, 0.55f, 0.65f, 0.6f) : new Color(0.95f, 0.98f, 1f, 1f);
		string translatedMain = SafeToUpper(TranslateText(MainText));
		DrawString(RajdhaniFont, new Vector2(mainX, size.Y / 2f - 4f), translatedMain, HorizontalAlignment.Left, -1, 22, mainColor);

		// Sub Label.
		Color subColor = IsDisabled ? new Color(0.3f, 0.35f, 0.45f, 0.5f) : (new Color(0f, 0.94f, 1f, 0.7f)).Lerp(new Color(1f, 0.3f, 0.65f, 0.9f), _hoverProgress);
		string translatedSub = TranslateText(SubText);
		DrawString(RajdhaniFont, new Vector2(mainX, size.Y / 2f + 16f), translatedSub, HorizontalAlignment.Left, -1, 14, subColor);

		// 7. Corner Reticle Accent (+) at top-right for visual flair.
		if (!IsDisabled)
		{
			Vector2 tr = new Vector2(xOffset + size.X - 10, 10);
			DrawLine(tr + new Vector2(-4, 0), tr + new Vector2(4, 0), activeBorder * 0.7f, 1.5f);
			DrawLine(tr + new Vector2(0, -4), tr + new Vector2(0, 4), activeBorder * 0.7f, 1.5f);
		}
	}
}
