using Godot;

/// <summary>
/// Procedural Cyberpunk Circular Loading Spinner.
/// Renders dual counter-rotating glowing rings (cyan outer, magenta inner) with a center core.
/// </summary>
public partial class CyberCircularSpinner : Control
{
	[Export] public float Radius { get; set; } = 36f;
	[Export] public float Thickness { get; set; } = 5f;
	[Export] public Color OuterColor { get; set; } = new Color(0f, 0.94f, 1f, 1f);
	[Export] public Color InnerColor { get; set; } = new Color(1f, 0f, 0.5f, 1f);
	[Export] public float Speed { get; set; } = 4.5f;

	private float _angleOuter = 0f;
	private float _angleInner = 0f;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(Radius * 2 + 20, Radius * 2 + 20);
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_angleOuter += dt * Speed;
		_angleInner -= dt * (Speed * 1.35f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = Size / 2;

		// 1. Outer track background
		DrawArc(center, Radius, 0, Mathf.Pi * 2, 48, new Color(0.04f, 0.08f, 0.22f, 0.85f), Thickness);

		// 2. Outer glowing cyan arc (clockwise rotation)
		float startOuter = _angleOuter;
		float endOuter   = _angleOuter + (Mathf.Pi * 0.75f);
		DrawArc(center, Radius, startOuter, endOuter, 32, OuterColor, Thickness);
		DrawArc(center, Radius, startOuter, endOuter, 32, new Color(OuterColor.R, OuterColor.G, OuterColor.B, 0.35f), Thickness + 4f);

		// 3. Inner track background
		float innerRadius = Radius - 10f;
		DrawArc(center, innerRadius, 0, Mathf.Pi * 2, 36, new Color(0.04f, 0.08f, 0.22f, 0.65f), Thickness - 1.5f);

		// 4. Inner glowing magenta arc (counter-clockwise rotation)
		float startInner = _angleInner;
		float endInner   = _angleInner + (Mathf.Pi * 0.55f);
		DrawArc(center, innerRadius, startInner, endInner, 24, InnerColor, Thickness - 1.5f);
		DrawArc(center, innerRadius, startInner, endInner, 24, new Color(InnerColor.R, InnerColor.G, InnerColor.B, 0.35f), Thickness + 2.5f);

		// 5. Center cyber core dot with aura
		DrawCircle(center, 5f, new Color(OuterColor.R, OuterColor.G, OuterColor.B, 0.3f));
		DrawCircle(center, 3.5f, OuterColor);
	}
}
