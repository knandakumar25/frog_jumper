using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// MenuBackgroundManager handles the atmospheric visuals for the main menu.
/// </summary>
public partial class MenuBackgroundManager : Node
{
	public override void _Ready()
	{
		// Atmospheric visuals (clouds/falling characters) have been removed per design request.
	}

	public override void _Process(double delta)
	{
		// No active background visual processing.
	}
}
