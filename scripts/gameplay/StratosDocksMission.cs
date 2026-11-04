using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Mission 8: Stratos Docks (High-Altitude Aviation Hub).
/// Scaffold level implementation establishing the Act III operational environment.
/// Extends <see cref="CodeLevel"/> with high-altitude gantries, aviation lighting, and extraction handoff.
/// </summary>
public partial class StratosDocksMission : CodeLevel
{
    /// <inheritdoc/>
    protected override int MissionNumber => 8;

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1400, 9600, 2200);

    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.35f, 0.45f, 0.65f);

    private PalaceObjective _exitTrigger = null!;

    /// <summary>Constructs high-altitude steel gantry platforms.</summary>
    protected override void BuildGeometry()
    {
        // Starter platform (under player spawn)
        AddPlatform(0, 600, 600, 80);

        // Mid gantry catwalks
        AddPlatform(680, 540, 500, 60);
        AddPlatform(1260, 480, 500, 60);
        AddPlatform(1840, 420, 600, 60);

        // VTOL Bay 4 flight deck
        AddPlatform(2520, 360, 800, 80);
    }

    /// <summary>Places initial tactical objectives and extraction beacon.</summary>
    protected override void BuildObjectives()
    {
        _exitTrigger = new PalaceObjective
        {
            Position = new Vector2(3100, 310),
            Kind = "Exit"
        };
        AddChild(_exitTrigger);
    }

    /// <summary>Spawns initial perimeter security patrol drone.</summary>
    protected override void SpawnEnemies()
    {
        var drone = new CyberDrone2D
        {
            Position = new Vector2(1400, 430),
            PatrolDistance = 250f
        };
        AddChild(drone);
    }

    /// <summary>Checks extraction proximity each frame.</summary>
    protected override void UpdateMission()
    {
        if (Player == null || MissionComplete) return;

        if (_exitTrigger != null && Player.GlobalPosition.DistanceTo(_exitTrigger.GlobalPosition) < 80f)
        {
            CompleteMission();
        }
    }
}
