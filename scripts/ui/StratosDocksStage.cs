using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Pre-Level 8 prologue ("Infiltration at Stratos Docks").
/// Renders high-altitude stormy night atmosphere, driving rain, mooring pylons with hazard beacons,
/// the massive silhouette of the heavy VTOL gunship moored in Bay 4, operative silhouettes on the gantry,
/// sweeping air-traffic searchlights, AXIOM-7 telemetry, and smooth Hermite camera transitions across nine authored beats.
/// </summary>
public partial class StratosDocksStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/stratos_docks_intro/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>Requires authored environment art and cue metadata; the owner supplies a safe completion fallback.</summary>
    public bool ResourcesAvailable => CampaignArtwork.Available("stratos_docks", ArtPath);

    // Operative color accents
    private static readonly Color ColorKaito = new("00f0ff");    // Cyan
    private static readonly Color ColorMirage = new("aa44ff");   // Phase Violet
    private static readonly Color ColorTitan = new("ffcc00");    // Kinetic Gold
    private static readonly Color ColorOceanus = new("00d4ff");  // Hydro Aqua
    private static readonly Color ColorAura = new("ff00aa");     // Ultrasonic Pink
    private static readonly Color ColorSurya = new("ffaa00");    // Solar Amber
    private static readonly Color ColorBeacon = new("ff2222");   // Aviation Beacon Red
    private static readonly Color ColorSearchlight = new(1f, 1f, 0.7f, 0.08f); // Soft searchlight beam

    // Shot schedule matching 9 cues (90 seconds total)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera targets and zooms for 9 shots
    private static readonly Vector2[] CameraTargets = {
        new(960, 540),  // Shot 0: Wide shot - storm over aviation hub & flight decks
        new(1320, 520), // Shot 1: Mirage scanning Bay 4 VTOL gunship
        new(720, 580),  // Shot 2: Titan studying heavy magnetic mooring titan-locks
        new(860, 560),  // Shot 3: Oceanus analyzing pressurized hydrogen lines
        new(1100, 480), // Shot 4: Aura tracking control tower radio traffic
        new(680, 540),  // Shot 5: Surya checking solar barrier against flak guns
        new(1240, 540), // Shot 6: Mirage tracing phase path through laser tripwires
        new(960, 420),  // Shot 7: AXIOM-7 upward Spire trajectory vector
        new(960, 540)   // Shot 8: Squad breach order ready tableau
    };

    private static readonly float[] CameraZooms = {
        1.0f,  // Shot 0: Wide
        1.15f, // Shot 1: Closer on Bay 4
        1.18f, // Shot 2: Titan focus
        1.16f, // Shot 3: Oceanus focus
        1.14f, // Shot 4: Aura focus
        1.15f, // Shot 5: Surya focus
        1.20f, // Shot 6: Laser tripwire focus
        1.10f, // Shot 7: Spire flight path
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
        camPos = CampaignArtwork.Focus(ArtPath, Seconds, camPos);
        float zoom = InterpolateCameraZoom(shot, tInShot);

        // Apply camera transformation centered on 1920x1080 design space
        float fit = Mathf.Min(viewport.X / 1920, viewport.Y / 1080);
        Vector2 margin = (viewport - new Vector2(1920, 1080) * fit) * .5f;
        DrawSetTransform(margin + (new Vector2(960, 540) - camPos * zoom) * fit, 0, Vector2.One * fit * zoom);

        // 1. Stormy Sky & Distant Storm Clouds
        DrawSkyAndClouds(Seconds);
        CampaignArtwork.DrawEnvironment(this, "stratos_docks", Seconds, aftermath: false, drawBase: true);

        // 2. Distant Citadel Spire in Storm Background
        DrawDistantSpireInStorm(Seconds);

        // 3. Aviation Hub Flight Decks & Mooring Pylons
        DrawFlightDecksAndPylons(Seconds);

        // 4. Heavy VTOL Gunship in Bay 4
        DrawVtolGunship(Seconds);

        // 5. High-Altitude Gantry Walkway
        DrawGantryWalkway(Seconds);

        // 6. Squad Silhouettes
        DrawSquad(Seconds, shot);

        // 7. Dynamic Weather: Driving Rain & Searchlights
        DrawSearchlights(Seconds);
        DrawDrivingRain(Seconds);

        // Reset transform for HUD & Letterboxing
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // 8. Tactical Telemetry Overlay
        DrawTelemetry(viewport, Seconds, shot);

        // 9. 2.39:1 Cinema Letterbox Bars
        DrawCinemaBars(viewport);
    }

    /// <summary>Draws turbulent stormy gradient with periodic lightning flash illumination.</summary>
    private void DrawSkyAndClouds(float time)
    {
        // Deep stormy midnight blue
        var skyRect = new Rect2(0, 0, 1920, 1080);
        DrawRect(skyRect, new Color(0.015f, 0.02f, 0.045f));

        // Periodic cloud lightning flash (around 12s, 34s, 56s, 77s)
        float flash = 0f;
        foreach (float fTime in new[] { 12.2f, 34.5f, 56.1f, 77.4f })
        {
            float dt = time - fTime;
            if (dt >= 0 && dt < 0.35f)
            {
                flash = Mathf.Max(flash, Mathf.Sin(dt / 0.35f * Mathf.Pi) * 0.22f);
            }
        }
        if (flash > 0f)
        {
            DrawRect(skyRect, new Color(0.4f, 0.6f, 0.9f, flash));
        }

        // Drifting storm cloud bands
        for (int i = 0; i < 6; i++)
        {
            float cloudY = 120 + i * 90;
            float scroll = (time * (25 + i * 15)) % 1920;
            var cloudColor = new Color(0.025f + i * 0.005f, 0.04f + i * 0.006f, 0.08f + i * 0.01f, 0.65f);
            DrawCircle(new Vector2(scroll, cloudY), 240 + i * 40, cloudColor);
            DrawCircle(new Vector2(scroll - 1920, cloudY), 240 + i * 40, cloudColor);
        }
    }

    /// <summary>Draws the massive dark silhouette of the unshielded Citadel Spire looming in the background.</summary>
    private void DrawDistantSpireInStorm(float time)
    {
        // Spire silhouette centered at X=960, reaching from Y=50 down to horizon
        Vector2[] spirePts = {
            new(960, 40),
            new(980, 200),
            new(1030, 450),
            new(1080, 800),
            new(840, 800),
            new(890, 450),
            new(940, 200)
        };
        DrawColoredPolygon(spirePts, new Color(0.01f, 0.015f, 0.028f));

        // Pulsing warning spire summit beacon
        float pulse = (Mathf.Sin(time * 3f) + 1f) * 0.5f;
        DrawCircle(new Vector2(960, 40), 5f + pulse * 4f, new Color(1f, 0.2f, 0.2f, 0.6f + pulse * 0.4f));

        // Faint cyber window lattices on Spire
        for (int y = 220; y < 650; y += 45)
        {
            DrawLine(new Vector2(945, y), new Vector2(975, y), new Color(0.0f, 0.9f, 1.0f, 0.15f), 1.5f);
        }
    }

    /// <summary>Draws aviation control towers, flight deck gantries, and mooring pylons with red beacons.</summary>
    private void DrawFlightDecksAndPylons(float time)
    {
        // Far background hangar structure
        DrawRect(new Rect2(100, 380, 1720, 400), new Color(0.02f, 0.03f, 0.05f));

        // Control Tower on the left (X: 180 - 320)
        Vector2[] towerPts = {
            new(200, 260),
            new(300, 260),
            new(320, 600),
            new(180, 600)
        };
        DrawColoredPolygon(towerPts, new Color(0.03f, 0.045f, 0.07f));
        // Control tower panoramic windows (amber tint)
        DrawRect(new Rect2(205, 275, 90, 22), new Color(1f, 0.7f, 0.2f, 0.35f));

        // Mooring Pylon 1 (left of Bay 4)
        DrawMooringPylon(new Vector2(1100, 360), 320, time, 0);

        // Mooring Pylon 2 (right of Bay 4)
        DrawMooringPylon(new Vector2(1650, 340), 340, time, 1);
    }

    /// <summary>Draws a heavy industrial mooring pylon with cables and flashing aviation beacon.</summary>
    private void DrawMooringPylon(Vector2 top, float height, float time, int offset)
    {
        // Pylon mast
        DrawRect(new Rect2(top.X - 12, top.Y, 24, height), new Color(0.04f, 0.055f, 0.08f));
        DrawRect(new Rect2(top.X - 25, top.Y + 40, 50, 14), new Color(0.06f, 0.08f, 0.12f));

        // Red flashing aviation beacon
        float flash = (Mathf.Sin(time * 4f + offset * 2f) > 0.3f) ? 1.0f : 0.15f;
        DrawCircle(new Vector2(top.X, top.Y - 6), 6f, new Color(ColorBeacon.R, ColorBeacon.G, ColorBeacon.B, flash));

        // Fueling conduit line extending from pylon toward Bay 4
        DrawLine(new Vector2(top.X, top.Y + 80), new Vector2(top.X - 120, top.Y + 160), new Color(0.1f, 0.6f, 0.8f, 0.4f), 3f);
    }

    /// <summary>Draws the massive heavy VTOL gunship silhouette moored in Bay 4 with engine vents.</summary>
    private void DrawVtolGunship(float time)
    {
        // Bay 4 Landing Pad
        DrawRect(new Rect2(1120, 560, 520, 28), new Color(0.05f, 0.07f, 0.1f));
        // Yellow safety edge stripes
        for (int x = 1140; x < 1620; x += 40)
        {
            DrawLine(new Vector2(x, 560), new Vector2(x + 15, 588), new Color(1f, 0.8f, 0.1f, 0.7f), 3f);
        }

        // Heavy VTOL Hull Silhouette
        Vector2[] hullPts = {
            new(1180, 520), // Nose cockpit
            new(1250, 470), // Forward dorsal
            new(1480, 460), // Mid fuselage
            new(1580, 420), // Tail boom up
            new(1640, 410), // Tail fin
            new(1650, 480), // Lower tail
            new(1520, 540), // Lower aft
            new(1240, 545)  // Lower nose chin
        };
        DrawColoredPolygon(hullPts, new Color(0.025f, 0.035f, 0.055f));

        // Cockpit canopy glow (Cyan HUD internal glow)
        Vector2[] cockpitPts = {
            new(1200, 515),
            new(1245, 480),
            new(1280, 485),
            new(1230, 520)
        };
        DrawColoredPolygon(cockpitPts, new Color(0.0f, 0.9f, 1.0f, 0.25f));

        // Heavy Left Tilt-Rotor Engine (idling with glowing ionization)
        DrawRect(new Rect2(1330, 450, 60, 36), new Color(0.04f, 0.06f, 0.09f));
        float enginePulse = (Mathf.Sin(time * 12f) + 1f) * 0.5f;
        DrawCircle(new Vector2(1360, 468), 16f, new Color(0.0f, 0.8f, 1.0f, 0.4f + enginePulse * 0.35f));

        // Heavy Right Tilt-Rotor Engine
        DrawRect(new Rect2(1470, 440, 60, 36), new Color(0.04f, 0.06f, 0.09f));
        DrawCircle(new Vector2(1500, 458), 16f, new Color(0.0f, 0.8f, 1.0f, 0.4f + enginePulse * 0.35f));

        // Green wingtip navigation beacon
        DrawCircle(new Vector2(1645, 412), 4f, new Color(0.1f, 1.0f, 0.3f, 0.85f));
    }

    /// <summary>Draws high steel gantry walkway where the squad is stationed.</summary>
    private void DrawGantryWalkway(float time)
    {
        // Gantry platform surface (Y: 680 - 740 across the screen)
        DrawRect(new Rect2(0, 680, 1920, 30), new Color(0.05f, 0.065f, 0.09f));
        // Steel grating texture pattern
        for (int x = 20; x < 1900; x += 25)
        {
            DrawLine(new Vector2(x, 680), new Vector2(x + 10, 710), new Color(0.08f, 0.12f, 0.16f), 1.5f);
        }
        // Safety handrail
        DrawLine(new Vector2(0, 650), new Vector2(1920, 650), new Color(0.12f, 0.16f, 0.22f), 2.5f);
        for (int post = 50; post < 1920; post += 120)
        {
            DrawLine(new Vector2(post, 650), new Vector2(post, 680), new Color(0.12f, 0.16f, 0.22f), 2f);
        }
    }

    /// <summary>Draws the 6 operatives gathered on the gantry overlooking Bay 4.</summary>
    private void DrawSquad(float time, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Draws a humanoid operative silhouette with energy ability rim glow.</summary>
    private void DrawOperativeSilhouette(Vector2 pos, Color energy, float scale, string tag)
    {
        float h = 60 * scale;
        float w = 18 * scale;

        // Torso & legs
        DrawRect(new Rect2(pos.X - w * 0.5f, pos.Y - h, w, h), new Color(0.02f, 0.03f, 0.05f));
        // Head
        DrawCircle(new Vector2(pos.X, pos.Y - h - 8 * scale), 8 * scale, new Color(0.02f, 0.03f, 0.05f));

        // Distinctive energy glow contour
        DrawLine(new Vector2(pos.X - w * 0.5f, pos.Y), new Vector2(pos.X - w * 0.5f, pos.Y - h), energy, 1.5f);
        DrawLine(new Vector2(pos.X + w * 0.5f, pos.Y), new Vector2(pos.X + w * 0.5f, pos.Y - h), energy, 1.5f);
        DrawCircle(new Vector2(pos.X, pos.Y - h - 8 * scale), 9 * scale, new Color(energy.R, energy.G, energy.B, 0.35f));
    }

    /// <summary>Renders sweeping airport security searchlights across the dark sky.</summary>
    private void DrawSearchlights(float time)
    {
        // Searchlight 1 from control tower
        float angle1 = Mathf.Sin(time * 0.4f) * 0.35f + 0.3f;
        DrawSearchlightCone(new Vector2(250, 270), angle1, 1400);

        // Searchlight 2 from right hangar
        float angle2 = -Mathf.Sin(time * 0.35f + 1.2f) * 0.4f - 0.2f;
        DrawSearchlightCone(new Vector2(1750, 360), angle2, 1300);
    }

    /// <summary>Draws a soft translucent searchlight beam polygon.</summary>
    private void DrawSearchlightCone(Vector2 origin, float angle, float length)
    {
        Vector2 dir = Vector2.FromAngle(angle);
        Vector2 normal = new(-dir.Y, dir.X);
        float width = 110f;

        Vector2[] pts = {
            origin,
            origin + dir * length - normal * width,
            origin + dir * length + normal * width
        };
        DrawColoredPolygon(pts, ColorSearchlight);
    }

    /// <summary>Draws angled driving rain lines matching high-wind conditions.</summary>
    private void DrawDrivingRain(float time)
    {
        var rainColor = new Color(0.7f, 0.85f, 1.0f, 0.18f);
        for (int i = 0; i < 90; i++)
        {
            float seed = i * 47.13f;
            float rx = (seed * 19.3f + time * 650f) % 1920;
            float ry = (seed * 31.7f + time * 1200f) % 1080;
            Vector2 p1 = new(rx, ry);
            Vector2 p2 = p1 + new Vector2(18, 38); // Angled down-right
            DrawLine(p1, p2, rainColor, 1.2f);
        }
    }

    /// <summary>Draws AXIOM-7 tactical telemetry overlay with target bracket on Bay 4.</summary>
    private void DrawTelemetry(Vector2 vp, float time, int shot)
    {
        // Target bracket over VTOL Gunship (around X: 1150-1650, Y: 430-560)
        float bracketAlpha = 0.5f + Mathf.Sin(time * 3f) * 0.3f;
        var bracketColor = new Color(0.0f, 0.94f, 1.0f, bracketAlpha);

        // Corner brackets on Bay 4
        DrawLine(new Vector2(1150, 440), new Vector2(1180, 440), bracketColor, 2f);
        DrawLine(new Vector2(1150, 440), new Vector2(1150, 470), bracketColor, 2f);

        DrawLine(new Vector2(1650, 440), new Vector2(1620, 440), bracketColor, 2f);
        DrawLine(new Vector2(1650, 440), new Vector2(1650, 470), bracketColor, 2f);

        DrawLine(new Vector2(1150, 560), new Vector2(1180, 560), bracketColor, 2f);
        DrawLine(new Vector2(1150, 560), new Vector2(1150, 530), bracketColor, 2f);

        DrawLine(new Vector2(1650, 560), new Vector2(1620, 560), bracketColor, 2f);
        DrawLine(new Vector2(1650, 560), new Vector2(1650, 530), bracketColor, 2f);

        // Header Telemetry text bar at top left
        DrawRect(new Rect2(30, 30, 360, 48), new Color(0.01f, 0.03f, 0.06f, 0.85f));
        DrawRect(new Rect2(30, 30, 360, 48), new Color("00f0ff"), false, 1.5f);
        DrawString(ThemeDB.FallbackFont, new Vector2(44, 52), WildfireText.Get("UI_STRATOSDOCKSSTAGE_DRAWTELEMETRY_FA0D0B4C", "STRATOS DOCKS // AVIATION SECTOR 08"), HorizontalAlignment.Left, -1, 11, new Color("00f0ff"));
        DrawString(ThemeDB.FallbackFont, new Vector2(44, 68), WildfireText.Interpolate("UI_STRATOSDOCKSSTAGE_DRAWTELEMETRY_2DC8E301", $"ALTITUDE: 1,240M  |  WIND: 48KT  |  LOCKDOWN: ACTIVE"), HorizontalAlignment.Left, -1, 10, Colors.White);
    }

    /// <summary>Draws 2.39:1 anamorphic cinema bars.</summary>
    private void DrawCinemaBars(Vector2 vp)
    {
        float targetRatio = 2.39f;
        float currentRatio = vp.X / Mathf.Max(vp.Y, 1f);
        if (currentRatio > targetRatio)
        {
            float w = (vp.X - vp.Y * targetRatio) * 0.5f;
            DrawRect(new Rect2(0, 0, w, vp.Y), Colors.Black);
            DrawRect(new Rect2(vp.X - w, 0, w, vp.Y), Colors.Black);
        }
        else
        {
            float h = (vp.Y - vp.X / targetRatio) * 0.5f;
            DrawRect(new Rect2(0, 0, vp.X, h), Colors.Black);
            DrawRect(new Rect2(0, vp.Y - h, vp.X, h), Colors.Black);
        }
    }

    private int GetCurrentShot(float time)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (time >= ShotTimes[i] && time < ShotTimes[i + 1]) return i;
        }
        return ShotTimes.Length - 2;
    }

    private float GetShotProgress(float time, int shot)
    {
        float start = ShotTimes[shot];
        float end = ShotTimes[shot + 1];
        return Mathf.Clamp((time - start) / (end - start), 0f, 1f);
    }

    private Vector2 InterpolateCameraPosition(int shot, float t)
    {
        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        Vector2 cur = CameraTargets[shot];
        Vector2 next = (shot < CameraTargets.Length - 1) ? CameraTargets[shot + 1] : cur;
        return cur.Lerp(next, smoothT * 0.3f);
    }

    private float InterpolateCameraZoom(int shot, float t)
    {
        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        float cur = CameraZooms[shot];
        float next = (shot < CameraZooms.Length - 1) ? CameraZooms[shot + 1] : cur;
        return Mathf.Lerp(cur, next, smoothT * 0.3f);
    }
}
