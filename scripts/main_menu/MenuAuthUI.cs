using Godot;
using System;

/// <summary>
/// MenuAuthUI handles the user profile and authentication display in the main menu.
/// </summary>
public partial class MenuAuthUI : Node
{
	private CyberButton _logoutButton;
	private TextureRect     _avatarRect;
	private Label           _usernameLabel;
	private Label           _headerLabel;
	private Label           _statusLabel;
	private HBoxContainer   _profileDisplayContainer;
	private Button          _browserLoginButton;

	public void Initialize(Button loginButton, Control profileContainer)
	{
		_browserLoginButton = loginButton;

		// 1. Create Styled Cyber-Frame Panel
		var frame = new Panel
		{
			Name = "ProfileFrame",
			CustomMinimumSize = new Vector2(300, 80),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};

		var frameStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.08f, 0.18f, 0.92f),
			BorderColor = new Color(0f, 0.94f, 1f, 0.85f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12
		};
		frameStyle.SetCornerRadiusAll(8);
		frame.AddThemeStyleboxOverride("panel", frameStyle);

		// 2. Header Label "USER IDENTIFIED"
		var header = new Label
		{
			Text = Tr("// USER IDENTIFIED"),
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_headerLabel = header;
		header.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf"));
		header.AddThemeFontSizeOverride("font_size", 12);
		header.AddThemeColorOverride("font_color", new Color(0f, 0.94f, 1f, 0.6f));
		frame.AddChild(header);

		// 3. Profile Content Row
		_profileDisplayContainer = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Begin,
			Visible = false
		};
		frame.AddChild(_profileDisplayContainer);

		// 3a. Avatar Rect with Neon Border
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

		// 3b. Info Container (Username + Online Light)
		var infoContainer = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		// Online Indicator Light
		var onlineLight = new ColorRect
		{
			CustomMinimumSize = new Vector2(8, 8),
			Color = new Color(0, 1, 0, 1),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
		};

		var statusRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
		statusRow.AddChild(onlineLight);
		var statusLbl = new Label { Text = " " + Tr("ONLINE"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_statusLabel = statusLbl;
		statusLbl.AddThemeFontSizeOverride("font_size", 10);
		statusLbl.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf"));
		statusLbl.AddThemeColorOverride("font_color", new Color(0, 1, 0, 0.8f));
		statusRow.AddChild(statusLbl);
		infoContainer.AddChild(statusRow);

		_usernameLabel = new Label
		{
			Text = "",
			CustomMinimumSize = new Vector2(100, 20)
		};
		_usernameLabel.AddThemeColorOverride("font_color", new Color(0, 0.95f, 1f, 1f)); // Neon cyan
		_usernameLabel.AddThemeFontSizeOverride("font_size", 18);
		_usernameLabel.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf"));
		infoContainer.AddChild(_usernameLabel);
		_profileDisplayContainer.AddChild(infoContainer);

		// 3c. Logout button (using CyberButton for consistency)
		_logoutButton = new CyberButton
		{
			MainText = "LOGOUT",
			SubText = "// TERMINATE SESSION",
			IndexText = "00",
			CustomMinimumSize = new Vector2(120, 44),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		_logoutButton.Pressed += OnLogoutPressed;
		_profileDisplayContainer.AddChild(_logoutButton);

		profileContainer.AddChild(frame);

		if (AuthManager.Instance != null)
		{
			AuthManager.Instance.OnProfileUpdated += UpdateAuthUI;
		}

		UpdateAuthUI();
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
		}
		else
		{
			if (_profileDisplayContainer != null)
				_profileDisplayContainer.Visible = false;
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
