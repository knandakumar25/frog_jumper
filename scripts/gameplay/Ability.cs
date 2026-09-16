using Godot;
using System;

/// <summary>
/// Base class for all operative abilities.
/// Handles cooldowns, animations, and damage parameters.
/// </summary>
public abstract partial class Ability : Node
{
	[Export] public string Name { get; set; }
	[Export] public float Cooldown { get; set; } = 1.0f;
	[Export] public float Damage { get; set; } = 20.0f;
	[Export] public float Range { get; set; } = 2.0f;
	[Export] public string AnimationName { get; set; }

	protected float _timer = 0f;
	public bool IsReady => _timer <= 0;
	public float CooldownProgress => _timer <= 0 ? 1.0f : 1.0f - (_timer / Cooldown);

	public override void _Process(double delta)
	{
		if (_timer > 0)
		{
			_timer -= (float)delta;
		}
	}

	/// <summary>
	/// Triggers the ability. Returns true if successfully executed.
	/// </summary>
	public virtual bool Execute(Player3D player)
	{
		if (!IsReady) return false;

		_timer = Cooldown;

		// Trigger animation.
		player.PlayAbilityAnimation(AnimationName);

		// Execute the specific effect.
		PerformEffect(player);

		return true;
	}

	protected abstract void PerformEffect(Player3D player);
}
