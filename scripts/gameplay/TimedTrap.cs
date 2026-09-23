using Godot;
using System;

/// <summary>
/// TimedTrap is an environmental hazard that cycles between a warning state
/// and an active state, dealing damage and knockback to entities in its zone.
/// </summary>
public partial class TimedTrap : Node3D
{
    [Export] public float WarningDuration = 2.0f;
    [Export] public float ActiveDuration = 0.5f;
    [Export] public float CycleInterval = 4.0f;
    [Export] public float Damage = 30.0f;
    [Export] public float KnockbackForce = 12.0f;

    private Timer _cycleTimer;
    private Area3D _trapArea;
    private MeshInstance3D _visualIndicator;

    public enum TrapState { Warning, Active, Idle }
    public TrapState CurrentState { get; private set; } = TrapState.Idle;

    public override void _Ready()
    {
        // Setup Area3D for detection.
        _trapArea = new Area3D { Name = "TrapArea" };
        var collision = new CollisionShape3D();
        collision.Shape = new CylinderShape3D { Radius = 2.0f, Height = 1.0f };
        _trapArea.AddChild(collision);
        AddChild(_trapArea);

        // Setup visual indicator (a simple cylinder that changes color).
        _visualIndicator = new MeshInstance3D();
        var mesh = new CylinderMesh { TopRadius = 2.0f, BottomRadius = 2.0f, Height = 0.1f };
        _visualIndicator.Mesh = mesh;
        AddChild(_visualIndicator);
        UpdateVisuals();

        // Setup Cycle Timer.
        _cycleTimer = new Timer();
        _cycleTimer.OneShot = false;
        _cycleTimer.WaitTime = CycleInterval;
        _cycleTimer.Timeout += OnCycleTimeout;
        AddChild(_cycleTimer);
        _cycleTimer.Start();
    }

    private async void OnCycleTimeout()
    {
        // 1. Warning Phase.
        CurrentState = TrapState.Warning;
        UpdateVisuals();
        GD.Print("TimedTrap: WARNING!");
        await ToSignal(GetTree().CreateTimer(WarningDuration), "timeout");

        // 2. Active Phase.
        CurrentState = TrapState.Active;
        UpdateVisuals();
        GD.Print("TimedTrap: ACTIVE!");
        TriggerTrap();
        await ToSignal(GetTree().CreateTimer(ActiveDuration), "timeout");

        // 3. Idle Phase.
        CurrentState = TrapState.Idle;
        UpdateVisuals();
    }

    private void TriggerTrap()
    {
        var bodies = _trapArea.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            // Apply Damage.
            var health = body.GetNodeOrNull<Health>("Health") ?? body.FindChild("Health", true, false) as Health;
            health?.TakeDamage(Damage);

            // Apply Knockback.
            Vector3 trapDiff = body.GlobalPosition - GlobalPosition;
            Vector3 knockDir = trapDiff.LengthSquared() > 0 ? trapDiff.Normalized() : Vector3.Up;
            knockDir.Y = 0.5f; // Pop them up slightly.

            if (body is Enemy3D enemy)
            {
                enemy.OnTacticalImpact(knockDir * KnockbackForce);
            }
            else if (body is Player3D player)
            {
                player.Velocity += knockDir * KnockbackForce;
            }
        }
    }

    private void UpdateVisuals()
    {
        if (_visualIndicator == null) return;

        var material = new StandardMaterial3D();
        switch (CurrentState)
        {
            case TrapState.Warning:
                material.AlbedoColor = new Color(1, 0.8f, 0); // Yellow
                material.EmissionEnabled = true;
                material.Emission = new Color(1, 0.8f, 0);
                break;
            case TrapState.Active:
                material.AlbedoColor = new Color(1, 0, 0); // Red
                material.EmissionEnabled = true;
                material.Emission = new Color(1, 0, 0);
                break;
            case TrapState.Idle:
                material.AlbedoColor = new Color(0.2f, 0.2f, 0.2f); // Dark Grey
                material.EmissionEnabled = false;
                break;
        }
        _visualIndicator.MaterialOverride = material;
    }
}
