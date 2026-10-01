using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// CyberDialogueBubble is a high-fidelity cyberpunk holographic speech terminal for AXIOM-7.
/// Features:
///   - 9-Slice chamfered cyber armor frame with custom CanvasItem shader.
///   - Animated scanlines, dynamic chromatic aberration, and holographic transmission pulse.
///   - Real-time animated audio frequency visualizer bars that react to voice transmissions.
///   - Holographic AXIOM-7 AI Core avatar emblem with idle breathing rotation.
///   - Directional energy pointer / chevron array pointing toward AXIOM-7 in world space.
///   - Integrated typewriter sequence with high-tech audio teletype blips.
///   - Encrypted transmission telemetry header and animated advance footer prompt.
/// </summary>
public partial class CyberDialogueBubble : Control
{
	[Signal] public delegate void LineCompletedEventHandler();
	[Signal] public delegate void AdvanceRequestedEventHandler();

	[Export] public float TypewriterSpeed { get; set; } = 0.024f;
	[Export] public bool EnableTypingSound { get; set; } = true;

	private NinePatchRect? _frameBackground;
	private ShaderMaterial? _frameMaterial;
	private TextureRect? _avatarIcon;
	private Label? _statusDot;
	private Label? _speakerLabel;
	private Label? _channelLabel;
	private RichTextLabel? _dialogueLabel;
	private Control? _audioVisualizerContainer;
	private List<ColorRect> _visualizerBars = new();
	private TextureRect? _holoPointer;
	private Control? _advancePrompt;
	private Label? _promptText;
	private Label? _promptChevron;

	private bool _isTransmitting = false;
	private bool _isTyping = false;
	private float _typewriterTimer = 0f;
	private int _currentCharIndex = 0;
	private int _totalCharCount = 0;
	private float _time = 0f;
	private float _pointerInitX = 0f;
	private float _avatarBaseAngle = 0f;

	// Procedural audio blip for typing
	private AudioStreamPlayer? _typewriterAudioPlayer;
	private AudioStreamWav? _typewriterClickStream;

	public bool IsTyping => _isTyping;
	public RichTextLabel? DialogueLabel => _dialogueLabel;

	public override void _Ready()
	{
		// Locate or create UI hierarchy
		_frameBackground         = GetNodeOrNull<NinePatchRect>("FrameBackground");
		_holoPointer             = GetNodeOrNull<TextureRect>("HoloPointer");
		_avatarIcon              = GetNodeOrNull<TextureRect>("ContentMargin/VBoxContainer/HeaderBar/AvatarIcon");
		_statusDot               = GetNodeOrNull<Label>("ContentMargin/VBoxContainer/HeaderBar/HeaderInfoVBox/StatusLineHBox/StatusDot");
		_speakerLabel            = GetNodeOrNull<Label>("ContentMargin/VBoxContainer/HeaderBar/HeaderInfoVBox/StatusLineHBox/SpeakerNameLabel");
		_channelLabel            = GetNodeOrNull<Label>("ContentMargin/VBoxContainer/HeaderBar/HeaderInfoVBox/ChannelLabel");
		_audioVisualizerContainer= GetNodeOrNull<Control>("ContentMargin/VBoxContainer/HeaderBar/AudioVisualizer");
		_dialogueLabel           = GetNodeOrNull<RichTextLabel>("ContentMargin/VBoxContainer/DialogueLabel");
		_advancePrompt           = GetNodeOrNull<Control>("ContentMargin/VBoxContainer/FooterBar/PromptIndicator");
		_promptText              = GetNodeOrNull<Label>("ContentMargin/VBoxContainer/FooterBar/PromptIndicator/PromptText");
		_promptChevron           = GetNodeOrNull<Label>("ContentMargin/VBoxContainer/FooterBar/PromptIndicator/PromptChevron");

		if (_frameBackground != null)
		{
			_frameBackground.MouseFilter = MouseFilterEnum.Pass;
			if (_frameBackground.Material is ShaderMaterial sm)
			{
				_frameMaterial = sm;
			}
		}

		if (_dialogueLabel != null)
		{
			_dialogueLabel.MouseFilter = MouseFilterEnum.Pass;
		}

		if (_holoPointer != null)
		{
			_pointerInitX = _holoPointer.Position.X;
		}

		// Cache visualizer bars
		if (_audioVisualizerContainer != null)
		{
			foreach (Node child in _audioVisualizerContainer.GetChildren())
			{
				if (child is ColorRect bar)
				{
					_visualizerBars.Add(bar);
				}
			}
		}

		SetupTypewriterAudio();
		ShowAdvancePrompt(false);
	}

