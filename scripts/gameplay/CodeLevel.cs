using Godot;
using System.Collections.Generic;

/// <summary>
/// Base class for the campaign's code-built side-scrolling missions. It owns everything that is the same in every
/// level — backdrop and parallax, world/actor containers, the squad-aware player and camera, combat HUD, mission HUD
/// (objective, fragment counter, controls hint, squad strip, dialogue, prompt), checkpoint shrines, chasm recovery,
/// optional memory fragments, music, pause menu and the move to the next mission — and leaves layout, enemies and the
/// objective chain to subclasses through the virtual members below.
/// <para>
/// Y grows downward, so "up" means a smaller Y. Every vertical step in a level should be ≤ 150 px and every gap ≤ 220 px
/// so that all six operatives can make the jumps (a Shogun jump clears ~200 px, the heaviest operative ~177 px).
/// </para>
/// </summary>
public abstract partial class CodeLevel : Node2D
{
    // ─── Level definition (override in subclasses) ───────────────────────────
    /// <summary>This mission's number in <see cref="LevelCatalog"/>.</summary>
    protected abstract int MissionNumber { get; }
    /// <summary>Playable world bounds the camera is limited to (left, top, right, bottom).</summary>
    protected virtual Rect2I WorldBounds => new(0, -1300, 9460, 2120);
    /// <summary>Where the operative starts and respawns before any shrine is reached (the player's origin, not their feet).</summary>
    protected abstract Vector2 StartPosition { get; }
    /// <summary>Path of a painted backdrop image, or null to use a procedural <see cref="SkylineBackdrop"/>.</summary>
    protected virtual string? BackdropPath => null;
    /// <summary>Palette used by the procedural skyline when no painted backdrop is set.</summary>
    protected virtual SkylinePalette SkylinePalette => SkylinePalette.Night;
    /// <summary>Colour multiplied onto every platform so each level gets its own mood without new art.</summary>
    protected virtual Color PlatformTint => Colors.White;
    /// <summary>Music for the level.</summary>
    protected virtual string MusicPath => "res://assets/music/Sakura-Girl-Yay-chosic.com_.mp3";
    /// <summary>Shrine checkpoints as (name, bottom-centre position).</summary>
    protected virtual IEnumerable<(string Name, Vector2 Position)> Shrines => System.Array.Empty<(string, Vector2)>();
    /// <summary>Hidden memory fragments (bottom-centre positions).</summary>
    protected virtual IEnumerable<Vector2> FragmentPositions => System.Array.Empty<Vector2>();
    /// <summary>Builds platforms and gates. Called before the player exists.</summary>
    protected abstract void BuildGeometry();
    /// <summary>Builds objective props. Called after the player, HUD and game manager exist.</summary>
    protected virtual void BuildObjectives() { }
    /// <summary>Builds lasers, surge floors and similar hazards.</summary>
    protected virtual void BuildHazards() { }
    /// <summary>Spawns enemies.</summary>
    protected virtual void SpawnEnemies() { }
    /// <summary>Called last in <c>_Ready</c>, for recruit beats and story hooks.</summary>
    protected virtual void OnLevelReady() { }
    /// <summary>Per-physics-frame objective logic (pickups, gates, completion). Runs only while the mission is live.</summary>
    protected virtual void UpdateMission() { }
    /// <summary>Current objective line for the HUD.</summary>
    protected virtual string ComputeObjective() => "Reach the end of the mission";
    /// <summary>Opening line shown when the level starts.</summary>
    protected virtual string IntroLine() => "";
    /// <summary>Title shown in the HUD when the mission completes.</summary>
    protected virtual string CompletionLine => "MISSION COMPLETE";

    // ─── Public state ────────────────────────────────────────────────────────
    /// <summary>The controlled operative (the same body is reused for every squad member).</summary>
    public PalaceOperative Player => _player;
    /// <summary>True once the mission's final objective has been completed.</summary>
    public bool MissionComplete { get; protected set; }
    /// <summary>Optional archive memory fragments found so far.</summary>
    public int MemoryFragmentsFound { get; private set; }
    /// <summary>Number of optional fragments hidden in the level.</summary>
    public int FragmentTotal => _fragments.Count;
    /// <summary>The objective line currently shown in the HUD.</summary>
    public string CurrentObjective { get; protected set; } = "";
    /// <summary>World position the operative respawns at after a defeat or a fall.</summary>
    public Vector2 CheckpointPosition => _checkpoint;
    /// <summary>Score/points counter for this mission.</summary>
    protected GameManager Score => _gameManager;
    /// <summary>The camera that follows the player.</summary>
    protected Camera2D Camera => _camera;

