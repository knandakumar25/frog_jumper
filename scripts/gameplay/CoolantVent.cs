using Godot;
using System;

/// <summary>
/// Cryogenic and superheated geothermal coolant vent hazard for subterranean levels.
/// Periodically vents damaging cryogenic vapor or superheated steam columns.
/// Can be extinguished or bypassed via Oceanus's Douse ability or terminal overrides.
/// </summary>
public partial class CoolantVent : Area2D
{
    /// <summary>Height of the vertical vent steam jet in pixels.</summary>
    [Export] public float VentHeight { get; set; } = 220.0f;

    /// <summary>Width of the vent jet in pixels.</summary>
    [Export] public float VentWidth { get; set; } = 48.0f;

    /// <summary>Venting duration in seconds.</summary>
    [Export] public float ActiveDuration { get; set; } = 2.0f;

    /// <summary>Downtime duration in seconds.</summary>
    [Export] public float InactiveDuration { get; set; } = 2.0f;

    /// <summary>Start offset in seconds to stagger multiple vents.</summary>
    [Export] public float StartOffset { get; set; } = 0.0f;

    /// <summary>Damage inflicted per hit.</summary>
    [Export] public float Damage { get; set; } = 20.0f;

    /// <summary>Vapor tint (cryogenic cyan/frost by default).</summary>
    [Export] public Color VaporColor { get; set; } = new Color(0.2f, 0.85f, 1.0f, 0.85f);

    /// <summary>True if actively expelling steam.</summary>
    public bool IsVenting { get; private set; } = true;

    /// <summary>True if neutralized by player ability or objective completion.</summary>
    public bool IsNeutralized { get; private set; } = false;

    private float _timer;
    private CollisionShape2D? _collision;
    private RectangleShape2D? _shape;

    public override void _Ready()
    {
        _timer = StartOffset;
        CollisionLayer = 0;
        CollisionMask = 2; // Match Player layer
        Monitoring = true;

        _shape = new RectangleShape2D { Size = new Vector2(VentWidth, VentHeight) };
        _collision = new CollisionShape2D
        {
            Shape = _shape,
            Position = new Vector2(0, -VentHeight / 2f)
        };
        AddChild(_collision);

        BodyEntered += OnBodyEntered;
    }

    /// <summary>Cycles venting states and updates steam opacity.</summary>
    public override void _Process(double delta)
    {
        if (IsNeutralized)
        {
            if (IsVenting)
            {
                IsVenting = false;
                if (_collision != null) _collision.SetDeferred("disabled", true);
                QueueRedraw();
            }
            return;
        }

        _timer += (float)delta;
        float cycle = ActiveDuration + InactiveDuration;
        float current = _timer % cycle;
        bool shouldVent = current < ActiveDuration;

        if (shouldVent != IsVenting)
        {
            IsVenting = shouldVent;
            if (_collision != null) _collision.SetDeferred("disabled", !IsVenting);
        }

        QueueRedraw();
    }

    /// <summary>Neutralizes the vent permanently (e.g. via Oceanus douse or puzzle override).</summary>
    public void Neutralize()
    {
        IsNeutralized = true;
        IsVenting = false;
        if (_collision != null) _collision.SetDeferred("disabled", true);
        QueueRedraw();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!IsVenting || IsNeutralized) return;
        if (body is CyberShogun player)
        {
            player.TakeDamage(Damage);
        }
        else if (body.HasMethod("TakeDamage"))
        {
            body.Call("TakeDamage", Damage);
        }
    }

    /// <summary>Draws the metallic vent grille and pulsating steam jet.</summary>
    public override void _Draw()
    {
        // Metallic base grille
        DrawRect(new Rect2(-VentWidth / 2f - 4, -10, VentWidth + 8, 10), new Color("14222e"));
        DrawLine(new Vector2(-VentWidth / 2f, 0), new Vector2(VentWidth / 2f, 0), VaporColor, 3f);

        if (!IsVenting || IsNeutralized) return;

        // Procedural steam jet plume
        float pulse = 0.6f + 0.4f * Mathf.Sin(_timer * 12f);
        Color plumeColor = new(VaporColor.R, VaporColor.G, VaporColor.B, 0.45f * pulse);

        Rect2 plumeRect = new(-VentWidth / 2f, -VentHeight, VentWidth, VentHeight);
        DrawRect(plumeRect, plumeColor);

        // Core high-velocity steam column
        Rect2 coreRect = new(-VentWidth / 4f, -VentHeight * 0.9f, VentWidth / 2f, VentHeight * 0.9f);
        DrawRect(coreRect, new Color(1f, 1f, 1f, 0.6f * pulse));
    }
}
