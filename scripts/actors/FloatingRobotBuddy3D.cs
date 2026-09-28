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
    [Export] public float FlyInDuration { get; set; } = 1.25f;
    [Export] public Vector3 FlyInOffset { get; set; } = new Vector3(-3.2f, 2.6f, -1.0f);

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
    private GpuParticles3D? _thrusterParticles;

    public override void _Ready()
    {
        _initialPosition = Position;
        _initialRotation = Rotation;

        SetupRobotMeshAndMaterials();
        SetupThrusterParticles();
        SetupThrusterPlume();
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
    }

    /// <summary>
    /// Executes the high-altitude fly-in sequence for AXIOM-7 into its hover station.
    /// </summary>
    public void FlyIn(float duration = 1.25f)
    {
        if (_flyInTween != null && _flyInTween.IsValid())
        {
            _flyInTween.Kill();
        }

        _isFlyingIn = true;
        _time = 0f;

        Vector3 startPos = _initialPosition + FlyInOffset;
        Position = startPos;

        // Lean forward into trajectory with flight banking
        Vector3 startRotDegrees = new Vector3(22f, -25f, -18f);
        RotationDegrees = startRotDegrees;

        // Flare thruster plume to flight burn scale
        if (_thrusterPlume != null)
        {
            _thrusterPlume.Scale = new Vector3(0.9f, 1.8f, 0.9f);
        }

        // Play thruster swoop audio
        if (_audioPlayer != null)
        {
            _audioPlayer.Play();
        }

        _flyInTween = CreateTween();
        _flyInTween.SetParallel(true);

        // 1. Smooth swoop to target hover position
        _flyInTween.TweenProperty(this, "position", _initialPosition, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        // 2. Level out rotation to resting stance
        _flyInTween.TweenProperty(this, "rotation", _initialRotation, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        // 3. Thruster plume deceleration
        if (_thrusterPlume != null)
        {
            _flyInTween.TweenProperty(_thrusterPlume, "scale", new Vector3(0.45f, 0.55f, 0.45f), duration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }

        // Touchdown / arrival at hover station
        _flyInTween.Chain().TweenCallback(Callable.From(OnFlyInArrived));
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
            _thrusterPlume.Scale = new Vector3(0.45f, 0.55f, 0.45f);
        }

        _isFlyingIn = false;
        _time = 0f;
        EmitSignal(SignalName.FlyInCompleted);
    }

    private void SetupRobotMeshAndMaterials()
    {
        string glbPath = "res://assets/models/hq/Meshy_AI__0807164603_texture.glb";
        if (!FileAccess.FileExists(glbPath))
        {
            GD.PrintErr($"FloatingRobotBuddy3D: GLB model not found at '{glbPath}'");
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

        // Load emissive texture if available
        Texture2D? emissiveTex = null;
        string emissivePath = "res://assets/models/hq/Meshy_AI__0807164603_texture_emissive.jpg";
        if (FileAccess.FileExists(emissivePath))
        {
            emissiveTex = GD.Load<Texture2D>(emissivePath);
        }

        // Find all MeshInstance3D nodes and configure emissive materials
        FindAndConfigureMaterials(modelInstance, emissiveTex);
        GD.Print($"FloatingRobotBuddy3D: Configured {_emissiveMaterials.Count} emissive material(s)");
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
                    // Duplicate material to allow runtime emission pulsing without affecting disk resources
                    var dupMat = (StandardMaterial3D)stdMat.Duplicate();
                    dupMat.EmissionEnabled = true;
                    dupMat.Emission = new Color(0.0f, 0.94f, 1.0f); // Vibrant cyan glow
                    
                    if (emissiveTex != null)
                    {
                        dupMat.EmissionTexture = emissiveTex;
                    }

                    dupMat.EmissionEnergyMultiplier = MaxEmissionEnergy;
                    meshInst.SetSurfaceOverrideMaterial(i, dupMat);
                    _emissiveMaterials.Add(dupMat);
                }
            }
        }

        foreach (Node child in node.GetChildren())
        {
            FindAndConfigureMaterials(child, emissiveTex);
        }
    }

    private void SetupThrusterParticles()
    {
        _thrusterParticles = new GpuParticles3D();
        _thrusterParticles.Amount = 24;
        _thrusterParticles.Lifetime = 0.6f;
        _thrusterParticles.Position = new Vector3(0f, -0.35f, 0f);

        var processMat = new ParticleProcessMaterial();
        processMat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        processMat.EmissionSphereRadius = 0.12f;
        processMat.Direction = new Vector3(0f, -1f, 0f);
        processMat.Spread = 25f;
        processMat.InitialVelocityMin = 0.8f;
        processMat.InitialVelocityMax = 1.6f;
        processMat.Gravity = new Vector3(0f, -0.5f, 0f);
        processMat.ScaleMin = 0.04f;
        processMat.ScaleMax = 0.08f;
        processMat.Color = new Color(0.0f, 0.94f, 1.0f, 0.85f); // Cyan spark glow

        _thrusterParticles.ProcessMaterial = processMat;

        // QuadMesh for spark particles
        var quadMesh = new QuadMesh();
        quadMesh.Size = new Vector2(0.06f, 0.06f);

        var pMat = new StandardMaterial3D();
        pMat.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
        pMat.Transparency = StandardMaterial3D.TransparencyEnum.Alpha;
        pMat.AlbedoColor = new Color(0.0f, 0.94f, 1.0f, 0.9f);
        pMat.BillboardMode = StandardMaterial3D.BillboardModeEnum.Particles;
        quadMesh.Material = pMat;

        _thrusterParticles.DrawPass1 = quadMesh;
        AddChild(_thrusterParticles);
    }

    private void SetupThrusterPlume()
    {
        string plumePath = "res://assets/models/menu/thruster_plume.glb";
        if (!FileAccess.FileExists(plumePath)) return;

        var scene = GD.Load<PackedScene>(plumePath);
        if (scene == null) return;

        _thrusterPlume = scene.Instantiate<Node3D>();
        if (_thrusterPlume == null) return;

        _thrusterPlume.Position = new Vector3(0f, -0.32f, 0f);
        _thrusterPlume.Scale = new Vector3(0.45f, 0.55f, 0.45f);
        AddChild(_thrusterPlume);
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

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        // Visor emission pulsing (active during both flight and idle)
        UpdateEmissiveVisor();

        if (_isFlyingIn)
        {
            // Position and rotation are driven by _flyInTween during flight
            return;
        }

        // 1. Smooth sine-wave vertical hover bob
        float hoverOffsetY = Mathf.Sin(_time * HoverSpeed) * HoverHeight;
        Position = new Vector3(_initialPosition.X, _initialPosition.Y + hoverOffsetY, _initialPosition.Z);

        // 2. Subtle Z-axis tilt sway (phase-offset)
        float tiltZ = Mathf.Sin(_time * HoverSpeed * 0.85f + 0.5f) * Mathf.DegToRad(TiltAngleDegrees);
        Rotation = new Vector3(_initialRotation.X, _initialRotation.Y, _initialRotation.Z + tiltZ);

        // 3. Subtle thruster flame flicker while hovering
        if (_thrusterPlume != null)
        {
            float flicker = 0.52f + Mathf.Sin(_time * 14f) * 0.05f;
            _thrusterPlume.Scale = new Vector3(0.45f, flicker, 0.45f);
        }
    }

    private void UpdateEmissiveVisor()
    {
        if (_emissiveMaterials.Count == 0) return;

        float pulseNorm = (Mathf.Sin(_time * PulseSpeed) * 0.5f + 0.5f);
        float currentEnergy = Mathf.Lerp(MinEmissionEnergy, MaxEmissionEnergy, pulseNorm);

        foreach (var mat in _emissiveMaterials)
        {
            mat.EmissionEnergyMultiplier = currentEnergy;
        }
    }
}
