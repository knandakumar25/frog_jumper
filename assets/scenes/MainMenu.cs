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
	private CyberDropdown _displayDropdown;
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
	private string          _pendingSelectedDisplayMode = "";

	// Keybindings management
	private Button          _controlsButton;
	private Panel           _keybindingsPopup;
	private VBoxContainer   _keybindingRowsContainer;
	private Button          _resetDefaultsButton;
	private Button          _closeKeybindingsButton;

	private Panel           _keyDetectModal;
	private Label           _promptLabel;
	private Label           _detectedKeyLabel;
	private Button          _acceptKeyButton;
	private Button          _cancelKeyButton;

	private string          _remappingAction = "";
	private InputEventKey   _proposedKey = null;


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
		GetNodeOrNull<Node>("/root/Global")?.Call("play_menu_music");
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
			_displayDropdown    = GetNodeOrNull<CyberDropdown>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayDropdown");
			GD.Print("main_menu: resolved DisplayDropdown");
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

			_controlsButton          = GetNodeOrNull<Button>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/ControlsButton");
			_keybindingsPopup        = GetNodeOrNull<Panel>("KeybindingsPopup");
			_keybindingRowsContainer = GetNodeOrNull<VBoxContainer>("KeybindingsPopup/VBoxContainer/ScrollContainer/RowsContainer");
			_resetDefaultsButton     = GetNodeOrNull<Button>("KeybindingsPopup/VBoxContainer/ButtonContainer/ResetDefaultsButton");
			_closeKeybindingsButton  = GetNodeOrNull<Button>("KeybindingsPopup/VBoxContainer/ButtonContainer/CloseKeybindingsButton");

			_keyDetectModal          = GetNodeOrNull<Panel>("KeyDetectModal");
			_promptLabel             = GetNodeOrNull<Label>("KeyDetectModal/VBoxContainer/PromptLabel");
			_detectedKeyLabel        = GetNodeOrNull<Label>("KeyDetectModal/VBoxContainer/DetectedKeyLabel");
			_acceptKeyButton         = GetNodeOrNull<Button>("KeyDetectModal/VBoxContainer/ButtonContainer/AcceptButton");
			_cancelKeyButton         = GetNodeOrNull<Button>("KeyDetectModal/VBoxContainer/ButtonContainer/CancelButton");

			if (_controlsButton != null)          _controlsButton.Pressed += _on_controls_button_pressed;
			if (_resetDefaultsButton != null)     _resetDefaultsButton.Pressed += _on_reset_defaults_pressed;
			if (_closeKeybindingsButton != null)  _closeKeybindingsButton.Pressed += _on_close_keybindings_pressed;
			if (_acceptKeyButton != null)         _acceptKeyButton.Pressed += _on_accept_key_pressed;
			if (_cancelKeyButton != null)         _cancelKeyButton.Pressed += _on_cancel_key_pressed;

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
			PopulateDisplayModeButtons();
			GD.Print("main_menu: PopulateDisplayModeButtons complete");

			ApplyCustomCursor();

			_ = SetupFloatingTitleAsync();
			GD.Print("main_menu: SetupFloatingTitleAsync started");

			ResetSpawnTimer();
			GD.Print("main_menu: ResetSpawnTimer complete");

			MoveChild(_titleContainer, GetChildCount() - 1);
			GD.Print("main_menu: MoveChild TitleContainer complete");

			_continueButton.Visible = SessionManager.Instance.HasSessions();
			GD.Print("main_menu: updated _continueButton.Visible");

			ApplyCyberpunkPanelStyles();

			var playBtn = GetNodeOrNull<CyberButton>("ButtonContainer/Play");
			if (playBtn != null)
			{
				playBtn.Pressed += _on_play_pressed;
				playBtn.CallDeferred(Control.MethodName.GrabFocus);
			}
			GetNode<CyberButton>("ButtonContainer/ContinueButton").Pressed += _on_continue_pressed;
			GetNode<CyberButton>("ButtonContainer/Instructions").Pressed += _on_instructions_pressed;
			GetNode<CyberButton>("ButtonContainer/Settings").Pressed += _on_settings_pressed;
			GetNode<CyberButton>("ButtonContainer/QuitButton").Pressed += _on_quit_button_pressed;

			EnsureUISoundManager();
			UISoundManager.AutoWireButtons(this);

			SetupNavPromptFooter();

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

	private void ApplyCustomCursor()
	{
		try
		{
			var cursorTex = LoadImageTexture("res://assets/images/cursor.png");
			if (cursorTex != null)
			{
				Input.SetCustomMouseCursor(cursorTex, Input.CursorShape.Arrow, new Vector2(4, 4));
				GD.Print("main_menu: Custom cyber cursor applied at runtime successfully!");
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"main_menu: Failed to apply custom mouse cursor: {ex.Message}");
		}
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
						img = MakeImageTransparent(img, 0.28f);
					}
					return ImageTexture.CreateFromImage(img);
				}
			}

			if (ResourceLoader.Exists(resPath))
			{
				var tex = GD.Load<Texture2D>(resPath);
				if (tex != null)
				{
					if (makeTransparent)
					{
						var img = tex.GetImage();
						if (img != null)
						{
							img = MakeImageTransparent(img, 0.28f);
							return ImageTexture.CreateFromImage(img);
						}
					}
					return tex;
				}
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"MainMenu: Failed to load image texture '{resPath}': {ex.Message}");
		}
		return null;
	}

	private Image MakeImageTransparent(Image img, float darkThreshold = 0.28f)
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
		if (_titleContainer != null) _titleContainer.Visible = false;
		await System.Threading.Tasks.Task.CompletedTask;
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

	private void PopulateDisplayModeButtons()
	{
		if (_displayDropdown == null) return;
		_displayDropdown.Clear();
		_displayDropdown.ItemSelected -= OnDisplayDropdownSelected;
		_displayDropdown.ItemSelected += OnDisplayDropdownSelected;

		_displayDropdown.AddItem(Tr("Fullscreen"));
		_displayDropdown.AddItem(Tr("Windowed Fullscreen"));
		_displayDropdown.AddItem(Tr("Windowed"));

		var globalNode = GetNodeOrNull<Node>("/root/Global");
		string currentMode = "Fullscreen";
		if (globalNode != null)
		{
			currentMode = (string)globalNode.Get("current_display_mode");
		}
		_pendingSelectedDisplayMode = currentMode;
		_displayDropdown.SelectItemByText(Tr(currentMode));
	}

	private void OnDisplayDropdownSelected(int index, string modeName)
	{
		if (modeName == Tr("Windowed Fullscreen"))
			_pendingSelectedDisplayMode = "Windowed Fullscreen";
		else if (modeName == Tr("Windowed"))
			_pendingSelectedDisplayMode = "Windowed";
		else
			_pendingSelectedDisplayMode = "Fullscreen";
	}

	private void UpdateInstructionsControlsText()
	{
		if (_instructionsPopup == null) return;
		var content = _instructionsPopup.GetNodeOrNull<Container>(
			"VBoxContainer/ScrollContainer/MarginContainer/ContentContainer");
		if (content == null) return;

		var moveLbl  = content.GetNodeOrNull<Label>("ControlMove");
		var jumpLbl  = content.GetNodeOrNull<Label>("ControlJump");
		var pauseLbl = content.GetNodeOrNull<Label>("ControlPause");

		string leftKeys  = GetBoundKeysString("left");
		string rightKeys = GetBoundKeysString("right");
		string jumpKeys  = GetBoundKeysString("jump");
		string pauseKeys = GetBoundKeysString("escape");

		if (moveLbl != null)
			moveLbl.Text = $"{Tr("Move")}: {Tr("Left")} [ {leftKeys} ]  |  {Tr("Right")} [ {rightKeys} ]";
		if (jumpLbl != null)
			jumpLbl.Text = $"{Tr("Jump")}: [ {jumpKeys} ]";
		if (pauseLbl != null)
			pauseLbl.Text = $"{Tr("Pause")}: [ {pauseKeys} ]";
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
				Text                = Tr("No saved games found"),
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			empty.AddThemeFontSizeOverride("font_size", 24);
			empty.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Rajdhani-Medium.ttf"));
			empty.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.95f, 0.8f));
			_sessionsContainer.AddChild(empty);
			return;
		}

		// Header Action Bar: Purge All Records button
		var actionRow    = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		var deleteAllBtn = MakeDeleteButton(Tr("// PURGE ALL RECORDS"));
		deleteAllBtn.Pressed += OnDeleteAllPressed;
		actionRow.AddChild(deleteAllBtn);
		_sessionsContainer.AddChild(actionRow);

		var spacer = new Control { CustomMinimumSize = new Vector2(0, 10) };
		_sessionsContainer.AddChild(spacer);

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		int idx = 1;
		foreach (var item in sessions)
		{
			var session    = (Dictionary)item;
			int  sessionId = session["id"].AsInt32();
			string character  = (string)session["character"];
			int  level     = session["current_level"].AsInt32();
			int  score     = session["total_score"].AsInt32();
			string lastPlayed = SessionManager.Instance.FormatDatetime((string)session["last_played"]);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);

			// Cyber Card Continue button
			string cardTitle = $"[{idx:D2}]  {Tr(character).ToUpper()}   |   {Tr("Level")} {level}   |   {Tr("Score")}: {score}   |   {lastPlayed}";
			var sessionBtn = new Button
			{
				Text                = cardTitle,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize   = new Vector2(0, 68),
				Alignment           = HorizontalAlignment.Left
			};
			sessionBtn.AddThemeFontSizeOverride("font_size", 19);
			sessionBtn.AddThemeFontOverride("font", rajdhaniFont);

			// Normal Style: Dark Cyber Glass with Cyan Border
			var normalBox = new StyleBoxFlat
			{
				BgColor = new Color(0.04f, 0.08f, 0.18f, 0.92f),
				BorderColor = new Color(0f, 0.94f, 1f, 0.70f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
				ContentMarginLeft = 20, ContentMarginRight = 20
			};
			normalBox.SetCornerRadiusAll(8);

			// Hover Style: Glowing Neon Pink Border
			var hoverBox = new StyleBoxFlat
			{
				BgColor = new Color(0.08f, 0.14f, 0.32f, 0.98f),
				BorderColor = new Color(1f, 0f, 0.5f, 1.0f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
				ContentMarginLeft = 20, ContentMarginRight = 20
			};
			hoverBox.SetCornerRadiusAll(8);

			sessionBtn.AddThemeStyleboxOverride("normal",  normalBox);
			sessionBtn.AddThemeStyleboxOverride("hover",   hoverBox);
			sessionBtn.AddThemeStyleboxOverride("pressed", hoverBox);
			sessionBtn.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f, 1f));

			var capturedId   = sessionId;
			var capturedSess = session;
			sessionBtn.Pressed += () => OnSessionSelected(capturedId, capturedSess);
			row.AddChild(sessionBtn);

			// Delete (✕) button
			var delBtn = new Button
			{
				Text              = "✕",
				CustomMinimumSize = new Vector2(60, 68),
			};
			delBtn.AddThemeFontSizeOverride("font_size", 22);
			delBtn.AddThemeFontOverride("font", orbitronFont);

			var delNormal = new StyleBoxFlat
			{
				BgColor = new Color(0.18f, 0.04f, 0.08f, 0.90f),
				BorderColor = new Color(1f, 0f, 0.35f, 0.75f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2
			};
			delNormal.SetCornerRadiusAll(8);

			var delHover = new StyleBoxFlat
			{
				BgColor = new Color(0.35f, 0.06f, 0.12f, 0.98f),
				BorderColor = new Color(1f, 0.2f, 0.45f, 1.0f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2
			};
			delHover.SetCornerRadiusAll(8);

			delBtn.AddThemeStyleboxOverride("normal",  delNormal);
			delBtn.AddThemeStyleboxOverride("hover",   delHover);
			delBtn.AddThemeStyleboxOverride("pressed", delHover);
			delBtn.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.45f, 1f));
			delBtn.Pressed += () => OnDeleteOnePressed(capturedId);
			row.AddChild(delBtn);

			_sessionsContainer.AddChild(row);
			_sessionsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
			idx++;
		}
	}

	private static Button MakeDeleteButton(string text)
	{
		var btn = new Button { Text = text };
		btn.AddThemeFontSizeOverride("font_size", 15);
		btn.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf"));
		
		var style = new StyleBoxFlat
		{
			BgColor = new Color(0.20f, 0.04f, 0.08f, 0.90f),
			BorderColor = new Color(1f, 0f, 0.35f, 0.85f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8
		};
		style.SetCornerRadiusAll(6);
		
		btn.AddThemeStyleboxOverride("normal", style);
		btn.AddThemeStyleboxOverride("hover",  style);
		btn.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.5f, 1f));
		return btn;
	}

	private void ApplyCyberpunkPanelStyles()
	{
		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		var cyberPanelStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.08f, 0.18f, 0.95f),
			BorderColor = new Color(0f, 0.94f, 1f, 0.85f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 24, ContentMarginBottom = 24
		};
		cyberPanelStyle.SetCornerRadiusAll(12);

		// Apply to ContinuePopup
		if (_continuePopup != null)
		{
			_continuePopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var title = _continuePopup.GetNodeOrNull<Label>("VBoxContainer/Title");
			if (title != null)
			{
				title.Text = Tr("// OPERATIVE RECORDS MATRIX //");
				title.AddThemeFontOverride("font", orbitronFont);
				title.AddThemeFontSizeOverride("font_size", 24);
				title.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}
			var backBtn = _continuePopup.GetNodeOrNull<Button>("VBoxContainer/BackButton");
			if (backBtn != null)
			{
				backBtn.Text = Tr("Back");
				if (!backBtn.IsConnected(Button.SignalName.Pressed, Callable.From(OnContinueBackPressed)))
				{
					backBtn.Pressed += OnContinueBackPressed;
				}
				StyleCyberButton(backBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			}
		}

		// Apply to ConfirmPopup
		if (_confirmPopup != null)
		{
			_confirmPopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			if (_confirmLabel != null)
			{
				_confirmLabel.AddThemeFontOverride("font", rajdhaniFont);
				_confirmLabel.AddThemeFontSizeOverride("font_size", 22);
				_confirmLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f, 1f));
			}
			var yesBtn = _confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/YesButton");
			var noBtn  = _confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton");
			if (yesBtn != null) StyleCyberButton(yesBtn, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
			if (noBtn != null)  StyleCyberButton(noBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
		}

		// Apply to NewGamePopup
		if (_newGamePopup != null)
		{
			_newGamePopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var msg = _newGamePopup.GetNodeOrNull<Label>("VBoxContainer/Message");
			if (msg != null)
			{
				msg.AddThemeFontOverride("font", orbitronFont);
				msg.AddThemeFontSizeOverride("font_size", 24);
				msg.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}
			var confirmBtn = _newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ConfirmButton");
			var cancelBtn  = _newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/CancelButton");
			if (confirmBtn != null) StyleCyberButton(confirmBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			if (cancelBtn != null)  StyleCyberButton(cancelBtn, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
		}

		// Apply to SettingsPopup
		if (_settingsPopup != null)
		{
			_settingsPopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var settingsTitle = _settingsPopup.GetNodeOrNull<Label>("VBoxContainer/Title");
			if (settingsTitle != null)
			{
				settingsTitle.Text = Tr("// SYSTEM CALIBRATION //");
				settingsTitle.AddThemeFontOverride("font", orbitronFont);
				settingsTitle.AddThemeFontSizeOverride("font_size", 24);
				settingsTitle.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}

			var volLbl  = _settingsPopup.GetNodeOrNull<Label>("VBoxContainer/AudioOptions/VBoxContainer/VolumeLabel");
			var langLbl = _settingsPopup.GetNodeOrNull<Label>("VBoxContainer/AudioOptions/VBoxContainer/LanguageLabel");
			var dispLbl = _settingsPopup.GetNodeOrNull<Label>("VBoxContainer/AudioOptions/VBoxContainer/DisplayLabel");
			if (volLbl != null)
			{
				volLbl.Text = Tr("Volume").ToUpper() + " [ 100% ]";
				volLbl.AddThemeFontOverride("font", orbitronFont);
				volLbl.AddThemeFontSizeOverride("font_size", 16);
				volLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f, 0.9f));
			}
			if (langLbl != null)
			{
				langLbl.Text = Tr("Language").ToUpper();
				langLbl.AddThemeFontOverride("font", orbitronFont);
				langLbl.AddThemeFontSizeOverride("font_size", 16);
				langLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f, 0.9f));
			}
			if (dispLbl != null)
			{
				dispLbl.Text = Tr("Display Mode").ToUpper();
				dispLbl.AddThemeFontOverride("font", orbitronFont);
				dispLbl.AddThemeFontSizeOverride("font_size", 16);
				dispLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f, 0.9f));
			}

			var slider = _settingsPopup.GetNodeOrNull<HSlider>("VBoxContainer/AudioOptions/VBoxContainer/MasterSlider");
			if (slider != null)
			{
				StyleCyberSlider(slider);
			}

			if (_controlsButton != null)
			{
				_controlsButton.Text = Tr("// REBIND CONTROLS");
				StyleCyberButton(_controlsButton, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			}

			var applyBtn = _settingsPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ApplyButton");
			var closeBtn = _settingsPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/CloseButton");
			if (applyBtn != null) StyleCyberButton(applyBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			if (closeBtn != null) StyleCyberButton(closeBtn, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
		}

		// Apply to KeybindingsPopup
		if (_keybindingsPopup != null)
		{
			_keybindingsPopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var keyTitle = _keybindingsPopup.GetNodeOrNull<Label>("VBoxContainer/Title");
			if (keyTitle != null)
			{
				keyTitle.Text = Tr("// CUSTOM CONTROLS REBIND //");
				keyTitle.AddThemeFontOverride("font", orbitronFont);
				keyTitle.AddThemeFontSizeOverride("font_size", 24);
				keyTitle.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}

			if (_resetDefaultsButton != null)
			{
				_resetDefaultsButton.Text = Tr("Reset Defaults");
				StyleCyberButton(_resetDefaultsButton, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			}
			if (_closeKeybindingsButton != null)
			{
				_closeKeybindingsButton.Text = Tr("Close");
				StyleCyberButton(_closeKeybindingsButton, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
			}
		}

		var statusTicker = GetNodeOrNull<Label>("StatusTicker");
		if (statusTicker != null)
		{
			statusTicker.Text = Tr("AXIOM NET // STATUS: ONLINE // SYSTEM READY");
		}
	}

	private static void StyleCyberSlider(HSlider slider)
	{
		var trackNormal = new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.08f, 0.18f, 0.95f),
			BorderColor = new Color(0f, 0.94f, 1f, 0.65f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			ContentMarginTop = 6, ContentMarginBottom = 6
		};
		trackNormal.SetCornerRadiusAll(4);

		var trackHighlight = new StyleBoxFlat
		{
			BgColor = new Color(0f, 0.85f, 1f, 0.90f),
			BorderColor = new Color(1f, 0f, 0.5f, 1.0f),
			BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
			ContentMarginTop = 6, ContentMarginBottom = 6
		};
		trackHighlight.SetCornerRadiusAll(4);

		slider.AddThemeStyleboxOverride("slider", trackNormal);
		slider.AddThemeStyleboxOverride("grabber_area", trackHighlight);
		slider.AddThemeStyleboxOverride("grabber_area_highlight", trackHighlight);

		var grabberTex = CreateCyberGrabberTexture();
		slider.AddThemeIconOverride("grabber", grabberTex);
		slider.AddThemeIconOverride("grabber_highlight", grabberTex);
	}

	private static Texture2D CreateCyberGrabberTexture()
	{
		int w = 20, h = 28;
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		img.Fill(new Color(0, 0, 0, 0));

		Color cyan = new Color(0f, 0.94f, 1f, 1f);
		Color pink = new Color(1f, 0f, 0.5f, 1f);
		Color bg   = new Color(0.04f, 0.08f, 0.22f, 1f);

		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				int dx = Math.Abs(x - w / 2);
				int dy = Math.Abs(y - h / 2);
				if (dx + dy / 2 <= w / 2 - 1)
				{
					if (dx + dy / 2 == w / 2 - 1 || x == 0 || x == w - 1 || y == 0 || y == h - 1)
					{
						img.SetPixel(x, y, cyan);
					}
					else if (dx <= 1 && dy <= 3)
					{
						img.SetPixel(x, y, pink);
					}
					else
					{
						img.SetPixel(x, y, bg);
					}
				}
			}
		}
		return ImageTexture.CreateFromImage(img);
	}

	private static void StyleCyberButton(Button btn, Font font, Color borderColor)
	{
		btn.AddThemeFontOverride("font", font);
		btn.AddThemeFontSizeOverride("font_size", 18);

		var normal = new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.10f, 0.22f, 0.90f),
			BorderColor = borderColor,
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2
		};
		normal.SetCornerRadiusAll(8);

		var hover = new StyleBoxFlat
		{
			BgColor = new Color(0.12f, 0.18f, 0.38f, 0.98f),
			BorderColor = new Color(1f, 0f, 0.5f, 1.0f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2
		};
		hover.SetCornerRadiusAll(8);

		btn.AddThemeStyleboxOverride("normal",  normal);
		btn.AddThemeStyleboxOverride("hover",   hover);
		btn.AddThemeStyleboxOverride("pressed", hover);
		btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f, 1f));
	}

	// ─── Signal / button handlers ────────────────────────────────────────────
	private void CloseAllPopups()
	{
		if (_newGamePopup != null)      _newGamePopup.Visible = false;
		if (_continuePopup != null)     _continuePopup.Visible = false;
		if (_instructionsPopup != null) _instructionsPopup.Visible = false;
		if (_settingsPopup != null)     _settingsPopup.Visible = false;
		if (_quitPopup != null)         _quitPopup.Visible = false;
		if (_quitBackground != null)    _quitBackground.Visible = false;
		if (_confirmPopup != null)      _confirmPopup.Visible = false;
		if (_keybindingsPopup != null) _keybindingsPopup.Visible = false;
		if (_keyDetectModal != null)   _keyDetectModal.Visible = false;

		GetNodeOrNull<CyberButton>("ButtonContainer/Play")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private bool HasAnyOpenPopup()
	{
		return (_newGamePopup != null && _newGamePopup.Visible) ||
			   (_continuePopup != null && _continuePopup.Visible) ||
			   (_instructionsPopup != null && _instructionsPopup.Visible) ||
			   (_settingsPopup != null && _settingsPopup.Visible) ||
			   (_quitPopup != null && _quitPopup.Visible) ||
			   (_confirmPopup != null && _confirmPopup.Visible) ||
			   (_keybindingsPopup != null && _keybindingsPopup.Visible) ||
			   (_keyDetectModal != null && _keyDetectModal.Visible);
	}

	public void _on_play_pressed()
	{
		CloseAllPopups();
		_newGamePopup.Visible = true;
		MoveChild(_newGamePopup, GetChildCount() - 1);
		_newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ConfirmButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public void _on_instructions_pressed()
	{
		CloseAllPopups();
		UpdateInstructionsControlsText();
		UpdateInstructionsAlignment();
		_instructionsPopup.Visible = true;
		MoveChild(_instructionsPopup, GetChildCount() - 1);
		_instructionsPopup.GetNodeOrNull<Button>("VBoxContainer/CloseButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public void _on_settings_pressed() 
	{ 
		CloseAllPopups();
		var audioOptions = GetNode<Node>("SettingsPopup/VBoxContainer/AudioOptions");
		_pendingSelectedLanguage = (string)audioOptions.Get("selected_language");
		if (_languageDropdown != null && !string.IsNullOrEmpty(_pendingSelectedLanguage))
		{
			_languageDropdown.SelectItemByText(_pendingSelectedLanguage);
		}

		if (_displayDropdown != null)
		{
			var globalNode = GetNodeOrNull<Node>("/root/Global");
			string currentMode = (globalNode != null) ? (string)globalNode.Get("current_display_mode") : "Fullscreen";
			_pendingSelectedDisplayMode = currentMode;
			_displayDropdown.SelectItemByText(Tr(currentMode));
		}

		_settingsPopup.Visible = true; 
		MoveChild(_settingsPopup, GetChildCount() - 1);
		_settingsPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ApplyButton")?.CallDeferred(Control.MethodName.GrabFocus);
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

		if (!string.IsNullOrEmpty(_pendingSelectedDisplayMode))
		{
			var globalNode = GetNodeOrNull<Node>("/root/Global");
			if (globalNode != null)
			{
				globalNode.Call("apply_display_mode", _pendingSelectedDisplayMode);
			}
		}

		if (_instructionsPopup.Visible) UpdateInstructionsAlignment();
		_ = SetupFloatingTitleAsync();
		_settingsPopup.Visible = false;

		ApplyCyberpunkPanelStyles();
		QueueRedrawAllButtons(this);
	}

	private void QueueRedrawAllButtons(Node node)
	{
		if (node is Control c) c.QueueRedraw();
		foreach (Node child in node.GetChildren())
		{
			QueueRedrawAllButtons(child);
		}
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
		CloseAllPopups();
		_quitBackground.Visible = true;
		_quitPopup.Visible = true;
		MoveChild(_quitPopup, GetChildCount() - 1);
		MoveChild(_quitBackground, GetChildCount() - 2);
		_quitPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton")?.CallDeferred(Control.MethodName.GrabFocus);
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
		CloseAllPopups();
		PopulateSessions();
		_continuePopup.Visible = true;
		MoveChild(_continuePopup, GetChildCount() - 1);
	}

	private void OnContinueBackPressed()
	{
		if (_continuePopup != null)
		{
			_continuePopup.Visible = false;
		}
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

	// ─── Keybindings Remap System ───────────────────────────────────────────
	private void PopulateKeybindingRows()
	{
		if (_keybindingRowsContainer == null) return;
		foreach (Node child in _keybindingRowsContainer.GetChildren())
			child.QueueFree();

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		var actions = new (string key, string title)[]
		{
			("left",   Tr("Move Left")),
			("right",  Tr("Move Right")),
			("jump",   Tr("Jump")),
			("escape", Tr("Pause Game"))
		};

		foreach (var (actionKey, actionTitle) in actions)
		{
			var row = new HBoxContainer();
			row.CustomMinimumSize = new Vector2(0, 56);
			row.AddThemeConstantOverride("separation", 16);

			var nameLbl = new Label
			{
				Text = actionTitle.ToUpper(),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				VerticalAlignment = VerticalAlignment.Center
			};
			nameLbl.AddThemeFontOverride("font", orbitronFont);
			nameLbl.AddThemeFontSizeOverride("font_size", 16);
			nameLbl.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			row.AddChild(nameLbl);

			string keyDisplay = GetBoundKeysString(actionKey);
			var keysLbl = new Label
			{
				Text = $"[ {keyDisplay} ]",
				CustomMinimumSize = new Vector2(240, 0),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			keysLbl.AddThemeFontOverride("font", rajdhaniFont);
			keysLbl.AddThemeFontSizeOverride("font_size", 18);
			keysLbl.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f, 0.90f));
			row.AddChild(keysLbl);

			var rebindBtn = new Button
			{
				Text = Tr("Rebind").ToUpper(),
				CustomMinimumSize = new Vector2(120, 44)
			};
			StyleCyberButton(rebindBtn, orbitronFont, new Color(1f, 0f, 0.5f, 0.9f));
			string capturedKey = actionKey;
			string capturedTitle = actionTitle;
			rebindBtn.Pressed += () => OpenKeyDetectModal(capturedKey, capturedTitle);
			row.AddChild(rebindBtn);

			_keybindingRowsContainer.AddChild(row);
			_keybindingRowsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
	}

	private string GetBoundKeysString(string actionName)
	{
		if (!InputMap.HasAction(actionName)) return Tr("None");
		var events = InputMap.ActionGetEvents(actionName);
		var keyNames = new List<string>();
		foreach (var ev in events)
		{
			if (ev is InputEventKey keyEv)
			{
				string keyStr = OS.GetKeycodeString(keyEv.PhysicalKeycode);
				if (string.IsNullOrEmpty(keyStr)) keyStr = OS.GetKeycodeString(keyEv.Keycode);
				if (!string.IsNullOrEmpty(keyStr) && !keyNames.Contains(keyStr))
					keyNames.Add(keyStr);
			}
		}
		return keyNames.Count > 0 ? string.Join(" / ", keyNames) : Tr("None");
	}

	private void OpenKeyDetectModal(string actionKey, string actionTitle)
	{
		_remappingAction = actionKey;
		_proposedKey = null;
		GetViewport().GuiReleaseFocus();

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		if (_promptLabel != null)
		{
			_promptLabel.Text = $"{Tr("PRESS ANY KEY TO REBIND")} [{actionTitle.ToUpper()}]...";
			_promptLabel.AddThemeFontOverride("font", orbitronFont);
			_promptLabel.AddThemeFontSizeOverride("font_size", 18);
			_promptLabel.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
		}
		if (_detectedKeyLabel != null)
		{
			_detectedKeyLabel.Text = $"[ {Tr("AWAITING KEY INPUT...")} ]";
			_detectedKeyLabel.AddThemeFontOverride("font", rajdhaniFont);
			_detectedKeyLabel.AddThemeFontSizeOverride("font_size", 22);
			_detectedKeyLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.95f, 0.8f));
		}
		if (_acceptKeyButton != null)
		{
			_acceptKeyButton.FocusMode = FocusModeEnum.None;
			_acceptKeyButton.Disabled = true;
			StyleCyberButton(_acceptKeyButton, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
		}
		if (_cancelKeyButton != null)
		{
			_cancelKeyButton.FocusMode = FocusModeEnum.None;
			StyleCyberButton(_cancelKeyButton, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
		}

		if (_keyDetectModal != null)
		{
			_keyDetectModal.Visible = true;
			MoveChild(_keyDetectModal, GetChildCount() - 1);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_keyDetectModal != null && _keyDetectModal.Visible)
		{
			if (@event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo)
			{
				_proposedKey = keyEv;
				string keyName = OS.GetKeycodeString(keyEv.PhysicalKeycode);
				if (string.IsNullOrEmpty(keyName)) keyName = OS.GetKeycodeString(keyEv.Keycode);

				if (_detectedKeyLabel != null)
				{
					_detectedKeyLabel.Text = $"{Tr("PROPOSED KEY")}: [ {keyName.ToUpper()} ]";
					_detectedKeyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f, 1f));
				}
				if (_acceptKeyButton != null)
				{
					_acceptKeyButton.Disabled = false;
				}
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (@event.IsActionPressed("ui_cancel") && HasAnyOpenPopup())
		{
			CloseAllPopups();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void _on_controls_button_pressed()
	{
		CloseAllPopups();
		PopulateKeybindingRows();
		if (_keybindingsPopup != null)
		{
			_keybindingsPopup.Visible = true;
			MoveChild(_keybindingsPopup, GetChildCount() - 1);
		}
	}

	private void _on_close_keybindings_pressed()
	{
		if (_keybindingsPopup != null) _keybindingsPopup.Visible = false;
		if (_keyDetectModal != null)   _keyDetectModal.Visible = false;
		if (_settingsPopup != null)
		{
			_settingsPopup.Visible = true;
			MoveChild(_settingsPopup, GetChildCount() - 1);
		}
	}

	private void _on_accept_key_pressed()
	{
		if (_proposedKey != null && !string.IsNullOrEmpty(_remappingAction))
		{
			InputMap.ActionEraseEvents(_remappingAction);
			InputMap.ActionAddEvent(_remappingAction, _proposedKey);

			var globalNode = GetNodeOrNull<Node>("/root/Global");
			if (globalNode != null)
			{
				globalNode.Call("save_custom_keybindings");
			}
			PopulateKeybindingRows();
		}
		if (_keyDetectModal != null) _keyDetectModal.Visible = false;
	}

	private void _on_cancel_key_pressed()
	{
		_proposedKey = null;
		if (_keyDetectModal != null) _keyDetectModal.Visible = false;
	}

	private void _on_reset_defaults_pressed()
	{
		InputMap.LoadFromProjectSettings();
		var globalNode = GetNodeOrNull<Node>("/root/Global");
		if (globalNode != null)
		{
			globalNode.Call("save_custom_keybindings");
		}
		PopulateKeybindingRows();
	}

	private Label _navPromptLabel;

	private void SetupNavPromptFooter()
	{
		var footerContainer = new MarginContainer
		{
			Name = "NavPromptFooter",
			CustomMinimumSize = new Vector2(0, 48)
		};
		footerContainer.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		footerContainer.AddThemeConstantOverride("margin_bottom", 16);

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
		AddChild(footerContainer);

		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null)
		{
			global.Connect("input_device_changed", Callable.From<bool>((isGamepad) => UpdateNavPromptFooter()));
		}

		UpdateNavPromptFooter();
	}

	private void UpdateNavPromptFooter()
	{
		if (_navPromptLabel == null) return;
		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null && global.HasMethod("get_nav_prompt"))
		{
			_navPromptLabel.Text = (string)global.Call("get_nav_prompt");
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GetNodeOrNull<Node>("/root/Global")?.Call("stop_menu_music");
	}
}
