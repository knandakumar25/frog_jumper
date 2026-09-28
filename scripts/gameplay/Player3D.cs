using Godot;
using System;

/// <summary>
/// Player3D handles 3D locomotion for the Axiom Force operatives.
/// It implements standard WASD movement, jumping, and physics interaction
/// using CharacterBody3D.
/// </summary>
public partial class Player3D : CharacterBody3D
{
	// ─── Constants ───────────────────────────────────────────────────────────
	[Export] public float Speed = 7.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public bool CaptureMouseOnReady = true;

	// ─── State ───────────────────────────────────────────────────────────────
	private bool _isMoving = true;
	private float _cameraPitch = -15f;

	// ─── Character Data ──────────────────────────────────────────────────────────
	private struct CharacterStats
	{
		public string ModelPath;
		public float SpeedMod;
		public float AgilityMod;
		public AbilityData PrimaryAbility;
		public AbilityData SecondaryAbility;

		public CharacterStats(string path, float speed, float agility, AbilityData primary, AbilityData secondary)
		{
			ModelPath = path;
			SpeedMod = speed;
			AgilityMod = agility;
			PrimaryAbility = primary;
			SecondaryAbility = secondary;
		}
	}

	private struct AbilityData
	{
		public string Name;
		public AbilityType Type;
		public float Cooldown;
		public float Damage;
		public float Range;
		public float EffectRadius;
		public float Knockback;
		public string Animation;

		public AbilityData(string name, AbilityType type, float cd, float dmg, float range, float rad, float kb, string anim)
		{
			Name = name;
			Type = type;
			Cooldown = cd;
			Damage = dmg;
			Range = range;
			EffectRadius = rad;
			Knockback = kb;
			Animation = anim;
		}
	}

	private static readonly System.Collections.Generic.Dictionary<string, CharacterStats> CharacterRegistry = new()
	{
		{ "Cyber Shogun", new CharacterStats("res://assets/models/hq/cyber_shogun.glb", 98f, 96f,
			new AbilityData("Sword Slash", AbilityType.Melee, 0.4f, 25f, 2.0f, 0f, 0f, "Thrust_Slash"),
			new AbilityData("Karate Strike", AbilityType.Melee, 0.8f, 40f, 1.5f, 0f, 0f, "Karate_Strike")) },
		{ "Solaria",      new CharacterStats("res://assets/models/hq/solaria.glb", 95f, 99f,
			new AbilityData("Solar Flare", AbilityType.AoE, 2.0f, 30f, 0f, 5.0f, 0f, "Attack"),
			new AbilityData("Sun Beam", AbilityType.Melee, 0.6f, 20f, 2.0f, 0f, 0f, "Attack")) },
		{ "Oceanus",      new CharacterStats("res://assets/models/hq/oceanus.glb", 96f, 97f,
			new AbilityData("Plasma Wave", AbilityType.Push, 1.5f, 20f, 0f, 4.0f, 10f, "Attack"),
			new AbilityData("Hydro Strike", AbilityType.Melee, 0.5f, 25f, 2.0f, 0f, 0f, "Attack")) },
		{ "Surya Prime",  new CharacterStats("res://assets/models/hq/surya_prime.glb", 97f, 98f,
			new AbilityData("Cosmic Slam", AbilityType.AoE, 3.0f, 50f, 0f, 6.0f, 5f, "Attack"),
			new AbilityData("Solar Jab", AbilityType.Melee, 0.4f, 20f, 2.0f, 0f, 0f, "Attack")) },
		{ "Mirage Blade", new CharacterStats("res://assets/models/hq/mirage_blade.glb", 99f, 99f,
			new AbilityData("Phantom Dash", AbilityType.Dash, 1.2f, 30f, 0f, 0f, 0f, "Attack"),
			new AbilityData("Crystal Edge", AbilityType.Melee, 0.3f, 20f, 2.0f, 0f, 0f, "Attack")) },
		{ "Apex Titan",   new CharacterStats("res://assets/models/hq/apex_titan.glb", 93f, 94f,
			new AbilityData("EM Storm", AbilityType.AoE, 4.0f, 35f, 0f, 7.0f, 0f, "Attack"),
			new AbilityData("Titan Punch", AbilityType.Melee, 0.7f, 60f, 2.0f, 0f, 15f, "Attack")) },
	};

