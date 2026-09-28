namespace AxiomForce.Tests;

using System.Numerics;
using NUnit.Framework;

[TestFixture]
public class RobotBuddyMathTests
{
    public static class RobotTrajectoryMath
    {
        public static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
        }

        public static float ComputeHoverOffset(float time, float speed, float height)
        {
            return MathF.Sin(time * speed) * height;
        }

        public static (float leftHandY, float rightHandY) ComputeFloatingHandsBob(float time, float speed, float amplitude, float phaseOffset = 2.0f)
        {
            float left = MathF.Sin(time * speed) * amplitude;
            float right = MathF.Sin(time * speed + phaseOffset) * amplitude;
            return (left, right);
        }

        public static float ComputeEmissivePulse(float time, float speed, float minEnergy, float maxEnergy)
        {
            float norm = (MathF.Sin(time * speed) * 0.5f + 0.5f);
            return minEnergy + (maxEnergy - minEnergy) * norm;
        }
    }

    [Test]
    public void TestBezierStartAndEndPoints()
    {
        var p0 = new Vector3(-1.25f, 4.2f, -4.5f);
        var p1 = new Vector3(-1.20f, -0.40f, -2.2f);
        var p2 = new Vector3(-0.35f, -0.55f, -0.5f);
        var p3 = new Vector3(-0.85f, -0.12f, -1.0f);

        Vector3 start = RobotTrajectoryMath.EvaluateCubicBezier(p0, p1, p2, p3, 0f);
        Vector3 end = RobotTrajectoryMath.EvaluateCubicBezier(p0, p1, p2, p3, 1f);

        Assert.That(start.X, Is.EqualTo(p0.X).Within(0.001f));
        Assert.That(start.Y, Is.EqualTo(p0.Y).Within(0.001f));
        Assert.That(start.Z, Is.EqualTo(p0.Z).Within(0.001f));

        Assert.That(end.X, Is.EqualTo(p3.X).Within(0.001f));
        Assert.That(end.Y, Is.EqualTo(p3.Y).Within(0.001f));
        Assert.That(end.Z, Is.EqualTo(p3.Z).Within(0.001f));
    }

    [Test]
    public void TestFloatingHandsOutOfPhase()
    {
        float speed = 2.6f;
        float amp = 0.016f;
        var (left, right) = RobotTrajectoryMath.ComputeFloatingHandsBob(0f, speed, amp, 2.0f);

        // At time 0, left is sin(0) = 0, right is sin(2.0) ~= 0.909 * amp
        Assert.That(left, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(MathF.Abs(right), Is.GreaterThan(0.01f));
        Assert.That(left, Is.Not.EqualTo(right));
    }

    [Test]
    public void TestEmissivePulseBounds()
    {
        float minEnergy = 1.2f;
        float maxEnergy = 4.5f;

        for (float t = 0f; t < 5f; t += 0.25f)
        {
            float energy = RobotTrajectoryMath.ComputeEmissivePulse(t, 3.0f, minEnergy, maxEnergy);
            Assert.That(energy, Is.GreaterThanOrEqualTo(minEnergy - 0.001f));
            Assert.That(energy, Is.LessThanOrEqualTo(maxEnergy + 0.001f));
        }
    }
}
