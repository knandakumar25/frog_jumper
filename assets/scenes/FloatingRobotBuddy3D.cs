using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Controls the 3D AXIOM-7 Floating Robot Buddy.
/// Features:
/// - Smooth vertical sine-wave hover & Z-tilt sway.
/// - Dynamic emissive visor pulsing on a timer (modulating emission energy multiplier).
/// - Cyan thruster spark particle system at the base.
/// </summary>
public partial class FloatingRobotBuddy3D : Node3D
{
    [Export] public float HoverSpeed { get; set; } = 2.5f;
    [Export] public float HoverHeight { get; set; } = 0.12f;
    [Export] public float TiltAngleDegrees { get; set; } = 4f;

    [Export] public float PulseSpeed { get; set; } = 3.0f;
    [Export] public float MinEmissionEnergy { get; set; } = 1.2f;
    [Export] public float MaxEmissionEnergy { get; set; } = 4.5f;

    private float _time = 0f;
    private Vector3 _initialPosition;
    private List<StandardMaterial3D> _emissiveMaterials = new();
    private GpuParticles3D? _thrusterParticles;

    public override void _Ready()
    {
        _initialPosition = Position;
        SetupRobotMeshAndMaterials();
        SetupThrusterParticles();
    }

    private void SetupRobotMeshAndMaterials()
    {
        string glbPath = "res://assets/images/AXIOM-7/Meshy_AI__0807164603_texture.glb";
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
        string emissivePath = "res://assets/images/AXIOM-7/Meshy_AI__0807164603_texture_emissive.jpg";
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

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        // 1. Smooth sine-wave vertical hover bob
        float hoverOffsetY = Mathf.Sin(_time * HoverSpeed) * HoverHeight;
        Position = new Vector3(_initialPosition.X, _initialPosition.Y + hoverOffsetY, _initialPosition.Z);

        // 2. Subtle Z-axis tilt sway (slightly phase-offset for floaty feel)
        float tiltZ = Mathf.Sin(_time * HoverSpeed * 0.85f + 0.5f) * Mathf.DegToRad(TiltAngleDegrees);
        Rotation = new Vector3(Rotation.X, Rotation.Y, tiltZ);

        // 3. Emissive Visor Pulsing on Timer
        if (_emissiveMaterials.Count > 0)
        {
            float pulseNorm = (Mathf.Sin(_time * PulseSpeed) * 0.5f + 0.5f);
            float currentEnergy = Mathf.Lerp(MinEmissionEnergy, MaxEmissionEnergy, pulseNorm);

            foreach (var mat in _emissiveMaterials)
            {
                mat.EmissionEnergyMultiplier = currentEnergy;
            }
        }
    }
}
