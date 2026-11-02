using Godot;

/// <summary>Analytically sampled surveillance district shots; artwork and existing operatives have no gameplay state.</summary>
public partial class ActIIStage : Control
{
    /// <summary>Timeline position supplied exclusively by the preview controller.</summary>
    public float Seconds { get; set; }
    /// <summary>Essential artwork root, replaceable by tests to exercise safe completion.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/act_ii_opening/";
    /// <summary>All scenery and established operative silhouettes are required; speech is optional.</summary>
    public bool ResourcesAvailable => _squad != null && _surya != null && _civilians != null && _civilianLights != null &&
        System.Array.TrueForAll(_layers, x => x != null);
    private readonly Texture2D?[] _layers = new Texture2D?[12];
    private Texture2D? _squad, _surya, _civilians, _civilianLights;
    private static readonly float[] Starts = { 0,8,20,32,44,56,68,80,92,104,114,120 };

    /// <summary>Load layer families once; missing files are handled by the controller without loader errors.</summary>
    public override void _Ready()
    {
        string[] scenes = { "queue", "scanner", "service", "registry" };
        string[] layers = { "base", "structure", "lights" };
        for (int scene = 0; scene < 4; scene++)
            for (int layer = 0; layer < 3; layer++)
                _layers[scene * 3 + layer] = Load(ArtPath + scenes[scene] + "_" + layers[layer] + ".png");
        _civilians = Load(ArtPath + "civilians_structure.png");
        _civilianLights = Load(ArtPath + "civilians_lights.png");
        _squad = Load("res://assets/cutscenes/neon_rust_aftermath/poses.png");
        _surya = Load("res://assets/cutscenes/shattered_aegis_intro/surya.png");
    }

