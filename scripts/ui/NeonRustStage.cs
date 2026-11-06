using Godot;

/// <summary>Nine analytical live arrival shots built from layered Docker/Blender art; seeking never mutates gameplay.</summary>
public partial class NeonRustStage : Control
{
    /// <summary>Single presentation time shared with dialogue and compensated speech.</summary>
    public float Seconds { get; set; }
    /// <summary>Replaceable resource directory for production and missing-art verification.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/neon_rust_intro/";
    /// <summary>Every essential scenery and pose layer must exist before freezing gameplay for playback.</summary>
    public bool ResourcesAvailable => _depot != null && _underpass != null && _cargo != null && _walkway != null && _poses != null && _walking != null && _city != null;
    private Texture2D? _depot, _depotLights, _underpass, _underpassLights, _cargo, _cargoLights, _walkway, _walkwayLights, _poses, _walking, _city;
    private static readonly float[] Starts = { 0, 9, 18, 27, 36, 45, 55, 64, 72, 75 };
    private static readonly Color Cyan = new("77e5eb");

    /// <summary>Loads scenery and the existing operative walk family once; only new action poses are authored.</summary>
    public override void _Ready()
    {
        _city = LoadTexture("res://assets/cutscenes/deployment/live/city_base.png");
        _walking = LoadTexture("res://assets/cutscenes/wildfire_aftermath/poses.png");
        _depot = LoadTexture(ArtPath + "depot_structure.png");
        _depotLights = LoadTexture(ArtPath + "depot_lights.png");
        _underpass = LoadTexture(ArtPath + "underpass_structure.png");
        _underpassLights = LoadTexture(ArtPath + "underpass_lights.png");
        _cargo = LoadTexture(ArtPath + "cargo_structure.png");
        _cargoLights = LoadTexture(ArtPath + "cargo_lights.png");
        _walkway = LoadTexture(ArtPath + "walkway_structure.png");
        _walkwayLights = LoadTexture(ArtPath + "walkway_lights.png");
        _poses = LoadTexture(ArtPath + "poses.png");
    }

