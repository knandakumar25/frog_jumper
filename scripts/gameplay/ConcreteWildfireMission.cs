using Godot;
using System.Collections.Generic;

/// <summary>
/// Mission 2: six linear sectors of finite combat, local rescues, biometric spires and a stationary siege battery.
/// Instance progress survives checkpoint recovery; loading the scene resets it under the existing campaign save model.
/// </summary>
public partial class ConcreteWildfireMission : CodeLevel, IFiniteCombatHost
{
    /// <inheritdoc/>
    public CyberShogun CombatPlayer => Player;
    /// <inheritdoc/>
    public bool HasCombatEvent(string id) => Progress.Has(id);
    /// <inheritdoc/>
    public void CompleteCombatEvent(string id) => Progress.Complete(id);
    /// <summary>Retains Level 2's original activation behavior and authored group sizes.</summary>
    public bool CanActivateEncounter(int members) => true;
    /// <summary>Stable scene-local objective and reward ledger.</summary>
    public WildfireProgress Progress { get; } = new();
    /// <summary>Distinct spires destroyed, independent of repeated callbacks.</summary>
    public int SpiresDisabled => Progress.Spires;
    /// <summary>Counts shelter arrivals, not just safe-route triggers.</summary>
    public int CivilianGroupsEvacuated => Progress.Rescued;
    /// <summary>True after all three individually identified spires fall.</summary>
    public bool MembraneOpen => SpiresDisabled == 3;
    /// <summary>New actors cannot activate during arrival, recovery or completion.</summary>
    public bool GameplaySuspended => ArrivalActive || _recoveringLocal || MissionComplete || Player.CurrentHealth <= 0;
    /// <summary>One controller owns all pending artillery and cancellation.</summary>
    public WildfireArtillery Artillery { get; private set; } = null!;
    /// <summary>Stationary relay/core encounter.</summary>
    public WildfireBattery Battery { get; private set; } = null!;
    /// <summary>Proximity to extraction is insufficient without every mandatory objective.</summary>
    public bool ExtractionReady => Progress.CanExtract;
    private readonly List<WildfireEncounter> _encounters = new();
    private readonly List<(PalaceObjective Spire, string Encounter)> _spireDefenses = new();
    private PalacePlatform _membrane = null!;
    private PalaceObjective _exit = null!;
    private AbilityGate _finalRubble = null!;
    private bool _recoveringLocal;

