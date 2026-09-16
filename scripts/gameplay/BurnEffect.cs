using Godot;
using System;

public partial class BurnEffect : StatusEffect
{
	[Export] public float DamagePerSecond = 5.0f;

	public override void OnApply(Node3D target)
	{
		GD.Print($"{target.Name} is BURNING!");
	}

	public override void OnTick(float delta, Node3D target)
	{
		var health = target.GetNodeOrNull<Health>("Health") ?? target.FindChild("Health", true, false) as Health;
		if (health != null)
		{
			health.TakeDamage(DamagePerSecond * delta);
		}
	}

	public override void OnEffectExpired()
	{
		GD.Print("Burn effect expired.");
	}
}
