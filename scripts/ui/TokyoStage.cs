using Godot;

/// <summary>
/// Live 2D extraction stage. Camera framing, poses, lanterns, debris and petals are analytic
/// functions of Seconds, so every review seek reproduces exactly the same composition.
/// Artwork fits a 1920x1080 safe frame at every aspect ratio; no gameplay nodes are reused.
/// </summary>
public partial class TokyoStage : Control
{
    /// <summary>Local cinematic time, clamped by the overlay.</summary>
    public float Seconds { get; set; }
    /// <summary>Root of the Docker-produced visual family.</summary>
    public string ArtPath { get; set; } = "res://assets/cutscenes/tokyo/";
    /// <summary>Whether essential visual resources exist; the overlay skips safely if they do not.</summary>
    public bool ResourcesAvailable => _dojo != null && _poses != null && _blade != null;
    private Texture2D? _city, _cityStructure, _cityLights, _dojo, _dojoLights, _poses, _blade;
    private static readonly Color Cyan = new("77e5eb");

    /// <summary>Loads resources once; optional background layers may be absent without blocking progression.</summary>
    public override void _Ready()
    {
        _city = LoadTexture("res://assets/cutscenes/deployment/live/city_base.png");
        _cityStructure = LoadTexture("res://assets/cutscenes/deployment/live/city_structure.png");
        _cityLights = LoadTexture("res://assets/cutscenes/deployment/live/city_lights.png");
        _dojo = LoadTexture(ArtPath + "dojo_structure.png");
        _dojoLights = LoadTexture(ArtPath + "dojo_lights.png");
        _poses = LoadTexture(ArtPath + "poses.png");
        _blade = LoadTexture(ArtPath + "blade.png");
    }

