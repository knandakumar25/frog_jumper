using Godot;

/// <summary>Visual and collision styles available to Level 1's palace geometry.</summary>
public enum PalaceSurface
{
    /// <summary>Mossy stone garden masonry used for ground, terraces and cliff faces.</summary>
    Stone,
    /// <summary>Curved ceramic roof tiles used on the rooftop run.</summary>
    Roof,
    /// <summary>Lacquered timber walkway; normally one-way so the operative can jump up through it.</summary>
    Wood,
    /// <summary>Tall carved stone wall that the operative can wall-slide and wall-jump against.</summary>
    Wall,
    /// <summary>Magenta purge-field barrier that seals the dojo core until the beacon is destroyed.</summary>
    Gate,
    /// <summary>Poured concrete with rebar seams and a hazard-striped lip, for the city levels.</summary>
    Concrete,
    /// <summary>Riveted steel grating used for catwalks and gantries; normally one-way.</summary>
    Steel
}

/// <summary>
/// A rectangular static platform that draws itself procedurally, so Level 1 needs no tilemap or art import.
/// The node's <see cref="Node2D.Position"/> is the rectangle's top-left corner; collision is on physics layer 1
/// (the layer the operative collides with) and the node never moves.
/// </summary>
public partial class PalacePlatform : StaticBody2D
{
    /// <summary>Width and height of the platform in world pixels.</summary>
    public Vector2 Size { get; set; } = new(200f, 40f);
    /// <summary>Selects the drawn material and how the surface is textured.</summary>
    public PalaceSurface Surface { get; set; } = PalaceSurface.Stone;
    /// <summary>When true the platform only blocks from above, letting the player jump up through it.</summary>
    public bool OneWay { get; set; }
    /// <summary>True once <see cref="Open"/> has removed a gate's collision.</summary>
    public bool IsOpen { get; private set; }

    private CollisionShape2D _shape = null!;
    private float _time;

