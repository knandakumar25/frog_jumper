using Godot;
using System;

/// <summary>
/// A tactical cover object that can be destroyed by high-damage attacks.
/// When destroyed, it spawns a "neon debris" effect.
/// </summary>
public partial class DestructibleCover : TacticalObject
{
	[Export] public float Health = 50.0f;
	private float _currentHealth;

	public override void _Ready()
	{
		base._Ready();
		_currentHealth = Health;

		// Add a Health component if not already present in the scene.
		var health = GetNodeOrNull<Health>("Health");
		if (health == null)
		{
			health = new Health();
			health.Name = "Health";
			health.MaxHealth = Health;
			AddChild(health);
			health.Died += OnCoverDestroyed;
		}
		else
		{
			health.Died += OnCoverDestroyed;
		}
	}

	public void TakeDamage(float amount)
	{
		var health = GetNode<Health>("Health");
		health.TakeDamage(amount);
	}

	private void OnCoverDestroyed()
	{
		GD.Print($"Cover destroyed at {GlobalPosition}");

		// Create a "Neon Debris" effect.
		SpawnDebris();

		// Remove the object from the game.
		QueueFree();
	}

	private void SpawnDebris()
	{
		// In a full version, this would instantiate a GPU particle system.
		// For the prototype, we'll simulate it with a few small neon cubes.
		for (int i = 0; i < 5; i++)
		{
			var debris = new MeshInstance3D();
			debris.Mesh = new BoxMesh { Size = new Vector3(0.2f, 0.2f, 0.2f) };

			var mat = new StandardMaterial3D();
			mat.AlbedoColor = new Color(0, 1, 1);
			mat.EmissionEnabled = true;
			mat.Emission = new Color(0, 1, 1);
			mat.EmissionEnergyMultiplier = 2.0f;
			debris.MaterialOverride = mat;

			GetTree().Root.AddChild(debris);
			debris.GlobalPosition = GlobalPosition;

			// Launch debris in random directions.
			var velocity = new Vector3(
				(float)GD.RandRange(-2, 2),
				(float)GD.RandRange(2, 5),
				(float)GD.RandRange(-2, 2)
			);

			var tween = debris.CreateTween();
			tween.TweenProperty(debris, "global_position", debris.GlobalPosition + velocity, 0.5f);
			tween.Parallel().TweenProperty(debris, "scale", Vector3.Zero, 0.5f);
			tween.Finished += () => debris.QueueFree();
		}
	}
}
