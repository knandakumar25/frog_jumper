using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 7: The Foundry Forge (Act II Finale).
/// A climactic 6-sector sub-surface industrial magma rift mission featuring convective lava rivers,
/// superheated steam vents, high-voltage induction laser curtains, the multi-phase IGNIS-COLOSSUS boss,
/// and full 6-operative synergy. Overloading the core collapses the Citadel Spire's planetary sky-shields.
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class FoundryForgeMission : LaterCodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression, gates, boss phases, and objectives.</summary>
    public FoundryProgress Progress { get; } = new();

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
    private PalaceObjective _coreTerminal = null!;
    private PalaceObjective _exitTrigger = null!;
    private CyberDialogueBubble _dialogue = null!;
    private AudioStreamPlayer _acceptedSound = null!;
    private IgnisColossusBoss2D? _boss;
    private readonly HashSet<string> _milestoneRewards = new();
    private bool _recovering;
    private bool _bossSpawned;

    /// <inheritdoc/>
    protected override int MissionNumber => 7;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1200, 26500, 2200);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.55f, 0.22f, 0.08f);

    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/cutscenes/foundry_forge_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) =>
        WildfireText.Get("CYBER_FORGE_TITLE", "THE FOUNDRY FORGE");

    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) =>
        WildfireText.Get("CYBER_FORGE_BRIEFING", "Sever cryo-coolant valves. Breach magnetic induction conduits. Overload the geothermal core and shatter the Citadel's sky-shields.");

    /// <inheritdoc/>
    protected override string IntroLine() =>
        WildfireText.Get("CYBER_FORGE_INTRO", "AXIOM-7: Sub-surface industrial rift reached. Ambient temperature 4,000 K. Sever primary coolant loops and breach the core.");

    /// <inheritdoc/>
    protected override string CompletionLine => "THE FOUNDRY FORGE OVERLOADED // PLANETARY SKY-SHIELDS SHATTERED";

    /// <summary>Subterranean checkpoints gating the 6 sectors.</summary>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("RiftIngress", new Vector2(500, 600)),
        ("CoolantTurbines", new Vector2(5500, 600)),
        ("MagneticGantry", new Vector2(11000, 600)),
        ("CoreVaultOverlook", new Vector2(16500, 600)),
        ("IgnisColossusArena", new Vector2(20500, 600)),
        ("EvacuationGantry", new Vector2(24500, 600))
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
        string text = WildfireText.Get("CYBER_FORGE_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // GEOTHERMAL TELEMETRY: Plasma core channeling 14,000 gigawatts into the Citadel Spire's orbital emitter array.",
            2 => "DATA SLATE 02 // CONTAINMENT FAULT: Cryo-coolant pressure dropping. High-voltage magnetic conduits cannot withstand thermal runaway.",
            _ => "DATA SLATE 03 // EMERGENCY PROTOCOL: In event of core breach, planetary sky-shields will collapse across all municipal sectors."
        });
        _dialogue?.SetDialogueLine(text, "AXIOM-7 // TRANSMISSION ONLINE");
        return text;
    }

    /// <summary>Builds volcanic chasm geometry, catwalks, wall-jump columns, and ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and foundation
        AddPlatform(-100, -1200, 100, 2200, PalaceSurface.Wall);
        AddPlatform(0, 600, 26500, 900, PalaceSurface.Concrete, false, "ForgeFoundation");
        AddPlatform(26500, -1200, 100, 2200, PalaceSurface.Wall);

        // Wall-jump columns for vertical mobility across basalt chimneys
        foreach (float wx in new[] { 1600f, 1780f, 6200f, 8400f, 12200f, 14800f, 19400f, 23800f })
        {
            // Keep 160 px beneath decorative supports so ground defenders remain reachable.
            AddPlatform(wx, 260, 24, 180, PalaceSurface.Wall, false);
        }

        // Sector 1: Basalt Ingress catwalks
        AddPlatform(800, 440, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(1900, 410, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(2600, 440, 360, 22, PalaceSurface.Steel, true);
        AddPlatform(3400, 440, 450, 22, PalaceSurface.Steel, true);

        // Sector 2: Cryo-Coolant Turbines catwalks
        AddPlatform(5800, 430, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(7000, 410, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(8200, 430, 480, 22, PalaceSurface.Steel, true);

        // Sector 3: Magnetic Induction Conduits
        AddPlatform(11300, 430, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(12600, 360, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(14000, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 4: Primary Containment Overlook
        AddPlatform(16800, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(17900, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(19000, 440, 440, 22, PalaceSurface.Steel, true);

        // Sector 5: IGNIS-COLOSSUS Arena Catwalks
        AddPlatform(20700, 420, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(21400, 340, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(22300, 420, 460, 22, PalaceSurface.Steel, true);

        // Sector 6: Meltdown Extraction Gantry
        AddPlatform(24600, 440, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(25400, 360, 520, 22, PalaceSurface.Steel, true);

        // Ability-Gated Obstacles
        // Sector 2: Superheated steam jet requiring Oceanus Douse
        AddObjectiveGate("CoolantVentGate", GateKind.FireVent, new Vector2(4500, 600), "coolant_douse");

        // Sector 2 exit: Heavy magnetic pressure valve requiring Apex Titan Slam
        AddObjectiveGate("PressureValveBulkhead", GateKind.RubbleWall, new Vector2(8500, 600), "valve_shatter");

        // Sector 3: High-frequency ultrasonic barrier requiring Aura Resonate
        AddObjectiveGate("SonicLockGate", GateKind.SonicLock, new Vector2(14500, 600), "resonance_disable");

        // Sector 4: Extreme radiation vent requiring Surya Prime Bulwark
        AddObjectiveGate("RadiationVentGate", GateKind.HeatVent, new Vector2(17800, 600), "radiation_bulwark");
    }

    /// <summary>Builds interactive coolant vents, laser security grids, and extraction console.</summary>
    protected override void BuildObjectives()
    {
        // Environmental Coolant Vents in Sector 2
        var vent1 = new CoolantVent
        {
            Name = "Vent1",
            Position = new Vector2(6400, 600),
            StartOffset = 0.0f
        };
        Actors.AddChild(vent1);
        _vents.Add(vent1);

        var vent2 = new CoolantVent
        {
            Name = "Vent2",
            Position = new Vector2(7800, 600),
            StartOffset = 1.2f
        };
        Actors.AddChild(vent2);
        _vents.Add(vent2);

        // High-voltage laser grid in Sector 3 (bypassed with Mirage Phase Shift)
        var laser = new CyberLaserGrid2D
        {
            Name = "InductionLaserGrid",
            Position = new Vector2(13200, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 2.8f,
            InactiveDuration = 1.4f,
            Damage = 28f
        };
        TrackPhaseBoundary(laser, "arc_phase");
        Actors.AddChild(laser);
        _lasers.Add(laser);

        // Sector 6: Core Overload Override Terminal
        _coreTerminal = new PalaceObjective
        {
            Name = "CoreTerminal",
            Kind = "Memory",
            Position = new Vector2(24800, 600)
        };
        Actors.AddChild(_coreTerminal);

        // Final extraction gantry trigger
        _exitTrigger = new PalaceObjective
        {
            Name = "ExtractionGantry",
            Kind = "Exit",
            Position = new Vector2(25900, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Atmospheric Signage
        string[] sectors = {
            "SEC.01 // BASALT RIFT INGRESS",
            "SEC.02 // CRYO-COOLANT TURBINE LOOPS",
            "SEC.03 // MAGNETIC INDUCTION CONDUITS",
            "SEC.04 // PRIMARY CONTAINMENT OVERLOOK",
            "SEC.05 // CORE GUARDIAN // IGNIS-COLOSSUS",
            "SEC.06 // CRITICAL OVERLOAD EXTRACTION"
        };
        float[] sectorXs = { 400f, 4800f, 10500f, 15500f, 20200f, 24200f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label
            {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 260),
                Modulate = new Color(1f, 0.5f, 0.1f, 0.75f)
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

    /// <summary>Populates the 15 finite authored combat formations across all 6 sectors.</summary>
    protected override void SpawnEnemies()
    {
        // Sector 1: Basalt Ingress patrols
        AddEncounter("ingress1", new Rect2(1200, 200, 1200, 500), new[] { (new Vector2(1700, 554), true), (new Vector2(2000, 554), false) });
        AddEncounter("ingress2", new Rect2(2800, 200, 1200, 500), new[] { (new Vector2(3200, 554), true), (new Vector2(3500, 554), true) });

        // Sector 2: Turbine room defense
        AddEncounter("coolant1", new Rect2(5200, 200, 1000, 500), new[] { (new Vector2(5700, 554), false), (new Vector2(6000, 554), true) });
        AddEncounter("coolant2", new Rect2(6800, 200, 1000, 500), new[] { (new Vector2(7200, 554), true), (new Vector2(7500, 554), false) });
        AddEncounter("coolant3", new Rect2(8800, 200, 1000, 500), new[] { (new Vector2(9300, 554), true), (new Vector2(9600, 554), true) });

        // Sector 3: Magnetic conduit guards
        AddEncounter("induction1", new Rect2(11500, 200, 1000, 500), new[] { (new Vector2(12000, 554), true), (new Vector2(12300, 554), false) });
        AddEncounter("induction2", new Rect2(13000, 200, 1000, 500), new[] { (new Vector2(13500, 554), true), (new Vector2(13800, 554), true) });
        AddEncounter("induction3", new Rect2(14800, 200, 1000, 500), new[] { (new Vector2(15200, 554), false), (new Vector2(15500, 554), true) });

        // Sector 4: Containment overlook garrison
        AddEncounter("containment1", new Rect2(16900, 200, 1000, 500), new[] { (new Vector2(17300, 554), true), (new Vector2(17600, 554), false) });
        AddEncounter("containment2", new Rect2(18200, 200, 1000, 500), new[] { (new Vector2(18600, 554), true), (new Vector2(18900, 554), true) });
        AddEncounter("containment3", new Rect2(19500, 200, 1000, 500), new[] { (new Vector2(19900, 554), true), (new Vector2(20200, 554), false) });

        // Sector 5: Boss support waves
        AddEncounter("boss1", new Rect2(20800, 200, 1000, 500), new[] { (new Vector2(21200, 554), true), (new Vector2(21500, 554), false) });
        AddEncounter("boss2", new Rect2(21800, 200, 1000, 500), new[] { (new Vector2(22100, 554), true), (new Vector2(22400, 554), true) });
        AddEncounter("boss3", new Rect2(22800, 200, 1000, 500), new[] { (new Vector2(23100, 554), true), (new Vector2(23400, 554), false) });
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
            ManualActivation = new[] { "boss1", "boss2", "boss3" }.Contains(id),
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

            if (id == "boss_phase:3")
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
        if (!Progress.Has("ingress_breach") && Progress.CanComplete("ingress_breach"))
            CommitObjective("ingress_breach");

        if (!Progress.Has("coolant_douse") && Progress.CanComplete("coolant_douse") && HasActionEvidence("coolant_douse"))
            CommitObjective("coolant_douse");

        if (!Progress.Has("valve_shatter") && Progress.CanComplete("valve_shatter") && HasActionEvidence("valve_shatter"))
            CommitObjective("valve_shatter");

        if (!Progress.Has("turbines_overloaded") && Progress.CanComplete("turbines_overloaded"))
            CommitObjective("turbines_overloaded");

        if (!Progress.Has("arc_phase") && Progress.CanComplete("arc_phase") && HasActionEvidence("arc_phase"))
            CommitObjective("arc_phase");

        if (!Progress.Has("resonance_disable") && Progress.CanComplete("resonance_disable") && HasActionEvidence("resonance_disable"))
            CommitObjective("resonance_disable");

        if (!Progress.Has("conduits_severed") && Progress.CanComplete("conduits_severed"))
            CommitObjective("conduits_severed");

        if (!Progress.Has("radiation_bulwark") && Progress.CanComplete("radiation_bulwark") && HasActionEvidence("radiation_bulwark"))
            CommitObjective("radiation_bulwark");

        if (!Progress.Has("core_overlook_breach") && Progress.CanComplete("core_overlook_breach"))
            CommitObjective("core_overlook_breach");

        // Spawn Boss when entering Sector 5
        if (!_bossSpawned && !Progress.Has("boss_phase:3") && Player.GlobalPosition.X >= 20400f)
        {
            SpawnBoss();
        }

        // Interaction with the core terminal
        if (_coreTerminal != null && !_coreTerminal.Collected &&
            Progress.Has("boss_phase:3") &&
            Player.GlobalPosition.DistanceTo(_coreTerminal.GlobalPosition) < 130f)
        {
            _coreTerminal.SetCollected();
        }

        // Final extraction reached after boss defeat
        if (_exitTrigger != null && Progress.CanExtract && _coreTerminal.Collected &&
            Player.GlobalPosition.DistanceTo(_exitTrigger.GlobalPosition) < 140f)
        {
            CompleteMission();
        }
    }

    /// <summary>Spawns the IGNIS-COLOSSUS boss mech into the arena.</summary>
    private void SpawnBoss()
    {
        _bossSpawned = true;
        _boss = new IgnisColossusBoss2D
        {
            Name = "IgnisColossus",
            Position = new Vector2(21800, 510)
        };
        _boss.BossHealthChanged += (curr, max, phase) =>
        {
            if (phase == 2 && !Progress.Has("boss_phase:1") && Progress.CanComplete("boss_phase:1"))
                CommitObjective("boss_phase:1");
            else if (phase == 3 && !Progress.Has("boss_phase:2") && Progress.CanComplete("boss_phase:2"))
                CommitObjective("boss_phase:2");
        };
        _boss.BossDefeated += () =>
        {
            if (!Progress.Has("boss_phase:3") && Progress.CanComplete("boss_phase:3"))
                CommitObjective("boss_phase:3");
        };

        _boss.Target = Player;
        _boss.PhaseAcceptance = stage => Progress.CanComplete("boss_phase:" + stage);
        Actors.AddChild(_boss);
        GetNode<CyberCombatHud>("CyberCombatHud").BindCampaignBoss(_boss);
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "RiftIngress" => true,
        "CoolantTurbines" => Progress.Has("ingress_breach"),
        "MagneticGantry" => Progress.Has("turbines_overloaded"),
        "CoreVaultOverlook" => Progress.Has("conduits_severed"),
        "IgnisColossusArena" => Progress.Has("core_overlook_breach"),
        "EvacuationGantry" => Progress.Has("boss_phase:3"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "ingress_breach" => "Breach volcanic rift perimeter",
        "coolant_douse" => "Douse superheated steam jet with Oceanus",
        "valve_shatter" => "Shatter heavy pressure valve with Titan",
        "turbines_overloaded" => "Overload cryo-coolant turbine loops",
        "arc_phase" => "Phase through magnetic arc with Mirage",
        "resonance_disable" => "Bypass ultrasonic frequency lock with Aura",
        "conduits_severed" => "Sever high-voltage induction conduits",
        "radiation_bulwark" => "Cross radiation bridge under Surya Bulwark",
        "core_overlook_breach" => "Breach primary reactor containment overlook",
        "boss_phase:1" => "Defeat IGNIS-COLOSSUS mortar artillery",
        "boss_phase:2" => "Shatter IGNIS-COLOSSUS magnetic armor",
        "boss_phase:3" => "Destroy IGNIS-COLOSSUS core to collapse sky-shields",
        "extract" => "Sprint to evacuation gantry // ACT II FINALE",
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
        ResetArenaAttempt(ref _boss, ref _bossSpawned, "boss_phase", new[] { "boss1", "boss2", "boss3" });
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
