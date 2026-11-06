using Godot;

/// <summary>Thirteen deterministic live shots of the fortress and separated allies; no gameplay actors, timers or hazards are spawned.</summary>
public partial class ShatteredAegisStage : Control
{
    /// <summary>Presentation time sampled by the overlay's compensated audio clock.</summary>
    public float Seconds { get; set; }
    /// <summary>Required artwork directory; missing layers invoke the overlay's safe gameplay fallback.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/shattered_aegis_intro/";
    /// <summary>All essential scenery and original character silhouettes must load.</summary>
    public bool ResourcesAvailable => _city != null && _homes != null && _homeLights != null && _squad != null && _surya != null && _voss != null &&
        System.Array.TrueForAll(_structure, x => x != null) && System.Array.TrueForAll(_lights, x => x != null);
    private Texture2D? _city, _homes, _homeLights, _squad, _surya, _voss;
    private readonly Texture2D?[] _structure = new Texture2D?[3], _lights = new Texture2D?[3];
    private static readonly float[] Starts = { 0,10,20,29,39,49,59,69,79,89,99,109,117,120 };
    private static readonly Color Gold = new("ffe29a"), Cyan = new("77e5eb");

    /// <summary>Load raster layers once, reusing the five-operative atlas and the actual boss's armored sprite.</summary>
    public override void _Ready()
    {
        _city = Load("res://assets/cutscenes/deployment/live/city_base.png");
        _homes = Load("res://assets/cutscenes/deployment/live/city_structure.png");
        _homeLights = Load("res://assets/cutscenes/deployment/live/city_lights.png");
        _squad = Load("res://assets/cutscenes/neon_rust_aftermath/poses.png");
        _voss = Load("res://assets/characters/rendered/heavy_atlas.png");
        _surya = Load(ArtPath + "surya.png");
        string[] names = { "approach", "shield_road", "command" };
        for (int i = 0; i < names.Length; i++)
        {
            _structure[i] = Load(ArtPath + names[i] + "_structure.png");
            _lights[i] = Load(ArtPath + names[i] + "_lights.png");
        }
    }

