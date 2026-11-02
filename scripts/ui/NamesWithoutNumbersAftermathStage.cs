using Godot;

/// <summary>Seekable 2D tableau of an open civic processing district, returning records and civilians.</summary>
public partial class NamesWithoutNumbersAftermathStage : Control
{
    /// <summary>Asset root shared with the parent overlay's optional atmosphere stream.</summary>
    [Export] public string ArtPath { get; set; } = "res://assets/cutscenes/names_without_numbers/";
    /// <summary>Timeline sample supplied by the overlay; motion never accumulates frame error.</summary>
    public float Seconds { get; set; }
    /// <summary>Essential scene plates gate playback; their absence requests a safe results handoff.</summary>
    public bool ResourcesAvailable => _district != null && _lights != null;

    private Texture2D? _district;
    private Texture2D? _lights;
    private static readonly Color[] OperativeColors = { new("e7a832"), new("36cda5"), new("75c5e8"), new("b57de5"), new("e6b8d4") };

    /// <summary>Loads the two original transparent district plates without emitting missing-file errors.</summary>
    public override void _Ready()
    {
        _district = Load(ArtPath + "district_structure.png");
        _lights = Load(ArtPath + "district_lights.png");
    }

    /// <summary>Returns null quietly for absent essential art so the owning overlay can finish safely.</summary>
    private static Texture2D? Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>Composes the district, record return, civilians and squad reactions analytically at any viewport size.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("071018"));
        if (!ResourcesAvailable) return;
        float t = Mathf.Clamp(Seconds, 0, 90);
        int shot = t < 12 ? 0 : t < 22 ? 1 : t < 31 ? 2 : t < 40 ? 3 : t < 49 ? 4 : t < 58 ? 5 : t < 68 ? 6 : 7;
        float fit = Mathf.Min(Size.X / 1920, Size.Y / 1080);
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) * .5f;
        DrawSetTransform(margin, 0, Vector2.One * fit);
        DrawTextureRect(_district!, new Rect2(0, 0, 1920, 1080), false);
        DrawTextureRect(_lights!, new Rect2(0, 0, 1920, 1080), false, new Color(1, 1, 1, .9f));
        float focusX = shot switch { 0 or 1 => 1010, 2 => 1410, 3 => 520, 4 => 910, 5 => 1320, 6 => 420, _ => 1020 };
        float zoom = shot is 0 or 1 ? 1.12f : shot == 7 ? 1.03f : 1.18f;
        DrawSetTransform(margin + (new Vector2(960, 480) - new Vector2(focusX, 500) * zoom) * fit, 0, Vector2.One * fit * zoom);

        // Paper records move from the terminal to waiting hands only after the first resident names herself.
        float returnProgress = Mathf.Clamp((t - 8) / 48, 0, 1);
        DrawRect(new Rect2(825, 646, 184, 72), new Color("263941"));
        DrawRect(new Rect2(846, 660, 142, 7), new Color("e4c98b"));
        DrawRect(new Rect2(846, 676, 142, 7), new Color("b9d5c6"));
        DrawLine(new Vector2(915, 720), new Vector2(Mathf.Lerp(915, 1090, returnProgress), 770), new Color("dfc99c", .75f), 2);

        // Civilians gather at their own pace. Their poses are authored in code so no procedural pose can drift.
        for (int i = 0; i < 7; i++)
        {
            float x = 935 + i * 91 + (i > 0 ? Mathf.Min(t, 24) * (i % 2 == 0 ? .9f : -.45f) : 0);
            float feet = 828 + (i % 2) * 3;
            float scale = i == 0 ? 1.08f : .82f + (i % 3) * .08f;
            Color coat = i == 0 ? new Color("79918a") : new Color(i % 2 == 0 ? "495c64" : "655c62");
            DrawCivilian(x, feet, scale, coat, i == 0 && t < 13);
        }
        if (shot >= 2 && shot <= 6)
        {
            int operative = shot switch { 2 => 4, 3 => 3, 4 => 2, 5 => 1, _ => 0 };
            DrawOperative(340, 826, OperativeColors[operative], operative);
            DrawString(ThemeDB.FallbackFont, new Vector2(215, 905), new[] { "KAITO", "MIRAGE", "AURA", "OCEANUS", "TITAN" }[operative], fontSize: 20, modulate: OperativeColors[operative]);
        }
        if (shot == 6)
        {
            float pulse = .5f + .5f * Mathf.Sin(t * 3);
            DrawArc(new Vector2(178, 335), 18 + pulse * 9, 0, Mathf.Tau, 32, new Color("e7a832", .6f), 3);
            DrawString(ThemeDB.FallbackFont, new Vector2(92, 385), "SURYA // COMMS", fontSize: 17, modulate: new Color("e7a832"));
        }
        if (shot == 7)
        {
            DrawOperative(1360, 826, OperativeColors[0], 0);
            DrawRect(new Rect2(0, 0, 1920, 1080), new Color(0, 0, 0, Mathf.Clamp((t - 86) / 4, 0, 1)));
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Draws a civilian with a steady silhouette and visible paper in hand during her chosen introduction.</summary>
    private void DrawCivilian(float x, float feet, float scale, Color coat, bool holdingRecord)
    {
        DrawCircle(new Vector2(x, feet - 117 * scale), 22 * scale, new Color("b98e70"));
        DrawRect(new Rect2(x - 19 * scale, feet - 151 * scale, 38 * scale, 10 * scale), new Color("273a42"));
        DrawPolygon(new Vector2[] { new(x - 34 * scale, feet - 88 * scale), new(x + 34 * scale, feet - 88 * scale), new(x + 47 * scale, feet - 14 * scale), new(x - 46 * scale, feet - 14 * scale) }, new Color[] { coat });
        DrawLine(new Vector2(x - 22 * scale, feet - 15 * scale), new Vector2(x - 26 * scale, feet), new Color("26323a"), 12 * scale);
        DrawLine(new Vector2(x + 22 * scale, feet - 15 * scale), new Vector2(x + 28 * scale, feet), new Color("26323a"), 12 * scale);
        DrawLine(new Vector2(x + 25 * scale, feet - 76 * scale), new Vector2(x + 50 * scale, feet - 57 * scale), new Color("b98e70"), 8 * scale);
        if (holdingRecord) DrawRect(new Rect2(x + 45 * scale, feet - 70 * scale, 27 * scale, 35 * scale), new Color("ead9af"));
    }

    /// <summary>Draws a simplified, scene-scale operative silhouette while preserving each established squad color.</summary>
    private void DrawOperative(float x, float feet, Color color, int id)
    {
        DrawCircle(new Vector2(x, feet - 125), 26, new Color("c69b7d"));
        DrawPolygon(new Vector2[] { new(x - 40, feet - 91), new(x + 40, feet - 91), new(x + 48, feet - 14), new(x - 47, feet - 14) }, new Color[] { color });
        DrawLine(new Vector2(x - 21, feet - 14), new Vector2(x - 25, feet), new Color("202a37"), 14);
        DrawLine(new Vector2(x + 21, feet - 14), new Vector2(x + 27, feet), new Color("202a37"), 14);
        if (id == 0) DrawLine(new Vector2(x + 38, feet - 82), new Vector2(x + 63, feet - 26), new Color("c8d4d7"), 4);
    }
}
