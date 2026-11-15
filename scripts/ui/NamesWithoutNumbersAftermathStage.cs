using Godot;

/// <summary>
/// Seekable 2D cinematic tableau of the liberated civic processing district in Mission 5 ("Names Without Numbers").
/// Features smooth camera choreography, volumetric searchlights, holographic registry projection,
/// civilian identity record returns, and distinctive operative staging.
/// </summary>
public partial class NamesWithoutNumbersAftermathStage : Control
{
    /// <summary>Asset root shared with the parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/names_without_numbers/";
    /// <summary>Timeline sample supplied by the overlay; motion never accumulates frame error.</summary>
    public float Seconds { get; set; }
    /// <summary>Essential scene plates gate playback; their absence requests a safe results handoff.</summary>
    public bool ResourcesAvailable => _district != null && _lights != null;

    private Texture2D? _district;
    private Texture2D? _lights;

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan

    private static readonly Color[] OperativeColors = { ColorKaito, ColorMirage, ColorAura, ColorOceanus, ColorTitan };

    // Shot boundary schedule matching NamesWithoutNumbersAftermath
    private static readonly float[] ShotTimes = { 0f, 12f, 22f, 31f, 40f, 49f, 58f, 68f, 82f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(1010, 500), // Shot 0: Amara at the terminal
        new(1060, 510), // Shot 1: Civilians gathering
        new(1410, 480), // Shot 2: Mirage vantage
        new(520, 480),  // Shot 3: Aura resonance
        new(910, 490),  // Shot 4: Oceanus transit gates
        new(1320, 500), // Shot 5: Titan bulwark
        new(420, 460),  // Shot 6: Surya comms uplink
        new(1020, 500), // Shot 7: Kaito rally
        new(960, 540)   // Shot 8: District wide fade
    };

    private static readonly float[] CameraZooms = {
        1.13f, // Shot 0
        1.15f, // Shot 1
        1.18f, // Shot 2
        1.18f, // Shot 3
        1.18f, // Shot 4
        1.18f, // Shot 5
        1.15f, // Shot 6
        1.06f, // Shot 7
        1.00f  // Shot 8
    };

