using Godot;
using System.Collections.Generic;

/// <summary>
/// Builds and directs Level 1: Ghost in the Gridlock as a non-platforming isometric mission.
/// The scene uses the selected operative, six story locations, optional archive fragments,
/// checkpoint recovery, the demolition beacon, Data-Blade recovery, and extraction.
/// </summary>
public partial class IsometricMission : Node2D
{
    /// <summary>Public read-only player reference for scene integration checks.</summary>
    public IsometricOperative Player => _player;
    /// <summary>True after the demolition beacon has been destroyed.</summary>
    public bool BeaconNeutralized { get; private set; }
    /// <summary>True after the player has interacted with the unlocked Data-Blade.</summary>
    public bool DataBladeRecovered { get; private set; }
    /// <summary>True after extraction completes and the campaign advances.</summary>
    public bool MissionComplete { get; private set; }
    /// <summary>Number of optional memory fragments recovered from side alcoves.</summary>
    public int MemoryFragmentsFound { get; private set; }
    /// <summary>Current world objective displayed in the mission HUD.</summary>
    public string CurrentObjective { get; private set; } = "Infiltrate the Eternal Zen Dojo";

    private const float MapWidth = 4800f;
    private const float MapHeight = 1600f;
    private const string LevelTwoPath = "res://scenes/gameplay/level2.tscn";
    private IsometricOperative _player = null!;
    private Node2D _actors = null!;
    private Sprite2D _map = null!;
    private IsometricLevelObjective _beacon = null!;
    private IsometricLevelObjective _dataBlade = null!;
    private IsometricLevelObjective _extraction = null!;
    private IsometricSweepBeam _sweepBeam = null!;
    private readonly List<IsometricLevelObjective> _memoryFragments = new();
    private Node2D _collisionRoot = null!;
    private CanvasLayer _hud = null!;
    private Label _objectiveLabel = null!;
    private Label _dialogueLabel = null!;
    private Label _purgeLabel = null!;
    private Label _pauseLabel = null!;
    private ProgressBar _purgeProgress = null!;
    private Control _pausePanel = null!;
    private GameManager _gameManager = null!;
    private Vector2 _checkpoint = new(520f, 1160f);
    private bool _firstCheckpointReached;
    private bool _secondCheckpointReached;
    private bool _paused;
    private bool _finishing;
    private float _elapsed;
    private float _dialogueRemaining;
    private Label _controlsLabel = null!;

    /// <summary>Creates the Blender map, isometric world collision, selected operative and mission props.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Set("reached_finish", false);
        GetTree().Paused = false;
        global?.Call("stop_menu_music");
        global?.Call("set_control_prompt_visible", false);

        _map = new Sprite2D
        {
            Name = "IsometricPalaceMap",
            Texture = GD.Load<Texture2D>("res://assets/images/level1/ghost_gridlock.png"),
            Position = new Vector2(MapWidth * .5f, MapHeight * .5f),
            Scale = new Vector2(MapWidth / 4608f, MapHeight / 1536f),
            FlipV = true,
            ZIndex = -100,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear
        };
        AddChild(_map);

        _collisionRoot = new Node2D { Name = "WorldCollision" };
        AddChild(_collisionRoot);
        BuildWorldBounds();
        BuildCoverCollisions();

        _actors = new Node2D { Name = "Actors", YSortEnabled = true };
        AddChild(_actors);
        _player = GD.Load<PackedScene>("res://scenes/gameplay/isometric_operative.tscn").Instantiate<IsometricOperative>();
        _player.Name = "CyberShogun";
        _player.Position = new Vector2(520f, 1160f);
        _player.ExternalRespawn = true;
        _player.PlayerDefeated += OnPlayerDefeated;
        _actors.AddChild(_player);

