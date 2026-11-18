/// <summary>
/// Account deletion links and session handling (2026-11-18): the game's "Delete account" button
/// opens the website's deletion page in the player's language, and a 401 or 404 profile answer
/// ends the game session. Engine-free.
/// </summary>
public class AccountLinksTests
{
    [TestCase("en", "http://localhost:5173/en/account/delete")]
    [TestCase("pt_BR", "http://localhost:5173/pt/account/delete")]
    [TestCase("zh_CN", "http://localhost:5173/zh/account/delete")]
    [TestCase("zh_TW", "http://localhost:5173/zh_tw/account/delete")]
    [TestCase("ar", "http://localhost:5173/ar/account/delete")]
    [TestCase("", "http://localhost:5173/en/account/delete")]
    [TestCase(null, "http://localhost:5173/en/account/delete")]
    /// <summary>Every game locale maps to the website's URL locale; unknown or empty falls back to English.</summary>
    public void DeletePageUsesTheWebsiteLocale(string? iso, string expected) =>
        Assert.That(AccountLinks.DeleteAccountUrl(AccountLinks.DefaultWebsiteBaseUrl, iso), Is.EqualTo(expected));

    /// <summary>A trailing slash on a configured website address is not doubled.</summary>
    [Test]
    public void ConfiguredBaseWithTrailingSlash() =>
        Assert.That(AccountLinks.DeleteAccountUrl("https://axiom.example/", "de"), Is.EqualTo("https://axiom.example/de/account/delete"));

    [TestCase(401, true)]
    [TestCase(404, true)]
    [TestCase(403, false)]
    [TestCase(500, false)]
    [TestCase(200, false)]
    /// <summary>Only "not signed in" and "no such account" end the game session; outages keep it.</summary>
    public void OnlyMissingAccountOrSessionSignsOut(int status, bool ends) =>
        Assert.That(AuthManager.SessionEnded(status), Is.EqualTo(ends));
}
