using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Owns the game's credentials and one cancellable browser approval attempt.
///
/// Since 2026-11-18 saves live on the account, so play requires sign-in. Sign-in is remembered for
/// up to 7 days (the refresh token's lifetime): the refresh token is stored encrypted in
/// <c>user://auth.dat</c>, with a key derived from this device's id, and is exchanged at launch.
/// Every authorized call goes through <see cref="SendAuthorizedAsync"/>, which refreshes an expired
/// access token once (refresh tokens are single-use, so concurrent callers share one refresh) and
/// retries. A failed refresh signs the game out.
/// </summary>
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
    /// <summary>Bearer credential (one hour), kept in memory only.</summary>
    public string AccessToken { get; private set; }
    /// <summary>Refresh credential (single use, seven days); also stored encrypted in <see cref="TokenFile"/>.</summary>
    public string RefreshToken { get; private set; }
    /// <summary>Encrypted file holding the remembered refresh token.</summary>
    public const string TokenFile = "user://auth.dat";
    // The refresh in progress, shared by every caller that hits an expired token at the same time.
    private Task<bool> _refreshing;
    private readonly object _refreshLock = new();
    /// <summary>The verified game profile, or null for guest play.</summary>
    public UserProfile CurrentUser { get; private set; }
    /// <summary>True while a device attempt owns the polling/cancellation lifecycle.</summary>
    public bool IsWaiting => _attempt != null;
    /// <summary>Notify menu subscribers after successful sign-in or local logout.</summary>
    public event Action OnProfileUpdated;
    /// <summary>Send only localized presentation keys, never transport bodies or credentials.</summary>
    public event Action<string> OnBrowserStatus;

    /// <summary>Registers the singleton and, outside headless test runs, resumes a remembered sign-in.</summary>
    public override void _Ready()
    {
        Instance = this;
        // Headless runs are tests and tools; they must never pick up the developer's real account.
        if (DisplayServer.GetName() != "headless") Callable.From(() => _ = TryResumeAsync()).CallDeferred();
    }

    /// <summary>The API base address, for GDScript callers (the static property is not visible there).</summary>
    public string GetApiBaseUrl() => ApiBaseUrl;

    /// <summary>
    /// Exchanges the remembered refresh token for a session and loads the profile. Returns false when
    /// nothing is remembered or the token is no longer valid (the file is then removed).
    /// </summary>
    public async Task<bool> TryResumeAsync()
    {
        string stored = ReadStoredRefreshToken();
        if (string.IsNullOrEmpty(stored) || CurrentUser != null) return false;
        RefreshToken = stored;
        if (!await RefreshAsync()) return false;
        var profile = await GetProfileAsync();
        if (profile == null) return false;
        CurrentUser = profile;
        OnProfileUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// Sends a request built by <paramref name="makeRequest"/> with the bearer token. On 401 it
    /// refreshes once and sends a fresh request. Returns null when not signed in or the refresh failed
    /// (the game is then signed out). Network errors propagate to the caller.
    /// </summary>
    public async Task<HttpResponseMessage> SendAuthorizedAsync(Func<HttpRequestMessage> makeRequest)
    {
        // ConfigureAwait(false) throughout: blocking GDScript save helpers may wait on the main thread.
        if (string.IsNullOrEmpty(AccessToken) && !(await RefreshAsync().ConfigureAwait(false))) return null;
        var response = await SendWithTokenAsync(makeRequest).ConfigureAwait(false);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return response;
        response.Dispose();
        if (!await RefreshAsync().ConfigureAwait(false)) return null;
        return await SendWithTokenAsync(makeRequest).ConfigureAwait(false);
    }

    /// <summary>Sends one request with the current access token.</summary>
    private Task<HttpResponseMessage> SendWithTokenAsync(Func<HttpRequestMessage> makeRequest)
    {
        var request = makeRequest();
        request.Headers.Authorization = new("Bearer", AccessToken);
        return _http.SendAsync(request);
    }

    /// <summary>
    /// Exchanges the refresh token for a new pair. Single-flight: refresh tokens work once (a replay
    /// revokes the whole sign-in), so callers arriving during a refresh wait for the same one.
    /// A refused token signs the game out; a network failure keeps the session for a later retry.
    /// </summary>
    public Task<bool> RefreshAsync()
    {
        lock (_refreshLock)
        {
            if (_refreshing != null) return _refreshing;
            _refreshing = RefreshOnceAsync();
            var current = _refreshing;
            current.ContinueWith(_ => { lock (_refreshLock) { if (_refreshing == current) _refreshing = null; } });
            return current;
        }
    }

    /// <summary>One refresh call; see <see cref="RefreshAsync"/>.</summary>
    private async Task<bool> RefreshOnceAsync()
    {
        string refresh = RefreshToken;
        if (string.IsNullOrEmpty(refresh)) return false;
        try
        {
            using var content = BrowserAuthPolicy.CreateJsonContent(new { refresh });
            using var response = await _http.PostAsync(ApiBaseUrl + "/auth/token/refresh/", content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // This may run off the main thread, so sign-out (which updates menus) is deferred to it.
                if (SessionEnded((int)response.StatusCode)) { AccessToken = RefreshToken = null; DeleteStoredRefreshToken(); Callable.From(Logout).CallDeferred(); }
                return false;
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            AccessToken = doc.RootElement.GetProperty("access").GetString();
            RefreshToken = doc.RootElement.GetProperty("refresh").GetString();
            StoreRefreshToken(RefreshToken);
            return !string.IsNullOrEmpty(AccessToken);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException) { return false; }
    }

    /// <summary>Encryption key for <see cref="TokenFile"/>, tied to this device so a copied file is useless elsewhere.</summary>
    private static string TokenKey() => "axiom-force-auth-" + OS.GetUniqueId();

    /// <summary>Writes the refresh token to <see cref="TokenFile"/> (encrypted), or deletes the file for null.</summary>
    private static void StoreRefreshToken(string token)
    {
        if (string.IsNullOrEmpty(token)) { DeleteStoredRefreshToken(); return; }
        using var file = FileAccess.OpenEncryptedWithPass(TokenFile, FileAccess.ModeFlags.Write, TokenKey());
        file?.StoreString(token);
    }

    /// <summary>The remembered refresh token, or "" when none is stored or it cannot be read.</summary>
    private static string ReadStoredRefreshToken()
    {
        if (!FileAccess.FileExists(TokenFile)) return "";
        using var file = FileAccess.OpenEncryptedWithPass(TokenFile, FileAccess.ModeFlags.Read, TokenKey());
        return file?.GetAsText().Trim() ?? "";
    }

    /// <summary>Forgets the remembered sign-in.</summary>
    private static void DeleteStoredRefreshToken()
    {
        if (FileAccess.FileExists(TokenFile)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(TokenFile));
    }

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
                        StoreRefreshToken(refreshToken);
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

    /// <summary>Clear the game session and the remembered sign-in, without changing the independent website Clerk session.</summary>
    public void Logout()
    {
        CancelBrowserLogin();
        AccessToken = RefreshToken = null;
        DeleteStoredRefreshToken();
        CurrentUser = null;
        OnProfileUpdated?.Invoke();
    }

    /// <summary>
    /// Retrieve the profile using a per-request header so device calls never inherit tokens.
    /// If the API answers 401 or 404, the account is gone (for example deleted on the website,
    /// 2026-11-18) or the session was revoked, so the game signs out instead of keeping a dead
    /// session. Network failures keep the session and return null.
    /// </summary>
    public async Task<UserProfile> GetProfileAsync()
    {
        if (AccessToken == null) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + "/auth/profile/");
        request.Headers.Authorization = new("Bearer", AccessToken);
        try
        {
            using var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<UserProfile>();
            if (SessionEnded((int)response.StatusCode)) Logout();
            return null;
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }

    /// <summary>True for API answers that mean the signed-in session no longer exists (401, 404).</summary>
    public static bool SessionEnded(int statusCode) => statusCode is 401 or 404;

    /// <summary>Cancel asynchronous work when the autoload exits; never touch freed UI nodes.</summary>
    public override void _ExitTree()
    {
        CancelBrowserLogin();
        OnProfileUpdated = null;
        OnBrowserStatus = null;
        Instance = null;
    }
}

/// <summary>Profile contract returned by the accounts API (Express.js since 2026-11-15; same JSON as the Django backend it replaced); credentials remain separate.</summary>
public class UserProfile
{
    /// <summary>Stable account identifier (the accounts_user row id).</summary>
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
