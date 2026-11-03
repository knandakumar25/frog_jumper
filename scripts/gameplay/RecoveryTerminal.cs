using Godot;
using System;

/// <summary>
/// Indestructible archive recovery terminal featuring interactive holographic slate projection,
/// status indicators, and context-action integration across keyboard, controller, mouse, and touch.
/// </summary>
public partial class RecoveryTerminal : Node2D
{
    /// <summary>Stable ledger objective, checked before every interaction.</summary>
    public string Objective { get; set; } = "";
    /// <summary>Owning mission supplies prerequisites and the sole speech/subtitle lane.</summary>
    public NamesWithoutNumbersMission Mission { get; set; } = null!;
    /// <summary>Readable interaction title; localization uses the stable objective ID.</summary>
    public string Caption { get; set; } = "Recover original records";
    /// <summary>Tests proximity to the body, independent of decoration and combat collision.</summary>
    public bool InRange => Mission.Player.GlobalPosition.DistanceTo(GlobalPosition + new Vector2(0, -46)) < 135;
    /// <summary>Accepts only fresh context presses near a ready terminal; records have no hurtbox.</summary>
    public bool TryInteract() => !Mission.GameplaySuspended && InRange && Mission.CommitObjective(Objective);

    private float _holoTimer;

    /// <summary>Pointer and touch can select the visible console while in context range; combat collision remains absent.</summary>
    public override void _Input(InputEvent input)
    {
        if (!IsInsideTree() || Mission.GameplaySuspended || !InRange) return;
        Vector2? point = input is InputEventScreenTouch { Pressed: true } touch ? touch.Position :
            input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse ? mouse.Position : null;
        if (!point.HasValue) return;
        Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * point.Value;
        if (new Rect2(-70, -155, 140, 175).HasPoint(local) && TryInteract()) GetViewport().SetInputAsHandled();
    }

    /// <summary>Draws an elevated cyber pedestal, holographic data slate, and status brackets.</summary>
    public override void _Draw()
    {
        bool done = Mission.Progress.Has(Objective);
        bool ready = Mission.Progress.CanComplete(Objective);
        Color stateColor = done ? new Color("85dacb") : ready ? Colors.White : new Color("e6a650");

        // Console Pedestal
        DrawRect(new Rect2(-34, -108, 68, 108), new Color("101c29"));
        DrawRect(new Rect2(-27, -96, 54, 40), stateColor);
        DrawLine(new Vector2(-45, 0), new Vector2(45, 0), stateColor, 5);

        // Hardware Bevel & Base
        DrawRect(new Rect2(-38, -12, 76, 12), new Color("182635"));
        DrawLine(new Vector2(-34, -108), new Vector2(34, -108), new Color(stateColor, 0.8f), 2);

        // Floating Holographic Slate Projection (Centered at y = -140)
        float floatOffset = Mathf.Sin(_holoTimer * 3.0f) * 3.0f;
        Vector2 holoCenter = new(0, -145 + floatOffset);

        if (done)
        {
            // Authenticated State: Emerald Shield / Check Reticle
            DrawArc(holoCenter, 24, 0, Mathf.Tau, 24, new Color("85dacb", 0.7f), 2);
            DrawLine(holoCenter + new Vector2(-10, 0), holoCenter + new Vector2(-2, 8), new Color("85dacb"), 2.5f);
            DrawLine(holoCenter + new Vector2(-2, 8), holoCenter + new Vector2(12, -8), new Color("85dacb"), 2.5f);
        }
        else if (ready)
        {
            // Ready / Active State: Pulsating Data Reticle & Interaction Brackets
            float pulse = 0.6f + 0.4f * Mathf.Sin(_holoTimer * 6.0f);
            DrawArc(holoCenter, 22, -0.8f, 0.8f, 16, new Color(0, 0.94f, 1f, pulse), 2);
            DrawArc(holoCenter, 22, Mathf.Pi - 0.8f, Mathf.Pi + 0.8f, 16, new Color(0, 0.94f, 1f, pulse), 2);
            DrawCircle(holoCenter, 5, new Color(Colors.White, pulse));

            // Proximity invitation pulse
            if (InRange)
            {
                float expand = Mathf.PosMod(_holoTimer * 35, 28);
                DrawArc(holoCenter, 16 + expand, 0, Mathf.Tau, 24, new Color(0, 0.94f, 1f, (1f - expand / 28f) * 0.5f), 1.5f);
            }
        }
        else
        {
            // Locked State: Amber Security Lock Hexagon
            DrawArc(holoCenter, 18, 0, Mathf.Tau, 6, new Color("e6a650", 0.75f), 2);
            DrawRect(new Rect2(holoCenter + new Vector2(-6, -6), new Vector2(12, 12)), new Color("e6a650", 0.5f));
        }
    }

    /// <summary>Refreshes readiness emission and advances holographic pulse.</summary>
    public override void _Process(double delta)
    {
        _holoTimer += (float)delta;
        QueueRedraw();
    }
}
