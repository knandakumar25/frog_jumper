using Godot;
using System;

/// <summary>
/// Custom 45° Chamfered Cut-Corner Cyberpunk HUD Button.
/// Features sci-fi indexing ("01 // PLAY"), dynamic 14px right-slide hover animation,
/// neon indicator bars, and custom vector drawing for an authentic AAA sci-fi UI look.
/// </summary>
[GlobalClass]
public partial class CyberButton : Control
{
	[Signal] public delegate void PressedEventHandler();

	[Export] public string IndexText { get; set; } = "01";
	[Export] public string MainText { get; set; } = "DEPLOY MISSION";
	[Export] public string SubText { get; set; } = "// SECTOR ACCESS";
	[Export] public bool IsDisabled { get; set; } = false;

	private bool _isHovered = false;
	private bool _isPressed = false;
	private float _slideOffset = 0f;
	private float _hoverProgress = 0f; // 0.0 to 1.0 smooth transition

	private static readonly FontFile OrbitronFont = GD.Load<FontFile>("res://assets/fonts/Orbitron-VariableFont.ttf");
	private static readonly FontFile RajdhaniFont = GD.Load<FontFile>("res://assets/fonts/Rajdhani-Bold.ttf");

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(0, 68);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		FocusMode = FocusModeEnum.All;
		MouseFilter = MouseFilterEnum.Stop;

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
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (IsDisabled) return;

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
				if (_isHovered)
				{
					UISoundManager.Instance?.PlayClickSound();
					EmitSignal(SignalName.Pressed);
				}
			}
		}
	}

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
		if (_hoverProgress > 0f && _hoverProgress < 1f)
		{
			QueueRedraw();
		}
	}

	private string TranslateText(string input)
	{
		if (string.IsNullOrEmpty(input)) return "";
		string clean = input.Trim();

		// Handle leading "// " for subtitle tags
		string prefix = "";
		if (clean.StartsWith("// "))
		{
			prefix = "// ";
			clean = clean.Substring(3).Trim();
		}

		// 1. Direct translation check
		string trans = Tr(clean);
		if (trans != clean) return prefix + trans;

		// 2. Explicit match for "HOW TO PLAY" -> "How to Play"
		if (clean.Equals("HOW TO PLAY", StringComparison.OrdinalIgnoreCase))
		{
			trans = Tr("How to Play");
			if (trans != "How to Play") return prefix + trans;
		}

		// 3. Try Title Case (e.g. "PLAY" -> "Play")
		string titleCase = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean.ToLower());
		trans = Tr(titleCase);
		if (trans != titleCase) return prefix + trans;

		// 4. Try Title Case with lowercase preposition ("To" -> "to")
		string titleCaseLowerTo = titleCase.Replace(" To ", " to ");
		trans = Tr(titleCaseLowerTo);
		if (trans != titleCaseLowerTo) return prefix + trans;

		// 5. Try Capitalized first letter (e.g. "SETTINGS" -> "Settings")
		string cap = clean.Length > 1 ? char.ToUpper(clean[0]) + clean.Substring(1).ToLower() : clean;
		trans = Tr(cap);
		if (trans != cap) return prefix + trans;

		return prefix + clean;
	}

	public override void _Draw()
	{
		Vector2 size = Size;
		if (size.X <= 0 || size.Y <= 0) return;

		float cut = 14f; // 45° chamfer cut size
		float xOffset = _slideOffset;

		// 1. Polygon Points for 45° Chamfered Cut Corners (Top-Right and Bottom-Left)
		Vector2[] points = new Vector2[]
		{
			new Vector2(xOffset, 0),
			new Vector2(xOffset + size.X - cut, 0),
			new Vector2(xOffset + size.X, cut),
			new Vector2(xOffset + size.X, size.Y),
			new Vector2(xOffset + cut, size.Y),
			new Vector2(xOffset, size.Y - cut)
		};

		// 2. Colors
		Color normalBg = new Color(0.04f, 0.06f, 0.14f, 0.88f);
		Color hoverBg  = new Color(0.08f, 0.12f, 0.26f, 0.95f);
		Color disabledBg = new Color(0.02f, 0.03f, 0.07f, 0.5f);

		Color normalBorder = new Color(0f, 0.94f, 1f, 0.85f); // Cyber Cyan
		Color hoverBorder  = new Color(1f, 0f, 0.5f, 1f);     // Neon Pink
		Color disabledBorder = new Color(0.3f, 0.35f, 0.45f, 0.4f);

		Color activeBg = IsDisabled ? disabledBg : normalBg.Lerp(hoverBg, _hoverProgress);
		Color activeBorder = IsDisabled ? disabledBorder : normalBorder.Lerp(hoverBorder, _hoverProgress);

		// 3. Fill Polygon
		DrawPolygon(points, new Color[] { activeBg, activeBg, activeBg, activeBg, activeBg, activeBg });

		// 4. Draw Border
		for (int i = 0; i < points.Length; i++)
		{
			Vector2 p1 = points[i];
			Vector2 p2 = points[(i + 1) % points.Length];
			DrawLine(p1, p2, activeBorder, IsDisabled ? 1.5f : 2.5f, true);
		}

		// 5. Left Indicator Bar
		if (_hoverProgress > 0.01f && !IsDisabled)
		{
			Color barColor = new Color(1f, 0f, 0.5f, _hoverProgress);
			DrawRect(new Rect2(xOffset + 2, 6, 6, size.Y - 12), barColor);
		}

		// 6. Draw Text Elements
		float contentX = xOffset + 24f;

		// Index Badge [01]
		Color indexColor = IsDisabled ? new Color(0.4f, 0.45f, 0.55f, 0.6f) : (new Color(0f, 0.94f, 1f, 0.9f)).Lerp(new Color(1f, 0f, 0.5f, 1f), _hoverProgress);
		DrawString(OrbitronFont, new Vector2(contentX, size.Y / 2f + 6f), IndexText, HorizontalAlignment.Left, -1, 18, indexColor);

		// Main Label
		float mainX = contentX + 44f;
		Color mainColor = IsDisabled ? new Color(0.5f, 0.55f, 0.65f, 0.6f) : new Color(0.95f, 0.98f, 1f, 1f);
		string translatedMain = TranslateText(MainText).ToUpper();
		DrawString(RajdhaniFont, new Vector2(mainX, size.Y / 2f - 4f), translatedMain, HorizontalAlignment.Left, -1, 22, mainColor);

		// Sub Label
		Color subColor = IsDisabled ? new Color(0.3f, 0.35f, 0.45f, 0.5f) : (new Color(0f, 0.94f, 1f, 0.7f)).Lerp(new Color(1f, 0.3f, 0.65f, 0.9f), _hoverProgress);
		string translatedSub = TranslateText(SubText);
		DrawString(RajdhaniFont, new Vector2(mainX, size.Y / 2f + 16f), translatedSub, HorizontalAlignment.Left, -1, 14, subColor);

		// 7. Corner Reticle Accent (+) at top-right
		if (!IsDisabled)
		{
			Vector2 tr = new Vector2(xOffset + size.X - 10, 10);
			DrawLine(tr + new Vector2(-4, 0), tr + new Vector2(4, 0), activeBorder * 0.7f, 1.5f);
			DrawLine(tr + new Vector2(0, -4), tr + new Vector2(0, 4), activeBorder * 0.7f, 1.5f);
		}
	}
}
