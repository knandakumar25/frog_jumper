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

	public override void _Ready()
	{
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
	{
		if (_rootPanel == null) return;
		_rootPanel.Visible = true;

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

	private void OnReplayPressed()
	{
		GetTree()?.ReloadCurrentScene();
	}

	private void OnLevelSelectPressed()
	{
		GetTree()?.ChangeSceneToFile("res://scenes/main_menu/level_selection.tscn");
	}

	private void OnMainMenuPressed()
	{
		GetTree()?.ChangeSceneToFile("res://scenes/main_menu/main_menu.tscn");
	}
}