    // ─── Constants ───────────────────────────────────────────────────────────
    /// <summary>The player's origin sits this far above its feet because the collision box is centered on the body.</summary>
    protected const float FeetOffset = 46f;
    /// <summary>Falling this far below the world bottom counts as falling into a chasm.</summary>
    private const float KillMargin = 130f;

    // ─── Runtime references ──────────────────────────────────────────────────
    private PalaceOperative _player = null!;
    private Camera2D _camera = null!;
    /// <summary>Container for static geometry.</summary>
    protected Node2D World = null!;
    /// <summary>Container for enemies, pickups and props.</summary>
    protected Node2D Actors = null!;
    private GameManager _gameManager = null!;
    private Node2D? _skyline;
    private Sprite2D? _backdrop;
    private Label _objectiveLabel = null!;
    private Label _fragmentLabel = null!;
    private Label _dialogueLabel = null!;
    private Control _dialoguePanel = null!;
    private Label _controlsLabel = null!;
    private Label _promptLabel = null!;
    private Control _promptPanel = null!;
    private readonly List<PalaceObjective> _fragments = new();
    private readonly List<PalaceObjective> _checkpoints = new();
    private Vector2 _checkpoint;
    private float _dialogueRemaining;
    private bool _finishing;
    private bool _recovering;
    private bool _usingPad;
    private OperativeRoster? _hintRoster;
    private CyberVictoryScorecard? _scorecard;
    private int _maxCombo = 1;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    /// <summary>Creates the whole mission: backdrop, geometry, operative, HUD, props, enemies, audio and pause menu.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var global = GetNodeOrNull<Node>("/root/Global");
        global?.Set("reached_finish", false);
        GetTree().Paused = false;
        global?.Call("stop_menu_music");
        // The squad is always rebuilt from campaign progress, however this mission was reached.
        CampaignProgress.PrepareMission(MissionNumber, global?.Get("selected_character").AsString());
        _checkpoint = StartPosition;

        AddChild(new WorldEnvironment { Environment = GD.Load<Godot.Environment>("res://assets/themes/cyber_glow.tres") });
        BuildBackdrop();

        World = new Node2D { Name = "PalaceGeometry", ProcessMode = ProcessModeEnum.Pausable };
        AddChild(World);
        Actors = new Node2D { Name = "Actors", ProcessMode = ProcessModeEnum.Pausable };
        AddChild(Actors);

        BuildGeometry();
        AddCeiling();
        BuildPlayer();

        _gameManager = new GameManager { Name = "GameManager" };
        AddChild(_gameManager);
        var combatHud = GD.Load<PackedScene>("res://scenes/gameplay/cyber_combat_hud.tscn").Instantiate<CanvasLayer>();
        combatHud.Name = "CyberCombatHud";
        AddChild(combatHud);

        // The final mission of an act ends on the combat-grade scorecard instead of auto-advancing.
        if (MissionNumber == 1 || !LevelCatalog.HasNext(MissionNumber))
        {
            _scorecard = GD.Load<PackedScene>("res://scenes/ui/cyber_victory_scorecard.tscn").Instantiate<CyberVictoryScorecard>();
            _scorecard.Name = "CyberVictoryScorecard";
            AddChild(_scorecard);
        }
        _player.ComboChanged += combo => _maxCombo = Mathf.Max(_maxCombo, combo);

