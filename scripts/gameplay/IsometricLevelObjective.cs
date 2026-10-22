using Godot;

/// <summary>Reusable authored objective prop for the isometric mission's beacon, memories and exit.</summary>
public partial class IsometricLevelObjective : Node2D
{
    /// <summary>Controls the prop drawing and whether it can take damage.</summary>
    [Export] public string Kind { get; set; } = "Fragment";
    /// <summary>Health used only by the demolition beacon.</summary>
    [Export] public float MaxHealth { get; set; } = 260f;
    /// <summary>Current beacon health, exposed for mission acceptance checks.</summary>
    public float CurrentHealth { get; private set; }
    /// <summary>Raised when the player destroys the beacon.</summary>
    [Signal] public delegate void NeutralizedEventHandler();

    private Area2D? _hitArea;
    private bool _collected;
    private float _pulse;

    /// <summary>Creates a damage query target for the beacon and initializes its integrity.</summary>
    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        if (Kind == "Beacon")
        {
            _hitArea = new Area2D { Name = "BeaconHitArea", CollisionLayer = 4, CollisionMask = 0 };
            _hitArea.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 92f } });
            AddChild(_hitArea);
        }
        SetProcess(true);
    }

    /// <summary>Advances the emitted energy pulse used by the beacon, blade and memory glyphs.</summary>
    public override void _Process(double delta)
    {
        _pulse += (float)delta * 2.3f;
        QueueRedraw();
    }

    /// <summary>Applies attack damage to the active beacon and unlocks the next objective at zero.</summary>
    public void TakeDamage(float amount)
    {
        if (Kind != "Beacon" || _collected || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth <= 0f)
        {
            _collected = true;
            if (_hitArea != null) _hitArea.CollisionLayer = 0;
            EmitSignal(SignalName.Neutralized);
        }
        QueueRedraw();
    }

    /// <summary>Hides a recovered Data-Blade or optional memory fragment without freeing its scene node.</summary>
    public void SetCollected()
    {
        _collected = true;
        if (_hitArea != null) _hitArea.CollisionLayer = 0;
        QueueRedraw();
    }

    /// <summary>Renders each objective as readable original 2D line art over the Blender map.</summary>
    public override void _Draw()
    {
        if (_collected) return;
        switch (Kind)
        {
            case "Beacon": DrawBeacon(); break;
            case "DataBlade": DrawDataBlade(); break;
            case "Extraction": DrawExtraction(); break;
            default: DrawMemory(); break;
        }
    }

    /// <summary>Draws a three-stage demolition pylon and health gauge.</summary>
    private void DrawBeacon()
    {
        Color violet = new("#d16be9");
        DrawEllipse(new Vector2(0, 26), new Vector2(78, 30), new Color("#090c17"));
        DrawArc(new Vector2(0, 14), 63f + Mathf.Sin(_pulse) * 7f, 0f, Mathf.Tau, 40, violet, 4f, true);
        DrawColoredPolygon(new Vector2[] {
            new(-37, 12), new(-24, -39), new(-10, -61), new(11, -61), new(26, -38),
            new(39, 12), new(25, 22), new(-25, 22)
        }, new Color("#4b2d59"));
        DrawLine(new Vector2(0, -53), new Vector2(0, 12), new Color("#ff8d98"), 8f, true);
        DrawCircle(new Vector2(0, -5), 11f + Mathf.Sin(_pulse) * 2f, new Color("#ffaaa4"));
        DrawLine(new Vector2(-45, 32), new Vector2(-45 + 90f * CurrentHealth / MaxHealth, 32), violet, 5f, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(-58, 58), "DEMOLITION BEACON", HorizontalAlignment.Left, -1, 11, Colors.White);
    }

    /// <summary>Draws the recovered archive blade as a glowing silhouette.</summary>
    private void DrawDataBlade()
    {
        DrawEllipse(new Vector2(0, 16), new Vector2(45, 17), new Color("#0a0c17"));
        Color cyan = new("#9bfff1");
        DrawLine(new Vector2(-3, 28), new Vector2(-3, -52), cyan, 8f, true);
        DrawLine(new Vector2(-3, -52), new Vector2(19, -31), new Color("#51dfd2"), 4f, true);
        DrawLine(new Vector2(-20, 25), new Vector2(14, 25), new Color("#dbb278"), 7f, true);
        DrawArc(Vector2.Zero, 48f + Mathf.Sin(_pulse) * 4f, 0, Mathf.Tau, 32, cyan, 2f, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(-45, 54), "DATA-BLADE", HorizontalAlignment.Left, -1, 11, cyan);
    }

    /// <summary>Draws the safe extraction marker after the archive is recovered.</summary>
    private void DrawExtraction()
    {
        Color green = new("#78ffc6");
        DrawArc(Vector2.Zero, 53f + Mathf.Sin(_pulse) * 5f, 0, Mathf.Tau, 32, green, 4f, true);
        DrawArc(Vector2.Zero, 34f, 0, Mathf.Tau, 24, new Color(green, .75f), 2f, true);
        DrawLine(new Vector2(0, -20), new Vector2(0, 22), green, 4f, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(-46, 68), "EXTRACTION", HorizontalAlignment.Left, -1, 11, green);
    }

    /// <summary>Draws one optional fragment that reveals environmental archive text when collected.</summary>
    private void DrawMemory()
    {
        Color amber = new("#ffd28b");
        float radius = 17f + Mathf.Sin(_pulse) * 3f;
        DrawArc(Vector2.Zero, radius + 10f, 0, Mathf.Tau, 20, new Color(amber, .7f), 2f, true);
        DrawColoredPolygon(new Vector2[] { new(0, -radius), new(radius * .65f, 0), new(0, radius), new(-radius * .65f, 0) }, amber);
    }

    /// <summary>Draws an ellipse with a polygon to keep all objective art in 2D CanvasItem geometry.</summary>
    private void DrawEllipse(Vector2 center, Vector2 radii, Color color)
    {
        var points = new Vector2[24];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = index * Mathf.Tau / points.Length;
            points[index] = center + new Vector2(Mathf.Cos(angle) * radii.X, Mathf.Sin(angle) * radii.Y);
        }
        DrawColoredPolygon(points, color);
    }
}
