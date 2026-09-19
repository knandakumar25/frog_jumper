// Updated CyberButton.cs with clean Setup3DViewport implementation
using Godot;
using System;

/// <summary>
/// CyberButton is a high-fidelity, 3D-backed UI component.
/// It uses a SubViewport to render a 3D chamfered mesh with parallax tilting
/// and holographic state animations.
/// </summary>
[GlobalClass]
public partial class CyberButton : Control
{
    [Signal] public delegate void PressedEventHandler();

    // UI Configuration
    [Export] public string IndexText { get; set; } = "01";
    [Export] public string MainText { get; set; } = "DEPLOY MISSION";
    [Export] public string SubText { get; set; } = "// SECTOR ACCESS";
    [Export] public bool IsDisabled { get; set; } = false;

    // 3D Nodes
    private SubViewportContainer _viewportContainer;
    private SubViewport _viewport;
    private Camera3D _camera;
    private MeshInstance3D _buttonMesh;
    private Label3D _mainLabel;
    private Label3D _subLabel;
    private Label3D _indexLabel;
    private StandardMaterial3D _meshMaterial;

    // State tracking
    private bool _isHovered = false;
    private bool _isPressed = false;
    private float _parallaxIntensity = 0.08f; // Max rotation in radians

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 68);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;

        Setup3DViewport();

        // Hover Logic: Mouse Entry
        MouseEntered += () =>
        {
            if (IsDisabled) return;
            _isHovered = true;
            AnimateState(true);
            UISoundManager.Instance?.PlayHoverSound();
        };

        // Hover Logic: Mouse Exit
        MouseExited += () =>
        {
            if (IsDisabled) return;
            _isHovered = false;
            _isPressed = false;
            AnimateState(false);
        };

        // Gamepad Navigation: Focus Entry
        FocusEntered += () =>
        {
            if (IsDisabled) return;
            _isHovered = true;
            AnimateState(true);
            UISoundManager.Instance?.PlayHoverSound();
        };

        // Gamepad Navigation: Focus Exit
        FocusExited += () =>
        {
            if (IsDisabled) return;
            _isHovered = false;
            _isPressed = false;
            AnimateState(false);
        };
    }

    private void Setup3DViewport()
    {
        // Container for the isolated viewport
        _viewportContainer = new SubViewportContainer
        {
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            Stretch = true
        };

        // The viewport that will render the 3D button
        _viewport = new SubViewport
        {
            TransparentBg = true,
            Size = new Vector2I(256, 64), // Low‑res for a pixel‑art style; increase for crisper rendering
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        _viewportContainer.AddChild(_viewport);
        AddChild(_viewportContainer);

        // Lighting for the viewport – a simple directional light
        var light = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45, 45, 0),
            LightEnergy = 1.5f
        };
        _viewport.AddChild(light);

        // Ambient environment so the mesh is visible even without direct light
        var env = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Canvas,
                AmbientLightColor = new Color(0.2f, 0.2f, 0.3f),
                AmbientLightEnergy = 0.5f
            }
        };
        _viewport.AddChild(env);

        // Camera looking at the button mesh
        _camera = new Camera3D
        {
            Position = new Vector3(0, 0, 2.0f),
            Fov = 45f
        };
        _viewport.AddChild(_camera);

        // Load the button mesh from the GLB asset
        MeshInstance3D meshNode = null;
        var packed = GD.Load<PackedScene>("res://assets/models/hq/cyber_button_mesh.glb");
        var root = packed?.Instantiate();
        if (root != null)
        {
            // Try to find a MeshInstance3D named "CyberButton" first
            meshNode = root.GetNodeOrNull<MeshInstance3D>("CyberButton");
            if (meshNode == null)
            {
                // If not found, fall back to the first MeshInstance3D child
                foreach (var child in root.FindChildren("*", "MeshInstance3D"))
                {
                    if (child is MeshInstance3D mi)
                    {
                        meshNode = mi;
                        break;
                    }
                }
            }
        }

        _buttonMesh = new MeshInstance3D();
        if (meshNode != null && meshNode.Mesh != null)
        {
            _buttonMesh.Mesh = meshNode.Mesh;
        }
        else
        {
            GD.PrintErr("CyberButton: Could not find MeshInstance3D in cyber_button_mesh.glb, falling back to BoxMesh placeholder.");
            _buttonMesh.Mesh = new BoxMesh { Size = new Vector3(1, 0.2f, 0.1f) };
        }
        _viewport.AddChild(_buttonMesh);

        // Material for the button mesh – bright cyan emission for the cyber theme
        _meshMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.04f, 0.06f, 0.14f),
            EmissionEnabled = true,
            Emission = new Color(0f, 0.94f, 1f),
            EmissionEnergyMultiplier = 1.0f,
            Roughness = 0.2f,
            Metallic = 0.8f
        };
        _buttonMesh.MaterialOverride = _meshMaterial;

        // 3D labels: index, main text, sub‑text
        _indexLabel = CreateLabel3D(IndexText, new Vector3(-0.9f, 0.1f, 0.26f), 12f, new Color(0f, 0.94f, 1f));
        _mainLabel = CreateLabel3D(SafeToUpper(TranslateText(MainText)), new Vector3(-0.6f, 0.1f, 0.26f), 18f, Colors.White);
        _subLabel = CreateLabel3D(TranslateText(SubText), new Vector3(-0.6f, -0.1f, 0.26f), 10f, new Color(0f, 0.94f, 1f, 0.7f));

        _viewport.AddChild(_indexLabel);
        _viewport.AddChild(_mainLabel);
        _viewport.AddChild(_subLabel);
    }

    private Label3D CreateLabel3D(string text, Vector3 pos, float size, Color color)
    {
        var lbl = new Label3D
        {
            Text = text,
            Position = pos,
            FontSize = (int)size,
            Modulate = color,
            HorizontalAlignment = HorizontalAlignment.Left,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        };
        return lbl;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsDisabled) return;
        if (!HasFocus()) return;

        bool isAccept = @event.IsActionPressed("ui_accept") ||
            (@event is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == JoyButton.A);

        if (isAccept)
        {
            UISoundManager.Instance?.PlayClickSound();
            EmitSignal(SignalName.Pressed);
            GetViewport()?.SetInputAsHandled();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (IsDisabled) return;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _isPressed = true;
                AnimatePress(true);
            }
            else if (_isPressed)
            {
                _isPressed = false;
                AnimatePress(false);
                if (_isHovered || GetGlobalRect().HasPoint(mb.GlobalPosition))
                {
                    UISoundManager.Instance?.PlayClickSound();
                    EmitSignal(SignalName.Pressed);
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (IsDisabled) return;

        // Parallax tilting based on mouse offset
        Vector2 mousePos = GetLocalMousePosition();
        Vector2 center = Size / 2f;
        Vector2 offset = (mousePos - center) / center;

        float targetRotX = -offset.Y * _parallaxIntensity;
        float targetRotY = offset.X * _parallaxIntensity;

        _camera.Rotation = _camera.Rotation.Lerp(new Vector3(targetRotX, targetRotY, 0), (float)delta * 10f);
    }

    private void AnimateState(bool hovering)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

        float targetZ = hovering ? 0.1f : 0f;
        Color targetEmission = hovering ? new Color(1f, 0f, 0.5f) : new Color(0f, 0.94f, 1f);
        float targetEnergy = hovering ? 3.0f : 1.0f;

        tween.TweenProperty(_buttonMesh, "position:z", targetZ, 0.2);
        tween.TweenProperty(_meshMaterial, "emission", targetEmission, 0.2);
        tween.TweenProperty(_meshMaterial, "emission_energy_multiplier", targetEnergy, 0.2);
    }

    private void AnimatePress(bool pressed)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

        float targetScale = pressed ? 0.95f : 1.0f;
        float targetZ = pressed ? -0.1f : 0f;

        tween.TweenProperty(_buttonMesh, "scale", Vector3.One * targetScale, 0.1);
        tween.TweenProperty(_buttonMesh, "position:z", targetZ, 0.1);
    }

    private string TranslateText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return Tr(input);
    }

    private static string SafeToUpper(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.ToUpper();
    }

    private string Tr(string text) => MainMenu.Tr(text);
}
