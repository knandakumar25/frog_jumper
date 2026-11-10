using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Checks the per-line voice gain table (assets/audio/voice_gain.json) against the shipped recordings,
/// and the pure parsing in <see cref="VoiceGain.Parse"/>, without initializing Godot.
/// </summary>
public class VoiceGainTests
{
    /// <summary>Finds the repository root from the test output directory.</summary>
    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Project root is required for voice gain checks.");
    }

    /// <summary>Gains are clamped to the generator's range; malformed entries and wrong shapes give no gain.</summary>
    [Test]
    public void ParseClampsGainsAndToleratesWrongShapes()
    {
        var gains = VoiceGain.Parse("""{"files": {"res://a.mp3": {"gain_db": 30}, "res://b.mp3": {"gain_db": -2.5}, "res://c.mp3": {}, "res://d.mp3": 4}}""");
        Assert.That(gains["res://a.mp3"], Is.EqualTo(VoiceGain.MaxGainDb));
        Assert.That(gains["res://b.mp3"], Is.EqualTo(-2.5f));
        Assert.That(gains.ContainsKey("res://c.mp3"), Is.False);
        Assert.That(gains.ContainsKey("res://d.mp3"), Is.False);
        Assert.That(VoiceGain.Parse("{}"), Is.Empty);
        Assert.That(VoiceGain.Parse("""{"files": []}"""), Is.Empty);
        Assert.That(VoiceGain.Parse("[]"), Is.Empty);
    }

    /// <summary>
    /// The table holds exactly the recordings the game plays, keyed by the same path rules the players
    /// use (<see cref="SpeechPaths"/>), each measured from the file that ships. A changed recording would
    /// otherwise keep a gain measured from its predecessor: re-run tools/assets/voice_gain.py.
    /// </summary>
    [Test]
    public void TableCoversExactlyThePlayedLinesAndMatchesShippedFiles()
    {
        string root = Root();
        using var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "assets/audio/voice_gain.json")));
        var files = table.RootElement.GetProperty("files");
        using var cues = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "assets/audio/english/cues.json")));
        var played = cues.RootElement.EnumerateArray()
            .Where(cue => cue.GetProperty("classification").GetString() == "speech")
            .Select(cue => SpeechPaths.CueResource(cue.GetProperty("audio_path").GetString()!))
            .Concat(Enumerable.Range(1, SpeechPaths.BriefingLineCount).Select(SpeechPaths.Briefing))
            .Append(SpeechPaths.DeploymentNarration)
            .ToHashSet();
        Assert.That(files.EnumerateObject().Select(entry => entry.Name).ToHashSet(), Is.EquivalentTo(played));
        foreach (string resPath in played)
        {
            string hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, resPath["res://".Length..]))));
            Assert.That(files.GetProperty(resPath).GetProperty("sha256").GetString(), Is.EqualTo(hash), "stale gain for " + resPath);
        }
    }

    /// <summary>
    /// Each gain follows the generator's rule: the boost toward the target, clamped to the gain limit and
    /// capped so the limiter removes at most the table's budget of peak. The recorded expected limiting
    /// must agree, and no line may ask the limiter for more than the budget.
    /// </summary>
    [Test]
    public void GainsFollowTheTargetAndLimiterBudget()
    {
        using var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "assets/audio/voice_gain.json")));
        var root = table.RootElement;
        double target = root.GetProperty("target_lufs").GetDouble();
        double ceiling = root.GetProperty("ceiling_dbtp").GetDouble();
        double budget = root.GetProperty("limit_budget_db").GetDouble();
        Assert.That(ceiling, Is.EqualTo(-1.0), "must match the Voice bus limiter in default_bus_layout.tres");
        foreach (var entry in root.GetProperty("files").EnumerateObject())
        {
            double gain = entry.Value.GetProperty("gain_db").GetDouble();
            double loudness = entry.Value.GetProperty("loudness_lufs").GetDouble();
            double peak = entry.Value.GetProperty("true_peak_dbtp").GetDouble();
            double expected = Math.Min(Math.Clamp(target - loudness, -VoiceGain.MaxGainDb, VoiceGain.MaxGainDb), ceiling + budget - peak);
            Assert.That(gain, Is.EqualTo(expected).Within(.02), entry.Name);
            double limiting = entry.Value.GetProperty("expected_limiting_db").GetDouble();
            Assert.That(limiting, Is.EqualTo(Math.Max(0, peak + gain - ceiling)).Within(.02), entry.Name);
            Assert.That(limiting, Is.LessThanOrEqualTo(budget + .01), entry.Name);
        }
    }
}
