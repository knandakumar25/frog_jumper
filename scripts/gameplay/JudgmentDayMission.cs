using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 12: Judgment Day (Citadel Summit &amp; Void Singularity Chamber).
/// The climactic final story mission of Axiom Force, set at 5,000 meters elevation inside the Grand Citadel Throne Room.
/// Axiom Force traverses high-voltage ingress catwalks over the cosmic chasm, douses power siphon coolant manifolds,
/// phases through defensive laser fields, scales vaulted cathedral elevator shafts, and confronts the supreme antagonist—
/// The Void Commander (750 HP)—in a multi-phase encounter before initiating Singularity Core containment to liberate the world.
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class JudgmentDayMission : LaterCodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression, gates, boss phases, and objectives.</summary>
    public JudgmentDayProgress Progress { get; } = new();

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
    private VoidCommanderBoss2D? _boss;
    private readonly HashSet<string> _milestoneRewards = new();
    private bool _recovering;
    private bool _bossSpawned;
    private bool _coreTerminalOverridden;

    /// <inheritdoc/>
    protected override int MissionNumber => 12;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1800, 28500, 2600);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.14f, 0.08f, 0.24f);

    /// <inheritdoc/>
    public override string MusicPath => "res://assets/cutscenes/judgment_day_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string CompletionLine =>
        "MISSION 12 COMPLETE // SINGULARITY CONTAINED // LIBERATION ACHIEVED";

    /// <inheritdoc/>
    protected override string IntroLine() =>
        "AXIOM-7: CITADEL SUMMIT REACHED // ENGAGE THE VOID COMMANDER // LIBERATE THE MACHINE EMPIRE";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) => "JUDGMENT DAY";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("IngressGantry", new Vector2(400, 600)),
        ("PowerSiphon", new Vector2(5500, 600)),
        ("VoidBarrier", new Vector2(10500, 600)),
        ("CathedralVault", new Vector2(16000, 600)),
        ("ThroneDais", new Vector2(21000, 600)),
        ("CoreOverload", new Vector2(26000, 600))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[]
    {
        new Vector2(3800, 440),
        new Vector2(13500, 440),
        new Vector2(19800, 440)
    };

    /// <inheritdoc/>
    protected override string FragmentLog(int found)
    {
        string text = WildfireText.Get("JUDGMENT_DAY_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // SINGULARITY GENESIS: The dark matter reactor was engineered to consume planetary energy reserves to sustain the imperial elite.",
            2 => "DATA SLATE 02 // IMPERIAL DECREE: All civilian identity registries slated for permanent eradication upon Singularity Core reaching 100% capacity.",
            _ => "DATA SLATE 03 // THE DAWN OF AXIOM: Six operatives stood together when millions fell. Their names will be remembered across eternity."
        });

        return text;
    }

    /// <summary>Constructs Citadel summit cathedral architecture, wall-jump shafts, and ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and foundation
        AddPlatform(-100, -1800, 100, 2800, PalaceSurface.Wall);
        AddPlatform(0, 600, 28500, 900, PalaceSurface.Concrete, false, "CitadelSummitFoundation");
        AddPlatform(28500, -1800, 100, 2800, PalaceSurface.Wall);

        // Wall-jump pylon columns across cathedral chambers
        foreach (float wx in new[] { 1600f, 1850f, 6500f, 8800f, 12500f, 15000f, 20000f, 24500f })
        {
            // Keep 160 px beneath decorative supports so ground defenders remain reachable.
            AddPlatform(wx, 240, 24, 200, PalaceSurface.Wall, false);
        }

        // Sector 1: Singularity Ingress Gantry
        AddPlatform(800, 440, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(1400, 360, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(2000, 440, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(2700, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(3400, 440, 420, 22, PalaceSurface.Steel, true);

        // Sector 2: Power Siphon Trench & Geothermal Shunts
        AddPlatform(4600, 430, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(5300, 360, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(6100, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(7000, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(8000, 430, 460, 22, PalaceSurface.Steel, true);

        // Sector 3: Void Barrier & Gravitational Corridor
        AddPlatform(11200, 430, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(12600, 360, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(14000, 440, 480, 22, PalaceSurface.Steel, true);

        // Sector 4: Cathedral Vaulted Ascent & Harmonic Locks
        AddPlatform(16800, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(17900, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(19000, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 5: Grand Throne Dais (Void Commander Boss)
        AddPlatform(20800, 420, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(21600, 340, 520, 22, PalaceSurface.Steel, true);
        AddPlatform(22600, 420, 500, 22, PalaceSurface.Steel, true);

        // Sector 6: Singularity Core Overload & Extraction Threshold
        AddPlatform(25000, 440, 500, 22, PalaceSurface.Steel, true);
        AddPlatform(26000, 360, 560, 22, PalaceSurface.Steel, true);

        // Ability-Gated Obstacles
        // Sector 2: Geothermal power siphon coolant manifold requiring Oceanus Hydro Torrent
        AddObjectiveGate("PowerSiphonCoolantGate", GateKind.FireVent, new Vector2(4800, 600), "siphon_coolant_doused");

        // Sector 2 exit: Reinforced dark-matter siphon clamp barricade requiring Apex Titan Slam
        AddObjectiveGate("SiphonMooringClampGate", GateKind.RubbleWall, new Vector2(8800, 600), "siphon_clamp_shattered");

        // Sector 3 exit: Concentrated cosmic radiation thermal emission vent requiring Surya Prime Bulwark
        AddObjectiveGate("CosmicRadiationVentGate", GateKind.HeatVent, new Vector2(14800, 600), "cosmic_vent_absorbed");

        // Sector 4: Harmonic acoustic security lock requiring Aura Phoenicia Resonate
        AddObjectiveGate("HarmonicAcousticLockGate", GateKind.SonicLock, new Vector2(18800, 600), "harmonic_lock_resonated");
    }

    /// <summary>Builds coolant vents, laser fields, core containment console, and extraction beacon.</summary>
    protected override void BuildObjectives()
    {
        // Environmental Coolant Vents in Sector 2
        var vent1 = new CoolantVent
        {
            Name = "SiphonCoolantVent1",
            Position = new Vector2(6600, 600),
            StartOffset = 0.0f
        };
        Actors.AddChild(vent1);
        _vents.Add(vent1);

        var vent2 = new CoolantVent
        {
            Name = "SiphonCoolantVent2",
            Position = new Vector2(7900, 600),
            StartOffset = 1.5f
        };
        Actors.AddChild(vent2);
        _vents.Add(vent2);

        // Environmental Laser Grid in Sector 3 (Mirage Blade Phase Dash bypass)
        var laserGrid = new CyberLaserGrid2D
        {
            Name = "VoidBarrierLaserGrid",
            Position = new Vector2(11800, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 3.0f,
            InactiveDuration = 1.5f,
            Damage = 35f
        };
        TrackPhaseBoundary(laserGrid, "laser_curtain_phased");
        Actors.AddChild(laserGrid);
        _lasers.Add(laserGrid);

        // Sector 6: Singularity Core Containment Console
        _coreTerminal = new PalaceObjective
        {
            Name = "CoreContainmentTerminal",
            Kind = "Memory",
            Position = new Vector2(27200, 600)
        };
        Actors.AddChild(_coreTerminal);

        // Final extraction beacon leading to Liberation Threshold
        _exitTrigger = new PalaceObjective
        {
            Name = "ExtractionGate",
            Kind = "Exit",
            Position = new Vector2(28000, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Atmospheric Signage
        string[] sectors = {
            "SEC.01 // SINGULARITY INGRESS GANTRY",
            "SEC.02 // POWER SIPHON TRENCH & DARK-MATTER SHUNTS",
            "SEC.03 // VOID BARRIER & GRAVITATIONAL CORRIDOR",
            "SEC.04 // CATHEDRAL VAULTED ASCENT & HARMONIC LOCKS",
            "SEC.05 // GRAND THRONE DAIS // THE VOID COMMANDER",
            "SEC.06 // SINGULARITY CORE OVERLOAD & LIBERATION"
        };
        float[] sectorXs = { 400f, 4900f, 10600f, 15600f, 20400f, 24600f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label
            {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 260),
                Modulate = new Color(0.8f, 0.3f, 0.95f, 0.85f)
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
        // Sector 1: Singularity Ingress Gantry encounters
        AddEncounter("ingress1", new Rect2(1200, 200, 1000, 500), new[] { (new Vector2(1400, 554), true), (new Vector2(1700, 554), false) });
        AddEncounter("ingress2", new Rect2(2400, 200, 1000, 500), new[] { (new Vector2(2600, 554), true), (new Vector2(2900, 554), true) });

        // Sector 2: Power Siphon Trench encounters
        AddEncounter("siphon1", new Rect2(4800, 200, 1000, 500), new[] { (new Vector2(5000, 554), false), (new Vector2(5300, 554), true) });
        AddEncounter("siphon2", new Rect2(6000, 200, 1000, 500), new[] { (new Vector2(6200, 554), true), (new Vector2(6500, 554), false) });
        AddEncounter("siphon3", new Rect2(7600, 200, 1000, 500), new[] { (new Vector2(7800, 554), true), (new Vector2(8100, 554), true) });

        // Sector 3: Void Barrier & Gravitational Corridor encounters
        AddEncounter("barrier1", new Rect2(10000, 200, 1000, 500), new[] { (new Vector2(10200, 554), true), (new Vector2(10500, 554), false) });
        AddEncounter("barrier2", new Rect2(11400, 200, 1000, 500), new[] { (new Vector2(11600, 554), true), (new Vector2(11900, 554), true) });
        // These defenders unlock after the heat crossing, so they occupy its safe exit rather than the earlier corridor.
        AddEncounter("barrier3", new Rect2(14950, 200, 850, 500), new[] { (new Vector2(15200, 554), false), (new Vector2(15500, 554), true) });

        // Sector 4: Cathedral Vaulted Ascent encounters
        AddEncounter("vault1", new Rect2(16000, 200, 1000, 500), new[] { (new Vector2(16200, 554), true), (new Vector2(16500, 554), false) });
        AddEncounter("vault2", new Rect2(18200, 200, 1000, 500), new[] { (new Vector2(18400, 554), true), (new Vector2(18700, 554), true) });

        // Sector 5: Grand Throne Dais boss support waves
        AddEncounter("boss1", new Rect2(21600, 200, 1000, 500), new[] { (new Vector2(21800, 554), true), (new Vector2(22100, 554), false) });
        AddEncounter("boss2", new Rect2(22600, 200, 1000, 500), new[] { (new Vector2(22800, 554), true), (new Vector2(23100, 554), true) });
        AddEncounter("boss3", new Rect2(23600, 200, 1000, 500), new[] { (new Vector2(23800, 554), true), (new Vector2(24100, 554), false) });

        // Sector 6: Singularity Core Remnant encounter
        AddEncounter("citadel_remnant1", new Rect2(26200, 200, 1000, 500), new[] { (new Vector2(26400, 554), true), (new Vector2(26700, 554), true) });
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

            if (id == "core_containment_initiated")
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
        if (Player.GlobalPosition.X >= 20400f && Player.GlobalPosition.X <= 24700f)
            foreach (var encounter in _encounters.Where(e => e.ManualActivation)) encounter.Activate();

        // Auto-check milestone conditions
        if (!Progress.Has("ingress_secured") && Progress.CanComplete("ingress_secured"))
            CommitObjective("ingress_secured");

        if (!Progress.Has("siphon_coolant_doused") && Progress.CanComplete("siphon_coolant_doused") && HasActionEvidence("siphon_coolant_doused"))
            CommitObjective("siphon_coolant_doused");

        if (!Progress.Has("siphon_clamp_shattered") && Progress.CanComplete("siphon_clamp_shattered") && HasActionEvidence("siphon_clamp_shattered"))
            CommitObjective("siphon_clamp_shattered");

        if (!Progress.Has("siphon_grid_neutralized") && Progress.CanComplete("siphon_grid_neutralized"))
            CommitObjective("siphon_grid_neutralized");

        if (!Progress.Has("laser_curtain_phased") && Progress.CanComplete("laser_curtain_phased") && HasActionEvidence("laser_curtain_phased"))
            CommitObjective("laser_curtain_phased");

        if (!Progress.Has("cosmic_vent_absorbed") && Progress.CanComplete("cosmic_vent_absorbed") && HasActionEvidence("cosmic_vent_absorbed"))
            CommitObjective("cosmic_vent_absorbed");

        if (!Progress.Has("barrier_corridor_cleared") && Progress.CanComplete("barrier_corridor_cleared"))
            CommitObjective("barrier_corridor_cleared");

        if (!Progress.Has("harmonic_lock_resonated") && Progress.CanComplete("harmonic_lock_resonated") && HasActionEvidence("harmonic_lock_resonated"))
            CommitObjective("harmonic_lock_resonated");

        if (!Progress.Has("cathedral_vault_scaled") && Progress.CanComplete("cathedral_vault_scaled"))
            CommitObjective("cathedral_vault_scaled");

        // Spawn Void Commander boss when entering Sector 5 Dais
        if (!_bossSpawned && !Progress.Has("void_commander_phase:3") && Player.GlobalPosition.X >= 20400f && Progress.Has("cathedral_vault_scaled"))
        {
            SpawnVoidCommanderBoss();
        }

        // Check core containment override console in Sector 6
        if (Progress.Has("void_commander_phase:3") && !Progress.Has("core_containment_initiated"))
        {
            if (!_coreTerminalOverridden && Player.GlobalPosition.DistanceTo(_coreTerminal.GlobalPosition) <= 90f &&
                AcceptTerminal(_coreTerminal, "core_containment_initiated", CommitObjective))
            {
                _coreTerminalOverridden = true;
                _coreTerminal.Visible = false;
                _acceptedSound?.Play();
            }
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
    private void SpawnVoidCommanderBoss()
    {
        _bossSpawned = true;
        _boss = new VoidCommanderBoss2D
        {
            Name = "VoidCommanderBoss",
            GlobalPosition = new Vector2(21800, 510)
        };
        _boss.BossHealthChanged += (curr, max, phase) =>
        {
            if (phase == 2 && !Progress.Has("void_commander_phase:1") && Progress.CanComplete("void_commander_phase:1"))
                CommitObjective("void_commander_phase:1");
            else if (phase == 3 && !Progress.Has("void_commander_phase:2") && Progress.CanComplete("void_commander_phase:2"))
                CommitObjective("void_commander_phase:2");
        };
        _boss.BossDefeated += () =>
        {
            if (!Progress.Has("void_commander_phase:3") && Progress.CanComplete("void_commander_phase:3"))
                CommitObjective("void_commander_phase:3");
        };

        _boss.Target = Player;
        _boss.PhaseAcceptance = stage => Progress.CanComplete("void_commander_phase:" + stage);
        Actors.AddChild(_boss);
        GetNode<CyberCombatHud>("CyberCombatHud").BindCampaignBoss(_boss);
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "IngressGantry" => true,
        "PowerSiphon" => Progress.Has("ingress_secured"),
        "VoidBarrier" => Progress.Has("siphon_grid_neutralized"),
        "CathedralVault" => Progress.Has("barrier_corridor_cleared"),
        "ThroneDais" => Progress.Has("cathedral_vault_scaled"),
        "CoreOverload" => Progress.Has("core_containment_initiated"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "ingress_secured" => "Secure Singularity Ingress Gantry",
        "siphon_coolant_doused" => "Douse power siphon coolant manifold with Oceanus",
        "siphon_clamp_shattered" => "Shatter dark-matter siphon clamp with Titan Slam",
        "siphon_grid_neutralized" => "Neutralize geothermal power siphon grid",
        "laser_curtain_phased" => "Phase through void barrier laser curtain with Mirage",
        "cosmic_vent_absorbed" => "Absorb cosmic radiation vent with Surya Bulwark",
        "barrier_corridor_cleared" => "Clear Void Barrier Corridor",
        "harmonic_lock_resonated" => "Resonate harmonic acoustic lock with Aura",
        "cathedral_vault_scaled" => "Scale cathedral vaulted elevator to Grand Throne Dais",
        "void_commander_phase:1" => "Engage The Void Commander",
        "void_commander_phase:2" => "Survive Void Commander blink teleportation and crescent slashes",
        "void_commander_phase:3" => "Defeat The Void Commander in Singularity Overdrive",
        "core_containment_initiated" => "Initiate Singularity Core containment override",
        "extract" => "Enter Liberation Threshold // CAMPAIGN COMPLETE",
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
        ResetArenaAttempt(ref _boss, ref _bossSpawned, "void_commander_phase", new[] { "boss1", "boss2", "boss3" });
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
