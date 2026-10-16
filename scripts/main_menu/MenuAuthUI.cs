using Godot;
using System;

/// <summary>
/// MenuAuthUI handles the user profile and authentication display in the main menu.
/// </summary>
public partial class MenuAuthUI : Node
{
	private CyberButton     _logoutButton;
	private TextureRect     _avatarRect;
	private Label           _usernameLabel;
	private Label           _headerLabel;
	private Label           _statusLabel;
	private Label           _guestUnitLabel;
	private Label           _guestStatusLabel;
	private HBoxContainer   _profileDisplayContainer;
	private VBoxContainer   _guestDisplayContainer;
	private Button          _browserLoginButton;

	public void Initialize(Button loginButton, Control profileContainer)
	{
		_browserLoginButton = loginButton;

		// 1. Create GoHUD-Styled Chamfered Cyber-Frame PanelContainer
		var frame = new PanelContainer
		{
			Name = "ProfileFrame",
			CustomMinimumSize = new Vector2(320, 78),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		StyleBox panelStyle = CreateGoHudCutStyleBox();
		frame.AddThemeStyleboxOverride("panel", panelStyle);

		var rootVBox = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		rootVBox.AddThemeConstantOverride("separation", 6);
		frame.AddChild(rootVBox);

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		// 2. Top Header Row ("// USER IDENTIFIED" + "[SEC: OMEGA]")
		var headerRow = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		rootVBox.AddChild(headerRow);

		var header = new Label
		{
			Text = Tr("// USER IDENTIFIED"),
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_headerLabel = header;
		if (orbitronFont != null) header.AddThemeFontOverride("font", orbitronFont);
		header.AddThemeFontSizeOverride("font_size", 11);
		header.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.78f));
		headerRow.AddChild(header);

		var secBadge = new Label
		{
			Text = "[SEC: OMEGA]",
			HorizontalAlignment = HorizontalAlignment.Right
		};
		if (orbitronFont != null) secBadge.AddThemeFontOverride("font", orbitronFont);
		secBadge.AddThemeFontSizeOverride("font_size", 10);
		secBadge.AddThemeColorOverride("font_color", new Color(1.0f, 0.16f, 0.58f, 0.85f));
		headerRow.AddChild(secBadge);

		// 3. Authenticated Profile Content Row
		_profileDisplayContainer = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Begin,
			Visible = false
		};
		_profileDisplayContainer.AddThemeConstantOverride("separation", 10);
		rootVBox.AddChild(_profileDisplayContainer);

