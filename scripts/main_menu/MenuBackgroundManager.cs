using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// MenuBackgroundManager handles the atmospheric visuals for the main menu,
/// including drifting clouds and randomly spawning falling characters.
/// </summary>
public partial class MenuBackgroundManager : Node
{
	// ─── Cached packed scene ─────────────────────────────────────────────────
	private static readonly PackedScene FallingCharacterScene =
		GD.Load<PackedScene>("res://scenes/main_menu/falling_character.tscn");

	// ─── Cloud Background Data ──────────────────────────────────────────────────
	private readonly Texture2D _cloudTexture =
		GD.Load<Texture2D>("res://assets/images/clouds.png");
	private const int MaxClouds = 8;
	private readonly List<Sprite2D> _clouds = new();

	private struct CloudData
	{
		public float Speed;
		public int   Layer;
		public float BaseY;
		public float DriftAmount;
		public float DriftSpeed;
		public float DriftOffset;
	}
	private readonly Dictionary<Sprite2D, CloudData> _cloudData = new();

	// ─── Falling-character spawn timer ───────────────────────────────────────
	private float _spawnTimer        = 0f;
	private float _nextSpawnTime     = 0f;
	private const float SpawnIntervalMin = 0.5f;
	private const float SpawnIntervalMax = 2.0f;

	public override void _Ready()
	{
		// Ensure CloudsLayer exists.
		if (!HasNode("CloudsLayer"))
		{
			var layer = new Node2D { Name = "CloudsLayer" };
			AddChild(layer);
			// Move to back if necessary, but as a child of MainMenu it depends on scene order.
		}

		if (_cloudTexture == null)
		{
			GD.PushError("MenuBackgroundManager: cloud_scene texture failed to load (null).");
		}
		else
		{
			SpawnInitialClouds();
		}

		ResetSpawnTimer();
	}

	private void SpawnInitialClouds()
	{
		var cloudsLayer = GetNode<Node2D>("CloudsLayer");
		var size = GetViewport().GetVisibleRect().Size;

		for (int i = 0; i < MaxClouds; i++)
		{
			var sprite = new Sprite2D { Texture = _cloudTexture };
			int layer  = i % 3;
			float y    = (float)GD.RandRange(40 + layer * 60, size.Y * 0.5f);

			sprite.Position = new Vector2((float)GD.RandRange(0, size.X), y);

			float depthFactor = 1f - layer * 0.2f;
			float baseScale   = (float)GD.RandRange(0.7, 1.3) * depthFactor;
			sprite.Scale = Vector2.One * baseScale;

			float startAlpha = (float)GD.RandRange(0.5, 0.8) * depthFactor;
			sprite.Modulate = sprite.Modulate with { A = 0f };

			var tween = CreateTween();
			tween?.TweenProperty(sprite, "modulate:a", startAlpha, GD.RandRange(1.5, 3.0));

			cloudsLayer.AddChild(sprite);
			_clouds.Add(sprite);

			_cloudData[sprite] = new CloudData
			{
				Speed       = (float)GD.RandRange((80 - layer * 25) - 15, (80 - layer * 25) + 15),
				Layer       = layer,
				BaseY       = y,
				DriftAmount = (float)GD.RandRange(5.0, 12.0),
				DriftSpeed  = (float)GD.RandRange(0.3, 0.7),
				DriftOffset = (float)GD.RandRange(0.0, Mathf.Tau),
			};
		}
	}

	public override void _Process(double delta)
	{
		float dt   = (float)delta;
		float time = Time.GetTicksMsec() / 1000f;
		var   size = GetViewport().GetVisibleRect().Size;

		foreach (var cloud in _clouds)
		{
			var data = _cloudData[cloud];
			cloud.Position -= new Vector2(data.Speed * dt, 0f);
			cloud.Position = cloud.Position with
			{
				Y = data.BaseY + Mathf.Sin(time * data.DriftSpeed + data.DriftOffset) * data.DriftAmount
			};

			if (cloud.Texture != null &&
				cloud.Position.X < -cloud.Texture.GetSize().X * cloud.Scale.X)
			{
				int   layer       = data.Layer;
				float baseSpeed   = 80 - layer * 25;
				float y           = (float)GD.RandRange(40 + layer * 60, size.Y * 0.5f);
				float depthFactor = 1f - layer * 0.2f;
				float baseScale   = (float)GD.RandRange(0.7, 1.3) * depthFactor;

				cloud.Position = new Vector2((float)GD.RandRange(size.X + 50, size.X + 300), cloud.Position.Y);
				cloud.Scale    = Vector2.One * baseScale;

				float targetAlpha = (float)GD.RandRange(0.5, 0.8) * depthFactor;
				cloud.Modulate = cloud.Modulate with { A = 0f };
				CreateTween()?.TweenProperty(cloud, "modulate:a", targetAlpha, GD.RandRange(2.0, 4.0));

				_cloudData[cloud] = new CloudData
				{
					Speed       = (float)GD.RandRange(baseSpeed - 15, baseSpeed + 15),
					Layer       = layer,
					BaseY       = y,
					DriftAmount = (float)GD.RandRange(5.0, 12.0),
					DriftSpeed  = (float)GD.RandRange(0.3, 0.7),
					DriftOffset = (float)GD.RandRange(0.0, Mathf.Tau),
				};
			}
		}

		_spawnTimer += dt;
		if (_spawnTimer >= _nextSpawnTime)
		{
			SpawnFallingCharacter();
			ResetSpawnTimer();
		}
	}

	private void ResetSpawnTimer()
	{
		_spawnTimer    = 0f;
		_nextSpawnTime = (float)GD.RandRange(SpawnIntervalMin, SpawnIntervalMax);
	}

	private void SpawnFallingCharacter()
	{
		var character = FallingCharacterScene.Instantiate<FallingCharacter>();
		string[] names = { "Cyber Shogun", "Mask Dude", "Pink Man", "Virtual Guy" };
		character.CharacterName = names[GD.Randi() % (uint)names.Length];

		float screenWidth = GetViewport().GetVisibleRect().Size.X;
		character.Position  = new Vector2((float)GD.RandRange(50, screenWidth - 50), -50f);
		float scaleFactor   = (float)GD.RandRange(1.5, 2.5);
		character.Scale     = Vector2.One * scaleFactor;
		character.FallSpeed = (float)GD.RandRange(150, 300);

		AddChild(character);
		// Note: MoveChild logic depends on parent. Since this is a manager child,
		// characters will be under this node.
	}
}
