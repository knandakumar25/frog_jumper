using Godot;

/// <summary>
/// Shared shape of a city operation: destroy a beacon, which opens a gate, then reach the exit behind it.
/// Subclasses supply only the layout, enemies and text. Layouts must keep required steps at or under 120 px
/// and required gaps at or under 200 px, and use no ability gates, because any squad member may be active.
/// </summary>
public abstract partial class BeaconOperationMission : CityOperationMission
{
    private PalaceObjective _beacon = null!, _exit = null!;
    /// <summary>The gate that the beacon holds shut. Subclasses create it in BuildGeometry with <see cref="AddOperationGate"/>.</summary>
    private PalacePlatform _gate = null!;

    /// <summary>True once the beacon is destroyed and the gate is open.</summary>
    public bool BeaconDestroyed { get; private set; }

    /// <summary>Where the beacon stands.</summary>
    protected abstract Vector2 BeaconPosition { get; }
    /// <summary>Where the exit stands, beyond the gate.</summary>
    protected abstract Vector2 ExitPosition { get; }
    /// <summary>Objective shown while approaching the beacon.</summary>
    protected abstract string ApproachObjective { get; }
    /// <summary>Objective shown near the beacon, once past BeaconZoneX.</summary>
    protected abstract string BeaconObjective { get; }
    /// <summary>X position at which the objective switches to the beacon.</summary>
    protected abstract float BeaconZoneX { get; }

    /// <summary>Adds ground reaching well below the view, so gaps read as drops rather than floating blocks.</summary>
    protected PalacePlatform Ground(float x, float top, float width, PalaceSurface surface = PalaceSurface.Stone) => AddPlatform(x, top, width, 1400 - top, surface);

    /// <summary>Adds the beacon's gate down to the floor. It runs above the camera ceiling, so wall-jumping can't bypass it.</summary>
    protected void AddOperationGate(float x, float floorTop) => _gate = AddPlatform(x, WorldBounds.Position.Y, 40, floorTop - WorldBounds.Position.Y, PalaceSurface.Gate, false, "OperationGate");

    /// <summary>Adds a laser that cycles on and off across a floor, with the bracket it hangs from.</summary>
    protected void AddTimedLaser(float x, float floorTop)
    {
        AddPlatform(x - 24, floorTop - 204, 48, 28, PalaceSurface.Stone, false, "LaserBracket");
        var laser = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        laser.Position = new Vector2(x, floorTop - 176);
        laser.BeamLength = 176f;
        laser.ActiveDuration = 2.0f;
        laser.InactiveDuration = 2.2f;
        Actors.AddChild(laser);
    }

    /// <summary>Adds a floor plate that pulses with damage on a timer. Placed a few pixels up so feet overlap it.</summary>
    protected void AddSurge(float x, float floorTop, float width)
    {
        var surge = GD.Load<PackedScene>("res://scenes/gameplay/cyber_hazard_floor_2d.tscn").Instantiate<CyberHazardFloor2D>();
        surge.Position = new Vector2(x, floorTop - 8);
        surge.FloorWidth = width;
        surge.ActiveDuration = 2.4f;
        surge.InactiveDuration = 2.0f;
        Actors.AddChild(surge);
    }

    /// <summary>Places the beacon and the exit.</summary>
    protected override void BuildObjectives()
    {
        _beacon = new PalaceObjective { Name = "OperationBeacon", Kind = "Beacon", Position = BeaconPosition };
        _beacon.Neutralized += () => { BeaconDestroyed = true; _gate.Open(); };
        Actors.AddChild(_beacon);
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = ExitPosition };
        Actors.AddChild(_exit);
    }

    /// <summary>Ends the operation at the exit, which is only reachable through the opened gate.</summary>
    protected override void UpdateMission()
    {
        if (BeaconDestroyed && Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) < 120f) CompleteMission();
    }

    /// <summary>Picks the HUD objective from progress along the route.</summary>
    protected override string ComputeObjective()
    {
        if (BeaconDestroyed) return "Reach the exit";
        return Player.GlobalPosition.X > BeaconZoneX ? BeaconObjective : ApproachObjective;
    }
}
