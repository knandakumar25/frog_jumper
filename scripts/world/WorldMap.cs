using Godot;
using System;
using System.Linq;

/// <summary>Vector map drawn from the playable geography. Local and full-screen maps share one projection and marker source.</summary>
public partial class WorldMap : Control
{
    public NeoKyotoWorld World { get; set; }=null!;
    public bool Mini { get; set; }
    public int Layer { get; set; }
    public double Zoom { get; private set; }=1;
    public WorldPoint Center { get; private set; }=new(64000,64000);
    private bool _drag;
    /// <summary>Metres per map pixel; underground layers initially frame their finite two-kilometre network.</summary>
    private double MapScale => Mini?.25:Math.Min(Size.X,Size.Y)/(Layer==0?128000:2400)*Zoom;
    /// <summary>Centers on a marker without changing player state.</summary>
    public void FocusPoint(WorldPoint p)
    {
        bool changed=Layer!=p.Layer; Layer=p.Layer;
        if(changed) Zoom=1;
        // At regional zoom keep all geography framed; closer views follow the selected destination.
        Center=Zoom<=1?(Layer==0?new(64000,64000):WorldCatalog.Elevators[Layer-1] with {Layer=Layer}):p;
        QueueRedraw();
    }
    /// <summary>Clamps zoom to keep marker rendering bounded and map controls predictable.</summary>
    public void ChangeZoom(double factor) { Zoom=Math.Clamp(Zoom*factor,.75,128); QueueRedraw(); }
    /// <summary>Switches the visible layer without moving the player or granting mission access.</summary>
    public void SelectLayer(int layer) { Layer=layer; Zoom=1; Center=layer==0?new(64000,64000):WorldCatalog.Elevators[layer-1] with {Layer=layer}; QueueRedraw(); }
    /// <summary>Transforms logical metres to control-local map pixels.</summary>
    private Vector2 MapPoint(WorldPoint p) => Size/2+new Vector2((float)((p.X-Center.X)*MapScale),(float)((p.Y-Center.Y)*MapScale));
    /// <summary>Converts pointer/crosshair position back into logical world coordinates.</summary>
    private WorldPoint Logical(Vector2 p) => new(Center.X+(p.X-Size.X/2)/MapScale,Center.Y+(p.Y-Size.Y/2)/MapScale,Layer);
    /// <summary>Redraws the minimap as the player moves; map panning uses mapped movement actions and shoulders zoom.</summary>
    public override void _Process(double delta)
    {
        if(World==null || !IsVisibleInTree()) return;
        if(Mini) { Center=World.PositionMetres; Layer=Center.Layer; }
        else
        {
            var direction=Input.GetVector("world_left","world_right","world_up","world_down");
            Center=Center with { X=Center.X+direction.X*delta*400/MapScale,Y=Center.Y+direction.Y*delta*400/MapScale };
        }
        QueueRedraw();
    }
    /// <summary>Draws region fills, roads, rail, river, destinations, clustered stations, mission locks and the player's waypoint.</summary>
    public override void _Draw()
    {
        if(World==null) return;
        DrawRect(new Rect2(Vector2.Zero,Size),new Color("0b1523"));
        ClipContents=true;
        if(Layer==0)
        {
            foreach(var region in WorldCatalog.Regions)
            {
                var p=new WorldPoint(region.Id%4*32000,region.Id/4*32000);
                var rect=new Rect2(MapPoint(p),Vector2.One*(float)(32000*MapScale));
                var color=region.Id is 0 or 4 or 8 or 15?new Color("1c403b"):region.Id is 2 or 12?new Color("354151"):new Color("213349");
                DrawRect(rect,color); DrawRect(rect,new Color("53717b"),false,1);
                if(!Mini && rect.Size.X>100) DrawString(ThemeDB.FallbackFont,rect.Position+new Vector2(8,24),WorldText.Get(region.Name),HorizontalAlignment.Left,-1,14,new Color("c8e6eb"));
            }
            for(int i=0;i<16;i++)
            {
                double at=4000+i*8000;
                DrawLine(MapPoint(new(at,0)),MapPoint(new(at,128000)),new Color("426678"),Mini?2:1);
                DrawLine(MapPoint(new(0,at)),MapPoint(new(128000,at)),new Color("426678"),Mini?2:1);
            }
            for(int i=1;i<WorldCatalog.River.Length;i++) DrawLine(MapPoint(WorldCatalog.River[i-1]),MapPoint(WorldCatalog.River[i]),new Color("24657c"),Math.Max(2,(float)(360*MapScale)));
            // Reservoir/coast footprints correspond to WorldCatalog.Water; arterial bridge decks remain visible above them.
            var lakeCenter=MapPoint(new(48000,16000));
            if(!Mini) DrawEllipse(lakeCenter,new((float)(7000*MapScale),(float)(9000*MapScale)),new Color("24657c"));
            DrawRect(new Rect2(MapPoint(new(124000,0)),new((float)(4000*MapScale),(float)(128000*MapScale))),new Color("24657c"));
            if(Mini || Zoom>=16)
            {
                double spanX=Size.X/MapScale/2,spanY=Size.Y/MapScale/2;
                int startX=(int)Math.Floor((Center.X-spanX-32)/128),endX=(int)Math.Ceiling((Center.X+spanX-32)/128);
                int startY=(int)Math.Floor((Center.Y-spanY-32)/128),endY=(int)Math.Ceiling((Center.Y+spanY-32)/128);
                for(int x=startX;x<=endX;x++) DrawLine(MapPoint(new(x*128+32,Center.Y-spanY)),MapPoint(new(x*128+32,Center.Y+spanY)),new Color("3d5262"),1);
                for(int y=startY;y<=endY;y++) DrawLine(MapPoint(new(Center.X-spanX,y*128+32)),MapPoint(new(Center.X+spanX,y*128+32)),new Color("3d5262"),1);
            }
            // A parallel rail corridor remains identifiable without an enormous texture.
            DrawLine(MapPoint(new(4000,108000)),MapPoint(new(124000,108000)),new Color("b99060"),2);
        }
        else
        {
            var elevator=WorldCatalog.Elevators[Layer-1];
            DrawRect(new Rect2(MapPoint(elevator with {X=elevator.X-1024,Y=elevator.Y-1024}),Vector2.One*(float)(2048*MapScale)),new Color("253349"));
            for(int i=-8;i<=8;i++)
            {
                double d=i*128;
                DrawLine(MapPoint(elevator with {X=elevator.X+d,Y=elevator.Y-1024}),MapPoint(elevator with {X=elevator.X+d,Y=elevator.Y+1024}),new Color("466275"),1);
                DrawLine(MapPoint(elevator with {X=elevator.X-1024,Y=elevator.Y+d}),MapPoint(elevator with {X=elevator.X+1024,Y=elevator.Y+d}),new Color("466275"),1);
            }
            DrawCircle(MapPoint(elevator),6,new Color("8edc9b"));
        }
        if(Layer==0)
        {
            if(!Mini && Zoom<2)
            {
                foreach(var region in WorldCatalog.Regions)
                {
                    var cluster=new WorldPoint(region.Id%4*32000+16000,region.Id/4*32000+16000);
                    DrawCircle(MapPoint(cluster),9,new Color("287b87"));
                    DrawString(ThemeDB.FallbackFont,MapPoint(cluster)+new Vector2(-7,5),"16",fontSize:12);
                }
            }
            else foreach(var station in WorldCatalog.Stations) Marker(station.Position,new Color("58dfe5"),4);
            foreach(var landmark in WorldCatalog.Landmarks)
            {
                Marker(landmark.Position,new Color("e9be78"),3);
                if(!Mini && Zoom>=8) DrawString(ThemeDB.FallbackFont,MapPoint(landmark.Position)+new Vector2(8,-8),WorldText.Get(landmark.Name),fontSize:13);
            }
        }
        foreach(var entrance in WorldCatalog.Entrances.Where(e=>e.Position.Layer==Layer))
        {
            Color color=entrance.Mission<=World.Progress?new("f3b672"):new("6b7482");
            Marker(entrance.Position,color,6);
            if(!Mini && Zoom>=4) DrawString(ThemeDB.FallbackFont,MapPoint(entrance.Position)+new Vector2(8,18),entrance.Mission.ToString(),fontSize:14,modulate:color);
        }
        if(World.State.Waypoint is WorldPoint waypoint && waypoint.Layer==Layer) { var p=MapPoint(waypoint); DrawLine(p-new Vector2(7,0),p+new Vector2(7,0),Colors.Gold,2); DrawLine(p-new Vector2(0,7),p+new Vector2(0,7),Colors.Gold,2); }
        if(World.PositionMetres.Layer==Layer) Marker(World.PositionMetres,Colors.White,5);
        if(!Mini) { DrawLine(Size/2-new Vector2(10,0),Size/2+new Vector2(10,0),Colors.White); DrawLine(Size/2-new Vector2(0,10),Size/2+new Vector2(0,10),Colors.White); }
        DrawRect(new Rect2(Vector2.One,Size-Vector2.One*2),new Color("458995"),false,2);
    }
    /// <summary>Clips marker work early; offscreen markers do not enqueue draw commands.</summary>
    private void Marker(WorldPoint p,Color color,float radius) { var at=MapPoint(p); if(new Rect2(Vector2.Zero,Size).HasPoint(at)) DrawCircle(at,radius,color); }
    /// <summary>Builds a small vector ellipse for the reservoir without a map-sized texture.</summary>
    private void DrawEllipse(Vector2 center,Vector2 radius,Color color)
    {
        var points=new Vector2[48];
        for(int i=0;i<points.Length;i++) { float angle=i*Mathf.Tau/points.Length; points[i]=center+new Vector2(Mathf.Cos(angle)*radius.X,Mathf.Sin(angle)*radius.Y); }
        DrawColoredPolygon(points,color);
    }
    /// <summary>Mouse wheel zoom, drag pan and one left-click waypoint use the same inverse projection.</summary>
    public override void _GuiInput(InputEvent input)
    {
        if(Mini) return;
        if(input is InputEventMouseButton mouse)
        {
            if(mouse.ButtonIndex==MouseButton.WheelUp && mouse.Pressed) ChangeZoom(1.3);
            if(mouse.ButtonIndex==MouseButton.WheelDown && mouse.Pressed) ChangeZoom(1/1.3);
            if(mouse.ButtonIndex==MouseButton.Right) _drag=mouse.Pressed;
            if(mouse.ButtonIndex==MouseButton.Left && mouse.Pressed) World.SetWaypoint(Logical(mouse.Position));
            AcceptEvent();
        }
        if(input is InputEventMouseMotion move && _drag) { Center=Center with {X=Center.X-move.Relative.X/MapScale,Y=Center.Y-move.Relative.Y/MapScale}; AcceptEvent(); }
    }
    /// <summary>Sets the crosshair destination for keyboard/controller navigation.</summary>
    public void SetCrosshairWaypoint() => World.SetWaypoint(Logical(Size/2));
}
