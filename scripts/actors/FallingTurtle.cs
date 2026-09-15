using Godot;

/// <summary>
/// FallingTurtle is a specialized version of FallingCharacter.
/// It is used specifically for the "Cyber Shogun" character sprites in the background.
///
/// Unity Migration Note: In Unity, instead of creating a separate class for this,
/// you would simply use the FallingCharacter script and set the 'CharacterName'
/// property to "Cyber Shogun" in the Inspector for the specific Prefab.
/// </summary>
public partial class FallingTurtle : FallingCharacter
{
	/// <summary>
	/// Constructor sets the default character name to "Cyber Shogun".
	/// </summary>
	public FallingTurtle()
	{
		CharacterName = "Cyber Shogun";
	}

	public override void _Ready()
	{
		base._Ready();
	}
}
