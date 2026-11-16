using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// MenuInputManager handles the key-rebinding system and the input detection modal.
/// </summary>
public partial class MenuInputManager : Node
{
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
	private MenuSettingsManager _settingsManager;
	private string          _remappingAction = "";
	private string          _remappingTitle = "";
	private InputEventKey   _proposedKey = null;

	/// <summary>Stores menu controls and connects each capture action once.</summary>
	public void Initialize(Button controlsButton, Panel keybindingsPopup, VBoxContainer rowsContainer, Button resetBtn, Button closeBtn, Panel detectModal, Label promptLbl, Label detectedLbl, Button acceptBtn, Button cancelBtn, MenuSettingsManager settingsManager)
	{
		_controlsButton = controlsButton;
		_keybindingsPopup = keybindingsPopup;
		_keybindingRowsContainer = rowsContainer;
		_resetDefaultsButton = resetBtn;
		_closeKeybindingsButton = closeBtn;
		_keyDetectModal = detectModal;
		_promptLabel = promptLbl;
		_detectedKeyLabel = detectedLbl;
		_acceptKeyButton = acceptBtn;
		_cancelKeyButton = cancelBtn;
		_settingsManager = settingsManager;

		ConnectButtonSafely(_controlsButton, OpenControls);
		ConnectButtonSafely(_resetDefaultsButton, ResetDefaults);
		ConnectButtonSafely(_closeKeybindingsButton, CloseControls);
		ConnectButtonSafely(_acceptKeyButton, AcceptKey);
		ConnectButtonSafely(_cancelKeyButton, CancelKey);
	}

	/// <summary>Connects a supplied button callback unless that exact callback is already connected.</summary>
	private static void ConnectButtonSafely(Button btn, Action action)
	{
		if (btn == null) return;
		var callable = Callable.From(action);
		if (!btn.IsConnected(BaseButton.SignalName.Pressed, callable))
		{
			btn.Connect(BaseButton.SignalName.Pressed, callable);
		}
	}

	/// <summary>Builds current binding rows and raises the controls popup above other panels.</summary>
	public void OpenControls()
	{
		PopulateKeybindingRows(_settingsManager);
		if (_keybindingsPopup != null)
		{
			_keybindingsPopup.Visible = true;
			var parent = _keybindingsPopup.GetParent();
			if (parent != null)
			{
				parent.MoveChild(_keybindingsPopup, parent.GetChildCount() - 1);
			}
			_closeKeybindingsButton?.CallDeferred(Control.MethodName.GrabFocus);
		}
	}

	/// <summary>Hides controls and capture overlays and restores focus to the controls button.</summary>
	public void CloseControls()
	{
		if (_keybindingsPopup != null) _keybindingsPopup.Visible = false;
		if (_keyDetectModal != null)   _keyDetectModal.Visible = false;
		_controlsButton?.CallDeferred(Control.MethodName.GrabFocus);
	}

