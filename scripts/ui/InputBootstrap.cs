using Godot;
using System;

/// <summary>
/// InputBootstrap ensures that the necessary input actions are mapped
/// at runtime, avoiding the need to manually edit project.godot.
/// </summary>
public partial class InputBootstrap : Node
{
	public override void _Ready()
	{
		MapAction("left", Key.A, JoyAxis.LeftX, -1);
		MapAction("right", Key.D, JoyAxis.LeftX, 1);
		MapAction("forward", Key.W, JoyAxis.LeftY, -1);
		MapAction("backward", Key.S, JoyAxis.LeftY, 1);
		MapAction("jump", Key.Space, JoyButton.A);
		MapAction("attack", Key.F, JoyButton.X);
		MapAction("karate", Key.Q, JoyButton.Y);
	}

	private void MapAction(string action, Key key, JoyAxis axis = 0, float axisValue = 0)
	{
		if (!InputMap.HasAction(action))
		{
			InputMap.AddAction(action);
		}

		// Map Keyboard
		var eventKey = new InputEventKey { Keycode = key };
		if (!InputMap.ActionHasEvent(action, eventKey))
		{
			InputMap.ActionAddEvent(action, eventKey);
		}

		// Map Joystick Axis (if provided)
		if (axis != 0 || axisValue != 0)
		{
			var eventAxis = new InputEventJoypadMotion { Axis = axis, AxisValue = axisValue };
			// Note: Joypad motion is slightly different; usually handled by Input.GetVector.
		}
	}

	private void MapAction(string action, Key key, JoyButton button)
	{
		if (!InputMap.HasAction(action))
		{
			InputMap.AddAction(action);
		}

		var eventBtn = new InputEventJoypadButton { ButtonIndex = button };
		if (!InputMap.ActionHasEvent(action, eventBtn))
		{
			InputMap.ActionAddEvent(action, eventBtn);
		}
	}
}
