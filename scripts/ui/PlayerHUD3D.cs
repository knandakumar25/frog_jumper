using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// PlayerHUD3D implements a high-fidelity diegetic HUD.
/// It features a holographic health bar, ability cooldown rings,
/// and a "glitch" effect that triggers on damage.
/// </summary>
public partial class PlayerHUD3D : Node3D
{
	private MeshInstance3D _healthBarBackground;
	private MeshInstance3D _healthBarFill;
	private Label3D _healthText;
	private Label3D _scoreLabel;

	private MeshInstance3D _primaryRing;
	private MeshInstance3D _secondaryRing;

	private Health _playerHealth;
	private Player3D _player;

	[Export] public Vector3 Offset = new Vector3(0, 2.2f, 0);
	private Vector3 _originalOffset;

	private float _glitchTimer = 0f;
	private bool _isGlitching = false;

	public override void _Ready()
	{
		_originalOffset = Offset;

		// 1. Setup Health Bar.
		_healthBarBackground = CreateQuadMesh(new Color(0, 0, 0, 0.5f), new Vector2(1.0f, 0.1f));
		_healthBarFill = CreateQuadMesh(new Color(0, 1, 1), new Vector2(1.0f, 0.1f));
		_healthBarFill.Position = new Vector3(-0.5f, 0, 0); // Start from left
		_healthBarFill.Scale = new Vector3(1.0f, 1.0f, 1.0f);
		_healthBarBackground.AddChild(_healthBarFill);
		AddChild(_healthBarBackground);

		_healthText = new Label3D();
		_healthText.Text = "HP: 100";
		_healthText.Modulate = new Color(0, 1, 1);
		_healthText.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_healthText.FontSize = 18;
		_healthText.Position = new Vector3(0, 0.15f, 0);
		_healthBarBackground.AddChild(_healthText);

		// 2. Setup Score Label.
		_scoreLabel = new Label3D();
		_scoreLabel.Text = "SCORE: 0";
		_scoreLabel.Modulate = new Color(1, 0, 1);
		_scoreLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_scoreLabel.FontSize = 16;
		_scoreLabel.Position = new Vector3(0, -0.2f, 0);
		AddChild(_scoreLabel);

		// 3. Setup Ability Rings.
		_primaryRing = CreateRingMesh(new Color(0, 1, 1));
		_primaryRing.Position = new Vector3(-0.3f, -0.5f, 0);
		AddChild(_primaryRing);

		_secondaryRing = CreateRingLMesh(new Color(1, 0, 1));
		_secondaryRing.Position = new Vector3(0.3f, -0.5f, 0);
		AddChild(_secondaryRing);

		// 4. Connect to Player.
		_player = GetParent<Player3D>();
		if (_player != null)
		{
			_playerHealth = _player.GetNode<Health>("Health");
			_playerHealth.HealthChanged += OnHealthChanged;
		}

		UpdateScore();
	}

	private MeshInstance3D CreateQuadMesh(Color color, Vector2 size)
	{
		var meshInstance = new MeshInstance3D();
		var quad = new QuadMesh { Size = size };
		meshInstance.Mesh = quad;

		var mat = new StandardMaterial3D
		{
			AlbedoColor = color,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};
		meshInstance.MaterialOverride = mat;
		return meshInstance;
	}

	private MeshInstance3D CreateRingMesh(Color color)
	{
		var meshInstance = new MeshInstance3D();
		var torus = new TorusMesh { InnerRadius = 0.05f, OuterRadius = 0.07f };
		meshInstance.Mesh = torus;

		var mat = new StandardMaterial3D
		{
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = 3.0f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};
		meshInstance.MaterialOverride = mat;
		return meshInstance;
	}

	private MeshInstance3D CreateRingLMesh(Color color)
	{
		// Reusing CreateRingMesh for simplicity in prototype.
		return CreateRingMesh(color);
	}

	private void OnHealthChanged(float current, float max)
	{
		float percent = current / max;
		_healthBarFill.Scale = new Vector3(percent, 1.0f, 1.0f);
		_healthText.Text = $"HP: {Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";

		if (percent < 0.3f)
		{
			var mat = _healthBarFill.MaterialOverride as StandardMaterial3D;
			if (mat != null) mat.AlbedoColor = new Color(1, 0, 0);
			_healthText.Modulate = new Color(1, 0, 0);
			TriggerGlitch();
		}
		else
		{
			var mat = _healthBarFill.MaterialOverride as StandardMaterial3D;
			if (mat != null) mat.AlbedoColor = new Color(0, 1, 1);
			_healthText.Modulate = new Color(0, 1, 1);
		}
	}

	public void UpdateScore()
	{
		var levelManager = GetTree().Root.FindChild("Level3DManager", true, false) as Level3DManager;
		if (levelManager != null)
		{
			_scoreLabel.Text = $"SCORE: {levelManager.CurrentPoints}";
		}
	}

	public void TriggerGlitch()
	{
		_isGlitching = true;
		_glitchTimer = 0.2f;
	}

	public override void _Process(double delta)
	{
		Position = Offset;

		// 1. Update Ability Cooldown Rings.
		if (_player != null)
		{
			// We assume abilities are children of the player.
			var abilities = _player.GetChildren();
			int abilityCount = 0;
			foreach (var child in abilities)
			{
				if (child is Ability ability)
				{
					float progress = ability.CooldownProgress;
					if (abilityCount == 0)
						_primaryRing.Scale = new Vector3(progress, progress, progress);
					else if (abilityCount == 1)
						_secondaryRing.Scale = new Vector3(progress, progress, progress);

					abilityCount++;
				}
			}
		}

		// 2. Handle Glitch Effect.
		if (_isGlitching)
		{
			_glitchTimer -= (float)delta;
			Position += new Vector3((float)GD.RandRange(-0.05f, 0.05f), 0, (float)GD.RandRange(-0.05f, 0.05f));
			_healthText.Modulate = new Color(1, 1, 1);

			if (_glitchTimer <= 0)
			{
				_isGlitching = false;
				_healthText.Modulate = new Color(0, 1, 1);
			}
		}

		UpdateScore();
	}
}
