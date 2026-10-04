using Godot;
using System;

/// <summary>
/// HeroAbilityVFX manages all high-fidelity elemental and cybernetic visual effects for operative abilities.
/// Supports both Character Selection Showcase preview and in-game combat execution.
/// Uses procedural GPU-compatible particle billboards with soft radial gradients and alpha composition
/// to ensure vibrant elemental rendering in both transparent SubViewports and full 3D gameplay worlds.
/// </summary>
public static class HeroAbilityVFX
{
	private const string VfxGroupName = "ActiveHeroAbilityVFX";

	private static GradientTexture2D? _softParticleTexture;

	/// <summary>
	/// Lazily creates and caches a procedural soft circular radial gradient texture.
	/// Guarantees smooth, organic particles without harsh polygon edges.
	/// </summary>
	private static GradientTexture2D GetSoftParticleTexture()
	{
		if (_softParticleTexture != null && GodotObject.IsInstanceValid(_softParticleTexture))
		{
			return _softParticleTexture;
		}

		var grad = new Gradient
		{
			Colors = new[]
			{
				new Color(1f, 1f, 1f, 1f),
				new Color(1f, 1f, 1f, 0.85f),
				new Color(1f, 1f, 1f, 0.3f),
				new Color(1f, 1f, 1f, 0f)
			},
			Offsets = new[] { 0.0f, 0.22f, 0.6f, 1.0f }
		};

		_softParticleTexture = new GradientTexture2D
		{
			Gradient = grad,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(0.5f, 0.0f),
			Width = 64,
			Height = 64
		};

		return _softParticleTexture;
	}

