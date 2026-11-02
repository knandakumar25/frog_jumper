using Godot;
using System.Collections.Generic;

/// <summary>Opt-in fortress combat sequence; standalone Vanguard-X retains its original behavior and HUD signal contract.</summary>
public partial class AegisBossController : Node2D
{
    /// <summary>Mission controls attempt lifetime and speech.</summary>
    public ShatteredAegisMission Mission { get; set; } = null!;
    /// <summary>Existing armored missile mech.</summary>
    public CyberBossMech2D Boss { get; set; } = null!;
    /// <summary>Three fixed health phases at 200 and 100 HP.</summary>
    public int Phase { get; private set; } = 1;
    /// <summary>Readable current attack state for runtime acceptance.</summary>
    public string Stage { get; private set; } = "aim";
    /// <summary>Five-second melee recovery, after all owned missiles have been cancelled.</summary>
    public bool Exposed => Stage == "recovery" && !Shielded;
    /// <summary>Two phase-two pylons block core damage until both are destroyed.</summary>
    public bool Shielded => Phase >= 2 && _pylons.Exists(p => !p.IsOffline);
    private readonly List<AegisDevice> _pylons = new();
    private readonly List<CyberBossMissile2D> _missiles = new();
    private float _clock, _markedX, _strikeCooldown, _exposureX;
    private Vector2 _aim;
    private int _lane;

    /// <summary>Ticks from the existing boss physics callback, respecting mission pause and recovery boundaries.</summary>
    public void Tick(double delta)
    {
        if (Mission.GameplaySuspended || Boss.IsDying) return;
        float dt = (float)delta;
        _clock += dt;
        Boss.GlobalPosition = new Vector2(Exposed ? _exposureX : Mathf.Clamp(Mission.Player.GlobalPosition.X + 150, 21900, 24100),
            Exposed ? 515 : 310);
        switch (Stage)
        {
            case "aim":
                _aim = Mission.Player.GlobalPosition - Boss.GlobalPosition;
                if (_clock >= 1.1f) { Volley(); SetStage("missiles"); }
                break;
            case "missiles":
                // End the volley completely before marking a floor strike: no missile/strike overlap.
                if (_clock >= 1.7f) { ClearProjectiles(); MarkStrike(); }
                break;
            case "warning":
                if (_clock >= 1.05f) { _strikeCooldown = 0; SetStage("strike"); }
                break;
            case "strike":
                _strikeCooldown -= dt;
                if (_strikeCooldown <= 0 && Mathf.Abs(Mission.Player.GlobalPosition.X - _markedX) < 210 &&
                    Mission.Player.GlobalPosition.Y > 390)
                { Mission.Player.TakeDamage(28); _strikeCooldown = .4f; }
                if (_clock >= .3f) SetStage(Shielded ? "lockdown" : "recovery");
                break;
            case "lockdown":
                if (!Shielded) SetStage("recovery");
                else if (_clock >= 1.2f) MarkStrike();
                break;
            case "recovery":
                if (_clock >= 5) SetStage("aim");
                break;
        }
        QueueRedraw();
    }
    /// <summary>Snapshots one marked lane; most of the broad arena stays safe and reachable with ordinary movement.</summary>
    private void MarkStrike()
    {
        _markedX = Shielded ? (++_lane % 2 == 0 ? 22500 : 23700) :
            Mathf.Clamp(Mission.Player.GlobalPosition.X, 21700, 24300);
        SetStage("warning");
    }
    /// <summary>Locks an aimed missile or phase-three spread after the visible aim warning.</summary>
    private void Volley()
    {
        Vector2 heading = _aim.LengthSquared() > 0 ? _aim.Normalized() : Vector2.Left;
        foreach (float angle in Phase == 3 ? new[] { -.25f, 0, .25f } : new[] { 0f })
        {
            var missile = GD.Load<PackedScene>("res://scenes/gameplay/cyber_boss_missile_2d.tscn").Instantiate<CyberBossMissile2D>();
            missile.OwningBoss = Boss;
            missile.Direction = heading.Rotated(angle);
            Mission.AddBossProjectile(missile, Boss.GlobalPosition);
            _missiles.Add(missile);
        }
    }
    /// <summary>Filters shielded hits and clamps at each phase boundary; a reflected projectile is valid counterfire outside recovery.</summary>
    public float ResolveDamage(float health, float damage, bool reflected = false)
    {
        if (damage <= 0 || Shielded || !(Exposed || reflected)) return health;
        float boundary = Phase == 1 ? 200 : Phase == 2 ? 100 : 0;
        float next = Mathf.Max(boundary, health - damage);
        if (next == boundary && Phase < 3)
        {
            Phase++;
            ClearProjectiles();
            if (Phase == 2)
            {
                foreach (float x in new[] { 22300f, 23800 })
                {
                    var pylon = new AegisDevice { Name = "ArenaPylon" + _pylons.Count, Position = new Vector2(x, 540), Health = 60,
                        Acceptance = () => !Mission.GameplaySuspended };
                    _pylons.Add(pylon);
                    Mission.AddArenaDevice(pylon);
                }
                Mission.QueueBeat("phase2");
                SetStage("lockdown");
            }
            else { Mission.QueueBeat("phase3"); SetStage("aim"); }
        }
        return next;
    }
    /// <summary>Changes stage synchronously; recovery always starts with a completely clear projectile field.</summary>
    private void SetStage(string stage)
    {
        Stage = stage;
        _clock = 0;
        if (stage == "recovery")
        {
            ClearProjectiles();
            // Hold a real melee target still: following the player at a fixed offset would let the core evade every approach.
            _exposureX = Boss.GlobalPosition.X;
            Boss.GlobalPosition = new Vector2(_exposureX, 515);
        }
        QueueRedraw();
    }
    /// <summary>Immediate cancellation is safe even inside a projectile's collision callback.</summary>
    public void ClearProjectiles()
    {
        foreach (var missile in _missiles)
            if (IsInstanceValid(missile) && !missile.IsQueuedForDeletion()) missile.Cancel();
        _missiles.Clear();
    }
    /// <summary>Teardown/recovery removes transient pylons too; a new attempt rebuilds both from scratch.</summary>
    public void CancelAttempt()
    {
        ClearProjectiles();
        foreach (var pylon in _pylons)
            if (IsInstanceValid(pylon)) { pylon.ProcessMode = ProcessModeEnum.Disabled; pylon.RemoveFromGroup("enemies"); pylon.QueueFree(); }
        _pylons.Clear();
        Stage = "offline";
        QueueRedraw();
    }
    /// <summary>Draws distinct aim, floor warning, active strike and core-exposure feedback in world space.</summary>
    public override void _Draw()
    {
        if (!IsInstanceValid(Boss) || Stage == "offline") return;
        if (Stage == "aim") DrawLine(Boss.GlobalPosition, Mission.Player.GlobalPosition, new Color(1, .65f, .2f, .65f), 3);
        if (Stage is "warning" or "strike")
            DrawRect(new Rect2(_markedX - 210, 390, 420, 210), new Color(1, .35f, .15f, Stage == "warning" ? .22f : .7f));
        if (Exposed) DrawArc(Boss.GlobalPosition, 70, 0, Mathf.Tau, 40, new Color("#77efff"), 4);
    }
    /// <summary>Scene removal cannot leave sibling projectiles or pylons active.</summary>
    public override void _ExitTree() => CancelAttempt();
}
