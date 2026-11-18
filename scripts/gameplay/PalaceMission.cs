using Godot;
using System.Collections.Generic;

/// <summary>
/// Mission 1, "Ghost in the Gridlock": a ~9,500 px side-scrolling infiltration of the Eternal Zen Dojo played with
/// Cyber Shogun alone. The route (Garden Gate → Lantern Court → Blossom Gorge → Rooftop Run → Archive Descent →
/// Beacon Court) teaches movement first, then Shogun's signature <b>Kinetic Parry</b> in the Lantern Court
/// (<see cref="RecruitBeat"/>), and finally requires it: the demolition beacon is shielded, and the shield only
/// overloads when sentry fire is parried near it. Destroying the beacon opens the dojo gate, the Data-Blade can then be
/// taken, and the extraction portal ends the mission. Shared construction lives in <see cref="CodeLevel"/>.
/// </summary>
public partial class PalaceMission : CodeLevel
{
    /// <summary>True once the demolition beacon has been destroyed.</summary>
    public bool BeaconNeutralized { get; private set; }
    /// <summary>True once the operative has taken the Data-Blade from its pedestal.</summary>
    public bool DataBladeRecovered { get; private set; }

    /// <summary>Parries needed to overload the beacon's shield.</summary>
    private const int BeaconShieldCharges = 2;
    /// <summary>A deflected shot only counts toward the shield when it happens within this distance of the beacon.</summary>
    private const float ParryRange = 900f;

    private PalaceObjective _beacon = null!;
    private PalaceObjective _dataBlade = null!;
    private PalaceObjective _extraction = null!;
    private PalacePlatform _dojoGate = null!;
    private bool _bladeHintShown;
    private Label _bladeStatus = null!;
    // Rich labels: these two name the ability binding, which is a button image on a controller.
    private RichTextLabel _lanternInstruction = null!, _beaconStatus = null!;
    private float _parrySuccessRemaining;
    private bool _courtDeflected;

