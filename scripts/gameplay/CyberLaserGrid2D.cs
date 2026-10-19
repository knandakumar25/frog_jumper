using Godot;

/// <summary>
/// CyberLaserGrid2D provides an interactive, pulsating laser barrier hazard for 2D levels.
/// It features configurable periodic on/off timing, visual neon beam lines,
/// localized audio hum, and contact damage to the player.
/// </summary>
public partial class CyberLaserGrid2D : Node2D
{
	// ─── Configuration ───────────────────────────────────────────────────────
	[Export] public float BeamLength { get; set; } = 240.0f;
	[Export] public Vector2 Direction { get; set; } = Vector2.Down;
	[Export] public float ActiveDuration { get; set; } = 2.5f;
	[Export] public float InactiveDuration { get; set; } = 1.5f;
	[Export] public float StartOffset { get; set; } = 0.0f;
	[Export] public float Damage { get; set; } = 25.0f;
	[Export] public Color LaserColor { get; set; } = new Color(0.0f, 0.94f, 1.0f, 1.0f);
	[Export] public bool AlwaysActive { get; set; } = false;

	// ─── State ───────────────────────────────────────────────────────────────
	public bool IsLaserActive { get; private set; } = true;
	private float _cycleTimer = 0.0f;

	// ─── Node References ─────────────────────────────────────────────────────
	private Line2D? _beamLine;
	private Area2D? _hazardArea;
	private CollisionShape2D? _collisionShape;
	private SegmentShape2D? _segmentShape;
	private AudioStreamPlayer2D? _humPlayer;
	private PointLight2D? _glowLight;

	public override void _Ready()
	{
		_beamLine = GetNodeOrNull<Line2D>("BeamLine");
		_hazardArea = GetNodeOrNull<Area2D>("HazardArea");
		_collisionShape = GetNodeOrNull<CollisionShape2D>("HazardArea/CollisionShape2D");
		_humPlayer = GetNodeOrNull<AudioStreamPlayer2D>("HumPlayer");
		_glowLight = GetNodeOrNull<PointLight2D>("GlowLight");

		if (_collisionShape != null)
		{
			_segmentShape = new SegmentShape2D
			{
				A = Vector2.Zero,
				B = Direction.Normalized() * BeamLength
			};
			_collisionShape.Shape = _segmentShape;
		}

		if (_hazardArea != null)
		{
			_hazardArea.BodyEntered += OnBodyEntered;
		}

		_cycleTimer = StartOffset;
		UpdateLaserState(true);
	}

	public override void _Process(double delta)
	{
		if (AlwaysActive)
		{
			if (!IsLaserActive) UpdateLaserState(true);
			AnimateLaserPulse((float)delta);
			return;
		}

		float dt = (float)delta;
		_cycleTimer += dt;

		if (IsLaserActive)
		{
			AnimateLaserPulse(dt);
			if (_cycleTimer >= ActiveDuration)
			{
				_cycleTimer = 0.0f;
				UpdateLaserState(false);
			}
		}
		else
		{
			if (_cycleTimer >= InactiveDuration)
			{
				_cycleTimer = 0.0f;
				UpdateLaserState(true);
			}
		}
	}

	private void AnimateLaserPulse(float dt)
	{
		if (_beamLine != null)
		{
			// Dynamic width modulation for buzzing effect
			float pulse = 3.0f + Mathf.Sin((float)Time.GetTicksMsec() * 0.03f) * 1.2f;
			_beamLine.Width = pulse;
			_beamLine.SetPointPosition(0, Vector2.Zero);
			_beamLine.SetPointPosition(1, Direction.Normalized() * BeamLength);
		}

		if (_glowLight != null)
		{
			_glowLight.Energy = 1.4f + Mathf.Sin((float)Time.GetTicksMsec() * 0.02f) * 0.4f;
			_glowLight.Position = Direction.Normalized() * (BeamLength * 0.5f);
		}
	}

	private void UpdateLaserState(bool active)
	{
		IsLaserActive = active;

		if (_beamLine != null)
		{
			_beamLine.Visible = active;
		}

		if (_collisionShape != null)
		{
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !active);
		}

		if (_glowLight != null)
		{
			_glowLight.Visible = active;
		}

		if (_humPlayer != null && IsInsideTree())
		{
			if (active && !_humPlayer.Playing)
			{
				_humPlayer.Play();
			}
			else if (!active && _humPlayer.Playing)
			{
				_humPlayer.Stop();
			}
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!IsLaserActive) return;

		if (body is CyberShogun shogun)
		{
			Vector2 knockback = -Direction.Normalized() * 250.0f + new Vector2(0f, -120f);
			shogun.TakeDamage(Damage, knockback);
		}
		else if (body.HasMethod("TakeDamage"))
		{
			body.Call("TakeDamage", Damage);
		}
	}
}
