using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Owns process-memory game credentials and one cancellable browser approval attempt.</summary>
public partial class AuthManager : Node
{
    /// <summary>The process-wide authentication autoload; null after scene-tree teardown.</summary>
    public static AuthManager Instance { get; private set; }
    private readonly System.Net.Http.HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private CancellationTokenSource _attempt;
    private string _sessionId, _secret, _browserUrl;
    private bool _cancelled;
    /// <summary>API endpoint can be overridden without saving authentication data to disk.</summary>
    public static string ApiBaseUrl => OS.HasEnvironment("AXIOM_API_BASE_URL")
        ? OS.GetEnvironment("AXIOM_API_BASE_URL").TrimEnd('/') : "http://localhost:8000/api";
    /// <summary>Bearer credential retained only in process memory.</summary>
    public string AccessToken { get; private set; }
    /// <summary>Refresh credential retained only in process memory.</summary>
    public string RefreshToken { get; private set; }
    /// <summary>The verified game profile, or null for guest play.</summary>
    public UserProfile CurrentUser { get; private set; }
    /// <summary>True while a device attempt owns the polling/cancellation lifecycle.</summary>
    public bool IsWaiting => _attempt != null;
    /// <summary>Notify menu subscribers after successful sign-in or local logout.</summary>
    public event Action OnProfileUpdated;
    /// <summary>Send only localized presentation keys, never transport bodies or credentials.</summary>
    public event Action<string> OnBrowserStatus;

    /// <summary>Starts each process signed out.</summary>
    public override void _Ready() => Instance = this;

    /// <summary>Reopen only the current public URL; no device proof is sent to the browser.</summary>
    public void OpenBrowserAgain()
    {
        if (!string.IsNullOrEmpty(_browserUrl) && OS.ShellOpen(_browserUrl) != Error.Ok)
            OnBrowserStatus?.Invoke("AUTH_BROWSER_ERROR");
    }

    /// <summary>Start a single fifteen-minute request; network failures back off to at most eight seconds.</summary>
    public async Task<bool> StartBrowserLoginAsync(string intent = "login", string locale = "en-US")
    {
        if (_attempt != null) return false;
        var owner = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        _attempt = owner;
        _cancelled = false;
        var ct = owner.Token;
        try
        {
            if (!BrowserAuthPolicy.IsSafeUrl(ApiBaseUrl)) throw new InvalidOperationException("Invalid API URL");
            OnBrowserStatus?.Invoke("AUTH_WAITING");
            using var start = await PostJsonAsync("start", new { intent, locale }, ct);
            start.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync(ct));
            ct.ThrowIfCancellationRequested();
            _sessionId = doc.RootElement.GetProperty("session_id").GetString();
            _secret = doc.RootElement.GetProperty("secret").GetString();
            _browserUrl = doc.RootElement.GetProperty("auth_url").GetString();
            // Reject malformed/misconfigured server URLs before invoking a shell handler.
            if (!BrowserAuthPolicy.IsSafeUrl(_browserUrl))
                throw new InvalidOperationException("Invalid browser URL");
            var expiry = doc.RootElement.GetProperty("expires_at").GetDateTimeOffset();
            OpenBrowserAgain();
            int failures = 0;
            while (DateTimeOffset.UtcNow < expiry)
            {
                await Task.Delay(TimeSpan.FromSeconds(BrowserAuthPolicy.PollDelaySeconds(failures)), ct);
                try
                {
                    using var poll = await PostJsonAsync("poll", new { session_id = _sessionId, secret = _secret }, ct);
                    if ((int)poll.StatusCode >= 500 || (int)poll.StatusCode == 429)
                        throw new HttpRequestException();
                    if (!poll.IsSuccessStatusCode) { OnBrowserStatus?.Invoke("AUTH_EXPIRED"); return false; }
                    using var result = JsonDocument.Parse(await poll.Content.ReadAsStringAsync(ct));
                    ct.ThrowIfCancellationRequested();
                    failures = 0;
                    OnBrowserStatus?.Invoke("AUTH_WAITING");
                    if (result.RootElement.TryGetProperty("access", out var access))
                    {
                        var profile = JsonSerializer.Deserialize<UserProfile>(result.RootElement.GetProperty("user").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var accessToken = access.GetString();
                        var refreshToken = result.RootElement.GetProperty("refresh").GetString();
                        if (profile == null || string.IsNullOrWhiteSpace(profile.Username) ||
                            string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                            throw new JsonException("Incomplete authentication response");
                        AccessToken = accessToken;
                        RefreshToken = refreshToken;
                        CurrentUser = profile;
                        OnProfileUpdated?.Invoke();
                        return true;
                    }
                }
                catch (HttpRequestException) { failures++; OnBrowserStatus?.Invoke("AUTH_NETWORK"); }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested) { failures++; OnBrowserStatus?.Invoke("AUTH_NETWORK"); }
            }
            OnBrowserStatus?.Invoke("AUTH_EXPIRED");
        }
        catch (OperationCanceledException) { if (_attempt == owner && !_cancelled) OnBrowserStatus?.Invoke("AUTH_EXPIRED"); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { OnBrowserStatus?.Invoke("AUTH_ERROR"); }
        finally
        {
            // Capture before clearing so cleanup cannot cancel a future attempt.
            var sid = _sessionId; var secret = _secret;
            _sessionId = _secret = _browserUrl = null;
            if (_attempt == owner) _attempt = null;
            owner.Dispose();
            if (CurrentUser == null && sid != null) await InvalidateAsync(sid, secret);
        }
        return false;
    }

    /// <summary>Best-effort server cancellation; local cancellation always prevents late sign-in.</summary>
    private async Task InvalidateAsync(string sid, string secret)
    {
        try { using var response = await PostJsonAsync("cancel", new { session_id = sid, secret }); }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or ObjectDisposedException) { }
    }

