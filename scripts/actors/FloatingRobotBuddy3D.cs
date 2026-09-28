using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Controls the 3D AXIOM-7 Floating Robot Buddy.
/// Features:
/// - Cinematic High-Altitude Fly-In & Retro-Braking Settle Sequence.
/// - Procedural Thruster Whoosh Audio Synthesis.
/// - Active Dual-Core Plasma Thruster Plume and Cyan Spark Particles.
/// - Smooth vertical sine-wave hover & Z-tilt sway.
/// - Dynamic emissive visor pulsing on a timer (modulating emission energy multiplier).
/// </summary>
public partial class FloatingRobotBuddy3D : Node3D
{
    [Signal] public delegate void FlyInCompletedEventHandler();

    [Export] public bool AutoFlyIn { get; set; } = true;
    [Export] public float FlyInDuration { get; set; } = 1.45f;
    [Export] public Vector3 BehindStartOffset { get; set; } = new Vector3(-1.25f, 4.2f, -4.5f);
    [Export] public Vector3 DiveControlOffset { get; set; } = new Vector3(-1.20f, -0.40f, -2.2f);
    [Export] public Vector3 RiseControlOffset { get; set; } = new Vector3(-0.35f, -0.55f, -0.5f);

    [Export] public float HoverSpeed { get; set; } = 2.5f;
    [Export] public float HoverHeight { get; set; } = 0.12f;
    [Export] public float TiltAngleDegrees { get; set; } = 4f;

    [Export] public float PulseSpeed { get; set; } = 3.0f;
    [Export] public float MinEmissionEnergy { get; set; } = 1.2f;
    [Export] public float MaxEmissionEnergy { get; set; } = 4.5f;

    private float _time = 0f;
    private Vector3 _initialPosition;
    private Vector3 _initialRotation;
    private bool _isFlyingIn = false;
    private Tween? _flyInTween;
    private Node3D? _thrusterPlume;
    private AudioStreamPlayer? _audioPlayer;
    private List<StandardMaterial3D> _emissiveMaterials = new();

    // Millennium Falcon Style Sublight Ion Engine
    private Node3D? _sublightEngineNode;
    private OmniLight3D? _engineBacklight;

    // Modular Articulated Components
    private Node3D? _leftHand;
    private Node3D? _rightHand;
    private Node3D? _eyesMesh;
    private Node3D? _antennaTip;
    private Node3D? _chestCore;
    private Vector3 _leftHandInitPos;
    private Vector3 _leftHandInitRot;
    private Vector3 _rightHandInitPos;
    private Vector3 _rightHandInitRot;
    private Vector3 _eyesInitScale = Vector3.One;
    private StandardMaterial3D? _antennaTipMat;
    private bool _isTransmitting = false;
    private float _blinkTimer = 0f;
    private float _nextBlinkTime = 3.2f;
    private Tween? _blinkTween;

    public override void _Ready()
    {
        _initialPosition = Position;
        _initialRotation = Rotation;

        SetupRobotMeshAndMaterials();
        SetupSublightEngineGlow();
        SetupAudio();

        if (AutoFlyIn)
        {
            FlyIn(FlyInDuration);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_flyInTween != null && _flyInTween.IsValid())
        {
            _flyInTween.Kill();
            _flyInTween = null;
        }
        if (_blinkTween != null && _blinkTween.IsValid())
        {
            _blinkTween.Kill();
            _blinkTween = null;
        }
    }

    /// <summary>
    /// Executes the swooping 3D Bezier curve fly-in for AXIOM-7:
    /// Starts far in the background ('Behind', top-left), dives down through the viewable screen,
    /// and swoops upward & forward into the foreground ('Front') settling at its hover station.
    /// </summary>
    public void FlyIn(float duration = 1.45f)
    {
        if (_flyInTween != null && _flyInTween.IsValid())
        {
            _flyInTween.Kill();
        }

        _isFlyingIn = true;
        _time = 0f;

        Vector3 p0 = _initialPosition + BehindStartOffset;
        Vector3 p1 = _initialPosition + DiveControlOffset;
        Vector3 p2 = _initialPosition + RiseControlOffset;
        Vector3 p3 = _initialPosition;

        Position = p0;

        // Flare sublight engine to flight burn scale
        if (_thrusterPlume != null)
        {
            _thrusterPlume.Scale = new Vector3(1.0f, 1.85f, 1.0f);
        }
        if (_engineBacklight != null)
        {
            _engineBacklight.LightEnergy = 4.2f;
        }

        // Play thruster swoop audio
        if (_audioPlayer != null)
        {
            _audioPlayer.Play();
        }

        _flyInTween = CreateTween();
        _flyInTween.TweenMethod(
            Callable.From<float>((t) => UpdateFlyInFlight(t, p0, p1, p2, p3)),
            0f,
            1f,
            duration
        )
        .SetTrans(Tween.TransitionType.Cubic)
        .SetEase(Tween.EaseType.InOut);

        _flyInTween.Chain().TweenCallback(Callable.From(OnFlyInArrived));
    }

