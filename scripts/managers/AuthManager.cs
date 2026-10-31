using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

/// <summary>
/// AuthManager handles user authentication, session tokens (JWT), and user profile management.
/// It communicates with a REST API to manage logins, registrations, and browser-based authentication.
///
/// Unity Migration Note: In Unity, this would likely be a singleton service.
/// Consider using a more robust networking library like UnityWebRequest or a dedicated
/// API wrapper. Ensure the API base URL is configurable for different environments (Dev/Prod).
/// </summary>
public partial class AuthManager : Node
{
	/// <summary>
	/// Singleton instance for global access to authentication state.
	/// </summary>
	public static AuthManager Instance { get; private set; }

	// ─── Networking Configuration ────────────────────────────────────────────────
	private readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

	/// <summary>
	/// The base URL for the authentication API.
	/// </summary>
	private const string API_BASE_URL = "http://localhost:8000/api";


	// ─── Session State ───────────────────────────────────────────────────────────
	/// <summary>
	/// The JWT access token used for authorizing API requests.
	/// </summary>
	public string AccessToken { get; private set; }

	/// <summary>
	/// The JWT refresh token used to obtain a new access token when the current one expires.
	/// </summary>
	public string RefreshToken { get; private set; }

	/// <summary>
	/// Event fired whenever the current user profile is updated.
	/// </summary>
	public event Action OnProfileUpdated;

	private UserProfile _currentUser;

	/// <summary>
	/// The currently authenticated user's profile data.
	/// </summary>
	public UserProfile CurrentUser
	{
		get => _currentUser;
		private set
		{
			_currentUser = value;
			OnProfileUpdated?.Invoke();
		}
	}

	// ─── Lifecycle ───────────────────────────────────────────────────────────────
	/// <summary>Starts signed out; bearer tokens exist only in this process and are never restored from disk.</summary>
	public override void _Ready()
	{
		Instance = this;
	}

	// ─── Authentication Methods ──────────────────────────────────────────────────
	/// <summary>
	/// Authenticates the user using a username and password.
	/// </summary>
	/// <returns>True if login was successful; otherwise false.</returns>
	public async Task<bool> LoginAsync(string username, string password)
	{
		var payload = new { username, password };
		var json = JsonSerializer.Serialize(payload);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		try
		{
			var response = await _httpClient.PostAsync($"{API_BASE_URL}/auth/login/", content);
			if (response.IsSuccessStatusCode)
			{
				var responseString = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(responseString);
				var root = doc.RootElement;

				// Extract tokens and user data from the API response.
				AccessToken = root.GetProperty("access").GetString();
				RefreshToken = root.GetProperty("refresh").GetString();

				var userJson = root.GetProperty("user").GetRawText();
				CurrentUser = JsonSerializer.Deserialize<UserProfile>(userJson);

				return true;
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Login failed: {e.Message}");
		}
		return false;
	}

	/// <summary>
	/// Initiates a browser-based OAuth-style login flow.
	/// Opens the system browser and then polls the API until the user completes the login.
	/// </summary>
	/// <returns>True if the browser login was successful; otherwise false.</returns>
	public async Task<bool> StartBrowserLoginAsync()
	{
		try
		{
			var response = await _httpClient.PostAsync($"{API_BASE_URL}/auth/browser-start/", null);
			if (response.IsSuccessStatusCode)
			{
				var responseString = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(responseString);
				var root = doc.RootElement;

				string sessionId = root.GetProperty("session_id").GetString();
				string authUrl = root.GetProperty("auth_url").GetString();

				// Unity Migration: Use Application.OpenURL(authUrl).
				OS.ShellOpen(authUrl);
				return await PollBrowserLoginAsync(sessionId);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Browser login start failed: {e.Message}");
		}
		return false;
	}

	/// <summary>
	/// Internal polling loop that checks if the browser login was completed.
	/// </summary>
	private async Task<bool> PollBrowserLoginAsync(string sessionId)
	{
		int attempts = 0;
		const int maxAttempts = 60; // Poll for 60 seconds (1 per second)

		while (attempts < maxAttempts)
		{
			try
			{
				var response = await _httpClient.GetAsync($"{API_BASE_URL}/auth/browser-poll/?session_id={sessionId}");

				if (response.IsSuccessStatusCode)
				{
					var responseString = await response.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(responseString);
					var root = doc.RootElement;

					if (root.TryGetProperty("2fa_required", out var tfaProp) && tfaProp.GetBoolean())
					{
						GD.Print("AuthManager: 2FA required for this account.");
						return false;
					}

					if (root.TryGetProperty("access", out var accessProp))
					{
						AccessToken = accessProp.GetString();
						RefreshToken = root.GetProperty("refresh").GetString();

						var userJson = root.GetProperty("user").GetRawText();
						CurrentUser = JsonSerializer.Deserialize<UserProfile>(userJson);

						return true;
					}
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Polling error: {e.Message}");
			}

			attempts++;
			await Task.Delay(1000);
		}

		GD.PrintErr("Browser login timed out.");
		return false;
	}

	/// <summary>
	/// Registers a new user account.
	/// </summary>
	public async Task<bool> RegisterAsync(string username, string email, string password)
	{
		var payload = new { username, email, password };
		var json = JsonSerializer.Serialize(payload);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		try
		{
			var response = await _httpClient.PostAsync($"{API_BASE_URL}/auth/register/", content);
			return response.IsSuccessStatusCode;
		}
		catch (Exception e)
		{
			GD.PrintErr($"Registration failed: {e.Message}");
			return false;
		}
	}

	/// <summary>
	/// Fetches the detailed user profile from the server.
	/// </summary>
	public async Task<UserProfile> GetProfileAsync()
	{
		if (string.IsNullOrEmpty(AccessToken)) return null;

		// Set the Authorization header for the request.
		_httpClient.DefaultRequestHeaders.Authorization =
			new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

		try
		{
			var response = await _httpClient.GetAsync($"{API_BASE_URL}/auth/profile/");
			if (response.IsSuccessStatusCode)
			{
				var responseString = await response.Content.ReadAsStringAsync();
				return JsonSerializer.Deserialize<UserProfile>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Profile fetch failed: {e.Message}");
		}
		return null;
	}

	/// <summary>
	/// Clears authentication tokens and user data locally.
	/// </summary>
	public void Logout()
	{
		AccessToken = null;
		RefreshToken = null;
		CurrentUser = null;
		_httpClient.DefaultRequestHeaders.Authorization = null;
	}


}

/// <summary>
/// Data Transfer Object (DTO) representing a user's profile information.
/// </summary>
public class UserProfile
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string Email { get; set; }

	[JsonPropertyName("first_name")]
	public string FirstName { get; set; }

	[JsonPropertyName("last_name")]
	public string LastName { get; set; }

	[JsonPropertyName("profile_picture")]
	public string ProfilePicture { get; set; }
}
