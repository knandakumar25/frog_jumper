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

        // Setup dialogue lines using translation keys (Condensed 4-slide tactical transmission)
        dialogueLines = new List<string>
        {
            $"[center][color=#00FF00]{TranslationServer.Translate("STORY_LINE_1")}[/color][/center]\n\n{TranslationServer.Translate("STORY_LINE_1_INTRO")}",
            TranslationServer.Translate("STORY_LINE_2"),
            TranslationServer.Translate("STORY_LINE_3"),
            TranslationServer.Translate("STORY_LINE_4")
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

        if (continueButton != null)
            continueButton.Pressed += OnContinueButtonPressed;
        if (skipButton != null)
            skipButton.Pressed += OnSkipButtonPressed;

        if (robotSprite != null && robotSprite.SpriteFrames != null)
            robotSprite.Play();

        Setup3DRobotBuddy();

        // Connect signals before starting the typewriter
        if (global != null)
        {
            global.Connect("input_device_changed", Callable.From<bool>((isGamepad) => UpdateStoryPrompts()));
        }

        UpdateStoryPrompts();
        StartNextLine();
    }

    private void Setup3DRobotBuddy()
    {
        var robotContainer = GetNodeOrNull<Control>("HBoxContainer/RobotContainer");
        if (robotContainer == null) return;

        // Hide 2D sprite if present
        if (robotSprite != null) robotSprite.Visible = false;

        // Create SubViewportContainer & SubViewport for 3D Robot Buddy preview
        var vpContainer = new SubViewportContainer
        {
            CustomMinimumSize = new Vector2(320, 380),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Stretch = true
        };

        var viewport = new SubViewport
        {
            Size = new Vector2I(320, 380),
            TransparentBg = true,
            OwnWorld3D = true
        };

        var camera = new Camera3D
        {
            Position = new Vector3(0f, 0.1f, 1.85f),
            Fov = 42f
        };

        var light = new DirectionalLight3D
        {
            Position = new Vector3(2f, 4f, 3f),
            RotationDegrees = new Vector3(-35f, 30f, 0f),
            LightColor = new Color(0.9f, 0.95f, 1.0f),
            LightEnergy = 1.6f
        };

        var fillLight = new DirectionalLight3D
        {
            Position = new Vector3(-2f, -1f, 2f),
            RotationDegrees = new Vector3(20f, -40f, 0f),
            LightColor = new Color(0.0f, 0.6f, 1.0f),
            LightEnergy = 0.8f
        };

        var robotBuddy = new FloatingRobotBuddy3D
        {
            Scale = new Vector3(1.1f, 1.1f, 1.1f),
            Position = new Vector3(0f, -0.05f, 0f),
            HoverSpeed = 2.8f,
            HoverHeight = 0.08f,
            PulseSpeed = 3.5f
        };

        viewport.AddChild(camera);
        viewport.AddChild(light);
        viewport.AddChild(fillLight);
        viewport.AddChild(robotBuddy);

        vpContainer.AddChild(viewport);
        robotContainer.AddChild(vpContainer);
        GD.Print("StoryIntro: Integrated 3D AXIOM-7 Floating Robot Buddy with emissive visor pulsing");
    }


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
            else if (currentLang == "traditionalchinese" || currentLang == "zh_tw" || currentLang == "zh_hk") langFolder = "traditionalchinese";
            else if (currentLang == "hi" || currentLang == "hindi") langFolder = "hindi";
            else if (currentLang == "ta" || currentLang == "tamil") langFolder = "tamil";
            else if (currentLang == "te" || currentLang == "telugu") langFolder = "telugu";
            else if (currentLang == "ml" || currentLang == "malayalam") langFolder = "malayalam";
            else if (currentLang == "kn" || currentLang == "kannada") langFolder = "kannada";
            else if (currentLang == "id" || currentLang == "indonesian") langFolder = "indonesian";
            else if (currentLang == "vi" || currentLang == "vietnamese") langFolder = "vietnamese";
            else if (currentLang == "th" || currentLang == "thai") langFolder = "thai";
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
        if (global != null && global.HasMethod("resume_menu_music"))
        {
            global.Call("resume_menu_music");
        }

        LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/training_protocol.tscn");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("escape") || @event.IsActionPressed("ui_cancel") ||
            (@event is InputEventKey ke && ke.Pressed && !ke.Echo && ke.Keycode == Key.Escape))
        {
            ProceedToLevelSelection();
            GetViewport()?.SetInputAsHandled();
            return;
        }

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

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter)
                isTriggered = true;
        }
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
                if (dialogueLabel != null)
                    dialogueLabel.VisibleCharacters = -1;
                isTyping = false;
                if (continueButton != null)
                    continueButton.Visible = true;
            }
        }
    }
}