    /// <inheritdoc/>
    protected override int MissionNumber => 2;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -700, 35800, 1520);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 600 - FeetOffset);
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Wildfire;
    /// <inheritdoc/>
    protected override Color PlatformTint => new(1, .86f, .8f);
    /// <inheritdoc/>
    protected override string CompletionLine => WildfireText.Get("WF_COMPLETE", "MISSION COMPLETE / ALL EVACUEES SAFE. BATTERY SILENCED.");
    /// <inheritdoc/>
    protected override string IntroLine() => WildfireText.Get("WF_INTRO", "AXIOM-7: Secure three civilian routes, disable the biometric spires, and silence the siege battery.");
    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) => WildfireText.Get("WF_BRIEFING", "Secure three civilian shelters. Disable the biometric spires. Destroy the siege battery.");
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("ShrineRoad", new Vector2(5400, 600)), ("ShrineOverpass", new Vector2(16000, 600)),
        ("ShrineMembrane", new Vector2(24800, 600)), ("ShrineBattery", new Vector2(28300, 600))
    };
    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[]
        { new Vector2(8500, 390), new Vector2(13350, 450), new Vector2(21300, 170) };
    /// <inheritdoc/>
    protected override string FragmentLog(int found) => WildfireText.Get($"WF_FRAGMENT_{found}", found switch
    {
        1 => "LOG: They fenced our neighbourhoods one street at a time, calling each fence temporary.",
        2 => "LOG: The scanner spires read faces, not names. We started wearing each other's.",
        _ => "LOG: Zone 4 held four thousand families. The census says forty."
    });

    /// <summary>Complete blockout: rises at most 110px, ferry docking gaps at most 170px, stable islands between crossings.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, -700, 160, 2100, PalaceSurface.Wall);
        AddPlatform(-100, 600, 11300, 700, PalaceSurface.Concrete);
        foreach (float x in new[] { 900f, 2400, 3700, 7000, 9200, 10400 })
            AddPlatform(x, 510, 170, 90, PalaceSurface.Concrete);
        AddPlatform(8200, 500, 160, 100, PalaceSurface.Concrete);
        AddPlatform(8400, 390, 230, 24, PalaceSurface.Steel, true, "FragmentPerchRoad");
        for (int i = 0; i < 4; i++)
        {
            float x = 11200 + i * 1200;
            AddMover($"OverpassFerry{i + 1}", new Vector2(x + 150, 600), new Vector2(330, 0), 4.5f);
            AddPlatform(x + 500, 600, 700, 70, PalaceSurface.Concrete, false, $"OverpassIsland{i + 1}");
        }
        AddPlatform(13150, 520, 150, 80, PalaceSurface.Concrete);
        AddPlatform(13270, 450, 180, 24, PalaceSurface.Steel, true, "FragmentPerchIsland");
        AddPlatform(16000, 600, 19800, 700, PalaceSurface.Concrete);
        // Broad stairs on both sides allow every recruited operative to climb without wall-jump assumptions.
        for (int i = 0; i < 3; i++)
        {
            AddPlatform(19500 + i * 220, 490 - i * 110, 220, 110 + i * 110, PalaceSurface.Concrete);
            AddPlatform(21100 + i * 220, 270 + i * 110, 220, 330 - i * 110, PalaceSurface.Concrete);
        }
        AddPlatform(20160, 270, 940, 330, PalaceSurface.Concrete, false, "SpireTower");
        AddPlatform(21180, 170, 240, 24, PalaceSurface.Steel, true, "FragmentPerchCompound");
        foreach (float x in new[] { 17000f, 18100, 22600, 26100, 28000, 29000, 31900 })
            AddPlatform(x, 500, 170, 100, PalaceSurface.Concrete);
        _membrane = AddPlatform(24400, -700, 40, 1300, PalaceSurface.Gate, false, "Membrane");
        AddPlatform(35800, -700, 100, 2000, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Places isolated ability lessons, civilian-route vents, permanent barriers and individually identified spires.</summary>
    protected override void BuildObjectives()
    {
        AddGate("OceanusPracticeFire", GateKind.FireVent, new Vector2(6100, 600), new Vector2(180, 100));
        AddGate("FireCorridor1", GateKind.FireVent, new Vector2(6700, 600), new Vector2(320, 200));
        var residentialFire = AddGate("ResidentialRouteFire", GateKind.FireVent, new Vector2(9850, 600), new Vector2(180, 170));
        AddGate("FireCorridor2", GateKind.FireVent, new Vector2(10700, 600), new Vector2(300, 170));
        var overpassFire = AddGate("OverpassRouteFire", GateKind.FireVent, new Vector2(15600, 600), new Vector2(150, 150));
        AddGate("SpireFire", GateKind.FireVent, new Vector2(22900, 600), new Vector2(280, 190));
        TrackBarrier(AddGate("TitanPracticeRubble", GateKind.RubbleWall, new Vector2(25500, 600), new Vector2(140, FullHeightFrom(600))), "lesson");
        TrackBarrier(AddGate("DefendedRubble", GateKind.RubbleWall, new Vector2(27200, 600), new Vector2(140, FullHeightFrom(600))), "approach");
        _finalRubble = AddGate("MembraneRubble", GateKind.RubbleWall, new Vector2(33100, 600), new Vector2(150, FullHeightFrom(600)));
        TrackBarrier(_finalRubble, "final");
        AddSpire("BiometricSpire1", "street", new Vector2(17600, 600));
        AddSpire("BiometricSpire2", "tower", new Vector2(20500, 270));
        AddSpire("BiometricSpire3", "courtyard", new Vector2(23400, 600));
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(35100, 600) };
        Actors.AddChild(_exit);
        AddRescue("residential", new Vector2(9650, 600), new[] { "encounter:residential-route" }, new[] { residentialFire });
        AddRescue("overpass", new Vector2(15380, 600), new[] { "encounter:overpass-exit" }, new[] { overpassFire });
        AddRescue("extraction", new Vector2(33300, 600), new[] { "encounter:extraction-guard", "barrier:final", "battery:core" }, System.Array.Empty<AbilityGate>());
    }

    /// <summary>Records a permanent wall by stable ID, regardless of repeated gate signals.</summary>
    private void TrackBarrier(AbilityGate gate, string id) => gate.GateCleared += () => Progress.Complete("barrier:" + id);

    /// <summary>Local defenders shield each spire; its death is individually recorded before opening the membrane.</summary>
    private void AddSpire(string name, string id, Vector2 position)
    {
        var spire = new PalaceObjective { Name = name, Kind = "Beacon", Position = position, ShieldCharges = 1 };
        spire.Neutralized += () =>
        {
            if (!Progress.Complete("spire:" + id)) return;
            if (!MembraneOpen) return;
            _membrane.Open();
            ShowDialogue(WildfireText.Get("WF_MEMBRANE", "OCEANUS: The membrane is falling. Move, all of you, now!"), 7);
        };
        _spireDefenses.Add((spire, "spire-" + id));
        Actors.AddChild(spire);
    }

    /// <summary>Each family follows a 440px same-floor route into shelter, never across a platform gap.</summary>
    private void AddRescue(string id, Vector2 position, string[] conditions, AbilityGate[] fires)
    {
        Actors.AddChild(new WildfireRescueGroup { Name = "Rescue_" + id, Id = id, Position = position,
            Mission = this, Conditions = conditions, RouteFires = fires, ShelterOffset = new Vector2(440, 0) });
    }

    /// <summary>Finite mixed groups alternate with traversals and quiet recovery spaces; no repeating waves or health inflation.</summary>
    protected override void SpawnEnemies()
    {
        AddEncounter("road-parry", 1200, 2);
        AddEncounter("road-dash", 3500, 3);
        AddEncounter("residential-entry", 7350, 3);
        AddEncounter("residential-roofs", 8750, 3);
        AddEncounter("residential-route", 9550, 3);
        AddEncounter("overpass-west", 12100, 2);
        AddEncounter("overpass-center", 13300, 2, 520);
        AddEncounter("overpass-east", 14500, 2);
        AddEncounter("overpass-exit", 15500, 3);
        AddEncounter("compound-barricade", 16800, 3);
        AddEncounter("spire-street", 17550, 4);
        AddEncounter("compound-approach", 18800, 3);
        AddEncounter("spire-tower", 20500, 4, 270);
        AddEncounter("compound-descent", 22000, 3);
        AddEncounter("spire-courtyard", 23400, 4);
        AddEncounter("breach-approach", 26500, 3);
        AddEncounter("breach-defense", 27600, 4);
        AddEncounter("battery-approach", 29500, 3);
        var reinforcements = AddEncounter("battery-reinforcements", 31000, 4, 600, true);
        AddEncounter("extraction-guard", 33800, 3);
        Battery = new WildfireBattery { Name = "SiegeBattery", Position = new Vector2(30800, 600), Mission = this, Reinforcements = reinforcements };
        Actors.AddChild(Battery);
    }

    /// <summary>Compacts a formation within its safe landing; hovering sentries are within normal jump/melee reach.</summary>
    private WildfireEncounter AddEncounter(string id, float x, int count, float floor = 600, bool manual = false)
    {
        var members = new (Vector2, bool)[count];
        for (int i = 0; i < count; i++)
            members[i] = (new Vector2(x + (i - (count - 1) * .5f) * 120, i % 2 == 0 ? floor : floor - 140), i % 2 == 0);
        var encounter = new WildfireEncounter { Name = "Encounter_" + id, Id = id, Mission = this,
            Members = members, ManualActivation = manual, Activation = new Rect2(x - 650, floor - 650, 1300, 750) };
        _encounters.Add(encounter);
        Actors.AddChild(encounter);
        return encounter;
    }

    /// <summary>Shells target only authored ground combat lanes; all checkpoints, lessons and shelter routes are outside them.</summary>
    protected override void BuildHazards()
    {
        Artillery = new WildfireArtillery { Name = "Artillery", Mission = this, Lanes = new[]
        {
            new Rect2(2600, 200, 650, 400), new Rect2(7450, 200, 650, 400),
            new Rect2(18000, 200, 650, 400), new Rect2(22200, 200, 650, 400),
            new Rect2(26100, 200, 650, 400), new Rect2(28900, 200, 2800, 400)
        } };
        Actors.AddChild(Artillery);
    }

    /// <summary>Recruits precede their required barriers. All children exist before the unchanged arrival lock captures them.</summary>
    protected override void OnLevelReady()
    {
        Actors.AddChild(new RecruitBeat { Name = "OceanusJoins", Level = this, Operative = "Oceanus", Position = new Vector2(5700, 600),
            Lines = new[] { "OCEANUS: As long as I draw breath, they will not relocate us like cargo in our own city. The tide rises HERE.",
                WildfireText.Get("WF_OCEANUS_LESSON", "AXIOM-7: Oceanus has joined. Practice Tidal Torrent on the isolated vent, then secure the residential route.") } });
        Actors.AddChild(new RecruitBeat { Name = "TitanJoins", Level = this, Operative = "Apex Titan", Position = new Vector2(25000, 600),
            Lines = new[] { "APEX TITAN: Nobody crosses me today, little ones. When the shells come, stand close. I am the wall they hit.",
                WildfireText.Get("WF_TITAN_LESSON", "AXIOM-7: Apex Titan has joined. Practice Tectonic Slam on the safe rubble before the defended breach.") } });
        Battery.BindHud(GetNode<Control>("MissionHUD/HUDRoot"));
        GetNode<Control>("MissionHUD/HUDRoot").GetChild<Control>(0).CustomMinimumSize = new Vector2(410, 135);
        // Set DressingEnabled=false for route blockout checks; art generation follows traversal review.
        if (DressingEnabled) DressRoute();
        BeginArrivalPresentation();
        // The generic menu banner advertises unrelated navigation/rotation controls; this mission has its own combat hints.
        GetNode<ConcreteWildfireIntro>("ConcreteWildfireIntro").Finished += () =>
            GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);
    }

    /// <summary>Restores the menu's generic help when leaving this mission and preserves shared roster cleanup.</summary>
    public override void _ExitTree()
    {
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", true);
        base._ExitTree();
    }

    /// <summary>Allows the same route to be traversed as a blockout before dressing; defaults to the finished presentation.</summary>
    [Export] public bool DressingEnabled { get; set; } = true;

    /// <summary>Arrival-matched cyan/navy facades sit behind explicit concrete collision surfaces.</summary>
    private void DressRoute()
    {
        foreach (float x in new[] { 600f, 2800, 4700, 7200, 8800, 10300, 16600, 18400, 22200, 23800, 26200, 27900, 29200, 32300, 34600 })
        {
            var scenery = new Node2D { Name = "DamagedFacade", Position = new Vector2(x, 600), ZIndex = -3 };
            World.AddChild(scenery);
            WildfireVisual.AddSprite(scenery, "facade", new Vector2(950, 600), new Vector2(0, -290));
        }
        foreach (var (x, key, fallback) in new[]
        {
            (400f, "WF_SECTOR_ROAD", "01 / EVACUATION ROAD"), (5400f, "WF_SECTOR_BLOCKS", "02 / BURNING RESIDENTIAL BLOCKS"),
            (11000f, "WF_SECTOR_OVERPASS", "03 / BROKEN OVERPASS"), (16200f, "WF_SECTOR_COMPOUND", "04 / BIOMETRIC COMPOUND"),
            (24800f, "WF_SECTOR_BREACH", "05 / TITAN'S BREACH"), (28300f, "WF_SECTOR_BATTERY", "06 / SIEGE BATTERY AND EXTRACTION")
        })
        {
            var sign = new Label { Text = WildfireText.Get(key, fallback), Position = new Vector2(x, 340), Modulate = new Color("#77efff") };
            sign.AddThemeFontSizeOverride("font_size", 23);
            World.AddChild(sign);
        }
    }

    /// <summary>Defeating each local formation exposes its spire. Full objective readiness guards the existing campaign handoff.</summary>
    protected override void UpdateMission()
    {
        foreach (var (spire, encounter) in _spireDefenses)
            if (spire.Shielded && Progress.Has("encounter:" + encounter)) spire.HitShield();
        if (!ExtractionReady || Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) >= 130) return;
        Artillery.StopForever();
        CompleteMission();
    }

    /// <summary>Names missing local safety conditions, including any skipped civilian group requiring backtracking.</summary>
    protected override string ComputeObjective()
    {
        string key, text;
        float x = Player.GlobalPosition.X;
        if (x < 5200) { key = "ROAD"; text = "Follow the evacuation road; parry cyan rounds and dash between cover"; }
        else if (!Progress.Has("rescue:residential")) { key = "RESIDENTIAL"; text = "Residential shelter: defeat guards and douse the route fire"; }
        else if (!Progress.Has("rescue:overpass")) { key = "OVERPASS"; text = "Cross the overpass; clear guards and fire at the east shelter"; }
        else if (!MembraneOpen) { key = "SPIRES"; text = "Defeat each spire's defenders, then destroy its exposed scanner"; }
        else if (x < 25500 && !Progress.Has("barrier:lesson")) { key = "TITAN"; text = "Recruit Apex Titan beyond the membrane; practice Slam on the safe wall"; }
        else if (!Battery.Destroyed) { key = "BATTERY"; text = "Destroy both battery relays, then the exposed core"; }
        else if (!_finalRubble.Cleared) { key = "FINAL_WALL"; text = "Break the final barrier with Titan's Tectonic Slam"; }
        else if (!Progress.Has("rescue:extraction")) { key = "FINAL_RESCUE"; text = "Defeat extraction guards and secure the last civilian group"; }
        else { key = "EXTRACT"; text = "All civilians safe. Reach extraction"; }
        return $"{WildfireText.Get("WF_EVAC", "EVAC")} {CivilianGroupsEvacuated}/3 | {WildfireText.Get("WF_SPIRES", "SPIRES")} {SpiresDisabled}/3\n{WildfireText.Get("WF_OBJECTIVE_" + key, text)}";
    }

    /// <summary>One reward per authored member prevents farming during incomplete-encounter restarts.</summary>
    public void RewardMember(string id) { if (Progress.Reward(id)) Score.AddPoint(); }
    /// <summary>Mission-owned transient rounds share the arrival freeze and are cleared before any recovery teleport.</summary>
    public void LaunchRound(Vector2 origin, Vector2 heading, WildfireGunner source)
    {
        if (GameplaySuspended) return;
        var round = new WildfireRound { Heading = heading, Source = source };
        Actors.AddChild(round);
        round.GlobalPosition = origin;
    }
    /// <summary>Cancels pending impacts and abandons incomplete attempts before teleporting; completed objects remain intact.</summary>
    protected override void OnRecoveryStarting()
    {
        _recoveringLocal = true;
        Artillery.CancelPending();
        foreach (Node node in GetTree().GetNodesInGroup("wildfire_rounds"))
            if (IsAncestorOf(node)) { node.ProcessMode = ProcessModeEnum.Disabled; node.QueueFree(); }
        foreach (var encounter in _encounters) encounter.ResetIncomplete();
    }
    /// <summary>Rearms local proximity activation after the player has been restored.</summary>
    protected override void OnRecoveryCompleted() => _recoveringLocal = false;
    /// <summary>The east-overpass checkpoint is earned by securing that shelter's route, before the compound.</summary>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) =>
        shrine.Name != "ShrineOverpass" || Progress.Has("route:overpass");
    /// <summary>Embers preserve the arrival cinematic's warm fire against cool concrete.</summary>
    protected override void BuildPetals()
    {
        Camera.AddChild(new CpuParticles2D { Name = "Embers", Position = new Vector2(0, 620), Amount = 90,
            Lifetime = 7, Preprocess = 7, EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(1300, 10), Gravity = new Vector2(30, -80),
            InitialVelocityMin = 10, InitialVelocityMax = 50, Direction = Vector2.Up, Spread = 25,
            Color = new Color(1, .55f, .2f, .85f), ScaleAmountMin = 1.5f, ScaleAmountMax = 3.2f, ZIndex = 20 });
    }
}