    /// <summary>Loads the two original transparent district plates without emitting missing-file errors.</summary>
    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _district = Load(ArtPath + "district_structure.png");
        _lights = Load(ArtPath + "district_lights.png");
    }

    /// <summary>Returns null quietly for absent essential art so the owning overlay can finish safely.</summary>
    private static Texture2D? Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Composes the district, holographic record return, civilians and squad reactions analytically.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("050c14"));
        if (!ResourcesAvailable) return;

        float t = Mathf.Clamp(Seconds, 0, 90);
        int shot = GetCurrentShot(t);

        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) * 0.5f;

        // Calculate smooth interpolated camera position and zoom across shot transitions
        Vector2 camPos = GetSmoothCameraPosition(t, shot);
        float camZoom = GetSmoothCameraZoom(t, shot);

        // 1. Static base district plates
        DrawSetTransform(margin, 0, Vector2.One * fit);
        DrawTextureRect(_district!, new Rect2(0, 0, 1920, 1080), false);
        
        // Dynamic district lighting glow - pulse subtly with civic power restoration
        float lightPulse = 0.88f + 0.12f * Mathf.Sin(t * 1.8f);
        DrawTextureRect(_lights!, new Rect2(0, 0, 1920, 1080), false, new Color(1, 1, 1, lightPulse));

        // Volumetric sweeping searchlights from civic towers
        DrawVolumetricSearchlights(t);

        // 2. Camera-zoomed cinematic layer
        DrawSetTransform(margin + (new Vector2(960, 480) - camPos * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // Holographic registry data pillar rising from the restored terminal
        DrawHolographicTerminal(t);

        // Civilian group with individual posture and glowing returned records
        DrawCivilians(t);

        // Operative squad staging based on current shot
        DrawOperatives(t, shot);

        // 3. Screen-space post-processing layer
        DrawSetTransform(margin, 0, Vector2.One * fit);

        // Anamorphic lens flare sweeps during key narrative moments
        DrawAnamorphicFlares(t, shot);

        // Cinematic 2.39:1 letterbox bars with tech brackets
        DrawCinematicLetterbox();

        // Final fade to black in closing seconds
        if (t >= 84)
        {
            float fade = Mathf.Clamp((t - 84) / 5f, 0, 1);
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0, 0, 0, fade));
        }

        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Finds the active shot index from timeline position.</summary>
    private static int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t < ShotTimes[i + 1]) return i;
        }
        return ShotTimes.Length - 2;
    }

    /// <summary>Smoothly interpolates camera position between shot targets with an ease-in-out curve.</summary>
    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotElapsed = t - shotStart;
        Vector2 target = CameraTargets[shot];

        // Smooth transition over the first 1.4 seconds of each shot
        if (shot > 0 && shotElapsed < 1.4f)
        {
            Vector2 prev = CameraTargets[shot - 1];
            float blend = Mathf.SmoothStep(0f, 1f, shotElapsed / 1.4f);
            return prev.Lerp(target, blend);
        }
        return target;
    }

    /// <summary>Smoothly interpolates camera zoom between shot targets.</summary>
    private static float GetSmoothCameraZoom(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotElapsed = t - shotStart;
        float target = CameraZooms[shot];

        if (shot > 0 && shotElapsed < 1.4f)
        {
            float prev = CameraZooms[shot - 1];
            float blend = Mathf.SmoothStep(0f, 1f, shotElapsed / 1.4f);
            return Mathf.Lerp(prev, target, blend);
        }
        return target;
    }

    /// <summary>Renders sweeping volumetric searchlight cones that evoke surveillance yielding to freedom.</summary>
    private void DrawVolumetricSearchlights(float t)
    {
        // Tower 1 (Left): Sweeping across the sky
        float angle1 = Mathf.Sin(t * 0.4f) * 0.25f + 0.35f;
        Vector2 origin1 = new(240, 120);
        Vector2 dir1 = new(Mathf.Cos(angle1), Mathf.Sin(angle1));
        Vector2 perp1 = new(-dir1.Y, dir1.X);
        Vector2[] cone1 = {
            origin1,
            origin1 + dir1 * 1200 + perp1 * 180,
            origin1 + dir1 * 1200 - perp1 * 180
        };
        DrawPolygon(cone1, new[] { new Color(0, 0.94f, 1f, 0.035f) });

        // Tower 2 (Right): Sweeping across the district plaza
        float angle2 = Mathf.Cos(t * 0.35f) * 0.2f + 2.7f;
        Vector2 origin2 = new(1720, 140);
        Vector2 dir2 = new(Mathf.Cos(angle2), Mathf.Sin(angle2));
        Vector2 perp2 = new(-dir2.Y, dir2.X);
        Vector2[] cone2 = {
            origin2,
            origin2 + dir2 * 1100 + perp2 * 160,
            origin2 + dir2 * 1100 - perp2 * 160
        };
        DrawPolygon(cone2, new[] { new Color(0.95f, 0.78f, 0.45f, 0.03f) });
    }

    /// <summary>Renders the active registry terminal with holographic data projection beams.</summary>
    private void DrawHolographicTerminal(float t)
    {
        // Terminal physical housing at (825, 646)
        DrawRect(new Rect2(825, 646, 184, 72), new Color("14222a"));
        DrawRect(new Rect2(825, 646, 184, 3), ColorCyan);

        // Terminal status displays
        DrawRect(new Rect2(846, 660, 142, 8), new Color("e4c98b"));
        DrawRect(new Rect2(846, 676, 142, 8), new Color("34d399"));

        // Holographic vertical data pillar (restored registry carrier stream)
        float streamAlpha = 0.18f + 0.08f * Mathf.Sin(t * 4f);
        DrawRect(new Rect2(895, 280, 44, 366), new Color(0, 0.94f, 1f, streamAlpha));
        DrawRect(new Rect2(912, 280, 10, 366), new Color(1f, 1f, 1f, streamAlpha * 1.5f));

        // Floating digital data brackets
        for (int b = 0; b < 4; b++)
        {
            float by = 620 - ((t * 45 + b * 80) % 320);
            DrawLine(new Vector2(880, by), new Vector2(954, by), new Color(0, 0.94f, 1f, 0.4f), 1.5f);
        }

        // Animated record return stream: golden identity packets floating to civilians
        float returnProgress = Mathf.Clamp((t - 6) / 50f, 0f, 1f);
        DrawLine(new Vector2(915, 646), new Vector2(Mathf.Lerp(915, 1100, returnProgress), 770), new Color("dfc99c", 0.65f), 2);

        // Floating identity particles
        for (int p = 0; p < 8; p++)
        {
            float seed = p * 1.37f;
            float pProgress = Mathf.PosMod(t * 0.18f + seed * 0.2f, 1f);
            float px = Mathf.Lerp(915, 1150, pProgress);
            float py = Mathf.Lerp(646, 780, pProgress) + Mathf.Sin(t * 3f + seed) * 16;
            DrawCircle(new Vector2(px, py), 3f, new Color("ffe29a"));
        }
    }

    /// <summary>Draws civilians in the district with natural poses and glowing recovered documents.</summary>
    private void DrawCivilians(float t)
    {
        for (int i = 0; i < 7; i++)
        {
            float x = 935 + i * 91 + (i > 0 ? Mathf.Min(t, 24) * (i % 2 == 0 ? 0.9f : -0.45f) : 0);
            float feet = 828 + (i % 2) * 3;
            float scale = i == 0 ? 1.08f : 0.82f + (i % 3) * 0.08f;
            DrawCivilian(x, feet, scale, i % 3, i == 0 && t < 13);
        }
    }

    /// <summary>Reuses one of the three SpriteCook civilians, keeping the authored ground line and record effect.</summary>
    private void DrawCivilian(float x, float feet, float scale, int variant, bool holdingRecord)
    {
        if (!CampaignArtwork.DrawCivilian(this, new Vector2(x, feet), 151f * scale, variant)) return;

        // Glowing identity paper in hand
        if (holdingRecord)
        {
            DrawRect(new Rect2(x + 45 * scale, feet - 70 * scale, 27 * scale, 35 * scale), new Color("fff3cf"));
            DrawRect(new Rect2(x + 47 * scale, feet - 68 * scale, 23 * scale, 31 * scale), new Color("00f0ff", 0.4f));
        }
    }

    /// <summary>Renders squad operatives in position with signature gear and active speaker beacon.</summary>
    private void DrawOperatives(float t, int shot)
    {
        // Shots 2 through 6 focus on individual squad members
        if (shot >= 2 && shot <= 6)
        {
            int operative = shot switch { 2 => 1, 3 => 2, 4 => 3, 5 => 4, _ => 0 };
            float opX = shot switch { 2 => 1410, 3 => 520, 4 => 910, 5 => 1320, _ => 420 };
            float opFeet = 826;

            // Active speaker ground beacon ring
            DrawSpeakerBeacon(opX, opFeet, OperativeColors[operative], t);

            // Operative silhouette
            DrawOperative(opX, opFeet, OperativeColors[operative], operative);

            // Callsign banner
            string[] names = { "KAITO", "MIRAGE", "AURA", "OCEANUS", "TITAN" };
            DrawString(ThemeDB.FallbackFont, new Vector2(opX - 60, opFeet + 35), names[operative], fontSize: 18, modulate: OperativeColors[operative]);
        }

        // Shot 6: Surya Comms Uplink Beacon
        if (shot == 6)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3.5f);
            Vector2 satPos = new(420, 335);
            DrawArc(satPos, 22 + pulse * 14, 0, Mathf.Tau, 32, new Color(ColorSurya, 0.7f), 3);
            DrawArc(satPos, 36 + pulse * 20, 0, Mathf.Tau, 32, new Color(ColorSurya, 0.35f), 2);
            DrawLine(satPos - new Vector2(25, 0), satPos + new Vector2(25, 0), ColorSurya, 2);
            DrawLine(satPos - new Vector2(0, 25), satPos + new Vector2(0, 25), ColorSurya, 2);
            DrawString(ThemeDB.FallbackFont, new Vector2(330, 395), WildfireText.Get("UI_NAMESWITHOUTNUMBERSAFTERMATHSTAGE_DRAWOPERATIVES_54F4CC75", "SURYA // COMMS LINK"), fontSize: 16, modulate: ColorSurya);
        }

        // Shot 7: Kaito Vanguard Leader Rally
        if (shot == 7)
        {
            DrawSpeakerBeacon(1020, 826, ColorKaito, t);
            DrawOperative(1020, 826, ColorKaito, 0);
            DrawString(ThemeDB.FallbackFont, new Vector2(965, 862), WildfireText.Get("UI_NAMESWITHOUTNUMBERSAFTERMATHSTAGE_DRAWOPERATIVES_C9FE73BD", "KAITO // COMMAND"), fontSize: 18, modulate: ColorKaito);
        }
    }

    /// <summary>Draws a pulsing ground beacon ring under the speaking character.</summary>
    private void DrawSpeakerBeacon(float x, float feet, Color color, float t)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);
        DrawArc(new Vector2(x, feet), 36 + pulse * 8, 0, Mathf.Tau, 32, new Color(color, 0.45f), 2);
        DrawArc(new Vector2(x, feet), 48 + pulse * 12, 0, Mathf.Tau, 32, new Color(color, 0.2f), 1.5f);
    }

    /// <summary>Uses the matching SpriteCook body at the shot's original foot position.</summary>
    private void DrawOperative(float x, float feet, Color color, int id)
    {
        string operative = id switch
        {
            1 => "mirage_blade", 2 => "phoenicia", 3 => "oceanus", 4 => "apex_titan", _ => "cyber_shogun"
        };
        CampaignArtwork.DrawOperative(this, operative, new Vector2(x, feet), 151f, speaking: true);
    }
    /// <summary>Draws horizontal anamorphic cyber flare sweeps during key narrative moments.</summary>
    private void DrawAnamorphicFlares(float t, int shot)
    {
        // Trigger subtle anamorphic flares at Amara declaration, Surya comms, and Kaito command
        float flareAlpha = 0f;
        Color flareColor = ColorCyan;

        if (shot == 0 && t > 1f && t < 4.5f)
        {
            flareAlpha = Mathf.Sin((t - 1f) / 3.5f * Mathf.Pi) * 0.45f;
            flareColor = new Color("fff3cf");
        }
        else if (shot == 6 && t > 59f && t < 63f)
        {
            flareAlpha = Mathf.Sin((t - 59f) / 4f * Mathf.Pi) * 0.5f;
            flareColor = ColorSurya;
        }
        else if (shot == 7 && t > 69f && t < 74f)
        {
            flareAlpha = Mathf.Sin((t - 69f) / 5f * Mathf.Pi) * 0.55f;
            flareColor = ColorKaito;
        }

        if (flareAlpha > 0)
        {
            DrawLine(new Vector2(100, 520), new Vector2(1820, 520), new Color(flareColor, flareAlpha * 0.4f), 2);
            DrawLine(new Vector2(300, 520), new Vector2(1620, 520), new Color(1f, 1f, 1f, flareAlpha * 0.8f), 1);
        }
    }

    /// <summary>Renders cinematic 2.39:1 widescreen letterbox bars with tech brackets.</summary>
    private void DrawCinematicLetterbox()
    {
        float barHeight = 84;
        // Top bar
        DrawRect(new Rect2(0, 0, 1920, barHeight), new Color(0, 0, 0, 0.95f));
        DrawLine(new Vector2(0, barHeight), new Vector2(1920, barHeight), new Color(ColorCyan, 0.35f), 1.5f);

        // Bottom bar
        DrawRect(new Rect2(0, 1080 - barHeight, 1920, barHeight), new Color(0, 0, 0, 0.95f));
        DrawLine(new Vector2(0, 1080 - barHeight), new Vector2(1920, 1080 - barHeight), new Color(ColorCyan, 0.35f), 1.5f);
    }
}