	// ─── Node References ────────────────────────────────────────────────────
	private Node3D _cameraPivot;
	private Camera3D _camera;
	private AnimationPlayer _animPlayer;
	private WeaponArea3D _weaponArea;
	private Node3D _characterModel;
	private OperativeAbility _primaryAbility;
	private OperativeAbility _secondaryAbility;

	public override void _Ready()
	{
		// 1. Identify the selected operative.
		var global = GetNode<Node>("/root/Global");
		string selectedChar = global.Get("selected_character").AsString();
			if (string.IsNullOrEmpty(selectedChar)) selectedChar = "Cyber Shogun";

		// 2. Load the correct model and stats.
		if (CharacterRegistry.TryGetValue(selectedChar, out var stats))
		{
			LoadCharacterModel(stats.ModelPath);
			SetupAbilities(stats);

			// Scale base speed (7.0) by the character's speed stat.
			Speed = 7.0f * (stats.SpeedMod / 100.0f);
			GD.Print($"Player3D: Deployed {selectedChar}. Adjusted Speed: {Speed}");
		}
		else
		{
			GD.PrintErr($"Player3D: Character {selectedChar} not found in registry. Defaulting to Shogun.");
			LoadCharacterModel(CharacterRegistry["Cyber Shogun"].ModelPath);
		}

		// 3. Setup Diegetic HUD.
		var hud = new PlayerHUD3D();
		AddChild(hud);

		// Setup camera references.
		_cameraPivot = GetNodeOrNull<Node3D>("CameraPivot");
		_camera = GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
		if (_camera != null)
		{
			_cameraPitch = _camera.RotationDegrees.X;
		}

		// Find the WeaponArea3D.
		_weaponArea = GetNodeOrNull<WeaponArea3D>("WeaponArea3D");

		if (_camera == null)
		{
			GD.PrintErr("Player3D: Camera3D not found! Please ensure the node structure is: Player3D -> CameraPivot -> Camera3D");
		}

		if (CaptureMouseOnReady && !OS.HasFeature("editor_headless"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Capture mouse on left click in the viewport if not already captured
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (Input.MouseMode != Input.MouseModeEnum.Captured && !GetTree().Paused)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}

		// 3rd-person orbit camera control via mouse motion
		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
		{
			if (_cameraPivot != null)
			{
				_cameraPivot.RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			}

			if (_camera != null)
			{
				_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Relative.Y * MouseSensitivity * Mathf.RadToDeg(1f), -60f, 30f);
				_camera.RotationDegrees = new Vector3(_cameraPitch, 0f, 0f);
			}
		}

		// Optional keyboard camera rotation (Q/E)
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Q && _cameraPivot != null)
			{
				_cameraPivot.RotateY(Mathf.DegToRad(30f));
			}
			else if (keyEvent.Keycode == Key.E && _cameraPivot != null)
			{
				_cameraPivot.RotateY(Mathf.DegToRad(-30f));
			}
		}
	}

	private void LoadCharacterModel(string path)
	{
		var scene = GD.Load<PackedScene>(path);
		if (scene != null)
		{
			_characterModel = scene.Instantiate<Node3D>();
			CharacterSelection.CleanDuplicateMeshes(_characterModel);
			AddChild(_characterModel);

			// Find the AnimationPlayer inside the instantiated GLB model.
			_animPlayer = _characterModel.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
		}
		else
		{
			GD.PrintErr($"Player3D: Failed to load model at {path}");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float fDelta = (float)delta;

		// 1. Gravity
		if (!IsOnFloor())
		{
			Velocity += GetGravity() * fDelta;
			PlayAbilityAnimation("jumping"); // Assuming a jump animation exists or defaults to idle
		}

		// 2. Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			Velocity = Velocity with { Y = JumpVelocity };
		}

		// 3. Attacks
		if (Input.IsActionJustPressed("attack"))
		{
			_primaryAbility?.Execute(this);
		}
		if (Input.IsActionJustPressed("karate"))
		{
			_secondaryAbility?.Execute(this);
		}

		// 4. Movement Input
		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");

		// Calculate movement direction relative to the camera
		Vector3 direction = Vector3.Zero;
		if (_cameraPivot != null)
		{
			Vector3 forward = -_cameraPivot.GlobalTransform.Basis.Z;
			Vector3 right = _cameraPivot.GlobalTransform.Basis.X;

			// Flatten vectors to prevent flying/sinking when looking up/down
			forward.Y = 0;
			right.Y = 0;
			if (forward.LengthSquared() > 0) forward = forward.Normalized();
			if (right.LengthSquared() > 0) right = right.Normalized();

			// Guard forward vector normalization to avoid "Vector3 cannot be normalized" warning
			if (forward.LengthSquared() > 0)
			{
				forward = forward.Normalized();
			}
			else
			{
				// Fallback to default forward if camera looks straight up or down
				forward = Vector3.Forward;
			}

			// Guard right vector normalization
			if (right.LengthSquared() > 0)
			{
				right = right.Normalized();
			}
			else
			{
				// Fallback to default right if camera basis collapses
				right = Vector3.Right;
			}

			Vector3 combined = (forward * -inputDir.Y + right * inputDir.X);
			direction = combined.LengthSquared() > 0 ? combined.Normalized() : Vector3.Zero;
			// Guard combined direction vector normalization
			if (combined.LengthSquared() > 0)
			{
				direction = combined.Normalized();
			}
			else
			{
				// Sensible stationary fallback when no movement input is provided
				direction = Vector3.Zero;
			}
		}
		else
		{
			// Fallback if no camera pivot
			Vector3 fallbackDir = new Vector3(inputDir.X, 0, inputDir.Y);
			direction = fallbackDir.LengthSquared() > 0 ? fallbackDir.Normalized() : Vector3.Zero;
			// Guard fallback movement vector normalization
			if (fallbackDir.LengthSquared() > 0)
			{
				direction = fallbackDir.Normalized();
			}
			else
			{
				// Sensible stationary fallback when no movement input is provided
				direction = Vector3.Zero;
			}
		}

		if (_isMoving)
		{
			if (direction != Vector3.Zero)
			{
				Velocity = new Vector3(direction.X * Speed, Velocity.Y, direction.Z * Speed);
				PlayAbilityAnimation(direction.Length() > 0.5f ? "running" : "walking");

				// Smoothly rotate the character model to face the movement direction
				if (_characterModel != null)
				{
					float targetAngle = Mathf.Atan2(direction.X, direction.Z);
					float currentAngle = _characterModel.Rotation.Y;
					_characterModel.Rotation = new Vector3(
						_characterModel.Rotation.X,
						Mathf.LerpAngle(currentAngle, targetAngle, RotationSpeed * fDelta),
						_characterModel.Rotation.Z
					);
				}
			}
			else
			{
				Velocity = Velocity with { X = Mathf.MoveToward(Velocity.X, 0, Speed), Z = Mathf.MoveToward(Velocity.Z, 0, Speed) };
				PlayAbilityAnimation("idle");
			}
		}
		else
		{
			Velocity = Velocity with { X = 0, Z = 0 };
			PlayAbilityAnimation("idle");
		}

		MoveAndSlide();
	}

	private void SetupAbilities(CharacterStats stats)
	{
		_primaryAbility = CreateAbility(stats.PrimaryAbility);
		_secondaryAbility = CreateAbility(stats.SecondaryAbility);
	}

	private OperativeAbility CreateAbility(AbilityData data)
	{
		var ability = new OperativeAbility
		{
			Name = data.Name,
			Cooldown = data.Cooldown,
			Damage = data.Damage,
			Range = data.Range,
			EffectRadius = data.EffectRadius,
			KnockbackForce = data.Knockback,
			AnimationName = data.Animation,
			Type = data.Type
		};
		AddChild(ability);
		return ability;
	}

	public void PlayAbilityAnimation(string animName)
	{
		if (_animPlayer == null) return;

		// Map generic names to the specific Meshy GLB animation names.
		string finalAnim = animName switch
		{
			"running" => "Running",
			"walking" => "Walking",
			"idle" => "Idle",
			"sword_attack" => "Thrust_Slash",
			"karate_attack" => "Karate_Strike", // Assuming this naming convention
			_ => animName
		};

		if (_animPlayer.HasAnimation(finalAnim))
		{
			_animPlayer.Play(finalAnim);
			return;
		}

		// Fallback fuzzy search (e.g. "idle" matching "Armature|Idle_02|baselayer")
		string searchTarget = animName.ToLowerInvariant();
		foreach (var clip in _animPlayer.GetAnimationList())
		{
			string clipLower = clip.ToString().ToLowerInvariant();
			if (clipLower.Contains(searchTarget))
			{
				_animPlayer.Play(clip);
				return;
			}
		}
	}

	public void StopMovement()
	{
		_isMoving = false;
	}

	public void ResumeMovement()
	{
		_isMoving = true;
	}

	public void ApplyStatusEffect(StatusEffect effect)
	{
		AddChild(effect);
		effect.OnApply(this);
	}
}
