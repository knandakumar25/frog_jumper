using Godot;

/// <summary>
/// CyberBossMech2D is a multi-phase 2D boss encounter ("Vanguard-X").
/// Features hovering tactical strafing, plasma missile barrages,
/// an Overdrive rage phase shift at 50% HP, and explosive defeat sequences.
/// </summary>
public partial class CyberBossMech2D : CharacterBody2D
{
	// ─── Signals ─────────────────────────────────────────────────────────────
	[Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);
	[Signal] public delegate void BossDefeatedEventHandler();

	// ─── Exports & Stats ─────────────────────────────────────────────────────
	[Export] public float MaxHealth { get; set; } = 300.0f;
	[Export] public float PatrolDistance { get; set; } = 400.0f;
	[Export] public float BaseSpeed { get; set; } = 130.0f;
	[Export] public int PointValue { get; set; } = 50;

	public float CurrentHealth { get; private set; } = 300.0f;
	public int CurrentPhase { get; private set; } = 1;
	public bool IsDying { get; private set; } = false;

	// ─── Internal State ───────────────────────────────────────────────────────
	private Vector2 _originPosition;
	private float _hoverTimer = 0.0f;
	private float _attackCooldown = 0.0f;
	private int _patrolDirection = 1;
	private bool _enteredPhaseTwo = false;

	// ─── Node References ─────────────────────────────────────────────────────
	private Sprite2D? _sprite;
	private PointLight2D? _eyeLight;
	private AudioStreamPlayer2D? _cannonAudio;
	private AudioStreamPlayer2D? _alarmAudio;
	private CpuParticles2D? _thrusterSparks;

	private PackedScene? _missileScene;

	public override void _Ready()
	{
		AddToGroup("enemies");
		_originPosition = GlobalPosition;
		CurrentHealth = MaxHealth;

		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		_eyeLight = GetNodeOrNull<PointLight2D>("EyeLight");
		_thrusterSparks = GetNodeOrNull<CpuParticles2D>("ThrusterSparks");

		if (ResourceLoader.Exists("res://scenes/gameplay/cyber_boss_missile_2d.tscn"))
		{
			_missileScene = GD.Load<PackedScene>("res://scenes/gameplay/cyber_boss_missile_2d.tscn");
		}

		SetupAudio();
		EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
	}

	private void SetupAudio()
	{
		_cannonAudio = CreateAudioPlayer("res://assets/audio/sfx/boss_cannon.wav", 0.0f);
		_alarmAudio = CreateAudioPlayer("res://assets/audio/sfx/boss_alarm.wav", 2.0f);
	}

