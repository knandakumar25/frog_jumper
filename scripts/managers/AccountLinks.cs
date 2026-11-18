using System;

/// <summary>
/// Website addresses the game opens for account tasks it does not handle itself.
/// Engine-free (no Godot calls), so NUnit can test it.
///
/// Account deletion (2026-11-18): the game's "Delete account" button opens the website's
/// deletion page, which handles sign-in, the emailed code and the confirmation. Apple and Google
/// accept an in-app link that goes straight to the deletion page.
/// </summary>
public static class AccountLinks
{
    /// <summary>Default website address for local development (the Docker `website` service).</summary>
    public const string DefaultWebsiteBaseUrl = "http://localhost:5173";

    /// <summary>
    /// The website's base address: the <c>AXIOM_WEBSITE_URL</c> environment variable when set
    /// (for example a deployed site), otherwise <see cref="DefaultWebsiteBaseUrl"/>.
    /// </summary>
    public static string WebsiteBaseUrl
    {
        get
        {
            string? configured = Environment.GetEnvironmentVariable("AXIOM_WEBSITE_URL");
            return string.IsNullOrWhiteSpace(configured) ? DefaultWebsiteBaseUrl : configured.Trim().TrimEnd('/');
        }
    }

    /// <summary>
    /// Converts a game locale ISO code (from <c>LocaleCatalog</c>, such as <c>pt_BR</c> or
    /// <c>zh_TW</c>) to the website's URL locale code (such as <c>pt</c> or <c>zh_tw</c>).
    /// Unknown or empty codes fall back to English.
    /// </summary>
    public static string WebsiteLocale(string? gameIso) => (gameIso ?? "").Trim() switch
    {
        "" => "en",
        "pt_BR" => "pt",
        "zh_CN" => "zh",
        "zh_TW" => "zh_tw",
        var other => other.ToLowerInvariant(),
    };

    /// <summary>The website's account-deletion page in the player's language.</summary>
    /// <param name="websiteBase">Website base address, for example <see cref="WebsiteBaseUrl"/>.</param>
    /// <param name="gameIso">The game's current locale ISO code.</param>
    public static string DeleteAccountUrl(string websiteBase, string? gameIso) =>
        $"{websiteBase.TrimEnd('/')}/{WebsiteLocale(gameIso)}/account/delete";
}