    /// <summary>Checks existence before loading to keep the documented fallback free of engine errors.</summary>
    private static Texture2D? LoadTexture(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Draws six cinematic shots with separate scenery, character, archive and atmospheric layers.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("030812"));
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) / 2;
        float t = Seconds;
        bool close = t >= 8 && t < 15;
        float zoom = close ? 1.42f + (t - 8) * .012f : t >= 15 && t < 23 ? Mathf.Lerp(1.12f, 1, (t - 15) / 8) : 1;
        Vector2 focus = close ? new Vector2(1160, 560) : new Vector2(960, 540);
        DrawSetTransform(margin + new Vector2(960, 540) * fit - focus * fit * zoom, 0, Vector2.One * fit * zoom);
        DrawRect(new Rect2(-400, -300, 2720, 1680), new Color("081a2a"));
        Layer(_city, new Color(.65f, .7f, .8f));
        Layer(_cityStructure, new Color(.5f, .65f, .75f));
        Layer(_cityLights, new Color(.7f, .8f, .8f));
        Layer(_dojo, Colors.White);
        Layer(_dojoLights, new Color(1, .8f + Mathf.Sin(t * 2) * .06f, .6f));
        // Restrained distant fires belong to the occupied city, never the protected archive.
        for (int i = 0; i < 5; i++)
        {
            Vector2 p = new(1460 + i * 67, 475 - i % 2 * 35);
            DrawCircle(p, 20 + Mathf.Sin(t * 3 + i) * 4, new Color(.95f, .33f, .09f, .11f));
        }
        // The intact archive stays lit through every shot; its recovered testimony travels with Kaito.
        DrawRect(new Rect2(450, 540, 150, 155), new Color(.05f, .5f, .6f, .23f));
        for (int i = 0; i < 9; i++) DrawLine(new Vector2(463, 556 + i * 14), new Vector2(580 - i % 3 * 23, 556 + i * 14), Cyan, 2);
        for (int i = 0; i < 7; i++)
        {
            float fall = Mathf.PosMod(t * 23 + i * 81, 290);
            Vector2 p = new(230 + i * 96 + Mathf.Sin(t + i) * 9, 310 + fall);
            DrawLine(p - new Vector2(0, 32), p, new Color(.5f, .35f, .24f, .5f), 2);
            DrawCircle(p, 16, new Color(1, .45f, .14f, .14f));
            DrawRect(new Rect2(p - new Vector2(8, 10), new Vector2(16, 20)), new Color("dc9250"));
        }
        if (_poses != null && t < 39.6f)
        {
            float x = t < 8 ? Mathf.Lerp(860, 1120, t / 8) : t < 37 ? 1120 : Mathf.Lerp(1120, 2100, (t - 37) / 3);
            int pose = t < 8 || t >= 37 ? 0 : t < 23 ? 1 : t < 26 ? 2 : 3;
            float bob = t < 8 || t >= 37 ? Mathf.Sin(t * 12) * 5 : Mathf.Sin(t * 1.8f) * 1.5f;
            DrawTextureRectRegion(_poses, new Rect2(x - 256, 372 + bob, 512, 512), new Rect2(pose * 512, 0, 512, 512));
            if (t < 23 && _blade != null)
            {
                // Match the Data-Blade hilt to the authored hand pivot for escape and testimony poses.
                DrawTextureRect(_blade, new Rect2(x - (t < 8 ? 39 : 21), (t < 8 ? 417 : 479) + bob, 205, 205), false);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 p = new(x + 100 + Mathf.Sin(t + i) * 26, 510 + i * 12 + bob);
                    DrawLine(p, p + new Vector2(16 + i % 2 * 9, 0), new Color(.65f, 1, 1, .75f), 2);
                }
            }
        }
        // Bounded analytic particles cannot leak nodes or accumulate state after seeking.
        for (int i = 0; i < 65; i++)
        {
            Vector2 p = new(Mathf.PosMod(i * 173 + t * (18 + i % 5), 2100) - 90,
                Mathf.PosMod(i * 79 + t * (9 + i % 4), 960));
            DrawLine(p, p + new Vector2(5, Mathf.Sin(i + t) * 3), new Color(.95f, .56f, .53f, .45f), 3);
        }
        for (int i = 0; i < 24; i++)
        {
            float fall = Mathf.PosMod(t * 36 + i * 43, 440);
            Vector2 p = new(230 + i * 23 + Mathf.Sin(i + t) * 8, 330 + fall);
            DrawRect(new Rect2(p, new Vector2(4 + i % 4, 3)), new Color(.5f, .48f, .46f, .45f));
        }
        if (t >= 29)
        {
            float a = Mathf.Clamp((t - 29) / 1.2f, 0, 1);
            DrawRect(new Rect2(1250, 235, 490, 450), new Color(.008f, .05f, .08f, .94f * a));
            DrawRect(new Rect2(1250, 235, 490, 450), new Color(.3f, .9f, 1, a), false, 2);
            for (int i = 0; i < 8; i++)
                DrawLine(new Vector2(1280, 330 + i * 37), new Vector2(1700, 330 + i * 37), new Color(.2f, .65f, .7f, .3f * a), 1);
            DrawArc(new Vector2(1510, 485), 112, 0, Mathf.Tau * Mathf.Clamp(t - 29, 0, 1), 64, Cyan, 2, true);
            DrawString(ThemeDB.FallbackFont, new Vector2(1280, 290), "AXIOM-7 // SECURE UPLINK", fontSize: 22, modulate: Cyan);
            DrawString(ThemeDB.FallbackFont, new Vector2(1380, 480), "CONTAINMENT", fontSize: 24, modulate: Cyan);
            DrawString(ThemeDB.FallbackFont, new Vector2(1410, 520), "ZONE 04", fontSize: 36, modulate: Colors.White);
            DrawString(ThemeDB.FallbackFont, new Vector2(1300, 650), "THE CONCRETE WILDFIRE", fontSize: 22, modulate: new Color("edb66a"));
        }
        DrawSetTransform(margin, 0, Vector2.One * fit);
        DrawRect(new Rect2(0, 0, 1920, 90), new Color(0, 0, 0, .8f));
        DrawString(ThemeDB.FallbackFont, new Vector2(85, 58), "TOKYO REMEMBERS // ARCHIVE SECURED", fontSize: 24, modulate: Cyan);
        float fade = t < 1 ? 1 - t : Mathf.Clamp((t - 39) / 1, 0, 1);
        DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0, 0, 0, fade));
        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>Composites an optional scenery layer in the shared native art frame.</summary>
    private void Layer(Texture2D? texture, Color tint)
    {
        if (texture != null) DrawTextureRect(texture, new Rect2(0, 0, 1920, 1080), false, tint);
    }
}
