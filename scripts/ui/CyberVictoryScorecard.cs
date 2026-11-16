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
	private bool _saveConfigured, _savePending;
	private CompletionSaveResult? _saveResult;
	private int _saveMission, _saveScore, _saveSession;
	private string _runToken = "";
	private Label? _saveLabel;
	private Button? _retrySaveBtn;
	private Input.MouseModeEnum _previousMouseMode;
	private bool _previousPromptVisible;

	/// <summary>Resolves terminal controls; detached scene tests skip the live global prompt lookup.</summary>
	public override void _Ready()
	{
		_previousMouseMode = Input.MouseMode;
		var prompt = IsInsideTree()
			? GetNodeOrNull<Node>("/root/Global")?.Get("_prompt_label").AsGodotObject() as CanvasItem : null;
		_previousPromptVisible = prompt?.IsVisibleInTree() ?? true;
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
		var box = GetNodeOrNull<VBoxContainer>("RootPanel/MainVBox");
		if (box != null)
		{
			_saveLabel = new Label { Name = "SaveStatus", HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			box.AddChild(_saveLabel); box.MoveChild(_saveLabel, 2);
			_retrySaveBtn = new Button { Name = "RetrySave", Text = "RETRY SAVE", Visible = false };
			_retrySaveBtn.Pressed += PersistCompletion;
			box.AddChild(_retrySaveBtn); box.MoveChild(_retrySaveBtn, 3);
		}
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

    	/// <summary>Shows Act II finale results upon completing Mission 7 (The Foundry Forge).</summary>
    public void ShowActIIScorecard(int score, int maxCombo, string operative)
    {
        RenderScorecard(score, maxCombo, operative, 0);
        if (_titleLabel != null) _titleLabel.Text = "ACT II COMPLETE";
    }

    /// <summary>Finale results return to exploration after the existing aftermath and retain replay.</summary>
    public void ShowCampaignCompleteScorecard(int score, int maxCombo, string operative)
    {
        RenderScorecard(score, maxCombo, operative, 0);
        if (_titleLabel != null) _titleLabel.Text = "CAMPAIGN COMPLETE";
    }

    /// <summary>Act I results return to extraction; the Act II opening belongs to Mission 5's entrance.</summary>
    public void ShowActScorecard(int score, int maxCombo, string operative)
    {
        RenderScorecard(score, maxCombo, operative, 0);
        if (_titleLabel != null) _titleLabel.Text = "ACT I COMPLETE";
    }

	/// <summary>Renders the captured results, optionally showing Continue; grading is shared by all callers.</summary>
	private void RenderScorecard(int score, int maxCombo, string operative, int nextMission)
	{
		if (_rootPanel == null) return;
		if (_titleLabel != null) _titleLabel.Text = "MISSION COMPLETE";
		if (_previewBtn != null) _previewBtn.Visible = false;
		_nextMission = nextMission > 0 && nextMission <= LevelCatalog.Count ? nextMission : 0;
		// Every result, including act/finale results, returns to the committed nearby extraction.
		if (_continueBtn != null) { _continueBtn.Visible = true; _continueBtn.Text = WorldText.Get("Return to Neo-Los Angeles"); }
		RefreshSaveStatus();
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
			WildfireText.SetText(_scoreLabel, "Text", "UI_CYBERVICTORYSCORECARD_RENDERSCORECARD_EF1F4E5A", $"FINAL SCORE: {score} PTS");
		}

		if (_comboLabel != null)
		{
			WildfireText.SetText(_comboLabel, "Text", "UI_CYBERVICTORYSCORECARD_RENDERSCORECARD_D0D59A16", $"PEAK COMBO: {maxCombo}x STRIKE");
		}

		if (_operativeLabel != null)
		{
            WildfireText.SetText(_operativeLabel, "Text", "UI_CYBERVICTORYSCORECARD_RENDERSCORECARD_DF6F8A63", $"OPERATIVE: {WildfireText.DisplayValue(operative, englishUppercase: true)}");
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

    /// <summary>Returns to the extraction committed alongside settlement; missions launch at world terminals.</summary>
    private void OnContinuePressed()
    {
        if (!IsInsideTree() || _navigating || !CanLeaveResults) return;
        Navigate(WorldCatalog.Scene);
    }

	/// <summary>Reloads the still-current mission, preserving Replay after an overlay cinematic.</summary>
    private async void OnReplayPressed()
    {
        if (!IsInsideTree() || _navigating || !CanLeaveResults) return;
        _navigating = true;
        try
        {
            if (_saveSession > 0)
            {
                var state = await WorldStorage.LoadAsync(_saveSession, LevelCatalog.Clamp(_saveMission + 1));
                if (state != null) await WorldStorage.SaveAsync(_saveSession, state with { Resume = "mission", Mission = _saveMission });
            }
            if (!IsInsideTree()) return;
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        }
        catch (System.Exception) { _navigating = false; WorldNavigation.ShowSaveError(this); }
    }

	/// <summary>Returns to the existing mission selector.</summary>
	private void OnLevelSelectPressed()
	{
		if (!CanLeaveResults) return;
		Navigate("res://scenes/main_menu/level_selection.tscn");
	}

	/// <summary>Returns to the existing main menu.</summary>
	private void OnMainMenuPressed()
	{
		Navigate("res://scenes/main_menu/main_menu.tscn");
	}

    /// <summary>Compatibility action for older result scenes uses the same saved extraction route.</summary>
    private void OnPreviewPressed() => OnContinuePressed();

	/// <summary>Consumes repeated navigation input and releases any legacy pause before changing scene.</summary>
	private void Navigate(string path)
	{
		if (!IsInsideTree() || _navigating) return;
		_navigating = true;
		Callable.From(() => { if (!IsInsideTree()) return; GetTree().Paused = false; GetTree().ChangeSceneToFile(path); }).CallDeferred();
	}

	/// <summary>Captures one immutable extraction snapshot and starts its awaited save while the aftermath plays.</summary>
	public void ConfigureCompletion(int mission, int score, string runToken, int sessionId)
	{
		if (_saveConfigured) return;
		_saveConfigured = true;
		_saveMission = mission; _saveScore = score; _runToken = runToken; _saveSession = sessionId;
		PersistCompletion();
	}

	/// <summary>Forward navigation is permitted only after confirmed settlement or direct play with no active campaign.</summary>
	private bool CanLeaveResults => !_saveConfigured || (!_savePending && _saveResult?.Status != CompletionSaveStatus.Failed);

	/// <summary>Async UI event boundary: awaits the entire transaction and main-thread state update, including retries.
	/// A lost response reuses the captured run token; exceptions become a visible retry state.</summary>
	private async void PersistCompletion()
	{
		if (!_saveConfigured || _savePending) return;
		_savePending = true;
		RefreshSaveStatus();
		CompletionSaveResult result;
		try { result = await CampaignProgress.CompleteMissionAsync(_saveMission, _saveScore, _runToken, _saveSession); }
		catch (System.Exception) { result = new(CompletionSaveStatus.Failed, Error: "Campaign save could not be confirmed. Retry Save."); }
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
			_savePending = false; _saveResult = result;
			RefreshSaveStatus();
		}).CallDeferred();
	}

	/// <summary>Shows pending/saved/failed/direct-play status beside results and gates campaign navigation.</summary>
	private void RefreshSaveStatus()
	{
		if (_saveLabel != null) _saveLabel.Text = !_saveConfigured ? "" : _savePending ? "SAVING CAMPAIGN…" : _saveResult?.Status switch
		{ CompletionSaveStatus.Saved => "CAMPAIGN SAVED", CompletionSaveStatus.NoActiveCampaign => "DIRECT PLAY // NO ACTIVE CAMPAIGN", _ => "SAVE NOT CONFIRMED // RETRY SAVE OR RETURN TO MAIN MENU" };
		if (_retrySaveBtn != null) _retrySaveBtn.Visible = !_savePending && _saveResult?.Status == CompletionSaveStatus.Failed;
		foreach (var button in new[] { _continueBtn, _replayBtn, _levelSelectBtn }) if (button != null) button.Disabled = !CanLeaveResults;
		if (_previewBtn != null) _previewBtn.Disabled = !CanLeaveResults;
	}

	/// <summary>Handles pointer and direct touch before Global's legacy mouse suppression consumes them.</summary>
	public override void _Input(InputEvent input)
	{
		InputPromptService.Instance?.ObserveInput(input);
		if (_rootPanel is not { Visible: true } || !IsInsideTree() || _navigating) return;
		Vector2 point;
		if (input is InputEventScreenTouch { Pressed: true } touch) point = touch.Position;
		else if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse) point = mouse.Position;
		else return;
		foreach (var button in new[] { _continueBtn, _previewBtn, _replayBtn, _levelSelectBtn, _mainMenuBtn, _retrySaveBtn })
		{
			if (button is not { Visible: true, Disabled: false } || !button.GetGlobalRect().HasPoint(point)) continue;
			GetViewport().SetInputAsHandled();
			button.EmitSignal(Button.SignalName.Pressed);
			return;
		}
	}

	/// <summary>Restores the gameplay cursor policy when leaving the results overlay's owning mission.</summary>
	public override void _ExitTree()
	{
		Input.MouseMode = _previousMouseMode;
		GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _previousPromptVisible);
	}
}