    private void UpdateFlyInFlight(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // 1. Evaluate 3D position along Bezier curve
        Position = EvaluateCubicBezier(p0, p1, p2, p3, t);

        // 2. Compute flight velocity/tangent for dynamic banking and pitch
        Vector3 nextP = EvaluateCubicBezier(p0, p1, p2, p3, Mathf.Min(t + 0.02f, 1f));
        Vector3 tangent = nextP - Position;

        if (tangent.LengthSquared() > 0.0001f)
        {
            Vector3 dir = tangent.Normalized();

            // Pitch: nose down when diving, nose up when climbing
            float pitch = -Mathf.Asin(Mathf.Clamp(dir.Y, -0.95f, 0.95f));

            // Yaw: heading towards direction of travel
            float yaw = Mathf.Atan2(dir.X, dir.Z);

            // Roll / Banking: lean into the turn as it curves across the screen
            float bank = -Mathf.Sin(t * Mathf.Pi) * Mathf.DegToRad(32f);

            Vector3 flightRot = new Vector3(pitch, yaw, bank);

            // Smoothly blend into the resting stance as it approaches the front
            float blendToRest = Mathf.SmoothStep(0.6f, 1.0f, t);
            Rotation = flightRot.Lerp(_initialRotation, blendToRest);
        }

        // 3. Thruster plume scaling: strong flame during swoop, eases down upon arrival
        if (_thrusterPlume != null)
        {
            float plumeT = Mathf.Clamp(1.0f - Mathf.Pow(t, 2f), 0.35f, 1.0f);
            float plumeX = Mathf.Lerp(0.45f, 0.85f, plumeT);
            float plumeY = Mathf.Lerp(0.55f, 1.8f, plumeT);
            _thrusterPlume.Scale = new Vector3(plumeX, plumeY, plumeX);
        }
    }

