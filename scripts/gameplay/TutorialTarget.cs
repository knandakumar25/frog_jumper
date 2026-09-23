using Godot;
using System;

/// <summary>
/// TutorialTarget is a simple area that triggers a signal when the player enters.
/// Used by TrainingManager to track tutorial progress.
/// </summary>
public partial class TutorialTarget : Area3D
{
	[Signal] public delegate void TargetReachedEventHandler();

	public override void _Ready()
	{
		BodyEntered += (body) =>
		{
			if (body is Player3D)
			{
				EmitSignal(SignalName.TargetReached);
			}
		};
	}
}
