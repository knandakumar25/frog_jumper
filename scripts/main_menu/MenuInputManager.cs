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
	private InputEventKey   _proposedKey = null;

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

	private static void ConnectButtonSafely(Button btn, Action action)
	{
		if (btn == null) return;
		var callable = Callable.From(action);
		if (!btn.IsConnected(BaseButton.SignalName.Pressed, callable))
		{
			btn.Connect(BaseButton.SignalName.Pressed, callable);
		}
	}

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

	public void CloseControls()
	{
		if (_keybindingsPopup != null) _keybindingsPopup.Visible = false;
		if (_keyDetectModal != null)   _keyDetectModal.Visible = false;
		_controlsButton?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public void PopulateKeybindingRows(MenuSettingsManager settingsManager)
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
			if (settingsManager != null)
				settingsManager.StyleCyberButton(rebindBtn, orbitronFont, new Color(1f, 0f, 0.5f, 0.9f));
			string capturedKey = actionKey;
			string capturedTitle = actionTitle;
			rebindBtn.Pressed += () => OpenKeyDetectModal(capturedKey, capturedTitle);
			row.AddChild(rebindBtn);

			_keybindingRowsContainer.AddChild(row);
			_keybindingRowsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
	}

	public string GetBoundKeysString(string actionName)
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

	public void HandleInput(InputEvent @event)
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
			}
		}
	}

	public void AcceptKey()
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
		}
		if (_keyDetectModal != null) _keyDetectModal.Visible = false;
		PopulateKeybindingRows(_settingsManager);
	}

	public void CancelKey()
	{
		_proposedKey = null;
		if (_keyDetectModal != null) _keyDetectModal.Visible = false;
	}

	public void ResetDefaults()
	{
		InputMap.LoadFromProjectSettings();
		var globalNode = GetNodeOrNull<Node>("/root/Global");
		if (globalNode != null)
		{
			globalNode.Call("save_custom_keybindings");
		}
		PopulateKeybindingRows(_settingsManager);
	}

	public void UpdateInstructionsControlsText(Label moveLbl, Label jumpLbl, Label pauseLbl)
	{
		if (moveLbl == null || jumpLbl == null || pauseLbl == null) return;

		string leftKeys  = GetBoundKeysString("left");
		string rightKeys = GetBoundKeysString("right");
		string jumpKeys  = GetBoundKeysString("jump");
		string pauseKeys = GetBoundKeysString("escape");

		moveLbl.Text = $"{Tr("Move")}: {Tr("Left")} [ {leftKeys} ]  |  {Tr("Right")} [ {rightKeys} ]";
		jumpLbl.Text = $"{Tr("Jump")}: [ {jumpKeys} ]";
		pauseLbl.Text = $"{Tr("Pause")}: [ {pauseKeys} ]";
	}

	private string Tr(string text) => MainMenu.Tr(text);
}
