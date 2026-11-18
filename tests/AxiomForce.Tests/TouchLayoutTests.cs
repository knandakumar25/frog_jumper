using Godot;
using System.IO;
using System.Linq;

/// <summary>
/// On-screen touch layouts: every action they press exists, the joystick splits into the right
/// action strengths, and the show/hide rule follows the owner's 2026-11-17 decision.
/// Engine-free: no native Godot objects are created.
/// </summary>
public class TouchLayoutTests
{
    private static readonly TouchLayout[] Layouts = { TouchLayout.Platformer, TouchLayout.Isometric, TouchLayout.World };

    /// <summary>Every action a layout presses is registered in project.godot or InputActionDefaults.</summary>
    [Test]
    public void EveryTouchActionIsARegisteredInputAction()
    {
        string root = ProjectRoot();
        string sources = File.ReadAllText(Path.Combine(root, "project.godot")) +
            File.ReadAllText(Path.Combine(root, "scripts", "input", "InputActionDefaults.cs"));
        foreach (var layout in Layouts)
            foreach (string action in layout.Actions())
                Assert.That(sources.Contains($"\"{action}\"") || sources.Contains($"\n{action}="), Is.True, $"{layout.Name}: {action}");
    }

    /// <summary>Each layout has buttons, no duplicate actions, and the expected main controls.</summary>
    [Test]
    public void LayoutsHaveTheirMainButtons()
    {
        Assert.That(TouchLayout.Platformer.Buttons.Select(b => b.Action), Is.SupersetOf(new[] { "jump", "attack", "karate", "dash", "escape", "squad_next" }));
        Assert.That(TouchLayout.Isometric.Buttons.Select(b => b.Action), Is.SupersetOf(new[] { "iso_primary_attack", "iso_special_ability", "iso_dash", "iso_interact", "iso_pause" }));
        Assert.That(TouchLayout.World.Buttons.Select(b => b.Action), Is.SupersetOf(new[] { "world_attack", "world_dodge", "world_defend", "world_interact", "world_run", "world_bike", "world_map", "world_swap" }));
        foreach (var layout in Layouts)
            Assert.That(layout.Actions().ToList(), Is.Unique, layout.Name);
    }

    /// <summary>The platformer stick never presses up, so a drifting thumb cannot jump.</summary>
    [Test]
    public void PlatformerStickOnlyMovesSideways()
    {
        var strengths = TouchLayout.Platformer.StickStrengths(new Vector2(-0.6f, -0.8f));
        Assert.That(strengths.Keys, Is.EquivalentTo(new[] { "left", "right" }));
        Assert.That(strengths["left"], Is.EqualTo(0.6f).Within(1e-4));
        Assert.That(strengths["right"], Is.EqualTo(0f));
    }

    /// <summary>A diagonal mixes two directions; Godot's Y axis grows downward.</summary>
    [Test]
    public void DiagonalStickMixesTwoDirections()
    {
        var strengths = TouchLayout.World.StickStrengths(new Vector2(0.6f, -0.8f));
        Assert.That(strengths["world_right"], Is.EqualTo(0.6f).Within(1e-4));
        Assert.That(strengths["world_up"], Is.EqualTo(0.8f).Within(1e-4));
        Assert.That(strengths["world_left"], Is.EqualTo(0f));
        Assert.That(strengths["world_down"], Is.EqualTo(0f));
    }

    /// <summary>Inside the dead zone, and on release, every direction is zero.</summary>
    [Test]
    public void DeadZoneAndReleaseClearEveryDirection()
    {
        foreach (var stick in new[] { new Vector2(0.05f, 0.05f), Vector2.Zero })
            Assert.That(TouchLayout.Isometric.StickStrengths(stick).Values, Is.All.EqualTo(0f));
    }

    /// <summary>Mobile starts shown and desktop hidden; a touch shows; a key, pad or mouse press hides; nothing else changes it.</summary>
    [Test]
    public void VisibilityFollowsTheLastDeliberateInput()
    {
        Assert.That(TouchVisibility.Initial(mobile: true), Is.True);
        Assert.That(TouchVisibility.Initial(mobile: false), Is.False);
        Assert.That(TouchVisibility.Next(false, TouchInputKind.Touch), Is.True);
        Assert.That(TouchVisibility.Next(true, TouchInputKind.KeyOrPadOrMouse), Is.False);
        Assert.That(TouchVisibility.Next(true, TouchInputKind.None), Is.True);
        Assert.That(TouchVisibility.Next(false, TouchInputKind.None), Is.False);
    }

    /// <summary>Finds the repository root (the folder with project.godot) above the test output folder.</summary>
    private static string ProjectRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot"))) dir = dir.Parent;
        Assert.That(dir, Is.Not.Null);
        return dir!.FullName;
    }
}
