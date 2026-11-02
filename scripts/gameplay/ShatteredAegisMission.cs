using Godot;
using System.Collections.Generic;

/// <summary>
/// Mission 4, "Shattered Aegis": the Border Defense Fortress and Act I's finale. The squad fights up to the fortress,
/// where <b>Surya Prime</b> joins before the first superheated corridor (Solar Bulwark is the only way through), then
/// phases through a lethal laser curtain with Mirage Blade, and finally faces Border Commander Voss — the
/// <see cref="CyberBossMech2D"/> — in the arena. Voss's missiles can be parried by Shogun or absorbed by Surya. When Voss
/// falls the border wall collapses and the exit opens.
/// Required route abilities: Phase, Bulwark.
/// </summary>
public partial class ShatteredAegisMission : CodeLevel
{
    /// <summary>True once Border Commander Voss has been defeated and the wall has opened.</summary>
    public bool BossDefeated { get; private set; }
    /// <summary>The boss, exposed so tests and the HUD can inspect it. Null until the player enters the arena.</summary>
    public CyberBossMech2D? Boss { get; private set; }

    private PalacePlatform _borderWall = null!;
    private PalaceObjective _exit = null!;
    private bool _bossSpawned;

    /// <summary>X position where entering triggers the boss fight.</summary>
    private const float ArenaTrigger = 5800f;

