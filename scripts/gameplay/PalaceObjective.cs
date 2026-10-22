using Godot;

/// <summary>Side-view mission props with a destructible beacon and persistent collectible state.</summary>
public partial class PalaceObjective : Node2D
{
    /// <summary>Beacon, Blade, Memory or Exit determines drawing and damageability.</summary>
    public string Kind { get; set; } = "Memory";
    /// <summary>Beacon integrity is independent from player and enemy health.</summary>
    public float CurrentHealth { get; private set; } = 210;
    /// <summary>Collected or destroyed props cannot be interacted with twice.</summary>
    public bool Collected { get; private set; }
    /// <summary>Raised exactly once after beacon integrity reaches zero.</summary>
    [Signal] public delegate void NeutralizedEventHandler();
    private Area2D? _hurtbox;
    private float _time;

    /// <summary>Gives the beacon a layer-two area aligned with the player's platform melee hitbox.</summary>
    public override void _Ready()
    {
        if (Kind != "Beacon") return;
        _hurtbox = new Area2D { CollisionLayer = 2, CollisionMask = 0 };
        _hurtbox.AddChild(new CollisionShape2D { Position = new Vector2(0,-48), Shape = new RectangleShape2D { Size = new Vector2(76,108) } });
        AddChild(_hurtbox);
    }

    /// <summary>Animates emission rings without moving the interaction point.</summary>
    public override void _Process(double delta) { _time += (float)delta; QueueRedraw(); }

    /// <summary>Accepts real melee damage only while the demolition beacon is active.</summary>
    public void TakeDamage(float damage)
    {
        if (Kind != "Beacon" || Collected || damage <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (CurrentHealth <= 0)
        {
            SetCollected();
            if (_hurtbox != null) _hurtbox.CollisionLayer = 0;
            EmitSignal(SignalName.Neutralized);
        }
        QueueRedraw();
    }

    /// <summary>Persists collection locally and hides the prop so retries cannot award duplicate fragments.</summary>
    public void SetCollected() { Collected = true; Visible = false; }

    /// <summary>True once a Checkpoint shrine has been reached; it then burns bright and stays lit.</summary>
    public bool Activated { get; private set; }

    /// <summary>Lights a Checkpoint shrine. Other kinds ignore the call.</summary>
    public void Activate()
    {
        if (Kind != "Checkpoint" || Activated) return;
        Activated = true;
        QueueRedraw();
    }

    /// <summary>Draws grounded side-view machinery, the ceremonial blade, archive glyphs, a checkpoint shrine or extraction portal.</summary>
    public override void _Draw()
    {
        if (Collected) return;
        Color cyan = new("#a8eae6"), gold = new("#ebc290");
        if (Kind == "Checkpoint")
        {
            // A stone lantern shrine: dim and cold until reached, then it glows warm and shows a halo.
            DrawRect(new Rect2(-22, -10, 44, 10), new Color("#3b4152"));
            DrawRect(new Rect2(-9, -78, 18, 68), new Color("#4a5063"));
            DrawRect(new Rect2(-26, -104, 52, 28), new Color("#2b3040"));
            DrawColoredPolygon(new Vector2[] { new(-38,-104), new(0,-132), new(38,-104) }, new Color("#2f3446"));
            Color flame = Activated ? new Color("#ffcf8a") : new Color("#4b6a78");
            DrawRect(new Rect2(-14, -98, 28, 18), flame);
            if (Activated)
            {
                float pulse = 1f + Mathf.Sin(_time * 3f) * .12f;
                DrawCircle(new Vector2(0, -89), 46 * pulse, new Color(1f, .8f, .5f, .14f));
                DrawCircle(new Vector2(0, -89), 26 * pulse, new Color(1f, .8f, .5f, .22f));
            }
        }
        else if (Kind == "Beacon")
        {
            DrawColoredPolygon(new Vector2[] { new(-50,0),new(-32,-19),new(-25,-100),new(25,-100),new(32,-19),new(50,0) }, new Color("#17222f"));
            DrawRect(new Rect2(-24,-96,48,80),new Color("#394257"));
            DrawRect(new Rect2(-11,-91,22,67),new Color("#b5659c"));
            DrawLine(new Vector2(0,-88),new Vector2(0,-27),new Color("#f2adc7"),5,true);
            for (int i=0;i<3;i++) DrawArc(new Vector2(0,-62),23+i*11,Mathf.Pi*.1f+_time,Mathf.Pi*1.8f+_time,30,new Color(.74f,.44f,.88f,.65f),2,true);
            DrawLine(new Vector2(-42,5),new Vector2(-42+84*CurrentHealth/210,5),new Color("#d58ec0"),5,true);
        }
        else if (Kind == "Blade")
        {
            DrawRect(new Rect2(-48,-18,96,18),new Color("#1b2d38"));
            DrawLine(new Vector2(-50,-20),new Vector2(50,-20),gold,4,true);
            DrawLine(new Vector2(-22,-33),new Vector2(16,-104),cyan,7,true);
            DrawLine(new Vector2(-32,-48),new Vector2(-2,-34),gold,5,true);
            DrawArc(new Vector2(0,-65),50+Mathf.Sin(_time*2)*3,0,Mathf.Tau,36,new Color(cyan,.35f),2,true);
        }
        else if (Kind == "Exit")
        {
            DrawRect(new Rect2(-58,-174,12,174),new Color("#8b4d4d"));
            DrawRect(new Rect2(46,-174,12,174),new Color("#8b4d4d"));
            DrawLine(new Vector2(-78,-176),new Vector2(78,-176),gold,12,true);
            DrawArc(new Vector2(0,-82),64,0,Mathf.Tau,48,new Color("#91e9d1"),3,true);
            DrawLine(new Vector2(-28,-82),new Vector2(27,-82),cyan,3,true);
            DrawPolyline(new Vector2[]{new(10,-100),new(28,-82),new(10,-64)},cyan,3,true);
        }
        else
        {
            float y = -32 + Mathf.Sin(_time*2)*5;
            DrawColoredPolygon(new Vector2[]{new(0,y-23),new(13,y),new(0,y+23),new(-13,y)},gold);
            DrawArc(new Vector2(0,y),30,0,Mathf.Tau,28,new Color(gold,.4f),1.5f,true);
        }
    }
}
