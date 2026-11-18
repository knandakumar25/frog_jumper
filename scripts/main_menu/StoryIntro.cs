using Godot;
using System.Collections.Generic;

/// <summary>
/// StoryIntro manages the cinematic "Mission Briefing" sequence. It coordinates
/// a series of localized dialogue lines delivered via a typewriter effect,
/// synchronized with voice-over audio and a 2D robot illustration.
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

		// Initialize the dedicated voice-over audio player on the limited Voice bus (which sends
		// into SFX at 0 dB, so the base level is unchanged); each line's gain is set on play.
		voicePlayer = new AudioStreamPlayer { Bus = VoiceGain.Bus };
		AddChild(voicePlayer);

		// Initialize the dialogue sequence using localized translation keys.
		// The first line includes BBCode for centering and coloring the header.
		RefreshWrittenLines();

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


		InputPromptService.Instance?.SetContext("cinematic", "story_advance", "story_skip");
		InputPromptBinding.Track(this, UpdateStoryPrompts);
		UpdateStoryPrompts();
		StartNextLine();
	}

    /// <summary>Rebuilds only the translated written lines; stable English recording indices remain unchanged.</summary>
    private void RefreshWrittenLines()
    {
		dialogueLines = new List<string>
		{
			$"[center][color=#00FF00]{TranslationServer.Translate("STORY_LINE_1")}[/color][/center]\n\n{TranslationServer.Translate("STORY_LINE_1_INTRO")}",
			TranslationServer.Translate("STORY_LINE_2"),
			TranslationServer.Translate("STORY_LINE_3"),
			TranslationServer.Translate("STORY_LINE_4")
		};
		// The voice gain table covers exactly SpeechPaths.BriefingLineCount recordings.
		if (dialogueLines.Count != SpeechPaths.BriefingLineCount)
			GD.PushWarning($"StoryIntro: {dialogueLines.Count} lines but SpeechPaths.BriefingLineCount is {SpeechPaths.BriefingLineCount}; update it and re-run tools/assets/voice_gain.py");
    }

    /// <summary>Updates an active subtitle at its reveal fraction without calling StartNextLine or replaying speech.</summary>
    public override void _Notification(int what)
    {
        if (what != NotificationTranslationChanged || dialogueLines.Count == 0) return;
        double fraction = currentText.Length > 0 ? (double)currentCharIndex / currentText.Length : 0;
        RefreshWrittenLines();
        if (currentLineIndex >= dialogueLines.Count) return;
        currentText = dialogueLines[currentLineIndex];
        currentCharIndex = System.Math.Clamp((int)(currentText.Length * fraction), 0, currentText.Length);
        if (cyberBubble != null) cyberBubble.RelocalizeLine(currentText);
        else if (dialogueLabel != null)
        {
            dialogueLabel.Text = currentText;
            dialogueLabel.VisibleCharacters = isTyping ? currentCharIndex : -1;
        }
        UpdateStoryPrompts();
    }

	/// <summary>
	/// Updates the "Continue" and "Skip" button labels based on whether the user is using a gamepad.
	/// </summary>
	private void UpdateStoryPrompts()
	{
		if (skipButton != null)
		{
			skipButton.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			skipButton.GrowHorizontal = Control.GrowDirection.Begin;
			skipButton.OffsetLeft = -440;
			skipButton.OffsetRight = -24;
			skipButton.OffsetBottom = skipButton.OffsetTop + 90;
			InputGlyphText.SetButton(skipButton, InputPromptBinding.Marked("story_skip", "Skip") + " >>>");
		}
		if (continueButton != null)
		{
			InputGlyphText.SetButton(continueButton, InputPromptBinding.Marked("story_advance", "Continue") + " >>>");
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
			string audioPath = SpeechPaths.Briefing(currentLineIndex + 1);

			GD.Print($"StoryIntro: Attempting to load audio: {audioPath}");
			AudioStream? stream = LoadAudioStream(audioPath);
			if (stream != null && voicePlayer != null)
			{
				GD.Print($"StoryIntro: Successfully loaded and playing: {audioPath}");
				voicePlayer.Stream = stream;
				// Level-match this briefing line with the others (0 dB if it is not in the table).
				voicePlayer.VolumeDb = VoiceGain.GainDb(audioPath);
				voicePlayer.Play();
			}
			else
			{
				GD.PrintErr($"StoryIntro: Failed to load audio stream at {audioPath}");
			}
		}
		else
		{
			ProceedToMission();
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
		ProceedToMission();
	}

	/// <summary>
	/// Cleans up audio and transitions the player straight into mission 1 (there is no separate training level;
	/// each operative's abilities are taught in the mission where they join).
	/// </summary>
	private void ProceedToMission()
	{
		if (voicePlayer != null)
			voicePlayer.Stop();

		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null && global.HasMethod("resume_menu_music"))
		{
			global.Call("resume_menu_music");
		}

		LoadingScreen.LoadScene(GetTree(), LevelCatalog.ScenePath(1));
	}

	public override void _Input(InputEvent @event)
	{
		InputPromptService.Instance?.ObserveInput(@event);
		// Global skip trigger (Escape / Cancel).
		if (@event.IsActionPressed("escape") || @event.IsActionPressed("ui_cancel") ||
			(@event is InputEventKey ke && ke.Pressed && !ke.Echo && ke.Keycode == Key.Escape))
		{
			ProceedToMission();
			GetViewport()?.SetInputAsHandled();
			return;
		}

		// Gamepad skip triggers (Start / Back / Y).
		if (@event is InputEventJoypadButton jb && jb.Pressed)
		{
			if (jb.ButtonIndex == JoyButton.Start || jb.ButtonIndex == JoyButton.Back || jb.ButtonIndex == JoyButton.Y)
			{
				ProceedToMission();
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
