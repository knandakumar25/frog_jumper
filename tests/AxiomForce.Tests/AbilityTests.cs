namespace AxiomForce.Tests;

using NUnit.Framework;

[TestFixture]
public class AbilityTests
{
    private class TestAbility
    {
        public string Name { get; set; } = "Cyber Leap";
        public float Cooldown { get; set; } = 2.0f;
        public float Damage { get; set; } = 35.0f;
        private float _timer = 0f;

        public bool IsReady => _timer <= 0f;
        public float CooldownProgress => _timer <= 0f ? 1.0f : 1.0f - (_timer / Cooldown);

        public bool Trigger()
        {
            if (!IsReady) return false;
            _timer = Cooldown;
            return true;
        }

        public void ProcessTick(float delta)
        {
            if (_timer > 0f)
            {
                _timer = MathF.Max(0f, _timer - delta);
            }
        }
    }

    private TestAbility _ability = null!;

    [SetUp]
    public void SetUp()
    {
        _ability = new TestAbility();
    }

    [Test]
    public void TestInitialState_IsReady()
    {
        Assert.That(_ability.IsReady, Is.True);
        Assert.That(_ability.CooldownProgress, Is.EqualTo(1.0f));
    }

    [Test]
    public void TestTrigger_StartsCooldown()
    {
        bool success = _ability.Trigger();
        Assert.That(success, Is.True);
        Assert.That(_ability.IsReady, Is.False);
        Assert.That(_ability.CooldownProgress, Is.EqualTo(0.0f));

        // Subsequent trigger while on cooldown should fail
        bool secondAttempt = _ability.Trigger();
        Assert.That(secondAttempt, Is.False);
    }

    [Test]
    public void TestCooldownProgression()
    {
        _ability.Trigger();
        _ability.ProcessTick(1.0f); // 1s out of 2s elapsed -> 50%

        Assert.That(_ability.IsReady, Is.False);
        Assert.That(_ability.CooldownProgress, Is.EqualTo(0.5f).Within(0.001f));

        _ability.ProcessTick(1.0f); // remaining 1s elapsed -> ready
        Assert.That(_ability.IsReady, Is.True);
        Assert.That(_ability.CooldownProgress, Is.EqualTo(1.0f));
    }
}