    /// <summary>Missing resources are a supported gameplay fallback and must not emit loader errors.</summary>
    private static Texture2D? LoadTexture(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Samples convoy, concealment, runoff, support, surveillance, cargo, rubble, insertion and title shots.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("030812"));
        float t = Mathf.Clamp(Seconds, 0, 75);
        int shot = 0;
        while (shot < 8 && t >= Starts[shot + 1]) shot++;
        float u = (t - Starts[shot]) / (Starts[shot + 1] - Starts[shot]);
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) / 2;
        float zoom = shot switch { 1 => 1.3f, 2 => 1.15f, 3 => 1.25f, 4 => 1.15f, 5 => 1.2f, _ => 1f };
        Vector2 focus = shot switch { 1 => new(650, 560), 3 => new(1050, 590), 4 => new(1320, 460 - u * 100), 5 => new(1000, 590), _ => new(960, 540) };
        Vector2 origin = margin + (new Vector2(960, 540) - focus * zoom) * fit;
        DrawSetTransform(margin, 0, Vector2.One * fit);
        Layer(_city, new Color(.38f, .52f, .65f));
        DrawSetTransform(origin, 0, Vector2.One * fit * zoom);
        Layer(_depot, Colors.White);
        Layer(_depotLights, new Color(1, 1, 1, .65f + .2f * Mathf.Sin(t * 1.6f)));
        // Cold surveillance stays above the concealed squad. Search cones never become gameplay hazards.
        for (int i = 0; i < 3; i++)
        {
            float x = 420 + i * 610;
            float sweep = Mathf.Sin(t * .45f + i * 2) * 220;
            DrawColoredPolygon(new Vector2[] { new(x, 280), new(x + sweep - 140, 580), new(x + sweep + 140, 580) }, new Color(.35f, .75f, .95f, .10f));
            float patrolX = Mathf.PosMod(t * 30 + i * 580, 1900);
            DrawCircle(new Vector2(patrolX, 462), 8, new Color("536a7c"));
            DrawLine(new Vector2(patrolX, 470), new Vector2(patrolX, 510), new Color("405767"), 14);
        }
        // Freight is a separate moving layer with warm cargo illumination and matching siege-battery markings.
        Vector2 freightOffset = new(Mathf.PosMod(t * 36, 480) - 240, 0);
        DrawSetTransform(origin + freightOffset * fit * zoom, 0, Vector2.One * fit * zoom);
        Layer(_cargo, Colors.White);
        Layer(_cargoLights, Colors.White);
        DrawSetTransform(origin, 0, Vector2.One * fit * zoom);
        Layer(_underpass, Colors.White);
        Layer(_underpassLights, new Color(1, 1, 1, .6f));
        Layer(_walkway, Colors.White);
        Layer(_walkwayLights, Colors.White);
        // Closed rubble is established, never removed: the first playable Titan obstacle is still intact.
        if (shot >= 6)
        {
            DrawColoredPolygon(new Vector2[] { new(1450, 785), new(1470, 595), new(1530, 570), new(1550, 670), new(1620, 620), new(1680, 785) }, new Color("33434e"));
            DrawLine(new Vector2(1470, 610), new Vector2(1590, 750), new Color("89929a"), 7);
        }
        for (int actor = 2; actor >= 0; actor--)
        {
            bool moving = shot is 0 or 2 or 7 || shot == 3 && actor != 2;
            float x = actor == 0 ? 650 : actor == 1 ? 880 : 1100;
            float feet = shot <= 1 ? 680 : 800;
            if (shot == 0) x += u * 70;
            if (shot == 2) x += u * 100;
            if (shot == 3 && actor != 2)
            {
                // Teammates cross the supported upper span while Titan holds it from below.
                x = Mathf.Lerp(850 + actor * 130, 1270 + actor * 130, u);
                feet = 560;
            }
            if (shot == 7)
            {
                // Kaito lands low and left, matching Level 3's existing scrapyard spawn composition.
                x = Mathf.Lerp(x, actor == 0 ? 570 : actor == 1 ? 820 : 1020, u);
                feet = Mathf.Lerp(680, 800, Mathf.Clamp(u * 1.5f, 0, 1));
            }
            int pose = moving ? 2 + (int)(t * 7 + actor) % 4 : 0;
            Texture2D? atlas = _walking;
            if (shot == 1 && actor == 0 || shot == 2 && actor == 1 || shot == 3 && actor == 2 || shot == 5 && actor == 1)
            {
                atlas = _poses;
                pose = shot == 3 ? 1 : shot == 5 ? 2 : 0;
            }
            float bob = moving ? Mathf.Sin(t * 8 + actor) * 2 : Mathf.Sin(t * 1.4f + actor) * .8f;
            if (atlas != null) DrawTextureRectRegion(atlas, new Rect2(x - 150, feet - 265 + bob, 300, 300), new Rect2(pose * 512, actor * 512, 512, 512));
            if (shot == 2)
                for (int ring = 0; ring < 3; ring++)
                    DrawArc(new Vector2(x, feet - 3), 28 + ring * 14 + u * 12, 0, Mathf.Pi, 24, new Color(.2f, .8f, 1, (1 - u) * .22f), 2);
        }
        if (shot == 3)
        {
            float sag = Mathf.Sin(u * Mathf.Pi) * 5;
            DrawLine(new Vector2(900, 560), new Vector2(1240, 567 + sag), new Color("8c9ea8"), 15);
            DrawLine(new Vector2(1105, 570), new Vector2(1105, 685), new Color("bf91f2"), 5);
        }
        if (shot == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 endpoint = new(350 + i * 380, 490);
                DrawLine(new Vector2(1430, 230), endpoint, new Color(.3f, .85f, 1, Mathf.Sin(u * Mathf.Pi) * .5f), 2);
                DrawCircle(endpoint, 7, Cyan);
            }
        }
        DrawSetTransform(margin, 0, Vector2.One * fit);
        DrawRect(new Rect2(0, 0, 1920, 88), new Color(0, 0, 0, .88f));
        DrawString(ThemeDB.FallbackFont, new Vector2(70, 57), WildfireText.Get("UI_NEONRUSTSTAGE__DRAW_9FFD3A0E", "SILENT FREIGHT // REGIONAL TRANSIT DEPOT"), fontSize: 24, modulate: Cyan);
        if (shot == 8)
        {
            DrawRect(new Rect2(0, 210, 1920, 390), new Color(.008f, .02f, .04f, .92f));
            DrawString(ThemeDB.FallbackFont, new Vector2(180, 385), Localized("SILENT_FREIGHT_TITLE", "LEVEL 3: NEON RUST AND DATA DUST"), fontSize: 48, modulate: Cyan);
            DrawString(ThemeDB.FallbackFont, new Vector2(630, 475), Localized("SILENT_FREIGHT_INSERTION", "SCRAPYARD // INSERTION"), fontSize: 26, modulate: Colors.White);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Composites a normalized transparent 1920 by 1080 scenery layer.</summary>
    private void Layer(Texture2D? texture, Color tint)
    {
        if (texture != null) DrawTextureRect(texture, new Rect2(0, 0, 1920, 1080), false, tint);
    }

    /// <summary>Untranslated cinematic labels retain their authored English text.</summary>
    private string Localized(string key, string fallback) => Tr(key) is string text && text != key ? text : fallback;
}
