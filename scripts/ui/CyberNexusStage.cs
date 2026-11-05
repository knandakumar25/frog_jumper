using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 10 prologue ("The Heart of the Machine // Cyber-Nexus Intro").
/// Composes the cavernous interior cybernetic chasm of the Citadel Spire, towering vertical mainframe server monoliths,
/// cascading quantum digital data waterfalls, floating surveillance prism drones, the spinning quantum core matrix,
/// the ingress gantry bridge, operative silhouettes, and smooth camera choreography across nine authored narrative beats.
/// </summary>
public partial class CyberNexusStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_nexus_intro/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>Requires authored environment art and cue metadata; the owner supplies a safe completion fallback.</summary>
    public bool ResourcesAvailable => CampaignArtwork.Available("cyber_nexus", ArtPath);

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan
    private static readonly Color ColorGold = new("ffd700");     // Core Gold
    private static readonly Color ColorAmber = new("fbbf24");    // Hazard Amber
    private static readonly Color ColorServer = new("0a0f1d");   // Server Tower Dark
    private static readonly Color ColorGantry = new("111827");   // Gantry Deck

    // Shot boundary schedule matching CyberNexusIntro dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 540),  // Shot 0: Wide shot - entering the Cyber-Nexus cavernous abyss
        new(680, 520),  // Shot 1: Mirage gazing at the towering server towers
        new(1180, 540), // Shot 2: Titan listening to deep electrical vibration
        new(820, 580),  // Shot 3: Oceanus inspecting liquid helium conduits beneath gantry
        new(1040, 480), // Shot 4: Aura detecting the awakening neural hive mind
        new(940, 520),  // Shot 5: Surya charging solar bulwark against defense firewalls
        new(960, 420),  // Shot 6: AXIOM-7 holographic telemetry showing 3 quantum nodes
        new(1280, 480), // Shot 7: Mirage analyzing Apex Bastion connection
        new(960, 520)   // Shot 8: Epic wide squad shot; advancing into the Cyber-Nexus
    };

    private static readonly float[] CameraZooms = {
        1.00f, // Shot 0
        1.14f, // Shot 1
        1.16f, // Shot 2
        1.15f, // Shot 3
        1.18f, // Shot 4
        1.15f, // Shot 5
        1.20f, // Shot 6
        1.14f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes the deep cybernetic chasm, vertical server towers, falling digital data streams,
    /// central quantum core, holographic interface, gantry bridge, operative silhouettes, and cinema letterbox analytically.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep cybernetic abyss backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("02040c"));

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

        // Cybernetic vertical chasm background gradient
        DrawChasmGradient(t);
        CampaignArtwork.DrawEnvironment(this, "cyber_nexus", Seconds, aftermath: false, drawBase: true);

        // Towering server monoliths on left and right flanks
        DrawServerMonoliths(t);

        // Cascading digital data streams (data waterfalls)
        DrawDataWaterfalls(t);

        // Central Quantum Processing Core in the deep distance
        DrawQuantumProcessingCore(t);

        // Floating surveillance prism drones
        DrawSurveillancePrismDrones(t);

        // Ingress Gantry Bridge across the chasm
        DrawIngressGantry(t);

        // AXIOM-7 holographic wireframe projection pedestal
        DrawHolographicInterface(t, shot);

        // Operative squad silhouettes on the gantry
        DrawSquadSilhouettes(t, shot);

        // Tactical holographic HUD telemetry
        DrawTacticalTelemetry(t, shot);

        // 3. Screen-space overlays (CRT scanlines & 2.39:1 Cinema Letterbox)
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawCrtScanlines();
        DrawCinemaLetterbox();
    }

    private static int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t >= ShotTimes[i] && t < ShotTimes[i + 1]) return i;
        }
        return ShotTimes.Length - 2;
    }

    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float localT = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);
        float smoothT = localT * localT * (3f - 2f * localT); // Smoothstep

        Vector2 current = CameraTargets[shot];
        Vector2 next = (shot < CameraTargets.Length - 1) ? CameraTargets[shot + 1] : current;

        // Subtle slow pan drift within the shot
        Vector2 drift = new(Mathf.Sin(t * 0.22f) * 14f, Mathf.Cos(t * 0.18f) * 8f);
        return current.Lerp(next, smoothT * 0.25f) + drift;
    }

    private static float GetSmoothCameraZoom(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float localT = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);
        return Mathf.Lerp(CameraZooms[shot], CameraZooms[shot] + 0.03f, localT);
    }

    private void DrawChasmGradient(float t)
    {
        for (int i = 0; i < 22; i++)
        {
            float y = i * 50f;
            float ratio = (float)i / 22f;
            Color chasmColor = new Color("02050e").Lerp(new Color("081026"), ratio);
            DrawRect(new Rect2(-200, y - 100, 2320, 52), chasmColor);
        }

        // Deep chasm perspective grid lines vanishing toward center-depth
        Vector2 vanishPoint = new(960, 480);
        for (int g = -4; g <= 4; g++)
        {
            Vector2 bottomEdge = new(960 + g * 320, 1100);
            DrawLine(vanishPoint, bottomEdge, new Color(0f, 0.94f, 1f, 0.08f), 1f);
        }
    }

    private void DrawServerMonoliths(float t)
    {
        // Left Flank Server Towers: x=-100 to 480
        DrawRect(new Rect2(-100, -200, 480, 1400), ColorServer);
        DrawLine(new Vector2(380, -200), new Vector2(380, 1200), new Color("1e293b"), 4f);

        // Right Flank Server Towers: x=1480 to 2060
        DrawRect(new Rect2(1540, -200, 520, 1400), ColorServer);
        DrawLine(new Vector2(1540, -200), new Vector2(1540, 1200), new Color("1e293b"), 4f);

        // Blinking server status LED arrays on left flank
        for (int row = 0; row < 18; row++)
        {
            float ry = row * 65f - 100f;
            for (int col = 0; col < 6; col++)
            {
                float rx = 40f + col * 50f;
                float blink = Mathf.Sin(t * 3.5f + row * 1.3f + col * 0.7f);
                Color ledColor = (blink > 0.4f) ? ColorCyan : (blink > -0.2f ? ColorAmber : new Color("059669"));
                DrawCircle(new Vector2(rx, ry), 2.5f, new Color(ledColor.R, ledColor.G, ledColor.B, 0.65f));
            }
        }

        // Blinking server status LED arrays on right flank
        for (int row = 0; row < 18; row++)
        {
            float ry = row * 65f - 100f;
            for (int col = 0; col < 6; col++)
            {
                float rx = 1600f + col * 50f;
                float blink = Mathf.Sin(t * 4.0f + row * 1.1f + col * 0.9f);
                Color ledColor = (blink > 0.3f) ? ColorCyan : (blink > -0.1f ? ColorGold : new Color("059669"));
                DrawCircle(new Vector2(rx, ry), 2.5f, new Color(ledColor.R, ledColor.G, ledColor.B, 0.65f));
            }
        }

        // High-voltage quantum fiber cables slung across the chasm
        for (int c = 0; c < 5; c++)
        {
            float cy1 = 80f + c * 140f;
            float cy2 = 120f + c * 140f;
            float sag = 60f + c * 25f;
            DrawLine(new Vector2(380, cy1), new Vector2(960, cy1 + sag), new Color("0f172a"), 5f);
            DrawLine(new Vector2(960, cy1 + sag), new Vector2(1540, cy2), new Color("0f172a"), 5f);

            // Pulsing cyan light packet traveling across the fiber cable
            float packetT = (t * 0.8f + c * 0.35f) % 1.0f;
            Vector2 packetPos = (packetT < 0.5f)
                ? new Vector2(380, cy1).Lerp(new Vector2(960, cy1 + sag), packetT * 2f)
                : new Vector2(960, cy1 + sag).Lerp(new Vector2(1540, cy2), (packetT - 0.5f) * 2f);
            DrawCircle(packetPos, 3.5f, ColorCyan);
        }
    }

    private void DrawDataWaterfalls(float t)
    {
        // Cascading vertical data glyph streams in background
        for (int stream = 0; stream < 16; stream++)
        {
            float sx = 440f + stream * 68f;
            float speed = 260f + (stream * 47) % 180;
            float sy = ((t * speed + stream * 120f) % 1100f) - 100f;

            Color streamColor = (stream % 4 == 0) ? ColorGold : ColorCyan;

            // Stream trailing light
            DrawLine(new Vector2(sx, sy - 80f), new Vector2(sx, sy), new Color(streamColor.R, streamColor.G, streamColor.B, 0.35f), 1.5f);
            DrawCircle(new Vector2(sx, sy), 2.5f, new Color(1f, 1f, 1f, 0.8f));
        }
    }

    private void DrawQuantumProcessingCore(float t)
    {
        Vector2 coreCenter = new(960, 380);

        // Core background glow
        float corePulse = 0.7f + 0.3f * Mathf.Sin(t * 3f);
        DrawCircle(coreCenter, 180f, new Color(0f, 0.94f, 1f, 0.08f * corePulse));
        DrawCircle(coreCenter, 120f, new Color(1f, 0.84f, 0.2f, 0.12f * corePulse));
        DrawCircle(coreCenter, 60f, new Color(1f, 1f, 1f, 0.35f * corePulse));

        // Concentric rotating logic rings
        for (int r = 1; r <= 3; r++)
        {
            float rad = r * 45f;
            float spin = t * (0.6f / r) * (r % 2 == 0 ? 1 : -1);

            // Ring perimeter segments
            for (int seg = 0; seg < 6; seg++)
            {
                float ang1 = seg * (Mathf.Pi / 3f) + spin;
                float ang2 = ang1 + 0.35f;
                Vector2 p1 = coreCenter + new Vector2(Mathf.Cos(ang1), Mathf.Sin(ang1)) * rad;
                Vector2 p2 = coreCenter + new Vector2(Mathf.Cos(ang2), Mathf.Sin(ang2)) * rad;
                Color ringColor = (r == 2) ? ColorGold : ColorCyan;
                DrawLine(p1, p2, new Color(ringColor.R, ringColor.G, ringColor.B, 0.7f), 3f);
            }
        }

        // The 3 Quantum Processing Nodes (targets described in narrative)
        for (int node = 0; node < 3; node++)
        {
            float nodeAngle = node * (Mathf.Pi * 2f / 3f) - Mathf.Pi / 2f + t * 0.4f;
            Vector2 nodePos = coreCenter + new Vector2(Mathf.Cos(nodeAngle), Mathf.Sin(nodeAngle)) * 170f;

            DrawCircle(nodePos, 14f, new Color("1e293b"));
            DrawCircle(nodePos, 8f, ColorCyan);
            DrawCircle(nodePos, 4f, Colors.White);

            // Energy beam connecting node to central core
            DrawLine(nodePos, coreCenter, new Color(0f, 0.94f, 1f, 0.25f), 1.5f);
        }
    }

    private void DrawSurveillancePrismDrones(float t)
    {
        // 3 floating hexagonal surveillance prism drones patrolling in slow orbits
        for (int d = 0; d < 3; d++)
        {
            float dAngle = d * 2.1f + t * 0.35f;
            Vector2 dPos = new(960 + Mathf.Cos(dAngle) * 440f, 320 + Mathf.Sin(dAngle * 1.5f) * 120f);

            // Hexagonal prism chassis
            DrawCircle(dPos, 16f, new Color("0f172a"));
            DrawCircle(dPos, 10f, new Color("1e293b"));
            DrawCircle(dPos, 5f, ColorAmber);

            // Scanning cone emitted downward
            Vector2 coneLeft = dPos + new Vector2(-40, 140);
            Vector2 coneRight = dPos + new Vector2(40, 140);
            DrawLine(dPos, coneLeft, new Color(1f, 0.8f, 0.2f, 0.15f), 1f);
            DrawLine(dPos, coneRight, new Color(1f, 0.8f, 0.2f, 0.15f), 1f);
        }
    }

    private void DrawIngressGantry(float t)
    {
        // Ingress Gantry Bridge spanning x=0 to 1920 at y=680, height 120
        DrawRect(new Rect2(0, 680, 1920, 140), ColorGantry);

        // Deck steel top plate
        DrawRect(new Rect2(0, 680, 1920, 10), new Color("334155"));

        // Glowing cyan energy inlay strip along deck edge
        float stripPulse = 0.7f + 0.3f * Mathf.Sin(t * 3f);
        DrawLine(new Vector2(0, 685), new Vector2(1920, 685), new Color(0f, 0.94f, 1f, 0.85f * stripPulse), 3f);

        // Yellow caution chevrons
        for (int x = 0; x < 1920; x += 50)
        {
            DrawLine(new Vector2(x, 690), new Vector2(x + 22, 706), ColorAmber, 3f);
        }

        // Under-deck liquid helium coolant pipe manifold (described by Oceanus)
        DrawLine(new Vector2(0, 725), new Vector2(1920, 725), new Color("66d9e8"), 8f);
        for (int v = 120; v < 1900; v += 240)
        {
            DrawCircle(new Vector2(v, 725), 8f, new Color("38bdf8"));
        }

        // Bridge safety railing
        DrawLine(new Vector2(0, 630), new Vector2(1920, 630), new Color("475569"), 3f);
        DrawLine(new Vector2(0, 655), new Vector2(1920, 655), new Color("334155"), 2f);
        for (int p = 40; p < 1920; p += 110)
        {
            DrawLine(new Vector2(p, 630), new Vector2(p, 680), new Color("475569"), 3f);
        }
    }

    private void DrawHolographicInterface(float t, int shot)
    {
        Vector2 consolePos = new(960, 680);

        // Physical terminal pedestal
        DrawRect(new Rect2(consolePos.X - 16, consolePos.Y - 60, 32, 60), new Color("1e293b"));
        DrawRect(new Rect2(consolePos.X - 22, consolePos.Y - 65, 44, 10), new Color("334155"));

        // Holographic projector emitter cone
        Vector2 holoBase = consolePos + new Vector2(0, -65);
        DrawCircle(holoBase, 6f, ColorCyan);

        // Floating wireframe holographic Spire / Nexus schematic
        Vector2 holoCenter = holoBase + new Vector2(0, -90);
        float holoPulse = 0.7f + 0.3f * Mathf.Sin(t * 4f);

        // Holographic emitter cone rays
        DrawLine(holoBase, holoCenter + new Vector2(-60, 40), new Color(0f, 0.94f, 1f, 0.25f), 1f);
        DrawLine(holoBase, holoCenter + new Vector2(60, 40), new Color(0f, 0.94f, 1f, 0.25f), 1f);

        // Rotating wireframe cylinder schematic of the Spire
        float spin = t * 0.8f;
        for (int h = -2; h <= 2; h++)
        {
            float hy = holoCenter.Y + h * 20;
            float hw = 45f - Mathf.Abs(h) * 8f;
            Vector2 p1 = new(holoCenter.X - hw * Mathf.Cos(spin), hy);
            Vector2 p2 = new(holoCenter.X + hw * Mathf.Cos(spin), hy);
            DrawLine(p1, p2, new Color(0f, 0.94f, 1f, 0.65f * holoPulse), 2f);
        }

        // Floating holographic targeting reticles on the 3 nodes
        for (int ret = 0; ret < 3; ret++)
        {
            float ra = ret * 2.09f + t;
            Vector2 rpos = holoCenter + new Vector2(Mathf.Cos(ra) * 50f, Mathf.Sin(ra) * 35f);
            DrawCircle(rpos, 4f, ColorAmber);
        }
    }

    private void DrawSquadSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    private void DrawTacticalTelemetry(float t, int shot)
    {
        // Holographic HUD bracket & telemetry in upper left
        Vector2 hudPos = new(80, 160);
        DrawLine(hudPos, hudPos + new Vector2(170, 0), ColorCyan, 2f);
        DrawLine(hudPos, hudPos + new Vector2(0, 55), ColorCyan, 2f);

        // Indicator beacons
        DrawCircle(hudPos + new Vector2(18, 18), 3.5f, ColorCyan);
        DrawCircle(hudPos + new Vector2(36, 18), 3.5f, ColorAmber);
        DrawCircle(hudPos + new Vector2(54, 18), 3.5f, new Color("34d399"));

        // Pulse warning light if neural matrix active (t >= 40s)
        if (t >= 40f)
        {
            float warn = Mathf.Sin(t * 6f) > 0.3f ? 1f : 0.2f;
            DrawCircle(hudPos + new Vector2(72, 18), 3.5f, new Color(1f, 0.2f, 0.2f, warn));
        }
    }

    private void DrawCrtScanlines()
    {
        for (int y = 0; y < (int)Size.Y; y += 4)
        {
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), new Color(0f, 0f, 0f, 0.12f));
        }
    }

    private void DrawCinemaLetterbox()
    {
        float targetRatio = 2.39f;
        float currentRatio = Size.X / Mathf.Max(Size.Y, 1f);
        if (currentRatio > targetRatio)
        {
            float letterboxW = (Size.X - Size.Y * targetRatio) * 0.5f;
            DrawRect(new Rect2(0, 0, letterboxW, Size.Y), Colors.Black);
            DrawRect(new Rect2(Size.X - letterboxW, 0, letterboxW, Size.Y), Colors.Black);
        }
        else
        {
            float letterboxH = (Size.Y - Size.X / targetRatio) * 0.5f;
            DrawRect(new Rect2(0, 0, Size.X, letterboxH), Colors.Black);
            DrawRect(new Rect2(0, Size.Y - letterboxH, Size.X, letterboxH), Colors.Black);
        }
    }
}
