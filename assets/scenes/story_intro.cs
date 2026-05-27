using Godot;
using System.Collections.Generic;

public partial class StoryIntro : Control
{
    private AnimatedSprite2D robotSprite;
    private Panel dialogueBox;
    private RichTextLabel dialogueLabel;
    private Button continueButton;
    private Button skipButton;

    private List<string> dialogueLines = new();
    private int currentLineIndex = 0;
    private float typewriterSpeed = 0.03f;
    private bool isTyping = false;
    private string currentText = "";
    private int currentCharIndex = 0;
    private float typewriterTimer = 0f;

    public override void _Ready()
    {
        robotSprite = GetNode<AnimatedSprite2D>("HBoxContainer/RobotContainer/RobotSprite");
        dialogueBox = GetNode<Panel>("HBoxContainer/DialogueBox");
        dialogueLabel = GetNode<RichTextLabel>("HBoxContainer/DialogueBox/MarginContainer/DialogueLabel");
        continueButton = GetNode<Button>("ContinueButton");
        skipButton = GetNode<Button>("SkipButton");

        // Load dialogue from InkManager
        var inkManager = InkManager.Instance;
        if (inkManager != null)
        {
            var inkText = inkManager.GetInkStory();
            if (!string.IsNullOrEmpty(inkText))
            {
                dialogueLines = new List<string>(inkText.Split("\n\n"));
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

        continueButton.Visible = false;
        dialogueLabel.VisibleCharacters = 0;
        if (robotSprite.SpriteFrames != null)
            robotSprite.Play();
        StartNextLine();
        continueButton.Pressed += OnContinueButtonPressed;
        skipButton.Pressed += OnSkipButtonPressed;
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
                    dialogueLabel.VisibleCharacters = currentCharIndex;
                }
                else
                {
                    isTyping = false;
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
            dialogueLabel.Text = currentText;
            currentCharIndex = 0;
            dialogueLabel.VisibleCharacters = 0;
            isTyping = true;
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
            dialogueLabel.VisibleCharacters = -1;
            isTyping = false;
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
                if (continueButton.Visible)
                    OnContinueButtonPressed();
                else if (isTyping)
                {
                    dialogueLabel.VisibleCharacters = -1;
                    isTyping = false;
                    continueButton.Visible = true;
                }
            }
        }
    }
}

