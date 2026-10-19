using Godot;
using Godot.Collections;

/// <summary>
/// GameManager handles the in-level scoring system and interfaces with the <see cref="SessionManager"/>
/// to persist progress. It acts as the primary bridge between gameplay events (collecting points)
/// and the session persistence layer.
///
/// Unity Migration Note: This will likely become a Singleton or a Component on a Global GameState object.
/// </summary>
public partial class GameManager : Node
{
	// ─── Node references ─────────────────────────────────────────────────────
	/// <summary>
	/// UI Label used to display the current point total to the player.
	/// Unity Migration: Replace with a reference to a TMPro.TextMeshProUGUI component.
	/// </summary>
	private Label _pointsLabel;

	// ─── State ───────────────────────────────────────────────────────────────
	/// <summary>
	/// Current score for the active level.
	/// </summary>
	[Export] public int Points { get; set; } = 0;

	/// <summary>
	/// Flag to prevent multiple reload calls when the player hits the kill-plane.
	/// </summary>
	private bool _isReloading = false;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// Godot Unique Name lookup (%PointsLabel).
		// Unity Migration: Assign via Inspector or use GameObject.FindWithTag.
		// The combat HUD reads Points directly; legacy scenes still use this label.
		_pointsLabel = GetNodeOrNull<Label>("%PointsLabel");

		if (_pointsLabel != null)
		{
			// Tr() is Godot's built-in localization function.
			// Unity Migration: Use the Unity Localization Package (LocalizedString).
			_pointsLabel.Text = Tr("Points") + ": " + Points;
		}
	}

	// ─── Score management ────────────────────────────────────────────────────
	/// <summary>
	/// Persists the current points to the active session via the SessionManager.
	/// </summary>
	/// <param name="levelName">The name of the level being completed (e.g., "level1").</param>
	public async void SaveScore(string levelName)
	{
		// Access the SessionManager singleton instance.
		var sm = SessionManager.Instance;
		if (sm != null && sm.CurrentSessionId > 0)
		{
			int levelNum = 1;
			// Extract numeric level ID from string (e.g., "level3" -> 3).
			if (levelName.StartsWith("level") && int.TryParse(levelName.Substring(5), out int n))
				levelNum = n;

			// Update the database record for the current user session.
			await sm.UpdateSessionAsync(sm.CurrentSessionId, levelNum, Points, 0);
		}
	}

	/// <summary>
	/// Increments the point counter and updates the UI display.
	/// </summary>
	public void AddPoint()
	{
		Points += 1;
		GD.Print(Points);
		if (_pointsLabel != null)
		{
			_pointsLabel.Text = Tr("Points") + ": " + Points;
		}
	}

	// ─── Water kill-plane signal receiver ────────────────────────────────────
	/// <summary>
	/// Receiver for the 'body_entered' signal from the water hazard area.
	/// Triggers a scene reload if the colliding body is a character.
	/// </summary>
	/// <param name="body">The node that entered the water area.</param>
	public void _on_water_body_entered(Node2D body)
	{
		// Prevent infinite reload loops.
		if (_isReloading) return;

		// Verify the object is a physics character.
		// Unity Migration: Use Tags (e.g., if (body.CompareTag("Player"))) or Component check.
		if (body is CharacterBody2D)
		{
			_isReloading = true;
			// Deferred call to avoid modifying the scene tree during a physics callback.
			// Unity Migration: Use SceneManager.LoadScene(SceneManager.GetActiveScene().name).
			GetTree().CallDeferred("reload_current_scene");
		}
	}

	// ─── GDScript-compatible wrappers ──────────────────────────────────────────
	// These methods allow GDScript files (which use snake_case) to call C# methods without naming conflicts.
	public void save_score(string level_name) => SaveScore(level_name);
	public void add_point() => AddPoint();
}
