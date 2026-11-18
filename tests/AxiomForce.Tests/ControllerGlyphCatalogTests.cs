using Godot;
using System.IO;

/// <summary>
/// Controller button images: which set a controller gets, which file a binding draws, that every
/// file exists at 128x128, and how image markers become BBCode or plain text. No engine objects needed.
/// </summary>
public class ControllerGlyphCatalogTests
{
    [TestCase("Xbox 360 Controller", 0x045E, 0x028E, ControllerGlyphSet.Xbox360)]
    [TestCase("Xbox 360 Wireless Receiver", 0x045E, 0x0719, ControllerGlyphSet.Xbox360)]
    [TestCase("Xbox One Controller", 0x045E, 0x02EA, ControllerGlyphSet.XboxOne)]
    [TestCase("Xbox Wireless Controller", 0x045E, 0x0B13, ControllerGlyphSet.XboxSeries)]
    [TestCase("Xbox Series X Controller", 0x045E, 0x0B12, ControllerGlyphSet.XboxSeries)]
    [TestCase("Microsoft Xbox Controller", 0x045E, 0x7777, ControllerGlyphSet.XboxSeries)]
    [TestCase("DualSense Wireless Controller", 0x054C, 0x0CE6, ControllerGlyphSet.PS5)]
    [TestCase("DualSense Edge", 0x054C, 0x0DF2, ControllerGlyphSet.PS5)]
    [TestCase("PS4 Controller", 0x054C, 0x09CC, ControllerGlyphSet.PS5)]
    [TestCase("Nintendo Switch Pro Controller", 0x057E, 0x2009, ControllerGlyphSet.Switch2)]
    [TestCase("Joy-Con 2 (R)", 0x057E, 0x2066, ControllerGlyphSet.Switch2)]
    [TestCase("Logitech F310", 0x046D, 0xC216, ControllerGlyphSet.None)]
    /// <summary>USB IDs choose the closest set; DualShock 4 shows PS5 and every Nintendo pad shows Switch 2.</summary>
    public void VendorAndProductIdsPickTheClosestSet(string name, int vendor, int product, ControllerGlyphSet expected) =>
        Assert.That(ControllerGlyphCatalog.Detect(name, vendor, product), Is.EqualTo(expected));

    [TestCase("Xbox 360 Controller", ControllerGlyphSet.Xbox360)]
    [TestCase("Microsoft X-Box 360 pad", ControllerGlyphSet.Xbox360)]
    [TestCase("Xbox One S Controller", ControllerGlyphSet.XboxOne)]
    [TestCase("Xbox Series X Controller", ControllerGlyphSet.XboxSeries)]
    [TestCase("XInput Controller", ControllerGlyphSet.XboxSeries)]
    [TestCase("Sony DualSense Wireless Controller", ControllerGlyphSet.PS5)]
    [TestCase("DualShock 4", ControllerGlyphSet.PS5)]
    [TestCase("Nintendo Switch 2 Pro Controller", ControllerGlyphSet.Switch2)]
    [TestCase("Joy-Con (L)", ControllerGlyphSet.Switch2)]
    [TestCase("Retro Series Arcade Stick", ControllerGlyphSet.None)]
    [TestCase("Generic USB Joystick", ControllerGlyphSet.None)]
    [TestCase("", ControllerGlyphSet.None)]
    /// <summary>Without USB IDs the reported name decides; unknown pads, and "Series" on other brands, keep text.</summary>
    public void NameAloneSelectsTheSetWhenIdsAreMissing(string name, ControllerGlyphSet expected) =>
        Assert.That(ControllerGlyphCatalog.Detect(name, 0, 0), Is.EqualTo(expected));

    [TestCase(ControllerGlyphStyle.Xbox, ControllerGlyphSet.XboxSeries)]
    [TestCase(ControllerGlyphStyle.PlayStation, ControllerGlyphSet.PS5)]
    [TestCase(ControllerGlyphStyle.NintendoSwitch, ControllerGlyphSet.Switch2)]
    [TestCase(ControllerGlyphStyle.Xbox360, ControllerGlyphSet.Xbox360)]
    [TestCase(ControllerGlyphStyle.XboxOne, ControllerGlyphSet.XboxOne)]
    /// <summary>Saved style numbers 1-3 keep loading and now mean the newest consoles; 5 and 6 were appended.</summary>
    public void SavedStylesForceTheirSet(ControllerGlyphStyle style, ControllerGlyphSet expected) =>
        Assert.That(ControllerGlyphCatalog.ForcedSet(style), Is.EqualTo(expected));

