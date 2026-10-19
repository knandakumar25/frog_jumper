using Godot;

/// <summary>
/// CyberHazardFloor2D creates an electrified floor hazard.
/// Features periodic surge states, crackling electric sparks,
/// and damages characters standing on the surface.
/// </summary>
public partial class CyberHazardFloor2D : Node2D
{
	[Export] public float FloorWidth { get; set; } = 200.0f;
	[Export] public float Damage { get; set; } = 20.0f;
	[Export] public float ActiveDuration { get; set; } = 3.0f;
	[Export] public float InactiveDuration { get; set; } = 2.0f;
	[Export] public float StartOffset { get; set; } = 0.0f;
	[Export] public bool AlwaysActive { get; set; } = false;

	public bool IsSurging { get; private set; } = true;
	private float _cycleTimer = 0.0f;

	private Area2D? _hazardArea;
	private CollisionShape2D? _collisionShape;
	private RectangleShape2D? _rectShape;
	private CpuParticles2D? _sparks;
	private ColorRect? _floorVisual;

	public override void _Ready()
	{
		_hazardArea = GetNodeOrNull<Area2D>("HazardArea");
		_collisionShape = GetNodeOrNull<CollisionShape2D>("HazardArea/CollisionShape2D");
		_sparks = GetNodeOrNull<CpuParticles2D>("Sparks");
		_floorVisual = GetNodeOrNull<ColorRect>("FloorVisual");

		if (_collisionShape != null)
		{
			_rectShape = new RectangleShape2D
			{
				Size = new Vector2(FloorWidth, 14.0f)
			};
			_collisionShape.Shape = _rectShape;
			_collisionShape.Position = new Vector2(FloorWidth * 0.5f, 7.0f);
		}

		if (_floorVisual != null)
		{
			_floorVisual.Size = new Vector2(FloorWidth, 10.0f);
		}

		if (_sparks != null)
		{
			_sparks.Position = new Vector2(FloorWidth * 0.5f, 0.0f);
			_sparks.EmissionRectExtents = new Vector2(FloorWidth * 0.5f, 2.0f);
		}

		if (_hazardArea != null)
		{
			_hazardArea.BodyEntered += OnBodyEntered;
		}

		_cycleTimer = StartOffset;
		UpdateSurgeState(true);
	}

	public override void _Process(double delta)
	{
		if (AlwaysActive)
		{
			if (!IsSurging) UpdateSurgeState(true);
			return;
		}

		float dt = (float)delta;
		_cycleTimer += dt;

		if (IsSurging)
		{
			if (_cycleTimer >= ActiveDuration)
			{
				_cycleTimer = 0.0f;
				UpdateSurgeState(false);
			}
		}
		else
		{
			if (_cycleTimer >= InactiveDuration)
			{
				_cycleTimer = 0.0f;
				UpdateSurgeState(true);
			}
		}
	}

	private void UpdateSurgeState(bool surging)
	{
		IsSurging = surging;

		if (_collisionShape != null)
		{
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !surging);
		}

		if (_sparks != null)
		{
			_sparks.Emitting = surging;
		}

		if (_floorVisual != null)
		{
			_floorVisual.Color = surging ? new Color(0.0f, 0.94f, 1.0f, 0.9f) : new Color(0.1f, 0.15f, 0.2f, 0.6f);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!IsSurging) return;

		if (body is CyberShogun shogun)
		{
			Vector2 knockback = new Vector2(0f, -320f);
			shogun.TakeDamage(Damage, knockback);
		}
		else if (body.HasMethod("TakeDamage"))
		{
			body.Call("TakeDamage", Damage);
		}
	}
}
