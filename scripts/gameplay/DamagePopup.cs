using Godot;
using System;

/// <summary>
/// DamagePopup is a holographic 3D label that floats upwards and glitches
/// before fading away.
/// </summary>
public partial class DamagePopup : Label3D
{
	private float _lifeTime = 1.0f;
	private float _elapsed = 0f;
	private Color _originalColor;

	public DamagePopup(float damage, Color color)
	{
		Text = $" {damage:F0} ";
		_originalColor = color;
		Modulate = color;

		HorizontalAlignment = HorizontalAlignment.Center;
		VerticalAlignment = VerticalAlignment.Center;
		Billboard = BillboardMode.Enabled;
		FontSize = 32;
		OutlineMode = Text3D.OutlineModeEnum.Enabled;
		OutlineColor = new Color(0, 0, 0, 0.5f);
	}

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;

		// 1. Float upwards.
		GlobalPosition += Vector3.Up * 1.0f * (float)delta;

		// 2. Glitch effect (randomly shift position and color).
		if (GD.Randf() < 0.1f)
		{
			GlobalPosition += new Vector3((float)GD.RandRange(-0.1f, 0.1f), 0, (float)GD.RandRange(-0.1f, 0.1f));
			Modulate = new Color(1, 1, 1); // Flash white
		}
		else
		{
			Modulate = _originalColor;
		}

		// 3. Fade out.
		float alpha = 1.0f - (_elapsed / _lifeTime);
		Modulate = Modulate with { A = alpha };

		if (_elapsed >= _lifeTime)
		{
			QueueFree();
		}
	}
}
