using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Streamed Neo-Los Angeles exploration scene (the class keeps its original name for save and scene stability).
/// Runs walking, the electric bike, residents, safehouses, the eight playable slice quests with
/// conversations and combat encounters, local alert states and PostgreSQL persistence.
/// Scene mutations and UI run on the main thread; workers produce data only.
/// </summary>
public partial class NeoKyotoWorld : Node2D
{
    public enum AlertState { None, Suspicion, Search, Pursuit }
    public enum WeatherState { Clear, Rain, Fog, RegionalStorm }

    /// <summary>
    /// Computes the regional weather state according to a deterministic 48-minute cycle:
    /// - 00 to 24 min: Clear
    /// - 24 to 32 min: Rain
    /// - 32 to 40 min: Fog
    /// - 40 to 48 min: RegionalStorm
    /// </summary>
    /// <param name="worldMinutes">Current world elapsed time in minutes.</param>
    /// <returns>The active WeatherState enum value.</returns>
    public static WeatherState GetWeatherForTime(double worldMinutes)
    {
        double cycle = (worldMinutes % 48.0 + 48.0) % 48.0;
        return cycle switch
        {
            < 24.0 => WeatherState.Clear,
            < 32.0 => WeatherState.Rain,
            < 40.0 => WeatherState.Fog,
            _ => WeatherState.RegionalStorm
        };
    }

    /// <summary>
    /// The current weather state computed from the active instance's world simulation time.
    /// </summary>
    public WeatherState CurrentWeather => GetWeatherForTime(_worldTimeMinutes);

    private WorldDefinition _world = WorldCatalog.Definition;
    private WorldStream _stream = new();
    // Unsaved stand-ins for each city's database row, used only by the preview with no campaign.
    private readonly Dictionary<string, WorldSave> _previewRecords = new();
    private readonly Dictionary<ChunkAddress, WorldChunkView> _views = new();
    private readonly List<Node2D> _markers = new();
    private ExplorationPlayer _player = null!;
    private ChunkAddress _origin;
    private CanvasLayer _hud = null!;
    private Control _mapPanel = null!;
    private WorldMap _map = null!, _mini = null!;
    private Label _status = null!, _heading = null!, _hint = null!, _questLabel = null!;
    private Label _mapStatus = null!, _mapTitle = null!;
    private OptionButton _layers = null!;
    private ItemList _destinations = null!;
    private readonly List<WorldPoint> _destinationPoints = new();
    private readonly List<WorldStation?> _destinationStations = new();
    private Button _travel = null!, _retry = null!;
    private Button _mapRetry = null!;
    private Func<Task>? _retryOperation;
    private bool _busy = true, _loaded, _mapOpen;
    private WorldConversationPanel _conversation = null!;
    private WorldEncounterController _encounter = null!;
    private Label _combatLabel = null!;
    // One token per quest, so retrying a failed completion can never earn its rewards twice.
    private readonly Dictionary<string, string> _pendingCompletions = new();
    private string? _trackedQuest;
    private string _toast = "";
    private double _toastSeconds;
    private bool _lastIsDay = true;
    private double _autosave;
    private double _worldTimeMinutes;
    private int _session;

    /// <summary>The city currently loaded.</summary>
    public WorldDefinition Definition => _world;
    public int Progress { get; private set; } = 1;
    public WorldSave State { get; private set; } = WorldSave.AfterMission(1);
    public AlertState CurrentAlert { get; private set; } = AlertState.None;

    public WorldPoint PositionMetres => _player == null
        ? State.Position
        : new WorldPoint(_origin.X * 128 + _player.Position.X / 16, _origin.Y * 128 + _player.Position.Y / (16 * .65), _origin.Layer);

    public int ResidentChunks => _views.Count;
    public int CachedChunks => _stream.Cache.Count;

    /// <summary>Active accessibility and gameplay assist profile.</summary>
    public AccessibilityProfile Accessibility { get; set; } = AccessibilityProfile.Default;

    /// <summary>Read-only diagnostic snapshot for Godot MCP, automated tours and on-machine streaming measurements.</summary>
    public Godot.Collections.Dictionary InspectionState()
    {
        var p = PositionMetres;
        return new Godot.Collections.Dictionary
        {
            { "city", _world.Id },
            { "x", p.X }, { "y", p.Y }, { "layer", p.Layer },
            { "resident", ResidentChunks }, { "cached", CachedChunks },
            { "busy", _busy }, { "map_open", _mapOpen }, { "progress", Progress },
            { "waypoint_set", State.Waypoint != null },
            { "moving_enabled", _player?.MovementEnabled ?? false },
            { "riding_bike", _player?.IsRidingBike ?? false },
            { "riding_boat", _player?.IsRidingBoat ?? false },
            { "alert_state", CurrentAlert.ToString() },
            { "world_time", _worldTimeMinutes },
            { "weather", CurrentWeather.ToString() },
            { "era", DistrictState.DetermineEra(_world.Region(p).Id, Progress).ToString() },
            { "accessibility_high_contrast", Accessibility.HighContrastTelegraphs },
            { "accessibility_parry_scale", Accessibility.ParryTimingWindowScale },
            { "accessibility_puzzle_assist", Accessibility.PuzzleAssist.ToString() },
            { "reconstruction_tasks_count", EndgameContentCatalog.ReconstructionTasks.Count },
            { "remnant_incidents_count", EndgameContentCatalog.RemnantIncidents.Count },
            { "archive_simulations_count", EndgameContentCatalog.ArchiveSimulations.Count },
            { "operation_modifiers_count", EndgameContentCatalog.Modifiers.Count },
            { "conversation_open", _conversation?.IsOpen ?? false },
            { "live_hostiles", _encounter?.LiveCount ?? 0 },
            { "wave", _encounter?.WaveKey ?? "" },
            { "health", _player?.CurrentHealth ?? 0 },
            { "last_toast", _toast },
            { "completed_quests", State.CompletedQuests?.Count ?? 0 }
        };
    }

    /// <summary>Moves the player for automated tests. Refused for a real campaign, so it can never alter a save.</summary>
    public void PreviewPlace(double x, double y)
    {
        if (_session > 0) return;
        _encounter.Clear();
        Place(new WorldPoint(x, y));
    }

