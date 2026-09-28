using Godot;
using System;

/// <summary>
/// FinishZone3D is an Area3D that detects when the player has completed the level.
/// It triggers the Level3DManager to save progress and initiates the transition.
/// </summary>
public partial class FinishZone3D : Area3D
{
	[Export] public string TargetScenePath = "res://scenes/main_menu/main_menu.tscn";
	private bool _isTriggered = false;

	public override void _Ready()
	{
		// Connect the body_entered signal to our handler.
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_isTriggered) return;

		// Verify that the body is the player.
		if (body is Player3D player)
		{
			// 1. Check if the objective is complete.
			var levelManager = (GetTree().CurrentScene as Level3DManager)
				?? (GetTree().Root.FindChild("Level3DManager", true, false) as Level3DManager)
				?? (GetTree().Root.FindChild("Prototype3D", true, false) as Level3DManager);
			if (levelManager != null && !levelManager.IsObjectiveComplete())
			{
				GD.Print("You haven't collected all the cherries! Prove your worth, Warrior!");
				// Here we could trigger a UI notification.
				return;
			}

			_isTriggered = true;
			GD.Print("Exit reached! You've conquered this sector!");

			// 2. Stop player movement.
			player.StopMovement();

			// 3. Notify the Level Manager to save.
			if (levelManager != null)
			{
				levelManager.OnLevelComplete();

				// Show the mission summary screen.
				var summaryUI = GetTree().Root.FindChild("MissionSummaryUI", true, false) as MissionSummaryUI;
				if (summaryUI != null)
				{
					summaryUI.ShowSummary(levelManager.CurrentPoints, levelManager.CollectedCount);
				}
			}
			else
			{
				// Fallback if manager is missing.
				GetTree().CallDeferred("call_deferred", "load_3d_scene", TargetScenePath);
			}
		}
	}

	// Helper to trigger the existing loading screen.
	private void load_3d_scene(string path)
	{
		// This calls the static helper in LoadingScreen.cs.
		LoadingScreen.LoadScene(GetTree(), path);
	}
}
