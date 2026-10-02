namespace AxiomForce.Tests;

using NUnit.Framework;

[TestFixture]
public class CharacterProgressionTests
{
    [Test]
    public void TestProgressionCurve_Values()
    {
        // Level 1 -> 2: 100
        Assert.That(CharacterProgressionManager.GetRequiredSparksForLevel(1), Is.EqualTo(100));

        // Level 2 -> 3: 100 + 140*1 + 10*1 = 250
        Assert.That(CharacterProgressionManager.GetRequiredSparksForLevel(2), Is.EqualTo(250));

        // Level 3 -> 4: 100 + 140*2 + 10*4 = 420
        Assert.That(CharacterProgressionManager.GetRequiredSparksForLevel(3), Is.EqualTo(420));

        // Max Level (10) or beyond should return 0 required sparks
        Assert.That(CharacterProgressionManager.GetRequiredSparksForLevel(10), Is.EqualTo(0));
        Assert.That(CharacterProgressionManager.GetRequiredSparksForLevel(15), Is.EqualTo(0));
    }

    [Test]
    public void TestHeroUpgradeDefs_AllSixOperativesExist()
    {
        string[] operatives = {
            "Cyber Shogun",
            "Solaria",
            "Oceanus",
            "Surya Prime",
            "Mirage Blade",
            "Apex Titan"
        };

        foreach (var op in operatives)
        {
            var defs = CharacterProgressionManager.GetUpgradeDefs(op);
            Assert.That(defs, Is.Not.Null, $"Upgrade tracks for {op} should not be null");
            Assert.That(defs.Length, Is.EqualTo(3), $"{op} should have exactly 3 upgrade tracks");

            for (int i = 0; i < defs.Length; i++)
            {
                Assert.That(defs[i].Name, Is.Not.Empty, $"{op} track {i} must have a name");
                Assert.That(defs[i].StatBonusPerRank, Is.GreaterThan(0), $"{op} track {i} bonus must be positive");
                Assert.That(defs[i].RankDescriptions.Length, Is.GreaterThanOrEqualTo(5), $"{op} track {i} descriptions");
            }
        }
    }

    [Test]
    public void TestOperativeProgression_ProgressionDataModel()
    {
        var data = new CharacterProgressionManager.HeroProgressionData();
        Assert.That(data.Level, Is.EqualTo(1));
        Assert.That(data.CurrentSparks, Is.EqualTo(0));
        Assert.That(data.SkillPoints, Is.EqualTo(0));
        Assert.That(data.UpgradeRanks.Length, Is.EqualTo(3));
        Assert.That(data.UpgradeRanks[0], Is.EqualTo(0));
    }

    [Test]
    public void TestOperativeProgression_SaveDataModelDefaults()
    {
        var saveModel = new CharacterProgressionManager.SaveDataModel();
        Assert.That(saveModel.GlobalSparksBank, Is.EqualTo(250));
        Assert.That(saveModel.Operatives, Is.Not.Null);
    }
}