    /// <summary>Flies to a city for automated tests, without saving. Refused for a real campaign. Returns false if the city is unknown.</summary>
    public bool PreviewTravel(string cityId)
    {
        var city = CityRegistry.ById(cityId);
        if (_session > 0 || city == null || city == _world) return false;
        _ = TravelTo(city);
        return true;
    }

    /// <summary>Sets the world clock for automated tests. Refused for a real campaign.</summary>
    public void PreviewSetWorldMinutes(double minutes)
    {
        if (_session <= 0) _worldTimeMinutes = minutes;
    }

    /// <summary>Spawns one drone for automated tests. Returns false when the budget refuses it.</summary>
    public bool PreviewSpawnHostile(double x, double y) => _session <= 0 && _encounter.TrySpawn(x, y);

    /// <summary>Marks a prerequisite quest finished for automated tests. Refused for a real campaign.</summary>
    public void PreviewCompleteQuest(string questId)
    {
        if (_session <= 0) State = State.WithQuestCompleted(questId, null);
    }

    /// <summary>Reports the saved objective index of a quest, for automated tests.</summary>
    public int ObjectiveIndex(string questId) => State.ActiveQuests != null && State.ActiveQuests.TryGetValue(questId, out int index) ? index : 0;

    /// <summary>Reports whether a quest has been completed, for automated tests.</summary>
    public bool IsQuestComplete(string questId) => State.CompletedQuests?.ContainsKey(questId) == true;

    /// <summary>Evaluates an operation clear, returning rank, score, credits, and cosmetic unlocks for UI and GdUnit4 testing.</summary>
    public Godot.Collections.Dictionary EvaluateOperationMastery(
        string operationId,
        float clearTimeSeconds,
        float targetGoldTimeSeconds,
        Godot.Collections.Array<string> activeModifierIds,
        bool tookDamage
    )
    {
        var res = EndgameContentCatalog.EvaluateMastery(
            operationId,
            clearTimeSeconds,
            targetGoldTimeSeconds,
            activeModifierIds.ToArray(),
            tookDamage
        );
        return new Godot.Collections.Dictionary
        {
            { "rank", (int)res.Rank },
            { "final_score", res.FinalScore },
            { "credits", res.CreditsAwarded },
            { "faction_rep", res.FactionReputationAwarded },
            { "unlocked_cosmetic", res.UnlockedCosmetic ?? "" }
        };
    }

    /// <summary>Configures the active parry timing window scale in accessibility settings.</summary>
    public void SetParryTimingScale(float scale)
    {
        Accessibility.ParryTimingWindowScale = scale;
    }

    /// <summary>Calculates the effective parry window with current accessibility settings.</summary>
    public float CalculateParryWindow(float baseWindowSeconds)
    {
        return Accessibility.CalculateParryWindow(baseWindowSeconds);
    }

    /// <summary>Calculates the effective screen shake intensity with current accessibility settings.</summary>
    public float CalculateScreenShake(float baseIntensity)
    {
        return Accessibility.CalculateScreenShake(baseIntensity);
    }

    /// <summary>Builds UI and starts a database load. Direct scene inspection receives an unsaved preview, never a local save.</summary>
    public override void _Ready()
    {
        AddToGroup("world_exploration");
        InputActionDefaults.Initialize();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", false);
        _player = new ExplorationPlayer { Name = "ExplorationPlayer" };
        AddChild(_player);
        _encounter = new WorldEncounterController { Name = "WorldEncounters", ZIndex = 3 };
        AddChild(_encounter);
        _encounter.WaveCleared += OnWaveCleared;
        _player.Defeated += OnPlayerDefeated;
        BuildHud();
        InputPromptService.Instance?.SetContext("world", "world_left", "world_right", "world_up", "world_down", "world_map", "world_interact", "world_swap");
        _session = SessionManager.Instance?.CurrentSessionId ?? -1;
        Progress = Math.Max(1, GetNodeOrNull<Node>("/root/Global")?.Get("unlocked_levels").AsInt32() ?? 1);
        RunOperation(LoadWorld);
    }

    /// <summary>Loads only the selected campaign, migrates legacy campaigns once, and never treats an outage as missing data.</summary>
    private async Task LoadWorld()
    {
        string preferred = GetNodeOrNull<Node>("/root/Global")?.Get("selected_character").AsString() ?? OperativeRoster.Starter;
        WorldSave? loaded = _session > 0 ? await WorldStorage.LoadActiveAsync(_session, Progress, preferredOperative: preferred) : null;
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        State = loaded ?? WorldSave.AfterMission(1);
        UseCity(CityRegistry.ForIdentity(State.World) ?? WorldCatalog.Definition);
        _worldTimeMinutes = State.WorldTimeMinutes;

        if (_session > 0 && loaded == null && Progress == 1) throw new InvalidOperationException("Finish Mission 1 before entering Neo-Los Angeles.");
        var roster = OperativeRoster.BeginLevel(Progress);
        if (Progress == 5) roster = OperativeRoster.BeginLevel(6);
        roster.SetActive(State.Operative);
        _player.RefreshOperative();
        GetNodeOrNull<Node>("/root/Global")?.Set("selected_character", roster.Active);
        if (_session > 0)
        {
            // Keep memory equal to what was written, or the next autosave would restore a stale resume mode.
            var entered = State with { Resume = "world", Operative = roster.Active };
            await WorldStorage.SaveAsync(_session, entered);
            State = entered;
        }
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        Place(State.Position);
        if (State.ActiveVehicle == WorldContentCatalog.StarterBike.Id) _player.ToggleBike(State.Position);
        _loaded = true;
        PopulateDestinations();
    }

    /// <summary>Rebases rendering to an integer chunk and discards old layer views before a committed transit or elevator move.</summary>
    private void Place(WorldPoint p)
    {
        foreach (var view in _views.Values) { RemoveChild(view); view.QueueFree(); }
        _views.Clear();
        _encounter.Clear();
        _origin = p.Chunk;
        _player.Position = ExplorationPlayer.Project(p.X - _origin.X * 128, p.Y - _origin.Y * 128);
        RebuildMarkers();
    }

    /// <summary>Shifts every resident object by a bounded delta, keeping coordinates precise across the million possible addresses.</summary>
    private void Rebase(ChunkAddress center)
    {
        Vector2 shift = ExplorationPlayer.Project((_origin.X - center.X) * 128, (_origin.Y - center.Y) * 128);
        _player.Position += shift;
        foreach (var view in _views.Values) view.Position += shift;
        foreach (var marker in _markers) marker.Position += shift;
        _origin = center;
    }

