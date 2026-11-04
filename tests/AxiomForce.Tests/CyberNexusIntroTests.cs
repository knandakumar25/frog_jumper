using System;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit tests verifying Cyber-Nexus Intro metadata, scene paths, cues.json schema,
/// and LevelCatalog Mission 10 registration in pure NUnit.
/// </summary>
[TestFixture]
public class CyberNexusIntroTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>Verifies Cyber-Nexus intro scene and level 10 scene exist on disk.</summary>
    [Test]
    public void CyberNexusIntroScenesExistOnDisk()
    {
        string root = FindProjectRoot();
        string introScene = Path.Combine(root, "scenes", "cutscenes", "cyber_nexus_intro.tscn");
        string level10Scene = Path.Combine(root, "scenes", "gameplay", "level10.tscn");

        Assert.That(File.Exists(introScene), Is.True, $"Cyber-Nexus Intro scene missing at {introScene}");
        Assert.That(File.Exists(level10Scene), Is.True, $"Level 10 scene missing at {level10Scene}");
    }

    /// <summary>Verifies LevelCatalog registers Mission 10 with all 6 abilities required.</summary>
    [Test]
    public void LevelCatalogRegistersMission10CyberNexus()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(10));
        var entry = LevelCatalog.Get(10);

        Assert.That(entry.Number, Is.EqualTo(10));
        Assert.That(entry.Title, Is.EqualTo("The Cyber-Nexus"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level10.tscn"));
        Assert.That(entry.RequiredAbilities.Count, Is.EqualTo(6));
    }

    /// <summary>Verifies Cyber-Nexus Intro duration and cues.json schema matching 9 cues.</summary>
    [Test]
    public void CyberNexusIntroCuesSchemaMatches90SecondsDuration()
    {
        Assert.That(CyberNexusIntro.Duration, Is.EqualTo(90.0));

        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets", "cutscenes", "cyber_nexus_intro", "cues.json");
        Assert.That(File.Exists(cuesPath), Is.True, $"cues.json missing at {cuesPath}");

        string json = File.ReadAllText(cuesPath);
        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement;

        Assert.That(array.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(array.GetArrayLength(), Is.EqualTo(9));

        double lastEnd = 0.0;
        foreach (var element in array.EnumerateArray())
        {
            Assert.That(element.TryGetProperty("speaker", out var speakerProp), Is.True);
            Assert.That(element.TryGetProperty("text", out var textProp), Is.True);
            Assert.That(element.TryGetProperty("start", out var startProp), Is.True);
            Assert.That(element.TryGetProperty("end", out var endProp), Is.True);

            double start = startProp.GetDouble();
            double end = endProp.GetDouble();

            Assert.That(start, Is.GreaterThanOrEqualTo(lastEnd - 0.01));
            Assert.That(end, Is.GreaterThan(start));
            lastEnd = end;
        }

        Assert.That(lastEnd, Is.EqualTo(90.0));
    }
}
