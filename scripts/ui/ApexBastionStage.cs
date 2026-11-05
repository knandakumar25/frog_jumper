using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 11 prologue ("The Praetorian Vanguard").
/// Composes the Spire Level 100 summit fortress at 4,500m elevation, rolling stratospheric clouds below,
/// obsidian battlements with crimson heraldry, automated twin-linked plasma turrets, Praetorian guard silhouettes,
/// the looming silhouette of the Bastion Praetor war mechanoid, and the squad stepping off the freight lift.
/// </summary>
public partial class ApexBastionStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/apex_bastion_intro/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>Requires authored environment art and cue metadata; the owner supplies a safe completion fallback.</summary>
    public bool ResourcesAvailable => CampaignArtwork.Available("apex_bastion", ArtPath);

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan
    private static readonly Color ColorAmber = new("fbbf24");    // Hazard Amber
    private static readonly Color ColorPraetorian = new("ff1144"); // Praetorian Crimson
    private static readonly Color ColorObsidian = new("0a0f1d"); // Fortress Obsidian
    private static readonly Color ColorSteel = new("1e293b");    // Fortified Steel

    // Shot boundary schedule matching ApexBastionIntro dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - lift arriving at Spire Level 100, clouds below, fortress gates
        new(1280, 520), // Shot 1: Titan admiring 3-meter obsidian battlements and plasma turrets
        new(620, 580),  // Shot 2: Oceanus looking over outer parapet at clouds 4km above the city
        new(1350, 480), // Shot 3: Mirage scanning Praetorian cyber-samurai and shock-troopers on ramparts
        new(920, 540),  // Shot 4: Aura sensing the chilling silence of the Praetorian comms
        new(1480, 460), // Shot 5: Surya examining perimeter barrier gate and grand stairway to Throne Room
        new(1580, 440), // Shot 6: AXIOM-7 scanning the colossal Bastion Praetor war mechanoid
        new(1100, 540), // Shot 7: Mirage reminding the squad of past victories
        new(1020, 540)  // Shot 8: Epic wide squad shot; stepping onto the courtyard to breach the Bastion
    };

    private static readonly float[] CameraZooms = {
        1.00f, // Shot 0
        1.15f, // Shot 1
        1.18f, // Shot 2
        1.14f, // Shot 3
        1.12f, // Shot 4
        1.16f, // Shot 5
        1.22f, // Shot 6
        1.14f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes stratospheric clouds, obsidian fortress ramparts, automated plasma turrets,
    /// Praetorian enemy silhouettes, Bastion Praetor mechanoid, freight lift, and operative squad analytically.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep space / stratospheric twilight backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("030511"));

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

        // Stratospheric twilight sky, stars, and sea of clouds at 4,000m
        DrawStratosphereSkyAndClouds(t);
        CampaignArtwork.DrawEnvironment(this, "apex_bastion", Seconds, aftermath: false, drawBase: true);

        // Distant grand stairway and colossal Throne Room vault portal
        DrawGrandStairwayAndThroneVault(t);

        // Colossal obsidian fortress battlements and defense towers
        DrawObsidianFortressBattlements(t);

        // Automated twin-linked plasma flak turrets and laser beams
        DrawAutomatedPlasmaTurrets(t);

        // Looming Bastion Praetor war mechanoid silhouette
        DrawBastionPraetorSilhouette(t);

        // Praetorian shock-trooper and cyber-samurai silhouettes on ramparts
        DrawPraetorianEnemySilhouettes(t);

        // Freight lift platform and open pneumatic doors on screen left
        DrawFreightLiftPlatform(t);

        // Operative squad silhouettes advancing onto the courtyard
        DrawSquadSilhouettes(t, shot);

        // 3. Reset transform for static cinematic screen overlay
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // CRT scanlines overlay
        DrawScanlines();

        // 2.39:1 Anamorphic cinema letterbox bars
        DrawCinemaBars(margin, fit);
    }

    /// <summary>Draws stratospheric sky gradient, stars, and dense rolling clouds beneath the fortress.</summary>
    private void DrawStratosphereSkyAndClouds(float t)
    {
        // Stratospheric sky gradient
        for (int i = 0; i < 18; i++)
        {
            float y = i * 40f;
            float ratio = i / 18f;
            Color band = new Color("030511").Lerp(new Color("0d1430"), ratio);
            DrawRect(new Rect2(0, y, 1920, 40), band);
        }

        // Stars in upper atmosphere
        for (int s = 0; s < 45; s++)
        {
            float sx = (s * 137.5f) % 1920f;
            float sy = (s * 73.1f) % 420f;
            float twinkle = Mathf.Abs(Mathf.Sin(t * 2f + s));
            float sz = 1.0f + (s % 3) * 0.5f;
            DrawCircle(new Vector2(sx, sy), sz, new Color(1f, 1f, 1f, 0.4f + twinkle * 0.5f));
        }

        // Rolling cloud bank below fortress (Y = 640 to 1080)
        for (int c = 0; c < 16; c++)
        {
            float cx = ((c * 140f - t * 25f) % 2200f) - 140f;
            float cy = 720f + Mathf.Sin(t * 0.5f + c) * 30f;
            float rad = 110f + (c % 5) * 25f;
            Color cloudCol = new(0.12f, 0.16f, 0.28f, 0.65f);
            DrawCircle(new Vector2(cx, cy), rad, cloudCol);
            DrawCircle(new Vector2(cx + 60, cy + 20), rad * 0.85f, new Color(0.16f, 0.22f, 0.36f, 0.55f));
        }
    }

    /// <summary>Draws elevated grand stairway leading to the Throne Room vault gate in the distance.</summary>
    private void DrawGrandStairwayAndThroneVault(float t)
    {
        float stairX = 1420f;
        float stairY = 220f;

        // Monolithic Throne Room portal archway in the far background
        Vector2[] vaultArch = {
            new(stairX - 80, stairY - 140),
            new(stairX + 260, stairY - 140),
            new(stairX + 320, stairY + 220),
            new(stairX - 140, stairY + 220)
        };
        DrawColoredPolygon(vaultArch, new Color("070a14"));
        DrawPolyline(vaultArch, new Color("141c30"), 3f);

        // Massive circular vault door with ominous crimson glow
        Vector2 doorCenter = new(stairX + 90, stairY + 40);
        DrawCircle(doorCenter, 75f, new Color("0a0f1e"));
        DrawCircle(doorCenter, 68f, new Color("05070f"));

        // Crimson runes & energy seams
        float pulse = Mathf.Abs(Mathf.Sin(t * 2.2f));
        Color runeCol = ColorPraetorian.Lerp(Colors.Black, 0.25f + pulse * 0.35f);
        DrawArc(doorCenter, 50f, 0, Mathf.Tau, 24, runeCol, 3f);
        DrawLine(doorCenter + new Vector2(-60, 0), doorCenter + new Vector2(60, 0), runeCol, 2f);
        DrawLine(doorCenter + new Vector2(0, -60), doorCenter + new Vector2(0, 60), runeCol, 2f);

        // Grand stairway ascending from courtyard to vault door
        for (int step = 0; step < 12; step++)
        {
            float stepY = stairY + 140f + step * 20f;
            float stepW = 260f + step * 16f;
            float stepX = stairX + 90f - stepW * 0.5f;
            DrawRect(new Rect2(stepX, stepY, stepW, 16), new Color("121828"));
            DrawLine(new Vector2(stepX, stepY), new Vector2(stepX + stepW, stepY), new Color("25324d"), 2f);
        }
    }

    /// <summary>Draws colossal obsidian fortress walls, battlements, and defensive ramparts.</summary>
    private void DrawObsidianFortressBattlements(float t)
    {
        // Fortress foundation wall (Y = 660 to 1080)
        DrawRect(new Rect2(320, 660, 1600, 420), ColorObsidian);
        DrawLine(new Vector2(320, 660), new Vector2(1920, 660), ColorSteel, 4f);
        DrawLine(new Vector2(320, 664), new Vector2(1920, 664), ColorPraetorian, 2f);

        // Heavy fortress towers along the ramparts (X = 580, 940, 1320, 1720)
        float[] towerX = { 640f, 1020f, 1380f, 1740f };
        for (int i = 0; i < towerX.Length; i++)
        {
            float tx = towerX[i];
            float tw = 90f;
            DrawRect(new Rect2(tx, 440, tw, 220), new Color("0e1628"));
            DrawRect(new Rect2(tx + 8, 450, tw - 16, 210), new Color("080d19"));

            // Tower crenellations / battlements
            for (int b = 0; b < 3; b++)
            {
                DrawRect(new Rect2(tx + b * 30, 415, 20, 25), new Color("141d33"));
            }

            // Praetorian crimson crest emblem on tower face
            Vector2 crest = new(tx + tw * 0.5f, 490);
            Vector2[] diamond = {
                crest + new Vector2(0, -18),
                crest + new Vector2(12, 0),
                crest + new Vector2(0, 18),
                crest + new Vector2(-12, 0)
            };
            DrawColoredPolygon(diamond, ColorPraetorian);
            DrawPolyline(diamond, Colors.White, 1.2f);
        }

        // Parapet safety barrier along courtyard edge
        for (int p = 340; p < 1920; p += 60)
        {
            DrawLine(new Vector2(p, 660), new Vector2(p, 635), ColorSteel, 3f);
        }
        DrawLine(new Vector2(340, 635), new Vector2(1920, 635), ColorSteel, 3f);
    }

    /// <summary>Draws automated twin-linked plasma flak turrets tracking with red targeting lasers.</summary>
    private void DrawAutomatedPlasmaTurrets(float t)
    {
        float[] turretX = { 685f, 1065f, 1425f, 1785f };
        float turretY = 415f;

        for (int i = 0; i < turretX.Length; i++)
        {
            float tx = turretX[i];
            float angle = Mathf.Sin(t * 1.5f + i * 1.2f) * 0.45f - 0.2f;

            // Turret base pivot
            DrawCircle(new Vector2(tx, turretY), 12f, new Color("1e293b"));
            DrawCircle(new Vector2(tx, turretY), 7f, ColorPraetorian);

            // Dual gun barrels
            Vector2 barrelDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Rotated(-Mathf.Pi * 0.65f);
            Vector2 b1 = new Vector2(tx, turretY) + barrelDir.Rotated(0.12f) * 32f;
            Vector2 b2 = new Vector2(tx, turretY) + barrelDir.Rotated(-0.12f) * 32f;

            DrawLine(new Vector2(tx, turretY), b1, new Color("334155"), 4f);
            DrawLine(new Vector2(tx, turretY), b2, new Color("334155"), 4f);

            // Sweeping red targeting laser beam
            Vector2 laserEnd = new Vector2(tx, turretY) + barrelDir * 700f;
            DrawLine(new Vector2(tx, turretY), laserEnd, new Color(1f, 0.1f, 0.2f, 0.35f), 1.5f);
            DrawCircle(laserEnd, 3f, ColorPraetorian);
        }
    }

    /// <summary>Draws looming giant silhouette of the Bastion Praetor war mechanoid guarding the gate.</summary>
    private void DrawBastionPraetorSilhouette(float t)
    {
        Vector2 mechPos = new(1620, 480);

        // Massive armored torso and shoulders
        Vector2[] chassis = {
            mechPos + new Vector2(-55, -80),
            mechPos + new Vector2(55, -80),
            mechPos + new Vector2(75, -20),
            mechPos + new Vector2(40, 60),
            mechPos + new Vector2(-40, 60),
            mechPos + new Vector2(-75, -20)
        };
        DrawColoredPolygon(chassis, new Color("090c16"));
        DrawPolyline(chassis, new Color("1a233a"), 2.5f);

        // Giant shoulder missile pods
        DrawRect(new Rect2(mechPos.X - 95, mechPos.Y - 95, 38, 50), new Color("090c16"));
        DrawRect(new Rect2(mechPos.X + 57, mechPos.Y - 95, 38, 50), new Color("090c16"));

        // Glowing red ocular sensor array (pulsing menacingly)
        float eyeGlow = 0.5f + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.5f;
        Color eyeCol = new(1f, 0.05f, 0.2f, eyeGlow);
        DrawLine(mechPos + new Vector2(-22, -50), mechPos + new Vector2(22, -50), eyeCol, 3.5f);
        DrawCircle(mechPos + new Vector2(-12, -50), 3.5f, Colors.White);
        DrawCircle(mechPos + new Vector2(12, -50), 3.5f, Colors.White);

        // Heavy arm cannon silhouette extending downward
        DrawLine(mechPos + new Vector2(-75, -10), mechPos + new Vector2(-90, 80), new Color("141c2e"), 14f);
        DrawLine(mechPos + new Vector2(75, -10), mechPos + new Vector2(90, 80), new Color("141c2e"), 14f);
    }

    /// <summary>Draws elite Praetorian shock-trooper and cyber-samurai silhouettes stationed along the ramparts.</summary>
    private void DrawPraetorianEnemySilhouettes(float t)
    {
        float[] guardX = { 840f, 1180f, 1310f, 1500f };
        for (int i = 0; i < guardX.Length; i++)
        {
            float gx = guardX[i];
            float gy = 660f;

            Color guardCol = new("060810");

            // Shadow
            DrawCircle(new Vector2(gx, gy + 2), 14f, new Color(0, 0, 0, 0.5f));

            // Armor body
            DrawRect(new Rect2(gx - 8, gy - 55, 16, 35), guardCol);
            DrawRect(new Rect2(gx - 7, gy - 20, 6, 20), guardCol);
            DrawRect(new Rect2(gx + 1, gy - 20, 6, 20), guardCol);

            // Helmet
            DrawCircle(new Vector2(gx, gy - 64), 8f, guardCol);

            // Glowing red visor
            DrawLine(new Vector2(gx - 4, gy - 65), new Vector2(gx + 4, gy - 65), ColorPraetorian, 2f);

            // Dual red vibro-blades (for cyber-samurai)
            if (i % 2 == 0)
            {
                DrawLine(new Vector2(gx - 12, gy - 40), new Vector2(gx - 20, gy - 15), ColorPraetorian, 2.2f);
                DrawLine(new Vector2(gx + 12, gy - 40), new Vector2(gx + 20, gy - 15), ColorPraetorian, 2.2f);
            }
            else
            {
                // Heavy plasma rifle
                DrawLine(new Vector2(gx - 6, gy - 38), new Vector2(gx + 18, gy - 42), new Color("334155"), 4f);
                DrawCircle(new Vector2(gx + 18, gy - 42), 2.5f, ColorPraetorian);
            }
        }
    }

    /// <summary>Draws the magnetic freight lift cabin on screen left with open pneumatic doors.</summary>
    private void DrawFreightLiftPlatform(float t)
    {
        float liftX = 40f;
        float liftY = 240f;
        float liftW = 280f;
        float liftH = 450f;

        // Lift vertical shaft framing
        DrawRect(new Rect2(liftX - 20, 0, 16, 720), new Color("141c2e"));
        DrawRect(new Rect2(liftX + liftW + 4, 0, 16, 720), new Color("141c2e"));

        // Glowing cyan magnetic lift rails
        DrawRect(new Rect2(liftX - 6, 0, 6, 720), ColorCyan);
        DrawRect(new Rect2(liftX + liftW, 0, 6, 720), ColorCyan);

        // Lift cabin chassis
        DrawRect(new Rect2(liftX, liftY, liftW, liftH), new Color("0a101f"));
        DrawRect(new Rect2(liftX + 8, liftY + 8, liftW - 16, liftH - 16), new Color("050811"));

        // Open doors (slid into side walls)
        DrawRect(new Rect2(liftX + 8, liftY + 30, 24, liftH - 60), new Color("1e293b"));
        DrawRect(new Rect2(liftX + liftW - 32, liftY + 30, 24, liftH - 60), new Color("1e293b"));

        // Cabin interior illumination
        DrawCircle(new Vector2(liftX + liftW * 0.5f, liftY + 80), 35f, new Color(0f, 0.94f, 1f, 0.18f));

        // Steam venting from pneumatic brake clamps
        for (int p = 0; p < 5; p++)
        {
            float prog = ((t * 0.9f + p * 0.2f) % 1.0f);
            float sx = liftX + liftW + 10f + prog * 45f;
            float sy = 680f - Mathf.Sin(prog * Mathf.Pi) * 20f;
            float alpha = (1f - prog) * 0.25f;
            DrawCircle(new Vector2(sx, sy), 10f + prog * 16f, new Color(0.8f, 0.95f, 1f, alpha));
        }
    }

    /// <summary>Draws all 6 operative silhouettes advancing from the lift onto the courtyard.</summary>
    private void DrawSquadSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Draws an individual operative silhouette with glowing weapon/visor accent.</summary>
    private void DrawOperative(Vector2 feet, Color accent, string name)
    {
        DrawCircle(feet + new Vector2(0, 2), 15f, new Color(0, 0, 0, 0.5f));

        Color bodyCol = new("080c16");

        // Body & legs
        DrawRect(new Rect2(feet.X - 9, feet.Y - 58, 18, 36), bodyCol);
        DrawRect(new Rect2(feet.X - 7, feet.Y - 24, 6, 24), bodyCol);
        DrawRect(new Rect2(feet.X + 1, feet.Y - 24, 6, 24), bodyCol);

        // Head
        DrawCircle(new Vector2(feet.X, feet.Y - 68), 8.5f, bodyCol);

        // Visor glow
        DrawLine(new Vector2(feet.X + 1, feet.Y - 69), new Vector2(feet.X + 7, feet.Y - 68), accent, 2.2f);

        // Weapons
        if (name == "Kaito")
        {
            DrawLine(new Vector2(feet.X - 10, feet.Y - 45), new Vector2(feet.X + 24, feet.Y - 74), accent, 2.4f);
        }
        else if (name == "Mirage")
        {
            DrawLine(new Vector2(feet.X + 8, feet.Y - 42), new Vector2(feet.X + 18, feet.Y - 30), accent, 2f);
            DrawLine(new Vector2(feet.X - 10, feet.Y - 40), new Vector2(feet.X - 18, feet.Y - 28), accent, 2f);
        }
        else if (name == "Titan")
        {
            DrawRect(new Rect2(feet.X - 16, feet.Y - 62, 32, 14), bodyCol);
            DrawLine(new Vector2(feet.X - 16, feet.Y - 62), new Vector2(feet.X + 16, feet.Y - 62), accent, 2.2f);
        }
        else if (name == "Surya")
        {
            DrawLine(new Vector2(feet.X + 11, feet.Y - 60), new Vector2(feet.X + 11, feet.Y - 22), accent, 3f);
        }
        else if (name == "Oceanus")
        {
            DrawLine(new Vector2(feet.X - 5, feet.Y - 52), new Vector2(feet.X - 5, feet.Y - 32), accent, 2.2f);
        }
        else if (name == "Aura")
        {
            DrawArc(new Vector2(feet.X + 10, feet.Y - 45), 10f, 0, Mathf.Pi, 12, accent, 2f);
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

    /// <summary>Determines current shot index based on elapsed seconds.</summary>
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

        float eased = progress * progress * (3f - 2f * progress);

        Vector2 target = CameraTargets[shot];
        Vector2 drift = new(Mathf.Sin(t * 0.25f) * 10f, Mathf.Cos(t * 0.2f) * 5f);
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
