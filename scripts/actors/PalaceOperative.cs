using Godot;

/// <summary>
/// Player controller for the code-built missions (<c>CodeLevel</c>): CyberShogun's combat and gravity,
/// drawn by its own AnimatedSprite2D from the operative's SpriteCook atlas through
/// <c>OperativeVisuals</c>. Operatives whose atlas is not made yet stay invisible (an intentional gap);
/// the old hand-drawn overlay was deleted on 2026-11-12.
/// </summary>
public partial class PalaceOperative : CyberShogun
{
    /// <summary>Enlarges the body and sets the mission hurtbox and attack reach without changing operative stats.</summary>
    public override void _Ready()
    {
        base._Ready();
        ExternalRespawn = true;
        Scale = Vector2.One * 1.35f;
        var body = GetNode<CollisionShape2D>("CollisionShape2D");
        body.Shape = new RectangleShape2D { Size = new Vector2(40, 94) };
        body.Position = new Vector2(0, -13);
        var attack = GetNode<Area2D>("AttackArea");
        attack.CollisionMask = 2;
        attack.GetNode<CollisionShape2D>("CollisionShape2D").Shape = new RectangleShape2D { Size = new Vector2(86, 86) };
        GetNodeOrNull<PointLight2D>("OperativeAuraLight")?.Hide();
        GetNode<AudioStreamPlayer>("JumpSound").VolumeDb = -18;
    }
}
