using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Main menu controller. Manages animated clouds/title, session UI, leaderboard,
/// settings, language selection, and falling-character decoration.
/// Converted from main_menu.gd.
/// </summary>
public partial class MainMenu : Node
{
	// ─── Cached packed scene ─────────────────────────────────────────────────
	private static readonly PackedScene FallingCharacterScene =
		GD.Load<PackedScene>("res://assets/scenes/falling_character.tscn");

	// ─── Node references ─────────────────────────────────────────────────────
	private Panel       _quitPopup;
	private ColorRect   _quitBackground;
	private Panel       _settingsPopup;
	private Panel       _instructionsPopup;
	private Panel       _leaderboardPopup;
	private Panel       _languagePopup;
	private GridContainer _languageGrid;
	private VBoxContainer _scoresTable;
	private HBoxContainer _titleContainer;
	private Button       _browserLoginButton;
	private HBoxContainer _profileDisplayContainer;
	private TextureRect  _avatarRect;
	private Label        _usernameLabel;

	// Session management
	private Panel           _newGamePopup;
	private Panel           _continuePopup;
	private VBoxContainer   _sessionsContainer;
	private Button          _continueButton;
	private Panel           _confirmPopup;
	private Label           _confirmLabel;


	// ─── Cloud data ──────────────────────────────────────────────────────────
	private readonly Texture2D _cloudTexture =
		GD.Load<Texture2D>("res://assets/images/clouds.png");
	private const int MaxClouds = 8;
	private readonly List<Sprite2D> _clouds = new();

	private struct CloudData
	{
		public float Speed;
		public int   Layer;
		public float BaseY;
		public float DriftAmount;
		public float DriftSpeed;
		public float DriftOffset;
	}
	private readonly System.Collections.Generic.Dictionary<Sprite2D, CloudData> _cloudData = new();

	// ─── Falling-character spawn timer ───────────────────────────────────────
	private float _spawnTimer        = 0f;
	private float _nextSpawnTime     = 0f;
	private const float SpawnIntervalMin = 0.5f;
	private const float SpawnIntervalMax = 2.0f;

	// ─── Leaderboard DB ──────────────────────────────────────────────────────
	private GodotObject _db = null;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		GD.Print("main_menu: _ready() start");
		GetNode<Node>("/root/Global").Call("play_menu_music");

		// Resolve node references
		_quitPopup          = GetNode<Panel>("QuitPopup");
		_quitBackground     = GetNode<ColorRect>("QuitBackground");
		_settingsPopup      = GetNode<Panel>("SettingsPopup");
		_instructionsPopup  = GetNode<Panel>("InstructionsPopup");
		_leaderboardPopup   = GetNode<Panel>("LeaderboardPopup");
		_languagePopup      = GetNode<Panel>("LanguagePopup");
		_languageGrid       = GetNode<GridContainer>("LanguagePopup/VBoxContainer/ScrollContainer/LanguageGrid");
		_scoresTable        = GetNode<VBoxContainer>("LeaderboardPopup/VBoxContainer/ScrollContainer/ScoresTable");
		_titleContainer     = GetNode<HBoxContainer>("TitleContainer");
		_newGamePopup       = GetNode<Panel>("NewGamePopup");
		_continuePopup      = GetNode<Panel>("ContinuePopup");
		_sessionsContainer  = GetNode<VBoxContainer>("ContinuePopup/VBoxContainer/ScrollContainer/SessionsContainer");
		_continueButton     = GetNode<Button>("ButtonContainer/ContinueButton");
		_confirmPopup       = GetNode<Panel>("ConfirmPopup");
		_confirmLabel       = GetNode<Label>("ConfirmPopup/VBoxContainer/ConfirmLabel");
		SetupBrowserLoginButton();
		SetupProfileUI();
		if (AuthManager.Instance != null)
		{
			AuthManager.Instance.OnProfileUpdated += UpdateAuthUI;
		}
		UpdateAuthUI();

		PopulateLanguageButtons();
		_ = SetupFloatingTitleAsync();
		ResetSpawnTimer();

		_continueButton.Visible = SessionManager.Instance.HasSessions();

