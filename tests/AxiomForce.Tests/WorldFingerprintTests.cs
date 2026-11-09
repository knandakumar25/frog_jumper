using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

/// <summary>
/// Guards the Los Angeles world against change while worlds become data. The expected value was
/// recorded from the code as it stood before the multi-city refactor, so any difference in
/// geography, terrain or generated props fails here instead of silently moving a player's world.
/// </summary>
[TestFixture]
public class WorldFingerprintTests
{
    /// <summary>SHA-256 of the Los Angeles fingerprint text, recorded before the refactor.</summary>
    private const string Expected = "EBE76C3E2FDCB9D189FDC8BC37C1E42E3314D51FDF18C047CC11B13CBDAC5615";

    /// <summary>Spread of surface and underground chunks: corners, river, reservoir, cities, elevators, rail.</summary>
    private static readonly ChunkAddress[] Samples =
    {
        new(0, 0), new(999, 999), new(375, 125), new(343, 343), new(250, 250), new(343, 593),
        new(468, 500), new(600, 843), new(990, 400), new(500, 843), new(343, 345, 1), new(593, 843, 2)
    };

    /// <summary>Builds a stable text description of the catalog and the sampled generated chunks.</summary>
    private static string Describe()
    {
        var text = new StringBuilder();
        text.AppendLine($"{WorldCatalog.Identity}|{WorldCatalog.Size}|{WorldCatalog.RegionSize}|{WorldCatalog.ChunkSize}");
        foreach (var r in WorldCatalog.Regions) text.AppendLine($"R{r.Id}|{r.Name}|{r.Biome}");
        foreach (var s in WorldCatalog.Stations) text.AppendLine($"S{s.Id}|{s.Position}");
        foreach (var e in WorldCatalog.Entrances) text.AppendLine($"E{e.Mission}|{e.Position}");
        foreach (var l in WorldCatalog.Landmarks) text.AppendLine($"L{l.Id}|{l.Name}|{l.Kind}|{l.Position}");
        foreach (var e in WorldCatalog.Elevators) text.AppendLine($"V{e}");
        foreach (var a in Samples)
        {
            var chunk = WorldGeneration.Generate(a);
            text.AppendLine($"C{a}|{Convert.ToHexString(chunk.Terrain)}");
            foreach (var p in chunk.Props) text.AppendLine($"P{p}");
        }
        return text.ToString().Replace("\r", "");
    }

    /// <summary>SHA-256 of Kyoto's fingerprint text, recorded before city data moved onto the shared builder.</summary>
    private const string ExpectedKyoto = "CEBE0B27C5FF3552F94899FC62E85AE987BC6F6968E959F9DA6B299D86A0A274";

    /// <summary>Describes Kyoto's geography, authored positions, written lines and sampled generated chunks.</summary>
    private static string DescribeKyoto()
    {
        var kyoto = KyotoCity.Definition;
        var text = new StringBuilder($"{kyoto.Identity}|{kyoto.Size}|{kyoto.Skyport}\n");
        foreach (var r in kyoto.Regions) text.AppendLine($"R{r.Id}|{r.Name}|{r.Biome}");
        foreach (var s in kyoto.Stations) text.AppendLine($"S{s.Id}|{s.Position}");
        foreach (var l in kyoto.Landmarks) text.AppendLine($"L{l.Id}|{l.Name}|{l.Kind}|{l.Position}");
        foreach (var r in kyoto.Content.Residents) text.AppendLine($"N{r.Id}|{r.DayLocation}|{r.EveningLocation}|{r.GreetingDialogue}");
        foreach (var q in kyoto.Content.Quests)
            foreach (var o in q.Objectives)
            {
                var talk = WorldDialogueCatalog.For(o);
                text.AppendLine($"O{q.Id}|{o.Id}|{o.Type}|{o.TargetLocation}|{talk.Speaker}|{string.Join("/", talk.Lines)}");
            }
        for (int x = 0; x < 250; x += 37)
            for (int y = 0; y < 250; y += 37)
            {
                var chunk = WorldGeneration.Generate(kyoto, new ChunkAddress(x, y));
                text.AppendLine($"C{x},{y}|{Convert.ToHexString(chunk.Terrain)}");
                foreach (var p in chunk.Props) text.AppendLine($"P{p}");
            }
        return text.ToString().Replace("\r", "");
    }

    /// <summary>Kyoto must be unchanged by later shared-city work.</summary>
    [Test]
    public void KyotoMatchesRecordedFingerprint() =>
        Assert.That(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(DescribeKyoto()))), Is.EqualTo(ExpectedKyoto));

    /// <summary>Los Angeles geography and generation must stay byte-for-byte identical.</summary>
    [Test]
    public void LosAngelesMatchesPreRefactorFingerprint()
    {
        string actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Describe())));
        Assert.That(actual, Is.EqualTo(Expected));
    }
}
