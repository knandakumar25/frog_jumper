using System.Buffers.Binary;
using System.Text.Json;

/// <summary>
/// Guards the art regeneration list in <c>docs/art_regeneration.json</c>.
/// </summary>
/// <remarks>
/// The Blender-made images were deleted on 2026-11-11 and are being remade with SpriteCook. The game
/// code expects each file at its old path and pixel size, so a regenerated image may be absent
/// (still waiting) or present at exactly the listed size. Anything else, such as an image at the
/// wrong size or a non-PNG file under a listed name, fails here before it reaches the game.
/// </remarks>
public class ArtRegenerationSpecTests
{
    /// <summary>Finds the repository root from the test output directory.</summary>
    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Project root is required for the art spec.");
    }

    /// <summary>Reads every listed image as (relative path, expected width, expected height).</summary>
    private static List<(string Path, int Width, int Height)> Spec()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "docs/art_regeneration.json")));
        var images = new List<(string, int, int)>();
        foreach (var group in document.RootElement.GetProperty("groups").EnumerateArray())
        foreach (var image in group.GetProperty("images").EnumerateArray())
            images.Add((image.GetProperty("path").GetString()!, image.GetProperty("width").GetInt32(), image.GetProperty("height").GetInt32()));
        return images;
    }

    /// <summary>
    /// The list must still describe every deleted image, with no duplicate paths: the 422 Blender
    /// images deleted on 2026-11-11 plus the 115 remaining images (hand-drawn studies, older logos,
    /// UI art, backgrounds and icons) deleted on 2026-11-12.
    /// </summary>
    [Test]
    public void SpecListsEveryDeletedImageOnce()
    {
        var images = Spec();
        Assert.That(images, Has.Count.EqualTo(537));
        Assert.That(images.Select(i => i.Path).Distinct().Count(), Is.EqualTo(images.Count));
        Assert.That(images.All(i => i.Width > 0 && i.Height > 0), Is.True);
    }

    /// <summary>
    /// Every image has a status: "remake", "skip: ..." (nothing loads it) or "copy of `path`", where
    /// the copied path is itself a listed image to remake. This keeps credits off unused files.
    /// </summary>
    [Test]
    public void EveryImageHasAValidStatus()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "docs/art_regeneration.json")));
        var statuses = new Dictionary<string, string>();
        foreach (var group in document.RootElement.GetProperty("groups").EnumerateArray())
        foreach (var image in group.GetProperty("images").EnumerateArray())
            statuses[image.GetProperty("path").GetString()!] = image.GetProperty("status").GetString()!;
        foreach (var (path, status) in statuses)
        {
            if (status == "remake" || status.StartsWith("skip: ")) continue;
            Assert.That(status, Does.StartWith("copy of `"), path);
            string source = status["copy of `".Length..].TrimEnd('`');
            Assert.That(statuses.GetValueOrDefault(source), Is.EqualTo("remake"), path + " copies " + source);
        }
    }

    /// <summary>
    /// Each listed PNG is either absent or a PNG at exactly the listed size. The few JPG, ICO and SVG
    /// entries (old logos and icons) are not size-checked here: their headers differ, and an SVG
    /// has no fixed pixel size.
    /// </summary>
    [Test]
    public void RegeneratedImagesAreAbsentOrTheListedSize()
    {
        string root = Root();
        foreach (var (path, width, height) in Spec())
        {
            string file = Path.Combine(root, path);
            if (!File.Exists(file) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            byte[] header = new byte[24];
            using (var stream = File.OpenRead(file)) stream.ReadExactly(header);
            // PNG signature, then the IHDR chunk holding big-endian width and height at bytes 16-23.
            Assert.That(header.AsSpan(1, 3).SequenceEqual("PNG"u8), Is.True, path + " is not a PNG");
            Assert.That(BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)), Is.EqualTo(width), path + " width");
            Assert.That(BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)), Is.EqualTo(height), path + " height");
        }
    }
}
