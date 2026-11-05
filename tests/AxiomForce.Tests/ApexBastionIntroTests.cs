using System;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit tests verifying Apex Bastion Intro metadata, scene paths, cues.json schema,
/// and LevelCatalog Mission 11 registration in pure NUnit.
/// </summary>
[TestFixture]
public class ApexBastionIntroTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>Verifies Apex Bastion intro scene and level 11 scene exist on disk.</summary>
    [Test]
    public void ApexBastionIntroScenesExistOnDisk()
    {
        string root = FindProjectRoot();
        string introScene = Path.Combine(root, "scenes", "cutscenes", "apex_bastion_intro.tscn");
        string level11Scene = Path.Combine(root, "scenes", "gameplay", "level11.tscn");

        Assert.That(File.Exists(introScene), Is.True, $"Apex Bastion Intro scene missing at {introScene}");
        Assert.That(File.Exists(level11Scene), Is.True, $"Level 11 scene missing at {level11Scene}");
    }

    /// <summary>Verifies LevelCatalog registers Mission 11 with all 6 abilities required.</summary>
    [Test]
    public void LevelCatalogRegistersMission11ApexBastion()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(11));
        var entry = LevelCatalog.Get(11);

        Assert.That(entry.Number, Is.EqualTo(11));
        Assert.That(entry.Title, Is.EqualTo("The Apex Bastion"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level11.tscn"));
        Assert.That(entry.RequiredAbilities.Count, Is.EqualTo(6));
    }

    /// <summary>Verifies Apex Bastion Intro duration and cues.json schema matching 9 cues.</summary>
    [Test]
    public void ApexBastionIntroCuesSchemaMatches90SecondsDuration()
    {
        Assert.That(ApexBastionIntro.Duration, Is.EqualTo(90.0));

        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets", "cutscenes", "apex_bastion_intro", "cues.json");
        Assert.That(File.Exists(cuesPath), Is.True, $"cues.json missing at {cuesPath}");

        string json = File.ReadAllText(cuesPath);
        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement;

        Assert.That(array.GetArrayLength(), Is.EqualTo(9));

        double lastEnd = 0.0;
        foreach (var item in array.EnumerateArray())
        {
            double start = item.GetProperty("start").GetDouble();
            double end = item.GetProperty("end").GetDouble();
            string speaker = item.GetProperty("speaker").GetString() ?? "";
            string text = item.GetProperty("text").GetString() ?? "";

            Assert.That(start, Is.GreaterThanOrEqualTo(lastEnd));
            Assert.That(end, Is.GreaterThan(start));
            Assert.That(speaker, Is.Not.Empty);
            Assert.That(text, Does.Contain(speaker));

            lastEnd = end;
        }

        Assert.That(lastEnd, Is.EqualTo(90.0));
    }
}
