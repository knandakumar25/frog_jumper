using Godot;
using System;

/// <summary>Indestructible archive interaction. Uses the shipped attack context action across keyboard, gamepad and touch.</summary>
public partial class RecoveryTerminal : Node2D
{
    /// <summary>Stable ledger objective, checked before every interaction.</summary>
    public string Objective { get; set; } = "";
    /// <summary>Owning mission supplies prerequisites and the sole speech/subtitle lane.</summary>
    public NamesWithoutNumbersMission Mission { get; set; } = null!;
    /// <summary>Readable interaction title; localization uses the stable objective ID.</summary>
    public string Caption { get; set; } = "Recover original records";
    /// <summary>Tests proximity to the body, independent of decoration and combat collision.</summary>
    public bool InRange => Mission.Player.GlobalPosition.DistanceTo(GlobalPosition + new Vector2(0,-46)) < 135;
    /// <summary>Accepts only fresh context presses near a ready terminal; records have no hurtbox.</summary>
    public bool TryInteract() => !Mission.GameplaySuspended && InRange && Mission.CommitObjective(Objective);
    /// <summary>Pointer and touch can select the visible console while in context range; combat collision remains absent.</summary>
    public override void _Input(InputEvent input)
    {
        if (!IsInsideTree() || Mission.GameplaySuspended || !InRange) return;
        Vector2? point = input is InputEventScreenTouch { Pressed: true } touch ? touch.Position :
            input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse ? mouse.Position : null;
        if (!point.HasValue) return;
        Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * point.Value;
        if (new Rect2(-70,-155,140,175).HasPoint(local) && TryInteract()) GetViewport().SetInputAsHandled();
    }
    /// <summary>Draws a legible amber locked/cyan ready console beneath optional production art.</summary>
    public override void _Draw()
    {
        var color = Mission.Progress.Has(Objective) ? new Color("85dacb") : Mission.Progress.CanComplete(Objective) ? Colors.White : new Color("e6a650");
        DrawRect(new Rect2(-34,-108,68,108),new Color("101c29"));
        DrawRect(new Rect2(-27,-96,54,40),color);
        DrawLine(new Vector2(-45,0),new Vector2(45,0),color,5);
    }
    /// <summary>Refreshes readiness emission without running an independent input handler.</summary>
    public override void _Process(double delta) => QueueRedraw();
}
