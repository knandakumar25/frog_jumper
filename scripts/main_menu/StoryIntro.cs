using Godot;
using System.Collections.Generic;

/// <summary>
/// StoryIntro manages the cinematic "Mission Briefing" sequence. It coordinates
/// a series of localized dialogue lines delivered via a typewriter effect,
/// synchronized with voice-over audio and a 3D-rendered robot companion.
///
/// Unity Migration Note:
/// 1. Dialogue System: Replace the manual typewriter logic with a plugin like
///    Dialogue System for Unity or Ink.
/// 2. 3D Integration: Instead of a SubViewport, use a separate Layer and a
///    Camera with a specific Culling Mask to render the robot buddy behind
///    the UI.
/// 3. Audio: Use Unity's AudioSource. Use a localized Audio Clip mapping
///    system (e.g., a ScriptableObject) to handle voice-over paths.
/// 4. UI: Use a TMP text component with a Coroutine to handle the typewriter
///    character revelation.
/// </summary>
public partial class StoryIntro : Control
{
	// UI and Sprite references
	private AnimatedSprite2D? robotSprite;
	private Panel? dialogueBox;
	private RichTextLabel? dialogueLabel;
	private CyberDialogueBubble? cyberBubble;
	private Button? continueButton;
	private Button? skipButton;

	// Dialogue state tracking
	private List<string> dialogueLines = new();
	private int currentLineIndex = 0;
	private float typewriterSpeed = 0.03f;
	private bool isTyping = false;
	private string currentText = "";
	private int currentCharIndex = 0;
	private float typewriterTimer = 0f;
	private AudioStreamPlayer? voicePlayer;

	public override void _Ready()
	{
		// Prevent the device from entering sleep mode during the briefing transmission.
		DisplayServer.ScreenSetKeepOn(true);
		GD.Print("StoryIntro: DisplayServer.ScreenSetKeepOn enabled");

		// Resolve UI node references.
		robotSprite = GetNodeOrNull<AnimatedSprite2D>("HBoxContainer/RobotContainer/RobotSprite");
		dialogueBox = GetNodeOrNull<Panel>("HBoxContainer/DialogueBox");
		dialogueLabel = GetNodeOrNull<RichTextLabel>("HBoxContainer/DialogueBox/MarginContainer/DialogueLabel");
		cyberBubble = GetNodeOrNull<CyberDialogueBubble>("HBoxContainer/CyberDialogueBubble");
		continueButton = GetNodeOrNull<Button>("ContinueButton");
		skipButton = GetNodeOrNull<Button>("SkipButton");

		if (cyberBubble != null)
		{
			cyberBubble.LineCompleted += () =>
			{
				isTyping = false;
				if (continueButton != null)
					continueButton.Visible = true;
			};
			cyberBubble.AdvanceRequested += OnContinueButtonPressed;
			if (dialogueLabel == null)
				dialogueLabel = cyberBubble.DialogueLabel;
		}

		// Stop ambient home screen music to prioritize voice-over audio.
		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null)
		{
			global.Call("stop_menu_music");
			GD.Print("StoryIntro: Stopped menu music via Global");
		}

		// Initialize the dedicated voice-over audio player.
		voicePlayer = new AudioStreamPlayer();
		AddChild(voicePlayer);

		// Initialize the dialogue sequence using localized translation keys.
		// The first line includes BBCode for centering and coloring the header.
		dialogueLines = new List<string>
		{
			$"[center][color=#00FF00]{TranslationServer.Translate("STORY_LINE_1")}[/color][/center]\n\n{TranslationServer.Translate("STORY_LINE_1_INTRO")}",
			TranslationServer.Translate("STORY_LINE_2"),
			TranslationServer.Translate("STORY_LINE_3"),
			TranslationServer.Translate("STORY_LINE_4")
		};

		// Localize the title banner.
		var titleBanner = GetNodeOrNull<Label>("TitleBanner");
		if (titleBanner != null)
			titleBanner.Text = TranslationServer.Translate("MISSION BRIEFING TRANSMISSION");

		// Default UI state: Hide the continue button until the typewriter finishes.
		if (continueButton != null)
			continueButton.Visible = false;
		else
			GD.PrintErr("StoryIntro: ContinueButton node not found");

		if (dialogueLabel != null)
			dialogueLabel.VisibleCharacters = 0;
		else
			GD.PrintErr("StoryIntro: DialogueLabel node not found");

		if (continueButton != null)
			continueButton.Pressed += OnContinueButtonPressed;
		if (skipButton != null)
			skipButton.Pressed += OnSkipButtonPressed;

