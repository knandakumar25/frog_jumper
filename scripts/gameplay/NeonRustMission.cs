using Godot;
using System.Collections.Generic;

/// <summary>
/// Mission 3, "Neon Rust and Data Dust": scrap yards and a transit depot powering Xelthara logistics. The squad opens
/// with Shogun, Oceanus and Apex Titan: Titan slams a collapsed wall in the scrapyard, the depot is a chain of steel
/// platforms under a timed laser, and <b>Aura Phoenicia</b> joins before a sonic-locked door at the foot of the comm
/// tower. Her Harmonic Pulse opens it, and again at the relay at the top to black out the occupation's comms — after
/// which <b>Mirage Blade</b> joins and phases through the lethal laser curtain that seals the exit.
/// Required route abilities: Slam, Resonate, Phase.
/// </summary>
public partial class NeonRustMission : CodeLevel
{
    /// <summary>True once the relay at the top of the tower has been opened (comm grid blacked out).</summary>
    public bool CommsBlackedOut { get; private set; }

    private AbilityGate _relay = null!;
    private PalaceObjective _exit = null!;

    /// <inheritdoc/>
    protected override int MissionNumber => 3;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -720, 8460, 1540);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.NeonRust;
    /// <inheritdoc/>
    protected override Color PlatformTint => new(.8f, .98f, 1f);
    /// <inheritdoc/>
    protected override string CompletionLine => "MISSION COMPLETE  /  COMM GRID BLACKED OUT.";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: Comm towers are broadcasting denial of service. Reach the relay and black out their patrol network.";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("ShrineDepot", new Vector2(4500f, 600f)), ("ShrineRelay", new Vector2(5600f, -520f)), ("ShrineExitRun", new Vector2(6900f, 60f))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[] { new Vector2(960, 450), new Vector2(3500, 350), new Vector2(6330, -300) };

    /// <inheritdoc/>
    protected override string FragmentLog(int found) => found switch
    {
        1 => "LOG: Every crate in this yard carries a stolen name on its manifest.",
        2 => "LOG: The depot trains ran empty north and full south, for years.",
        _ => "LOG: The towers do not broadcast. They listen, and they tell the patrols what they heard."
    };

    /// <summary>
    /// Lays out the yard, the depot chain, the tower shaft and the exit run. Steps ≤ 150 px, gaps ≤ 200 px; the depot's
    /// widest gap is crossed by a ferry. The relay shelf, tower walls and gate walls reach the world ceiling.
    /// </summary>
    protected override void BuildGeometry()
    {
        // Zone A — scrapyard: a long yard with a few crates to hop. A collapsed wall (below) blocks the road.
        AddPlatform(-100, 600, 2300, 700, PalaceSurface.Concrete);
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        AddPlatform(420, 540, 140, 60, PalaceSurface.Concrete);
        AddPlatform(800, 520, 160, 80, PalaceSurface.Concrete);
        AddPlatform(880, 450, 140, 22, PalaceSurface.Steel, true, "FragmentPerchYard");

        // Zone B — transit depot: a chain of steel platforms over a rail pit, ending in a ferry to the tower road.
        float[,] chain = { { 2330, 560, 200 }, { 2680, 500, 200 }, { 3030, 440, 260 }, { 3420, 500, 200 }, { 3770, 560, 200 } };
        for (int i = 0; i < chain.GetLength(0); i++)
            AddPlatform(chain[i, 0], chain[i, 1], chain[i, 2], 24, PalaceSurface.Steel, true, $"DepotCar{i + 1}");
        AddPlatform(3440, 350, 120, 22, PalaceSurface.Steel, true, "FragmentPerchDepot");
        AddMover("DepotFerry", new Vector2(4050, 560), new Vector2(240, 0), 5f);

        // Zone C — tower road and shaft: floor, walls with an opening under the left wall, and alternating ledges.
        AddPlatform(4400, 600, 1500, 700, PalaceSurface.Concrete, false, "TowerRoad");
        AddPlatform(5100, -800, 90, 1050, PalaceSurface.Wall, false, "TowerWestWall");
        AddPlatform(5430, -520, 90, 1120, PalaceSurface.Wall, false, "TowerEastWall");
        float[] ledgeY = { 450, 300, 150, 0, -150, -300, -450 };
        for (int i = 0; i < ledgeY.Length; i++)
            AddPlatform(i % 2 == 0 ? 5300 : 5190, ledgeY[i], 130, 22, PalaceSurface.Steel, true, $"TowerLedge{i + 1}");
        AddPlatform(5520, -520, 520, 60, PalaceSurface.Steel, false, "RelayShelf");

        // Zone D — descent and exit run: shelves step down to a lower floor, then the laser-sealed exit.
        float[] shelfY = { -420, -300, -180, -60 };
        for (int i = 0; i < shelfY.Length; i++)
            AddPlatform(6050 + i * 210, shelfY[i], 150, 22, PalaceSurface.Steel, true, $"DescentShelf{i + 1}");
        AddPlatform(6880, 60, 1580, 800, PalaceSurface.Concrete, false, "ExitRun");
        AddPlatform(8460, WorldBounds.Position.Y, 100, 1500, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Places the collapsed wall, the sonic door, the relay, the laser curtain and the exit.</summary>
    protected override void BuildObjectives()
    {
        AddGate("CollapsedWall", GateKind.RubbleWall, new Vector2(1500, 600), new Vector2(150f, FullHeightFrom(600f)));
        AddGate("TowerDoor", GateKind.SonicLock, new Vector2(5000, 600), new Vector2(110f, FullHeightFrom(600f)));
        // The relay is a sonic lock too: opening it is the "upload" that blacks out the grid.
        _relay = AddGate("CommRelay", GateKind.SonicLock, new Vector2(5800, -520), new Vector2(90f, FullHeightFrom(-520f)));
        _relay.GateCleared += OnRelayOpened;
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(8260, 60) };
        Actors.AddChild(_exit);
    }

    /// <summary>The laser curtain only Mirage Blade can pass, plus the depot's timed laser.</summary>
    protected override void BuildHazards()
    {
        // Lethal full-height curtain from the ceiling down to the exit-run floor.
        AddLaserField(new Vector2(7700, WorldBounds.Position.Y), 60f - WorldBounds.Position.Y);
        // Timed laser over the depot's highest car: wait for the off-phase.
        var timed = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        timed.Position = new Vector2(3160, 300);
        timed.BeamLength = 140f;
        timed.ActiveDuration = 2.0f;
        timed.InactiveDuration = 2.2f;
        Actors.AddChild(timed);
    }

    /// <summary>Patrol sentries, thickest around the tower and the exit run.</summary>
    protected override void SpawnEnemies()
    {
        var positions = new[]
        {
            new Vector2(700, 300), new Vector2(1200, 250), new Vector2(1850, 260),
            new Vector2(2800, 200), new Vector2(3600, 250),
            new Vector2(4700, 300), new Vector2(5650, -640), new Vector2(6300, -600),
            new Vector2(7200, -150), new Vector2(7500, -170)
        };
        for (int i = 0; i < positions.Length; i++) AddSentry($"DepotSentry{i + 1:00}", positions[i], 75f);
    }

    /// <summary>Adds the existing recruit beats, then starts the arrival after every mission system exists.</summary>
    protected override void OnLevelReady()
    {
        Actors.AddChild(new RecruitBeat
        {
            Name = "PhoeniciaJoins", Level = this, Operative = "Aura Phoenicia", Position = new Vector2(4650, 600),
            Lines = new[]
            {
                "AURA PHOENICIA: Their denial-of-service hums in C-flat. Offended, honestly. Let me answer it.",
                "AXIOM-7: Aura Phoenicia has joined. Her Harmonic Pulse can open sonic locks, like the door at the tower."
            }
        });
        Actors.AddChild(new RecruitBeat
        {
            Name = "MirageJoins", Level = this, Operative = "Mirage Blade", Position = new Vector2(7100, 60),
            Lines = new[]
            {
                "MIRAGE BLADE: Patrols are blind. The route to the Border Fortress is open. Moving before you finish applauding.",
                "AXIOM-7: Mirage Blade has joined. His Phase Blink slips through laser fields that would kill anyone else."
            }
        });
        BeginArrivalPresentation(new NeonRustIntro { Name = "NeonRustIntro", Layer = 40 });
    }

    /// <summary>Marks the comm grid as blacked out.</summary>
    private void OnRelayOpened()
    {
        CommsBlackedOut = true;
        ShowDialogue("AURA PHOENICIA: Listen. That is what a free city sounds like. Comm grid down.", 8f);
    }

    /// <summary>Completes the mission at the extraction gate once the comms are down.</summary>
    protected override void UpdateMission()
    {
        if (CommsBlackedOut && Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) < 130f)
            CompleteMission();
    }

    /// <summary>Chooses the HUD objective from comm status and position.</summary>
    protected override string ComputeObjective()
    {
        float x = Player.GlobalPosition.X;
        if (CommsBlackedOut) return x > 7000f ? "Phase through the laser curtain to the exit" : "Descend and run for extraction";
        if (x > 4400f) return "Climb the comm tower and open the relay with Aura Phoenicia's Harmonic Pulse";
        if (x > 2200f) return "Cross the transit depot";
        return "Break through the scrapyard";
    }

    /// <summary>Drifting rust and data dust replace blossom petals.</summary>
    protected override void BuildPetals()
    {
        Camera.AddChild(new CpuParticles2D
        {
            Name = "Dust", Position = new Vector2(0, -620), Amount = 60, Lifetime = 9f, Preprocess = 9f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(1300, 10),
            Gravity = new Vector2(60, 30), InitialVelocityMin = 10f, InitialVelocityMax = 30f,
            Direction = new Vector2(1, 1), Spread = 40f, Color = new Color(.55f, .95f, 1f, .55f),
            ScaleAmountMin = 1.2f, ScaleAmountMax = 2.6f, ZIndex = 20
        });
    }
}
