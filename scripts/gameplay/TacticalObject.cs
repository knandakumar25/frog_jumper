using Godot;
using System;

/// <summary>
/// Base class for interactive tactical objects in the environment.
/// These can be pushed, destroyed, or trigger events.
/// </summary>
public partial class TacticalObject : RigidBody3D
{
	[Export] public float Mass = 1.0f;
	[Export] public float Friction = 0.5f;

	public override void _Ready()
	{
		// Configure physics for a "heavy but movable" feel.
		this.Mass = Mass;
		PhysicsMaterial = new PhysicsMaterial
		{
			Friction = Friction,
			Bounce = 0.2f
		};

		// Enable contact monitoring to detect hits on enemies.
		ContactMonitor = true;
		MaxContactsReported = 5;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Enemy3D enemy)
		{
			Vector3 impactForce = LinearVelocity * 0.5f;
			enemy.OnTacticalImpact(impactForce);
		}
	}

	public virtual void OnImpact(Vector3 force, Node3D source)
	{
		ApplyCentralImpulse(force);
	}
}