    /// <inheritdoc/>
    protected override int MissionNumber => 1;
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200f, 600f - FeetOffset);
    /// <inheritdoc/>
    protected override string? BackdropPath => "res://assets/images/level1/palace_garden_painted.png";
    /// <inheritdoc/>
    protected override string CompletionLine => "MISSION COMPLETE  /  THE ETERNAL MEMORY";
    /// <inheritdoc/>
    protected override string IntroLine() => "KAITO: They do not just take our land; they delete our soul. We will save the story.";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("ShrineGorge", new Vector2(3700f, -640f)), ("ShrineRooftop", new Vector2(5780f, -640f)), ("ShrineCourt", new Vector2(7080f, 0f))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[] { new Vector2(2085, 350), new Vector2(4450, -850), new Vector2(7630, -150) };

    /// <inheritdoc/>
    protected override string FragmentLog(int found) => found switch
    {
        1 => "ARCHIVE LOG: The garden once marked the changing of each imperial season.",
        2 => "ARCHIVE LOG: The dojo sheltered historians during the first occupation.",
        _ => "ARCHIVE LOG: The Data-Blade holds the testimony of every erased district."
    };

    /// <summary>
    /// Lays out the whole route. Every vertical step is ≤ 150 px and every gap ≤ 220 px, so every operative can finish.
    /// </summary>
    protected override void BuildGeometry()
    {
        // Zone A — Garden Gate: open ground, a low planter to hop, then a river gap with a stepping stone.
        AddPlatform(-100, 600, 1200, 700);
        AddPlatform(520, 540, 160, 60);
        AddPlatform(1230, 612, 70, 60);
        AddPlatform(1420, 600, 520, 700);
        // Cliff wall at the world's west edge so the player can't walk off the start.
        AddPlatform(-160, WorldBounds.Position.Y, 160, 2400, PalaceSurface.Wall);

        // Zone B — Lantern Court: two terraces up, an optional blossom perch, then a timber bridge under a laser.
        AddPlatform(1940, 500, 420, 800);
        AddPlatform(2360, 400, 340, 900);
        AddPlatform(2010, 350, 150, 22, PalaceSurface.Wood, true, "FragmentPerchLantern");
        AddPlatform(2700, 400, 440, 26, PalaceSurface.Wood, true, "LanternBridge");
        AddPlatform(2896, 196, 48, 28, PalaceSurface.Stone, false, "LaserBracket");

        // Zone C — Blossom Gorge: a 240 px wide shaft. Alternating timber ledges every 150 px let anyone climb it,
        // while confident players can skip them with wall jumps off the carved walls.
        AddPlatform(3140, 400, 330, 900, PalaceSurface.Stone, false, "GorgeFloor");
        AddPlatform(3140, -820, 90, 1070, PalaceSurface.Wall, false, "GorgeWestWall");
        AddPlatform(3470, -640, 90, 1040, PalaceSurface.Wall, false, "GorgeEastWall");
        float[] ledgeY = { 250, 100, -50, -200, -350, -500 };
        for (int i = 0; i < ledgeY.Length; i++)
            AddPlatform(i % 2 == 0 ? 3340 : 3230, ledgeY[i], 130, 22, PalaceSurface.Wood, true, $"GorgeLedge{i + 1}");

        // Zone D — Rooftop Run: tiled roofs with gaps, a high perch and a moving platform over the widest gap.
        AddPlatform(3560, -640, 560, 90, PalaceSurface.Roof);
        AddPlatform(4300, -700, 360, 90, PalaceSurface.Roof);
        AddPlatform(4380, -850, 140, 22, PalaceSurface.Wood, true, "FragmentPerchRoof");
        AddPlatform(4860, -640, 300, 90, PalaceSurface.Roof);
        AddMover("SkyFerry", new Vector2(5200, -640), new Vector2(240, 0), 5.5f);
        AddPlatform(5640, -640, 420, 90, PalaceSurface.Roof);

        // Zone E — Archive Descent: shelves step down into the archive. Falling off simply sends you back to a checkpoint.
        float[] shelfY = { -520, -400, -280, -160 };
        for (int i = 0; i < shelfY.Length; i++)
            AddPlatform(6120 + i * 220, shelfY[i], 160, 22, PalaceSurface.Wood, true, $"ArchiveShelf{i + 1}");
        AddPlatform(7560, -150, 140, 22, PalaceSurface.Wood, true, "FragmentPerchArchive");

        // Zone F — Archive floor and Beacon Court share one long floor; the beacon sits on a low dais.
        AddPlatform(6960, 0, 2400, 900, PalaceSurface.Stone, false, "ArchiveCourtFloor");
        AddPlatform(8420, -60, 160, 60, PalaceSurface.Stone, false, "BeaconDais");
        // The dojo gate is tall enough (it runs above the camera ceiling) that wall-jumping can't bypass it.
        _dojoGate = AddPlatform(8900, WorldBounds.Position.Y, 40, -WorldBounds.Position.Y, PalaceSurface.Gate, false, "DojoGate");
        AddPlatform(9360, WorldBounds.Position.Y, 100, 1400, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Places the shielded beacon, the Data-Blade and the extraction portal.</summary>
    protected override void BuildObjectives()
    {
        _beacon = new PalaceObjective { Name = "DemolitionBeacon", Kind = "Beacon", Position = new Vector2(8500, -60), ShieldCharges = BeaconShieldCharges };
        _beacon.Neutralized += OnBeaconNeutralized;
        Actors.AddChild(_beacon);
        _dataBlade = new PalaceObjective { Name = "DataBlade", Kind = "Blade", Sealed = true, Position = new Vector2(9080, 0) };
        Actors.AddChild(_dataBlade);
        _extraction = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(9250, 0) };
        Actors.AddChild(_extraction);
        // Parried sentry shots near the beacon overload its shield.
        Player.ParryDeflected += OnParryDeflected;
    }

    /// <summary>Adds the cycling laser grid, the low tripwire and the surge plate.</summary>
    protected override void BuildHazards()
    {
        // Tall laser across the bridge: wait for the off-phase, then run through.
        var bridgeLaser = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        bridgeLaser.Position = new Vector2(2920, 224);
        bridgeLaser.BeamLength = 176f;
        bridgeLaser.ActiveDuration = 2.0f;
        bridgeLaser.InactiveDuration = 2.2f;
        Actors.AddChild(bridgeLaser);
        // Low tripwire in the archive: short enough to simply jump over.
        var tripwire = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        tripwire.Position = new Vector2(7660, -88);
        tripwire.BeamLength = 88f;
        tripwire.AlwaysActive = true;
        tripwire.Damage = 15f;
        Actors.AddChild(tripwire);
        // Surge plate; placed a few pixels above the floor so the player's feet overlap its damage area.
        var surge = GD.Load<PackedScene>("res://scenes/gameplay/cyber_hazard_floor_2d.tscn").Instantiate<CyberHazardFloor2D>();
        surge.Position = new Vector2(7280, -8);
        surge.FloorWidth = 180f;
        surge.ActiveDuration = 2.4f;
        surge.InactiveDuration = 2.0f;
        Actors.AddChild(surge);
    }

    /// <summary>Hovering demolition sentries, hand-placed so each zone gets a new threat angle.</summary>
    protected override void SpawnEnemies()
    {
        var positions = new[] {
            new Vector2(960, 330), new Vector2(2140, 150), new Vector2(2520, 40),
            new Vector2(3850, -900), new Vector2(4620, -920), new Vector2(5420, -900),
            new Vector2(7400, -330), new Vector2(7840, -330),
            new Vector2(8240, -320), new Vector2(8700, -330), new Vector2(8500, -470)
        };
        for (int i = 0; i < positions.Length; i++) AddSentry($"PurgeSentry{i + 1:00}", positions[i], i >= 8 ? 90f : 70f);
    }

    /// <summary>Adds the Kinetic Parry lesson in the Lantern Court, just before the sentries start firing in earnest.</summary>
    protected override void OnLevelReady()
    {
        _lanternInstruction = RichGuidanceLabel(GetNode<Control>("MissionHUD/HUDRoot"), "LanternInstruction", new Vector2(0, 230));
        _lanternInstruction.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _lanternInstruction.OffsetLeft = -340; _lanternInstruction.OffsetRight = 340;
        _lanternInstruction.OffsetTop = 230; _lanternInstruction.OffsetBottom = 310;
        _lanternInstruction.Visible = false;
        // Keep all sealed-state lines above the extraction portal's lintel rather than behind its bright bar.
        _bladeStatus = GuidanceLabel(_dataBlade, "BladeStatus", new Vector2(-180, -260));
        _bladeStatus.Size = new Vector2(360, 105);
        _beaconStatus = RichGuidanceLabel(_beacon, "BeaconStatus", new Vector2(-240, -245));
        _beaconStatus.Size = new Vector2(480, 110);
        GuidanceLabel(World, "ArchiveRoofSign", new Vector2(5700, -860)).Text = WildfireText.Get("PALACE_ARCHIVE_ROOF", "ARCHIVE →\nDESCEND THE SHELVES");
        GuidanceLabel(World, "ArchiveDescentSign", new Vector2(6530, -420)).Text = WildfireText.Get("PALACE_ARCHIVE_DOWN", "↓ ARCHIVE / BEACON COURT →");
        Actors.AddChild(new RecruitBeat
        {
            Name = "ParryLesson", Level = this, Operative = "Cyber Shogun", Position = new Vector2(1700, 600),
            Lines = new[]
            {
                "AXIOM-7: Sentry fire ahead. Time your Kinetic Parry on the shot and it becomes theirs.",
                "AXIOM-7: The demolition beacon is shielded. Only parried sentry fire can overload that field."
            }
        });
    }

    /// <summary>Reuses the mission's cyan/gold, wrapped world-marker styling without new assets or voice cues.</summary>
    private static Label GuidanceLabel(Node parent, string name, Vector2 position)
    {
        var label = new Label { Name = name, Position = position, Size = new Vector2(320, 90),
            HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color("#77efff") };
        label.AddThemeFontSizeOverride("font_size", 18);
        parent.AddChild(label);
        return label;
    }

    /// <summary>Same as <see cref="GuidanceLabel"/>, but able to draw controller button images; fixed size, so no fit-to-content.</summary>
    private static RichTextLabel RichGuidanceLabel(Node parent, string name, Vector2 position)
    {
        var label = InputGlyphText.CreateLabel(18, Colors.White);
        label.Name = name;
        label.FitContent = false;
        label.Position = position;
        label.Size = new Vector2(320, 90);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Modulate = new Color("#77efff");
        parent.AddChild(label);
        return label;
    }

    /// <summary>Maintains the Lantern Court instruction independently of the optional recruit lesson's prompt timeout.</summary>
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_lanternInstruction == null || MissionComplete || ArrivalActive) return;
        _parrySuccessRemaining = Mathf.Max(0, _parrySuccessRemaining - (float)delta);
        bool inCourt = Player.Position.X >= 1550 && Player.Position.X < 3100;
        _lanternInstruction.Visible = inCourt && (!_courtDeflected || _parrySuccessRemaining > 0);
        InputGlyphText.Set(_lanternInstruction, _courtDeflected
            ? WildfireText.Get("PALACE_PARRY_SUCCESS", "DEFLECTION CONFIRMED / KINETIC PARRY")
            : WildfireText.Get("PALACE_PARRY_CONTEXT", "KINETIC PARRY / {ability} as a sentry shot lands. Deflect it back.").Replace("{ability}", ContextGlyph("karate")));
        InputGlyphText.Set(_beaconStatus, BeaconNeutralized ? "" : BeaconGuidance());
        _bladeStatus.Text = BeaconNeutralized
            ? WildfireText.Get("PALACE_BLADE_READY", "DATA-BLADE / ACCESSIBLE")
            : _beacon.Shielded
                ? WildfireText.Get("PALACE_BLADE_SEALED", "DATA-BLADE / SEALED\nOVERLOAD SHIELD, THEN STRIKE BEACON")
                : WildfireText.Get("PALACE_BLADE_EXPOSED", "DATA-BLADE / SEALED\nSTRIKE THE EXPOSED BEACON");
        _bladeStatus.Modulate = new Color(BeaconNeutralized ? "#77ffba" : "#ffcf8a");
    }

    /// <summary>Reads live shield charges after recovery, so damage is never advertised while the field remains active.</summary>
    private string BeaconGuidance() => _beacon.Shielded
        ? WildfireText.Get("PALACE_BEACON_SHIELD", "BEACON SHIELD / {charges} CHARGES\nKINETIC PARRY: {ability} as nearby sentry fire lands")
            .Replace("{charges}", _beacon.ShieldCharges.ToString()).Replace("{ability}", ContextGlyph("karate"))
        : WildfireText.Get("PALACE_BEACON_EXPOSED", "Shield down—strike the beacon");

    /// <summary>A deflected shot near the beacon knocks down one shield charge.</summary>
    private void OnParryDeflected(Node2D source)
    {
        if (Player.Position.X >= 1550 && Player.Position.X < 3100)
        {
            _courtDeflected = true;
            _parrySuccessRemaining = 3;
        }
        if (BeaconNeutralized || !_beacon.Shielded) return;
        if (_beacon.GlobalPosition.DistanceTo(Player.GlobalPosition) > ParryRange) return;
        bool dropped = _beacon.HitShield();
        ShowDialogue(dropped ? "BEACON SHIELD OVERLOADED. Strike it now!" : "Shield charge overloaded. Parry again.", 4f);
    }

    /// <summary>Runs one pickup pass (fragments, Data-Blade, extraction) immediately; integration tests use it after placing the player.</summary>
    public void CollectNearbyObjectives()
    {
        CollectFragments();
        UpdateMission();
    }

    /// <summary>Handles the Data-Blade pickup and the extraction portal.</summary>
    protected override void UpdateMission()
    {
        float bladeDistance = Player.GlobalPosition.DistanceTo(_dataBlade.GlobalPosition + new Vector2(0, -60));
        if (!DataBladeRecovered && bladeDistance < 110f)
        {
            if (!BeaconNeutralized)
            {
                if (!_bladeHintShown) ShowDialogue("The Data-Blade is sealed. Destroy the demolition beacon first.", 5f);
                _bladeHintShown = true;
            }
            else
            {
                DataBladeRecovered = true;
                _dataBlade.SetCollected();
                ShowDialogue("DATA-BLADE SECURED. AXIOM-7: Enforcer signatures converging. Reach the extraction gate!", 8f);
                AddSentry("ReroutedEnforcer1", new Vector2(8980, -330), 100f);
                AddSentry("ReroutedEnforcer2", new Vector2(9170, -360), 100f);
            }
        }
        if (DataBladeRecovered && Player.GlobalPosition.DistanceTo(_extraction.GlobalPosition + new Vector2(0, -80)) < 120f)
        {
            ShowDialogue("The dojo falls, but the resistance rises. Tokyo remembers.", 6f);
            CompleteMission();
        }
    }

    /// <summary>Picks the HUD objective from the beacon/blade state and how far the player has advanced.</summary>
    protected override string ComputeObjective()
    {
        float x = Player.GlobalPosition.X;
        if (DataBladeRecovered) return "Reach the extraction gate";
        if (BeaconNeutralized) return "Take the Data-Blade from the dojo core";
        if (x > 7000f) return BeaconGuidance();
        if (x > 5600f) return "Descend into the archive";
        if (x > 3560f) return "Run the palace rooftops";
        if (x > 3100f) return "Climb the Blossom Gorge";
        if (x > 1500f) return "Cross the Lantern Court";
        return "Infiltrate the Eternal Zen Dojo";
    }

    /// <summary>Opens the dojo gate once the beacon's integrity reaches zero.</summary>
    private void OnBeaconNeutralized()
    {
        BeaconNeutralized = true;
        _dataBlade.Sealed = false;
        _dojoGate.Open();
        ShowDialogue("AXIOM-7: Beacon neutralized. The dojo gate is unsealed. Take the Data-Blade!", 8f);
    }
}
