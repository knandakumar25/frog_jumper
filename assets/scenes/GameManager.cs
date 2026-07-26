using Godot;
using Godot.Collections;

/// <summary>
/// In-level manager that tracks points and saves scores through SessionManager.
/// Converted from game_manager.gd.
/// </summary>
public partial class GameManager : Node
{
	// ─── Node references ─────────────────────────────────────────────────────
	private Label _pointsLabel;

	// ─── State ───────────────────────────────────────────────────────────────
	[Export] public int Points { get; set; } = 0;
	private bool _isReloading = false;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		_pointsLabel = GetNode<Label>("%PointsLabel");
		if (_pointsLabel != null)
			_pointsLabel.Text = Tr("Points") + ": " + Points;
	}

	// ─── Score management ────────────────────────────────────────────────────
	public void SaveScore(string levelName)
	{
		// Update active session progress via SessionManager
		var sm = SessionManager.Instance;
		if (sm != null && sm.CurrentSessionId > 0)
		{
			int levelNum = 1;
			if (levelName.StartsWith("level") && int.TryParse(levelName.Substring(5), out int n))
				levelNum = n;

			sm.UpdateSession(sm.CurrentSessionId, levelNum, Points, 0);
		}
	}

	public void AddPoint()
	{
		Points += 1;
		GD.Print(Points);
		if (_pointsLabel != null)
			_pointsLabel.Text = Tr("Points") + ": " + Points;
	}

	// ─── Water kill-plane signal receiver ────────────────────────────────────
	public void _on_water_body_entered(Node2D body)
	{
		if (_isReloading) return;
		if (body is CharacterBody2D)
		{
			_isReloading = true;
			GetTree().CallDeferred("reload_current_scene");
		}
	}

	// GDScript-compatible wrappers
	public void save_score(string level_name) => SaveScore(level_name);
	public void add_point() => AddPoint();
}
