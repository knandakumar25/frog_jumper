using System;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit tests verifying Judgment Day Intro metadata, scene paths, cues.json schema,
/// and LevelCatalog Mission 12 registration in pure NUnit.
/// </summary>
[TestFixture]
public class JudgmentDayIntroTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>Verifies Judgment Day intro scene and level 12 scene exist on disk.</summary>
    [Test]
    public void JudgmentDayIntroScenesExistOnDisk()
    {
        string root = FindProjectRoot();
        string introScene = Path.Combine(root, "scenes", "cutscenes", "judgment_day_intro.tscn");
        string level12Scene = Path.Combine(root, "scenes", "gameplay", "level12.tscn");

        Assert.That(File.Exists(introScene), Is.True, $"Judgment Day Intro scene missing at {introScene}");
        Assert.That(File.Exists(level12Scene), Is.True, $"Level 12 scene missing at {level12Scene}");
    }

    /// <summary>Verifies LevelCatalog registers Mission 12 with all 6 abilities required.</summary>
    [Test]
    public void LevelCatalogRegistersMission12JudgmentDay()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(12));
        var entry = LevelCatalog.Get(12);

        Assert.That(entry.Number, Is.EqualTo(12));
        Assert.That(entry.Title, Is.EqualTo("Judgment Day"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level12.tscn"));
        Assert.That(entry.RequiredAbilities.Count, Is.EqualTo(6));
    }

    /// <summary>Verifies Judgment Day Intro duration and cues.json schema matching 9 cues.</summary>
    [Test]
    public void JudgmentDayIntroCuesSchemaMatches90SecondsDuration()
    {
        Assert.That(JudgmentDayIntro.Duration, Is.EqualTo(90.0));

        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets", "cutscenes", "judgment_day_intro", "cues.json");
        Assert.That(File.Exists(cuesPath), Is.True, $"cues.json missing at {cuesPath}");

        string json = File.ReadAllText(cuesPath);
        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement;

        Assert.That(array.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(array.GetArrayLength(), Is.EqualTo(9));

        double lastEnd = 0.0;
        foreach (var item in array.EnumerateArray())
        {
            double start = item.GetProperty("start").GetDouble();
            double duration = item.GetProperty("duration").GetDouble();
            string speaker = item.GetProperty("speaker").GetString()!;
            string text = item.GetProperty("text").GetString()!;

            Assert.That(start, Is.GreaterThanOrEqualTo(lastEnd));
            Assert.That(duration, Is.GreaterThan(0.0));
            Assert.That(string.IsNullOrWhiteSpace(speaker), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(text), Is.False);
            lastEnd = start + duration;
        }

        Assert.That(lastEnd, Is.EqualTo(90.0));
    }
}
