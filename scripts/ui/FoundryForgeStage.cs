using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 7 pre-mission prelude ("The Molten Heart // The Foundry Forge").
/// Composes the subterranean volcanic industrial chasm, surging molten plasma rivers, geothermal cooling towers,
/// high-voltage conduit arcs, squad catwalk overlook, and holographic reactor core telemetry across nine authored beats.
/// </summary>
public partial class FoundryForgeStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/foundry_forge_intro/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>Requires authored environment art and cue metadata; the owner supplies a safe completion fallback.</summary>
    public bool ResourcesAvailable => CampaignArtwork.Available("foundry_forge", ArtPath);

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan
    private static readonly Color ColorLavaCore = new("ff4400"); // Incandescent Magma Red
    private static readonly Color ColorLavaMid = new("f97316");  // Molten Plasma Orange
    private static readonly Color ColorLavaGlow = new("facc15"); // Superheated Yellow Glow
    private static readonly Color ColorBasalt = new("0c0604");   // Volcanic Basalt Rock

    // Shot boundary schedule matching FoundryForgeIntro dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 8f, 18f, 28f, 38f, 48f, 58f, 68f, 78f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - volcanic chasm & molten rivers
        new(820, 520),  // Shot 1: Kaito pointing at upward conduit towers
        new(1380, 560), // Shot 2: Titan clenching fists at catwalk edge
        new(620, 580),  // Shot 3: Oceanus observing boiling coolant channels
        new(1160, 540), // Shot 4: Aura analyzing acoustic lattice harmonics
        new(460, 540),  // Shot 5: Mirage scouting magma sluice infiltration route
        new(960, 460),  // Shot 6: Surya tactical comms link & reactor telemetry
        new(1100, 540), // Shot 7: Kaito and Titan side-by-side overlooking core
        new(960, 500)   // Shot 8: Wide squad rally - drop into the forge
    };

    private static readonly float[] CameraZooms = {
        1.02f, // Shot 0
        1.16f, // Shot 1
        1.20f, // Shot 2
        1.18f, // Shot 3
        1.18f, // Shot 4
        1.22f, // Shot 5
        1.14f, // Shot 6
        1.12f, // Shot 7
        1.00f  // Shot 8
    };

    /// <summary>
    /// Composes volcanic chasm, molten plasma flows, cooling towers, electrical conduit arcs,
    /// squad operative silhouettes on the gantry, holographic core schematics, and cinema letterbox.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep volcanic abyss backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), ColorBasalt);

        float t = Mathf.Clamp(Seconds, 0, 90f);
        int shot = GetCurrentShot(t);

        // Aspect fit to standard 1920x1080 canvas
        float fit = Mathf.Min(Size.X / 1920f, Size.Y / 1080f);
        Vector2 margin = (Size - new Vector2(1920f, 1080f) * fit) * 0.5f;

        Vector2 camPos = GetSmoothCameraPosition(t, shot);
        camPos = CampaignArtwork.Focus(ArtPath, Seconds, camPos);
        float camZoom = GetSmoothCameraZoom(t, shot);

        // 2. Camera-zoomed cinematic world layer
        DrawSetTransform(margin + (new Vector2(960f, 540f) - camPos * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // Volcanic rift basalt rock formations and cavernous roof
        DrawBasaltRiftBackground(t);
        CampaignArtwork.DrawEnvironment(this, "foundry_forge", Seconds, aftermath: false, drawBase: true);

        // Surging rivers of molten geothermal plasma flowing through subterranean trenches
        DrawMoltenPlasmaRivers(t);

        // Massive industrial geothermal cooling towers venting superheated steam plumes
        DrawCoolingTowersAndSteam(t);

        // Towering energy conduits with high-voltage crackling electric arcs beaming upward
        DrawEnergyConduitsAndArcs(t);

        // Industrial maintenance catwalk gantry overlooking the chasm
        DrawCatwalkGantry(t);

        // Axiom Force operative silhouettes staged on the gantry
        DrawOperativeSilhouettes(t, shot);

        // Floating holographic tactical display (reactor core 3D wireframe, shield uplink telemetry)
        DrawReactorCoreHologram(t, shot);

        // 3. Screen-space overlays (letterbox bars, CRT scanlines, heat haze vignette)
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawScreenVignette(fit, margin, t);
    }

    /// <summary>Identifies the active shot index based on timeline seconds.</summary>
    private static int GetCurrentShot(float t)
    {
        for (int i = ShotTimes.Length - 2; i >= 0; i--)
        {
            if (t >= ShotTimes[i]) return i;
        }
        return 0;
    }

    /// <summary>Smoothly interpolates camera focal coordinates between adjacent shots.</summary>
    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        if (shot >= CameraTargets.Length - 1) return CameraTargets[^1];
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float progress = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);
        float ease = progress < 0.28f ? Mathf.SmoothStep(0f, 1f, progress / 0.28f) : 1f;
        Vector2 prev = shot > 0 ? CameraTargets[shot - 1] : CameraTargets[0];
        return prev.Lerp(CameraTargets[shot], ease);
    }

    /// <summary>Smoothly interpolates camera zoom between adjacent shots.</summary>
    private static float GetSmoothCameraZoom(float t, int shot)
    {
        if (shot >= CameraZooms.Length - 1) return CameraZooms[^1];
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float progress = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);
        float ease = progress < 0.32f ? Mathf.SmoothStep(0f, 1f, progress / 0.32f) : 1f;
        float prev = shot > 0 ? CameraZooms[shot - 1] : CameraZooms[0];
        return Mathf.Lerp(prev, CameraZooms[shot], ease);
    }

    /// <summary>Renders jagged basalt rock cavern formations and distant volcanic fissures.</summary>
    private void DrawBasaltRiftBackground(float t)
    {
        // Ambient heat haze wash from below
        float heatPulse = 0.85f + 0.15f * Mathf.Sin(t * 2.0f);
        DrawRect(new Rect2(0, 400, 1920, 680), ColorLavaCore * (0.09f * heatPulse));

        // Distant jagged basalt pillars
        Vector2[] leftPillar = {
            new(0, 0), new(280, 0), new(220, 520), new(320, 780), new(180, 1080), new(0, 1080)
        };
        DrawColoredPolygon(leftPillar, new Color("120906"));
        DrawPolyline(leftPillar, new Color("26120b"), 2f);

        Vector2[] rightPillar = {
            new(1920, 0), new(1640, 0), new(1700, 480), new(1580, 760), new(1740, 1080), new(1920, 1080)
        };
        DrawColoredPolygon(rightPillar, new Color("120906"));
        DrawPolyline(rightPillar, new Color("26120b"), 2f);

        // Ceiling rock strata and hanging industrial pipe racks
        for (int p = 120; p < 1800; p += 140)
        {
            float sag = Mathf.Sin(p * 0.05f) * 15f;
            DrawLine(new Vector2(p, 0), new Vector2(p + 20, 180 + sag), new Color("1e120d"), 3f);
        }
    }

    /// <summary>Renders glowing molten plasma rivers flowing across subterranean channels.</summary>
    private void DrawMoltenPlasmaRivers(float t)
    {
        // Lower trench bounds: Y=780 to Y=1080
        Rect2 riverBed = new(0, 820, 1920, 260);
        DrawRect(riverBed, new Color("2b0f06"));

        // Flowing plasma river bands with undulating sinuous surface waves
        for (int layer = 0; layer < 4; layer++)
        {
            float speed = 1.2f + layer * 0.4f;
            float yBase = 840f + layer * 55f;
            Color col = layer switch
            {
                0 => ColorLavaCore * 0.9f,
                1 => ColorLavaMid * 0.95f,
                2 => ColorLavaGlow * 0.9f,
                _ => new Color("fffbeb") * 0.85f
            };

            int segments = 24;
            Vector2[] points = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float x = i * (1920f / segments);
                float wave = Mathf.Sin(x * 0.008f + t * speed + layer) * (14f - layer * 2f);
                points[i] = new Vector2(x, yBase + wave);
            }

            DrawPolyline(points, col, 12f - layer * 2f);
        }

        // Convective incandescent bubbles popping along plasma surface
        for (int b = 0; b < 10; b++)
        {
            float seed = b * 31.7f;
            float bx = (seed * 83f) % 1800f + 60f;
            float life = (t * 1.5f + seed) % 1.0f;
            float by = 860f + (b % 3) * 60f - life * 24f;
            float radius = 5f + life * 12f;
            float alpha = (1f - life) * 0.7f;

            DrawCircle(new Vector2(bx, by), radius, ColorLavaGlow * alpha);
            DrawArc(new Vector2(bx, by), radius, 0, Mathf.Tau, 16, ColorLavaMid * alpha, 1.5f);
        }

        // Floating fiery thermal embers rising into the air
        for (int e = 0; e < 20; e++)
        {
            float eseed = e * 19.3f;
            float ex = ((eseed * 123f + t * 40f) % 1840f) + 40f;
            float ey = 980f - ((t * 70f + eseed * 80f) % 650f);
            float esize = 2f + (e % 3);
            float ealpha = Mathf.Clamp(1f - (980f - ey) / 650f, 0f, 1f);
            DrawCircle(new Vector2(ex, ey), esize, ColorLavaGlow * (0.8f * ealpha));
        }
    }

    /// <summary>Renders massive geothermal cooling towers venting clouds of superheated steam.</summary>
    private void DrawCoolingTowersAndSteam(float t)
    {
        // Left cooling tower (X=440)
        DrawTower(new Vector2(440, 480), 140, 340, t);

        // Right cooling tower (X=1480)
        DrawTower(new Vector2(1480, 480), 140, 340, t + 1.5f);

        // Central reactor containment housing (X=960)
        Rect2 housing = new(840, 460, 240, 360);
        DrawRect(housing, new Color("140a08"));
        DrawRect(housing, new Color("3d1a10"), false, 3f);

        // Reactor core circular observation window with pulsing inner plasma
        float corePulse = 0.75f + 0.25f * Mathf.Sin(t * 3.5f);
        Vector2 coreCenter = new(960, 580);
        DrawCircle(coreCenter, 65, ColorLavaCore * corePulse);
        DrawCircle(coreCenter, 45, ColorLavaMid * corePulse);
        DrawCircle(coreCenter, 25, ColorLavaGlow * corePulse);
        DrawArc(coreCenter, 65, 0, Mathf.Tau, 32, ColorLavaGlow * 0.8f, 3f);
        DrawArc(coreCenter, 80, 0, Mathf.Tau, 32, ColorCyan * 0.5f, 2f);
    }

    /// <summary>Renders a single hyperbolic geothermal cooling tower and venting steam.</summary>
    private void DrawTower(Vector2 pos, float width, float height, float t)
    {
        Rect2 towerRect = new(pos.X - width * 0.5f, pos.Y, width, height);
        DrawRect(towerRect, new Color("170d0a"));
        DrawRect(towerRect, new Color("2e1811"), false, 2f);

        // Horizontal steel cooling rib bands
        for (float y = pos.Y + 40; y < pos.Y + height; y += 50)
        {
            DrawLine(new Vector2(pos.X - width * 0.5f, y), new Vector2(pos.X + width * 0.5f, y), new Color("422116"), 2f);
        }

        // Billowing steam clouds rising from top of tower
        for (int s = 0; s < 5; s++)
        {
            float sTime = (t * 0.8f + s * 0.2f) % 1.0f;
            float sy = pos.Y - sTime * 180f;
            float sx = pos.X + Mathf.Sin(sTime * 4f + s) * 25f;
            float sRadius = 24f + sTime * 35f;
            float sAlpha = (1f - sTime) * 0.16f;
            DrawCircle(new Vector2(sx, sy), sRadius, new Color("fef3c7") * sAlpha);
        }
    }

    /// <summary>Renders heavy high-voltage energy conduits crackling with electric arcs beaming skyward.</summary>
    private void DrawEnergyConduitsAndArcs(float t)
    {
        // Central conduit trunks channeling power to the Citadel Spire
        Vector2[] conduits = {
            new(880, 460), new(960, 460), new(1040, 460)
        };

        foreach (var c in conduits)
        {
            // Vertical conduit cables stretching up off screen
            DrawLine(c, new Vector2(c.X, 0), new Color("1e100a"), 8f);
            DrawLine(c, new Vector2(c.X, 0), ColorCyan * 0.5f, 2f);

            // High-voltage electric arc spikes traveling up the conduits
            float arcPhase = (t * 2.5f + c.X * 0.01f) % 1.0f;
            float arcY = 460f - arcPhase * 460f;
            DrawCircle(new Vector2(c.X, arcY), 6f, ColorCyan * 0.9f);
            DrawLine(new Vector2(c.X - 8, arcY - 4), new Vector2(c.X + 8, arcY + 4), Colors.White, 2f);
        }
    }

    /// <summary>Renders the heavy industrial maintenance catwalk gantry where the squad stands.</summary>
    private void DrawCatwalkGantry(float t)
    {
        // Catwalk level: centered at Y=660, width 1400, left 260, right 1660
        Rect2 gantryRect = new(260, 660, 1400, 45);
        DrawRect(gantryRect, new Color("161210"));
        DrawRect(new Rect2(260, 660, 1400, 6), new Color("3b2820"));

        // Diamond tread floor lines
        for (int x = 280; x < 1640; x += 32)
        {
            DrawLine(new Vector2(x, 666), new Vector2(x + 16, 705), new Color("251b17"), 1f);
            DrawLine(new Vector2(x + 16, 666), new Vector2(x, 705), new Color("251b17"), 1f);
        }

        // Safety railings (top bar, mid bar, support posts)
        DrawLine(new Vector2(260, 600), new Vector2(1660, 600), new Color("573b2d"), 4f);
        DrawLine(new Vector2(260, 630), new Vector2(1660, 630), new Color("3d281e"), 2f);
        for (int px = 260; px <= 1660; px += 140)
        {
            DrawLine(new Vector2(px, 600), new Vector2(px, 660), new Color("573b2d"), 3f);
        }

        // Glowing under-gantry heat shield reflection
        DrawRect(new Rect2(300, 705, 1320, 15), ColorLavaMid * 0.18f);
    }

    /// <summary>Renders distinctive Axiom Force squad operative silhouettes staged on the catwalk.</summary>
    private void DrawOperativeSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Renders Mirage Blade crouched on the catwalk railing, scanning the slag trenches.</summary>
    private void DrawMirageSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorMirage : ColorMirage * 0.75f;

        // Crouched agile figure
        Vector2[] stance = {
            new(pos.X - 10, pos.Y),
            new(pos.X - 12, pos.Y - 36),
            new(pos.X + 8, pos.Y - 36),
            new(pos.X + 12, pos.Y)
        };
        DrawColoredPolygon(stance, new Color("051a12"));
        DrawPolyline(stance, color, 2f);

        // Head and phantom cowl
        DrawCircle(new Vector2(pos.X - 2, pos.Y - 44), 7, new Color("051a12"));
        DrawLine(new Vector2(pos.X - 4, pos.Y - 44), new Vector2(pos.X + 3, pos.Y - 44), color, 2f);

        // Daggers sheathed at lower back reflecting forge glow
        DrawLine(new Vector2(pos.X - 14, pos.Y - 26), new Vector2(pos.X - 2, pos.Y - 18), color, 2f);
    }

    /// <summary>Renders Oceanus standing looking out, projecting protective hydro-vapor mist.</summary>
    private void DrawOceanusSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorOceanus : ColorOceanus * 0.75f;

        // Upright athletic silhouette
        Vector2[] body = {
            new(pos.X - 9, pos.Y),
            new(pos.X - 10, pos.Y - 54),
            new(pos.X + 10, pos.Y - 54),
            new(pos.X + 9, pos.Y)
        };
        DrawColoredPolygon(body, new Color("081726"));
        DrawPolyline(body, color, 2.5f);

        // Head and visor
        DrawCircle(new Vector2(pos.X, pos.Y - 62), 7, new Color("081726"));
        DrawLine(new Vector2(pos.X - 2, pos.Y - 62), new Vector2(pos.X + 5, pos.Y - 62), color, 2f);

        // Protective hydro-cellular vapor barrier around Oceanus
        float vaporPulse = 0.5f + 0.5f * Mathf.Sin(t * 2.2f);
        DrawArc(new Vector2(pos.X, pos.Y - 30), 28, 0, Mathf.Tau, 24, ColorOceanus * (0.35f * vaporPulse), 1.5f);
    }

    /// <summary>Renders Cyber Shogun Kaito with drawn katana reflecting the molten plasma core.</summary>
    private void DrawKaitoSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorKaito : ColorKaito * 0.75f;

        // Broad vanguard stance
        Vector2[] torso = {
            new(pos.X - 12, pos.Y),
            new(pos.X - 14, pos.Y - 58),
            new(pos.X + 14, pos.Y - 58),
            new(pos.X + 12, pos.Y)
        };
        DrawColoredPolygon(torso, new Color("1c0b0b"));
        DrawPolyline(torso, color, 2.5f);

        // Helmet crest
        DrawCircle(new Vector2(pos.X, pos.Y - 68), 8, new Color("1c0b0b"));
        DrawLine(new Vector2(pos.X - 4, pos.Y - 68), new Vector2(pos.X + 4, pos.Y - 68), color, 2f);
        DrawArc(new Vector2(pos.X, pos.Y - 76), 8, Mathf.Pi * 0.2f, Mathf.Pi * 0.8f, 16, color, 2.5f);

        // Drawn katana angled forward pointing toward the reactor core
        Vector2 hilt = new(pos.X + 12, pos.Y - 42);
        Vector2 tip = new(pos.X + 48, pos.Y - 68);
        DrawLine(hilt, tip, Colors.White, 2.5f);
        DrawLine(hilt, tip, ColorLavaGlow * 0.6f, 5f);
    }

    /// <summary>Renders Aura Phoenicia tuning into acoustic turbine resonance.</summary>
    private void DrawAuraSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorAura : ColorAura * 0.75f;

        // Slender figure
        Vector2[] body = {
            new(pos.X - 8, pos.Y),
            new(pos.X - 7, pos.Y - 52),
            new(pos.X + 7, pos.Y - 52),
            new(pos.X + 8, pos.Y)
        };
        DrawColoredPolygon(body, new Color("18081a"));
        DrawPolyline(body, color, 2f);

        // Head
        DrawCircle(new Vector2(pos.X, pos.Y - 60), 7, new Color("18081a"));
        DrawCircle(new Vector2(pos.X, pos.Y - 60), 8, color * 0.4f);

        // Prism wave rings visualizing the magnetic lattice dissonance
        for (int r = 1; r <= 2; r++)
        {
            float ringPhase = (t * 1.8f + r * 0.5f) % 1.0f;
            float rad = 12f + ringPhase * 40f;
            float alpha = (1f - ringPhase) * 0.4f;
            DrawArc(new Vector2(pos.X, pos.Y - 35), rad, 0, Mathf.Tau, 24, ColorAura * alpha, 1.5f);
        }
    }

    /// <summary>Renders Apex Titan standing as an immovable bulwark overlooking the forge.</summary>
    private void DrawTitanSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorTitan : ColorTitan * 0.75f;

        // Heavy massive torso
        Vector2[] torso = {
            new(pos.X - 18, pos.Y),
            new(pos.X - 22, pos.Y - 64),
            new(pos.X + 22, pos.Y - 64),
            new(pos.X + 18, pos.Y)
        };
        DrawColoredPolygon(torso, new Color("140921"));
        DrawPolyline(torso, color, 3f);

        // Heavy shoulder pauldrons
        DrawRect(new Rect2(pos.X - 28, pos.Y - 68, 12, 16), color);
        DrawRect(new Rect2(pos.X + 16, pos.Y - 68, 12, 16), color);

        // Armored helmet
        DrawRect(new Rect2(pos.X - 8, pos.Y - 80, 16, 16), new Color("140921"));
        DrawLine(new Vector2(pos.X - 6, pos.Y - 74), new Vector2(pos.X + 6, pos.Y - 74), color, 2f);

        // Both fists resting heavily on the catwalk railing
        DrawCircle(new Vector2(pos.X - 16, pos.Y - 58), 6, color);
        DrawCircle(new Vector2(pos.X + 16, pos.Y - 58), 6, color);
    }

    /// <summary>Renders holographic tactical display (reactor core schematic, shield uplink, temperature).</summary>
    private void DrawReactorCoreHologram(float t, int shot)
    {
        // Displayed prominently during Shots 1, 2, 6, 7
        if (shot != 1 && shot != 2 && shot != 6 && shot != 7) return;

        float alpha = Mathf.Clamp(Mathf.Sin(t * 3.0f) * 0.15f + 0.85f, 0.4f, 1.0f);
        Vector2 center = new(960, 320);

        Rect2 frame = new(center.X - 210, center.Y - 130, 420, 240);
        DrawRect(frame, new Color("0a0502") * (0.85f * alpha));
        DrawRect(frame, ColorLavaGlow * (0.6f * alpha), false, 2f);

        // Corner brackets
        float cLen = 16.0f;
        DrawLine(new Vector2(frame.Position.X, frame.Position.Y), new Vector2(frame.Position.X + cLen, frame.Position.Y), ColorLavaGlow, 3f);
        DrawLine(new Vector2(frame.Position.X, frame.Position.Y), new Vector2(frame.Position.X, frame.Position.Y + cLen), ColorLavaGlow, 3f);
        DrawLine(new Vector2(frame.End.X, frame.Position.Y), new Vector2(frame.End.X - cLen, frame.Position.Y), ColorLavaGlow, 3f);
        DrawLine(new Vector2(frame.End.X, frame.Position.Y), new Vector2(frame.End.X, frame.End.Y - cLen), ColorLavaGlow, 3f);

        if (shot == 6)
        {
            // Comms link with Surya Prime
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 30), "SURYA // TACTICAL RADIATION UPLINK", HorizontalAlignment.Left, -1, 14, ColorSurya);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 60), "SOLAR BULWARK SYNC: 100% ONLINE", HorizontalAlignment.Left, -1, 13, Colors.White);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 85), "REACTOR CORE TEMP: 4,120° KELVIN", HorizontalAlignment.Left, -1, 13, ColorLavaCore);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 110), "OVERLOAD WINDOW: 180 SECONDS CRITICAL", HorizontalAlignment.Left, -1, 13, ColorLavaGlow);

            // Circular temperature meter
            Vector2 meterCenter = new(center.X + 110, center.Y);
            DrawArc(meterCenter, 40, 0, Mathf.Tau, 32, ColorLavaCore * 0.4f, 4f);
            float tempAngle = Mathf.Pi * 1.5f;
            DrawArc(meterCenter, 40, 0, tempAngle, 32, ColorLavaGlow, 4f);
        }
        else
        {
            // Facility schematic & Citadel sky shield vectors
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 30), "FACILITY: THE FOUNDRY FORGE // SECTOR 7", HorizontalAlignment.Left, -1, 14, ColorCyan);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 60), "OBJECTIVE: SEVER COOLANT & OVERLOAD REACTOR", HorizontalAlignment.Left, -1, 13, Colors.White);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 85), "CITADEL SKY-SHIELD STATUS: 100% ACTIVE", HorizontalAlignment.Left, -1, 13, ColorLavaGlow);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 110), "ACT II CLIMAX: DROP THE SHIELDS", HorizontalAlignment.Left, -1, 13, ColorKaito);

            // Wireframe core schematic
            Vector2 wireCenter = new(center.X + 110, center.Y);
            DrawCircle(wireCenter, 28, ColorLavaCore * 0.3f);
            DrawArc(wireCenter, 28, 0, Mathf.Tau, 24, ColorLavaGlow * 0.8f, 2f);
            DrawLine(wireCenter, wireCenter + new Vector2(0, -50), ColorCyan, 2f);
            DrawString(ThemeDB.FallbackFont, wireCenter + new Vector2(-40, -55), "TO SKY SHIELDS", HorizontalAlignment.Left, -1, 11, ColorCyan);
        }
    }

    /// <summary>Draws cinema letterbox framing bars and CRT scanlines across the active frame.</summary>
    private void DrawScreenVignette(float fit, Vector2 margin, float t)
    {
        // 1. Top and bottom letterbox bars
        if (margin.Y > 0)
        {
            DrawRect(new Rect2(0, 0, Size.X, margin.Y), Colors.Black);
            DrawRect(new Rect2(0, Size.Y - margin.Y, Size.X, margin.Y), Colors.Black);
        }
        if (margin.X > 0)
        {
            DrawRect(new Rect2(0, 0, margin.X, Size.Y), Colors.Black);
            DrawRect(new Rect2(Size.X - margin.X, 0, margin.X, Size.Y), Colors.Black);
        }

        // 2. Subtle CRT scanlines across the active 1920x1080 display
        Rect2 activeRect = new(margin, new Vector2(1920f, 1080f) * fit);
        for (float y = activeRect.Position.Y; y < activeRect.End.Y; y += 4.0f)
        {
            DrawLine(new Vector2(activeRect.Position.X, y), new Vector2(activeRect.End.X, y), new Color(0, 0, 0, 0.08f), 1f);
        }
    }
}
