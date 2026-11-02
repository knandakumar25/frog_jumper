/// <summary>Pure objective contract tests: dependencies, exact IDs, branch convergence and nonfarmable rewards.</summary>
public class AegisProgressTests
{
    /// <summary>Completes the minimum defended path; ordinary formations are deliberately omitted.</summary>
    private static AegisProgress Ready()
    {
        var p = new AegisProgress();
        foreach (string id in new[] { "encounter:s1-3", "interlock:approach", "encounter:s2-1", "encounter:s2-2",
            "recruit:surya", "lesson:bulwark", "crossing:bulwark", "encounter:s3-3", "coolant:intake", "encounter:s4-3",
            "guns:western", "lesson:blink", "encounter:s5-3", "curtain:inner", "boss:voss", "crossing:open" })
            Assert.That(p.Complete(id), Is.True, id);
        return p;
    }
    /// <summary>Unknown, repeated and premature triggers cannot skip the mission chain.</summary>
    [Test]
    public void RequiresExactDependencies()
    {
        var p = new AegisProgress();
        foreach (string id in AegisProgress.Objectives) Assert.That(p.Complete(id), Is.False, id);
        Assert.That(p.Complete("encounter:s1-30"), Is.False);
        Assert.That(p.Complete("encounter:s1-3"), Is.True);
        Assert.That(p.Complete("interlock:approach"), Is.True);
        Assert.That(p.Complete("interlock:approach"), Is.False);
        Assert.That(p.CurrentObjective, Is.EqualTo("recruit:surya"));
        Assert.That(p.CanExtract, Is.False);
    }
    /// <summary>Extraction is objective-based even when bypassable ordinary formations remain alive.</summary>
    [Test]
    public void OptionalKillsAreNotExtractionDependencies()
    {
        var p = Ready();
        Assert.That(p.CanExtract, Is.True);
        Assert.That(p.Has("encounter:s1-1"), Is.False);
        Assert.That(p.CurrentObjective, Is.EqualTo("extract"));
        Assert.That(p.Complete("guns:western"), Is.False, "Both routes converge on one completion");
        Assert.That(new AegisProgress().CanExtract, Is.False, "Replay starts fresh");
    }
    /// <summary>Every missing objective/defender blocks the remaining chain, including Voss and crossing release.</summary>
    [Test]
    public void MissingAnyDependencyBlocksExtraction()
    {
        string[] chain = { "encounter:s1-3", "interlock:approach", "encounter:s2-1", "encounter:s2-2", "recruit:surya",
            "lesson:bulwark", "crossing:bulwark", "encounter:s3-3", "coolant:intake", "encounter:s4-3", "guns:western",
            "lesson:blink", "encounter:s5-3", "curtain:inner", "boss:voss", "crossing:open" };
        foreach (string omitted in chain)
        {
            var p = new AegisProgress();
            foreach (string id in chain.Where(id => id != omitted)) p.Complete(id);
            Assert.That(p.CanExtract, Is.False, omitted);
        }
    }
    /// <summary>Only authored slots can award score, once even when an incomplete formation is retried.</summary>
    [Test]
    public void KillSlotsCannotBeFarmed()
    {
        var p = new AegisProgress();
        foreach (string encounter in AegisProgress.Encounters)
        for (int slot = 0; slot < 3; slot++)
        {
            string id = encounter[10..] + ":" + slot;
            Assert.That(p.Reward(id), Is.True);
            Assert.That(p.Reward(id), Is.False);
        }
        Assert.That(p.Reward("s1-1:3"), Is.False);
        Assert.That(p.Reward("s1-10:0"), Is.False);
        Assert.That(p.Reward("boss:voss"), Is.True);
        Assert.That(p.Reward("boss:voss"), Is.False);
    }
}
