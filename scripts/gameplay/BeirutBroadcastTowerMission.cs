using Godot;
using System.Collections.Generic;

/// <summary>Beirut's operation: up a zig-zag shaft inside the tower, then out along the antenna deck.</summary>
public partial class BeirutBroadcastTowerMission : BeaconOperationMission
{
    /// <inheritdoc/>
    protected override string OperationId => "op_beirut";
    /// <inheritdoc/>
    protected override string CityOperationTitle => "The Broadcast Tower";
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Dusk;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 5400, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override Vector2 BeaconPosition => new(4300, -360);
    /// <inheritdoc/>
    protected override Vector2 ExitPosition => new(5050, -360);
    /// <inheritdoc/>
    protected override string ApproachObjective => "Climb the broadcast tower";
    /// <inheritdoc/>
    protected override string BeaconObjective => "Destroy the jamming beacon";
    /// <inheritdoc/>
    protected override float BeaconZoneX => 3100f;
    /// <inheritdoc/>
    protected override string CompletionLine => "OPERATION COMPLETE  /  THE BROADCAST TOWER";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: This tower is jamming every free station in the city. Take out its beacon and we get the airwaves back.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] { ("CheckpointBase", new Vector2(1500f, 600f)), ("CheckpointDeck", new Vector2(3200f, -360f)) };

    /// <summary>A ground floor, an alternating ledge shaft like Mission 1's gorge, then the deck.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        Ground(-100, 600, 2200);
        AddTimedLaser(1050, 600);
        // The shaft's walls stop short of its floor, so the player walks in from the west.
        AddPlatform(2100, -1300, 90, 1700, PalaceSurface.Wall, false, "ShaftWestWall");
        AddPlatform(2430, -240, 90, 1640, PalaceSurface.Wall, false, "ShaftEastWall");
        Ground(2100, 600, 420);
        float[] ledges = { 480, 360, 240, 120, 0, -120, -240, -360 };
        for (int i = 0; i < ledges.Length; i++)
            AddPlatform(i % 2 == 0 ? 2300 : 2190, ledges[i], 130, 22, PalaceSurface.Wood, true, $"ShaftLedge{i + 1}");
        Ground(2520, -360, 2800);
        AddOperationGate(4700, -360);
        AddPlatform(5320, WorldBounds.Position.Y, 100, 2600, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Adds a surge plate on the antenna deck.</summary>
    protected override void BuildHazards() => AddSurge(3700, -360, 140);

    /// <summary>Sentries cover the ground floor, the shaft's top and the deck.</summary>
    protected override void SpawnEnemies()
    {
        var at = new[] { new Vector2(700, 330), new Vector2(1600, 330), new Vector2(2850, -630), new Vector2(3500, -630), new Vector2(4100, -640), new Vector2(4500, -650) };
        for (int i = 0; i < at.Length; i++) AddSentry($"TowerSentry{i + 1:00}", at[i], i >= 3 ? 90f : 70f);
    }
}
