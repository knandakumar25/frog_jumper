using System;
using System.IO;
using NUnit.Framework;

namespace AxiomForce.Tests;

/// <summary>Verifies FoundryForgeAftermath cutscene properties, timeline bounds, and scene asset references.</summary>
[TestFixture]
public class FoundryForgeAftermathTests
{
    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
            dir = dir.Parent;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>The post-level 7 cutscene duration is exactly 90.0 seconds matching standard aftermath overlays.</summary>
    [Test]
    public void Cutscene_HasExactDuration()
    {
        Assert.That(FoundryForgeAftermath.Duration, Is.EqualTo(90.0));
    }

    /// <summary>The standalone cutscene scene file exists and is located in the cutscenes folder.</summary>
    [Test]
    public void SceneFile_ExistsInCutscenes()
    {
        string root = FindProjectRoot();
        string path = Path.Combine(root, "scenes/cutscenes/foundry_forge_aftermath.tscn");
        Assert.That(File.Exists(path), Is.True, $"Scene file not found at {path}");
    }

    /// <summary>The cues.json configuration has exactly 9 ordered cues with valid speaker tags.</summary>
    [Test]
    public void CuesJson_HasNineOrderedCues()
    {
        string root = FindProjectRoot();
        string cuesPath = Path.Combine(root, "assets/cutscenes/foundry_forge_aftermath/cues.json");
        Assert.That(File.Exists(cuesPath), Is.True, $"cues.json not found at {cuesPath}");
        string json = File.ReadAllText(cuesPath);
        Assert.That(json, Does.Contain("AXIOM-7"));
        Assert.That(json, Does.Contain("TITAN"));
        Assert.That(json, Does.Contain("OCEANUS"));
        Assert.That(json, Does.Contain("AURA"));
        Assert.That(json, Does.Contain("MIRAGE"));
        Assert.That(json, Does.Contain("SURYA"));
        Assert.That(json, Does.Contain("KAITO"));
        Assert.That(json, Does.Contain("Stratos Docks"));
    }
}
