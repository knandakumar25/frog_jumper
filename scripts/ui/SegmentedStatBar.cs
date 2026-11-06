using Godot;
using System;

/// <summary>
/// SegmentedStatBar is a custom UI component that visualizes a numeric value (0-100)
/// as a series of glowing LED segments. This provides a high-tech, "inspection panel"
/// feel common in cyberpunk HUDs, replacing standard continuous progress bars.
///
/// Unity Migration Note:
/// 1. Visuals: Implement this as a custom UI component. Instead of _Draw(), use a
///    Horizontal Layout Group containing a pool of 'Segment' GameObjects (UI Images).
/// 2. Logic: Create a script that updates the 'Color' or 'Sprite' of each segment
///    based on the current value.
/// 3. Text: Use TextMeshPro (TMP) for the stat label and the numeric value display.
/// </summary>
[GlobalClass]
public partial class SegmentedStatBar : Control
{
	// Configurable properties for the stat meter.
	[Export] public string StatName { get; set; } = "SPEED";
	[Export] public float Value { get; set; } = 85.0f; // Value range: 0 to 100.
	[Export] public int SegmentCount { get; set; } = 10;

	private static readonly FontFile RajdhaniFont = GD.Load<FontFile>("res://assets/fonts/Rajdhani-Bold.ttf");

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(0, 42);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
	}

	/// <summary>
	/// Updates the stat label and value, then triggers a redraw of the LED segments.
	/// </summary>
	public void SetStat(string name, float val)
	{
		StatName = name;
		Value = val;
		QueueRedraw();
	}

	/// <summary>
	/// Selects the appropriate font based on the current locale to ensure glyph support.
	/// </summary>
	private static Font GetActiveFont(Font defaultFont)
	{
        string path = "res://assets/fonts/" + LocaleCatalog.Resolve(TranslationServer.GetLocale()).Font;
        return GD.Load<Font>(path) ?? defaultFont;
	}

	/// <summary>
	/// Preserves authored casing, including Turkish dotted I and scripts without letter case.
	/// </summary>
	private static string SafeToUpper(string text) => text ?? "";
	/// <summary>
	/// Performs the low-level vector drawing of the stat label and the LED segment bar.
	/// </summary>
	public override void _Draw()
	{
		Vector2 size = Size;
		if (size.X <= 0 || size.Y <= 0) return;

		// 1. Render Label Header & Numeric Value.
		Color textColor = new Color(0.85f, 0.92f, 1f, 0.95f);
		Color cyanColor = new Color(0f, 0.94f, 1f, 1f);
		DrawString(GetActiveFont(RajdhaniFont), new Vector2(0, 16), SafeToUpper(TranslationServer.Translate(StatName)), HorizontalAlignment.Left, size.X, 16, textColor);
		// A fraction remains value/maximum in RTL menus; automatic bidi otherwise reverses its operands.
		DrawString(RajdhaniFont, new Vector2(0, 16), $"{(int)Value} / 100", HorizontalAlignment.Right, size.X, 16, cyanColor,
			direction: TextServer.Direction.Ltr);

		// 2. LED Segment Bar Calculations.
		float startY = 22f;
		float barHeight = 14f;
		float totalWidth = size.X;

		float segmentSpacing = 4f;
		float segmentWidth = (totalWidth - (segmentSpacing * (SegmentCount - 1))) / SegmentCount;

		// Determine how many segments should be "filled" based on the 0-100 value.
		int filledSegments = Mathf.Clamp(Mathf.RoundToInt((Value / 100f) * SegmentCount), 0, SegmentCount);

		Color filledColor = new Color(0f, 0.94f, 1f, 0.9f); // Neon Cyan for filled.
		Color unfilledColor = new Color(0.06f, 0.09f, 0.2f, 0.8f); // Dark Navy for empty.
		Color borderColor = new Color(0.12f, 0.2f, 0.4f, 0.6f);

		for (int i = 0; i < SegmentCount; i++)
		{
			float segX = i * (segmentWidth + segmentSpacing);
			Rect2 segRect = new Rect2(segX, startY, segmentWidth, barHeight);

			// Set segment color based on fill status.
			Color segColor = (i < filledSegments) ? filledColor : unfilledColor;
			DrawRect(segRect, segColor);
			DrawRect(segRect, borderColor, false, 1.0f);

			// Add a white "glow line" at the top of filled segments for a high-tech LED effect.
			if (i < filledSegments)
			{
				DrawLine(new Vector2(segX, startY), new Vector2(segX + segmentWidth, startY), new Color(1f, 1f, 1f, 0.8f), 1.5f);
			}
		}
	}
}
