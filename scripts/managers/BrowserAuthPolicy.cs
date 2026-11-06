using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>Shared desktop handoff transport constraints, independent of Godot's node lifecycle.</summary>
public static class BrowserAuthPolicy
{
    /// <summary>Allow HTTPS and local development HTTP, rejecting shell schemes and remote plaintext transport.</summary>
    public static bool IsSafeUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == "https" || (uri.Scheme == "http" && uri.IsLoopback));

    /// <summary>Return a two-second polling interval with bounded exponential backoff after transient failures.</summary>
    public static int PollDelaySeconds(int failures) => 2 * (1 << Math.Clamp(failures, 0, 2));

    /// <summary>Django's WSGI development server requires Content-Length; streaming JsonContent uses unsupported chunked bodies.</summary>
    public static StringContent CreateJsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
}
