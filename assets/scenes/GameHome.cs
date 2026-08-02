using Godot;
using System;

/// <summary>
/// Game Landing / Home Screen shown before the player enters the Main Menu.
/// Displays AAA Cyberpunk title visuals and waits for any key press or click to transition.
/// </summary>
public partial class GameHome : Control
{
	private TextureRect _titleLogo;
	private Label       _promptLabel;
	private Label       _versionLabel;
	private TextureRect _bgTexture;
	private ColorRect   _fadeOverlay;

	private bool  _isTransitioning = false;
	private float _pulseTimer = 0f;

	public override void _Ready()
	{
		GetNodeOrNull<Node>("/root/Global")?.Call("play_home_music");

		_bgTexture    = GetNodeOrNull<TextureRect>("Background");
		_titleLogo    = GetNodeOrNull<TextureRect>("CenterContainer/VBoxContainer/TitleLogo");
		_promptLabel  = GetNodeOrNull<Label>("CenterContainer/VBoxContainer/PromptLabel");
		_versionLabel = GetNodeOrNull<Label>("BottomMarginContainer/VersionLabel");
		_fadeOverlay  = GetNodeOrNull<ColorRect>("FadeOverlay");

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		// Load background texture if missing
		if (_bgTexture != null && _bgTexture.Texture == null)
		{
			string bgPath = ProjectSettings.GlobalizePath("res://assets/images/training_dojo_background.png");
			if (System.IO.File.Exists(bgPath))
			{
				var img = Image.LoadFromFile(bgPath);
				if (img != null) _bgTexture.Texture = ImageTexture.CreateFromImage(img);
			}
		}

		// Load localized or default title logo image
		if (_titleLogo != null)
		{
			string lang = TranslationServer.GetLocale().ToLower();
			string logoFile = "axiom_force_title_logo.jpg";

			if (lang.Contains("ar")) logoFile = "axiom_force_title_logo_arabic.jpg";
			else if (lang.Contains("pt")) logoFile = "axiom_force_title_logo_brazilianportuguese.jpg";
			else if (lang.Contains("fr")) logoFile = "axiom_force_title_logo_eurofrench.jpg";
			else if (lang.Contains("de")) logoFile = "axiom_force_title_logo_german.jpg";
			else if (lang.Contains("he")) logoFile = "axiom_force_title_logo_hebrew.jpg";
			else if (lang.Contains("it")) logoFile = "axiom_force_title_logo_italian.jpg";
			else if (lang.Contains("ja")) logoFile = "axiom_force_title_logo_japanese.jpg";
			else if (lang.Contains("ko")) logoFile = "axiom_force_title_logo_korean.jpg";
			else if (lang.Contains("mandarin") || lang == "zh_cn") logoFile = "axiom_force_title_logo_mandarin.jpg";
			else if (lang.Contains("traditionalchinese") || lang.Contains("tw") || lang.Contains("hk") || lang == "zh") logoFile = "axiom_force_title_logo_traditionalchinese.jpg";
			else if (lang.Contains("indonesian") || lang.Contains("id")) logoFile = "axiom_force_title_logo_indonesian.jpg";
			else if (lang.Contains("vietnamese") || lang.Contains("vi")) logoFile = "axiom_force_title_logo_vietnamese.jpg";
			else if (lang.Contains("thai") || lang.Contains("th")) logoFile = "axiom_force_title_logo_thai.jpg";
			else if (lang.Contains("pl")) logoFile = "axiom_force_title_logo_polish.jpg";
			else if (lang.Contains("es")) logoFile = "axiom_force_title_logo_spanish.jpg";
			else if (lang.Contains("tr")) logoFile = "axiom_force_title_logo_turkish.jpg";

			string resPath = "res://assets/images/" + logoFile;
			string globalPath = ProjectSettings.GlobalizePath(resPath);
			if (System.IO.File.Exists(globalPath))
			{
				var img = Image.LoadFromFile(globalPath);
				if (img != null) _titleLogo.Texture = ImageTexture.CreateFromImage(img);
			}
		}

		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null)
		{
			global.Connect("input_device_changed", Callable.From<bool>(OnInputDeviceChanged));
		}

		UpdatePromptLabel();

		if (_versionLabel != null)
		{
			_versionLabel.Text = $"v1.0.4  |  {Tr("RESISTANCE PROTOCOL ACTIVE")}  |  {Tr("AXIOM FORCE")}";
			if (rajdhaniFont != null) _versionLabel.AddThemeFontOverride("font", rajdhaniFont);
			_versionLabel.AddThemeFontSizeOverride("font_size", 14);
			_versionLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.95f, 0.6f));
		}

		// Fade in from black overlay
		if (_fadeOverlay != null)
		{
			_fadeOverlay.Visible = true;
			_fadeOverlay.Modulate = new Color(1, 1, 1, 1);
			var tween = CreateTween();
			tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, 0.6f);
		}
	}

	public override void _Process(double delta)
	{
		if (_isTransitioning) return;

		// Pulse prompt text opacity smoothly
		_pulseTimer += (float)delta;
		if (_promptLabel != null)
		{
			float alpha = 0.35f + 0.65f * (Mathf.Sin(_pulseTimer * 3.5f) * 0.5f + 0.5f);
			_promptLabel.Modulate = new Color(1f, 1f, 1f, alpha);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_isTransitioning) return;

		bool isGamepadInput = (@event is InputEventJoypadButton joyBtn && joyBtn.Pressed) ||
							  (@event is InputEventJoypadMotion joyMot && Mathf.Abs(joyMot.AxisValue) > 0.5f);

		if ((@event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo) ||
			(@event is InputEventMouseButton mouseEv && mouseEv.Pressed) ||
			isGamepadInput)
		{
			TriggerStartTransition();
			GetViewport().SetInputAsHandled();
		}
	}

	private async void TriggerStartTransition()
	{
		_isTransitioning = true;

		// Play UI click and transition sounds if available
		UISoundManager.Instance?.PlayClickSound();
		UISoundManager.Instance?.PlayTransitionSound();

		// Flash prompt text
		if (_promptLabel != null)
		{
			_promptLabel.Modulate = new Color(1f, 0f, 0.5f, 1f);
			_promptLabel.Text = Tr("// SYSTEM INITIALIZED //");
		}

		// Fade out black overlay
		if (_fadeOverlay != null)
		{
			var tween = CreateTween();
			tween.TweenProperty(_fadeOverlay, "modulate:a", 1.0f, 0.4f);
			await ToSignal(tween, Tween.SignalName.Finished);
		}

		// Transition into Main Menu
		GetTree().ChangeSceneToFile("res://assets/scenes/main_menu.tscn");
	}

	private void OnInputDeviceChanged(bool isGamepad)
	{
		UpdatePromptLabel();
	}

	private void UpdatePromptLabel()
	{
		if (_promptLabel == null) return;
		var global = GetNodeOrNull<Node>("/root/Global");
		if (global != null && global.HasMethod("get_home_prompt"))
		{
			_promptLabel.Text = (string)global.Call("get_home_prompt");
		}
		else
		{
			_promptLabel.Text = Tr("// PRESS ANY KEY OR CLICK TO INITIALIZE SYSTEM //");
		}
		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		if (orbitronFont != null) _promptLabel.AddThemeFontOverride("font", orbitronFont);
		_promptLabel.AddThemeFontSizeOverride("font_size", 20);
		_promptLabel.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.95f));
	}
}
