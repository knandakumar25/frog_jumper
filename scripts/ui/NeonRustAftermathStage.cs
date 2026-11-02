using Godot;

/// <summary>Eleven deterministic live shots showing sabotage consequences and the distant fortress approaches.</summary>
public partial class NeonRustAftermathStage : Control
{
    /// <summary>Presentation time owned by the overlay; sampling has no gameplay side effects.</summary>
    public float Seconds { get; set; }
    /// <summary>Essential new artwork root; invalid paths safely bypass the cinematic.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/neon_rust_aftermath/";
    /// <summary>Required layers, including reused depot art, must exist for presentation.</summary>
    public bool ResourcesAvailable => _city != null && _depot != null && _lights != null && _cargo != null && _cargoLights != null && _poses != null && _fortress != null && _fortressLights != null;
    private Texture2D? _city, _depot, _lights, _cargo, _cargoLights, _poses, _fortress, _fortressLights;
    private static readonly float[] Starts = { 0,9,18,27,36,45,54,64,74,82,88,90 };
    private static readonly Color Cyan = new("77e5eb");
    private static readonly Color[] Accents = { new("f8c868"),new("70e5df"),new("bf91f2"),new("ed73ae"),new("65edab") };

    /// <summary>Load layers once, preserving original arrival assets and the five earned operative identities.</summary>
    public override void _Ready()
    {
        _city = Load("res://assets/cutscenes/deployment/live/city_base.png");
        _depot = Load("res://assets/cutscenes/neon_rust_intro/depot_structure.png");
        _lights = Load("res://assets/cutscenes/neon_rust_intro/depot_lights.png");
        _cargo = Load("res://assets/cutscenes/neon_rust_intro/cargo_structure.png");
        _cargoLights = Load("res://assets/cutscenes/neon_rust_intro/cargo_lights.png");
        _poses = Load(ArtPath + "poses.png");
        _fortress = Load(ArtPath + "fortress_structure.png");
        _fortressLights = Load(ArtPath + "fortress_lights.png");
    }

