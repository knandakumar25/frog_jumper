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
	private string _errorKey = "";
	private string _popupKey = "";
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
	/// Retranslates visible form labels and known errors without clearing user-entered text.
	/// </summary>
	public override void _Notification(int what)
    {
        // Notification can arrive before _Ready resolves the form; never clear user-entered fields.
        if (what == NotificationTranslationChanged && _title != null)
        {
            UpdateLocalizedText();
            // Locale changes may come from settings or restored session state,
            // rather than the selector itself. Keep its selected identity in sync.
            if (_languageSelector != null) PopulateLanguageSelector();
        }
    }

    /// <summary>Sets initial form visibility without altering user data.</summary>
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
		if (!string.IsNullOrEmpty(_errorKey)) _errorLabel.Text = Tr(_errorKey);
		if (!string.IsNullOrEmpty(_popupKey)) _popupMessage.Text = Tr(_popupKey);
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
			ShowNetworkErrorPopup("No internet connection. Please check your network and try again.");
		}
	}

	/// <summary>
	/// Displays a modal dialog when a network-related failure occurs.
	/// </summary>
	private void ShowNetworkErrorPopup(string message)
	{
		_popupTitle.Text = Tr("Connection Error");
		_popupKey = message;
		_popupMessage.Text = Tr(message);
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
			ShowError("Please enter both email and password");
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
			ShowError("Invalid email or password");
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
			ShowError("Please fill in all fields");
			return;
		}

		_submitButton.Disabled = true;
		_errorLabel.Visible = false;

		bool success = await AuthManager.Instance.RegisterAsync(username, email, password);
		if (success)
		{
			ShowError("Account created successfully! Please log in.");
			_isSignupMode = false;
			_usernameInput.Visible = false;
			UpdateLocalizedText();
		}
		else
		{
			ShowError("Registration failed. Username or email may be taken.");
		}
		_submitButton.Disabled = false;
	}

	/// <summary>
	/// Updates the error label with a specific failure message.
	/// </summary>
	private void ShowError(string message)
	{
		_errorKey = message;
		_errorLabel.Text = Tr(message);
		_errorLabel.Visible = true;
	}

	/// <summary>
	/// Populates the login selector from the same stable language catalog as settings.
	/// </summary>
	private void PopulateLanguageSelector()
	{
        // The selector simultaneously displays every script, before any locale
        // has been selected. Its popup needs the same complete fallback chain.
        var selectorFont = GD.Load<Font>("res://assets/fonts/LocaleSelectorFont.tres");
        _languageSelector.AddThemeFontOverride("font", selectorFont);
        _languageSelector.GetPopup().AddThemeFontOverride("font", selectorFont);
        // Native-script item widths must not expand an anchored picker past the viewport.
        _languageSelector.FitToLongestItem = false;
        _languageSelector.ClipText = true;
        _languageSelector.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        // OptionButton requests separate RTL styles; keep the authored neon frame in both directions.
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
            _languageSelector.AddThemeStyleboxOverride(state + "_mirrored", _languageSelector.GetThemeStylebox(state));
        _languageSelector.Clear();
        _localeCodes.Clear();
        string current = LocaleCatalog.Resolve(TranslationServer.GetLocale()).Key;
        foreach (var language in LocaleCatalog.All)
        {
            _languageSelector.AddItem(language.DisplayName);
            _localeCodes.Add(language.Key);
            if (language.Key == current) _languageSelector.Select(_localeCodes.Count - 1);
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
			GetNode<LocaleService>("/root/LocaleService").ApplyLocale(selectedLocale);
			UpdateLocalizedText();
		}
	}
}
