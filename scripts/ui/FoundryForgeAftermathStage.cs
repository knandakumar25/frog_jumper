using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 7 post-mission epilogue ("The Sky-Shield Falls // Act II Finale").
/// Composes the extraction transport cresting the volcanic caldera rim, the buckling geothermal rift below,
/// the Neo-Kyoto skyline with the monolithic Citadel Spire, the catastrophic collapse of the planetary hexagonal sky-shield,
/// operative character silhouettes, holographic AXIOM-7 telemetry, and smooth camera choreography across nine authored beats.
/// </summary>
public partial class FoundryForgeAftermathStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/foundry_forge_aftermath/";

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
    private static readonly Color ColorAmber = new("fbbf24");    // Hazard Amber
    private static readonly Color ColorLavaCore = new("ff4400"); // Incandescent Magma Red
    private static readonly Color ColorLavaMid = new("f97316");  // Molten Plasma Orange
    private static readonly Color ColorLavaGlow = new("facc15"); // Superheated Yellow Glow
    private static readonly Color ColorBasalt = new("0c0604");   // Volcanic Basalt Rock
    private static readonly Color ColorSkyShield = new("38bdf8"); // Sky-Shield Blue
    private static readonly Color ColorSpire = new("1e1e2f");    // Citadel Spire Dark Metal

    // Shot boundary schedule matching FoundryForgeAftermath dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 8f, 18f, 28f, 38f, 48f, 58f, 68f, 78f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - transport cresting caldera rim
        new(1380, 560), // Shot 1: Titan looking back at buckling volcanic rift
        new(620, 580),  // Shot 2: Oceanus observing flooded flues & steam eruptions
        new(1160, 540), // Shot 3: Aura listening to ultrasonic dissonance in the sky
        new(960, 420),  // Shot 4: Mirage watching hexagonal sky-shield fracture
        new(960, 440),  // Shot 5: Surya comms & global sky-shield grid collapse
        new(880, 500),  // Shot 6: Kaito facing the newly unshielded Citadel Spire
        new(960, 360),  // Shot 7: Upward pan to high clouds & Stratos Docks vector
        new(960, 520)   // Shot 8: Epic wide squad tableau; Act II Complete
    };

    private static readonly float[] CameraZooms = {
        1.02f, // Shot 0
        1.18f, // Shot 1
        1.18f, // Shot 2
        1.18f, // Shot 3
        1.14f, // Shot 4
        1.16f, // Shot 5
        1.12f, // Shot 6
        1.20f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes volcanic caldera rim, exploding rift vents, city skyline, Citadel Spire,
    /// fracturing sky-shield, operative silhouettes, holographic telemetry, and cinema letterbox analytically.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep night sky backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("04050a"));

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

        // Volcanic caldera rim backdrop and distant storm clouds
        DrawCalderaAndAtmosphere(t);
        CampaignArtwork.DrawEnvironment(this, "foundry_forge", Seconds, aftermath: true, drawBase: true);

        // Distant Neo-Kyoto metropolis skyline with the Citadel Spire
        DrawCitySkylineAndSpire(t);

        // The climactic Sky-Shield Collapse VFX (fracturing, arcing, and exploding)
        DrawSkyShieldCollapseAnimation(t);

        // Exploding subterranean rift vents behind the caldera rim
        DrawRiftExplosionsAndSteam(t);

        // Foreground caldera ridge and extraction hover-transport gantry
        DrawExtractionGantryAndTransport(t);

        // Axiom Force operative silhouettes staged on the platform
        DrawOperativeSilhouettes(t, shot);

        // 3. Screen-space UI overlay layer
        DrawSetTransform(margin, 0, Vector2.One * fit);

        // AXIOM-7 holographic tactical telemetry (status, shield drop %, vector)
        DrawHolographicTelemetry(t, shot);

        // 4. Anamorphic cinema letterboxing and CRT scanlines
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawCinemaBars(fit, margin);
        DrawCrtScanlines();
    }

    /// <summary>Finds the active shot index for timestamp t.</summary>
    private static int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t >= ShotTimes[i] && t < ShotTimes[i + 1])
                return i;
        }
        return ShotTimes.Length - 2;
    }

    /// <summary>Interpolates camera focal position with smooth Hermite easing.</summary>
    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        float start = ShotTimes[shot];
        float end = ShotTimes[shot + 1];
        float progress = Mathf.Clamp((t - start) / (end - start), 0f, 1f);
        float eased = Mathf.SmoothStep(0f, 1f, progress);

        Vector2 current = CameraTargets[shot];
        Vector2 target = (shot < CameraTargets.Length - 1) ? CameraTargets[shot + 1] : current;

        // Subtle pan within the shot plus smooth transition near boundaries
        Vector2 intraShotDrift = new(Mathf.Sin(t * 0.15f) * 12f, Mathf.Cos(t * 0.12f) * 8f);
        return current.Lerp(target, eased * 0.35f) + intraShotDrift;
    }

    /// <summary>Interpolates camera zoom factor with smooth easing.</summary>
    private static float GetSmoothCameraZoom(float t, int shot)
    {
        float start = ShotTimes[shot];
        float end = ShotTimes[shot + 1];
        float progress = Mathf.Clamp((t - start) / (end - start), 0f, 1f);
        float eased = Mathf.SmoothStep(0f, 1f, progress);

        float current = CameraZooms[shot];
        float target = (shot < CameraZooms.Length - 1) ? CameraZooms[shot + 1] : current;
        return Mathf.Lerp(current, target, eased * 0.3f);
    }

    /// <summary>Renders dark storm clouds, volcanic glow on horizon, and mountain ridges.</summary>
    private void DrawCalderaAndAtmosphere(float t)
    {
        // Gradient sky with stormy horizon
        for (int y = 0; y < 600; y += 40)
        {
            float ratio = y / 600f;
            Color skyColor = new Color("050914").Lerp(new Color("1e0b06"), ratio * 0.7f);
            DrawRect(new Rect2(0, y, 1920, 40), skyColor);
        }

        // Distant jagged volcanic caldera mountain silhouette
        Vector2[] calderaRidge = {
            new(0, 700),
            new(240, 620),
            new(480, 660),
            new(720, 580),
            new(960, 640),
            new(1200, 590),
            new(1440, 650),
            new(1680, 610),
            new(1920, 680),
            new(1920, 1080),
            new(0, 1080)
        };
        DrawColoredPolygon(calderaRidge, new Color("0a0505"));
    }

    /// <summary>Renders distant Neo-Kyoto skyscraper silhouettes and the monolithic Citadel Spire.</summary>
    private void DrawCitySkylineAndSpire(float t)
    {
        // Skyscraper silhouettes in background
        float[] bldXs = { 180f, 320f, 460f, 600f, 740f, 1180f, 1320f, 1460f, 1600f, 1740f };
        float[] bldHeights = { 280f, 340f, 300f, 380f, 320f, 350f, 310f, 390f, 290f, 330f };

        for (int i = 0; i < bldXs.Length; i++)
        {
            float bx = bldXs[i];
            float bh = bldHeights[i];
            DrawRect(new Rect2(bx, 620 - bh, 110, bh + 100), new Color("070a14"));

            // Grid of distant neon windows
            for (float wy = 620 - bh + 20; wy < 600; wy += 24)
            {
                for (float wx = bx + 12; wx < bx + 98; wx += 20)
                {
                    float winAlpha = (Mathf.Sin(wx * 3.1f + wy * 1.7f + t * 0.5f) > 0.3f) ? 0.35f : 0.08f;
                    DrawRect(new Rect2(wx, wy, 8, 12), new Color(ColorCyan, winAlpha));
                }
            }
        }

        // Center Monolithic Citadel High Command Spire (dominant structure)
        Vector2[] spirePoly = {
            new(960, 140),   // Spire tip apex
            new(940, 260),
            new(920, 420),
            new(880, 620),   // Base west
            new(1040, 620),  // Base east
            new(1000, 420),
            new(980, 260)
        };
        DrawColoredPolygon(spirePoly, ColorSpire);

        // Spire glowing vertical neon data spine
        DrawLine(new Vector2(960, 150), new Vector2(960, 620), new Color("ff2a5f", 0.75f), 3f);

        // Stratos high-altitude flight docks ring near spire apex
        DrawArc(new Vector2(960, 260), 65f, 0f, Mathf.Tau, 32, new Color(ColorCyan, 0.6f), 2.5f);
        DrawArc(new Vector2(960, 360), 95f, 0f, Mathf.Tau, 32, new Color(ColorAmber, 0.5f), 2f);

        // High-altitude cloud layers shrouding Spire docks
        for (int c = 0; c < 5; c++)
        {
            float cx = 600f + c * 200f + Mathf.Sin(t * 0.2f + c) * 30f;
            float cy = 220f + (c % 2) * 40f;
            DrawCircle(new Vector2(cx, cy), 50f + c * 8f, new Color("141c2e", 0.35f));
        }
    }

    /// <summary>
    /// Renders the dramatic collapse of the planetary hexagonal sky-shield:
    /// Active lattice (0-38s), fracture and electric arcing (38-48s), catastrophic shatter explosion (48-58s),
    /// fading embers and completely open sky (58-90s).
    /// </summary>
    private void DrawSkyShieldCollapseAnimation(float t)
    {
        // The shield dome exists while t < 58.0s; thereafter it has completely collapsed.
        if (t >= 58.0f)
        {
            // Falling residual prismatic embers
            float emberT = t - 58.0f;
            if (emberT < 14.0f)
            {
                float emberFade = 1f - (emberT / 14.0f);
                for (int e = 0; e < 40; e++)
                {
                    float ex = 200f + ((e * 47) % 1520);
                    float ey = (e * 23 + emberT * 80f) % 700f;
                    DrawCircle(new Vector2(ex, ey), 2.5f, new Color(ColorSkyShield, emberFade * 0.7f));
                }
            }
            return;
        }

        Vector2 domeCenter = new(960, 680);
        float domeRadius = 760f;

        // Base dome energy field opacity
        float shieldAlpha = 0.45f;
        if (t >= 38.0f && t < 48.0f)
        {
            // Rapid flickering during fracture
            shieldAlpha = 0.45f + Mathf.Sin(t * 30f) * 0.35f;
        }
        else if (t >= 48.0f && t < 58.0f)
        {
            // Explosive surge and fade
            float collapseProgress = (t - 48.0f) / 10.0f;
            shieldAlpha = Mathf.Max(0f, (1f - collapseProgress) * (0.8f + Mathf.Sin(t * 50f) * 0.4f));
        }

        // Draw primary curved energy perimeter
        DrawArc(domeCenter, domeRadius, Mathf.Pi, Mathf.Tau, 64, new Color(ColorSkyShield, shieldAlpha), 4f);
        DrawArc(domeCenter, domeRadius - 20f, Mathf.Pi, Mathf.Tau, 48, new Color(ColorCyan, shieldAlpha * 0.5f), 1.5f);

        // Draw hexagonal lattice ribs
        for (float angle = Mathf.Pi + 0.15f; angle < Mathf.Tau - 0.15f; angle += 0.22f)
        {
            Vector2 p1 = domeCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * domeRadius;
            Vector2 p0 = domeCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (domeRadius * 0.4f);
            DrawLine(p0, p1, new Color(ColorSkyShield, shieldAlpha * 0.35f), 1.5f);
        }

        // Expanding electric fracture branches (Shots 4 and 5)
        if (t >= 38.0f)
        {
            float fractureSeverity = Mathf.Clamp((t - 38.0f) / 10.0f, 0f, 1f);
            int branches = 6 + (int)(fractureSeverity * 12);

            for (int b = 0; b < branches; b++)
            {
                float seed = b * 13.7f;
                float startAng = Mathf.Pi + 0.3f + ((b * 0.45f) % (Mathf.Pi - 0.6f));
                Vector2 root = domeCenter + new Vector2(Mathf.Cos(startAng), Mathf.Sin(startAng)) * domeRadius;

                Vector2 cur = root;
                for (int seg = 0; seg < 6; seg++)
                {
                    float jitterX = Mathf.Sin(t * 25f + seed + seg) * 28f;
                    float jitterY = Mathf.Cos(t * 22f + seed + seg) * 24f;
                    Vector2 next = cur + new Vector2(-40f + jitterX, 35f + jitterY);
                    DrawLine(cur, next, new Color("ffffff", shieldAlpha), 2.5f);
                    DrawLine(cur, next, new Color(ColorSkyShield, shieldAlpha * 0.7f), 6f);
                    cur = next;
                }
            }

            // Expanding shockwave ring at 48.0s
            if (t >= 48.0f)
            {
                float shockT = (t - 48.0f) * 120f;
                DrawArc(new Vector2(960, 320), shockT, 0f, Mathf.Tau, 48, new Color("ffffff", shieldAlpha), 3.5f);
                DrawArc(new Vector2(960, 320), shockT * 0.8f, 0f, Mathf.Tau, 36, new Color(ColorCyan, shieldAlpha * 0.5f), 2f);
            }
        }
    }

    /// <summary>Renders geothermal steam geysers and bubbling red magma vents in the rift below.</summary>
    private void DrawRiftExplosionsAndSteam(float t)
    {
        // Glowing magma cracks along the bottom ridge
        for (int i = 0; i < 7; i++)
        {
            float vx = 160f + i * 260f;
            float vy = 680f + (i % 3) * 20f;
            float pulse = 0.5f + Mathf.Sin(t * 3f + i) * 0.5f;

            // Magma pool glow
            DrawCircle(new Vector2(vx, vy), 35f, new Color(ColorLavaMid, 0.4f * pulse));
            DrawCircle(new Vector2(vx, vy), 18f, new Color(ColorLavaGlow, 0.6f * pulse));

            // Rising billowing steam plumes
            float steamH = 120f + Mathf.Sin(t * 2f + i) * 30f;
            DrawLine(new Vector2(vx, vy), new Vector2(vx + Mathf.Sin(t + i) * 25f, vy - steamH), new Color("ffeedd", 0.25f), 18f);
        }
    }

    /// <summary>Renders the heavy extraction hover-transport gantry platform.</summary>
    private void DrawExtractionGantryAndTransport(float t)
    {
        // Heavy steel cantilever overlook ridge
        Vector2[] gantryBase = {
            new(300, 720),
            new(1620, 720),
            new(1540, 800),
            new(380, 800)
        };
        DrawColoredPolygon(gantryBase, new Color("141824"));
        DrawLine(new Vector2(300, 720), new Vector2(1620, 720), new Color(ColorCyan, 0.8f), 3f);

        // Yellow-black industrial hazard edge striping
        for (float hx = 320; hx < 1600; hx += 40)
        {
            DrawLine(new Vector2(hx, 720), new Vector2(hx + 20, 740), ColorAmber, 3f);
        }

        // Hover transport landing struts and safety railings
        DrawLine(new Vector2(400, 680), new Vector2(1520, 680), new Color("475569"), 2.5f);
        for (float rx = 440; rx < 1520; rx += 120)
        {
            DrawLine(new Vector2(rx, 720), new Vector2(rx, 680), new Color("475569"), 2f);
        }

        // Flashing amber safety beacons at platform corners
        float beaconPulse = (Mathf.Sin(t * 6f) > 0f) ? 1f : 0.2f;
        DrawCircle(new Vector2(320, 715), 6f, new Color(ColorAmber, beaconPulse));
        DrawCircle(new Vector2(1600, 715), 6f, new Color(ColorAmber, beaconPulse));
    }

    /// <summary>Renders all 6 Axiom Force squad members staged on the gantry overlooking the skyline.</summary>
    private void DrawOperativeSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Draws a styled operative silhouette with glowing cyber energy highlights.</summary>
    private void DrawCharacterSilhouette(Vector2 pos, Color glow, string name, bool isSpeaking, bool isLeader = false, bool isHeavy = false)
    {
        float scale = isHeavy ? 1.25f : (isLeader ? 1.1f : 1.0f);
        Color bodyColor = new("080a10");

        // Active speaker ground highlight ring
        if (isSpeaking)
        {
            DrawArc(pos + new Vector2(0, 50), 32f * scale, 0f, Mathf.Tau, 24, new Color(glow, 0.7f), 2f);
        }

        // Head and cyber visor / crest
        DrawCircle(pos + new Vector2(0, -18 * scale), 12f * scale, bodyColor);
        DrawLine(pos + new Vector2(-6 * scale, -18 * scale), pos + new Vector2(8 * scale, -18 * scale), glow, 2.5f);

        // Torso
        DrawRect(new Rect2(pos.X - 10 * scale, pos.Y - 6 * scale, 20 * scale, 32 * scale), bodyColor);

        // Signature equipment accents
        if (isLeader)
        {
            // Katana scabbard and flowing crimson scarf
            DrawLine(pos + new Vector2(-12 * scale, 10 * scale), pos + new Vector2(-28 * scale, -15 * scale), glow, 2.5f);
            DrawLine(pos + new Vector2(-6 * scale, -12 * scale), pos + new Vector2(-20 * scale, -2 * scale), glow, 2f);
        }
        else if (isHeavy)
        {
            // Massive shoulder pauldrons and kinetic coil hum
            DrawRect(new Rect2(pos.X - 18 * scale, pos.Y - 8 * scale, 8 * scale, 14 * scale), glow);
            DrawRect(new Rect2(pos.X + 10 * scale, pos.Y - 8 * scale, 8 * scale, 14 * scale), glow);
        }

        // Legs
        DrawLine(pos + new Vector2(-6 * scale, 26 * scale), pos + new Vector2(-8 * scale, 50 * scale), bodyColor, 5f * scale);
        DrawLine(pos + new Vector2(6 * scale, 26 * scale), pos + new Vector2(8 * scale, 50 * scale), bodyColor, 5f * scale);
    }

    /// <summary>Renders AXIOM-7 holographic city map and sky-shield countdown telemetry.</summary>
    private void DrawHolographicTelemetry(float t, int shot)
    {
        // Top-left mission tactical readout
        DrawRect(new Rect2(60, 50, 480, 110), new Color("050c18", 0.85f));
        DrawRect(new Rect2(60, 50, 480, 110), new Color(ColorCyan, 0.4f), false, 1.5f);

        // Title and phase
        DrawString(ThemeDB.FallbackFont, new Vector2(75, 78), "AXIOM-7 // GEOTHERMAL TELEMETRY", HorizontalAlignment.Left, -1, 14, ColorCyan);

        // Dynamic Shield Integrity Readout
        float shieldPct = 100f;
        if (t >= 38.0f && t < 48.0f)
        {
            shieldPct = Mathf.Lerp(100f, 42f, (t - 38f) / 10f);
        }
        else if (t >= 48.0f && t < 58.0f)
        {
            shieldPct = Mathf.Lerp(42f, 0f, (t - 48f) / 10f);
        }
        else if (t >= 58.0f)
        {
            shieldPct = 0f;
        }

        string shieldText = shieldPct > 0f
            ? $"SKY-SHIELD INTEGRITY: {shieldPct:F0}% [CRITICAL FAULT]"
            : "PLANETARY SKY-SHIELD: OFFLINE // COLLAPSED";

        Color statColor = shieldPct > 50f ? ColorAmber : (shieldPct > 0f ? ColorLavaCore : ColorCyan);
        DrawString(ThemeDB.FallbackFont, new Vector2(75, 105), shieldText, HorizontalAlignment.Left, -1, 13, statColor);

        // Progress bar for shield integrity
        DrawRect(new Rect2(75, 116, 450, 8), new Color("1e293b"));
        if (shieldPct > 0f)
        {
            DrawRect(new Rect2(75, 116, 450 * (shieldPct / 100f), 8), statColor);
        }

        string actText = t >= 58.0f
            ? "ACT II: COMPLETE // CITADEL SPIRE EXPOSED"
            : "TARGET: CITADEL GEOTHERMAL REACTOR 07";
        DrawString(ThemeDB.FallbackFont, new Vector2(75, 144), actText, HorizontalAlignment.Left, -1, 12, new Color("94a3b8"));
    }

    /// <summary>Draws 2.39:1 anamorphic cinema bars.</summary>
    private void DrawCinemaBars(float fit, Vector2 margin)
    {
        float barHeight = 85f * fit;
        DrawRect(new Rect2(0, 0, Size.X, barHeight + margin.Y), Colors.Black);
        DrawRect(new Rect2(0, Size.Y - barHeight - margin.Y, Size.X, barHeight + margin.Y), Colors.Black);
    }

    /// <summary>Draws subtle CRT horizontal scanline effect across the viewport.</summary>
    private void DrawCrtScanlines()
    {
        Color scanColor = new(0f, 0f, 0f, 0.08f);
        for (float y = 0; y < Size.Y; y += 4f)
        {
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), scanColor, 1.5f);
        }
    }
}
