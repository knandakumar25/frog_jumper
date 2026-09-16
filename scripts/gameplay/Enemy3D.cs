using Godot;
using System;

/// <summary>
/// Enemy3D implements AI for combat entities in Axiom Force.
/// It uses a state machine: Idle -> Chase -> Attack -> HitStun.
/// </summary>
public partial class Enemy3D : CharacterBody3D
{
	public enum EnemyState { Idle, Chase, Attack, HitStun }
	public enum EnemyType { Vanguard, Heavy, Stalker }

	[Export] public EnemyType Type = EnemyType.Vanguard;
	[Export] public float MaxHealth = 100.0f;
	[Export] public float Damage = 10.0f;

	private float _speed = 3.0f;
	private float _detectionRange = 10.0f;
	private float _attackRange = 2.0f;
	private float _attackCooldown = 1.5f;

	private EnemyState _currentState = EnemyState.Idle;
	private Player3D _player;
	private WeaponArea3D _weaponArea;
	private AnimationPlayer _animPlayer;
	private NavigationAgent3D _navAgent;
	private float _attackTimer = 0f;
	private float _hitStunTimer = 0f;
	private System.Collections.Generic.List<StatusEffect> _activeEffects = new();

	public override void _Ready()
	{
		ApplyTypeStats();

		_player = GetTree().Root.FindChild("Player3D", true, false) as Player3D;
		_weaponArea = GetNodeOrNull<WeaponArea3D>("WeaponArea3D");
		_animPlayer = FindChild("AnimationPlayer", true, false) as AnimationPlayer;
		_navAgent = GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");

		var health = GetNodeOrNull<Health>("Health");
		if (health != null)
		{
			health.MaxHealth = MaxHealth;
			health.Died += OnDied;
		}

		if (_weaponArea != null)
		{
			_weaponArea.Damage = Damage;
		}
	}

	private void ApplyTypeStats()
	{
		switch (Type)
		{
			case EnemyType.Heavy:
				_speed = 1.5f;
				MaxHealth = 250.0f;
				Damage = 30.0f;
				_attackCooldown = 2.5f;
				break;
			case EnemyType.Stalker:
				_speed = 5.0f;
				MaxHealth = 60.0f;
				Damage = 15.0f;
				_attackCooldown = 1.0f;
				break;
			case EnemyType.Vanguard:
			default:
				_speed = 3.0f;
				_detectionRange = 10.0f;
				_attackRange = 2.0f;
				_attackCooldown = 1.5f;
				break;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float fDelta = (float)delta;
		if (_player == null) return;

		if (_currentState == EnemyState.HitStun)
		{
			_hitStunTimer -= fDelta;
			if (_hitStunTimer <= 0)
			{
				_currentState = EnemyState.Idle;
			}
		}
		else
		{
			float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

			switch (_currentState)
			{
				case EnemyState.Idle:
					UpdateIdleState(distanceToPlayer);
					break;
				case EnemyState.Chase:
					UpdateChaseState(distanceToPlayer, fDelta);
					break;
				case EnemyState.Attack:
					UpdateAttackState(distanceToPlayer, fDelta);
					break;
			}
		}

		MoveAndSlide();
	}

	// Handle collisions with tactical objects (e.g., getting hit by a pushed crate).
	public void OnTacticalImpact(Vector3 impactForce)
	{
		TakeHit();
		Velocity += impactForce;
	}

	private void UpdateIdleState(float distance)
	{
		PlayAnimation("idle");
		if (distance < _detectionRange)
		{
			_currentState = EnemyState.Chase;
		}
	}

	private void UpdateChaseState(float distance, float delta)
	{
		PlayAnimation("running");

		Vector3 direction;
		if (_navAgent != null)
		{
			_navAgent.TargetPosition = _player.GlobalPosition;
			direction = (_navAgent.GetNextPathPosition() - GlobalPosition).Normalized();
		}
		else
		{
			direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		}

		direction.Y = 0;

		// Calculate current speed based on status effects.
		float currentSpeed = _speed;
		foreach (var effect in _activeEffects)
		{
			if (effect is SlowEffect slow)
			{
				currentSpeed *= slow.SpeedMultiplier;
			}
		}

		Velocity = new Vector3(direction.X * currentSpeed, Velocity.Y, direction.Z * currentSpeed);

		// Look at player.
		Vector3 targetPos = _player.GlobalPosition;
		targetPos.Y = GlobalPosition.Y;
		if (GlobalPosition.DistanceSquaredTo(targetPos) > 0.1f)
		{
			LookAt(targetPos, Vector3.Up);
		}

		if (distance < _attackRange)
		{
			_currentState = EnemyState.Attack;
		}
		else if (distance > _detectionRange * 1.2f)
		{
			_currentState = EnemyState.Idle;
		}
	}


	private void UpdateAttackState(float distance, float delta)
	{
		_attackTimer -= delta;
		if (_attackTimer <= 0)
		{
			PerformAttack();
			_attackTimer = _attackCooldown;
		}

		Velocity = Velocity with { X = 0, Z = 0 };
		PlayAnimation("attacking");

		if (distance > _attackRange * 1.1f)
		{
			_currentState = EnemyState.Chase;
		}
	}

	private void PerformAttack()
	{
		if (_weaponArea != null)
		{
			_weaponArea.ActivateAttack();
		}
	}

	private void PlayAnimation(string animName)
	{
		if (_animPlayer == null) return;

		string finalAnim = animName switch
		{
			"running" => "Running",
			"idle" => "Idle",
			"attacking" => "Thrust_Slash",
			_ => animName
		};

		if (_animPlayer.HasAnimation(finalAnim))
		{
			_animPlayer.Play(finalAnim);
		}
	}

	public void TakeHit()
	{
		// Trigger Hit-Stop for impact feel.
		CombatVFXManager.Instance?.TriggerHitStop(0.05f);

		_currentState = EnemyState.HitStun;
		_hitStunTimer = 0.3f;

		// Visual hit flash.
		TriggerHitFlash();

		// Basic knockback.
		if (_player != null)
		{
			Vector3 knockbackDir = (GlobalPosition - _player.GlobalPosition).Normalized();
			knockbackDir.Y = 0;
			Velocity += knockbackDir * 10.0f;
		}

		if (_animPlayer != null && _animPlayer.HasAnimation("Hit"))
		{
			_animPlayer.Play("Hit");
		}
	}

	private void TriggerHitFlash()
	{
		// Find the mesh to apply flash.
		var mesh = FindChild("*", true, false) as MeshInstance3D;
		if (mesh == null) return;

		// In a real scenario, we'd use a shader parameter.
		// For the prototype, we'll use a Tween on the modulate if the material supports it.
		var tween = CreateTween();
		tween.TweenProperty(mesh, "modulate", new Color(1, 0, 0), 0.05f);
		tween.TweenProperty(mesh, "modulate", new Color(1, 1, 1), 0.15f);
	}

	public void ApplyStatusEffect(StatusEffect effect)
	{
		_activeEffects.Add(effect);
		AddChild(effect);
		effect.OnApply(this);

		if (effect is StunEffect)
		{
			_currentState = EnemyState.HitStun;
			_hitStunTimer = effect.Duration;
		}
	}

	private void OnDied()
	{
		_currentState = EnemyState.Idle;
		Velocity = Vector3.Zero;


		if (_animPlayer != null && _animPlayer.HasAnimation("Death"))
		{
			_animPlayer.Play("Death");
		}

		// Visual disintegration.
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Vector3.Zero, 1.0f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
		tween.Finished += () => QueueFree();

		CollisionLayer = 0;
		CollisionMask = 0;
	}
}
