using Godot;
using System;

/// <summary>
/// Separate eight-direction exploration controller supporting on-foot navigation,
/// top-down combat actions, traversal perks, the starter electric bike, and the utility boat.
/// Mission combat controllers remain untouched.
/// </summary>
public partial class ExplorationPlayer : CharacterBody2D
{
    public const float PixelsPerMetre = 16, GroundAngle = .65f;
    /// <summary>The city being walked in, set by the world scene; decides where water is.</summary>
    public WorldDefinition City { get; set; } = WorldCatalog.Definition;
    public bool MovementEnabled { get; set; }
    public int Facing { get; private set; }
    public bool IsRidingBike { get; private set; }
    public bool IsRidingBoat { get; private set; }
    public bool InInterior { get; set; }

    // Top-down combat and operative status
    public float CurrentHealth { get; private set; } = 100.0f;
    public float MaxHealth { get; private set; } = 100.0f;
    public float CurrentShield { get; private set; } = 50.0f;
    public float MaxShield { get; private set; } = 50.0f;

    public bool IsAttacking { get; private set; }
    public bool IsDefending { get; private set; }
    public double AttackCooldown { get; private set; }
    public double DodgeCooldown { get; private set; }
    public double ShieldRegenTimer { get; private set; }

    /// <summary>Length of the dodge's burst of movement and damage immunity.</summary>
    public const double DodgeSeconds = 0.2;
    private const float DodgeSpeed = 16f;
    private double _dodgeTimer;
    private Vector2 _dodgeDirection;
    private bool _defeatRaised;

    /// <summary>True during the dodge burst; incoming damage is ignored.</summary>
    public bool IsDodging => _dodgeTimer > 0;

    /// <summary>Raised once when health reaches zero. Restoring vitals re-arms it.</summary>
    public event Action? Defeated;

    private Sprite2D _sprite = null!;
    private Sprite2D? _bikeSprite;
    private AudioStreamPlayer2D? _bikeEngine;
    private Sprite2D? _boatSprite;
    private string _operative = "";
    private double _stride;

