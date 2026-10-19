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
	private ColorRect   _fadeOverlay;

	private bool  _isTransitioning = false;
	private float _pulseTimer = 0f;

	public override void _Ready()
	{
		GetNodeOrNull<Node>("/root/Global")?.Call("play_home_music");

		_titleLogo    = GetNodeOrNull<TextureRect>("CenterContainer/VBoxContainer/TitleLogo");
		_promptLabel  = GetNodeOrNull<Label>("CenterContainer/VBoxContainer/PromptLabel");
		_versionLabel = GetNodeOrNull<Label>("BottomMarginContainer/VersionLabel");
		_fadeOverlay  = GetNodeOrNull<ColorRect>("FadeOverlay");

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		// Load localized or default high-contrast 2D title logo image
		if (_titleLogo != null)
		{
			string resPath = TitleLogo.GetLocalizedTitle2DPath();
			var tex = GD.Load<Texture2D>(resPath);
			if (tex != null)
			{
				_titleLogo.Texture = tex;
			}
			else
			{
				string globalPath = ProjectSettings.GlobalizePath(resPath);
				if (System.IO.File.Exists(globalPath))
				{
					var img = Image.LoadFromFile(globalPath);
					if (img != null) _titleLogo.Texture = ImageTexture.CreateFromImage(img);
				}
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
			_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			_fadeOverlay.Visible = true;
			_fadeOverlay.Modulate = new Color(1, 1, 1, 1);
			var tween = CreateTween();
			tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, 0.6f);
			tween.TweenCallback(Callable.From(() =>
			{
				if (_fadeOverlay != null)
				{
					_fadeOverlay.Visible = false;
					_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
				}
			}));
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
			_fadeOverlay.Visible = true;
			_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
			var tween = CreateTween();
			tween.TweenProperty(_fadeOverlay, "modulate:a", 1.0f, 0.4f);
			await ToSignal(tween, Tween.SignalName.Finished);
		}

		// Transition into Main Menu
		GetTree().ChangeSceneToFile("res://scenes/main_menu/main_menu.tscn");
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
