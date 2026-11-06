using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Streamed Neo-Kyoto exploration scene. Scene mutations and UI run on the main thread; workers produce data only.</summary>
public partial class NeoKyotoWorld : Node2D
{
    private readonly WorldStream _stream=new();
    private readonly Dictionary<ChunkAddress,WorldChunkView> _views=new();
    private readonly List<Node2D> _markers=new();
    private ExplorationPlayer _player=null!;
    private ChunkAddress _origin;
    private CanvasLayer _hud=null!;
    private Control _mapPanel=null!;
    private WorldMap _map=null!,_mini=null!;
    private Label _status=null!,_heading=null!,_hint=null!;
    private Label _mapStatus=null!;
    private ItemList _destinations=null!;
    private readonly List<WorldPoint> _destinationPoints=new();
    private readonly List<WorldStation?> _destinationStations=new();
    private Button _travel=null!,_retry=null!;
    private Button _mapRetry=null!;
    private Func<Task>? _retryOperation;
    private bool _busy=true,_loaded,_mapOpen;
    private double _autosave;
    private int _session;
    public int Progress { get; private set; }=1;
    public WorldSave State { get; private set; }=WorldSave.AfterMission(1);
    public WorldPoint PositionMetres => _player==null?State.Position:new(_origin.X*128+_player.Position.X/16,_origin.Y*128+_player.Position.Y/(16*.65),_origin.Layer);
    public int ResidentChunks => _views.Count;
    public int CachedChunks => _stream.Cache.Count;
    /// <summary>Read-only diagnostic snapshot for Godot MCP, automated tours and on-machine streaming measurements.</summary>
    public Godot.Collections.Dictionary InspectionState()
    {
        var p=PositionMetres;
        return new() {{"x",p.X},{"y",p.Y},{"layer",p.Layer},{"resident",ResidentChunks},{"cached",CachedChunks},{"busy",_busy},{"map_open",_mapOpen},{"progress",Progress},{"moving_enabled",_player?.MovementEnabled??false}};
    }

