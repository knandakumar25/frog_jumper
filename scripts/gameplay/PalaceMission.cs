using Godot;
using System.Collections.Generic;

/// <summary>
/// Builds and directs Level 1, "Ghost in the Gridlock", as a side-scrolling 2D platformer.
/// <para>
/// The route runs east through six beats: Garden Gate (jump basics) → Lantern Court (terraces, a timed laser bridge)
/// → Blossom Gorge (a wall-jump climb) → Rooftop Run (gaps, a moving platform, sentry fire) → Archive Descent
/// (hazard floor, laser tripwire) → Beacon Court (destroy the beacon, open the dojo gate, take the Data-Blade, extract).
/// </para>
/// All geometry, enemies, pickups and UI are created in code; the scene file only attaches this script.
/// Y grows downward, so "up" in this class's comments means a smaller Y value.
/// </summary>
public partial class PalaceMission : Node2D
{
    /// <summary>The selected operative, exposed for scene integration checks.</summary>
    public PalaceOperative Player => _player;
    /// <summary>True once the demolition beacon has been destroyed.</summary>
    public bool BeaconNeutralized { get; private set; }
    /// <summary>True once the operative has taken the Data-Blade from its pedestal.</summary>
    public bool DataBladeRecovered { get; private set; }
    /// <summary>True once extraction has begun and the campaign is advancing.</summary>
    public bool MissionComplete { get; private set; }
    /// <summary>Optional archive memory fragments found so far (maximum <see cref="FragmentTotal"/>).</summary>
    public int MemoryFragmentsFound { get; private set; }
    /// <summary>Number of optional fragments hidden in the level.</summary>
    public int FragmentTotal => _fragments.Count;
    /// <summary>The objective line currently shown in the HUD.</summary>
    public string CurrentObjective { get; private set; } = "Infiltrate the Eternal Zen Dojo";
    /// <summary>World position the operative respawns at after a defeat or a fall.</summary>
    public Vector2 CheckpointPosition => _checkpoint;

    // ─── Layout constants ────────────────────────────────────────────────────
    /// <summary>Left edge of the playable world; an invisible-feeling cliff wall stops the player here.</summary>
    private const int WorldLeft = 0;
    /// <summary>Right edge of the playable world, just past the extraction portal.</summary>
    private const int WorldRight = 9460;
    /// <summary>Highest point the camera can show (the rooftop sentries and wall-jump shaft).</summary>
    private const int WorldTop = -1300;
    /// <summary>Lowest point the camera can show; chasms look empty below this.</summary>
    private const int WorldBottom = 820;
    /// <summary>Falling below this Y counts as falling into a chasm and sends the player to the checkpoint.</summary>
    private const float KillPlaneY = 950f;
    /// <summary>The player's origin sits this far above its feet because the collision box is centered on the body.</summary>
    private const float FeetOffset = 46f;
    private const string LevelTwoPath = "res://scenes/gameplay/level2.tscn";

    // ─── Runtime references ──────────────────────────────────────────────────
    private PalaceOperative _player = null!;
    private Camera2D _camera = null!;
    private Node2D _world = null!;
    private Node2D _actors = null!;
    private GameManager _gameManager = null!;
    private PalaceObjective _beacon = null!;
    private PalaceObjective _dataBlade = null!;
    private PalaceObjective _extraction = null!;
    private PalacePlatform _dojoGate = null!;
    private Sprite2D _backdrop = null!;
    private Label _objectiveLabel = null!;
    private Label _fragmentLabel = null!;
    private Label _dialogueLabel = null!;
    private Control _dialoguePanel = null!;
    private Label _controlsLabel = null!;
    private readonly List<PalaceObjective> _fragments = new();
    private readonly List<PalaceObjective> _checkpoints = new();
    private Vector2 _checkpoint = new(200f, 600f - FeetOffset);
    private float _dialogueRemaining;
    private bool _finishing;
    private bool _recovering;
    private bool _bladeHintShown;

    /// <summary>Creates the backdrop, geometry, operative, enemies, objectives, HUD and pause menu.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Set("reached_finish", false);
        GetTree().Paused = false;
        global?.Call("stop_menu_music");

        AddChild(new WorldEnvironment { Environment = GD.Load<Godot.Environment>("res://assets/themes/cyber_glow.tres") });
        BuildBackdrop();

