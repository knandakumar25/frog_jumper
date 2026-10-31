using Godot;
using System;

/// <summary>
/// Authored shelling lanes only. Impact coordinates lock at warning time, with 2.4 seconds to escape a 100px radius.
/// Shelters, checkpoints, recruits and lessons lie outside these rectangles. Recovery cancels rather than fast-forwards.
/// </summary>
public partial class WildfireArtillery : Node2D
{
    /// <summary>Player and recovery state provided by the orchestrator.</summary>
    public ConcreteWildfireMission Mission { get; set; } = null!;
    /// <summary>World rectangles whose horizontal spans permit shelling; their bottom is the impact surface.</summary>
    public Rect2[] Lanes { get; set; } = Array.Empty<Rect2>();
    /// <summary>True after core destruction; cannot be reset by checkpoint recovery.</summary>
    public bool Stopped { get; private set; }
    /// <summary>Remaining warning time; zero when no impact is pending.</summary>
    public float WarningRemaining { get; private set; }
    /// <summary>Immutable target for the current warning.</summary>
    public Vector2 LockedTarget { get; private set; }
    private float _cooldown = 3, _flash;

    /// <summary>Arms one lane-local shell, resolves damage once, and leaves clear space between attacks.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Stopped || Mission.GameplaySuspended) return;
        float dt = (float)delta;
        _flash = Mathf.Max(0, _flash - dt);
        if (WarningRemaining > 0)
        {
            WarningRemaining -= dt;
            if (WarningRemaining <= 0)
            {
                _flash = .3f;
                Vector2 offset = Mission.Player.GlobalPosition - (LockedTarget + new Vector2(0, -46));
                if (Mathf.Abs(offset.X) < 125 && Mathf.Abs(offset.Y) < 150) Mission.Player.TakeDamage(32);
                _cooldown = 3.5f;
            }
        }
        else
        {
            _cooldown -= dt;
            if (_cooldown <= 0) TryWarn(Mission.Player.GlobalPosition);
        }
        QueueRedraw();
    }

    /// <summary>Rejects safe areas; targets never track the player after this call succeeds.</summary>
    public bool TryWarn(Vector2 position)
    {
        if (Stopped || WarningRemaining > 0 || Mission.GameplaySuspended) return false;
        foreach (var lane in Lanes)
        {
            if (!lane.HasPoint(position)) continue;
            LockedTarget = new Vector2(Mathf.Clamp(position.X, lane.Position.X + 120, lane.End.X - 120), lane.End.Y);
            WarningRemaining = 2.4f;
            return true;
        }
        return false;
    }
    /// <summary>Cancels all delayed damage and provides a fresh warning interval after recovery.</summary>
    public void CancelPending() { WarningRemaining = 0; _flash = 0; _cooldown = 3; QueueRedraw(); }
    /// <summary>Core destruction permanently ends the battery's local shelling.</summary>
    public void StopForever() { Stopped = true; CancelPending(); }
    /// <summary>Orange chevrons and a ground ring distinguish unparryable shelling from cyan gunfire.</summary>
    public override void _Draw()
    {
        if (WarningRemaining <= 0 && _flash <= 0) return;
        Vector2 p = ToLocal(LockedTarget);
        Color orange = new("#ff984f");
        DrawRect(new Rect2(p + new Vector2(-100, -8), new Vector2(200, 8)), orange);
        DrawArc(p + new Vector2(0, -15), 100, Mathf.Pi, Mathf.Tau, 40, orange, 4, true);
        for (int i = -1; i <= 1; i++) DrawPolyline(new[] { p + new Vector2(i * 48 - 12, -100),
            p + new Vector2(i * 48, -78), p + new Vector2(i * 48 + 12, -100) }, orange, 4, true);
        if (_flash > 0) DrawCircle(p + new Vector2(0, -40), 110, new Color(1, .45f, .12f, _flash * 2));
    }
}
