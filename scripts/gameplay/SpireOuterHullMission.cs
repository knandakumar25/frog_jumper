using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 9: Spire Outer Hull (High-Mobility Vertical Exterior Ascent).
/// Authored at 2,500m elevation featuring extreme gale-force wind shears, cryogenic exhaust manifold conduits,
/// wall-jump exterior structural pylon elevator shafts, perimeter flak laser grids, the multi-phase Spire Sentinel
/// mechanoid boss encounter ("Vanguard-9"), and extraction via the central Cyber-Nexus airlock.
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class SpireOuterHullMission : LaterCodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression, gates, boss phases, and objectives.</summary>
    public SpireProgress Progress { get; } = new();

    /// <inheritdoc/>
    protected override ILaterMissionProgress Ledger => Progress;

    /// <summary>Whether all prerequisite milestones have been reached to allow extraction.</summary>
    public bool CanExtract => Progress.CanExtract;

    /// <inheritdoc/>
    public CyberShogun CombatPlayer => Player;

    /// <inheritdoc/>
    public bool GameplaySuspended => _recovering || MissionComplete || !IsInsideTree() || GetTree().Paused;

    /// <inheritdoc/>
    public float EncounterRetirementDistance => 1600f;

    private readonly List<WildfireEncounter> _encounters = new();
    private readonly List<CoolantVent> _vents = new();
    private readonly List<CyberLaserGrid2D> _lasers = new();
    private readonly List<(Node Node, ProcessModeEnum Mode)> _recoveryModes = new();
    private PalaceObjective _nexusTerminal = null!;
    private PalaceObjective _exitTrigger = null!;
    private CyberDialogueBubble _dialogue = null!;
    private AudioStreamPlayer _acceptedSound = null!;
    private SpireSentinelBoss2D? _boss;
    private readonly HashSet<string> _milestoneRewards = new();
    private bool _recovering;
    private bool _bossSpawned;

    /// <inheritdoc/>
    protected override int MissionNumber => 9;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1600, 28000, 2400);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.2f, 0.4f, 0.55f);

    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/cutscenes/spire_outer_hull_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string CompletionLine => "SPIRE OUTER HULL BREACHED // CYBER-NEXUS AIRLOCK UNLOCKED";

    /// <inheritdoc/>
    protected override string IntroLine() => "SECTOR 09 // SPIRE OUTER HULL // ELEVATION 2,500M";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) => "SPIRE OUTER HULL";

    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) => "Ascend the exposed hull. Disable flak batteries and the drone relay. Defeat the Spire Sentinel and enter the Cyber-Nexus.";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("GantryCatwalks", new Vector2(400, 600)),
        ("CryoExhaustDepot", new Vector2(5000, 600)),
        ("FlakBatteryDeck", new Vector2(11000, 600)),
        ("CargoElevatorShaft", new Vector2(16500, 600)),
        ("SummitCatwalkArena", new Vector2(20500, 600)),
        ("CyberNexusAirlock", new Vector2(24500, 600))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[]
    {
        new Vector2(3600, 440),
        new Vector2(14200, 440),
        new Vector2(19200, 440)
    };

    /// <inheritdoc/>
    protected override string FragmentLog(int found)
    {
        string text = WildfireText.Get("SPIRE_OUTER_HULL_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // EXTERIOR MAINTENANCE LOG: Gantry 42 elevation 2,500m. Gale-force shears require magnetic boot solenoids.",
            2 => "DATA SLATE 02 // EXHAUST MANIFOLD OVERHEAT: Cryogenic louvers vent excess reactor heat directly through exterior armor seams.",
            _ => "DATA SLATE 03 // SPIRE SENTINEL PROTOCOL: Automated aerial defense mechanoid V-9 authorized to fire overdrive plasma salvos."
        });

        return text;
    }

    /// <summary>Constructs high-altitude gantries, wall-jump elevator shafts, and operative ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and foundation
        AddPlatform(-100, -1600, 100, 2600, PalaceSurface.Wall);
        AddPlatform(0, 600, 27500, 900, PalaceSurface.Concrete, false, "SpireFoundation");
        AddPlatform(27500, -1600, 100, 2600, PalaceSurface.Wall);

        // Wall-jump columns across exterior structural pylons and crane shafts
        foreach (float wx in new[] { 1700f, 1880f, 6300f, 8500f, 12300f, 14900f, 19500f, 23900f })
        {
            // Keep 160 px beneath decorative supports so ground defenders remain reachable.
            AddPlatform(wx, 240, 24, 200, PalaceSurface.Wall, false);
        }

        // Sector 1: Gantry 42 Exterior Insertion
        AddPlatform(800, 440, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(2000, 410, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(2700, 440, 380, 22, PalaceSurface.Steel, true);
        AddPlatform(3500, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 2: Cryogenic Heat Exhaust Conduits
        AddPlatform(5900, 430, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(7100, 410, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(8300, 430, 480, 22, PalaceSurface.Steel, true);

        // Sector 3: Flak Battery Mounting Deck
        AddPlatform(11400, 430, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(12700, 360, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(14100, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 4: Cargo Elevator & Vertical Wall Shafts
        AddPlatform(16900, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(18000, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(19100, 440, 450, 22, PalaceSurface.Steel, true);

        // Sector 5: Summit Catwalk Arena (Spire Sentinel Boss)
        AddPlatform(20800, 420, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(21500, 340, 500, 22, PalaceSurface.Steel, true);
        AddPlatform(22400, 420, 480, 22, PalaceSurface.Steel, true);

        // Sector 6: Cyber-Nexus Intake Airlock
        AddPlatform(24700, 440, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(25500, 360, 540, 22, PalaceSurface.Steel, true);

        // Ability-Gated Obstacles
        // Sector 2: Cryogenic coolant leak requiring Oceanus Douse
        AddObjectiveGate("CryoShaftGate", GateKind.FireVent, new Vector2(4600, 600), "coolant_shaft_douse");

        // Sector 2 exit: Heavy magnetic hull clamp brace requiring Apex Titan Slam
        AddObjectiveGate("TitanBraceBulkhead", GateKind.RubbleWall, new Vector2(8600, 600), "clamp_brace_shatter");

        // Sector 3 exit: Flak artillery barrage corridor requiring Surya Prime Bulwark
        AddObjectiveGate("FlakBulwarkGate", GateKind.HeatVent, new Vector2(14600, 600), "flak_barrier_deflect");

        // Sector 4: Drone relay acoustic sonic lock requiring Aura Phoenicia Resonate
        AddObjectiveGate("DroneRelaySonicGate", GateKind.SonicLock, new Vector2(18600, 600), "relay_resonance_disable");
    }

    /// <summary>Builds interactive cryogenic louvers, laser grids, and Cyber-Nexus airlock terminal.</summary>
    protected override void BuildObjectives()
    {
        // Environmental Cryogenic Vents in Sector 2
        var vent1 = new CoolantVent
        {
            Name = "CryoVent1",
            Position = new Vector2(6500, 600),
            StartOffset = 0.0f
        };
        Actors.AddChild(vent1);
        _vents.Add(vent1);

        var vent2 = new CoolantVent
        {
            Name = "CryoVent2",
            Position = new Vector2(7800, 600),
            StartOffset = 1.6f
        };
        Actors.AddChild(vent2);
        _vents.Add(vent2);

        // Environmental Laser Grid in Sector 3 (Mirage Blade Phase Shift bypass)
        var laserGrid = new CyberLaserGrid2D
        {
            Name = "FlakPerimeterLaser",
            Position = new Vector2(10400, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 3.0f,
            InactiveDuration = 1.5f,
            Damage = 35f
        };
        TrackPhaseBoundary(laserGrid, "laser_grid_phase");
        Actors.AddChild(laserGrid);
        _lasers.Add(laserGrid);

        // Cyber-Nexus Intake Airlock Terminal in Sector 6
        _nexusTerminal = new PalaceObjective
        {
            Name = "AirlockTerminal",
            Kind = "Memory",
            Position = new Vector2(25200, 600)
        };
        Actors.AddChild(_nexusTerminal);

        // Final extraction beacon at airlock threshold
        _exitTrigger = new PalaceObjective
        {
            Name = "AirlockBreachTrigger",
            Kind = "Exit",
            Position = new Vector2(26800, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Atmospheric Signage
        string[] sectors = {
            "SEC.01 // GANTRY 42 EXTERIOR INSERTION",
            "SEC.02 // CRYOGENIC HEAT EXHAUST CONDUITS",
            "SEC.03 // FLUID-COUPLING FLAK BATTERY DECK",
            "SEC.04 // CARGO ELEVATOR & STRUCTURAL PYLONS",
            "SEC.05 // SUMMIT CATWALK // SPIRE SENTINEL",
            "SEC.06 // CYBER-NEXUS INTAKE AIRLOCK"
        };
        float[] sectorXs = { 400f, 4800f, 10500f, 15500f, 20200f, 24200f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label
            {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 260),
                Modulate = new Color(0.2f, 0.8f, 1.0f, 0.8f)
            };
            secLabel.AddThemeFontSizeOverride("font_size", 14);
            World.AddChild(secLabel);
        }

        _dialogue = new CyberDialogueBubble { Name = "MissionDialogue" };
        AddChild(_dialogue);

        _acceptedSound = new AudioStreamPlayer { Name = "AcceptedSound", Bus = "SFX", VolumeDb = -4f };
        AddChild(_acceptedSound);
        if (ResourceLoader.Exists("res://assets/sound/parry.wav"))
        {
            _acceptedSound.Stream = GD.Load<AudioStream>("res://assets/sound/parry.wav");
        }
    }

    /// <summary>Populates the 14 finite authored combat formations across all 6 sectors.</summary>
    protected override void SpawnEnemies()
    {
        // Sector 1: Gantry Ingress encounters
        AddEncounter("gantry1", new Rect2(1200, 200, 1200, 500), new[] { (new Vector2(1600, 554), true), (new Vector2(1900, 554), false) });
        AddEncounter("gantry2", new Rect2(2800, 200, 1200, 500), new[] { (new Vector2(3100, 554), true), (new Vector2(3400, 554), true) });

        // Sector 2: Cryogenic Heat Exhaust encounters
        AddEncounter("exhaust1", new Rect2(5000, 200, 1000, 500), new[] { (new Vector2(5400, 554), false), (new Vector2(5800, 554), true) });
        AddEncounter("exhaust2", new Rect2(6800, 200, 1000, 500), new[] { (new Vector2(7200, 554), true), (new Vector2(7500, 554), false) });
        AddEncounter("exhaust3", new Rect2(8800, 200, 1000, 500), new[] { (new Vector2(9200, 554), true), (new Vector2(9500, 554), true) });

        // Sector 3: Flak Battery Deck encounters
        AddEncounter("flak1", new Rect2(11500, 200, 1000, 500), new[] { (new Vector2(11900, 554), true), (new Vector2(12200, 554), false) });
        AddEncounter("flak2", new Rect2(13000, 200, 1000, 500), new[] { (new Vector2(13400, 554), true), (new Vector2(13700, 554), true) });
        AddEncounter("flak3", new Rect2(14800, 200, 1000, 500), new[] { (new Vector2(15100, 554), false), (new Vector2(15400, 554), true) });

        // Sector 4: Cargo Elevator Shaft encounters
        AddEncounter("shaft1", new Rect2(16900, 200, 1000, 500), new[] { (new Vector2(17300, 554), true), (new Vector2(17600, 554), false) });
        AddEncounter("shaft2", new Rect2(18200, 200, 1000, 500), new[] { (new Vector2(18500, 554), true), (new Vector2(18800, 554), true) });
        AddEncounter("shaft3", new Rect2(19500, 200, 1000, 500), new[] { (new Vector2(19800, 554), true), (new Vector2(20100, 554), false) });

        // Sector 5: Summit Catwalk Arena boss support waves
        AddEncounter("summit1", new Rect2(20800, 200, 1000, 500), new[] { (new Vector2(21100, 554), true), (new Vector2(21400, 554), false) });
        AddEncounter("summit2", new Rect2(21800, 200, 1000, 500), new[] { (new Vector2(22100, 554), true), (new Vector2(22400, 554), true) });
        AddEncounter("summit3", new Rect2(22800, 200, 1000, 500), new[] { (new Vector2(23100, 554), true), (new Vector2(23400, 554), false) });
    }

    /// <summary>Creates a finite formation whose activation and completion obey ledger prerequisites.</summary>
    private void AddEncounter(string id, Rect2 activation, (Vector2 Position, bool Gunner)[] members)
    {
        var encounter = new WildfireEncounter
        {
            Name = "Encounter_" + id,
            Id = id,
            Activation = activation,
            Members = members,
            Mission = this,
            ManualActivation = new[] { "summit1", "summit2", "summit3" }.Contains(id),
            Acceptance = () => Progress.CanStartFormation(id)
        };
        Actors.AddChild(encounter);
        _encounters.Add(encounter);
    }

    /// <summary>Commits an objective to the mission ledger and provides audio/visual feedback.</summary>
    public bool CommitObjective(string id)
    {
        if (GameplaySuspended || !Progress.CanComplete(id)) return false;
        bool completed = Progress.Complete(id);
        if (completed)
        {
            if (_acceptedSound != null && IsInsideTree() && _acceptedSound.Stream != null)
            {
                _acceptedSound.Play();
            }
            if (_milestoneRewards.Add(id)) Score.AddPoint();
            CurrentObjective = ComputeObjective();

            if (id == "spire_sentinel_phase:3")
            {
                _exitTrigger.Visible = true;
            }
        }
        return completed;
    }

    /// <inheritdoc/>
    protected override void UpdateMission()
    {
        base.UpdateMission();
        if (GameplaySuspended || Player == null) return;

        // Accepted stages explicitly start their next finite wave anywhere within the defense arena.
        if (Player.GlobalPosition.X >= 20400f && Player.GlobalPosition.X <= 24100f)
            foreach (var encounter in _encounters.Where(e => e.ManualActivation)) encounter.Activate();

        // Auto-check milestone conditions
        if (!Progress.Has("gantry_ingress") && Progress.CanComplete("gantry_ingress"))
            CommitObjective("gantry_ingress");

        if (!Progress.Has("coolant_shaft_douse") && Progress.CanComplete("coolant_shaft_douse") && HasActionEvidence("coolant_shaft_douse"))
            CommitObjective("coolant_shaft_douse");

        if (!Progress.Has("clamp_brace_shatter") && Progress.CanComplete("clamp_brace_shatter") && HasActionEvidence("clamp_brace_shatter"))
            CommitObjective("clamp_brace_shatter");

        if (!Progress.Has("exhaust_conduits_cleared") && Progress.CanComplete("exhaust_conduits_cleared"))
            CommitObjective("exhaust_conduits_cleared");

        if (!Progress.Has("laser_grid_phase") && Progress.CanComplete("laser_grid_phase") && HasActionEvidence("laser_grid_phase"))
            CommitObjective("laser_grid_phase");

        if (!Progress.Has("flak_barrier_deflect") && Progress.CanComplete("flak_barrier_deflect") && HasActionEvidence("flak_barrier_deflect"))
            CommitObjective("flak_barrier_deflect");

        if (!Progress.Has("battery_deck_cleared") && Progress.CanComplete("battery_deck_cleared"))
            CommitObjective("battery_deck_cleared");

        if (!Progress.Has("relay_resonance_disable") && Progress.CanComplete("relay_resonance_disable") && HasActionEvidence("relay_resonance_disable"))
            CommitObjective("relay_resonance_disable");

        if (!Progress.Has("elevator_ascent_breach") && Progress.CanComplete("elevator_ascent_breach"))
            CommitObjective("elevator_ascent_breach");

        // Spawn Spire Sentinel Boss when entering Sector 5 summit arena
        if (!_bossSpawned && !Progress.Has("spire_sentinel_phase:3") && Player.GlobalPosition.X >= 20400f && Progress.Has("elevator_ascent_breach"))
        {
            SpawnSpireSentinelBoss();
        }

        // Check extraction completion
        if (!Progress.Has("extract") && CanExtract && _exitTrigger != null &&
            Player.GlobalPosition.DistanceTo(_exitTrigger.GlobalPosition) <= 120f)
        {
            CommitObjective("extract");
            CompleteMission();
        }
    }

    /// <summary>Creates the mission-owned arena boss, binds its player/HUD and accepts phase changes only after supporting defenders.</summary>
    private void SpawnSpireSentinelBoss()
    {
        _bossSpawned = true;
        _boss = new SpireSentinelBoss2D
        {
            Name = "SpireSentinelBoss",
            GlobalPosition = new Vector2(21800, 510)
        };
        _boss.BossHealthChanged += (curr, max, phase) =>
        {
            if (phase == 2 && !Progress.Has("spire_sentinel_phase:1") && Progress.CanComplete("spire_sentinel_phase:1"))
                CommitObjective("spire_sentinel_phase:1");
            else if (phase == 3 && !Progress.Has("spire_sentinel_phase:2") && Progress.CanComplete("spire_sentinel_phase:2"))
                CommitObjective("spire_sentinel_phase:2");
        };
        _boss.BossDefeated += () =>
        {
            if (!Progress.Has("spire_sentinel_phase:3") && Progress.CanComplete("spire_sentinel_phase:3"))
                CommitObjective("spire_sentinel_phase:3");
        };

        _boss.Target = Player;
        _boss.PhaseAcceptance = stage => Progress.CanComplete("spire_sentinel_phase:" + stage);
        Actors.AddChild(_boss);
        GetNode<CyberCombatHud>("CyberCombatHud").BindCampaignBoss(_boss);
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "GantryCatwalks" => true,
        "CryoExhaustDepot" => Progress.Has("gantry_ingress"),
        "FlakBatteryDeck" => Progress.Has("exhaust_conduits_cleared"),
        "CargoElevatorShaft" => Progress.Has("battery_deck_cleared"),
        "SummitCatwalkArena" => Progress.Has("elevator_ascent_breach"),
        "CyberNexusAirlock" => Progress.Has("spire_sentinel_phase:3"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "gantry_ingress" => "Infiltrate exterior gantry catwalks",
        "coolant_shaft_douse" => "Douse cryogenic coolant vent with Oceanus",
        "clamp_brace_shatter" => "Shatter heavy hull clamp brace with Titan Slam",
        "exhaust_conduits_cleared" => "Clear cryogenic exhaust conduit sector",
        "laser_grid_phase" => "Phase through flak laser grid with Mirage",
        "flak_barrier_deflect" => "Deflect flak barrage corridor under Surya Bulwark",
        "battery_deck_cleared" => "Clear flak battery mounting deck",
        "relay_resonance_disable" => "Bypass drone acoustic relay with Aura Resonate",
        "elevator_ascent_breach" => "Breach summit cargo elevator shaft",
        "spire_sentinel_phase:1" => "Engage Spire Sentinel aerial mechanoid",
        "spire_sentinel_phase:2" => "Survive Spire Sentinel overdrive plasma barrages",
        "spire_sentinel_phase:3" => "Destroy Spire Sentinel aerial mechanoid",
        "extract" => "Breach central Cyber-Nexus airlock // ENTER THE CITADEL CORE",
        _ => "Mission complete"
    };

    // ─── IFiniteCombatHost Implementation ───────────────────────────────────
    /// <inheritdoc/>
    public bool HasCombatEvent(string id) => Progress.Has(id);

    /// <inheritdoc/>
    public void CompleteCombatEvent(string id) => Progress.Complete(id);

    /// <summary>Returns explicit acceptance so rejected deaths cannot strand encounter slots.</summary>
    public bool TryCompleteCombatEvent(string id) => !GameplaySuspended && Progress.Complete(id);

    /// <inheritdoc/>
    public bool CanActivateEncounter(int members) =>
        !GameplaySuspended && _encounters.Sum(e => e.LivingMembers) + members <= 6;

    /// <inheritdoc/>
    public void RewardMember(string id)
    {
        if (Progress.Reward(id)) Score.AddPoint();
    }

    /// <inheritdoc/>
    public void LaunchRound(Vector2 origin, Vector2 heading, WildfireGunner source)
    {
        if (GameplaySuspended || heading.LengthSquared() <= 0) return;
        var round = new WildfireRound { Heading = heading, Source = source };
        Actors.AddChild(round);
        round.GlobalPosition = origin;
    }

    /// <inheritdoc/>
    protected override void OnRecoveryStarting()
    {
        _recovering = true;
        ClearTransientCombat();
        _acceptedSound?.Stop();
        ResetArenaAttempt(ref _boss, ref _bossSpawned, "spire_sentinel_phase", new[] { "summit1", "summit2", "summit3" });
        foreach (var e in _encounters) e.ResetIncomplete();
        foreach (Node n in Actors.FindChildren("*", "", true, false))
        {
            if (n.IsQueuedForDeletion()) continue;
            _recoveryModes.Add((n, n.ProcessMode));
            n.ProcessMode = ProcessModeEnum.Disabled;
        }
    }

    /// <inheritdoc/>
    protected override void OnRecoveryCompleted()
    {
        foreach (var state in _recoveryModes)
        {
            if (GodotObject.IsInstanceValid(state.Node) && !state.Node.IsQueuedForDeletion())
            {
                state.Node.ProcessMode = state.Mode;
            }
        }
        _recoveryModes.Clear();
        _recovering = false;
    }
}
