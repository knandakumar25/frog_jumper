using Godot;
using System;

/// <summary>
/// One surveillance drone for open-world encounters. Position and logic use world metres,
/// and the node is only a drawing. Drones hover, so they don't collide with the terrain.
/// Attacks wind up visibly before landing, so a dodge or stepping away avoids them.
/// </summary>
public partial class WorldHostile : Node2D
{
    /// <summary>Awareness, which drives the local alert state.</summary>
    public enum Awareness { Unaware, Investigating, Engaged }

    public const float MaxHealth = 60f, MoveSpeed = 5f, AttackRange = 3.2f, AttackDamage = 12f;
    public const double WindupSeconds = 0.7, RecoverySeconds = 1.6;
    private const double EngageRange = 12, InvestigateRange = 25;

    /// <summary>World position in metres.</summary>
    public double XMetres { get; private set; }
    /// <summary>World position in metres.</summary>
    public double YMetres { get; private set; }
    public float Health { get; private set; } = MaxHealth;
    public Awareness State { get; private set; } = Awareness.Unaware;
    private double _windup, _recovery;

    /// <summary>Sets the starting position before the drone joins the tree.</summary>
    public void Spawn(double x, double y) { XMetres = x; YMetres = y; }

    /// <summary>Applies player damage and alerts the drone. Returns true when this hit destroys it.</summary>
    public bool Hit(float damage)
    {
        if (Health <= 0) return false;
        Health -= damage;
        State = Awareness.Engaged;
        if (IsInsideTree()) QueueRedraw();
        return Health <= 0;
    }

    /// <summary>
    /// Advances perception, movement and the attack timer, then updates the drawn position.
    /// Engaged drones stay engaged. A landing attack calls <paramref name="strike"/>.
    /// </summary>
    public void Tick(double delta, WorldPoint player, ChunkAddress origin, Action<float> strike)
    {
        double dx = player.X - XMetres, dy = player.Y - YMetres;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < EngageRange) State = Awareness.Engaged;
        else if (State != Awareness.Engaged) State = distance < InvestigateRange ? Awareness.Investigating : Awareness.Unaware;

        if (_recovery > 0) _recovery = Math.Max(0, _recovery - delta);
        if (State == Awareness.Engaged)
        {
            if (_windup > 0)
            {
                _windup -= delta;
                if (_windup <= 0)
                {
                    _windup = 0;
                    _recovery = RecoverySeconds;
                    // The attack resolves on current distance, so moving away during the windup avoids it.
                    if (distance <= AttackRange) strike(AttackDamage);
                    QueueRedraw();
                }
            }
            else if (distance <= AttackRange - 0.4)
            {
                if (_recovery <= 0) { _windup = WindupSeconds; QueueRedraw(); }
            }
            else if (distance > 0.001)
            {
                double step = Math.Min(MoveSpeed * delta, distance);
                XMetres += dx / distance * step;
                YMetres += dy / distance * step;
            }
        }
        Position = ExplorationPlayer.Project(XMetres - origin.X * 128, YMetres - origin.Y * 128);
    }

    /// <summary>Draws a hovering drone, its health bar and a red eye and ring during the windup.</summary>
    public override void _Draw()
    {
        DrawColoredPolygon(new[] { new Vector2(-12, -26), new(0, -36), new(12, -26), new(0, -16) }, new Color("3b4a63"));
        DrawCircle(new Vector2(0, -26), 4, _windup > 0 ? new Color("ff3b30") : new Color("4edee5"));
        DrawArc(Vector2.Zero, 9, 0, Mathf.Tau, 16, new Color(0, 0, 0, 0.5f), 2);
        if (_windup > 0)
        {
            // Range is in metres, so the ring is squashed like the ground to show the real safe distance.
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1, ExplorationPlayer.GroundAngle));
            DrawArc(Vector2.Zero, AttackRange * ExplorationPlayer.PixelsPerMetre, 0, Mathf.Tau, 32, new Color("ff3b30"), 2);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
        DrawRect(new Rect2(-14, -44, 28 * Math.Max(0, Health) / MaxHealth, 3), new Color("ff6b5e"));
    }
}