    /// <summary>Creates the small foot collider and angled-camera sprite at a common foot pivot.</summary>
    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;
        CollisionLayer = 2;
        CollisionMask = 1;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 5 } });

        _sprite = new Sprite2D { Hframes = 4, Vframes = 8, Offset = new(0, -23), Scale = Vector2.One * .65f };
        AddChild(_sprite);

        // Visual indicator for electric bike mount
        _bikeSprite = new Sprite2D
        {
            Name = "BikeMountSprite",
            Offset = new(0, -10),
            Scale = Vector2.One * 0.7f,
            Visible = false,
            ZIndex = -1
        };
        AddChild(_bikeSprite);
        _bikeSprite.Texture = StandInAssets.Texture(StandInAssets.BikeTexture);
        if (_bikeSprite.Texture != null)
        {
            // The stand-in is seen from above, so it turns with Facing. Move the lift from Offset to
            // Position so the bike rotates about its own centre rather than swinging around the feet.
            _bikeSprite.Offset = Vector2.Zero;
            _bikeSprite.Position = new Vector2(0, -10);
            _bikeSprite.Scale = Vector2.One * .45f;
        }
        else
        {
            // Without the stand-in art, the mount falls back to shapes under the same toggled node.
            _bikeSprite.AddChild(new Polygon2D { Name = "BikeFrame", Color = new Color("4edee5"), Polygon = new[] { new Vector2(-18, -6), new(14, -12), new(20, -6), new(-12, 0) } });
            _bikeSprite.AddChild(new Polygon2D { Name = "BikeWheelRear", Color = new Color("1b2433"), Polygon = new[] { new Vector2(-24, -2), new(-16, -8), new(-8, -2), new(-16, 4) } });
            _bikeSprite.AddChild(new Polygon2D { Name = "BikeWheelFront", Color = new Color("1b2433"), Polygon = new[] { new Vector2(10, -2), new(18, -8), new(26, -2), new(18, 4) } });
        }
        _bikeEngine = StandInAssets.AddPlayer(this, StandInAssets.BikeEngine, -14, loop: true);

        // Visual indicator for utility boat mount
        _boatSprite = new Sprite2D
        {
            Name = "BoatMountSprite",
            Offset = new(0, -5),
            Scale = Vector2.One * 0.8f,
            Visible = false,
            ZIndex = -1
        };
        AddChild(_boatSprite);

        AddChild(new Camera2D { Position = new(0, -40), PositionSmoothingEnabled = false, Zoom = Vector2.One * .8f });
        RefreshOperative();
    }

    /// <summary>Loads only the currently selected operative atlas; all six share the same animation contract.</summary>
    public void RefreshOperative()
    {
        string active = OperativeRoster.Current.Active;
        if (_operative == active || _sprite == null) return;
        // Health carries over as a fraction, so swapping can't lose or gain it. The first load starts full.
        float healthFraction = _operative.Length == 0 || MaxHealth <= 0 ? 1f : CurrentHealth / MaxHealth;
        float shieldFraction = _operative.Length == 0 || MaxShield <= 0 ? 1f : CurrentShield / MaxShield;
        _operative = active;
        var profile = OperativeRegistry.GetProfile(active);
        MaxHealth = profile.MaxHealth;
        MaxShield = profile.MaxShield;
        CurrentHealth = MaxHealth * healthFraction;
        CurrentShield = MaxShield * shieldFraction;

        int index = Array.IndexOf(new[] { "Cyber Shogun", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade", "Surya Prime" }, active);
        string path = $"res://assets/world/operative_{Math.Max(0, index)}.png";
        if (ResourceLoader.Exists(path)) _sprite.Texture = GD.Load<Texture2D>(path);
    }

    /// <summary>Toggles mounting the starter electric bike at the current player position.</summary>
    public bool ToggleBike() => ToggleBike(new WorldPoint(0, 0));

    /// <summary>Toggles mounting the starter electric bike at coordinates.</summary>
    public bool ToggleBikeAt(double x, double y, int layer = 0) => ToggleBike(new WorldPoint(x, y, layer));

    /// <summary>Toggles mounting the starter electric bike on suitable routes outside interiors.</summary>
    public bool ToggleBike(WorldPoint currentMetres)
    {
        if (IsRidingBike)
        {
            IsRidingBike = false;
            if (_bikeSprite != null) _bikeSprite.Visible = false;
            _bikeEngine?.Stop();
            return true;
        }

        // Electric bikes cannot be operated inside buildings, in water, or while on a boat
        if (InInterior || IsRidingBoat || City.Water(currentMetres))
        {
            return false;
        }

        IsRidingBike = true;
        IsDefending = false;
        if (_bikeSprite != null) _bikeSprite.Visible = true;
        UpdateBikeHeading();
        StandInAssets.Play(_bikeEngine);
        return true;
    }

    /// <summary>
    /// Points the top-down stand-in bike along <see cref="Facing"/>. Facing F moves along (-sin a, cos a)
    /// with a = F * 45 degrees; the art faces up the image, so it needs a + 180 degrees. The fallback shapes
    /// are drawn side-on and must not rotate.
    /// </summary>
    private void UpdateBikeHeading()
    {
        if (_bikeSprite?.Texture != null) _bikeSprite.Rotation = Facing * Mathf.Pi / 4 + Mathf.Pi;
    }

    /// <summary>Toggles mounting the utility boat at the current player position.</summary>
    public bool ToggleBoat() => ToggleBoat(new WorldPoint(0, 0));

    /// <summary>Toggles mounting the utility boat at coordinates.</summary>
    public bool ToggleBoatAt(double x, double y, int layer = 0) => ToggleBoat(new WorldPoint(x, y, layer));

    /// <summary>Toggles mounting the utility boat on water surfaces outside interiors.</summary>
    public bool ToggleBoat(WorldPoint currentMetres)
    {
        if (IsRidingBoat)
        {
            IsRidingBoat = false;
            if (_boatSprite != null) _boatSprite.Visible = false;
            return true;
        }

        // Boats can only be operated in water outside interiors and cannot be mounted while on bike
        if (InInterior || IsRidingBike || !City.Water(currentMetres))
        {
            return false;
        }

        IsRidingBoat = true;
        if (_boatSprite != null) _boatSprite.Visible = true;
        return true;
    }

    /// <summary>Executes top-down light attack for the current operative.</summary>
    public bool ExecuteAttack()
    {
        if (IsAttacking || AttackCooldown > 0 || IsRidingBike || IsRidingBoat) return false;
        IsAttacking = true;
        AttackCooldown = 0.45;
        ShieldRegenTimer = 3.0; // Reset shield regen delay
        return true;
    }

    /// <summary>Executes defensive guard or parry stance.</summary>
    public bool ExecuteDefend(bool active)
    {
        if (IsRidingBike || IsRidingBoat) { IsDefending = false; return false; }
        IsDefending = active;
        return true;
    }

    /// <summary>Performs evasive dash / dodge roll.</summary>
    public bool ExecuteDodge()
    {
        if (DodgeCooldown > 0 || IsRidingBike || IsRidingBoat) return false;
        DodgeCooldown = 1.0;
        _dodgeTimer = DodgeSeconds;
        Vector2 input = MovementEnabled ? Input.GetVector("world_left", "world_right", "world_up", "world_down") : Vector2.Zero;
        // With no direction held, dodge along the facing, which uses the same angle convention as _PhysicsProcess.
        double angle = Facing * Math.PI / 4;
        _dodgeDirection = input.LengthSquared() > 0 ? input.Normalized() : new Vector2((float)-Math.Sin(angle), (float)Math.Cos(angle));
        return true;
    }

    /// <summary>Applies incoming combat or environmental damage with armor mitigation.</summary>
    public void TakeDamage(float rawDamage)
    {
        if (IsDodging || _defeatRaised) return;
        var profile = OperativeRegistry.GetProfile(_operative);
        float mitigated = rawDamage * (1.0f - profile.DamageReduction);
        if (IsDefending) mitigated *= 0.5f;

        if (CurrentShield > 0)
        {
            float absorbed = Math.Min(CurrentShield, mitigated);
            CurrentShield -= absorbed;
            mitigated -= absorbed;
        }

        CurrentHealth = Math.Max(0, CurrentHealth - mitigated);
        ShieldRegenTimer = 4.0;
        if (CurrentHealth <= 0)
        {
            _defeatRaised = true;
            Defeated?.Invoke();
        }
    }

    /// <summary>Full health and shield replenishment in safehouses and medical clinics.</summary>
    public void RestoreFullVitals()
    {
        CurrentHealth = MaxHealth;
        CurrentShield = MaxShield;
        _defeatRaised = false;
    }

    /// <summary>
    /// Moves at 4/8 metres per second on foot, up to 20 m/s on bike, or 14 m/s on boat.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (!IsInsideTree()) return;

        // Cooldown processing
        if (AttackCooldown > 0)
        {
            AttackCooldown = Math.Max(0, AttackCooldown - delta);
            if (AttackCooldown == 0) IsAttacking = false;
        }
        if (DodgeCooldown > 0) DodgeCooldown = Math.Max(0, DodgeCooldown - delta);

        // Passive shield regeneration after delay
        if (ShieldRegenTimer > 0)
        {
            ShieldRegenTimer -= delta;
        }
        else if (CurrentShield < MaxShield)
        {
            var profile = OperativeRegistry.GetProfile(_operative);
            CurrentShield = Math.Min(MaxShield, CurrentShield + (float)(profile.ShieldRegenRate * delta));
        }

        Vector2 direction = MovementEnabled ? Input.GetVector("world_left", "world_right", "world_up", "world_down") : Vector2.Zero;

        float speed;
        if (_dodgeTimer > 0)
        {
            // The burst replaces normal movement and stops immediately if the world pauses play.
            _dodgeTimer = Math.Max(0, _dodgeTimer - delta);
            direction = MovementEnabled ? _dodgeDirection : Vector2.Zero;
            speed = DodgeSpeed;
        }
        else if (IsRidingBike)
        {
            speed = 20.0f; // Bike: ~20 m/s on roads
        }
        else if (IsRidingBoat)
        {
            speed = 14.0f; // Utility Boat: ~14 m/s on water
        }
        else
        {
            // Foot: 4 m/s walking, 8 m/s sprinting
            speed = Input.IsActionPressed("world_run") ? 8.0f : 4.0f;
        }

        Velocity = new Vector2(direction.X, direction.Y * GroundAngle) * (speed * PixelsPerMetre);
        MoveAndSlide();

        if (direction.LengthSquared() > 0)
        {
            // Atlas rows rotate south-facing Blender rig clockwise
            Facing = ((int)Math.Round(Math.Atan2(-direction.X, direction.Y) / (Math.PI / 4)) + 8) % 8;
            UpdateBikeHeading();
            _stride += delta * (speed >= 8 ? 12 : 7);
        }

        if (_sprite != null)
        {
            _sprite.Frame = Facing * 4 + (direction.LengthSquared() > 0 ? 1 + (int)_stride % 3 : 0);
        }
    }

    /// <summary>Projects a small logical offset to the angled ground plane; never use this on absolute world coordinates.</summary>
    public static Vector2 Project(double x, double y) => new((float)x * PixelsPerMetre, (float)y * PixelsPerMetre * GroundAngle);
}