	/// <summary>
	/// Generates a subtle procedural sci-fi micro-click for the typewriter.
	/// </summary>
	private void SetupTypewriterAudio()
	{
		_typewriterAudioPlayer = new AudioStreamPlayer
		{
			Name = "TypewriterAudio",
			Bus = "Master",
			VolumeDb = -22.0f // Subtle background clicks
		};
		AddChild(_typewriterAudioPlayer);

		// Synthesize a soft 1200Hz sine blip (25ms duration)
		int sampleRate = 22050;
		int numSamples = (int)(sampleRate * 0.025f);
		byte[] wavData = new byte[numSamples * 2];

		for (int i = 0; i < numSamples; i++)
		{
			float t = (float)i / sampleRate;
			float env = 1.0f - (float)i / numSamples;
			float sample = Mathf.Sin(t * 1200.0f * Mathf.Tau) * env * 0.35f;
			short val = (short)(sample * 32767);
			wavData[i * 2]     = (byte)(val & 0xFF);
			wavData[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
		}

		_typewriterClickStream = new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = sampleRate,
			Stereo = false,
			Data = wavData
		};
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;

		// 1. Animate Holo Pointer floating toward AXIOM-7 (Left side)
		if (_holoPointer != null)
		{
			float bobX = Mathf.Sin(_time * 3.5f) * 6.0f;
			_holoPointer.Position = new Vector2(_pointerInitX + bobX, _holoPointer.Position.Y);
			_holoPointer.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.75f + Mathf.Sin(_time * 4.0f) * 0.25f);
		}

		// 2. Animate Avatar Icon breathing rotation
		if (_avatarIcon != null)
		{
			_avatarIcon.PivotOffset = _avatarIcon.Size / 2.0f;
			float rot = Mathf.Sin(_time * 2.0f) * 4.5f;
			_avatarIcon.RotationDegrees = rot;
		}

		// 3. Animate Status Dot pulsation
		if (_statusDot != null)
		{
			float alpha = _isTransmitting ? (0.6f + Mathf.Sin(_time * 10.0f) * 0.4f) : 1.0f;
			_statusDot.Modulate = new Color(0.0f, 1.0f, 0.45f, alpha);
		}

		// 4. Animate Audio Visualizer Bars
		if (_visualizerBars.Count > 0)
		{
			for (int i = 0; i < _visualizerBars.Count; i++)
			{
				var bar = _visualizerBars[i];
				float targetHeight;
				if (_isTransmitting)
				{
					// Active audio equalizer bounce
					float freq = 8.0f + i * 4.0f;
					float noise = Mathf.Sin(_time * freq + i * 1.35f) * 0.5f + 0.5f;
					targetHeight = Mathf.Lerp(4.0f, 24.0f, noise);
				}
				else
				{
					// Idle low baseline
					targetHeight = 3.0f;
				}
				bar.CustomMinimumSize = new Vector2(bar.CustomMinimumSize.X, targetHeight);
				bar.Size = new Vector2(bar.Size.X, targetHeight);
			}
		}

		// 5. Update Shader Transmission Pulse
		if (_frameMaterial != null)
		{
			float currentPulse = (float)_frameMaterial.GetShaderParameter("transmission_pulse");
			float targetPulse = _isTransmitting ? 1.0f : 0.0f;
			float newPulse = Mathf.MoveToward(currentPulse, targetPulse, dt * 4.0f);
			_frameMaterial.SetShaderParameter("transmission_pulse", newPulse);
		}

