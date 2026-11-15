/// <summary>Transport boundaries that prevent accidental shell execution or plaintext remote credential exchange.</summary>
public class BrowserAuthPolicyTests
{
    /// <summary>Device proof is sent in a length-delimited body rather than a chunked stream (required by the old Django server; kept for proxy compatibility).</summary>
    [Test]
    public async Task JsonBodiesHaveExplicitUtf8ContentLength()
    {
        using var content = BrowserAuthPolicy.CreateJsonContent(new { locale = "日本語", secret = "test-fixture" });
        var bytes = await content.ReadAsByteArrayAsync();
        Assert.That(content.Headers.ContentLength, Is.EqualTo(bytes.Length));
        Assert.That(content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(System.Text.Json.JsonDocument.Parse(bytes).RootElement.GetProperty("locale").GetString(), Is.EqualTo("日本語"));
    }
    /// <summary>Desktop development uses loopback; hosted handoffs require TLS.</summary>
    [TestCase("http://localhost:8000/api", true)]
    [TestCase("http://127.0.0.1:8000/api", true)]
    [TestCase("https://accounts.example/auth", true)]
    [TestCase("http://accounts.example/auth", false)]
    [TestCase("file:///C:/Windows/cmd.exe", false)]
    [TestCase("javascript:alert(1)", false)]
    [TestCase("not a URL", false)]
    public void BrowserAndApiUrlsMustUseSafeTransport(string value, bool expected) =>
        Assert.That(BrowserAuthPolicy.IsSafeUrl(value), Is.EqualTo(expected));

    /// <summary>Repeated connection failures cannot cause a busy loop or unbounded retry delay.</summary>
    [TestCase(-1, 2)]
    [TestCase(0, 2)]
    [TestCase(1, 4)]
    [TestCase(2, 8)]
    [TestCase(1000, 8)]
    public void NetworkBackoffStaysBounded(int failures, int expected) =>
        Assert.That(BrowserAuthPolicy.PollDelaySeconds(failures), Is.EqualTo(expected));
}
