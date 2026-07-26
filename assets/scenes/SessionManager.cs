using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Npgsql;
using System.Text.Json;

/// <summary>
/// Singleton autoload for managing game sessions using PostgreSQL (Npgsql)
/// with seamless offline JSON fallback (user://game_sessions.json).
/// </summary>
public partial class SessionManager : Node
{
	public static SessionManager Instance { get; private set; }

	// PostgreSQL Connection String
	private const string PgConnectionString = "Host=127.0.0.1;Port=5432;Database=accounts_db;Username=postgres;Password=postgres;Timeout=3;";
	private const string JsonBackupPath = "user://game_sessions.json";

	private bool _isPostgresAvailable = false;

	[Export] public int CurrentSessionId { get; set; } = -1;
	public Dictionary CurrentSessionData { get; set; } = new Dictionary();

	public override void _Ready()
	{
		GD.Print("SessionManager: _ready() start");
		Instance = this;
		try
		{
			InitDatabase();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"SessionManager: CRITICAL ERROR in _ready(): {ex.Message}\n{ex.StackTrace}");
		}
		GD.Print("SessionManager: _ready() complete");
	}

	public void InitDatabase()
	{
		GD.Print("SessionManager: Testing PostgreSQL connection...");
		try
		{
			using var conn = new NpgsqlConnection(PgConnectionString);
			conn.Open();
			_isPostgresAvailable = true;
			GD.Print("SessionManager: Connected to PostgreSQL server successfully!");

			string createTableSql = @"
				CREATE TABLE IF NOT EXISTS game_sessions (
					id SERIAL PRIMARY KEY,
					character VARCHAR(100) NOT NULL,
					current_level INT NOT NULL DEFAULT 1,
					total_score INT NOT NULL DEFAULT 0,
					collectables INT NOT NULL DEFAULT 0,
					created_at VARCHAR(100) NOT NULL,
					last_played VARCHAR(100) NOT NULL
				);";

			using var cmd = new NpgsqlCommand(createTableSql, conn);
			cmd.ExecuteNonQuery();
			GD.Print("SessionManager: PostgreSQL table 'game_sessions' verified/created.");
		}
		catch (Exception ex)
		{
			_isPostgresAvailable = false;
			GD.PrintErr($"SessionManager: PostgreSQL connection offline or unavailable ({ex.Message}). Using local JSON fallback: {JsonBackupPath}");
		}
	}

	public int CreateNewSession(string characterName)
	{
		string now = Time.GetDatetimeStringFromSystem();

		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = @"
					INSERT INTO game_sessions (character, current_level, total_score, collectables, created_at, last_played)
					VALUES (@char, 1, 0, 0, @created, @last)
					RETURNING id;";

				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("char", characterName);
				cmd.Parameters.AddWithValue("created", now);
				cmd.Parameters.AddWithValue("last", now);

				int newId = Convert.ToInt32(cmd.ExecuteScalar());
				CurrentSessionId = newId;
				CurrentSessionData = GetSession(newId);
				GD.Print($"SessionManager: Created new PostgreSQL session ID {newId}");
				return newId;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error creating PostgreSQL session: {ex.Message}");
			}
		}

		// Fallback to JSON
		return CreateJsonSession(characterName, now);
	}

	public Godot.Collections.Array GetAllSessions()
	{
		var result = new Godot.Collections.Array();

		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = "SELECT * FROM game_sessions ORDER BY last_played DESC;";
				using var cmd = new NpgsqlCommand(sql, conn);
				using var reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					var row = new Dictionary
					{
						{ "id", reader.GetInt32(0) },
						{ "character", reader.GetString(1) },
						{ "current_level", reader.GetInt32(2) },
						{ "total_score", reader.GetInt32(3) },
						{ "collectables", reader.GetInt32(4) },
						{ "created_at", reader.GetString(5) },
						{ "last_played", reader.GetString(6) }
					};
					result.Add(row);
				}
				return result;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error fetching PostgreSQL sessions: {ex.Message}");
			}
		}

		return GetAllJsonSessions();
	}

	public Dictionary GetSession(int sessionId)
	{
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = "SELECT * FROM game_sessions WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("id", sessionId);
				using var reader = cmd.ExecuteReader();

				if (reader.Read())
				{
					return new Dictionary
					{
						{ "id", reader.GetInt32(0) },
						{ "character", reader.GetString(1) },
						{ "current_level", reader.GetInt32(2) },
						{ "total_score", reader.GetInt32(3) },
						{ "collectables", reader.GetInt32(4) },
						{ "created_at", reader.GetString(5) },
						{ "last_played", reader.GetString(6) }
					};
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error getting PostgreSQL session: {ex.Message}");
			}
		}

		return GetJsonSession(sessionId);
	}

	public bool LoadSession(int sessionId)
	{
		var data = GetSession(sessionId);
		if (data.Count == 0) return false;

		CurrentSessionId = sessionId;
		CurrentSessionData = data;
		UpdateLastPlayed(sessionId);
		return true;
	}

	public void UpdateSession(int sessionId, int level, int score, int collectables)
	{
		string now = Time.GetDatetimeStringFromSystem();

		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = @"
					UPDATE game_sessions
					SET current_level = @level, total_score = @score, collectables = @coll, last_played = @last
					WHERE id = @id;";

				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("level", level);
				cmd.Parameters.AddWithValue("score", score);
				cmd.Parameters.AddWithValue("coll", collectables);
				cmd.Parameters.AddWithValue("last", now);
				cmd.Parameters.AddWithValue("id", sessionId);

				cmd.ExecuteNonQuery();

				if (sessionId == CurrentSessionId)
					CurrentSessionData = GetSession(sessionId);
				return;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error updating PostgreSQL session: {ex.Message}");
			}
		}

		UpdateJsonSession(sessionId, level, score, collectables, now);
	}

	public void UpdateLastPlayed(int sessionId)
	{
		string now = Time.GetDatetimeStringFromSystem();
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = "UPDATE game_sessions SET last_played = @last WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("last", now);
				cmd.Parameters.AddWithValue("id", sessionId);
				cmd.ExecuteNonQuery();
				return;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error updating PostgreSQL last_played: {ex.Message}");
			}
		}
	}

	public void DeleteSession(int sessionId)
	{
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = "DELETE FROM game_sessions WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("id", sessionId);
				cmd.ExecuteNonQuery();

				if (sessionId == CurrentSessionId)
				{
					CurrentSessionId = -1;
					CurrentSessionData = new Dictionary();
				}
				return;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error deleting PostgreSQL session: {ex.Message}");
			}
		}

		DeleteJsonSession(sessionId);
	}

	public void DeleteAllSessions()
	{
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				conn.Open();
				string sql = "DELETE FROM game_sessions;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.ExecuteNonQuery();

				CurrentSessionId = -1;
				CurrentSessionData = new Dictionary();
				return;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error deleting all PostgreSQL sessions: {ex.Message}");
			}
		}

		DeleteAllJsonSessions();
	}

	public bool HasSessions() => GetAllSessions().Count > 0;

	public string FormatDatetime(string datetimeStr)
	{
		if (string.IsNullOrEmpty(datetimeStr)) return "Unknown";
		var formatted = datetimeStr.Replace("T", " ");
		var parts = formatted.Split(":");
		return parts.Length >= 2 ? parts[0] + ":" + parts[1] : formatted;
	}

	// ─── JSON Local Storage Fallback Helpers ─────────────────────────────────
	private int CreateJsonSession(string characterName, string now)
	{
		var sessions = LoadJsonList();
		int maxId = 0;
		foreach (var s in sessions)
		{
			if (s.ContainsKey("id") && Convert.ToInt32(s["id"]) > maxId)
				maxId = Convert.ToInt32(s["id"]);
		}
		int newId = maxId + 1;

		var newSession = new System.Collections.Generic.Dictionary<string, object>
		{
			{ "id", newId },
			{ "character", characterName },
			{ "current_level", 1 },
			{ "total_score", 0 },
			{ "collectables", 0 },
			{ "created_at", now },
			{ "last_played", now }
		};

		sessions.Add(newSession);
		SaveJsonList(sessions);

		CurrentSessionId = newId;
		CurrentSessionData = GetJsonSession(newId);
		return newId;
	}

	private Godot.Collections.Array GetAllJsonSessions()
	{
		var sessions = LoadJsonList();
		var result = new Godot.Collections.Array();
		foreach (var s in sessions)
		{
			result.Add(new Dictionary
			{
				{ "id", Convert.ToInt32(s["id"]) },
				{ "character", s["character"]?.ToString() ?? "" },
				{ "current_level", Convert.ToInt32(s["current_level"]) },
				{ "total_score", Convert.ToInt32(s["total_score"]) },
				{ "collectables", Convert.ToInt32(s["collectables"]) },
				{ "created_at", s["created_at"]?.ToString() ?? "" },
				{ "last_played", s["last_played"]?.ToString() ?? "" }
			});
		}
		return result;
	}

	private Dictionary GetJsonSession(int sessionId)
	{
		var sessions = LoadJsonList();
		foreach (var s in sessions)
		{
			if (Convert.ToInt32(s["id"]) == sessionId)
			{
				return new Dictionary
				{
					{ "id", Convert.ToInt32(s["id"]) },
					{ "character", s["character"]?.ToString() ?? "" },
					{ "current_level", Convert.ToInt32(s["current_level"]) },
					{ "total_score", Convert.ToInt32(s["total_score"]) },
					{ "collectables", Convert.ToInt32(s["collectables"]) },
					{ "created_at", s["created_at"]?.ToString() ?? "" },
					{ "last_played", s["last_played"]?.ToString() ?? "" }
				};
			}
		}
		return new Dictionary();
	}

	private void UpdateJsonSession(int sessionId, int level, int score, int collectables, string now)
	{
		var sessions = LoadJsonList();
		foreach (var s in sessions)
		{
			if (Convert.ToInt32(s["id"]) == sessionId)
			{
				s["current_level"] = level;
				s["total_score"] = score;
				s["collectables"] = collectables;
				s["last_played"] = now;
				break;
			}
		}
		SaveJsonList(sessions);
		if (sessionId == CurrentSessionId)
			CurrentSessionData = GetJsonSession(sessionId);
	}

	private void DeleteJsonSession(int sessionId)
	{
		var sessions = LoadJsonList();
		sessions.RemoveAll(s => Convert.ToInt32(s["id"]) == sessionId);
		SaveJsonList(sessions);
		if (sessionId == CurrentSessionId)
		{
			CurrentSessionId = -1;
			CurrentSessionData = new Dictionary();
		}
	}

	private void DeleteAllJsonSessions()
	{
		SaveJsonList(new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>());
		CurrentSessionId = -1;
		CurrentSessionData = new Dictionary();
	}

	private System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> LoadJsonList()
	{
		if (!FileAccess.FileExists(JsonBackupPath))
			return new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();

		try
		{
			using var file = FileAccess.Open(JsonBackupPath, FileAccess.ModeFlags.Read);
			string text = file.GetAsText();
			if (string.IsNullOrWhiteSpace(text)) return new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
			return JsonSerializer.Deserialize<System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>>(text)
				?? new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
		}
		catch
		{
			return new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
		}
	}

	private void SaveJsonList(System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> list)
	{
		try
		{
			using var file = FileAccess.Open(JsonBackupPath, FileAccess.ModeFlags.Write);
			string json = JsonSerializer.Serialize(list);
			file.StoreString(json);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"SessionManager: Failed to save JSON session backup: {ex.Message}");
		}
	}

	// ─── GDScript snake_case aliases ─────────────────────────────────────────
	public int create_new_session(string character_name) => CreateNewSession(character_name);
	public Godot.Collections.Array get_all_sessions() => GetAllSessions();
	public Dictionary get_session(int session_id) => GetSession(session_id);
	public bool load_session(int session_id) => LoadSession(session_id);
	public void update_session(int session_id, int level, int score, int collectables)
		=> UpdateSession(session_id, level, score, collectables);
	public void delete_session(int session_id) => DeleteSession(session_id);
	public void delete_all_sessions() => DeleteAllSessions();
	public bool has_sessions() => HasSessions();
	public string format_datetime(string datetime_str) => FormatDatetime(datetime_str);
}
