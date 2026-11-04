using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 8 post-mission epilogue ("Airborne Ascension").
/// Composes the hijacked military VTOL gunship taking flight, receding Stratos Docks floating gantries,
/// the storm cloud ceiling breaking into twilight skies, the monolithic Citadel Spire Outer Hull looming ahead,
/// flak artillery tracer arcs, operative silhouettes, and smooth camera choreography across nine authored beats.
/// </summary>
public partial class StratosDocksAftermathStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/stratos_docks_aftermath/";

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
    private static readonly Color ColorBeacon = new("ff2222");   // Aviation Red
    private static readonly Color ColorVtolArmor = new("1e293b"); // Deep Gunship Slate
    private static readonly Color ColorSpireHull = new("0f172a"); // Spire Monolith Obsidian

    // Shot boundary schedule matching StratosDocksAftermath dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - VTOL liftoff from Bay 4
        new(1380, 560), // Shot 1: Looking back at receding docks & beacon pylons
        new(720, 560),  // Shot 2: Titan admiring heavy reinforced gunship armor
        new(620, 580),  // Shot 3: Oceanus looking down through cloud breaks at liberated city
        new(1160, 540), // Shot 4: Aura monitoring Citadel distress broadcasts
        new(960, 420),  // Shot 5: Breaking cloud ceiling; Spire outer hull looms ahead
        new(960, 440),  // Shot 6: AXIOM-7 telemetry - anti-air flak grid detecting radar
        new(880, 500),  // Shot 7: Mirage identifying lower exterior catwalk entry
        new(960, 520)   // Shot 8: Epic squad wide shot; preparing for Spire hull drop
    };

    private static readonly float[] CameraZooms = {
        1.02f, // Shot 0
        1.16f, // Shot 1
        1.18f, // Shot 2
        1.18f, // Shot 3
        1.16f, // Shot 4
        1.14f, // Shot 5
        1.18f, // Shot 6
        1.14f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes high-altitude skies, clouds, receding docks, heavy VTOL gunship, Citadel Spire outer hull,
    /// operative silhouettes, and cinema letterbox analytically.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep night / storm sky backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("050814"));

        float t = Mathf.Clamp(Seconds, 0, 90f);
        int shot = GetCurrentShot(t);

        // Aspect fit to standard 1920x1080 canvas
        float fit = Mathf.Min(Size.X / 1920f, Size.Y / 1080f);
        Vector2 margin = (Size - new Vector2(1920f, 1080f) * fit) * 0.5f;

        Vector2 camPos = GetSmoothCameraPosition(t, shot);
        float camZoom = GetSmoothCameraZoom(t, shot);

        // 2. Camera-zoomed cinematic world layer
        DrawSetTransform(margin + (new Vector2(960f, 540f) - camPos * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // Atmospheric storm background and twilight horizon
        DrawSkyAndClouds(t);

        // Receding Stratos Docks floating gantries & beacon towers
        DrawRecedingDocks(t);

        // Citadel Spire Outer Hull towering in the background
        DrawCitadelSpireHull(t);

        // Heavy military VTOL gunship in flight
        DrawVtolGunship(t);

        // Operative silhouettes inside gunship hold
        DrawSquadSilhouettes(t, shot);

        // Holographic tactical radar and telemetry
        DrawTacticalTelemetry(t, shot);

        // 3. Screen-space overlays (cinema letterbox and scanlines)
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawCinemaBorders(fit, margin);
        DrawScanlines();
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
        float shotEnd = ShotTimes[shot + 1];
        float progress = Mathf.Clamp((t - shotStart) / (shotEnd - shotStart), 0f, 1f);
        float ease = progress * progress * (3f - 2f * progress);

        Vector2 current = CameraTargets[shot];
        Vector2 next = shot + 1 < CameraTargets.Length ? CameraTargets[shot + 1] : current;

        // Slight drift across shot
        Vector2 drift = new Vector2(Mathf.Sin(t * 0.35f) * 18f, Mathf.Cos(t * 0.28f) * 12f);
        return current.Lerp(next, ease * 0.25f) + drift;
    }

    private static float GetSmoothCameraZoom(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotEnd = ShotTimes[shot + 1];
        float progress = Mathf.Clamp((t - shotStart) / (shotEnd - shotStart), 0f, 1f);
        float ease = progress * progress * (3f - 2f * progress);

        float current = CameraZooms[shot];
        float next = shot + 1 < CameraZooms.Length ? CameraZooms[shot + 1] : current;
        return Mathf.Lerp(current, next, ease * 0.18f);
    }

    private void DrawSkyAndClouds(float t)
    {
        // Twilight sky gradient
        for (int i = 0; i < 18; i++)
        {
            float y = i * 60f;
            float ratio = (float)i / 18f;
            Color skyCol = new Color("040716").Lerp(new Color("24122d"), ratio * 0.7f);
            DrawRect(new Rect2(0, y, 1920, 65), skyCol);
        }

        // Billowing volumetric cloud banks
        for (int i = 0; i < 7; i++)
        {
            float cx = ((i * 320f - t * 45f) % 2400f);
            if (cx < -200f) cx += 2400f;
            float cy = 720f + Mathf.Sin(t * 0.6f + i) * 35f;
            float rad = 180f + (i % 3) * 60f;
            DrawCircle(new Vector2(cx, cy), rad, new Color(0.12f, 0.16f, 0.25f, 0.45f));
        }

        // Distant lightning flashes
        if (Mathf.Sin(t * 4.2f) > 0.88f)
        {
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0.3f, 0.6f, 0.9f, 0.12f));
        }
    }

    private void DrawRecedingDocks(float t)
    {
        // Distant floating gantry silhouettes receding into background
        float dockOffset = t * 35f;
        float dockX = 350f - dockOffset * 0.4f;

        if (dockX > -400f)
        {
            // Mooring pylon towers
            DrawRect(new Rect2(dockX, 580, 45, 380), new Color("090d18"));
            DrawRect(new Rect2(dockX + 160, 620, 35, 340), new Color("090d18"));
            DrawRect(new Rect2(dockX - 80, 640, 320, 24), new Color("0e1424"));

            // Flashing aviation red beacons
            if (Mathf.Sin(t * 5.0f) > 0.0f)
            {
                DrawCircle(new Vector2(dockX + 22, 575), 5f, ColorBeacon);
                DrawCircle(new Vector2(dockX + 177, 615), 4f, ColorBeacon);
            }
        }
    }

    private void DrawCitadelSpireHull(float t)
    {
        // Colossal Citadel Spire Outer Hull looming ahead on right
        float spireX = 1420f;
        Vector2[] spirePolygon = {
            new(spireX, -100),
            new(1920, -100),
            new(1920, 1080),
            new(spireX - 120, 1080),
            new(spireX - 80, 450)
        };
        DrawPolygon(spirePolygon, new[] { ColorSpireHull });

        // Armor plating seams and glowing energy conduits
        for (int y = 50; y < 1000; y += 140)
        {
            DrawLine(new Vector2(spireX - 60, y), new Vector2(1920, y + 20), new Color(0.2f, 0.35f, 0.5f, 0.6f), 2.5f);
            DrawLine(new Vector2(spireX - 50, y + 25), new Vector2(spireX - 30, y + 115), ColorCyan * 0.7f, 1.8f);
        }

        // Distant automated flak defense tracer fire arcing into clouds
        float flakTime = t * 1.8f;
        for (int f = 0; f < 3; f++)
        {
            float seed = f * 2.3f;
            float tracerT = Mathf.PosMod(flakTime + seed, 2.5f) / 2.5f;
            Vector2 origin = new(spireX - 40, 300 + f * 180);
            Vector2 target = origin + new Vector2(-480, -220).Rotated(Mathf.Sin(seed) * 0.4f);
            Vector2 head = origin.Lerp(target, tracerT);
            Vector2 tail = origin.Lerp(target, Mathf.Max(0, tracerT - 0.15f));
            DrawLine(tail, head, new Color(1f, 0.4f, 0.1f, 0.85f), 2.5f);
        }
    }

    private void DrawVtolGunship(float t)
    {
        // Heavy military VTOL gunship in center-left, banking forward
        Vector2 center = new(840 + Mathf.Sin(t * 0.8f) * 12f, 520 + Mathf.Cos(t * 1.1f) * 10f);
        float bankAngle = -0.08f + Mathf.Sin(t * 0.5f) * 0.04f;

        DrawSetTransform(center, bankAngle, Vector2.One);

        // Fuselage body polygon
        Vector2[] fuselage = {
            new(190, 15),
            new(130, -35),
            new(-120, -45),
            new(-240, -15),
            new(-260, 20),
            new(-160, 55),
            new(110, 50)
        };
        DrawPolygon(fuselage, new[] { ColorVtolArmor });

        // Cockpit canopy window
        Vector2[] canopy = {
            new(140, -18),
            new(175, 10),
            new(110, 15),
            new(90, -18)
        };
        DrawPolygon(canopy, new[] { new Color(0.1f, 0.75f, 0.95f, 0.85f) });

        // Wings and tilt-rotor engine nacelles
        DrawRect(new Rect2(-70, -85, 45, 170), new Color("141b2d"));

        // Left tilt-rotor thrust ionization plume
        DrawCircle(new Vector2(-48, -90), 22f, new Color(0.1f, 0.85f, 1f, 0.5f));
        DrawLine(new Vector2(-48, -90), new Vector2(-120, -90), ColorCyan, 6f);

        // Right tilt-rotor thrust ionization plume
        DrawCircle(new Vector2(-48, 90), 22f, new Color(0.1f, 0.85f, 1f, 0.5f));
        DrawLine(new Vector2(-48, 90), new Vector2(-120, 90), ColorCyan, 6f);

        // Main rear thruster exhaust plume
        float plumeFlicker = 1.0f + Mathf.Sin(t * 18f) * 0.15f;
        Vector2[] plume = {
            new(-260, -5),
            new(-260, 15),
            new(-360 * plumeFlicker, 5)
        };
        DrawPolygon(plume, new[] { new Color(0.2f, 0.8f, 1.0f, 0.85f) });

        // Reset transform back to camera world
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void DrawSquadSilhouettes(float t, int shot)
    {
        // Operative silhouettes inside gunship hold / cockpit
        Vector2 basePos = new(760, 520);
        DrawCircle(basePos + new Vector2(40, -8), 8f, ColorKaito);      // Kaito
        DrawCircle(basePos + new Vector2(10, -6), 7f, ColorMirage);     // Mirage
        DrawCircle(basePos + new Vector2(-20, -4), 8f, ColorTitan);     // Titan
        DrawCircle(basePos + new Vector2(-50, -6), 7f, ColorOceanus);   // Oceanus
        DrawCircle(basePos + new Vector2(-80, -6), 7f, ColorAura);      // Aura
        DrawCircle(basePos + new Vector2(-110, -5), 8f, ColorSurya);    // Surya
    }

    private void DrawTacticalTelemetry(float t, int shot)
    {
        // Holographic flight HUD in top-left
        Vector2 hudPos = new(180, 140);
        DrawRect(new Rect2(hudPos, new Vector2(260, 100)), new Color(0.02f, 0.08f, 0.15f, 0.65f));
        DrawRect(new Rect2(hudPos, new Vector2(260, 100)), ColorCyan * 0.8f, false, 1.5f);

        // Radar blip circle
        DrawCircle(hudPos + new Vector2(50, 50), 32f, new Color(0.0f, 0.3f, 0.4f, 0.5f));
        DrawCircle(hudPos + new Vector2(50, 50), 32f, ColorCyan * 0.7f, false, 1.2f);
        DrawLine(hudPos + new Vector2(50, 50), hudPos + new Vector2(50, 50) + Vector2.Right.Rotated(t * 3f) * 30f, ColorCyan, 1.5f);

        // Telemetry lines
        DrawLine(hudPos + new Vector2(100, 35), hudPos + new Vector2(240, 35), ColorCyan * 0.6f, 1.5f);
        DrawLine(hudPos + new Vector2(100, 55), hudPos + new Vector2(220, 55), ColorCyan * 0.6f, 1.5f);
        DrawLine(hudPos + new Vector2(100, 75), hudPos + new Vector2(200, 75), ColorAmber * 0.7f, 1.5f);
    }

    private void DrawCinemaBorders(float fit, Vector2 margin)
    {
        // 2.39:1 anamorphic cinema bars
        float cinemaHeight = 1920f / 2.39f * fit;
        float barHeight = (Size.Y - cinemaHeight) * 0.5f;
        if (barHeight > 0)
        {
            DrawRect(new Rect2(0, 0, Size.X, barHeight), Colors.Black);
            DrawRect(new Rect2(0, Size.Y - barHeight, Size.X, barHeight), Colors.Black);
        }
    }

    private void DrawScanlines()
    {
        // Subtle CRT scanline overlay
        Color scanlineColor = new(0, 0, 0, 0.07f);
        for (float y = 0; y < Size.Y; y += 4f)
        {
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), scanlineColor, 1.5f);
        }
    }
}
