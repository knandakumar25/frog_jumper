using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Reads the sixteen illustrated poses of one operative without changing its PNG.
/// Transparent gutters define extraction boundaries; contact-pose baselines keep
/// airborne frames airborne and prevent per-frame silhouette recentering.
/// </summary>
public sealed class OperativeStudyAtlas
{
    private static readonly Dictionary<string, OperativeStudyAtlas> Cache = new();

    /// <summary>Original transparent art, retained as a shared Godot texture.</summary>
    public Texture2D Texture { get; }
    /// <summary>Opaque bounds for the sixteen poses in row-major order.</summary>
    public Rect2[] Regions { get; } = new Rect2[16];
    /// <summary>Fixed contact baseline of each illustrated row, in source pixels.</summary>
    public float[] Baselines { get; } = new float[4];
    /// <summary>Standing height used to keep all action poses at the same scale.</summary>
    public float StandingHeight => Regions[0].Size.Y;

    /// <summary>Loads each operative once, avoiding PNG scans during keyboard selection.</summary>
    public static OperativeStudyAtlas Load(string id)
    {
        if (!Cache.TryGetValue(id, out var art))
        {
            art = new OperativeStudyAtlas(GD.Load<Texture2D>(
                $"res://assets/characters/hand_drawn/{id}_poses.png"));
            Cache.Add(id, art);
        }
        return art;
    }

    /// <summary>Finds safe cell gutters and visible bounds using the PNG alpha channel.</summary>
    private OperativeStudyAtlas(Texture2D texture)
    {
        Texture = texture;
        using var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        int width = image.GetWidth(), height = image.GetHeight();
        byte[] pixels = image.GetData();
        int[] rowInk = new int[height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (pixels[(y * width + x) * 4 + 3] > 25) rowInk[y]++;
        int[] rows = Gutters(rowInk);
        for (int row = 0; row < 4; row++)
        {
            int[] columnInk = new int[width];
            for (int y = rows[row]; y < rows[row + 1]; y++)
                for (int x = 0; x < width; x++)
                    if (pixels[(y * width + x) * 4 + 3] > 25) columnInk[x]++;
            int[] columns = Gutters(columnInk);
            for (int col = 0; col < 4; col++)
            {
                int left = columns[col + 1], top = rows[row + 1];
                int right = columns[col], bottom = rows[row];
                for (int y = rows[row]; y < rows[row + 1]; y++)
                    for (int x = columns[col]; x < columns[col + 1]; x++)
                    {
                        if (pixels[(y * width + x) * 4 + 3] <= 25) continue;
                        left = Math.Min(left, x); right = Math.Max(right, x + 1);
                        top = Math.Min(top, y); bottom = Math.Max(bottom, y + 1);
                    }
                if (right <= left || bottom <= top)
                    throw new InvalidOperationException($"Empty pose {row * 4 + col} in {texture.ResourcePath}");
                Regions[row * 4 + col] = new Rect2(left, top, right - left, bottom - top);
            }
        }
        // Grounded windup/release establish row 2; exclude the airborne jump.
        // Row 3 excludes the slam's energy splash, which can extend below the feet.
        Baselines[0] = MaxBottom(0, 1, 2, 3);
        Baselines[1] = MaxBottom(4, 5, 6, 7);
        Baselines[2] = MaxBottom(10, 11);
        Baselines[3] = MaxBottom(12, 13);
    }

    /// <summary>Returns the lowest visible point among specified grounded poses.</summary>
    private float MaxBottom(params int[] poses)
    {
        float bottom = 0;
        foreach (int pose in poses) bottom = Mathf.Max(bottom, Regions[pose].End.Y);
        return bottom;
    }

    /// <summary>
    /// Searches near each expected quarter boundary for the least occupied scanline.
    /// Nearest-to-grid wins ties, keeping large transparent gutters deterministic.
    /// </summary>
    private static int[] Gutters(int[] ink)
    {
        int[] edges = { 0, 0, 0, 0, ink.Length };
        for (int edge = 1; edge < 4; edge++)
        {
            int nominal = ink.Length * edge / 4;
            int radius = ink.Length / 16;
            int best = nominal;
            for (int p = nominal - radius; p <= nominal + radius; p++)
                if (ink[p] < ink[best] || (ink[p] == ink[best] && Math.Abs(p - nominal) < Math.Abs(best - nominal)))
                    best = p;
            edges[edge] = best;
        }
        return edges;
    }
}
