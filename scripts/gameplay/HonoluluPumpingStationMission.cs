using Godot;
using System.Collections.Generic;

/// <summary>Honolulu's operation: down through the pumping station's tiers, ending on the flooded lower floor.</summary>
public partial class HonoluluPumpingStationMission : BeaconOperationMission
{
    /// <inheritdoc/>
    protected override string OperationId => "op_honolulu";
    /// <inheritdoc/>
    protected override string CityOperationTitle => "The Pumping Station";
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Dawn;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 5900, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 120f - FeetOffset);
    /// <inheritdoc/>
    protected override Vector2 BeaconPosition => new(4700, 600);
    /// <inheritdoc/>
    protected override Vector2 ExitPosition => new(5500, 600);
    /// <inheritdoc/>
    protected override string ApproachObjective => "Descend through the pump tiers";
    /// <inheritdoc/>
    protected override string BeaconObjective => "Destroy the pump control beacon";
    /// <inheritdoc/>
    protected override float BeaconZoneX => 3600f;
    /// <inheritdoc/>
    protected override string CompletionLine => "OPERATION COMPLETE  /  THE PUMPING STATION";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: They've locked the pumps, and the rains are coming. Get down to the control floor.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] { ("CheckpointTier", new Vector2(2050f, 360f)), ("CheckpointFloor", new Vector2(3700f, 600f)) };

    /// <summary>High walkway, descending tiers with two ferries, then the long lower floor.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        Ground(-100, 120, 900);
        Ground(960, 240, 460);
        AddMover("PumpFerryUpper", new Vector2(1460, 240), new Vector2(240, 0), 5f);
        Ground(1900, 360, 700);
        AddTimedLaser(2300, 360);
        Ground(2760, 480, 400);
        AddMover("PumpFerryLower", new Vector2(3200, 480), new Vector2(180, 0), 4.5f);
        Ground(3580, 600, 2200);
        AddOperationGate(5100, 600);
        AddPlatform(5780, WorldBounds.Position.Y, 100, 2600, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Adds a surge plate where the water has shorted the floor.</summary>
    protected override void BuildHazards() => AddSurge(4100, 600, 160);

    /// <summary>Sentries guard the ferries and the control floor.</summary>
    protected override void SpawnEnemies()
    {
        var at = new[] { new Vector2(1100, -30), new Vector2(1980, 90), new Vector2(2900, 210), new Vector2(3800, 330), new Vector2(4400, 320), new Vector2(4900, 310) };
        for (int i = 0; i < at.Length; i++) AddSentry($"PumpSentry{i + 1:00}", at[i], i >= 3 ? 90f : 70f);
    }
}
