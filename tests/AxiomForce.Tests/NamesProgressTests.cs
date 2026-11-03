namespace AxiomForce.Tests;

/// <summary>Identity recovery prerequisites, finite defense ordering, retry rewards and deployment rules without an engine host.</summary>
[TestFixture]
public class NamesProgressTests
{
    /// <summary>Unknown IDs, early records, exclusion and extraction must never advance a fresh run.</summary>
    [TestCase("record:1")][TestCase("record:2")][TestCase("record:3")][TestCase("isolate")]
    [TestCase("defense:2")][TestCase("commit")][TestCase("extract")][TestCase("encounter:unknown")]
    public void RejectsPrematureEvents(string id) => Assert.That(new NamesProgress().Complete(id),Is.False);

    /// <summary>Every record and defense needs its own defenders; previous clearances cannot substitute for later waves.</summary>
    [Test]
    public void CompleteChainRequiresExactDefendersAndRejectsDuplicates()
    {
        var progress=new NamesProgress();
        foreach(string objective in NamesProgress.Objectives)
        {
            foreach(string formation in NamesProgress.Formations)
                if(progress.CanStartFormation(formation)) Assert.That(progress.Complete("encounter:"+formation),Is.True);
            Assert.That(progress.Complete(objective),Is.True,objective);
            Assert.That(progress.Complete(objective),Is.False,objective+" duplicate");
        }
        Assert.That(progress.CanExtract,Is.True);
        Assert.That(new NamesProgress().CanExtract,Is.False,"Replay starts fresh");
    }
    /// <summary>Defenders are mandatory even if the player reaches an objective through another traversal route.</summary>
    [Test]
    public void FormationClearanceCannotBeSkipped()
    {
        var progress=new NamesProgress();
        Assert.That(progress.Complete("phase"),Is.False);
        Assert.That(progress.Complete("encounter:service1"),Is.True);
        Assert.That(progress.Complete("phase"),Is.False);
        Assert.That(progress.Complete("encounter:service2"),Is.True);
        Assert.That(progress.Complete("phase"),Is.True);
        Assert.That(progress.Complete("signal"),Is.False);
        Assert.That(progress.Complete("encounter:core2"),Is.False);
    }
    /// <summary>A killed slot cannot award points twice when its unfinished formation is rebuilt after recovery.</summary>
    [Test]
    public void RetryRewardsAreBoundedToAuthoredSlots()
    {
        var progress=new NamesProgress();
        Assert.That(progress.Reward("service1:0"),Is.True);
        Assert.That(progress.Reward("service1:0"),Is.False);
        Assert.That(progress.Reward("service1:3"),Is.False);
        Assert.That(progress.Reward("unknown:0"),Is.False);
        Assert.That(progress.Reward("service1:1"),Is.True);
    }
    /// <summary>Surya remains earned, cannot be selected directly or through cycling, and other missions still field him.</summary>
    [Test]
    public void MissionFiveKeepsSuryaAtTheQueue()
    {
        var roster=OperativeRoster.ForLevelStart(5);
        Assert.That(roster.IsUnlocked("Surya"),Is.True);
        Assert.That(roster.Available,Has.Count.EqualTo(5));
        Assert.That(roster.SetActive("Arjun"),Is.False);
        Assert.That(roster.Active,Is.EqualTo(OperativeRoster.Starter));
        for(int i=0;i<12;i++) Assert.That(roster.Cycle(1),Is.Not.EqualTo("Surya Prime"));
        Assert.That(roster.AvailabilityReason("Surya"),Is.EqualTo("Protecting the queue"));
        var fourth=OperativeRoster.ForLevelStart(4);fourth.Unlock("Surya");
        Assert.That(fourth.SetActive("Surya"),Is.True);
        Assert.That(LevelCatalog.HasNext(4),Is.True);
        Assert.That(LevelCatalog.HasNext(5),Is.True);
        Assert.That(LevelCatalog.HasNext(6),Is.False);
        Assert.That(LevelCatalog.Get(5).GrantsOperatives,Is.Empty);
    }
}
