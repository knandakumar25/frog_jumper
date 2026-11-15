using Godot;
using System;

/// <summary>
/// One SpriteCook vanguard for open-world encounters. Position and logic use world metres,
/// and the node is only a drawing; movement does not collide with the terrain.
/// Attacks wind up visibly before landing, so a dodge or stepping away avoids them.
/// </summary>
public partial class WorldHostile : Node2D
{
    /// <summary>Awareness, which drives the local alert state.</summary>
    public enum Awareness { Unaware, Investigating, Engaged }

    public const float MaxHealth = 60f, MoveSpeed = 5f, AttackRange = 3.2f, AttackDamage = 12f;
    public const double WindupSeconds = 0.7, RecoverySeconds = 1.6;
    private const double EngageRange = 12, InvestigateRange = 25;

    /// <summary>World position in metres.</summary>
    public double XMetres { get; private set; }
    /// <summary>World position in metres.</summary>
    public double YMetres { get; private set; }
    public float Health { get; private set; } = MaxHealth;
    public Awareness State { get; private set; } = Awareness.Unaware;
    private double _windup, _recovery;
    private AudioStreamPlayer2D? _strikeSound;

    private AnimatedSprite2D? _body;
    private double _animationTime;

    /// <summary>Adds the canonical SpriteCook body and optional strike sound without replacing combat markers.</summary>
    public override void _Ready()
    {
        _strikeSound = StandInAssets.AddPlayer(this, StandInAssets.Strike, -9);
        // SpriteCook's 256-pixel cell has its feet at y=224: offset -96 aligns them with
        // this actor's ground ring. Keep the existing world position and attack origin intact.
        _body = new AnimatedSprite2D
        {
            Name = "SpriteCookBody",
            SpriteFrames = OperativeVisuals.LoadFramesAt("res://assets/characters/rendered/vanguard.tres"),
            Animation = "idle", Scale = Vector2.One * .28f, Offset = new Vector2(0, -96),
            TextureFilter = TextureFilterEnum.Nearest, ShowBehindParent = true,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        AddChild(_body);
    }

    /// <summary>
    /// Retains the public eight-direction numbering for callers: south=8, west=2, north=4, east=6.
    /// SpriteCook's side-facing idle sheet uses horizontal mirroring rather than the retired robot sheets.
    /// </summary>
    public int RobotDirection(double dx, double dy)
    {
        int facing = ((int)Math.Round(Math.Atan2(-dx, dy) / (Math.PI / 4)) + 8) % 8;
        return facing == 0 ? 8 : facing;
    }

    /// <summary>Advances the available SpriteCook idle cycle and faces the target; no invented attack/walk sheets.</summary>
    private void UpdateBody(double delta, double dx)
    {
        if (_body == null) return;
        _animationTime += delta;
        int frames = _body.SpriteFrames.GetFrameCount("idle");
        if (frames > 0) _body.Frame = (int)(_animationTime * _body.SpriteFrames.GetAnimationSpeed("idle")) % frames;
        _body.FlipH = dx < 0;
    }

    /// <summary>Sets the starting position before the drone joins the tree.</summary>
    public void Spawn(double x, double y) { XMetres = x; YMetres = y; }

    /// <summary>Applies player damage and alerts the drone. Returns true when this hit destroys it.</summary>
    public bool Hit(float damage)
    {
        if (Health <= 0) return false;
        Health -= damage;
        State = Awareness.Engaged;
        if (IsInsideTree()) QueueRedraw();
        if (Health <= 0) StandInAssets.PlayDetached(this, StandInAssets.Explosion, -8);
        return Health <= 0;
    }

    /// <summary>
    /// Advances perception, movement and the attack timer, then updates the drawn position.
    /// Engaged drones stay engaged. A landing attack calls <paramref name="strike"/>.
    /// </summary>
    public void Tick(double delta, WorldPoint player, ChunkAddress origin, Action<float> strike)
    {
        double dx = player.X - XMetres, dy = player.Y - YMetres;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < EngageRange) State = Awareness.Engaged;
        else if (State != Awareness.Engaged) State = distance < InvestigateRange ? Awareness.Investigating : Awareness.Unaware;

        if (_recovery > 0) _recovery = Math.Max(0, _recovery - delta);
        if (State == Awareness.Engaged)
        {
            if (_windup > 0)
            {
                _windup -= delta;
                if (_windup <= 0)
                {
                    _windup = 0;
                    _recovery = RecoverySeconds;
                    // The attack resolves on current distance, so moving away during the windup avoids it.
                    if (distance <= AttackRange)
                    {
                        strike(AttackDamage);
                        StandInAssets.Play(_strikeSound);
                    }
                    QueueRedraw();
                }
            }
            else if (distance <= AttackRange - 0.4)
            {
                if (_recovery <= 0) { _windup = WindupSeconds; QueueRedraw(); }
            }
            else if (distance > 0.001)
            {
                double step = Math.Min(MoveSpeed * delta, distance);
                XMetres += dx / distance * step;
                YMetres += dy / distance * step;
            }
        }
        UpdateBody(delta, dx);
        Position = ExplorationPlayer.Project(XMetres - origin.X * 128, YMetres - origin.Y * 128);
    }

    /// <summary>Draws only the ground ring, windup range and health bar; absent SpriteCook art leaves no fallback body.</summary>
    public override void _Draw()
    {
        DrawArc(Vector2.Zero, 9, 0, Mathf.Tau, 16, new Color(0, 0, 0, 0.5f), 2);
        if (_windup > 0)
        {
            // Range is in metres, so the ring is squashed like the ground to show the real safe distance.
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1, ExplorationPlayer.GroundAngle));
            DrawArc(Vector2.Zero, AttackRange * ExplorationPlayer.PixelsPerMetre, 0, Mathf.Tau, 32, new Color("ff3b30"), 2);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
        DrawRect(new Rect2(-14, -44, 28 * Math.Max(0, Health) / MaxHealth, 3), new Color("ff6b5e"));
    }
}
