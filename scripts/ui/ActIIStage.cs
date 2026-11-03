using Godot;

/// <summary>
/// Analytically sampled surveillance district shots with high-fidelity cyber atmospheric rendering,
/// tactical squad blocking, volumetric searchlights, and smooth cinematic camera easing.
/// </summary>
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
    private static readonly float[] Starts = { 0, 8, 20, 32, 44, 56, 68, 80, 92, 104, 114, 120 };

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

    /// <summary>Composite camera inserts, warm civilians, scanner pulses and tactical operative gestures inside a fitted frame.</summary>
    public override void _Draw()
    {
        // Deep cyber backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("020509"));
        float t = Mathf.Clamp(Seconds, 0, 120);
        int shot = 0;
        while (shot < 10 && t >= Starts[shot + 1]) shot++;
        float u = (t - Starts[shot]) / (Starts[shot + 1] - Starts[shot]);
        float smoothU = Mathf.SmoothStep(0f, 1f, u);
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) / 2;

        int scene = shot is 6 or 9 ? 2 : shot is 7 or 8 or 10 ? 3 : shot is 2 or 3 or 5 ? 1 : 0;

        // Cinematic focal targeting and smooth zoom per shot
        float zoom = shot switch {
            0 => Mathf.Lerp(1.04f, 1.0f, smoothU),
            1 => 1.15f,                      // Surya at border queue
            2 => 1.25f,                      // AXIOM-7 scanning overview
            3 => 1.22f,                      // Oceanus near civilian mother & child
            4 => 1.18f,                      // Titan observing checkpoint barrier
            5 => 1.28f,                      // Aura analyzing command frequencies
            6 => 1.24f,                      // Mirage scouting subterranean passage
            7 => 1.16f,                      // AXIOM-7 registry reveal
            8 => 1.25f,                      // Kaito tactical mission order
            9 => Mathf.Lerp(1.12f, 1.02f, smoothU), // Squad deployment
            _ => 1.0f
        };

        Vector2 focus = shot switch {
            1 => new Vector2(620, 520),
            2 => new Vector2(1000, 540),
            3 => new Vector2(760, 535),
            4 => new Vector2(720, 520),
            5 => new Vector2(1150, 535),
            6 => new Vector2(1360, 530),
            7 => new Vector2(960, 520),
            8 => new Vector2(980, 535),
            _ => new Vector2(960, 540)
        };

        Vector2 origin = margin + (new Vector2(960, 540) - focus * zoom) * fit;
        DrawSetTransform(origin, 0, Vector2.One * fit * zoom);

        // Render base, structural, and lighting environment plates
        for (int layer = 0; layer < 3; layer++)
        {
            Color tint = layer == 2 ? new Color(1, 1, 1, 0.85f + 0.15f * Mathf.Sin(t * 1.5f)) : Colors.White;
            Layer(_layers[scene * 3 + layer], tint);
        }

        // Sector 0 & 1: Queue and Scanner biometric exclusion environment
        if (scene < 2)
        {
            // Displaced civilians seeking refuge
            Vector2 escort = shot == 3 ? new Vector2(-u * 145, 0) : Vector2.Zero;
            Layer(_civilians, Colors.White, escort);
            Layer(_civilianLights, Colors.White, escort);

            // Volumetric searchlight cones sweeping the crossing
            for (int s = 0; s < 4; s++)
            {
                float sx = 400 + s * 380;
                float sweep = Mathf.Sin(t * 0.4f + s * 1.5f) * 160;
                Vector2 top = new(sx, 120);
                Vector2 left = new(sx - 180 + sweep, 850);
                Vector2 right = new(sx + 60 + sweep, 850);
                DrawColoredPolygon(new[] { top, right, left }, new Color(0.7f, 0.85f, 1f, 0.035f));
            }

            // Biometric scanning columns with frequency pulses
            for (int i = 0; i < 3; i++)
            {
                float x = 792 + i * 360;
                float y = 345 + Mathf.PosMod(t * 75, 290);
                DrawRect(new Rect2(x, y, 214, 8), new Color(0.9f, 0.97f, 1, 0.25f));
                DrawColoredPolygon(new Vector2[] { new(x, 315), new(x + 214, 315), new(x + 240, 790), new(x - 26, 790) },
                    new Color(0.85f, 0.93f, 1, 0.045f));
            }
        }

        // Operative tactical staging across Shots 1 to 9
        if (shot > 0 && shot < 10)
        {
            // Character thematic positioning and speaker identification
            // 5: Surya (Queue), 1: Oceanus (Civilians), 2: Titan (Anchor), 0: Kaito (Lead), 3: Aura (Scan), 4: Mirage (Flank)
            for (int actor = 5; actor >= 0; actor--)
            {
                bool speaker = shot == 1 && actor == 5 || shot == 3 && actor == 1 || shot == 4 && actor == 2 ||
                    shot == 5 && actor == 3 || shot == 6 && actor == 4 || shot == 8 && actor == 0;

                // Tactical coordinates per operative
                float x = actor switch {
                    5 => 360,                  // Surya protecting the border queue
                    1 => 760 - (shot == 3 ? u * 145 : 0), // Oceanus comforting civilians
                    2 => 560,                  // Titan heavy anchor
                    0 => 980,                  // Kaito vanguard leader
                    3 => 1180,                 // Aura analyzing signals
                    4 => 1380,                 // Mirage flanking service entrance
                    _ => 280 + actor * 185
                };
                float y = actor switch {
                    5 => 525,
                    1 => 535,
                    2 => 515,
                    0 => 540,
                    3 => 535,
                    4 => 525,
                    _ => 545
                };

                if (shot == 9) x += actor == 5 ? 0 : u * 280;
                int pose = shot == 9 && actor != 5 ? 2 + (int)(t * 6 + actor) % 4 : speaker ? 1 : 0;
                float breath = Mathf.Sin(t * 2.0f + actor) * 1.8f;

                // Speaker tactical beacon ring
                if (speaker)
                {
                    Color beaconColor = actor switch {
                        5 => new Color("ffe29a"), // Surya (Gold)
                        1 => new Color("66d9e8"), // Oceanus (Aqua)
                        2 => new Color("c084fc"), // Titan (Violet)
                        3 => new Color("f472b6"), // Aura (Pink)
                        4 => new Color("34d399"), // Mirage (Emerald)
                        0 => new Color("f87171"), // Kaito (Crimson)
                        _ => Colors.Cyan
                    };
                    float pulse = 0.4f + 0.3f * Mathf.Sin(t * 5.0f);
                    DrawArc(new Vector2(x, y + 215), 52, 0, Mathf.Tau, 32, new Color(beaconColor, pulse), 2.5f);
                    DrawArc(new Vector2(x, y + 215), 68, -0.6f, 0.6f, 16, new Color(beaconColor, pulse * 0.7f), 1.5f);
                }

                if (actor == 5)
                {
                    if (_surya != null)
                        DrawTextureRectRegion(_surya, new Rect2(x - 120, y + breath, 240, 240), new Rect2((speaker ? 1 : 0) * 512, 0, 512, 512));
                }
                else if (_squad != null)
                {
                    DrawTextureRectRegion(_squad, new Rect2(x - 120, y + breath, 240, 240), new Rect2(pose * 512, actor * 512, 512, 512));
                }
            }
        }

        // Aura's harmonic resonance diagnostic display (Shot 5)
        if (shot == 5)
        {
            Vector2 scanOrigin = new(1180, 580);
            for (int ring = 0; ring < 5; ring++)
            {
                float radius = 30 + ring * 28 + Mathf.PosMod(t * 40, 28);
                float alpha = (1f - (radius / 180f)) * 0.45f;
                DrawArc(scanOrigin, radius, -1.8f, 1.8f, 48, new Color(0.95f, 0.45f, 0.85f, alpha), 2);
            }
            DrawLine(scanOrigin, new Vector2(1000, 420), new Color(0.95f, 0.45f, 0.85f, 0.5f), 1.5f);
        }

        // Mirage's subterranean service blueprint trace (Shot 6)
        if (shot == 6)
        {
            Vector2[] path = { new(1380, 720), new(1520, 720), new(1640, 610), new(1780, 560) };
            DrawPolyline(path, new Color(0.3f, 0.95f, 0.65f, 0.75f), 3);
            for (int node = 0; node < path.Length; node++)
                DrawCircle(path[node], 4.5f, new Color(0.8f, 1f, 0.9f, 0.9f));
        }

        // Return to standard letterbox coordinates for overlays and title typography
        DrawSetTransform(margin, 0, Vector2.One * fit);

        // Shot 0: High-fidelity Act II Title Reveal
        if (shot == 0)
        {
            // Transition curtains with cyber borders
            if (t < 3.2f)
            {
                float retreat = t * 175;
                DrawRect(new Rect2(-retreat, 0, 510, 1080), new Color("141e26"));
                DrawRect(new Rect2(1410 + retreat, 0, 510, 1080), new Color("141e26"));
                DrawLine(new Vector2(510 - retreat, 0), new Vector2(510 - retreat, 1080), new Color("e4b777"), 6);
                DrawLine(new Vector2(1410 + retreat, 0), new Vector2(1410 + retreat, 1080), new Color("e4b777"), 6);
            }

            // Digital scan flash
            float flash = t < 2.5f ? Mathf.Sin(t / 2.5f * Mathf.Pi) * 0.4f : 0;
            if (flash > 0) DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0, 0.94f, 1f, flash));

            // Letterbox dim overlay
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0.008f, 0.018f, 0.035f, 0.72f));

            // Anamorphic cyber flare lines
            float flareAlpha = Mathf.Clamp((t - 1.2f) * 0.8f, 0, 1);
            if (flareAlpha > 0)
            {
                DrawLine(new Vector2(200, 465), new Vector2(1720, 465), new Color(0, 0.9f, 1f, flareAlpha * 0.35f), 2);
                DrawLine(new Vector2(350, 465), new Vector2(1570, 465), new Color(1f, 0.85f, 0.45f, flareAlpha * 0.7f), 1);
            }

            // Decorative title brackets
            if (t >= 1.5f)
            {
                Title("ACT_II_TITLE", "ACT II", 405, 48, Colors.White);
            }
            if (t >= 2.0f)
            {
                Title("ACT_II_SUBTITLE", "THE UNREGISTERED", 505, 66, new Color("e4b777"));
            }
        }

        // Shot 10: Mission 5 Prelude Transition
        if (shot == 10)
        {
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0.008f, 0.018f, 0.035f, 0.88f));
            Title("ACT_II_MISSION_TITLE", "MISSION 5: NAMES WITHOUT NUMBERS", 440, 42, new Color("e4b777"));
            Title("ACT_II_OBJECTIVE", "Infiltrate the district registry. Recover the original identity records.", 515, 26, Colors.White);
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0, 0, 0, Mathf.Clamp((t - 118) / 2, 0, 1)));
        }

        // Reset transform
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Render localized title text centered independently of artwork and viewport aspect.</summary>
    private void Title(string key, string fallback, float y, int size, Color color)
    {
        string text = Tr(key);
        if (text == key || string.IsNullOrWhiteSpace(text)) text = fallback;
        float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: size).X;
        if (width > 1740) size = (int)(size * 1740 / width);
        width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: size).X;
        DrawString(ThemeDB.FallbackFont, new Vector2((1920 - width) / 2, y), text, fontSize: size, modulate: color);
    }

    /// <summary>Composite a transparent layer with an optional deterministic civilian escort offset.</summary>
    private void Layer(Texture2D? texture, Color tint, Vector2 offset = default)
    {
        if (texture != null) DrawTextureRect(texture, new Rect2(offset, new Vector2(1920, 1080)), false, tint);
    }
}
