using Godot;

/// <summary>Six analytically sampled arrival shots using separate Docker-authored planar layers.</summary>
public partial class WildfireStage : Control
{
    /// <summary>Timeline position in seconds; seeking never accumulates particles or timers.</summary>
    public float Seconds { get; set; }
    /// <summary>Configurable resource root for fallback verification.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/wildfire/";
    /// <summary>Essential composition layers; optional light layers may be absent.</summary>
    public bool ResourcesAvailable => _checkpoint != null && _poses != null && _civilians != null && _siege != null && _blade != null;
    private Texture2D? _city, _cityLights, _checkpoint, _lights, _poses, _civilians, _siege, _blade, _civilianLights, _siegeLights;
    private static readonly Color Cyan = new("77e5eb");

    /// <summary>Loads existing skyline and the arrival family once, checking paths before resource loading.</summary>
    public override void _Ready()
    {
        _city = LoadTexture("res://assets/cutscenes/deployment/live/city_base.png");
        _cityLights = LoadTexture("res://assets/cutscenes/deployment/live/city_lights.png");
        _checkpoint = LoadTexture(ArtPath + "checkpoint_structure.png");
        _lights = LoadTexture(ArtPath + "checkpoint_lights.png");
        _poses = LoadTexture(ArtPath + "poses.png");
        _civilians = LoadTexture(ArtPath + "civilians_structure.png");
        _blade = LoadTexture(ArtPath + "blade.png");
        _civilianLights = LoadTexture(ArtPath + "civilians_lights.png");
        _siegeLights = LoadTexture(ArtPath + "siege_lights.png");
        _siege = LoadTexture(ArtPath + "siege_structure.png");
    }

