using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 6: Cyber-Grave Labs - Underground Facility.
/// A high-intensity 6-sector subterranean infiltration mission featuring gene-warping stasis pods,
/// superheated cryogenic coolant vents, ultrasonic containment locks, and full 6-operative synergy.
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class CyberGraveMission : LaterCodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression and objectives.</summary>
    public CyberGraveProgress Progress { get; } = new();

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
    private readonly List<GenePod> _pods = new();
    private readonly List<CoolantVent> _vents = new();
    private readonly List<(Node Node, ProcessModeEnum Mode)> _recoveryModes = new();
    private PalaceObjective _vaultTerminal = null!;
    private PalaceObjective _exitTrigger = null!;
    private CyberDialogueBubble _dialogue = null!;
    private AudioStreamPlayer _acceptedSound = null!;
    private readonly HashSet<string> _milestoneRewards = new();
    private bool _recovering;
    private int _currentSector = -1;
    private Vector2 _previousPlayerPos;

    /// <inheritdoc/>
    protected override int MissionNumber => 6;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1100, 26000, 2100);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.22f, 0.42f, 0.38f);

    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/cutscenes/cyber_grave_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) =>
        WildfireText.Get("CYBER_GRAVE_TITLE", "CYBER-GRAVE LABS");

    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) =>
        WildfireText.Get("CYBER_GRAVE_BRIEFING", "Liberate captive operatives. Overload bio-synthetic conduits. Breach central security vault.");

    /// <inheritdoc/>
    protected override string IntroLine() =>
        WildfireText.Get("CYBER_GRAVE_INTRO", "AXIOM-7: Depth -400m. Bio-synthetic incubators and heavy conduits ahead. Breach perimeter and extract captives.");

    /// <inheritdoc/>
    protected override string CompletionLine => "CYBER-GRAVE LABS SECURED // CAPTIVES EVACUATED";

    /// <summary>Subterranean checkpoints gating the 6 sectors.</summary>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("FreightLift", new Vector2(500, 600)),
        ("Incubation", new Vector2(4500, 600)),
        ("Coolant", new Vector2(9000, 600)),
        ("ResonanceCore", new Vector2(13500, 600)),
        ("Geothermal", new Vector2(18000, 600)),
        ("CentralVault", new Vector2(22500, 600))
    };

    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[]
    {
        new Vector2(2450, 440),
        new Vector2(11700, 420),
        new Vector2(17000, 420)
    };

    /// <inheritdoc/>
    protected override string FragmentLog(int found)
    {
        string text = WildfireText.Get("CYBER_GRAVE_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // BIO-SYNTHESIS LOG: Subjects 401 through 480 transferred to incubation. Neural mapping running at 94%.",
            2 => "DATA SLATE 02 // GEOTHERMAL SIPHON: Overdrive reactor feeding primary conduits. Any disruption threatens structural containment.",
            _ => "DATA SLATE 03 // EXTRACTION DIRECTIVE: Evacuate all personnel. No human component may fall into enemy command hands."
        });

        return text;
    }

    /// <summary>Builds subterranean world geometry, catwalks, wall-jump columns, and ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and main foundation floor
        AddPlatform(-100, -1100, 100, 2100, PalaceSurface.Wall);
        AddPlatform(0, 600, 26000, 900, PalaceSurface.Concrete, false, "FacilityFloor");
        AddPlatform(26000, -1100, 100, 2100, PalaceSurface.Wall);

        // Wall-jump columns for vertical mobility across sectors
        foreach (float wx in new[] { 1400f, 1580f, 5200f, 7600f, 9800f, 11800f, 19200f, 21400f })
        {
            // Keep 160 px beneath decorative supports so ground defenders remain reachable.
            AddPlatform(wx, 260, 24, 180, PalaceSurface.Wall, false);
        }

        // Sector 1: Subterranean Freight Sump overhead gantries
        AddPlatform(700, 440, 380, 22, PalaceSurface.Steel, true);
        AddPlatform(1700, 410, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(2300, 440, 320, 22, PalaceSurface.Steel, true);
        AddPlatform(2900, 430, 400, 22, PalaceSurface.Steel, true);

        // Sector 2: Gene-Warping Incubation catwalks
        AddPlatform(4700, 430, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(5900, 410, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(7200, 430, 460, 22, PalaceSurface.Steel, true);

        // Sector 3: Cryogenic Coolant inspection conduits
        AddPlatform(9200, 430, 380, 22, PalaceSurface.Steel, true);
        AddPlatform(10200, 350, 380, 22, PalaceSurface.Steel, true);
        AddPlatform(11500, 420, 420, 22, PalaceSurface.Steel, true);

        // Sector 4: Resonance Containment vantage platforms
        AddPlatform(13900, 380, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(15200, 320, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(16800, 420, 400, 22, PalaceSurface.Steel, true);

        // Sector 5: Geothermal Siphon reactor platforms
        AddPlatform(18600, 440, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(19800, 360, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(21100, 430, 420, 22, PalaceSurface.Steel, true);

        // Sector 6: Central Vault fortified observation decks
        AddPlatform(23100, 430, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(24200, 360, 480, 22, PalaceSurface.Steel, true);

        // Ability-gated obstacles
        // Sector 2 entrance: Reinforced bulkhead requiring Titan's Slam
        AddGate("StasisBulkhead", GateKind.RubbleWall, new Vector2(4000, 600), new Vector2(120, 1700));

        // Sector 3 entrance: Superheated coolant vent requiring Oceanus's Douse
        AddObjectiveGate("CoolantVentGate", GateKind.FireVent, new Vector2(8500, 600), "coolant_override");

        // Sector 4 entrance: Ultrasonic frequency lock requiring Aura's Resonate
        AddObjectiveGate("ResonanceLock", GateKind.SonicLock, new Vector2(13000, 600), "resonance_bypass");

        // Sector 5 entrance: High-intensity radiation vent requiring Surya's Bulwark
        AddObjectiveGate("RadiationSiphonGate", GateKind.HeatVent, new Vector2(17500, 600), "siphon_contain");
    }

    /// <summary>Builds interactive stasis pods, coolant vents, laser grids, and the vault extraction console.</summary>
    protected override void BuildObjectives()
    {
        // Stasis Pods in Incubation Chamber (Sector 2)
        var pod1 = new GenePod
        {
            Name = "Pod1",
            Objective = "pod:1",
            Mission = this,
            Position = new Vector2(4900, 600),
            Caption = "Liberate operative 01"
        };
        Actors.AddChild(pod1);
        _pods.Add(pod1);

        var pod2 = new GenePod
        {
            Name = "Pod2",
            Objective = "pod:2",
            Mission = this,
            Position = new Vector2(6500, 600),
            Caption = "Liberate operative 02"
        };
        Actors.AddChild(pod2);
        _pods.Add(pod2);

        // Environmental Coolant Vents in Sector 3
        var vent1 = new CoolantVent
        {
            Name = "Vent1",
            Position = new Vector2(9600, 600),
            StartOffset = 0.0f
        };
        Actors.AddChild(vent1);
        _vents.Add(vent1);

        var vent2 = new CoolantVent
        {
            Name = "Vent2",
            Position = new Vector2(10900, 600),
            StartOffset = 1.0f
        };
        Actors.AddChild(vent2);
        _vents.Add(vent2);

        // Laser grid security barrier in Sector 4 (bypassed with Mirage Phase Shift)
        var laser = new CyberLaserGrid2D
        {
            Name = "LaserSecurity",
            Position = new Vector2(15800, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 3.0f,
            InactiveDuration = 1.2f,
            Damage = 25f
        };
        TrackPhaseBoundary(laser, "grid_phase");
        Actors.AddChild(laser);

        // Sector 6: Central Vault Decryption Terminal
        _vaultTerminal = new PalaceObjective
        {
            Name = "VaultTerminal",
            Kind = "Memory",
            Position = new Vector2(24600, 600)
        };
        Actors.AddChild(_vaultTerminal);

        // Final extraction lift trigger
        _exitTrigger = new PalaceObjective
        {
            Name = "ExtractionLift",
            Kind = "Exit",
            Position = new Vector2(25400, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Landmark Atmospheric Signs
        string[] sectors = {
            "SEC.01 // SUBTERRANEAN FREIGHT SUMP",
            "SEC.02 // GENE-WARPING INCUBATION CHAMBER",
            "SEC.03 // CRYOGENIC COOLANT DISTRIBUTION",
            "SEC.04 // RESONANCE CONTAINMENT CORE",
            "SEC.05 // GEOTHERMAL SIPHON & OVERDRIVE",
            "SEC.06 // CENTRAL EXTRACTION VAULT"
        };
        float[] sectorXs = { 400f, 4400f, 8900f, 13400f, 17900f, 22400f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label
            {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 280),
                Modulate = new Color(0, 0.94f, 1f, 0.65f)
            };
            secLabel.AddThemeFontSizeOverride("font_size", 14);
            World.AddChild(secLabel);
        }

        // Dialogue bubble and sound
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
        // Sector 1: Sump ambushes
        AddEncounter("sump1", new Rect2(1000, 200, 1200, 500), new[] { (new Vector2(1600, 554), true), (new Vector2(1900, 554), false) });
        AddEncounter("sump2", new Rect2(2400, 200, 1200, 500), new[] { (new Vector2(3000, 554), true), (new Vector2(3300, 554), true) });

        // Sector 2: Incubation defenders
        AddEncounter("incubate1", new Rect2(4400, 200, 1000, 500), new[] { (new Vector2(4800, 554), false), (new Vector2(5100, 554), true) });
        AddEncounter("incubate2", new Rect2(5800, 200, 1000, 500), new[] { (new Vector2(6200, 554), true), (new Vector2(6500, 554), false) });
        AddEncounter("incubate3", new Rect2(7000, 200, 1200, 500), new[] { (new Vector2(7400, 554), true), (new Vector2(7700, 554), true) });

        // Sector 3: Coolant conduit guards
        AddEncounter("coolant1", new Rect2(9200, 200, 1000, 500), new[] { (new Vector2(9800, 554), true), (new Vector2(10100, 554), false) });
        AddEncounter("coolant2", new Rect2(10600, 200, 1200, 500), new[] { (new Vector2(11200, 554), true), (new Vector2(11500, 554), true) });

        // Sector 4: Resonance core security
        AddEncounter("resonance1", new Rect2(13600, 200, 1000, 500), new[] { (new Vector2(14200, 554), false), (new Vector2(14500, 554), true) });
        AddEncounter("resonance2", new Rect2(14800, 200, 1000, 500), new[] { (new Vector2(15300, 554), true), (new Vector2(15600, 554), false) });
        AddEncounter("laser1", new Rect2(16200, 200, 1000, 500), new[] { (new Vector2(16700, 554), true), (new Vector2(17000, 554), true) });

        // Sector 5: Geothermal siphon sentries
        AddEncounter("siphon1", new Rect2(18200, 200, 1000, 500), new[] { (new Vector2(18800, 554), true), (new Vector2(19100, 554), false) });
        AddEncounter("siphon2", new Rect2(19600, 200, 1200, 500), new[] { (new Vector2(20200, 554), true), (new Vector2(20500, 554), true) });

        // Sector 6: Central vault defense waves
        AddEncounter("core1", new Rect2(22600, 200, 1000, 500), new[] { (new Vector2(23200, 554), true), (new Vector2(23500, 554), false) });
        AddEncounter("core2", new Rect2(23600, 200, 1000, 500), new[] { (new Vector2(24000, 554), true), (new Vector2(24300, 554), true) });
        AddEncounter("core3", new Rect2(24500, 200, 1000, 500), new[] { (new Vector2(24800, 554), true), (new Vector2(25100, 554), false) });
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

            // When vault override finishes, activate extraction exit
            if (id == "vault_override")
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
        if (Player.GlobalPosition.X >= 22400f && Player.GlobalPosition.X <= 25700f)
            foreach (var encounter in _encounters.Where(e => e.ManualActivation)) encounter.Activate();

        // Auto-check sector completion events based on player position and clears
        if (!Progress.Has("sump_breach") && Progress.CanComplete("sump_breach"))
            CommitObjective("sump_breach");

        if (!Progress.Has("incubation_cleared") && Progress.CanComplete("incubation_cleared"))
            CommitObjective("incubation_cleared");

        if (!Progress.Has("coolant_override") && Progress.CanComplete("coolant_override") && HasActionEvidence("coolant_override"))
            CommitObjective("coolant_override");

        if (!Progress.Has("resonance_bypass") && Progress.CanComplete("resonance_bypass") && HasActionEvidence("resonance_bypass"))
            CommitObjective("resonance_bypass");

        if (!Progress.Has("grid_phase") && Progress.CanComplete("grid_phase") && HasActionEvidence("grid_phase"))
            CommitObjective("grid_phase");

        if (!Progress.Has("siphon_contain") && Progress.CanComplete("siphon_contain") && HasActionEvidence("siphon_contain"))
            CommitObjective("siphon_contain");

        if (!Progress.Has("core_defense:1") && Progress.CanComplete("core_defense:1"))
            CommitObjective("core_defense:1");

        if (!Progress.Has("core_defense:2") && Progress.CanComplete("core_defense:2"))
            CommitObjective("core_defense:2");

        if (!Progress.Has("core_defense:3") && Progress.CanComplete("core_defense:3"))
            CommitObjective("core_defense:3");

        // Interaction with the vault terminal
        if (_vaultTerminal != null && !_vaultTerminal.Collected &&
            Player.GlobalPosition.DistanceTo(_vaultTerminal.GlobalPosition) < 120f)
        {
            AcceptTerminal(_vaultTerminal, "vault_override", CommitObjective);
        }

        // Final extraction reached
        if (_exitTrigger != null && Progress.CanExtract &&
            Player.GlobalPosition.DistanceTo(_exitTrigger.GlobalPosition) < 140f)
        {
            CompleteMission();
        }
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "FreightLift" => true,
        "Incubation" => Progress.Has("sump_breach"),
        "Coolant" => Progress.Has("incubation_cleared"),
        "ResonanceCore" => Progress.Has("coolant_override"),
        "Geothermal" => Progress.Has("grid_phase"),
        "CentralVault" => Progress.Has("siphon_contain"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "sump_breach" => "Clear freight sump security units",
        "pod:1" => "Shatter gene-pod 01 to liberate captive",
        "pod:2" => "Shatter gene-pod 02 to liberate captive",
        "incubation_cleared" => "Clear incubation chamber defenses",
        "coolant_override" => "Override cryogenic coolant distribution network",
        "resonance_bypass" => "Bypass ultrasonic resonance barrier",
        "grid_phase" => "Phase through security laser grid",
        "siphon_contain" => "Contain geothermal siphon reactor radiation",
        "core_defense:1" => "Defeat vault perimeter defense wave 01",
        "core_defense:2" => "Defeat vault perimeter defense wave 02",
        "core_defense:3" => "Defeat elite security unit",
        "vault_override" => "Access central security terminal",
        "extract" => "Board freight extraction lift",
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
        _previousPlayerPos = Player.Position;
        _recovering = false;
    }
}