    /// <summary>Absent resources are an expected fallback, so avoid emitting loader errors.</summary>
    private static Texture2D? Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Sample pans, breathing, searchlights, shield/fire and tactical inserts analytically inside a letterboxed 1920x1080 frame.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("030812"));
        float t = Mathf.Clamp(Seconds, 0, 120);
        int shot = 0;
        while (shot < 12 && t >= Starts[shot + 1]) shot++;
        float u = (t - Starts[shot]) / (Starts[shot + 1] - Starts[shot]);
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920,1080) * fit) / 2;
        DrawSetTransform(margin, 0, Vector2.One * fit);
        Layer(_city, new Color(.42f,.56f,.69f));
        float zoom = shot switch { 0 => Mathf.Lerp(1.08f,1,u), 1 => 1.3f, 2 => 1.12f, 3 => 1.15f, 4 => 1.2f, 5 or 8 => 1.32f, 6 => 1.2f, 7 => 1.1f, _ => 1 };
        Vector2 focus = shot switch { 1 => new(1030,550), 2 => new(900,Mathf.Lerp(620,380,u)), 3 => new(650,460), 4 => new(1090,550), 5 or 8 => new(1440,530), 6 => new(540,550), 7 => new(960,500), _ => new(960,540) };
        Vector2 origin = margin + (new Vector2(960,540) - focus * zoom) * fit;
        DrawSetTransform(origin, 0, Vector2.One * fit * zoom);
        int scene = shot == 7 ? 2 : shot is 5 or 8 or 9 ? 1 : 0;
        if (shot == 3)
        {
            // A view past the lower parapet exposes anonymous residential lights,
            // without tactical markers that could identify civilian positions.
            if (_city != null) DrawTextureRect(_city,new Rect2(0,-160,1920,1080),false,new Color(.65f,.75f,.8f));
            if (_homes != null) DrawTextureRect(_homes,new Rect2(0,-160,1920,1080),false,new Color(.55f,.65f,.75f));
            if (_homeLights != null) DrawTextureRect(_homeLights,new Rect2(0,-160,1920,1080),false,new Color(.8f,.7f,.6f));
            if (_structure[scene] != null) DrawTextureRect(_structure[scene]!,new Rect2(0,400,1920,1080),false);
            if (_lights[scene] != null) DrawTextureRect(_lights[scene]!,new Rect2(0,400,1920,1080),false);
            DrawRect(new Rect2(0,800,1920,280),new Color("17232e"));
            DrawLine(new Vector2(0,800),new Vector2(1920,800),Cyan,3);
        }
        else
        {
            Layer(_structure[scene], Colors.White);
            Layer(_lights[scene], new Color(1,1,1,.88f + .1f * Mathf.Sin(t * 1.2f)));
        }
        if (shot == 7)
        {
            // This is a sprite presentation of the established armored identity, never a live boss node.
            if (_voss != null) DrawTextureRectRegion(_voss, new Rect2(640,245,640,640), new Rect2(0,0,256,256));
            for (int i = 0; i < 5; i++) DrawArc(new Vector2(960,450),190 + i * 20,0,Mathf.Tau,64,new Color(1,.35f,.2f,.06f),2);
        }
        else
        {
            // Guns aim toward anonymous residential lights beyond the wall. Search cones have no collision or damage.
            for (int i = 0; i < 6; i++)
            {
                float x = 180 + i * 310;
                float sweep = Mathf.Sin(t * .23f + i * 1.8f) * 200;
                DrawColoredPolygon(new Vector2[]{new(x,shot == 3 ? 630 : 230),new(x - 290 + sweep,shot == 3 ? 320 : 680),new(x - 100 + sweep,shot == 3 ? 320 : 680)},new Color(.7f,.8f,1,.07f));
            }
            if (shot >= 4)
            {
                float pulse = (shot == 4 ? .15f : .6f) + .1f * Mathf.Sin(t * 2);
                if (shot >= 5 && _surya != null) DrawTextureRectRegion(_surya,new Rect2(1400,505,260,260),new Rect2((shot == 5 || shot == 8 ? 1 : 0)*512,0,512,512));
                for (int ring = 0; ring < 4; ring++) DrawArc(new Vector2(1530,735),132 + ring*12,Mathf.Pi,Mathf.Tau,64,new Color(1,.73f,.22f,pulse/(ring+2)),3);
                // Intermittent defensive rounds visibly stop on the cinematic shield.
                if (shot is 5 or 8)
                {
                    for (int vent = 0; vent < 2; vent++)
                        for (int wisp = 0; wisp < 5; wisp++)
                        {
                            float rise = Mathf.PosMod(t * 70 + wisp * 32,160);
                            float x = 1100 + vent * 200 + wisp * 12;
                            DrawLine(new Vector2(x,760-rise),new Vector2(x+Mathf.Sin(t+wisp)*12,730-rise),new Color(1,.48f,.16f,(1-rise/160)*.25f),6);
                        }
                    float flight = Mathf.PosMod(t * 1.2f,1);
                    Vector2 tip = new Vector2(1780,290).Lerp(new Vector2(1620,600),flight);
                    DrawLine(tip,tip+new Vector2(16,-25),new Color(1,.45f,.18f),4);
                    DrawCircle(new Vector2(1620,600),8 + flight*12,new Color(1,.8f,.4f,(1-flight)*.3f));
                }
            }
            if (shot is not 5 and not 8 and not 9)
            {
                for (int actor = 4; actor >= 0; actor--)
                {
                    float x = 280 + actor * 205;
                    if (shot == 1 && actor == 4) x += u * 120;
                    if (shot == 11) x += u * 70;
                    int pose = shot is 0 or 11 ? 2 + (int)(t * 6 + actor) % 4 : 0;
                    if (shot == 4 && actor == 3 || shot == 6 && actor == 0 || shot == 10 && actor == 4) pose = 1;
                    float bob = Mathf.Sin(t * (pose >= 2 ? 7 : 1.4f) + actor) * (pose >= 2 ? 2 : .7f);
                    if (_squad != null) DrawTextureRectRegion(_squad,new Rect2(x-120,550+bob,240,240),new Rect2(pose*512,actor*512,512,512));
                }
            }
            if (shot is 1 or 10)
                DrawPolyline(new Vector2[]{new(220,796),new(880,796),new(1250,785),new(1400,760)},new Color(.3f,1,.7f,.7f),3);
            if (shot == 4)
                for (int ring = 0; ring < 4; ring++) DrawArc(new Vector2(1090,610),30+Mathf.PosMod(t*25+ring*30,130),-1.2f,1.2f,32,new Color(.95f,.5f,.8f,.35f),2);
            if (shot == 9) TacticalInserts(u);
        }
        DrawSetTransform(margin,0,Vector2.One*fit);
        DrawRect(new Rect2(0,0,1920,88),new Color(0,0,0,.88f));
        DrawString(ThemeDB.FallbackFont,new Vector2(55,57),WildfireText.Get("UI_SHATTEREDAEGISSTAGE__DRAW_2EA9D1F4", "THE WALL THAT FACES INWARD // BORDER DEFENSE FORTRESS"),fontSize:24,modulate:Gold);
        if (shot == 12)
        {
            DrawRect(new Rect2(0,88,1920,992),new Color(.008f,.015f,.03f,Mathf.Clamp(u*2,0,1)));
            DrawString(ThemeDB.FallbackFont,new Vector2(430,450),WildfireText.Get("UI_SHATTEREDAEGISSTAGE__DRAW_1BCF424C", "LEVEL 4: SHATTERED AEGIS"),fontSize:48,modulate:Gold);
            DrawString(ThemeDB.FallbackFont,new Vector2(650,515),WildfireText.Get("UI_SHATTEREDAEGISSTAGE__DRAW_B878D0A4", "BORDER FORTRESS // APPROACH"),fontSize:26,modulate:Colors.White);
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
    }

    /// <summary>Cycle three presentation-only tactical views: vents, inner curtain and remote arena.</summary>
    private void TacticalInserts(float u)
    {
        int view = Mathf.Min(2,(int)(u*3));
        DrawRect(new Rect2(180,230,1560,440),new Color(.008f,.02f,.035f,.94f));
        string[] labels = { "01 // SUPERHEATED VENTS", "02 // INNER LASER CURTAIN", "03 // COMMAND ARENA" };
        DrawString(ThemeDB.FallbackFont,new Vector2(240,300),labels[view],fontSize:30,modulate:Cyan);
        if (view == 0)
            for (int i = 0; i < 9; i++) DrawLine(new Vector2(510+i*90,600),new Vector2(510+i*90,420+Mathf.Sin(Seconds*2+i)*30),new Color(1,.4f,.12f,.7f),18);
        else if (view == 1)
            for (int i = 0; i < 8; i++) DrawLine(new Vector2(600+i*100,370),new Vector2(600+i*100,610),new Color(1,.18f,.25f,.7f),5);
        else
        {
            DrawRect(new Rect2(650,400,620,200),new Color(.18f,.23f,.29f));
            DrawLine(new Vector2(650,600),new Vector2(1270,600),Gold,4);
            DrawString(ThemeDB.FallbackFont,new Vector2(790,510),WildfireText.Get("UI_SHATTEREDAEGISSTAGE_TACTICALINSERTS_00E30ECC", "BEYOND THE CURTAIN"),fontSize:22,modulate:Gold);
        }
    }

    /// <summary>Composite one transparent native-resolution layer without changing the current camera transform.</summary>
    private void Layer(Texture2D? texture, Color tint)
    {
        if (texture != null) DrawTextureRect(texture,new Rect2(0,0,1920,1080),false,tint);
    }
}
