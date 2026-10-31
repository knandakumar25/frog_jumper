using Godot;

/// <summary>Deterministic live evacuation tableau: nine shots, layered Blender art and analytical motion.</summary>
public partial class WildfireAftermathStage : Control
{
    /// <summary>Reviewable clock; no accumulated particles or asynchronous animation state.</summary>
    public float Seconds { get; set; }
    /// <summary>Resource root can be replaced to verify the safe missing-art handoff.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/wildfire_aftermath/";
    /// <summary>All story-critical new layers must load before the cinematic can play.</summary>
    public bool ResourcesAvailable => _reunion != null && _poses != null && _terminal != null && _city != null && _checkpoint != null && _battery != null;
    private Texture2D? _city, _checkpoint, _battery, _batteryLights, _reunion, _poses, _terminal;
    private static readonly float[] Starts = { 0, 8, 17, 26, 34, 43, 54, 64, 72, 75 };
    private static readonly Color Cyan = new("77e5eb");

    /// <summary>Loads shared checkpoint geography and the original aftermath family once.</summary>
    public override void _Ready()
    {
        _city = LoadTexture("res://assets/cutscenes/deployment/live/city_base.png");
        _checkpoint = LoadTexture("res://assets/cutscenes/wildfire/checkpoint_structure.png");
        _battery = LoadTexture("res://assets/cutscenes/wildfire/siege_structure.png");
        _batteryLights = LoadTexture("res://assets/cutscenes/wildfire/siege_lights.png");
        _reunion = LoadTexture(ArtPath + "reunion_structure.png");
        _terminal = LoadTexture(ArtPath + "terminal_structure.png");
        _poses = LoadTexture(ArtPath + "poses.png");
    }

