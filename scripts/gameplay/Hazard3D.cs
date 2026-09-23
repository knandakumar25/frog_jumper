using Godot;
using System;

/// <summary>
/// Hazard3D is an Area3D that damages any entity (usually the Player) that enters its zone.
/// Examples: Laser grids, electrified floors, toxic pools.
/// </summary>
public partial class Hazard3D : Area3D
{
	[Export] public float DamagePerSecond = 20.0f;
	[Export] public string HazardName = "Laser Grid";

	private double _timer = 0;
	private const double DamageInterval = 0.5; // Damage every 0.5 seconds

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		_timer += delta;
		if (_timer >= DamageInterval)
		{
			ApplyDamageToBodies();
			_timer = 0;
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		GD.Print($"{HazardName} activated for {body.Name}!");
	}

	private void OnBodyExited(Node3D body)
	{
		GD.Print($"{body.Name} exited {HazardName}.");
	}

	private void ApplyDamageToBodies()
	{
		var bodies = GetOverlappingBodies();
		foreach (var body in bodies)
		{
			var health = body.GetNodeOrNull<Health>("Health");
			if (health == null)
			{
				health = body.FindChild("Health", true, false) as Health;
			}

			if (health != null)
			{
				health.TakeDamage(DamagePerSecond * (float)DamageInterval);
				GD.Print($"{body.Name} is taking damage from {HazardName}!");
			}
		}
	}
}
