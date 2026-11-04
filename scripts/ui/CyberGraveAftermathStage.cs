using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 6 post-extraction epilogue ("Ascent from the Cyber-Grave").
/// Composes the heavy freight lift ascending through the 400m vertical shaft, rescued captives in thermal blankets,
/// operative character silhouettes, venting geothermal steam, decrypted Citadel Spire holographic intelligence,
/// and smooth camera choreography across nine authored narrative beats.
/// </summary>
public partial class CyberGraveAftermathStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_grave_aftermath/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>All visuals are generated procedurally, guaranteeing zero missing-resource crashes.</summary>
    public bool ResourcesAvailable => true;

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan
    private static readonly Color ColorAmber = new("fbbf24");    // Hazard Amber
    private static readonly Color ColorBioGreen = new("10b981"); // Bio Green
    private static readonly Color ColorFoil = new("fef08a");     // Thermal Blanket Gold

    // Shot boundary schedule matching CyberGraveAftermath dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 8f, 18f, 28f, 38f, 48f, 58f, 68f, 78f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - lift platform ascending shaft
        new(720, 600),  // Shot 1: Oceanus tending to freed captives
        new(1180, 560), // Shot 2: Aura harmonic resonance ring
        new(1380, 580), // Shot 3: Titan watching venting geothermal siphons
        new(580, 550),  // Shot 4: Mirage analyzing decrypted vault data
        new(960, 480),  // Shot 5: Surya topside comms link & extraction route
        new(960, 480),  // Shot 6: Orbital defense radar lock warning
        new(960, 560),  // Shot 7: Kaito standing vanguard before squad
        new(960, 420)   // Shot 8: Wide upward pull toward surface daylight
    };

    private static readonly float[] CameraZooms = {
        1.05f, // Shot 0
        1.18f, // Shot 1
        1.18f, // Shot 2
        1.16f, // Shot 3
        1.20f, // Shot 4
        1.12f, // Shot 5
        1.14f, // Shot 6
        1.10f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes the ascending shaft architecture, freight lift, rescued civilians, operative silhouettes,
    /// venting steam particles, holographic intelligence display, and cinema letterbox analytically.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep bedrock shaft backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("03070d"));

        float t = Mathf.Clamp(Seconds, 0, 90f);
        int shot = GetCurrentShot(t);

        // Aspect fit to standard 1920x1080 canvas
        float fit = Mathf.Min(Size.X / 1920f, Size.Y / 1080f);
        Vector2 margin = (Size - new Vector2(1920f, 1080f) * fit) * 0.5f;

        Vector2 camPos = GetSmoothCameraPosition(t, shot);
        float camZoom = GetSmoothCameraZoom(t, shot);

        // 2. Camera-zoomed cinematic world layer
        DrawSetTransform(margin + (new Vector2(960f, 540f) - camPos * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // Subterranean vertical shaft background with downward rushing parallax
        DrawShaftBackground(t);

        // Rising geothermal steam and exhaust plumes from reactor containment
        DrawRisingSteamPlumes(t);

        // Heavy industrial freight lift platform
        DrawFreightLift(t);

        // Rescued civilians wrapped in thermal survival foil blankets
        DrawRescuedCaptives(t);

        // Axiom Force operative silhouettes staged on the platform
        DrawOperativeSilhouettes(t, shot);

        // Holographic Citadel Spire intelligence projection and comms telemetry
        DrawHolographicIntelligence(t, shot);

        // 3. Screen-space overlays (letterbox bars, CRT scanlines, lens vignette)
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawScreenVignette(fit, margin, t);
    }

    /// <summary>Identifies the current active shot index based on timeline seconds.</summary>
    private static int GetCurrentShot(float t)
    {
        for (int i = ShotTimes.Length - 2; i >= 0; i--)
        {
            if (t >= ShotTimes[i]) return i;
        }
        return 0;
    }

    /// <summary>Calculates smooth eased camera focal coordinates between adjacent shots.</summary>
    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        if (shot >= CameraTargets.Length - 1) return CameraTargets[^1];
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float progress = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);

        // Smooth cubic ease-in-out for shot pan
        float ease = progress < 0.25f ? Mathf.SmoothStep(0f, 1f, progress / 0.25f) : 1f;
        Vector2 prev = shot > 0 ? CameraTargets[shot - 1] : CameraTargets[0];
        return prev.Lerp(CameraTargets[shot], ease);
    }

    /// <summary>Calculates smooth camera zoom between adjacent shots.</summary>
    private static float GetSmoothCameraZoom(float t, int shot)
    {
        if (shot >= CameraZooms.Length - 1) return CameraZooms[^1];
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float progress = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);
        float ease = progress < 0.3f ? Mathf.SmoothStep(0f, 1f, progress / 0.3f) : 1f;
        float prev = shot > 0 ? CameraZooms[shot - 1] : CameraZooms[0];
        return Mathf.Lerp(prev, CameraZooms[shot], ease);
    }

    /// <summary>Renders the massive shaft concrete walls, downward-rushing guide rails, and depth cabling.</summary>
    private void DrawShaftBackground(float t)
    {
        // Downward rushing motion representing upward lift ascent (420 px/sec)
        float railOffset = (t * 420.0f) % 240.0f;

        // Side concrete bedrock walls
        DrawRect(new Rect2(0, 0, 240, 1080), new Color("050c17"));
        DrawRect(new Rect2(1680, 0, 240, 1080), new Color("050c17"));

        // Heavy vertical elevator guide rails
        DrawLine(new Vector2(240, 0), new Vector2(240, 1080), new Color("1e293b"), 8);
        DrawLine(new Vector2(1680, 0), new Vector2(1680, 1080), new Color("1e293b"), 8);
        DrawLine(new Vector2(248, 0), new Vector2(248, 1080), new Color("334155"), 2);
        DrawLine(new Vector2(1672, 0), new Vector2(1672, 1080), new Color("334155"), 2);

        // Repeating horizontal shaft support cross-beams rushing downwards
        for (float y = -240.0f + railOffset; y < 1320.0f; y += 240.0f)
        {
            // Left wall structural brackets
            DrawRect(new Rect2(180, y, 60, 24), new Color("0f172a"));
            DrawLine(new Vector2(180, y + 12), new Vector2(240, y + 12), new Color("00f0ff") * 0.25f, 2);

            // Right wall structural brackets
            DrawRect(new Rect2(1680, y, 60, 24), new Color("0f172a"));
            DrawLine(new Vector2(1680, y + 12), new Vector2(1740, y + 12), new Color("00f0ff") * 0.25f, 2);

            // Background cable conduits
            DrawLine(new Vector2(300, y - 60), new Vector2(300, y + 180), new Color("0a101d"), 3);
            DrawLine(new Vector2(1620, y - 60), new Vector2(1620, y + 180), new Color("0a101d"), 3);
        }

        // Heavy high-tension suspension steel cables running down the center
        DrawLine(new Vector2(400, 0), new Vector2(400, 720), new Color("1e293b"), 4);
        DrawLine(new Vector2(1520, 0), new Vector2(1520, 720), new Color("1e293b"), 4);

        // Surface daylight opening glow far above (expands as t increases)
        float daylightFactor = Mathf.Clamp(t / 90.0f, 0.1f, 1.0f);
        Color daylight = new Color("38bdf8") * (0.08f + 0.18f * daylightFactor);
        DrawCircle(new Vector2(960, -200), 400.0f + daylightFactor * 250.0f, daylight);
    }

    /// <summary>Renders billowing thermal steam plumes venting upward from the shut-down reactors below.</summary>
    private void DrawRisingSteamPlumes(float t)
    {
        for (int i = 0; i < 14; i++)
        {
            float seed = i * 47.3f;
            float speed = 80.0f + (i % 5) * 25.0f;
            float yPos = 1100.0f - ((t * speed + seed * 10.0f) % 650.0f);
            float xPos = 320.0f + (Mathf.Sin(seed + t * 0.8f) * 0.5f + 0.5f) * 1280.0f;
            float radius = 35.0f + (1100.0f - yPos) * 0.08f;
            float alpha = Mathf.Clamp(Mathf.Sin((1100.0f - yPos) / 650.0f * Mathf.Pi) * 0.14f, 0f, 0.16f);

            // Steam hue alternates between cool white-cyan and faint geothermal amber
            Color steamColor = (i % 3 == 0 ? ColorAmber : new Color("e2e8f0")) * alpha;
            DrawCircle(new Vector2(xPos, yPos), radius, steamColor);
        }
    }

    /// <summary>Renders the heavy industrial elevator platform, diamond grating, and safety railings.</summary>
    private void DrawFreightLift(float t)
    {
        // Platform main bounds: centered at X=960, top at Y=720, height 120, width 1320
        Rect2 platRect = new(300, 720, 1320, 100);

        // Heavy steel girder base
        DrawRect(platRect, new Color("0f172a"));
        DrawRect(new Rect2(300, 720, 1320, 8), new Color("334155"));

        // Diamond tread floor pattern
        for (int x = 320; x < 1600; x += 36)
        {
            DrawLine(new Vector2(x, 728), new Vector2(x + 18, 800), new Color("1e293b"), 1);
            DrawLine(new Vector2(x + 18, 728), new Vector2(x, 800), new Color("1e293b"), 1);
        }

        // Protective caged safety railing (posts and horizontal mesh bars)
        DrawLine(new Vector2(300, 640), new Vector2(1620, 640), new Color("475569"), 4);
        DrawLine(new Vector2(300, 680), new Vector2(1620, 680), new Color("334155"), 2);
        for (int postX = 300; postX <= 1620; postX += 165)
        {
            DrawLine(new Vector2(postX, 640), new Vector2(postX, 720), new Color("475569"), 4);
            // Yellow caution stripes on corner posts
            if (postX == 300 || postX == 1620)
            {
                DrawRect(new Rect2(postX - 6, 640, 12, 80), ColorAmber * 0.8f);
            }
        }

        // Flashing amber caution strobe lights on elevator corners
        float strobeAlpha = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(t * 4.5f));
        DrawCircle(new Vector2(300, 634), 8, ColorAmber * strobeAlpha);
        DrawCircle(new Vector2(1620, 634), 8, ColorAmber * strobeAlpha);
        DrawCircle(new Vector2(300, 634), 24, ColorAmber * (strobeAlpha * 0.25f));
        DrawCircle(new Vector2(1620, 634), 24, ColorAmber * (strobeAlpha * 0.25f));

        // Subterranean under-platform machinery glow (soft cyan-blue hydraulic light)
        DrawRect(new Rect2(360, 820, 1200, 40), new Color("00f0ff") * 0.08f);
    }

    /// <summary>Renders the rescued civilians huddled in reflective thermal foil blankets.</summary>
    private void DrawRescuedCaptives(float t)
    {
        // Cluster of civilians centered between X=680 and X=840 on the lift floor
        Vector2[] captivePositions = {
            new(700, 715),
            new(735, 712),
            new(770, 716),
            new(805, 713),
            new(840, 717),
            new(720, 718),
            new(755, 715),
            new(790, 719),
            new(825, 716)
        };

        for (int i = 0; i < captivePositions.Length; i++)
        {
            Vector2 pos = captivePositions[i];
            float breathe = Mathf.Sin(t * 1.8f + i * 0.7f) * 2.0f;

            // Thermal emergency blanket wrap (trapezoid-like polygon simulation)
            float shimmer = 0.75f + 0.25f * Mathf.Sin(t * 3.0f + i * 1.1f);
            Color blanketColor = ColorFoil * shimmer;

            Vector2[] foilPoly = {
                new(pos.X - 16, pos.Y),
                new(pos.X - 12, pos.Y - 32 + breathe),
                new(pos.X + 12, pos.Y - 32 + breathe),
                new(pos.X + 16, pos.Y)
            };
            DrawColoredPolygon(foilPoly, blanketColor * 0.85f);
            DrawPolyline(foilPoly, new Color("fef9c3"), 1.5f);

            // Silhouette head
            DrawCircle(new Vector2(pos.X, pos.Y - 38 + breathe), 7, new Color("0f172a"));
        }

        // Bio-stability status display floating above the captive cluster
        float bioPulse = 0.7f + 0.3f * Mathf.Sin(t * 2.5f);
        DrawString(ThemeDB.FallbackFont, new Vector2(690, 655), "CAPTIVES SECURED: 17 // STABLE", HorizontalAlignment.Left, -1, 13, ColorBioGreen * bioPulse);
    }

    /// <summary>Renders distinctive Axiom Force squad operative silhouettes staged across the lift.</summary>
    private void DrawOperativeSilhouettes(float t, int shot)
    {
        // 1. OCEANUS (Kiana Vaea) - Kneeling by civilians at X=650, administering hydro-mist
        DrawOceanusSilhouette(new Vector2(650, 720), t, shot == 1);

        // 2. AURA PHOENICIA (Maya Khoury) - Standing at X=1180, hands up with ultrasonic prism rings
        DrawAuraSilhouette(new Vector2(1180, 720), t, shot == 2);

        // 3. APEX TITAN (King Tariq Nzinga) - Heavy bulwark frame at X=1420 leaning against railing
        DrawTitanSilhouette(new Vector2(1420, 720), t, shot == 3);

        // 4. MIRAGE BLADE (Zayd Al-Mansoor) - Stealth stance at X=520 holding glowing holographic slate
        DrawMirageSilhouette(new Vector2(520, 720), t, shot == 4);

        // 5. CYBER SHOGUN (Kaito Kurogane) - Vanguard stance at X=960 facing upward with sheathed blade
        DrawKaitoSilhouette(new Vector2(960, 720), t, shot >= 7);
    }

    /// <summary>Renders Oceanus kneeling and channeling soothing hydro-cellular mist.</summary>
    private void DrawOceanusSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorOceanus : ColorOceanus * 0.7f;

        // Kneeling torso & legs
        Vector2[] torso = {
            new(pos.X - 10, pos.Y),
            new(pos.X - 12, pos.Y - 34),
            new(pos.X + 8, pos.Y - 34),
            new(pos.X + 14, pos.Y)
        };
        DrawColoredPolygon(torso, new Color("0a192f"));
        DrawPolyline(torso, color, 2f);

        // Head and visor
        DrawCircle(new Vector2(pos.X - 2, pos.Y - 42), 8, new Color("0a192f"));
        DrawLine(new Vector2(pos.X, pos.Y - 42), new Vector2(pos.X + 6, pos.Y - 42), color, 2f);

        // Outstretched hand administering hydro-mist toward civilians
        DrawLine(new Vector2(pos.X + 6, pos.Y - 26), new Vector2(pos.X + 24, pos.Y - 22), color, 2f);

        // Soothing aqua hydro mist particles
        for (int p = 0; p < 6; p++)
        {
            float ang = t * 2.0f + p * Mathf.Pi / 3.0f;
            Vector2 mist = new Vector2(pos.X + 26, pos.Y - 22) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (12.0f + p * 2.0f);
            DrawCircle(mist, 3f, ColorOceanus * 0.4f);
        }
    }

    /// <summary>Renders Aura Phoenicia projecting harmonic resonance waves that calm the bio-network.</summary>
    private void DrawAuraSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorAura : ColorAura * 0.7f;

        // Standing slender figure
        Vector2[] body = {
            new(pos.X - 8, pos.Y),
            new(pos.X - 7, pos.Y - 52),
            new(pos.X + 7, pos.Y - 52),
            new(pos.X + 8, pos.Y)
        };
        DrawColoredPolygon(body, new Color("1a0b1e"));
        DrawPolyline(body, color, 2f);

        // Head
        DrawCircle(new Vector2(pos.X, pos.Y - 60), 7, new Color("1a0b1e"));
        DrawCircle(new Vector2(pos.X, pos.Y - 60), 8, color * 0.4f);

        // Ultrasonic harmonic resonance wave rings expanding outwards
        for (int w = 1; w <= 3; w++)
        {
            float ringPhase = (t * 1.5f + w * 0.33f) % 1.0f;
            float ringRadius = 15.0f + ringPhase * 55.0f;
            float ringAlpha = (1.0f - ringPhase) * 0.5f;
            DrawCircle(new Vector2(pos.X, pos.Y - 45), ringRadius, ColorAura * (ringAlpha * 0.25f));
            DrawArc(new Vector2(pos.X, pos.Y - 45), ringRadius, 0, Mathf.Tau, 32, ColorAura * ringAlpha, 1.5f);
        }
    }

    /// <summary>Renders Apex Titan standing as a massive kinetic bulwark overlooking the reactor shaft.</summary>
    private void DrawTitanSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorTitan : ColorTitan * 0.7f;

        // Heavy broad-shouldered torso
        Vector2[] torso = {
            new(pos.X - 18, pos.Y),
            new(pos.X - 22, pos.Y - 62),
            new(pos.X + 22, pos.Y - 62),
            new(pos.X + 18, pos.Y)
        };
        DrawColoredPolygon(torso, new Color("160b24"));
        DrawPolyline(torso, color, 3f);

        // Kinetic shoulder pauldrons
        DrawRect(new Rect2(pos.X - 28, pos.Y - 66, 12, 14), color);
        DrawRect(new Rect2(pos.X + 16, pos.Y - 66, 12, 14), color);

        // Armored helmet
        DrawRect(new Rect2(pos.X - 8, pos.Y - 78, 16, 16), new Color("160b24"));
        DrawLine(new Vector2(pos.X - 6, pos.Y - 72), new Vector2(pos.X + 6, pos.Y - 72), color, 2f);

        // Heavy arm resting on the elevator railing
        DrawLine(new Vector2(pos.X + 20, pos.Y - 50), new Vector2(pos.X + 38, pos.Y - 40), color, 3f);
    }

    /// <summary>Renders Mirage Blade crouched, analyzing the decrypted vault slate.</summary>
    private void DrawMirageSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorMirage : ColorMirage * 0.7f;

        // Agile crouched stance
        Vector2[] stance = {
            new(pos.X - 12, pos.Y),
            new(pos.X - 14, pos.Y - 38),
            new(pos.X + 10, pos.Y - 38),
            new(pos.X + 12, pos.Y)
        };
        DrawColoredPolygon(stance, new Color("051c14"));
        DrawPolyline(stance, color, 2f);

        // Head and phantom hood
        DrawCircle(new Vector2(pos.X - 2, pos.Y - 46), 7, new Color("051c14"));
        DrawLine(new Vector2(pos.X - 5, pos.Y - 46), new Vector2(pos.X + 2, pos.Y - 46), color, 2f);

        // Glowing holographic tactical data slate in hand
        Vector2 slatePos = new(pos.X + 16, pos.Y - 32);
        DrawRect(new Rect2(slatePos.X - 6, slatePos.Y - 10, 12, 20), ColorMirage * 0.9f, false, 2f);
        DrawRect(new Rect2(slatePos.X - 4, slatePos.Y - 8, 8, 16), ColorMirage * 0.3f);
        DrawLine(slatePos, slatePos + new Vector2(14, -14), ColorMirage * 0.5f, 1f);
    }

    /// <summary>Renders Cyber Shogun Kaito Kurogane at the platform center vanguard stance.</summary>
    private void DrawKaitoSilhouette(Vector2 pos, float t, bool active)
    {
        Color color = active ? ColorKaito : ColorKaito * 0.75f;

        // Centered vanguard stance
        Vector2[] torso = {
            new(pos.X - 12, pos.Y),
            new(pos.X - 14, pos.Y - 56),
            new(pos.X + 14, pos.Y - 56),
            new(pos.X + 12, pos.Y)
        };
        DrawColoredPolygon(torso, new Color("1f0d0d"));
        DrawPolyline(torso, color, 2.5f);

        // Crest helmet
        DrawCircle(new Vector2(pos.X, pos.Y - 66), 8, new Color("1f0d0d"));
        DrawLine(new Vector2(pos.X - 4, pos.Y - 66), new Vector2(pos.X + 4, pos.Y - 66), color, 2f);
        // Shogun crest crescent
        DrawArc(new Vector2(pos.X, pos.Y - 74), 7, Mathf.Pi * 0.2f, Mathf.Pi * 0.8f, 16, color, 2f);

        // Katana scabbard angled at hip
        DrawLine(new Vector2(pos.X - 14, pos.Y - 32), new Vector2(pos.X - 32, pos.Y - 8), color, 3f);
        // Hilt resting under right hand
        DrawLine(new Vector2(pos.X - 14, pos.Y - 32), new Vector2(pos.X - 8, pos.Y - 42), ColorCyan, 2f);
    }

    /// <summary>Renders holographic projection of Citadel Spire blueprints and topside extraction radar.</summary>
    private void DrawHolographicIntelligence(float t, int shot)
    {
        // Active in Shots 4, 5, 6, 7
        if (shot < 4 || shot > 7) return;

        float alpha = Mathf.Clamp(Mathf.Sin(t * 3.0f) * 0.15f + 0.85f, 0.4f, 1.0f);
        Vector2 center = new(960, 360);

        // Outer holographic frame
        Rect2 frame = new(center.X - 220, center.Y - 140, 440, 260);
        DrawRect(frame, new Color("030f1c") * (0.8f * alpha));
        DrawRect(frame, ColorCyan * (0.6f * alpha), false, 2f);

        // Corner brackets
        float cLen = 18.0f;
        DrawLine(new Vector2(frame.Position.X, frame.Position.Y), new Vector2(frame.Position.X + cLen, frame.Position.Y), ColorCyan, 3f);
        DrawLine(new Vector2(frame.Position.X, frame.Position.Y), new Vector2(frame.Position.X, frame.Position.Y + cLen), ColorCyan, 3f);
        DrawLine(new Vector2(frame.End.X, frame.Position.Y), new Vector2(frame.End.X - cLen, frame.Position.Y), ColorCyan, 3f);
        DrawLine(new Vector2(frame.End.X, frame.Position.Y), new Vector2(frame.End.X, frame.Position.Y + cLen), ColorCyan, 3f);
        DrawLine(new Vector2(frame.Position.X, frame.End.Y), new Vector2(frame.Position.X + cLen, frame.End.Y), ColorCyan, 3f);
        DrawLine(new Vector2(frame.Position.X, frame.End.Y), new Vector2(frame.Position.X, frame.End.Y - cLen), ColorCyan, 3f);
        DrawLine(new Vector2(frame.End.X, frame.End.Y), new Vector2(frame.End.X - cLen, frame.End.Y), ColorCyan, 3f);
        DrawLine(new Vector2(frame.End.X, frame.End.Y), new Vector2(frame.End.X, frame.End.Y - cLen), ColorCyan, 3f);

        if (shot == 4)
        {
            // Decrypted Vault Ledger - Cyber-Grave linked to Citadel Spire
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 30), "CENTRAL VAULT LEDGER // DECRYPTED", HorizontalAlignment.Left, -1, 14, ColorMirage);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 60), "FACILITY AUTHORIZATION: CITADEL SPIRE", HorizontalAlignment.Left, -1, 13, Colors.White);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 85), "BIO-SYNTHETIC QUOTA: 10,000 CONVERSIONS", HorizontalAlignment.Left, -1, 13, ColorAmber);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 110), "DIRECTIVE ORIGIN: HIGH ARCHITECT VOID", HorizontalAlignment.Left, -1, 13, ColorKaito);

            // Wireframe spire blueprint
            Vector2 spireTip = new(center.X + 110, center.Y - 80);
            Vector2 spireBaseLeft = new(center.X + 60, center.Y + 80);
            Vector2 spireBaseRight = new(center.X + 160, center.Y + 80);
            DrawLine(spireTip, spireBaseLeft, ColorCyan * 0.8f, 2f);
            DrawLine(spireTip, spireBaseRight, ColorCyan * 0.8f, 2f);
            DrawLine(spireBaseLeft, spireBaseRight, ColorCyan * 0.8f, 2f);
            DrawLine(new Vector2(center.X + 85, center.Y), new Vector2(center.X + 135, center.Y), ColorCyan * 0.5f, 1f);
        }
        else if (shot == 5 || shot == 6)
        {
            // Comms telemetry & Orbital radar lock
            Color statusColor = shot == 6 ? ColorAmber : ColorSurya;
            string statusHeader = shot == 6 ? "ALERT: ORBITAL DEFENSE ARRAYS ARMED" : "SURYA // TOPSIDE EXTRACTION ROUTE";
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 30), statusHeader, HorizontalAlignment.Left, -1, 14, statusColor);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 60), "SURFACE SECTOR: UPPER TRANSIT JUNCTION", HorizontalAlignment.Left, -1, 13, Colors.White);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 85), "MEDIC TRANSPORTS: SECURED & ARMED", HorizontalAlignment.Left, -1, 13, ColorBioGreen);

            // Radar display
            Vector2 radarCenter = new(center.X + 110, center.Y);
            DrawCircle(radarCenter, 45, new Color("00f0ff") * 0.1f);
            DrawArc(radarCenter, 45, 0, Mathf.Tau, 32, statusColor * 0.6f, 1.5f);
            float sweepAngle = t * 3.5f;
            DrawLine(radarCenter, radarCenter + new Vector2(Mathf.Cos(sweepAngle), Mathf.Sin(sweepAngle)) * 45f, statusColor, 2f);
        }
        else
        {
            // Shot 7: Kaito rally call
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 35), "AXIOM FORCE // STATUS REPORT", HorizontalAlignment.Left, -1, 14, ColorKaito);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 70), "OPERATION: SUBTERRANEAN BREACH COMPLETE", HorizontalAlignment.Left, -1, 13, ColorCyan);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 100), "CASUALTIES: ZERO CIVILIANS LOST", HorizontalAlignment.Left, -1, 13, ColorBioGreen);
            DrawString(ThemeDB.FallbackFont, new Vector2(frame.Position.X + 20, frame.Position.Y + 130), "NEXT TARGET: CITADEL SPIRE", HorizontalAlignment.Left, -1, 14, ColorAmber);
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
