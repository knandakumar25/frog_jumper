using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 8: Stratos Docks (Aviation Hub - Act III Opening).
/// A high-altitude 6-sector aerial mission situated at 1,200m elevation featuring storm wind shears,
/// cryogenic hydrogen fuel lines, dual-phase radar scanner arrays, flak artillery corridors,
/// the multi-phase Aero-Centurion flight-deck mechanoid boss, and commandeering a heavy military VTOL gunship.
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class StratosDocksMission : LaterCodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression, gates, boss phases, and objectives.</summary>
    public StratosProgress Progress { get; } = new();

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
    private PalaceObjective _vtolTerminal = null!;
    private PalaceObjective _exitTrigger = null!;
    private CyberDialogueBubble _dialogue = null!;
    private AudioStreamPlayer _acceptedSound = null!;
    private AeroCenturionBoss2D? _boss;
    private readonly HashSet<string> _milestoneRewards = new();
    private bool _recovering;
    private bool _bossSpawned;

    /// <inheritdoc/>
    protected override int MissionNumber => 8;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1400, 26500, 2200);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.25f, 0.35f, 0.48f);

    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/cutscenes/stratos_docks_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) =>
        WildfireText.Get("STRATOS_DOCKS_TITLE", "STRATOS DOCKS // AVIATION HUB");

    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) =>
        WildfireText.Get("STRATOS_DOCKS_BRIEFING", "Infiltrate perimeter catwalks. Neutralize cryo-fuel conduits. Bypass radar scanner arrays. Defeat the Aero-Centurion and seize the Bay 4 heavy VTOL.");

    /// <inheritdoc/>
    protected override string IntroLine() =>
        WildfireText.Get("STRATOS_DOCKS_INTRO", "AXIOM-7: Elevation 1,200m. Storm wind shear at maximum. Infiltrate the perimeter and commandeer the Bay 4 VTOL.");

    /// <inheritdoc/>
    protected override string CompletionLine => "STRATOS DOCKS SECURED // VTOL GUNSHIP COMMANDEERED // ASCENDING TO THE CITADEL SPIRE";

    /// <summary>Subterranean checkpoints gating the 6 sectors.</summary>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("PerimeterCatwalks", new Vector2(500, 600)),
        ("CryoFuelDepot", new Vector2(5500, 600)),
        ("ScannerGridArray", new Vector2(11000, 600)),
        ("TrafficControlGantry", new Vector2(16500, 600)),
        ("FlightDeckBay4", new Vector2(20500, 600)),
        ("VTOLBoardingRamp", new Vector2(24500, 600))
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
        string text = WildfireText.Get("STRATOS_DOCKS_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // AVIATION MANIFEST: Citadel Air Corps operates at 1,200m. Patrols maintain tight containment corridors across municipal sectors.",
            2 => "DATA SLATE 02 // CRYO-HYDROGEN RESERVES: Heavy military gunship in Bay 4 spooled with supercharged hydrogen fuel cells.",
            _ => "DATA SLATE 03 // AIRSPACE INTERCEPT PROTOCOL: Aero-Centurion flight-deck mechanoid authorized to deploy overdrive micro-missiles against invaders."
        });
        _dialogue?.SetDialogueLine(text, "AXIOM-7 // TRANSMISSION ONLINE");
        return text;
    }

    /// <summary>Constructs high-altitude gantries, crane elevator shafts, and operative ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and foundation
        AddPlatform(-100, -1400, 100, 2400, PalaceSurface.Wall);
        AddPlatform(0, 600, 26500, 900, PalaceSurface.Concrete, false, "StratosFoundation");
        AddPlatform(26500, -1400, 100, 2400, PalaceSurface.Wall);

        // Wall-jump columns across mooring pylons and crane towers
        foreach (float wx in new[] { 1600f, 1780f, 6200f, 8400f, 12200f, 14800f, 19400f, 23800f })
        {
            // Keep 160 px beneath decorative supports so ground defenders remain reachable.
            AddPlatform(wx, 260, 24, 180, PalaceSurface.Wall, false);
        }

        // Sector 1: Perimeter Catwalks
        AddPlatform(800, 440, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(1900, 410, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(2600, 440, 360, 22, PalaceSurface.Steel, true);
        AddPlatform(3400, 440, 450, 22, PalaceSurface.Steel, true);

        // Sector 2: Cryogenic Fuel Depot catwalks
        AddPlatform(5800, 430, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(7000, 410, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(8200, 430, 480, 22, PalaceSurface.Steel, true);

        // Sector 3: Flight Deck Scanner Array catwalks
        AddPlatform(11300, 430, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(12600, 360, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(14000, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 4: Air Traffic Control Gantry
        AddPlatform(16800, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(17900, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(19000, 440, 440, 22, PalaceSurface.Steel, true);

        // Sector 5: Aero-Centurion Arena Catwalks
        AddPlatform(20700, 420, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(21400, 340, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(22300, 420, 460, 22, PalaceSurface.Steel, true);

        // Sector 6: Bay 4 VTOL Boarding Ramp
        AddPlatform(24600, 440, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(25400, 360, 520, 22, PalaceSurface.Steel, true);

        // Ability-Gated Obstacles
        // Sector 2: Cryogenic fuel leak requiring Oceanus Douse
        AddObjectiveGate("CryoFuelGate", GateKind.FireVent, new Vector2(4500, 600), "cryo_fuel_douse");

        // Sector 2 exit: Heavy magnetic titan-clamp mooring lock requiring Apex Titan Slam
        AddObjectiveGate("TitanClampBulkhead", GateKind.RubbleWall, new Vector2(8500, 600), "titan_clamp_shatter");

        // Sector 3 exit: High-frequency acoustic radar lock requiring Aura Phoenicia Resonate
        AddObjectiveGate("RadarSonicLockGate", GateKind.SonicLock, new Vector2(14500, 600), "radar_resonance_disable");

        // Sector 4: Flak battery artillery corridor requiring Surya Prime Bulwark
        AddObjectiveGate("FlakBulwarkGate", GateKind.HeatVent, new Vector2(17800, 600), "flak_bulwark_breach");
    }

    /// <summary>Builds interactive cryogenic fuel vents, scanner grids, and VTOL boarding console.</summary>
    protected override void BuildObjectives()
    {
        // Environmental Cryogenic Vents in Sector 2
        var vent1 = new CoolantVent
        {
            Name = "CryoVent1",
            Position = new Vector2(6400, 600),
            StartOffset = 0.0f
        };
        Actors.AddChild(vent1);
        _vents.Add(vent1);

        var vent2 = new CoolantVent
        {
            Name = "CryoVent2",
            Position = new Vector2(7800, 600),
            StartOffset = 1.2f
        };
        Actors.AddChild(vent2);
        _vents.Add(vent2);

        // High-voltage laser grid in Sector 3 (bypassed with Mirage Phase Shift)
        var laser = new CyberLaserGrid2D
        {
            Name = "FlightDeckLaserGrid",
            Position = new Vector2(13200, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 2.8f,
            InactiveDuration = 1.4f,
            Damage = 28f
        };
        TrackPhaseBoundary(laser, "scanner_grid_phase");
        Actors.AddChild(laser);
        _lasers.Add(laser);

        // Sector 6: Bay 4 VTOL Command Override Terminal
        _vtolTerminal = new PalaceObjective
        {
            Name = "VtolTerminal",
            Kind = "Memory",
            Position = new Vector2(24800, 600)
        };
        Actors.AddChild(_vtolTerminal);

        // Final extraction boarding ramp trigger
        _exitTrigger = new PalaceObjective
        {
            Name = "VtolBoardingRampTrigger",
            Kind = "Exit",
            Position = new Vector2(25900, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Atmospheric Signage
        string[] sectors = {
            "SEC.01 // PERIMETER GANTRY CATWALKS",
            "SEC.02 // CRYO-FUEL DEPOT & CONDUITS",
            "SEC.03 // FLIGHT DECK SCANNER ARRAYS",
            "SEC.04 // AIR TRAFFIC CONTROL GANTRY",
            "SEC.05 // FLIGHT DECK GUARDIAN // AERO-CENTURION",
            "SEC.06 // BAY 4 VTOL COMMAND INTERLOCK"
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
        // Sector 1: Perimeter catwalk patrols
        AddEncounter("catwalk1", new Rect2(1200, 200, 1200, 500), new[] { (new Vector2(1700, 554), true), (new Vector2(2000, 554), false) });
        AddEncounter("catwalk2", new Rect2(2800, 200, 1200, 500), new[] { (new Vector2(3200, 554), true), (new Vector2(3500, 554), true) });

        // Sector 2: Cryo-fuel depot security
        AddEncounter("fuel1", new Rect2(5200, 200, 1000, 500), new[] { (new Vector2(5700, 554), false), (new Vector2(6000, 554), true) });
        AddEncounter("fuel2", new Rect2(6800, 200, 1000, 500), new[] { (new Vector2(7200, 554), true), (new Vector2(7500, 554), false) });
        AddEncounter("fuel3", new Rect2(8800, 200, 1000, 500), new[] { (new Vector2(9300, 554), true), (new Vector2(9600, 554), true) });

        // Sector 3: Scanner array defense
        AddEncounter("scanner1", new Rect2(11500, 200, 1000, 500), new[] { (new Vector2(12000, 554), true), (new Vector2(12300, 554), false) });
        AddEncounter("scanner2", new Rect2(13000, 200, 1000, 500), new[] { (new Vector2(13500, 554), true), (new Vector2(13800, 554), true) });
        AddEncounter("scanner3", new Rect2(14800, 200, 1000, 500), new[] { (new Vector2(15200, 554), false), (new Vector2(15500, 554), true) });

        // Sector 4: Air traffic control garrison
        AddEncounter("gantry1", new Rect2(16900, 200, 1000, 500), new[] { (new Vector2(17300, 554), true), (new Vector2(17600, 554), false) });
        AddEncounter("gantry2", new Rect2(18200, 200, 1000, 500), new[] { (new Vector2(18600, 554), true), (new Vector2(18900, 554), true) });
        AddEncounter("gantry3", new Rect2(19500, 200, 1000, 500), new[] { (new Vector2(19900, 554), true), (new Vector2(20200, 554), false) });

        // Sector 5: Bay 4 Boss support waves
        AddEncounter("hangar1", new Rect2(20800, 200, 1000, 500), new[] { (new Vector2(21200, 554), true), (new Vector2(21500, 554), false) });
        AddEncounter("hangar2", new Rect2(21800, 200, 1000, 500), new[] { (new Vector2(22100, 554), true), (new Vector2(22400, 554), true) });
        AddEncounter("hangar3", new Rect2(22800, 200, 1000, 500), new[] { (new Vector2(23100, 554), true), (new Vector2(23400, 554), false) });
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
            ManualActivation = new[] { "hangar1", "hangar2", "hangar3" }.Contains(id),
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

            if (id == "aero_guardian_phase:3")
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
        if (!Progress.Has("perimeter_ingress") && Progress.CanComplete("perimeter_ingress"))
            CommitObjective("perimeter_ingress");

        if (!Progress.Has("cryo_fuel_douse") && Progress.CanComplete("cryo_fuel_douse") && HasActionEvidence("cryo_fuel_douse"))
            CommitObjective("cryo_fuel_douse");

        if (!Progress.Has("titan_clamp_shatter") && Progress.CanComplete("titan_clamp_shatter") && HasActionEvidence("titan_clamp_shatter"))
            CommitObjective("titan_clamp_shatter");

        if (!Progress.Has("fuel_conduits_secured") && Progress.CanComplete("fuel_conduits_secured"))
            CommitObjective("fuel_conduits_secured");

        if (!Progress.Has("scanner_grid_phase") && Progress.CanComplete("scanner_grid_phase") && HasActionEvidence("scanner_grid_phase"))
            CommitObjective("scanner_grid_phase");

        if (!Progress.Has("radar_resonance_disable") && Progress.CanComplete("radar_resonance_disable") && HasActionEvidence("radar_resonance_disable"))
            CommitObjective("radar_resonance_disable");

        if (!Progress.Has("scanner_array_cleared") && Progress.CanComplete("scanner_array_cleared"))
            CommitObjective("scanner_array_cleared");

        if (!Progress.Has("flak_bulwark_breach") && Progress.CanComplete("flak_bulwark_breach") && HasActionEvidence("flak_bulwark_breach"))
            CommitObjective("flak_bulwark_breach");

        if (!Progress.Has("hangar_overlook_breach") && Progress.CanComplete("hangar_overlook_breach"))
            CommitObjective("hangar_overlook_breach");

        // Spawn Boss when entering Sector 5
        if (!_bossSpawned && !Progress.Has("aero_guardian_phase:3") && Player.GlobalPosition.X >= 20400f)
        {
            SpawnBoss();
        }

        // Interaction with the VTOL command terminal
        if (_vtolTerminal != null && !_vtolTerminal.Collected &&
            Progress.Has("aero_guardian_phase:3") &&
            Player.GlobalPosition.DistanceTo(_vtolTerminal.GlobalPosition) < 130f)
        {
            _vtolTerminal.SetCollected();
        }

        // Final extraction reached after boss defeat
        if (_exitTrigger != null && Progress.CanExtract && _vtolTerminal.Collected &&
            Player.GlobalPosition.DistanceTo(_exitTrigger.GlobalPosition) < 140f)
        {
            CompleteMission();
        }
    }

    /// <summary>Spawns the Aero-Centurion boss mechanoid into the flight deck arena.</summary>
    private void SpawnBoss()
    {
        _bossSpawned = true;
        _boss = new AeroCenturionBoss2D
        {
            Name = "AeroCenturion",
            Position = new Vector2(21800, 510)
        };
        _boss.BossHealthChanged += (curr, max, phase) =>
        {
            if (phase == 2 && !Progress.Has("aero_guardian_phase:1") && Progress.CanComplete("aero_guardian_phase:1"))
                CommitObjective("aero_guardian_phase:1");
            else if (phase == 3 && !Progress.Has("aero_guardian_phase:2") && Progress.CanComplete("aero_guardian_phase:2"))
                CommitObjective("aero_guardian_phase:2");
        };
        _boss.BossDefeated += () =>
        {
            if (!Progress.Has("aero_guardian_phase:3") && Progress.CanComplete("aero_guardian_phase:3"))
                CommitObjective("aero_guardian_phase:3");
        };

        _boss.Target = Player;
        _boss.PhaseAcceptance = stage => Progress.CanComplete("aero_guardian_phase:" + stage);
        Actors.AddChild(_boss);
        GetNode<CyberCombatHud>("CyberCombatHud").BindCampaignBoss(_boss);
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "PerimeterCatwalks" => true,
        "CryoFuelDepot" => Progress.Has("perimeter_ingress"),
        "ScannerGridArray" => Progress.Has("fuel_conduits_secured"),
        "TrafficControlGantry" => Progress.Has("scanner_array_cleared"),
        "FlightDeckBay4" => Progress.Has("hangar_overlook_breach"),
        "VTOLBoardingRamp" => Progress.Has("aero_guardian_phase:3"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "perimeter_ingress" => "Infiltrate perimeter gantry catwalks",
        "cryo_fuel_douse" => "Douse cryogenic hydrogen fuel leak with Oceanus",
        "titan_clamp_shatter" => "Shatter magnetic titan-clamp with Titan Slam",
        "fuel_conduits_secured" => "Secure fuel gantry defense corridor",
        "scanner_grid_phase" => "Phase through radar scanner grid with Mirage",
        "radar_resonance_disable" => "Bypass acoustic radar lock with Aura Resonate",
        "scanner_array_cleared" => "Clear flight deck scanner defense array",
        "flak_bulwark_breach" => "Cross flak artillery corridor under Surya Bulwark",
        "hangar_overlook_breach" => "Breach Flight Deck Bay 4 outer defense perimeter",
        "aero_guardian_phase:1" => "Neutralize Aero-Centurion plasma repeaters",
        "aero_guardian_phase:2" => "Survive Aero-Centurion overdrive missile salvos",
        "aero_guardian_phase:3" => "Neutralize Aero-Centurion flight-deck mechanoid",
        "extract" => "Board Bay 4 heavy VTOL gunship // COMMENCE CITADEL ASSAULT",
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
        ResetArenaAttempt(ref _boss, ref _bossSpawned, "aero_guardian_phase", new[] { "hangar1", "hangar2", "hangar3" });
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
