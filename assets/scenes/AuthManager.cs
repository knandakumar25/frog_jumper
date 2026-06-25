using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public partial class AuthManager : Node
{
    public static AuthManager Instance { get; private set; }

    private readonly HttpClient _httpClient = new HttpClient();
    private const string API_BASE_URL = "http://localhost:8000/api";
    private const string AUTH_CONFIG_PATH = "user://auth.cfg";

    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public UserProfile CurrentUser { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        LoadAuthData();
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var payload = new { username, password };
        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{API_BASE_URL}/auth/login/", content);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(responseString);

                AccessToken = data["access"]?.ToString();
                RefreshToken = data["refresh"]?.ToString();
                CurrentUser = data["user"].ToObject<UserProfile>();

                SaveAuthData();
                return true;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Login failed: {e.Message}");
        }
        return false;
    }

    public async Task<bool> RegisterAsync(string username, string email, string password)
    {
        var payload = new { username, email, password };
        var json = JsonConvert.SerializeObject(payload);
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

    public async Task<UserProfile> GetProfileAsync()
    {
        if (string.IsNullOrEmpty(AccessToken)) return null;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        try
        {
            var response = await _httpClient.GetAsync($"{API_BASE_URL}/profile/");
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserProfile>(responseString);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Profile fetch failed: {e.Message}");
        }
        return null;
    }

    public void Logout()
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = null;
        DeleteAuthData();
    }

    private void SaveAuthData()
    {
        var config = new ConfigFile();
        config.SetValue("auth", "access_token", AccessToken);
        config.SetValue("auth", "refresh_token", RefreshToken);
        config.Save(AUTH_CONFIG_PATH);
    }

    private void LoadAuthData()
    {
        var config = new ConfigFile();
        if (config.Load(AUTH_CONFIG_PATH) == Error.Ok)
        {
            AccessToken = config.GetValue("auth", "access_token", "").ToString();
            RefreshToken = config.GetValue("auth", "refresh_token", "").ToString();
        }
    }

    private void DeleteAuthData()
    {
        DirAccess.RemoveAbsolute(AUTH_CONFIG_PATH);
    }
}

public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}