	private AudioStreamPlayer2D? CreateAudioPlayer(string path, float volumeDb)
	{
		if (!ResourceLoader.Exists(path)) return null;
		var stream = GD.Load<AudioStream>(path);
		if (stream == null) return null;
		var player = new AudioStreamPlayer2D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			MaxDistance = 1200.0f,
			Bus = "Master"
		};
		AddChild(player);
		return player;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDying) return;

		float dt = (float)delta;
		_hoverTimer += dt;
		_attackCooldown += dt;

		// 1. Hover patrol traversal
		float speed = CurrentPhase == 2 ? BaseSpeed * 1.8f : BaseSpeed;
		float targetX = _originPosition.X + Mathf.Sin(_hoverTimer * 0.8f) * PatrolDistance;
		float hoverY = _originPosition.Y + Mathf.Sin(_hoverTimer * 2.5f) * 24.0f;

		Velocity = new Vector2(
			(targetX - GlobalPosition.X) * 2.5f,
			(hoverY - GlobalPosition.Y) * 3.0f
		);

		MoveAndSlide();

		// Face movement / player direction
		if (_sprite != null && Velocity.X != 0f)
		{
			_sprite.FlipH = Velocity.X > 0f;
		}

		// 2. Attack execution
		float interval = CurrentPhase == 2 ? 1.4f : 2.2f;
		if (_attackCooldown >= interval)
		{
			_attackCooldown = 0.0f;
			FireMissileAttack();
		}
	}

	private void FireMissileAttack()
	{
		if (_missileScene == null || !IsInsideTree()) return;

		_cannonAudio?.Play();

		// Find target player direction
		var player = GetTree()?.Root?.FindChild("CyberShogun", true, false) as Node2D;
		Vector2 toPlayer = player != null ? (player.GlobalPosition - GlobalPosition).Normalized() : Vector2.Left;

		if (CurrentPhase == 1)
		{
			SpawnMissile(toPlayer);
		}
		else
		{
			// Phase 2: Triple spread barrage
			SpawnMissile(toPlayer);
			SpawnMissile(toPlayer.Rotated(Mathf.DegToRad(18f)));
			SpawnMissile(toPlayer.Rotated(Mathf.DegToRad(-18f)));
		}
	}

	private void SpawnMissile(Vector2 direction)
	{
		if (_missileScene == null) return;
		var missile = _missileScene.Instantiate<CyberBossMissile2D>();
		missile.GlobalPosition = GlobalPosition + direction * 35.0f;
		missile.Direction = direction;
		GetParent()?.AddChild(missile);
	}

	public void TakeDamage(float damage)
	{
		if (IsDying || CurrentHealth <= 0f) return;

		CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

		// Check Phase 2 transition (< 50% HP)
		if (CurrentHealth <= MaxHealth * 0.5f && !_enteredPhaseTwo)
		{
			TriggerPhaseTwo();
		}

		EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
		FlashHitVfx();

		if (CurrentHealth <= 0f)
		{
			Die();
		}
	}

	private void TriggerPhaseTwo()
	{
		_enteredPhaseTwo = true;
		CurrentPhase = 2;
		if (IsInsideTree())
		{
			_alarmAudio?.Play();
		}

		if (_eyeLight != null)
		{
			_eyeLight.Color = new Color(1.0f, 0.15f, 0.2f, 1f);
			_eyeLight.Energy = 3.5f;
		}

		if (_sprite != null)
		{
			_sprite.Modulate = new Color(1.4f, 0.45f, 0.35f, 1f);
		}
	}

	private void FlashHitVfx()
	{
		if (_sprite == null) return;
		if (IsInsideTree())
		{
			var orig = CurrentPhase == 2 ? new Color(1.4f, 0.45f, 0.35f, 1f) : new Color(1f, 1f, 1f, 1f);
			_sprite.Modulate = new Color(2.5f, 2.5f, 2.5f, 1f);
			var tween = CreateTween();
			tween?.TweenProperty(_sprite, "modulate", orig, 0.12f);
		}
	}

	private async void Die()
	{
		IsDying = true;
		EmitSignal(SignalName.BossDefeated);

		// Award points via GameManager
		var gm = GetTree()?.Root?.FindChild("GameManager", true, false) as GameManager;
		if (gm != null)
		{
			for (int i = 0; i < PointValue; i++)
			{
				gm.AddPoint();
			}
		}

		// Sequential explosion bursts
		if (GetParent() != null && IsInsideTree())
		{
			for (int i = 0; i < 4; i++)
			{
				SpawnExplosionBurst(GlobalPosition + new Vector2(
					(float)GD.RandRange(-40.0, 40.0),
					(float)GD.RandRange(-30.0, 30.0)
				));
				await ToSignal(GetTree().CreateTimer(0.18f), SceneTreeTimer.SignalName.Timeout);
			}
		}

		QueueFree();
	}

	private void SpawnExplosionBurst(Vector2 pos)
	{
		if (GetParent() == null) return;

		var burst = new CpuParticles2D
		{
			Position = pos,
			Amount = 28,
			Lifetime = 0.45,
			OneShot = true,
			Explosiveness = 0.95f,
			Spread = 180.0f,
			InitialVelocityMin = 100.0f,
			InitialVelocityMax = 220.0f,
			Color = new Color(1.0f, 0.3f, 0.1f, 1f),
			Scale = new Vector2(2.5f, 2.5f)
		};
		GetParent().AddChild(burst);
		burst.Emitting = true;

		if (ResourceLoader.Exists("res://assets/audio/sfx/drone_explosion.wav"))
		{
			var stream = GD.Load<AudioStream>("res://assets/audio/sfx/drone_explosion.wav");
			if (stream != null)
			{
				var sfx = new AudioStreamPlayer2D
				{
					Stream = stream,
					GlobalPosition = pos,
					Bus = "Master"
				};
				GetParent().AddChild(sfx);
				sfx.Play();
				sfx.Finished += () => sfx.QueueFree();
			}
		}

		var timer = GetTree()?.CreateTimer(0.55);
		if (timer != null)
		{
			timer.Timeout += () => burst.QueueFree();
		}
	}
}