		// 3a. Avatar Rect with Neon Border
		_avatarRect = new TextureRect
		{
			CustomMinimumSize = new Vector2(44, 44),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		var avatarBorder = new PanelContainer
		{
			CustomMinimumSize = new Vector2(48, 48),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		var borderStyle = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.5f),
			BorderColor = new Color(0.96f, 0.08f, 0.56f, 1f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2
		};
		avatarBorder.AddThemeStyleboxOverride("panel", borderStyle);
		avatarBorder.AddChild(_avatarRect);
		_profileDisplayContainer.AddChild(avatarBorder);

		// 3b. Info Container (Username + Online Light)
		var infoContainer = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		var onlineLight = new ColorRect
		{
			CustomMinimumSize = new Vector2(8, 8),
			Color = new Color(0, 1, 0.5f, 1),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		var statusRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
		statusRow.AddChild(onlineLight);
		var statusLbl = new Label { Text = " " + Tr("ONLINE"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_statusLabel = statusLbl;
		statusLbl.AddThemeFontSizeOverride("font_size", 11);
		if (rajdhaniFont != null) statusLbl.AddThemeFontOverride("font", rajdhaniFont);
		statusLbl.AddThemeColorOverride("font_color", new Color(0, 1, 0.5f, 0.9f));
		statusRow.AddChild(statusLbl);
		infoContainer.AddChild(statusRow);

		_usernameLabel = new Label
		{
			Text = "",
			CustomMinimumSize = new Vector2(100, 20)
		};
		_usernameLabel.AddThemeColorOverride("font_color", new Color(0, 0.95f, 1f, 1f));
		_usernameLabel.AddThemeFontSizeOverride("font_size", 18);
		if (rajdhaniFont != null) _usernameLabel.AddThemeFontOverride("font", rajdhaniFont);
		infoContainer.AddChild(_usernameLabel);
		_profileDisplayContainer.AddChild(infoContainer);

		// 3c. Logout button
		_logoutButton = new CyberButton
		{
			MainText = "LOGOUT",
			SubText = "// TERMINATE SESSION",
			IndexText = "00",
			CustomMinimumSize = new Vector2(115, 42),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		_logoutButton.Pressed += OnLogoutPressed;
		_profileDisplayContainer.AddChild(_logoutButton);

		// 4. Local / Standby Operative Telemetry Display (shown when not logged into web profile)
		_guestDisplayContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Visible = true
		};
		_guestDisplayContainer.AddThemeConstantOverride("separation", 2);
		rootVBox.AddChild(_guestDisplayContainer);

		var guestRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
		guestRow.AddThemeConstantOverride("separation", 8);
		var pulseDot = new ColorRect
		{
			CustomMinimumSize = new Vector2(8, 8),
			Color = new Color(0f, 0.95f, 1f, 0.95f),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		guestRow.AddChild(pulseDot);

		_guestUnitLabel = new Label
		{
			Text = "OPERATIVE: CITADEL-UNIT-07",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		if (rajdhaniFont != null) _guestUnitLabel.AddThemeFontOverride("font", rajdhaniFont);
		_guestUnitLabel.AddThemeFontSizeOverride("font_size", 16);
		_guestUnitLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.98f, 1.0f, 0.95f));
		guestRow.AddChild(_guestUnitLabel);
		_guestDisplayContainer.AddChild(guestRow);

		_guestStatusLabel = new Label
		{
			Text = "STATUS: LOCAL COMMAND LINK // ACTIVE"
		};
		if (orbitronFont != null) _guestStatusLabel.AddThemeFontOverride("font", orbitronFont);
		_guestStatusLabel.AddThemeFontSizeOverride("font_size", 10);
		_guestStatusLabel.AddThemeColorOverride("font_color", new Color(0f, 0.9f, 0.65f, 0.85f));
		_guestDisplayContainer.AddChild(_guestStatusLabel);

		profileContainer.AddChild(frame);

		if (AuthManager.Instance != null)
		{
			AuthManager.Instance.OnProfileUpdated += UpdateAuthUI;
		}

		UpdateAuthUI();
	}

	private static StyleBox CreateGoHudCutStyleBox()
	{
		var cutScript = GD.Load<GDScript>("res://addons/gohud/widgets/go_stylebox_cut.gd");
		if (cutScript != null)
		{
			var instance = cutScript.New().As<StyleBox>();
			if (instance != null)
			{
				instance.Set("bg_color", new Color(0.02f, 0.055f, 0.13f, 0.88f));
				instance.Set("border_color", new Color(0f, 0.88f, 1f, 0.78f));
				instance.Set("border_width", 1.5f);
				instance.Set("cut", 12.0f);
				instance.Set("cut_corners", 5); // TOP_LEFT (1) | BOTTOM_RIGHT (4)
				instance.Set("edge_color", new Color(1.0f, 0.12f, 0.56f, 0.95f));
				instance.Set("edge_width", 2.5f);
				instance.Set("edge_side", 0); // SIDE_LEFT
				instance.Set("glow_color", new Color(0f, 0.88f, 1f, 0.2f));
				instance.Set("glow_size", 5.0f);
				instance.ContentMarginLeft = 14;
				instance.ContentMarginRight = 14;
				instance.ContentMarginTop = 10;
				instance.ContentMarginBottom = 10;
				return instance;
			}
		}

		var fallback = new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.08f, 0.18f, 0.92f),
			BorderColor = new Color(0f, 0.94f, 1f, 0.85f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 10, ContentMarginBottom = 10
		};
		fallback.SetCornerRadiusAll(8);
		return fallback;
	}

	private void OnLogoutPressed()
	{
		AuthManager.Instance.Logout();
		UpdateAuthUI();
	}

	private void UpdateAuthUI()
	{
		var user = AuthManager.Instance?.CurrentUser;
		if (user != null)
		{
			if (_browserLoginButton != null)
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

			if (_profileDisplayContainer != null)
				_profileDisplayContainer.Visible = true;
			if (_guestDisplayContainer != null)
				_guestDisplayContainer.Visible = false;
		}
		else
		{
			if (_profileDisplayContainer != null)
				_profileDisplayContainer.Visible = false;
			if (_guestDisplayContainer != null)
				_guestDisplayContainer.Visible = true;
		}
	}

	public void UpdateLocalizedTexts()
	{
		if (_headerLabel != null)
		{
			_headerLabel.Text = Tr("// USER IDENTIFIED");
		}
		if (_statusLabel != null)
		{
			_statusLabel.Text = " " + Tr("ONLINE");
		}
		if (_logoutButton != null)
		{
			_logoutButton.UpdateLocalization();
		}
	}

	private string Tr(string text) => MainMenu.Tr(text);
}