        BuildObjectives();
        BuildShrines();
        BuildFragments();
        BuildHazards();
        SpawnEnemies();
        BuildPetals();
        BuildUserInterface();
        BuildAudioAndPause();
        ShowDialogue(IntroLine(), 11f);
        OnLevelReady();
        GD.Print($"{GetType().Name}: mission {MissionNumber} ready for {_player.ActiveProfile.DisplayName}");
    }

    /// <summary>Scrolls the backdrop and ticks the dialogue timer.</summary>
    public override void _Process(double delta)
    {
        UpdateBackdrop();
        if (MissionComplete) return;
        _dialogueRemaining = Mathf.Max(0f, _dialogueRemaining - (float)delta);
        _dialoguePanel.Visible = _dialogueRemaining > 0f;
    }

    /// <summary>Runs checkpoints, fragments, level logic and chasm recovery after the player has moved each physics tick.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (MissionComplete || _player == null) return;
        UpdateShrines();
        CollectFragments();
        UpdateMission();
        CurrentObjective = ComputeObjective();
        _objectiveLabel.Text = "OBJECTIVE  /  " + CurrentObjective.ToUpperInvariant();
        if (_player.GlobalPosition.Y > WorldBounds.End.Y + KillMargin) RecoverFromFall();
    }

    /// <summary>Switches the on-screen hint between keyboard and gamepad as the player changes device.</summary>
    public override void _Input(InputEvent @event)
    {
        if (_controlsLabel == null) return;
        if (@event is InputEventJoypadButton or InputEventJoypadMotion) { _usingPad = true; RefreshControlsHint(); }
        else if (@event is InputEventKey) { _usingPad = false; RefreshControlsHint(); }
    }

    /// <summary>Refreshes the controls hint when a new operative joins (it then also lists the swap keys).</summary>
    private void OnOperativeUnlocked(string _) => RefreshControlsHint();

    /// <summary>Leaves the tree unpaused so a scene change never inherits a paused state, and unsubscribes from the roster.</summary>
    public override void _ExitTree()
    {
        if (_hintRoster != null) _hintRoster.OperativeUnlocked -= OnOperativeUnlocked;
        if (GetTree() != null) GetTree().Paused = false;
    }

    // ─── World construction helpers ──────────────────────────────────────────

    /// <summary>Adds one solid platform. <paramref name="top"/> is the walkable surface Y; the body extends downward by <paramref name="height"/>.</summary>
    protected PalacePlatform AddPlatform(float x, float top, float width, float height, PalaceSurface surface = PalaceSurface.Stone, bool oneWay = false, string? name = null)
    {
        var platform = new PalacePlatform { Position = new Vector2(x, top), Size = new Vector2(width, height), Surface = surface, OneWay = oneWay, Modulate = PlatformTint };
        if (name != null) platform.Name = name;
        World.AddChild(platform);
        return platform;
    }

    /// <summary>
    /// Adds an invisible ceiling along the top of the world. Gate walls reach all the way up to it, so a gate can never
    /// be bypassed by wall-jumping over its top.
    /// </summary>
    private void AddCeiling()
    {
        var bounds = WorldBounds;
        var ceiling = AddPlatform(bounds.Position.X - 200, bounds.Position.Y - 40, bounds.Size.X + 400, 40, PalaceSurface.Wall, false, "WorldCeiling");
        ceiling.Visible = false;
    }

    /// <summary>Height (px) of a wall whose feet are at <paramref name="feetY"/> and whose top meets the world ceiling.</summary>
    protected float FullHeightFrom(float feetY) => feetY - WorldBounds.Position.Y;

    /// <summary>Adds a platform that glides between its position and position + <paramref name="travel"/>.</summary>
    protected PalaceMovingPlatform AddMover(string name, Vector2 position, Vector2 travel, float period)
    {
        var mover = new PalaceMovingPlatform { Name = name, Position = position, Travel = travel, Period = period };
        World.AddChild(mover);
        return mover;
    }

    /// <summary>Adds an operative-gated obstacle whose feet sit at <paramref name="position"/>.</summary>
    protected AbilityGate AddGate(string name, GateKind kind, Vector2 position, Vector2? size = null)
    {
        var gate = new AbilityGate { Name = name, Kind = kind, Position = position };
        if (size.HasValue) gate.Size = size.Value;
        Actors.AddChild(gate);
        return gate;
    }

    /// <summary>
    /// Adds an always-on laser field. With the default lethal damage, a full-height beam cannot be crossed by anyone
    /// except an operative who is Phasing (Mirage Blade's Phase Blink); pass a small damage for jumpable tripwires.
    /// </summary>
    protected CyberLaserGrid2D AddLaserField(Vector2 topPosition, float length, float damage = 999f)
    {
        var laser = GD.Load<PackedScene>("res://scenes/gameplay/cyber_laser_grid_2d.tscn").Instantiate<CyberLaserGrid2D>();
        laser.Position = topPosition;
        laser.BeamLength = length;
        laser.AlwaysActive = true;
        laser.Damage = damage;
        Actors.AddChild(laser);
        return laser;
    }

    /// <summary>Adds a hovering demolition sentry bound to this mission's player and score.</summary>
    protected PalaceSentry AddSentry(string name, Vector2 position, float health)
    {
        var sentry = new PalaceSentry { Name = name, Position = position, MaxHealth = health, Target = _player };
        sentry.Defeated += () => _gameManager.AddPoint();
        Actors.AddChild(sentry);
        return sentry;
    }

    // ─── Backdrop ────────────────────────────────────────────────────────────

    /// <summary>Adds the backdrop on its own canvas layer so it can scroll slower than the world.</summary>
    private void BuildBackdrop()
    {
        var layer = new CanvasLayer { Name = "BackdropLayer", Layer = -10 };
        AddChild(layer);
        if (BackdropPath != null)
        {
            _backdrop = new Sprite2D
            {
                Name = "PalaceGardenBackdrop",
                Texture = GD.Load<Texture2D>(BackdropPath),
                // The painting is 2149x732; scaling to ~1700 px tall keeps it covering a full screen after parallax shifts.
                Scale = Vector2.One * 2.35f,
                // Dim it so bright lanterns and platform rims stay readable on top.
                Modulate = new Color(.78f, .8f, .95f),
                TextureFilter = CanvasItem.TextureFilterEnum.Linear
            };
            layer.AddChild(_backdrop);
            _backdrop.Position = new Vector2(960f, 540f);
        }
        else
        {
            _skyline = new SkylineBackdrop { Name = "SkylineBackdrop", Palette = SkylinePalette };
            layer.AddChild(_skyline);
        }
    }

    /// <summary>Keeps the backdrop sliding at a fraction of camera speed (parallax), clamped so no edge is ever exposed.</summary>
    private void UpdateBackdrop()
    {
        if (_camera == null) return;
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 center = _camera.GetScreenCenterPosition();
        if (_skyline is SkylineBackdrop skyline) { skyline.CameraCenter = center; skyline.ViewSize = viewport; return; }
        if (_backdrop?.Texture == null) return;
        // Grow the image if the window is larger than expected (ultra-wide or tall) so it always covers the viewport
        // with room left to scroll.
        Vector2 texture = _backdrop.Texture.GetSize();
        float scale = Mathf.Max(2.35f, Mathf.Max(viewport.Y * 1.25f / texture.Y, viewport.X * 1.2f / texture.X));
        _backdrop.Scale = Vector2.One * scale;
        Vector2 half = texture * scale * .5f;
        float x = viewport.X * .5f - (center.X - viewport.X * .5f) * .17f;
        float y = viewport.Y * .5f - (center.Y + 250f) * .1f + 60f;
        // The image must always span the whole viewport: its edges can't move inside the screen edges.
        x = ClampSpan(x, viewport.X - half.X, half.X);
        y = ClampSpan(y, viewport.Y - half.Y, half.Y);
        _backdrop.Position = new Vector2(x, y);
    }

    /// <summary>Clamps to [a, b] regardless of which bound is larger (the image can briefly be smaller than the window).</summary>
    private static float ClampSpan(float value, float a, float b) => Mathf.Clamp(value, Mathf.Min(a, b), Mathf.Max(a, b));

    // ─── Player, shrines, fragments, atmosphere ──────────────────────────────

    /// <summary>Instantiates the operative, adds the follow camera and connects defeat handling.</summary>
    private void BuildPlayer()
    {
        _player = GD.Load<PackedScene>("res://scenes/gameplay/palace_operative.tscn").Instantiate<PalaceOperative>();
        // The combat HUD finds the player by this exact node name.
        _player.Name = "CyberShogun";
        _player.Position = StartPosition;
        _player.PlayerDefeated += OnPlayerDefeated;
        Actors.AddChild(_player);

        var bounds = WorldBounds;
        _camera = new Camera2D
        {
            Name = "Camera2D",
            LimitLeft = bounds.Position.X, LimitRight = bounds.End.X,
            LimitTop = bounds.Position.Y, LimitBottom = bounds.End.Y,
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed = 7f,
            // Look slightly above the player so landing spots below stay visible.
            Offset = new Vector2(0f, -80f),
            Zoom = Vector2.One * .95f
        };
        // The player is scaled 1.35x; compensate so the camera behaves as if on an unscaled parent.
        _camera.Scale = Vector2.One / _player.Scale.X;
        _player.AddChild(_camera);
        _camera.MakeCurrent();
    }

    /// <summary>Creates the checkpoint shrines declared by <see cref="Shrines"/>.</summary>
    private void BuildShrines()
    {
        foreach (var (name, position) in Shrines)
        {
            var shrine = new PalaceObjective { Name = name, Kind = "Checkpoint", Position = position };
            _checkpoints.Add(shrine);
            Actors.AddChild(shrine);
        }
    }

    /// <summary>Creates the optional memory fragments declared by <see cref="FragmentPositions"/>.</summary>
    private void BuildFragments()
    {
        foreach (var point in FragmentPositions)
        {
            var fragment = new PalaceObjective { Name = "MemoryFragment", Kind = "Fragment", Position = point };
            _fragments.Add(fragment);
            Actors.AddChild(fragment);
        }
    }

    /// <summary>Adds drifting particles that follow the camera for atmosphere (blossom petals by default).</summary>
    protected virtual void BuildPetals()
    {
        _camera.AddChild(new CpuParticles2D
        {
            Name = "Petals", Position = new Vector2(0, -620), Amount = 70, Lifetime = 9f, Preprocess = 9f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(1300, 10),
            Gravity = new Vector2(-45, 70), InitialVelocityMin = 10f, InitialVelocityMax = 40f,
            Direction = new Vector2(-1, 1), Spread = 30f, Color = new Color(.95f, .6f, .78f, .8f),
            ScaleAmountMin = 1.8f, ScaleAmountMax = 3.5f, ZIndex = 20
        });
    }

    /// <summary>Lights and stores the checkpoint when the player reaches an unlit shrine.</summary>
    private void UpdateShrines()
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
    /// Walk-into pickup for memory fragments. Public so integration tests can place the player and trigger a pass
    /// deterministically.
    /// </summary>
    public void CollectFragments()
    {
        foreach (var fragment in _fragments)
        {
            if (fragment.Collected || _player.GlobalPosition.DistanceTo(fragment.GlobalPosition + new Vector2(0, -40)) > 80f) continue;
            fragment.SetCollected();
            MemoryFragmentsFound++;
            _gameManager.Points += 25;
            ShowDialogue(FragmentLog(MemoryFragmentsFound), 8f);
            UpdateFragmentLabel();
        }
    }

    /// <summary>Story text shown when fragment number <paramref name="found"/> is collected.</summary>
    protected virtual string FragmentLog(int found) => found switch
    {
        1 => "ARCHIVE LOG: Every district kept a memory of its own.",
        2 => "ARCHIVE LOG: The resistance began as neighbours sharing stories.",
        _ => "ARCHIVE LOG: Nothing erased is ever truly gone."
    };

    // ─── Defeat and recovery ─────────────────────────────────────────────────

    /// <summary>Returns a fallen operative to the last checkpoint with a small penalty instead of a full defeat.</summary>
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

    /// <summary>Records extraction once; mission one keeps its scene loaded for cinematic/results replay.</summary>
    protected async void CompleteMission()
    {
        if (_finishing) return;
        _finishing = MissionComplete = true;
        _player.StopMovement();
        CurrentObjective = "Mission complete";
        _objectiveLabel.Text = CompletionLine;
        _dialoguePanel.Visible = true;
        // Snapshot before any cinematic or asynchronous work: results describe the extraction instant.
        int finalScore = _gameManager.Points;
        int peakCombo = _maxCombo;
        string operative = $"{_player.ActiveProfile.CivilianName} // {_player.ActiveProfile.DisplayName}".ToUpperInvariant();
        string? nextScene = CampaignProgress.CompleteMission(MissionNumber, finalScore);
        if (MissionNumber == 1)
        {
            // Disable this entire mission, including pause input and physics. Overlays explicitly run Always.
            ProcessMode = ProcessModeEnum.Disabled;
            foreach (Node child in GetChildren())
                if (child != _scorecard) FreezeForEpilogue(child);
            var cinematic = new TokyoRemembers { Name = "TokyoRemembers", Layer = 40 };
            cinematic.Finished += () => _scorecard?.ShowMissionScorecard(finalScore, peakCombo, operative, 2);
            // Defer tree mutation because extraction is entered from an Area2D physics callback.
            Callable.From(() => { if (IsInsideTree()) AddChild(cinematic); else cinematic.Free(); }).CallDeferred();
            return;
        }
        if (_scorecard != null)
        {
            // Final mission: show the grade; the scorecard's own buttons (replay / level select / menu) take it from here.
            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree()) return;
            _scorecard.ShowScorecard(finalScore, peakCombo, operative);
            return;
        }
        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree()) GetTree().ChangeSceneToFile(nextScene ?? "res://scenes/main_menu/level_selection.tscn");
    }

    /// <summary>Freezes explicit Pausable/Always descendants too, including pause input, and silences mission audio.</summary>
    private static void FreezeForEpilogue(Node node)
    {
        node.ProcessMode = ProcessModeEnum.Disabled;
        if (node is AudioStreamPlayer audio) audio.Stop();
        if (node is AudioStreamPlayer2D audio2D) audio2D.Stop();
        if (node is CanvasLayer layer) layer.Visible = false;
        foreach (Node child in node.GetChildren()) FreezeForEpilogue(child);
    }

    // ─── User interface ──────────────────────────────────────────────────────

    /// <summary>Builds the objective panel, fragment counter, controls hint, squad strip, dialogue strip and prompt.</summary>
    private void BuildUserInterface()
    {
        var hud = new CanvasLayer { Name = "MissionHUD", Layer = 5 };
        AddChild(hud);
        var root = new Control { Name = "HUDRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        hud.AddChild(root);

        // Sits below the combat HUD's points counter (top-right) so the two never overlap.
        var objectivePanel = NewPanel(new Vector2(-430, 84), new Vector2(410, 84), new Vector2(1, 0));
        root.AddChild(objectivePanel);
        var box = new VBoxContainer();
        objectivePanel.AddChild(box);
        _objectiveLabel = NewLabel("", 15, new Color("#77efff"));
        box.AddChild(_objectiveLabel);
        _fragmentLabel = NewLabel("", 13, new Color("#ffcf8a"));
        box.AddChild(_fragmentLabel);
        UpdateFragmentLabel();

        _controlsLabel = NewLabel("", 12, Colors.White);
        _controlsLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        _controlsLabel.OffsetLeft = 20;
        _controlsLabel.OffsetTop = -34;
        _controlsLabel.OffsetRight = 1240;
        _controlsLabel.OffsetBottom = -10;
        root.AddChild(_controlsLabel);
        RefreshControlsHint();

        // Squad strip: shows who is unlocked/active; number keys and Tab swap between unlocked operatives.
        var squad = new SquadStrip { Name = "SquadStrip" };
        squad.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        squad.OffsetLeft = -620;
        squad.OffsetTop = -34;
        squad.OffsetRight = -20;
        squad.OffsetBottom = -8;
        root.AddChild(squad);
        squad.Bind(_player);
        _hintRoster = OperativeRoster.Current;
        _hintRoster.OperativeUnlocked += OnOperativeUnlocked;

        _dialoguePanel = NewPanel(new Vector2(-430, -116), new Vector2(860, 80), new Vector2(.5f, 1));
        root.AddChild(_dialoguePanel);
        _dialogueLabel = NewLabel("", 14, new Color("#ffe9c9"));
        _dialoguePanel.AddChild(_dialogueLabel);
        _dialoguePanel.Visible = false;

        BuildTitleCard(root);

        // Prompt: a persistent top-centre hint used by ability lessons; hidden until a lesson shows it.
        _promptPanel = NewPanel(new Vector2(-300, 90), new Vector2(600, 64), new Vector2(.5f, 0));
        root.AddChild(_promptPanel);
        _promptLabel = NewLabel("", 16, new Color("#ffffff"));
        _promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _promptPanel.AddChild(_promptLabel);
        _promptPanel.Visible = false;
    }

    /// <summary>
    /// Shows "MISSION N: TITLE" with the story's objective text for a few seconds, then fades it out. Title and objective
    /// come from <c>NarrativeManager</c> (the story JSON) when available, falling back to the level catalog.
    /// </summary>
    private void BuildTitleCard(Control root)
    {
        var entry = LevelCatalog.Get(MissionNumber);
        var narrative = GetNodeOrNull<Node>("/root/NarrativeManager");
        string title = narrative?.Call("get_level_title", entry.NarrativeKey).AsString() ?? "";
        string objective = narrative?.Call("get_level_objective", entry.NarrativeKey).AsString() ?? "";
        if (string.IsNullOrEmpty(title)) title = entry.Title;
        var card = new VBoxContainer { Name = "TitleCard", MouseFilter = Control.MouseFilterEnum.Ignore };
        card.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        card.OffsetLeft = -560; card.OffsetRight = 560; card.OffsetTop = -120; card.OffsetBottom = 40;
        var heading = NewLabel($"MISSION {MissionNumber}: {title.ToUpperInvariant()}", 34, new Color("#77efff"));
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        card.AddChild(heading);
        if (!string.IsNullOrEmpty(objective))
        {
            var sub = NewLabel(objective, 16, new Color("#ffe9c9"));
            sub.HorizontalAlignment = HorizontalAlignment.Center;
            card.AddChild(sub);
        }
        root.AddChild(card);
        // Hold briefly, then fade; the tween belongs to the card so it dies with the scene.
        var tween = card.CreateTween();
        tween.TweenInterval(3.2);
        tween.TweenProperty(card, "modulate:a", 0f, 1.2);
        tween.TweenCallback(Callable.From(card.QueueFree));
    }

    /// <summary>Refreshes the optional-fragment counter shown under the objective.</summary>
    private void UpdateFragmentLabel() => _fragmentLabel.Text = _fragments.Count == 0 ? "" : $"MEMORY FRAGMENTS  {MemoryFragmentsFound} / {FragmentTotal}";

    /// <summary>Rebuilds the controls hint for the active device, adding the swap keys once a second operative exists.</summary>
    private void RefreshControlsHint()
    {
        if (_controlsLabel == null) return;
        bool canSwap = OperativeRoster.Current.Unlocked.Count > 1;
        _controlsLabel.Text = _usingPad
            ? "L STICK MOVE | A JUMP | X ATTACK | Y ABILITY | RB DASH | " + (canSwap ? "D-PAD DOWN / BACK SWAP | " : "") + "START PAUSE"
            : "A / D MOVE | SPACE JUMP | F ATTACK | Q ABILITY | SHIFT DASH | " + (canSwap ? "TAB / 1-6 SWAP | " : "") + "ESC PAUSE";
    }

    /// <summary>Starts the stage music and instantiates the shared pause menu used by the other levels.</summary>
    private void BuildAudioAndPause()
    {
        var music = new AudioStreamPlayer
        {
            Name = "AudioStreamPlayer", Stream = GD.Load<AudioStream>(MusicPath), VolumeDb = 24f, Autoplay = true
        };
        AddChild(music);
        var pauseLayer = new CanvasLayer { Name = "CanvasLayer" };
        AddChild(pauseLayer);
        pauseLayer.AddChild(GD.Load<PackedScene>("res://scenes/gameplay/pause_menu.tscn").Instantiate());
    }

    /// <summary>Shows a status or story line in the bottom dialogue strip for <paramref name="seconds"/>. Empty text is ignored.</summary>
    public void ShowDialogue(string text, float seconds)
    {
        if (string.IsNullOrEmpty(text)) return;
        _dialogueLabel.Text = text;
        _dialogueRemaining = seconds;
        _dialoguePanel.Visible = true;
    }

    /// <summary>Shows a persistent prompt at the top of the screen (used by ability lessons).</summary>
    public void ShowPrompt(string text)
    {
        _promptLabel.Text = text;
        _promptPanel.Visible = true;
    }

    /// <summary>Hides the lesson prompt.</summary>
    public void HidePrompt() => _promptPanel.Visible = false;

    /// <summary>Text currently shown in the lesson prompt (empty when hidden); used by tests.</summary>
    public string PromptText => _promptPanel != null && _promptPanel.Visible ? _promptLabel.Text : "";

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
}