		// 6. Typewriter Progression
		if (_isTyping)
		{
			_typewriterTimer += dt;
			if (_typewriterTimer >= TypewriterSpeed)
			{
				_typewriterTimer = 0f;
				_currentCharIndex++;

				if (_dialogueLabel != null)
				{
					_dialogueLabel.VisibleCharacters = _currentCharIndex;

					// Play subtle teletype sound on alphanumeric characters
					if (EnableTypingSound && _currentCharIndex % 2 == 0 && _typewriterAudioPlayer != null && _typewriterClickStream != null)
					{
						_typewriterAudioPlayer.Stream = _typewriterClickStream;
						_typewriterAudioPlayer.PitchScale = 0.95f + (float)GD.RandRange(0.0, 0.15);
						_typewriterAudioPlayer.Play();
					}

					int total = _dialogueLabel.GetTotalCharacterCount();
					if (total <= 0) total = _totalCharCount;
					if (_currentCharIndex >= total && total > 0)
					{
						CompleteTyping();
					}
				}
			}
		}

		// 7. Animate Prompt Chevron
		if (_promptChevron != null && _advancePrompt != null && _advancePrompt.Visible)
		{
			float pulse = Mathf.Sin(_time * 6.0f) * 0.5f + 0.5f;
			_promptChevron.Modulate = new Color(0.0f, 0.94f, 1.0f, 0.4f + pulse * 0.6f);
		}
	}

	/// <summary>
	/// Initiates display of dialogue line with typewriter effect and holographic glitch snap.
	/// </summary>
	public void SetDialogueLine(string rawText, string speakerName = "AXIOM-7 // TRANSMISSION ONLINE")
	{
		if (_speakerLabel != null)
		{
			_speakerLabel.Text = speakerName;
		}

		if (_dialogueLabel != null)
		{
			_dialogueLabel.BbcodeEnabled = true;
			_dialogueLabel.Text = rawText;
			_dialogueLabel.VisibleCharacters = 0;
			_totalCharCount = _dialogueLabel.GetTotalCharacterCount();
			if (_totalCharCount <= 0)
			{
				_totalCharCount = rawText.Length;
			}
		}

		_currentCharIndex = 0;
		_typewriterTimer = 0f;
		_isTyping = true;
		SetTransmitting(true);
		ShowAdvancePrompt(false);
		TriggerHoloGlitch();
	}

	/// <summary>
	/// Triggers a brief holographic chromatic aberration / scanline glitch on line reveal.
	/// </summary>
	public void TriggerHoloGlitch()
	{
		if (_frameMaterial == null) return;

		_frameMaterial.SetShaderParameter("glitch_intensity", 0.75f);
		var tween = CreateTween();
		tween.TweenProperty(_frameMaterial, "shader_parameter/glitch_intensity", 0.0f, 0.22f)
			 .SetEase(Tween.EaseType.Out)
			 .SetTrans(Tween.TransitionType.Quad);
	}

	public void SetTransmitting(bool transmitting)
	{
		_isTransmitting = transmitting;
	}

	/// <summary>
	/// Completes the typewriter effect immediately and displays the advance prompt.
	/// </summary>
	public void CompleteTyping()
	{
		if (!_isTyping && (_dialogueLabel == null || _dialogueLabel.VisibleCharacters == -1))
			return;

		_isTyping = false;
		SetTransmitting(false);

		if (_dialogueLabel != null)
		{
			_dialogueLabel.VisibleCharacters = -1; // Show full line
		}

		ShowAdvancePrompt(true);
		EmitSignal(SignalName.LineCompleted);
	}

	public void ShowAdvancePrompt(bool visible)
	{
		if (_advancePrompt == null) return;

		if (visible)
		{
			_advancePrompt.Visible = true;
			_advancePrompt.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
			var tween = CreateTween();
			tween.TweenProperty(_advancePrompt, "modulate:a", 1.0f, 0.35f);
		}
		else
		{
			_advancePrompt.Visible = false;
		}
	}

	/// <summary>
	/// Allows clicking directly on the dialogue bubble to fast-forward typewriter or advance line.
	/// </summary>
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			if (_isTyping)
			{
				CompleteTyping();
			}
			else
			{
				EmitSignal(SignalName.AdvanceRequested);
			}
			AcceptEvent();
		}
	}
}