    /// <summary>Pumps worker data, applies at most one chunk per frame, updates alert state, and updates the 48-minute day/night cycle.</summary>
    public override void _Process(double delta)
    {
        if (!_loaded) return;
        var p = PositionMetres;
        var center = p.Chunk;
        if (center != _origin) { Rebase(center); RebuildMarkers(); }

        // The clock stops in menus and conversations, as the world's time rules require.
        if (!_mapOpen && !_conversation.IsOpen)
        {
            _worldTimeMinutes += delta / 60.0;
            if (_worldTimeMinutes >= 48.0) _worldTimeMinutes %= 48.0;
        }
        bool isDay = WorldQuestRules.IsDay(_worldTimeMinutes);
        if (isDay != _lastIsDay) { _lastIsDay = isDay; RebuildMarkers(); }

        _stream.Pump(center);
        var cacheWindow = WorldStream.Window(center, 2).ToArray();
        if (!_stream.Busy && cacheWindow.Any(a => !_stream.Cache.ContainsKey(a))) _stream.Request(center);
        var active = WorldStream.Window(center, 1).ToHashSet();
        foreach (var key in _views.Keys.Where(a => !active.Contains(a)).ToArray())
        {
            var view = _views[key];
            RemoveChild(view);
            view.QueueFree();
            _views.Remove(key);
        }

        foreach (var key in WorldStream.Window(center, 1))
        {
            if (!_views.ContainsKey(key) && _stream.Cache.TryGetValue(key, out var data))
            {
                var view = new WorldChunkView { Position = ExplorationPlayer.Project((key.X - _origin.X) * 128, (key.Y - _origin.Y) * 128) };
                AddChild(view);
                view.Initialize(data, _world);
                _views[key] = view;
                break;
            }
        }

        _player.MovementEnabled = !_busy && !_mapOpen && !_conversation.IsOpen && active.All(_views.ContainsKey);
        foreach (var view in _views.Values) view.UpdateRoofs(_player.GlobalPosition);

        UpdateEncounters(delta, p);

        // Refresh UI indicators
        string tod = (_worldTimeMinutes % 48.0) < 24.0 ? "Day" : "Night";
        _heading.Text = $"{WorldText.Get(_world.Region(p).Name)}  /  {WorldText.Get(OperativeRoster.Current.Active)}  [{tod}]";
        // A release can arrive while input is gated (saving, menus, mounting), so guard follows the held state here.
        if (_player.IsDefending && !Input.IsActionPressed("world_defend")) _player.ExecuteDefend(false);

        if (_toastSeconds > 0) _toastSeconds -= delta;
        _hint.Text = _toastSeconds > 0 ? _toast : NearbyHint(p);
        UpdateQuestHud();
        _combatLabel.Text = $"{WorldText.Get("Health")} {_player.CurrentHealth:0}/{_player.MaxHealth:0} · {WorldText.Get("Shield")} {_player.CurrentShield:0}/{_player.MaxShield:0}" + (CurrentAlert == AlertState.None ? "" : $" · {WorldText.Get(CurrentAlert.ToString())}");

        // Saving is held during a conversation or pursuit, when a mid-write freeze would interrupt play.
        if (!_busy && !_mapOpen && !_conversation.IsOpen && CurrentAlert != AlertState.Pursuit && (_autosave += delta) > 20)
        {
            _autosave = 0;
            if (_session > 0) RunOperation(() => Commit(BuildSnapshot()));
        }
    }

    /// <summary>Starts objective waves, advances live drones, and derives the alert from what they are doing.</summary>
    private void UpdateEncounters(double delta, WorldPoint p)
    {
        // Held while saving: otherwise a just-cleared wave could respawn before its progress is stored.
        // MovementEnabled also covers chunk streaming, so drones never advance on a player who can't move.
        if (_player.MovementEnabled)
        {
            var combat = _encounter.WaveKey == null ? WorldQuestRules.FindCombat(State, p, Progress, OperativeRoster.Current.Active, IsRecruited) : null;
            // Nearby objectives can overlap. A running wave keeps going until cleared or left, so kills are not reset.
            if (combat != null) _encounter.StartWave(combat.Objective.Id, combat.Objective.TargetLocation, combat.Objective.RequiredCount);
            _encounter.Tick(delta, p, _origin, damage => _player.TakeDamage(damage));
        }
        CurrentAlert = _encounter.Alert;

        // Fast transit is disabled during active pursuit
        if (_travel != null)
        {
            int[] selected = _destinations.GetSelectedItems();
            _travel.Disabled = CurrentAlert == AlertState.Pursuit || selected.Length == 0 || _destinationStations[selected[0]] == null;
        }
    }

    /// <summary>Updates active quest header and objective tracking.</summary>
    private void UpdateQuestHud()
    {
        if (_questLabel == null) return;
        var tracked = WorldQuestRules.Tracked(State, _trackedQuest, Progress, IsRecruited);
        if (tracked != null)
        {
            string operativeNote = tracked.Quest.RequiredOperative != null && tracked.Quest.RequiredOperative != OperativeRoster.Current.Active ? $" ({WorldText.Get(tracked.Quest.RequiredOperative)})" : "";
            _questLabel.Text = $"⚡ {WorldText.Get(tracked.Quest.Title)}: {WorldText.Get(tracked.Objective.Description)}{operativeNote}";
            _questLabel.Visible = true;
        }
        else
        {
            _questLabel.Text = "";
            _questLabel.Visible = false;
        }
    }

    /// <summary>Whether a required operative has joined the squad in this campaign.</summary>
    private static bool IsRecruited(string operative) => OperativeRoster.Current.IsUnlocked(operative);

    /// <summary>Copies the live position, operative, clock and vehicle into the state, the one source for every save.</summary>
    private WorldSave BuildSnapshot() => State with
    {
        Position = PositionMetres,
        Operative = OperativeRoster.Current.Active,
        WorldTimeMinutes = _worldTimeMinutes,
        ActiveVehicle = _player.IsRidingBike ? WorldContentCatalog.StarterBike.Id : null
    };