	/// <summary>Builds localized remap rows and the persisted prompt-style selector.</summary>
	public void PopulateKeybindingRows(MenuSettingsManager settingsManager)
	{
		if (_keybindingRowsContainer == null) return;
		foreach (Node child in _keybindingRowsContainer.GetChildren())
			child.QueueFree();

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		var actions = new (string key, string title)[]
		{
			("left",   "Move Left"),
			("right",  "Move Right"),
			("jump",   "Jump"),
			("escape", "Pause Game")
		};

		foreach (var (actionKey, actionTitle) in actions)
		{
			var row = new HBoxContainer();
			row.CustomMinimumSize = new Vector2(0, 56);
			row.AddThemeConstantOverride("separation", 16);

			var nameLbl = new Label
			{
				Text = Tr(actionTitle),
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
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
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
				Text = Tr("Rebind"),
				CustomMinimumSize = new Vector2(120, 44)
			};
			if (settingsManager != null)
				settingsManager.StyleCyberButton(rebindBtn, orbitronFont, new Color(1f, 0f, 0.5f, 0.9f));
			string capturedKey = actionKey;
			string capturedTitle = actionTitle;
			rebindBtn.Pressed += () => OpenKeyDetectModal(capturedKey, capturedTitle);
			row.AddChild(rebindBtn);
			InputPromptBinding.Track(row, () => {
				nameLbl.Text = Tr(actionTitle);
				keysLbl.Text = $"[ {GetBoundKeysString(actionKey)} ]";
				rebindBtn.Text = Tr("Rebind");
			});

			_keybindingRowsContainer.AddChild(row);
			_keybindingRowsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}

		var styleRow = new HBoxContainer();
		var styleLabel = new Label { Text = Tr("Input prompt style"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		styleRow.AddChild(styleLabel);
		var style = new OptionButton { CustomMinimumSize = new Vector2(240, 44) };
		foreach (string name in new[] { "Automatic", "Xbox", "PlayStation", "Nintendo", "Keyboard" }) style.AddItem(Tr(name));
		style.Select((int)(InputPromptService.Instance?.Style ?? ControllerGlyphStyle.AutoDetect));
		style.ItemSelected += value => InputPromptService.Instance?.SetStyle((int)value);
		styleRow.AddChild(style);
		InputPromptBinding.Track(styleRow, () => {
			styleLabel.Text = Tr("Input prompt style");
			string[] names = { "Automatic", "Xbox", "PlayStation", "Nintendo", "Keyboard" };
			for (int i = 0; i < names.Length; i++) style.SetItemText(i, Tr(names[i]));
			style.Select((int)(InputPromptService.Instance?.Style ?? ControllerGlyphStyle.AutoDetect));
		});
		_keybindingRowsContainer.AddChild(styleRow);
	}

	/// <summary>Formats keyboard bindings for one remappable action, retaining an Unbound fallback.</summary>
	public string GetBoundKeysString(string actionName)
	{
		return InputPromptService.Instance?.Binding(actionName, false) ?? Tr("Unbound");
	}

	/// <summary>Opens key capture while retaining the source title for later locale changes.</summary>
	private void OpenKeyDetectModal(string actionKey, string actionTitle)
	{
		_remappingAction = actionKey;
		_remappingTitle = actionTitle;
		_proposedKey = null;
		GetViewport().GuiReleaseFocus();

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		if (_promptLabel != null)
		{
			RefreshRemapPrompt();
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
			_acceptKeyButton.FocusMode = Control.FocusModeEnum.None;
			_acceptKeyButton.Disabled = true;
		}
		if (_cancelKeyButton != null)
		{
			_cancelKeyButton.FocusMode = Control.FocusModeEnum.None;
		}

		if (_keyDetectModal != null)
		{
			_keyDetectModal.Visible = true;
			var parent = _keyDetectModal.GetParent();
			if (parent != null)
			{
				parent.MoveChild(_keyDetectModal, parent.GetChildCount() - 1);
			}
		}
	}

	/// <summary>Retranslates the open capture prompt without replacing a proposed key.</summary>
	private void RefreshRemapPrompt()
	{
		if (_promptLabel != null)
			_promptLabel.Text = $"{Tr("PRESS ANY KEY TO REBIND")} [{Tr(_remappingTitle)}]...";
	}

	/// <summary>Updates the open modal when the language changes in place.</summary>
	public override void _Notification(int what)
	{
		if (what == NotificationTranslationChanged && IsInsideTree() && _keyDetectModal?.Visible == true)
			RefreshRemapPrompt();
	}

	/// <summary>Captures a fresh key in the open modal and consumes it before normal navigation.</summary>
	public void HandleInput(InputEvent @event)
	{
		if (_keyDetectModal != null && _keyDetectModal.Visible)
		{
			if (@event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo)
			{
				_proposedKey = keyEv;
				string keyName = InputPromptService.Instance?.FormatBindingEvent(keyEv) ?? keyEv.AsText();

				if (_detectedKeyLabel != null)
				{
					_detectedKeyLabel.Text = $"{Tr("PROPOSED KEY")}: [ {keyName} ]";
					_detectedKeyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f, 1f));
				}
				if (_acceptKeyButton != null)
				{
					_acceptKeyButton.Disabled = false;
				}
				GetViewport().SetInputAsHandled();
			}
		}
	}

	/// <summary>Applies the proposed keyboard binding, saves through Global and refreshes rows.</summary>
	public void AcceptKey()
	{
		if (_proposedKey != null && !string.IsNullOrEmpty(_remappingAction))
		{
			InputPromptService.Instance?.ReplaceKeyboardBinding(_remappingAction, _proposedKey);

			var globalNode = GetNodeOrNull<Node>("/root/Global");
			if (globalNode != null)
			{
				globalNode.Call("save_custom_keybindings");
			}
		}
		if (_keyDetectModal != null) _keyDetectModal.Visible = false;
		PopulateKeybindingRows(_settingsManager);
	}

	/// <summary>Discards the proposed key and hides its capture panel without changing bindings.</summary>
	public void CancelKey()
	{
		_proposedKey = null;
		if (_keyDetectModal != null) _keyDetectModal.Visible = false;
	}

	/// <summary>Restores all action defaults, persists keyboard settings and rebuilds rows.</summary>
	public void ResetDefaults()
	{
		InputPromptService.Instance?.ResetDefaults();
		var globalNode = GetNodeOrNull<Node>("/root/Global");
		if (globalNode != null)
		{
			globalNode.Call("save_custom_keybindings");
		}
		PopulateKeybindingRows(_settingsManager);
	}

	/// <summary>Updates instruction labels from the shared active-device bindings.</summary>
	public void UpdateInstructionsControlsText(Label moveLbl, Label jumpLbl, Label pauseLbl)
	{
		if (moveLbl == null || jumpLbl == null || pauseLbl == null) return;

		string leftKeys  = InputPromptBinding.Binding("left");
		string rightKeys = InputPromptBinding.Binding("right");
		string jumpKeys  = InputPromptBinding.Binding("jump");
		string pauseKeys = InputPromptBinding.Binding("escape");

		moveLbl.Text = $"{Tr("Move")}: {Tr("Left")} [ {leftKeys} ]  |  {Tr("Right")} [ {rightKeys} ]";
		jumpLbl.Text = $"{Tr("Jump")}: [ {jumpKeys} ]";
		pauseLbl.Text = $"{Tr("Pause")}: [ {pauseKeys} ]";
	}

	/// <summary>Uses the main menu translation policy for source text.</summary>
	private string Tr(string text) => MainMenu.Tr(text);
}
