using Godot;
using System.Collections.Generic;

/// <summary>Dubai's operation: a long stepped climb up the plant, ending in a run across its roof.</summary>
public partial class DubaiSaltPlantMission : BeaconOperationMission
{
    /// <inheritdoc/>
    protected override string OperationId => "op_dubai";
    /// <inheritdoc/>
    protected override string CityOperationTitle => "The Salt Plant";
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 6000, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override Vector2 BeaconPosition => new(4900, -240);
    /// <inheritdoc/>
    protected override Vector2 ExitPosition => new(5650, -240);
    /// <inheritdoc/>
    protected override string ApproachObjective => "Climb the desalination plant";
    /// <inheritdoc/>
    protected override string BeaconObjective => "Destroy the plant's control beacon";
    /// <inheritdoc/>
    protected override float BeaconZoneX => 3800f;
    /// <inheritdoc/>
    protected override string CompletionLine => "OPERATION COMPLETE  /  THE SALT PLANT";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: This plant is rationing the city's water. Get onto the roof and cut the control beacon.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] { ("CheckpointTank", new Vector2(2200f, 240f)), ("CheckpointRoof", new Vector2(3900f, -240f)) };

    /// <summary>Seven 120 px steps with rest landings, then the long roof.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        Ground(-100, 600, 900);
        Ground(800, 480, 320);
        Ground(1120, 360, 380);
        Ground(1680, 360, 200);
        Ground(1880, 240, 720);
        AddTimedLaser(2400, 240);
        Ground(2600, 120, 300);
        Ground(2900, 0, 340);
        AddMover("PlantLift", new Vector2(3280, 0), new Vector2(180, 0), 4.5f);
        Ground(3640, -120, 200);
        Ground(3840, -240, 2100);
        AddOperationGate(5300, -240);
        AddPlatform(5940, WorldBounds.Position.Y, 100, 2600, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Adds a surge plate on the last landing before the lift.</summary>
    protected override void BuildHazards() => AddSurge(3080, 0, 120);

    /// <summary>Sentries cover each landing and the roof.</summary>
    protected override void SpawnEnemies()
    {
        var at = new[] { new Vector2(1250, 90), new Vector2(2100, -30), new Vector2(3000, -270), new Vector2(4200, -510), new Vector2(4700, -520), new Vector2(5100, -530) };
        for (int i = 0; i < at.Length; i++) AddSentry($"PlantSentry{i + 1:00}", at[i], i >= 3 ? 90f : 70f);
    }
}
