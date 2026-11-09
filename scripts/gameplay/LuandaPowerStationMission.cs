using Godot;
using System.Collections.Generic;

/// <summary>Luanda's operation: hopping across uneven container stacks, with pulsing plates between them.</summary>
public partial class LuandaPowerStationMission : BeaconOperationMission
{
    /// <inheritdoc/>
    protected override string OperationId => "op_luanda";
    /// <inheritdoc/>
    protected override string CityOperationTitle => "The Power Station";
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Wildfire;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 6100, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override Vector2 BeaconPosition => new(5050, 480);
    /// <inheritdoc/>
    protected override Vector2 ExitPosition => new(5800, 480);
    /// <inheritdoc/>
    protected override string ApproachObjective => "Cross the container stacks";
    /// <inheritdoc/>
    protected override string BeaconObjective => "Destroy the station's control beacon";
    /// <inheritdoc/>
    protected override float BeaconZoneX => 4100f;
    /// <inheritdoc/>
    protected override string CompletionLine => "OPERATION COMPLETE  /  THE POWER STATION";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: Luanda's lights are going to the occupation's port. Shut down the station's control beacon.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] { ("CheckpointStacks", new Vector2(2300f, 360f)), ("CheckpointHall", new Vector2(4350f, 480f)) };

    /// <summary>Alternating stack heights across short gaps, a moving crane load, then the long turbine hall.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        Ground(-100, 600, 900);
        float x = 960;
        float[] tops = { 480, 600, 480, 360, 480, 600, 480 };
        foreach (float top in tops)
        {
            Ground(x, top, 280, PalaceSurface.Wood);
            x += 280 + 130;
        }
        AddMover("CraneLoad", new Vector2(x, 480), new Vector2(200, 0), 5f);
        Ground(x + 420, 480, 6020 - (x + 420));
        AddOperationGate(5500, 480);
        AddPlatform(6020, WorldBounds.Position.Y, 100, 2600, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Adds two pulsing plates in the turbine hall.</summary>
    protected override void BuildHazards()
    {
        AddSurge(4500, 480, 140);
        AddSurge(4800, 480, 100);
    }

    /// <summary>Sentries work along the stacks and thicken in the hall.</summary>
    protected override void SpawnEnemies()
    {
        var at = new[] { new Vector2(1100, 210), new Vector2(1900, 90), new Vector2(2700, 90), new Vector2(4200, 210), new Vector2(4700, 200), new Vector2(5200, 190) };
        for (int i = 0; i < at.Length; i++) AddSentry($"StationSentry{i + 1:00}", at[i], i >= 3 ? 90f : 70f);
    }
}
