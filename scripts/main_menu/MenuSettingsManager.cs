using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// MenuSettingsManager handles the system calibration, including audio, language,
/// and display mode settings, as well as providing shared cyber-styling utilities.
/// </summary>
public partial class MenuSettingsManager : Node
{
	private Panel           _settingsPopup;
	private CyberDropdown   _languageDropdown;
	private CyberDropdown   _displayDropdown;
	private string          _pendingSelectedLanguage = "";
	private string          _pendingSelectedDisplayMode = "";

	public void Initialize(Panel settingsPopup, CyberDropdown languageDropdown, CyberDropdown displayDropdown)
	{
		_settingsPopup = settingsPopup;
		_languageDropdown = languageDropdown;
		_displayDropdown = displayDropdown;
	}

	public void OpenSettings()
	{
		if (_settingsPopup == null) return;

		var audioOptions = _settingsPopup.GetNode<Node>("VBoxContainer/AudioOptions");
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
		// coordinator handles MoveChild and GrabFocus
	}

	public void CloseSettings()
	{
		if (_settingsPopup != null)
		{
			_settingsPopup.Visible = false;
			var audioOptions = _settingsPopup.GetNode<Node>("VBoxContainer/AudioOptions");
			string currentLang = (string)audioOptions.Get("selected_language");
			_pendingSelectedLanguage = currentLang;
			if (_languageDropdown != null && !string.IsNullOrEmpty(currentLang))
			{
				_languageDropdown.SelectItemByText(currentLang);
			}
		}
	}

	public void ApplySettings()
	{
		var slider = _settingsPopup.GetNode<HSlider>("VBoxContainer/AudioOptions/VBoxContainer/MasterSlider");
		AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb((float)slider.Value));

		var audioOptions = _settingsPopup.GetNode<Node>("VBoxContainer/AudioOptions");

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

