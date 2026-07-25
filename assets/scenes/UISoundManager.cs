using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for playing procedural Cyberpunk UI sound effects
/// (hover blips, button clicks, screen transition hums) with an AudioStreamPlayer pool.
/// </summary>
public partial class UISoundManager : Node
{
	public static UISoundManager Instance { get; private set; }

	private const int POOL_SIZE = 8;
	private List<AudioStreamPlayer> _playerPool = new List<AudioStreamPlayer>();
	private int _nextPlayerIndex = 0;

	private AudioStreamWav _hoverStream;
	private AudioStreamWav _clickStream;
	private AudioStreamWav _transitionStream;

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;

		for (int i = 0; i < POOL_SIZE; i++)
		{
			var player = new AudioStreamPlayer
			{
				Name = "UIAudioPlayer_" + i,
				Bus = "Master",
				VolumeDb = 2.0f
			};
			AddChild(player);
			_playerPool.Add(player);
		}

		GenerateSoundStreams();
	}

	private void GenerateSoundStreams()
	{
		_hoverStream      = GenerateSynthTone(1200f, 1600f, 0.04f,  0.4f);
		_clickStream      = GenerateSynthTone(400f,  900f,  0.06f,  0.7f);
		_transitionStream = GenerateSynthTone(700f,  250f,  0.14f,  0.5f);
	}

	private AudioStreamWav GenerateSynthTone(float startFreq, float endFreq, float durationSeconds, float volumeScale)
	{
		int sampleRate = 44100;
		int totalSamples = (int)(sampleRate * durationSeconds);
		byte[] buffer = new byte[totalSamples * 2];

		float phase = 0f;
		for (int i = 0; i < totalSamples; i++)
		{
			float t = (float)i / totalSamples;
			float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
			float envelope = Mathf.Sin(t * Mathf.Pi); // Smooth bell curve

			phase += 2f * Mathf.Pi * currentFreq / sampleRate;
			float sampleVal = Mathf.Sin(phase) * envelope * volumeScale;
			short pcmSample = (short)Mathf.Clamp(sampleVal * short.MaxValue, short.MinValue, short.MaxValue);

			byte[] bytes = BitConverter.GetBytes(pcmSample);
			buffer[i * 2]     = bytes[0];
			buffer[i * 2 + 1] = bytes[1];
		}

		return new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = sampleRate,
			Stereo = false,
			LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
			Data = buffer
		};
	}

	public void PlayHoverSound()
	{
		PlayStream(_hoverStream);
	}

	public void PlayClickSound()
	{
		PlayStream(_clickStream);
	}

	public void PlayTransitionSound()
	{
		PlayStream(_transitionStream);
	}

	private void PlayStream(AudioStreamWav stream)
	{
		if (stream == null || _playerPool.Count == 0) return;

		var player = _playerPool[_nextPlayerIndex];
		_nextPlayerIndex = (_nextPlayerIndex + 1) % POOL_SIZE;

		player.Stream = stream;
		player.Play();
	}

	/// <summary>
	/// Recursively connects all buttons under <paramref name="rootNode"/> to automatically
	/// play hover and click sound effects.
	/// </summary>
	public static void AutoWireButtons(Node rootNode)
	{
		if (rootNode == null || Instance == null) return;

		foreach (Node child in rootNode.GetChildren())
		{
			if (child is Button button)
			{
				button.MouseEntered += () => Instance.PlayHoverSound();
				button.Pressed      += () => Instance.PlayClickSound();
			}
			else if (child is CyberButton cyberBtn)
			{
				cyberBtn.Pressed += () => Instance.PlayClickSound();
			}

			AutoWireButtons(child);
		}
	}
}
