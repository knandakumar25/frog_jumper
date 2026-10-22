using Godot;

/// <summary>Level 1's platform controller: existing combat and gravity with approved illustrated character art.</summary>
public partial class PalaceOperative : CyberShogun
{
    /// <summary>Aligns the hurtbox and attack reach with the larger illustrated body without changing operative stats.</summary>
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
        AddChild(new TrainingArtwork { Name = "ApprovedCharacterArtwork" });
        GetNodeOrNull<PointLight2D>("OperativeAuraLight")?.Hide();
        GetNode<AudioStreamPlayer>("JumpSound").VolumeDb = -18;
    }
}
