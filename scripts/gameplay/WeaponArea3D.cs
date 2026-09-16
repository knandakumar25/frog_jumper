using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// WeaponArea3D is an Area3D used for combat hit-detection.
/// When enabled during an attack animation, it applies damage to any entity with a Health component.
/// </summary>
public partial class WeaponArea3D : Area3D
{
	[Export] public float Damage = 20.0f;
	private List<Node> _hitTargets = new List<Node>();

	public override void _Ready()
	{
		// Start disabled.
		Monitoring = false;
		BodyEntered += OnBodyEntered;
	}

	public void ActivateAttack()
	{
		_hitTargets.Clear();
		Monitoring = true;

		// Play attack whoosh sound.
		var soundManager = GetTree().Root.FindChild("CombatSoundManager", true, false) as CombatSoundManager;
		if (soundManager != null)
		{
			soundManager.PlayAttackSound();
		}

		GetTree().CreateTimer(0.2f).Timeout += () => DeactivateAttack();
	}

	public void DeactivateAttack()
	{
		Monitoring = false;
	}

	private void OnBodyEntered(Node3D body)
	{
		// Avoid hitting the player themselves.
		if (body is Player3D && GetParent() is Player3D) return;

		// Check if the body has a Health component.
		var health = body.GetNodeOrNull<Health>("Health");
		if (health == null)
		{
			// Try to find it as a child of the body.
			health = body.FindChild("Health", true, false) as Health;
		}

		if (health != null && !_hitTargets.Contains(body))
		{
			_hitTargets.Add(body);
			health.TakeDamage(Damage);
			GD.Print($"Hit {body.Name} for {Damage} damage!");

			// Trigger a hit reaction if the entity implements it.
			if (body is Enemy3D enemy)
			{
				enemy.TakeHit();
			}
			else if (body is Player3D player)
			{
				// Player hit reaction (can be implemented in Player3D).
				GD.Print("Player hit!");
			}
		}
	}
}