		// Ensure CloudsLayer exists
		if (!HasNode("CloudsLayer"))
		{
			GD.Print("main_menu: CloudsLayer not found — creating runtime CloudsLayer node");
			var layer = new Node2D { Name = "CloudsLayer" };
			AddChild(layer);
			MoveChild(layer, Mathf.Clamp(1, 0, GetChildCount() - 1));
		}

		if (_cloudTexture == null)
			GD.PushError("main_menu: cloud_scene texture failed to load (null).");
		else
			SpawnInitialClouds();

		GD.Print("main_menu: _ready() complete");
	}

	// ─── Cloud spawning ──────────────────────────────────────────────────────
	private void SpawnInitialClouds()
	{
		if (!HasNode("CloudsLayer")) { GD.PushError("main_menu: CloudsLayer missing."); return; }

		var cloudsLayer = GetNode<Node2D>("CloudsLayer");
		var size = GetViewport().GetVisibleRect().Size;

		for (int i = 0; i < MaxClouds; i++)
		{
			var sprite = new Sprite2D { Texture = _cloudTexture };
			int layer  = i % 3;
			float y    = (float)GD.RandRange(40 + layer * 60, size.Y * 0.5f);

			sprite.Position = new Vector2((float)GD.RandRange(0, size.X), y);

			float depthFactor = 1f - layer * 0.2f;
			float baseScale   = (float)GD.RandRange(0.7, 1.3) * depthFactor;
			sprite.Scale = Vector2.One * baseScale;

			float startAlpha = (float)GD.RandRange(0.5, 0.8) * depthFactor;
			sprite.Modulate = sprite.Modulate with { A = 0f };

			var tween = CreateTween();
			tween?.TweenProperty(sprite, "modulate:a", startAlpha, GD.RandRange(1.5, 3.0));

			cloudsLayer.AddChild(sprite);
			_clouds.Add(sprite);

			_cloudData[sprite] = new CloudData
			{
				Speed       = (float)GD.RandRange((80 - layer * 25) - 15, (80 - layer * 25) + 15),
				Layer       = layer,
				BaseY       = y,
				DriftAmount = (float)GD.RandRange(5.0, 12.0),
				DriftSpeed  = (float)GD.RandRange(0.3, 0.7),
				DriftOffset = (float)GD.RandRange(0.0, Mathf.Tau),
			};
		}
	}

	// ─── Process (clouds + falling character spawn) ──────────────────────────
	public override void _Process(double delta)
	{
		float dt   = (float)delta;
		float time = Time.GetTicksMsec() / 1000f;
		var   size = GetViewport().GetVisibleRect().Size;

		foreach (var cloud in _clouds)
		{
			var data = _cloudData[cloud];

			cloud.Position -= new Vector2(data.Speed * dt, 0f);

			// Vertical drift

			cloud.Position = cloud.Position with
			{
				Y = data.BaseY + Mathf.Sin(time * data.DriftSpeed + data.DriftOffset) * data.DriftAmount
			};

			// Recycle off-screen clouds
			if (cloud.Texture != null &&
				cloud.Position.X < -cloud.Texture.GetSize().X * cloud.Scale.X)
			{
				int   layer       = data.Layer;
				float baseSpeed   = 80 - layer * 25;
				float y           = (float)GD.RandRange(40 + layer * 60, size.Y * 0.5f);
				float depthFactor = 1f - layer * 0.2f;
				float baseScale   = (float)GD.RandRange(0.7, 1.3) * depthFactor;

				cloud.Position = new Vector2((float)GD.RandRange(size.X + 50, size.X + 300), cloud.Position.Y);
				cloud.Scale    = Vector2.One * baseScale;

				float targetAlpha = (float)GD.RandRange(0.5, 0.8) * depthFactor;
				cloud.Modulate = cloud.Modulate with { A = 0f };
				CreateTween()?.TweenProperty(cloud, "modulate:a", targetAlpha, GD.RandRange(2.0, 4.0));

				_cloudData[cloud] = new CloudData
				{
					Speed       = (float)GD.RandRange(baseSpeed - 15, baseSpeed + 15),
					Layer       = layer,
					BaseY       = y,
					DriftAmount = (float)GD.RandRange(5.0, 12.0),
					DriftSpeed  = (float)GD.RandRange(0.3, 0.7),
					DriftOffset = (float)GD.RandRange(0.0, Mathf.Tau),
				};
			}
		}

		// Spawn falling characters
		_spawnTimer += dt;
		if (_spawnTimer >= _nextSpawnTime)
		{
			SpawnFallingCharacter();
			ResetSpawnTimer();
		}
	}

	private void ResetSpawnTimer()
	{
		_spawnTimer    = 0f;
		_nextSpawnTime = (float)GD.RandRange(SpawnIntervalMin, SpawnIntervalMax);
	}

	private void SpawnFallingCharacter()
	{
		var character = FallingCharacterScene.Instantiate<FallingCharacter>();
		string[] names = { "Ninja Frog", "Mask Dude", "Pink Man", "Virtual Guy" };
		character.CharacterName = names[GD.Randi() % (uint)names.Length];

		float screenWidth = GetViewport().GetVisibleRect().Size.X;
		character.Position  = new Vector2((float)GD.RandRange(50, screenWidth - 50), -50f);
		float scaleFactor   = (float)GD.RandRange(1.5, 2.5);
		character.Scale     = Vector2.One * scaleFactor;
		character.FallSpeed = (float)GD.RandRange(150, 300);

		AddChild(character);
		MoveChild(character, 1);
	}

	// ─── Floating title ──────────────────────────────────────────────────────
	private async System.Threading.Tasks.Task SetupFloatingTitleAsync()
	{
		string titleText = Tr("PIXEL JUMPERS");
		var boldFont = GD.Load<FontFile>("res://assets/fonts/ComicNeue-Bold.ttf");

		foreach (Node child in _titleContainer.GetChildren())
			child.QueueFree();

		var wrapper = new Control
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize   = new Vector2(0, 120),
		};
		_titleContainer.AddChild(wrapper);

		var label = new Label
		{
			Text               = titleText,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
			AnchorLeft         = 0.5f,
			AnchorRight        = 0.5f,
			AnchorTop          = 0.5f,
			AnchorBottom       = 0.5f,
			GrowHorizontal     = Control.GrowDirection.Both,
			GrowVertical       = Control.GrowDirection.Both,
		};
		label.AddThemeColorOverride("font_color",         new Color(1, 1, 1, 1));
		label.AddThemeColorOverride("font_outline_color", new Color(0, 0.4f, 0, 1));
		label.AddThemeConstantOverride("outline_size", 12);
		label.AddThemeFontOverride("font", boldFont);
		label.AddThemeFontSizeOverride("font_size", 80);
		wrapper.AddChild(label);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		label.PivotOffset = label.Size / 2f;

		await AnimateFloatingTitleAsync(label);
	}

	private async System.Threading.Tasks.Task AnimateFloatingTitleAsync(Label label)
	{
		label.Scale   = new Vector2(0.1f, 0.1f);
		label.Modulate = label.Modulate with { A = 0f };

		var zoom = CreateTween();
		zoom.SetParallel(true);
		zoom.TweenProperty(label, "scale",      Vector2.One, 1.5)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		zoom.TweenProperty(label, "modulate:a", 1.0f,        1.0)
			.SetEase(Tween.EaseType.Out);
		await ToSignal(zoom, Tween.SignalName.Finished);

		float origY = label.Position.Y;
		StartFloatLoop(label, origY);
	}

	private async void StartFloatLoop(Node label, float origY)
	{
		var t = CreateTween();
		t.TweenProperty(label, "position:y", origY - 15f, 2.0)
			.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
		t.TweenProperty(label, "position:y", origY + 15f, 2.0)
			.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
		await ToSignal(t, Tween.SignalName.Finished);
		if (IsInstanceValid(label))
			StartFloatLoop(label, origY);
	}

	// ─── Language buttons ────────────────────────────────────────────────────
	private void PopulateLanguageButtons()
	{
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		var languages    = (Dictionary)audioOptions.Get("languages");

		foreach (Node child in _languageGrid.GetChildren())
			child.QueueFree();

		// English first
		string englishKey = "";
		foreach (var key in languages.Keys)
		{
			if ((string)languages[key] == "english") { englishKey = (string)key; break; }
		}
		if (!string.IsNullOrEmpty(englishKey))
			CreateLanguageButton(englishKey, (string)languages[englishKey]);

		var sorted = new List<string>();
		foreach (var key in languages.Keys)
			if ((string)key != englishKey) sorted.Add((string)key);
		sorted.Sort();

		foreach (var langName in sorted)
			CreateLanguageButton(langName, (string)languages[langName]);
	}

	private async void CreateLanguageButton(string langName, string locale)
	{
		var button = new Button
		{
			Text               = langName,
			CustomMinimumSize  = new Vector2(370, 50),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ClipText           = false,
			TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
		};
		button.AddThemeColorOverride("font_color",        new Color(1, 1, 1, 1));
		button.AddThemeColorOverride("font_hover_color",  new Color(1, 1, 1, 1));
		button.AddThemeColorOverride("font_pressed_color",new Color(0.9f, 0.9f, 0.9f, 1));
		button.AddThemeFontSizeOverride("font_size", 20);

		var settingsBtn = GetNode<Button>("ButtonContainer/Row2/Settings");
		button.AddThemeStyleboxOverride("normal",  settingsBtn.GetThemeStylebox("normal",  "Button"));
		button.AddThemeStyleboxOverride("hover",   settingsBtn.GetThemeStylebox("hover",   "Button"));
		button.AddThemeStyleboxOverride("pressed", settingsBtn.GetThemeStylebox("normal",  "Button"));

		button.Pressed += () => OnLanguageSelected(langName);
		_languageGrid.AddChild(button);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		string[] rtlLocales = { "arabic", "hebrew" };
		bool isRtl = locale != null && System.Array.Exists(rtlLocales,
						l => l == locale.ToLower());

		GD.Print("create_language_button: added", langName, " locale=", locale, " is_rtl=", isRtl);
		foreach (Node child in button.GetChildren())
		{
			GD.Print("  child: ", child, " ", child.GetType());
			if (child is Label lbl)
			{
				GD.Print("    before: text_direction=", lbl.TextDirection, " h_align=", lbl.HorizontalAlignment);
				if (isRtl)
				{
					lbl.TextDirection       = Control.TextDirection.Rtl;
					lbl.HorizontalAlignment = HorizontalAlignment.Right;
				}
				GD.Print("    after: text_direction=", lbl.TextDirection, " h_align=", lbl.HorizontalAlignment);
			}
		}
	}

	private void OnLanguageSelected(string langName)
	{
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		audioOptions.Set("selected_language", langName);

		var langButton = GetNode<Button>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageButton");
		langButton.Text = langName;

		var languages   = (Dictionary)audioOptions.Get("languages");
		string locale   = languages.ContainsKey(Variant.From(langName)) ? (string)languages[langName] : "";
		string[] rtlLocales = { "arabic", "hebrew" };
		bool isRtl = !string.IsNullOrEmpty(locale) &&
					 System.Array.Exists(rtlLocales, l => l == locale.ToLower());

		if (isRtl)
		{
			foreach (Node child in langButton.GetChildren())
			{
				if (child is Label lbl)
				{
					lbl.TextDirection       = Control.TextDirection.Rtl;
					lbl.HorizontalAlignment = HorizontalAlignment.Right;
				}
			}
		}

		if (_instructionsPopup.Visible) UpdateInstructionsAlignment();
		_languagePopup.Visible = false;
	}

	private void UpdateInstructionsAlignment()
	{
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		var currentLang  = (string)audioOptions.Get("selected_language");
		var languages    = (Dictionary)audioOptions.Get("languages");

		bool isRtl = false;
		if (languages.ContainsKey(Variant.From(currentLang)))
		{
			string locale = (string)languages[currentLang];
			isRtl = locale.ToLower() is "arabic" or "hebrew";
		}

		var content = _instructionsPopup.GetNode<Container>(
			"VBoxContainer/ScrollContainer/MarginContainer/ContentContainer");
		var alignment = isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;

		foreach (Node child in content.GetChildren())
		{
			if (child is Label lbl)
			{
				lbl.TextDirection       = Control.TextDirection.Auto;
				lbl.HorizontalAlignment = alignment;
			}
		}
	}

	// ─── Leaderboard ─────────────────────────────────────────────────────────
	private void InitDatabase()
	{
		_db = ClassDB.Instantiate("SQLite").As<GodotObject>();
		_db.Set("path", "user://scores.db");
		_db.Call("open_db");

		var tableDict = new Dictionary
		{
			{ "id",         new Dictionary { { "data_type", "INTEGER" }, { "primary_key", true }, { "auto_increment", true } } },
			{ "level_name", new Dictionary { { "data_type", "TEXT"    }, { "not_null",    true } } },
			{ "score",      new Dictionary { { "data_type", "INTEGER" }, { "not_null",    true } } },
		};
		_db.Call("create_table", "scores", tableDict);
	}

	private void LoadScores()
	{
		foreach (Node child in _scoresTable.GetChildren())
			child.QueueFree();

		_db.Call("query", "SELECT level_name, score FROM scores ORDER BY score DESC");
		var results = (Godot.Collections.Array)_db.Get("query_result");

		AddRow(Tr("Level"), Tr("Points"), true);
		foreach (var item in results)
		{
			var row = (Dictionary)item;
			AddRow((string)row["level_name"], row["score"].ToString(), false);
		}
	}

	private void AddRow(string level, string score, bool isHeader)
	{
		var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 40) };

		var boldFont    = GD.Load<Font>("res://assets/fonts/ComicNeue-Bold.ttf");
		var regularFont = GD.Load<Font>("res://assets/fonts/ComicNeue-Regular.ttf");

		row.AddChild(MakeScoreLabel(level, isHeader, boldFont, regularFont));
		row.AddChild(MakeScoreLabel(score, isHeader, boldFont, regularFont));
		_scoresTable.AddChild(row);
	}

	private static Label MakeScoreLabel(string text, bool isHeader, Font bold, Font regular)
	{
		var lbl = new Label
		{
			Text              = text,
			CustomMinimumSize = new Vector2(300, 0),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		if (isHeader)
		{
			lbl.AddThemeColorOverride("font_color", new Color(0, 0.8f, 0, 1));
			lbl.AddThemeFontSizeOverride("font_size", 24);
			lbl.AddThemeFontOverride("font", bold);
		}
		else
		{
			lbl.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
			lbl.AddThemeFontSizeOverride("font_size", 20);
			lbl.AddThemeFontOverride("font", regular);
		}
		return lbl;
	}

	// ─── Session list ────────────────────────────────────────────────────────
	private void PopulateSessions()
	{
		foreach (Node child in _sessionsContainer.GetChildren())
			child.QueueFree();

		var sessions = SessionManager.Instance.GetAllSessions();
		if (sessions.Count == 0)
		{
			var empty = new Label
			{
				Text               = Tr("No saved games found"),
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			empty.AddThemeFontSizeOverride("font_size", 24);
			empty.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
			_sessionsContainer.AddChild(empty);
			return;
		}

		// Delete All button
		var actionRow     = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		var deleteAllBtn  = MakeDeleteButton("Delete All");
		deleteAllBtn.Pressed += OnDeleteAllPressed;
		actionRow.AddChild(deleteAllBtn);
		_sessionsContainer.AddChild(actionRow);

		var spacer = new Control { CustomMinimumSize = new Vector2(0, 6) };
		_sessionsContainer.AddChild(spacer);

		var regularFont = GD.Load<Font>("res://assets/fonts/ComicNeue-Regular.ttf");

		foreach (var item in sessions)
		{
			var session    = (Dictionary)item;
			int  sessionId = session["id"].AsInt32();
			string character  = (string)session["character"];
			int  level     = session["current_level"].AsInt32();
			int  score     = session["total_score"].AsInt32();
			string lastPlayed = SessionManager.Instance.FormatDatetime((string)session["last_played"]);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);

			// Continue button
			var sessionBtn = new Button
			{
				Text               = character + "  |  " + Tr("Level") + " " + level +
									 "  |  " + Tr("Score") + ": " + score + "  |  " + lastPlayed,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize  = new Vector2(0, 70),
			};
			sessionBtn.AddThemeFontSizeOverride("font_size", 22);
			sessionBtn.AddThemeFontOverride("font", regularFont);
			sessionBtn.AddThemeStyleboxOverride("normal",  MakeRoundedBox(new Color(0, 0.5f, 0, 0.8f)));
			sessionBtn.AddThemeStyleboxOverride("hover",   MakeRoundedBox(new Color(0, 0.7f, 0, 0.9f)));
			sessionBtn.AddThemeStyleboxOverride("pressed", MakeRoundedBox(new Color(0, 0.7f, 0, 0.9f)));
			sessionBtn.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));

			var capturedId   = sessionId;
			var capturedSess = session;
			sessionBtn.Pressed += () => OnSessionSelected(capturedId, capturedSess);
			row.AddChild(sessionBtn);

			// Delete (✕) button
			var delBtn = new Button
			{
				Text              = "✕",
				CustomMinimumSize = new Vector2(60, 70),
			};
			delBtn.AddThemeFontSizeOverride("font_size", 28);
			delBtn.AddThemeStyleboxOverride("normal",  MakeRoundedBox(new Color(0.75f, 0.1f, 0.1f, 0.95f)));
			delBtn.AddThemeStyleboxOverride("hover",   MakeRoundedBox(new Color(0.9f,  0.15f, 0.15f, 1f)));
			delBtn.AddThemeStyleboxOverride("pressed", MakeRoundedBox(new Color(0.9f,  0.15f, 0.15f, 1f)));
			delBtn.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
			delBtn.Pressed += () => OnDeleteOnePressed(capturedId);
			row.AddChild(delBtn);

			_sessionsContainer.AddChild(row);
			_sessionsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
		}
	}

	private static StyleBoxFlat MakeRoundedBox(Color color, int radius = 10)
	{
		var s = new StyleBoxFlat { BgColor = color };
		s.SetCornerRadiusAll(radius);
		return s;
	}

	private static Button MakeDeleteButton(string text)
	{
		var btn = new Button { Text = text };
		btn.AddThemeFontSizeOverride("font_size", 20);
		btn.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/ComicNeue-Regular.ttf"));
		var style = new StyleBoxFlat { BgColor = new Color(0.55f, 0.05f, 0.05f, 0.95f) };
		style.SetCornerRadiusAll(8);
		btn.AddThemeStyleboxOverride("normal", style);
		btn.AddThemeStyleboxOverride("hover",  style);
		btn.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
		return btn;
	}

	// ─── Signal / button handlers ────────────────────────────────────────────
	public void _on_language_button_pressed()  => _languagePopup.Visible = true;
	public void _on_close_language_popup_pressed() => _languagePopup.Visible = false;
	public void _on_play_pressed()             => _newGamePopup.Visible = true;

	public void _on_instructions_pressed()
	{
		UpdateInstructionsAlignment();
		_instructionsPopup.Visible = true;
	}

	public void _on_leaderboard_pressed()
	{
		if (_db == null) InitDatabase();
		LoadScores();
		_leaderboardPopup.Visible = true;
	}

	public void _on_settings_pressed() => _settingsPopup.Visible = true;

	public void _on_apply_settings_pressed()
	{
		var slider = GetNode<HSlider>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/MasterSlider");
		AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb((float)slider.Value));

		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		audioOptions.Call("apply_language_change");

		_ = SetupFloatingTitleAsync();
		_settingsPopup.Visible = false;
	}

	public void _on_close_settings_pressed()      => _settingsPopup.Visible = false;
	public void _on_yes_pressed()                 => GetTree().Quit();
	public void _on_no_pressed()
	{
		_quitPopup.Visible = false;
		_quitBackground.Visible = false;
	}
	public void _on_quit_button_pressed()
	{
		_quitBackground.Visible = true;
		_quitPopup.Visible = true;
	}
	public void _on_close_instructions_pressed()  => _instructionsPopup.Visible = false;
	public void _on_close_leaderboard_pressed()   => _leaderboardPopup.Visible = false;

	public void _on_new_game_confirm_pressed()
	{
		_newGamePopup.Visible = false;
		LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/character_selection.tscn");
	}

	public void _on_new_game_cancel_pressed() => _newGamePopup.Visible = false;

	public void _on_continue_pressed()
	{
		PopulateSessions();
		_continuePopup.Visible = true;
	}

	private void OnDeleteOnePressed(int sessionId)
	{
		_confirmLabel.Text = "Delete this saved game?\nThis cannot be undone.";
		_confirmPopup.SetMeta("delete_mode",       "one");
		_confirmPopup.SetMeta("delete_session_id", sessionId);
		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
	}

	private void OnDeleteAllPressed()
	{
		_confirmLabel.Text = "Delete ALL saved games?\nThis cannot be undone.";
		_confirmPopup.SetMeta("delete_mode", "all");
		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
	}

	private void OnSessionSelected(int sessionId, Dictionary sessionData)
	{
		string character  = (string)sessionData["character"];
		int    level      = sessionData["current_level"].AsInt32();
		string lastPlayed = SessionManager.Instance.FormatDatetime((string)sessionData["last_played"]);

		_confirmLabel.Text = character + "  |  " + Tr("Level") + " " + level + "\n" +
							 Tr("Continue this game?") + "\n" + Tr("Last played") + ": " + lastPlayed;

		_confirmPopup.SetMeta("delete_mode",   "none");
		_confirmPopup.SetMeta("session_id",    sessionId);
		_confirmPopup.SetMeta("session_data",  sessionData);

		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
	}

	public void _on_confirm_yes_pressed()
	{
		string deleteMode = (string)_confirmPopup.GetMeta("delete_mode", "none");

		if (deleteMode == "one")
		{
			int delId = _confirmPopup.GetMeta("delete_session_id").AsInt32();
			SessionManager.Instance.DeleteSession(delId);
			_confirmPopup.Visible = false;
			if (SessionManager.Instance.HasSessions())
			{
				PopulateSessions();
				_continuePopup.Visible = true;
			}
			else
			{
				_continuePopup.Visible = _continueButton.Visible = false;
			}
			return;
		}

		if (deleteMode == "all")
		{
			SessionManager.Instance.DeleteAllSessions();
			_confirmPopup.Visible  = false;
			_continuePopup.Visible = false;
			_continueButton.Visible = false;
			return;
		}

		// Continue session
		int sessionId   = _confirmPopup.GetMeta("session_id").AsInt32();
		var sessionData = (Dictionary)_confirmPopup.GetMeta("session_data");
		SessionManager.Instance.LoadSession(sessionId);
		GetNode<Node>("/root/Global").Set("selected_character", sessionData["character"]);

		int  levelNum   = sessionData["current_level"].AsInt32();
		string levelScene = "res://assets/scenes/level" + levelNum + ".tscn";

		_confirmPopup.Visible = false;
		LoadingScreen.LoadScene(GetTree(), levelScene);
	}

	public void _on_confirm_no_pressed()
	{
		_confirmPopup.Visible  = false;
		_continuePopup.Visible = true;
	}

	private void SetupBrowserLoginButton()
	{
		var row2 = GetNode<HBoxContainer>("ButtonContainer/Row2");

		_browserLoginButton = new Button
		{
			Text               = Tr("Login via Browser"),
			CustomMinimumSize  = new Vector2(200, 50),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		var settingsBtn = GetNode<Button>("ButtonContainer/Row2/Settings");
		_browserLoginButton.AddThemeStyleboxOverride("normal",  settingsBtn.GetThemeStylebox("normal",  "Button"));
		_browserLoginButton.AddThemeStyleboxOverride("hover",   settingsBtn.GetThemeStylebox("hover",   "Button"));
		_browserLoginButton.AddThemeStyleboxOverride("pressed", settingsBtn.GetThemeStylebox("normal",  "Button"));
		_browserLoginButton.AddThemeFontSizeOverride("font_size", 20);
		_browserLoginButton.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));

		_browserLoginButton.Pressed += OnBrowserLoginPressed;
		row2.AddChild(_browserLoginButton);
	}

	private async void OnBrowserLoginPressed()
	{
		_browserLoginButton.Disabled = true;
		_browserLoginButton.Text = Tr("Waiting for Browser...");

		bool success = await AuthManager.Instance.StartBrowserLoginAsync();

		if (success)
		{
			GD.Print("Browser login successful!");
			_browserLoginButton.Text = Tr("Logged In!");
			// Optionally trigger a profile popup or a visual indicator
		}
		else
		{
			GD.PrintErr("Browser login failed or timed out.");
			_browserLoginButton.Text = Tr("Login Failed");
			await System.Threading.Tasks.Task.Delay(3000);
		}

		_browserLoginButton.Disabled = false;
		_browserLoginButton.Text = Tr("Login via Browser");
	}

	public override void _ExitTree()
	{
		if (AuthManager.Instance != null)
		{
			AuthManager.Instance.OnProfileUpdated -= UpdateAuthUI;
		}
	}

	private void SetupProfileUI()
	{
		var row2 = GetNode<HBoxContainer>("ButtonContainer/Row2");

		_profileDisplayContainer = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Begin,
			Visible = false
		};

		// 1. Avatar Rect
		_avatarRect = new TextureRect
		{
			CustomMinimumSize = new Vector2(50, 50),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		var avatarBorder = new PanelContainer
		{
			CustomMinimumSize = new Vector2(54, 54),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		var borderStyle = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.5f),
			BorderColor = new Color(0.96f, 0.08f, 0.56f, 1f), // Neon fuchsia
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2
		};
		avatarBorder.AddThemeStyleboxOverride("panel", borderStyle);
		avatarBorder.AddChild(_avatarRect);
		_profileDisplayContainer.AddChild(avatarBorder);

		// 2. Info Container
		var infoContainer = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		_usernameLabel = new Label
		{
			Text = "",
			CustomMinimumSize = new Vector2(100, 20)
		};
		_usernameLabel.AddThemeColorOverride("font_color", new Color(0, 0.95f, 1f, 1f)); // Neon cyan
		_usernameLabel.AddThemeFontSizeOverride("font_size", 18);
		infoContainer.AddChild(_usernameLabel);

		_profileDisplayContainer.AddChild(infoContainer);

		// 3. Logout button
		_logoutButton = new Button
		{
			Text = Tr("Logout"),
			CustomMinimumSize = new Vector2(100, 40),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		
		var settingsBtn = GetNode<Button>("ButtonContainer/Row2/Settings");
		_logoutButton.AddThemeStyleboxOverride("normal", settingsBtn.GetThemeStylebox("normal", "Button"));
		_logoutButton.AddThemeStyleboxOverride("hover", settingsBtn.GetThemeStylebox("hover", "Button"));
		_logoutButton.AddThemeStyleboxOverride("pressed", settingsBtn.GetThemeStylebox("normal", "Button"));
		_logoutButton.AddThemeFontSizeOverride("font_size", 16);
		_logoutButton.Pressed += OnLogoutPressed;
		_profileDisplayContainer.AddChild(_logoutButton);

		row2.AddChild(_profileDisplayContainer);
	}

	private Button _logoutButton;

	private void OnLogoutPressed()
	{
		AuthManager.Instance.Logout();
		UpdateAuthUI();
	}

	private void UpdateAuthUI()
	{
		var user = AuthManager.Instance.CurrentUser;
		if (user != null)
		{
			_browserLoginButton.Visible = false;
			_usernameLabel.Text = user.Username;

			if (!string.IsNullOrEmpty(user.ProfilePicture))
			{
				try
				{
					string base64Data = user.ProfilePicture;
					if (base64Data.Contains(","))
					{
						base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
					}
					byte[] imageBytes = Convert.FromBase64String(base64Data);

					var img = new Image();
					var err = img.LoadPngFromBuffer(imageBytes);
					if (err != Error.Ok)
					{
						err = img.LoadJpgFromBuffer(imageBytes);
					}

					if (err == Error.Ok)
					{
						var tex = ImageTexture.CreateFromImage(img);
						_avatarRect.Texture = tex;
					}
					else
					{
						GD.PrintErr($"Failed to parse image from buffer: {err}");
						_avatarRect.Texture = null;
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"Error loading profile picture in Godot: {ex.Message}");
					_avatarRect.Texture = null;
				}
			}
			else
			{
				_avatarRect.Texture = null;
			}

			_profileDisplayContainer.Visible = true;
		}
		else
		{
			if (_profileDisplayContainer != null)
				_profileDisplayContainer.Visible = false;
			if (_browserLoginButton != null)
				_browserLoginButton.Visible = true;
		}
	}
}
