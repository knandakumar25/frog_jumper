using System;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit tests verifying Spire Outer Hull Intro metadata, scene paths, cues.json schema,
/// and LevelCatalog Mission 9 registration in pure NUnit.
/// </summary>
[TestFixture]
public class SpireOuterHullIntroTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>Verifies Spire Outer Hull intro scene and level 9 scene exist on disk.</summary>
    [Test]
    public void SpireOuterHullIntroScenesExistOnDisk()
    {
        string root = FindProjectRoot();
        string introScene = Path.Combine(root, "scenes", "cutscenes", "spire_outer_hull_intro.tscn");
        string level9Scene = Path.Combine(root, "scenes", "gameplay", "level9.tscn");

        Assert.That(File.Exists(introScene), Is.True, $"Spire Outer Hull Intro scene missing at {introScene}");
        Assert.That(File.Exists(level9Scene), Is.True, $"Level 9 scene missing at {level9Scene}");
    }

    /// <summary>Verifies LevelCatalog registers Mission 9 with all 6 abilities required.</summary>
    [Test]
    public void LevelCatalogRegistersMission9SpireOuterHull()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(9));
        var entry = LevelCatalog.Get(9);

        Assert.That(entry.Number, Is.EqualTo(9));
        Assert.That(entry.Title, Is.EqualTo("Spire Outer Hull"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level9.tscn"));
        Assert.That(entry.RequiredAbilities.Count, Is.EqualTo(6));
    }

    /// <summary>Verifies Spire Outer Hull Intro duration and cues.json schema matching 9 cues.</summary>
    [Test]
    public void SpireOuterHullIntroCuesSchemaMatches90SecondsDuration()
    {
        Assert.That(SpireOuterHullIntro.Duration, Is.EqualTo(90.0));

        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets", "cutscenes", "spire_outer_hull_intro", "cues.json");
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
