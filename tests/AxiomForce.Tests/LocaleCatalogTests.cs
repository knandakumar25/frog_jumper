using System.Collections.Generic;
using System.Linq;

/// <summary>Pure locale identities and placeholder contracts, independent of Godot's native runtime.</summary>
public class LocaleCatalogTests
{
    /// <summary>Legacy catalog columns and all ISO names resolve to the same stable identities.</summary>
    [Test]
    public void AllSeventeenLanguagesRoundTripWithoutDuplicateSelectors()
    {
        Assert.That(LocaleCatalog.All, Has.Count.EqualTo(17));
        Assert.That(LocaleCatalog.All.Select(x => x.DisplayName).Distinct().Count(), Is.EqualTo(17));
        foreach (var language in LocaleCatalog.All)
        {
            Assert.That(LocaleCatalog.Resolve(language.Key), Is.EqualTo(language));
            Assert.That(LocaleCatalog.Resolve(language.Iso.Replace('_', '-')), Is.EqualTo(language));
        }
    }

    /// <summary>Region/script aliases preserve the two Chinese scripts and historical language codes.</summary>
    [TestCase("zh-Hant-HK", "traditionalchinese")]
    [TestCase("zh-Hans-SG", "mandarin")]
    [TestCase("zh-HK", "traditionalchinese")]
    [TestCase("zh-CN", "mandarin")]
    [TestCase("zh", "mandarin")]
    [TestCase("iw-IL", "hebrew")]
    [TestCase("in-ID", "indonesian")]
    [TestCase("fr-CA", "eurofrench")]
    [TestCase("pt-PT", "brazilianportuguese")]
    [TestCase("tr-TR", "turkish")]
    [TestCase("unsupported", "english")]
    public void AliasesUseExplicitLanguageIdentity(string alias, string expected) =>
        Assert.That(LocaleCatalog.Resolve(alias).Key, Is.EqualTo(expected));

    /// <summary>Mandarin must never accidentally select Arabic through a substring match.</summary>
    [Test]
    public void ScriptFontAndDirectionAreExplicit()
    {
        Assert.That(LocaleCatalog.Resolve("mandarin").Font, Is.EqualTo("NotoSansCJKsc-Regular.otf"));
        Assert.That(LocaleCatalog.Resolve("traditionalchinese").Font, Is.EqualTo("NotoSansCJKtc-Regular.otf"));
        Assert.That(LocaleCatalog.All.Where(x => x.Rtl).Select(x => x.Key), Is.EquivalentTo(new[] { "arabic", "hebrew" }));
    }

    /// <summary>Named fields can reorder and repeat while user-supplied braces remain literal.</summary>
    [Test]
    public void PlaceholderValuesAreInsertedOnlyOnce()
    {
        var values = new Dictionary<string, object> { ["name"] = "User {count}", ["count"] = 3 };
        Assert.That(LocalizedTemplate.Fill("{count}: {name} / {count}", values), Is.EqualTo("3: User {count} / 3"));
        Assert.That(LocalizedTemplate.Fill("{unknown}", values), Is.EqualTo("{unknown}"));
    }
}
