using Godot;
using System;

public enum AbilityType { Melee, AoE, Push, Dash }

/// <summary>
/// Concrete implementation of abilities for Axiom Force operatives.
/// Handles different effects based on the AbilityType.
/// </summary>
public partial class OperativeAbility : Ability
{
	[Export] public AbilityType Type { get; set; }
	[Export] public float EffectRadius { get; set; } = 3.0f;
	[Export] public float KnockbackForce { get; set; } = 5.0f;

	private float _currentCooldown = 0f;
	public float CooldownProgress => _currentCooldown <= 0 ? 1.0f : 1.0f - (_currentCooldown / Cooldown);
	public bool IsReady => _currentCooldown <= 0;

	public override void Execute(Player3D player)
	{
		if (_currentCooldown > 0) return;

		PerformEffect(player);
		_currentCooldown = Cooldown;
	}

	public override void _Process(double delta)
	{
		if (_currentCooldown > 0)
		{
			_currentCooldown -= (float)delta;
		}
	}
		// Trigger Neon Pulse VFX.
		Color vfxColor = Type switch
		{
			AbilityType.Melee => new Color(1, 1, 1), // White flash
			AbilityType.AoE => new Color(1, 0, 1),   // Magenta pulse
			AbilityType.Push => new Color(0, 1, 1),   // Cyan wave
			AbilityType.Dash => new Color(1, 1, 0),   // Yellow streak
			_ => new Color(1, 1, 1)
		};

		CombatVFXManager.Instance?.PlayAbilityPulse(player, vfxColor);

		// Trigger screen shake for high-impact abilities.
		if (Type == AbilityType.AoE || Type == AbilityType.Push)
		{
			CombatVFXManager.Instance?.ScreenShake(0.3f, 0.2f);
		}

		switch (Type)
		{
			case AbilityType.Melee:
				ExecuteMelee(player);
				break;
			case AbilityType.AoE:
				ExecuteAoE(player);
				break;
			case AbilityType.Push:
				ExecutePush(player);
				break;
			case AbilityType.Dash:
				ExecuteDash(player);
				break;
		}
	}

	private void ExecuteMelee(Player3D player)
	{
		// Use the standard WeaponArea3D hitbox.
		var weapon = player.GetNodeOrNull<WeaponArea3D>("WeaponArea3D");
		if (weapon != null)
		{
			weapon.Damage = this.Damage;
			weapon.ActivateAttack();
		}
	}

	private void ExecuteAoE(Player3D player)
	{
		// Find all entities in a radius.
		var spaceState = player.GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		var sphere = new SphereShape3D { Radius = EffectRadius };
		query.ShapeRid = sphere.GetRid();
		query.Transform = player.GlobalTransform;
		query.CollisionMask = 1 | 2; // Enemy layer AND Tactical layer

		var results = spaceState.IntersectShape(query);
		foreach (var result in results)
		{
			var collider = (Node3D)result["collider"];

			// 1. Damage living entities.
			var health = collider.GetNodeOrNull<Health>("Health") ?? collider.FindChild("Health", true, false) as Health;
			if (health != null)
			{
				health.TakeDamage(this.Damage);

				var enemy = collider as Enemy3D;
				if (enemy != null)
				{
					enemy.TakeHit();
					ApplyUniqueEffect(enemy, player);
				}
			}

			// 2. Destroy destructible cover.
			if (collider is DestructibleCover cover)
			{
				cover.TakeDamage(this.Damage);
			}
		}
		GD.Print($"Executed AoE Ability: {Name}!");
	}

	private void ExecutePush(Player3D player)
	{
		// Similar to AoE but adds knockback.
		var spaceState = player.GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		var sphere = new SphereShape3D { Radius = EffectRadius };
		query.ShapeRid = sphere.GetRid();
		query.Transform = player.GlobalTransform;
		query.CollisionMask = 1 | 2; // Enemy layer AND Tactical layer

		var results = spaceState.IntersectShape(query);
		foreach (var result in results)
		{
			var collider = (Node3D)result["collider"];

			// 1. Damage living entities.
			var health = collider.GetNodeOrNull<Health>("Health") ?? collider.FindChild("Health", true, false) as Health;
			if (health != null)
			{
				health.TakeDamage(this.Damage);

				var enemy = collider as Enemy3D;
				if (enemy != null)
				{
					enemy.TakeHit();
					ApplyUniqueEffect(enemy, player);
				}
			}

			// 2. Push tactical objects.
			if (collider is TacticalObject tactical)
			{
				Vector3 pushDir = (collider.GlobalPosition - player.GlobalPosition).Normalized();
				pushDir.Y = 0;
				tactical.OnImpact(pushDir * KnockbackForce * 5.0f, player);
			}
		}
		GD.Print($"Executed Push Ability: {Name}!");
	}

	private void ApplyUniqueEffect(Enemy3D enemy, Player3D player)
	{
		// Determine effect based on the player's current character.
		var global = player.GetNode<Node>("/root/Global");
		string charName = global.Get("selected_character")?.ToString() ?? "Cyber Shogun";

		StatusEffect effect = null;
		switch (charName)
		{
			case "Solaria":
				effect = new BurnEffect { Duration = 4.0f, DamagePerSecond = 8.0f };
				break;
			case "Oceanus":
				effect = new SlowEffect { Duration = 3.0f, SpeedMultiplier = 0.4f };
				break;
			case "Surya Prime":
			case "Apex Titan":
				effect = new StunEffect { Duration = 2.0f };
				break;
		}

		if (effect != null)
		{
			enemy.ApplyStatusEffect(effect);
		}
	}

	private void ExecuteDash(Player3D player)
	{
		// Quick burst of speed in current movement direction.
		Vector3 dashDir = player.Velocity.Normalized();
		if (dashDir == Vector3.Zero)
		{
			// Dash forward relative to camera if stationary.
			var pivot = player.GetNodeOrNull<Node3D>("CameraPivot");
			dashDir = pivot != null ? -pivot.GlobalTransform.Basis.Z : Vector3.Forward;
		}
		dashDir.Y = 0;

		player.Velocity += dashDir * 15.0f;

		// Also trigger a melee strike at the end of the dash.
		ExecuteMelee(player);
		GD.Print($"Executed Dash Ability: {Name}!");
	}
}