		if (robotSprite != null && robotSprite.SpriteFrames != null)
			robotSprite.Play();

		// Initialize the 3D robot buddy visual.
		Setup3DRobotBuddy();

		// Handle gamepad/keyboard prompt updates.
		if (global != null)
		{
			global.Connect("input_device_changed", Callable.From<bool>((isGamepad) => UpdateStoryPrompts()));
		}

		UpdateStoryPrompts();
		StartNextLine();
	}

	/// <summary>
	/// Creates a full-screen 3D viewport to render the FloatingRobotBuddy3D.
	/// This allows a 3D object to exist as a background element behind the 2D UI.
	/// </summary>
	private void Setup3DRobotBuddy()
	{
		var vpContainer = new SubViewportContainer
		{
			Name = "FullScreen3DRobotViewport",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Stretch = true
		};
		vpContainer.SetAnchorsPreset(LayoutPreset.FullRect);

		var viewport = new SubViewport
		{
			TransparentBg = true,
			OwnWorld3D = true
		};

		// Position the camera to frame the robot on the left side of the screen.
		var camera = new Camera3D
		{
			Position = new Vector3(-1.65f, -0.05f, 3.4f),
			Fov = 32f
		};

		// Primary key light.
		var light = new DirectionalLight3D
		{
			Position = new Vector3(2.5f, 4f, 3.5f),
			RotationDegrees = new Vector3(-35f, 30f, 0f),
			LightColor = new Color(0.95f, 0.98f, 1.0f),
			LightEnergy = 1.8f
		};

		// Cyan fill light for cyberpunk atmosphere.
		var fillLight = new DirectionalLight3D
		{
			Position = new Vector3(-2.5f, -1f, 2.5f),
			RotationDegrees = new Vector3(20f, -40f, 0f),
			LightColor = new Color(0.0f, 0.7f, 1.0f),
			LightEnergy = 0.9f
		};

		// Instantiate the floating robot entity.
		var robotBuddy = new FloatingRobotBuddy3D
		{
			Scale = new Vector3(1.15f, 1.15f, 1.15f),
			Position = new Vector3(0f, 0f, -0.8f),
			HoverSpeed = 2.6f,
			HoverHeight = 0.08f,
			PulseSpeed = 3.5f
		};

		viewport.AddChild(camera);
		viewport.AddChild(light);
		viewport.AddChild(fillLight);
		viewport.AddChild(robotBuddy);

		vpContainer.AddChild(viewport);

		// Place the viewport behind the 2D Dialogue UI overlay.
		AddChild(vpContainer);
		MoveChild(vpContainer, 1);
		GD.Print("StoryIntro: Integrated 3D AXIOM-7 Floating Robot Buddy across 100% full-screen 3D space (Zero 2D container box clipping)");
	}

	/// <summary>
	/// Updates the "Continue" and "Skip" button labels based on whether the user is using a gamepad.
	/// </summary>
	private void UpdateStoryPrompts()
	{
		var global = GetNodeOrNull<Node>("/root/Global");
		bool isGamepad = (global != null && (bool)global.Get("is_gamepad_mode"));

		if (skipButton != null)
		{
			skipButton.Text = isGamepad ? "Skip (START) >>>" : "Skip (ESC) >>>";
		}
		if (continueButton != null)
		{
			continueButton.Text = isGamepad ? "Continue (A) >>>" : "Continue (SPACE) >>>";
		}
	}

	public override void _Process(double delta)
	{
		// Manage the typewriter revelation of text if cyberBubble is absent.
		if (cyberBubble == null && isTyping)
		{
			typewriterTimer += (float)delta;
			if (typewriterTimer >= typewriterSpeed)
			{
				typewriterTimer = 0f;
				currentCharIndex++;
				if (currentCharIndex <= currentText.Length)
				{
					if (dialogueLabel != null)
						dialogueLabel.VisibleCharacters = currentCharIndex;
				}
				else
				{
					// Typing finished: reveal the continue button.
					isTyping = false;
					if (continueButton != null)
						continueButton.Visible = true;
				}
			}
		}
	}

	/// <summary>
	/// Initializes the next line of dialogue and triggers the corresponding voice-over audio.
	/// </summary>
	private void StartNextLine()
	{
		if (currentLineIndex < dialogueLines.Count)
		{
			currentText = dialogueLines[currentLineIndex];

			if (cyberBubble != null)
			{
				cyberBubble.SetDialogueLine(currentText, "AXIOM-7 // TRANSMISSION ONLINE");
			}
			else if (dialogueLabel != null)
			{
				dialogueLabel.Text = currentText;
				dialogueLabel.VisibleCharacters = 0;
			}

			currentCharIndex = 0;
			isTyping = true;

			if (continueButton != null)
				continueButton.Visible = false;

			typewriterTimer = 0f;

			// Voice-over audio remains in English across all language versions, while subtitles match the selected language.
			string speaker = "AXIOM-7";
			string audioPath = $"res://assets/speech/{speaker}/english/AXIOM-7-{currentLineIndex + 1}.mp3";

			GD.Print($"StoryIntro: Attempting to load audio: {audioPath}");
			AudioStream? stream = LoadAudioStream(audioPath);
			if (stream != null && voicePlayer != null)
			{
				GD.Print($"StoryIntro: Successfully loaded and playing: {audioPath}");
				voicePlayer.Stream = stream;
				voicePlayer.Play();
			}
			else
			{
				GD.PrintErr($"StoryIntro: Failed to load audio stream at {audioPath}");
			}
		}
		else
		{
			ProceedToLevelSelection();
		}
	}

	/// <summary>
	/// Utility to load an audio stream from a resource path or raw file system.
	/// </summary>
	private AudioStream? LoadAudioStream(string resPath)
	{
		if (ResourceLoader.Exists(resPath))
		{
			var res = GD.Load<AudioStream>(resPath);
			if (res != null) return res;
		}

		string globalPath = ProjectSettings.GlobalizePath(resPath);
		if (System.IO.File.Exists(globalPath))
		{
			byte[] bytes = System.IO.File.ReadAllBytes(globalPath);
			if (bytes.Length > 0)
			{
				var mp3 = new AudioStreamMP3();
				mp3.Data = bytes;
				return mp3;
			}
		}
		return null;
	}

	private void OnContinueButtonPressed()
	{
		if (isTyping)
		{
			// Fast-forward the current line to the end.
			cyberBubble?.CompleteTyping();
			if (dialogueLabel != null && cyberBubble == null)
				dialogueLabel.VisibleCharacters = -1;
			isTyping = false;
			if (continueButton != null)
				continueButton.Visible = true;
		}
		else
		{
			// Move to the next dialogue line.
			currentLineIndex++;
			StartNextLine();
		}
	}

	private void OnSkipButtonPressed()
	{
		ProceedToLevelSelection();
	}

	/// <summary>
	/// Cleans up audio and transitions the user to the training protocol scene.
	/// </summary>
	private void ProceedToLevelSelection()
	{
		if (voicePlayer != null)
			voicePlayer.Stop();

		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null && global.HasMethod("resume_menu_music"))
		{
			global.Call("resume_menu_music");
		}

		LoadingScreen.LoadScene(GetTree(), "res://scenes/gameplay/training_protocol.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		// Global skip trigger (Escape / Cancel).
		if (@event.IsActionPressed("escape") || @event.IsActionPressed("ui_cancel") ||
			(@event is InputEventKey ke && ke.Pressed && !ke.Echo && ke.Keycode == Key.Escape))
		{
			ProceedToLevelSelection();
			GetViewport()?.SetInputAsHandled();
			return;
		}

		// Gamepad skip triggers (Start / Back / Y).
		if (@event is InputEventJoypadButton jb && jb.Pressed)
		{
			if (jb.ButtonIndex == JoyButton.Start || jb.ButtonIndex == JoyButton.Back || jb.ButtonIndex == JoyButton.Y)
			{
				ProceedToLevelSelection();
				GetViewport()?.SetInputAsHandled();
				return;
			}
		}

		bool isTriggered = false;

		// Keyboard "Continue" triggers (Space / Enter).
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter)
				isTriggered = true;
		}
		// Gamepad "Continue" triggers (A / X).
		else if (@event is InputEventJoypadButton joyb && joyb.Pressed)
		{
			if (joyb.ButtonIndex == JoyButton.A || joyb.ButtonIndex == JoyButton.X)
				isTriggered = true;
		}

		if (isTriggered)
		{
			if (continueButton != null && continueButton.Visible)
			{
				OnContinueButtonPressed();
			}
			else if (isTyping)
			{
				// Fast-forward typewriter.
				if (dialogueLabel != null)
					dialogueLabel.VisibleCharacters = -1;
				isTyping = false;
				if (continueButton != null)
					continueButton.Visible = true;
			}
		}
	}
}
