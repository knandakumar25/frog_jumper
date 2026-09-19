using Godot;
using System;

/// <summary>
/// ExplosiveBarrel is a tactical object that detonates when its health reaches zero,
/// causing high AoE damage and knockback to surrounding entities.
/// </summary>
public partial class ExplosiveBarrel : TacticalObject
{
    [Export] public float ExplosionRadius = 5.0f;
    [Export] public float ExplosionDamage = 50.0f;
    [Export] public float ExplosionKnockback = 15.0f;

    public override void _Ready()
    {
        base._Ready();

        // Add Health component.
        var health = new Health();
        health.MaxHealth = 20.0f;
        health.Died += OnDetonate;
        AddChild(health);
    }

    private void OnDetonate()
    {
        GD.Print("ExplosiveBarrel: DETONATING!");

        // 1. Trigger visual/audio effects via CombatVFXManager.
        CombatVFXManager.Instance?.PlayAbilityPulse(this, new Color(1, 0.5f, 0), ExplosionRadius);
        CombatVFXManager.Instance?.ScreenShake(0.5f, 0.3f);

        // 2. Find all entities in the explosion radius.
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D();
        var sphere = new SphereShape3D { Radius = ExplosionRadius };
        query.ShapeRid = sphere.GetRid();
        query.Transform = GlobalTransform;
        query.CollisionMask = 1 | 2; // Enemy layer AND Tactical layer

        var results = spaceState.IntersectShape(query);
        foreach (var result in results)
        {
            var collider = (Node3D)result["collider"];

            // Apply damage if a Health component exists.
            var healthComp = collider.GetNodeOrNull<Health>("Health") ?? collider.FindChild("Health", true, false) as Health;
            healthComp?.TakeDamage(ExplosionDamage);

            // Compute a safe knockback direction.
            Vector3 diff = collider.GlobalPosition - GlobalPosition;
            Vector3 knockDir = diff.LengthSquared() > 0 ? diff.Normalized() : Vector3.Up; // fallback upward

            if (collider is Enemy3D enemy)
            {
                knockDir.Y = 0.2f; // slight lift for enemies
                enemy.OnTacticalImpact(knockDir * ExplosionKnockback);
            }
            else if (collider is Player3D player)
            {
                knockDir.Y = 0.2f;
                player.Velocity += knockDir * ExplosionKnockback;
            }
            else if (collider is TacticalObject tactical)
            {
                knockDir.Y = 0f;
                tactical.OnImpact(knockDir * ExplosionKnockback * 2.0f, this);
            }
        }

        // Remove barrel from scene.
        QueueFree();
    }
}