    /// <summary>Builds UI and starts a database load. Direct scene inspection receives an unsaved preview, never a local save.</summary>
    public override void _Ready()
    {
        AddToGroup("world_exploration");
        ConfigureInput(); Input.MouseMode=Input.MouseModeEnum.Visible;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible",false);
        _player=new ExplorationPlayer {Name="ExplorationPlayer"}; AddChild(_player);
        BuildHud();
        _session=SessionManager.Instance?.CurrentSessionId??-1;
        Progress=Math.Max(1,GetNodeOrNull<Node>("/root/Global")?.Get("unlocked_levels").AsInt32()??1);
        RunOperation(LoadWorld);
    }
    /// <summary>Loads only the selected campaign, migrates legacy campaigns once, and never treats an outage as missing data.</summary>
    private async Task LoadWorld()
    {
        string preferred=GetNodeOrNull<Node>("/root/Global")?.Get("selected_character").AsString()??OperativeRoster.Starter;
        WorldSave? loaded=_session>0?await WorldStorage.LoadAsync(_session,Progress,preferredOperative:preferred):null;
        if(!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        State=loaded??WorldSave.AfterMission(1);
        if(_session>0 && loaded==null && Progress==1) throw new InvalidOperationException("Finish Mission 1 before entering Neo-Kyoto.");
        var roster=OperativeRoster.BeginLevel(Progress);
        // Exploration uses the whole earned squad; Mission 5's deployment restriction is mission-specific.
        if(Progress==5) roster=OperativeRoster.BeginLevel(6);
        roster.SetActive(State.Operative); _player.RefreshOperative();
        GetNodeOrNull<Node>("/root/Global")?.Set("selected_character",roster.Active);
        if(_session>0) await WorldStorage.SaveAsync(_session,State with {Resume="world",Operative=roster.Active});
        if(!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        Place(State.Position); _loaded=true; PopulateDestinations();
    }
    /// <summary>Registers world-specific named actions with keyboard, D-pad and analog defaults without changing mission controls.</summary>
    private static void ConfigureInput()
    {
        AddAction("world_left",Key.A,JoyButton.DpadLeft,JoyAxis.LeftX,-1);
        AddAction("world_right",Key.D,JoyButton.DpadRight,JoyAxis.LeftX,1);
        AddAction("world_up",Key.W,JoyButton.DpadUp,JoyAxis.LeftY,-1);
        AddAction("world_down",Key.S,JoyButton.DpadDown,JoyAxis.LeftY,1);
        AddAction("world_run",Key.Shift,JoyButton.LeftStick);
        AddAction("world_map",Key.M,JoyButton.Back);
        AddAction("world_interact",Key.E,JoyButton.A);
        AddAction("world_swap",Key.Tab,JoyButton.Y);
        AddAction("world_zoom_in",Key.Equal,JoyButton.RightShoulder);
        AddAction("world_zoom_out",Key.Minus,JoyButton.LeftShoulder);
    }
    /// <summary>Adds defaults only once, retaining any action bindings already configured in the project.</summary>
    private static void AddAction(string name,Key key,JoyButton button,JoyAxis? axis=null,float value=0)
    {
        if(InputMap.HasAction(name)) return;
        InputMap.AddAction(name,.2f); InputMap.ActionAddEvent(name,new InputEventKey {PhysicalKeycode=key});
        InputMap.ActionAddEvent(name,new InputEventJoypadButton {ButtonIndex=button,Device=-1});
        if(axis!=null) InputMap.ActionAddEvent(name,new InputEventJoypadMotion {Axis=axis.Value,AxisValue=value,Device=-1});
    }
    /// <summary>Rebases rendering to an integer chunk and discards old layer views before a committed transit or elevator move.</summary>
    private void Place(WorldPoint p)
    {
        foreach(var view in _views.Values) { RemoveChild(view); view.QueueFree(); } _views.Clear();
        _origin=p.Chunk; _player.Position=ExplorationPlayer.Project(p.X-_origin.X*128,p.Y-_origin.Y*128);
        RebuildMarkers();
    }
    /// <summary>Shifts every resident object by a bounded delta, keeping coordinates precise across the million possible addresses.</summary>
    private void Rebase(ChunkAddress center)
    {
        Vector2 shift=ExplorationPlayer.Project((_origin.X-center.X)*128,(_origin.Y-center.Y)*128);
        _player.Position+=shift;
        foreach(var view in _views.Values) view.Position+=shift;
        foreach(var marker in _markers) marker.Position+=shift;
        _origin=center;
    }
    /// <summary>Pumps worker data, applies at most one chunk per frame, and blocks movement toward unloaded terrain.</summary>
    public override void _Process(double delta)
    {
        if(!_loaded) return;
        var p=PositionMetres; var center=p.Chunk;
        if(center!=_origin) { Rebase(center); RebuildMarkers(); }
        _stream.Pump(center);
        var cacheWindow=WorldStream.Window(center,2).ToArray();
        if(!_stream.Busy && cacheWindow.Any(a=>!_stream.Cache.ContainsKey(a))) _stream.Request(center);
        var active=WorldStream.Window(center,1).ToHashSet();
        foreach(var key in _views.Keys.Where(a=>!active.Contains(a)).ToArray()) { var view=_views[key]; RemoveChild(view); view.QueueFree(); _views.Remove(key); }
        // One local chunk is the scene-application unit. Generation never touches a Godot object off-thread.
        foreach(var key in WorldStream.Window(center,1)) if(!_views.ContainsKey(key) && _stream.Cache.TryGetValue(key,out var data))
        {
            var view=new WorldChunkView {Position=ExplorationPlayer.Project((key.X-_origin.X)*128,(key.Y-_origin.Y)*128)};
            AddChild(view); view.Initialize(data); _views[key]=view; break;
        }
        _player.MovementEnabled=!_busy && !_mapOpen && active.All(_views.ContainsKey);
        foreach(var view in _views.Values) view.UpdateRoofs(_player.GlobalPosition);
        _heading.Text=WorldText.Get(WorldCatalog.Region(p).Name)+"  /  "+WorldText.Get(OperativeRoster.Current.Active);
        _hint.Text=NearbyHint(p);
        if(!_busy && !_mapOpen && (_autosave+=delta)>20) { _autosave=0; if(_session>0) RunOperation(()=>Commit(State with {Position=PositionMetres,Operative=OperativeRoster.Current.Active})); }
    }
    /// <summary>Names the nearest interactable instead of displaying inaccessible quest placeholders.</summary>
    private string NearbyHint(WorldPoint p)
    {
        var entrance=WorldCatalog.NearbyEntrance(p);
        if(entrance!=null) return WorldText.Get(entrance.Mission<=Progress?"Enter mission":"Mission locked")+" "+entrance.Mission+" / "+WorldText.Get("Interact");
        if(WorldCatalog.Elevators.Any(e=>e.Distance(p with {Layer=0})<8)) return WorldText.Get("Elevator")+" / "+WorldText.Get("Interact");
        if(WorldCatalog.NearestStation(p).Position.Distance(p with {Layer=0})<12) return WorldText.Get("Free transit")+" / "+WorldText.Get("Open map");
        var landmark=WorldCatalog.Landmarks.FirstOrDefault(e=>e.Position.Distance(p)<30);
        return landmark!=null?WorldText.Get(landmark.Name):WorldText.Get("Walk · Run · Explore");
    }
    /// <summary>Handles map, interaction, waypoint and squad actions with a single operation gate.</summary>
    public override void _UnhandledInput(InputEvent input)
    {
        if(input.IsActionPressed("world_map")) { ToggleMap(); GetViewport().SetInputAsHandled(); return; }
        if(input.IsActionPressed("ui_cancel")) { if(_mapOpen) ToggleMap(); else ToggleMap(); GetViewport().SetInputAsHandled(); return; }
        if(_busy || !_loaded) return;
        if(_mapOpen)
        {
            if(input.IsActionPressed("world_zoom_in")) _map.ChangeZoom(1.3);
            if(input.IsActionPressed("world_zoom_out")) _map.ChangeZoom(1/1.3);
            if(input.IsActionPressed("world_interact")) _map.SetCrosshairWaypoint();
            return;
        }
        if(input.IsActionPressed("world_swap"))
        {
            OperativeRoster.Current.Cycle(1); _player.RefreshOperative();
            GetNodeOrNull<Node>("/root/Global")?.Set("selected_character",OperativeRoster.Current.Active);
        }
        if(input.IsActionPressed("world_interact")) Interact();
    }
    /// <summary>Launches only a nearby unlocked mission entrance; elevators connect both underground networks to the surface.</summary>
    private void Interact()
    {
        var p=PositionMetres;
        var entry=WorldCatalog.NearbyEntrance(p);
        if(entry!=null && entry.Mission<=Progress) { RunOperation(()=>EnterMission(entry.Mission)); return; }
        for(int i=0;i<2;i++) if(WorldCatalog.Elevators[i].Distance(p with {Layer=0})<8 && (p.Layer==0 || p.Layer==i+1))
        { int layer=p.Layer==0?i+1:0; var target=WorldCatalog.Elevators[i] with {Layer=layer}; RunOperation(()=>MoveCommitted(target)); return; }
        if(WorldCatalog.NearestStation(p).Position.Distance(p with {Layer=0})<12) ToggleMap();
    }
    /// <summary>Stores mission-resume mode and selected operative before any introduction or scene change.</summary>
    private async Task EnterMission(int mission)
    {
        await Commit(State with {Position=PositionMetres,Resume="mission",Mission=mission,Operative=OperativeRoster.Current.Active});
        if(!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        CampaignProgress.PrepareMission(mission,State.Operative);
        LoadingScreen.LoadScene(GetTree(),WorldMissionRouting.LaunchPath(mission));
    }
    /// <summary>Free transit/elevator travel commits first; a failed write leaves position and last committed state unchanged.</summary>
    private async Task MoveCommitted(WorldPoint destination)
    {
        await Commit(State with {Position=destination,Resume="world",Operative=OperativeRoster.Current.Active});
        if(!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        Place(destination); if(_mapOpen) ToggleMap();
    }
    /// <summary>Commits an immutable snapshot to PostgreSQL. Preview mode explicitly has no persistent campaign.</summary>
    private async Task Commit(WorldSave candidate)
    {
        if(_session>0) await WorldStorage.SaveAsync(_session,candidate);
        if(GodotObject.IsInstanceValid(this) && IsInsideTree()) State=candidate;
    }
    /// <summary>Retries the same operation after an outage and holds navigation/movement until storage confirms success.</summary>
    private async void RunOperation(Func<Task> operation)
    {
        if(_busy && _retryOperation!=null) return;
        _busy=true; _retryOperation=operation; _retry.Visible=_mapRetry.Visible=false;
        _status.Text=_mapStatus.Text=WorldText.Get("Saving campaign…");
        try
        {
            await operation();
            if(!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            _retryOperation=null; _busy=false; _status.Text=_mapStatus.Text=WorldText.Get(_session>0?"Campaign saved":"Preview · no active campaign");
        }
        catch(Exception)
        {
            if(!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            _status.Text=_mapStatus.Text=WorldText.Get("Save failed. Retry to continue.");
            _retry.Visible=_mapRetry.Visible=true; (_mapOpen?_mapRetry:_retry).GrabFocus();
        }
    }
    /// <summary>Both HUD and map retries repeat the same retained operation, with no duplicate in-flight writes.</summary>
    private void RetryPendingOperation()
    {
        var retry=_retryOperation; _retryOperation=null;
        if(retry!=null) RunOperation(retry);
    }
    /// <summary>Stores one validated waypoint. Changing it never teleports or launches missions.</summary>
    public void SetWaypoint(WorldPoint p)
    {
        if(_busy || !WorldCatalog.InBounds(p)) return;
        RunOperation(()=>Commit(State with {Position=PositionMetres,Waypoint=p}));
    }
    /// <summary>Builds nearby landmarks and terminals at local coordinates; scenery outside resident chunks is not instanced.</summary>
    private void RebuildMarkers()
    {
        foreach(var marker in _markers) { RemoveChild(marker); marker.QueueFree(); } _markers.Clear();
        var center=_origin;
        foreach(var station in WorldCatalog.Stations) AddMarker(station.Position,WorldText.Get("Free transit")+" "+(station.Id+1),new Color("4edee5"),false);
        foreach(var landmark in WorldCatalog.Landmarks) AddMarker(landmark.Position,WorldText.Get(landmark.Name),new Color("e8bd76"),true);
        foreach(var entrance in WorldCatalog.Entrances) AddMarker(entrance.Position,WorldText.Get(entrance.Mission<=Progress?"Enter mission":"Mission locked")+" "+entrance.Mission,new Color("ffb26a"),false);
        for(int i=0;i<2;i++) { AddMarker(WorldCatalog.Elevators[i],WorldText.Get("Elevator"),new Color("89d29c"),false); AddMarker(WorldCatalog.Elevators[i] with {Layer=i+1},WorldText.Get("Elevator"),new Color("89d29c"),false); }
    }
    /// <summary>Creates a named plaza beacon; distinctive monument silhouettes are selected by destination ID.</summary>
    private void AddMarker(WorldPoint p,string text,Color color,bool monument)
    {
        if(p.Layer!=_origin.Layer || Math.Abs(p.Chunk.X-_origin.X)>1 || Math.Abs(p.Chunk.Y-_origin.Y)>1) return;
        var marker=new Node2D {Position=ExplorationPlayer.Project(p.X-_origin.X*128,p.Y-_origin.Y*128),ZIndex=2};
        AddChild(marker); _markers.Add(marker);
        marker.AddChild(new Polygon2D {Polygon=new[]{new Vector2(-12,0),new(0,-7),new(12,0),new(0,7)},Color=color});
        if(monument)
        {
            var landmark=WorldCatalog.Landmarks.First(l=>l.Position==p);
            // Arrival plazas keep their foot position open; only scenic/architectural monuments rise above the ground.
            string path=$"res://assets/world/landmark_{landmark.Id}.png";
            if(landmark.Kind!="plaza" && ResourceLoader.Exists(path)) marker.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>(path),Offset=new(0,-12)});
            if(landmark.Kind!="plaza")
            {
                var footprint=new StaticBody2D {CollisionLayer=1,CollisionMask=2};
                footprint.AddChild(new CollisionShape2D {Shape=new RectangleShape2D {Size=ExplorationPlayer.Project(12,8)}}); marker.AddChild(footprint);
            }
        }
        bool plaza=monument && WorldCatalog.Landmarks.Any(l=>l.Position==p && l.Kind=="plaza");
        var label=new Label {Text=text,Position=new(-160,plaza?-100:-65),Size=new(320,40),HorizontalAlignment=HorizontalAlignment.Center};
        label.AddThemeFontSizeOverride("font_size",16); label.AddThemeColorOverride("font_color",color); marker.AddChild(label);
    }
    /// <summary>Builds responsive map/sidebar controls with native focus navigation and an always reachable retry button.</summary>
    private void BuildHud()
    {
        _hud=new CanvasLayer {Name="WorldHUD",Layer=30}; AddChild(_hud);
        var root=new Control {MouseFilter=Control.MouseFilterEnum.Ignore}; _hud.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var header=new VBoxContainer {Position=new(24,20),MouseFilter=Control.MouseFilterEnum.Ignore}; root.AddChild(header);
        _heading=new Label(); _heading.AddThemeFontSizeOverride("font_size",24); header.AddChild(_heading);
        _hint=new Label(); header.AddChild(_hint);
        header.AddChild(new Label {Text=WorldText.Get("Move: WASD / left stick · Run: Shift / L3 · Map: M / Back · Interact: E / A · Squad: Tab / Y")});
        _status=new Label(); header.AddChild(_status);
        _retry=new Button {Text=WorldText.Get("Retry Save"),Visible=false}; header.AddChild(_retry);
        _retry.Pressed+=RetryPendingOperation;
        _mini=new WorldMap {World=this,Mini=true,MouseFilter=Control.MouseFilterEnum.Ignore}; root.AddChild(_mini);
        _mini.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight); _mini.OffsetLeft=-284; _mini.OffsetRight=-24; _mini.OffsetTop=24; _mini.OffsetBottom=204;
        _mapPanel=new PanelContainer {Name="WorldMapPanel",Visible=false};
        _mapPanel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color("0c1826")});
        root.AddChild(_mapPanel); _mapPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var margin=new MarginContainer(); margin.AddThemeConstantOverride("margin_left",24); margin.AddThemeConstantOverride("margin_right",24); margin.AddThemeConstantOverride("margin_top",24); margin.AddThemeConstantOverride("margin_bottom",24); _mapPanel.AddChild(margin);
        var row=new HBoxContainer(); margin.AddChild(row);
        _map=new WorldMap {Name="WorldMapViewport",World=this,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,FocusMode=Control.FocusModeEnum.All}; row.AddChild(_map);
        var side=new VBoxContainer {CustomMinimumSize=new(320,0)}; row.AddChild(side);
        side.AddChild(new Label {Text=WorldText.Get("Neo-Kyoto · 128 × 128 km")});
        _mapStatus=new Label {AutowrapMode=TextServer.AutowrapMode.WordSmart}; side.AddChild(_mapStatus);
        _mapRetry=AddButton(side,"Retry Save",RetryPendingOperation); _mapRetry.Visible=false;
        var layers=new OptionButton(); foreach(string text in new[]{"Surface","Downtown underground","Foundry underworks"}) layers.AddItem(WorldText.Get(text));
        layers.ItemSelected+=index=>_map.SelectLayer((int)index); side.AddChild(layers);
        var zoom=new HBoxContainer(); side.AddChild(zoom); AddButton(zoom,"+",()=>_map.ChangeZoom(1.3)); AddButton(zoom,"−",()=>_map.ChangeZoom(1/1.3));
        side.AddChild(new Label {Text=WorldText.Get("Transit · landmarks · mission entrances")});
        _destinations=new ItemList {Name="WorldDestinations",SizeFlagsVertical=Control.SizeFlags.ExpandFill,CustomMinimumSize=new(320,160)}; side.AddChild(_destinations);
        _destinations.ItemSelected+=index=> { int i=(int)index; _map.FocusPoint(_destinationPoints[i]); layers.Select(_map.Layer); _travel.Disabled=_destinationStations[i]==null; };
        _travel=AddButton(side,"Free transit",()=> { int[] selected=_destinations.GetSelectedItems(); if(selected.Length>0 && _destinationStations[selected[0]] is WorldStation station && !_busy) RunOperation(()=>MoveCommitted(station.Position)); });
        AddButton(side,"Set waypoint",()=> { int[] selected=_destinations.GetSelectedItems(); if(selected.Length>0) SetWaypoint(_destinationPoints[selected[0]]); else _map.SetCrosshairWaypoint(); });
        AddButton(side,"Clear waypoint",()=> { if(!_busy) RunOperation(()=>Commit(State with {Position=PositionMetres,Waypoint=null})); });
        AddButton(side,"Return to main menu",()=> { if(!_busy) RunOperation(async()=> { await Commit(State with {Position=PositionMetres,Operative=OperativeRoster.Current.Active}); if(GodotObject.IsInstanceValid(this) && IsInsideTree()) LoadingScreen.LoadScene(GetTree(),"res://scenes/main_menu/main_menu.tscn"); }); });
        AddButton(side,"Close map",ToggleMap);
        side.AddChild(new Label {Text=WorldText.Get("Pan: WASD / left stick\nZoom: + − / shoulders\nRight-drag to pan · click for waypoint"),AutowrapMode=TextServer.AutowrapMode.WordSmart});
    }
    /// <summary>Creates a translated, focusable button whose container controls sizing.</summary>
    private static Button AddButton(Container parent,string text,Action action) { var button=new Button {Text=WorldText.Get(text)}; button.Pressed+=action; parent.AddChild(button); return button; }
    /// <summary>Lists all destinations immediately; mission progress changes status text rather than hiding geography.</summary>
    private void PopulateDestinations()
    {
        _destinations.Clear(); _destinationPoints.Clear(); _destinationStations.Clear();
        foreach(var entrance in WorldCatalog.Entrances) Destination(WorldText.Get(entrance.Mission<=Progress?"Enter mission":"Mission locked")+" "+entrance.Mission,entrance.Position,null);
        foreach(var landmark in WorldCatalog.Landmarks) Destination(WorldText.Get(landmark.Name),landmark.Position,null);
        foreach(var station in WorldCatalog.Stations) Destination(WorldText.Get(WorldCatalog.Region(station.Position).Name)+" / "+(station.Id+1),station.Position,station);
        _destinations.Select(0); _travel.Disabled=true;
    }
    /// <summary>Keeps native list indexes associated with stable positions and optional transit endpoints.</summary>
    private void Destination(string label,WorldPoint p,WorldStation? station) { _destinations.AddItem(label); _destinationPoints.Add(p); _destinationStations.Add(station); }
    /// <summary>Map blocks movement and starts with keyboard/controller focus on the destination list.</summary>
    private void ToggleMap() { _mapOpen=!_mapOpen; _mapPanel.Visible=_mapOpen; _mini.Visible=!_mapOpen; if(_mapOpen) _destinations.GrabFocus(); }
    /// <summary>Restores menu cursor/prompt policy without stopping containers or touching the user's editor.</summary>
    public override void _ExitTree() { Input.MouseMode=Input.MouseModeEnum.Hidden; GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible",true); }
}
