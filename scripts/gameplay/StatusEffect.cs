using Godot;
using System;

/// <summary>
/// Base class for status effects (Burn, Slow, Stun, etc.) applied to entities.
/// </summary>
public abstract partial class StatusEffect : Node
{
	[Export] public string EffectName { get; set; }
	[Export] public float Duration { get; set; } = 3.0f;
	protected float _elapsed = 0f;

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;
		OnTick((float)delta, GetParent<Node3D>());
		if (_elapsed >= Duration)
		{
			OnEffectExpired();
			QueueFree();
		}
	}

	public abstract void OnApply(Node3D target);
	public abstract void OnTick(float delta, Node3D target);
	public virtual void OnEffectExpired() { }
}
