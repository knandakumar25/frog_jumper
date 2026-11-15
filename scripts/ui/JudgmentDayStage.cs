using System;
using Godot;

/// <summary>
/// Seekable 2D procedural cinematic tableau for the Mission 12 prologue ("The Final Reckoning // Judgment Day Intro").
/// Composes the Grand Citadel Summit Throne Room &amp; Void Singularity Chamber at 5,000m elevation.
/// Procedurally renders the catastrophic cosmic singularity core, swirling accretion disk, coronal flares,
/// monumental cathedral obsidian architecture, reflective polished floor, the floating throne platform
/// with the Void Commander wielding twin dual-phase energy blades, and the full six-operative squad of Axiom Force
/// preparing for the final battle of the campaign.
/// </summary>
public partial class JudgmentDayStage : Control
{
    /// <summary>Asset root shared with parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/judgment_day_intro/";

    /// <summary>Monotonic timeline sample in seconds provided by the cutscene controller.</summary>
    public float Seconds { get; set; }

    /// <summary>Requires authored environment art and cue metadata; the owner supplies a safe completion fallback.</summary>
    public bool ResourcesAvailable => CampaignArtwork.Available("judgment_day", ArtPath);

    // Distinctive operative squad palette
    private static readonly Color ColorKaito = new("f87171");    // Crimson Vanguard
    private static readonly Color ColorMirage = new("34d399");   // Phase Emerald
    private static readonly Color ColorAura = new("f472b6");     // Prism Pink
    private static readonly Color ColorOceanus = new("66d9e8");  // Hydro Aqua
    private static readonly Color ColorTitan = new("c084fc");    // Kinetic Violet
    private static readonly Color ColorSurya = new("ffe29a");    // Solar Gold

    // Cosmic Void & Chamber Palette
    private static readonly Color ColorCyan = new("00f0ff");     // Cyber Cyan
    private static readonly Color ColorAmber = new("fbbf24");    // Coronal Gold
    private static readonly Color ColorAlert = new("ff2255");    // Critical Crimson
    private static readonly Color ColorVoidCore = new("05000d"); // Pure Void Black
    private static readonly Color ColorVoidPurple = new("7928ca"); // Dark Matter Violet
    private static readonly Color ColorVoidMagenta = new("ec4899"); // Singularity Magenta
    private static readonly Color ColorFloor = new("0a0514");    // Reflective Obsidian
    private static readonly Color ColorPillar = new("140924");   // Citadel Column

    // Shot boundary schedule matching JudgmentDayIntro dialogue cues (90 seconds)
    private static readonly float[] ShotTimes = { 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f };

    // Camera focal coordinates (X, Y) and zoom per shot
    private static readonly Vector2[] CameraTargets = {
        new(960, 540),  // Shot 0: Wide shot - Throne room cathedral, singularity core, elevated throne dais
        new(820, 560),  // Shot 1: Surya raising solar bulwark shield against cosmic gravitational waves
        new(720, 560),  // Shot 2: Oceanus tracking planetary power conduits surging into the core
        new(1120, 480), // Shot 3: Mirage observing the Void Commander high on the floating dais
        new(1240, 560), // Shot 4: Titan slamming electromagnetic knuckles together
        new(960, 540),  // Shot 5: Aura sensing ultrasonic psychic resonance from the 11 sectors below
        new(1060, 420), // Shot 6: Low-angle dramatic look up at the floating throne: Void Commander rising, blades igniting
        new(880, 550),  // Shot 7: Squad group shot powering up abilities for the final strike
        new(960, 540)   // Shot 8: Epic wide standoff: Kaito leading Axiom Force to strike down the Commander
    };

    private static readonly float[] CameraZooms = {
        1.00f, // Shot 0: Majestic wide
        1.18f, // Shot 1: Surya focus
        1.15f, // Shot 2: Oceanus focus
        1.14f, // Shot 3: Mirage / Dais
        1.18f, // Shot 4: Titan focus
        1.12f, // Shot 5: Aura focus
        1.25f, // Shot 6: Commander focus
        1.15f, // Shot 7: Squad synergy
        0.98f  // Shot 8: Epic finale wide
    };

