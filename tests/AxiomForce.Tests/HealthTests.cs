namespace AxiomForce.Tests;

using NUnit.Framework;

[TestFixture]
public class HealthTests
{
    private class TestEntity
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public TestEntity(float maxHealth = 100f)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0) return;
            CurrentHealth = MathF.Max(0, CurrentHealth - amount);
        }

        public void Heal(float amount)
        {
            if (amount <= 0) return;
            CurrentHealth = MathF.Min(MaxHealth, CurrentHealth + amount);
        }
    }

    [Test]
    [TestCase(100f, 25f, 75f)]
    [TestCase(100f, 150f, 0f)]
    [TestCase(50f, 50f, 0f)]
    public void TestDamageCalculation(float initial, float damage, float expected)
    {
        var entity = new TestEntity(initial);
        entity.TakeDamage(damage);
        Assert.That(entity.CurrentHealth, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(50f, 25f, 75f)]
    [TestCase(90f, 30f, 100f)]
    public void TestHealCalculation(float current, float heal, float expected)
    {
        var entity = new TestEntity { MaxHealth = 100f };
        entity.TakeDamage(100f - current);
        entity.Heal(heal);
        Assert.That(entity.CurrentHealth, Is.EqualTo(expected));
    }

    [Test]
    public void TestDeathThreshold()
    {
        var entity = new TestEntity { MaxHealth = 100f };
        Assert.That(entity.IsDead, Is.False);
        entity.TakeDamage(100f);
        Assert.That(entity.IsDead, Is.True);
    }
}
