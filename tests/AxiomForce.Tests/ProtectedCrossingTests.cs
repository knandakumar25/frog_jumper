/// <summary>Spatial evidence requires the entire protected footprint, including expiry and bypass rejection.</summary>
public class ProtectedCrossingTests
{
    /// <summary>A thin laser can be crossed by a real phase blink; merely activating next to it does not count.</summary>
    [Test]
    public void BlinkCrossesBoundaryButStationaryAbilityDoesNot()
    {
        var crossing = new ProtectedCrossing(100, 160, 0, 600, 240);
        Assert.That(crossing.Sample(50, 554, false), Is.False);
        Assert.That(crossing.Sample(50, 554, true), Is.False);
        Assert.That(crossing.Sample(230, 554, true), Is.True);
    }

    /// <summary>Bulwark must stay active from entry through exit; touching the first edge is insufficient.</summary>
    [Test]
    public void HeatFootprintRequiresContinuousProtection()
    {
        var crossing = new ProtectedCrossing(100, 400, 0, 600, 80);
        crossing.Sample(80, 554, false);
        for (int x = 120; x <= 360; x += 40) Assert.That(crossing.Sample(x, 554, true), Is.False);
        Assert.That(crossing.Sample(410, 554, false), Is.False);
        crossing.Reset(); crossing.Sample(80, 554, false);
        for (int x = 120; x <= 360; x += 40) Assert.That(crossing.Sample(x, 554, true), Is.False);
        Assert.That(crossing.Sample(410, 554, true), Is.True);
    }

    /// <summary>Crossing above the field, teleporting, or entering from the wrong side cannot award progress.</summary>
    [TestCase(300, -20, true)]
    [TestCase(900, 554, true)]
    [TestCase(300, 554, false)]
    public void InvalidSamplesDoNotCount(float x, float y, bool protection)
    {
        var crossing = new ProtectedCrossing(100, 200, 0, 600, 240);
        crossing.Sample(50, 554, true);
        Assert.That(crossing.Sample(x, y, protection), Is.False);
    }

    /// <summary>Resetting an unfinished arena discards its phases and defenders, but retains earlier progress and rewards.</summary>
    [Test]
    public void ArenaRetryPreservesPreArenaAndDeduplicatesRewards()
    {
        var ledger = new JudgmentDayProgress();
        foreach (string objective in JudgmentDayProgress.Objectives.TakeWhile(id => id != "void_commander_phase:3"))
        {
            foreach (string formation in JudgmentDayProgress.Formations)
                if (ledger.CanStartFormation(formation)) ledger.Complete("encounter:" + formation);
            Assert.That(ledger.Complete(objective), Is.True);
        }
        Assert.That(ledger.Reward("boss1:0"), Is.True);
        ledger.ResetArena("void_commander_phase", new[] { "boss1", "boss2", "boss3" });
        Assert.That(ledger.Has("cathedral_vault_scaled"), Is.True);
        Assert.That(ledger.Has("void_commander_phase:1"), Is.False);
        Assert.That(ledger.Has("encounter:boss1"), Is.False);
        Assert.That(ledger.Reward("boss1:0"), Is.False);
        Assert.That(ledger.CanStartFormation("boss1"), Is.True);
        Assert.That(ledger.CanStartFormation("boss2"), Is.False);
    }
}