    /// <summary>Automatic and Keyboard force nothing, and the saved numbers of the original five styles never change.</summary>
    [Test]
    public void AutomaticAndKeyboardForceNoSetAndOldNumbersAreStable()
    {
        Assert.That(ControllerGlyphCatalog.ForcedSet(ControllerGlyphStyle.AutoDetect), Is.Null);
        Assert.That(ControllerGlyphCatalog.ForcedSet(ControllerGlyphStyle.Keyboard), Is.Null);
        Assert.That(new[] { (int)ControllerGlyphStyle.AutoDetect, (int)ControllerGlyphStyle.Xbox, (int)ControllerGlyphStyle.PlayStation,
            (int)ControllerGlyphStyle.NintendoSwitch, (int)ControllerGlyphStyle.Keyboard, (int)ControllerGlyphStyle.Xbox360,
            (int)ControllerGlyphStyle.XboxOne }, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    /// <summary>Console export feature tags pick their set; a desktop build picks none.</summary>
    [Test]
    public void ConsoleFeatureTagsPickTheirSet()
    {
        Assert.That(ControllerGlyphCatalog.PlatformSet(f => f == "xbox"), Is.EqualTo(ControllerGlyphSet.XboxSeries));
        Assert.That(ControllerGlyphCatalog.PlatformSet(f => f == "ps5"), Is.EqualTo(ControllerGlyphSet.PS5));
        Assert.That(ControllerGlyphCatalog.PlatformSet(f => f == "switch2"), Is.EqualTo(ControllerGlyphSet.Switch2));
        Assert.That(ControllerGlyphCatalog.PlatformSet(f => f is "windows" or "pc"), Is.Null);
    }

    /// <summary>Bindings map to the right files; keyboard input and unknown pads get no image.</summary>
    [Test]
    public void BindingsMapToButtonFiles()
    {
        string root = ControllerGlyphCatalog.Root;
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.XboxSeries, PromptInputKind.ControllerButton, (int)JoyButton.A, 0),
            Is.EqualTo(root + "xbox_series/face_south.png"));
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.Xbox360, PromptInputKind.ControllerButton, (int)JoyButton.Back, 0),
            Is.EqualTo(root + "xbox_360/back.png"));
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.PS5, PromptInputKind.ControllerAxis, (int)JoyAxis.TriggerLeft, 1),
            Is.EqualTo(root + "ps5/trigger_left.png"));
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.Switch2, PromptInputKind.ControllerAxis, (int)JoyAxis.LeftY, -1),
            Is.EqualTo(root + "switch2/stick_left_up.png"));
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.PS5, PromptInputKind.ControllerButton, (int)JoyButton.Guide, 0), Is.Null);
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.None, PromptInputKind.ControllerButton, (int)JoyButton.A, 0), Is.Null);
        Assert.That(ControllerGlyphCatalog.Path(ControllerGlyphSet.XboxSeries, PromptInputKind.Keyboard, (int)Key.Space, 0), Is.Null);
    }

    [TestCase(ControllerGlyphSet.Xbox360)]
    [TestCase(ControllerGlyphSet.XboxOne)]
    [TestCase(ControllerGlyphSet.XboxSeries)]
    [TestCase(ControllerGlyphSet.PS5)]
    [TestCase(ControllerGlyphSet.Switch2)]
    /// <summary>Every file a set names exists as a 128x128 PNG, and every mapped binding resolves to one of them.</summary>
    public void EveryFileExistsAt128Pixels(ControllerGlyphSet set)
    {
        string folder = Path.Combine(ProjectRoot(), "assets", "ui", "input_prompts", ControllerGlyphCatalog.Folder(set)!);
        foreach (string name in ControllerGlyphCatalog.FileNames(set))
        {
            string file = Path.Combine(folder, name + ".png");
            Assert.That(File.Exists(file), Is.True, file);
            var (width, height) = PngSize(file);
            Assert.That((width, height), Is.EqualTo((128, 128)), file);
        }
        for (int button = 0; button <= (int)JoyButton.DpadRight; button++)
        {
            string? path = ControllerGlyphCatalog.Path(set, PromptInputKind.ControllerButton, button, 0);
            if (path != null) Assert.That(File.Exists(Path.Combine(ProjectRoot(), path.Substring("res://".Length))), Is.True, path);
        }
    }

    /// <summary>Markers become images in BBCode and names in plain text; surrounding brackets are escaped.</summary>
    [Test]
    public void MarkersBecomeImagesOrNames()
    {
        string marked = "Jump: [ " + InputGlyphText.Mark("res://a.png", "A") + " / Space ]";
        Assert.That(InputGlyphText.HasImage(marked), Is.True);
        Assert.That(InputGlyphText.ToPlain(marked), Is.EqualTo("Jump: [ A / Space ]"));
        Assert.That(InputGlyphText.ToBbcode(marked, 20), Is.EqualTo("Jump: [lb] [img height=20]res://a.png[/img] / Space [rb]"));
        Assert.That(InputGlyphText.ToPlain("plain"), Is.EqualTo("plain"));
        Assert.That(InputGlyphText.HasImage("plain"), Is.False);
    }

    /// <summary>Finds the repository root (the folder with project.godot) above the test output folder.</summary>
    private static string ProjectRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot"))) dir = dir.Parent;
        Assert.That(dir, Is.Not.Null, "project.godot not found above the test folder");
        return dir!.FullName;
    }

    /// <summary>Reads a PNG's width and height from its IHDR chunk (bytes 16-23, big-endian).</summary>
    private static (int, int) PngSize(string file)
    {
        byte[] head = new byte[24];
        using (var stream = File.OpenRead(file)) stream.ReadExactly(head);
        int Read(int at) => (head[at] << 24) | (head[at + 1] << 16) | (head[at + 2] << 8) | head[at + 3];
        return (Read(16), Read(20));
    }
}
