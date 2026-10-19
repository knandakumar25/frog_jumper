using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// MainMenu acts as the central coordinator for the main menu UI.
/// It delegates specific responsibilities to specialized manager components:
/// - MenuBackgroundManager: Atmospheric visuals.
/// - MenuSettingsManager: System settings and styling.
/// - MenuInputManager: Key-rebinding system.
/// - MenuAuthUI: User profile and authentication.
/// - MenuSessionUI: Saved game session management.
/// </summary>
public partial class MainMenu : Control
{
	// ─── Coordinator References ────────────────────────────────────────────────
	private MenuBackgroundManager _backgroundManager;
	private MenuSettingsManager   _settingsManager;
	private MenuInputManager      _inputManager;
	private MenuAuthUI            _authUI;
	private MenuSessionUI        _sessionUI;

	// ─── Local UI References (for coordinating popups) ──────────────────────────
	private Panel       _quitPopup;
	private ColorRect   _quitBackground;
	private Panel       _instructionsPopup;
	private HBoxContainer _titleContainer;
	private Button       _browserLoginButton;
	private Label       _navPromptLabel;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		GD.Print("main_menu: _ready() start");
		GetNodeOrNull<Node>("/root/Global")?.Call("play_menu_music");

		try
		{
			// 1. Resolve root-level UI references.
			_quitPopup          = GetNode<Panel>("QuitPopup");
			_quitBackground     = GetNode<ColorRect>("QuitBackground");
			_instructionsPopup  = GetNode<Panel>("InstructionsPopup");
			_titleContainer     = GetNode<HBoxContainer>("TitleContainer");
			_browserLoginButton = GetNodeOrNull<Button>("ButtonContainer/LoginButton"); // Adjust path if needed

			// 2. Instantiate and Initialize Managers.
			InitializeManagers();

			// 3. General Setup.
			ApplyCustomCursor();
			_titleContainer.Visible = true;

			MoveChild(_titleContainer, GetChildCount() - 1);

			// Update continue button visibility based on session data.
			var continueBtn = GetNodeOrNull<Control>("ButtonContainer/ContinueButton");
			if (continueBtn != null)
			{
				continueBtn.Visible = SessionManager.Instance?.HasSessions() ?? false;
			}

			// Initial styling pass.
			_settingsManager.ApplyCyberpunkPanelStyles(this);

			// Wire up primary menu buttons.
			var playBtn = GetNodeOrNull<CyberButton>("ButtonContainer/Play");
			if (playBtn != null)
			{
				playBtn.Pressed += _on_play_pressed;
				playBtn.CallDeferred(Control.MethodName.GrabFocus);
			}
			var contBtn = GetNodeOrNull<CyberButton>("ButtonContainer/ContinueButton");
			if (contBtn != null) contBtn.Pressed += _on_continue_pressed;

			var instBtn = GetNodeOrNull<CyberButton>("ButtonContainer/Instructions");
			if (instBtn != null) instBtn.Pressed += _on_instructions_pressed;

			var setBtn = GetNodeOrNull<CyberButton>("ButtonContainer/Settings");
			if (setBtn != null) setBtn.Pressed += _on_settings_pressed;

			var quitBtn = GetNodeOrNull<CyberButton>("ButtonContainer/QuitButton");
			if (quitBtn != null) quitBtn.Pressed += _on_quit_button_pressed;

			// Wire popup sub-buttons.
			WireButtonSignal(_quitPopup, "VBoxContainer/ButtonContainer/YesButton", _on_yes_pressed);
			WireButtonSignal(_quitPopup, "VBoxContainer/ButtonContainer/NoButton", _on_no_pressed);

			// Session confirmation buttons are handled inside MenuSessionUI.
			// Settings/Instructions/NewGame buttons handled by their respective managers.
			WireButtonSignal(_instructionsPopup, "VBoxContainer/CloseButton", _on_close_instructions_pressed);

			EnsureUISoundManager();
			UISoundManager.AutoWireButtons(this);
			SetupNavPromptFooter();

			GD.Print("main_menu: _ready() complete successfully");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"main_menu: CRITICAL ERROR in _ready(): {ex.Message}\n{ex.StackTrace}");
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationTranslationChanged)
		{
			UpdateLocalization();
		}
	}

	private void InitializeManagers()
	{
		// Background Manager.
		_backgroundManager = new MenuBackgroundManager { Name = "MenuBackgroundManager" };
		AddChild(_backgroundManager);

		// Settings Manager.
		_settingsManager = new MenuSettingsManager { Name = "MenuSettingsManager" };
		AddChild(_settingsManager);
		_settingsManager.Initialize(
			GetNode<Panel>("SettingsPopup"),
			GetNode<CyberDropdown>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown"),
			GetNodeOrNull<CyberDropdown>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayDropdown")
		);
		_settingsManager.PopulateLanguageButtons();
		_settingsManager.PopulateDisplayModeButtons();

		// Input Manager.
		_inputManager = new MenuInputManager { Name = "MenuInputManager" };
		AddChild(_inputManager);
		_inputManager.Initialize(
			GetNodeOrNull<Button>("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/ControlsButton"),
			GetNodeOrNull<Panel>("KeybindingsPopup"),
			GetNodeOrNull<VBoxContainer>("KeybindingsPopup/VBoxContainer/ScrollContainer/RowsContainer"),
			GetNodeOrNull<Button>("KeybindingsPopup/VBoxContainer/ButtonContainer/ResetDefaultsButton"),
			GetNodeOrNull<Button>("KeybindingsPopup/VBoxContainer/ButtonContainer/CloseKeybindingsButton"),
			GetNodeOrNull<Panel>("KeyDetectModal"),
			GetNodeOrNull<Label>("KeyDetectModal/VBoxContainer/PromptLabel"),
			GetNodeOrNull<Label>("KeyDetectModal/VBoxContainer/DetectedKeyLabel"),
			GetNodeOrNull<Button>("KeyDetectModal/VBoxContainer/ButtonContainer/AcceptButton"),
			GetNodeOrNull<Button>("KeyDetectModal/VBoxContainer/ButtonContainer/CancelButton"), _settingsManager
		);

		// Auth UI.
		_authUI = new MenuAuthUI { Name = "MenuAuthUI" };
		AddChild(_authUI);
		_authUI.Initialize(_browserLoginButton, GetNode<Control>("ProfileContainer"));

		// Session UI.
		_sessionUI = new MenuSessionUI { Name = "MenuSessionUI" };
		AddChild(_sessionUI);
		_sessionUI.Initialize(
			GetNode<Panel>("NewGamePopup"),
			GetNode<Panel>("ContinuePopup"),
			GetNode<VBoxContainer>("ContinuePopup/VBoxContainer/ScrollContainer/SessionsContainer"),
			GetNode<Control>("ButtonContainer/ContinueButton"),
			GetNode<Panel>("ConfirmPopup"),
			GetNode<Label>("ConfirmPopup/VBoxContainer/ConfirmLabel")
		);
	}

	// ─── Global Utilities ────────────────────────────────────────────────────────
	public static string Tr(string text) => TranslationServer.Translate(text);

	private void WireButtonSignal(Node parent, string nodePath, Action callback)
	{
		if (parent == null) return;
		var btn = parent.GetNodeOrNull<Button>(nodePath);
		if (btn != null)
		{
			var callable = Callable.From(callback);
			if (!btn.IsConnected(Button.SignalName.Pressed, callable))
			{
				btn.Pressed += callback;
			}
		}
	}

	public void CloseAllPopups()
	{
		_settingsManager.CloseSettings();
		_inputManager.CloseControls();
		_sessionUI.CloseAll();

		if (_instructionsPopup != null) _instructionsPopup.Visible = false;
		if (_quitPopup != null)         _quitPopup.Visible = false;
		if (_quitBackground != null)    _quitBackground.Visible = false;

		RestoreMainMenuFocus();
	}

	public void RestoreMainMenuFocus()
	{
		GetNodeOrNull<CyberButton>("ButtonContainer/Play")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	// ─── Event Handlers ──────────────────────────────────────────────────────────
	public void _on_play_pressed()
	{
		CloseAllPopups();
		_sessionUI.OpenNewGame();
	}

	public void _on_continue_pressed()
	{
		CloseAllPopups();
		_sessionUI.OpenContinue();
	}

	public void _on_settings_pressed()
	{
		CloseAllPopups();
		_settingsManager.OpenSettings();
	}

	public void _on_instructions_pressed()
	{
		CloseAllPopups();
		UpdateInstructionsUI();
		_instructionsPopup.Visible = true;
		MoveChild(_instructionsPopup, GetChildCount() - 1);
		_instructionsPopup.GetNodeOrNull<Button>("VBoxContainer/CloseButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void UpdateInstructionsUI()
	{
		var content = _instructionsPopup.GetNodeOrNull<Container>("VBoxContainer/ScrollContainer/MarginContainer/ContentContainer");
		if (content == null) return;

		var moveLbl  = content.GetNodeOrNull<Label>("ControlMove");
		var jumpLbl  = content.GetNodeOrNull<Label>("ControlJump");
		var pauseLbl = content.GetNodeOrNull<Label>("ControlPause");

		_inputManager.UpdateInstructionsControlsText(moveLbl, jumpLbl, pauseLbl);
		UpdateInstructionsAlignment();
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

		var content = _instructionsPopup.GetNode<Container>("VBoxContainer/ScrollContainer/MarginContainer/ContentContainer");
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

	public void _on_quit_button_pressed()
	{
		CloseAllPopups();
		_quitBackground.Visible = true;
		_quitPopup.Visible = true;
		MoveChild(_quitPopup, GetChildCount() - 1);
		MoveChild(_quitBackground, GetChildCount() - 2);
		_quitPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public void _on_yes_pressed() => GetTree().Quit();
	public void _on_no_pressed()
	{
		_quitPopup.Visible = false;
		_quitBackground.Visible = false;
		RestoreMainMenuFocus();
	}

	public void _on_close_instructions_pressed()
	{
		_instructionsPopup.Visible = false;
		RestoreMainMenuFocus();
	}

	public void _on_apply_settings_pressed()
	{
		_settingsManager?.ApplySettings();
		UpdateLocalization();
		RestoreMainMenuFocus();
	}

	public virtual void UpdateLocalization()
	{
		_settingsManager?.UpdateLocalizedTexts(this);
		_authUI?.UpdateLocalizedTexts();
		UpdateLocalizedTitle();

		var global = GetNodeOrNull<Node>("/root/Global");
		if (_navPromptLabel != null && global != null && global.HasMethod("get_nav_prompt"))
		{
			_navPromptLabel.Text = (string)global.Call("get_nav_prompt");
		}
	}

	public void _on_close_settings_pressed()
	{
		_settingsManager?.CloseSettings();
		RestoreMainMenuFocus();
	}

	public void _on_confirm_yes_pressed() => _sessionUI?.ConfirmAction();
	public void _on_confirm_no_pressed() => _sessionUI?.CancelAction();
	public void _on_new_game_confirm_pressed() => _sessionUI?.ConfirmNewGame();
	public void _on_new_game_cancel_pressed()
	{
		_sessionUI?.CancelNewGame();
		RestoreMainMenuFocus();
	}
	public void _on_continue_back_pressed()
	{
		_sessionUI?.CloseAll();
		RestoreMainMenuFocus();
	}

	public virtual void UpdateLocalizedTitle()
	{
	}

	public override void _Input(InputEvent @event)
	{
		// Completely eliminate mouse cursor compatibility
		if (@event is InputEventMouse)
		{
			GetViewport()?.SetInputAsHandled();
			return;
		}

		_inputManager.HandleInput(@event);

		if (@event.IsActionPressed("ui_cancel"))
		{
			CloseAllPopups();
			GetViewport()?.SetInputAsHandled();
		}
	}

	// ─── Visuals ────────────────────────────────────────────────────────────────
	private void ApplyCustomCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
	}

	private Texture2D LoadImageTexture(string resPath)
	{
		if (ResourceLoader.Exists(resPath))
		{
			return GD.Load<Texture2D>(resPath);
		}
		return null;
	}

	private void SetupNavPromptFooter()
	{
		var footerContainer = new MarginContainer
		{
			Name = "NavPromptFooter",
			CustomMinimumSize = new Vector2(0, 48)
		};
		footerContainer.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		footerContainer.AddThemeConstantOverride("margin_bottom", 16);

		var navPromptLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		if (orbitronFont != null) navPromptLabel.AddThemeFontOverride("font", orbitronFont);
		navPromptLabel.AddThemeFontSizeOverride("font_size", 16);
		navPromptLabel.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.85f));

		_navPromptLabel = navPromptLabel;
		footerContainer.AddChild(navPromptLabel);
		AddChild(footerContainer);

		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null)
		{
			global.Connect("input_device_changed", Callable.From<bool>((isGamepad) => {
				if (navPromptLabel != null)
					navPromptLabel.Text = (string)global.Call("get_nav_prompt");
			}));
		}

		if (global != null && global.HasMethod("get_nav_prompt"))
			navPromptLabel.Text = (string)global.Call("get_nav_prompt");
	}

	private void EnsureUISoundManager()
	{
		if (UISoundManager.Instance == null && !HasNode("UISoundManager"))
		{
			var snd = new UISoundManager { Name = "UISoundManager" };
			GetTree().Root.CallDeferred(Node.MethodName.AddChild, snd);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GetNodeOrNull<Node>("/root/Global")?.Call("stop_menu_music");
	}
}