    /// <summary>Send bounded JSON with an explicit body length, compatible with the local WSGI backend.</summary>
    private async Task<HttpResponseMessage> PostJsonAsync(string action, object payload, CancellationToken cancellation = default)
    {
        using var content = BrowserAuthPolicy.CreateJsonContent(payload);
        return await _http.PostAsync(ApiBaseUrl + "/auth/browser-" + action + "/", content, cancellation);
    }

    /// <summary>Stop polling immediately when the menu is left, dismissed or the game closes.</summary>
    public void CancelBrowserLogin()
    {
        _cancelled = true;
        _attempt?.Cancel();
    }

    /// <summary>Clear the game session without changing the independent website Clerk session.</summary>
    public void Logout()
    {
        CancelBrowserLogin();
        AccessToken = RefreshToken = null;
        CurrentUser = null;
        OnProfileUpdated?.Invoke();
    }

    /// <summary>Retrieve the profile using a per-request header so device calls never inherit tokens.</summary>
    public async Task<UserProfile> GetProfileAsync()
    {
        if (AccessToken == null) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + "/auth/profile/");
        request.Headers.Authorization = new("Bearer", AccessToken);
        try { using var response = await _http.SendAsync(request); return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserProfile>() : null; }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>Cancel asynchronous work when the autoload exits; never touch freed UI nodes.</summary>
    public override void _ExitTree()
    {
        CancelBrowserLogin();
        OnProfileUpdated = null;
        OnBrowserStatus = null;
        Instance = null;
    }
}

/// <summary>Profile contract returned by Django; credentials remain separate.</summary>
public class UserProfile
{
    /// <summary>Stable Django account identifier.</summary>
    public int Id { get; set; }
    /// <summary>Account handle displayed without localization.</summary>
    public string Username { get; set; }
    /// <summary>Verified account email address.</summary>
    public string Email { get; set; }
    /// <summary>Optional profile given name.</summary>
    [JsonPropertyName("first_name")] public string FirstName { get; set; }
    /// <summary>Optional profile family name.</summary>
    [JsonPropertyName("last_name")] public string LastName { get; set; }
    /// <summary>Existing optional base64 avatar contract.</summary>
    [JsonPropertyName("profile_picture")] public string ProfilePicture { get; set; }
}
