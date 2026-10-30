using Godot;
using System.Collections.Generic;

/// <summary>
/// Mission 2, "The Concrete Wildfire": Containment Zone 4, a burning checkpoint district. The player arrives as Shogun,
/// is joined by <b>Oceanus</b> before the first burning corridor (her Tidal Torrent puts fires out), crosses a chasm
/// overpass, disables three biometric spires, and is joined by <b>Apex Titan</b> beyond the energy membrane — whose
/// Tectonic Slam is needed to bring down the last wall before extraction.
/// Required route abilities: Parry (optional here), Douse (fire vents), Slam (final rubble wall).
/// </summary>
public partial class ConcreteWildfireMission : CodeLevel
{
    /// <summary>Spires that must be destroyed to open the membrane.</summary>
    private const int SpireTotal = 3;

    /// <summary>Number of biometric spires destroyed so far.</summary>
    public int SpiresDisabled { get; private set; }
    /// <summary>True once all spires are down and the membrane has opened.</summary>
    public bool MembraneOpen { get; private set; }

    private readonly List<PalaceObjective> _spires = new();
    private PalacePlatform _membrane = null!;
    private AbilityGate _finalRubble = null!;
    private PalaceObjective _exit = null!;

    /// <inheritdoc/>
    protected override int MissionNumber => 2;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -700, 7860, 1520);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Wildfire;
    /// <inheritdoc/>
    protected override Color PlatformTint => new(1f, .86f, .8f);
    /// <inheritdoc/>
    protected override string CompletionLine => "MISSION COMPLETE  /  REFUGEES EVACUATED. CHECKPOINT DESTROYED.";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: Containment Zone 4. Siege units are shelling the evacuation route. Disable the biometric spires.";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("ShrineRoad", new Vector2(2750f, 600f)), ("ShrineOverpass", new Vector2(4900f, 600f)), ("ShrineMembrane", new Vector2(6800f, 600f))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[] { new Vector2(2950, 450), new Vector2(4350, 450), new Vector2(5370, 450) };

    /// <inheritdoc/>
    protected override string FragmentLog(int found) => found switch
    {
        1 => "LOG: They fenced our neighbourhoods one street at a time, calling each fence temporary.",
        2 => "LOG: The scanner spires read faces, not names. We started wearing each other's.",
        _ => "LOG: Zone 4 held four thousand families. The census says forty."
    };

    /// <summary>
    /// Lays out the route: road → burning corridors → chasm overpass → spire compound → membrane. Every step ≤ 150 px
    /// and every gap ≤ 200 px except the overpass, which is bridged by moving platforms.
    /// </summary>
    protected override void BuildGeometry()
    {
        // Zone A/B — one long road: concrete barriers to hop, then the burning corridors.
        AddPlatform(-100, 600, 3800, 700, PalaceSurface.Concrete);
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        AddPlatform(500, 540, 120, 60, PalaceSurface.Concrete);
        AddPlatform(900, 520, 160, 80, PalaceSurface.Concrete);
        AddPlatform(1300, 540, 120, 60, PalaceSurface.Concrete);
        AddPlatform(2880, 450, 140, 22, PalaceSurface.Steel, true, "FragmentPerchRoad");

        // Zone C — overpass: the road ends at a burning chasm, crossed by two ferries and an island.
        AddMover("OverpassFerry1", new Vector2(3760, 600), new Vector2(260, 0), 5f);
        AddPlatform(4260, 600, 180, 40, PalaceSurface.Concrete, false, "OverpassIsland");
        AddPlatform(4290, 450, 120, 22, PalaceSurface.Steel, true, "FragmentPerchIsland");
        AddMover("OverpassFerry2", new Vector2(4500, 600), new Vector2(260, 0), 5f);

        // Zone D — spire compound: stairs up to a tower, an elevated spire, and a fire-guarded spire.
        AddPlatform(4800, 600, 1900, 700, PalaceSurface.Concrete);
        AddPlatform(5300, 450, 140, 22, PalaceSurface.Steel, true, "FragmentPerchCompound");
        AddPlatform(5450, 520, 140, 80, PalaceSurface.Concrete, false, "TowerStep1");
        AddPlatform(5580, 440, 140, 160, PalaceSurface.Concrete, false, "TowerStep2");
        AddPlatform(5720, 400, 280, 200, PalaceSurface.Concrete, false, "SpireTower");

        // Zone E — the membrane and the final wall.
        AddPlatform(6700, 600, 1160, 700, PalaceSurface.Concrete);
        _membrane = AddPlatform(6900, WorldBounds.Position.Y, 40, FullHeightFrom(600f), PalaceSurface.Gate, false, "Membrane");
        AddPlatform(7860, WorldBounds.Position.Y, 100, 1400, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Places the three biometric spires, the burning corridors, the last wall and the exit.</summary>
    protected override void BuildObjectives()
    {
        foreach (var (name, position) in new[] { ("BiometricSpire1", new Vector2(5200, 600)), ("BiometricSpire2", new Vector2(5830, 400)), ("BiometricSpire3", new Vector2(6370, 600)) })
        {
            var spire = new PalaceObjective { Name = name, Kind = "Beacon", Position = position };
            spire.Neutralized += OnSpireDisabled;
            _spires.Add(spire);
            Actors.AddChild(spire);
        }
        // Burning corridors: the first is where Oceanus joins; spire three sits behind another.
        AddGate("FireCorridor1", GateKind.FireVent, new Vector2(2400, 600));
        AddGate("FireCorridor2", GateKind.FireVent, new Vector2(3180, 600), new Vector2(380f, 170f));
        AddGate("SpireFire", GateKind.FireVent, new Vector2(6100, 600), new Vector2(300f, 170f));
        _finalRubble = AddGate("MembraneRubble", GateKind.RubbleWall, new Vector2(7330, 600), new Vector2(150f, FullHeightFrom(600f)));
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(7640, 600) };
        Actors.AddChild(_exit);
    }

    /// <summary>Hovering siege sentries guard each beat of the route.</summary>
    protected override void SpawnEnemies()
    {
        var positions = new[]
        {
            new Vector2(900, 300), new Vector2(1600, 260), new Vector2(2750, 280),
            new Vector2(5100, 300), new Vector2(5300, 300), new Vector2(5850, 150), new Vector2(6000, 300), new Vector2(6350, 250),
            new Vector2(7150, 300), new Vector2(7450, 280)
        };
        for (int i = 0; i < positions.Length; i++) AddSentry($"SiegeSentry{i + 1:00}", positions[i], 75f);
    }

    /// <summary>Adds the Oceanus and Apex Titan recruit beats with their story lines.</summary>
    protected override void OnLevelReady()
    {
        Actors.AddChild(new RecruitBeat
        {
            Name = "OceanusJoins", Level = this, Operative = "Oceanus", Position = new Vector2(1900, 600),
            Lines = new[]
            {
                "OCEANUS: As long as I draw breath, they will not relocate us like cargo in our own city. The tide rises HERE.",
                "AXIOM-7: Oceanus has joined. Her Tidal Torrent can put out the fire corridors ahead."
            }
        });
        Actors.AddChild(new RecruitBeat
        {
            Name = "TitanJoins", Level = this, Operative = "Apex Titan", Position = new Vector2(7000, 600),
            Lines = new[]
            {
                "APEX TITAN: Nobody crosses me today, little ones. When the shells come, stand close. I am the wall they hit.",
                "AXIOM-7: Apex Titan has joined. His Tectonic Slam can bring down the collapsed barrier ahead."
            }
        });
        // Construction is complete and physics has not started. Every entry route
        // reaches this hook; recovery reuses the mission and never calls it again.
        BeginArrivalPresentation();
    }

    /// <summary>Opens the membrane after the last spire falls; otherwise just reports progress.</summary>
    private void OnSpireDisabled()
    {
        SpiresDisabled++;
        if (SpiresDisabled >= SpireTotal)
        {
            MembraneOpen = true;
            _membrane.Open();
            ShowDialogue("OCEANUS: The membrane is falling. Move, all of you, now!", 8f);
        }
        else ShowDialogue($"Biometric spire disabled ({SpiresDisabled}/{SpireTotal}).", 4f);
    }

    /// <summary>Ends the mission when the squad reaches the extraction gate with the way open.</summary>
    protected override void UpdateMission()
    {
        if (MembraneOpen && _finalRubble.Cleared && Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) < 130f)
            CompleteMission();
    }

    /// <summary>Chooses the HUD objective from spire progress and position.</summary>
    protected override string ComputeObjective()
    {
        float x = Player.GlobalPosition.X;
        if (MembraneOpen) return _finalRubble.Cleared ? "Reach the extraction gate" : "Break the barrier with Apex Titan's Tectonic Slam";
        if (x > 4700f) return $"Disable the biometric spires ({SpiresDisabled}/{SpireTotal})";
        if (x > 3600f) return "Cross the overpass";
        if (x > 1800f) return "Fight through the burning corridors";
        return "Follow the evacuation road";
    }

    /// <summary>Embers rising from the fires replace the garden's drifting petals.</summary>
    protected override void BuildPetals()
    {
        Camera.AddChild(new CpuParticles2D
        {
            Name = "Embers", Position = new Vector2(0, 620), Amount = 90, Lifetime = 7f, Preprocess = 7f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(1300, 10),
            Gravity = new Vector2(30, -80), InitialVelocityMin = 10f, InitialVelocityMax = 50f,
            Direction = new Vector2(0, -1), Spread = 25f, Color = new Color(1f, .55f, .2f, .85f),
            ScaleAmountMin = 1.5f, ScaleAmountMax = 3.2f, ZIndex = 20
        });
    }
}