    /// <inheritdoc/>
    protected override int MissionNumber => 4;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -700, 7600, 1520);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;
    /// <inheritdoc/>
    protected override Color PlatformTint => new(.86f, .82f, 1f);
    /// <inheritdoc/>
    protected override string CompletionLine => "ACT I COMPLETE  /  BORDER WALL BREACHED. THE DOWNTOWN CORE IS OPEN.";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: Border Commander Voss is deployed with heavy plasma turrets. The wall has divided this city for five years.";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("ShrineGate", new Vector2(2150f, 600f)), ("ShrineCorridor", new Vector2(4700f, 600f)), ("ShrineArena", new Vector2(5650f, 600f))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[] { new Vector2(1060, 450), new Vector2(3800, 330), new Vector2(5320, 450) };

    /// <inheritdoc/>
    protected override string FragmentLog(int found) => found switch
    {
        1 => "LOG: The wall went up in a weekend. Taking it down will take more than a weekend.",
        2 => "LOG: Every watchtower faces inward. The wall was never meant to keep anyone out.",
        _ => "LOG: Voss signed four thousand relocation orders and never read one."
    };

    /// <summary>
    /// Lays out approach → gate road → fortress corridor → boss arena. Steps ≤ 150 px, gaps ≤ 200 px; the corridor's
    /// moving platform bridges its widest gap. The border wall reaches the world ceiling.
    /// </summary>
    protected override void BuildGeometry()
    {
        // Zone A — approach road with barricades; the first superheated corridor sits near its east end.
        AddPlatform(-100, 600, 3000, 700, PalaceSurface.Concrete);
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);
        AddPlatform(600, 540, 140, 60, PalaceSurface.Concrete);
        AddPlatform(1000, 500, 160, 100, PalaceSurface.Concrete);
        AddPlatform(1000, 450, 120, 22, PalaceSurface.Steel, true, "FragmentPerchBarricade");
        AddPlatform(1500, 540, 140, 60, PalaceSurface.Concrete);

        // Zone B — fortress corridor: a steel gantry chain over a pit, a ferry, then one long solid floor that
        // carries the second heat vent, the laser curtain and the boss arena.
        float[,] gantry = { { 2980, 560, 220 }, { 3320, 500, 220 }, { 3660, 440, 260 }, { 4020, 500, 220 } };
        for (int i = 0; i < gantry.GetLength(0); i++)
            AddPlatform(gantry[i, 0], gantry[i, 1], gantry[i, 2], 24, PalaceSurface.Steel, true, $"Gantry{i + 1}");
        AddPlatform(3740, 330, 120, 22, PalaceSurface.Steel, true, "FragmentPerchGantry");
        AddMover("FortressFerry", new Vector2(4300, 560), new Vector2(260, 0), 5f);
        AddPlatform(4640, 600, 2960, 700, PalaceSurface.Concrete, false, "FortressFloor");
        AddPlatform(5250, 450, 140, 22, PalaceSurface.Steel, true, "FragmentPerchFloor");

        // Zone C — the boss arena is the east end of the floor; the border wall seals it until Voss falls.
        _borderWall = AddPlatform(7200, WorldBounds.Position.Y, 60, FullHeightFrom(600f), PalaceSurface.Gate, false, "BorderWall");
        AddPlatform(7600, WorldBounds.Position.Y, 100, 1400, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Places the heat vents, the lethal laser curtain and the exit.</summary>
    protected override void BuildObjectives()
    {
        AddGate("SuperheatedCorridor1", GateKind.HeatVent, new Vector2(2500, 600), new Vector2(300f, 170f));
        AddGate("SuperheatedCorridor2", GateKind.HeatVent, new Vector2(5000, 600), new Vector2(320f, 170f));
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(7420, 600) };
        Actors.AddChild(_exit);
    }

    /// <summary>The curtain only Mirage Blade can cross (full height, lethal).</summary>
    protected override void BuildHazards()
    {
        AddLaserField(new Vector2(5480, WorldBounds.Position.Y), 600f - WorldBounds.Position.Y);
    }

    /// <summary>Turret sentries along the approach and corridor; Voss himself is spawned when the arena is entered.</summary>
    protected override void SpawnEnemies()
    {
        var positions = new[]
        {
            new Vector2(800, 300), new Vector2(1300, 250), new Vector2(1800, 280),
            new Vector2(2700, 230), new Vector2(3300, 200), new Vector2(3900, 250),
            new Vector2(4850, 250), new Vector2(5300, 260)
        };
        for (int i = 0; i < positions.Length; i++) AddSentry($"TurretSentry{i + 1:00}", positions[i], 85f);
    }

    /// <summary>Constructs Surya recruitment before freezing the complete mission for arrival; Voss remains guarded by the arena trigger.</summary>
    protected override void OnLevelReady()
    {
        Actors.AddChild(new RecruitBeat
        {
            Name = "SuryaJoins", Level = this, RecruitmentCondition = () => !ArrivalActive, Operative = "Surya Prime", Position = new Vector2(1900, 600),
            Lines = new[]
            {
                "SURYA PRIME: Stone can fall and still be rebuilt. The wall will fall, and the people behind it will remain.",
                "AXIOM-7: Surya Prime has joined. His Solar Bulwark absorbs heat and incoming fire, enough to cross the superheated corridors."
            }
        });
        BeginArrivalPresentation(new ShatteredAegisIntro { Name = "ShatteredAegisIntro", Layer = 40 });
    }

    /// <summary>Spawns Voss when the player first enters the arena, and ends the mission once the wall is down.</summary>
    protected override void UpdateMission()
    {
        // External diagnostic calls must obey the same arrival lock as normal physics.
        if (ArrivalActive) return;
        if (!_bossSpawned && Player.GlobalPosition.X > ArenaTrigger) SpawnBoss();
        if (BossDefeated && Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) < 130f)
            CompleteMission();
    }

    /// <summary>Creates the Voss mech, binds its health bar to the HUD and wires its defeat to the wall.</summary>
    private void SpawnBoss()
    {
        _bossSpawned = true;
        Boss = GD.Load<PackedScene>("res://scenes/gameplay/cyber_boss_mech_2d.tscn").Instantiate<CyberBossMech2D>();
        Boss.Name = "CyberBossMech2D";
        Boss.Position = new Vector2(6500, 250);
        Boss.PatrolDistance = 500f;
        Actors.AddChild(Boss);
        Boss.BossDefeated += OnBossDefeated;
        GetNode<CyberCombatHud>("CyberCombatHud").BindBoss(Boss);
        ShowDialogue("BORDER COMMANDER VOSS: Unregistered insurgents! You will not step past this line!", 8f);
    }

    /// <summary>Brings the wall down and opens the exit.</summary>
    private void OnBossDefeated()
    {
        BossDefeated = true;
        _borderWall.Open();
        ShowDialogue("AXIOM-7: Voss defeated. Fortress gates compromised. The wall is collapsing!", 8f);
    }

    /// <summary>Chooses the HUD objective from boss state and position.</summary>
    protected override string ComputeObjective()
    {
        float x = Player.GlobalPosition.X;
        if (BossDefeated) return "Cross the fallen wall to the extraction gate";
        if (x > ArenaTrigger) return "Defeat Border Commander Voss";
        if (x > 4100f) return "Cross the superheated corridor and the laser curtain";
        if (x > 2300f) return "Fight through the fortress corridor";
        return "Advance on the Border Defense Fortress";
    }
}
