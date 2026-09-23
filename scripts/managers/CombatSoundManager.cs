using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// CombatSoundManager provides procedural, synth-based sound effects for combat actions.
/// It uses a circular pool of AudioStreamPlayers to handle overlapping sound effects.
/// </summary>
public partial class CombatSoundManager : Node
{
	public static CombatSoundManager Instance { get; private set; }

	private const int POOL_SIZE = 12;
	private List<AudioStreamPlayer> _playerPool = new List<AudioStreamPlayer>();
	private int _nextPlayerIndex = 0;

	private AudioStreamWav _hitStream;
	private AudioStreamWav _attackStream;
	private AudioStreamWav _deathStream;

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
				Name = "CombatAudioPlayer_" + i,
				Bus = "Master",
				VolumeDb = 1.0f
			};
			AddChild(player);
			_playerPool.Add(player);
		}

		GenerateCombatSounds();
	}

	private void GenerateCombatSounds()
	{
		// Sharp, punchy thud for hits.
		_hitStream = GenerateSynthTone(600f, 100f, 0.08f, 0.8f);
		// Quick, sweeping whoosh for attacks.
		_attackStream = GenerateSynthTone(300f, 800f, 0.12f, 0.5f);
		// Low-frequency rumble/crash for death.
		_deathStream = GenerateSynthTone(200f, 50f, 0.4f, 0.9f);
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

	public void PlayHitSound() => PlayStream(_hitStream);
	public void PlayAttackSound() => PlayStream(_attackStream);
	public void PlayDeathSound() => PlayStream(_deathStream);

	private void PlayStream(AudioStreamWav stream)
	{
		if (stream == null || _playerPool.Count == 0) return;
		var player = _playerPool[_nextPlayerIndex];
		_nextPlayerIndex = (_nextPlayerIndex + 1) % POOL_SIZE;
		player.Stream = stream;
		player.Play();
	}
}
