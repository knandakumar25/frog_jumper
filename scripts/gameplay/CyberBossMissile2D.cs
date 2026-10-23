using Godot;

/// <summary>
/// CyberBossMissile2D represents a plasma energy projectile launched by CyberBossMech2D.
/// Moves toward a target direction with trailing particles and inflicts contact damage.
/// </summary>
public partial class CyberBossMissile2D : Area2D
{
	[Export] public float Speed { get; set; } = 420.0f;
	[Export] public float Damage { get; set; } = 18.0f;
	[Export] public float Lifetime { get; set; } = 4.0f;

	public Vector2 Direction { get; set; } = Vector2.Left;

	private float _aliveTimer = 0.0f;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		Rotation = Direction.Angle();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Position += Direction.Normalized() * Speed * dt;
		_aliveTimer += dt;

		if (_aliveTimer >= Lifetime)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is CyberBossMech2D) return; // Do not hit boss

		if (body is CyberShogun shogun)
		{
			Vector2 knockback = Direction.Normalized() * 260.0f + new Vector2(0f, -80f);
			shogun.TakeDamageFrom(Damage, knockback, this);
			Explode();
		}
		else if (body is TileMap || body is StaticBody2D)
		{
			Explode();
		}
	}

	private void Explode()
	{
		// Spawn quick burst
		if (GetParent() != null)
		{
			var burst = new CpuParticles2D
			{
				Position = GlobalPosition,
				Amount = 14,
				Lifetime = 0.25,
				OneShot = true,
				Explosiveness = 0.9f,
				Spread = 180.0f,
				InitialVelocityMin = 60.0f,
				InitialVelocityMax = 120.0f,
				Color = new Color(1.0f, 0.4f, 0.1f, 1f),
				Scale = new Vector2(2.0f, 2.0f)
			};
			GetParent().AddChild(burst);
			burst.Emitting = true;

			var timer = GetTree()?.CreateTimer(0.3);
			if (timer != null)
			{
				timer.Timeout += () => burst.QueueFree();
			}
		}

		QueueFree();
	}
}
