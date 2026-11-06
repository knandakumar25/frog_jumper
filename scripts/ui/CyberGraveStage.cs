using Godot;

/// <summary>
/// Seekable 2D cinematic tableau of the freight-lift descent into the subterranean Cyber-Grave Labs (Pre-Level 6).
/// Features parallax shaft girders, rushing hydraulic cables, bioluminescent nutrient conduits,
/// sweeping hazard warning beacons, an interactive holographic depth HUD, and operative squad silhouettes.
/// </summary>
public partial class CyberGraveStage : Control
{
    /// <summary>Asset root shared with the parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_grave_intro/";
    /// <summary>Timeline sample supplied by the overlay; motion never accumulates frame error.</summary>
    public float Seconds { get; set; }
    /// <summary>All procedural visual passes can render without requiring external textures, ensuring zero-missing-resource crashes.</summary>
    public bool ResourcesAvailable => CampaignArtwork.Available("cyber_grave", ArtPath);

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold
    private static readonly Color ColorAmber = new("ffaa00");    // Industrial Hazard
    private static readonly Color ColorBioGreen = new("00ff99"); // Bio-Fluid Nutrient
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan

    // Shot boundary schedule matching CyberGraveIntro (120s total timeline)
    private static readonly float[] ShotTimes = { 0f, 13.5f, 26.5f, 39.0f, 52.5f, 67.5f, 81.0f, 92.5f, 101.0f, 111.0f, 120.0f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 480),  // Shot 0: Wide descent shaft and holographic telemetry (AXIOM-7)
        new(820, 560),  // Shot 1: Kaito close vantage on the lift edge
        new(1180, 540), // Shot 2: Aura sensing the bio-resonance conduits
        new(720, 550),  // Shot 3: Oceanus looking into the subterranean abyss
        new(1350, 560), // Shot 4: Titan inspecting the heavy blast bulkheads
        new(1040, 540), // Shot 5: Mirage charting the service duct breach points
        new(580, 520),  // Shot 6: Surya monitoring orbital blind spots and uplink
        new(750, 550),  // Shot 7: Oceanus resolving to open every cage
        new(960, 540),  // Shot 8: Kaito rally with full squad silhouettes
        new(960, 520)   // Shot 9: Lift deceleration and arrival at the Cyber-Grave blast vault
    };

    private static readonly float[] CameraZooms = {
        1.05f, // Shot 0: Wide establishing
        1.18f, // Shot 1: Kaito
        1.20f, // Shot 2: Aura
        1.20f, // Shot 3: Oceanus
        1.18f, // Shot 4: Titan
        1.18f, // Shot 5: Mirage
        1.15f, // Shot 6: Surya
        1.22f, // Shot 7: Oceanus focus
        1.12f, // Shot 8: Squad rally
        1.02f  // Shot 9: Final arrival
    };

    /// <summary>Initializes stage properties with full-rect mouse filtering.</summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Composes the subterranean shaft, rushing tracks, bio conduits, lift platform, silhouettes and HUD analytically.</summary>
    public override void _Draw()
    {
        // 1. Deep bedrock shaft backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("03070d"));

        float t = Mathf.Clamp(Seconds, 0, 120);
        int shot = GetCurrentShot(t);

        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) * 0.5f;

        Vector2 camPos = GetSmoothCameraPosition(t, shot);
        camPos = CampaignArtwork.Focus(ArtPath, Seconds, camPos);
        float camZoom = GetSmoothCameraZoom(t, shot);

        // Apply letterboxed 1920x1080 canvas transformation
        DrawSetTransform(margin + (new Vector2(960, 540) - camPos * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // 2. Shaft architecture and rushing parallax struts
        DrawShaftBackground(t);
        CampaignArtwork.DrawEnvironment(this, "cyber_grave", Seconds, aftermath: false, drawBase: true);

        // 3. Bioluminescent chemical nutrient conduits and energy nodes
        DrawBioNutrientConduits(t);

        // 4. Sweeping amber hazard strobes and steam vents
        DrawHazardStrobesAndSteam(t);

        // 5. The industrial freight elevator platform
        DrawLiftPlatform(t);

        // 6. Squad operative silhouettes on the platform
        DrawOperativeSilhouettes(t, shot);

        // 7. Tactical holographic telemetry HUD
        DrawHolographicTelemetry(t);

        // Reset transform to screen space for atmospheric vignette and scanlines
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawScreenVignette(fit, margin, t);
    }

    /// <summary>Identifies the current active shot index based on the timeline position.</summary>
    private static int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t < ShotTimes[i + 1]) return i;
        }
        return ShotTimes.Length - 2;
    }

    /// <summary>Hermite cubic smoothstep for organic cinematic camera panning.</summary>
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float val = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return val * val * (3.0f - 2.0f * val);
    }

    /// <summary>Calculates interpolated camera focal point with smooth lead-in and lead-out transitions.</summary>
    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        if (shot >= CameraTargets.Length - 1) return CameraTargets[^1];
        float shotStart = ShotTimes[shot];
        float shotEnd = ShotTimes[shot + 1];
        float duration = shotEnd - shotStart;

        // 1.5s transition window at the start of each shot
        float transition = Mathf.Clamp((t - shotStart) / Mathf.Min(1.5f, duration * 0.4f), 0.0f, 1.0f);
        float ease = SmoothStep(0.0f, 1.0f, transition);

        Vector2 prevTarget = shot > 0 ? CameraTargets[shot - 1] : CameraTargets[0];
        Vector2 target = CameraTargets[shot];

        // Subtle continuous drifting camera breathing
        Vector2 drift = new(Mathf.Sin(t * 0.35f) * 12.0f, Mathf.Cos(t * 0.28f) * 8.0f);
        return prevTarget.Lerp(target, ease) + drift;
    }

    /// <summary>Calculates interpolated camera zoom with smooth transitions.</summary>
    private static float GetSmoothCameraZoom(float t, int shot)
    {
        if (shot >= CameraZooms.Length - 1) return CameraZooms[^1];
        float shotStart = ShotTimes[shot];
        float shotEnd = ShotTimes[shot + 1];
        float duration = shotEnd - shotStart;

        float transition = Mathf.Clamp((t - shotStart) / Mathf.Min(1.5f, duration * 0.4f), 0.0f, 1.0f);
        float ease = SmoothStep(0.0f, 1.0f, transition);

        float prevZoom = shot > 0 ? CameraZooms[shot - 1] : CameraZooms[0];
        float targetZoom = CameraZooms[shot];
        return Mathf.Lerp(prevZoom, targetZoom, ease);
    }

    /// <summary>Renders rushing shaft wall girders, tracks, and depth cables.</summary>
    private void DrawShaftBackground(float t)
    {
        // Shaft speed accelerates initially then decelerates in final shot
        float speed = t > 105.0f ? Mathf.Max(60.0f, 600.0f * (1.0f - (t - 105.0f) / 15.0f)) : 580.0f;
        float yOffset = Mathf.PosMod(t * speed, 240.0f);

        // Shaft side concrete walls
        DrawRect(new Rect2(0, 0, 260, 1080), new Color("050c17"));
        DrawRect(new Rect2(1660, 0, 260, 1080), new Color("050c17"));

        // Heavy vertical elevator rails
        DrawLine(new Vector2(260, 0), new Vector2(260, 1080), new Color("1a2a3a"), 8);
        DrawLine(new Vector2(1660, 0), new Vector2(1660, 1080), new Color("1a2a3a"), 8);
        DrawLine(new Vector2(275, 0), new Vector2(275, 1080), ColorCyan * 0.35f, 2);
        DrawLine(new Vector2(1645, 0), new Vector2(1645, 1080), ColorCyan * 0.35f, 2);

        // Rushing horizontal structural girders
        for (int y = -240; y < 1320; y += 240)
        {
            float drawY = y - yOffset;
            // Cross bracing
            DrawLine(new Vector2(0, drawY), new Vector2(260, drawY + 120), new Color(0.12f, 0.18f, 0.25f, 0.45f), 4);
            DrawLine(new Vector2(0, drawY + 120), new Vector2(260, drawY), new Color(0.12f, 0.18f, 0.25f, 0.45f), 4);
            DrawLine(new Vector2(1660, drawY), new Vector2(1920, drawY + 120), new Color(0.12f, 0.18f, 0.25f, 0.45f), 4);
            DrawLine(new Vector2(1660, drawY + 120), new Vector2(1920, drawY), new Color(0.12f, 0.18f, 0.25f, 0.45f), 4);

            // Girders behind the lift
            DrawRect(new Rect2(260, drawY + 40, 1400, 16), new Color(0.04f, 0.08f, 0.12f, 0.7f));
        }

        // Hydraulic suspension cables rushing vertically
        float cableOffset = Mathf.PosMod(t * speed * 1.4f, 160.0f);
        for (int c = 0; c < 4; c++)
        {
            float cx = 400 + c * 380;
            DrawLine(new Vector2(cx, 0), new Vector2(cx, 1080), new Color(0.2f, 0.25f, 0.3f, 0.5f), 3);
            for (int mark = -160; mark < 1240; mark += 160)
            {
                DrawCircle(new Vector2(cx, mark - cableOffset), 4, ColorCyan * 0.4f);
            }
        }
    }

    /// <summary>Draws pressurized vertical nutrient conduits glowing with bioluminescent chemical fluid.</summary>
    private void DrawBioNutrientConduits(float t)
    {
        float[] conduitX = { 320f, 1600f };
        for (int i = 0; i < conduitX.Length; i++)
        {
            float x = conduitX[i];
            // Outer glass sleeve
            DrawRect(new Rect2(x - 22, 0, 44, 1080), new Color(0.02f, 0.1f, 0.08f, 0.6f));
            DrawLine(new Vector2(x - 22, 0), new Vector2(x - 22, 1080), ColorBioGreen * 0.4f, 2);
            DrawLine(new Vector2(x + 22, 0), new Vector2(x + 22, 1080), ColorBioGreen * 0.4f, 2);

            // Glowing inner bio fluid with pulsing core
            float pulse = 0.5f + 0.3f * Mathf.Sin(t * 2.5f + i * 1.5f);
            DrawRect(new Rect2(x - 14, 0, 28, 1080), ColorBioGreen * (0.25f + pulse * 0.2f));
            DrawLine(new Vector2(x, 0), new Vector2(x, 1080), new Color(1, 1, 1, 0.6f + pulse * 0.3f), 4);

            // Rising bio bubbles inside the conduit
            for (int b = 0; b < 10; b++)
            {
                float by = Mathf.PosMod(1080 - (t * 80.0f + b * 110.0f), 1080.0f);
                float bx = x + Mathf.Sin(t * 3.0f + b) * 8.0f;
                DrawCircle(new Vector2(bx, by), 3.5f, new Color(0.8f, 1f, 0.9f, 0.7f));
            }

            // High-voltage containment ring clamps along the tube
            for (int ring = 100; ring < 1080; ring += 200)
            {
                DrawRect(new Rect2(x - 30, ring, 60, 18), new Color("1e2a38"));
                DrawLine(new Vector2(x - 30, ring + 9), new Vector2(x + 30, ring + 9), ColorCyan * 0.7f, 2);
            }
        }
    }

    /// <summary>Draws sweeping amber emergency beacons and intermittent steam valve releases.</summary>
    private void DrawHazardStrobesAndSteam(float t)
    {
        // 1. Rotating amber hazard beacons on the shaft walls
        Vector2[] beaconPositions = { new(240, 220), new(240, 780), new(1680, 220), new(1680, 780) };
        for (int i = 0; i < beaconPositions.Length; i++)
        {
            Vector2 pos = beaconPositions[i];
            float angle = t * 3.2f + i * Mathf.Pi * 0.5f;
            DrawCircle(pos, 10, ColorAmber);

            // Rotating volumetric light cone
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 leftEdge = new(Mathf.Cos(angle - 0.35f), Mathf.Sin(angle - 0.35f));
            Vector2 rightEdge = new(Mathf.Cos(angle + 0.35f), Mathf.Sin(angle + 0.35f));

            Vector2 p1 = pos;
            Vector2 p2 = pos + leftEdge * 460.0f;
            Vector2 p3 = pos + rightEdge * 460.0f;

            Color coneColor = new(ColorAmber.R, ColorAmber.G, ColorAmber.B, 0.08f + 0.04f * Mathf.Sin(t * 4.0f));
            DrawColoredPolygon(new Vector2[] { p1, p2, p3 }, coneColor);
        }

        // 2. High pressure steam venting across the shaft
        for (int v = 0; v < 3; v++)
        {
            float ventY = 320 + v * 280;
            float steamPhase = Mathf.PosMod(t * 1.5f + v * 0.9f, 4.0f);
            if (steamPhase < 1.4f)
            {
                float progress = steamPhase / 1.4f;
                float steamLength = Mathf.Lerp(0f, 480f, Mathf.Sin(progress * Mathf.Pi));
                float alpha = (1.0f - progress) * 0.35f;
                Vector2 start = v % 2 == 0 ? new(260, ventY) : new(1660, ventY);
                Vector2 end = v % 2 == 0 ? new(260 + steamLength, ventY + 30) : new(1660 - steamLength, ventY + 30);
                DrawLine(start, end, new Color(0.9f, 0.95f, 1f, alpha), 18);
                DrawLine(start, end + new Vector2(0, -6), new Color(1, 1, 1, alpha * 0.8f), 8);
            }
        }
    }

    /// <summary>Draws the heavy steel freight elevator platform carrying the squad.</summary>
    private void DrawLiftPlatform(float t)
    {
        // Platform vertical position with slight mechanical vibration
        float rumble = Mathf.Sin(t * 26.0f) * 1.5f;
        float platY = 720.0f + rumble;

        // Platform underside support trusses
        Vector2[] truss = {
            new(360, platY + 140), new(540, platY + 28),
            new(960, platY + 160), new(1380, platY + 28),
            new(1560, platY + 140)
        };
        for (int i = 0; i < truss.Length - 1; i++)
        {
            DrawLine(truss[i], truss[i + 1], new Color("0c1520"), 8);
            DrawLine(truss[i], new Vector2(truss[i].X, platY + 28), new Color("0c1520"), 6);
        }

        // Main platform steel deck
        DrawRect(new Rect2(360, platY, 1200, 36), new Color("14202c"));
        DrawLine(new Vector2(360, platY), new Vector2(1560, platY), ColorCyan * 0.8f, 3);

        // Industrial hazard stripes along the platform edge
        for (int x = 360; x < 1560; x += 36)
        {
            DrawLine(new Vector2(x, platY + 2), new Vector2(x + 18, platY + 34), ColorAmber * 0.7f, 4);
        }

        // Safety railings
        DrawLine(new Vector2(360, platY - 44), new Vector2(1560, platY - 44), new Color("253545"), 4);
        DrawLine(new Vector2(360, platY - 80), new Vector2(1560, platY - 80), new Color("253545"), 4);
        for (int post = 380; post <= 1540; post += 145)
        {
            DrawLine(new Vector2(post, platY), new Vector2(post, platY - 80), new Color("253545"), 5);
        }

        // Under-floor neon uplight casting upward rim glow on operatives
        DrawRect(new Rect2(420, platY - 4, 1080, 8), new Color(ColorCyan.R, ColorCyan.G, ColorCyan.B, 0.4f + 0.15f * Mathf.Sin(t * 3.0f)));
    }

    /// <summary>Draws stylized squad operative silhouettes positioned across the elevator platform.</summary>
    private void DrawOperativeSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Draws Kaito's silhouette with katana blade gleam and signature crimson scarf glow.</summary>
    private void DrawKaitoSilhouette(Vector2 pos, float t, bool isSpeaker)
    {
        Color highlight = isSpeaker ? ColorKaito : ColorKaito * 0.5f;
        // Body silhouette
        DrawRect(new Rect2(pos.X - 16, pos.Y - 110, 32, 110), new Color("0a1018"));
        // Head / Samurai helm silhouette
        DrawCircle(new Vector2(pos.X, pos.Y - 124), 16, new Color("0a1018"));
        // Helm crest
        DrawLine(new Vector2(pos.X - 14, pos.Y - 134), new Vector2(pos.X + 14, pos.Y - 134), highlight, 3);
        // Katana on back / shoulder
        Vector2 hilt = new(pos.X - 22, pos.Y - 140);
        Vector2 tip = new(pos.X + 26, pos.Y - 40);
        DrawLine(hilt, tip, new Color("1e2c38"), 4);
        DrawLine(hilt, hilt + (tip - hilt) * 0.85f, highlight, 2);
        // Scarf wisps drifting in descent wind
        for (int i = 0; i < 3; i++)
        {
            float wind = Mathf.Sin(t * 8.0f + i) * 14.0f;
            DrawLine(new Vector2(pos.X - 10, pos.Y - 108), new Vector2(pos.X - 35 - i * 10, pos.Y - 120 + wind), highlight * 0.7f, 3);
        }
    }

    /// <summary>Draws Oceanus's silhouette with hydro catalyst rings and flowing cape.</summary>
    private void DrawOceanusSilhouette(Vector2 pos, float t, bool isSpeaker)
    {
        Color highlight = isSpeaker ? ColorOceanus : ColorOceanus * 0.5f;
        DrawRect(new Rect2(pos.X - 14, pos.Y - 100, 28, 100), new Color("081018"));
        DrawCircle(new Vector2(pos.X, pos.Y - 114), 14, new Color("081018"));
        // Hydro catalyst orbs floating around shoulders
        for (int i = 0; i < 2; i++)
        {
            float orbAngle = t * 2.2f + i * Mathf.Pi;
            Vector2 orbPos = new(pos.X + Mathf.Cos(orbAngle) * 26.0f, pos.Y - 80 + Mathf.Sin(orbAngle) * 10.0f);
            DrawCircle(orbPos, 6, highlight);
            DrawCircle(orbPos, 3, Colors.White);
        }
    }

    /// <summary>Draws Aura Phoenicia's silhouette with ultrasonic prism emitter antennae.</summary>
    private void DrawAuraSilhouette(Vector2 pos, float t, bool isSpeaker)
    {
        Color highlight = isSpeaker ? ColorAura : ColorAura * 0.5f;
        DrawRect(new Rect2(pos.X - 12, pos.Y - 96, 24, 96), new Color("0c0a16"));
        DrawCircle(new Vector2(pos.X, pos.Y - 110), 13, new Color("0c0a16"));
        // Prism tuning fork antennae on shoulders
        DrawLine(new Vector2(pos.X - 12, pos.Y - 90), new Vector2(pos.X - 22, pos.Y - 125), highlight, 2);
        DrawLine(new Vector2(pos.X + 12, pos.Y - 90), new Vector2(pos.X + 22, pos.Y - 125), highlight, 2);
        // Ultrasonic frequency arc pulses
        if (isSpeaker)
        {
            for (int r = 1; r <= 3; r++)
            {
                float arcRad = 16.0f + r * 14.0f;
                DrawArc(new Vector2(pos.X, pos.Y - 110), arcRad, -Mathf.Pi * 0.8f, -Mathf.Pi * 0.2f, 16, highlight * (0.6f / r), 2);
            }
        }
    }

    /// <summary>Draws Mirage Blade's low crouching infiltration silhouette with phase daggers.</summary>
    private void DrawMirageSilhouette(Vector2 pos, float t, bool isSpeaker)
    {
        Color highlight = isSpeaker ? ColorMirage : ColorMirage * 0.5f;
        // Crouching posture
        DrawRect(new Rect2(pos.X - 18, pos.Y - 74, 36, 74), new Color("06120e"));
        DrawCircle(new Vector2(pos.X, pos.Y - 86), 13, new Color("06120e"));
        // Twin phase daggers gleaming
        DrawLine(new Vector2(pos.X - 16, pos.Y - 50), new Vector2(pos.X - 32, pos.Y - 20), highlight, 3);
        DrawLine(new Vector2(pos.X + 16, pos.Y - 50), new Vector2(pos.X + 32, pos.Y - 20), highlight, 3);
        // Holographic wrist display map
        if (isSpeaker)
        {
            DrawCircle(new Vector2(pos.X - 20, pos.Y - 45), 7, highlight * 0.8f);
            DrawRect(new Rect2(pos.X - 45, pos.Y - 80, 50, 30), new Color(ColorMirage.R, ColorMirage.G, ColorMirage.B, 0.25f));
            DrawRect(new Rect2(pos.X - 45, pos.Y - 80, 50, 30), highlight, false, 1);
        }
    }

    /// <summary>Draws Apex Titan's massive kinetic armored silhouette.</summary>
    private void DrawTitanSilhouette(Vector2 pos, float t, bool isSpeaker)
    {
        Color highlight = isSpeaker ? ColorTitan : ColorTitan * 0.5f;
        // Heavy broad shoulder plates
        DrawRect(new Rect2(pos.X - 34, pos.Y - 130, 68, 130), new Color("0c0c16"));
        DrawRect(new Rect2(pos.X - 46, pos.Y - 120, 92, 36), new Color("141424"));
        DrawCircle(new Vector2(pos.X, pos.Y - 142), 18, new Color("0c0c16"));
        // Kinetic shield battery capacitors
        DrawLine(new Vector2(pos.X - 38, pos.Y - 110), new Vector2(pos.X - 38, pos.Y - 40), highlight, 4);
        DrawLine(new Vector2(pos.X + 38, pos.Y - 110), new Vector2(pos.X + 38, pos.Y - 40), highlight, 4);
    }

    /// <summary>Draws Surya Prime's silhouette with orbital uplink beacon.</summary>
    private void DrawSuryaSilhouette(Vector2 pos, float t, bool isSpeaker)
    {
        Color highlight = isSpeaker ? ColorSurya : ColorSurya * 0.5f;
        DrawRect(new Rect2(pos.X - 16, pos.Y - 105, 32, 105), new Color("141006"));
        DrawCircle(new Vector2(pos.X, pos.Y - 118), 14, new Color("141006"));
        // Solar transmitter staff / antenna
        Vector2 antennaBase = new(pos.X - 18, pos.Y);
        Vector2 antennaTip = new(pos.X - 22, pos.Y - 160);
        DrawLine(antennaBase, antennaTip, new Color("2a2414"), 3);
        DrawCircle(antennaTip, 5, highlight);
        if (isSpeaker)
        {
            float pulse = Mathf.Sin(t * 6.0f) * 8.0f;
            DrawCircle(antennaTip, 8 + pulse, new Color(ColorSurya.R, ColorSurya.G, ColorSurya.B, 0.4f));
        }
    }

    /// <summary>Renders holographic tactical telemetry projected above the central lift console.</summary>
    private void DrawHolographicTelemetry(float t)
    {
        Vector2 hudPos = new(960, 480);
        float alpha = 0.85f + 0.15f * Mathf.Sin(t * 3.5f);

        // Holographic console projector beam from platform floor
        DrawColoredPolygon(new Vector2[] {
            new(940, 720), new(980, 720),
            new(1180, 420), new(740, 420)
        }, new Color(ColorCyan.R, ColorCyan.G, ColorCyan.B, 0.05f));

        // Subterranean Depth Counter
        float depth = Mathf.Lerp(45.0f, 412.0f, Mathf.Clamp(t / 110.0f, 0.0f, 1.0f));
        string depthStr = $"DEPTH: -{depth:F0} METERS // SECTOR 06";

        // Main holographic schematic box
        Rect2 hudBox = new(hudPos.X - 200, hudPos.Y - 80, 400, 160);
        DrawRect(hudBox, new Color(0.01f, 0.04f, 0.08f, 0.75f));
        DrawRect(hudBox, ColorCyan * (0.6f * alpha), false, 2);

        // Corner framing brackets
        float cLen = 16.0f;
        DrawLine(new Vector2(hudBox.Position.X, hudBox.Position.Y), new Vector2(hudBox.Position.X + cLen, hudBox.Position.Y), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.Position.X, hudBox.Position.Y), new Vector2(hudBox.Position.X, hudBox.Position.Y + cLen), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.End.X, hudBox.Position.Y), new Vector2(hudBox.End.X - cLen, hudBox.Position.Y), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.End.X, hudBox.Position.Y), new Vector2(hudBox.End.X, hudBox.Position.Y + cLen), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.Position.X, hudBox.End.Y), new Vector2(hudBox.Position.X + cLen, hudBox.End.Y), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.Position.X, hudBox.End.Y), new Vector2(hudBox.Position.X, hudBox.End.Y - cLen), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.End.X, hudBox.End.Y), new Vector2(hudBox.End.X - cLen, hudBox.End.Y), ColorCyan, 3);
        DrawLine(new Vector2(hudBox.End.X, hudBox.End.Y), new Vector2(hudBox.End.X, hudBox.End.Y - cLen), ColorCyan, 3);

        // Wireframe schematic of Cyber-Grave Labs
        Vector2 labCenter = new(hudPos.X, hudPos.Y - 10);
        DrawCircle(labCenter, 32, ColorBioGreen * (0.4f * alpha));
        DrawCircle(labCenter, 20, ColorBioGreen * (0.7f * alpha));
        for (int p = 0; p < 6; p++)
        {
            float ang = p * Mathf.Pi / 3.0f + t * 0.5f;
            Vector2 podPos = labCenter + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 48.0f;
            DrawCircle(podPos, 6, ColorCyan * 0.8f);
            DrawLine(labCenter, podPos, ColorCyan * 0.4f, 1);
        }

        // Subterranean facility title label
        DrawString(ThemeDB.FallbackFont, new Vector2(hudPos.X - 180, hudPos.Y - 55),
            WildfireText.Get("UI_CYBERGRAVESTAGE_DRAWHOLOGRAPHICTELEMETRY_0B228379", "FACILITY: CYBER-GRAVE // LABS"), HorizontalAlignment.Left, -1, 14, ColorCyan);
        DrawString(ThemeDB.FallbackFont, new Vector2(hudPos.X - 180, hudPos.Y + 60),
            depthStr, HorizontalAlignment.Left, -1, 16, ColorAmber);
    }

    /// <summary>Draws screen-space cinema letterbox bars, chromatic vignette and scanlines.</summary>
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

        // 2. Subtle CRT scanlines across the active frame
        Rect2 activeRect = new(margin, new Vector2(1920, 1080) * fit);
        for (float y = activeRect.Position.Y; y < activeRect.End.Y; y += 4.0f)
        {
            DrawLine(new Vector2(activeRect.Position.X, y), new Vector2(activeRect.End.X, y), new Color(0, 0, 0, 0.08f), 1);
        }
    }
}
