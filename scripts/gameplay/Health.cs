using Godot;
using System;

/// <summary>
/// Health component that can be attached to any entity (Player, Enemy, Destructible).
/// Handles current health, damage application, and death events.
/// </summary>
public partial class Health : Node
{
	[Export] public float MaxHealth = 100.0f;
	public float CurrentHealth { get; private set; }

	[Signal] public delegate void HealthChangedEventHandler(float newHealth, float maxHealth);
	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
	}

	public void TakeDamage(float amount)
	{
		if (amount <= 0) return;

		CurrentHealth -= amount;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
		GD.Print($"{GetParent().Name} took {amount} damage. Health: {CurrentHealth}/{MaxHealth}");

		// --- Neon Pulse Visuals: Damage Pop-up ---
		if (CombatVFXManager.Instance != null)
		{
			Color damageColor = amount > 20 ? new Color(1, 0, 0) : new Color(1, 1, 1);
			CombatVFXManager.Instance.SpawnDamagePopup(GetParent<Node3D>(), amount, damageColor);
		}

		// Trigger combat hit sound.
		var soundManager = GetTree().Root.FindChild("CombatSoundManager", true, false) as CombatSoundManager;
		if (soundManager != null)
		{
			soundManager.PlayHitSound();
		}

		if (CurrentHealth <= 0)
		{
			CurrentHealth = 0;
			Die();
		}
	}

	private void SpawnDamagePopUp(float amount)
	{
		var popUp = new DamagePopUp();
		popUp.SetDamage(amount);

		// Add to the scene tree.
		// Using GetTree().Root.AddChild(popUp) keeps the text in world space.
		GetTree().Root.AddChild(popUp);

		// Position it at the entity's location.
		popUp.GlobalPosition = GetParent<Node3D>().GlobalPosition + Vector3.Up * 1.5f;
	}

	public void Heal(float amount)
	{
		if (amount <= 0) return;

		CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	private void Die()
	{
		GD.Print($"{GetParent().Name} has died.");
		EmitSignal(SignalName.Died);

		// Trigger combat death sound.
		var soundManager = GetTree().Root.FindChild("CombatSoundManager", true, false) as CombatSoundManager;
		if (soundManager != null)
		{
			soundManager.PlayDeathSound();
		}

		// If the player died, show the Game Over screen.
		if (GetParent() is Player3D)
		{
			var gameOverUI = GetTree().Root.FindChild("GameOverUI", true, false) as GameOverUI;
			if (gameOverUI != null)
			{
				gameOverUI.ShowScreen();
			}
		}
	}
}
