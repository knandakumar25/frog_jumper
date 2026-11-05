using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission 11: The Apex Bastion (Stratospheric Summit Fortress Infiltration).
/// Authored atop the Citadel Spire summit at 4,000 meters elevation featuring howling crosswinds,
/// twin-linked automated plasma flak batteries, exterior coolant manifolds, laser defense corridors,
/// the multi-phase Bastion Praetor boss encounter ("Praetor-11"), and fortress blast doors breach
/// leading directly to the Grand Citadel Throne Room (Mission 12).
/// Implements <see cref="IFiniteCombatHost"/> for deterministic, finite-formation encounter pacing.
/// </summary>
public partial class ApexBastionMission : CodeLevel, IFiniteCombatHost
{
    /// <summary>Scene-local mission ledger tracking sector progression, gates, boss phases, and objectives.</summary>
    public ApexBastionProgress Progress { get; } = new();

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
    private PalaceObjective _breachTerminal = null!;
    private PalaceObjective _exitTrigger = null!;
    private CyberDialogueBubble _dialogue = null!;
    private AudioStreamPlayer _acceptedSound = null!;
    private BastionPraetorBoss2D? _boss;
    private bool _recovering;
    private bool _bossSpawned;
    private bool _breachTerminalOverridden;

    /// <inheritdoc/>
    protected override int MissionNumber => 11;

    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1600, 28500, 2400);

    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 554);

    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;

    /// <inheritdoc/>
    protected override Color PlatformTint => new(0.18f, 0.22f, 0.32f);

    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/cutscenes/apex_bastion_intro/atmosphere.ogg";

    /// <inheritdoc/>
    protected override string CompletionLine => "APEX BASTION DEFENSES SHATTERED // CITADEL THRONE ROOM BREACHED";

    /// <inheritdoc/>
    protected override string IntroLine() => "SECTOR 11 // THE APEX BASTION // STRATOSPHERIC SUMMIT 4,000M";

    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) => "THE APEX BASTION";

    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[]
    {
        ("SummitGantry", new Vector2(400, 600)),
        ("FlakTrenches", new Vector2(5500, 600)),
        ("LowerRedoubt", new Vector2(10500, 600)),
        ("ElevatorShaft", new Vector2(16000, 600)),
        ("BastionArena", new Vector2(21000, 600)),
        ("CitadelBreach", new Vector2(26000, 600))
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
        string text = WildfireText.Get("APEX_BASTION_FRAGMENT_" + found, found switch
        {
            1 => "DATA SLATE 01 // PRAETORIAN DOCTRINE: The Citadel's elite guard mechanoids are programmed to fight to complete destruction.",
            2 => "DATA SLATE 02 // SUMMIT POWER MATRIX: The Void Commander channels stratospheric lightning directly into the upper throne room spire.",
            _ => "DATA SLATE 03 // BASTION PRAETOR DIRECTIVE: Supreme mechanoid guardian granted autonomous tactical command over all Citadel flak batteries."
        });
        _dialogue?.SetDialogueLine(text, "AXIOM-7 // TRANSMISSION ONLINE");
        return text;
    }

    /// <summary>Constructs high-altitude fortress battlements, wall-jump towers, and ability gates.</summary>
    protected override void BuildGeometry()
    {
        // World boundary walls and foundation
        AddPlatform(-100, -1600, 100, 2600, PalaceSurface.Wall);
        AddPlatform(0, 600, 28500, 900, PalaceSurface.Concrete, false, "BastionFoundation");
        AddPlatform(28500, -1600, 100, 2600, PalaceSurface.Wall);

        // Wall-jump pylon columns across the stratospheric fortress
        foreach (float wx in new[] { 1600f, 1850f, 6500f, 8800f, 12500f, 15000f, 20000f, 24500f })
        {
            AddPlatform(wx, 240, 24, 360, PalaceSurface.Wall, false);
        }

        // Sector 1: Stratospheric Summit Gantry
        AddPlatform(800, 440, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(2000, 410, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(2700, 440, 400, 22, PalaceSurface.Steel, true);
        AddPlatform(3500, 440, 480, 22, PalaceSurface.Steel, true);

        // Sector 2: Flak Battery Trenches & Coolant Conduits
        AddPlatform(5800, 430, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(7000, 410, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(8200, 430, 480, 22, PalaceSurface.Steel, true);

        // Sector 3: Lower Redoubt & Laser Defense Corridor
        AddPlatform(11200, 430, 450, 22, PalaceSurface.Steel, true);
        AddPlatform(12600, 360, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(14000, 440, 480, 22, PalaceSurface.Steel, true);

        // Sector 4: Praetorian Hydraulic Elevator Shaft
        AddPlatform(16800, 440, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(17900, 360, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(19000, 440, 460, 22, PalaceSurface.Steel, true);

        // Sector 5: Grand Bastion Redoubt (Bastion Praetor Boss)
        AddPlatform(20800, 420, 480, 22, PalaceSurface.Steel, true);
        AddPlatform(21600, 340, 520, 22, PalaceSurface.Steel, true);
        AddPlatform(22600, 420, 500, 22, PalaceSurface.Steel, true);

        // Sector 6: Throne Room Embankment & Apex Citadel Breach
        AddPlatform(25000, 440, 500, 22, PalaceSurface.Steel, true);
        AddPlatform(26000, 360, 560, 22, PalaceSurface.Steel, true);

        // Ability-Gated Obstacles
        // Sector 2: Plasma flak coolant pipe rupture requiring Oceanus Hydro Torrent
        AddGate("FlakCoolantGate", GateKind.FireVent, new Vector2(4800, 600));

        // Sector 2 exit: Reinforced magnetic titan-clamp mooring barricade requiring Apex Titan Slam
        AddGate("MooringClampGate", GateKind.RubbleWall, new Vector2(8800, 600));

        // Sector 3 exit: Concentrated plasma radiation thermal vents requiring Surya Prime Bulwark
        AddGate("PlasmaRadiationGate", GateKind.HeatVent, new Vector2(14800, 600));

        // Sector 4: Harmonic dissonance acoustic security lock requiring Aura Phoenicia Resonate
        AddGate("AcousticLockGate", GateKind.SonicLock, new Vector2(18800, 600));
    }

    /// <summary>Builds coolant louvers, perimeter laser grids, fortress breach terminal, and extraction beacon.</summary>
    protected override void BuildObjectives()
    {
        // Environmental Coolant Vents in Sector 2
        var vent1 = new CoolantVent
        {
            Name = "FlakCoolantVent1",
            Position = new Vector2(6600, 600),
            StartOffset = 0.0f
        };
        Actors.AddChild(vent1);
        _vents.Add(vent1);

        var vent2 = new CoolantVent
        {
            Name = "FlakCoolantVent2",
            Position = new Vector2(7900, 600),
            StartOffset = 1.5f
        };
        Actors.AddChild(vent2);
        _vents.Add(vent2);

        // Environmental Laser Grid in Sector 3 (Mirage Blade Phase Dash bypass)
        var laserGrid = new CyberLaserGrid2D
        {
            Name = "RedoubtLaserGrid",
            Position = new Vector2(10400, 360),
            BeamLength = 240f,
            Direction = Vector2.Down,
            ActiveDuration = 3.0f,
            InactiveDuration = 1.5f,
            Damage = 35f
        };
        Actors.AddChild(laserGrid);
        _lasers.Add(laserGrid);

        // Sector 6: Citadel Blast Doors Breach Terminal
        _breachTerminal = new PalaceObjective
        {
            Name = "CitadelBreachTerminal",
            Kind = "Memory",
            Position = new Vector2(26500, 600)
        };
        Actors.AddChild(_breachTerminal);

        // Final extraction beacon leading to Citadel Throne Room
        _exitTrigger = new PalaceObjective
        {
            Name = "ThroneRoomAscentTrigger",
            Kind = "Exit",
            Position = new Vector2(27800, 600)
        };
        Actors.AddChild(_exitTrigger);

        // Sector Atmospheric Signage
        string[] sectors = {
            "SEC.01 // STRATOSPHERIC SUMMIT GANTRY",
            "SEC.02 // FLAK BATTERY TRENCHES & COOLANT CONDUITS",
            "SEC.03 // LOWER REDOUBT & LASER DEFENSE CORRIDOR",
            "SEC.04 // PRAETORIAN HYDRAULIC ELEVATOR SHAFT",
            "SEC.05 // GRAND BASTION REDOUBT // BASTION PRAETOR",
            "SEC.06 // THRONE ROOM EMBANKMENT & CITADEL BREACH"
        };
        float[] sectorXs = { 400f, 4900f, 10600f, 15600f, 20400f, 24600f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label
            {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 260),
                Modulate = new Color(0.9f, 0.2f, 0.3f, 0.85f)
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
        // Sector 1: Stratospheric Summit Gantry encounters
        AddEncounter("summit1", new Rect2(1200, 200, 1200, 500), new[] { (new Vector2(1600, 554), true), (new Vector2(1900, 554), false) });
        AddEncounter("summit2", new Rect2(2800, 200, 1200, 500), new[] { (new Vector2(3100, 554), true), (new Vector2(3400, 554), true) });

        // Sector 2: Flak Battery Trenches encounters
        AddEncounter("flak1", new Rect2(5000, 200, 1000, 500), new[] { (new Vector2(5400, 554), false), (new Vector2(5800, 554), true) });
        AddEncounter("flak2", new Rect2(6800, 200, 1000, 500), new[] { (new Vector2(7200, 554), true), (new Vector2(7500, 554), false) });
        AddEncounter("flak3", new Rect2(8800, 200, 1000, 500), new[] { (new Vector2(9200, 554), true), (new Vector2(9500, 554), true) });

        // Sector 3: Lower Redoubt & Laser Defense Corridor encounters
        AddEncounter("redoubt1", new Rect2(11500, 200, 1000, 500), new[] { (new Vector2(11900, 554), true), (new Vector2(12200, 554), false) });
        AddEncounter("redoubt2", new Rect2(13000, 200, 1000, 500), new[] { (new Vector2(13400, 554), true), (new Vector2(13700, 554), true) });
        AddEncounter("redoubt3", new Rect2(14800, 200, 1000, 500), new[] { (new Vector2(15100, 554), false), (new Vector2(15400, 554), true) });

        // Sector 4: Praetorian Hydraulic Elevator Shaft encounters
        AddEncounter("praetorian1", new Rect2(16900, 200, 1000, 500), new[] { (new Vector2(17300, 554), true), (new Vector2(17600, 554), false) });
        AddEncounter("praetorian2", new Rect2(18500, 200, 1000, 500), new[] { (new Vector2(18800, 554), true), (new Vector2(19200, 554), true) });

        // Sector 5: Grand Bastion Redoubt boss support waves
        AddEncounter("arena1", new Rect2(20800, 200, 1000, 500), new[] { (new Vector2(21100, 554), true), (new Vector2(21400, 554), false) });
        AddEncounter("arena2", new Rect2(21800, 200, 1000, 500), new[] { (new Vector2(22100, 554), true), (new Vector2(22400, 554), true) });
        AddEncounter("arena3", new Rect2(22800, 200, 1000, 500), new[] { (new Vector2(23100, 554), true), (new Vector2(23400, 554), false) });

        // Sector 6: Throne Room Embankment breach encounter
        AddEncounter("breach1", new Rect2(25200, 200, 1000, 500), new[] { (new Vector2(25500, 554), true), (new Vector2(25800, 554), true) });
    }

    private void AddEncounter(string id, Rect2 activation, (Vector2 Position, bool Gunner)[] members)
    {
        var encounter = new WildfireEncounter
        {
            Name = "Encounter_" + id,
            Id = id,
            Activation = activation,
            Members = members,
            Mission = this
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
            Score.AddPoint();
            CurrentObjective = ComputeObjective();

            if (id == "citadel_blast_doors_breached")
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

        // Auto-check milestone conditions
        if (!Progress.Has("staging_secured") && Progress.CanComplete("staging_secured"))
            CommitObjective("staging_secured");

        if (!Progress.Has("flak_coolant_doused") && Progress.CanComplete("flak_coolant_doused"))
            CommitObjective("flak_coolant_doused");

        if (!Progress.Has("titan_clamp_shattered") && Progress.CanComplete("titan_clamp_shattered"))
            CommitObjective("titan_clamp_shattered");

        if (!Progress.Has("flak_battery_neutralized") && Progress.CanComplete("flak_battery_neutralized"))
            CommitObjective("flak_battery_neutralized");

        if (!Progress.Has("laser_curtain_phased") && Progress.CanComplete("laser_curtain_phased"))
            CommitObjective("laser_curtain_phased");

        if (!Progress.Has("thermal_vent_absorbed") && Progress.CanComplete("thermal_vent_absorbed"))
            CommitObjective("thermal_vent_absorbed");

        if (!Progress.Has("lower_redoubt_cleared") && Progress.CanComplete("lower_redoubt_cleared"))
            CommitObjective("lower_redoubt_cleared");

        if (!Progress.Has("acoustic_lock_resonated") && Progress.CanComplete("acoustic_lock_resonated"))
            CommitObjective("acoustic_lock_resonated");

        if (!Progress.Has("elevator_shaft_scaled") && Progress.CanComplete("elevator_shaft_scaled"))
            CommitObjective("elevator_shaft_scaled");

        // Spawn Bastion Praetor Boss when entering Sector 5 grand redoubt
        if (!_bossSpawned && Player.GlobalPosition.X >= 20400f && Progress.Has("elevator_shaft_scaled"))
        {
            SpawnBastionPraetorBoss();
        }

        // Check citadel blast doors breach terminal interaction in Sector 6
        if (Progress.Has("bastion_praetor_phase:3") && !Progress.Has("citadel_blast_doors_breached"))
        {
            if (!_breachTerminalOverridden && Player.GlobalPosition.DistanceTo(_breachTerminal.GlobalPosition) <= 90f)
            {
                _breachTerminalOverridden = true;
                _breachTerminal.Visible = false;
                _acceptedSound?.Play();
                CommitObjective("citadel_blast_doors_breached");
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

    private void SpawnBastionPraetorBoss()
    {
        _bossSpawned = true;
        _boss = new BastionPraetorBoss2D
        {
            Name = "BastionPraetorBoss",
            GlobalPosition = new Vector2(21800, 300)
        };
        _boss.BossHealthChanged += (curr, max, phase) =>
        {
            if (phase == 2 && !Progress.Has("bastion_praetor_phase:1") && Progress.CanComplete("bastion_praetor_phase:1"))
                CommitObjective("bastion_praetor_phase:1");
            else if (phase == 3 && !Progress.Has("bastion_praetor_phase:2") && Progress.CanComplete("bastion_praetor_phase:2"))
                CommitObjective("bastion_praetor_phase:2");
        };
        _boss.BossDefeated += () =>
        {
            if (!Progress.Has("bastion_praetor_phase:3") && Progress.CanComplete("bastion_praetor_phase:3"))
                CommitObjective("bastion_praetor_phase:3");
        };

        Actors.AddChild(_boss);
    }

    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine) => !GameplaySuspended && (shrine.Name.ToString() switch
    {
        "SummitGantry" => true,
        "FlakTrenches" => Progress.Has("staging_secured"),
        "LowerRedoubt" => Progress.Has("flak_battery_neutralized"),
        "ElevatorShaft" => Progress.Has("lower_redoubt_cleared"),
        "BastionArena" => Progress.Has("elevator_shaft_scaled"),
        "CitadelBreach" => Progress.Has("citadel_blast_doors_breached"),
        _ => false
    });

    /// <inheritdoc/>
    protected override string ComputeObjective() => Progress.Current switch
    {
        "staging_secured" => "Secure Stratospheric Summit Gantry",
        "flak_coolant_doused" => "Douse flak battery coolant pipe with Oceanus",
        "titan_clamp_shattered" => "Shatter magnetic mooring clamp with Titan Slam",
        "flak_battery_neutralized" => "Neutralize perimeter flak batteries",
        "laser_curtain_phased" => "Phase through redoubt laser curtain with Mirage",
        "thermal_vent_absorbed" => "Absorb plasma thermal vents with Surya Bulwark",
        "lower_redoubt_cleared" => "Clear Lower Fortress Redoubt",
        "acoustic_lock_resonated" => "Resonate acoustic security lock with Aura",
        "elevator_shaft_scaled" => "Scale hydraulic elevator shaft to Grand Redoubt",
        "bastion_praetor_phase:1" => "Engage Bastion Praetor war mechanoid",
        "bastion_praetor_phase:2" => "Survive Praetor twin-linked plasma flak bursts",
        "bastion_praetor_phase:3" => "Destroy Bastion Praetor war mechanoid",
        "citadel_blast_doors_breached" => "Breach Grand Citadel Throne Room blast doors",
        "extract" => "Enter Citadel Throne Room // PREPARE FOR JUDGMENT DAY",
        _ => "Mission complete"
    };

    // ─── IFiniteCombatHost Implementation ───────────────────────────────────
    /// <inheritdoc/>
    public bool HasCombatEvent(string id) => Progress.Has(id);

    /// <inheritdoc/>
    public void CompleteCombatEvent(string id) => Progress.Complete(id);

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
        _recovering = false;
    }
}