    /// <summary>A missing resource participates in safe fallback without logging a loader error.</summary>
    private static Texture2D? LoadTexture(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Draws the district, arrival, mortar deployment, comms map, descent and evacuation-road shots.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("030812"));
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) / 2;
        float t = Seconds;
        int shot = Mathf.Min((int)(t / 10), 5);
        float u = (t - shot * 10) / 10;
        float zoom = shot == 1 ? 1.2f + .025f * u : shot == 2 ? 1.05f : 1;
        Vector2 focus = shot == 1 ? new Vector2(820, 495) : new Vector2(960 + u * 12, 540);
        DrawSetTransform(margin + new Vector2(960, 540) * fit - focus * fit * zoom, 0, Vector2.One * fit * zoom);
        DrawRect(new Rect2(-400,-300,2720,1680), new Color("081a2a"));
        Layer(_city, new Color(.6f,.7f,.8f));
        Layer(_cityLights, new Color(.7f,.8f,.8f));
        if (shot >= 2) { Layer(_siege, Colors.White); Layer(_siegeLights, Colors.White); }
        // Families remain protected behind barriers; movement is evacuation, never a casualty effect.
        DrawSetTransform(margin + new Vector2(960,540) * fit - focus * fit * zoom + new Vector2(shot >= 4 ? u * 70 : 0,0) * fit, 0, Vector2.One * fit * zoom);
        Layer(_civilians, Colors.White);
        Layer(_civilianLights, Colors.White);
        DrawSetTransform(margin + new Vector2(960,540) * fit - focus * fit * zoom, 0, Vector2.One * fit * zoom);
        Layer(_checkpoint, Colors.White);
        Layer(_lights, new Color(1,.88f + Mathf.Sin(t * 2) * .06f,.75f));
        DrawRect(new Rect2(1460,350,30,470), new Color(.2f,.8f,.9f,.2f + .04f * Mathf.Sin(t * 2)));
        for (int i = 0; i < 16; i++) DrawLine(new Vector2(1460,365+i*28),new Vector2(1490,365+i*28),new Color(.3f,.9f,1,.35f),2);
        // Quiet amber firelight and distant mortar flashes stay away from civilian silhouettes.
        for (int i = 0; i < 6; i++)
        {
            Vector2 p = new(70+i*335, 650-i%2*150);
            DrawCircle(p, 45 + Mathf.Sin(t*4+i)*8, new Color(1,.3f,.06f,.045f));
            DrawColoredPolygon(new Vector2[] { p + new Vector2(-12,8), p + new Vector2(-6,-24),
                p + new Vector2(Mathf.Sin(t*3+i)*8,-52), p + new Vector2(12,8) },new Color(1,.43f,.1f,.45f));
        }
        if (shot == 2)
        {
            float flash = Mathf.Pow(Mathf.Max(0,Mathf.Sin((t-20)*2.4f)),18);
            DrawCircle(new Vector2(1230,440),110,new Color(1,.6f,.25f,flash*.5f));
            DrawLine(new Vector2(1260,490),new Vector2(1180,330),new Color(.6f,.8f,1,flash),4);
        }
        if (_poses != null)
        {
            float x = t < 10 ? Mathf.Lerp(370,530,u) : t < 40 ? 530 : t < 50 ? Mathf.Lerp(530,760,u) : Mathf.Lerp(760,1090,u);
            float feet = t < 40 ? 590 : t < 50 ? Mathf.Lerp(590,820,u) : 820;
            int pose = t < 10 || t >= 50 ? 0 : t < 40 ? 1 : 2;
            float bob = t < 10 || t >= 40 ? Mathf.Sin(t*10)*3 : 0;
            DrawTextureRectRegion(_poses,new Rect2(x-205,feet-358+bob,410,410),new Rect2(pose*512,0,512,512));
            if (shot == 1 && u < .5f && _blade != null)
            {
                // Only the secured hilt peeks from beneath the coat, rather than a combat draw.
                DrawTextureRect(_blade,new Rect2(x-15,feet-155,46,46),false,new Color(1,1,1,Mathf.Sin(u*2*Mathf.Pi)));
                DrawLine(new Vector2(x+8,feet-140),new Vector2(x+35,feet-108),new Color(.5f,1,1,Mathf.Sin(u*2*Mathf.Pi)),4);
            }
        }
        if (shot == 3) DrawSpireProjection(u);
        for (int i = 0; i < 60; i++)
        {
            Vector2 p = new(Mathf.PosMod(i*173+t*(14+i%5),1920),1080-Mathf.PosMod(i*79+t*(22+i%4),1080));
            DrawCircle(p,1.5f,new Color(1,.55f,.18f,.55f));
        }
        DrawSetTransform(margin,0,Vector2.One*fit);
        DrawRect(new Rect2(0,0,1920,90),new Color(0,0,0,.85f));
        DrawString(ThemeDB.FallbackFont,new Vector2(85,58),WildfireText.Get("UI_WILDFIRESTAGE__DRAW_D4E2C1BE", "CONTAINMENT ZONE 04 // ARRIVAL"),fontSize:24,modulate:Cyan);
        if (shot == 5)
        {
            DrawString(ThemeDB.FallbackFont,new Vector2(540,230),WildfireText.Get("UI_WILDFIRESTAGE__DRAW_55E6069D", "THE CONCRETE WILDFIRE"),fontSize:44,modulate:new Color("edb66a"));
            DrawString(ThemeDB.FallbackFont,new Vector2(700,270),WildfireText.Get("UI_WILDFIRESTAGE__DRAW_B75DC7A7", "FOLLOW THE EVACUATION ROAD"),fontSize:22,modulate:Cyan);
        }
        float fade = t < 1 ? 1-t : Mathf.Clamp(t-59,0,1);
        DrawRect(new Rect2(0,0,1920,1080),new Color(0,0,0,fade));
        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>Comms schematic makes all three spires and their membrane connection readable without adding objectives.</summary>
    private void DrawSpireProjection(float progress)
    {
        DrawRect(new Rect2(880,210,850,430),new Color(.005f,.025f,.05f,.95f));
        DrawRect(new Rect2(880,210,850,430),Cyan,false,2);
        DrawString(ThemeDB.FallbackFont,new Vector2(920,265),WildfireText.Get("UI_WILDFIRESTAGE_DRAWSPIREPROJECTION_D87F5668", "AXIOM-7 // BIOMETRIC NETWORK"),fontSize:24,modulate:Cyan);
        for (int i=0;i<3;i++)
        {
            Vector2 p = new(1000+i*250,390);
            DrawRect(new Rect2(p-new Vector2(12,45),new Vector2(24,90)),Cyan);
            DrawLine(p+new Vector2(0,45),new Vector2(1305,540),Cyan,2);
            DrawString(ThemeDB.FallbackFont,p+new Vector2(-45,-65),WildfireText.Interpolate("UI_WILDFIRESTAGE_DRAWSPIREPROJECTION_8CFB0505", $"SPIRE {i+1}"),fontSize:20,modulate:Colors.White);
        }
        DrawRect(new Rect2(1180,530,250,18),new Color(.3f,.9f,1,.5f+progress*.3f));
        DrawString(ThemeDB.FallbackFont,new Vector2(1180,590),WildfireText.Get("UI_WILDFIRESTAGE_DRAWSPIREPROJECTION_E2DE08DC", "ENERGY MEMBRANE"),fontSize:22,modulate:Cyan);
    }

    /// <summary>Composites optional layers in the shared 1920 by 1080 native coordinate frame.</summary>
    private void Layer(Texture2D? texture, Color tint)
    {
        if (texture != null) DrawTextureRect(texture,new Rect2(0,0,1920,1080),false,tint);
    }
}
