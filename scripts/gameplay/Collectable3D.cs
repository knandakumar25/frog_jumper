using Godot;
using System;

/// <summary>
/// Collectable3D is an Area3D that grants points to the player upon contact.
/// </summary>
public partial class Collectable3D : Area3D
{
	[Export] public int PointValue = 50;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		// Simple rotation effect for the collectable.
		// Since it's a simple script, we can use a tween or just process.
	}

	public override void _Process(double delta)
	{
		RotateY((float)(Mathf.Pi * delta));
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player3D)
		{
			GD.Print($"Collected item! +{PointValue} points.");

			// Notify the Level Manager.
			var levelManager = GetTree().Root.FindChild("Level3DManager", true, false) as Level3DManager;
			if (levelManager != null)
			{
				// Increment collectable count for objective.
				levelManager.IncrementCollectable();

				// Add points.
				int calls = PointValue / 10;
				for (int i = 0; i < calls; i++)
				{
					levelManager.AddPoint();
				}
			}

			// Simple despawn effect.
			QueueFree();
		}
	}
}