		_settingsPopup.Visible = false;
	}

	public void PopulateLanguageButtons()
	{
		var audioOptions = _settingsPopup.GetNode<Node>("VBoxContainer/AudioOptions");
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

	public void PopulateDisplayModeButtons()
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

	public void UpdateLocalizedTexts(Control root)
	{
		var playBtn = root.GetNodeOrNull<CyberButton>("ButtonContainer/Play");
		if (playBtn != null) { playBtn.MainText = Tr("Play"); playBtn.QueueRedraw(); }

		var contBtn = root.GetNodeOrNull<CyberButton>("ButtonContainer/ContinueButton");
		if (contBtn != null) { contBtn.MainText = Tr("Continue"); contBtn.QueueRedraw(); }

		var instBtn = root.GetNodeOrNull<CyberButton>("ButtonContainer/Instructions");
		if (instBtn != null) { instBtn.MainText = Tr("Instructions"); instBtn.QueueRedraw(); }

		var setBtn = root.GetNodeOrNull<CyberButton>("ButtonContainer/Settings");
		if (setBtn != null) { setBtn.MainText = Tr("Settings"); setBtn.QueueRedraw(); }

		var quitBtn = root.GetNodeOrNull<CyberButton>("ButtonContainer/QuitButton");
		if (quitBtn != null) { quitBtn.MainText = Tr("Quit"); quitBtn.QueueRedraw(); }

		var settingsTitle = _settingsPopup?.GetNodeOrNull<Label>("VBoxContainer/TitleLabel");
		if (settingsTitle != null) settingsTitle.Text = Tr("// SYSTEM CALIBRATION //");

		var applyBtn = _settingsPopup?.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ApplyButton");
		if (applyBtn != null) applyBtn.Text = Tr("Apply");

		var closeBtn = _settingsPopup?.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/CloseButton");
		if (closeBtn != null) closeBtn.Text = Tr("Close");

		var quitMsg = root.GetNodeOrNull<Panel>("QuitPopup")?.GetNodeOrNull<Label>("VBoxContainer/Label");
		if (quitMsg != null) quitMsg.Text = Tr("Are you sure you want to quit?");

		var quitYes = root.GetNodeOrNull<Panel>("QuitPopup")?.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/YesButton");
		if (quitYes != null) quitYes.Text = Tr("Yes");

		var quitNo = root.GetNodeOrNull<Panel>("QuitPopup")?.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton");
		if (quitNo != null) quitNo.Text = Tr("No");
	}

	public void ApplyCyberpunkPanelStyles(Control root)
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

		// ContinuePopup
		var continuePopup = root.GetNodeOrNull<Panel>("ContinuePopup");
		if (continuePopup != null)
		{
			continuePopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var title = continuePopup.GetNodeOrNull<Label>("VBoxContainer/Title");
			if (title != null)
			{
				title.Text = Tr("// OPERATIVE RECORDS MATRIX //");
				title.AddThemeFontOverride("font", orbitronFont);
				title.AddThemeFontSizeOverride("font_size", 24);
				title.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}
			var backBtn = continuePopup.GetNodeOrNull<Button>("VBoxContainer/BackButton");
			if (backBtn != null)
			{
				backBtn.Text = Tr("Back");
				StyleCyberButton(backBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			}
		}

		// ConfirmPopup
		var confirmPopup = root.GetNodeOrNull<Panel>("ConfirmPopup");
		if (confirmPopup != null)
		{
			confirmPopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var confirmLabel = confirmPopup.GetNodeOrNull<Label>("VBoxContainer/ConfirmLabel");
			if (confirmLabel != null)
			{
				confirmLabel.AddThemeFontOverride("font", rajdhaniFont);
				confirmLabel.AddThemeFontSizeOverride("font_size", 22);
				confirmLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f, 1f));
			}
			var yesBtn = confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/YesButton");
			var noBtn  = confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton");
			if (yesBtn != null) StyleCyberButton(yesBtn, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
			if (noBtn != null)  StyleCyberButton(noBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
		}

		// NewGamePopup
		var newGamePopup = root.GetNodeOrNull<Panel>("NewGamePopup");
		if (newGamePopup != null)
		{
			newGamePopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var msg = newGamePopup.GetNodeOrNull<Label>("VBoxContainer/Message");
			if (msg != null)
			{
				msg.AddThemeFontOverride("font", orbitronFont);
				msg.AddThemeFontSizeOverride("font_size", 24);
				msg.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}
			var confirmBtn = newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ConfirmButton");
			var cancelBtn  = newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/CancelButton");
			if (confirmBtn != null) StyleCyberButton(confirmBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			if (cancelBtn != null)  StyleCyberButton(cancelBtn, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
		}

		// SettingsPopup
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

			var controlsBtn = _settingsPopup.GetNodeOrNull<Button>("VBoxContainer/AudioOptions/VBoxContainer/ControlsButton");
			if (controlsBtn != null)
			{
				controlsBtn.Text = Tr("// REBIND CONTROLS");
				StyleCyberButton(controlsBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			}

			var applyBtn = _settingsPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ApplyButton");
			var closeBtn = _settingsPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/CloseButton");
			if (applyBtn != null) StyleCyberButton(applyBtn, orbitronFont, new Color(0f, 0.94f, 1f, 0.85f));
			if (closeBtn != null) StyleCyberButton(closeBtn, orbitronFont, new Color(1f, 0f, 0.5f, 1f));
		}

		// KeybindingsPopup
		var keybindingsPopup = root.GetNodeOrNull<Panel>("KeybindingsPopup");
		if (keybindingsPopup != null)
		{
			keybindingsPopup.AddThemeStyleboxOverride("panel", cyberPanelStyle);
			var keyTitle = keybindingsPopup.GetNodeOrNull<Label>("VBoxContainer/Title");
			if (keyTitle != null)
			{
				keyTitle.Text = Tr("// CUSTOM CONTROLS REBIND //");
				keyTitle.AddThemeFontOverride("font", orbitronFont);
				keyTitle.AddThemeFontSizeOverride("font_size", 24);
				keyTitle.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
			}
		}
	}

	public void StyleCyberButton(Button btn, Font font, Color borderColor)
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

	private void StyleCyberSlider(HSlider slider)
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

	private Texture2D CreateCyberGrabberTexture()
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

	private string Tr(string text) => MainMenu.Tr(text);
}
