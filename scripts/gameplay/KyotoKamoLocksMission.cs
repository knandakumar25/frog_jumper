using Godot;
using System.Collections.Generic;

/// <summary>
/// "The Kamo Locks": Kyoto's optional operation. A ~5,800 px run along the flood locks, in which the player
/// destroys the control beacon that opens the sluice gate, then leaves through the far gate. It needs no
/// operative-specific ability, so any squad member can clear it. Every required step is at most 120 px
/// high and every required gap at most about 200 px, within the campaign's measured jump limits.
/// </summary>
public partial class KyotoKamoLocksMission : CityOperationMission
{
    private PalaceObjective _beacon = null!, _exit = null!;
    private PalacePlatform _sluiceGate = null!;

    /// <summary>True once the control beacon is destroyed and the gate is open.</summary>
    public bool BeaconDestroyed { get; private set; }

    /// <inheritdoc/>
    protected override string OperationId => "op_kyoto_kamo_locks";
    /// <inheritdoc/>
    protected override string CityOperationTitle => "The Kamo Locks";
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 5900, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override string CompletionLine => "OPERATION COMPLETE  /  THE KAMO LOCKS";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: The occupation is holding the flood locks shut. Break their control beacon and the river runs free.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] { ("CheckpointLocks", new Vector2(2400f, 480f)), ("CheckpointFloor", new Vector2(3700f, 240f)) };

    /// <summary>Lays out four beats: open dock (learn the route), lock bridge (timing), climb (rest), long floor (fight).</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        // Dock: open ground and one easy gap.
        AddPlatform(-100, 600, 900, 700);
        AddPlatform(980, 600, 500, 700);
        // Lock wall and ferry: the same mover spacing as Mission 1's sky ferry.
        AddPlatform(1480, 480, 360, 820);
        AddMover("LockFerry", new Vector2(1880, 480), new Vector2(240, 0), 5.5f);
        AddPlatform(2320, 480, 520, 820, PalaceSurface.Stone, false, "LockTop");
        AddPlatform(2576, 276, 48, 28, PalaceSurface.Stone, false, "LaserBracket");
        // Two steps up to the sluice floor.
        AddPlatform(2840, 360, 300, 940);
        AddPlatform(3140, 240, 360, 1060);
        AddPlatform(3500, 240, 2300, 1060, PalaceSurface.Stone, false, "SluiceFloor");
        // The gate runs above the camera ceiling, so wall-jumping can't bypass it.
        _sluiceGate = AddPlatform(5200, WorldBounds.Position.Y, 40, 240 - WorldBounds.Position.Y, PalaceSurface.Gate, false, "SluiceGate");
        AddPlatform(5800, WorldBounds.Position.Y, 100, 2600, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Places the control beacon and the exit.</summary>
    protected override void BuildObjectives()
    {
        _beacon = new PalaceObjective { Name = "LockBeacon", Kind = "Beacon", Position = new Vector2(4800, 240) };
        _beacon.Neutralized += OnBeaconDestroyed;
        Actors.AddChild(_beacon);
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(5500, 240) };
        Actors.AddChild(_exit);
    }

    /// <summary>Adds one timed laser on the lock top and one surge plate on the climb.</summary>
    protected override void BuildHazards()
    {
        var laser = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        laser.Position = new Vector2(2600, 304);
        laser.BeamLength = 176f;
        laser.ActiveDuration = 2.0f;
        laser.InactiveDuration = 2.2f;
        Actors.AddChild(laser);
        var surge = GD.Load<PackedScene>("res://scenes/gameplay/cyber_hazard_floor_2d.tscn").Instantiate<CyberHazardFloor2D>();
        surge.Position = new Vector2(3300, 232);
        surge.FloorWidth = 120f;
        surge.ActiveDuration = 2.4f;
        surge.InactiveDuration = 2.0f;
        Actors.AddChild(surge);
    }

    /// <summary>Hovering sentries: sparse early, heaviest around the beacon.</summary>
    protected override void SpawnEnemies()
    {
        var positions = new[] { new Vector2(1150, 330), new Vector2(2450, 210), new Vector2(3300, -30), new Vector2(4200, -40), new Vector2(4600, -60), new Vector2(4950, -70) };
        for (int i = 0; i < positions.Length; i++) AddSentry($"LockSentry{i + 1:00}", positions[i], i >= 3 ? 90f : 70f);
    }

    /// <summary>Ends the operation at the exit, which is only reachable through the opened gate.</summary>
    protected override void UpdateMission()
    {
        if (BeaconDestroyed && Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) < 120f) CompleteMission();
    }

    /// <summary>Picks the HUD objective from progress along the route.</summary>
    protected override string ComputeObjective()
    {
        if (BeaconDestroyed) return "Leave through the sluice gate";
        return Player.GlobalPosition.X > 3500f ? "Destroy the lock control beacon" : "Follow the locks upstream";
    }

    /// <summary>Opens the gate as soon as the beacon falls.</summary>
    private void OnBeaconDestroyed()
    {
        BeaconDestroyed = true;
        _sluiceGate.Open();
    }
}
