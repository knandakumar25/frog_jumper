using Godot;
using System;

/// <summary>
/// HeroAbilityVFX manages all high-fidelity elemental visual effects for operative abilities.
/// Supports both Character Selection Showcase preview and in-game combat execution.
/// Features velocity-aligned fluid droplets, aerodynamic flame tongues, tumbling 3D rock fragments,
/// and smooth procedural acoustic shockwave rings with zero billboard seams.
/// </summary>
public static class HeroAbilityVFX
{
	private const string VfxGroupName = "ActiveHeroAbilityVFX";

	private static ImageTexture? _softGaussianTexture;
	private static ImageTexture? _streakTexture;
	private static ImageTexture? _ringTexture;
	private static ImageTexture? _sparkTexture;

	#region Procedural Textures & Materials

	/// <summary>
	/// Ultra-soft Gaussian falloff circular glow texture for mist, atmospheric haze, and soft ambient glow.
	/// Completely eliminates hard rectangular borders or billboard seams.
	/// </summary>
	private static ImageTexture GetSoftGaussianTexture()
	{
		if (_softGaussianTexture != null && GodotObject.IsInstanceValid(_softGaussianTexture))
		{
			return _softGaussianTexture;
		}

		const int size = 64;
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var center = new Vector2(size * 0.5f, size * 0.5f);
		float radius = size * 0.5f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dist = center.DistanceTo(new Vector2(x, y)) / radius;
				if (dist < 1.0f)
				{
					float falloff = 1.0f - dist;
					float alpha = Mathf.Pow(falloff, 2.2f);
					img.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
				}
				else
				{
					img.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
				}
			}
		}

		_softGaussianTexture = ImageTexture.CreateFromImage(img);
		return _softGaussianTexture;
	}

	/// <summary>
	/// Smooth organic aerodynamic streak / droplet texture for fluid jets, flame tongues, and trails.
	/// Smoothly tapers to zero on all 4 edges so it never exhibits hard rectangular boundaries.
	/// </summary>
	private static ImageTexture GetStreakTexture()
	{
		if (_streakTexture != null && GodotObject.IsInstanceValid(_streakTexture))
		{
			return _streakTexture;
		}

		const int size = 64;
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		float cx = size * 0.5f;
		float cy = size * 0.4f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dy = (y - cy) / (size * 0.45f);
				float taper = dy > 0 ? (1.0f + dy * 0.6f) : (1.0f - dy * 0.2f);
				float dx = (x - cx) / (size * 0.28f * taper);

				float rSq = dx * dx + dy * dy;
				if (rSq < 1.0f)
				{
					float a = 1.0f - Mathf.Sqrt(rSq);
					float alpha = a * a;
					img.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
				}
				else
				{
					img.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
				}
			}
		}

		_streakTexture = ImageTexture.CreateFromImage(img);
		return _streakTexture;
	}

	/// <summary>
	/// Smooth procedural energy shockwave ring with inner and outer Gaussian falloff.
	/// Used for floor shockwaves, ripples, and acoustic pulses without clunky 3D Torus geometry.
	/// </summary>
	private static ImageTexture GetRingTexture()
	{
		if (_ringTexture != null && GodotObject.IsInstanceValid(_ringTexture))
		{
			return _ringTexture;
		}

		const int size = 128;
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var center = new Vector2(size * 0.5f, size * 0.5f);
		float radius = size * 0.48f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dist = center.DistanceTo(new Vector2(x, y)) / radius;
				float ringCenter = 0.78f;
				float halfWidth = 0.18f;
				float delta = Mathf.Abs(dist - ringCenter);

				if (delta < halfWidth)
				{
					float norm = 1.0f - (delta / halfWidth);
					float alpha = norm * norm;
					img.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
				}
				else
				{
					img.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
				}
			}
		}

		_ringTexture = ImageTexture.CreateFromImage(img);
		return _ringTexture;
	}

	/// <summary>
	/// Starburst spark texture for high-intensity incandescent sparks and prismatic glints.
	/// </summary>
	private static ImageTexture GetSparkTexture()
	{
		if (_sparkTexture != null && GodotObject.IsInstanceValid(_sparkTexture))
		{
			return _sparkTexture;
		}

		const int size = 64;
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var center = new Vector2(size * 0.5f, size * 0.5f);
		float radius = size * 0.5f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dist = center.DistanceTo(new Vector2(x, y)) / radius;
				float dx = Mathf.Abs(x - center.X) / radius;
				float dy = Mathf.Abs(y - center.Y) / radius;

				float core = Mathf.Clamp(1.0f - dist * 2.2f, 0f, 1f);
				float crossX = Mathf.Clamp(1.0f - dy * 6f, 0f, 1f) * Mathf.Clamp(1.0f - dx * 1.2f, 0f, 1f);
				float crossY = Mathf.Clamp(1.0f - dx * 6f, 0f, 1f) * Mathf.Clamp(1.0f - dy * 1.2f, 0f, 1f);

				float val = Mathf.Clamp(core + (crossX + crossY) * 0.45f, 0f, 1f);
				img.SetPixel(x, y, new Color(1f, 1f, 1f, val * val));
			}
		}

		_sparkTexture = ImageTexture.CreateFromImage(img);
		return _sparkTexture;
	}

	private enum TextureType { FluidStreak, SoftGaussian, Spark, Ring }

	private static StandardMaterial3D CreateParticleMaterial(TextureType textureType, BaseMaterial3D.BlendModeEnum blendMode = BaseMaterial3D.BlendModeEnum.Mix)
	{
		Texture2D tex = textureType switch
		{
			TextureType.FluidStreak => GetStreakTexture(),
			TextureType.Spark => GetSparkTexture(),
			TextureType.Ring => GetRingTexture(),
			_ => GetSoftGaussianTexture()
		};

		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = blendMode,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
			BillboardKeepScale = true,
			AlbedoTexture = tex,
			VertexColorUseAsAlbedo = true
		};
	}

	private static StandardMaterial3D CreateRingMaterial(Color color, BaseMaterial3D.BlendModeEnum blendMode = BaseMaterial3D.BlendModeEnum.Mix)
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = blendMode,
			AlbedoTexture = GetRingTexture(),
			AlbedoColor = color,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};
	}

	#endregion

	/// <summary>
	/// Clears all currently active ability VFX inside the parent container to prevent clutter.
	/// </summary>
	public static void ClearActiveVFX(Node3D parent)
	{
		if (parent == null || !GodotObject.IsInstanceValid(parent)) return;

		var children = parent.GetChildren();
		foreach (var child in children)
		{
			if (child is Node3D node && node.IsInGroup(VfxGroupName))
			{
				node.QueueFree();
			}
		}
	}

	/// <summary>
	/// Triggers the full elemental VFX corresponding to the character and ability.
	/// Note: Pure martial operatives (Cyber Shogun, Mirage Blade) do not spawn visual effects.
	/// </summary>
	public static Node3D? TriggerAbilityVFX(Node3D parent, Node3D heroModel, string characterName, string animName, Vector3 forwardDir)
	{
		if (parent == null || !GodotObject.IsInstanceValid(parent)) return null;
		if (heroModel == null || !GodotObject.IsInstanceValid(heroModel)) return null;

		// Clean up existing active preview effects
		ClearActiveVFX(parent);

		// Pure martial/blade operatives (Cyber Shogun, Mirage Blade) do not spawn visual effects
		switch (characterName)
		{
			case "Oceanus":
			case "Surya Prime":
			case "Apex Titan":
			case "Aura Phoenicia":
			case "Phoenicia":
				break;
			default:
				return null;
		}

		Vector3 forward = forwardDir.LengthSquared() > 0.001f ? forwardDir.Normalized() : new Vector3(0, 0, 1);

		var vfxRoot = new Node3D
		{
			Name = $"VFX_{characterName}_{animName}_{GD.Randi() % 10000}"
		};
		vfxRoot.AddToGroup(VfxGroupName);
		parent.AddChild(vfxRoot);

		// Dispatch by character and animation
		switch (characterName)
		{
			case "Oceanus":
				DispatchOceanusVFX(vfxRoot, heroModel, animName, forward);
				break;
			case "Surya Prime":
				DispatchSuryaPrimeVFX(vfxRoot, heroModel, animName, forward);
				break;
			case "Apex Titan":
				DispatchApexTitanVFX(vfxRoot, heroModel, animName, forward);
				break;
			case "Aura Phoenicia":
			case "Phoenicia":
				DispatchPhoeniciaVFX(vfxRoot, heroModel, animName, forward);
				break;
		}

		// Ensure automatic self-cleanup after 2.8s
		var cleanupTween = vfxRoot.CreateTween();
		cleanupTween.TweenInterval(2.8f);
		cleanupTween.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(vfxRoot))
			{
				vfxRoot.QueueFree();
			}
		}));

		return vfxRoot;
	}

	#region Helpers

	private static Vector3 GetHandsPosition(Node3D model, Vector3 forward)
	{
		Vector3 fallbackPos = model.GlobalPosition + Vector3.Up * 0.82f + forward * 0.35f;
		var skeleton = FindChildByType<Skeleton3D>(model);
		if (skeleton != null)
		{
			int rHand = skeleton.FindBone("RightHand");
			int lHand = skeleton.FindBone("LeftHand");

			if (rHand != -1 && lHand != -1)
			{
				Vector3 rPos = skeleton.ToGlobal(skeleton.GetBoneGlobalPose(rHand).Origin);
				Vector3 lPos = skeleton.ToGlobal(skeleton.GetBoneGlobalPose(lHand).Origin);
				Vector3 mid = (rPos + lPos) * 0.5f;
				if (mid.DistanceTo(model.GlobalPosition) < 2.5f && mid.Y > model.GlobalPosition.Y)
				{
					return mid + forward * 0.15f;
				}
			}
			else if (rHand != -1)
			{
				Vector3 rPos = skeleton.ToGlobal(skeleton.GetBoneGlobalPose(rHand).Origin);
				if (rPos.DistanceTo(model.GlobalPosition) < 2.5f && rPos.Y > model.GlobalPosition.Y)
				{
					return rPos + forward * 0.15f;
				}
			}
		}

		return fallbackPos;
	}

	private static Gradient CreateGradient(Color[] colors, float[] offsets)
	{
		return new Gradient { Colors = colors, Offsets = offsets };
	}

	private static T? FindChildByType<T>(Node parent) where T : class
	{
		if (parent == null) return null;
		foreach (Node child in parent.GetChildren())
		{
			if (child is T typedChild) return typedChild;
			var found = FindChildByType<T>(child);
			if (found != null) return found;
		}
		return null;
	}

	/// <summary>
	/// Spawns an expanding, fading horizontal planar shockwave ring on the floor.
	/// </summary>
	private static MeshInstance3D SpawnFloorShockwaveRing(Node3D root, Vector3 floorPos, Color color, float initialRadius, float targetRadius, float duration)
	{
		var ringMat = CreateRingMaterial(color);
		var ringMesh = new QuadMesh
		{
			Size = Vector2.One,
			Material = ringMat
		};

		var ringInst = new MeshInstance3D
		{
			Mesh = ringMesh,
			RotationDegrees = new Vector3(-90f, 0f, 0f),
			Scale = Vector3.One * initialRadius
		};
		root.AddChild(ringInst);
		ringInst.GlobalPosition = floorPos;

		var tween = ringInst.CreateTween().SetParallel(true);
		tween.TweenProperty(ringInst, "scale", Vector3.One * targetRadius, duration).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(ringMat, "albedo_color:a", 0.0f, duration).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		tween.Chain().TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(ringInst)) ringInst.QueueFree();
		}));

		return ringInst;
	}

	#endregion

	#region Oceanus (Hydrokinetic Plasma Manipulator)

	private static void DispatchOceanusVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("torrent") || animLower.Contains("blast") || animLower.Contains("primary"))
		{
			SpawnWaterTorrent(root, model, forward);
		}
		else if (animLower.Contains("vortex") || animLower.Contains("cast") || animLower.Contains("secondary"))
		{
			SpawnWaterVortex(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("jet") || animLower.Contains("slide"))
		{
			SpawnWaterDash(root, model, forward);
		}
		else
		{
			SpawnWaterSlam(root, model, forward);
		}
	}

	/// <summary>
	/// Oceanus Primary: Tidal Torrent (High-velocity directional stream of elongated water streaks + fine mist spray)
	/// Arcs gracefully downward and diagonally across the podium for full visual clarity.
	/// </summary>
	private static void SpawnWaterTorrent(Node3D root, Node3D model, Vector3 forward)
	{
		// Directional arc: forward-right and slightly downward so the camera sees the full volume
		Vector3 blastDir = (forward * 0.65f + Vector3.Right * 0.55f + Vector3.Down * 0.12f).Normalized();
		var streakMat = CreateParticleMaterial(TextureType.FluidStreak);
		var mistMat = CreateParticleMaterial(TextureType.SoftGaussian);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 emitPos = GetHandsPosition(model, forward);

			// 1. Core High-Velocity Hydro Droplet Streaks (Velocity-Aligned)
			var pStream = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.12f, 0.42f), Material = streakMat },
				ParticleFlagAlignY = true,
				Amount = 48,
				Lifetime = 0.55,
				OneShot = false,
				Emitting = true,
				Direction = blastDir,
				Spread = 12.0f,
				Gravity = new Vector3(0, -2.5f, 0),
				InitialVelocityMin = 3.5f,
				InitialVelocityMax = 6.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.90f, 1.00f, 1.0f, 0.98f),  // foamy aqua crest
						new Color(0.20f, 0.88f, 1.0f, 0.90f),  // radiant turquoise fluid
						new Color(0.06f, 0.55f, 0.98f, 0.65f), // deep ocean body
						new Color(0.02f, 0.25f, 0.85f, 0.00f)  // fade
					},
					new[] { 0.0f, 0.20f, 0.70f, 1.0f }
				)
			};
			root.AddChild(pStream);
			pStream.GlobalPosition = emitPos;

			// 2. Fine Ambient Aqua Mist Spray
			var pMist = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.28f, 0.28f), Material = mistMat },
				Amount = 32,
				Lifetime = 0.60,
				OneShot = false,
				Emitting = true,
				Direction = blastDir,
				Spread = 22.0f,
				Gravity = new Vector3(0, -1.2f, 0),
				InitialVelocityMin = 2.5f,
				InitialVelocityMax = 4.8f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.50f, 0.95f, 1.0f, 0.55f),
						new Color(0.10f, 0.70f, 1.0f, 0.35f),
						new Color(0.02f, 0.30f, 0.80f, 0.00f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pMist);
			pMist.GlobalPosition = emitPos;

			// 3. Ground Impact Splash Ring on podium edge
			Vector3 impactPos = model.GlobalPosition + forward * 0.85f + Vector3.Right * 0.35f + new Vector3(0, 0.03f, 0);
			SpawnFloorShockwaveRing(root, impactPos, new Color(0.25f, 0.95f, 1.0f, 0.85f), 0.35f, 1.6f, 0.55f);

			// Continuous emission during cast, stops at 1.4s
			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				pStream.Emitting = false;
				pMist.Emitting = false;
			}));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pStream)) pStream.QueueFree();
				if (GodotObject.IsInstanceValid(pMist)) pMist.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Oceanus Secondary: Maelstrom Vortex (Swirling aquatic whirlpool ring on floor + rising hydro spiral)
	/// </summary>
	private static void SpawnWaterVortex(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 basePos = model.GlobalPosition + new Vector3(0, 0.03f, 0);
		var streakMat = CreateParticleMaterial(TextureType.FluidStreak);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// 1. Expanding & Rotating Ground Whirlpool Ring
			var floorRing = SpawnFloorShockwaveRing(root, basePos, new Color(0.18f, 0.92f, 1.0f, 0.90f), 0.45f, 1.85f, 1.4f);
			if (floorRing != null)
			{
				var rotTween = floorRing.CreateTween();
				rotTween.TweenProperty(floorRing, "rotation:z", Mathf.Pi * 4.0f, 1.4f);
			}

			// 2. Upward-Spiraling Water Streaks
			var pSpiral = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.09f, 0.35f), Material = streakMat },
				ParticleFlagAlignY = true,
				Amount = 45,
				Lifetime = 0.65,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
				EmissionRingRadius = 0.65f,
				EmissionRingInnerRadius = 0.25f,
				Direction = Vector3.Up + forward * 0.1f,
				Spread = 12.0f,
				TangentialAccelMin = 6.0f,
				TangentialAccelMax = 9.5f,
				InitialVelocityMin = 2.2f,
				InitialVelocityMax = 4.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.50f, 0.98f, 1.0f, 0.92f),
						new Color(0.12f, 0.78f, 1.0f, 0.75f),
						new Color(0.02f, 0.40f, 0.90f, 0.00f)
					},
					new[] { 0.0f, 0.50f, 1.0f }
				)
			};
			root.AddChild(pSpiral);
			pSpiral.GlobalPosition = basePos;

			// Continuous emission during cast, stops at 1.4s
			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() => pSpiral.Emitting = false));
			stopTween.TweenInterval(0.65f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSpiral)) pSpiral.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Oceanus Mobility: Aqua Jet (Frictionless forward slide hydro-wake hugging the ground)
	/// </summary>
	private static void SpawnWaterDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + new Vector3(0, 0.04f, 0);
		var streakMat = CreateParticleMaterial(TextureType.FluidStreak);

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.10f, 0.35f), Material = streakMat },
			ParticleFlagAlignY = true,
			Amount = 32,
			Lifetime = 0.45,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward + Vector3.Up * 0.15f,
			Spread = 20.0f,
			Gravity = new Vector3(0, -4.0f, 0),
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.0f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.60f, 0.98f, 1.0f, 0.90f),
					new Color(0.15f, 0.75f, 1.0f, 0.70f),
					new Color(0.02f, 0.35f, 0.85f, 0.00f)
				},
				new[] { 0.0f, 0.40f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.65f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	/// <summary>
	/// Oceanus Slam: Tidal Crash (Downburst hydro splash shockwave on ground impact)
	/// </summary>
	private static void SpawnWaterSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.06f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.20f + new Vector3(0, 0.03f, 0);

			// 1. Expanding Planar Hydro Shockwave Ring
			SpawnFloorShockwaveRing(root, impactPos, new Color(0.20f, 0.95f, 1.0f, 0.90f), 0.4f, 2.0f, 0.65f);

			// 2. Upward Splash Droplet Fountain
			var streakMat = CreateParticleMaterial(TextureType.FluidStreak);
			var pSplash = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.10f, 0.35f), Material = streakMat },
				ParticleFlagAlignY = true,
				Amount = 40,
				Lifetime = 0.60,
				OneShot = true,
				Explosiveness = 0.92f,
				Emitting = true,
				Direction = Vector3.Up,
				Spread = 35.0f,
				Gravity = new Vector3(0, -8.5f, 0),
				InitialVelocityMin = 4.5f,
				InitialVelocityMax = 8.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.85f, 1.0f, 1.0f, 0.98f),
						new Color(0.20f, 0.85f, 1.0f, 0.80f),
						new Color(0.02f, 0.35f, 0.85f, 0.00f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pSplash);
			pSplash.GlobalPosition = impactPos;

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.85f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSplash)) pSplash.QueueFree();
			}));
		}));
	}

	#endregion

	#region Surya Prime (Cosmic Solar Vanguard)

	private static void DispatchSuryaPrimeVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("bash") || animLower.Contains("strike") || animLower.Contains("primary"))
		{
			SpawnSolarPlume(root, model, forward);
		}
		else if (animLower.Contains("guard") || animLower.Contains("shield") || animLower.Contains("secondary") || animLower.Contains("aegis"))
		{
			SpawnFireShieldGuard(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("vanguard"))
		{
			SpawnSolarDash(root, model, forward);
		}
		else
		{
			SpawnSolarSlam(root, model, forward);
		}
	}

	/// <summary>
	/// Surya Prime Primary: Solar Shield Bash (Forward roaring solar plasma plume + flying embers)
	/// Arcs dynamically across the podium.
	/// </summary>
	private static void SpawnSolarPlume(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 fireDir = (forward * 0.65f + Vector3.Right * 0.55f + Vector3.Down * 0.10f).Normalized();
		var flameMat = CreateParticleMaterial(TextureType.FluidStreak);
		var sparkMat = CreateParticleMaterial(TextureType.Spark);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 launchPos = model.GlobalPosition + Vector3.Up * 0.82f + forward * 0.35f;

			// 1. Incandescent Solar Flame Plume (Velocity-Aligned)
			var pFire = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.14f, 0.48f), Material = flameMat },
				ParticleFlagAlignY = true,
				Amount = 40,
				Lifetime = 0.50,
				OneShot = false,
				Emitting = true,
				Direction = fireDir,
				Spread = 14.0f,
				Gravity = new Vector3(0, 1.2f, 0),
				InitialVelocityMin = 3.5f,
				InitialVelocityMax = 6.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 1.0f, 0.90f, 0.98f), // solar core
						new Color(1.0f, 0.75f, 0.15f, 0.90f), // incandescent gold
						new Color(0.98f, 0.35f, 0.05f, 0.55f), // flame amber
						new Color(0.55f, 0.08f, 0.00f, 0.00f)  // fade
					},
					new[] { 0.0f, 0.25f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pFire);
			pFire.GlobalPosition = launchPos;

			// 2. High-Speed Solar Sparks
			var pSparks = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
				Amount = 30,
				Lifetime = 0.55,
				OneShot = false,
				Emitting = true,
				Direction = fireDir + Vector3.Up * 0.15f,
				Spread = 20.0f,
				Gravity = new Vector3(0, 1.0f, 0),
				InitialVelocityMin = 3.2f,
				InitialVelocityMax = 5.8f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 1.0f, 0.85f, 1.0f),
						new Color(1.0f, 0.60f, 0.10f, 0.85f),
						new Color(0.85f, 0.15f, 0.00f, 0.00f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pSparks);
			pSparks.GlobalPosition = launchPos;

			// 3. Ground Shockwave Ring
			Vector3 impactPos = model.GlobalPosition + forward * 0.75f + Vector3.Right * 0.35f + new Vector3(0, 0.03f, 0);
			SpawnFloorShockwaveRing(root, impactPos, new Color(1.0f, 0.75f, 0.15f, 0.85f), 0.35f, 1.6f, 0.55f);

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				pFire.Emitting = false;
				pSparks.Emitting = false;
			}));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pFire)) pFire.QueueFree();
				if (GodotObject.IsInstanceValid(pSparks)) pSparks.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Surya Prime Secondary: Aegis of the Sun (Radiant solar crest barrier + orbiting golden corona embers)
	/// </summary>
	private static void SpawnFireShieldGuard(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.85f + forward * 0.40f;
		var sparkMat = CreateParticleMaterial(TextureType.Spark);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// 1. Shimmering Solar Crest Hardlight Barrier
			var shieldMat = CreateRingMaterial(new Color(1.0f, 0.75f, 0.15f, 0.90f));
			var shieldQuad = new QuadMesh { Size = Vector2.One, Material = shieldMat };
			var shieldInst = new MeshInstance3D
			{
				Mesh = shieldQuad,
				Rotation = new Vector3(0, Mathf.Atan2(forward.X, forward.Z), 0),
				Scale = Vector3.Zero
			};
			root.AddChild(shieldInst);
			shieldInst.GlobalPosition = chestPos;

			var shieldTween = shieldInst.CreateTween().SetParallel(true);
			shieldTween.TweenProperty(shieldInst, "scale", Vector3.One * 1.55f, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			shieldTween.Chain().TweenInterval(0.95f);
			shieldTween.Chain().TweenProperty(shieldMat, "albedo_color:a", 0.0f, 0.35f);
			shieldTween.Chain().TweenCallback(Callable.From(() => shieldInst.QueueFree()));

			// 2. Orbiting Solar Corona Embers
			var pCorona = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
				Amount = 35,
				Lifetime = 0.65,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
				EmissionRingRadius = 0.70f,
				EmissionRingInnerRadius = 0.35f,
				Direction = Vector3.Up,
				Spread = 180.0f,
				TangentialAccelMin = 5.0f,
				TangentialAccelMax = 8.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 1.0f, 0.85f, 0.98f),
						new Color(1.0f, 0.65f, 0.10f, 0.85f),
						new Color(0.85f, 0.20f, 0.00f, 0.00f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pCorona);
			pCorona.GlobalPosition = chestPos;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() => pCorona.Emitting = false));
			stopTween.TweenInterval(0.65f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pCorona)) pCorona.QueueFree();
			}));
		}));
	}

	private static void SpawnSolarDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.4f;
		var flameMat = CreateParticleMaterial(TextureType.FluidStreak);

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.10f, 0.38f), Material = flameMat },
			ParticleFlagAlignY = true,
			Amount = 28,
			Lifetime = 0.40,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 18.0f,
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.0f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(1.0f, 0.98f, 0.80f, 0.98f),
					new Color(1.0f, 0.55f, 0.10f, 0.80f),
					new Color(0.80f, 0.15f, 0.00f, 0.00f)
				},
				new[] { 0.0f, 0.40f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.65f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	private static void SpawnSolarSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.06f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.20f + new Vector3(0, 0.03f, 0);
			SpawnFloorShockwaveRing(root, impactPos, new Color(1.0f, 0.75f, 0.15f, 0.90f), 0.4f, 2.0f, 0.65f);
		}));
	}

	#endregion

	#region Apex Titan (Electro-Magnetic Storm Vanguard)

	private static void DispatchApexTitanVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("tectonic") || animLower.Contains("hammer") || animLower.Contains("primary") || animLower.Contains("crater"))
		{
			SpawnTectonicSlam(root, model, forward);
		}
		else if (animLower.Contains("magnetic") || animLower.Contains("pull") || animLower.Contains("secondary") || animLower.Contains("tether"))
		{
			SpawnMagneticPull(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("surge"))
		{
			SpawnTitanDash(root, model, forward);
		}
		else
		{
			SpawnSeismicDrop(root, model, forward);
		}
	}

	/// <summary>
	/// Apex Titan Primary: Tectonic Slam (Physical 3D tumbling rock shards + seismic shockwave + lightning arcs)
	/// </summary>
	private static void SpawnTectonicSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.06f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.40f + new Vector3(0, 0.03f, 0);

			// 1. Expanding Ground Rupture Ring
			SpawnFloorShockwaveRing(root, impactPos, new Color(0.95f, 0.65f, 0.15f, 0.90f), 0.4f, 2.0f, 0.65f);

			// 2. Physical 3D Tumbling Rock Fragments
			var rockMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
				AlbedoColor = new Color(0.48f, 0.40f, 0.32f, 1.0f),
				Roughness = 0.9f
			};
			var pRocks = new CpuParticles3D
			{
				Mesh = new BoxMesh { Size = new Vector3(0.14f, 0.14f, 0.14f), Material = rockMat },
				Amount = 24,
				Lifetime = 0.60,
				OneShot = true,
				Explosiveness = 0.90f,
				Emitting = true,
				Direction = Vector3.Up + forward * 0.25f,
				Spread = 38.0f,
				Gravity = new Vector3(0, -9.8f, 0),
				InitialVelocityMin = 4.0f,
				InitialVelocityMax = 7.0f,
				AngularVelocityMin = 140.0f,
				AngularVelocityMax = 400.0f
			};
			root.AddChild(pRocks);
			pRocks.GlobalPosition = impactPos;

			// 3. Electric Discharge Lightning Sparks
			var sparkMat = CreateParticleMaterial(TextureType.Spark);
			var pElectric = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
				Amount = 32,
				Lifetime = 0.55,
				OneShot = true,
				Explosiveness = 0.85f,
				Emitting = true,
				Direction = Vector3.Up,
				Spread = 50.0f,
				InitialVelocityMin = 3.5f,
				InitialVelocityMax = 6.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.45f, 0.90f, 1.0f, 0.98f),
						new Color(0.15f, 0.55f, 1.0f, 0.80f),
						new Color(0.00f, 0.25f, 0.70f, 0.00f)
					},
					new[] { 0.0f, 0.50f, 1.0f }
				)
			};
			root.AddChild(pElectric);
			pElectric.GlobalPosition = impactPos;

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.85f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pRocks)) pRocks.QueueFree();
				if (GodotObject.IsInstanceValid(pElectric)) pElectric.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Apex Titan Secondary: Magnetic Tether (Electromagnetic beam surge with inward pull motes)
	/// </summary>
	private static void SpawnMagneticPull(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.85f;
		var sparkMat = CreateParticleMaterial(TextureType.Spark);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// Inward Magnetic Influx Particles
			var pPull = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
				Amount = 35,
				Lifetime = 0.55,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 1.1f,
				Direction = -forward,
				Spread = 25.0f,
				InitialVelocityMin = 3.0f,
				InitialVelocityMax = 5.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.30f, 0.95f, 1.0f, 0.98f),
						new Color(0.90f, 0.30f, 1.0f, 0.80f),
						new Color(0.15f, 0.35f, 0.85f, 0.00f)
					},
					new[] { 0.0f, 0.50f, 1.0f }
				)
			};
			root.AddChild(pPull);
			pPull.GlobalPosition = chestPos + forward * 0.5f;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() => pPull.Emitting = false));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pPull)) pPull.QueueFree();
			}));
		}));
	}

	private static void SpawnTitanDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.15f;
		var mistMat = CreateParticleMaterial(TextureType.SoftGaussian);

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.18f, 0.18f), Material = mistMat },
			Amount = 24,
			Lifetime = 0.40,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 22.0f,
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.0f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.65f, 0.60f, 0.55f, 0.55f),
					new Color(0.35f, 0.30f, 0.25f, 0.00f)
				},
				new[] { 0.0f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.65f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	private static void SpawnSeismicDrop(Node3D root, Node3D model, Vector3 forward)
	{
		SpawnTectonicSlam(root, model, forward);
	}

	#endregion

	#region Aura Phoenicia (Ultrasonic Prism Weaver)

	private static void DispatchPhoeniciaVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("harmonic") || animLower.Contains("pulse") || animLower.Contains("primary"))
		{
			SpawnHarmonicPulse(root, model, forward);
		}
		else if (animLower.Contains("prism") || animLower.Contains("reverberation") || animLower.Contains("secondary"))
		{
			SpawnPrismReverberation(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("wave") || animLower.Contains("glide"))
		{
			SpawnWaveGlide(root, model, forward);
		}
		else
		{
			SpawnCedarsEcho(root, model, forward);
		}
	}

	/// <summary>
	/// Aura Phoenicia Primary: Harmonic Pulse (Concentric ultrasonic acoustic shockwave rings pulsing forward)
	/// </summary>
	private static void SpawnHarmonicPulse(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.85f;
		var sparkMat = CreateParticleMaterial(TextureType.Spark);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// Pulse 3 concentric acoustic shockwave rings
			for (int i = 0; i < 3; i++)
			{
				int ringIdx = i;
				var ringDelay = root.CreateTween();
				ringDelay.TweenInterval(ringIdx * 0.14f);
				ringDelay.TweenCallback(Callable.From(() =>
				{
					if (!GodotObject.IsInstanceValid(root)) return;
					var ringMat = CreateRingMaterial(new Color(0.20f, 0.98f, 0.90f, 0.90f));
					var ringQuad = new QuadMesh { Size = Vector2.One, Material = ringMat };
					var ringInst = new MeshInstance3D
					{
						Mesh = ringQuad,
						Rotation = new Vector3(0, Mathf.Atan2(forward.X, forward.Z), 0),
						Scale = Vector3.One * 0.4f
					};
					root.AddChild(ringInst);
					ringInst.GlobalPosition = chestPos + forward * 0.25f;

					var tween = ringInst.CreateTween().SetParallel(true);
					tween.TweenProperty(ringInst, "scale", Vector3.One * 2.2f, 0.65f);
					tween.TweenProperty(ringInst, "global_position", ringInst.GlobalPosition + (forward * 0.7f + Vector3.Right * 0.4f) * 1.5f, 0.65f);
					tween.TweenProperty(ringMat, "albedo_color:a", 0.0f, 0.65f);
					tween.Chain().TweenCallback(Callable.From(() => ringInst.QueueFree()));
				}));
			}

			// Prismatic Shimmer Sparkles
			var pSparkles = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
				Amount = 35,
				Lifetime = 0.60,
				OneShot = false,
				Emitting = true,
				Direction = forward * 0.7f + Vector3.Right * 0.4f,
				Spread = 20.0f,
				InitialVelocityMin = 3.5f,
				InitialVelocityMax = 6.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.30f, 1.0f, 0.92f, 0.98f),
						new Color(0.92f, 0.25f, 0.98f, 0.85f),
						new Color(0.15f, 0.55f, 0.90f, 0.00f)
					},
					new[] { 0.0f, 0.50f, 1.0f }
				)
			};
			root.AddChild(pSparkles);
			pSparkles.GlobalPosition = chestPos + forward * 0.35f;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() => pSparkles.Emitting = false));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSparkles)) pSparkles.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Aura Phoenicia Secondary: Prism Reverberation (Geometric acoustic prism field + crystalline resonance)
	/// </summary>
	private static void SpawnPrismReverberation(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.85f;
		var sparkMat = CreateParticleMaterial(TextureType.Spark);

		var delay = root.CreateTween();
		delay.TweenInterval(0.04f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// Resonant Prism Ring
			var prismMat = CreateRingMaterial(new Color(0.25f, 0.98f, 0.90f, 0.90f));
			var prismQuad = new QuadMesh { Size = Vector2.One, Material = prismMat };
			var prismInst = new MeshInstance3D
			{
				Mesh = prismQuad,
				Rotation = new Vector3(0, Mathf.Atan2(forward.X, forward.Z), 0),
				Scale = Vector3.One * 0.45f
			};
			root.AddChild(prismInst);
			prismInst.GlobalPosition = chestPos;

			var tween = prismInst.CreateTween().SetParallel(true);
			tween.TweenProperty(prismInst, "scale", Vector3.One * 2.2f, 0.80f);
			tween.TweenProperty(prismMat, "albedo_color:a", 0.0f, 0.80f);
			tween.Chain().TweenCallback(Callable.From(() => prismInst.QueueFree()));

			// Resonant Acoustic Sparkles
			var pSparkles = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
				Amount = 40,
				Lifetime = 0.70,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 0.85f,
				Direction = Vector3.Up,
				Spread = 180.0f,
				InitialVelocityMin = 0.6f,
				InitialVelocityMax = 1.8f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.25f, 1.0f, 0.90f, 0.98f),
						new Color(0.95f, 0.30f, 0.90f, 0.80f),
						new Color(0.15f, 0.45f, 0.80f, 0.00f)
					},
					new[] { 0.0f, 0.50f, 1.0f }
				)
			};
			root.AddChild(pSparkles);
			pSparkles.GlobalPosition = chestPos;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(1.4f);
			stopTween.TweenCallback(Callable.From(() => pSparkles.Emitting = false));
			stopTween.TweenInterval(0.65f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSparkles)) pSparkles.QueueFree();
			}));
		}));
	}

	private static void SpawnWaveGlide(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.10f;
		var sparkMat = CreateParticleMaterial(TextureType.Spark);

		var pGlide = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = sparkMat },
			Amount = 28,
			Lifetime = 0.40,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 20.0f,
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.0f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.25f, 1.0f, 0.90f, 0.98f),
					new Color(0.90f, 0.25f, 0.95f, 0.75f),
					new Color(0.10f, 0.35f, 0.70f, 0.00f)
				},
				new[] { 0.0f, 0.45f, 1.0f }
			)
		};
		root.AddChild(pGlide);
		pGlide.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.65f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pGlide)) pGlide.QueueFree();
		}));
	}

	private static void SpawnCedarsEcho(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.06f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.20f + new Vector3(0, 0.03f, 0);
			SpawnFloorShockwaveRing(root, impactPos, new Color(0.25f, 0.98f, 0.90f, 0.90f), 0.4f, 2.0f, 0.65f);
		}));
	}

	#endregion
}
