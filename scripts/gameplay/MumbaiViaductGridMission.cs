using Godot;
using System.Collections.Generic;

/// <summary>Mumbai's operation: a rising run along broken rail viaducts, then a long deck to the solar grid's beacon.</summary>
public partial class MumbaiViaductGridMission : BeaconOperationMission
{
    /// <inheritdoc/>
    protected override string OperationId => "op_mumbai";
    /// <inheritdoc/>
    protected override string CityOperationTitle => "The Viaduct Grid";
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.NeonRust;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 6100, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override Vector2 BeaconPosition => new(5000, 240);
    /// <inheritdoc/>
    protected override Vector2 ExitPosition => new(5750, 240);
    /// <inheritdoc/>
    protected override string ApproachObjective => "Cross the broken viaducts";
    /// <inheritdoc/>
    protected override string BeaconObjective => "Destroy the grid beacon";
    /// <inheritdoc/>
    protected override float BeaconZoneX => 3900f;
    /// <inheritdoc/>
    protected override string CompletionLine => "OPERATION COMPLETE  /  THE VIADUCT GRID";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: The occupation is draining Mumbai's solar grid through the viaducts. Break the beacon.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] { ("CheckpointSpan", new Vector2(2500f, 360f)), ("CheckpointDeck", new Vector2(4000f, 240f)) };

    /// <summary>Open start, two steps up, spans over gaps, a timed laser, a moving span, then the final deck.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        Ground(-100, 600, 1000);
        Ground(1060, 600, 300);
        Ground(1360, 480, 320);
        Ground(1680, 360, 420);
        Ground(2280, 360, 560);
        AddTimedLaser(2660, 360);
        AddMover("ViaductSpan", new Vector2(2880, 360), new Vector2(240, 0), 5.5f);
        Ground(3320, 360, 360);
        Ground(3680, 240, 2300);
        AddOperationGate(5400, 240);
        AddPlatform(5980, WorldBounds.Position.Y, 100, 2600, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Adds a surge plate on the last span.</summary>
    protected override void BuildHazards() => AddSurge(3500, 360, 120);

    /// <summary>Sentries hover over each span and cluster at the beacon.</summary>
    protected override void SpawnEnemies()
    {
        var at = new[] { new Vector2(1200, 330), new Vector2(2400, 90), new Vector2(3480, 90), new Vector2(4300, -40), new Vector2(4800, -60), new Vector2(5200, -70) };
        for (int i = 0; i < at.Length; i++) AddSentry($"GridSentry{i + 1:00}", at[i], i >= 3 ? 90f : 70f);
    }
}
