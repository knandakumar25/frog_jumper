using Godot;

/// <summary>Isolated selection-screen art study; never changes campaign stats or saves.</summary>
public partial class ShogunArtPreview : Control
{
    public string CurrentAction { get; private set; } = "Idle";
    private Texture2D? _atlas;
    private Texture2D? _locomotionAtlas;
    private float _elapsed;
    private readonly Color _cyan = new(0.25f, 0.9f, 1f);
    // Explicit art regions preserve long blades and scarf tails in the drawn sheet.
    // Generated pose sheets are not guaranteed to obey a uniform tile boundary.
    private readonly Rect2[] _regions = {
        new(40, 70, 240, 260), new(360, 70, 220, 260),
        new(680, 70, 230, 260), new(990, 70, 240, 260),
        new(20, 400, 260, 230), new(350, 400, 260, 230),
        new(660, 420, 260, 210), new(990, 390, 240, 235),
        new(20, 670, 270, 260), new(350, 700, 320, 230),
        new(690, 680, 230, 250), new(970, 680, 270, 250),
        new(20, 1010, 330, 200), new(370, 950, 270, 280),
        new(680, 950, 250, 280), new(950, 1010, 300, 244)
    };

    public override void _Ready()
    {
        _atlas = GD.Load<Texture2D>("res://assets/characters/shogun_study/poses.png");
        _locomotionAtlas = GD.Load<Texture2D>("res://assets/characters/shogun_study/locomotion.png");
        if (_atlas != null)
        {
            using var image = _atlas.GetImage();
            for (int i = 0; i < _regions.Length; i++)
            {
                var region = _regions[i];
                using var crop = image.GetRegion(new Rect2I((Vector2I)region.Position, (Vector2I)region.Size));
                var used = crop.GetUsedRect();
                _regions[i] = new Rect2(region.Position + used.Position, used.Size);
            }
        }
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Preview(string action)
    {
        CurrentAction = action;
        _elapsed = 0;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _elapsed += (float)delta;
        if (_elapsed > 1.6f && CurrentAction is not ("Idle" or "Walk" or "Run"))
            Preview("Idle");
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#101b2c"));
        float ground = Size.Y - 28;
        DrawLine(new Vector2(12, ground), new Vector2(Size.X - 12, ground), _cyan.Darkened(.6f), 2);
        if (_atlas == null) return;
        if (CurrentAction is "Walk" or "Run" && _locomotionAtlas != null)
        {
            DrawLocomotion(ground);
            return;
        }
        int pose = 0;
        float x = 0, lift = 0;
        float t = Mathf.Clamp(_elapsed / 1.2f, 0, 1);
        switch (CurrentAction)
        {
            case "Jump":
            case "Double jump":
                pose = t < .15f ? 6 : 7;
                lift = Mathf.Sin(t * Mathf.Pi) * 90;
                if (CurrentAction == "Double jump") lift += Mathf.Abs(Mathf.Sin(t * Mathf.Tau)) * 35;
                break;
            case "Katana": pose = _elapsed < .2f ? 8 : 9; break;
            case "Martial strike": pose = _elapsed < .2f ? 10 : 11; break;
            case "Shogun's Flash":
            case "Cyber Dash":
                pose = 12;
                x = (Mathf.Clamp(_elapsed * 3, 0, 1) - .5f) * Size.X * .35f;
                break;
            case "Kinetic Parry": pose = 13; break;
            case "Ground Slam":
                pose = _elapsed < .6f ? 14 : 15;
                lift = _elapsed < .6f ? Mathf.Sin(_elapsed / .6f * Mathf.Pi) * 90 : 0;
                if (_elapsed >= .6f)
                    DrawArc(new Vector2(Size.X / 2, ground - 5), 20 + (_elapsed - .6f) * 90,
                        Mathf.Pi, Mathf.Tau, 32, new Color(_cyan, Mathf.Max(0, 1.5f - _elapsed)), 3, true);
                break;
            default: pose = (int)(_elapsed * 2) % 2; break;
        }
        float side = Mathf.Min(Size.X * .76f, 440);
        Rect2 source = _regions[pose];
        Vector2 drawnSize = source.Size * (side / 320);
        var destination = new Rect2(new Vector2((Size.X - drawnSize.X) / 2 + x,
            ground - drawnSize.Y - lift), drawnSize);
        DrawTextureRectRegion(_atlas, destination, source);
    }

    private void DrawLocomotion(float ground)
    {
        bool running = CurrentAction == "Run";
        // A complete left/right stride, with a stable canvas anchor. Do not
        // recenter each silhouette or add sinusoidal drift/bob on top of the art:
        // either would make planted feet slide and the body pop between frames.
        int frame = (int)(_elapsed * (running ? 14f : 10f)) % 8;
        int tile = frame + (running ? 8 : 0);
        // The illustrated rows have different gutters. Keep fixed row baselines
        // rather than cropping every frame to its (changing) opaque silhouette.
        int row = tile / 4;
        float[] tops = { 40, 370, 700, 975 };
        float[] heights = { 310, 310, 270, 279 };
        float[] baselines = { 300, 300, 260, 245 };
        float cellWidth = _locomotionAtlas!.GetWidth() / 4f;
        float side = Mathf.Min(Size.X * .76f, 440) * .84f;
        float scale = side / cellWidth;
        var destination = new Rect2((Size.X - side) / 2,
            ground - baselines[row] * scale, side, heights[row] * scale);
        DrawTextureRectRegion(_locomotionAtlas, destination,
            new Rect2((tile % 4) * cellWidth, tops[row], cellWidth, heights[row]));
    }
}