    /// <summary>The single decision about what E does here, shared by the action and its on-screen hint so they can't disagree.</summary>
    private (QuestAction? Quest, NamedResident? Resident, InteriorDefinition? Interior) ResolveInteractable(WorldPoint p)
    {
        bool isDay = WorldQuestRules.IsDay(_worldTimeMinutes);
        var quest = WorldQuestRules.FindInteractable(State, p, Progress, isDay, OperativeRoster.Current.Active, IsRecruited);
        var interior = _world.Content.Interiors.Where(i => i.EntranceLocation.Distance(p) < 14).OrderBy(i => i.EntranceLocation.Distance(p)).FirstOrDefault();
        var resident = _world.Content.Residents.Where(r => (isDay ? r.DayLocation : r.EveningLocation).Distance(p) < 14).OrderBy(r => (isDay ? r.DayLocation : r.EveningLocation).Distance(p)).FirstOrDefault();
        return (quest, resident, interior);
    }

    /// <summary>Names the nearest interactable instead of displaying inaccessible quest placeholders.</summary>
    private string NearbyHint(WorldPoint p)
    {
        var (quest, resident, interior) = ResolveInteractable(p);
        if (quest != null) return $"{WorldText.Get(quest.Objective.Description)} / {WorldText.Get("Interact")}";
        var openEntrance = _world.NearbyEntrance(p);
        if (openEntrance != null && openEntrance.Mission <= Progress) return $"{WorldText.Get("Enter mission")} {openEntrance.Mission} / {WorldText.Get("Interact")}";
        if (resident != null) return $"{WorldText.Get(resident.Name)} ({WorldText.Get(resident.Title)}) / {WorldText.Get("Interact")}";
        if (interior != null) return $"{WorldText.Get(interior.Name)} / {WorldText.Get("Interact")}";

        // Open entrances returned earlier, so only a locked one can remain here.
        if (openEntrance != null) return $"{WorldText.Get("Mission locked")} {openEntrance.Mission}";

        var operation = OperationAt(p);
        if (operation != null) return $"{WorldText.Get(operation.Name)} / {WorldText.Get("Interact")}";
        if (AtSkyport(p)) return $"{WorldText.Get("Skyport")} / {WorldText.Get("Interact")}";

        // Elevator
        if (_world.Elevators.Any(e => e.Distance(p with { Layer = 0 }) < 8)) return $"{WorldText.Get("Elevator")} / {WorldText.Get("Interact")}";

        // Transit station
        if (_world.NearestStation(p).Position.Distance(p with { Layer = 0 }) < 12) return $"{WorldText.Get("Free transit")} / {WorldText.Get("Open map")}";

        // Landmark
        var landmark = _world.Landmarks.FirstOrDefault(e => e.Position.Distance(p) < 30);
        if (landmark != null) return WorldText.Get(landmark.Name);

        // Vehicle status
        string vehicleHint;
        if (_player.IsRidingBike) vehicleHint = "Dismount bike: V";
        else vehicleHint = "Electric bike: V";
        return $"{WorldText.Get("Walk · Run · Explore")} · {vehicleHint}";
    }

