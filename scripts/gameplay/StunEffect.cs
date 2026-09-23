using Godot;
using System;

public partial class StunEffect : StatusEffect
{
	public override void OnApply(Node3D target)
	{
		var enemy = target as Enemy3D;
		if (enemy != null)
		{
			// We can't set state directly, but we can call a method.
			GD.Print($"{target.Name} is STUNNED!");
		}
	}

	public override void OnTick(float delta, Node3D target)
	{
		// Stun usually just prevents action.
	}

	public override void OnEffectExpired()
	{
		GD.Print("Stun effect expired.");
	}
}
