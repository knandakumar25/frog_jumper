using Godot;
using Godot.Collections;

/// <summary>
/// In-level manager that tracks points, saves scores to SQLite, and reloads on water death.
/// Converted from game_manager.gd.
/// </summary>
public partial class GameManager : Node
{
    // ─── Node references ─────────────────────────────────────────────────────
    private Label _pointsLabel;

    // ─── State ───────────────────────────────────────────────────────────────
    [Export] public int Points { get; set; } = 0;
    private bool _isReloading = false;

    // ─── SQLite ──────────────────────────────────────────────────────────────
    private GodotObject _db;
    private readonly string _dbPath = "user://scores.db";

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        _pointsLabel = GetNode<Label>("%PointsLabel");
        _pointsLabel.Text = Tr("Points") + ": " + Points;
        InitDatabase();
    }

    // ─── Database ────────────────────────────────────────────────────────────
    private void InitDatabase()
    {
        _db = ClassDB.Instantiate("SQLite").As<GodotObject>();
        _db.Set("path", _dbPath);
        _db.Call("open_db");

        var tableDict = new Dictionary
        {
            { "id",         new Dictionary { { "data_type", "INTEGER" }, { "primary_key", true }, { "auto_increment", true } } },
            { "level_name", new Dictionary { { "data_type", "TEXT"    }, { "not_null",    true } } },
            { "score",      new Dictionary { { "data_type", "INTEGER" }, { "not_null",    true } } },
        };
        _db.Call("create_table", "scores", tableDict);
    }

    // ─── Score management ────────────────────────────────────────────────────
    public void SaveScore(string levelName)
    {
        if (_db == null) return;

        var data = new Dictionary
        {
            { "level_name", levelName },
            { "score",      Points },
        };
        _db.Call("insert_row", "scores", data);

        // Also update the active session progress
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
        _pointsLabel.Text = Tr("Points") + ": " + Points;
    }

    // ─── Water kill-plane signal receiver ────────────────────────────────────
    // Method name matches the string stored in level TSCN connection entries.
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