        _world = new Node2D { Name = "PalaceGeometry", ProcessMode = ProcessModeEnum.Pausable };
        AddChild(_world);
        _actors = new Node2D { Name = "Actors", ProcessMode = ProcessModeEnum.Pausable };
        AddChild(_actors);

        BuildGeometry();
        BuildPlayer();

        _gameManager = new GameManager { Name = "GameManager" };
        AddChild(_gameManager);
        var combatHud = GD.Load<PackedScene>("res://scenes/gameplay/cyber_combat_hud.tscn").Instantiate<CanvasLayer>();
        combatHud.Name = "CyberCombatHud";
        AddChild(combatHud);

        BuildObjectives();
        BuildCheckpoints();
        BuildFragments();
        BuildHazards();
        SpawnSentries();
        BuildPetals();
        BuildUserInterface();
        BuildAudioAndPause();
        ShowDialogue(DialogueFor(_player.ActiveProfile.DisplayName), 11f);
        GD.Print("PalaceMission: Ghost in the Gridlock platformer ready for " + _player.ActiveProfile.DisplayName);
    }

    // ─── World construction ──────────────────────────────────────────────────

    /// <summary>Adds the painted night-garden image on its own canvas layer so it can scroll slower than the world.</summary>
    private void BuildBackdrop()
    {
        var layer = new CanvasLayer { Name = "BackdropLayer", Layer = -10 };
        AddChild(layer);
        _backdrop = new Sprite2D
        {
            Name = "PalaceGardenBackdrop",
            Texture = GD.Load<Texture2D>("res://assets/images/level1/palace_garden_painted.png"),
            // The painting is 2149x732; scaling to ~1700px tall means it still covers a full screen after parallax shifts.
            Scale = Vector2.One * 2.35f,
            // Dim it so bright lanterns and the cyan platform rims stay readable on top.
            Modulate = new Color(.78f, .8f, .95f),
            TextureFilter = CanvasItem.TextureFilterEnum.Linear
        };
        layer.AddChild(_backdrop);
        _backdrop.Position = new Vector2(960f, 540f);
    }

    /// <summary>Keeps the backdrop sliding at a fraction of camera speed (parallax), clamped so no edge is ever exposed.</summary>
    private void UpdateBackdrop()
    {
        if (_camera == null || _backdrop.Texture == null) return;
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 half = _backdrop.Texture.GetSize() * _backdrop.Scale * .5f;
        Vector2 center = _camera.GetScreenCenterPosition();
        float x = viewport.X * .5f - (center.X - viewport.X * .5f) * .17f;
        float y = viewport.Y * .5f - (center.Y + 250f) * .1f + 60f;
        // The image must always span the whole viewport: its edges can't move inside the screen edges.
        x = Mathf.Clamp(x, viewport.X - half.X, half.X);
        y = Mathf.Clamp(y, viewport.Y - half.Y, half.Y);
        _backdrop.Position = new Vector2(x, y);
    }

    /// <summary>Adds one solid platform. <paramref name="top"/> is the walkable surface Y; the body extends downward by <paramref name="height"/>.</summary>
    private PalacePlatform AddPlatform(float x, float top, float width, float height, PalaceSurface surface = PalaceSurface.Stone, bool oneWay = false, string? name = null)
    {
        var platform = new PalacePlatform { Position = new Vector2(x, top), Size = new Vector2(width, height), Surface = surface, OneWay = oneWay };
        if (name != null) platform.Name = name;
        _world.AddChild(platform);
        return platform;
    }

    /// <summary>
    /// Lays out the whole route. Every vertical step is ≤ 150 px (a Shogun jump clears ~200 px and the heaviest
    /// operative ~177 px) and every gap is ≤ 220 px (the shortest running jump is ~256 px), so all six operatives can finish.
    /// </summary>
    private void BuildGeometry()
    {
        // Zone A — Garden Gate: open ground, a low planter to hop, then a river gap with a stepping stone.
        AddPlatform(-100, 600, 1200, 700);
        AddPlatform(520, 540, 160, 60);
        AddPlatform(1230, 612, 70, 60);
        AddPlatform(1420, 600, 520, 700);
        // Cliff wall at the world's west edge so the player can't walk off the start.
        AddPlatform(-160, WorldTop, 160, 2400, PalaceSurface.Wall);

        // Zone B — Lantern Court: two terraces up, an optional blossom perch, then a timber bridge under a laser.
        AddPlatform(1940, 500, 420, 800);
        AddPlatform(2360, 400, 340, 900);
        AddPlatform(2010, 350, 150, 22, PalaceSurface.Wood, true, "FragmentPerchLantern");
        AddPlatform(2700, 400, 440, 26, PalaceSurface.Wood, true, "LanternBridge");
        AddPlatform(2896, 196, 48, 28, PalaceSurface.Stone, false, "LaserBracket");

        // Zone C — Blossom Gorge: a 240 px wide shaft. Alternating timber ledges every 150 px let anyone climb it,
        // while confident players can skip them with wall jumps off the carved walls.
        AddPlatform(3140, 400, 330, 900, PalaceSurface.Stone, false, "GorgeFloor");
        AddPlatform(3140, -820, 90, 1070, PalaceSurface.Wall, false, "GorgeWestWall");
        AddPlatform(3470, -640, 90, 1040, PalaceSurface.Wall, false, "GorgeEastWall");
        float[] ledgeY = { 250, 100, -50, -200, -350, -500 };
        for (int i = 0; i < ledgeY.Length; i++)
            AddPlatform(i % 2 == 0 ? 3340 : 3230, ledgeY[i], 130, 22, PalaceSurface.Wood, true, $"GorgeLedge{i + 1}");

        // Zone D — Rooftop Run: tiled roofs with gaps, a high perch and a moving platform over the widest gap.
        AddPlatform(3560, -640, 560, 90, PalaceSurface.Roof);
        AddPlatform(4300, -700, 360, 90, PalaceSurface.Roof);
        AddPlatform(4380, -850, 140, 22, PalaceSurface.Wood, true, "FragmentPerchRoof");
        AddPlatform(4860, -640, 300, 90, PalaceSurface.Roof);
        var mover = new PalaceMovingPlatform { Name = "SkyFerry", Position = new Vector2(5200, -640), Travel = new Vector2(240, 0), Period = 5.5f };
        _world.AddChild(mover);
        AddPlatform(5640, -640, 420, 90, PalaceSurface.Roof);

        // Zone E — Archive Descent: shelves step down into the archive. Falling off simply sends you back to a checkpoint.
        float[] shelfY = { -520, -400, -280, -160 };
        for (int i = 0; i < shelfY.Length; i++)
            AddPlatform(6120 + i * 220, shelfY[i], 160, 22, PalaceSurface.Wood, true, $"ArchiveShelf{i + 1}");
        AddPlatform(7560, -150, 140, 22, PalaceSurface.Wood, true, "FragmentPerchArchive");

        // Zone F — Archive floor and Beacon Court share one long floor; the beacon sits on a low dais.
        AddPlatform(6960, 0, 2400, 900, PalaceSurface.Stone, false, "ArchiveCourtFloor");
        AddPlatform(8420, -60, 160, 60, PalaceSurface.Stone, false, "BeaconDais");
        // The dojo gate is tall enough (it runs above the camera ceiling) that wall-jumping can't bypass it.
        _dojoGate = AddPlatform(8900, WorldTop, 40, -WorldTop, PalaceSurface.Gate, false, "DojoGate");
        AddPlatform(9360, WorldTop, 100, 1400, PalaceSurface.Wall, false, "EastWall");
    }

    /// <summary>Instantiates the selected operative, adds the follow camera and connects defeat handling.</summary>
    private void BuildPlayer()
    {
        _player = GD.Load<PackedScene>("res://scenes/gameplay/palace_operative.tscn").Instantiate<PalaceOperative>();
        // The combat HUD finds the player by this exact node name.
        _player.Name = "CyberShogun";
        _player.Position = _checkpoint;
        _player.PlayerDefeated += OnPlayerDefeated;
        _actors.AddChild(_player);

        _camera = new Camera2D
        {
            Name = "Camera2D",
            LimitLeft = WorldLeft,
            LimitRight = WorldRight,
            LimitTop = WorldTop,
            LimitBottom = WorldBottom,
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed = 7f,
            // Look slightly ahead and above the player so landing spots below stay visible.
            Offset = new Vector2(0f, -80f),
            Zoom = Vector2.One * .95f
        };
        // The player is scaled 1.35x; compensate so the camera behaves as if on an unscaled parent.
        _camera.Scale = Vector2.One / _player.Scale.X;
        _player.AddChild(_camera);
        _camera.MakeCurrent();
    }

    /// <summary>Places the beacon, Data-Blade, extraction portal and the door that seals the dojo core.</summary>
    private void BuildObjectives()
    {
        _beacon = new PalaceObjective { Name = "DemolitionBeacon", Kind = "Beacon", Position = new Vector2(8500, -60) };
        _beacon.Neutralized += OnBeaconNeutralized;
        _actors.AddChild(_beacon);
        _dataBlade = new PalaceObjective { Name = "DataBlade", Kind = "Blade", Position = new Vector2(9080, 0) };
        _actors.AddChild(_dataBlade);
        _extraction = new PalaceObjective { Name = "ExtractionGate", Kind = "Exit", Position = new Vector2(9250, 0) };
        _actors.AddChild(_extraction);
    }

    /// <summary>Lights shrines at the gorge summit, the end of the moving-platform crossing, and the archive exit.</summary>
    private void BuildCheckpoints()
    {
        foreach (var (name, x, y) in new[] { ("ShrineGorge", 3700f, -640f), ("ShrineRooftop", 5780f, -640f), ("ShrineCourt", 7080f, 0f) })
        {
            var shrine = new PalaceObjective { Name = name, Kind = "Checkpoint", Position = new Vector2(x, y) };
            _checkpoints.Add(shrine);
            _actors.AddChild(shrine);
        }
    }

    /// <summary>Hides three optional fragments on perches that reward a small detour; none gate progress.</summary>
    private void BuildFragments()
    {
        foreach (var point in new[] { new Vector2(2085, 350), new Vector2(4450, -850), new Vector2(7630, -150) })
        {
            var fragment = new PalaceObjective { Name = "MemoryFragment", Kind = "Fragment", Position = point };
            _fragments.Add(fragment);
            _actors.AddChild(fragment);
        }
    }

    /// <summary>Adds the cycling laser grids and surge floor from the shared hazard scenes.</summary>
    private void BuildHazards()
    {
        var hazards = new Node2D { Name = "Hazards" };
        _actors.AddChild(hazards);
        // Tall laser across the bridge: wait for the off-phase, then run through.
        var bridgeLaser = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        bridgeLaser.Position = new Vector2(2920, 224);
        bridgeLaser.BeamLength = 176f;
        bridgeLaser.ActiveDuration = 2.0f;
        bridgeLaser.InactiveDuration = 2.2f;
        hazards.AddChild(bridgeLaser);
        // Low tripwire in the archive: short enough to simply jump over.
        var tripwire = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        tripwire.Position = new Vector2(7660, -88);
        tripwire.BeamLength = 88f;
        tripwire.AlwaysActive = true;
        tripwire.Damage = 15f;
        hazards.AddChild(tripwire);
        // Surge plate; placed a few pixels above the floor so the player's feet overlap its damage area.
        var surge = GD.Load<PackedScene>("res://scenes/gameplay/cyber_hazard_floor_2d.tscn").Instantiate<CyberHazardFloor2D>();
        surge.Position = new Vector2(7280, -8);
        surge.FloorWidth = 180f;
        surge.ActiveDuration = 2.4f;
        surge.InactiveDuration = 2.0f;
        hazards.AddChild(surge);
    }

    /// <summary>Hovering demolition sentries, hand-placed so each zone gets a new threat angle.</summary>
    private void SpawnSentries()
    {
        var positions = new[] {
            new Vector2(960, 330), new Vector2(2140, 150), new Vector2(2520, 40),
            new Vector2(3850, -900), new Vector2(4620, -920), new Vector2(5420, -900),
            new Vector2(7400, -330), new Vector2(7840, -330),
            new Vector2(8240, -320), new Vector2(8700, -330), new Vector2(8500, -470)
        };
        for (int i = 0; i < positions.Length; i++) AddSentry($"PurgeSentry{i + 1:00}", positions[i], i >= 8 ? 90f : 70f);
    }

    /// <summary>Creates one sentry bound to this mission's player and score.</summary>
    private void AddSentry(string name, Vector2 position, float health)
    {
        var sentry = new PalaceSentry { Name = name, Position = position, MaxHealth = health, Target = _player };
        sentry.Defeated += () => _gameManager.AddPoint();
        _actors.AddChild(sentry);
    }

    /// <summary>Adds drifting blossom petals that follow the camera for atmosphere.</summary>
    private void BuildPetals()
    {
        _camera.AddChild(new CpuParticles2D
        {
            Name = "Petals",
            Position = new Vector2(0, -620),
            Amount = 70,
            Lifetime = 9f,
            Preprocess = 9f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(1300, 10),
            Gravity = new Vector2(-45, 70),
            InitialVelocityMin = 10f,
            InitialVelocityMax = 40f,
            Direction = new Vector2(-1, 1),
            Spread = 30f,
            Color = new Color(.95f, .6f, .78f, .8f),
            ScaleAmountMin = 1.8f,
            ScaleAmountMax = 3.5f,
            ZIndex = 20
        });
    }

    // ─── Per-frame logic ─────────────────────────────────────────────────────

    /// <summary>Scrolls the backdrop, ticks the dialogue timer and keeps the controls hint matched to the active device.</summary>
    public override void _Process(double delta)
    {
        UpdateBackdrop();
        if (MissionComplete) return;
        _dialogueRemaining = Mathf.Max(0f, _dialogueRemaining - (float)delta);
        _dialoguePanel.Visible = _dialogueRemaining > 0f;
    }

    /// <summary>Runs checkpoint, pickup, pit and objective logic after the player has moved each physics tick.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (MissionComplete || _player == null) return;
        UpdateCheckpoints();
        CollectNearbyObjectives();
        UpdateObjective();
        if (_player.GlobalPosition.Y > KillPlaneY) RecoverFromFall();
    }

    /// <summary>Switches the on-screen hint between keyboard and gamepad as the player changes device.</summary>
    public override void _Input(InputEvent @event)
    {
        if (_controlsLabel == null) return;
        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
            _controlsLabel.Text = "L STICK MOVE | A JUMP | X ATTACK | Y ABILITY | RB DASH | START PAUSE";
        else if (@event is InputEventKey)
            _controlsLabel.Text = "A / D MOVE | SPACE JUMP | F ATTACK | Q ABILITY | SHIFT DASH | ESC PAUSE";
    }

    /// <summary>Lights and stores the checkpoint when the player reaches an unlit shrine.</summary>
    private void UpdateCheckpoints()
    {
        foreach (var shrine in _checkpoints)
        {
            if (shrine.Activated) continue;
            Vector2 offset = _player.GlobalPosition - shrine.GlobalPosition;
            if (Mathf.Abs(offset.X) > 90f || offset.Y > 20f || offset.Y < -220f) continue;
            shrine.Activate();
            _checkpoint = shrine.GlobalPosition + new Vector2(0f, -FeetOffset - 4f);
            ShowDialogue("AXIOM-7: Recovery point synchronized. You will return here if you fall.", 5f);
        }
    }

    /// <summary>
    /// Walk-into pickups: fragments always, the Data-Blade once the beacon is down, and the exit once the Blade is held.
    /// Public so integration tests can place the player and trigger a pass deterministically.
    /// </summary>
    public void CollectNearbyObjectives()
    {
        foreach (var fragment in _fragments)
        {
            if (fragment.Collected || _player.GlobalPosition.DistanceTo(fragment.GlobalPosition + new Vector2(0, -40)) > 80f) continue;
            fragment.SetCollected();
            MemoryFragmentsFound++;
            _gameManager.Points += 25;
            ShowDialogue(MemoryFragmentsFound switch
            {
                1 => "ARCHIVE LOG: The garden once marked the changing of each imperial season.",
                2 => "ARCHIVE LOG: The dojo sheltered historians during the first occupation.",
                _ => "ARCHIVE LOG: The Data-Blade holds the testimony of every erased district."
            }, 8f);
            UpdateFragmentLabel();
        }

        float bladeDistance = _player.GlobalPosition.DistanceTo(_dataBlade.GlobalPosition + new Vector2(0, -60));
        if (!DataBladeRecovered && bladeDistance < 110f)
        {
            if (!BeaconNeutralized)
            {
                if (!_bladeHintShown) ShowDialogue("The Data-Blade is sealed. Destroy the demolition beacon first.", 5f);
                _bladeHintShown = true;
            }
            else
            {
                DataBladeRecovered = true;
                _dataBlade.SetCollected();
                ShowDialogue("DATA-BLADE SECURED. AXIOM-7: Enforcer signatures converging. Reach the extraction gate!", 8f);
                SpawnReinforcements();
                UpdateObjective();
            }
        }

        if (DataBladeRecovered && _player.GlobalPosition.DistanceTo(_extraction.GlobalPosition + new Vector2(0, -80)) < 120f)
            CompleteMission();
    }

    /// <summary>Raises the enforcers AXIOM-7 warned about once the Blade is taken.</summary>
    private void SpawnReinforcements()
    {
        AddSentry("ReroutedEnforcer1", new Vector2(8980, -330), 100f);
        AddSentry("ReroutedEnforcer2", new Vector2(9170, -360), 100f);
    }

    /// <summary>Chooses the HUD objective from the beacon/blade state and how far the player has advanced.</summary>
    private void UpdateObjective()
    {
        float x = _player.GlobalPosition.X;
        if (DataBladeRecovered) CurrentObjective = "Reach the extraction gate";
        else if (BeaconNeutralized) CurrentObjective = "Take the Data-Blade from the dojo core";
        else if (x > 7000f) CurrentObjective = "Destroy the demolition beacon";
        else if (x > 5600f) CurrentObjective = "Descend into the archive";
        else if (x > 3560f) CurrentObjective = "Run the palace rooftops";
        else if (x > 3100f) CurrentObjective = "Climb the Blossom Gorge";
        else if (x > 1500f) CurrentObjective = "Cross the Lantern Court";
        else CurrentObjective = "Infiltrate the Eternal Zen Dojo";
        _objectiveLabel.Text = "OBJECTIVE  /  " + CurrentObjective.ToUpperInvariant();
    }

    /// <summary>Returns a fallen operative to the last checkpoint with a small shield/health penalty instead of a full defeat.</summary>
    private void RecoverFromFall()
    {
        if (_recovering) return;
        _recovering = true;
        _player.ResetAtCheckpoint(_checkpoint);
        // ResetAtCheckpoint heals fully, so the penalty is applied afterwards. It is small enough never to defeat anyone.
        _player.TakeDamage(15f);
        ShowDialogue("You fell into the chasm. AXIOM-7 rerouted you to the last recovery point.", 4f);
        _recovering = false;
    }

    /// <summary>Opens the dojo gate and rewards the player after the beacon's integrity reaches zero.</summary>
    private void OnBeaconNeutralized()
    {
        BeaconNeutralized = true;
        _dojoGate.Open();
        UpdateObjective();
        ShowDialogue("AXIOM-7: Beacon neutralized. The dojo gate is unsealed. Take the Data-Blade!", 8f);
    }

    /// <summary>Delays the respawn briefly so the defeat reads, and keeps the physics callback safe.</summary>
    private void OnPlayerDefeated() => RespawnAfterDelay();

    /// <summary>Restores the operative at the checkpoint after a short pause.</summary>
    private async void RespawnAfterDelay()
    {
        await ToSignal(GetTree().CreateTimer(.9f), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree() || _player == null || !GodotObject.IsInstanceValid(_player) || MissionComplete) return;
        _player.ResetAtCheckpoint(_checkpoint);
        ShowDialogue("Systems restored at the last recovery point.", 4f);
    }

    /// <summary>Saves progress, unlocks Level 2 and changes scene after a short victory beat.</summary>
    private async void CompleteMission()
    {
        if (_finishing || !DataBladeRecovered) return;
        _finishing = MissionComplete = true;
        _player.StopMovement();
        CurrentObjective = "Mission complete";
        _objectiveLabel.Text = "MISSION COMPLETE  /  THE ETERNAL MEMORY";
        ShowDialogue("The dojo falls, but the resistance rises. Tokyo remembers.", 6f);
        _dialoguePanel.Visible = true;
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Call("unlock_next_level", 1);
        _gameManager.SaveScore("level1");
        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree()) GetTree().ChangeSceneToFile(LevelTwoPath);
    }

    // ─── User interface ──────────────────────────────────────────────────────

    /// <summary>Builds the objective panel, fragment counter, dialogue strip and controls hint.</summary>
    private void BuildUserInterface()
    {
        var hud = new CanvasLayer { Name = "MissionHUD", Layer = 5 };
        AddChild(hud);
        var root = new Control { Name = "HUDRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        hud.AddChild(root);

        var objectivePanel = NewPanel(new Vector2(-430, 18), new Vector2(410, 84), new Vector2(1, 0));
        root.AddChild(objectivePanel);
        var box = new VBoxContainer();
        objectivePanel.AddChild(box);
        _objectiveLabel = NewLabel("OBJECTIVE  /  INFILTRATE THE ETERNAL ZEN DOJO", 15, new Color("#77efff"));
        box.AddChild(_objectiveLabel);
        _fragmentLabel = NewLabel("", 13, new Color("#ffcf8a"));
        box.AddChild(_fragmentLabel);
        UpdateFragmentLabel();

        _controlsLabel = NewLabel("A / D MOVE | SPACE JUMP | F ATTACK | Q ABILITY | SHIFT DASH | ESC PAUSE", 12, Colors.White);
        _controlsLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        _controlsLabel.OffsetLeft = 20;
        _controlsLabel.OffsetTop = -34;
        _controlsLabel.OffsetRight = 1240;
        _controlsLabel.OffsetBottom = -10;
        root.AddChild(_controlsLabel);

        _dialoguePanel = NewPanel(new Vector2(-430, -116), new Vector2(860, 80), new Vector2(.5f, 1));
        root.AddChild(_dialoguePanel);
        _dialogueLabel = NewLabel("", 14, new Color("#ffe9c9"));
        _dialoguePanel.AddChild(_dialogueLabel);
        _dialoguePanel.Visible = false;
    }

    /// <summary>Refreshes the optional-fragment counter shown under the objective.</summary>
    private void UpdateFragmentLabel() => _fragmentLabel.Text = $"MEMORY FRAGMENTS  {MemoryFragmentsFound} / {FragmentTotal}";

    /// <summary>Starts the stage music and instantiates the shared pause menu used by the other levels.</summary>
    private void BuildAudioAndPause()
    {
        var music = new AudioStreamPlayer
        {
            Name = "AudioStreamPlayer",
            Stream = GD.Load<AudioStream>("res://assets/music/Sakura-Girl-Yay-chosic.com_.mp3"),
            VolumeDb = 24f,
            Autoplay = true
        };
        AddChild(music);
        var pauseLayer = new CanvasLayer { Name = "CanvasLayer" };
        AddChild(pauseLayer);
        pauseLayer.AddChild(GD.Load<PackedScene>("res://scenes/gameplay/pause_menu.tscn").Instantiate());
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

    /// <summary>Shows a status or story line in the bottom dialogue strip for <paramref name="seconds"/>.</summary>
    private void ShowDialogue(string text, float seconds)
    {
        _dialogueLabel.Text = text;
        _dialogueRemaining = seconds;
        _dialoguePanel.Visible = true;
    }

    /// <summary>Creates an anchored translucent panel in the game's cyan-edged HUD style.</summary>
    private static PanelContainer NewPanel(Vector2 offset, Vector2 size, Vector2 anchor)
    {
        var panel = new PanelContainer
        {
            Position = offset, Size = size,
            AnchorLeft = anchor.X, AnchorRight = anchor.X, AnchorTop = anchor.Y, AnchorBottom = anchor.Y,
            OffsetLeft = offset.X, OffsetTop = offset.Y, OffsetRight = offset.X + size.X, OffsetBottom = offset.Y + size.Y,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(.015f, .025f, .06f, .88f),
            BorderColor = new Color(.1f, .84f, 1f, .92f),
            ShadowColor = new Color(.1f, .84f, 1f, .22f),
            ShadowSize = 6,
            ContentMarginLeft = 14, ContentMarginTop = 10, ContentMarginRight = 14, ContentMarginBottom = 10
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(10);
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    /// <summary>Creates a wrapped label using the game's sci-fi typeface when available.</summary>
    private static Label NewLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        var font = GD.Load<FontFile>("res://assets/fonts/Orbitron-VariableFont.ttf");
        if (font != null) label.AddThemeFontOverride("font", font);
        return label;
    }

    /// <summary>Leaves the tree unpaused so a scene change never inherits a paused state.</summary>
    public override void _ExitTree()
    {
        if (GetTree() != null) GetTree().Paused = false;
    }
}
