using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic stage for the Pre-Level 9 prologue ("Exterior Breach — Spire Outer Hull").
/// Renders high-altitude stratospheric twilight at 2,500m elevation, the monolithic vertical armor face of the Citadel Spire,
/// the hovering hijacked military VTOL gunship with open cargo ramps and dangling harness tethers,
/// operative silhouettes, anti-air flak bursts, tactical HUD telemetry, and smooth Hermite camera transitions across nine authored beats.
/// </summary>
public partial class SpireOuterHullStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/spire_outer_hull_intro/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>All visuals are generated procedurally, guaranteeing zero missing-resource crashes.</summary>
    public bool ResourcesAvailable => true;

    // Operative color accents
    private static readonly Color ColorKaito = new("00f0ff");    // Cyan
    private static readonly Color ColorMirage = new("aa44ff");   // Phase Violet
    private static readonly Color ColorTitan = new("ffcc00");    // Kinetic Gold
    private static readonly Color ColorOceanus = new("00d4ff");  // Hydro Aqua
    private static readonly Color ColorAura = new("ff00aa");     // Ultrasonic Pink
    private static readonly Color ColorSurya = new("ffaa00");    // Solar Amber
    private static readonly Color ColorBeacon = new("ff2222");   // Aviation Beacon Red
    private static readonly Color ColorFlak = new("ff6600");     // Flak Burst Orange

    // Shot schedule matching 9 cues (90 seconds total)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera targets and zooms for 9 shots
    private static readonly Vector2[] CameraTargets = {
        new(960, 540),  // Shot 0: Wide shot - VTOL approaching colossal Spire Outer Hull
        new(720, 560),  // Shot 1: Surya raising forward solar barrier against flak bursts
        new(1380, 500), // Shot 2: Mirage scanning Spire Gantry 42 maintenance rails
        new(820, 580),  // Shot 3: Titan checking magnetic boot clamp solenoids & harness lines
        new(1250, 480), // Shot 4: Oceanus analyzing freezing coolant venting from vertical shafts
        new(1150, 380), // Shot 5: Aura detecting acoustic drone silos launching aerial swarms
        new(760, 540),  // Shot 6: AXIOM-7 opening cargo ramps in high turbulence
        new(1050, 540), // Shot 7: Mirage looking out over the bottomless 2,500m cloud abyss
        new(960, 540)   // Shot 8: Kaito unsheathing blade with squad ready to breach
    };

    private static readonly float[] CameraZooms = {
        1.0f,  // Shot 0: Wide
        1.18f, // Shot 1: Surya flak focus
        1.20f, // Shot 2: Spire gantry rails focus
        1.16f, // Shot 3: Titan solenoid focus
        1.15f, // Shot 4: Coolant vent focus
        1.14f, // Shot 5: Drone silos focus
        1.12f, // Shot 6: Cargo ramp deployment
        1.18f, // Shot 7: Abyss threshold
        1.05f  // Shot 8: Full squad wide
    };

    /// <summary>Paints the entire procedural tableau each frame.</summary>
    public override void _Draw()
    {
        var viewport = GetViewportRect().Size;
        if (viewport.X < 10 || viewport.Y < 10) return;

        int shot = GetCurrentShot(Seconds);
        float tInShot = GetShotProgress(Seconds, shot);

        Vector2 camPos = InterpolateCameraPosition(shot, tInShot);
        float zoom = InterpolateCameraZoom(shot, tInShot);

        // Apply camera transformation centered on 1920x1080 design space
        Vector2 designCenter = new(960, 540);
        Vector2 offset = (designCenter - camPos) * zoom;
        Vector2 scale = (viewport / new Vector2(1920, 1080)) * zoom;

        DrawSetTransform(offset + (viewport - new Vector2(1920, 1080) * scale) * 0.5f, 0, scale);

        // 1. Stratospheric Sky & Storm Ceiling Below
        DrawSkyAndClouds(Seconds);

        // 2. Colossal Spire Outer Hull (Vertical Monolith)
        DrawSpireOuterHull(Seconds, shot);

        // 3. Flak Cannons & Atmospheric Tracers
        DrawFlakAndTracers(Seconds, shot);

        // 4. Hovering VTOL Gunship with Open Cargo Bay
        DrawVTOLGunship(Seconds, shot);

        // 5. Operative Silhouettes & Signature Energy Accents
        DrawOperatives(Seconds, shot);

        // 6. Reset Transform for Screen-Space Overlays (HUD, Vignette, Cinema Bars)
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawScreenOverlays(viewport, Seconds, shot);
    }

    private static int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t >= ShotTimes[i] && t < ShotTimes[i + 1]) return i;
        }
        return ShotTimes.Length - 2;
    }

    private static float GetShotProgress(float t, int shot)
    {
        float start = ShotTimes[shot];
        float end = ShotTimes[shot + 1];
        return Mathf.Clamp((t - start) / (end - start), 0f, 1f);
    }

    private static Vector2 InterpolateCameraPosition(int shot, float t)
    {
        float smoothT = t * t * (3f - 2f * t);
        Vector2 current = CameraTargets[shot];
        Vector2 next = shot < CameraTargets.Length - 1 ? CameraTargets[shot + 1] : current;
        return current.Lerp(next, smoothT * 0.25f);
    }

    private static float InterpolateCameraZoom(int shot, float t)
    {
        float smoothT = t * t * (3f - 2f * t);
        float current = CameraZooms[shot];
        float next = shot < CameraZooms.Length - 1 ? CameraZooms[shot + 1] : current;
        return Mathf.Lerp(current, next, smoothT * 0.25f);
    }

    private void DrawSkyAndClouds(float t)
    {
        // Stratospheric sky gradient: Deep void black to dark navy twilight
        for (int y = 0; y < 1080; y += 40)
        {
            float ratio = y / 1080f;
            Color skyCol = new Color(0.02f, 0.03f, 0.08f).Lerp(new Color(0.08f, 0.12f, 0.22f), ratio);
            DrawRect(new Rect2(0, y, 1920, 42), skyCol);
        }

        // Distant stars in upper stratosphere
        for (int i = 0; i < 40; i++)
        {
            float sx = (i * 123.4f) % 1920;
            float sy = (i * 71.3f) % 450;
            float twinkle = 0.4f + 0.6f * Mathf.Sin(t * 3f + i);
            DrawCircle(new Vector2(sx, sy), 1.2f, new Color(1f, 1f, 1f, twinkle * 0.7f));
        }

        // Storm cloud ceiling rolling far below (2,500m elevation)
        for (int x = -100; x < 2100; x += 180)
        {
            float drift = Mathf.Sin(t * 0.4f + x * 0.01f) * 30f;
            float cy = 880f + Mathf.Cos(x * 0.02f) * 40f + drift;
            DrawCircle(new Vector2(x, cy), 140f, new Color(0.07f, 0.09f, 0.15f, 0.85f));
            DrawCircle(new Vector2(x + 80, cy + 30), 120f, new Color(0.05f, 0.07f, 0.12f, 0.95f));
        }
        DrawRect(new Rect2(0, 960, 1920, 200), new Color(0.04f, 0.05f, 0.10f, 0.98f));
    }

    private void DrawSpireOuterHull(float t, int shot)
    {
        // The Citadel Spire: sheer monolithic titanium vertical wall dominating x = 1100 to 1920
        DrawRect(new Rect2(1100, 0, 820, 1080), new Color(0.09f, 0.11f, 0.16f));

        // Heavy vertical armor seams
        for (int x = 1100; x <= 1920; x += 160)
        {
            DrawLine(new Vector2(x, 0), new Vector2(x, 1080), new Color(0.05f, 0.06f, 0.09f), 4f);
            DrawLine(new Vector2(x + 2, 0), new Vector2(x + 2, 1080), new Color(0.18f, 0.22f, 0.30f), 1f);
        }

        // Horizontal structural girders & maintenance levels
        for (int y = 0; y <= 1080; y += 220)
        {
            DrawRect(new Rect2(1100, y, 820, 16), new Color(0.06f, 0.08f, 0.12f));
            DrawLine(new Vector2(1100, y), new Vector2(1920, y), new Color(0.25f, 0.30f, 0.40f), 2f);

            // Red warning beacons flashing on gantry edges
            float beaconBlink = (Mathf.Sin(t * 4f + y) > 0.1f) ? 1.0f : 0.2f;
            DrawCircle(new Vector2(1115, y + 8), 6f, ColorBeacon * beaconBlink);
            DrawCircle(new Vector2(1115, y + 8), 12f, new Color(1f, 0.1f, 0.1f, 0.25f * beaconBlink));
        }

        // Glowing cyan energy conduits & data trunks
        float pulse = 0.7f + 0.3f * Mathf.Sin(t * 3f);
        DrawLine(new Vector2(1260, 0), new Vector2(1260, 1080), ColorKaito * (0.8f * pulse), 3f);
        DrawLine(new Vector2(1580, 0), new Vector2(1580, 1080), ColorKaito * (0.6f * pulse), 2f);

        // Gantry 42 maintenance catwalks jutting out from hull (x = 980 to 1100)
        float gantryY = 620f;
        DrawRect(new Rect2(980, gantryY, 150, 14), new Color(0.15f, 0.18f, 0.25f));
        DrawLine(new Vector2(980, gantryY), new Vector2(1130, gantryY), new Color(0.4f, 0.5f, 0.65f), 2f);

        // Diagonal support struts below gantry
        DrawLine(new Vector2(980, gantryY + 14), new Vector2(1100, gantryY + 90), new Color(0.12f, 0.14f, 0.20f), 4f);
        DrawLine(new Vector2(1040, gantryY + 14), new Vector2(1100, gantryY + 50), new Color(0.12f, 0.14f, 0.20f), 3f);

        // Vertical elevator guide rails
        DrawLine(new Vector2(1180, 0), new Vector2(1180, 1080), new Color(0.2f, 0.25f, 0.35f), 2f);
        DrawLine(new Vector2(1200, 0), new Vector2(1200, 1080), new Color(0.2f, 0.25f, 0.35f), 2f);

        // Cryogenic exhaust louvers venting freezing mist in Shot 4
        if (shot == 4 || shot == 3 || shot == 5)
        {
            for (int i = 0; i < 15; i++)
            {
                float mistX = 1100f - (i * 12f) - Mathf.Sin(t * 5f + i) * 15f;
                float mistY = 480f + (i * 8f) + Mathf.Cos(t * 4f + i) * 10f;
                DrawCircle(new Vector2(mistX, mistY), 16f + i * 2f, new Color(0.5f, 0.85f, 1.0f, 0.12f));
            }
        }
    }

    private void DrawFlakAndTracers(float t, int shot)
    {
        // Flak tracer rounds arcing across the sky
        for (int i = 0; i < 5; i++)
        {
            float seed = i * 2.3f;
            float progress = Mathf.PosMod(t * 1.5f + seed, 3.0f) / 3.0f;
            Vector2 start = new(1350 + i * 80, 100 + i * 150);
            Vector2 end = new(200 + i * 100, 450 + i * 80);
            Vector2 current = start.Lerp(end, progress);

            DrawLine(current, current + (start - end).Normalized() * 45f, ColorFlak, 2.5f);
            DrawCircle(current, 3f, new Color(1f, 0.9f, 0.4f));

            // Flak air-burst explosion
            if (progress > 0.85f)
            {
                float burstRatio = (progress - 0.85f) / 0.15f;
                DrawCircle(current, 25f * burstRatio, new Color(1f, 0.4f, 0.1f, 0.4f * (1f - burstRatio)));
                DrawCircle(current, 12f * burstRatio, new Color(1f, 0.8f, 0.2f, 0.7f * (1f - burstRatio)));
            }
        }

        // Shot 1: Surya Prime's solar barrier deflecting incoming flak
        if (shot == 1)
        {
            float shieldPulse = 0.8f + 0.2f * Mathf.Sin(t * 8f);
            Vector2 shieldCenter = new(730, 560);
            DrawArc(shieldCenter, 70f, -Mathf.Pi * 0.4f, Mathf.Pi * 0.4f, 24, ColorSurya * shieldPulse, 5f);
            DrawArc(shieldCenter, 75f, -Mathf.Pi * 0.35f, Mathf.Pi * 0.35f, 20, new Color(1f, 0.9f, 0.5f, 0.6f * shieldPulse), 2f);
        }
    }

    private void DrawVTOLGunship(float t, int shot)
    {
        // Gunship hovering at left (x = 180 to 760, y = 380 to 720) with subtle engine hover oscillation
        float hoverY = Mathf.Sin(t * 2f) * 6f;
        Vector2 vtolBase = new(220, 420 + hoverY);

        // Main fuselage hull
        Vector2[] fuselage = {
            vtolBase + new Vector2(0, 80),
            vtolBase + new Vector2(80, 0),
            vtolBase + new Vector2(480, 20),
            vtolBase + new Vector2(540, 120),
            vtolBase + new Vector2(500, 200),
            vtolBase + new Vector2(60, 200)
        };
        DrawColoredPolygon(fuselage, new Color(0.12f, 0.14f, 0.18f));
        DrawPolyline(fuselage, new Color(0.25f, 0.30f, 0.38f), 2f);

        // Cockpit canopy on nose (tinted amber)
        Vector2[] cockpit = {
            vtolBase + new Vector2(80, 20),
            vtolBase + new Vector2(160, 20),
            vtolBase + new Vector2(140, 70),
            vtolBase + new Vector2(70, 70)
        };
        DrawColoredPolygon(cockpit, new Color(0.9f, 0.6f, 0.1f, 0.5f));

        // Dual tilt-rotor engine nacelles with glowing cyan thrust plumes
        Vector2 nacelle1 = vtolBase + new Vector2(180, -30);
        Vector2 nacelle2 = vtolBase + new Vector2(400, -10);

        DrawRect(new Rect2(nacelle1.X, nacelle1.Y, 50, 60), new Color(0.18f, 0.20f, 0.26f));
        DrawRect(new Rect2(nacelle2.X, nacelle2.Y, 50, 60), new Color(0.18f, 0.20f, 0.26f));

        float thrustPulse = 0.85f + 0.15f * Mathf.Sin(t * 12f);
        DrawLine(nacelle1 + new Vector2(25, 60), nacelle1 + new Vector2(25, 110 + 15f * thrustPulse), ColorKaito * thrustPulse, 6f);
        DrawLine(nacelle2 + new Vector2(25, 60), nacelle2 + new Vector2(25, 110 + 15f * thrustPulse), ColorKaito * thrustPulse, 6f);

        // Open Side Cargo Bay Door (x = 420 to 520, y = 90 to 190)
        DrawRect(new Rect2(vtolBase.X + 260, vtolBase.Y + 60, 220, 130), new Color(0.04f, 0.05f, 0.07f));

        // Yellow and black hazard warning stripes on ramp threshold
        for (int hx = 0; hx < 220; hx += 20)
        {
            Color stripeCol = ((hx / 20) % 2 == 0) ? new Color(0.9f, 0.8f, 0.1f) : new Color(0.1f, 0.1f, 0.1f);
            DrawRect(new Rect2(vtolBase.X + 260 + hx, vtolBase.Y + 182, 20, 8), stripeCol);
        }

        // Dangling magnetic grapple & harness cables reaching toward Gantry 42
        float cableSway = Mathf.Sin(t * 3.5f) * 12f;
        DrawLine(vtolBase + new Vector2(460, 180), new Vector2(980, 620 + cableSway), new Color(0.4f, 0.45f, 0.55f), 2f);
        DrawLine(vtolBase + new Vector2(400, 180), new Vector2(990, 630 - cableSway), new Color(0.35f, 0.40f, 0.50f), 2f);
    }

    private void DrawOperatives(float t, int shot)
    {
        // Operative silhouettes positioned at the threshold of the open cargo bay
        float hoverY = Mathf.Sin(t * 2f) * 6f;
        Vector2 bayPos = new(480, 480 + hoverY);

        // 1. Surya Prime (Bulwark Tank) - rear left
        DrawOperativeSilhouette(bayPos + new Vector2(30, 80), ColorSurya, shot == 1);

        // 2. Apex Titan (Juggernaut) - middle left
        DrawOperativeSilhouette(bayPos + new Vector2(65, 75), ColorTitan, shot == 3, 1.2f);

        // 3. Oceanus (Tactician) - center
        DrawOperativeSilhouette(bayPos + new Vector2(100, 80), ColorOceanus, shot == 4);

        // 4. Aura Phoenicia (Prism Weaver) - middle right
        DrawOperativeSilhouette(bayPos + new Vector2(135, 82), ColorAura, shot == 5);

        // 5. Mirage Blade (Assassin) - front threshold
        DrawOperativeSilhouette(bayPos + new Vector2(170, 78), ColorMirage, shot == 2 || shot == 7);

        // 6. Cyber Shogun (Kaito) - lead point at edge
        DrawOperativeSilhouette(bayPos + new Vector2(205, 76), ColorKaito, shot == 8, 1.05f);

        // Shot 8: Kaito drawing glowing katana energy trail
        if (shot == 8)
        {
            Vector2 kaitoHand = bayPos + new Vector2(215, 60);
            Vector2 bladeTip = kaitoHand + new Vector2(40, -45);
            DrawLine(kaitoHand, bladeTip, ColorKaito, 3f);
            DrawLine(kaitoHand, bladeTip, new Color(1f, 1f, 1f), 1.5f);
        }
    }

    private void DrawOperativeSilhouette(Vector2 pos, Color accent, bool highlighted, float scale = 1.0f)
    {
        float h = 48f * scale;
        float w = 16f * scale;

        // Dark operative silhouette body
        DrawRect(new Rect2(pos.X - w * 0.5f, pos.Y - h, w, h), new Color(0.08f, 0.09f, 0.12f));
        // Head
        DrawCircle(new Vector2(pos.X, pos.Y - h - 6f * scale), 7f * scale, new Color(0.08f, 0.09f, 0.12f));

        // Signature visor / energy accent
        float glowAlpha = highlighted ? 1.0f : 0.6f;
        DrawLine(
            new Vector2(pos.X - 3f * scale, pos.Y - h - 6f * scale),
            new Vector2(pos.X + 4f * scale, pos.Y - h - 6f * scale),
            accent * glowAlpha, 2f * scale
        );

        if (highlighted)
        {
            // Aura ring when speaking
            DrawArc(new Vector2(pos.X, pos.Y - h * 0.5f), 32f * scale, 0, Mathf.Tau, 16, accent * 0.5f, 1.5f);
        }
    }

    private void DrawScreenOverlays(Vector2 size, float t, int shot)
    {
        // 1. 2.39:1 Anamorphic cinema letterbox bars
        float targetAspect = 2.39f;
        float currentAspect = size.X / size.Y;
        if (currentAspect < targetAspect)
        {
            float targetHeight = size.X / targetAspect;
            float barHeight = (size.Y - targetHeight) * 0.5f;
            DrawRect(new Rect2(0, 0, size.X, barHeight), new Color(0, 0, 0));
            DrawRect(new Rect2(0, size.Y - barHeight, size.X, barHeight), new Color(0, 0, 0));
        }

        // 2. Tactical Infiltration HUD overlay (top left)
        Color hudCyan = new(0f, 0.94f, 1f, 0.85f);
        DrawLine(new Vector2(40, 50), new Vector2(420, 50), hudCyan, 1.5f);
        DrawLine(new Vector2(40, 50), new Vector2(40, 80), hudCyan, 1.5f);

        // 3. CRT Scanline overlay effect
        Color scanline = new(0f, 0f, 0f, 0.04f);
        for (int y = 0; y < (int)size.Y; y += 4)
        {
            DrawRect(new Rect2(0, y, size.X, 1.5f), scanline);
        }
    }
}
