using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Checks the mission music gain table (assets/audio/music_gain.json) against the MusicPath tracks in
/// scripts/gameplay and the shipped files, and the shared parsing, without initializing Godot.
/// </summary>
public class MusicGainTests
{
    /// <summary>Finds the repository root from the test output directory.</summary>
    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Project root is required for music gain checks.");
    }

    /// <summary>The music limit is wider than the voice limit, because the default track needs a large cut.</summary>
    [Test]
    public void ParseUsesTheMusicLimit()
    {
        var gains = MusicGain.Parse("""{"files": {"res://a.mp3": {"gain_db": -40}, "res://b.ogg": {"gain_db": -17.7}}}""");
        Assert.That(gains["res://a.mp3"], Is.EqualTo(-MusicGain.MaxGainDb));
        Assert.That(gains["res://b.ogg"], Is.EqualTo(-17.7f));
    }

    /// <summary>
    /// Every MusicPath in scripts/gameplay (the same pattern tools/assets/music_gain.py reads) has a gain
    /// measured from the shipped file, and the table lists nothing else. A new or changed track fails
    /// here until tools/assets/music_gain.py is re-run.
    /// </summary>
    [Test]
    public void TableCoversExactlyTheMissionTracksAndMatchesShippedFiles()
    {
        string root = Root();
        var pattern = new Regex("MusicPath\\s*=>\\s*\"(res://[^\"]+)\"");
        var tracks = Directory.GetFiles(Path.Combine(root, "scripts/gameplay"), "*.cs")
            .SelectMany(file => pattern.Matches(File.ReadAllText(file)).Select(match => match.Groups[1].Value))
            .ToHashSet();
        Assert.That(tracks, Has.Count.GreaterThan(1));
        using var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "assets/audio/music_gain.json")));
        var files = table.RootElement.GetProperty("files");
        Assert.That(files.EnumerateObject().Select(entry => entry.Name).ToHashSet(), Is.EquivalentTo(tracks));
        foreach (string resPath in tracks)
        {
            string hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, resPath["res://".Length..]))));
            Assert.That(files.GetProperty(resPath).GetProperty("sha256").GetString(), Is.EqualTo(hash), "stale gain for " + resPath);
        }
    }

    /// <summary>Each gain brings its track to the target, capped so its true peak stays at or below the ceiling.</summary>
    [Test]
    public void GainsFollowTheTargetAndPeakCeiling()
    {
        using var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "assets/audio/music_gain.json")));
        var root = table.RootElement;
        double target = root.GetProperty("target_lufs").GetDouble();
        double ceiling = root.GetProperty("ceiling_dbtp").GetDouble();
        // Pinned so a regenerated table with a wrong target, ceiling or limit fails here.
        Assert.That(target, Is.EqualTo(-29.0), "music_gain.py MUSIC_LUFS");
        Assert.That(ceiling, Is.EqualTo(-1.0), "music_gain.py CEILING_DBTP");
        Assert.That(root.GetProperty("max_gain_db").GetDouble(), Is.EqualTo(MusicGain.MaxGainDb), "music_gain.py MAX_GAIN_DB");
        foreach (var entry in root.GetProperty("files").EnumerateObject())
        {
            double gain = entry.Value.GetProperty("gain_db").GetDouble();
            double loudness = entry.Value.GetProperty("loudness_lufs").GetDouble();
            double peak = entry.Value.GetProperty("true_peak_dbtp").GetDouble();
            double expected = Math.Min(Math.Clamp(target - loudness, -MusicGain.MaxGainDb, MusicGain.MaxGainDb), ceiling - peak);
            Assert.That(gain, Is.EqualTo(expected).Within(.02), entry.Name);
            Assert.That(peak + gain, Is.LessThanOrEqualTo(ceiling + .01), entry.Name);
        }
    }
}
