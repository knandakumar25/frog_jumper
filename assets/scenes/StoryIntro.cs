using Godot;
using System.Collections.Generic;

public partial class StoryIntro : Control
{
    private AnimatedSprite2D? robotSprite;
    private Panel? dialogueBox;
    private RichTextLabel? dialogueLabel;
    private Button? continueButton;
    private Button? skipButton;

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
        // Ensure device screen does not dim or sleep during transmission briefing
        DisplayServer.ScreenSetKeepOn(true);
        GD.Print("StoryIntro: DisplayServer.ScreenSetKeepOn enabled");

        // Safely get nodes (will warn if missing instead of throwing)
        robotSprite = GetNodeOrNull<AnimatedSprite2D>("HBoxContainer/RobotContainer/RobotSprite");
        dialogueBox = GetNodeOrNull<Panel>("HBoxContainer/DialogueBox");
        dialogueLabel = GetNodeOrNull<RichTextLabel>("HBoxContainer/DialogueBox/MarginContainer/DialogueLabel");
        continueButton = GetNodeOrNull<Button>("ContinueButton");
        skipButton = GetNodeOrNull<Button>("SkipButton");

        // Stop home screen music to make voice recordings audible
        var global = GetNodeOrNull<Node>("/root/Global");
        if (global != null)
        {
            global.Call("stop_menu_music");
            GD.Print("StoryIntro: Stopped menu music via Global");
        }

        // Setup audio player
        voicePlayer = new AudioStreamPlayer();
        AddChild(voicePlayer);

        // Setup dialogue lines using translation keys
        dialogueLines = new List<string>
        {
            $"[center][color=#00FF00]{TranslationServer.Translate("STORY_LINE_1")}[/color][/center]\n\n{TranslationServer.Translate("STORY_LINE_1_INTRO")}",
            TranslationServer.Translate("STORY_LINE_2"),
            TranslationServer.Translate("STORY_LINE_3"),
            TranslationServer.Translate("STORY_LINE_4"),
            TranslationServer.Translate("STORY_LINE_5"),
            TranslationServer.Translate("STORY_LINE_6"),
            TranslationServer.Translate("STORY_LINE_7"),
            TranslationServer.Translate("STORY_LINE_8"),
            TranslationServer.Translate("STORY_LINE_9")
        };

        // Translate title banner
        var titleBanner = GetNodeOrNull<Label>("TitleBanner");
        if (titleBanner != null)
            titleBanner.Text = TranslationServer.Translate("MISSION BRIEFING TRANSMISSION");

        // Initialize UI state defensively
        if (continueButton != null)
            continueButton.Visible = false;
        else
            GD.PrintErr("StoryIntro: ContinueButton node not found");

        if (dialogueLabel != null)
            dialogueLabel.VisibleCharacters = 0;
        else
            GD.PrintErr("StoryIntro: DialogueLabel node not found");

        if (robotSprite != null && robotSprite.SpriteFrames != null)
            robotSprite.Play();

        // Connect signals before starting the typewriter
        if (continueButton != null)
            continueButton.Pressed += OnContinueButtonPressed;
        if (skipButton != null)
            skipButton.Pressed += OnSkipButtonPressed;

        StartNextLine();
    }

    public override void _Process(double delta)
    {
        if (isTyping)
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
                    isTyping = false;
                    if (continueButton != null)
                        continueButton.Visible = true;
                }
            }
        }
    }

    private void StartNextLine()
    {
        if (currentLineIndex < dialogueLines.Count)
        {
            currentText = dialogueLines[currentLineIndex];
            if (dialogueLabel != null)
                dialogueLabel.Text = currentText;

            currentCharIndex = 0;

            if (dialogueLabel != null)
                dialogueLabel.VisibleCharacters = 0;

            isTyping = true;

            if (continueButton != null)
                continueButton.Visible = false;

            typewriterTimer = 0f;

            // Play corresponding voice recording from language subfolder
            string currentLang = TranslationServer.GetLocale().ToLower();
            string langFolder = "english";

            if (currentLang == "ja" || currentLang == "japanese") langFolder = "japanese";
            else if (currentLang == "es" || currentLang == "spanish") langFolder = "spanish";
            else if (currentLang == "fr" || currentLang == "french" || currentLang == "eurofrench") langFolder = "french";
            else if (currentLang == "de" || currentLang == "german") langFolder = "german";
            else if (currentLang == "ko" || currentLang == "korean") langFolder = "korean";
            else if (currentLang == "zh" || currentLang == "mandarin") langFolder = "mandarin";
            else if (currentLang == "ar" || currentLang == "arabic") langFolder = "arabic";
            else if (currentLang == "he" || currentLang == "hebrew") langFolder = "hebrew";
            else if (currentLang == "it" || currentLang == "italian") langFolder = "italian";
            else if (currentLang == "pl" || currentLang == "polish") langFolder = "polish";
            else if (currentLang == "pt" || currentLang == "portuguese" || currentLang == "brazilianportuguese") langFolder = "portuguese";
            else if (currentLang == "tr" || currentLang == "turkish") langFolder = "turkish";

            string speaker = "AXIOM-7";
            string primaryPath = $"res://assets/speech/{speaker}/{langFolder}/AXIOM-7-{currentLineIndex + 1}.mp3";
            string fallbackPath = $"res://assets/speech/{speaker}/english/AXIOM-7-{currentLineIndex + 1}.mp3";
            string audioPath = System.IO.File.Exists(ProjectSettings.GlobalizePath(primaryPath)) ? primaryPath : fallbackPath;

            GD.Print($"StoryIntro: Attempting to load audio: {audioPath}");
            AudioStream stream = LoadAudioStream(audioPath);
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
            if (dialogueLabel != null)
                dialogueLabel.VisibleCharacters = -1;
            isTyping = false;
            if (continueButton != null)
                continueButton.Visible = true;
        }
        else
        {
            currentLineIndex++;
            StartNextLine();
        }
    }

    private void OnSkipButtonPressed()
    {
        ProceedToLevelSelection();
    }

    private void ProceedToLevelSelection()
    {
        // Stop all voice audio immediately
        if (voicePlayer != null)
            voicePlayer.Stop();

        var global = GetNodeOrNull<Node>("/root/Global");
        if (global != null)
        {
            global.Call("resume_menu_music_allowed");
        }

        LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/training_protocol.tscn");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter)
            {
                if (continueButton != null && continueButton.Visible)
                {
                    OnContinueButtonPressed();
                }
                else if (isTyping)
                {
                    if (dialogueLabel != null)
                        dialogueLabel.VisibleCharacters = -1;
                    isTyping = false;
                    if (continueButton != null)
                        continueButton.Visible = true;
                }
            }
        }
    }
}