    /// <summary>Probe before loading, allowing explicit missing-art fallback without engine diagnostics.</summary>
    private static Texture2D? Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Composite camera inserts, warm civilians, scanner pulses and quiet operative gestures inside a fitted frame.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("03070c"));
        float t = Mathf.Clamp(Seconds, 0, 120);
        int shot = 0;
        while (shot < 10 && t >= Starts[shot + 1]) shot++;
        float u = (t - Starts[shot]) / (Starts[shot + 1] - Starts[shot]);
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920,1080) * fit) / 2;
        int scene = shot is 6 or 9 ? 2 : shot is 7 or 8 or 10 ? 3 : shot is 2 or 3 or 5 ? 1 : 0;
        float zoom = shot is 2 or 3 or 5 ? 1.2f : shot == 4 ? 1.1f : 1;
        Vector2 focus = shot is 2 or 3 or 5 ? new(1000,550) : new(960,540);
        DrawSetTransform(margin + (new Vector2(960,540) - focus * zoom) * fit, 0, Vector2.One * fit * zoom);
        for (int layer = 0; layer < 3; layer++) Layer(_layers[scene*3+layer], Colors.White);
        if (scene < 2)
        {
            // The rejected adult and child remain visible beside Oceanus, away from the still queue.
            Vector2 escort = shot == 3 ? new Vector2(-u*145,0) : Vector2.Zero;
            Layer(_civilians, Colors.White, escort);
            Layer(_civilianLights, Colors.White, escort);
            for (int i = 0; i < 3; i++)
            {
                float x = 792+i*360;
                float y = 345+Mathf.PosMod(t*75,290);
                DrawRect(new Rect2(x,y,214,8),new Color(.9f,.97f,1,.22f));
                DrawColoredPolygon(new Vector2[]{new(x,315),new(x+214,315),new(x+240,790),new(x-26,790)},new Color(.85f,.93f,1,.035f));
            }
        }
        if (shot > 0 && shot < 10)
        {
            // Atlas rows retain the established five-member order; Surya is a separate existing asset.
            for (int actor = 5; actor >= 0; actor--)
            {
                float x = 280+actor*185;
                float y = 545;
                bool speaker = shot == 1 && actor == 5 || shot == 3 && actor == 1 || shot == 4 && actor == 2 ||
                    shot == 5 && actor == 3 || shot == 6 && actor == 4 || shot == 8 && actor == 0;
                if (shot == 3 && actor == 1) { x = 830-u*145; y = 535; }
                if (shot == 9) x += actor == 5 ? 0 : u*250;
                int pose = shot == 9 && actor != 5 ? 2+(int)(t*6+actor)%4 : speaker ? 1 : 0;
                if (actor == 5)
                {
                    if (_surya != null) DrawTextureRectRegion(_surya,new Rect2(x-120,y,240,240),new Rect2((speaker?1:0)*512,0,512,512));
                }
                else if (_squad != null)
                    DrawTextureRectRegion(_squad,new Rect2(x-120,y+Mathf.Sin(t*1.5f+actor),240,240),new Rect2(pose*512,actor*512,512,512));
            }
        }
        if (shot == 5)
            for (int ring = 0; ring < 4; ring++)
                DrawArc(new Vector2(835,625),35+ring*24, -1.4f,1.4f,40,new Color(.9f,.5f,.85f,.4f),2);
        if (shot == 6)
            DrawPolyline(new Vector2[]{new(350,800),new(1100,800),new(1500,710),new(1500,540)},new Color(.5f,.85f,.67f,.65f),3);
        DrawSetTransform(margin,0,Vector2.One*fit);
        if (shot == 0)
        {
            // Open border transitions into a sterile white scan before the title resolves.
            if (t < 3)
            {
                float retreat = t*180;
                DrawRect(new Rect2(-retreat,0,510,1080),new Color("18242d"));
                DrawRect(new Rect2(1410+retreat,0,510,1080),new Color("18242d"));
                DrawLine(new Vector2(510-retreat,0),new Vector2(510-retreat,1080),new Color("e4b777"),8);
                DrawLine(new Vector2(1410+retreat,0),new Vector2(1410+retreat,1080),new Color("e4b777"),8);
            }
            DrawRect(new Rect2(0,0,1920,1080),new Color(1,1,1,t < 3 ? Mathf.Sin(t/3*Mathf.Pi) : 0));
            DrawRect(new Rect2(0,0,1920,1080),new Color(.01f,.02f,.035f,.68f));
            if (t >= 1.5f) Title("ACT_II_TITLE", "ACT II", 405, 46, Colors.White);
            if (t >= 2) Title("ACT_II_SUBTITLE", "THE UNREGISTERED", 500, 64, new Color("e4b777"));
        }
        if (shot == 10)
        {
            DrawRect(new Rect2(0,0,1920,1080),new Color(.01f,.02f,.035f,.88f));
            Title("ACT_II_MISSION_TITLE", "MISSION 5: NAMES WITHOUT NUMBERS", 440, 42, new Color("e4b777"));
            Title("ACT_II_OBJECTIVE", "Infiltrate the district registry. Recover the original identity records.", 515, 26, Colors.White);
            DrawRect(new Rect2(0,0,1920,1080),new Color(0,0,0,Mathf.Clamp((t-118)/2,0,1)));
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
    }

    /// <summary>Render localized title text centered independently of artwork and viewport aspect.</summary>
    private void Title(string key, string fallback, float y, int size, Color color)
    {
        string text = Tr(key);
        if (text == key || string.IsNullOrWhiteSpace(text)) text = fallback;
        // Long translations stay inside the native frame rather than clipping at either side.
        float width = ThemeDB.FallbackFont.GetStringSize(text,fontSize:size).X;
        if (width > 1740) size = (int)(size*1740/width);
        width = ThemeDB.FallbackFont.GetStringSize(text,fontSize:size).X;
        DrawString(ThemeDB.FallbackFont,new Vector2((1920-width)/2,y),text,fontSize:size,modulate:color);
    }

    /// <summary>Composite a transparent layer with an optional deterministic civilian escort offset.</summary>
    private void Layer(Texture2D? texture, Color tint, Vector2 offset = default)
    {
        if (texture != null) DrawTextureRect(texture,new Rect2(offset,new Vector2(1920,1080)),false,tint);
    }
}
