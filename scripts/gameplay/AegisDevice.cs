using Godot;
using System;

/// <summary>Mission-owned attack target. Existing melee/dash attacks operate interlocks and arena pylons without a new binding.</summary>
public partial class AegisDevice : Node2D
{
    /// <summary>Defenders and objective dependencies must permit operation before damage is accepted.</summary>
    public Func<bool>? Acceptance { get; set; }
    /// <summary>Successful operation callback, invoked exactly once.</summary>
    public Action? Operated { get; set; }
    /// <summary>Attack budget; ordinary interlocks use one hit, arena pylons use several.</summary>
    public float Health { get; set; } = 1;
    /// <summary>True after this device has completed its operation.</summary>
    public bool IsOffline { get; private set; }
    private Area2D? _hurtbox;
    /// <summary>Registers with the same attack query as ordinary enemies; no outgoing damage or body collision.</summary>
    public override void _Ready()
    {
        AddToGroup("enemies");
        var hurtbox = new Area2D { Name = "Hurtbox", CollisionLayer = 2, CollisionMask = 0 };
        _hurtbox = hurtbox;
        hurtbox.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(72,110) }, Position = new Vector2(0,-10) });
        AddChild(hurtbox);
    }
    /// <summary>Ignores shielded or repeated attacks; the supplied mission owns any durable objective reward.</summary>
    public void TakeDamage(float amount)
    {
        if (IsOffline || amount <= 0 || Acceptance?.Invoke() == false) return;
        Health -= amount;
        if (Health > 0) return;
        IsOffline = true;
        RemoveFromGroup("enemies");
        // The attack query uses collision layers rather than group membership: remove the spent hurtbox to prevent combo farming.
        _hurtbox?.SetDeferred(Area2D.PropertyName.CollisionLayer, 0);
        Operated?.Invoke();
        QueueRedraw();
    }
    /// <summary>Amber live bars become dark cyan sockets after operation; visual state never controls collision.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(-36, -65, 72, 110), new Color("#172736"));
        for (int i = 0; i < 3; i++) DrawRect(new Rect2(-24, -50 + i * 28, 48, 12),
            new Color(IsOffline ? "#245e66" : "#ffc36a"));
    }
}
