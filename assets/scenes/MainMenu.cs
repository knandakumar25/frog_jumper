using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Main menu controller. Manages animated clouds/title, session UI, leaderboard,
/// settings, language selection, and falling-character decoration.
/// Converted from main_menu.gd.
/// </summary>
public partial class MainMenu : Control
{
	// ─── Cached packed scene ─────────────────────────────────────────────────
	private static readonly PackedScene FallingCharacterScene =
		GD.Load<PackedScene>("res://assets/scenes/falling_character.tscn");

	// ─── Node references ─────────────────────────────────────────────────────
	private Panel       _quitPopup;
	private ColorRect   _quitBackground;
	private Panel       _settingsPopup;
	private Panel       _instructionsPopup;
	private CyberDropdown _languageDropdown;
	private HBoxContainer _titleContainer;
	private Button       _browserLoginButton;
	private HBoxContainer _profileDisplayContainer;
	private TextureRect  _avatarRect;
	private Label        _usernameLabel;

	// Session management
	private Panel           _newGamePopup;
	private Panel           _continuePopup;
	private VBoxContainer   _sessionsContainer;
	private Control         _continueButton;
	private Panel           _confirmPopup;
	private Label           _confirmLabel;
	private string          _pendingSelectedLanguage = "";


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

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		GD.Print("main_menu: _ready() start");
		try
		{
			// Resolve node references
			GD.Print("main_menu: resolving node references...");
			_quitPopup          = GetNode<Panel>("QuitPopup");
			GD.Print("main_menu: resolved QuitPopup");
			_quitBackground     = GetNode<ColorRect>("QuitBackground");
			GD.Print("main_menu: resolved QuitBackground");
			_settingsPopup      = GetNode<Panel>("SettingsPopup");
			GD.Print("main_menu: resolved SettingsPopup");
			_instructionsPopup  = GetNode<Panel>("InstructionsPopup");
			GD.Print("main_menu: resolved InstructionsPopup");
			_languageDropdown   = GetNode<CyberDropdown>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown");
			GD.Print("main_menu: resolved LanguageDropdown");
			_titleContainer     = GetNode<HBoxContainer>("TitleContainer");
			GD.Print("main_menu: resolved TitleContainer");
			_newGamePopup       = GetNode<Panel>("NewGamePopup");
			GD.Print("main_menu: resolved NewGamePopup");
			_continuePopup      = GetNode<Panel>("ContinuePopup");
			GD.Print("main_menu: resolved ContinuePopup");
			_sessionsContainer  = GetNode<VBoxContainer>("ContinuePopup/VBoxContainer/ScrollContainer/SessionsContainer");
			GD.Print("main_menu: resolved SessionsContainer");
			_continueButton     = GetNode<Control>("ButtonContainer/ContinueButton");
			GD.Print("main_menu: resolved ContinueButton");
			_confirmPopup       = GetNode<Panel>("ConfirmPopup");
			GD.Print("main_menu: resolved ConfirmPopup");
			_confirmLabel       = GetNode<Label>("ConfirmPopup/VBoxContainer/ConfirmLabel");
			GD.Print("main_menu: resolved ConfirmLabel");

			SetupProfileUI();
			GD.Print("main_menu: SetupProfileUI complete");

			if (AuthManager.Instance != null)
			{
				GD.Print("main_menu: AuthManager.Instance is not null");
				AuthManager.Instance.OnProfileUpdated += UpdateAuthUI;
			}
			else
			{
				GD.PrintErr("main_menu: AuthManager.Instance is NULL!");
			}

			UpdateAuthUI();
			GD.Print("main_menu: UpdateAuthUI complete");

			PopulateLanguageButtons();
			GD.Print("main_menu: PopulateLanguageButtons complete");

			_ = SetupFloatingTitleAsync();
			GD.Print("main_menu: SetupFloatingTitleAsync started");

			ResetSpawnTimer();
			GD.Print("main_menu: ResetSpawnTimer complete");

			MoveChild(_titleContainer, GetChildCount() - 1);
			GD.Print("main_menu: MoveChild TitleContainer complete");

			_continueButton.Visible = SessionManager.Instance.HasSessions();
			GD.Print("main_menu: updated _continueButton.Visible");

			GetNode<CyberButton>("ButtonContainer/Play").Pressed += _on_play_pressed;
			GetNode<CyberButton>("ButtonContainer/ContinueButton").Pressed += _on_continue_pressed;
			GetNode<CyberButton>("ButtonContainer/Instructions").Pressed += _on_instructions_pressed;
			GetNode<CyberButton>("ButtonContainer/Settings").Pressed += _on_settings_pressed;
			GetNode<CyberButton>("ButtonContainer/QuitButton").Pressed += _on_quit_button_pressed;

			EnsureUISoundManager();
			UISoundManager.AutoWireButtons(this);

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
			{
				SpawnInitialClouds();
				GD.Print("main_menu: SpawnInitialClouds complete");
			}

			GD.Print("main_menu: _ready() complete successfully");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"main_menu: CRITICAL ERROR in _ready(): {ex.Message}\n{ex.StackTrace}");
		}
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

	private Texture2D LoadImageTexture(string resPath, bool makeTransparent = false)
	{
		try
		{
			string globalPath = ProjectSettings.GlobalizePath(resPath);
			if (System.IO.File.Exists(globalPath))
			{
				var img = Image.LoadFromFile(globalPath);
				if (img != null)
				{
					if (makeTransparent)
					{
						img = MakeImageTransparent(img, 0.14f);
					}
					return ImageTexture.CreateFromImage(img);
				}
			}

			if (ResourceLoader.Exists(resPath))
			{
				var tex = GD.Load<Texture2D>(resPath);
				if (tex != null) return tex;
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"MainMenu: Failed to load image texture '{resPath}': {ex.Message}");
		}
		return null;
	}

	private Image MakeImageTransparent(Image img, float darkThreshold)
	{
		if (img == null) return null;
		img.Convert(Image.Format.Rgba8);

		int width = img.GetWidth();
		int height = img.GetHeight();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Color color = img.GetPixel(x, y);
				float brightness = Mathf.Max(color.R, Mathf.Max(color.G, color.B));

				if (brightness < darkThreshold)
				{
					img.SetPixel(x, y, new Color(0, 0, 0, 0));
				}
				else if (brightness < darkThreshold + 0.18f)
				{
					float alpha = (brightness - darkThreshold) / 0.18f;
					img.SetPixel(x, y, new Color(color.R, color.G, color.B, alpha));
				}
			}
		}
		return img;
	}

	// ─── Floating title ──────────────────────────────────────────────────────
	private async System.Threading.Tasks.Task SetupFloatingTitleAsync()
	{
		var audioOptions = GetNodeOrNull<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		string langName  = (audioOptions != null) ? (string)audioOptions.Get("selected_language") : "english";
		var languages    = (audioOptions != null) ? (Dictionary)audioOptions.Get("languages") : null;
		
		string localeKey = (languages != null && languages.ContainsKey(Variant.From(langName)))
			? (string)languages[langName] 
			: TranslationServer.GetLocale().ToLower();

		string[] candidates = new string[]
		{
			$"res://assets/images/axiom_force_title_logo_{localeKey}.jpg",
			$"res://assets/images/axiom_force_title_logo_{langName.ToLower()}.jpg",
			"res://assets/images/axiom_force_title_logo.jpg"
		};

		Texture2D titleTexture = null;
		foreach (string path in candidates)
		{
			titleTexture = LoadImageTexture(path, makeTransparent: true);
			if (titleTexture != null) break;
		}

		foreach (Node child in _titleContainer.GetChildren())
			child.QueueFree();

		if (titleTexture != null)
		{
			var logoRect = new TextureRect
			{
				Texture = titleTexture,
				CustomMinimumSize = new Vector2(520, 200),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = Control.SizeFlags.ExpandFill
			};

			_titleContainer.AddChild(logoRect);

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			logoRect.PivotOffset = logoRect.Size / 2f;

			await AnimateFloatingTitleAsync(logoRect);
		}
	}

	private async System.Threading.Tasks.Task AnimateFloatingTitleAsync(Control node)
	{
		node.Scale    = new Vector2(0.1f, 0.1f);
		node.Modulate = node.Modulate with { A = 0f };

		var zoom = CreateTween();
		zoom.SetParallel(true);
		zoom.TweenProperty(node, "scale",      Vector2.One, 1.2)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		zoom.TweenProperty(node, "modulate:a", 1.0f,        0.8)
			.SetEase(Tween.EaseType.Out);
		await ToSignal(zoom, Tween.SignalName.Finished);

		float origY = node.Position.Y;
		StartFloatLoop(node, origY);
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

	// ─── Language dropdown ───────────────────────────────────────────────────
	private void PopulateLanguageButtons()
	{
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		var languages    = (Dictionary)audioOptions.Get("languages");
		string currentLang = (string)audioOptions.Get("selected_language");

		_languageDropdown.Clear();
		_languageDropdown.ItemSelected -= OnLanguageDropdownSelected;
		_languageDropdown.ItemSelected += OnLanguageDropdownSelected;

		string englishKey = "";
		foreach (var key in languages.Keys)
		{
			if ((string)languages[key] == "english") { englishKey = (string)key; break; }
		}
		if (!string.IsNullOrEmpty(englishKey))
			_languageDropdown.AddItem(englishKey);

		var sorted = new List<string>();
		foreach (var key in languages.Keys)
			if ((string)key != englishKey) sorted.Add((string)key);
		sorted.Sort();

		foreach (var langName in sorted)
			_languageDropdown.AddItem(langName);

		if (!string.IsNullOrEmpty(currentLang))
			_languageDropdown.SelectItemByText(currentLang);
	}

	private void OnLanguageDropdownSelected(int index, string langName)
	{
		_pendingSelectedLanguage = langName;
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
	public void _on_play_pressed()
	{
		_newGamePopup.Visible = true;
		MoveChild(_newGamePopup, GetChildCount() - 1);
	}

	public void _on_instructions_pressed()
	{
		UpdateInstructionsAlignment();
		_instructionsPopup.Visible = true;
		MoveChild(_instructionsPopup, GetChildCount() - 1);
	}

	public void _on_settings_pressed() 
	{ 
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		_pendingSelectedLanguage = (string)audioOptions.Get("selected_language");
		if (_languageDropdown != null && !string.IsNullOrEmpty(_pendingSelectedLanguage))
		{
			_languageDropdown.SelectItemByText(_pendingSelectedLanguage);
		}

		_settingsPopup.Visible = true; 
		MoveChild(_settingsPopup, GetChildCount() - 1);
	}

	public void _on_apply_settings_pressed()
	{
		var slider = GetNode<HSlider>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/MasterSlider");
		AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb((float)slider.Value));

		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");

		if (!string.IsNullOrEmpty(_pendingSelectedLanguage))
		{
			audioOptions.Set("selected_language", _pendingSelectedLanguage);
			audioOptions.Call("apply_language_change");
		}

		if (_instructionsPopup.Visible) UpdateInstructionsAlignment();
		_ = SetupFloatingTitleAsync();
		_settingsPopup.Visible = false;
	}

	public void _on_close_settings_pressed()
	{
		_settingsPopup.Visible = false;
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		string currentLang = (string)audioOptions.Get("selected_language");
		_pendingSelectedLanguage = currentLang;
		if (_languageDropdown != null && !string.IsNullOrEmpty(currentLang))
		{
			_languageDropdown.SelectItemByText(currentLang);
		}
	}
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
		MoveChild(_quitPopup, GetChildCount() - 1);
		MoveChild(_quitBackground, GetChildCount() - 2);
	}
	public void _on_close_instructions_pressed()  => _instructionsPopup.Visible = false;
	// Leaderboard popup close handler removed.

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
		MoveChild(_continuePopup, GetChildCount() - 1);
	}

	private void OnDeleteOnePressed(int sessionId)
	{
		_confirmLabel.Text = Tr("Delete this saved game?") + "\n" + Tr("This cannot be undone.");
		_confirmPopup.SetMeta("delete_mode",       "one");
		_confirmPopup.SetMeta("delete_session_id", sessionId);
		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
	}

	private void OnDeleteAllPressed()
	{
		_confirmLabel.Text = Tr("Delete ALL saved games?") + "\n" + Tr("This cannot be undone.");
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

	private void SetupProfileUI()
	{
		var profileContainer = GetNode<Control>("ProfileContainer");

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
		
		_logoutButton.AddThemeFontSizeOverride("font_size", 16);
		_logoutButton.Pressed += OnLogoutPressed;
		_profileDisplayContainer.AddChild(_logoutButton);

		profileContainer.AddChild(_profileDisplayContainer);
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
		}
	}

	private void EnsureUISoundManager()
	{
		if (UISoundManager.Instance == null && !HasNode("UISoundManager"))
		{
			var snd = new UISoundManager { Name = "UISoundManager" };
			GetTree().Root.CallDeferred(Node.MethodName.AddChild, snd);
		}
	}
}
