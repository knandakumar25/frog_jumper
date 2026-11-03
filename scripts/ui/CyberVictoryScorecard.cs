using Godot;

/// <summary>
/// CyberVictoryScorecard provides an interactive post-mission tactical evaluation terminal.
/// It renders combat grade calculations (S / A / B / C rank), point summaries,
/// combo statistics, audio jingles, and navigation actions.
/// </summary>
public partial class CyberVictoryScorecard : CanvasLayer
{
	// ─── Node References ─────────────────────────────────────────────────────
	private Control? _rootPanel;
	private Label? _titleLabel;
	private Label? _rankLabel;
	private Label? _scoreLabel;
	private Label? _comboLabel;
	private Label? _operativeLabel;
	private Button? _replayBtn;
	private Button? _levelSelectBtn;
	private Button? _mainMenuBtn;
	private AudioStreamPlayer? _jinglePlayer;
	private Button? _continueBtn;
	private Button? _previewBtn;
	private int _nextMission;
	private bool _navigating;
	private Input.MouseModeEnum _previousMouseMode;

	/// <summary>Resolves the terminal controls; Continue is optional for existing scene callers.</summary>
	public override void _Ready()
	{
		_previousMouseMode = Input.MouseMode;
		_rootPanel = GetNodeOrNull<Control>("RootPanel");
		_titleLabel = GetNodeOrNull<Label>("RootPanel/MainVBox/TitleLabel");
		_rankLabel = GetNodeOrNull<Label>("RootPanel/MainVBox/RankPanel/RankLabel");
		_scoreLabel = GetNodeOrNull<Label>("RootPanel/MainVBox/StatsGrid/ScoreLabel");
		_comboLabel = GetNodeOrNull<Label>("RootPanel/MainVBox/StatsGrid/ComboLabel");
		_operativeLabel = GetNodeOrNull<Label>("RootPanel/MainVBox/StatsGrid/OperativeLabel");
		_replayBtn = GetNodeOrNull<Button>("RootPanel/MainVBox/ButtonsHBox/ReplayBtn");
		_levelSelectBtn = GetNodeOrNull<Button>("RootPanel/MainVBox/ButtonsHBox/LevelSelectBtn");
		_mainMenuBtn = GetNodeOrNull<Button>("RootPanel/MainVBox/ButtonsHBox/MainMenuBtn");
		_jinglePlayer = GetNodeOrNull<AudioStreamPlayer>("JinglePlayer");
		_continueBtn = GetNodeOrNull<Button>("RootPanel/MainVBox/ContinueBtn");
		_previewBtn = GetNodeOrNull<Button>("RootPanel/MainVBox/PreviewBtn");
		if (_previewBtn != null) _previewBtn.Pressed += OnPreviewPressed;
		if (_continueBtn != null) _continueBtn.Pressed += OnContinuePressed;

		if (_replayBtn != null) _replayBtn.Pressed += OnReplayPressed;
		if (_levelSelectBtn != null) _levelSelectBtn.Pressed += OnLevelSelectPressed;
		if (_mainMenuBtn != null) _mainMenuBtn.Pressed += OnMainMenuPressed;

		if (_rootPanel != null)
		{
			_rootPanel.Visible = false;
		}
	}

	/// <summary>
	/// Displays the victory scorecard, calculates rank, and triggers victory fanfare.
	/// </summary>
	public void ShowScorecard(int score, int maxCombo, string operative = "KAITO KUROGANE // CYBER SHOGUN")
		=> RenderScorecard(score, maxCombo, operative, 0);

	/// <summary>Progression entry point with an explicit next target; preserves the original three-argument Godot binding.</summary>
	public void ShowMissionScorecard(int score, int maxCombo, string operative, int nextMission)
		=> RenderScorecard(score, maxCombo, operative, nextMission);

    /// <summary>Shows Act I results with the Act II opening, Replay, Level Select and Main Menu.</summary>
    public void ShowActScorecard(int score, int maxCombo, string operative)
    {
        RenderScorecard(score, maxCombo, operative, 0);
        if (_titleLabel != null) _titleLabel.Text = "ACT I COMPLETE";
        if (_previewBtn != null)
        {
            string text = Tr("CONTINUE_TO_ACT_II");
            _previewBtn.Text = text == "CONTINUE_TO_ACT_II" || string.IsNullOrWhiteSpace(text) ? "CONTINUE TO ACT II" : text;
            _previewBtn.Visible = true;
        }
    }

	/// <summary>Renders the captured results, optionally showing Continue; grading is shared by all callers.</summary>
	private void RenderScorecard(int score, int maxCombo, string operative, int nextMission)
	{
		if (_rootPanel == null) return;
		if (_titleLabel != null) _titleLabel.Text = "MISSION COMPLETE";
		if (_previewBtn != null) _previewBtn.Visible = false;
		_nextMission = nextMission > 0 && nextMission <= LevelCatalog.Count ? nextMission : 0;
		if (_continueBtn != null) _continueBtn.Visible = _nextMission > 0;
		if (_continueBtn != null && _nextMission > 0) _continueBtn.Text = $"CONTINUE TO LEVEL {_nextMission}";
		_rootPanel.Visible = true;
		if (IsInsideTree()) Input.MouseMode = Input.MouseModeEnum.Visible;
		if (IsInsideTree()) GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);
		if (IsInsideTree()) (_nextMission > 0 ? _continueBtn : _replayBtn)?.GrabFocus();

