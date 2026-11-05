using Godot;
using System;

/// <summary>A full-height sector interlock that opens only after accepted mission progress.
/// It prevents jumping past incomplete objectives without changing their order or requiring defenders behind a locked ability gate.</summary>
public partial class MissionInterlock2D : Node2D
{
    /// <summary>World-space vertical extent; collision is independent from environment artwork.</summary>
    public float Height { get; set; }
    /// <summary>Mission ledger predicate queried while this sector remains sealed.</summary>
    public Func<bool> Accepted { get; set; } = () => false;
    private CollisionShape2D? _shape;
    private bool _open;

    /// <summary>Builds the narrow forcefield and an accessible ground-level status label.</summary>
    public override void _Ready()
    {
        var body = new StaticBody2D { CollisionLayer = 1, CollisionMask = 0 };
        _shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(24, Height) }, Position = new(0, -Height / 2) };
        body.AddChild(_shape); AddChild(body);
        var label = new Label { Position = new Vector2(-180, -130), Text = "SECTOR INTERLOCK // COMPLETE OBJECTIVE" };
        label.AddThemeColorOverride("font_color", new Color("ffcc88")); AddChild(label);
    }

    /// <summary>Opens once the ledger accepts the stage; retries before accepted boss defeat leave the arena sealed.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_open || !Accepted()) return;
        _open = true;
        _shape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Visible = false;
    }

    /// <summary>Draws an unmistakable nondecorative barrier with ground-level emitter hardware.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(-12, -Height, 24, Height), new Color("ffbb4438"));
        DrawLine(new Vector2(0, -Height), Vector2.Zero, new Color("ffbb44"), 3);
        DrawRect(new Rect2(-30, -20, 60, 20), new Color("687786"));
    }
}