    /// <summary>
    /// Renders the complete procedural scene with dynamic camera tracking, singularity physics,
    /// architectural perspective, character silhouettes, and cinematic letterboxing.
    /// </summary>
    /// <param name="viewport">The active viewport canvas item.</param>
    public override void _Draw()
    {
        Vector2 viewport = GetViewportRect().Size;
        Vector2 size = new(1920, 1080);
        if (size.X <= 0 || size.Y <= 0) size = new Vector2(1920, 1080);

        int shot = ResolveCurrentShot(Seconds);
        float shotProgress = ComputeShotProgress(Seconds, shot);

        // Smooth camera focal point interpolation
        Vector2 camTarget = CameraTargets[Math.Clamp(shot, 0, CameraTargets.Length - 1)];
        camTarget = CampaignArtwork.Focus(ArtPath, Seconds, camTarget);
        float camZoom = CameraZooms[Math.Clamp(shot, 0, CameraZooms.Length - 1)];

        if (shot < CameraTargets.Length - 1)
        {
            float t = EaseInOut(shotProgress);
            camTarget = camTarget.Lerp(CameraTargets[shot + 1], t * 0.25f);
            camZoom = Mathf.Lerp(camZoom, CameraZooms[shot + 1], t * 0.25f);
        }

        // Apply camera transformation centered on (size / 2)
        float fit = Mathf.Min(viewport.X / 1920, viewport.Y / 1080);
        Vector2 margin = (viewport - size * fit) * .5f;
        DrawSetTransform(margin + (size * .5f - camTarget * camZoom) * fit, 0, Vector2.One * fit * camZoom);

        // 1. Cosmic Void Singularity Background Sky & Accretion Swirl
        DrawCosmicSkyAndSingularity(size, Seconds);
        CampaignArtwork.DrawEnvironment(this, "judgment_day", Seconds, aftermath: false, drawBase: false);

        // 2. Grand Cathedral Throne Room Architecture & Columns
        DrawCathedralArchitecture(size, Seconds);

        // 3. Reflective Obsidian Floor & Power Conduits
        DrawReflectiveFloorAndConduits(size, Seconds);

        // 4. Floating Throne Platform & Void Commander
        DrawFloatingThroneAndCommander(size, Seconds, shot);

        // 5. Axiom Force Full Squad Silhouettes & Powers
        DrawAxiomForceSquad(size, Seconds, shot);

        // 6. Reset transform for UI / Overlay presentation
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

        // 7. Atmospheric Dust Motes & Cosmic Arcs
        DrawCosmicParticles(viewport, Seconds);

        // 8. CRT Scanlines & Anamorphic Letterbox Bars
        DrawCinemaOverlays(viewport);
    }

    /// <summary>Draws the swirling cosmic void singularity, event horizon, and coronal flare emissions.</summary>
    private void DrawCosmicSkyAndSingularity(Vector2 size, float t)
    {
        // Dark space gradient backdrop
        Rect2 skyRect = new(-400, -300, size.X + 800, size.Y + 600);
        DrawRect(skyRect, new Color(0.01f, 0.005f, 0.03f, 1f));

        Vector2 singularityCenter = new(size.X * 0.5f, size.Y * 0.35f);

        // Outer gravitational accretion disk glow
        for (int r = 480; r >= 160; r -= 40)
        {
            float alpha = 0.04f + 0.02f * Mathf.Sin(t * 1.5f + r * 0.05f);
            Color ringCol = (r % 80 == 0) ? ColorVoidMagenta : ColorVoidPurple;
            DrawCircle(singularityCenter, r, new Color(ringCol.R, ringCol.G, ringCol.B, alpha));
        }

        // Swirling coronal energy flares
        int flareCount = 18;
        for (int i = 0; i < flareCount; i++)
        {
            float angle = (i / (float)flareCount) * Mathf.Tau + t * 0.6f;
            float flareLength = 260f + 60f * Mathf.Sin(t * 3f + i * 2f);
            Vector2 flareEnd = singularityCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f) * flareLength;
            Color flareCol = (i % 2 == 0) ? ColorAmber : ColorAlert;
            DrawLine(singularityCenter, flareEnd, new Color(flareCol.R, flareCol.G, flareCol.B, 0.35f), 3.5f);
        }

        // Accretion disk orbiting particle stream (50 particles)
        for (int p = 0; p < 50; p++)
        {
            float pSeed = p * 13.37f;
            float radius = 180f + (p * 4.5f);
            float speed = 1.2f + (300f / MathF.Max(radius, 50f)) * 0.5f;
            float theta = pSeed + t * speed;
            Vector2 pPos = singularityCenter + new Vector2(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius * 0.38f);
            float pSize = 1.5f + 1.5f * Mathf.Sin(pSeed);
            Color pColor = (p % 3 == 0) ? ColorCyan : (p % 3 == 1) ? ColorAmber : ColorVoidMagenta;
            DrawCircle(pPos, pSize, new Color(pColor.R, pColor.G, pColor.B, 0.75f));
        }

        // Gravitational lens halo ring
        DrawArc(singularityCenter, 165f, 0f, Mathf.Tau, 48, new Color(ColorCyan.R, ColorCyan.G, ColorCyan.B, 0.6f + 0.2f * Mathf.Sin(t * 4f)), 4f);

