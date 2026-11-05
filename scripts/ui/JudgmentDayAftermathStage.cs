using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 12 campaign finale epilogue ("Dawn of Axiom").
/// Composes the grand liberation sequence atop the Spire summit at 5,000m: the collapsed Singularity Core
/// bursting into a golden aurora, the perpetual storm dissipating into a radiant sunrise, the sprawling
/// metropolis of Neo-Arcadia lighting up across all 12 districts, the shattered throne dais of the Void Commander,
/// and all six operatives standing united on the observation balcony to welcome the dawn of a free world.
/// </summary>
public partial class JudgmentDayAftermathStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/judgment_day_aftermath/";

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
    private static readonly Color ColorGold = new("f59e0b");     // Dawn Sunlight Gold
    private static readonly Color ColorSunrise = new("ea580c");  // Warm Sunrise Orange

    // Shot boundary schedule matching JudgmentDayAftermath dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 480),  // Shot 0: Wide shot - singularity collapsing into aurora above cathedral
        new(1180, 560), // Shot 1: Mirage at the observation rail watching firewalls collapse
        new(720, 560),  // Shot 2: Titan watching lower quarantine districts light up
        new(960, 520),  // Shot 3: Oceanus observing oceanic wind clearing the storm
        new(1280, 540), // Shot 4: Surya drinking in the golden dawn sunlight
        new(820, 540),  // Shot 5: Aura listening to the joyous resonance of millions singing
        new(960, 540),  // Shot 6: AXIOM-7 projecting global civilian autonomy status
        new(1040, 540), // Shot 7: Kaito sheathing his kinetic blade as the sun rises
        new(960, 540)   // Shot 8: Epic squad wide ensemble; all 6 operatives facing the dawn
    };

    private static readonly float[] CameraZooms = {
        0.98f, // Shot 0
        1.18f, // Shot 1
        1.15f, // Shot 2
        1.12f, // Shot 3
        1.16f, // Shot 4
        1.14f, // Shot 5
        1.10f, // Shot 6
        1.15f, // Shot 7
        0.95f  // Shot 8
    };

    /// <summary>
    /// Composes the radiant dawn sky, collapsing singularity aurora, illuminated metropolis skyline,
    /// shattered throne room architecture, and triumphant operative silhouettes.
    /// </summary>
    public override void _Draw()
    {
        // 1. Deep space base backdrop
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("06020f"));

        float t = Mathf.Clamp(Seconds, 0, 90f);
        int shot = GetCurrentShot(t);

        // Aspect fit to standard 1920x1080 canvas
        float fit = Mathf.Min(Size.X / 1920f, Size.Y / 1080f);
        Vector2 margin = (Size - new Vector2(1920f, 1080f) * fit) * 0.5f;

        Vector2 camPos = GetSmoothCameraPosition(t, shot);
        float camZoom = GetSmoothCameraZoom(t, shot);

        // 2. Camera-zoomed cinematic world layer
        DrawSetTransform(margin + (new Vector2(960f, 540f) - camPos * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // Radiant dawn sunrise sky & dissipating storm clouds
        DrawSunriseSky(t);

        // Sun disc and volumetric sunbeams cresting the horizon
        DrawRisingSun(t);

        // Collapsing Singularity Core aurora & rising luminescent motes
        DrawSingularityDawnAurora(t);

        // Distant illuminated Neo-Arcadia metropolis skyline
        DrawDistantLiberatedMetropolis(t);

        // Summit cathedral overlook, observation balcony, and shattered dais
        DrawCathedralOverlook(t);

        // Operative squad silhouettes on the balcony
        DrawOperativeSquad(t);

        // 3. Screen-space cinematic overlays (widescreen bars, scanlines, vignette)
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawCinematicOverlays(Size, t);
    }

    private int GetCurrentShot(float t)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (t >= ShotTimes[i] && t < ShotTimes[i + 1])
                return i;
        }
        return ShotTimes.Length - 2;
    }

    private Vector2 GetSmoothCameraPosition(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotEnd = ShotTimes[shot + 1];
        float progress = Mathf.Clamp((t - shotStart) / (shotEnd - shotStart), 0f, 1f);
        float ease = Mathf.SmoothStep(0f, 1f, progress);

        Vector2 current = CameraTargets[shot];
        Vector2 next = shot + 1 < CameraTargets.Length ? CameraTargets[shot + 1] : current;
        return current.Lerp(next, ease * 0.25f);
    }

    private float GetSmoothCameraZoom(float t, int shot)
    {
        float shotStart = ShotTimes[shot];
        float shotEnd = ShotTimes[shot + 1];
        float progress = Mathf.Clamp((t - shotStart) / (shotEnd - shotStart), 0f, 1f);
        float ease = Mathf.SmoothStep(0f, 1f, progress);

        float current = CameraZooms[shot];
        float next = shot + 1 < CameraZooms.Length ? CameraZooms[shot + 1] : current;
        return Mathf.Lerp(current, next, ease * 0.15f);
    }

    private void DrawSunriseSky(float t)
    {
        // As time progresses, the sky shifts from dark cosmic night to warm morning gold
        float dawnProgress = Mathf.Clamp(t / 80f, 0f, 1f);

        Color topNight = new("060212");
        Color topDawn = new("14092b");
        Color topColor = topNight.Lerp(topDawn, dawnProgress);

        Color midNight = new("1a0b30");
        Color midDawn = new("6b2148");
        Color midColor = midNight.Lerp(midDawn, dawnProgress);

        Color botNight = new("2d0c45");
        Color botDawn = new("c2410c");
        Color botColor = botNight.Lerp(botDawn, dawnProgress);

        // Vertical gradient bands
        DrawRect(new Rect2(0, 0, 1920, 360), topColor);
        DrawRect(new Rect2(0, 360, 1920, 240), midColor);
        DrawRect(new Rect2(0, 600, 1920, 480), botColor);

        // Soft cloud sea layers breaking apart
        for (int i = 0; i < 8; i++)
        {
            float cx = (i * 260 + t * 12f) % 2200 - 140;
            float cy = 520 + Mathf.Sin(t * 0.3f + i) * 25f;
            float alpha = 0.25f + 0.15f * Mathf.Sin(t * 0.5f + i);
            DrawCircle(new Vector2(cx, cy), 140 + i * 15, new Color(0.85f, 0.45f, 0.3f, alpha * (0.6f + 0.4f * dawnProgress)));
        }
    }

    private void DrawRisingSun(float t)
    {
        float dawnProgress = Mathf.Clamp((t - 15f) / 65f, 0f, 1f);
        if (dawnProgress <= 0f) return;

        Vector2 sunCenter = new(960f, 620f - dawnProgress * 140f);
        float sunRadius = 60f + dawnProgress * 20f;

        // Outer radiant atmospheric glow
        DrawCircle(sunCenter, sunRadius * 4.5f, new Color(1.0f, 0.8f, 0.4f, 0.12f * dawnProgress));
        DrawCircle(sunCenter, sunRadius * 2.8f, new Color(1.0f, 0.85f, 0.5f, 0.25f * dawnProgress));

        // Brilliant sun disc
        DrawCircle(sunCenter, sunRadius, new Color(1.0f, 0.95f, 0.8f, 0.85f * dawnProgress));

        // Volumetric sunbeams radiating outward
        for (int r = 0; r < 12; r++)
        {
            float angle = r * (Mathf.Pi / 6f) + t * 0.02f;
            Vector2 p1 = sunCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sunRadius;
            Vector2 p2 = sunCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 900f;
            DrawLine(p1, p2, new Color(1.0f, 0.9f, 0.6f, 0.07f * dawnProgress), 36f);
        }
    }

    private void DrawSingularityDawnAurora(float t)
    {
        Vector2 corePos = new(960f, 220f);
        float pulse = Mathf.Sin(t * 1.5f);

        // Core dissipating rings
        for (int ring = 1; ring <= 4; ring++)
        {
            float radius = ring * 50f + pulse * 12f;
            float alpha = Mathf.Clamp(1.0f - (t / 85f), 0.15f, 0.7f) * (0.8f / ring);
            Color ringColor = ring % 2 == 0
                ? new Color(0f, 0.94f, 1f, alpha)
                : new Color(1f, 0.85f, 0.3f, alpha);
            DrawArc(corePos, radius, 0, Mathf.Tau, 48, ringColor, 3f);
        }

        // Drifting luminescent dawn embers rising upwards
        for (int i = 0; i < 40; i++)
        {
            float seed = i * 47.13f;
            float x = 700f + (seed % 520f) + Mathf.Sin(t * 0.8f + i) * 35f;
            float y = (1000f - ((t * 40f + seed * 3f) % 900f));
            float size = 2f + (i % 3);
            Color emberColor = (i % 2 == 0)
                ? new Color(1f, 0.9f, 0.4f, 0.75f)
                : new Color(0.2f, 0.9f, 1f, 0.65f);
            DrawCircle(new Vector2(x, y), size, emberColor);
        }
    }

    private void DrawDistantLiberatedMetropolis(float t)
    {
        float baseY = 680f;

        // Distant background skyscrapers
        for (int i = 0; i < 28; i++)
        {
            float bx = i * 72f - 40f;
            float bWidth = 56f;
            float bHeight = 160f + Mathf.Abs(Mathf.Sin(i * 1.7f)) * 240f;
            DrawRect(new Rect2(bx, baseY - bHeight, bWidth, bHeight + 400), new Color("0a1024"));

            // Windows lighting up sector by sector
            int district = i % 12;
            float districtUnlockTime = district * 6f; // Each district powers on every 6s
            if (t >= districtUnlockTime)
            {
                float powerAlpha = Mathf.Clamp((t - districtUnlockTime) / 3f, 0f, 0.85f);
                for (int w = 0; w < 6; w++)
                {
                    float wy = baseY - bHeight + 20f + w * 28f;
                    Color winColor = (district % 3 == 0)
                        ? new Color(0.2f, 0.8f, 1f, powerAlpha)
                        : (district % 3 == 1)
                            ? new Color(1f, 0.85f, 0.4f, powerAlpha)
                            : new Color(0.9f, 0.95f, 1f, powerAlpha);
                    DrawRect(new Rect2(bx + 12, wy, bWidth - 24, 14), winColor);
                }
            }
        }

        // Midground transit viaducts and sky-bridges
        DrawLine(new Vector2(0, baseY - 40), new Vector2(1920, baseY - 40), new Color(0.12f, 0.18f, 0.32f, 0.9f), 8f);
        DrawLine(new Vector2(0, baseY + 60), new Vector2(1920, baseY + 60), new Color(0.08f, 0.12f, 0.22f, 0.95f), 12f);
    }

    private void DrawCathedralOverlook(float t)
    {
        // Massive summit terrace foundation
        DrawRect(new Rect2(0, 720, 1920, 360), new Color("0c0a18"));

        // Shattered Gothic cathedral arches on left and right
        DrawRect(new Rect2(60, 200, 90, 560), new Color("141124"));
        DrawRect(new Rect2(1770, 200, 90, 560), new Color("141124"));
        DrawArc(new Vector2(105, 200), 70, Mathf.Pi, Mathf.Tau, 24, new Color("1a1630"), 12f);
        DrawArc(new Vector2(1815, 200), 70, Mathf.Pi, Mathf.Tau, 24, new Color("1a1630"), 12f);

        // Balcony balustrade railing
        DrawRect(new Rect2(200, 700, 1520, 24), new Color("1e1b34"));
        for (int p = 0; p < 24; p++)
        {
            float px = 220 + p * 64f;
            DrawRect(new Rect2(px, 700, 10, 40), new Color("252240"));
        }

        // Shattered Void Commander dais remnants in background right
        DrawRect(new Rect2(1480, 640, 220, 40), new Color("181228"));
        // Broken void katana blades lying on the floor
        DrawLine(new Vector2(1520, 638), new Vector2(1590, 625), new Color(0.8f, 0.15f, 0.25f, 0.7f), 3f);
        DrawLine(new Vector2(1610, 630), new Vector2(1670, 639), new Color(0.7f, 0.2f, 0.9f, 0.7f), 3f);
    }

    private void DrawOperativeSquad(float t)
    {
        // 6 Operatives standing on balcony deck in heroic ensemble formation:
        // Titan (x:620), Oceanus (x:740), Surya (x:860), Kaito (x:980), Mirage (x:1100), Aura (x:1220)
        (string name, float x, Color col, float height)[] squad = {
            ("Titan", 620f, ColorTitan, 78f),
            ("Oceanus", 740f, ColorOceanus, 68f),
            ("Surya", 860f, ColorSurya, 72f),
            ("Kaito", 980f, ColorKaito, 70f),
            ("Mirage", 1100f, ColorMirage, 66f),
            ("Aura", 1220f, ColorAura, 65f)
        };

        float groundY = 700f;

        for (int i = 0; i < squad.Length; i++)
        {
            var op = squad[i];
            Vector2 foot = new(op.x, groundY);
            Vector2 head = new(op.x, groundY - op.height);

            // Soft operative backlight glow from rising sun
            DrawCircle(new Vector2(op.x, groundY - op.height * 0.6f), 38f, new Color(op.col.R, op.col.G, op.col.B, 0.15f));

            // Body silhouette (dark obsidian)
            DrawCapsule(new Vector2(op.x, groundY - op.height * 0.5f), new Vector2(16f, op.height * 0.7f), new Color("0a0714"));

            // Head silhouette
            DrawCircle(head, 9f, new Color("0a0714"));

            // Glowing cybernetic visor / accent trim
            DrawLine(head + new Vector2(-4, 0), head + new Vector2(4, 0), op.col, 2.5f);

            // Glowing shoulder / chassis lines
            DrawLine(head + new Vector2(-10, 15), head + new Vector2(-4, 35), op.col, 2f);
            DrawLine(head + new Vector2(10, 15), head + new Vector2(4, 35), op.col, 2f);

            // Sheathed weapons on hip/back
            if (op.name == "Kaito")
            {
                // Katana scabbard on hip
                DrawLine(foot + new Vector2(-12, -25), foot + new Vector2(18, -10), new Color(0.9f, 0.2f, 0.25f, 0.9f), 3f);
            }
            else if (op.name == "Titan")
            {
                // Heavy hammer resting head-down on the balcony floor
                DrawLine(foot + new Vector2(-16, 0), foot + new Vector2(-16, -55), new Color(0.7f, 0.4f, 0.95f, 0.9f), 4f);
                DrawRect(new Rect2(op.x - 24, groundY - 14, 16, 14), ColorTitan);
            }
        }
    }

    private void DrawCapsule(Vector2 center, Vector2 size, Color color)
    {
        DrawRect(new Rect2(center.X - size.X * 0.5f, center.Y - size.Y * 0.5f, size.X, size.Y), color);
        DrawCircle(new Vector2(center.X, center.Y - size.Y * 0.5f), size.X * 0.5f, color);
        DrawCircle(new Vector2(center.X, center.Y + size.Y * 0.5f), size.X * 0.5f, color);
    }

    private void DrawCinematicOverlays(Vector2 size, float t)
    {
        // 1. Anamorphic 2.39:1 letterbox bars (138px top and bottom on 1080p canvas)
        float barHeight = size.Y * 0.125f;
        DrawRect(new Rect2(0, 0, size.X, barHeight), Colors.Black);
        DrawRect(new Rect2(0, size.Y - barHeight, size.X, barHeight), Colors.Black);

        // Gold framing accent line on bottom bar edge
        DrawLine(new Vector2(0, barHeight), new Vector2(size.X, barHeight), new Color(1f, 0.85f, 0.3f, 0.35f), 1.5f);
        DrawLine(new Vector2(0, size.Y - barHeight), new Vector2(size.X, size.Y - barHeight), new Color(1f, 0.85f, 0.3f, 0.35f), 1.5f);

        // 2. Subtle CRT scanlines
        for (int y = 0; y < size.Y; y += 4)
        {
            DrawLine(new Vector2(0, y), new Vector2(size.X, y), new Color(0f, 0f, 0f, 0.04f));
        }
    }
}
