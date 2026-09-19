using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
    // ... (rest of class unchanged) ...
    private void Setup3DTitle()
    {
        if (_titleContainer == null) return;
        var oldTitle = _titleContainer.GetNodeOrNull<Label>("MainTitle");
        oldTitle?.QueueFree();

        _titleViewportContainer = new SubViewportContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Stretch = true
        };
        var viewport = new SubViewport
        {
            TransparentBg = true,
            Size = new Vector2I(1024, 256),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        _titleViewportContainer.AddChild(viewport);
        _titleContainer.AddChild(_titleViewportContainer);

        _titleCamera = new Camera3D { Position = new Vector3(0, 0, 4.0f), Fov = 45f };
        viewport.AddChild(_titleCamera);

        var titleLight = new DirectionalLight3D { RotationDegrees = new Vector3(-30, 30, 0), LightEnergy = 1.2f };
        viewport.AddChild(titleLight);

        var holoShader = GD.Load<Shader>("res://assets/shaders/holographic.gdshader");
        var holoMat = new ShaderMaterial();
        if (holoShader != null) holoMat.Shader = holoShader;

        // For now, use a simple TextMesh fallback for the title to avoid GLB issues.
        var fallback = new MeshInstance3D
        {
            Mesh = new TextMesh { Text = "AXIOM FORCE", FontSize = 48, Depth = 0.1f },
            MaterialOverride = holoMat
        };
        viewport.AddChild(fallback);
    }
    // ... (rest of class unchanged) ...
}