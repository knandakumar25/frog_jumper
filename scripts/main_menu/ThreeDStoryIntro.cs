using Godot;
using System.Collections.Generic;

/// <summary>
/// ThreeDStoryIntro is a native 3D scene that coordinates the "Pre-Tutorial Mission Briefing."
/// It integrates a 3D environment featuring the AXIOM-7 Floating Robot Buddy with a 2D
/// UI overlay for localized dialogue, voice-over audio, and navigation controls.
///
/// Unity Migration Note:
/// 1. Scene Setup: Use a standard Unity 3D scene. Place the Robot Buddy Prefab in the world
///    and use a separate UI Canvas for the dialogue overlay.
/// 2. Typewriter Effect: Replace the manual _Process logic with a Coroutine that updates
///    the text of a TextMeshPro component character by character.
/// 3. Audio: Use Unity's AudioSource. Map voice-over files to locales using a
///    localized asset table or an Addressables-based loading system.
/// 4. Navigation: Implement the input handling using the new Unity Input System (Input Actions)
///    to support both keyboard and gamepad seamlessly.
/// </summary>
public partial class ThreeDStoryIntro : Node3D
{
	// 2D UI Overlay references
	private Label? _titleBanner;
	private RichTextLabel? _dialogueLabel;
	private CyberDialogueBubble? _cyberBubble;
	private Button? _continueButton;
	private Button? _skipButton;
	private Label? _navPromptLabel;

	// Narrative state tracking
	private List<string> _dialogueLines = new();
	private int _currentLineIndex = 0;
	private float _typewriterSpeed = 0.03f;
	private bool _isTyping = false;
	private string _currentText = "";
	private int _currentCharIndex = 0;
	private float _typewriterTimer = 0f;
	private AudioStreamPlayer? _voicePlayer;

	// 3D Entity references
	private FloatingRobotBuddy3D? _robotBuddy;
	private Node3D? _speechTerminal3D;
	private Vector3 _speechTerminalInitPos;
	private Vector3 _speechTerminalInitRot;
	private float _speechTerminalTime = 0f;
	private Camera3D? _camera;
	private Vector3 _cameraInitPos;
	private float _cameraTime = 0f;

	public override void _Ready()
	{
		// Ensure the device screen does not dim or sleep during the mission briefing.
		DisplayServer.ScreenSetKeepOn(true);
		GD.Print("ThreeDStoryIntro: DisplayServer.ScreenSetKeepOn enabled");

		// Stop ambient menu music to prioritize the voice recordings.
		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null)
		{
			global.Call("stop_menu_music");
			GD.Print("ThreeDStoryIntro: Stopped menu music");
		}

		// Initialize the audio player for AXIOM-7's transmission clips.
		_voicePlayer = new AudioStreamPlayer();
		AddChild(_voicePlayer);

		// Resolve 2D UI overlay node references.
		_titleBanner    = GetNodeOrNull<Label>("CanvasLayer/UIOverlay/TitleBanner");
		_dialogueLabel  = GetNodeOrNull<RichTextLabel>("CanvasLayer/UIOverlay/DialogueBox/MarginContainer/DialogueLabel");
		_cyberBubble    = GetNodeOrNull<CyberDialogueBubble>("CanvasLayer/UIOverlay/CyberDialogueBubble");
		_continueButton = GetNodeOrNull<Button>("CanvasLayer/UIOverlay/ContinueButton");
		_skipButton     = GetNodeOrNull<Button>("CanvasLayer/UIOverlay/SkipButton");

		if (_cyberBubble != null)
		{
			_cyberBubble.LineCompleted += OnLineFinished;
			_cyberBubble.AdvanceRequested += OnContinueButtonPressed;
			if (_dialogueLabel == null)
			{
				_dialogueLabel = _cyberBubble.DialogueLabel;
			}
		}

		// Resolve 3D Camera reference for parallax breathing.
		_camera = GetNodeOrNull<Camera3D>("Camera3D");
		if (_camera != null)
		{
			_cameraInitPos = _camera.Position;
		}

		// Instantiate the 3D Robot Buddy in the scene.
		Setup3DRobotBuddy();

		// Define the dialogue sequence using localized translation keys.
		_dialogueLines = new List<string>
		{
			TranslationServer.Translate("STORY_LINE_1_INTRO"),
			TranslationServer.Translate("STORY_LINE_2"),
			TranslationServer.Translate("STORY_LINE_3"),
			TranslationServer.Translate("STORY_LINE_4")
		};

		if (_titleBanner != null)
			_titleBanner.Text = TranslationServer.Translate("MISSION BRIEFING TRANSMISSION");

		if (_continueButton != null)
		{
			_continueButton.Visible = false;
			_continueButton.Pressed += OnContinueButtonPressed;
		}

		if (_skipButton != null)
			_skipButton.Pressed += OnSkipButtonPressed;

		if (_dialogueLabel != null)
			_dialogueLabel.VisibleCharacters = 0;

		// Setup navigation prompt footer for the UI.
		SetupNavPromptFooter();

		if (global != null)
		{
			global.Connect("input_device_changed", Callable.From<bool>((isGamepad) => UpdateStoryPrompts()));
		}

