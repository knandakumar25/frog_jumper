using Godot;
using Godot.Collections;

/// <summary>
/// Singleton autoload for managing game sessions using the SQLite GDExtension plugin.
/// Converted from session_manager.gd.
/// </summary>
public partial class SessionManager : Node
{
    // ─── Singleton access for other C# classes ───────────────────────────────
    public static SessionManager Instance { get; private set; }

    // ─── Fields ─────────────────────────────────────────────────────────────
    private GodotObject _db;
    private readonly string _dbPath = "user://game_sessions.db";

    // Exported so GDScript can read them (Godot converts PascalCase → snake_case)
    [Export] public int CurrentSessionId { get; set; } = -1;
    public Dictionary CurrentSessionData { get; set; } = new Dictionary();

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        GD.Print("SessionManager: _ready() start");
        Instance = this;
        try
        {
            InitDatabase();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"SessionManager: CRITICAL ERROR in _ready(): {ex.Message}\n{ex.StackTrace}");
        }
        GD.Print("SessionManager: _ready() complete");
    }

    // ─── Database initialisation ─────────────────────────────────────────────
    public void InitDatabase()
    {
        GD.Print("SessionManager: Initializing database...");
        try
        {
            var dbInstance = ClassDB.Instantiate("SQLite");
            if (dbInstance.Obj == null)
            {
                GD.PrintErr("SessionManager: ClassDB.Instantiate('SQLite') returned null! SQLite GDExtension might not be loaded.");
                return;
            }
            _db = dbInstance.As<GodotObject>();
            if (_db == null)
            {
                GD.PrintErr("SessionManager: Failed to cast SQLite instance to GodotObject!");
                return;
            }
            GD.Print("SessionManager: SQLite class instantiated successfully. Setting path...");
            _db.Set("path", _dbPath);
            GD.Print("SessionManager: Database path set to: ", _dbPath, ". Opening database...");
            _db.Call("open_db");
            GD.Print("SessionManager: Database opened. Creating table...");

            var tableDict = new Dictionary
            {
                { "id",            new Dictionary { { "data_type", "INTEGER" }, { "primary_key", true  }, { "auto_increment", true } } },
                { "character",     new Dictionary { { "data_type", "TEXT"    }, { "not_null",     true  } } },
                { "current_level", new Dictionary { { "data_type", "INTEGER" }, { "not_null",     true  }, { "default", 1 } } },
                { "total_score",   new Dictionary { { "data_type", "INTEGER" }, { "not_null",     true  }, { "default", 0 } } },
                { "collectables",  new Dictionary { { "data_type", "INTEGER" }, { "not_null",     true  }, { "default", 0 } } },
                { "created_at",    new Dictionary { { "data_type", "TEXT"    }, { "not_null",     true  } } },
                { "last_played",   new Dictionary { { "data_type", "TEXT"    }, { "not_null",     true  } } },
            };

            _db.Call("create_table", "game_sessions", tableDict);
            GD.Print("SessionManager: Database initialized at ", _dbPath);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr("SessionManager: Exception caught during InitDatabase: ", ex.ToString());
        }
    }

    // ─── CRUD ────────────────────────────────────────────────────────────────
    public int CreateNewSession(string characterName)
    {
        if (_db == null) InitDatabase();

        var now = Time.GetDatetimeStringFromSystem();
        var data = new Dictionary
        {
            { "character",     characterName },
            { "current_level", 1 },
            { "total_score",   0 },
            { "collectables",  0 },
            { "created_at",    now },
            { "last_played",   now },
        };

        _db.Call("insert_row", "game_sessions", data);

        var result = (Array)_db.Call(
            "select_rows", "game_sessions",
            "id = (SELECT MAX(id) FROM game_sessions)",
            new Array { "*" });

        if (result.Count > 0)
        {
            var row = (Dictionary)result[0];
            CurrentSessionId = row["id"].AsInt32();
            CurrentSessionData = GetSession(CurrentSessionId);
            GD.Print("SessionManager: Created new session with ID ", CurrentSessionId);
            return CurrentSessionId;
        }
        return -1;
    }

    public Array GetAllSessions()
    {
        if (_db == null) InitDatabase();

        var raw = (Array)_db.Call("select_rows", "game_sessions", "", new Array { "*" });

        // Sort by last_played descending (most recent first)
        var list = new System.Collections.Generic.List<Dictionary>();
        foreach (var item in raw)
            list.Add((Dictionary)item);

        list.Sort((a, b) => string.Compare(
            b["last_played"].AsString(),
            a["last_played"].AsString(),
            System.StringComparison.Ordinal));

        var sorted = new Array();
        foreach (var d in list)
            sorted.Add(d);
        return sorted;
    }

    public Dictionary GetSession(int sessionId)
    {
        if (_db == null) InitDatabase();

        var result = (Array)_db.Call(
            "select_rows", "game_sessions",
            "id = " + sessionId,
            new Array { "*" });

        return result.Count > 0 ? (Dictionary)result[0] : new Dictionary();
    }

    public bool LoadSession(int sessionId)
    {
        var data = GetSession(sessionId);
        if (data.Count == 0)
        {
            GD.Print("SessionManager: Session ", sessionId, " not found");
            return false;
        }
        CurrentSessionId = sessionId;
        CurrentSessionData = data;
        UpdateLastPlayed(sessionId);
        GD.Print("SessionManager: Loaded session ", sessionId);
        return true;
    }

    public void UpdateSession(int sessionId, int level, int score, int collectables)
    {
        if (_db == null) InitDatabase();

        var update = new Dictionary
        {
            { "current_level", level },
            { "total_score",   score },
            { "collectables",  collectables },
            { "last_played",   Time.GetDatetimeStringFromSystem() },
        };
        _db.Call("update_rows", "game_sessions", "id = " + sessionId, update);

        if (sessionId == CurrentSessionId)
            CurrentSessionData = GetSession(sessionId);

        GD.Print("SessionManager: Updated session ", sessionId);
    }

    public void UpdateLastPlayed(int sessionId)
    {
        if (_db == null) InitDatabase();
        var update = new Dictionary { { "last_played", Time.GetDatetimeStringFromSystem() } };
        _db.Call("update_rows", "game_sessions", "id = " + sessionId, update);
    }

    public void DeleteSession(int sessionId)
    {
        if (_db == null) InitDatabase();
        _db.Call("delete_rows", "game_sessions", "id = " + sessionId);
        if (sessionId == CurrentSessionId)
        {
            CurrentSessionId = -1;
            CurrentSessionData = new Dictionary();
        }
        GD.Print("SessionManager: Deleted session ", sessionId);
    }

    public void DeleteAllSessions()
    {
        if (_db == null) InitDatabase();
        _db.Call("delete_rows", "game_sessions", "");
        CurrentSessionId = -1;
        CurrentSessionData = new Dictionary();
        GD.Print("SessionManager: Deleted all sessions");
    }

    public bool HasSessions() => GetAllSessions().Count > 0;

    // ─── Formatting ──────────────────────────────────────────────────────────
    /// <summary>Trims an ISO datetime string to "YYYY-MM-DD HH:MM".</summary>
    public string FormatDatetime(string datetimeStr)
    {
        if (string.IsNullOrEmpty(datetimeStr)) return "Unknown";
        var formatted = datetimeStr.Replace("T", " ");
        var parts = formatted.Split(":");
        return parts.Length >= 2 ? parts[0] + ":" + parts[1] : formatted;
    }

    // ─── GDScript snake_case aliases (Godot auto-maps PascalCase→snake_case,
    //     but explicit aliases guarantee compatibility with remaining GDScript) ─
    public int create_new_session(string character_name) => CreateNewSession(character_name);
    public Array get_all_sessions() => GetAllSessions();
    public Dictionary get_session(int session_id) => GetSession(session_id);
    public bool load_session(int session_id) => LoadSession(session_id);
    public void update_session(int session_id, int level, int score, int collectables)
        => UpdateSession(session_id, level, score, collectables);
    public void delete_session(int session_id) => DeleteSession(session_id);
    public void delete_all_sessions() => DeleteAllSessions();
    public bool has_sessions() => HasSessions();
    public string format_datetime(string datetime_str) => FormatDatetime(datetime_str);
}

