using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// HazardZone is an area that periodically applies a specific status effect
/// (Burn, Slow, Stun) to any player or enemy standing within its bounds.
/// </summary>
public partial class HazardZone : Area3D
{
	public enum EffectType { Burn, Slow, Stun }

	[Export] public EffectType ZoneEffect = EffectType.Burn;
	[Export] public float ApplicationInterval = 2.0f;
	[Export] public float EffectDuration = 3.0f;

	private float _timer = 0f;

	public override void _Process(double delta)
	{
		_timer += (float)delta;
		if (_timer >= ApplicationInterval)
		{
			ApplyHazard();
			_timer = 0f;
		}
	}

	private void ApplyHazard()
	{
		var bodies = GetOverlappingBodies();
		foreach (var body in bodies)
		{
			if (body is Enemy3D enemy)
			{
				enemy.ApplyStatusEffect(CreateEffect());
			}
			else if (body is Player3D player)
			{
				player.ApplyStatusEffect(CreateEffect());
			}
		}
	}

	private StatusEffect CreateEffect()
	{
		StatusEffect effect = ZoneEffect switch
		{
			EffectType.Burn => new BurnEffect { Duration = EffectDuration },
			EffectType.Slow => new SlowEffect { Duration = EffectDuration, SpeedMultiplier = 0.5f },
			EffectType.Stun => new StunEffect { Duration = EffectDuration },
			_ => new BurnEffect { Duration = EffectDuration }
		};
		return effect;
	}
}
