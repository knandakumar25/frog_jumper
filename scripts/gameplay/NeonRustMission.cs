using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Eight-sector freight sabotage mission. Exact objective dependencies and finite combat survive scene-local recovery;
/// Replay rebuilds the original three-operative squad and preserves the existing arrival presentation.
/// </summary>
public partial class NeonRustMission : CodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local objectives, lessons, encounter completion and nonfarmable rewards.</summary>
    public NeonRustProgress Progress { get; } = new();
    /// <summary>Compatibility property for cinematic and campaign callers.</summary>
    public bool CommsBlackedOut => Progress.CommsBlackedOut;
    /// <summary>All mandatory sabotage, recruitment and escape conditions, independent of position.</summary>
    public bool ExtractionReady => Progress.CanExtract;
    /// <inheritdoc/>
    public CyberShogun CombatPlayer => Player;
    /// <inheritdoc/>
    public bool GameplaySuspended => ArrivalActive || _recoveringLocal || MissionComplete || Player.CurrentHealth <= 0;
    /// <inheritdoc/>
    public float EncounterRetirementDistance => 1500;
    private readonly List<WildfireEncounter> _encounters = new();
    private readonly List<(PalacePlatform Barrier, string Event)> _seals = new();
    private readonly List<(AbilityGate Gate, string Event, Label Marker)> _targets = new();
    private readonly List<(Node Node, ProcessModeEnum Mode)> _recoveryModes = new();
    private PalaceObjective _exit = null!;
    private RecruitBeat _aura = null!, _mirage = null!;
    private bool _recoveringLocal;
    private Vector2 _previousPosition;
    /// <inheritdoc/>
    protected override int MissionNumber => 3;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 48000, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 600 - FeetOffset);
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.NeonRust;
    /// <inheritdoc/>
    protected override Color PlatformTint => new(.8f, .98f, 1);
    /// <inheritdoc/>
    protected override string CompletionLine => WildfireText.Get("NR_COMPLETE", "MISSION COMPLETE / SHIPMENTS HIJACKED. COMM GRID BLACKED OUT.");
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: Comm towers are broadcasting denial of service. Reach the relay and black out their patrol network.";
    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) => WildfireText.Get("NR_BRIEFING", "Hijack three shipments. Disable both uplinks. Black out the relay and escape with Mirage.");
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("ShrineFreight", new Vector2(5850, 600)), ("ShrineRunoff", new Vector2(11850, 600)),
        ("ShrineHarmonic", new Vector2(17850, 600)), ("ShrineWest", new Vector2(21900, 600)),
        ("ShrineTower", new Vector2(27900, 600)), ("ShrineSummit", new Vector2(34900, -500)),
        ("ShrineEscape", new Vector2(40900, -500))
    };
    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[]
        { new Vector2(8350, 400), new Vector2(25050, 400), new Vector2(45350, 400) };
    /// <inheritdoc/>
    protected override string FragmentLog(int found) => WildfireText.Get($"NR_FRAGMENT_{found}", found switch
    {
        1 => "ARCHIVE: Every crate carries a stolen name. The manifests remember.",
        2 => "ARCHIVE: West uplink sold the city's voices as patrol coordinates.",
        _ => "ARCHIVE: In the blackout, the first unscheduled train went home."
    });
    /// <summary>Walkable route height; 100px tower rises and 110px escape drops have broad stationary landings.</summary>
    public static float RouteFloor(float x) => x < 29000 ? 600 : x < 34500 ? 600 - (1 + (int)((x - 29000) / 500)) * 100 :
        x < 41100 ? -500 : x < 43100 ? -500 + (1 + (int)((x - 41100) / 200)) * 110 : 600;

    /// <summary>Builds collision first: freight cover, service decks, tower stairs and locally reconnecting archive loops.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160, -1300, 160, 2600, PalaceSurface.Wall, false, "WestWall");
        AddPlatform(0, 600, 29000, 700, PalaceSurface.Concrete, false, "DepotFoundation");
        for (int i = 0; i < 11; i++)
            AddPlatform(29000 + i * 500, 500 - i * 100, 500, 800 + i * 100, PalaceSurface.Concrete, false, $"TowerStep{i + 1}");
        AddPlatform(34500, -500, 6600, 1800, PalaceSurface.Steel, false, "RelayDeck");
        for (int i = 0; i < 10; i++)
            AddPlatform(41100 + i * 200, -390 + i * 110, 200, 1800 - i * 110, PalaceSurface.Steel, false, $"EscapeStep{i + 1}");
        AddPlatform(43100, 600, 4900, 700, PalaceSurface.Concrete, false, "ExtractionRoad");
        AddPlatform(48000, -1300, 100, 2600, PalaceSurface.Wall, false, "EastWall");
        // Low cover breaks gunner sightlines without exceeding the weakest operative's jump clearance.
        foreach (float x in new[] { 1800f, 3100, 4700, 7300, 8700, 10100, 12900, 14700, 16200,
            20100, 21100, 23100, 24400, 26100, 28600, 30200, 31700, 33200, 35700, 36700,
            37700, 38500, 39300, 43500, 44700, 46100 })
            AddPlatform(x, RouteFloor(x) - 90, 150, 90, PalaceSurface.Concrete, false, $"DepotCover{x}");
        // Optional upper combat/service decks have two-way local access and safe stationary ground underneath.
        foreach (float x in new[] { 6800f, 9400, 12500, 15300, 22600, 25400 })
        {
            AddPlatform(x, 500, 180, 100, PalaceSurface.Concrete);
            AddPlatform(x + 150, 400, 600, 24, PalaceSurface.Steel, true, $"ServiceDeck{x}");
            AddPlatform(x + 720, 500, 180, 100, PalaceSurface.Concrete);
        }
        foreach (float x in new[] { 8100f, 24800, 45100 })
        {
            AddPlatform(x, 500, 180, 100, PalaceSurface.Concrete);
            AddPlatform(x + 150, 400, 240, 24, PalaceSurface.Steel, true, $"ArchivePerch{x}");
        }
        BuildDepotArtwork();
    }

    /// <summary>Dresses the movement-tested blockout with quiet background freight, machinery and surveillance supports.</summary>
    private void BuildDepotArtwork()
    {
        for (int i = 0; i < 12; i++)
        {
            float x = 1400 + i * 3900;
            var backdrop = new Node2D { Name = $"DepotScenery{i}", Position = new Vector2(x, RouteFloor(x)),
                ZIndex = -5, Modulate = new Color(.65f, .75f, .85f, .85f) };
            World.AddChild(backdrop);
            NeonRustVisual.AddSprite(backdrop, "depot", new Vector2(1900, 950), new Vector2(0, -430));
        }
        foreach (float x in new[] { 7400f, 9900, 12800, 15600, 22900, 25700 })
        {
            var freight = new Node2D { Name = $"FreightArt{x}", Position = new Vector2(x, 600), ZIndex = -2 };
            World.AddChild(freight);
            NeonRustVisual.AddSprite(freight, "freight", new Vector2(900, 390), new Vector2(0, -162));
        }
        foreach (float x in new[] { 13400f, 16600, 35800, 38600 })
        {
            var machine = new Node2D { Name = $"MachineryArt{x}", Position = new Vector2(x, RouteFloor(x)), ZIndex = -2 };
            World.AddChild(machine);
            NeonRustVisual.AddSprite(machine, "machinery", new Vector2(330, 350), new Vector2(0, -158));
        }
        foreach (float x in new[] { 29400f, 30900, 32400, 33900 })
        {
            var support = new Node2D { Name = $"TowerSupportArt{x}", Position = new Vector2(x, RouteFloor(x)), ZIndex = -3 };
            World.AddChild(support);
            NeonRustVisual.AddSprite(support, "support", new Vector2(410, 650), new Vector2(0, -290));
        }
    }

    /// <summary>Adds a defended ability target and records successful sabotage exactly once; doused shipment fires stay safe.</summary>
    private AbilityGate Target(string name, GateKind kind, float x, string id, bool fullHeight = true)
    {
        float floor = RouteFloor(x);
        var gate = AddGate(name, kind, new Vector2(x, floor), new Vector2(90, fullHeight ? FullHeightFrom(floor) : 140));
        gate.Acceptance = () => !GameplaySuspended && Progress.CanComplete(id);
        var marker = new Label { Name = "ObjectiveStatus", Position = new Vector2(-240, -285), Size = new Vector2(480, 130),
            HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color("#77efff"), MouseFilter = Control.MouseFilterEnum.Ignore };
        marker.AddThemeFontSizeOverride("font_size", 18);
        gate.AddChild(marker);
        _targets.Add((gate, id, marker));
        NeonRustVisual.AddSprite(gate, id.StartsWith("coupler:") ? "coupler" : kind == GateKind.SonicLock ? "sonic" : "machinery",
            new Vector2(150, 180), new Vector2(0, -90));
        gate.GateCleared += () =>
        {
            if (!Progress.Complete(id)) return;
            if (kind == GateKind.FireVent) gate.SecureForEvacuation();
            ShowDialogue(WildfireText.Get("NR_DONE_" + id.Replace(':', '_').ToUpperInvariant(), "AXIOM-7: Target secured. Proceed to the next objective."), 5);
        };
        return gate;
    }

    /// <summary>Orders three shipment abilities, Aura's safe lesson, both uplinks, couplers and the protected relay.</summary>
    protected override void BuildObjectives()
    {
        Target("CollapsedWall", GateKind.RubbleWall, 1500, "barrier:insertion");
        Target("FreightShipment", GateKind.RubbleWall, 11400, "shipment:freight");
        Target("RunoffShipment", GateKind.FireVent, 17300, "shipment:runoff", false);
        // Fire cannot physically block traversal, so the shipment's interlock opens only after the douse succeeds.
        AddSeal("RunoffInterlock", 17600, "shipment:runoff");
        Target("AuraPracticeLock", GateKind.SonicLock, 19500, "lesson:aura");
        Target("HarmonicShipment", GateKind.SonicLock, 27200, "shipment:harmonic");
        Target("WestUplink", GateKind.SonicLock, 27600, "uplink:west");
        Target("EastUplink", GateKind.SonicLock, 34550, "uplink:east");
        Target("WestCoupler", GateKind.SonicLock, 36900, "coupler:west");
        Target("EastCoupler", GateKind.SonicLock, 37900, "coupler:east");
        var relay = Target("CommRelay", GateKind.SonicLock, 39700, "relay:central");
        relay.GateCleared += () => ShowDialogue("AURA PHOENICIA: Listen. That is what a free city sounds like. Comm grid down.", 8);
        AddSeal("PracticeInterlock", 40800, "lesson:mirage");
        _exit = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(47700, 600) };
        Actors.AddChild(_exit);
    }

    /// <summary>Visible safe waiting/landing ground surrounds each isolated beam; practice damage is zero.</summary>
    protected override void BuildHazards()
    {
        foreach (float x in new[] { 13700f, 15900 })
        {
            var fire = AddGate($"CoolingFire{x}", GateKind.FireVent, new Vector2(x, 600), new Vector2(160, 130));
            fire.Acceptance = () => !GameplaySuspended;
        }
        foreach (float x in new[] { 29900f, 31400, 32900 })
        {
            var beam = AddLaserField(new Vector2(x, RouteFloor(x) - 240), 240, 25);
            beam.AlwaysActive = false;
            beam.ActiveDuration = 2;
            beam.InactiveDuration = 2.2f;
        }
        var practice = AddLaserField(new Vector2(40500, -1300), 800, 0);
        practice.Name = "MiragePracticeBeam";
        foreach (float x in new[] { 43800f, 45000, 46600 })
            AddLaserField(new Vector2(x, -1300), 1900);
        foreach (Node child in Actors.GetChildren())
            if (child is CyberLaserGrid2D beam)
                NeonRustVisual.AddSprite(beam, "laser", new Vector2(110, 240), new Vector2(0, beam.BeamLength - 100));
    }

    /// <summary>Places a full-height route seal that disappears once its associated combat/objective event is complete.</summary>
    private void AddSeal(string name, float x, string id)
    {
        var barrier = AddPlatform(x, -1300, 24, FullHeightFrom(RouteFloor(x)), PalaceSurface.Gate, false, name);
        _seals.Add((barrier, id));
    }

    /// <summary>Seats grounded defenders on the actual cover/stair surface at their own X rather than the group's origin.</summary>
    private (Vector2 Position, bool Gunner) Member(float x, bool gunner)
    {
        float floor = RouteFloor(x);
        if (gunner)
            foreach (Node child in World.GetChildren())
                if (child is PalacePlatform platform && platform.Surface != PalaceSurface.Gate &&
                    platform.Surface != PalaceSurface.Wall && x >= platform.Position.X && x <= platform.Position.X + platform.Size.X)
                    floor = Mathf.Min(floor, platform.Position.Y);
        return (new Vector2(x, floor - (gunner ? 0 : 145)), gunner);
    }

    /// <summary>Thirty finite formations; each has three or four normal-health actors with at most six awake globally.</summary>
    protected override void SpawnEnemies()
    {
        float[][] centers = {
            new[] { 800f, 2800, 4400 }, new[] { 6600f, 8000, 9400, 10600 },
            new[] { 12400f, 14300, 16200 }, new[] { 20100f, 20900 },
            new[] { 22500f, 23900, 25300, 26500 }, new[] { 28400f, 29800, 31300, 32800, 33800 },
            new[] { 35400f, 36400, 37400, 38200, 39000 }, new[] { 43200f, 44400, 45800, 47200 }
        };
        for (int sector = 0; sector < centers.Length; sector++)
        for (int group = 0; group < centers[sector].Length; group++)
        {
            int s = sector + 1, g = group + 1;
            string id = $"s{s}-{g}";
            float x = centers[sector][group], floor = RouteFloor(x);
            var encounter = new WildfireEncounter { Name = "Encounter_" + id, Id = id, Mission = this,
                Activation = new Rect2(x - 430, floor - 650, 900, 850), ManualActivation = s == 7 && g >= 4,
                Members = g % 2 == 0 ? new[] { Member(x + 120, true), Member(x + 300, false),
                    Member(x - 80, false), Member(x + 390, true) } : new[] { Member(x + 100, true),
                    Member(x + 270, false), Member(x - 70, false) } };
            if (s == 7 && g == 4) encounter.Acceptance = () => Progress.Has("coupler:east");
            if (s == 7 && g == 5) encounter.Acceptance = () => Progress.Has("encounter:s7-4");
            _encounters.Add(encounter);
            Actors.AddChild(encounter);
            var role = new Label { Name = "FormationRole", Position = new Vector2(x - 210, floor - 360), Size = new Vector2(420, 75),
                HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = new Color("#ffcf8a"), MouseFilter = Control.MouseFilterEnum.Ignore,
                Text = id + " / " + (NeonRustProgress.RequiredEncounters.Contains("encounter:" + id)
                    ? WildfireText.Get("NR_REQUIRED_DEFENSE", "OBJECTIVE DEFENDERS")
                    : WildfireText.Get("NR_OPTIONAL_PATROL", "OPTIONAL PATROL / SCORE BONUS")) };
            role.AddThemeFontSizeOverride("font_size", 18);
            World.AddChild(role);
            if (NeonRustProgress.RequiredEncounters.Contains("encounter:" + id))
                AddSeal("CombatSeal_" + id, x + 470, "encounter:" + id);
        }
    }

    /// <summary>Retains established recruitment lines; success requires the actual practice tasks, not a spare ability press.</summary>
    protected override void OnLevelReady()
    {
        _aura = new RecruitBeat { Name = "PhoeniciaJoins", Level = this, Operative = "Aura Phoenicia", Position = new Vector2(18700, 600),
            RecruitmentCondition = () => !GameplaySuspended && Progress.Has("shipment:runoff"),
            LessonCondition = () => Progress.Has("lesson:aura"), Lines = new[] {
                "AURA PHOENICIA: Their denial-of-service hums in C-flat. Offended, honestly. Let me answer it.",
                "AXIOM-7: Aura Phoenicia has joined. Her Harmonic Pulse can open sonic locks, like the door at the tower." } };
        _mirage = new RecruitBeat { Name = "MirageJoins", Level = this, Operative = "Mirage Blade", Position = new Vector2(40150, -500),
            RecruitmentCondition = () => !GameplaySuspended && CommsBlackedOut,
            LessonCondition = () => Progress.Has("lesson:mirage"), Lines = new[] {
                "MIRAGE BLADE: Patrols are blind. The route to the Border Fortress is open. Moving before you finish applauding.",
                "AXIOM-7: Mirage Blade has joined. His Phase Blink slips through laser fields that would kill anyone else." } };
        Actors.AddChild(_aura);
        Actors.AddChild(_mirage);
        Player.AbilityUsed += OnAbilityUsed;
        _previousPosition = Player.Position;
        BeginArrivalPresentation(new NeonRustIntro { Name = "NeonRustIntro", Layer = 40 });
    }

    /// <summary>Accepts only a real forward blink from the near side to the far side of the non-damaging practice beam.</summary>
    private void OnAbilityUsed(string tag)
    {
        if (GameplaySuspended || tag != LevelCatalog.Phase || !_mirage.Triggered || Player.ActiveProfile.DisplayName != "Mirage Blade") return;
        if (_previousPosition.X < 40500 && _previousPosition.X >= 40320 && Player.Position.X > 40528 &&
            Player.Position.X - _previousPosition.X >= 96 && Player.IsPhased && Mathf.Abs(Player.Position.Y + 546) < 90)
            Progress.Complete("lesson:mirage");
    }

    /// <summary>Tracks recruitment, releases completed route seals and starts exactly two sequential relay reinforcements.</summary>
    protected override void UpdateMission()
    {
        if (GameplaySuspended) return;
        foreach (var target in _targets)
        {
            target.Marker.Text = InputGlyphText.ToPlain(TargetGuidance(target.Event));
            target.Marker.Visible = !Progress.Has(target.Event);
        }
        if (_aura.Triggered) Progress.Complete("recruit:aura");
        if (_mirage.Triggered) Progress.Complete("recruit:mirage");
        // AbilityUsed subscribers run in registration order: evaluate mission success before the recruit lesson next tick.
        if (Progress.Has("lesson:mirage") && !_mirage.Completed) _mirage.CheckLessonSuccess();
        foreach (var seal in _seals)
            if (Progress.Has(seal.Event) && !seal.Barrier.IsOpen) seal.Barrier.Open();
        if (Progress.Has("coupler:east"))
            foreach (var encounter in _encounters.Where(e => e.ManualActivation &&
                Mathf.Abs(Player.Position.X - e.Activation.GetCenter().X) <= EncounterRetirementDistance)) encounter.Activate();
        _previousPosition = Player.Position;
        if (ExtractionReady && Player.GlobalPosition.DistanceTo(_exit.GlobalPosition + new Vector2(0, -80)) < 130) CompleteMission();
    }
    /// <inheritdoc/>
    protected override string ComputeObjective()
    {
        var objective = Progress.Objective;
        string target = objective.Key switch
        {
            "BREACH" => "barrier:insertion", "FREIGHT" => "shipment:freight", "RUNOFF" => "shipment:runoff",
            "PRACTICE" => "lesson:aura", "CARGO" => "shipment:harmonic", "WEST" => "uplink:west",
            "EAST" => "uplink:east", "COUPLER_W" => "coupler:west", "COUPLER_E" => "coupler:east",
            "REINFORCE" or "RELAY" => "relay:central", _ => ""
        };
        string instruction = target.Length > 0 ? TargetGuidance(target)
            : WildfireText.Get("NR_" + objective.Key + "_LOCAL", objective.Text);
        if (objective.Key is "PHASE" or "ESCAPE") instruction += " / " + ContextGlyph("karate");
        return WildfireText.Interpolate("UI_NEONRUSTMISSION_COMPUTEOBJECTIVE_DD0F3113", $"{Progress.Shipments}/3 CARGO  {Progress.Uplinks}/2 UPLINKS\n{instruction}");
    }

    /// <summary>Names the target, required operative and only its unresolved local defenders; no sector-wide clearance hint survives.</summary>
    private string TargetGuidance(string id)
    {
        string operative = id == "barrier:insertion" || id == "shipment:freight" ? "Apex Titan / Slam"
            : id == "shipment:runoff" ? "Oceanus / Tidal Torrent" : "Aura Phoenicia / Harmonic Pulse";
        string blockers = string.Join(", ", NeonRustProgress.Defenders(id).Where(d => !Progress.Has(d))
            .Select(d => d[10..] + ": " + (_encounters.FirstOrDefault(e => "encounter:" + e.Id == d)?.RemainingMembers ?? 0)));
        return WildfireText.Get("NR_TARGET_" + id.Replace(':', '_').ToUpperInvariant(), id.Replace(':', ' ').ToUpperInvariant()) + "\n" +
            (blockers.Length > 0 ? WildfireText.Get("NR_LOCAL_DEFENDERS", "LOCAL DEFENDERS / {groups}").Replace("{groups}", blockers) + "\n" : "") +
            WildfireText.Get("NR_USE_" + (id == "shipment:runoff" ? "DOUSE" : id == "barrier:insertion" || id == "shipment:freight" ? "SLAM" : "RESONATE"), operative) +
            " / " + ContextGlyph("karate");
    }
    /// <inheritdoc/>
    public bool HasCombatEvent(string id) => Progress.Has(id);
    /// <inheritdoc/>
    public void CompleteCombatEvent(string id) => Progress.Complete(id);
    /// <inheritdoc/>
    public bool CanActivateEncounter(int members)
    {
        // Retire before counting so scene-tree processing order cannot leave remote patrols holding the budget.
        foreach (var encounter in _encounters) encounter.RetireIfRemote();
        return _encounters.Sum(e => e.LivingMembers) + members <= 6;
    }
    /// <inheritdoc/>
    public void RewardMember(string id) { if (Progress.Reward(id)) Score.AddPoint(); }
    /// <inheritdoc/>
    public void LaunchRound(Vector2 origin, Vector2 heading, WildfireGunner source)
    {
        if (GameplaySuspended) return;
        var round = new WildfireRound { Heading = heading, Source = source };
        Actors.AddChild(round);
        round.GlobalPosition = origin;
    }
    /// <summary>Disables every actor, including explicit process modes, and clears transient rounds before teleporting.</summary>
    protected override void OnRecoveryStarting()
    {
        _recoveringLocal = true;
        foreach (Node node in GetTree().GetNodesInGroup("wildfire_rounds"))
            if (IsAncestorOf(node)) { node.ProcessMode = ProcessModeEnum.Disabled; node.QueueFree(); }
        foreach (var encounter in _encounters) encounter.ResetIncomplete();
        foreach (Node node in Actors.FindChildren("*", "", true, false))
        {
            if (node.IsQueuedForDeletion()) continue;
            _recoveryModes.Add((node, node.ProcessMode));
            node.ProcessMode = ProcessModeEnum.Disabled;
        }
    }
    /// <summary>Restores exact actor modes; completed targets and reward slots remain in the same ledger.</summary>
    protected override void OnRecoveryCompleted()
    {
        foreach (var state in _recoveryModes)
            if (IsInstanceValid(state.Node) && !state.Node.IsQueuedForDeletion()) state.Node.ProcessMode = state.Mode;
        _recoveryModes.Clear();
        _previousPosition = Player.Position;
        _recoveringLocal = false;
    }
    /// <summary>Each checkpoint follows an objective milestone; bypassed patrols never prevent synchronization.</summary>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended &&
        Progress.Has(shrine.Name.ToString() switch
        {
            "ShrineFreight" => "barrier:insertion", "ShrineRunoff" => "shipment:freight",
            "ShrineHarmonic" => "shipment:runoff", "ShrineWest" => "lesson:aura",
            "ShrineTower" => "uplink:west", "ShrineSummit" => "uplink:east",
            "ShrineEscape" => "lesson:mirage", _ => ""
        });
    /// <summary>Rust/data particles preserve the existing arrival palette without adding generated audio.</summary>
    protected override void BuildPetals()
    {
        Camera.AddChild(new CpuParticles2D { Name = "Dust", Position = new Vector2(0, -620), Amount = 60, Lifetime = 9,
            Preprocess = 9, EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(1300, 10),
            Gravity = new Vector2(60, 30), InitialVelocityMin = 10, InitialVelocityMax = 30, Direction = Vector2.One,
            Spread = 40, Color = new Color(.55f, .95f, 1, .55f), ScaleAmountMin = 1.2f, ScaleAmountMax = 2.6f, ZIndex = 20 });
    }
}
