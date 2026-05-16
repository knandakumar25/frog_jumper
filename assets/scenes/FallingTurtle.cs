using Godot;

/// <summary>
/// Same concept as FallingCharacter but used specifically for the Cyber Shogun sprite.
/// Converted from falling_turtle.gd. Inherits the full FallingCharacter implementation
/// but defaults CharacterName to "Cyber Shogun".
/// </summary>
public partial class FallingTurtle : FallingCharacter
{
    public FallingTurtle()
    {
        CharacterName = "Cyber Shogun";
    }

    public override void _Ready()
    {
        base._Ready();
    }
}

