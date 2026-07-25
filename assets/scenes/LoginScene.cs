using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public partial class LoginScene : Control
{
	private Label _title;
	private LineEdit _emailInput;
	private LineEdit _usernameInput;
	private LineEdit _passwordInput;
	private Button _submitButton;
	private Button _websiteButton;
	private Button _toggleModeButton;
	private Label _errorLabel;
	private OptionButton _languageSelector;

	// Modal Network Error Popup
	private Control _networkErrorPopup;
	private Label _popupTitle;
	private Label _popupMessage;
	private Button _popupOkButton;

	private bool _isSignupMode = false;
	private readonly List<string> _localeCodes = new();
	private const string TARGET_WEBSITE_URL = "https://axiomforcereclaimearth.vercel.app";

	public override void _Ready()
	{
		// Resolve node references
		_title = GetNode<Label>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/Title");
		_emailInput = GetNode<LineEdit>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/EmailInput");
		_usernameInput = GetNode<LineEdit>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/UsernameInput");
		_passwordInput = GetNode<LineEdit>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/PasswordInput");
		_submitButton = GetNode<Button>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/SubmitButton");
		_websiteButton = GetNode<Button>("WebsiteButton");
		_toggleModeButton = GetNode<Button>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/ToggleModeButton");
		_errorLabel = GetNode<Label>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/ErrorLabel");
		_languageSelector = GetNode<OptionButton>("LanguageSelector");

		// Popup node references
		_networkErrorPopup = GetNode<Control>("NetworkErrorPopup");
		_popupTitle = GetNode<Label>("NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupTitle");
		_popupMessage = GetNode<Label>("NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupMessage");
		_popupOkButton = GetNode<Button>("NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupOkButton");

		// Initial state check: if already logged in, skip to Main Menu
		if (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null)
		{
			GetTree().ChangeSceneToFile("res://assets/scenes/main_menu.tscn");
			return;
		}

		SetupUI();
		PopulateLanguageSelector();
		UISoundManager.AutoWireButtons(this);

		// Signals
		_submitButton.Pressed += OnSubmitPressed;
		_websiteButton.Pressed += OnWebsitePressed;
		_toggleModeButton.Pressed += OnToggleModePressed;
		_languageSelector.ItemSelected += OnLanguageSelected;
		_popupOkButton.Pressed += () => _networkErrorPopup.Visible = false;

		UpdateLocalizedText();
	}

	private void SetupUI()
	{
		_usernameInput.Visible = false;
		_networkErrorPopup.Visible = false;
	}

	private void UpdateLocalizedText()
	{
		_title.Text = Tr("Welcome");
		_emailInput.PlaceholderText = Tr("Email");
		_usernameInput.PlaceholderText = Tr("Username");
		_passwordInput.PlaceholderText = Tr("Password");
		_submitButton.Text = _isSignupMode ? Tr("Sign Up") : Tr("Login");
		_websiteButton.Text = Tr("Visit Website");
		_toggleModeButton.Text = _isSignupMode ? Tr("Back to Login") : Tr("Need an account? Sign Up");
		_errorLabel.Text = Tr("Error occurred");
		_popupTitle.Text = Tr("Connection Error");
		_popupOkButton.Text = Tr("OK");
	}

	private string Tr(string text)
	{
		return TranslationServer.Translate(text);
	}

	private void OnToggleModePressed()
	{
		_isSignupMode = !_isSignupMode;
		_usernameInput.Visible = _isSignupMode;
		_errorLabel.Visible = false;
		UpdateLocalizedText();
	}

	private async void OnWebsitePressed()
	{
		_websiteButton.Disabled = true;
		_errorLabel.Visible = false;

		bool hasInternet = await CheckInternetConnectionAsync();
		_websiteButton.Disabled = false;

		if (hasInternet)
		{
			OS.ShellOpen(TARGET_WEBSITE_URL);
		}
		else
		{
			ShowNetworkErrorPopup(Tr("No internet connection. Please check your network and try again."));
		}
	}

	private void ShowNetworkErrorPopup(string message)
	{
		_popupTitle.Text = Tr("Connection Error");
		_popupMessage.Text = message;
		_popupOkButton.Text = Tr("OK");
		_networkErrorPopup.Visible = true;
	}

	private async Task<bool> CheckInternetConnectionAsync()
	{
		try
		{
			using var client = new System.Net.Http.HttpClient();
			client.Timeout = TimeSpan.FromSeconds(3);
			using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, "https://1.1.1.1");
			using var response = await client.SendAsync(request);
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	private void OnSubmitPressed()
	{
		if (_isSignupMode)
		{
			OnSignupPressed();
		}
		else
		{
			OnLoginPressed();
		}
	}

	private async void OnLoginPressed()
	{
		string email = _emailInput.Text.Trim();
		string password = _passwordInput.Text.Trim();

		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			ShowError(Tr("Please enter both email and password"));
			return;
		}

		_submitButton.Disabled = true;
		_errorLabel.Visible = false;

		bool success = await AuthManager.Instance.LoginAsync(email, password);
		if (success)
		{
			GetTree().ChangeSceneToFile("res://assets/scenes/main_menu.tscn");
		}
		else
		{
			ShowError(Tr("Invalid email or password"));
			_submitButton.Disabled = false;
		}
	}

	private async void OnSignupPressed()
	{
		string username = _usernameInput.Text.Trim();
		string email = _emailInput.Text.Trim();
		string password = _passwordInput.Text.Trim();

		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			ShowError(Tr("Please fill in all fields"));
			return;
		}

		_submitButton.Disabled = true;
		_errorLabel.Visible = false;

		bool success = await AuthManager.Instance.RegisterAsync(username, email, password);
		if (success)
		{
			ShowError(Tr("Account created successfully! Please log in."));
			_isSignupMode = false;
			_usernameInput.Visible = false;
			UpdateLocalizedText();
		}
		else
		{
			ShowError(Tr("Registration failed. Username or email may be taken."));
		}
		_submitButton.Disabled = false;
	}

	private void ShowError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
	}

	private void PopulateLanguageSelector()
	{
		// Read the CSV header to find available locales
		string path = "res://assets/translations/Translations - Sheet1.csv";
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null) return;

		string header = file.GetLine();
		string[] locales = header.Split(',');

		// Map locale codes to readable names
		var localeMap = new Dictionary<string, string>
		{
			{ "english", "English" },
			{ "spanish", "Spanish (Español)" },
			{ "eurofrench", "French (Français)" },
			{ "brazilianportuguese", "Portuguese (Brasil)" },
			{ "german", "German (Deutsch)" },
			{ "italian", "Italian (Italiano)" },
			{ "japanese", "Japanese (日本語)" },
			{ "korean", "Korean (한국어)" },
			{ "mandarin", "Chinese (中文)" },
			{ "polish", "Polish (Polski)" },
			{ "turkish", "Turkish (Türkçe)" },
			{ "arabic", "Arabic (العربية)" },
			{ "hebrew", "Hebrew (עברית)" }
		};

		_languageSelector.Clear();
		_localeCodes.Clear();

		int currentSelectedIdx = 0;
		string activeLocale = TranslationServer.GetLocale().Trim().ToLower();

		for (int i = 1; i < locales.Length; i++)
		{
			string code = locales[i].Trim().ToLower();
			if (string.IsNullOrEmpty(code)) continue;

			if (localeMap.TryGetValue(code, out string readableName))
			{
				_languageSelector.AddItem(readableName);
				_localeCodes.Add(code);

				if (code == activeLocale)
				{
					currentSelectedIdx = _localeCodes.Count - 1;
				}
			}
		}

		if (_localeCodes.Count > 0)
		{
			_languageSelector.Select(currentSelectedIdx);
		}
	}

	private void OnLanguageSelected(long index)
	{
		if (index >= 0 && index < _localeCodes.Count)
		{
			string selectedLocale = _localeCodes[(int)index];
			TranslationServer.SetLocale(selectedLocale);
			UpdateLocalizedText();
		}
	}
}