    /// <summary>Handles map, interaction, vehicle toggle, waypoint and squad actions with a single operation gate.</summary>
    public override void _UnhandledInput(InputEvent input)
    {
        if (_conversation.IsOpen)
        {
            if (input.IsActionPressed("ui_cancel")) { _conversation.Cancel(); GetViewport().SetInputAsHandled(); }
            return;
        }
        if (input.IsActionPressed("world_map")) { ToggleMap(); GetViewport().SetInputAsHandled(); return; }
        // The controller's attack button doubles as ui_cancel, so combat is read first when no menu is open.
        if (!_mapOpen && !_busy && _loaded && HandleCombatInput(input)) { GetViewport().SetInputAsHandled(); return; }
        if (input.IsActionPressed("ui_cancel")) { if (_mapOpen) ToggleMap(); GetViewport().SetInputAsHandled(); return; }
        if (_busy || !_loaded) return;
        if (_mapOpen)
        {
            if (input.IsActionPressed("world_zoom_in")) _map.ChangeZoom(1.3);
            if (input.IsActionPressed("world_zoom_out")) _map.ChangeZoom(1 / 1.3);
            if (input.IsActionPressed("world_interact")) _map.SetCrosshairWaypoint();
            return;
        }

        if (input.IsActionPressed("world_bike"))
        {
            // Boats stay out of play until waterways work for vehicles, so V only ever means the bike.
            _player.ToggleBike(PositionMetres);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (input.IsActionPressed("world_swap"))
        {
            OperativeRoster.Current.Cycle(1);
            _player.RefreshOperative();
            GetNodeOrNull<Node>("/root/Global")?.Set("selected_character", OperativeRoster.Current.Active);
        }
        if (input.IsActionPressed("world_interact")) Interact();
    }

    /// <summary>Launches missions, safehouses, resident dialogues, or elevators.</summary>
    private void Interact()
    {
        var p = PositionMetres;

        // 1. A current quest step always wins, then an open mission, then a resident, then a building.
        var (quest, resident, interior) = ResolveInteractable(p);
        if (quest != null) { OpenObjective(quest); return; }
        // Residents stand close to some terminals, and must not take over a mission the player is standing at.
        var missionHere = _world.NearbyEntrance(p);
        if (missionHere != null && missionHere.Mission <= Progress) { RunOperation(() => EnterMission(missionHere.Mission)); return; }
        if (resident != null)
        {
            // A resident standing at a door must not hide it, so the conversation offers the building too.
            var door = interior;
            _conversation.Open(resident.Name, new[] { resident.GreetingDialogue }, null, _ => { }, null, door?.Name, door == null ? null : () => EnterSafehouse(door));
            return;
        }
        if (interior != null) { EnterSafehouse(interior); return; }

        var operation = OperationAt(p);
        if (operation != null) { RunOperation(() => EnterOperation(operation)); return; }
        if (AtSkyport(p)) { OpenSkyport(); return; }

        // 4. Check Elevators
        for (int i = 0; i < _world.Elevators.Count; i++)
        {
            if (_world.Elevators[i].Distance(p with { Layer = 0 }) < 8 && (p.Layer == 0 || p.Layer == i + 1))
            {
                int layer = p.Layer == 0 ? i + 1 : 0;
                var target = _world.Elevators[i] with { Layer = layer };
                RunOperation(() => MoveCommitted(target));
                return;
            }
        }

        // 5. Check Transit station
        if (_world.NearestStation(p).Position.Distance(p with { Layer = 0 }) < 12) ToggleMap();
    }

    /// <summary>
    /// Stows the vehicle. Places offering care restore vitals and become the defeat return point.
    /// Other buildings give no healing, so an interior inside a hostile pocket isn't a free recovery.
    /// </summary>
    private void EnterSafehouse(InteriorDefinition interior)
    {
        if (_player.IsRidingBike) _player.ToggleBike(PositionMetres);
        bool heals = interior.ProvidedServices.Any(s => s is "rest_heal" or "medical_clinic" or "first_aid");
        if (!heals) { ShowToast(WorldText.Get(interior.Name)); return; }
        _player.RestoreFullVitals();
        ShowToast($"{WorldText.Get(interior.Name)} · {WorldText.Get("Vitals restored")}");
        var entrance = interior.EntranceLocation;
        RunOperation(() => Commit(BuildSnapshot() with { LastExtraction = entrance }));
    }

    /// <summary>Shows a short message that survives the per-frame hint refresh.</summary>
    private void ShowToast(string text, double seconds = 4)
    {
        _toast = text;
        _toastSeconds = seconds;
    }

    /// <summary>Reads the combat actions. Returns true when the event was used.</summary>
    private bool HandleCombatInput(InputEvent input)
    {
        if (input.IsActionPressed("world_attack"))
        {
            if (_player.ExecuteAttack())
                _encounter.ResolveAttack(PositionMetres, _player.Facing, OperativeRegistry.GetProfile(OperativeRoster.Current.Active).MeleePrimaryDamage);
            return true;
        }
        if (input.IsActionPressed("world_dodge")) { _player.ExecuteDodge(); return true; }
        if (input.IsActionPressed("world_defend")) { _player.ExecuteDefend(true); return true; }
        if (input.IsActionReleased("world_defend")) { _player.ExecuteDefend(false); return true; }
        return false;
    }

    /// <summary>Shows an objective's conversation. Finishing advances it; leaving changes nothing.</summary>
    private void OpenObjective(QuestAction action)
    {
        var talk = WorldDialogueCatalog.For(action.Objective);
        var choices = action.Objective.Type == ObjectiveType.MakeConsequentialChoice ? action.Quest.BranchChoices : null;
        _conversation.Open(talk.Speaker, talk.Lines, choices, choice => AdvanceObjective(action, choice));
    }

    /// <summary>Stores the next step, applying state only after the write succeeds; the final step completes the quest.</summary>
    private void AdvanceObjective(QuestAction action, string? choice)
    {
        var quest = action.Quest;
        _trackedQuest = quest.Id;
        int next = action.ObjectiveIndex + 1;
        if (next >= quest.Objectives.Count) { CompleteQuest(quest); return; }
        var active = State.ActiveQuests != null ? new Dictionary<string, int>(State.ActiveQuests) : new();
        active[quest.Id] = next;
        var snapshot = BuildSnapshot() with { ActiveQuests = active };
        if (choice != null)
        {
            var choices = State.DistrictChoices != null ? new Dictionary<int, string>(State.DistrictChoices) : new();
            choices[quest.RegionId] = choice;
            snapshot = snapshot with { DistrictChoices = choices };
        }
        RunOperation(() => Commit(snapshot));
    }

    /// <summary>
    /// Commits rewards and completion together. Any receipt other than success throws, so
    /// RunOperation offers Retry and the HUD can't report a save that did not happen.
    /// </summary>
    private void CompleteQuest(QuestDefinition quest)
    {
        string? branch = null;
        if (quest.BranchChoices is { Count: > 0 }) State.DistrictChoices?.TryGetValue(quest.RegionId, out branch);
        if (!_pendingCompletions.TryGetValue(quest.Id, out string? token)) _pendingCompletions[quest.Id] = token = Guid.NewGuid().ToString();
        string? finalBranch = branch;
        RunOperation(async () =>
        {
            if (_session > 0)
            {
                var receipt = await WorldTransactionService.CompleteQuestAsync(_session, quest.Id, finalBranch, token);
                if (receipt.Status is not (TransactionStatus.Success or TransactionStatus.AlreadyCommitted))
                    throw new InvalidOperationException($"Quest completion refused: {receipt.Status}");
                if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            }
            _pendingCompletions.Remove(quest.Id);
            State = BuildSnapshot().WithQuestCompleted(quest.Id, finalBranch);
            ShowToast($"{WorldText.Get(quest.Title)} · +{quest.Reward.HeroSparks} {WorldText.Get("Sparks")} · +{quest.Reward.CityCredits} {WorldText.Get("Credits")}");
        });
    }

    /// <summary>Advances the combat objective whose wave was just cleared.</summary>
    private void OnWaveCleared(string objectiveId)
    {
        foreach (var quest in WorldQuestRules.QuestsFor(State))
        {
            if (!WorldQuestRules.IsAvailable(quest, State, Progress, IsRecruited)) continue;
            var action = WorldQuestRules.Current(quest, State);
            if (action?.Objective.Id != objectiveId) continue;
            // A personal quest only counts for its own operative; after a mid-fight swap the wave can be retried.
            if (quest.RequiredOperative != null && quest.RequiredOperative != OperativeRoster.Current.Active)
            {
                ShowToast(WorldText.Get(quest.RequiredOperative));
                return;
            }
            AdvanceObjective(action, null);
            return;
        }
    }

    /// <summary>
    /// Ends the fight and returns to the last safe point. Quests and equipment are kept, and
    /// only the unfinished wave resets. If the save fails, Retry repeats the same move.
    /// </summary>
    private void OnPlayerDefeated()
    {
        _encounter.Clear();
        _player.RestoreFullVitals();
        ShowToast(WorldText.Get("Defeated. Returned to safe arrival."));
        var safe = State.LastExtraction;
        RunOperation(() => MoveCommitted(safe));
    }

    /// <summary>Stores mission-resume mode and selected operative before any introduction or scene change.</summary>
    private async Task EnterMission(int mission)
    {
        await Commit(BuildSnapshot() with { Resume = "mission", Mission = mission });
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        CampaignProgress.PrepareMission(mission, State.Operative);
        LoadingScreen.LoadScene(GetTree(), WorldMissionRouting.LaunchPath(mission));
    }

    /// <summary>The city's operation terminal within reach, if any.</summary>
    private SideOperationDefinition? OperationAt(WorldPoint p) => _world.Content.Operations.FirstOrDefault(o => o.TerminalLocation.Distance(p) < 10);

    /// <summary>Whether the player is standing at this city's Skyport terminal.</summary>
    private bool AtSkyport(WorldPoint p) => _world.Skyport.Distance(p) < 10;

    /// <summary>Saves, then starts a city operation. Finishing it reloads this scene in the same city.</summary>
    private async Task EnterOperation(SideOperationDefinition operation)
    {
        await Commit(BuildSnapshot());
        if (GodotObject.IsInstanceValid(this) && IsInsideTree()) LoadingScreen.LoadScene(GetTree(), operation.ScenePath);
    }

    /// <summary>Offers every open city as a choice. Flights are free, and blocked during pursuit like transit.</summary>
    private void OpenSkyport()
    {
        if (CurrentAlert == AlertState.Pursuit) { ShowToast(WorldText.Get("Skyport closed during pursuit")); return; }
        var choices = CityRegistry.All.Where(c => c != _world && CityRegistry.IsOpen(c, IsRecruited))
            .Select(c => new QuestBranchChoice(c.Id, c.DisplayName, "Free flight", "", "")).ToArray();
        if (choices.Length == 0) { ShowToast(WorldText.Get("Skyport")); return; }
        _conversation.Open("Skyport", new[] { "Choose a destination." }, choices, id =>
        {
            var city = CityRegistry.ById(id);
            if (city != null) RunOperation(() => TravelTo(city));
        });
    }

    /// <summary>Stores the departure and arrival together, then swaps the loaded city in place. A failed save leaves the player where they were.</summary>
    private async Task TravelTo(WorldDefinition destination)
    {
        var departing = BuildSnapshot();
        WorldSave arrival;
        if (_session > 0) arrival = await WorldStorage.TravelAsync(_session, departing, destination);
        else
        {
            _previewRecords[departing.World] = departing;
            var home = _previewRecords.GetValueOrDefault(WorldCatalog.Identity) ?? WorldSave.AfterMission(1);
            arrival = WorldStorage.TravelArrival(home, departing, destination, _previewRecords.GetValueOrDefault(destination.Identity));
        }
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        if (_player.IsRidingBike) _player.ToggleBike(PositionMetres);
        State = arrival;
        _trackedQuest = null;
        UseCity(destination);
        Place(arrival.Position);
        PopulateDestinations();
    }

    /// <summary>Points every system at a city. A new stream means a batch still running for the old city is discarded.</summary>
    private void UseCity(WorldDefinition city)
    {
        _player.City = city;
        if (city == _world) { RefreshCityHud(); return; }
        _world = city;
        _stream = new WorldStream(city);
        _map.ResetView();
        RefreshCityHud();
    }

    /// <summary>Updates the map heading and layer list; a city with no underground hides the layer control.</summary>
    private void RefreshCityHud()
    {
        _mapTitle.Text = WorldText.Get(_world.MapTitle);
        _layers.Clear();
        foreach (string text in _world.LayerNames) _layers.AddItem(WorldText.Get(text));
        _layers.Visible = _world.LayerNames.Count > 1;
    }

    /// <summary>Free transit/elevator travel commits first; a failed write leaves position and last committed state unchanged.</summary>
    private async Task MoveCommitted(WorldPoint destination)
    {
        await Commit(BuildSnapshot() with { Position = destination, Resume = "world" });
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        Place(destination);
        if (_mapOpen) ToggleMap();
    }

    /// <summary>Commits an immutable snapshot to PostgreSQL. Preview mode explicitly has no persistent campaign.</summary>
    private async Task Commit(WorldSave candidate)
    {
        if (_session > 0) await WorldStorage.SaveAsync(_session, candidate);
        if (GodotObject.IsInstanceValid(this) && IsInsideTree()) State = candidate;
    }

    /// <summary>Retries the same operation after an outage and holds navigation/movement until storage confirms success.</summary>
    private async void RunOperation(Func<Task> operation)
    {
        if (_busy && _retryOperation != null) return;
        _busy = true;
        _retryOperation = operation;
        _retry.Visible = _mapRetry.Visible = false;
        _status.Text = _mapStatus.Text = WorldText.Get("Saving campaign…");
        try
        {
            await operation();
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            _retryOperation = null;
            _busy = false;
            _status.Text = _mapStatus.Text = WorldText.Get(_session > 0 ? "Campaign saved" : "Preview · no active campaign");
        }
        catch (Exception)
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            _status.Text = _mapStatus.Text = WorldText.Get("Save failed. Retry to continue.");
            _retry.Visible = _mapRetry.Visible = true;
            (_mapOpen ? _mapRetry : _retry).GrabFocus();
        }
    }

