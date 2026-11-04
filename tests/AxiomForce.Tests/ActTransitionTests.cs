using System;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>
/// Unit tests verifying Act II End Screen, Act III Start Screen, and Stratos Docks Intro metadata,
/// scene path existence, and LevelCatalog registrations in pure NUnit.
/// </summary>
[TestFixture]
public class ActTransitionTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>Verifies ActIIEndScreen and ActIIIStartScreen scenes exist on disk.</summary>
    [Test]
    public void ActTransitionScenesExistOnDisk()
    {
        string root = FindProjectRoot();
        string act2End = Path.Combine(root, "scenes", "ui", "act_ii_end_screen.tscn");
        string act3Start = Path.Combine(root, "scenes", "ui", "act_iii_start_screen.tscn");
        string stratosIntro = Path.Combine(root, "scenes", "cutscenes", "stratos_docks_intro.tscn");
        string level8Scene = Path.Combine(root, "scenes", "gameplay", "level8.tscn");

        Assert.That(File.Exists(act2End), Is.True, $"Act II End Screen scene missing at {act2End}");
        Assert.That(File.Exists(act3Start), Is.True, $"Act III Start Screen scene missing at {act3Start}");
        Assert.That(File.Exists(stratosIntro), Is.True, $"Stratos Docks Intro scene missing at {stratosIntro}");
        Assert.That(File.Exists(level8Scene), Is.True, $"Level 8 scene missing at {level8Scene}");
    }

    /// <summary>Verifies LevelCatalog registers Mission 8 with all 6 abilities required.</summary>
    [Test]
    public void LevelCatalogRegistersMission8StratosDocks()
    {
        Assert.That(LevelCatalog.Count, Is.GreaterThanOrEqualTo(8));
        var entry = LevelCatalog.Get(8);

        Assert.That(entry.Number, Is.EqualTo(8));
        Assert.That(entry.Title, Is.EqualTo("Stratos Docks"));
        Assert.That(entry.ScenePath, Is.EqualTo("res://scenes/gameplay/level8.tscn"));
        Assert.That(entry.RequiredAbilities.Count, Is.EqualTo(6));
    }

    /// <summary>Verifies Stratos Docks Intro duration and cues.json schema matching 9 cues.</summary>
    [Test]
    public void StratosDocksIntroCuesSchemaMatches90SecondsDuration()
    {
        Assert.That(StratosDocksIntro.Duration, Is.EqualTo(90.0));

        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets", "cutscenes", "stratos_docks_intro", "cues.json");
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
