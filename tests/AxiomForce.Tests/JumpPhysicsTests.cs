namespace AxiomForce.Tests;

using NUnit.Framework;

[TestFixture]
public class JumpPhysicsTests
{
    public static class FrogTrajectoryMath
    {
        public static float CalculateJumpVelocity(float jumpHeight, float gravity)
        {
            if (jumpHeight <= 0 || gravity <= 0) return 0f;
            return MathF.Sqrt(2f * gravity * jumpHeight);
        }

        public static float CalculateTimeToApex(float jumpVelocity, float gravity)
        {
            if (gravity <= 0) return 0f;
            return jumpVelocity / gravity;
        }

        public static float CalculateTotalAirTime(float jumpVelocity, float gravity)
        {
            return CalculateTimeToApex(jumpVelocity, gravity) * 2f;
        }

        public static float CalculateJumpDistance(float horizontalSpeed, float airTime)
        {
            return horizontalSpeed * airTime;
        }
    }

    [Test]
    [TestCase(4.0f, 9.8f, 8.854f)]
    [TestCase(2.5f, 15.0f, 8.660f)]
    public void TestJumpVelocity(float height, float gravity, float expectedVelocity)
    {
        float velocity = FrogTrajectoryMath.CalculateJumpVelocity(height, gravity);
        Assert.That(velocity, Is.EqualTo(expectedVelocity).Within(0.01f));
    }

    [Test]
    [TestCase(10.0f, 20.0f, 0.5f, 1.0f)]
    [TestCase(15.0f, 10.0f, 1.5f, 3.0f)]
    public void TestAirTime(float velocity, float gravity, float expectedApex, float expectedTotalAir)
    {
        float apex = FrogTrajectoryMath.CalculateTimeToApex(velocity, gravity);
        float total = FrogTrajectoryMath.CalculateTotalAirTime(velocity, gravity);

        Assert.That(apex, Is.EqualTo(expectedApex).Within(0.001f));
        Assert.That(total, Is.EqualTo(expectedTotalAir).Within(0.001f));
    }

    [Test]
    [TestCase(8.0f, 1.5f, 12.0f)]
    [TestCase(12.5f, 2.0f, 25.0f)]
    public void TestHorizontalDistance(float horizontalSpeed, float airTime, float expectedDistance)
    {
        float distance = FrogTrajectoryMath.CalculateJumpDistance(horizontalSpeed, airTime);
        Assert.That(distance, Is.EqualTo(expectedDistance).Within(0.01f));
    }

    [Test]
    public void TestZeroGravityOrHeight_ReturnsZero()
    {
        Assert.That(FrogTrajectoryMath.CalculateJumpVelocity(0f, 9.8f), Is.EqualTo(0f));
        Assert.That(FrogTrajectoryMath.CalculateJumpVelocity(5f, 0f), Is.EqualTo(0f));
        Assert.That(FrogTrajectoryMath.CalculateTimeToApex(10f, 0f), Is.EqualTo(0f));
    }
}