		UpdateStoryPrompts();

		// Synchronize dialogue transmission with AXIOM-7's high-altitude fly-in arrival
		if (_robotBuddy != null)
		{
			_robotBuddy.FlyInCompleted += OnRobotBuddyFlyInCompleted;
			// Fallback safety timer in case signal is missed
			GetTree().CreateTimer(1.8f).Timeout += () =>
			{
				if (_currentLineIndex == 0 && !_isTyping)
				{
					StartNextLine();
				}
			};
		}
		else
		{
			StartNextLine();
		}
	}

	private void OnRobotBuddyFlyInCompleted()
	{
		if (_currentLineIndex == 0 && !_isTyping)
		{
			StartNextLine();
		}
	}

	/// <summary>
	/// Instantiates and configures the AXIOM-7 Floating Robot Buddy in the 3D world.
	/// </summary>
	private void Setup3DRobotBuddy()
	{
		_robotBuddy = new FloatingRobotBuddy3D
		{
			Scale = new Vector3(0.75f, 0.75f, 0.75f),
			Position = new Vector3(-0.85f, -0.12f, -1.0f),
			RotationDegrees = new Vector3(0f, 15f, 0f),
			HoverSpeed = 2.4f,
			HoverHeight = 0.08f,
			PulseSpeed = 3.5f
		};
		AddChild(_robotBuddy);
		GD.Print("ThreeDStoryIntro: Successfully instantiated native 3D AXIOM-7 Robot Buddy in world space");
	}

	/// <summary>
	/// Instantiates and configures the 3D Holographic Speech Terminal in the world.
	/// </summary>
	private void Setup3DSpeechTerminal()
	{
		string terminalPath = "res://assets/models/menu/axiom7_speech_terminal.glb";
		if (ResourceLoader.Exists(terminalPath))
		{
			var scene = GD.Load<PackedScene>(terminalPath);
			if (scene != null)
			{
				_speechTerminal3D = scene.Instantiate<Node3D>();
				_speechTerminal3D.Name = "SpeechTerminal3D";
				_speechTerminalInitPos = new Vector3(0.95f, -0.05f, -1.2f);
				_speechTerminalInitRot = new Vector3(0f, -12f, 2f);
				_speechTerminal3D.Position = _speechTerminalInitPos;
				_speechTerminal3D.RotationDegrees = _speechTerminalInitRot;
				_speechTerminal3D.Scale = new Vector3(0.72f, 0.72f, 0.72f);
				AddChild(_speechTerminal3D);
				GD.Print("ThreeDStoryIntro: Successfully instantiated 3D AXIOM-7 Speech Terminal in world space");
			}
		}
	}

	/// <summary>
	/// Initiates the display of the next dialogue line and triggers corresponding voice audio.
	/// </summary>
	private void StartNextLine()
	{
		if (_currentLineIndex >= _dialogueLines.Count)
		{
			FinishIntro();
			return;
		}

		if (_continueButton != null)
			_continueButton.Visible = false;

		_currentText = _dialogueLines[_currentLineIndex];

		if (_cyberBubble != null)
		{
			_cyberBubble.SetDialogueLine(_currentText, TranslationServer.Translate("STORY_LINE_1"));
		}
		else if (_dialogueLabel != null)
		{
			_dialogueLabel.BbcodeEnabled = true;
			_dialogueLabel.Text = _currentText;
			_dialogueLabel.VisibleCharacters = 0;
			_currentCharIndex = 0;
			_typewriterTimer = 0f;
		}

		_isTyping = true;
		_robotBuddy?.SetTransmitting(true);

		PlayLineAudio(_currentLineIndex + 1);
	}

	/// <summary>
	/// Loads and plays the English voice recording for the current line.
	/// Dialogue audio remains in English across all language versions, while subtitles match the selected language.
	/// </summary>
	private void PlayLineAudio(int lineNum)
	{
		string audioPath = $"res://assets/speech/AXIOM-7/english/AXIOM-7-{lineNum}.mp3";

		AudioStream? stream = null;
		if (ResourceLoader.Exists(audioPath))
		{
			stream = GD.Load<AudioStream>(audioPath);
		}

		if (stream == null)
		{
			string globalPath = ProjectSettings.GlobalizePath(audioPath);
			if (System.IO.File.Exists(globalPath))
			{
				byte[] bytes = System.IO.File.ReadAllBytes(globalPath);
				if (bytes.Length > 0)
				{
					var mp3 = new AudioStreamMP3();
					mp3.Data = bytes;
					stream = mp3;
				}
			}
		}

		if (stream != null && _voicePlayer != null)
		{
			_voicePlayer.Stream = stream;
			_voicePlayer.Play();
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (_cyberBubble == null && _isTyping)
		{
			_typewriterTimer += dt;
			if (_typewriterTimer >= _typewriterSpeed)
			{
				_typewriterTimer = 0f;
				_currentCharIndex++;

				if (_dialogueLabel != null)
				{
					_dialogueLabel.VisibleCharacters = _currentCharIndex;
					if (_currentCharIndex >= _dialogueLabel.GetTotalCharacterCount())
					{
						OnLineFinished();
					}
				}
			}
		}

		// Floating 3D speech terminal subtle idle hover & swivel
		if (_speechTerminal3D != null)
		{
			_speechTerminalTime += dt;
			float bobY = Mathf.Sin(_speechTerminalTime * 2.2f) * 0.025f;
			float tiltZ = Mathf.Sin(_speechTerminalTime * 1.8f) * 1.2f;
			_speechTerminal3D.Position = new Vector3(_speechTerminalInitPos.X, _speechTerminalInitPos.Y + bobY, _speechTerminalInitPos.Z);
			_speechTerminal3D.RotationDegrees = new Vector3(_speechTerminalInitRot.X, _speechTerminalInitRot.Y, _speechTerminalInitRot.Z + tiltZ);
		}

		// Subtle cinematic camera sway / breathing across the command deck
		if (_camera != null)
		{
			_cameraTime += dt;
			float camSwayX = Mathf.Sin(_cameraTime * 0.45f) * 0.05f;
			float camSwayY = Mathf.Cos(_cameraTime * 0.35f) * 0.03f;
			_camera.Position = new Vector3(_cameraInitPos.X + camSwayX, _cameraInitPos.Y + camSwayY, _cameraInitPos.Z);
		}
	}

	/// <summary>
	/// Marks the end of the current typewriter sequence and reveals the "Continue" button.
	/// </summary>
	private void OnLineFinished()
	{
		_isTyping = false;
		_robotBuddy?.SetTransmitting(false);
		_cyberBubble?.CompleteTyping();
		if (_dialogueLabel != null && _cyberBubble == null)
			_dialogueLabel.VisibleCharacters = -1; // Show full text.

		if (_continueButton != null)
		{
			_continueButton.Visible = true;
			_continueButton.CallDeferred(Control.MethodName.GrabFocus);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Global "Accept" action for advancing dialogue or continuing.
		if (@event.IsActionPressed("ui_accept"))
		{
			if (_isTyping)
			{
				OnLineFinished();
				GetViewport()?.SetInputAsHandled();
			}
			else if (_continueButton != null && _continueButton.Visible)
			{
				OnContinueButtonPressed();
				GetViewport()?.SetInputAsHandled();
			}
		}
		// Global "Cancel" action for skipping the intro.
		else if (@event.IsActionPressed("ui_cancel"))
		{
			OnSkipButtonPressed();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void OnContinueButtonPressed()
	{
		_robotBuddy?.SkipFlyIn();
		UISoundManager.Instance?.PlayClickSound();
		if (_isTyping)
		{
			OnLineFinished();
		}
		else
		{
			_currentLineIndex++;
			StartNextLine();
		}
	}

	private void OnSkipButtonPressed()
	{
		_robotBuddy?.SkipFlyIn();
		UISoundManager.Instance?.PlayClickSound();
		if (_voicePlayer != null && _voicePlayer.Playing)
			_voicePlayer.Stop();

		FinishIntro();
	}

	/// <summary>
	/// Finalizes the intro sequence and transitions the user to the level selection screen.
	/// </summary>
	private void FinishIntro()
	{
		_robotBuddy?.SetTransmitting(false);
		DisplayServer.ScreenSetKeepOn(false);
		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/level_selection.tscn");
	}

	/// <summary>
	/// Initializes the navigation prompt footer in the 2D UI overlay.
	/// </summary>
	private void SetupNavPromptFooter()
	{
		var uiOverlay = GetNodeOrNull<Control>("CanvasLayer/UIOverlay");
		if (uiOverlay == null) return;

		var footerContainer = new MarginContainer
		{
			Name = "NavPromptFooter",
			CustomMinimumSize = new Vector2(0, 48)
		};

		_navPromptLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		if (orbitronFont != null) _navPromptLabel.AddThemeFontOverride("font", orbitronFont);
		_navPromptLabel.AddThemeFontSizeOverride("font_size", 16);
		_navPromptLabel.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.85f));

		footerContainer.AddChild(_navPromptLabel);
		uiOverlay.AddChild(footerContainer);

		footerContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide, Control.LayoutPresetMode.KeepSize, 16);
		footerContainer.OffsetTop = -64f;
		footerContainer.OffsetBottom = -16f;

		UpdateStoryPrompts();
	}

	/// <summary>
	/// Updates the labels on the skip/continue buttons based on the detected input device.
	/// </summary>
	private void UpdateStoryPrompts()
	{
		var global = GetNodeOrNull<Node>("/root/Global");
		bool isGamepad = (global != null && (bool)global.Get("is_gamepad_mode"));

		if (_skipButton != null)
			_skipButton.Text = isGamepad ? "Skip (B / Circle) >>>" : "Skip (ESC) >>>";

		if (_continueButton != null)
			_continueButton.Text = isGamepad ? "Continue (A / Cross)" : "Continue (ENTER)";

		if (_navPromptLabel != null && global != null && global.HasMethod("get_nav_prompt"))
		{
			_navPromptLabel.Text = (string)global.Call("get_nav_prompt");
		}
	}
}
