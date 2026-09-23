using Godot;
using System;

/// <summary>
/// SwitchObject is an interactive element that can be triggered by player entry
/// or by high-impact abilities, used to modify the level state (e.g., opening doors).
/// </summary>
public partial class SwitchObject : Area3D
{
	[Signal] public delegate void SwitchedEventHandler(bool state);

	[Export] public bool IsToggle = true;
	[Export] public float ActivationForceThreshold = 5.0f;

	private bool _isSwitched = false;
	private bool _isActive = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player3D)
		{
			TriggerSwitch();
		}
	}

	/// <summary>
	/// Allows tactical objects or abilities to trigger the switch via impact.
	/// </summary>
	public void OnImpact(Vector3 force, Node3D source)
	{
		if (force.Length() >= ActivationForceThreshold)
		{
			GD.Print($"SwitchObject: Triggered by impact from {source.Name}!");
			TriggerSwitch();
		}
	}

	private void TriggerSwitch()
	{
		if (IsToggle)
		{
			_isSwitched = !_isSwitched;
		}
		else
		{
			_isSwitched = true;
		}

		_isActive = _isSwitched;
		EmitSignal(SignalName.Switched, _isSwitched);
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		// In a real scene, this would trigger an animation.
		// For the prototype, we'll change the modulate of the node.
		Modulate = _isSwitched ? new Color(0, 1, 0) : new Color(1, 0, 0);
		GD.Print($"SwitchObject: State changed to {_isSwitched}");
	}
}