        // Event Horizon (Absolute Pitch Black Core)
        DrawCircle(singularityCenter, 150f, ColorVoidCore);
    }

    /// <summary>Draws the massive brutalist obsidian columns and vaulted cathedral ceiling arches.</summary>
    private void DrawCathedralArchitecture(Vector2 size, float t)
    {
        // Ceiling Vault Struts
        Vector2 vaultApex = new(size.X * 0.5f, 20f);
        for (int c = 0; c < 5; c++)
        {
            float xOffset = c * 380f;
            DrawLine(vaultApex, new Vector2(xOffset, 420f), new Color(0.12f, 0.05f, 0.22f, 0.5f), 6f);
            DrawLine(vaultApex, new Vector2(size.X - xOffset, 420f), new Color(0.12f, 0.05f, 0.22f, 0.5f), 6f);
        }

        // Colossal Flanking Obsidian Columns (Left and Right)
        float[] columnX = { 100f, 260f, size.X - 260f, size.X - 100f };
        for (int i = 0; i < columnX.Length; i++)
        {
            float cx = columnX[i];
            Rect2 colRect = new(cx - 50f, 80f, 100f, 860f);
            DrawRect(colRect, ColorPillar);
            DrawRect(new Rect2(cx - 52f, 80f, 4f, 860f), new Color(0.08f, 0.03f, 0.16f));
            DrawRect(new Rect2(cx + 48f, 80f, 4f, 860f), new Color(0.18f, 0.08f, 0.32f));

            // Pulsing energy glyph conduits along column centers
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3f + i * 1.2f);
            Color glyphCol = (i % 2 == 0) ? ColorCyan : ColorVoidMagenta;
            DrawLine(new Vector2(cx, 100f), new Vector2(cx, 840f), new Color(glyphCol.R, glyphCol.G, glyphCol.B, 0.4f * pulse), 3f);
        }
    }

    /// <summary>Draws the polished reflective obsidian dais floor with isometric perspective grid and power conduits.</summary>
    private void DrawReflectiveFloorAndConduits(Vector2 size, float t)
    {
        float floorY = size.Y * 0.62f;
        Rect2 floorRect = new(-400, floorY, size.X + 800, size.Y - floorY + 300);
        DrawRect(floorRect, ColorFloor);

        // Specular floor sheen reflecting the singularity core above
        Vector2 singularityMirror = new(size.X * 0.5f, floorY + 120f);
        DrawCircle(singularityMirror, 200f, new Color(ColorVoidMagenta.R, ColorVoidMagenta.G, ColorVoidMagenta.B, 0.07f));
        DrawCircle(singularityMirror, 120f, new Color(ColorAmber.R, ColorAmber.G, ColorAmber.B, 0.05f));

        // Perspective floor grid lines receding toward horizon
        Vector2 vanishPt = new(size.X * 0.5f, floorY);
        for (int x = -200; x <= size.X + 200; x += 180)
        {
            Vector2 floorEdge = new(x, size.Y + 200);
            DrawLine(vanishPt, floorEdge, new Color(0.2f, 0.1f, 0.35f, 0.3f), 1.5f);
        }

        // Horizontal depth lines
        for (float y = floorY + 40f; y < size.Y + 150f; y += (y - floorY) * 0.45f)
        {
            DrawLine(new Vector2(-400, y), new Vector2(size.X + 400, y), new Color(0.2f, 0.1f, 0.35f, 0.3f), 1.5f);
        }

        // Planetary power conduits converging toward center dais
        float conduitFlow = (t * 220f) % 180f;
        DrawLine(new Vector2(260, size.Y + 100), new Vector2(size.X * 0.5f - 120, floorY + 80), new Color(ColorCyan.R, ColorCyan.G, ColorCyan.B, 0.7f), 4f);
        DrawLine(new Vector2(size.X - 260, size.Y + 100), new Vector2(size.X * 0.5f + 120, floorY + 80), new Color(ColorCyan.R, ColorCyan.G, ColorCyan.B, 0.7f), 4f);
    }

    /// <summary>Draws the elevated floating dais, cybernetic throne, and the imposing Void Commander.</summary>
    private void DrawFloatingThroneAndCommander(Vector2 size, float t, int shot)
    {
        // Floating Dais Platform position (centered beneath singularity)
        float hoverY = (size.Y * 0.48f) + 12f * Mathf.Sin(t * 1.8f);
        Vector2 daisCenter = new(size.X * 0.5f, hoverY);

        // Anti-gravity repulsion rings beneath dais
        DrawArc(daisCenter + new Vector2(0, 35f), 130f, 0f, Mathf.Tau, 32, new Color(ColorVoidMagenta.R, ColorVoidMagenta.G, ColorVoidMagenta.B, 0.5f + 0.3f * Mathf.Sin(t * 4f)), 3f);
        DrawArc(daisCenter + new Vector2(0, 45f), 90f, 0f, Mathf.Tau, 24, new Color(ColorCyan.R, ColorCyan.G, ColorCyan.B, 0.6f + 0.2f * Mathf.Sin(t * 5f)), 2.5f);

        // Floating Obsidian Dais Slab
        Vector2[] daisPoints = {
            daisCenter + new Vector2(-160, -10),
            daisCenter + new Vector2(160, -10),
            daisCenter + new Vector2(130, 25),
            daisCenter + new Vector2(-130, 25)
        };
        DrawPolygon(daisPoints, new[] { new Color(0.12f, 0.05f, 0.22f), new Color(0.12f, 0.05f, 0.22f), new Color(0.06f, 0.02f, 0.12f), new Color(0.06f, 0.02f, 0.12f) });
        DrawPolyline(daisPoints, ColorAmber, 2f);

        // Massive Cybernetic Imperial Throne behind Commander
        Vector2 throneBase = daisCenter + new Vector2(0, -10);
        Vector2[] throneBack = {
            throneBase + new Vector2(-70, 0),
            throneBase + new Vector2(-60, -140),
            throneBase + new Vector2(0, -170),
            throneBase + new Vector2(60, -140),
            throneBase + new Vector2(70, 0)
        };
        DrawColoredPolygon(throneBack, new Color(0.08f, 0.03f, 0.15f));
        DrawPolyline(throneBack, new Color(ColorVoidMagenta.R, ColorVoidMagenta.G, ColorVoidMagenta.B, 0.6f), 2.5f);

        // Throne imperial glowing glyph
        DrawCircle(throneBase + new Vector2(0, -110), 16f, new Color(ColorAlert.R, ColorAlert.G, ColorAlert.B, 0.6f + 0.3f * Mathf.Sin(t * 3f)));

        // Silhouette of the VOID COMMANDER
        Vector2 cmdPos = daisCenter + new Vector2(0, -10);

        CampaignArtwork.DrawCommander(this, cmdPos);
    }

    /// <summary>Draws all six operatives of Axiom Force in battle formation facing the dais.</summary>
    private void DrawAxiomForceSquad(Vector2 size, float t, int shot)
    {
        CampaignArtwork.DrawSquad(this, ArtPath, Seconds);
    }

    /// <summary>Draws drifting cosmic dust motes and void lightning sparks across the screen.</summary>
    private void DrawCosmicParticles(Vector2 size, float t)
    {
        for (int i = 0; i < 35; i++)
        {
            float seed = i * 27.81f;
            float px = (seed * 83f + t * 45f) % size.X;
            float py = (seed * 117f + Mathf.Sin(t + seed) * 30f) % size.Y;
            float pAlpha = 0.2f + 0.3f * Mathf.Sin(t * 2f + seed);
            Color dotCol = (i % 2 == 0) ? ColorVoidMagenta : ColorAmber;
            DrawCircle(new Vector2(px, py), 1.5f, new Color(dotCol.R, dotCol.G, dotCol.B, pAlpha));
        }
    }

    /// <summary>Draws CRT scanlines and 2.39:1 anamorphic cinema bars.</summary>
    private void DrawCinemaOverlays(Vector2 size)
    {
        // 2.39:1 Anamorphic Cinema Letterbox Bars
        float barHeight = MathF.Max(0f, (size.Y - (size.X / 2.39f)) * 0.5f);
        if (barHeight > 0)
        {
            DrawRect(new Rect2(0, 0, size.X, barHeight), Colors.Black);
            DrawRect(new Rect2(0, size.Y - barHeight, size.X, barHeight), Colors.Black);
        }

        // Subtle CRT Scanlines
        for (int y = 0; y < (int)size.Y; y += 4)
        {
            DrawLine(new Vector2(0, y), new Vector2(size.X, y), new Color(0f, 0f, 0f, 0.08f), 1f);
        }
    }

    private static int ResolveCurrentShot(float time)
    {
        for (int i = 0; i < ShotTimes.Length - 1; i++)
        {
            if (time >= ShotTimes[i] && time < ShotTimes[i + 1]) return i;
        }
        return ShotTimes.Length - 2;
    }

    private static float ComputeShotProgress(float time, int shot)
    {
        if (shot < 0 || shot >= ShotTimes.Length - 1) return 0f;
        float start = ShotTimes[shot];
        float end = ShotTimes[shot + 1];
        return Math.Clamp((time - start) / (end - start), 0f, 1f);
    }

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);
}
