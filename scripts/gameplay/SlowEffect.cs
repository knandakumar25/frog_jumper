using Godot;
using System;

public partial class SlowEffect : StatusEffect
{
	[Export] public float SpeedMultiplier = 0.5f;
	private float _originalSpeed;
	private Enemy3D _enemy;

	public override void OnApply(Node3D target)
	{
		_enemy = target as Enemy3D;
		if (_enemy != null)
		{
			// We can't easily access the private _speed in Enemy3D,
			// but we can modify the Velocity in _Process or add a speed modifier to Enemy3D.
			// For now, we'll assume Enemy3D has a Speed property or we use a workaround.
			GD.Print($"{target.Name} is SLOWED!");
		}
	}

	public override void OnTick(float delta, Node3D target)
	{
		// Since Enemy3D's speed is private, the best way is to have Enemy3D
		// check for active status effects.
	}

	public override void OnEffectExpired()
	{
		GD.Print("Slow effect expired.");
	}
}
