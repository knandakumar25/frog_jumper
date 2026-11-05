using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 11 post-mission epilogue ("The Gates of Judgment").
/// Composes the stratospheric summit fortress battlements at 4,000m, the shattered smoking husk of the defeated Bastion Praetor,
/// localized void hurricane lightning swirling around the spire, the monumental unsealing of the Grand Citadel Throne Room
/// blast doors with radiant incandescent portal glow, and Axiom Force drawing steel for the final battle.
/// </summary>
public partial class ApexBastionAftermathStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/apex_bastion_aftermath/";

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
    private static readonly Color ColorAlert = new("ff2255");    // Critical Red
    private static readonly Color ColorVoidMagenta = new("c026d3"); // Void Lightning Magenta
    private static readonly Color ColorDeck = new("0f172a");     // Gantry Titanium
    private static readonly Color ColorSteel = new("1e293b");    // Catwalk Steel

    // Shot boundary schedule matching ApexBastionAftermath dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 560),  // Shot 0: Wide shot - summit battlements, smoldering Praetor, monumental blast doors
        new(1180, 580), // Shot 1: Titan inspecting smoking Praetor wreck and massive blast doors
        new(740, 560),  // Shot 2: Oceanus tracking barometric pressure drops and void storm thermal surge
        new(960, 500),  // Shot 3: Surya looking up at the void storm lightning funneling into throne spire
        new(860, 540),  // Shot 4: Aura sensing psychic dissonance reverberating from behind doors
        new(1380, 540), // Shot 5: Mirage slicing biometric terminal; hydraulic clamps disengage
        new(1440, 500), // Shot 6: Monumental blast doors parting, radiant golden-crimson light spills forth
        new(920, 540),  // Shot 7: Surya looking back along the ramparts at the long campaign journey
        new(1060, 540)  // Shot 8: Epic squad wide shot; Axiom Force stepping into radiant throne portal
    };

    private static readonly float[] CameraZooms = {
        1.00f, // Shot 0
        1.18f, // Shot 1
        1.14f, // Shot 2
        1.12f, // Shot 3
        1.15f, // Shot 4
        1.20f, // Shot 5
        1.16f, // Shot 6
        1.10f, // Shot 7
        0.98f  // Shot 8
    };

    /// <summary>
    /// Composes stratospheric void sky, dynamic void lightning, fortress battlements,
    /// defeated Bastion Praetor mechanoid wreck, monumental parting blast doors, and operative silhouettes.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep midnight stratospheric backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("050814"));

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

        // Stratospheric void storm sky & clouds
        DrawStratosphericStormSky(t);
        CampaignArtwork.DrawEnvironment(this, "apex_bastion", Seconds, aftermath: true, drawBase: true);

        // Dynamic void lightning strikes arcing through clouds
        DrawVoidLightningStrikes(t);

        // Distant Spire summit architectural silhouettes
        DrawDistantSpireArchitecture(t);

        // Fortress ramparts and battlements deck
        DrawFortressBattlements(t);

        // Smoldering wreck of the Bastion Praetor boss mechanoid
        DrawDefeatedPraetorWreck(t);

        // Monumental Grand Citadel Throne Room Blast Doors
        DrawThroneRoomBlastDoors(t);

        // Operative squad silhouettes advancing toward the portal
        DrawSquadSilhouettes(t, shot);

        // 3. Reset transform for static cinematic screen overlay
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // CRT scanlines overlay
        DrawScanlines();

        // 2.39:1 Anamorphic cinema letterbox bars
        DrawCinemaBars(margin, fit);
    }

    /// <summary>Draws high-altitude stratospheric void sky gradient and churning storm clouds.</summary>
    private void DrawStratosphericStormSky(float t)
    {
        for (int i = 0; i < 18; i++)
        {
            float y1 = i * 60f;
            float ratio = i / 18f;
            Color band = new Color("050814").Lerp(new Color("150b2a"), ratio);
            DrawRect(new Rect2(0, y1, 1920, 60), band);
        }

        // Drifting stratospheric cloud vapor layers
        for (int c = 0; c < 8; c++)
        {
            float cx = ((c * 280f - t * 45f) % 2400f) - 200f;
            float cy = 180f + (c * 55f % 300f);
            float cw = 420f + (c * 60f % 260f);
            float ch = 110f + (c * 30f % 80f);

            Color cloudCol = new Color(0.12f, 0.08f, 0.22f, 0.35f + Mathf.Sin(t * 1.5f + c) * 0.1f);
            DrawRect(new Rect2(cx, cy, cw, ch), cloudCol);
        }
    }

    /// <summary>Draws dynamic void lightning arcing through clouds in crimson, magenta, and gold.</summary>
    private void DrawVoidLightningStrikes(float t)
    {
        // Flash interval
        float flashCycle = (t * 2.2f) % 4.0f;
        if (flashCycle < 0.25f)
        {
            float flashAlpha = (1f - flashCycle / 0.25f) * 0.22f;
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0.8f, 0.2f, 0.6f, flashAlpha));

            // Jagged lightning bolt
            Vector2 boltStart = new(920 + Mathf.Sin(t * 8f) * 200f, 60f);
            Vector2 p1 = boltStart + new Vector2(-40, 120);
            Vector2 p2 = p1 + new Vector2(60, 110);
            Vector2 p3 = p2 + new Vector2(-30, 130);
            Vector2 p4 = p3 + new Vector2(40, 100);

            DrawLine(boltStart, p1, ColorVoidMagenta, 3.5f);
            DrawLine(p1, p2, Colors.White, 2.5f);
            DrawLine(p2, p3, ColorAlert, 3f);
            DrawLine(p3, p4, Colors.White, 2f);
        }
    }

    /// <summary>Draws ominous dark architecture of the Citadel upper spire looming in background.</summary>
    private void DrawDistantSpireArchitecture(float t)
    {
        // Central crown spire
        Vector2[] spirePts = {
            new(960, 140),
            new(910, 360),
            new(900, 680),
            new(1020, 680),
            new(1010, 360)
        };
        DrawColoredPolygon(spirePts, new Color("0a0f1e"));
        DrawPolyline(spirePts, new Color("1e293b"), 2f);

        // Flanking defense pylons
        DrawRect(new Rect2(620, 300, 70, 380), new Color("080d1a"));
        DrawRect(new Rect2(1230, 300, 70, 380), new Color("080d1a"));

        // Red security beacons on spire tips
        float beaconPulse = Mathf.Abs(Mathf.Sin(t * 4f));
        DrawCircle(new Vector2(960, 140), 5f + beaconPulse * 3f, ColorAlert);
        DrawCircle(new Vector2(655, 300), 4f, ColorAlert);
        DrawCircle(new Vector2(1265, 300), 4f, ColorAlert);
    }

    /// <summary>Draws high obsidian fortress ramparts deck and gantry railings.</summary>
    private void DrawFortressBattlements(float t)
    {
        // Main foundation deck
        DrawRect(new Rect2(0, 660, 1920, 420), ColorDeck);
        DrawRect(new Rect2(0, 660, 1920, 18), ColorSteel);

        // Yellow hazard chevrons along edge
        for (int h = 0; h < 24; h++)
        {
            float hx = h * 85f;
            DrawLine(new Vector2(hx, 660), new Vector2(hx + 35, 678), ColorAmber, 3f);
        }

        // Structural steel support pillars below
        for (int p = 0; p < 8; p++)
        {
            float px = 120f + p * 240f;
            DrawRect(new Rect2(px, 678, 48, 402), new Color("080f20"));
        }
    }

    /// <summary>Draws smoldering severed wreck of the defeated Bastion Praetor boss mechanoid.</summary>
    private void DrawDefeatedPraetorWreck(float t)
    {
        Vector2 wreckPos = new(1180, 670);

        // Ruptured obsidian armor chassis collapsed onto rampart
        Vector2[] chassisPts = {
            wreckPos + new Vector2(-55, -20),
            wreckPos + new Vector2(-25, -60),
            wreckPos + new Vector2(40, -45),
            wreckPos + new Vector2(65, 5),
            wreckPos + new Vector2(25, 45),
            wreckPos + new Vector2(-40, 35)
        };
        DrawColoredPolygon(chassisPts, new Color("0c101d"));
        DrawPolyline(chassisPts, new Color("1e293b"), 2.5f);

        // Severed flak cannon mounts with glowing heat and spark emissions
        Vector2 leftMount = wreckPos + new Vector2(-45, -35);
        Vector2 rightMount = wreckPos + new Vector2(35, -25);
        DrawCircle(leftMount, 10f, new Color(1f, 0.3f, 0.1f, 0.7f));
        DrawCircle(rightMount, 10f, new Color(1f, 0.3f, 0.1f, 0.7f));

        // Cracked command visor (flickering red dying pulse)
        float visorFlicker = Mathf.Abs(Mathf.Sin(t * 6f)) * (1f - Mathf.Clamp(t / 80f, 0f, 0.85f));
        DrawCircle(wreckPos + new Vector2(-5, -15), 6f, new Color(1f, 0.1f, 0.25f, visorFlicker));

        // Electric spark discharges firing intermittently
        if (Mathf.Sin(t * 13f) > 0.45f)
        {
            Vector2 sparkPos = wreckPos + new Vector2(Mathf.Sin(t * 17f) * 40f, Mathf.Cos(t * 14f) * 20f);
            DrawLine(sparkPos, sparkPos + new Vector2(10, -10), ColorAlert, 2.5f);
            DrawLine(sparkPos + new Vector2(10, -10), sparkPos + new Vector2(18, -4), Colors.White, 1.8f);
            DrawCircle(sparkPos, 3f, Colors.White);
        }

        // Billowing smoke plumes drifting into clouds
        for (int s = 0; s < 4; s++)
        {
            float sy = wreckPos.Y - 20f - ((t * 35f + s * 45f) % 180f);
            float sx = wreckPos.X + Mathf.Sin(t * 2f + s) * 25f + s * 10f;
            float sSize = 14f + ((wreckPos.Y - sy) / 180f) * 32f;
            float alpha = (1f - (wreckPos.Y - sy) / 180f) * 0.35f;

            DrawCircle(new Vector2(sx, sy), sSize, new Color(0.2f, 0.25f, 0.35f, alpha));
        }
    }

    /// <summary>Draws the colossal Grand Citadel Throne Room blast doors and parting portal sequence.</summary>
    private void DrawThroneRoomBlastDoors(float t)
    {
        Vector2 doorCenter = new(1480, 520);
        float doorWidth = 280f;
        float doorHeight = 420f;

        // Framing monolith archway
        DrawRect(new Rect2(doorCenter.X - doorWidth * 0.5f - 40, doorCenter.Y - doorHeight * 0.5f - 40, doorWidth + 80, doorHeight + 50), new Color("0a0f1d"));
        DrawRect(new Rect2(doorCenter.X - doorWidth * 0.5f - 20, doorCenter.Y - doorHeight * 0.5f - 20, doorWidth + 40, doorHeight + 30), new Color("060a14"));

        // Glowing power conduit pylons flanking the gate
        DrawRect(new Rect2(doorCenter.X - doorWidth * 0.5f - 30, doorCenter.Y - doorHeight * 0.5f, 12, doorHeight), ColorAlert);
        DrawRect(new Rect2(doorCenter.X + doorWidth * 0.5f + 18, doorCenter.Y - doorHeight * 0.5f, 12, doorHeight), ColorAlert);

        // Door unsealing opening progression (starts at t=55s, fully parts by t=75s)
        float openProgress = 0f;
        if (t >= 55f)
        {
            openProgress = Mathf.Clamp((t - 55f) / 18f, 0f, 1f);
        }

        float halfWidth = doorWidth * 0.5f;
        float doorSlide = openProgress * (halfWidth - 15f);

        // Radiant incandescent throne chamber glow emerging as doors open
        if (openProgress > 0f)
        {
            float portalGlowAlpha = openProgress * 0.85f;
            float fissureWidth = doorSlide * 2f;

            // Blinding golden-crimson portal core
            DrawRect(new Rect2(doorCenter.X - doorSlide, doorCenter.Y - doorHeight * 0.5f, fissureWidth, doorHeight), new Color(1f, 0.95f, 0.7f, portalGlowAlpha));

            // Wide radiant light beam spilling across ramparts
            Vector2[] lightBeam = {
                new(doorCenter.X - doorSlide, doorCenter.Y - doorHeight * 0.5f),
                new(doorCenter.X + doorSlide, doorCenter.Y - doorHeight * 0.5f),
                new(doorCenter.X + doorSlide + 350f * openProgress, 680),
                new(doorCenter.X - doorSlide - 550f * openProgress, 680)
            };
            DrawColoredPolygon(lightBeam, new Color(1f, 0.85f, 0.4f, portalGlowAlpha * 0.25f));
        }

        // Left door panel sliding left
        float leftX = doorCenter.X - halfWidth - doorSlide;
        DrawRect(new Rect2(leftX, doorCenter.Y - doorHeight * 0.5f, halfWidth, doorHeight), new Color("141b2d"));
        DrawRect(new Rect2(leftX + 8, doorCenter.Y - doorHeight * 0.5f + 10, halfWidth - 16, doorHeight - 20), new Color("0e1424"));
        DrawLine(new Vector2(leftX + halfWidth - 4, doorCenter.Y - doorHeight * 0.5f), new Vector2(leftX + halfWidth - 4, doorCenter.Y + doorHeight * 0.5f), ColorAmber, 3f);

        // Right door panel sliding right
        float rightX = doorCenter.X + doorSlide;
        DrawRect(new Rect2(rightX, doorCenter.Y - doorHeight * 0.5f, halfWidth, doorHeight), new Color("141b2d"));
        DrawRect(new Rect2(rightX + 8, doorCenter.Y - doorHeight * 0.5f + 10, halfWidth - 16, doorHeight - 20), new Color("0e1424"));
        DrawLine(new Vector2(rightX + 4, doorCenter.Y - doorHeight * 0.5f), new Vector2(rightX + 4, doorCenter.Y + doorHeight * 0.5f), ColorAmber, 3f);
    }

    /// <summary>Draws all 6 operative silhouettes standing on the battlements and advancing toward the unsealed doors.</summary>
    private void DrawSquadSilhouettes(float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    private void DrawScanlines()
    {
        for (int y = 0; y < (int)Size.Y; y += 4)
        {
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), new Color(0, 0, 0, 0.18f), 1f);
        }
    }

    private void DrawCinemaBars(Vector2 margin, float fit)
    {
        float barHeight = 85f * fit;
        DrawRect(new Rect2(0, 0, Size.X, barHeight + margin.Y), Colors.Black);
        DrawRect(new Rect2(0, Size.Y - barHeight - margin.Y, Size.X, barHeight + margin.Y), Colors.Black);
    }

    private int GetCurrentShot(float t)
    {
        for (int i = ShotTimes.Length - 1; i >= 0; i--)
        {
            if (t >= ShotTimes[i]) return i;
        }
        return 0;
    }

    private Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        if (shot >= CameraTargets.Length - 1) return CameraTargets[^1];
        float segStart = ShotTimes[shot];
        float segDur = ShotTimes[shot + 1] - segStart;
        float progress = Mathf.Clamp((t - segStart) / segDur, 0, 1);
        float smooth = Mathf.SmoothStep(0, 1, progress);

        return CameraTargets[shot].Lerp(CameraTargets[shot + 1], smooth * 0.35f);
    }

    private float GetSmoothCameraZoom(float t, int shot)
    {
        if (shot >= CameraZooms.Length - 1) return CameraZooms[^1];
        float segStart = ShotTimes[shot];
        float segDur = ShotTimes[shot + 1] - segStart;
        float progress = Mathf.Clamp((t - segStart) / segDur, 0, 1);

        return Mathf.Lerp(CameraZooms[shot], CameraZooms[shot + 1], progress * 0.25f);
    }
}
