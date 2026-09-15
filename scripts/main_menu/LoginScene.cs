using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>
/// LoginScene manages the user authentication portal, providing interfaces for both
/// account creation (Sign Up) and identity verification (Login). It integrates
/// directly with the AuthManager for API communication and provides a custom
/// locale selector based on an external translation CSV.
///
/// Unity Migration Note:
/// 1. UI Architecture: In Unity, use a Canvas with a 'LoginPanel' and 'SignupPanel'.
///    Instead of toggling visibility of individual fields, use a State-based UI
///    controller or simple GameObject toggles for the two modes.
/// 2. Networking: Replace System.Net.Http with UnityWebRequest for better
///    compatibility with Unity's main thread and mobile platforms.
/// 3. Localization: Use the Unity Localization package instead of manually
///    parsing CSV files and using TranslationServer.
/// </summary>
public partial class LoginScene : Control
{
	// ─── UI Component References ────────────────────────────────────────────────
	private Label _title;
	private LineEdit _emailInput;
	private LineEdit _usernameInput;
	private LineEdit _passwordInput;
	private Button _submitButton;
	private Button _websiteButton;
	private Button _toggleModeButton;
	private Label _errorLabel;
	private OptionButton _languageSelector;

	// Modal Network Error Popup components
	private Control _networkErrorPopup;
	private Label _popupTitle;
	private Label _popupMessage;
	private Button _popupOkButton;

	// State tracking
	private bool _isSignupMode = false;
	private readonly List<string> _localeCodes = new();
	private const string TARGET_WEBSITE_URL = "https://axiomforcereclaimearth.vercel.app";

	public override void _Ready()
	{
		// Resolve node references for the authentication form.
		_title = GetNode<Label>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/Title");
		_emailInput = GetNode<LineEdit>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/EmailInput");
		_usernameInput = GetNode<LineEdit>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/UsernameInput");
		_passwordInput = GetNode<LineEdit>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/PasswordInput");
		_submitButton = GetNode<Button>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/SubmitButton");
		_websiteButton = GetNode<Button>("WebsiteButton");
		_toggleModeButton = GetNode<Button>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/ToggleModeButton");
		_errorLabel = GetNode<Label>("SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/ErrorLabel");
		_languageSelector = GetNode<OptionButton>("LanguageSelector");

		// Resolve node references for the network error modal.
		_networkErrorPopup = GetNode<Control>("NetworkErrorPopup");
		_popupTitle = GetNode<Label>("NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupTitle");
		_popupMessage = GetNode<Label>("NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupMessage");
		_popupOkButton = GetNode<Button>("NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupOkButton");

		// Session Shortcut: If the user is already authenticated (e.g., from a previous run),
		// bypass the login screen and jump straight to the main menu.
		if (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null)
		{
			GetTree().ChangeSceneToFile("res://scenes/main_menu/main_menu.tscn");
			return;
		}

		SetupUI();
		PopulateLanguageSelector();
		UISoundManager.AutoWireButtons(this);

		// Event wiring
		_submitButton.Pressed += OnSubmitPressed;
		_websiteButton.Pressed += OnWebsitePressed;
		_toggleModeButton.Pressed += OnToggleModePressed;
		_languageSelector.ItemSelected += OnLanguageSelected;
		_popupOkButton.Pressed += () => _networkErrorPopup.Visible = false;

		UpdateLocalizedText();
	}

	/// <summary>
	/// Sets the initial visibility of UI elements based on the default Login mode.
	/// </summary>
	private void SetupUI()
	{
		_usernameInput.Visible = false;
		_networkErrorPopup.Visible = false;
	}

	/// <summary>
	/// Refreshes all UI text elements using the current translation locale.
	/// </summary>
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

	/// <summary>
	/// Helper wrapper for Godot's TranslationServer.
	/// </summary>
	private string Tr(string text)
	{
		return TranslationServer.Translate(text);
	}

	/// <summary>
	/// Switches the UI between 'Login' and 'Sign Up' modes.
	/// Updates field visibility and button labels accordingly.
	/// </summary>
	private void OnToggleModePressed()
	{
		_isSignupMode = !_isSignupMode;
		_usernameInput.Visible = _isSignupMode;
		_errorLabel.Visible = false;
		UpdateLocalizedText();
	}

	/// <summary>
	/// Handles requests to open the project's official website.
	/// Includes a pre-flight connectivity check to avoid browser timeouts.
	/// </summary>
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

	/// <summary>
	/// Displays a modal dialog when a network-related failure occurs.
	/// </summary>
	private void ShowNetworkErrorPopup(string message)
	{
		_popupTitle.Text = Tr("Connection Error");
		_popupMessage.Text = message;
		_popupOkButton.Text = Tr("OK");
		_networkErrorPopup.Visible = true;
	}

	/// <summary>
	/// Performs a lightweight HTTP HEAD request to a reliable DNS provider (Cloudflare)
	/// to verify the system has an active internet connection.
	/// </summary>
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

	/// <summary>
	/// Dispatches the authentication request to the AuthManager based on current mode.
	/// </summary>
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

	/// <summary>
	/// Validates inputs and attempts to authenticate the user via AuthManager.
	/// On success, redirects the user to the Main Menu.
	/// </summary>
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
			GetTree().ChangeSceneToFile("res://scenes/main_menu/main_menu.tscn");
		}
		else
		{
			ShowError(Tr("Invalid email or password"));
			_submitButton.Disabled = false;
		}
	}

	/// <summary>
	/// Validates inputs and attempts to create a new account via AuthManager.
	/// On success, switches the UI back to Login mode and notifies the user.
	/// </summary>
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

	/// <summary>
	/// Updates the error label with a specific failure message.
	/// </summary>
	private void ShowError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
	}

	/// <summary>
	/// Parses the translation CSV file to find all available locale codes and
	/// populates the language selection dropdown.
	/// </summary>
	private void PopulateLanguageSelector()
	{
		// Read the CSV header to find available locales.
		string path = "res://assets/translations/Translations - Sheet1.csv";
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null) return;

		string header = file.GetLine();
		string[] locales = header.Split(',');

		// Map internal locale codes to user-friendly display names.
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

	/// <summary>
	/// Signal handler for when a new language is selected from the dropdown.
	/// Applies the locale change immediately to the UI.
	/// </summary>
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
