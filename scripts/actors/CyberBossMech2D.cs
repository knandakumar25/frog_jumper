using Godot;

/// <summary>
/// CyberBossMech2D is a multi-phase 2D boss encounter ("Vanguard-X").
/// Features hovering tactical strafing, plasma missile barrages,
/// an Overdrive rage phase shift at 50% HP, and explosive defeat sequences.
/// </summary>
public partial class CyberBossMech2D : CharacterBody2D
{
	// â”€â”€â”€ Signals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
	[Signal] public delegate void BossHealthChangedEventHandler(float current, float max, int phase);
	[Signal] public delegate void BossDefeatedEventHandler();

	// â”€â”€â”€ Exports & Stats â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
	[Export] public float MaxHealth { get; set; } = 300.0f;
	[Export] public float PatrolDistance { get; set; } = 400.0f;
	[Export] public float BaseSpeed { get; set; } = 130.0f;
	[Export] public int PointValue { get; set; } = 50;

	public float CurrentHealth { get; private set; } = 300.0f;
	public int CurrentPhase { get; private set; } = 1;
	public bool IsDying { get; private set; } = false;

	// â”€â”€â”€ Internal State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
	private Vector2 _originPosition;
	private float _hoverTimer = 0.0f;
	private float _attackCooldown = 0.0f;
	private int _patrolDirection = 1;
	private bool _enteredPhaseTwo = false;

	// â”€â”€â”€ Node References â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
	private AnimatedSprite2D? _sprite;
	private PointLight2D? _eyeLight;
	private AudioStreamPlayer2D? _cannonAudio;
	private AudioStreamPlayer2D? _alarmAudio;
	private CpuParticles2D? _thrusterSparks;

	private PackedScene? _missileScene;

    /// <summary>Optional fortress controller; absent in standalone scenes and legacy callers.</summary>
    public AegisBossController? FortressController { get; set; }

    /// <summary>Initializes visuals, stats, missile resources and the existing HUD health signal.</summary>

	public override void _Ready()
	{
		AddToGroup("enemies");
		_originPosition = GlobalPosition;
		CurrentHealth = MaxHealth;

		_sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite2D");
		_eyeLight = GetNodeOrNull<PointLight2D>("EyeLight");
		_thrusterSparks = GetNodeOrNull<CpuParticles2D>("ThrusterSparks");
		if (_sprite != null)
		{
			// Loaded in code, not the scene, because the boss art awaits regeneration
			// (docs/ART_REGENERATION.md); a missing file gives an invisible placeholder.
			_sprite.SpriteFrames = OperativeVisuals.LoadFramesAt("res://assets/characters/rendered/heavy.tres");
			_sprite.Play("idle");
		}

		if (ResourceLoader.Exists("res://scenes/gameplay/cyber_boss_missile_2d.tscn"))
		{
			_missileScene = GD.Load<PackedScene>("res://scenes/gameplay/cyber_boss_missile_2d.tscn");
		}

		SetupAudio();
		EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
	}

	/// <summary>Creates optional local cannon and alarm sounds.</summary>
	private void SetupAudio()
	{
		_cannonAudio = CreateAudioPlayer("res://assets/audio/sfx/boss_cannon.wav", 0.0f);
		_alarmAudio = CreateAudioPlayer("res://assets/audio/sfx/boss_alarm.wav", 2.0f);
	}

	/// <summary>Loads a present sound and attaches its owned positional player; missing audio is harmless.</summary>
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

	/// <summary>Runs fortress sequencing when opted in, otherwise the original hover and barrage loop.</summary>
	public override void _PhysicsProcess(double delta)
	{
		if (IsDying) return;
        if (FortressController != null) { FortressController.Tick(delta); return; }

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
			_sprite.FlipH = Velocity.X < 0f;
		}

		// 2. Attack execution
		float interval = CurrentPhase == 2 ? 1.4f : 2.2f;
		if (_attackCooldown >= interval)
		{
			_attackCooldown = 0.0f;
			FireMissileAttack();
		}
	}

	/// <summary>Aims the legacy volley safely even when player and boss positions coincide.</summary>
	private void FireMissileAttack()
	{
		if (_missileScene == null || !IsInsideTree()) return;

		_cannonAudio?.Play();

		// Find target player direction
		var player = GetTree()?.Root?.FindChild("CyberShogun", true, false) as Node2D;
		Vector2 offset = player != null ? player.GlobalPosition - GlobalPosition : Vector2.Left;
        Vector2 toPlayer = offset.LengthSquared() > 0 ? offset.Normalized() : Vector2.Left;

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

	/// <summary>Creates a bounded projectile with explicit boss ownership.</summary>
	private void SpawnMissile(Vector2 direction)
	{
		if (_missileScene == null) return;
		var missile = _missileScene.Instantiate<CyberBossMissile2D>();
		missile.GlobalPosition = GlobalPosition + direction * 35.0f;
		missile.Direction = direction;
        missile.OwningBoss = this;
		GetParent()?.AddChild(missile);
	}

	/// <summary>Routes fortress damage through phase shields; standalone damage keeps its original threshold.</summary>
	public void TakeDamage(float damage)
	{
		if (IsDying || CurrentHealth <= 0f) return;

		if (FortressController != null)
        {
            ApplyFortressDamage(damage, false);
            return;
        }
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

    /// <summary>Accepts real reflected counterfire while retaining phase shields and damage boundaries.</summary>
    public void TakeReflectedDamage(float damage) => ApplyFortressDamage(damage, true);

    /// <summary>Applies the opt-in controller's accepted damage and keeps legacy HUD bindings current.</summary>
    private void ApplyFortressDamage(float damage, bool reflected)
    {
        if (IsDying || FortressController == null) return;
        CurrentHealth = FortressController.ResolveDamage(CurrentHealth, damage, reflected);
        CurrentPhase = FortressController.Phase;
        EmitSignal(SignalName.BossHealthChanged, CurrentHealth, MaxHealth, CurrentPhase);
        FlashHitVfx();
        if (CurrentHealth > 0) return;
        IsDying = true;
        CancelOwnedMissiles();
        FortressController.CancelAttempt();
        EmitSignal(SignalName.BossDefeated);
        QueueFree();
    }

	/// <summary>Enables legacy overdrive visuals and attack timing.</summary>
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

	/// <summary>Flashes the existing sprite only while attached to the scene tree.</summary>
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

	/// <summary>Runs the legacy defeat reward and explosion sequence.</summary>
	private async void Die()
	{
		IsDying = true;
        CancelOwnedMissiles();
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

    /// <summary>Immediately cancels every projectile owned by this mech, including standalone sibling shots.</summary>
    private void CancelOwnedMissiles()
    {
        if (!IsInsideTree()) return;
        foreach (Node node in GetTree().GetNodesInGroup("aegis_projectiles"))
            if (node is CyberBossMissile2D missile && missile.OwningBoss == this && !missile.IsQueuedForDeletion()) missile.Cancel();
    }

    /// <summary>Removing a mech cannot leave live missiles in its parent's scene.</summary>
    public override void _ExitTree() => CancelOwnedMissiles();

	/// <summary>Creates a short owned visual/audio explosion at the supplied world point.</summary>
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
