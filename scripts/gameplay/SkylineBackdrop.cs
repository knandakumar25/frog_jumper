using Godot;

/// <summary>Colour schemes for the procedural city skyline used by levels without a painted backdrop.</summary>
public enum SkylinePalette
{
    /// <summary>Calm indigo night with warm windows.</summary>
    Night,
    /// <summary>Burning district: dark red sky, orange glow, amber windows (Level 2).</summary>
    Wildfire,
    /// <summary>Dead comm district: teal and rust haze with cyan and magenta signage (Level 3).</summary>
    NeonRust,
    /// <summary>The fortified border: deep violet sky with cold white lights (Level 4).</summary>
    Citadel
}

/// <summary>
/// A layered parallax city skyline drawn entirely in code, in screen space on a background CanvasLayer.
/// Four silhouette layers scroll at different fractions of the camera's motion; windows and towers are placed from a
/// deterministic hash so the city never flickers or changes between frames. The owner feeds it
/// <see cref="CameraCenter"/> and <see cref="ViewSize"/> every frame.
/// </summary>
public partial class SkylineBackdrop : Node2D
{
    /// <summary>Colour scheme.</summary>
    public SkylinePalette Palette { get; set; } = SkylinePalette.Night;
    /// <summary>World position at the centre of the camera's view.</summary>
    public Vector2 CameraCenter { get; set; }
    /// <summary>Visible area in canvas pixels.</summary>
    public Vector2 ViewSize { get; set; } = new(1920f, 1080f);

    private float _time;

    /// <summary>Redraws every frame because the layers scroll with the camera and the glow flickers.</summary>
    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    /// <summary>Deterministic 0..1 value for a pair of numbers.</summary>
    private static float Hash(float x, float y)
    {
        float v = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    /// <summary>Returns (sky top, sky bottom, far, mid, near, window, glow) colours for the palette.</summary>
    private (Color, Color, Color, Color, Color, Color, Color) Colours() => Palette switch
    {
        SkylinePalette.Wildfire => (new Color("#1b0a12"), new Color("#7a2a1a"), new Color("#3a1418"), new Color("#2a0f14"), new Color("#170a0e"), new Color("#ffb34a"), new Color("#ff7a2e")),
        SkylinePalette.NeonRust => (new Color("#08141c"), new Color("#2c3e40"), new Color("#1d3138"), new Color("#15242b"), new Color("#0d171c"), new Color("#6ff3ff"), new Color("#ff5fb8")),
        SkylinePalette.Citadel => (new Color("#0b0a22"), new Color("#3b2a68"), new Color("#2a2152"), new Color("#1d1840"), new Color("#110e2a"), new Color("#e6eeff"), new Color("#8f7bff")),
        _ => (new Color("#080b1f"), new Color("#2a3566"), new Color("#1f2a55"), new Color("#161e42"), new Color("#0d1230"), new Color("#ffd8a0"), new Color("#7a8cff"))
    };

    /// <summary>Draws the sky gradient, a horizon glow, then four silhouette layers from far to near.</summary>
    public override void _Draw()
    {
        var (top, bottom, far, mid, near, window, glow) = Colours();
        float w = ViewSize.X, h = ViewSize.Y;
        DrawPolygon(new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, h), new Vector2(0, h) },
            new[] { top, top, bottom, bottom });
        // A soft horizon glow: stacked translucent bands that gently breathe, as if from distant fires or signage.
        float pulse = .85f + Mathf.Sin(_time * 1.3f) * .15f;
        for (int i = 0; i < 6; i++)
            DrawRect(new Rect2(0, h * .55f + i * h * .07f, w, h * .07f), new Color(glow, (.05f + i * .025f) * pulse));

        Color[] colours = { far, mid, near, near.Darkened(.35f) };
        float[] factors = { .06f, .12f, .22f, .38f };
        float[] baseline = { .74f, .80f, .88f, 1.02f };
        float[] maxHeight = { .42f, .5f, .55f, .5f };
        for (int layer = 0; layer < 4; layer++) DrawLayer(layer, colours[layer], factors[layer], baseline[layer], maxHeight[layer], window);
    }

    /// <summary>Draws one layer of building silhouettes with lit windows, scrolled by <paramref name="factor"/> of camera motion.</summary>
    private void DrawLayer(int layer, Color colour, float factor, float baselineFraction, float maxHeightFraction, Color window)
    {
        float w = ViewSize.X, h = ViewSize.Y;
        float scroll = CameraCenter.X * factor;
        float baseY = h * baselineFraction - (CameraCenter.Y + 250f) * factor * .15f;
        float step = 150f + layer * 40f;
        int first = Mathf.FloorToInt(scroll / step) - 1, last = Mathf.CeilToInt((scroll + w) / step) + 2;
        for (int k = first; k <= last; k++)
        {
            float bw = step * (.7f + Hash(k, layer) * .6f);
            float bh = h * maxHeightFraction * (.25f + Hash(k, layer + 9f) * .75f);
            float x = k * step - scroll;
            DrawRect(new Rect2(x, baseY - bh, bw, bh + h), colour);
            // Antenna on some towers makes the skyline read as a city rather than blocks.
            if (Hash(k, layer + 3f) > .7f) DrawLine(new Vector2(x + bw * .5f, baseY - bh), new Vector2(x + bw * .5f, baseY - bh - 40f - layer * 10f), colour, 3f);
            // Windows: only on the nearer layers, where individual lights are large enough to see.
            if (layer < 1) continue;
            // Window lighting is keyed on (building, row, column) rather than screen position so it never flickers while scrolling.
            int row = 0;
            for (float wy = baseY - bh + 14f; wy < baseY - 8f; wy += 26f, row++)
            {
                int col = 0;
                for (float wx = x + 10f; wx < x + bw - 12f; wx += 24f, col++)
                    if (Hash(k * 31 + col, row + layer * 97) > .72f)
                        DrawRect(new Rect2(wx, wy, 8f, 12f), new Color(window, .35f + .35f * Hash(col + k, row)));
            }
        }
    }
}
