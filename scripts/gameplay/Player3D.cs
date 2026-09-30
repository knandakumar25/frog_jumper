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
	private bool _canDoubleJump = true;
	private float _cameraPitch = -15f;

	// ─── Character Data ──────────────────────────────────────────────────────────
	private struct CharacterStats
	{
		public string ModelPath;
		public float SpeedMod;
		public float AgilityMod;

		public CharacterStats(string path, float speed, float agility)
		{
			ModelPath = path;
			SpeedMod = speed;
			AgilityMod = agility;
		}
	}

	private static readonly System.Collections.Generic.Dictionary<string, CharacterStats> CharacterRegistry = new()
	{
		{ "Cyber Shogun", new CharacterStats("res://assets/models/hq/cyber_shogun.glb", 98f, 96f) },
		{ "Solaria",      new CharacterStats("res://assets/models/hq/solaria.glb", 95f, 99f) },
		{ "Oceanus",      new CharacterStats("res://assets/models/hq/oceanus.glb", 96f, 97f) },
		{ "Surya Prime",  new CharacterStats("res://assets/models/hq/surya_prime.glb", 97f, 98f) },
		{ "Mirage Blade", new CharacterStats("res://assets/models/hq/mirage_blade.glb", 99f, 99f) },
		{ "Apex Titan",   new CharacterStats("res://assets/models/hq/apex_titan.glb", 93f, 94f) },
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

			// Scale base speed (7.0) by the character's speed stat.
			Speed = 7.0f * (stats.SpeedMod / 100.0f);
			GD.Print($"Player3D: Deployed {selectedChar}. Adjusted Speed: {Speed}");
		}
		else
		{
			GD.PrintErr($"Player3D: Character {selectedChar} not found in registry. Defaulting to Shogun.");
			LoadCharacterModel(CharacterRegistry["Cyber Shogun"].ModelPath);
		}

		// 3. Setup Operative Abilities (fast-paced short cooldowns).
		SetupCharacterAbilities(selectedChar);

		// 4. Setup Diegetic HUD.
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

		if (!OS.HasFeature("editor_headless"))
		{
			Input.MouseMode = Input.MouseModeEnum.Hidden;
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Completely eliminate mouse cursor compatibility
		if (@event is InputEventMouse)
		{
			GetViewport()?.SetInputAsHandled();
			return;
		}

		// Keyboard ability triggers (E = Primary, F = Secondary, Q/R = Camera Orbit)
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.E)
			{
				_primaryAbility?.Execute(this);
			}
			else if (keyEvent.Keycode == Key.F)
			{
				_secondaryAbility?.Execute(this);
			}
			else if (keyEvent.Keycode == Key.Q && _cameraPivot != null)
			{
				_cameraPivot.RotateY(Mathf.DegToRad(30f));
			}
			else if (keyEvent.Keycode == Key.R && _cameraPivot != null)
			{
				_cameraPivot.RotateY(Mathf.DegToRad(-30f));
			}
		}

		// Gamepad ability triggers (X / RB = Primary, Y / LB = Secondary)
		if (@event is InputEventJoypadButton joyButton && joyButton.Pressed)
		{
			if (joyButton.ButtonIndex == JoyButton.X || joyButton.ButtonIndex == JoyButton.RightShoulder)
			{
				_primaryAbility?.Execute(this);
			}
			else if (joyButton.ButtonIndex == JoyButton.Y || joyButton.ButtonIndex == JoyButton.LeftShoulder)
			{
				_secondaryAbility?.Execute(this);
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
			if (_animPlayer == null || (_animPlayer.CurrentAnimation != "Double_Jump" && _animPlayer.CurrentAnimation != "Double_Jumping"))
			{
				PlayAbilityAnimation("jumping");
			}
		}
		else
		{
			_canDoubleJump = true;
		}

		// 2. Jump & Double Jump
		if (Input.IsActionJustPressed("jump"))
		{
			if (IsOnFloor())
			{
				Velocity = Velocity with { Y = JumpVelocity };
				PlayAbilityAnimation("jumping");
			}
			else if (_canDoubleJump)
			{
				_canDoubleJump = false;
				Velocity = Velocity with { Y = JumpVelocity * 1.15f };
				PlayAbilityAnimation("double_jump");
			}
		}

		// 3. Movement Input
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
				if (!IsAbilityPlaying())
				{
					PlayAbilityAnimation(direction.Length() > 0.5f ? "running" : "walking");
				}

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
				if (!IsAbilityPlaying())
				{
					PlayAbilityAnimation("standing");
				}
			}
		}
		else
		{
			Velocity = Velocity with { X = 0, Z = 0 };
			if (!IsAbilityPlaying())
			{
				PlayAbilityAnimation("standing");
			}
		}

		// 4. Gamepad Right Stick Camera Orbit
		float joyCamX = Input.GetJoyAxis(0, JoyAxis.RightX);
		float joyCamY = Input.GetJoyAxis(0, JoyAxis.RightY);
		if (Mathf.Abs(joyCamX) > 0.15f && _cameraPivot != null)
		{
			_cameraPivot.RotateY(-joyCamX * RotationSpeed * 0.25f * fDelta);
		}
		if (Mathf.Abs(joyCamY) > 0.15f && _camera != null)
		{
			_cameraPitch = Mathf.Clamp(_cameraPitch - joyCamY * RotationSpeed * 12f * fDelta, -60f, 30f);
			_camera.RotationDegrees = new Vector3(_cameraPitch, 0f, 0f);
		}

		MoveAndSlide();
	}

	private bool IsAbilityPlaying()
	{
		if (_animPlayer == null || !_animPlayer.IsPlaying()) return false;
		string cur = _animPlayer.CurrentAnimation;
		return cur != "Idle" && cur != "Walking" && cur != "Running" && cur != "Standing" && cur != "Jumping" && cur != "RESET";
	}

	public void PlayAbilityAnimation(string animName)
	{
		if (_animPlayer == null) return;

		// Map generic names to the specific Meshy GLB animation names.
		string finalAnim = animName switch
		{
			"running" => "Running",
			"walking" => "Walking",
			"idle" => "Standing",
			"standing" => "Standing",
			"jumping" => "Jumping",
			"double_jump" => "Double_Jump",
			"double_jumping" => "Double_Jump",
			"dash" => "Dash",
			"ground_slam" => "Ground_Slam",
			_ => animName
		};

		if (_animPlayer.HasAnimation(finalAnim))
		{
			var anim = _animPlayer.GetAnimation(finalAnim);
			if (anim != null && (finalAnim == "Standing" || finalAnim == "Running" || finalAnim == "Walking"))
			{
				anim.LoopMode = Animation.LoopModeEnum.Linear;
			}
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

	private void SetupCharacterAbilities(string characterName)
	{
		// Clean any previous ability children
		foreach (var child in GetChildren())
		{
			if (child is Ability ab)
			{
				ab.QueueFree();
			}
		}

		_primaryAbility = new OperativeAbility();
		_secondaryAbility = new OperativeAbility();

		switch (characterName)
		{
			case "Solaria":
				_primaryAbility.Name = "Corona Lance";
				_primaryAbility.Type = AbilityType.Push;
				_primaryAbility.Cooldown = 4.5f;
				_primaryAbility.Damage = 30f;
				_primaryAbility.EffectRadius = 4.0f;
				_primaryAbility.KnockbackForce = 8.0f;
				_primaryAbility.AnimationName = "Beam_Cast";

				_secondaryAbility.Name = "Supernova Flash";
				_secondaryAbility.Type = AbilityType.AoE;
				_secondaryAbility.Cooldown = 6.5f;
				_secondaryAbility.Damage = 25f;
				_secondaryAbility.EffectRadius = 6.0f;
				_secondaryAbility.KnockbackForce = 5.0f;
				_secondaryAbility.AnimationName = "Supernova_Burst";
				break;

			case "Oceanus":
				_primaryAbility.Name = "Tidal Torrent";
				_primaryAbility.Type = AbilityType.Push;
				_primaryAbility.Cooldown = 4.0f;
				_primaryAbility.Damage = 28f;
				_primaryAbility.EffectRadius = 4.5f;
				_primaryAbility.KnockbackForce = 9.0f;
				_primaryAbility.AnimationName = "Torrent_Blast";

				_secondaryAbility.Name = "Maelstrom Vortex";
				_secondaryAbility.Type = AbilityType.AoE;
				_secondaryAbility.Cooldown = 6.0f;
				_secondaryAbility.Damage = 20f;
				_secondaryAbility.EffectRadius = 5.5f;
				_secondaryAbility.KnockbackForce = 4.0f;
				_secondaryAbility.AnimationName = "Vortex_Cast";
				break;

			case "Surya Prime":
				_primaryAbility.Name = "Solar Shield Bash";
				_primaryAbility.Type = AbilityType.Dash;
				_primaryAbility.Cooldown = 4.5f;
				_primaryAbility.Damage = 35f;
				_primaryAbility.EffectRadius = 3.5f;
				_primaryAbility.KnockbackForce = 10.0f;
				_primaryAbility.AnimationName = "Shield_Bash";

				_secondaryAbility.Name = "Aegis of the Sun";
				_secondaryAbility.Type = AbilityType.Push;
				_secondaryAbility.Cooldown = 5.5f;
				_secondaryAbility.Damage = 15f;
				_secondaryAbility.EffectRadius = 4.0f;
				_secondaryAbility.KnockbackForce = 5.0f;
				_secondaryAbility.AnimationName = "Shield_Guard";
				break;

			case "Mirage Blade":
				_primaryAbility.Name = "Phase Blink";
				_primaryAbility.Type = AbilityType.Dash;
				_primaryAbility.Cooldown = 3.5f;
				_primaryAbility.Damage = 25f;
				_primaryAbility.EffectRadius = 3.0f;
				_primaryAbility.KnockbackForce = 5.0f;
				_primaryAbility.AnimationName = "Phase_Dash";

				_secondaryAbility.Name = "Holographic Decoy";
				_secondaryAbility.Type = AbilityType.AoE;
				_secondaryAbility.Cooldown = 6.0f;
				_secondaryAbility.Damage = 15f;
				_secondaryAbility.EffectRadius = 4.0f;
				_secondaryAbility.KnockbackForce = 3.0f;
				_secondaryAbility.AnimationName = "Decoy_Deploy";
				break;

			case "Apex Titan":
				_primaryAbility.Name = "Tectonic Slam";
				_primaryAbility.Type = AbilityType.AoE;
				_primaryAbility.Cooldown = 4.5f;
				_primaryAbility.Damage = 45f;
				_primaryAbility.EffectRadius = 5.0f;
				_primaryAbility.KnockbackForce = 12.0f;
				_primaryAbility.AnimationName = "Tectonic_Slam";

				_secondaryAbility.Name = "Magnetic Tether";
				_secondaryAbility.Type = AbilityType.Push;
				_secondaryAbility.Cooldown = 5.5f;
				_secondaryAbility.Damage = 25f;
				_secondaryAbility.EffectRadius = 6.0f;
				_secondaryAbility.KnockbackForce = -8.0f;
				_secondaryAbility.AnimationName = "Magnetic_Pull";
				break;

			default: // Cyber Shogun
				_primaryAbility.Name = "Shogun's Flash";
				_primaryAbility.Type = AbilityType.Dash;
				_primaryAbility.Cooldown = 4.0f;
				_primaryAbility.Damage = 35f;
				_primaryAbility.EffectRadius = 3.5f;
				_primaryAbility.KnockbackForce = 6.0f;
				_primaryAbility.AnimationName = "Blade_Dash";

				_secondaryAbility.Name = "Kinetic Parry";
				_secondaryAbility.Type = AbilityType.Melee;
				_secondaryAbility.Cooldown = 5.5f;
				_secondaryAbility.Damage = 20f;
				_secondaryAbility.EffectRadius = 3.0f;
				_secondaryAbility.KnockbackForce = 4.0f;
				_secondaryAbility.AnimationName = "Parry_Stance";
				break;
		}

		AddChild(_primaryAbility);
		AddChild(_secondaryAbility);
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