        _gameManager = new GameManager { Name = "GameManager" };
        AddChild(_gameManager);
        var combatHud = GD.Load<PackedScene>("res://scenes/gameplay/cyber_combat_hud.tscn").Instantiate<CanvasLayer>();
        combatHud.Name = "CyberCombatHud";
        AddChild(combatHud);
        SpawnDroneCrew();
        BuildObjectives();
        BuildUserInterface();
        AddSweepBeam();
        AddMemoryFragments();
        InstallCheckpointMarkers();
        GD.Print("IsometricMission: Ghost in the Gridlock ready for " + _player.ActiveProfile.DisplayName);
    }

    /// <summary>Updates the environmental purge meter, pressure tint, and dialogue timeout.</summary>
    public override void _Process(double delta)
    {
        if (_paused || MissionComplete) return;
        _elapsed += (float)delta;
        _dialogueRemaining = Mathf.Max(0f, _dialogueRemaining - (float)delta);
        _dialogueLabel.Visible = _dialogueRemaining > 0f;
        float purge = Mathf.Clamp(_elapsed / (18f * 60f), 0f, 1f);
        _purgeProgress.Value = purge * 100f;
        _purgeLabel.Text = BeaconNeutralized ? "CULTURE-ERASURE FIELD  /  STALLED" : $"CULTURE-ERASURE FIELD  /  {purge:P0}";
        _map.Modulate = Colors.White.Lerp(new Color("#d8c3df"), purge * .19f);
        if (BeaconNeutralized) _sweepBeam.Disabled = true;
    }

    /// <summary>Tracks mission zones, checkpoint arrivals, objective interaction, and extraction.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_paused || MissionComplete || _player == null) return;
        UpdateRoomObjective();
        UpdateCheckpoints();
        if (Input.IsActionJustPressed("iso_interact")) InteractWithCurrentObjective();
    }

    /// <summary>Accepts the pause key while the world is paused and routes resume through the same action.</summary>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
            _controlsLabel.Text = "L STICK MOVE | R STICK AIM | LT ATTACK | RT ABILITY | RB DASH | A USE | START PAUSE";
        else if (@event is InputEventKey or InputEventMouseButton or InputEventMouseMotion)
            _controlsLabel.Text = "WASD MOVE | MOUSE AIM | F ATTACK | Q ABILITY | SHIFT DASH | E USE | ESC PAUSE";
        if (_paused && @event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            SetPaused(false);
            GetViewport().SetInputAsHandled();
        }
        else if (_paused && @event is InputEventJoypadButton button && button.Pressed && button.ButtonIndex == JoyButton.Start)
        {
            SetPaused(false);
            GetViewport().SetInputAsHandled();
        }
        else if (!_paused && @event is InputEventKey keyPressed && keyPressed.Pressed && !keyPressed.Echo && keyPressed.Keycode == Key.Escape)
            SetPaused(true);
        else if (!_paused && @event is InputEventJoypadButton padPressed && padPressed.Pressed && padPressed.ButtonIndex == JoyButton.Start)
            SetPaused(true);
    }

    /// <summary>Builds invisible world edges so players cannot leave the palace footprint.</summary>
    private void BuildWorldBounds()
    {
        AddWall(new Vector2(0f, 800f), new Vector2(100f, MapHeight));
        AddWall(new Vector2(MapWidth, 800f), new Vector2(100f, MapHeight));
        AddWall(new Vector2(3600f, 0f), new Vector2(2400f, 80f));
        AddWall(new Vector2(1800f, 800f), new Vector2(3600f, 82f));
        AddWall(new Vector2(2400f, 400f), new Vector2(80f, 800f));
        AddWall(new Vector2(MapWidth, 400f), new Vector2(80f, 800f));
        AddWall(new Vector2(MapWidth * .5f, MapHeight), new Vector2(MapWidth, 80f));
    }

    /// <summary>Adds solid props that visually match the lanterns, trees and archive shelves.</summary>
    private void BuildCoverCollisions()
    {
        foreach (Vector2 point in new[] {
            new Vector2(1440,1010), new Vector2(1790,1340), new Vector2(2150,1040), new Vector2(1980,1510),
            new Vector2(2670,1000), new Vector2(2940,1420), new Vector2(3370,1030),
            new Vector2(4110,1000), new Vector2(4480,1040), new Vector2(4710,1010)
        }) AddWall(point, new Vector2(62f, 58f), 1f);
    }

    /// <summary>Creates a rectangular static collision body at a world coordinate.</summary>
    private void AddWall(Vector2 center, Vector2 size, float rotation = 0f)
    {
        var body = new StaticBody2D { Position = center, Rotation = rotation, CollisionLayer = 1, CollisionMask = 0 };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        _collisionRoot.AddChild(body);
    }

    /// <summary>Places the story's six demolition drones and later enforcer reinforcements by sector.</summary>
    private void SpawnDroneCrew()
    {
        var positions = new[] {
            new Vector2(1510,1210), new Vector2(2080,1410),
            new Vector2(2680,1240), new Vector2(3260,1040),
            new Vector2(4010,1300), new Vector2(4520,660)
        };
        Color[] variants = { new("#cf668f"), new("#7967df"), new("#d08b59"), new("#52b7b0") };
        for (int index = 0; index < positions.Length; index++)
        {
            var drone = new IsometricPurgeDrone
            {
                Name = $"PurgeDrone{index + 1:00}",
                Position = positions[index],
                MaxHealth = index >= 8 ? 112f : 92f,
                HuntSpeed = 92f + index % 3 * 15f,
                DroneColor = variants[index % variants.Length]
            };
            drone.Defeated += () => _gameManager.AddPoint();
            _actors.AddChild(drone);
        }
    }

    /// <summary>Creates the beacon, gated Data-Blade and extraction marker described in the level script.</summary>
    private void BuildObjectives()
    {
        _beacon = new IsometricLevelObjective { Name = "DemolitionBeacon", Kind = "Beacon", MaxHealth = 360f, Position = new Vector2(4200, 350) };
        _beacon.Neutralized += OnBeaconNeutralized;
        _actors.AddChild(_beacon);
        _dataBlade = new IsometricLevelObjective { Name = "DataBlade", Kind = "DataBlade", Position = new Vector2(3020, 365) };
        _actors.AddChild(_dataBlade);
        _extraction = new IsometricLevelObjective { Name = "ExtractionGate", Kind = "Extraction", Position = new Vector2(2540, 540) };
        _actors.AddChild(_extraction);

        var camera = _player.GetNode<Camera2D>("Camera2D");
        camera.Position = Vector2.Zero;
        camera.Zoom = new Vector2(.88f, .88f);
        camera.LimitLeft = 0;
        camera.LimitTop = 0;
        camera.LimitRight = (int)MapWidth;
        camera.LimitBottom = (int)MapHeight;
        camera.PositionSmoothingEnabled = true;
        camera.PositionSmoothingSpeed = 6f;
        camera.Enabled = true;
    }

    /// <summary>Creates four ambient purge beams over the demolition approach and yard.</summary>
    private void AddSweepBeam()
    {
        _sweepBeam = new IsometricSweepBeam { Name = "TerraformingSweep", Position = new Vector2(4200, 420), ZIndex = 3 };
        _actors.AddChild(_sweepBeam);
    }

    /// <summary>Adds memory fragments that reward optional exploration but never gate extraction.</summary>
    private void AddMemoryFragments()
    {
        foreach (Vector2 point in new[] { new Vector2(1710, 930), new Vector2(3170, 1460), new Vector2(4560, 1390) })
        {
            var fragment = new IsometricLevelObjective { Name = "MemoryFragment", Kind = "Fragment", Position = point };
            _memoryFragments.Add(fragment);
            _actors.AddChild(fragment);
        }
    }

    /// <summary>Places the first safe recovery point after the archive and the second in the dojo core.</summary>
    private void InstallCheckpointMarkers()
    {
        var archiveMarker = new IsometricLevelObjective { Name = "ArchiveCheckpoint", Kind = "Checkpoint", Position = new Vector2(3720, 1260) };
        var dojoMarker = new IsometricLevelObjective { Name = "DojoCheckpoint", Kind = "Checkpoint", Position = new Vector2(3480, 700) };
        _actors.AddChild(archiveMarker);
        _actors.AddChild(dojoMarker);
    }

    /// <summary>Builds objective, purge-pressure and character-adapted story dialogue UI.</summary>
    private void BuildUserInterface()
    {
        _hud = new CanvasLayer { Name = "MissionHUD", Layer = 5, ProcessMode = ProcessModeEnum.Always };
        AddChild(_hud);
        var root = new Control { Name = "HUDRoot", LayoutMode = 3, AnchorsPreset = (int)Control.LayoutPreset.FullRect, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _hud.AddChild(root);

        var objectivePanel = NewPanel(new Vector2(-410, 18), new Vector2(390, 122), new Vector2(1, 0));
        root.AddChild(objectivePanel);
        var objectiveVBox = new VBoxContainer();
        objectivePanel.AddChild(objectiveVBox);
        _objectiveLabel = NewLabel("OBJECTIVE  /  INFILTRATE THE ETERNAL ZEN DOJO", 15, new Color("#77efff"));
        _objectiveLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        objectiveVBox.AddChild(_objectiveLabel);
        _purgeLabel = NewLabel("CULTURE-ERASURE FIELD  /  0%", 12, new Color("#ff8bb1"));
        objectiveVBox.AddChild(_purgeLabel);
        _purgeProgress = new ProgressBar { CustomMinimumSize = new Vector2(340, 15), MaxValue = 100, ShowPercentage = false };
        var progressTrack = new StyleBoxFlat { BgColor = new Color("#182333"), BorderColor = new Color("#744170") };
        progressTrack.SetBorderWidthAll(1);
        var progressFill = new StyleBoxFlat { BgColor = new Color("#e45d91") };
        _purgeProgress.AddThemeStyleboxOverride("background", progressTrack);
        _purgeProgress.AddThemeStyleboxOverride("fill", progressFill);
        objectiveVBox.AddChild(_purgeProgress);

        _controlsLabel = NewLabel("WASD MOVE | MOUSE AIM | F ATTACK | Q ABILITY | SHIFT DASH | E USE | ESC PAUSE", 12, Colors.White);
        _controlsLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        _controlsLabel.OffsetLeft = 20;
        _controlsLabel.OffsetTop = -34;
        _controlsLabel.OffsetRight = 1240;
        _controlsLabel.OffsetBottom = -10;
        root.AddChild(_controlsLabel);

        var dialoguePanel = NewPanel(new Vector2(-430, -112), new Vector2(860, 88), new Vector2(.5f, 1));
        root.AddChild(dialoguePanel);
        _dialogueLabel = NewLabel(DialogueFor(_player.ActiveProfile.DisplayName), 14, new Color("#ffe9c9"));
        _dialogueLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        dialoguePanel.AddChild(_dialogueLabel);
        _dialogueRemaining = 11f;

        _pausePanel = NewPanel(new Vector2(-240, -110), new Vector2(480, 220), new Vector2(.5f, .5f));
        _pausePanel.Visible = false;
        root.AddChild(_pausePanel);
        _pauseLabel = NewLabel("MISSION PAUSED\n\nPress ESC / START to resume", 22, new Color("#77efff"));
        _pauseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _pauseLabel.VerticalAlignment = VerticalAlignment.Center;
        _pausePanel.AddChild(_pauseLabel);
    }

    /// <summary>Returns the story's canonical Shogun line or a short response for another operative.</summary>
    private static string DialogueFor(string name) => name switch
    {
        "Cyber Shogun" => "KAITO: They do not just take our land; they delete our soul. We will save the story.",
        "Surya Prime" => "SURYA: Stone can fall and still be rebuilt. The people and their memory survive.",
        "Oceanus" => "OCEANUS: This garden remembers every season. The purge will not erase its roots.",
        "Mirage Blade" => "MIRAGE: Their scanners see ruins. They will not see the archive beneath them.",
        "Apex Titan" => "APEX: Let the walker crush stone. It will not crush the history carried by its people.",
        "Aura Phoenicia" => "AURA: Every name and every song endures. I will keep the archive alive.",
        _ => "AXIOM-7: Comm towers report a 98.7% detection probability. Retreat remains advisable."
    };

    /// <summary>Updates the main objective as the player enters each story sector.</summary>
    private void UpdateRoomObjective()
    {
        if (BeaconNeutralized)
        {
            CurrentObjective = DataBladeRecovered ? "Extract through the western garden gate" : "Recover the Data-Blade in the dojo core";
        }
        else if (_player.Position.Y < 800f && _player.Position.X > 3600f)
            CurrentObjective = "Destroy the demolition beacon in the Terra-Walker yard";
        else if (_player.Position.Y > 800f && _player.Position.X > 3600f)
            CurrentObjective = "Locate the demolition beacon beyond the archive";
        else if (_player.Position.X > 2400f)
            CurrentObjective = "Cross the cherry-blossom walk and reach the archive";
        else if (_player.Position.X > 1200f)
            CurrentObjective = "Fight through the Lantern Court";
        else CurrentObjective = "Infiltrate the Eternal Zen Dojo";

        _objectiveLabel.Text = "OBJECTIVE  /  " + CurrentObjective.ToUpperInvariant();
    }

    /// <summary>Stores safe positions as the operative passes the archive and reaches the dojo core.</summary>
    private void UpdateCheckpoints()
    {
        if (!_firstCheckpointReached && _player.Position.X > 3500f && _player.Position.Y > 800f)
        {
            _firstCheckpointReached = true;
            _checkpoint = new Vector2(3660f, 1190f);
            ShowDialogue("AXIOM-7: Archive passage secured. Checkpoint data synchronized.");
        }
        if (!_secondCheckpointReached && _player.Position.X < 3600f && _player.Position.Y < 800f)
        {
            _secondCheckpointReached = true;
            _checkpoint = new Vector2(3470f, 660f);
            ShowDialogue("The dojo core is ahead. The archive seal is holding.");
        }
    }

    /// <summary>Resolves optional lore, the beacon-gated Data-Blade, and the final extraction prompt.</summary>
    /// <summary>Performs the nearest permitted memory, archive, or extraction interaction.</summary>
    public void InteractWithCurrentObjective()
    {
        foreach (var fragment in _memoryFragments)
        {
            if (fragment.Visible && _player.GlobalPosition.DistanceTo(fragment.GlobalPosition) < 95f)
            {
                fragment.SetCollected();
                MemoryFragmentsFound++;
                _gameManager.Points += 25;
                ShowDialogue(MemoryFragmentsFound switch
                {
                    1 => "ARCHIVE LOG: The garden once marked the changing of each imperial season.",
                    2 => "ARCHIVE LOG: The dojo sheltered historians during the first occupation.",
                    _ => "ARCHIVE LOG: The Data-Blade contains the testimony of every erased district."
                });
                return;
            }
        }
        if (!BeaconNeutralized && _player.GlobalPosition.DistanceTo(_beacon.GlobalPosition) < 150f)
        {
            ShowDialogue("Disable the demolition beacon with aimed attacks before entering the dojo core.");
            return;
        }
        if (!DataBladeRecovered && BeaconNeutralized && _player.GlobalPosition.DistanceTo(_dataBlade.GlobalPosition) < 115f)
        {
            DataBladeRecovered = true;
            _dataBlade.SetCollected();
            UpdateRoomObjective();
            ShowDialogue("DATA-BLADE SECURED. AXIOM-7: Enforcer signatures are converging on your position.");
            SpawnCoreReinforcements();
            return;
        }
        if (DataBladeRecovered && _player.GlobalPosition.DistanceTo(_extraction.GlobalPosition) < 125f)
            CompleteMission();
    }

    /// <summary>Raises the reinforcements foreshadowed by AXIOM-7 after the purge beacon is disabled.</summary>
    private void SpawnCoreReinforcements()
    {
        foreach (Vector2 point in new[] { new Vector2(2780, 460), new Vector2(3220, 760), new Vector2(3440, 330) })
        {
            var drone = new IsometricPurgeDrone { Name = "ReroutedEnforcer", Position = point, MaxHealth = 125f, HuntSpeed = 125f, DroneColor = new Color("#db546b") };
            drone.Defeated += () => _gameManager.AddPoint();
            _actors.AddChild(drone);
        }
    }

    /// <summary>Updates the mission HUD and saves the normal campaign completion before advancing.</summary>
    private async void CompleteMission()
    {
        if (_finishing || !DataBladeRecovered) return;
        _finishing = MissionComplete = true;
        _player.StopMovement();
        _extraction.Visible = false;
        _objectiveLabel.Text = "MISSION COMPLETE  /  THE ETERNAL MEMORY";
        _purgeLabel.Text = "DEMOLITION STALLED  /  DATA-BLADE SECURED";
        _dialogueLabel.Text = "The dojo falls, but the resistance rises. Tokyo remembers.";
        _dialogueLabel.Visible = true;
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Call("unlock_next_level", 1);
        _gameManager.SaveScore("level1");
        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree()) GetTree().ChangeSceneToFile(LevelTwoPath);
    }

    /// <summary>Stalls the purge field and advances the objective after the beacon reaches zero health.</summary>
    private void OnBeaconNeutralized()
    {
        BeaconNeutralized = true;
        _sweepBeam.Disabled = true;
        _beacon.Visible = false;
        UpdateRoomObjective();
        ShowDialogue("AXIOM-7: Beacon neutralized. The demolition field is offline; Enforcer Legion rerouted.");
    }

    /// <summary>Schedules a checkpoint respawn so the inherited player defeat signal remains safe in physics.</summary>
    private void OnPlayerDefeated() => RespawnAfterDelay();

    /// <summary>Restores health and mission-local movement after the brief defeat transition.</summary>
    private async void RespawnAfterDelay()
    {
        await ToSignal(GetTree().CreateTimer(.9f), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree() || _player == null || !GodotObject.IsInstanceValid(_player)) return;
        _player.ResetAtCheckpoint(_checkpoint);
        _player.ResetIsometricTransientState();
        ShowDialogue("Checkpoint restored. The purge field continues, but the mission has no time limit.");
    }

    /// <summary>Shows a short narrative or status line without obstructing the combat HUD.</summary>
    private void ShowDialogue(string text)
    {
        _dialogueLabel.Text = text;
        _dialogueLabel.Visible = true;
        _dialogueRemaining = 8f;
    }

    /// <summary>Freezes only the isometric world while keeping the mission HUD and resume input active.</summary>
    private void SetPaused(bool paused)
    {
        if (MissionComplete || _paused == paused) return;
        _paused = paused;
        _actors.ProcessMode = paused ? ProcessModeEnum.Pausable : ProcessModeEnum.Inherit;
        _collisionRoot.ProcessMode = paused ? ProcessModeEnum.Pausable : ProcessModeEnum.Inherit;
        GetTree().Paused = paused;
        _pausePanel.Visible = paused;
    }

    /// <summary>Creates a themed anchored panel for the mission-only objective HUD.</summary>
    private PanelContainer NewPanel(Vector2 offset, Vector2 size, Vector2 anchor)
    {
        var panel = new PanelContainer
        {
            Position = offset,
            Size = size,
            AnchorLeft = anchor.X,
            AnchorRight = anchor.X,
            AnchorTop = anchor.Y,
            AnchorBottom = anchor.Y,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(.015f, .025f, .06f, .88f),
            BorderColor = new Color(.1f, .84f, 1f, .92f),
            ShadowColor = new Color(.1f, .84f, 1f, .22f),
            ShadowSize = 6,
            ContentMarginLeft = 14,
            ContentMarginTop = 10,
            ContentMarginRight = 14,
            ContentMarginBottom = 10
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(10);
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    /// <summary>Creates readable text using the game's native sci-fi typeface and a safe fallback.</summary>
    private Label NewLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        var font = GD.Load<FontFile>("res://assets/fonts/Orbitron-VariableFont.ttf");
        if (font != null) label.AddThemeFontOverride("font", font);
        return label;
    }

    /// <summary>Restores the global cursor policy when leaving this mission scene.</summary>
    public override void _ExitTree() => Input.MouseMode = Input.MouseModeEnum.Hidden;
}
