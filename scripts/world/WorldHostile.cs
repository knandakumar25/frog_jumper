using Godot;
using System;

/// <summary>
/// One surveillance drone for open-world encounters. Position and logic use world metres,
/// and the node is only a drawing. Drones hover, so they don't collide with the terrain.
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

    private Sprite2D? _body;
    private string _sheetKey = "";
    private int _sheetFrames = 1;
    private double _animationTime;

    /// <summary>Adds the optional stand-in robot and strike sound once the drone is in the scene.</summary>
    public override void _Ready()
    {
        _strikeSound = StandInAssets.AddPlayer(this, StandInAssets.Strike, -9);
        // Scale keeps the robot close to the operative's height; the offset lifts its feet (about y 138 in a 256 cell) to the origin.
        // At 0.3x the 256 px cells shimmer when moving unless mipmaps are sampled; the sheets import with mipmaps on.
        _body = StandInAssets.AddSheet(this, StandInAssets.RobotSheet("IdleAim", 8), 5, 5, .3f, new Vector2(0, -10), TextureFilterEnum.LinearWithMipmaps);
        _sheetKey = "IdleAim8";
        _sheetFrames = 24;
    }

    /// <summary>
    /// Maps a world direction to a robot sheet number. Uses the player's facing formula: 0 is south, stepping
    /// clockwise. The art's dir1 to dir7 match those steps and dir8 is south.
    /// </summary>
    public int RobotDirection(double dx, double dy)
    {
        int facing = ((int)Math.Round(Math.Atan2(-dx, dy) / (Math.PI / 4)) + 8) % 8;
        return facing == 0 ? 8 : facing;
    }

    /// <summary>Picks the sheet for the current action and direction, and advances its frame at 12 per second.</summary>
    private void UpdateBody(double delta, double dx, double dy, bool walking)
    {
        if (_body == null) return;
        var (animation, columns, rows, frames) = _windup > 0 ? ("StandingShoot", 4, 3, 10)
            : walking ? ("WalkingShoot", 4, 4, 16)
            : ("IdleAim", 5, 5, 24);
        int direction = RobotDirection(dx, dy);
        string key = animation + direction;
        if (key != _sheetKey)
        {
            Texture2D? texture = StandInAssets.Texture(StandInAssets.RobotSheet(animation, direction));
            if (texture != null)
            {
                // Frame must be reset first so it is valid for the new, possibly smaller, grid.
                _body.Frame = 0;
                _body.Texture = texture;
                _body.Hframes = columns;
                _body.Vframes = rows;
                _sheetKey = key;
                _sheetFrames = frames;
            }
        }
        _animationTime += delta;
        _body.Frame = (int)(_animationTime * 12) % _sheetFrames;
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
        bool walking = false;
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
                walking = true;
            }
        }
        UpdateBody(delta, dx, dy, walking);
        Position = ExplorationPlayer.Project(XMetres - origin.X * 128, YMetres - origin.Y * 128);
    }

    /// <summary>Draws the ground ring, the windup range and health bar, plus the code-drawn drone when the stand-in art is absent.</summary>
    public override void _Draw()
    {
        if (_body == null)
        {
            DrawColoredPolygon(new[] { new Vector2(-12, -26), new(0, -36), new(12, -26), new(0, -16) }, new Color("3b4a63"));
            DrawCircle(new Vector2(0, -26), 4, _windup > 0 ? new Color("ff3b30") : new Color("4edee5"));
        }
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
