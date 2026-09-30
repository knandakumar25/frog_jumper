using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// UISoundManager is a singleton that provides a system for playing procedural,
/// cyberpunk-themed UI sound effects. Instead of relying on external audio files,
/// it synthesizes "synth blips" at runtime and manages them via a circular
/// pool of AudioStreamPlayers to ensure seamless overlapping playback.
///
/// Unity Migration Note:
/// 1. Audio Engine: Replace Godot's AudioStreamPlayer pool with an array of
///    AudioSource components on a dedicated 'SoundManager' GameObject.
/// 2. Synthesis: Unity does not have a direct equivalent to AudioStreamWav's
///    Data buffer. Use OnAudioFilterRead or a custom AudioClip created from
///    a float array to generate procedural tones.
/// 3. Auto-Wiring: Replace the recursive AutoWireButtons with a 'UI Event Bus'
///    or a simple BaseButton script that calls the SoundManager singleton.
/// </summary>
public partial class UISoundManager : Node
{
	public static UISoundManager Instance { get; private set; }

	// Use a pool of players to avoid sound cutting when many buttons are hovered rapidly.
	private const int POOL_SIZE = 8;
	private List<AudioStreamPlayer> _playerPool = new List<AudioStreamPlayer>();
	private int _nextPlayerIndex = 0;

	// Procedurally generated audio streams.
	private AudioStreamWav _hoverStream;
	private AudioStreamWav _clickStream;
	private AudioStreamWav _transitionStream;

	public override void _Ready()
	{
		// Singleton enforcement.
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;

		// Initialize the audio player pool.
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

	/// <summary>
	/// Synthesizes the three core UI sound effects using frequency sweeps.
	/// </summary>
	private void GenerateSoundStreams()
	{
		// High-frequency blip for hover.
		_hoverStream      = GenerateSynthTone(1200f, 1600f, 0.04f,  0.4f);
		// Mid-frequency thud for clicks.
		_clickStream      = GenerateSynthTone(400f,  900f,  0.06f,  0.7f);
		// Descending slide for scene transitions.
		_transitionStream = GenerateSynthTone(700f,  250f,  0.14f,  0.5f);
	}

	/// <summary>
	/// Generates a raw PCM audio buffer for a synthetic tone.
	/// Implements a linear frequency sweep (chirp) and a sine-based volume envelope.
	/// </summary>
	/// <param name="startFreq">Starting frequency in Hz.</param>
	/// <param name="endFreq">Ending frequency in Hz.</param>
	/// <param name="durationSeconds">Total length of the sound.</param>
	/// <param name="volumeScale">Amplitude multiplier.</param>
	private AudioStreamWav GenerateSynthTone(float startFreq, float endFreq, float durationSeconds, float volumeScale)
	{
		int sampleRate = 44100;
		int totalSamples = (int)(sampleRate * durationSeconds);
		byte[] buffer = new byte[totalSamples * 2]; // 16-bit PCM = 2 bytes per sample.

		float phase = 0f;
		for (int i = 0; i < totalSamples; i++)
		{
			float t = (float)i / totalSamples;
			// Linear interpolation between start and end frequency.
			float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
			// Bell-curve envelope to prevent audio popping (clicks) at start/end.
			float envelope = Mathf.Sin(t * Mathf.Pi);

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

	/// <summary>
	/// Assigns the stream to the next available player in the circular pool and plays it.
	/// </summary>
	private void PlayStream(AudioStreamWav stream)
	{
		if (stream == null || _playerPool.Count == 0) return;

		var player = _playerPool[_nextPlayerIndex];
		_nextPlayerIndex = (_nextPlayerIndex + 1) % POOL_SIZE;

		player.Stream = stream;
		player.Play();
	}

	/// <summary>
	/// Recursively scans the provided node hierarchy and connects all Button
	/// components to the global UI sound effects.
	/// </summary>
	/// <param name="rootNode">The root node to start the recursive search from.</param>
	public static void AutoWireButtons(Node rootNode)
	{
		if (rootNode == null || Instance == null) return;

		foreach (Node child in rootNode.GetChildren())
		{
			if (child is Button button)
			{
				button.FocusEntered += () => Instance.PlayHoverSound();
				button.Pressed      += () => Instance.PlayClickSound();
			}
			else if (child is CyberButton cyberBtn)
			{
				// CyberButton handles its own hover sounds, only wire the click.
				cyberBtn.Pressed += () => Instance.PlayClickSound();
			}

			AutoWireButtons(child);
		}
	}
}
