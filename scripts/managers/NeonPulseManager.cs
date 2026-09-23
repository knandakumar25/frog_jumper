using Godot;
using System;

/// <summary>
/// NeonPulseManager controls the global rhythmic pulsing of the game's neon environment.
/// It provides a synchronized pulse value that other objects can use to modulate their emissives.
/// </summary>
public partial class NeonPulseManager : Node
{
	public static NeonPulseManager Instance { get; private set; }

	[Export] public float BasePulseSpeed = 2.0f;
	[Export] public float PulseAmplitude = 1.0f;

	private float _time = 0f;

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta * BasePulseSpeed;
	}

	/// <summary>
	/// Returns a value between 0.0 and 1.0 based on a sine wave.
	/// </summary>
	public float GetPulseValue()
	{
		return (Mathf.Sin(_time) + 1.0f) * 0.5f * PulseAmplitude;
	}

	/// <summary>
	/// Returns a value between 0.0 and 1.0 with a custom offset for staggered pulsing.
	/// </summary>
	public float GetPulseValueWithOffset(float offset)
	{
		return (Mathf.Sin(_time + offset) + 1.0f) * 0.5f * PulseAmplitude;
	}

	public void SetPulseSpeed(float speed)
	{
		BasePulseSpeed = speed;
	}
}
