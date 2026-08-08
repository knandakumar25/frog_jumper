using Godot;
using System.Collections.Generic;

/// <summary>
/// Brand-new standalone 3D Story Intro / Pre-Tutorial Mission Briefing screen controller.
/// Features a native 3D scene environment with AXIOM-7 Floating Robot Buddy floating in 3D space,
/// typewriter transmission text, audio dubbing, and 2D UI overlay.
/// </summary>
public partial class ThreeDStoryIntro : Node3D
{
    private Label? _titleBanner;
    private RichTextLabel? _dialogueLabel;
    private Button? _continueButton;
    private Button? _skipButton;
    private Label? _navPromptLabel;

    private List<string> _dialogueLines = new();
    private int _currentLineIndex = 0;
    private float _typewriterSpeed = 0.03f;
    private bool _isTyping = false;
    private string _currentText = "";
    private int _currentCharIndex = 0;
    private float _typewriterTimer = 0f;
    private AudioStreamPlayer? _voicePlayer;

    private FloatingRobotBuddy3D? _robotBuddy;

    public override void _Ready()
    {
        DisplayServer.ScreenSetKeepOn(true);
        GD.Print("ThreeDStoryIntro: DisplayServer.ScreenSetKeepOn enabled");

        // Stop menu music to make transmission voice recordings clear
        var global = GetNodeOrNull<Node>("/root/Global");
        if (global != null)
        {
            global.Call("stop_menu_music");
            GD.Print("ThreeDStoryIntro: Stopped menu music");
        }

        // Setup audio player for AXIOM-7 transmission clips
        _voicePlayer = new AudioStreamPlayer();
        AddChild(_voicePlayer);

        // Resolve 2D UI overlay node references
        _titleBanner    = GetNodeOrNull<Label>("CanvasLayer/UIOverlay/TitleBanner");
        _dialogueLabel  = GetNodeOrNull<RichTextLabel>("CanvasLayer/UIOverlay/DialogueBox/MarginContainer/DialogueLabel");
        _continueButton = GetNodeOrNull<Button>("CanvasLayer/UIOverlay/ContinueButton");
        _skipButton     = GetNodeOrNull<Button>("CanvasLayer/UIOverlay/SkipButton");

        // Wire 3D Floating Robot Buddy
        Setup3DRobotBuddy();

        // Setup dialogue lines using translation keys (Condensed 4-slide tactical transmission)
        _dialogueLines = new List<string>
        {
            $"[center][color=#00FF00]{TranslationServer.Translate("STORY_LINE_1")}[/color][/center]\n\n{TranslationServer.Translate("STORY_LINE_1_INTRO")}",
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

        // Wire gamepad/keyboard prompt footer
        SetupNavPromptFooter();

        if (global != null)
        {
            global.Connect("input_device_changed", Callable.From<bool>((isGamepad) => UpdateStoryPrompts()));
        }

        UpdateStoryPrompts();
        StartNextLine();
    }

    private void Setup3DRobotBuddy()
    {
        _robotBuddy = new FloatingRobotBuddy3D
        {
            Scale = new Vector3(0.72f, 0.72f, 0.72f),
            Position = new Vector3(-0.75f, -0.1f, 0f),
            RotationDegrees = new Vector3(0f, 15f, 0f),
            HoverSpeed = 2.4f,
            HoverHeight = 0.08f,
            PulseSpeed = 3.5f
        };
        AddChild(_robotBuddy);
        GD.Print("ThreeDStoryIntro: Successfully instantiated native 3D AXIOM-7 Robot Buddy in world space");
    }

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
        
        if (_dialogueLabel != null)
        {
            _dialogueLabel.BbcodeEnabled = true;
            _dialogueLabel.Text = _currentText;
            _dialogueLabel.VisibleCharacters = 0;
        }

        _currentCharIndex = 0;
        _typewriterTimer = 0f;
        _isTyping = true;

        PlayLineAudio(_currentLineIndex + 1);
    }

    private void PlayLineAudio(int lineNum)
    {
        string locale = TranslationServer.GetLocale().ToLower();
        string baseLocale = locale.Contains("_") ? locale.Split('_')[0] : locale;

        string audioPath = $"res://assets/speech/AXIOM-7/{baseLocale}/AXIOM-7-{lineNum}.mp3";

        if (!FileAccess.FileExists(audioPath))
        {
            audioPath = $"res://assets/speech/AXIOM-7/english/AXIOM-7-{lineNum}.mp3";
        }

        if (FileAccess.FileExists(audioPath))
        {
            var stream = GD.Load<AudioStream>(audioPath);
            if (stream != null && _voicePlayer != null)
            {
                _voicePlayer.Stream = stream;
                _voicePlayer.Play();
            }
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_isTyping)
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
    }

    private void OnLineFinished()
    {
        _isTyping = false;
        if (_dialogueLabel != null)
            _dialogueLabel.VisibleCharacters = -1; // Show full line

        if (_continueButton != null)
        {
            _continueButton.Visible = true;
            _continueButton.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    public override void _Input(InputEvent @event)
    {
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
        else if (@event.IsActionPressed("ui_cancel"))
        {
            OnSkipButtonPressed();
            GetViewport()?.SetInputAsHandled();
        }
    }

    private void OnContinueButtonPressed()
    {
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
        UISoundManager.Instance?.PlayClickSound();
        if (_voicePlayer != null && _voicePlayer.Playing)
            _voicePlayer.Stop();

        FinishIntro();
    }

    private void FinishIntro()
    {
        DisplayServer.ScreenSetKeepOn(false);
        LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/level_selection.tscn");
    }

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
