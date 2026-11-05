using Godot;

/// <summary>The four kinds of operative-gated obstacle (laser fields reuse <see cref="CyberLaserGrid2D"/> instead).</summary>
public enum GateKind
{
    /// <summary>Burning corridor. Oceanus's Tidal Torrent (douse) puts it out for several seconds; anyone else is burned.</summary>
    FireVent,
    /// <summary>Collapsed masonry that blocks the route until Apex Titan's Tectonic Slam (slam) breaks it.</summary>
    RubbleWall,
    /// <summary>A door sealed by a sonic lock; Aura Phoenicia's Harmonic Pulse (resonate) opens it.</summary>
    SonicLock,
    /// <summary>A constant column of searing heat; only Surya Prime's Solar Bulwark (bulwark) can cross it unharmed.</summary>
    HeatVent
}

/// <summary>
/// An obstacle that only one operative's signature ability can clear. Abilities do not push into gates: when a player
/// uses an ability it announces its tag, position and facing to every node in the <c>ability_gates</c> group via
/// <see cref="ReceiveAbility"/>, and each gate decides whether that counts. Position is the gate's bottom-centre (its feet).
/// Required routes only use gates whose ability is already unlocked; others are optional side routes.
/// </summary>
public partial class AbilityGate : Node2D
{
    /// <summary>Optional owning objective identifier used by later missions to report accepted physical action evidence.</summary>
    public string ObjectiveId { get; set; } = "";
    /// <summary>Optional mission defense/suspension check, evaluated before any gate state changes.</summary>
    public System.Func<bool>? Acceptance { get; set; }
    /// <summary>Which obstacle this is.</summary>
    public GateKind Kind { get; set; } = GateKind.RubbleWall;
    /// <summary>Width and height of the obstacle in pixels.</summary>
    public Vector2 Size { get; set; } = new(320f, 170f);
    /// <summary>True once the obstacle no longer blocks or harms (rubble broken, lock open, fire out).</summary>
    public bool Cleared { get; private set; }
    /// <summary>True after a rescue locks this vent safe; ordinary traversal vents still reignite.</summary>
    public bool PermanentlySafe { get; private set; }
    /// <summary>The ability tag that clears this gate (see <see cref="LevelCatalog"/> constants).</summary>
    public string RequiredTag => Kind switch
    {
        GateKind.FireVent => LevelCatalog.Douse, GateKind.RubbleWall => LevelCatalog.Slam,
        GateKind.SonicLock => LevelCatalog.Resonate, _ => LevelCatalog.Bulwark
    };
    /// <summary>Raised once each time the gate is cleared by its ability.</summary>
    [Signal] public delegate void GateClearedEventHandler();

    private StaticBody2D? _solid;
    private Area2D? _hurt;
    private float _time, _douseRemaining, _hurtCooldown;

