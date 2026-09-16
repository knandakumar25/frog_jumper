using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// CombatVFXManager handles the "Neon Pulse" visual cues for combat.
/// It provides methods to trigger hit-stops, neon pulses, screenshakes, and damage pop-ups.
/// </summary>
public partial class CombatVFXManager : Node
{
	public static CombatVFXManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>
	/// Creates a brief pause in time to emphasize a hit's impact.
	/// </summary>
	public async void TriggerHitStop(float duration = 0.05f)
	{
		float originalScale = Engine.TimeScale;
		Engine.TimeScale = 0.05f; // Near freeze
		await ToSignal(GetTree().CreateTimer(duration, false), "timeout");
		Engine.TimeScale = originalScale;
	}

	/// <summary>
	/// Creates a neon pulse effect around a target entity.
	/// </summary>
	public void PlayAbilityPulse(Node3D target, Color color, float radius = 3.0f)
	{
		// Spawn a pulse sphere.
		var pulseSphere = new MeshInstance3D();
		var sphereMesh = new SphereMesh { Radius = 0.1f, Height = 0.2f };
		pulseSphere.Mesh = sphereMesh;
		pulseSphere.GlobalPosition = target.GlobalPosition;

		// Create a neon material.
		var mat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0, 0, 0, 0),
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = 5.0f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};
		pulseSphere.MaterialOverride = mat;

		AddChild(pulseSphere);

		// Animate pulse expansion and fade.
		var tween = pulseSphere.CreateTween().SetParallel(true);
		tween.TweenProperty(pulseSphere, "scale", new Vector3(radius * 2, radius * 2, radius * 2), 0.3f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

		// Fade out alpha.
		var fadeTween = CreateTween();
		fadeTween.TweenProperty(mat, "albedo_color:a", 0.0f, 0.3f);
		fadeTween.Finished += () => pulseSphere.QueueFree();
	}

	/// <summary>
	/// Triggers a brief camera shake for high-impact abilities.
	/// </summary>
	public void ScreenShake(float intensity = 0.2f, float duration = 0.2f)
	{
		var cameraPivot = GetTree().Root.FindChild("CameraPivot", true, false) as Node3D;
		if (cameraPivot == null) return;

		var tween = cameraPivot.CreateTween();
		Vector3 originalPos = cameraPivot.Position;

		for (int i = 0; i < 6; i++)
		{
			Vector3 offset = new Vector3(
				(float)GD.RandRange(-intensity, intensity),
				(float)GD.RandRange(-intensity, intensity),
				(float)GD.RandRange(-intensity, intensity)
			);
			tween.TweenProperty(cameraPivot, "position", originalPos + offset, duration / 6f);
		}
		tween.TweenProperty(cameraPivot, "position", originalPos, 0.05f);
	}

	/// <summary>
	/// Spawns a holographic damage number that floats and glitches.
	/// </summary>
	public void SpawnDamagePopup(Node3D target, float damage, Color color)
	{
		var popup = new DamagePopup(damage, color);
		AddChild(popup);
		popup.GlobalPosition = target.GlobalPosition + Vector3.Up * 1.5f;
	}
}