    /// <summary>Creates the collision rectangle that matches the drawn rectangle.</summary>
    public override void _Ready()
    {
        CollisionLayer = 1;
        CollisionMask = 0;
        _shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = Size },
            Position = Size * .5f,
            OneWayCollision = OneWay,
            // A generous margin keeps one-way walkways from snagging the player's feet on landing.
            OneWayCollisionMargin = 10f
        };
        AddChild(_shape);
    }

    /// <summary>Only gates animate, so every other surface stays cheap and static.</summary>
    public override void _Process(double delta)
    {
        if (Surface != PalaceSurface.Gate || IsOpen) return;
        _time += (float)delta;
        QueueRedraw();
    }

    /// <summary>
    /// Removes the gate's collision and fades it out. Safe to call repeatedly and outside the scene tree
    /// (unit tests), where the fade is skipped and the gate simply disappears.
    /// </summary>
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        _shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        if (IsInsideTree()) CreateTween().TweenProperty(this, "modulate:a", 0f, .6f);
        else Visible = false;
    }

    /// <summary>Draws the platform in its material style.</summary>
    public override void _Draw() => DrawSurface(this, Size, Surface, OneWay, _time);

    /// <summary>
    /// Shared painter used by static and moving platforms. Detail positions come from a hash of the pixel offset,
    /// so a platform always looks identical between frames and between runs.
    /// </summary>
    /// <param name="canvas">The item whose draw callback is currently executing.</param>
    /// <param name="size">Rectangle size, with the origin at the top-left corner.</param>
    /// <param name="surface">Material style.</param>
    /// <param name="oneWay">True to add a thin lip showing the platform can be jumped through.</param>
    /// <param name="time">Seconds, used only to animate gate energy.</param>
    public static void DrawSurface(CanvasItem canvas, Vector2 size, PalaceSurface surface, bool oneWay, float time)
    {
        switch (surface)
        {
            case PalaceSurface.Roof: DrawRoof(canvas, size); break;
            case PalaceSurface.Wood: DrawWood(canvas, size); break;
            case PalaceSurface.Wall: DrawWall(canvas, size); break;
            case PalaceSurface.Gate: DrawGate(canvas, size, time); break;
            case PalaceSurface.Concrete: DrawConcrete(canvas, size); break;
            case PalaceSurface.Steel: DrawSteel(canvas, size); break;
            default: DrawStone(canvas, size); break;
        }
        if (oneWay && surface != PalaceSurface.Wood && surface != PalaceSurface.Steel)
            canvas.DrawLine(new Vector2(0, 2), new Vector2(size.X, 2), new Color("#f5d9a3"), 3f);
    }

    /// <summary>Deterministic 0..1 value for a pixel coordinate so decoration never flickers.</summary>
    private static float Hash(float x, float y)
    {
        float v = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    /// <summary>Mossy masonry: vertical shading, brick joints, a grass cap and fallen blossom petals.</summary>
    private static void DrawStone(CanvasItem c, Vector2 s)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color("#2a2e3c"));
        // Darken toward the bottom so tall ground masses sink into the background instead of looking flat.
        c.DrawRect(new Rect2(0, s.Y * .45f, s.X, s.Y * .55f), new Color(0, 0, .04f, .28f));
        Color joint = new("#1a1d29");
        const float brickH = 34f, brickW = 78f;
        int row = 0;
        for (float y = 18f; y < s.Y; y += brickH, row++)
        {
            c.DrawLine(new Vector2(0, y), new Vector2(s.X, y), joint, 2f);
            for (float x = (row % 2) * brickW * .5f; x < s.X; x += brickW)
                c.DrawLine(new Vector2(x, y), new Vector2(x, Mathf.Min(y + brickH, s.Y)), joint, 2f);
        }
        // Lit patches: a few lighter bricks give the surface hand-laid variation.
        for (float y = 18f, r = 0; y < s.Y; y += brickH, r++)
            for (float x = (r % 2) * brickW * .5f; x < s.X; x += brickW)
                if (Hash(x, y) > .8f)
                    c.DrawRect(new Rect2(x + 2, y + 2, brickW - 3, brickH - 3), new Color(.62f, .68f, .86f, .09f));
        float capH = Mathf.Min(16f, s.Y);
        c.DrawRect(new Rect2(0, 0, s.X, capH), new Color("#35523f"));
        c.DrawRect(new Rect2(0, 0, s.X, 4f), new Color("#6da36b"));
        for (float x = 6; x < s.X; x += 22f)
        {
            float h = 5f + Hash(x, 1f) * 9f;
            c.DrawLine(new Vector2(x, 2), new Vector2(x + (Hash(x, 2f) - .5f) * 8f, -h), new Color("#6da36b"), 2f);
        }
        for (float x = 14; x < s.X; x += 46f)
            if (Hash(x, 7f) > .35f)
                c.DrawCircle(new Vector2(x + Hash(x, 3f) * 20f, 2 + Hash(x, 4f) * 8f), 2.6f, new Color("#f2a7c3"));
        c.DrawLine(new Vector2(0, 1), new Vector2(s.X, 1), new Color("#a8eae6", .55f), 1.5f);
    }

    /// <summary>Overlapping curved tile rows under a gold ridge capping the top edge.</summary>
    private static void DrawRoof(CanvasItem c, Vector2 s)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color("#4b2a36"));
        Color tile = new("#6c3a45"), tileLight = new("#8e5560"), shade = new("#2e1822");
        const float tileW = 40f, tileH = 20f;
        int row = 0;
        for (float y = 10f; y < s.Y; y += tileH, row++)
        {
            for (float x = (row % 2) * tileW * .5f - tileW * .5f; x < s.X; x += tileW)
            {
                Vector2 center = new(x + tileW * .5f, y);
                c.DrawArc(center, tileW * .5f - 1f, 0, Mathf.Pi, 12, row % 2 == 0 ? tile : tileLight, 6f, true);
                c.DrawArc(center, tileW * .5f - 1f, 0, Mathf.Pi, 12, shade, 1.5f, true);
            }
        }
        c.DrawRect(new Rect2(0, 0, s.X, 8f), new Color("#cf9f58"));
        c.DrawRect(new Rect2(0, 0, s.X, 3f), new Color("#f2d79b"));
        // Gable end caps keep the rectangular collision readable as a roof edge.
        c.DrawRect(new Rect2(0, 0, 8f, s.Y), new Color("#2e1822"));
        c.DrawRect(new Rect2(s.X - 8f, 0, 8f, s.Y), new Color("#2e1822"));
    }

    /// <summary>Lacquered timber planks with a red rail and warm lantern-lit highlights.</summary>
    private static void DrawWood(CanvasItem c, Vector2 s)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color("#5d352f"));
        for (float x = 0; x < s.X; x += 46f)
            c.DrawLine(new Vector2(x, 0), new Vector2(x, s.Y), new Color("#33191b"), 2f);
        c.DrawRect(new Rect2(0, 0, s.X, 8f), new Color("#a8433f"));
        c.DrawRect(new Rect2(0, 0, s.X, 3f), new Color("#ec9378"));
        c.DrawLine(new Vector2(0, s.Y - 1), new Vector2(s.X, s.Y - 1), new Color("#241012"), 3f);
        for (float x = 20; x < s.X; x += 92f)
            c.DrawRect(new Rect2(x - 2, 10, 4, s.Y - 12), new Color("#eebd77", .35f));
    }

    /// <summary>Carved wall faces with vertical seams and glowing glyph inlays that suggest climbable stonework.</summary>
    private static void DrawWall(CanvasItem c, Vector2 s)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color("#303445"));
        c.DrawRect(new Rect2(0, 0, 6f, s.Y), new Color("#161a26"));
        c.DrawRect(new Rect2(s.X - 6f, 0, 6f, s.Y), new Color("#161a26"));
        for (float y = 30f; y < s.Y; y += 56f)
        {
            c.DrawLine(new Vector2(0, y), new Vector2(s.X, y), new Color("#1a1d29"), 2f);
            if (Hash(1f, y) > .55f)
                c.DrawRect(new Rect2(s.X * .5f - 4, y + 12, 8, 24), new Color("#77efff", .3f));
        }
        c.DrawLine(new Vector2(s.X * .5f, 0), new Vector2(s.X * .5f, s.Y), new Color("#1a1d29"), 2f);
        c.DrawLine(new Vector2(1, 0), new Vector2(1, s.Y), new Color("#a8eae6", .4f), 2f);
        c.DrawLine(new Vector2(s.X - 1, 0), new Vector2(s.X - 1, s.Y), new Color("#a8eae6", .4f), 2f);
    }

    /// <summary>Poured concrete: panel seams, stains, a hazard-striped lip and a cyan edge light.</summary>
    private static void DrawConcrete(CanvasItem c, Vector2 s)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color("#3a3d47"));
        c.DrawRect(new Rect2(0, s.Y * .5f, s.X, s.Y * .5f), new Color(0, 0, .04f, .3f));
        Color seam = new("#23252d");
        for (float x = 0; x < s.X; x += 140f) c.DrawLine(new Vector2(x, 0), new Vector2(x, s.Y), seam, 3f);
        for (float y = 70f; y < s.Y; y += 110f) c.DrawLine(new Vector2(0, y), new Vector2(s.X, y), seam, 2f);
        for (float x = 24f; x < s.X; x += 140f)
            for (float y = 30f; y < s.Y; y += 110f)
                if (Hash(x, y) > .55f) c.DrawRect(new Rect2(x, y, 38f + Hash(y, x) * 40f, 5f), new Color("#1d1f26", .5f));
        c.DrawRect(new Rect2(0, 0, s.X, 14f), new Color("#555a66"));
        for (float x = 0; x < s.X; x += 56f)
            c.DrawColoredPolygon(new[] { new Vector2(x, 0), new Vector2(x + 28f, 0), new Vector2(x + 14f, 14f), new Vector2(x - 14f, 14f) }, new Color("#d9b53c", .85f));
        c.DrawLine(new Vector2(0, 1), new Vector2(s.X, 1), new Color("#a8eae6", .5f), 1.5f);
    }

    /// <summary>Steel grating: dark mesh under a bright rail, with rivets and a faint cyan underglow.</summary>
    private static void DrawSteel(CanvasItem c, Vector2 s)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color("#2a3038"));
        for (float x = 0; x < s.X; x += 14f) c.DrawLine(new Vector2(x, 6), new Vector2(x, s.Y), new Color("#161a20"), 2f);
        c.DrawRect(new Rect2(0, 0, s.X, 6f), new Color("#8a97a3"));
        c.DrawRect(new Rect2(0, 0, s.X, 2f), new Color("#d6e1ea"));
        for (float x = 10f; x < s.X; x += 60f) c.DrawCircle(new Vector2(x, 3f), 1.6f, new Color("#3b4550"));
        c.DrawRect(new Rect2(0, s.Y - 2f, s.X, 2f), new Color("#6ff3ff", .35f));
    }

    /// <summary>Humming magenta energy curtain so players read the barrier as an objective-locked door.</summary>
    private static void DrawGate(CanvasItem c, Vector2 s, float time)
    {
        c.DrawRect(new Rect2(Vector2.Zero, s), new Color(.72f, .15f, .5f, .22f));
        c.DrawRect(new Rect2(0, 0, 6f, s.Y), new Color("#b5659c"));
        c.DrawRect(new Rect2(s.X - 6f, 0, 6f, s.Y), new Color("#b5659c"));
        for (float y = 0; y < s.Y; y += 26f)
        {
            float offset = Mathf.Sin(time * 4f + y * .08f) * s.X * .22f;
            c.DrawLine(new Vector2(s.X * .5f - 10f + offset, y), new Vector2(s.X * .5f + 10f - offset, y + 13f), new Color("#f2adc7", .85f), 2.5f);
        }
    }
}
