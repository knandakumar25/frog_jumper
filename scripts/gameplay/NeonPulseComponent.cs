using Godot;
using System;

/// <summary>
/// NeonPulseComponent can be attached to a MeshInstance3D or OmniLight3D
/// to make its emission/intensity pulse in sync with the NeonPulseManager.
/// </summary>
public partial class NeonPulseComponent : Node
{
	[Export] public float PulseOffset = 0f;
	[Export] public float MinIntensity = 0.5f;
	[Export] public float MaxIntensity = 2.0f;
	[Export] public bool AffectsLight = false;

	private MeshInstance3D _mesh;
	private OmniLight3D _light;
	private StandardMaterial3D _material;

	public override void _Ready()
	{
		var parent = GetParent();
		if (parent is MeshInstance3D mesh)
		{
			_mesh = mesh;
			_material = mesh.GetActiveMaterial(0) as StandardMaterial3D;
		}
		else if (parent is OmniLight3D light)
		{
			_light = light;
		}
	}

	public override void _Process(double delta)
	{
		if (NeonPulseManager.Instance == null) return;

		float pulse = NeonPulseManager.Instance.GetPulseValueWithOffset(PulseOffset);
		float intensity = Mathf.Lerp(MinIntensity, MaxIntensity, pulse);

		if (_material != null)
		{
			_material.EmissionEnergyMultiplier = intensity;
		}

		if (_light != null)
		{
			_light.LightEnergy = intensity;
		}
	}
}
