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

            // Play corresponding voice recording
            string audioPath = $"res://assets/speech/AXIOM-7/AXIOM-7-{currentLineIndex + 1} (ENG).mp3";
            GD.Print($"StoryIntro: Attempting to load audio: {audioPath}");
            AudioStream stream = GD.Load<AudioStream>(audioPath);
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
