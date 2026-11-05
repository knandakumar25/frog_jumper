using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 10: The Cyber-Nexus (Mainframe Core Infiltration).
/// Authored inside the Citadel Spire interior core featuring liquid helium coolant vaults,
/// optical data stream laser firewalls, neural matrix sub-stations, the multi-phase Nexus Overseer
/// boss encounter ("Overseer-10"), and extraction via the central express ascent elevator into the Apex Bastion.
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class CyberNexusMission : LaterCodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression, gates, boss phases, and objectives.</summary>
    public NexusProgress Progress { get; } = new();

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
    private PalaceObjective _nodeAlpha = null!;
    private PalaceObjective _nodeBeta = null!;
    private PalaceObjective _nodeGamma = null!;
    private PalaceObjective _exitTrigger = null!;
    private CyberDialogueBubble _dialogue = null!;
    private AudioStreamPlayer _acceptedSound = null!;
    private NexusOverseerBoss2D? _boss;
    private readonly HashSet<string> _milestoneRewards = new();
    private bool _recovering;
    private bool _bossSpawned;
    private bool _nodeAlphaOverridden;
    private bool _nodeBetaOverridden;
    private bool _nodeGammaOverridden;

    /// <inheritdoc/>
    protected override int MissionNumber => 10;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1600, 28000, 2400);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.12f, 0.28f, 0.45f);

    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/cutscenes/cyber_nexus_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string CompletionLine => "CYBER-NEXUS MAIN CORE NEUTRALIZED // APEX BASTION ASCENT UNLOCKED";

    /// <inheritdoc/>
    protected override string IntroLine() => "SECTOR 10 // THE CYBER-NEXUS // CITADEL SPIRE CORE";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) => "THE CYBER-NEXUS";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("IngressGantry", new Vector2(400, 600)),
        ("CoolingVaults", new Vector2(5000, 600)),
        ("OpticalConduits", new Vector2(11000, 600)),
        ("NeuralSubstation", new Vector2(16500, 600)),
        ("QuantumCoreArena", new Vector2(20500, 600)),
        ("ApexAscentLift", new Vector2(24500, 600))
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
        string text = WildfireText.Get("CYBER_NEXUS_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // QUANTUM MAINFRAME ARCHITECTURE: The Cyber-Nexus processes city-wide biometric tracking and combat drone subroutines.",
            2 => "DATA SLATE 02 // SUB-ZERO COOLANT SYSTEM: Liquid helium manifolds keep the central quantum core from thermal meltdown under maximum processing load.",
            _ => "DATA SLATE 03 // NEXUS OVERSEER DEFENSE DIRECTIVE: Primary mainframe security guardian authorized to purge unauthorized bio-signatures."
        });

        return text;
    }

    /// <summary>Constructs high-tech server racks, illuminated glass catwalks, wall-jump monoliths, and ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and foundation
        AddPlatform(-100, -1600, 100, 2600, PalaceSurface.Wall);
        AddPlatform(0, 600, 27500, 900, PalaceSurface.Concrete, false, "SpireFoundation");
        AddPlatform(27500, -1600, 100, 2600, PalaceSurface.Wall);

        // Wall-jump server monoliths across the abyssal mainframe core
        foreach (float wx in new[] { 1700f, 1880f, 6300f, 8500f, 12300f, 14900f, 19500f, 23900f })
        {
            // Keep 160 px beneath decorative supports so ground defenders remain reachable.
            AddPlatform(wx, 240, 24, 200, PalaceSurface.Wall, false);
        }

        // Sector 1: Main Ingress Gantry
        AddPlatform(800, 440, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(2000, 410, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(2700, 440, 380, 22, PalaceSurface.Steel, true);
        AddPlatform(3500, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 2: Liquid Helium Cooling Vaults
        AddPlatform(5900, 430, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(7100, 410, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(8300, 430, 480, 22, PalaceSurface.Steel, true);

        // Sector 3: Optical Data Stream Conduits
        AddPlatform(11400, 430, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(12700, 360, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(14100, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 4: Neural Matrix Sub-station
        AddPlatform(16900, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(18000, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(19100, 440, 450, 22, PalaceSurface.Steel, true);

        // Sector 5: Quantum Core Arena (Nexus Overseer Boss)
        AddPlatform(20800, 420, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(21500, 340, 500, 22, PalaceSurface.Steel, true);
        AddPlatform(22400, 420, 480, 22, PalaceSurface.Steel, true);

        // Sector 6: Three Quantum Nodes & Apex Ascent Lift
        AddPlatform(24700, 440, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(25500, 360, 540, 22, PalaceSurface.Steel, true);

        // Ability-Gated Obstacles
        // Sector 2: Liquid helium pipe rupture requiring Oceanus Hydro Torrent
        AddObjectiveGate("HeliumLeakGate", GateKind.FireVent, new Vector2(4600, 600), "helium_manifold_doused");

        // Sector 2 exit: Reinforced titanium server bracket requiring Apex Titan Slam
        AddObjectiveGate("ServerBracketBulkhead", GateKind.RubbleWall, new Vector2(8600, 600), "server_bracket_shattered");

        // Sector 3 exit: Biometric scanner firewall corridor requiring Surya Prime Bulwark
        AddObjectiveGate("BiometricFirewallGate", GateKind.HeatVent, new Vector2(14600, 600), "biometric_firewall_absorbed");

        // Sector 4: Neural matrix cipher sonic lock requiring Aura Phoenicia Resonate
        AddObjectiveGate("NeuralCipherSonicGate", GateKind.SonicLock, new Vector2(18600, 600), "cipher_resonance_bypassed");
    }

    /// <summary>Builds cryogenic louvers, optical laser firewalls, quantum override nodes, and lift trigger.</summary>
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

        // Environmental Laser Grid in Sector 3 (Mirage Blade Phase Dash bypass)
        var laserGrid = new CyberLaserGrid2D
        {
            Name = "OpticalFirewallLaser",
            Position = new Vector2(10400, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 3.0f,
            InactiveDuration = 1.5f,
            Damage = 35f
        };
        TrackPhaseBoundary(laserGrid, "laser_firewall_phased");
        Actors.AddChild(laserGrid);
        _lasers.Add(laserGrid);

        // Sector 6: Three Quantum Processing Nodes
        _nodeAlpha = new PalaceObjective
        {
            Name = "NodeAlpha",
            Kind = "Memory",
            Position = new Vector2(24800, 440)
        };
        Actors.AddChild(_nodeAlpha);

        _nodeBeta = new PalaceObjective
        {
            Name = "NodeBeta",
            Kind = "Memory",
            Position = new Vector2(25400, 360)
        };
        Actors.AddChild(_nodeBeta);

        _nodeGamma = new PalaceObjective
        {
            Name = "NodeGamma",
            Kind = "Memory",
            Position = new Vector2(26000, 600)
        };
        Actors.AddChild(_nodeGamma);

        // Final extraction beacon at Apex Ascent Lift
        _exitTrigger = new PalaceObjective
        {
            Name = "ApexAscentTrigger",
            Kind = "Exit",
            Position = new Vector2(26800, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Atmospheric Signage
        string[] sectors = {
            "SEC.01 // MAIN INGRESS GANTRY",
            "SEC.02 // LIQUID HELIUM COOLING VAULTS",
            "SEC.03 // OPTICAL DATA STREAM CONDUITS",
            "SEC.04 // NEURAL MATRIX SUB-STATION",
            "SEC.05 // QUANTUM CORE ARENA // NEXUS OVERSEER",
            "SEC.06 // QUANTUM NODES & APEX ASCENT LIFT"
        };
        float[] sectorXs = { 400f, 4800f, 10500f, 15500f, 20200f, 24200f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label
            {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 260),
                Modulate = new Color(0.1f, 0.9f, 1.0f, 0.85f)
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
        // Sector 1: Main Ingress Gantry encounters
        AddEncounter("ingress1", new Rect2(1200, 200, 1200, 500), new[] { (new Vector2(1600, 554), true), (new Vector2(1900, 554), false) });
        AddEncounter("ingress2", new Rect2(2800, 200, 1200, 500), new[] { (new Vector2(3100, 554), true), (new Vector2(3400, 554), true) });

        // Sector 2: Liquid Helium Cooling Vaults encounters
        AddEncounter("coolant1", new Rect2(5000, 200, 1000, 500), new[] { (new Vector2(5400, 554), false), (new Vector2(5800, 554), true) });
        AddEncounter("coolant2", new Rect2(6800, 200, 1000, 500), new[] { (new Vector2(7200, 554), true), (new Vector2(7500, 554), false) });
        AddEncounter("coolant3", new Rect2(8800, 200, 1000, 500), new[] { (new Vector2(9200, 554), true), (new Vector2(9500, 554), true) });

        // Sector 3: Optical Data Stream Conduits encounters
        AddEncounter("data1", new Rect2(11500, 200, 1000, 500), new[] { (new Vector2(11900, 554), true), (new Vector2(12200, 554), false) });
        AddEncounter("data2", new Rect2(13000, 200, 1000, 500), new[] { (new Vector2(13400, 554), true), (new Vector2(13700, 554), true) });
        AddEncounter("data3", new Rect2(14800, 200, 1000, 500), new[] { (new Vector2(15100, 554), false), (new Vector2(15400, 554), true) });

        // Sector 4: Neural Matrix Sub-station encounters
        AddEncounter("neural1", new Rect2(16900, 200, 1000, 500), new[] { (new Vector2(17300, 554), true), (new Vector2(17600, 554), false) });
        AddEncounter("neural2", new Rect2(18200, 200, 1000, 500), new[] { (new Vector2(18500, 554), true), (new Vector2(18800, 554), true) });
        AddEncounter("neural3", new Rect2(19500, 200, 1000, 500), new[] { (new Vector2(19800, 554), true), (new Vector2(20100, 554), false) });

        // Sector 5: Quantum Core Arena boss support waves
        AddEncounter("core1", new Rect2(20800, 200, 1000, 500), new[] { (new Vector2(21100, 554), true), (new Vector2(21400, 554), false) });
        AddEncounter("core2", new Rect2(21800, 200, 1000, 500), new[] { (new Vector2(22100, 554), true), (new Vector2(22400, 554), true) });
        AddEncounter("core3", new Rect2(22800, 200, 1000, 500), new[] { (new Vector2(23100, 554), true), (new Vector2(23400, 554), false) });
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
            ManualActivation = new[] { "core1", "core2", "core3" }.Contains(id),
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

            if (id == "quantum_nodes_overridden")
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
        if (!Progress.Has("ingress_confirmed") && Progress.CanComplete("ingress_confirmed"))
            CommitObjective("ingress_confirmed");

        if (!Progress.Has("helium_manifold_doused") && Progress.CanComplete("helium_manifold_doused") && HasActionEvidence("helium_manifold_doused"))
            CommitObjective("helium_manifold_doused");

        if (!Progress.Has("server_bracket_shattered") && Progress.CanComplete("server_bracket_shattered") && HasActionEvidence("server_bracket_shattered"))
            CommitObjective("server_bracket_shattered");

        if (!Progress.Has("cooling_vaults_secured") && Progress.CanComplete("cooling_vaults_secured"))
            CommitObjective("cooling_vaults_secured");

        if (!Progress.Has("laser_firewall_phased") && Progress.CanComplete("laser_firewall_phased") && HasActionEvidence("laser_firewall_phased"))
            CommitObjective("laser_firewall_phased");

        if (!Progress.Has("biometric_firewall_absorbed") && Progress.CanComplete("biometric_firewall_absorbed") && HasActionEvidence("biometric_firewall_absorbed"))
            CommitObjective("biometric_firewall_absorbed");

        if (!Progress.Has("optical_conduits_secured") && Progress.CanComplete("optical_conduits_secured"))
            CommitObjective("optical_conduits_secured");

        if (!Progress.Has("cipher_resonance_bypassed") && Progress.CanComplete("cipher_resonance_bypassed") && HasActionEvidence("cipher_resonance_bypassed"))
            CommitObjective("cipher_resonance_bypassed");

        if (!Progress.Has("neural_substation_breached") && Progress.CanComplete("neural_substation_breached"))
            CommitObjective("neural_substation_breached");

        // Spawn Nexus Overseer Boss when entering Sector 5 quantum core arena
        if (!_bossSpawned && !Progress.Has("nexus_overseer_phase:3") && Player.GlobalPosition.X >= 20400f && Progress.Has("neural_substation_breached"))
        {
            SpawnNexusOverseerBoss();
        }

        // Check quantum processing nodes interaction in Sector 6
        if (Progress.CanComplete("quantum_nodes_overridden"))
        {
            if (!_nodeAlphaOverridden && Player.GlobalPosition.DistanceTo(_nodeAlpha.GlobalPosition) <= 90f)
            {
                _nodeAlphaOverridden = true;
                _nodeAlpha.SetCollected();
                _acceptedSound?.Play();
            }
            if (!_nodeBetaOverridden && Player.GlobalPosition.DistanceTo(_nodeBeta.GlobalPosition) <= 90f)
            {
                _nodeBetaOverridden = true;
                _nodeBeta.SetCollected();
                _acceptedSound?.Play();
            }
            if (!_nodeGammaOverridden && Player.GlobalPosition.DistanceTo(_nodeGamma.GlobalPosition) <= 90f)
            {
                _nodeGammaOverridden = true;
                _nodeGamma.SetCollected();
                _acceptedSound?.Play();
            }

            if (_nodeAlphaOverridden && _nodeBetaOverridden && _nodeGammaOverridden)
            {
                CommitObjective("quantum_nodes_overridden");
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
    private void SpawnNexusOverseerBoss()
    {
        _bossSpawned = true;
        _boss = new NexusOverseerBoss2D
        {
            Name = "NexusOverseerBoss",
            GlobalPosition = new Vector2(21800, 510)
        };
        _boss.BossHealthChanged += (curr, max, phase) =>
        {
            if (phase == 2 && !Progress.Has("nexus_overseer_phase:1") && Progress.CanComplete("nexus_overseer_phase:1"))
                CommitObjective("nexus_overseer_phase:1");
            else if (phase == 3 && !Progress.Has("nexus_overseer_phase:2") && Progress.CanComplete("nexus_overseer_phase:2"))
                CommitObjective("nexus_overseer_phase:2");
        };
        _boss.BossDefeated += () =>
        {
            if (!Progress.Has("nexus_overseer_phase:3") && Progress.CanComplete("nexus_overseer_phase:3"))
                CommitObjective("nexus_overseer_phase:3");
        };

        _boss.Target = Player;
        _boss.PhaseAcceptance = stage => Progress.CanComplete("nexus_overseer_phase:" + stage);
        Actors.AddChild(_boss);
        GetNode<CyberCombatHud>("CyberCombatHud").BindCampaignBoss(_boss);
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "IngressGantry" => true,
        "CoolingVaults" => Progress.Has("ingress_confirmed"),
        "OpticalConduits" => Progress.Has("cooling_vaults_secured"),
        "NeuralSubstation" => Progress.Has("optical_conduits_secured"),
        "QuantumCoreArena" => Progress.Has("neural_substation_breached"),
        "ApexAscentLift" => Progress.Has("quantum_nodes_overridden"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "ingress_confirmed" => "Infiltrate Main Ingress Gantry",
        "helium_manifold_doused" => "Douse liquid helium vent with Oceanus",
        "server_bracket_shattered" => "Shatter reinforced server bracket with Titan Slam",
        "cooling_vaults_secured" => "Secure Liquid Helium Cooling Vaults",
        "laser_firewall_phased" => "Phase through optical firewall with Mirage",
        "biometric_firewall_absorbed" => "Absorb biometric scanner firewall with Surya Bulwark",
        "optical_conduits_secured" => "Clear Optical Data Stream Conduits",
        "cipher_resonance_bypassed" => "Bypass neural cipher with Aura Resonate",
        "neural_substation_breached" => "Breach Neural Matrix Sub-station",
        "nexus_overseer_phase:1" => "Engage Nexus Overseer in Quantum Core",
        "nexus_overseer_phase:2" => "Survive Nexus Overseer overdrive pulse barrages",
        "nexus_overseer_phase:3" => "Destroy Nexus Overseer mechanoid",
        "quantum_nodes_overridden" => "Override all 3 Quantum Processing Nodes (Alpha, Beta, Gamma)",
        "extract" => "Board central express ascent lift // ASCEND TO THE APEX BASTION",
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
        ResetArenaAttempt(ref _boss, ref _bossSpawned, "nexus_overseer_phase", new[] { "core1", "core2", "core3" });
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