    private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
    }

    private void OnFlyInArrived()
    {
        // Visor flare on connection
        foreach (var mat in _emissiveMaterials)
        {
            mat.EmissionEnergyMultiplier = MaxEmissionEnergy * 1.4f;
        }

        // Subtle retro-braking hover bounce (small dip and settle)
        var bounceTween = CreateTween();
        Vector3 dipPos = new Vector3(_initialPosition.X, _initialPosition.Y - 0.05f, _initialPosition.Z);
        bounceTween.TweenProperty(this, "position", dipPos, 0.09f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        bounceTween.TweenProperty(this, "position", _initialPosition, 0.16f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        bounceTween.Chain().TweenCallback(Callable.From(() =>
        {
            _isFlyingIn = false;
            _time = 0f;
            EmitSignal(SignalName.FlyInCompleted);
        }));
    }

    /// <summary>
    /// Immediately completes the fly-in and settles AXIOM-7 at its target hover position.
    /// </summary>
    public void SkipFlyIn()
    {
        if (!_isFlyingIn) return;

        if (_flyInTween != null && _flyInTween.IsValid())
        {
            _flyInTween.Kill();
            _flyInTween = null;
        }

        Position = _initialPosition;
        Rotation = _initialRotation;

        if (_thrusterPlume != null)
        {
            _thrusterPlume.Scale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        if (_engineBacklight != null)
        {
            _engineBacklight.LightEnergy = 2.5f;
        }

        _isFlyingIn = false;
        _time = 0f;
        EmitSignal(SignalName.FlyInCompleted);
    }

    private void SetupRobotMeshAndMaterials()
    {
        string enhancedGlbPath = "res://assets/models/hq/axiom_7_enhanced.glb";
        string fallbackGlbPath = "res://assets/models/hq/Meshy_AI__0807164603_texture.glb";
        string glbPath = FileAccess.FileExists(enhancedGlbPath) ? enhancedGlbPath : fallbackGlbPath;

        if (!FileAccess.FileExists(glbPath))
        {
            GD.PrintErr($"FloatingRobotBuddy3D: No model found at '{glbPath}'");
            return;
        }

        var scene = GD.Load<PackedScene>(glbPath);
        if (scene == null)
        {
            GD.PrintErr($"FloatingRobotBuddy3D: Failed to load scene from '{glbPath}'");
            return;
        }

        var modelInstance = scene.Instantiate();
        AddChild(modelInstance);

        // Cache modular sub-nodes for animation
        _leftHand = FindChildStartingWith(modelInstance, "Axiom_Hand_L") as Node3D;
        _rightHand = FindChildStartingWith(modelInstance, "Axiom_Hand_R") as Node3D;
        _eyesMesh = FindChildStartingWith(modelInstance, "Axiom_Eyes") as Node3D;
        _antennaTip = FindChildStartingWith(modelInstance, "Axiom_AntennaTip") as Node3D;
        _chestCore = FindChildStartingWith(modelInstance, "Axiom_ChestCore") as Node3D;

        if (_leftHand != null)
        {
            _leftHandInitPos = _leftHand.Position;
            _leftHandInitRot = _leftHand.Rotation;
        }
        if (_rightHand != null)
        {
            _rightHandInitPos = _rightHand.Position;
            _rightHandInitRot = _rightHand.Rotation;
        }
        if (_eyesMesh != null)
        {
            _eyesInitScale = _eyesMesh.Scale;
        }

        // Configure emissive materials
        Texture2D? emissiveTex = null;
        string emissivePath = "res://assets/models/hq/Meshy_AI__0807164603_texture_emissive.jpg";
        if (FileAccess.FileExists(emissivePath))
        {
            emissiveTex = GD.Load<Texture2D>(emissivePath);
        }

        FindAndConfigureMaterials(modelInstance, emissiveTex);
        GD.Print($"FloatingRobotBuddy3D: Successfully loaded '{glbPath}' with {_emissiveMaterials.Count} dynamic emissive material(s)");
    }

    private void FindAndConfigureMaterials(Node node, Texture2D? emissiveTex)
    {
        if (node is MeshInstance3D meshInst)
        {
            int surfaceCount = meshInst.Mesh != null ? meshInst.Mesh.GetSurfaceCount() : 0;
            for (int i = 0; i < surfaceCount; i++)
            {
                Material origMat = meshInst.GetActiveMaterial(i);
                if (origMat is StandardMaterial3D stdMat)
                {
                    string matName = stdMat.ResourceName;
                    bool isEmissiveSlot = stdMat.EmissionEnabled 
                        || matName.Contains("Eyes", StringComparison.OrdinalIgnoreCase)
                        || matName.Contains("Core", StringComparison.OrdinalIgnoreCase)
                        || matName.Contains("AntennaTip", StringComparison.OrdinalIgnoreCase)
                        || node.Name.ToString().Contains("Eyes", StringComparison.OrdinalIgnoreCase)
                        || node.Name.ToString().Contains("AntennaTip", StringComparison.OrdinalIgnoreCase)
                        || node.Name.ToString().Contains("ChestCore", StringComparison.OrdinalIgnoreCase);

                    // For the legacy monolithic mesh fallback:
                    if (!isEmissiveSlot && emissiveTex != null 
                        && !matName.Contains("Armor", StringComparison.OrdinalIgnoreCase) 
                        && !matName.Contains("Visor", StringComparison.OrdinalIgnoreCase) 
                        && !matName.Contains("Dark", StringComparison.OrdinalIgnoreCase))
                    {
                        isEmissiveSlot = true;
                    }

                    if (isEmissiveSlot)
                    {
                        var dupMat = (StandardMaterial3D)stdMat.Duplicate();
                        dupMat.EmissionEnabled = true;
                        dupMat.Emission = new Color(0.0f, 0.94f, 1.0f); // Pure sci-fi cyan glow
                        
                        // Use texture only if not the new clean procedural eyes/core/antenna
                        if (emissiveTex != null && !matName.Contains("Eyes", StringComparison.OrdinalIgnoreCase) && !matName.Contains("AntennaTip", StringComparison.OrdinalIgnoreCase))
                        {
                            dupMat.EmissionTexture = emissiveTex;
                        }

                        dupMat.EmissionEnergyMultiplier = MaxEmissionEnergy;
                        meshInst.SetSurfaceOverrideMaterial(i, dupMat);
                        _emissiveMaterials.Add(dupMat);

                        if (node.Name.ToString().Contains("AntennaTip", StringComparison.OrdinalIgnoreCase) || matName.Contains("AntennaTip", StringComparison.OrdinalIgnoreCase))
                        {
                            _antennaTipMat = dupMat;
                        }
                    }
                }
            }
        }

        foreach (Node child in node.GetChildren())
        {
            FindAndConfigureMaterials(child, emissiveTex);
        }
    }

    private static Node? FindChildStartingWith(Node root, string prefix)
    {
        if (root.Name.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        foreach (Node child in root.GetChildren())
        {
            var found = FindChildStartingWith(child, prefix);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Constructs the continuous Star Wars / Millennium Falcon style sublight ion engine glow:
    /// Positioned at the bottom thruster nozzle (Y = -0.98f).
    /// - An incandescent white-cyan horizontal engine emitter slot.
    /// - A continuous, smoothly tapered volumetric additive ion thrust plume (no particles/squares).
    /// - A superheated inner core beam.
    /// - A cinematic horizontal anamorphic Star Wars blue flare ribbon across the nozzle.
    /// - An authentic downward cyan engine backlight (OmniLight3D).
    /// </summary>
    private void SetupSublightEngineGlow()
    {
        _sublightEngineNode = new Node3D
        {
            Name = "SublightEngineGlow",
            Position = new Vector3(0f, -0.98f, 0f)
        };
        AddChild(_sublightEngineNode);

        // 1. Incandescent Engine Mouth Slot (Elliptical sublight emitter disc inside nozzle)
        var slotMesh = new CylinderMesh
        {
            TopRadius = 0.28f,
            BottomRadius = 0.28f,
            Height = 0.02f
        };
        var slotMat = new StandardMaterial3D
        {
            ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.95f, 1.0f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.0f, 0.95f, 1.0f),
            EmissionEnergyMultiplier = 8.0f
        };
        slotMesh.Material = slotMat;

        var slotInstance = new MeshInstance3D
        {
            Name = "EngineSlotLens",
            Mesh = slotMesh,
            Position = new Vector3(0f, 0.01f, 0f),
            Scale = new Vector3(1.0f, 1.0f, 1.15f) // Conforms to elliptical nozzle
        };
        _sublightEngineNode.AddChild(slotInstance);

        // 2. Star Wars / Millennium Falcon Horizontal Engine Slot Glow Halo (Billboard Ribbon)
        string slotTexPath = "res://assets/textures/sublight_engine_slot.png";
        Texture2D? slotTex = ResourceLoader.Exists(slotTexPath) ? GD.Load<Texture2D>(slotTexPath) : null;
        
        var slotHaloMesh = new QuadMesh
        {
            Size = new Vector2(1.35f, 0.65f)
        };
        var slotHaloMat = new StandardMaterial3D
        {
            ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = StandardMaterial3D.TransparencyEnum.Alpha,
            BlendMode = StandardMaterial3D.BlendModeEnum.Add,
            CullMode = StandardMaterial3D.CullModeEnum.Disabled,
            BillboardMode = StandardMaterial3D.BillboardModeEnum.Enabled,
            BillboardKeepScale = true,
            AlbedoTexture = slotTex,
            EmissionEnabled = true,
            Emission = new Color(0.0f, 0.95f, 1.0f),
            EmissionEnergyMultiplier = 4.5f
        };
        slotHaloMesh.Material = slotHaloMat;

        var slotHaloInstance = new MeshInstance3D
        {
            Name = "SublightSlotHalo",
            Mesh = slotHaloMesh,
            Position = new Vector3(0f, 0f, 0f)
        };
        _sublightEngineNode.AddChild(slotHaloInstance);

        // 3. Smooth Downward Atmospheric Ion Thrust Wash (Pure Light, No Particles / Squares)
        string plumeTexPath = "res://assets/textures/sublight_engine_plume.png";
        Texture2D? plumeTex = ResourceLoader.Exists(plumeTexPath) ? GD.Load<Texture2D>(plumeTexPath) : null;

        var plumeMesh = new QuadMesh
        {
            Size = new Vector2(0.85f, 1.25f)
        };
        var plumeMat = new StandardMaterial3D
        {
            ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = StandardMaterial3D.TransparencyEnum.Alpha,
            BlendMode = StandardMaterial3D.BlendModeEnum.Add,
            CullMode = StandardMaterial3D.CullModeEnum.Disabled,
            BillboardMode = StandardMaterial3D.BillboardModeEnum.Enabled,
            BillboardKeepScale = true,
            AlbedoTexture = plumeTex,
            EmissionEnabled = true,
            Emission = new Color(0.0f, 0.92f, 1.0f),
            EmissionEnergyMultiplier = 3.5f
        };
        plumeMesh.Material = plumeMat;

        var plumeInstance = new MeshInstance3D
        {
            Name = "SublightIonPlume",
            Mesh = plumeMesh,
            Position = new Vector3(0f, -0.62f, 0f)
        };
        _sublightEngineNode.AddChild(plumeInstance);

        // 4. Millennium Falcon Downward Engine Backlight (OmniLight3D)
        _engineBacklight = new OmniLight3D
        {
            Name = "EngineBacklight",
            LightColor = new Color(0.0f, 0.92f, 1.0f),
            LightEnergy = 3.5f,
            OmniRange = 4.0f,
            OmniAttenuation = 1.2f,
            Position = new Vector3(0f, -0.35f, 0f)
        };
        _sublightEngineNode.AddChild(_engineBacklight);

        // Alias _thrusterPlume to _sublightEngineNode so fly-in scaling operates directly on this assembly
        _thrusterPlume = _sublightEngineNode;
        _thrusterPlume.Scale = Vector3.One;
    }

    private void SetupAudio()
    {
        _audioPlayer = new AudioStreamPlayer
        {
            Name = "AxiomThrusterAudio",
            Bus = "Master",
            VolumeDb = -6.0f
        };
        AddChild(_audioPlayer);
        _audioPlayer.Stream = GenerateThrusterSweep(520f, 130f, FlyInDuration, 0.45f);
    }

    private static AudioStreamWav GenerateThrusterSweep(float startFreq, float endFreq, float durationSeconds, float volumeScale)
    {
        int sampleRate = 44100;
        int totalSamples = (int)(sampleRate * durationSeconds);
        byte[] buffer = new byte[totalSamples * 2];

        float phase = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / totalSamples;
            float currentFreq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.65f));
            float carrier = Mathf.Sin(phase);
            float sub = Mathf.Sin(phase * 0.5f) * 0.35f;
            float envelope = Mathf.Sin(t * Mathf.Pi) * (1.0f - t * 0.3f);

            phase += 2f * Mathf.Pi * currentFreq / sampleRate;
            float sampleVal = (carrier + sub) * envelope * volumeScale;
            short pcmSample = (short)Mathf.Clamp(sampleVal * short.MaxValue, short.MinValue, short.MaxValue);

            byte[] bytes = BitConverter.GetBytes(pcmSample);
            buffer[i * 2]     = bytes[0];
            buffer[i * 2 + 1] = bytes[1];
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
            LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
            Data = buffer
        };
    }

    public void SetTransmitting(bool transmitting)
    {
        _isTransmitting = transmitting;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        // Visor and reactor core emission pulsing
        UpdateEmissiveVisor();

        // Expressive digital eye blinking cycle
        UpdateBlink(dt);

        if (_isFlyingIn)
        {
            // Position and rotation are driven by _flyInTween during flight
            // Hands tuck back slightly during high-speed swoop
            if (_leftHand != null)
            {
                _leftHand.Position = _leftHandInitPos + new Vector3(0.01f, -0.02f, 0.04f);
            }
            if (_rightHand != null)
            {
                _rightHand.Position = _rightHandInitPos + new Vector3(-0.01f, -0.02f, 0.04f);
            }
            return;
        }

        // 1. Smooth sine-wave vertical hover bob
        float hoverOffsetY = Mathf.Sin(_time * HoverSpeed) * HoverHeight;
        Position = new Vector3(_initialPosition.X, _initialPosition.Y + hoverOffsetY, _initialPosition.Z);

        // 2. Subtle Z-axis tilt sway (phase-offset)
        float tiltZ = Mathf.Sin(_time * HoverSpeed * 0.85f + 0.5f) * Mathf.DegToRad(TiltAngleDegrees);
        Rotation = new Vector3(_initialRotation.X, _initialRotation.Y, _initialRotation.Z + tiltZ);

        // 3. Independent floating hands motion (gentle microgravity out-of-phase hover)
        if (_leftHand != null)
        {
            float handBobL = Mathf.Sin(_time * 2.6f) * 0.016f;
            float handDriftZL = Mathf.Cos(_time * 2.1f) * 0.008f;
            float handTiltXL = Mathf.Sin(_time * 2.4f) * 0.035f;
            float handTiltZL = Mathf.Cos(_time * 2.6f) * 0.045f;
            _leftHand.Position = _leftHandInitPos + new Vector3(0, handBobL, handDriftZL);
            _leftHand.Rotation = _leftHandInitRot + new Vector3(handTiltXL, 0, handTiltZL);
        }

        if (_rightHand != null)
        {
            float handBobR = Mathf.Sin(_time * 2.6f + 2.0f) * 0.016f;
            float handDriftZR = Mathf.Cos(_time * 2.1f + 1.5f) * 0.008f;
            float handTiltXR = Mathf.Sin(_time * 2.4f + 1.8f) * 0.035f;
            float handTiltZR = -Mathf.Cos(_time * 2.6f + 2.0f) * 0.045f;
            _rightHand.Position = _rightHandInitPos + new Vector3(0, handBobR, handDriftZR);
            _rightHand.Rotation = _rightHandInitRot + new Vector3(handTiltXR, 0, handTiltZR);
        }

        // 4. Millennium Falcon sublight engine hover hum & backlight pulse
        if (_thrusterPlume != null)
        {
            float hum = 1.0f + Mathf.Sin(_time * 18f) * 0.04f;
            _thrusterPlume.Scale = new Vector3(1.0f, hum, 1.0f);
            if (_engineBacklight != null)
            {
                _engineBacklight.LightEnergy = 2.5f + Mathf.Sin(_time * 16f) * 0.35f;
            }
        }
    }

    private void UpdateBlink(float dt)
    {
        if (_eyesMesh == null) return;

        _blinkTimer += dt;
        if (_blinkTimer >= _nextBlinkTime)
        {
            _blinkTimer = 0f;
            _nextBlinkTime = (float)GD.RandRange(2.8f, 5.0f);
            TriggerEyeBlink();
        }
    }

    private void TriggerEyeBlink()
    {
        if (_eyesMesh == null) return;
        if (_blinkTween != null && _blinkTween.IsValid())
        {
            _blinkTween.Kill();
        }

        _blinkTween = CreateTween();
        // Snap close eyelids
        _blinkTween.TweenProperty(_eyesMesh, "scale:y", _eyesInitScale.Y * 0.06f, 0.06f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        // Spring open eyelids
        _blinkTween.TweenProperty(_eyesMesh, "scale:y", _eyesInitScale.Y, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // 25% chance of a quick double-blink
        if (GD.Randf() < 0.25f)
        {
            _blinkTween.TweenInterval(0.08f);
            _blinkTween.TweenProperty(_eyesMesh, "scale:y", _eyesInitScale.Y * 0.06f, 0.05f);
            _blinkTween.TweenProperty(_eyesMesh, "scale:y", _eyesInitScale.Y, 0.07f);
        }
    }

    private void UpdateEmissiveVisor()
    {
        if (_emissiveMaterials.Count == 0) return;

        float pulseNorm = (Mathf.Sin(_time * PulseSpeed) * 0.5f + 0.5f);
        float currentEnergy = Mathf.Lerp(MinEmissionEnergy, MaxEmissionEnergy, pulseNorm);

        foreach (var mat in _emissiveMaterials)
        {
            if (mat == _antennaTipMat && _isTransmitting)
            {
                // Rapid comms transmission flicker (12 Hz)
                float transmitEnergy = Mathf.Sin(_time * 25f) > 0 ? MaxEmissionEnergy * 1.5f : MinEmissionEnergy * 0.6f;
                mat.EmissionEnergyMultiplier = transmitEnergy;
            }
            else
            {
                mat.EmissionEnergyMultiplier = currentEnergy;
            }
        }
    }
}
