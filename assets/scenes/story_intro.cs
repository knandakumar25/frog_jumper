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

    public override void _Ready()
    {
        // Safely get nodes (will warn if missing instead of throwing)
        robotSprite = GetNodeOrNull<AnimatedSprite2D>("HBoxContainer/RobotContainer/RobotSprite");
        dialogueBox = GetNodeOrNull<Panel>("HBoxContainer/DialogueBox");
        dialogueLabel = GetNodeOrNull<RichTextLabel>("HBoxContainer/DialogueBox/MarginContainer/DialogueLabel");
        continueButton = GetNodeOrNull<Button>("ContinueButton");
        skipButton = GetNodeOrNull<Button>("SkipButton");

        // Load dialogue from InkManager
        var inkManager = InkManager.Instance;
        if (inkManager != null)
        {
            // If there's an active story, gather its continuations into lines.
            if (inkManager.HasStory())
            {
                var parts = new List<string>();
                while (inkManager.CanContinue())
                {
                    var part = inkManager.Continue();
                    if (!string.IsNullOrEmpty(part))
                        parts.Add(part);
                }

                if (parts.Count > 0)
                    dialogueLines = parts;
                else
                    dialogueLines = new List<string> {"[color=red]No story found.[/color]"};
            }
            else
            {
                dialogueLines = new List<string> {"[color=red]No story found.[/color]"};
            }
        }
        else
        {
            dialogueLines = new List<string> {"[color=red]InkManager not found.[/color]"};
        }

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
        GetTree().ChangeSceneToFile("res://assets/scenes/training_protocol.tscn");
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