    /// <summary>Checks existence before loading to make missing resources an intentional fallback.</summary>
    private static Texture2D? LoadTexture(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Samples pullback, reunion, support, reflection, safe corridor, manifest, release, departure and title.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("030812"));
        float t = Mathf.Clamp(Seconds, 0, 75);
        int shot = 0;
        while (shot < 8 && t >= Starts[shot + 1]) shot++;
        float u = (t - Starts[shot]) / (Starts[shot + 1] - Starts[shot]);
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) / 2;
        float zoom = shot == 0 ? 1.18f - u * .18f : shot == 1 ? 1.35f : shot == 2 ? 1.55f : shot == 3 ? 1.4f : 1;
        Vector2 focus = shot == 1 ? new Vector2(650,650) : shot == 2 ? new Vector2(1180,650) : shot == 3 ? new Vector2(720,650) : new Vector2(960,540);
        Vector2 pan = new(shot == 1 ? -u * 55 : shot == 7 ? -u * 90 : 0, 0);
        Vector2 origin = margin + (new Vector2(960,540) - focus*zoom + pan) * fit;
        DrawSetTransform(margin + pan*.3f*fit, 0, Vector2.One * fit);
        Layer(_city, new Color(.65f,.75f,.85f));
        DrawSetTransform(origin, 0, Vector2.One * fit * zoom);
        Layer(_battery, Colors.White);
        Layer(_batteryLights, new Color(1,1,1,1-Mathf.Clamp(t/2,0,1)));
        Layer(_checkpoint, Colors.White);
        // The membrane is fallen: fractured cyan strips lie flat across a continuous open road.
        for (int i = 0; i < 9; i++)
            DrawLine(new Vector2(1100+i*45,817),new Vector2(1125+i*45,823),new Color(.2f,.8f,.9f,.3f),3);
        for (int i = 0; i < 12; i++)
        {
            Vector2 p = new(Mathf.PosMod(i*179+t*12,2100)-90, 410-i%4*65-Mathf.Sin(t*.2f+i)*20);
            DrawCircle(p, 35+i%3*14, new Color(.23f,.3f,.36f,.075f));
        }
        // One gentle isolated ember bed preserves amber continuity without threatening the corridor.
        DrawCircle(new Vector2(90,800),22,new Color(1,.4f,.08f,.22f+.03f*Mathf.Sin(t*3)));
        DrawSetTransform(origin + new Vector2(t < 64 ? t * 1.4f : 90,0)*fit*zoom,0,Vector2.One*fit*zoom);
        Layer(_reunion, Colors.White);
        DrawSetTransform(origin,0,Vector2.One*fit*zoom);
        // Atlas columns: calm, task gesture, four walking frames. Rows: Kaito, Oceanus, Titan.
        for (int actor = 0; actor < 3; actor++)
        {
            float x = actor == 0 ? 630 : actor == 1 ? 890 : 1210;
            // The atlas feet sit five pixels above the destination anchor; align them to the 832px road.
            float y = 837;
            int pose = shot == 7 ? 2 + (int)(t*7+actor)%4 : actor == 2 && (shot >= 2 && shot <= 5 || shot == 6 && u < .45f) ? 1 : actor == 1 && shot is 1 or 4 ? 1 : 0;
            if (shot == 7) { x += u*780; y -= Mathf.Max(0,x-1550)*.38f; }
            float bob = shot == 7 ? Mathf.Sin(t*8+actor)*3 : Mathf.Sin(t*1.7f+actor)*1.2f;
            bool lookBack = actor == 0 && shot == 3 && u > .35f;
            if (_poses != null) DrawTextureRectRegion(_poses,new Rect2(x-180,y-315+bob,lookBack ? -360 : 360,360),new Rect2(pose*512,actor*512,512,512));
        }
        if (shot <= 1)
        {
            float alpha = shot == 0 ? .25f : (1-u)*.25f;
            DrawArc(new Vector2(890,660),155,Mathf.Pi,Mathf.Tau,48,new Color(.3f,.9f,1,alpha),8);
            for (int i=0;i<14;i++) DrawCircle(new Vector2(770+i*17,665+u*90+Mathf.Sin(i+t)*10),3,new Color(.3f,.9f,1,alpha));
        }
        if (shot >= 2)
        {
            float settle = shot >= 7 ? 1 : shot == 6 ? Mathf.Clamp(u*3,0,1) : 0;
            DrawColoredPolygon(new Vector2[] { new(1100,532+settle*6),new(1410,557),new(1390,597),new(1100,572+settle*6) }, new Color("354653"));
            DrawLine(new Vector2(1110,573),new Vector2(1410,597),new Color("a4a4ad"),3);
            // Child waits beside Titan, then returns to the parent; never crosses beneath unsupported rubble.
            if (shot == 2)
            {
                float childX = Mathf.Lerp(1280,1480,Mathf.Clamp((u-.35f)*2,0,1));
                DrawCircle(new Vector2(childX,747),10,new Color("c3a38b"));
                DrawLine(new Vector2(childX,758),new Vector2(childX,790),new Color("8aa2b9"),18);
                DrawLine(new Vector2(childX-5,790),new Vector2(childX-8,832),new Color("526c83"),7);
                DrawLine(new Vector2(childX+5,790),new Vector2(childX+8,832),new Color("526c83"),7);
            }
        }
        if (shot == 4)
        {
            DrawLine(new Vector2(1020,775),new Vector2(1500,775),Cyan,4);
            DrawString(ThemeDB.FallbackFont,new Vector2(1050,735),"CORRIDOR OPEN // FREE PASSAGE",fontSize:23,modulate:Cyan);
        }
        if (shot == 5) DrawManifest(u);
        if (shot >= 6)
            for (int i=0;i<7;i++) DrawRect(new Rect2(Mathf.PosMod(i*210+t*40,1920),375,60,4),new Color("e9b875"));
        DrawSetTransform(margin,0,Vector2.One*fit);
        DrawRect(new Rect2(0,0,1920,90),new Color(0,0,0,.86f));
        DrawString(ThemeDB.FallbackFont,new Vector2(85,58),"THROUGH THE BREACH // EVACUATION SECURED",fontSize:24,modulate:Cyan);
        if (shot == 8)
        {
            DrawRect(new Rect2(0,90,1920,730),new Color(.01f,.025f,.045f,.97f));
            DrawString(ThemeDB.FallbackFont,new Vector2(750,365),"NEXT MISSION",fontSize:32,modulate:Cyan);
            DrawString(ThemeDB.FallbackFont,new Vector2(400,445),"NEON RUST AND DATA DUST",fontSize:48,modulate:new Color("edb66a"));
        }
        float fade = t < 1 ? 1-t : Mathf.Clamp(t-74,0,1);
        DrawRect(new Rect2(0,0,1920,1080),new Color(0,0,0,fade));
        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>Reveals the recovered supply route and tower network without introducing Level 3 operatives.</summary>
    private void DrawManifest(float progress)
    {
        Layer(_terminal,Colors.White);
        DrawRect(new Rect2(300,180,1320,470),new Color(.005f,.025f,.05f,.95f));
        DrawRect(new Rect2(300,180,1320,470),Cyan,false,2);
        DrawString(ThemeDB.FallbackFont,new Vector2(350,240),"RECOVERED MANIFEST // REGIONAL TRANSIT POWER",fontSize:27,modulate:Cyan);
        Vector2[] route = { new(430,480),new(750,480),new(980,360),new(1430,360) };
        for (int i=0;i<3;i++) DrawLine(route[i],route[i].Lerp(route[i+1],Mathf.Clamp(progress*4-i,0,1)),Cyan,5);
        DrawString(ThemeDB.FallbackFont,new Vector2(350,540),"CHECKPOINT",fontSize:24,modulate:Colors.White);
        DrawString(ThemeDB.FallbackFont,new Vector2(1100,550),"DEPOT / PATROL UPLINK",fontSize:24,modulate:Colors.White);
        for (int i=0;i<3;i++)
        {
            Vector2 tower = new(1110+i*150,350);
            DrawLine(tower,tower-new Vector2(0,65),Cyan,6);
            DrawArc(tower-new Vector2(0,65),22,Mathf.Pi,Mathf.Tau,20,Cyan,2);
        }
    }

    /// <summary>Composites transparent scenery in its shared 1920 by 1080 coordinate space.</summary>
    private void Layer(Texture2D? texture, Color tint)
    {
        if (texture != null) DrawTextureRect(texture,new Rect2(0,0,1920,1080),false,tint);
    }
}
