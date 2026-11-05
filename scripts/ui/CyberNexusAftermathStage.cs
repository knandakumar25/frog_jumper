using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 10 post-mission epilogue ("The Logic Collapse").
/// Composes the crumbling Cyber-Nexus mainframe sanctum, the shattered smoking husk of the defeated Nexus Overseer,
/// ruptured liquid helium coolant conduits venting freezing plumes, flashing red core-meltdown error cascades,
/// the colossal vertical Apex Ascent Lift powering up, and the squad preparing to assault the Apex Bastion.
/// </summary>
public partial class CyberNexusAftermathStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/cyber_nexus_aftermath/";

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
    private static readonly Color ColorAmber = new("fbbf24");    // Hazard Amber
    private static readonly Color ColorAlert = new("ff2255");    // Critical Red
    private static readonly Color ColorMist = new("a5f3fc");     // Cryo Steam
    private static readonly Color ColorDeck = new("0f172a");     // Gantry Titanium
    private static readonly Color ColorSteel = new("1e293b");    // Catwalk Steel

    // Shot boundary schedule matching CyberNexusAftermath dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - mainframe sanctum, smoldering Overseer, broken prisms
        new(620, 500),  // Shot 1: Mirage observing server towers dumping petabytes of encrypted garbage
        new(1180, 580), // Shot 2: Titan near smoldering Overseer boss wreck & shattered prism ring
        new(740, 560),  // Shot 3: Oceanus tracking venting helium manifold and thermal runaway
        new(1040, 520), // Shot 4: Surya looking up gantry shaft toward emergency auxiliary generators
        new(880, 540),  // Shot 5: Aura listening to heavy Praetorian footsteps on the ramparts
        new(1380, 520), // Shot 6: Mirage cracking the Apex Ascent Lift lockout; hydraulic gates unseal
        new(1440, 480), // Shot 7: AXIOM-7 telemetry warning; red spotlights piercing shaft from above
        new(1020, 540)  // Shot 8: Epic squad wide shot; stepping onto the illuminated Apex Ascent Lift
    };

    private static readonly float[] CameraZooms = {
        1.00f, // Shot 0
        1.15f, // Shot 1
        1.18f, // Shot 2
        1.12f, // Shot 3
        1.14f, // Shot 4
        1.16f, // Shot 5
        1.20f, // Shot 6
        1.15f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes the crumbling mainframe cavern, glitching server monoliths, smoldering Overseer mechanoid,
    /// venting cryogenic helium mist, the powering-up Apex Ascent Lift, and operative silhouettes analytically.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep midnight cybernetic backdrop
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

        // Mainframe cyber-chasm and perspective digital grid
        DrawMainframeBackdrop(t);
        CampaignArtwork.DrawEnvironment(this, "cyber_nexus", Seconds, aftermath: true, drawBase: true);

        // Server towers with flashing core meltdown error banners
        DrawGlitchingServerMonoliths(t);

        // Shattered smoking husk of the Nexus Overseer boss mechanoid
        DrawSmolderingOverseerHusk(t);

        // Ruptured coolant lines venting freezing white/cyan mist plumes
        DrawCryoHeliumMist(t);

        // Vertical Apex Ascent Lift gantry and shaft
        DrawApexAscentLift(t);

        // Ominous silhouette of the Apex Bastion high in the shaft
        DrawBastionSilhouettes(t);

        // Operative silhouettes standing on the gantry deck
        DrawSquadSilhouettes(t, shot);

        // 3. Reset transform for static cinematic screen overlay
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // CRT scanlines overlay
        DrawScanlines();

        // 2.39:1 Anamorphic cinema letterbox bars
        DrawCinemaBars(margin, fit);
    }

    /// <summary>Draws deep digital chasm background, perspective grid lines, and ambient circuit pulses.</summary>
    private void DrawMainframeBackdrop(float t)
    {
        // Gradient fill representing cavernous depth
        for (int i = 0; i < 18; i++)
        {
            float y1 = i * 60f;
            float ratio = i / 18f;
            Color band = new Color("02040c").Lerp(new Color("08102a"), ratio);
            DrawRect(new Rect2(0, y1, 1920, 60), band);
        }

        // Perspective grid lines on floor (Y = 620 to 1080)
        Color gridColor = new Color(0f, 0.94f, 1f, 0.08f);
        Color redPulseColor = new Color(1f, 0.15f, 0.35f, 0.12f * Mathf.Abs(Mathf.Sin(t * 3f)));

        for (int x = -400; x <= 2320; x += 120)
        {
            float topX = 960f + (x - 960f) * 0.25f;
            DrawLine(new Vector2(topX, 620), new Vector2(x, 1080), gridColor, 1.2f);
        }
        for (int y = 620; y <= 1080; y += 45)
        {
            float depth = (y - 620f) / 460f;
            float alpha = 0.04f + depth * 0.12f;
            DrawLine(new Vector2(0, y), new Vector2(1920, y), new Color(0f, 0.94f, 1f, alpha), 1.2f);
        }

        // Emergency flashing alarm wash
        float alarmFlash = Mathf.Abs(Mathf.Sin(t * 2.5f));
        if (alarmFlash > 0.6f)
        {
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(1f, 0.1f, 0.2f, (alarmFlash - 0.6f) * 0.15f));
        }
    }

    /// <summary>Draws towering server racks displaying critical failure banners and red data streams.</summary>
    private void DrawGlitchingServerMonoliths(float t)
    {
        // Left server towers (x = 80, 240, 420)
        float[] rackX = { 60f, 220f, 380f, 1680f, 1840f };
        for (int r = 0; r < rackX.Length; r++)
        {
            float rx = rackX[r];
            float rw = 120f;
            DrawRect(new Rect2(rx, 100, rw, 580), new Color("0a0f1e"));
            DrawRect(new Rect2(rx + 6, 110, rw - 12, 560), new Color("040812"));

            // Server slot shelves
            for (int s = 0; s < 14; s++)
            {
                float sy = 120f + s * 38f;
                DrawRect(new Rect2(rx + 10, sy, rw - 20, 30), new Color("0d172b"));

                // Flashing server LEDs (normally cyan/green, now flashing critical amber/red)
                for (int led = 0; led < 5; led++)
                {
                    float lx = rx + 16 + led * 18;
                    bool glitch = Mathf.Sin(t * 7f + r * 3 + s + led) > 0.2f;
                    Color ledCol = glitch ? ColorAlert : ColorAmber;
                    DrawCircle(new Vector2(lx, sy + 15), 2.5f, ledCol);
                }
            }

            // Pulsing error banner across tower face
            float bannerY = 220f + ((t * 40f + r * 60f) % 360f);
            DrawRect(new Rect2(rx + 8, bannerY, rw - 16, 18), new Color(1f, 0.15f, 0.3f, 0.35f));
        }

        // Falling corrupted data streams (red / amber characters)
        for (int c = 0; c < 24; c++)
        {
            float cx = 120f + c * 75f;
            float speed = 180f + (c * 23f % 120f);
            float cy = ((t * speed + c * 90f) % 600f) + 100f;
            float trailLen = 40f + (c % 4) * 15f;

            Color col = (c % 3 == 0) ? new Color(1f, 0.2f, 0.4f, 0.6f) : new Color(1f, 0.7f, 0.2f, 0.5f);
            DrawLine(new Vector2(cx, cy), new Vector2(cx, cy - trailLen), col, 1.8f);
            DrawCircle(new Vector2(cx, cy), 2.4f, Colors.White);
        }
    }

    /// <summary>Draws the shattered, sparking chassis of the defeated Nexus Overseer boss mechanoid.</summary>
    private void DrawSmolderingOverseerHusk(float t)
    {
        Vector2 bossPos = new(1180, 680);

        // Ruptured hexagonal armor shell lying tilted on the gantry
        Vector2[] hullPoints = {
            bossPos + new Vector2(-60, -25),
            bossPos + new Vector2(-20, -55),
            bossPos + new Vector2(45, -45),
            bossPos + new Vector2(70, 10),
            bossPos + new Vector2(30, 45),
            bossPos + new Vector2(-45, 35)
        };
        DrawColoredPolygon(hullPoints, new Color("1e293b"));
        DrawPolyline(hullPoints, new Color("334155"), 2.5f);

        // Cracked central core (faint flickering cyan/magenta spark)
        float coreFlicker = Mathf.Abs(Mathf.Sin(t * 8f));
        Color coreCol = ColorAlert.Lerp(ColorCyan, coreFlicker * 0.5f);
        DrawCircle(bossPos + new Vector2(0, -5), 14f, new Color("0a0f1e"));
        DrawCircle(bossPos + new Vector2(0, -5), 8f, coreCol);

        // Fractured orbital containment prism shards scattered around the wreckage
        Vector2[] shardOffsets = {
            new(-95, 20), new(-75, 45), new(60, 40), new(95, 15), new(120, 30), new(-30, 50)
        };
        for (int i = 0; i < shardOffsets.Length; i++)
        {
            Vector2 sp = bossPos + shardOffsets[i];
            float rot = i * 1.1f + t * 0.2f;
            Vector2 p1 = sp + new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)) * 12f;
            Vector2 p2 = sp + new Vector2(Mathf.Cos(rot + 2.2f), Mathf.Sin(rot + 2.2f)) * 10f;
            Vector2 p3 = sp + new Vector2(Mathf.Cos(rot + 4.1f), Mathf.Sin(rot + 4.1f)) * 11f;

            DrawColoredPolygon(new[] { p1, p2, p3 }, new Color(0f, 0.94f, 1f, 0.35f));
            DrawPolyline(new[] { p1, p2, p3, p1 }, ColorCyan, 1.4f);
        }

        // Severed conduit cables with electric arc discharges
        Vector2 cableStart = bossPos + new Vector2(-40, 20);
        Vector2 cableEnd = bossPos + new Vector2(-110, 40);
        DrawLine(cableStart, bossPos + new Vector2(-70, 35), new Color("475569"), 3f);
        DrawLine(bossPos + new Vector2(-70, 35), cableEnd, new Color("475569"), 2f);

        // Electric spark arcs firing intermittently
        if (Mathf.Sin(t * 11f) > 0.4f)
        {
            Vector2 sparkPos = bossPos + new Vector2(Mathf.Sin(t * 15f) * 35f, Mathf.Cos(t * 12f) * 20f);
            DrawLine(sparkPos, sparkPos + new Vector2(12, -8), ColorCyan, 2f);
            DrawLine(sparkPos + new Vector2(12, -8), sparkPos + new Vector2(18, -4), Colors.White, 1.5f);
            DrawCircle(sparkPos, 3f, Colors.White);
        }

        // Rising smoke plume
        for (int sm = 0; sm < 6; sm++)
        {
            float prog = ((t * 0.8f + sm * 0.25f) % 1.5f) / 1.5f;
            float sx = bossPos.X - 10f + Mathf.Sin(t * 2f + sm) * 20f * prog;
            float sy = bossPos.Y - 20f - prog * 110f;
            float rad = 10f + prog * 28f;
            float alpha = (1f - prog) * 0.22f;
            DrawCircle(new Vector2(sx, sy), rad, new Color(0.3f, 0.35f, 0.45f, alpha));
        }
    }

    /// <summary>Draws billowing cryogenic liquid helium steam venting from ruptured floor pipes.</summary>
    private void DrawCryoHeliumMist(float t)
    {
        float[] ventX = { 460f, 820f, 1380f };
        for (int v = 0; v < ventX.Length; v++)
        {
            float vx = ventX[v];
            float vy = 690f;

            // Pipe manifold flange
            DrawRect(new Rect2(vx - 16, vy - 10, 32, 16), new Color("334155"));
            DrawRect(new Rect2(vx - 12, vy - 8, 24, 12), new Color("1e293b"));

            // Rising cloud puffs
            for (int p = 0; p < 8; p++)
            {
                float prog = ((t * 1.2f + p * 0.16f + v * 0.3f) % 1.2f) / 1.2f;
                float px = vx + Mathf.Sin(t * 3f + p * 2f) * (20f + prog * 40f);
                float py = vy - prog * 130f;
                float size = 12f + prog * 36f;
                float alpha = (1f - prog) * 0.25f;

                Color mistCol = new(0.65f, 0.95f, 1f, alpha);
                DrawCircle(new Vector2(px, py), size, mistCol);
            }
        }
    }

    /// <summary>Draws the massive vertical magnetic Apex Ascent Lift gantry and open boarding gate.</summary>
    private void DrawApexAscentLift(float t)
    {
        float liftX = 1480f;
        float liftY = 280f;
        float liftW = 320f;
        float liftH = 430f;

        // Vertical elevator shaft framing
        DrawRect(new Rect2(liftX - 30, 0, 20, 710), new Color("1e293b"));
        DrawRect(new Rect2(liftX + liftW + 10, 0, 20, 710), new Color("1e293b"));

        // Glowing magnetic rail runners
        Color railGlow = ColorCyan.Lerp(ColorAmber, Mathf.Abs(Mathf.Sin(t * 2f)) * 0.3f);
        DrawRect(new Rect2(liftX - 14, 0, 8, 710), railGlow);
        DrawRect(new Rect2(liftX + liftW + 6, 0, 8, 710), railGlow);

        // Elevator freight cabin chassis
        DrawRect(new Rect2(liftX, liftY, liftW, liftH), new Color("0a1222"));
        DrawRect(new Rect2(liftX + 10, liftY + 10, liftW - 20, liftH - 20), new Color("050914"));

        // Hazard chevron threshold on boarding ramp
        float rampY = liftY + liftH - 30;
        DrawRect(new Rect2(liftX, rampY, liftW, 30), new Color("1e293b"));
        for (int ch = 0; ch < 12; ch++)
        {
            float cx = liftX + 10 + ch * 25;
            Vector2[] chev = {
                new(cx, rampY + 26), new(cx + 10, rampY + 4), new(cx + 18, rampY + 4), new(cx + 8, rampY + 26)
            };
            DrawColoredPolygon(chev, ColorAmber);
        }

        // Animated boarding gate (slides open between t=50s and t=70s)
        float openRatio = Mathf.Clamp((t - 50f) / 18f, 0f, 1f);
        float gateH = (liftH - 80f) * (1f - openRatio);
        if (gateH > 4f)
        {
            DrawRect(new Rect2(liftX + 20, liftY + 40, liftW - 40, gateH), new Color(1f, 0.2f, 0.2f, 0.4f));
            for (int bar = 0; bar < 6; bar++)
            {
                float by = liftY + 45 + bar * (gateH / 6f);
                DrawLine(new Vector2(liftX + 25, by), new Vector2(liftX + liftW - 25, by), ColorAlert, 2f);
            }
        }
        else
        {
            // Fully open gate glowing welcoming cyan
            DrawRect(new Rect2(liftX + 20, liftY + 40, liftW - 40, liftH - 80), new Color(0f, 0.94f, 1f, 0.12f));
            DrawRect(new Rect2(liftX + 20, liftY + liftH - 45, liftW - 40, 6), ColorCyan);
        }

        // Lift interior illumination
        DrawCircle(new Vector2(liftX + liftW * 0.5f, liftY + 60), 30f, new Color(0f, 0.94f, 1f, 0.15f));
    }

    /// <summary>Draws ominous silhouette ramparts and red searchlights in the high shaft leading to Apex Bastion.</summary>
    private void DrawBastionSilhouettes(float t)
    {
        // High bulkhead archway above the elevator shaft
        Vector2[] arch = {
            new(1380, 0), new(1380, 180), new(1440, 240), new(1840, 240), new(1900, 180), new(1900, 0)
        };
        DrawColoredPolygon(arch, new Color("0a0f1e"));
        DrawPolyline(arch, new Color("1e293b"), 3f);

        // Sweeping red Praetorian surveillance spotlights (Shot 6 onward)
        if (t > 45f)
        {
            float sweepAngle = Mathf.Sin(t * 1.4f) * 0.35f + 0.2f;
            Vector2 lightOrigin = new(1640, 140);
            Vector2 beamDir = new Vector2(Mathf.Cos(sweepAngle), Mathf.Sin(sweepAngle)).Rotated(Mathf.Pi * 0.5f);

            Vector2 leftEdge = lightOrigin + (beamDir.Rotated(-0.25f) * 600f);
            Vector2 rightEdge = lightOrigin + (beamDir.Rotated(0.25f) * 600f);

            Vector2[] cone = { lightOrigin, leftEdge, rightEdge };
            DrawColoredPolygon(cone, new Color(1f, 0.15f, 0.25f, 0.12f));
        }
    }

    /// <summary>Draws all 6 operative silhouettes positioned across the gantry platform.</summary>
    private void DrawSquadSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Draws a single operative silhouette with combat stance and glowing weapon/accent.</summary>
    private void DrawOperativeSilhouette(Vector2 feet, Color accent, string name, bool active)
    {
        // Shadow base
        DrawCircle(feet + new Vector2(0, 2), 16f, new Color(0, 0, 0, 0.45f));

        Color bodyCol = new("090d16");

        // Body torso and legs
        DrawRect(new Rect2(feet.X - 10, feet.Y - 60, 20, 38), bodyCol);
        DrawRect(new Rect2(feet.X - 8, feet.Y - 26, 7, 26), bodyCol);
        DrawRect(new Rect2(feet.X + 2, feet.Y - 26, 7, 26), bodyCol);

        // Head and helmet
        DrawCircle(new Vector2(feet.X, feet.Y - 70), 9f, bodyCol);

        // Glowing visor / signature energy accent
        DrawLine(new Vector2(feet.X + 1, feet.Y - 71), new Vector2(feet.X + 8, feet.Y - 70), accent, 2.2f);

        // Weapon silhouette based on operative
        if (name == "Kaito")
        {
            // Katana blade at side
            DrawLine(new Vector2(feet.X - 12, feet.Y - 45), new Vector2(feet.X + 22, feet.Y - 72), accent, 2.4f);
        }
        else if (name == "Mirage")
        {
            // Phase daggers
            DrawLine(new Vector2(feet.X + 8, feet.Y - 45), new Vector2(feet.X + 18, feet.Y - 32), accent, 2f);
            DrawLine(new Vector2(feet.X - 12, feet.Y - 42), new Vector2(feet.X - 20, feet.Y - 30), accent, 2f);
        }
        else if (name == "Titan")
        {
            // Massive kinetic shoulder pauldrons
            DrawRect(new Rect2(feet.X - 16, feet.Y - 65, 32, 14), bodyCol);
            DrawLine(new Vector2(feet.X - 16, feet.Y - 65), new Vector2(feet.X + 16, feet.Y - 65), accent, 2f);
        }
        else if (name == "Surya")
        {
            // Bulwark shield energy plate
            DrawLine(new Vector2(feet.X + 12, feet.Y - 62), new Vector2(feet.X + 12, feet.Y - 25), accent, 3f);
        }
        else if (name == "Oceanus")
        {
            // Hydro conduit glow
            DrawLine(new Vector2(feet.X - 6, feet.Y - 55), new Vector2(feet.X - 6, feet.Y - 35), accent, 2.2f);
        }
        else if (name == "Aura")
        {
            // Ultrasonic prism harp ring
            DrawArc(new Vector2(feet.X + 10, feet.Y - 48), 11f, 0, Mathf.Pi, 12, accent, 2f);
        }
    }

    /// <summary>Draws CRT scanlines across the viewport for atmospheric sci-fi texture.</summary>
    private void DrawScanlines()
    {
        Color scanColor = new(0f, 0f, 0f, 0.16f);
        for (int y = 0; y < (int)Size.Y; y += 4)
        {
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), scanColor, 1.2f);
        }
    }

    /// <summary>Draws 2.39:1 anamorphic cinema bars across top and bottom.</summary>
    private void DrawCinemaBars(Vector2 margin, float fit)
    {
        float barH = Mathf.Max(margin.Y, (Size.Y - 1920f * fit / 2.39f) * 0.5f);
        if (barH > 0)
        {
            DrawRect(new Rect2(0, 0, Size.X, barH), Colors.Black);
            DrawRect(new Rect2(0, Size.Y - barH, Size.X, barH), Colors.Black);
        }
    }

    /// <summary>Determines the current shot index based on elapsed seconds.</summary>
    private static int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t >= ShotTimes[i] && t < ShotTimes[i + 1])
                return i;
        }
        return ShotTimes.Length - 2;
    }

    /// <summary>Calculates smoothly interpolated camera position for the current shot.</summary>
    private static Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float progress = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);

        // Smooth cubic ease-in-out
        float eased = progress * progress * (3f - 2f * progress);

        Vector2 target = CameraTargets[shot];
        Vector2 drift = new(Mathf.Sin(t * 0.25f) * 12f, Mathf.Cos(t * 0.2f) * 6f);
        return target + drift * eased;
    }

    /// <summary>Calculates smoothly interpolated camera zoom for the current shot.</summary>
    private static float GetSmoothCameraZoom(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotDuration = ShotTimes[shot + 1] - shotStart;
        float progress = Mathf.Clamp((t - shotStart) / shotDuration, 0f, 1f);

        float baseZoom = CameraZooms[shot];
        return baseZoom + Mathf.Sin(progress * Mathf.Pi) * 0.02f;
    }
}
