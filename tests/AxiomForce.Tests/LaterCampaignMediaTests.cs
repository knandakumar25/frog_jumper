using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Checks shipped media evidence, natural cue delivery and original artwork manifests without initializing Godot.</summary>
public class LaterCampaignMediaTests
{
    private static readonly string[] Families = { "cyber_grave", "foundry_forge", "stratos_docks", "spire_outer_hull", "cyber_nexus", "apex_bastion", "judgment_day" };

    /// <summary>Finds the source repository from the test output directory without depending on the process working directory.</summary>
    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Project root is required for media verification.");
    }

    /// <summary>Every shipped audio hash must match its complete decoder report; atmosphere tracks cannot be duplicates.</summary>
    [Test]
    public void AudioHashesMatchFourteenCompleteTimelinesAndDistinctBeds()
    {
        string root = Root();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "assets/cutscenes/later_campaign_validation.json")));
        var atmosphereHashes = new HashSet<string>();
        Assert.That(document.RootElement.EnumerateObject().Count(), Is.EqualTo(14));
        foreach (var scene in document.RootElement.EnumerateObject())
        foreach (var track in scene.Value.EnumerateArray())
        {
            string name = track.GetProperty("track").GetString()!;
            string hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, "assets/cutscenes", scene.Name, name + ".ogg"))));
            Assert.That(hash, Is.EqualTo(track.GetProperty("sha256").GetString()), scene.Name + "/" + name);
            Assert.That(track.GetProperty("decoded_duration").GetDouble(), Is.EqualTo(scene.Name == "cyber_grave_intro" ? 120 : 90).Within(.03));
            Assert.That(track.GetProperty("peak_dbfs").GetDouble(), Is.LessThan(-.4));
            if (name == "atmosphere") Assert.That(atmosphereHashes.Add(hash), Is.True);
        }
    }

    /// <summary>All 117 new cues retain their full natural-speed takes and resolve actual cast IDs from established provenance.</summary>
    [Test]
    public void NewSpeechUsesCompleteUnacceleratedTakesAndEstablishedVoices()
    {
        string root = Root();
        int count = 0;
        foreach (string family in Families)
        foreach (string suffix in new[] { "intro", "aftermath" })
        {
            string scene = family + "_" + suffix;
            if (scene == "cyber_grave_intro") continue; // Preserve the existing accepted recording.
            string path = Path.Combine(root, "assets/cutscenes", scene);
            using var timing = JsonDocument.Parse(File.ReadAllText(Path.Combine(path, "dialogue_timing.json")));
            using var speech = JsonDocument.Parse(File.ReadAllText(Path.Combine(path, "speech_provenance.json")));
            Assert.That(speech.RootElement.GetArrayLength(), Is.EqualTo(9));
            foreach (var cue in timing.RootElement.GetProperty("cues").EnumerateArray())
            {
                Assert.That(cue.GetProperty("tempo").GetDouble(), Is.EqualTo(1));
                Assert.That(cue.GetProperty("complete_take").GetBoolean(), Is.True);
                Assert.That(cue.GetProperty("end").GetDouble(), Is.LessThanOrEqualTo(cue.GetProperty("window_end").GetDouble()));
                count++;
            }
            foreach (var voice in speech.RootElement.EnumerateArray())
            {
                string provenance = File.ReadAllText(Path.Combine(root, voice.GetProperty("cast_source").GetString()!));
                Assert.That(provenance, Does.Contain(voice.GetProperty("voice_id").GetString()));
                Assert.That(voice.GetProperty("settings").GetProperty("speed").GetDouble(), Is.EqualTo(1));
            }
        }
        Assert.That(count, Is.EqualTo(117));
    }

    // The artwork hash and size check that lived here was replaced by ArtRegenerationSpecTests:
    // the Blender layers were deleted on 2026-11-11 and their sizes are now held in
    // docs/art_regeneration.json for SpriteCook regeneration.
}
