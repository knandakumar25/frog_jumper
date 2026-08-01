using Godot;
using System;

/// <summary>
/// Custom Segmented LED Stat Meter for Operative inspection panels.
/// Replaces generic default progress bars with 10 glowing LED segments.
/// </summary>
[GlobalClass]
public partial class SegmentedStatBar : Control
{
	[Export] public string StatName { get; set; } = "SPEED";
	[Export] public float Value { get; set; } = 85.0f; // 0 to 100
	[Export] public int SegmentCount { get; set; } = 10;

	private static readonly FontFile RajdhaniFont = GD.Load<FontFile>("res://assets/fonts/Rajdhani-Bold.ttf");

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(0, 42);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
	}

	public void SetStat(string name, float val)
	{
		StatName = name;
		Value = val;
		QueueRedraw();
	}

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

	public override void _Draw()
	{
		Vector2 size = Size;
		if (size.X <= 0 || size.Y <= 0) return;

		// 1. Label Header & Value Text
		Color textColor = new Color(0.85f, 0.92f, 1f, 0.95f);
		Color cyanColor = new Color(0f, 0.94f, 1f, 1f);
		DrawString(RajdhaniFont, new Vector2(0, 16), SafeToUpper(StatName), HorizontalAlignment.Left, -1, 16, textColor);
		DrawString(RajdhaniFont, new Vector2(size.X, 16), $"{(int)Value} / 100", HorizontalAlignment.Right, -1, 16, cyanColor);

		// 2. LED Segment Bar Parameters
		float startY = 22f;
		float barHeight = 14f;
		float totalWidth = size.X;

		float segmentSpacing = 4f;
		float segmentWidth = (totalWidth - (segmentSpacing * (SegmentCount - 1))) / SegmentCount;

		int filledSegments = Mathf.Clamp(Mathf.RoundToInt((Value / 100f) * SegmentCount), 0, SegmentCount);

		Color filledColor = new Color(0f, 0.94f, 1f, 0.9f); // Neon Cyan fill
		Color unfilledColor = new Color(0.06f, 0.09f, 0.2f, 0.8f); // Dark Navy empty
		Color borderColor = new Color(0.12f, 0.2f, 0.4f, 0.6f);

		for (int i = 0; i < SegmentCount; i++)
		{
			float segX = i * (segmentWidth + segmentSpacing);
			Rect2 segRect = new Rect2(segX, startY, segmentWidth, barHeight);

			Color segColor = (i < filledSegments) ? filledColor : unfilledColor;
			DrawRect(segRect, segColor);
			DrawRect(segRect, borderColor, false, 1.0f);

			// Top glow line for filled segments
			if (i < filledSegments)
			{
				DrawLine(new Vector2(segX, startY), new Vector2(segX + segmentWidth, startY), new Color(1f, 1f, 1f, 0.8f), 1.5f);
			}
		}
	}
}