	/// <summary>
	/// Creates an unshaded alpha-blended particle material using the soft particle texture.
	/// BLEND_MODE_MIX guarantees proper alpha compositing in SubViewports with transparent_bg = true.
	/// </summary>
	private static StandardMaterial3D CreateParticleMaterial(BaseMaterial3D.BlendModeEnum blendMode = BaseMaterial3D.BlendModeEnum.Mix)
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = blendMode,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
			BillboardKeepScale = true,
			AlbedoTexture = GetSoftParticleTexture(),
			VertexColorUseAsAlbedo = true
		};
	}

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
	/// </summary>
	public static Node3D? TriggerAbilityVFX(Node3D parent, Node3D heroModel, string characterName, string animName, Vector3 forwardDir)
	{
		if (parent == null || !GodotObject.IsInstanceValid(parent)) return null;
		if (heroModel == null || !GodotObject.IsInstanceValid(heroModel)) return null;

		// Clean up existing active preview effects
		ClearActiveVFX(parent);

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
			case "Mirage Blade":
				DispatchMirageBladeVFX(vfxRoot, heroModel, animName, forward);
				break;
			case "Apex Titan":
				DispatchApexTitanVFX(vfxRoot, heroModel, animName, forward);
				break;
			case "Aura Phoenicia":
				DispatchPhoeniciaVFX(vfxRoot, heroModel, animName, forward);
				break;
			case "Cyber Shogun":
			default:
				DispatchCyberShogunVFX(vfxRoot, heroModel, animName, forward);
				break;
		}

		// Ensure automatic self-cleanup after 2.5s
		var cleanupTween = vfxRoot.CreateTween();
		cleanupTween.TweenInterval(2.5f);
		cleanupTween.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(vfxRoot))
			{
				vfxRoot.QueueFree();
			}
		}));

		return vfxRoot;
	}

	#region Trajectory & Positioning Helpers

	private static Vector3 GetShowcaseBlastDir(Vector3 forward)
	{
		Vector3 side = Vector3.Up.Cross(forward);
		if (side.LengthSquared() < 0.001f)
		{
			side = Vector3.Right;
		}
		else
		{
			side = side.Normalized();
		}

		// Angled 22 degrees across the showcase platform into open space for perfect 3/4 profile visibility
		Vector3 dir = forward * 0.92f + side * 0.38f + Vector3.Down * 0.04f;
		return dir.LengthSquared() > 0.001f ? dir.Normalized() : forward;
	}

	private static Vector3 GetHandsPosition(Node3D model, Vector3 forward)
	{
		var skeleton = FindChildByType<Skeleton3D>(model);
		if (skeleton != null)
		{
			int rHand = skeleton.FindBone("RightHand");
			int lHand = skeleton.FindBone("LeftHand");

			if (rHand != -1 && lHand != -1)
			{
				Vector3 rPos = (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(rHand)).Origin;
				Vector3 lPos = (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(lHand)).Origin;
				return (rPos + lPos) * 0.5f;
			}
			else if (rHand != -1)
			{
				return (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(rHand)).Origin;
			}
		}

		return model.GlobalPosition + Vector3.Up * 0.8f + forward * 0.35f;
	}

	private static Curve CreatePlumeCurve(float start = 0.35f, float peak = 1.25f, float end = 0.95f)
	{
		var curve = new Curve();
		curve.AddPoint(new Vector2(0.0f, start));
		curve.AddPoint(new Vector2(0.35f, 0.88f));
		curve.AddPoint(new Vector2(0.75f, peak));
		curve.AddPoint(new Vector2(1.0f, end));
		return curve;
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

	#endregion

	#region Oceanus (Hydro / Water Manipulator)

	private static void DispatchOceanusVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("torrent") || animLower.Contains("blast"))
		{
			SpawnWaterTorrent(root, model, forward);
		}
		else if (animLower.Contains("vortex") || animLower.Contains("cast"))
		{
			SpawnWaterVortex(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("jet"))
		{
			SpawnWaterDash(root, model, forward);
		}
		else if (animLower.Contains("slam") || animLower.Contains("crash"))
		{
			SpawnWaterSlam(root, model, forward);
		}
		else
		{
			SpawnWaterTorrent(root, model, forward);
		}
	}

	/// <summary>
	/// Oceanus Primary: Tidal Torrent (High-pressure hydro stream + cascading spray mist + splash droplets)
	/// </summary>
	private static void SpawnWaterTorrent(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 launchPos = GetHandsPosition(model, forward);
		Vector3 blastDir = GetShowcaseBlastDir(forward);
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.18f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			Vector3 emitPos = GetHandsPosition(model, forward);

			// 1. Core High-Pressure Liquid Stream
			var pCore = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.18f, 0.18f), Material = pmat },
				Amount = 70,
				Lifetime = 0.48,
				OneShot = false,
				Emitting = true,
				Direction = blastDir,
				Spread = 5.0f,
				Gravity = new Vector3(0, -2.2f, 0),
				InitialVelocityMin = 5.5f,
				InitialVelocityMax = 8.5f,
				ScaleAmountCurve = CreatePlumeCurve(0.4f, 1.25f, 0.9f),
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.5f, 0.95f, 1.0f, 0.95f),  // foamy aqua nozzle
						new Color(0.08f, 0.7f, 1.0f, 0.85f),  // vibrant turquoise stream
						new Color(0.02f, 0.45f, 0.9f, 0.65f), // deep ocean body
						new Color(0.01f, 0.2f, 0.65f, 0.0f)   // fade
					},
					new[] { 0.0f, 0.2f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pCore);
			pCore.GlobalPosition = emitPos;

			// 2. Surrounding Water Spray Mist
			var pMist = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.24f, 0.24f), Material = pmat },
				Amount = 45,
				Lifetime = 0.55,
				OneShot = false,
				Emitting = true,
				Direction = blastDir,
				Spread = 16.0f,
				Gravity = new Vector3(0, -3.2f, 0),
				InitialVelocityMin = 4.0f,
				InitialVelocityMax = 7.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.3f, 0.85f, 1.0f, 0.6f),
						new Color(0.05f, 0.55f, 0.95f, 0.4f),
						new Color(0.01f, 0.25f, 0.7f, 0.0f)
					},
					new[] { 0.0f, 0.4f, 1.0f }
				)
			};
			root.AddChild(pMist);
			pMist.GlobalPosition = emitPos;

			// 3. Energetic Splash Droplets
			var pDrops = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.07f, 0.07f), Material = pmat },
				Amount = 40,
				Lifetime = 0.5,
				OneShot = false,
				Emitting = true,
				Direction = blastDir,
				Spread = 24.0f,
				Gravity = new Vector3(0, -5.0f, 0),
				InitialVelocityMin = 5.0f,
				InitialVelocityMax = 9.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.7f, 0.98f, 1.0f, 0.95f),
						new Color(0.2f, 0.8f, 1.0f, 0.7f),
						new Color(0.05f, 0.4f, 0.85f, 0.0f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pDrops);
			pDrops.GlobalPosition = emitPos;

			// Stop emitting at 0.75s, then clean up
			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.75f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				pCore.Emitting = false;
				pMist.Emitting = false;
				pDrops.Emitting = false;
			}));
			stopTween.TweenInterval(0.55f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pCore)) pCore.QueueFree();
				if (GodotObject.IsInstanceValid(pMist)) pMist.QueueFree();
				if (GodotObject.IsInstanceValid(pDrops)) pDrops.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Oceanus Secondary: Maelstrom Vortex (Swirling aquatic cyclone + hydrodynamic ground ripple)
	/// </summary>
	private static void SpawnWaterVortex(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 groundPos = model.GlobalPosition + new Vector3(0, 0.04f, 0);
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.15f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// 1. Swirling Water Cyclone Particles
			var pVortex = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.12f, 0.12f), Material = pmat },
				Amount = 65,
				Lifetime = 0.7,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
				EmissionRingRadius = 0.85f,
				EmissionRingInnerRadius = 0.2f,
				Direction = Vector3.Up,
				Spread = 20.0f,
				InitialVelocityMin = 1.2f,
				InitialVelocityMax = 2.8f,
				TangentialAccelMin = 8.0f,
				TangentialAccelMax = 14.0f,
				RadialAccelMin = -2.5f,
				RadialAccelMax = -5.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.2f, 0.8f, 1.0f, 0.9f),
						new Color(0.05f, 0.6f, 0.95f, 0.75f),
						new Color(0.02f, 0.35f, 0.85f, 0.5f),
						new Color(0.0f, 0.15f, 0.6f, 0.0f)
					},
					new[] { 0.0f, 0.25f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pVortex);
			pVortex.GlobalPosition = groundPos;

			// 2. Expanding Hydrodynamic Ripple Ring on Ground
			var rippleMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.1f, 0.7f, 1.0f, 0.75f)
			};
			var ripple = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.65f, OuterRadius = 0.72f, Rings = 24, RingSegments = 8 },
				MaterialOverride = rippleMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(ripple);
			ripple.GlobalPosition = groundPos + new Vector3(0, 0.02f, 0);

			var ripTween = ripple.CreateTween().SetParallel(true);
			ripTween.TweenProperty(ripple, "scale", Vector3.One * 1.8f, 0.85f);
			ripTween.TweenProperty(rippleMat, "albedo_color:a", 0.0f, 0.85f);

			// Stop emitting after 0.8s
			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.85f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				pVortex.Emitting = false;
			}));
			stopTween.TweenInterval(0.7f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pVortex)) pVortex.QueueFree();
				if (GodotObject.IsInstanceValid(ripple)) ripple.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Oceanus Mobility: Aqua Jet (Hydro spray wake)
	/// </summary>
	private static void SpawnWaterDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 feetPos = model.GlobalPosition + new Vector3(0, 0.15f, 0);
		var pmat = CreateParticleMaterial();

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.14f, 0.14f), Material = pmat },
			Amount = 45,
			Lifetime = 0.5,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward + Vector3.Up * 0.25f,
			Spread = 22.0f,
			Gravity = new Vector3(0, -3.5f, 0),
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.5f,
			ScaleAmountCurve = CreatePlumeCurve(0.5f, 1.2f, 0.8f),
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.4f, 0.9f, 1.0f, 0.9f),
					new Color(0.08f, 0.65f, 0.95f, 0.75f),
					new Color(0.02f, 0.3f, 0.75f, 0.0f)
				},
				new[] { 0.0f, 0.4f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = feetPos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.8f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	/// <summary>
	/// Oceanus Slam: Tidal Crash (Ground tidal wave ripple + erupting 360 vertical geyser fountains)
	/// </summary>
	private static void SpawnWaterSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var slamDelay = root.CreateTween();
		slamDelay.TweenInterval(0.4f);
		slamDelay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			Vector3 impactPos = model.GlobalPosition + forward * 0.35f + new Vector3(0, 0.04f, 0);
			var pmat = CreateParticleMaterial();

			// 1. Erupting 360 Water Geysers
			var pGeyser = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.18f, 0.18f), Material = pmat },
				Amount = 70,
				Lifetime = 0.65,
				OneShot = true,
				Explosiveness = 0.92f,
				Emitting = true,
				Direction = Vector3.Up,
				Spread = 65.0f,
				Gravity = new Vector3(0, -5.5f, 0),
				InitialVelocityMin = 4.5f,
				InitialVelocityMax = 8.5f,
				ScaleAmountCurve = CreatePlumeCurve(0.6f, 1.35f, 0.9f),
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.6f, 0.95f, 1.0f, 0.95f),
						new Color(0.1f, 0.7f, 1.0f, 0.8f),
						new Color(0.02f, 0.4f, 0.9f, 0.5f),
						new Color(0.0f, 0.15f, 0.65f, 0.0f)
					},
					new[] { 0.0f, 0.25f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pGeyser);
			pGeyser.GlobalPosition = impactPos;

			// 2. Expanding Tidal Wave Ripple
			var waveMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.15f, 0.75f, 1.0f, 0.85f)
			};
			var wave = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.15f, OuterRadius = 0.28f, Rings = 24, RingSegments = 8 },
				MaterialOverride = waveMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(wave);
			wave.GlobalPosition = impactPos;

			var waveTween = wave.CreateTween().SetParallel(true);
			waveTween.TweenProperty(wave, "scale", Vector3.One * 4.5f, 0.48f);
			waveTween.TweenProperty(waveMat, "albedo_color:a", 0.0f, 0.48f);

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.85f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pGeyser)) pGeyser.QueueFree();
				if (GodotObject.IsInstanceValid(wave)) wave.QueueFree();
			}));
		}));
	}

	#endregion

	#region Surya Prime (Solar / Fire Champion)

	private static void DispatchSuryaPrimeVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("shield_bash") || animLower.Contains("bash") || animLower.Contains("rush"))
		{
			SpawnFireShieldBash(root, model, forward);
		}
		else if (animLower.Contains("guard") || animLower.Contains("shield") || animLower.Contains("barrier"))
		{
			SpawnFireShieldGuard(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("sprint"))
		{
			SpawnFireDash(root, model, forward);
		}
		else if (animLower.Contains("slam") || animLower.Contains("stomp") || animLower.Contains("cosmic"))
		{
			SpawnFireSlam(root, model, forward);
		}
		else
		{
			SpawnFireShieldBash(root, model, forward);
		}
	}

	/// <summary>
	/// Surya Prime Primary: Solar Shield Bash (Roaring flamethrower plume + flying solar embers)
	/// </summary>
	private static void SpawnFireShieldBash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 fireDir = GetShowcaseBlastDir(forward);
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.18f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			Vector3 launchPos = GetHandsPosition(model, forward);

			// 1. Main Billowing Flame Plume
			var pFire = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.26f, 0.26f), Material = pmat },
				Amount = 75,
				Lifetime = 0.52,
				OneShot = false,
				Emitting = true,
				Direction = fireDir,
				Spread = 12.0f,
				Gravity = new Vector3(0, 2.8f, 0), // Convection heat rises!
				InitialVelocityMin = 4.5f,
				InitialVelocityMax = 7.5f,
				ScaleAmountCurve = CreatePlumeCurve(0.4f, 1.25f, 0.95f),
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 0.98f, 0.65f, 0.95f),  // Incandescent white-yellow core
						new Color(1.0f, 0.55f, 0.02f, 0.9f),   // Blazing solar orange
						new Color(0.85f, 0.15f, 0.01f, 0.65f), // Crimson flame
						new Color(0.18f, 0.03f, 0.01f, 0.0f)   // Smoke dissipation
					},
					new[] { 0.0f, 0.22f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pFire);
			pFire.GlobalPosition = launchPos;

			// 2. White-Hot Core Jet
			var pCore = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.16f, 0.16f), Material = pmat },
				Amount = 45,
				Lifetime = 0.38,
				OneShot = false,
				Emitting = true,
				Direction = fireDir,
				Spread = 6.0f,
				Gravity = new Vector3(0, 1.2f, 0),
				InitialVelocityMin = 5.5f,
				InitialVelocityMax = 8.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 1.0f, 0.85f, 0.98f),
						new Color(1.0f, 0.72f, 0.12f, 0.8f),
						new Color(0.95f, 0.35f, 0.02f, 0.0f)
					},
					new[] { 0.0f, 0.4f, 1.0f }
				)
			};
			root.AddChild(pCore);
			pCore.GlobalPosition = launchPos;

			// 3. Flying Solar Embers & Sparks
			var pSparks = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.06f, 0.06f), Material = pmat },
				Amount = 40,
				Lifetime = 0.65,
				OneShot = false,
				Emitting = true,
				Direction = fireDir + Vector3.Up * 0.12f,
				Spread = 22.0f,
				Gravity = new Vector3(0, 1.8f, 0),
				InitialVelocityMin = 6.5f,
				InitialVelocityMax = 11.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 1.0f, 0.75f, 1.0f),
						new Color(1.0f, 0.58f, 0.08f, 0.85f),
						new Color(0.75f, 0.12f, 0.0f, 0.0f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pSparks);
			pSparks.GlobalPosition = launchPos;

			// Stop emitting at 0.75s, then clean up
			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.75f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				pFire.Emitting = false;
				pCore.Emitting = false;
				pSparks.Emitting = false;
			}));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pFire)) pFire.QueueFree();
				if (GodotObject.IsInstanceValid(pCore)) pCore.QueueFree();
				if (GodotObject.IsInstanceValid(pSparks)) pSparks.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Surya Prime Secondary: Aegis of the Sun (Orbiting solar corona embers + radiant camera-facing solar crest barrier)
	/// </summary>
	private static void SpawnFireShieldGuard(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.8f + forward * 0.4f;
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.12f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// 1. Orbiting Solar Corona Fire Motes
			var pAegis = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.12f, 0.12f), Material = pmat },
				Amount = 55,
				Lifetime = 0.65,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
				EmissionRingRadius = 0.65f,
				EmissionRingInnerRadius = 0.35f,
				Direction = Vector3.Up,
				Spread = 180.0f,
				InitialVelocityMin = 0.5f,
				InitialVelocityMax = 1.5f,
				TangentialAccelMin = 6.0f,
				TangentialAccelMax = 10.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 0.95f, 0.7f, 0.95f),
						new Color(1.0f, 0.6f, 0.05f, 0.8f),
						new Color(0.85f, 0.2f, 0.01f, 0.4f),
						new Color(0.2f, 0.03f, 0.0f, 0.0f)
					},
					new[] { 0.0f, 0.3f, 0.7f, 1.0f }
				)
			};
			root.AddChild(pAegis);
			pAegis.GlobalPosition = chestPos;

			// 2. Camera-Facing Radiant Solar Crest Barrier Disc
			var crestMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
				BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
				AlbedoTexture = GetSoftParticleTexture(),
				AlbedoColor = new Color(1.0f, 0.65f, 0.1f, 0.7f)
			};
			var crest = new MeshInstance3D
			{
				Mesh = new QuadMesh { Size = new Vector2(1.15f, 1.15f) },
				MaterialOverride = crestMat,
				Scale = Vector3.Zero
			};
			root.AddChild(crest);
			crest.GlobalPosition = chestPos;

			// Pop open crest and pulse
			var crestTween = crest.CreateTween().SetParallel(true);
			crestTween.TweenProperty(crest, "scale", Vector3.One, 0.2f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

			// Fade out at 0.75s
			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.75f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				pAegis.Emitting = false;
				var fade = crest.CreateTween().SetParallel(true);
				fade.TweenProperty(crestMat, "albedo_color:a", 0.0f, 0.3f);
				fade.TweenProperty(crest, "scale", Vector3.Zero, 0.3f);
			}));
			stopTween.TweenInterval(0.65f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pAegis)) pAegis.QueueFree();
				if (GodotObject.IsInstanceValid(crest)) crest.QueueFree();
			}));
		}));
	}

	/// <summary>
	/// Surya Prime Mobility: Vanguard Dash (Rocket exhaust fire plumes + spark trails)
	/// </summary>
	private static void SpawnFireDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 basePos = model.GlobalPosition + Vector3.Up * 0.65f;
		var pmat = CreateParticleMaterial();

		var pThruster = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.14f, 0.14f), Material = pmat },
			Amount = 45,
			Lifetime = 0.48,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward + Vector3.Up * 0.15f,
			Spread = 16.0f,
			Gravity = new Vector3(0, 1.8f, 0),
			InitialVelocityMin = 4.0f,
			InitialVelocityMax = 7.5f,
			ScaleAmountCurve = CreatePlumeCurve(0.5f, 1.2f, 0.8f),
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(1.0f, 0.95f, 0.6f, 0.95f),
					new Color(1.0f, 0.5f, 0.05f, 0.85f),
					new Color(0.8f, 0.12f, 0.01f, 0.0f)
				},
				new[] { 0.0f, 0.35f, 1.0f }
			)
		};
		root.AddChild(pThruster);
		pThruster.GlobalPosition = basePos - forward * 0.25f;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.75f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pThruster)) pThruster.QueueFree();
		}));
	}

	/// <summary>
	/// Surya Prime Slam: Cosmic Slam (Solar nova ground explosion + erupting vertical flame pillar)
	/// </summary>
	private static void SpawnFireSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var slamDelay = root.CreateTween();
		slamDelay.TweenInterval(0.4f);
		slamDelay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			Vector3 impactPos = model.GlobalPosition + forward * 0.35f + new Vector3(0, 0.04f, 0);
			var pmat = CreateParticleMaterial();

			// 1. Erupting Vertical Solar Flame & Embers Pillar
			var pFlame = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.2f, 0.2f), Material = pmat },
				Amount = 70,
				Lifetime = 0.65,
				OneShot = true,
				Explosiveness = 0.92f,
				Emitting = true,
				Direction = Vector3.Up,
				Spread = 55.0f,
				Gravity = new Vector3(0, 3.0f, 0), // Convection updraft
				InitialVelocityMin = 4.5f,
				InitialVelocityMax = 8.5f,
				ScaleAmountCurve = CreatePlumeCurve(0.6f, 1.35f, 0.9f),
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 0.98f, 0.7f, 0.95f),
						new Color(1.0f, 0.6f, 0.08f, 0.85f),
						new Color(0.85f, 0.18f, 0.02f, 0.6f),
						new Color(0.2f, 0.03f, 0.01f, 0.0f)
					},
					new[] { 0.0f, 0.22f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pFlame);
			pFlame.GlobalPosition = impactPos;

			// 2. Solar Nova Ground Shockwave
			var novaMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(1.0f, 0.55f, 0.05f, 0.85f)
			};
			var novaRing = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.16f, OuterRadius = 0.3f, Rings = 24, RingSegments = 8 },
				MaterialOverride = novaMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(novaRing);
			novaRing.GlobalPosition = impactPos;

			var novaTween = novaRing.CreateTween().SetParallel(true);
			novaTween.TweenProperty(novaRing, "scale", Vector3.One * 4.5f, 0.46f);
			novaTween.TweenProperty(novaMat, "albedo_color:a", 0.0f, 0.46f);

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.85f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pFlame)) pFlame.QueueFree();
				if (GodotObject.IsInstanceValid(novaRing)) novaRing.QueueFree();
			}));
		}));
	}

	#endregion

	#region Mirage Blade (Phase / Holographic)

	private static void DispatchMirageBladeVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("phase") || animLower.Contains("blink") || animLower.Contains("slash"))
		{
			SpawnPhaseSlash(root, model, forward);
		}
		else if (animLower.Contains("decoy") || animLower.Contains("deploy"))
		{
			SpawnDecoyDeploy(root, model, forward);
		}
		else if (animLower.Contains("dash"))
		{
			SpawnPhaseDash(root, model, forward);
		}
		else
		{
			SpawnPhaseSlam(root, model, forward);
		}
	}

	private static void SpawnPhaseSlash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.85f;
		Vector3 blastDir = GetShowcaseBlastDir(forward);
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.2f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// Digital phase slice particles
			var pSlash = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.12f, 0.12f), Material = pmat },
				Amount = 45,
				Lifetime = 0.45,
				OneShot = true,
				Explosiveness = 0.85f,
				Emitting = true,
				Direction = blastDir,
				Spread = 16.0f,
				InitialVelocityMin = 5.0f,
				InitialVelocityMax = 8.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.2f, 1.0f, 0.85f, 0.95f), // Holographic cyan
						new Color(0.85f, 0.15f, 0.85f, 0.75f), // Quantum magenta
						new Color(0.1f, 0.8f, 0.7f, 0.0f)
					},
					new[] { 0.0f, 0.5f, 1.0f }
				)
			};
			root.AddChild(pSlash);
			pSlash.GlobalPosition = chestPos + blastDir * 0.4f;

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.7f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSlash)) pSlash.QueueFree();
			}));
		}));
	}

	private static void SpawnDecoyDeploy(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 decoyPos = model.GlobalPosition + forward * 0.8f + Vector3.Up * 0.8f;
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.18f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			var pDecoy = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.1f, 0.1f), Material = pmat },
				Amount = 50,
				Lifetime = 0.65,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 0.45f,
				Direction = Vector3.Up,
				Spread = 20.0f,
				InitialVelocityMin = 1.0f,
				InitialVelocityMax = 2.5f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.0f, 0.95f, 0.8f, 0.85f),
						new Color(0.75f, 0.15f, 0.85f, 0.65f),
						new Color(0.0f, 0.5f, 0.6f, 0.0f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pDecoy);
			pDecoy.GlobalPosition = decoyPos;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.75f);
			stopTween.TweenCallback(Callable.From(() => pDecoy.Emitting = false));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pDecoy)) pDecoy.QueueFree();
			}));
		}));
	}

	private static void SpawnPhaseDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.7f;
		var pmat = CreateParticleMaterial();

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.12f, 0.12f), Material = pmat },
			Amount = 40,
			Lifetime = 0.45,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 18.0f,
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.5f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.2f, 1.0f, 0.85f, 0.9f),
					new Color(0.8f, 0.2f, 0.9f, 0.65f),
					new Color(0.05f, 0.5f, 0.6f, 0.0f)
				},
				new[] { 0.0f, 0.45f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.7f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	private static void SpawnPhaseSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.4f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.35f + new Vector3(0, 0.04f, 0);

			var prismMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.1f, 0.95f, 0.8f, 0.85f)
			};
			var prismRing = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.15f, OuterRadius = 0.28f, Rings = 6, RingSegments = 6 }, // Hexagonal prism
				MaterialOverride = prismMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(prismRing);
			prismRing.GlobalPosition = impactPos;

			var tween = prismRing.CreateTween().SetParallel(true);
			tween.TweenProperty(prismRing, "scale", Vector3.One * 4.2f, 0.44f);
			tween.TweenProperty(prismMat, "albedo_color:a", 0.0f, 0.44f);
			tween.Chain().TweenCallback(Callable.From(() => prismRing.QueueFree()));
		}));
	}

	#endregion

	#region Apex Titan (Earth / Seismic / Magnetic)

	private static void DispatchApexTitanVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("tectonic") || animLower.Contains("crater"))
		{
			SpawnTectonicSlam(root, model, forward);
		}
		else if (animLower.Contains("magnetic") || animLower.Contains("pull"))
		{
			SpawnMagneticPull(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("surge"))
		{
			SpawnTitanDash(root, model, forward);
		}
		else
		{
			SpawnTectonicSlam(root, model, forward);
		}
	}

	private static void SpawnTectonicSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.38f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.45f + new Vector3(0, 0.04f, 0);
			var pmat = CreateParticleMaterial();

			// 1. Erupting Earth & Amber Seismic Debris
			var pDust = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.2f, 0.2f), Material = pmat },
				Amount = 55,
				Lifetime = 0.65,
				OneShot = true,
				Explosiveness = 0.92f,
				Emitting = true,
				Direction = Vector3.Up + forward * 0.2f,
				Spread = 45.0f,
				Gravity = new Vector3(0, -6.5f, 0),
				InitialVelocityMin = 4.0f,
				InitialVelocityMax = 8.0f,
				ScaleAmountCurve = CreatePlumeCurve(0.5f, 1.3f, 0.85f),
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(1.0f, 0.75f, 0.25f, 0.9f),
						new Color(0.85f, 0.45f, 0.08f, 0.75f),
						new Color(0.5f, 0.25f, 0.05f, 0.4f),
						new Color(0.2f, 0.1f, 0.02f, 0.0f)
					},
					new[] { 0.0f, 0.25f, 0.65f, 1.0f }
				)
			};
			root.AddChild(pDust);
			pDust.GlobalPosition = impactPos;

			// 2. Heavy Seismic Shockwave Ring
			var shockMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.95f, 0.5f, 0.08f, 0.85f)
			};
			var shock = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.18f, OuterRadius = 0.35f, Rings = 24, RingSegments = 8 },
				MaterialOverride = shockMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(shock);
			shock.GlobalPosition = impactPos;

			var tween = shock.CreateTween().SetParallel(true);
			tween.TweenProperty(shock, "scale", Vector3.One * 4.5f, 0.44f);
			tween.TweenProperty(shockMat, "albedo_color:a", 0.0f, 0.44f);

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.85f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pDust)) pDust.QueueFree();
				if (GodotObject.IsInstanceValid(shock)) shock.QueueFree();
			}));
		}));
	}

	private static void SpawnMagneticPull(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 fistPos = model.GlobalPosition + Vector3.Up * 0.85f + forward * 0.4f;
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.18f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			var pMag = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.1f, 0.1f), Material = pmat },
				Amount = 50,
				Lifetime = 0.6,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 0.85f,
				Direction = -forward,
				Spread = 180.0f,
				RadialAccelMin = -5.0f, // Pulls INWARD to fist
				RadialAccelMax = -9.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.6f, 0.2f, 1.0f, 0.9f),
						new Color(0.85f, 0.3f, 0.95f, 0.7f),
						new Color(0.3f, 0.05f, 0.6f, 0.0f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pMag);
			pMag.GlobalPosition = fistPos;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.75f);
			stopTween.TweenCallback(Callable.From(() => pMag.Emitting = false));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pMag)) pMag.QueueFree();
			}));
		}));
	}

	private static void SpawnTitanDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.35f;
		var pmat = CreateParticleMaterial();

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.16f, 0.16f), Material = pmat },
			Amount = 40,
			Lifetime = 0.5,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 24.0f,
			Gravity = new Vector3(0, -2.5f, 0),
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 7.0f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.9f, 0.6f, 0.15f, 0.85f),
					new Color(0.65f, 0.35f, 0.08f, 0.6f),
					new Color(0.3f, 0.15f, 0.02f, 0.0f)
				},
				new[] { 0.0f, 0.45f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.7f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	#endregion

	#region Aura Phoenicia (Harmonic Acoustic Operative)

	private static void DispatchPhoeniciaVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("harmonic") || animLower.Contains("pulse"))
		{
			SpawnHarmonicPulse(root, model, forward);
		}
		else if (animLower.Contains("prism") || animLower.Contains("reverberation"))
		{
			SpawnPrismReverberation(root, model, forward);
		}
		else if (animLower.Contains("dash") || animLower.Contains("glide"))
		{
			SpawnHarmonicDash(root, model, forward);
		}
		else
		{
			SpawnHarmonicSlam(root, model, forward);
		}
	}

	/// <summary>
	/// Aura Phoenicia Primary: Harmonic Pulse (Concentric Tyrian purple & gold acoustic wave rings + sonic sparkles)
	/// </summary>
	private static void SpawnHarmonicPulse(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 gauntletPos = GetHandsPosition(model, forward);
		Vector3 blastDir = GetShowcaseBlastDir(forward);
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.18f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// Concentric acoustic rings propagating forward
			for (int i = 0; i < 3; i++)
			{
				int ringIdx = i;
				var rDelay = root.CreateTween();
				rDelay.TweenInterval(ringIdx * 0.1f);
				rDelay.TweenCallback(Callable.From(() =>
				{
					if (!GodotObject.IsInstanceValid(root)) return;

					Color ringCol = ringIdx % 2 == 0 ? new Color(0.6f, 0.1f, 0.85f, 0.85f) : new Color(0.95f, 0.78f, 0.15f, 0.85f);
					var ringMat = new StandardMaterial3D
					{
						ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
						Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
						BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
						AlbedoColor = ringCol
					};

					var acousticRing = new MeshInstance3D
					{
						Mesh = new TorusMesh { InnerRadius = 0.12f, OuterRadius = 0.22f, Rings = 24, RingSegments = 8 },
						MaterialOverride = ringMat,
						Scale = Vector3.One * 0.35f
					};
					root.AddChild(acousticRing);
					acousticRing.Transform = CreateFlatRingTransform(gauntletPos, blastDir);

					var ringTween = acousticRing.CreateTween().SetParallel(true);
					ringTween.TweenProperty(acousticRing, "position", acousticRing.Position + blastDir * 1.8f, 0.42f);
					ringTween.TweenProperty(acousticRing, "scale", Vector3.One * 2.8f, 0.42f);
					ringTween.TweenProperty(ringMat, "albedo_color:a", 0.0f, 0.42f);
					ringTween.Chain().TweenCallback(Callable.From(() => acousticRing.QueueFree()));
				}));
			}

			// Sonic acoustic sparkle burst
			var pSparkles = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = pmat },
				Amount = 45,
				Lifetime = 0.52,
				OneShot = true,
				Explosiveness = 0.75f,
				Emitting = true,
				Direction = blastDir,
				Spread = 16.0f,
				InitialVelocityMin = 5.0f,
				InitialVelocityMax = 9.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.7f, 0.2f, 0.95f, 0.95f),
						new Color(1.0f, 0.82f, 0.2f, 0.85f),
						new Color(0.4f, 0.05f, 0.6f, 0.0f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pSparkles);
			pSparkles.GlobalPosition = gauntletPos;

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.85f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSparkles)) pSparkles.QueueFree();
			}));
		}));
	}

	private static void SpawnPrismReverberation(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.8f;
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.15f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			var pField = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.1f, 0.1f), Material = pmat },
				Amount = 50,
				Lifetime = 0.7,
				OneShot = false,
				Emitting = true,
				EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
				EmissionRingRadius = 0.75f,
				EmissionRingInnerRadius = 0.4f,
				Direction = Vector3.Up,
				Spread = 180.0f,
				TangentialAccelMin = 6.0f,
				TangentialAccelMax = 10.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.65f, 0.15f, 0.9f, 0.9f),
						new Color(0.95f, 0.78f, 0.2f, 0.8f),
						new Color(0.3f, 0.05f, 0.5f, 0.0f)
					},
					new[] { 0.0f, 0.5f, 1.0f }
				)
			};
			root.AddChild(pField);
			pField.GlobalPosition = chestPos;

			var stopTween = root.CreateTween();
			stopTween.TweenInterval(0.75f);
			stopTween.TweenCallback(Callable.From(() => pField.Emitting = false));
			stopTween.TweenInterval(0.6f);
			stopTween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pField)) pField.QueueFree();
			}));
		}));
	}

	private static void SpawnHarmonicDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.6f;
		var pmat = CreateParticleMaterial();

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.1f, 0.1f), Material = pmat },
			Amount = 35,
			Lifetime = 0.45,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 18.0f,
			InitialVelocityMin = 3.5f,
			InitialVelocityMax = 6.5f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.75f, 0.2f, 0.95f, 0.9f),
					new Color(0.95f, 0.8f, 0.25f, 0.75f),
					new Color(0.3f, 0.05f, 0.5f, 0.0f)
				},
				new[] { 0.0f, 0.45f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.7f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	private static void SpawnHarmonicSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.4f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.35f + new Vector3(0, 0.04f, 0);

			var waveMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.65f, 0.12f, 0.9f, 0.85f)
			};
			var wave = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.18f, OuterRadius = 0.32f, Rings = 24, RingSegments = 8 },
				MaterialOverride = waveMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(wave);
			wave.GlobalPosition = impactPos;

			var tween = wave.CreateTween().SetParallel(true);
			tween.TweenProperty(wave, "scale", Vector3.One * 4.5f, 0.45f);
			tween.TweenProperty(waveMat, "albedo_color:a", 0.0f, 0.45f);
			tween.Chain().TweenCallback(Callable.From(() => wave.QueueFree()));
		}));
	}

	#endregion

	#region Cyber Shogun (Neon Kinetic Cyber Blade)

	private static void DispatchCyberShogunVFX(Node3D root, Node3D model, string animName, Vector3 forward)
	{
		string animLower = animName.ToLowerInvariant();
		if (animLower.Contains("blade") || animLower.Contains("flash"))
		{
			SpawnShogunFlash(root, model, forward);
		}
		else if (animLower.Contains("parry") || animLower.Contains("stance"))
		{
			SpawnKineticParry(root, model, forward);
		}
		else if (animLower.Contains("dash"))
		{
			SpawnCyberDash(root, model, forward);
		}
		else
		{
			SpawnCyberSlam(root, model, forward);
		}
	}

	private static void SpawnShogunFlash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.85f;
		Vector3 blastDir = GetShowcaseBlastDir(forward);
		var pmat = CreateParticleMaterial();

		var delay = root.CreateTween();
		delay.TweenInterval(0.2f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			// High-voltage electric blade spark shower
			var pSparks = new CpuParticles3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.08f, 0.08f), Material = pmat },
				Amount = 45,
				Lifetime = 0.42,
				OneShot = true,
				Explosiveness = 0.9f,
				Emitting = true,
				Direction = blastDir,
				Spread = 20.0f,
				InitialVelocityMin = 6.0f,
				InitialVelocityMax = 11.0f,
				ColorRamp = CreateGradient(
					new[]
					{
						new Color(0.2f, 0.95f, 1.0f, 0.95f),
						new Color(0.95f, 0.2f, 0.75f, 0.85f),
						new Color(0.05f, 0.5f, 0.85f, 0.0f)
					},
					new[] { 0.0f, 0.45f, 1.0f }
				)
			};
			root.AddChild(pSparks);
			pSparks.GlobalPosition = chestPos + blastDir * 0.45f;

			var cleanup = root.CreateTween();
			cleanup.TweenInterval(0.7f);
			cleanup.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(pSparks)) pSparks.QueueFree();
			}));
		}));
	}

	private static void SpawnKineticParry(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 chestPos = model.GlobalPosition + Vector3.Up * 0.8f + forward * 0.4f;

		var delay = root.CreateTween();
		delay.TweenInterval(0.12f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;

			var hexMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.0f, 0.92f, 1.0f, 0.85f)
			};
			var hexShield = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.5f, OuterRadius = 0.65f, Rings = 6, RingSegments = 6 }, // Hexagonal parry field
				MaterialOverride = hexMat,
				Rotation = new Vector3(0, Mathf.Atan2(forward.X, forward.Z), 0),
				Scale = Vector3.Zero
			};
			root.AddChild(hexShield);
			hexShield.GlobalPosition = chestPos;

			var tween = hexShield.CreateTween().SetParallel(true);
			tween.TweenProperty(hexShield, "scale", Vector3.One, 0.18f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			tween.Chain().TweenInterval(0.28f);
			tween.Chain().TweenProperty(hexMat, "albedo_color:a", 0.0f, 0.22f);
			tween.Chain().TweenCallback(Callable.From(() => hexShield.QueueFree()));
		}));
	}

	private static void SpawnCyberDash(Node3D root, Node3D model, Vector3 forward)
	{
		Vector3 pos = model.GlobalPosition + Vector3.Up * 0.6f;
		var pmat = CreateParticleMaterial();

		var pDash = new CpuParticles3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.1f, 0.1f), Material = pmat },
			Amount = 35,
			Lifetime = 0.45,
			OneShot = true,
			Explosiveness = 0.85f,
			Emitting = true,
			Direction = -forward,
			Spread = 18.0f,
			InitialVelocityMin = 4.0f,
			InitialVelocityMax = 7.5f,
			ColorRamp = CreateGradient(
				new[]
				{
					new Color(0.1f, 0.95f, 1.0f, 0.95f),
					new Color(0.85f, 0.15f, 0.8f, 0.7f),
					new Color(0.05f, 0.4f, 0.7f, 0.0f)
				},
				new[] { 0.0f, 0.45f, 1.0f }
			)
		};
		root.AddChild(pDash);
		pDash.GlobalPosition = pos;

		var cleanup = root.CreateTween();
		cleanup.TweenInterval(0.7f);
		cleanup.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(pDash)) pDash.QueueFree();
		}));
	}

	private static void SpawnCyberSlam(Node3D root, Node3D model, Vector3 forward)
	{
		var delay = root.CreateTween();
		delay.TweenInterval(0.4f);
		delay.TweenCallback(Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root)) return;
			Vector3 impactPos = model.GlobalPosition + forward * 0.35f + new Vector3(0, 0.04f, 0);

			var waveMat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
				AlbedoColor = new Color(0.05f, 0.92f, 1.0f, 0.9f)
			};
			var ring = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.15f, OuterRadius = 0.3f, Rings = 24, RingSegments = 8 },
				MaterialOverride = waveMat,
				Scale = Vector3.One * 0.4f
			};
			root.AddChild(ring);
			ring.GlobalPosition = impactPos;

			var tween = ring.CreateTween().SetParallel(true);
			tween.TweenProperty(ring, "scale", Vector3.One * 4.5f, 0.42f);
			tween.TweenProperty(waveMat, "albedo_color:a", 0.0f, 0.42f);
			tween.Chain().TweenCallback(Callable.From(() => ring.QueueFree()));
		}));
	}

	#endregion

	#region Transform Helpers

	private static Transform3D CreateFlatRingTransform(Vector3 centerPos, Vector3 forward)
	{
		Vector3 up = Vector3.Up;
		if (Mathf.Abs(forward.Dot(up)) > 0.95f)
		{
			up = Vector3.Right;
		}

		Vector3 side = forward.Cross(up);
		if (side.LengthSquared() < 0.001f) side = Vector3.Right;
		else side = side.Normalized();

		Vector3 localUp = side.Cross(forward);
		if (localUp.LengthSquared() < 0.001f) localUp = Vector3.Up;
		else localUp = localUp.Normalized();

		// TorusMesh normal is Y. Align Y with forward so ring faces the travel direction.
		Basis basis = new Basis(side, forward, localUp);
		return new Transform3D(basis, centerPos);
	}

	#endregion
}