    /// <summary>Builds the blocking body (rubble, lock) or damage area (fire, heat) and registers with the gate group.</summary>
    public override void _Ready()
    {
        AddToGroup("ability_gates");
        if (Kind is GateKind.RubbleWall or GateKind.SonicLock)
        {
            _solid = new StaticBody2D { CollisionLayer = 1, CollisionMask = 0 };
            _solid.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size }, Position = new Vector2(0, -Size.Y * .5f) });
            AddChild(_solid);
        }
        else
        {
            // Mask 1 sees the player; a slightly inset box keeps grazing a vent's edge from hurting.
            _hurt = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
            _hurt.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size * .9f }, Position = new Vector2(0, -Size.Y * .5f) });
            AddChild(_hurt);
        }
    }

    /// <summary>Counts down the douse timer, re-ignites fire afterwards, and burns anyone standing in a live vent.</summary>
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        _hurtCooldown -= dt;
        if (Kind == GateKind.FireVent && Cleared && !PermanentlySafe)
        {
            _douseRemaining -= dt;
            if (_douseRemaining <= 0f) Cleared = false;   // the fire catches again; dousing is temporary on purpose
        }
        if (_hurt != null && !Cleared && _hurtCooldown <= 0f)
        {
            foreach (Node2D body in _hurt.GetOverlappingBodies())
            {
                if (body is not CyberShogun player) continue;
                // Sourceless damage counts as environmental, so Phase Blink and Solar Bulwark windows ignore it.
                player.TakeDamage(Kind == GateKind.HeatVent ? 45f : 30f, new Vector2(-player.FacingSign * 260f, -160f));
                _hurtCooldown = .45f;
            }
        }
        QueueRedraw();
    }

    /// <summary>Locks an already-doused civilian route safe before any civilian starts moving.</summary>
    public bool SecureForEvacuation()
    {
        if (!Cleared) return false;
        PermanentlySafe = true;
        return true;
    }

    /// <summary>
    /// Offers a used ability to this gate. Returns true when it cleared the gate.
    /// Fire needs the douse spray in front of the player; rubble and locks need the burst to land within range.
    /// </summary>
    public bool ReceiveAbility(string tag, Vector2 origin, float facing)
    {
        if (Acceptance?.Invoke() == false) return false;
        if (Cleared && Kind != GateKind.FireVent) return false;
        if (tag != RequiredTag) return false;
        // Distances are measured to the gate's nearest point, not its centre: full-height walls have centres far overhead.
        var rect = new Rect2(GlobalPosition + new Vector2(-Size.X * .5f, -Size.Y), Size);
        Vector2 nearest = new(Mathf.Clamp(origin.X, rect.Position.X, rect.End.X), Mathf.Clamp(origin.Y, rect.Position.Y, rect.End.Y));
        float distance = origin.DistanceTo(nearest);
        switch (Kind)
        {
            case GateKind.FireVent:
                // The torrent is a forward jet: the vent must be ahead of the player and within 320 px.
                float ahead = (nearest.X - origin.X) * facing;
                if (ahead < -20f || distance > 320f) return false;
                Cleared = true;
                _douseRemaining = 8f;
                break;
            case GateKind.RubbleWall:
                if (distance > 280f) return false;   // a slam shakes the ground around the player
                Break();
                break;
            case GateKind.SonicLock:
                if (distance > 360f) return false;   // the pulse radiates in every direction
                Break();
                break;
            default:
                return false;
        }
        EmitSignal(SignalName.GateCleared);
        return true;
    }

    /// <summary>Permanently removes a solid gate's collision and hides it.</summary>
    private void Break()
    {
        Cleared = true;
        _solid?.GetChild<CollisionShape2D>(0).SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        if (IsInsideTree()) CreateTween().TweenProperty(this, "modulate:a", 0f, .5f);
        else Visible = false;
    }

    /// <summary>Draws flames, a rubble pile, a glowing sonic door or a heat column depending on <see cref="Kind"/>.</summary>
    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        switch (Kind)
        {
            case GateKind.RubbleWall:
                DrawRect(new Rect2(-w / 2, -h, w, h), new Color("#3d3a44"));
                for (int i = 0; i < 9; i++)
                {
                    float x = -w / 2 + (i % 3) * w / 3f, y = -h + (i / 3) * h / 3f;
                    DrawRect(new Rect2(x + 3, y + 3, w / 3f - 6, h / 3f - 6), i % 2 == 0 ? new Color("#5a5663") : new Color("#4a4652"));
                }
                DrawLine(new Vector2(-w / 2, -h), new Vector2(w / 2, -h + 14), new Color("#262330"), 5f);
                break;
            case GateKind.SonicLock:
                DrawRect(new Rect2(-w / 2, -h, w, h), new Color("#1f2b3b"));
                DrawRect(new Rect2(-w / 2, -h, 8, h), new Color("#77efff", .5f));
                DrawRect(new Rect2(w / 2 - 8, -h, 8, h), new Color("#77efff", .5f));
                for (int r = 0; r < 3; r++)
                    DrawArc(new Vector2(0, -h / 2), 18 + r * 12 + Mathf.Sin(_time * 3f + r) * 3f, 0, Mathf.Tau, 28, new Color("#8ab7ff", .8f - r * .2f), 3f);
                break;
            default:
                DrawRect(new Rect2(-w / 2, -10, w, 10), new Color("#26303b"));
                if (Cleared)
                {
                    DrawCircle(new Vector2(0, -14), 10f + Mathf.Sin(_time * 8f) * 2f, new Color("#7ec8ff", .6f));   // puddle / steam
                    break;
                }
                Color hot = Kind == GateKind.HeatVent ? new Color("#ffcf6a") : new Color("#ff6a2e");
                for (int i = 0; i < 7; i++)
                {
                    float x = -w / 2 + (i + .5f) * w / 7f;
                    float flame = h * (.55f + .4f * Mathf.Abs(Mathf.Sin(_time * 5f + i * 1.7f)));
                    DrawColoredPolygon(new Vector2[] { new(x - w / 16f, -10), new(x, -10 - flame), new(x + w / 16f, -10) }, new Color(hot, .85f));
                }
                break;
        }
    }
}
