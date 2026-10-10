using Godot;
using System;

/// <summary>
/// TrainingDrone is the passive ability target for Protocol Zero (step 3).
/// It never attacks and never moves on its own: the hover/death behaviour lives in
/// the GLB's bone clips, and this script only reacts to its Health component --
/// when the drone is destroyed by the player's ability it plays the Death clip,
/// drops out of weapon detection, and clears itself.
/// </summary>
public partial class TrainingDrone : Node3D
{
	private Health _health;
	private AnimationPlayer _animPlayer;
	private bool _destroyed;

	public override void _Ready()
	{
		_health = GetNodeOrNull<Health>("Hitbox/Health");
		if (_health != null)
		{
			_health.Died += OnDied;
		}
		else
		{
			GD.PrintErr("TrainingDrone: Health not found under Hitbox! Drone will be unusable as a target.");
		}
	}

	private void OnDied()
	{
		if (_destroyed) return;
		_destroyed = true;

		// Stop registering further weapon hits.
		var hitbox = GetNodeOrNull<PhysicsBody3D>("Hitbox");
		if (hitbox != null)
		{
			hitbox.CollisionLayer = 0;
		}

		// Play the authored crash clip from the GLB, then clear the wreck.
		_animPlayer = FindChild("AnimationPlayer", true, false) as AnimationPlayer;
		if (_animPlayer != null && _animPlayer.HasAnimation("Death"))
		{
			_animPlayer.Play("Death");
		}

		GetTree().CreateTimer(3.0).Timeout += QueueFree;
	}
}