		// Calculate Combat Grade
		string rankText;
		Color rankColor;

		if (score >= 120 && maxCombo >= 3)
		{
			rankText = "S RANK // FLAWLESS";
			rankColor = new Color(1.0f, 0.85f, 0.1f, 1f); // Gold
		}
		else if (score >= 50 || maxCombo >= 2)
		{
			rankText = "A RANK // VANGUARD";
			rankColor = new Color(0f, 0.94f, 1f, 1f); // Cyan
		}
		else
		{
			rankText = "B RANK // OPERATIVE";
			rankColor = new Color(0.9f, 0.4f, 0.9f, 1f); // Violet
		}

		if (_rankLabel != null)
		{
			_rankLabel.Text = rankText;
			_rankLabel.Modulate = rankColor;
		}

		if (_scoreLabel != null)
		{
			_scoreLabel.Text = $"FINAL SCORE: {score} PTS";
		}

		if (_comboLabel != null)
		{
			_comboLabel.Text = $"PEAK COMBO: {maxCombo}x STRIKE";
		}

		if (_operativeLabel != null)
		{
			_operativeLabel.Text = $"OPERATIVE: {operative}";
		}

		// Play fanfare jingle
		if (_jinglePlayer != null && IsInsideTree())
		{
			_jinglePlayer.Play();
		}

		// Reveal animation
		if (IsInsideTree())
		{
			_rootPanel.Modulate = new Color(1f, 1f, 1f, 0f);
			_rootPanel.Scale = new Vector2(0.95f, 0.95f);
			var tween = CreateTween();
			tween?.TweenProperty(_rootPanel, "modulate:a", 1.0f, 0.25f);
			tween?.Parallel().TweenProperty(_rootPanel, "scale", Vector2.One, 0.25f);
		}
		else
		{
			_rootPanel.Modulate = new Color(1f, 1f, 1f, 1f);
			_rootPanel.Scale = Vector2.One;
		}
	}

	/// <summary>Prepares the story-earned squad before entering the supplied next mission.</summary>
	private void OnContinuePressed()
	{
		if (_nextMission <= 0 || !IsInsideTree() || _navigating) return;
		CampaignProgress.PrepareMission(_nextMission);
		if (_nextMission == 6 && ResourceLoader.Exists("res://scenes/cutscenes/cyber_grave_intro.tscn"))
		{
			Navigate("res://scenes/cutscenes/cyber_grave_intro.tscn");
		}
		else
		{
			Navigate(LevelCatalog.ScenePath(_nextMission));
		}
	}

	/// <summary>Reloads the still-current mission, preserving Replay after an overlay cinematic.</summary>
	private void OnReplayPressed()
	{
		if (!IsInsideTree() || _navigating) return;
		_navigating = true;
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	/// <summary>Returns to the existing mission selector.</summary>
	private void OnLevelSelectPressed()
	{
		Navigate("res://scenes/main_menu/level_selection.tscn");
	}

	/// <summary>Returns to the existing main menu.</summary>
	private void OnMainMenuPressed()
	{
		Navigate("res://scenes/main_menu/main_menu.tscn");
	}

	/// <summary>Launches the opening after captured Act I results; the opening hands off to Mission 5.</summary>
	private void OnPreviewPressed()
	{
		if (_previewBtn is not { Visible: true }) return;
		Navigate("res://scenes/cutscenes/act_ii_opening.tscn");
	}

	/// <summary>Consumes repeated navigation input and releases any legacy pause before changing scene.</summary>
	private void Navigate(string path)
	{
		if (!IsInsideTree() || _navigating) return;
		_navigating = true;
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(path);
	}

	/// <summary>Handles pointer and direct touch before Global's legacy mouse suppression consumes them.</summary>
	public override void _Input(InputEvent input)
	{
		if (_rootPanel is not { Visible: true } || !IsInsideTree() || _navigating) return;
		Vector2 point;
		if (input is InputEventScreenTouch { Pressed: true } touch) point = touch.Position;
		else if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse) point = mouse.Position;
		else return;
		foreach (var button in new[] { _continueBtn, _previewBtn, _replayBtn, _levelSelectBtn, _mainMenuBtn })
		{
			if (button is not { Visible: true } || !button.GetGlobalRect().HasPoint(point)) continue;
			GetViewport().SetInputAsHandled();
			button.EmitSignal(Button.SignalName.Pressed);
			return;
		}
	}

	/// <summary>Restores the gameplay cursor policy when leaving the results overlay's owning mission.</summary>
	public override void _ExitTree()
	{
		Input.MouseMode = _previousMouseMode;
		GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", true);
	}
}