    /// <summary>Both HUD and map retries repeat the same retained operation, with no duplicate in-flight writes.</summary>
    private void RetryPendingOperation()
    {
        var retry = _retryOperation;
        _retryOperation = null;
        if (retry != null) RunOperation(retry);
    }

    /// <summary>Stores one validated waypoint. Changing it never teleports or launches missions.</summary>
    public void SetWaypoint(WorldPoint p)
    {
        if (_busy || !_world.InBounds(p)) return;
        RunOperation(() => Commit(BuildSnapshot() with { Waypoint = p }));
    }

    /// <summary>Builds nearby landmarks, terminals, residents, and safehouses at local coordinates.</summary>
    private void RebuildMarkers()
    {
        foreach (var marker in _markers) { RemoveChild(marker); marker.QueueFree(); }
        _markers.Clear();

        foreach (var station in _world.Stations) AddMarker(station.Position, $"{WorldText.Get("Free transit")} {station.Id + 1}", new Color("4edee5"), false);
        foreach (var landmark in _world.Landmarks) AddMarker(landmark.Position, WorldText.Get(landmark.Name), new Color("e8bd76"), true);
        foreach (var entrance in _world.Entrances) AddMarker(entrance.Position, $"{WorldText.Get(entrance.Mission <= Progress ? "Enter mission" : "Mission locked")} {entrance.Mission}", new Color("ffb26a"), false);
        AddMarker(_world.Skyport, WorldText.Get("Skyport"), new Color("9ad0ff"), false);
        foreach (var operation in _world.Content.Operations) AddMarker(operation.TerminalLocation, WorldText.Get(operation.Name), new Color("ffb26a"), false);
        for (int i = 0; i < _world.Elevators.Count; i++)
        {
            AddMarker(_world.Elevators[i], WorldText.Get("Elevator"), new Color("89d29c"), false);
            AddMarker(_world.Elevators[i] with { Layer = i + 1 }, WorldText.Get("Elevator"), new Color("89d29c"), false);
        }

        // Add Safehouses & Interiors
        foreach (var interior in _world.Content.Interiors)
        {
            AddMarker(interior.EntranceLocation, WorldText.Get(interior.Name), new Color("ff80e5"), false);
        }

        // Add Named Residents
        bool isDay = WorldQuestRules.IsDay(_worldTimeMinutes);
        foreach (var res in _world.Content.Residents)
        {
            WorldPoint pos = isDay ? res.DayLocation : res.EveningLocation;
            AddMarker(pos, $"{WorldText.Get(res.Name)} [{res.Title}]", new Color("a0e080"), false);
        }
    }

