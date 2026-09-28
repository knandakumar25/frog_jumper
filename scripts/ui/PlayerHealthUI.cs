using Godot;
using System;

/// <summary>
/// PlayerHealthUI connects the Player's Health component to a HUD ProgressBar.
/// </summary>
public partial class PlayerHealthUI : ProgressBar
{
	private Health _playerHealth;

	public override void _Ready()
	{
		// Find the player in the scene.
		var player = GetTree().Root.FindChild("Player3D", true, false) 
			?? GetTree().Root.FindChild("Player", true, false);
		if (player != null)
		{
			_playerHealth = player.GetNodeOrNull<Health>("Health");
			if (_playerHealth == null)
			{
				_playerHealth = player.FindChild("Health", true, false) as Health;
			}

			if (_playerHealth != null)
			{
				_playerHealth.HealthChanged += OnHealthChanged;
				UpdateBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
			}
		}
	}

	private void OnHealthChanged(float current, float max)
	{
		UpdateBar(current, max);
	}

	private void UpdateBar(float current, float max)
	{
		MaxValue = max;
		Value = current;
	}
}
