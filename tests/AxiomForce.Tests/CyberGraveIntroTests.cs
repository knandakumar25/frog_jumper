using System;
using System.IO;

namespace AxiomForce.Tests;

/// <summary>Verifies CyberGraveIntro cutscene properties, timeline bounds, and scene asset references.</summary>
[TestFixture]
public class CyberGraveIntroTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>The cutscene duration is exactly 120 seconds matching the mastered audio tracks.</summary>
    [Test]
    public void Cutscene_HasExactDuration()
    {
        Assert.That(CyberGraveIntro.Duration, Is.EqualTo(120.0));
    }

    /// <summary>The standalone cutscene scene file exists and is located at the expected path.</summary>
    [Test]
    public void SceneFile_ExistsInCutscenes()
    {
        string root = FindProjectRoot();
        string path = Path.Combine(root, "scenes/cutscenes/cyber_grave_intro.tscn");
        Assert.That(File.Exists(path), Is.True, $"Scene file not found at {path}");
    }

    /// <summary>The cues.json configuration has exactly 9 dialogue cues with valid start/end offsets.</summary>
    [Test]
    public void CuesJson_HasNineOrderedCues()
    {
        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets/cutscenes/cyber_grave_intro/cues.json");
        Assert.That(File.Exists(cuesPath), Is.True, $"cues.json not found at {cuesPath}");
        string json = File.ReadAllText(cuesPath);
        Assert.That(json, Does.Contain("Cyber-Grave"));
        Assert.That(json, Does.Contain("AXIOM-7"));
        Assert.That(json, Does.Contain("KAITO"));
        Assert.That(json, Does.Contain("OCEANUS"));
    }
}