    /// <summary>Missing resources are a supported fallback, so avoid invoking the loader on absent files.</summary>
    private static Texture2D? Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Draw normalized scenery, analytic motion, expressive poses and safe-area titles at any aspect ratio.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),new Color("030812"));
        float t = Mathf.Clamp(Seconds,0,90);
        int shot = 0;
        while (shot < 10 && t >= Starts[shot+1]) shot++;
        float u = (t-Starts[shot])/(Starts[shot+1]-Starts[shot]);
        float fit = Mathf.Min(Size.X/1920,Size.Y/1080);
        Vector2 margin = (Size-new Vector2(1920,1080)*fit)/2;
        DrawSetTransform(margin,0,Vector2.One*fit);
        Layer(_city,new Color(.5f,.65f,.78f));
        // Speaker shots reframe the shared set; UI stays in viewport space below.
        float zoom = shot switch { 0=>Mathf.Lerp(1.2f,1,u), 1 or 3 or 4 or 5 or 6=>1.35f, 2=>1.15f, 8=>1.3f, _=>1 };
        Vector2 focus = shot switch { 1 or 6=>new(1155,570), 2=>new(1400,570), 3=>new(665,570), 4=>new(920,570), 5=>new(420,570), 8=>new(1450,500), _=>new(960,540) };
        Vector2 origin = margin + ((zoom>1 && shot!=0 ? new Vector2(960,470) : new Vector2(960,540))-focus*zoom)*fit;
        DrawSetTransform(origin,0,Vector2.One*fit*zoom);
        if (shot < 7)
        {
            // Pull back from the relay; ordinary district lighting never goes dark.
            Layer(_depot,Colors.White);
            Layer(_lights,new Color(.65f,.85f,.9f));
            for (int i=0;i<4;i++)
            {
                float x=300+i*470;
                float sweep=Mathf.Sin(t*(.21f+i*.08f)+i)*180;
                DrawColoredPolygon(new Vector2[]{new(x,330),new(x+sweep-90,680),new(x+sweep+90,680)},new Color(.4f,.7f,.9f,.08f));
                if (shot==0) DrawLine(new Vector2(1430,220),new Vector2(x,570),new Color(1,.15f,.12f,(1-u)*.65f),3);
            }
            DrawRect(new Rect2(0,800,1920,280),new Color("101e2b"));
            DrawLine(new Vector2(0,805),new Vector2(1920,805),new Color("466674"),6);
            // Recovered cargo travels outward only after the load instruction, independently of mission objectives.
            float freight = shot >= 4 ? (t-36)*30 : 0;
            DrawSetTransform(origin+new Vector2(freight,220)*fit*zoom,0,Vector2.One*fit*zoom);
            Layer(_cargo,Colors.White); Layer(_cargoLights,new Color(.65f,1,.8f));
            DrawSetTransform(origin,0,Vector2.One*fit*zoom);
        }
        else
        {
            Layer(_fortress,Colors.White); Layer(_fortressLights,Colors.White);
            // Independently powered guns remain active; this is a horizon reveal, not Level 4 arrival.
            for (int i=0;i<6;i++) DrawLine(new Vector2(1120+i*120,370),new Vector2(1100+i*120+Mathf.Sin(t*.3f+i)*90,520),new Color(1,.45f,.23f,.22f),2);
        }
        int speaker = shot switch {1 or 6=>3,2 or 7=>4,3=>1,4=>2,5 or 9=>0,_=>-1};
        for (int actor=4;actor>=0;actor--)
        {
            if (shot==8) continue; // The defense insert isolates the independently powered wall.
            float x=420+actor*245;
            if (shot==2 && actor==4) x=Mathf.Lerp(1750,1400,u);
            if (shot==7) x+=u*160;
            bool walking=shot==7 || shot==2 && actor==4;
            int pose=walking ? 2+(int)(t*6+actor)%4 : actor==speaker && !(shot==1 && u>.45f) ? 1 : 0;
            float height=actor==speaker && shot is >=1 and <=6 ? 345 : 290;
            float bob=Mathf.Sin(t*(walking?8:1.5f)+actor)*(walking?2:1);
            // Feet follow the sloped service road rather than intersecting its rising deck.
            float feet=shot>=7 ? 805-Mathf.Max(0,x-1000)*215/690 : 800;
            if (_poses!=null) DrawTextureRectRegion(_poses,new Rect2(x-height/2,feet-height*.875f+bob,height,height),new Rect2(pose*512,actor*512,512,512));
            if (actor==speaker) DrawLine(new Vector2(x-40,feet+18),new Vector2(x+40,feet+18),Accents[actor],3);
        }
        if (shot==4)
        {
            // Titan guides a recovered crate from the platform onto the outward service freight.
            Vector2 crate=new(Mathf.Lerp(980,1190,u),Mathf.Lerp(730,650,u));
            DrawRect(new Rect2(crate,new Vector2(105,74)),new Color("314950"));
            DrawRect(new Rect2(crate+new Vector2(8,8),new Vector2(89,6)),Accents[2]);
            DrawLine(crate+new Vector2(46,24),crate+new Vector2(60,50),Cyan,5);
        }
        if (shot is 2 or 3 or 7)
        {
            Vector2[] route={new(1050,600),new(1170,550),new(1300,570),new(1460,480)};
            DrawPolyline(route,shot==3?Accents[1]:Accents[4],3,true);
            DrawString(ThemeDB.FallbackFont,new Vector2(1060,630),shot==3?"POWER // NEIGHBORHOODS":"SERVICE ROAD // APPROACHES",fontSize:20,modulate:Cyan);
        }
        if (shot==6)
            for (int i=0;i<7;i++)
            {
                // Abstract radio responses avoid exposing names or civilian coordinates.
                float pulse=(Mathf.Sin(t*3-i)+1)/2;
                DrawArc(new Vector2(160+i*245,390+i%2*60),12+pulse*18,Mathf.Pi,Mathf.Tau,24,new Color(1,.4f,.75f,pulse*.8f),2);
            }
        DrawSetTransform(margin,0,Vector2.One*fit);
        DrawRect(new Rect2(0,0,1920,88),new Color(0,0,0,.88f));
        DrawString(ThemeDB.FallbackFont,new Vector2(70,57),"THE CITY FINDS ITS VOICE // EXTRACTION",fontSize:24,modulate:Cyan);
        if (shot==10)
        {
            DrawRect(new Rect2(0,88,1920,992),new Color(0,0,0,Mathf.Min(1,u*2)));
            DrawString(ThemeDB.FallbackFont,new Vector2(390,475),Tr("CITY_VOICE_NEXT") is string next && next!="CITY_VOICE_NEXT"?next:"NEXT: SHATTERED AEGIS",fontSize:48,modulate:Cyan);
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
    }

    /// <summary>Composite a normalized transparent scenery layer without changing gameplay cameras.</summary>
    private void Layer(Texture2D? texture,Color tint)
    {
        if (texture!=null) DrawTextureRect(texture,new Rect2(0,0,1920,1080),false,tint);
    }
}
