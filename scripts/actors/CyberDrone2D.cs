using Godot;
using System;

/// <summary>
/// CyberDrone2D is an airborne patrol drone enemy in 2D levels.
/// It hovers and patrols horizontally, reacting to player attacks (sword slash, blade dash).
/// Upon defeat, it bursts into neon sparks and awards points to GameManager.
/// </summary>
public partial class CyberDrone2D : CharacterBody2D
{
	[Export] public float MaxHealth = 40.0f;
	[Export] public float PatrolDistance = 140.0f;
	[Export] public float PatrolSpeed = 50.0f;
	[Export] public int PointValue = 5;

	public float CurrentHealth { get; private set; }

	private Vector2 _startPosition;
	private int _direction = 1;
	private bool _isDying = false;
	private AnimatedSprite2D? _sprite;
	private PointLight2D? _eyeLight;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		_startPosition = GlobalPosition;
		AddToGroup("enemies");

		_sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite2D");
		_eyeLight = GetNodeOrNull<PointLight2D>("EyeLight");
		if (_sprite != null)
		{
			// Loaded in code, not the scene, because the drone art awaits regeneration
			// (docs/ART_REGENERATION.md); a missing file gives an invisible placeholder.
			_sprite.SpriteFrames = OperativeVisuals.LoadFramesAt("res://assets/characters/rendered/vanguard.tres");
			_sprite.Play("idle");
		}
	}

	[Export] public float ContactDamage = 15.0f;

	public override void _PhysicsProcess(double delta)
	{
		if (_isDying) return;

		// Horizontal patrol movement
		float newX = Position.X + (_direction * PatrolSpeed * (float)delta);
		if (Mathf.Abs(newX - _startPosition.X) >= PatrolDistance)
		{
			_direction *= -1;
			if (_sprite != null)
			{
				_sprite.FlipH = _direction < 0;
			}
		}

		// Gentle floating bob
		float hoverOffset = Mathf.Sin((float)Time.GetTicksMsec() * 0.004f) * 0.8f;
		Velocity = new Vector2(_direction * PatrolSpeed, hoverOffset * 20f);
		MoveAndSlide();

		// Check player collisions for contact damage
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			var collider = collision.GetCollider();
			if (collider is CyberShogun player)
			{
				Vector2 diff = player.GlobalPosition - GlobalPosition;
				Vector2 knockback = (diff.LengthSquared() > 0.001f ? diff.Normalized() : Vector2.Right) * 220f;
				player.TakeDamage(ContactDamage, knockback);
			}
		}
	}

	/// <summary>
	/// Receives incoming damage from Cyber Shogun's attacks.
	/// </summary>
	public void TakeDamage(float amount)
	{
		if (_isDying || amount <= 0f) return;

		CurrentHealth -= amount;
		GD.Print($"CyberDrone2D hit! Took {amount} dmg. HP: {CurrentHealth}/{MaxHealth}");

		// Hit reaction: Flash red and spawn spark particles
		FlashHit();
		SpawnHitSparks();

		if (CurrentHealth <= 0f)
		{
			Die();
		}
	}

	private void FlashHit()
	{
		if (_sprite == null) return;
		var tween = CreateTween();
		_sprite.Modulate = new Color(2.5f, 0.4f, 0.4f, 1f);
		tween.TweenProperty(_sprite, "modulate", new Color(1f, 1f, 1f, 1f), 0.18f);
	}

	private void SpawnHitSparks()
	{
		var sparks = new CpuParticles2D
		{
			Position = Vector2.Zero,
			Amount = 16,
			Lifetime = 0.3,
			OneShot = true,
			Explosiveness = 0.95f,
			Spread = 180.0f,
			InitialVelocityMin = 80.0f,
			InitialVelocityMax = 180.0f,
			Color = new Color(0f, 0.94f, 1f, 1f),
			Gravity = new Vector2(0, 120),
			Scale = new Vector2(1.8f, 1.8f)
		};
		AddChild(sparks);
		sparks.Emitting = true;

		var timer = GetTree()?.CreateTimer(0.35);
		if (timer != null)
		{
			timer.Timeout += () => sparks.QueueFree();
		}
	}

	private void Die()
	{
		_isDying = true;

		// Award points via GameManager if present in the scene
		var gm = GetTree()?.Root?.FindChild("GameManager", true, false) as GameManager;
		if (gm != null)
		{
			for (int i = 0; i < PointValue; i++)
			{
				gm.AddPoint();
			}
		}

		// Death explosion particle burst
		var explosion = new CpuParticles2D
		{
			Position = GlobalPosition,
			Amount = 32,
			Lifetime = 0.5,
			OneShot = true,
			Explosiveness = 0.98f,
			Spread = 180.0f,
			InitialVelocityMin = 120.0f,
			InitialVelocityMax = 260.0f,
			Color = new Color(1f, 0.3f, 0.1f, 1f),
			Gravity = new Vector2(0, 150),
			Scale = new Vector2(2.5f, 2.5f)
		};
		GetParent()?.AddChild(explosion);
		explosion.Emitting = true;

		// Death explosion audio
		if (GetParent() != null && ResourceLoader.Exists("res://assets/audio/sfx/drone_explosion.wav"))
		{
			var stream = GD.Load<AudioStream>("res://assets/audio/sfx/drone_explosion.wav");
			if (stream != null)
			{
				var sfx = new AudioStreamPlayer2D
				{
					Stream = stream,
					GlobalPosition = GlobalPosition,
					Bus = "Master"
				};
				GetParent().AddChild(sfx);
				sfx.Play();
				sfx.Finished += () => sfx.QueueFree();
			}
		}

		var cleanupTimer = GetTree()?.CreateTimer(0.6);
		if (cleanupTimer != null)
		{
			cleanupTimer.Timeout += () => explosion.QueueFree();
		}

		QueueFree();
	}
}