    /// <summary>Creates a named plaza beacon; distinctive monument silhouettes are selected by destination ID.</summary>
    private void AddMarker(WorldPoint p, string text, Color color, bool monument)
    {
        if (p.Layer != _origin.Layer || Math.Abs(p.Chunk.X - _origin.X) > 1 || Math.Abs(p.Chunk.Y - _origin.Y) > 1) return;
        var marker = new Node2D { Position = ExplorationPlayer.Project(p.X - _origin.X * 128, p.Y - _origin.Y * 128), ZIndex = 2 };
        AddChild(marker);
        _markers.Add(marker);
        marker.AddChild(new Polygon2D { Polygon = new[] { new Vector2(-12, 0), new(0, -7), new(12, 0), new(0, 7) }, Color = color });

        if (monument)
        {
            var landmark = _world.Landmarks.FirstOrDefault(l => l.Position == p);
            if (landmark != null)
            {
                string path = $"{_world.ArtFolder}landmark_{landmark.Id}.png";
                if (landmark.Kind != "plaza" && ResourceLoader.Exists(path)) marker.AddChild(new Sprite2D { Texture = GD.Load<Texture2D>(path), Offset = new(0, -12) });
                if (landmark.Kind != "plaza")
                {
                    var footprint = new StaticBody2D { CollisionLayer = 1, CollisionMask = 2 };
                    footprint.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = ExplorationPlayer.Project(12, 8) } });
                    marker.AddChild(footprint);
                }
            }
        }

        bool plaza = monument && _world.Landmarks.Any(l => l.Position == p && l.Kind == "plaza");
        var label = new Label { Text = text, Position = new(-160, plaza ? -100 : -65), Size = new(320, 40), HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", color);
        marker.AddChild(label);
    }

    /// <summary>Builds responsive map/sidebar controls with native focus navigation and an always reachable retry button.</summary>
    private void BuildHud()
    {
        _hud = new CanvasLayer { Name = "WorldHUD", Layer = 30 };
        AddChild(_hud);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _hud.AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var header = new VBoxContainer { Position = new(24, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(header);
        _heading = new Label();
        _heading.AddThemeFontSizeOverride("font_size", 24);
        header.AddChild(_heading);

        _questLabel = new Label { Modulate = new Color("ffea75") };
        _questLabel.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_questLabel);

        _hint = new Label();
        header.AddChild(_hint);
        // Help lines are rich labels so controller prompts show button images (InputGlyphText).
        var movementHelp = HelpLabel();
        movementHelp.Name = "MovementHelp";
        movementHelp.CustomMinimumSize = new(600, 0);
        header.AddChild(movementHelp);

        // On-screen joystick and buttons for phones and tablets; the key/controller help lines hide
        // while they show, because the buttons label themselves.
        _touchControls = TouchControls.Create(TouchLayout.World);
        AddChild(_touchControls);
        InputPromptBinding.Track(movementHelp, () => InputGlyphText.Set(movementHelp, string.Join("  |  ",
            InputPromptBinding.Marked("world_left", "Left"), InputPromptBinding.Marked("world_right", "Right"),
            InputPromptBinding.Marked("world_up", "Up"), InputPromptBinding.Marked("world_down", "Down"),
            InputPromptBinding.Marked("world_run", "Run"), InputPromptBinding.Marked("world_bike", "Bike"),
            InputPromptBinding.Marked("world_map", "Map"), InputPromptBinding.Marked("world_interact", "Interact"), InputPromptBinding.Marked("world_swap", "Squad"))));

        var combatHelp = HelpLabel();
        combatHelp.Name = "CombatHelp";
        combatHelp.CustomMinimumSize = new(600, 0);
        header.AddChild(combatHelp);
        InputPromptBinding.Track(combatHelp, () => InputGlyphText.Set(combatHelp, string.Join("  |  ",
            InputPromptBinding.Marked("world_attack", "Attack"), InputPromptBinding.Marked("world_dodge", "Dodge"), InputPromptBinding.Marked("world_defend", "Guard"))));
        // Help lines hide while the touch controls show (the buttons label themselves) and while the
        // map is open (map suppression also fires ShownChanged(false); review finding cb-N6).
        void ShowHelp(bool touchShown) { movementHelp.Visible = !touchShown && !_mapOpen; combatHelp.Visible = !touchShown && !_mapOpen; }
        _touchControls.ShownChanged += ShowHelp;
        ShowHelp(_touchControls.Shown);
        _refreshWorldHelp = () => ShowHelp(_touchControls.Shown);
        // Squad only cycles the unlocked roster, so it is pointless with one operative (review ir-N3).
        _touchControls.SetButtonVisible("world_swap", OperativeRoster.Current.Unlocked.Count > 1);
        _combatLabel = new Label();
        header.AddChild(_combatLabel);

        _status = new Label();
        header.AddChild(_status);
        _retry = new Button { Text = WorldText.Get("Retry Save"), Visible = false };
        header.AddChild(_retry);
        _retry.Pressed += RetryPendingOperation;

        _mini = new WorldMap { World = this, Mini = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_mini);
        _mini.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _mini.OffsetLeft = -284;
        _mini.OffsetRight = -24;
        _mini.OffsetTop = 24;
        _mini.OffsetBottom = 204;

        _conversation = new WorldConversationPanel();
        root.AddChild(_conversation);

        _mapPanel = new PanelContainer { Name = "WorldMapPanel", Visible = false };
        _mapPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("0c1826") });
        root.AddChild(_mapPanel);
        _mapPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        _mapPanel.AddChild(margin);

        var row = new HBoxContainer();
        margin.AddChild(row);
        _map = new WorldMap { Name = "WorldMapViewport", World = this, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All };
        row.AddChild(_map);

        var side = new VBoxContainer { CustomMinimumSize = new(320, 0) };
        row.AddChild(side);
        _mapTitle = new Label { Text = WorldText.Get(_world.MapTitle) };
        side.AddChild(_mapTitle);
        _mapStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        side.AddChild(_mapStatus);
        _mapRetry = AddButton(side, "Retry Save", RetryPendingOperation);
        _mapRetry.Visible = false;

        _layers = new OptionButton();
        foreach (string text in _world.LayerNames) _layers.AddItem(WorldText.Get(text));
        _layers.ItemSelected += index => _map.SelectLayer((int)index);
        side.AddChild(_layers);

        var zoom = new HBoxContainer();
        side.AddChild(zoom);
        AddButton(zoom, "+", () => _map.ChangeZoom(1.3));
        AddButton(zoom, "−", () => _map.ChangeZoom(1 / 1.3));

        side.AddChild(new Label { Text = WorldText.Get("Transit · landmarks · safehouses · mission entrances") });
        _destinations = new ItemList { Name = "WorldDestinations", SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(320, 160) };
        side.AddChild(_destinations);
        _destinations.ItemSelected += index =>
        {
            int i = (int)index;
            _map.FocusPoint(_destinationPoints[i]);
            _layers.Select(_map.Layer);
            _travel.Disabled = CurrentAlert == AlertState.Pursuit || _destinationStations[i] == null;
        };

        _travel = AddButton(side, "Free transit", () =>
        {
            int[] selected = _destinations.GetSelectedItems();
            if (selected.Length > 0 && _destinationStations[selected[0]] is WorldStation station && !_busy && CurrentAlert != AlertState.Pursuit)
                RunOperation(() => MoveCommitted(station.Position));
        });

        AddButton(side, "Set waypoint", () =>
        {
            int[] selected = _destinations.GetSelectedItems();
            if (selected.Length > 0) SetWaypoint(_destinationPoints[selected[0]]);
            else _map.SetCrosshairWaypoint();
        });

        AddButton(side, "Clear waypoint", () =>
        {
            if (!_busy) RunOperation(() => Commit(BuildSnapshot() with { Waypoint = null }));
        });

        AddButton(side, "Return to main menu", () =>
        {
            // After a failed save Retry might never succeed, so the player may leave and lose only the unsaved step.
            if (_busy && _retry.Visible)
            {
                if (IsInsideTree()) LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/main_menu.tscn");
                return;
            }
            if (!_busy) RunOperation(async () =>
            {
                await Commit(BuildSnapshot());
                if (GodotObject.IsInstanceValid(this) && IsInsideTree()) LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/main_menu.tscn");
            });
        });

        AddButton(side, "Close map", ToggleMap);
        var mapHelp = HelpLabel();
        mapHelp.Name = "MapHelp";
        side.AddChild(mapHelp);
        InputPromptBinding.Track(mapHelp, () => InputGlyphText.Set(mapHelp, string.Join("\n",
            InputPromptBinding.Marked("world_left", "Pan left"), InputPromptBinding.Marked("world_right", "Pan right"),
            InputPromptBinding.Marked("world_zoom_in", "Zoom in"), InputPromptBinding.Marked("world_zoom_out", "Zoom out"),
            InputPromptBinding.Marked("world_map", "Close map")) + (InputPromptService.Instance?.IsGamepad == true ? "" :
            "\n" + WorldText.Get("Right-drag to pan · click for waypoint"))));
    }

    /// <summary>This world's on-screen touch controls; used by tests.</summary>
    public TouchControls? TouchControlsNode => _touchControls;
    private TouchControls? _touchControls;
    // Re-applies the help-line visibility after the map opens or closes.
    private Action? _refreshWorldHelp;

    /// <summary>A wrapping help label that can draw controller button images, at the theme's normal 16 px size.</summary>
    private static RichTextLabel HelpLabel() => InputGlyphText.CreateLabel(16, Colors.White);

    /// <summary>Creates a translated, focusable button whose container controls sizing.</summary>
    private static Button AddButton(Container parent, string text, Action action)
    {
        var button = new Button { Text = WorldText.Get(text) };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    /// <summary>Lists all destinations immediately; mission progress changes status text rather than hiding geography.</summary>
    private void PopulateDestinations()
    {
        _destinations.Clear();
        _destinationPoints.Clear();
        _destinationStations.Clear();
        foreach (var entrance in _world.Entrances) Destination($"{WorldText.Get(entrance.Mission <= Progress ? "Enter mission" : "Mission locked")} {entrance.Mission}", entrance.Position, null);
        foreach (var landmark in _world.Landmarks) Destination(WorldText.Get(landmark.Name), landmark.Position, null);
        Destination(WorldText.Get("Skyport"), _world.Skyport, null);
        foreach (var operation in _world.Content.Operations) Destination(WorldText.Get(operation.Name), operation.TerminalLocation, null);
        // Only safehouses in playable areas are listed.
        foreach (var safehouse in _world.Content.Interiors.Where(i => _world.Content.ListedSafehouseIds.Contains(i.Id))) Destination(WorldText.Get(safehouse.Name), safehouse.EntranceLocation, null);
        foreach (var station in _world.Stations) Destination($"{WorldText.Get(_world.Region(station.Position).Name)} / {station.Id + 1}", station.Position, station);
        _destinations.Select(0);
        _travel.Disabled = true;
    }

    /// <summary>Keeps native list indexes associated with stable positions and optional transit endpoints.</summary>
    private void Destination(string label, WorldPoint p, WorldStation? station)
    {
        _destinations.AddItem(label);
        _destinationPoints.Add(p);
        _destinationStations.Add(station);
    }

    /// <summary>Map blocks movement and starts with keyboard/controller focus on the destination list.</summary>
    private void ToggleMap()
    {
        _mapOpen = !_mapOpen;
        _mapPanel.Visible = _mapOpen;
        _mini.Visible = !_mapOpen;
        // The map has its own touch handling (tap for a waypoint) and a Close button.
        _touchControls?.SetSuppressed(_mapOpen);
        _refreshWorldHelp?.Invoke();
        if (_mapOpen) _destinations.GrabFocus();
    }

    /// <summary>Restores menu cursor/prompt policy without stopping containers or touching the user's editor.</summary>
    public override void _ExitTree()
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", true);
    }
}
