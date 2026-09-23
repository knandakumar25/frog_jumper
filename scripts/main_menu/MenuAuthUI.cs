using Godot;
using System;

/// <summary>
/// MenuAuthUI handles the user profile and authentication display in the main menu.
/// </summary>
public partial class MenuAuthUI : Node
{
	private Button          _logoutButton;
	private TextureRect     _avatarRect;
	private Label           _usernameLabel;
	private HBoxContainer   _profileDisplayContainer;
	private Button          _browserLoginButton;

	public void Initialize(Button loginButton, Control profileContainer)
	{
		_browserLoginButton = loginButton;

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
		var user = AuthManager.Instance.CurrentUser;
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

	private string Tr(string text) => MainMenu.Tr(text);
}
