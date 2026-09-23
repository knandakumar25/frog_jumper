using Godot;
using System;

/// <summary>
/// HealthBar3D provides a simple visual representation of an entity's health in 3D space.
/// It listens to the Health component's signals and updates a Label3D.
/// </summary>
public partial class HealthBar3D : Label3D
{
	private Health _healthComponent;

	public override void _Ready()
	{
		// Find the Health component on the parent entity.
		var parent = GetParent();
		_healthComponent = parent.GetNodeOrNull<Health>("Health");
		if (_healthComponent == null)
		{
			_healthComponent = parent.FindChild("Health", true, false) as Health;
		}

		if (_healthComponent != null)
		{
			// Connect to the health changed signal.
			_healthComponent.HealthChanged += OnHealthChanged;
			UpdateText(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
		}
		else
		{
			GD.PrintErr($"HealthBar3D: No Health component found on {parent?.Name}");
			QueueFree();
		}

		// Ensure the label always faces the camera.
		Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
	}

	private void OnHealthChanged(float current, float max)
	{
		UpdateText(current, max);
	}

	private void UpdateText(float current, float max)
	{
		Text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

		// Change color based on health percentage.
		float pct = current / max;
		if (pct < 0.3f) Modulate = Colors.Red;
		else if (pct < 0.6f) Modulate = Colors.Yellow;
		else Modulate = Colors.Green;
	}
}
