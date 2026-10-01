using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Npgsql;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// SessionManager is a singleton autoload that manages game sessions (user progress, scores, and character data).
/// It implements a dual-storage strategy:
/// 1. Primary: PostgreSQL database via Npgsql for centralized, online storage.
/// 2. Fallback: Local JSON file in the user directory for offline persistence.
/// </summary>
public partial class SessionManager : Node
{
	public static SessionManager Instance { get; private set; }

	private static string PgConnectionString => BuildConnectionString();
	private const string JsonBackupPath = "user://game_sessions.json";

	private static string BuildConnectionString()
	{
		string host = System.Environment.GetEnvironmentVariable("PGHOST") ?? "127.0.0.1";
		string port = System.Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
		string db = System.Environment.GetEnvironmentVariable("PGDATABASE") ?? "accounts_db";
		string user = System.Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
		string pass = System.Environment.GetEnvironmentVariable("PGPASSWORD") ?? "Swathus@21";
		return $"Host={host};Port={port};Database={db};Username={user};Password={pass};Timeout=3;";
	}

	private bool _isPostgresAvailable = false;

	[Export] public int CurrentSessionId { get; set; } = -1;
	public Dictionary CurrentSessionData { get; set; } = new Dictionary();

	public override async void _Ready()
	{
		Instance = this;
		await InitDatabaseAsync();
	}

	public async Task InitDatabaseAsync()
	{
		GD.Print("SessionManager: Testing PostgreSQL connection...");
		try
		{
			using var conn = new NpgsqlConnection(PgConnectionString);
			await conn.OpenAsync();
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
			await cmd.ExecuteNonQueryAsync();
			GD.Print("SessionManager: PostgreSQL table 'game_sessions' verified/created.");
		}
		catch (Exception ex)
		{
			_isPostgresAvailable = false;
			GD.PrintErr($"SessionManager: PostgreSQL connection offline or unavailable ({ex.Message}). Using local JSON fallback: {JsonBackupPath}");
		}
	}

	public async Task<int> CreateNewSessionAsync(string characterName)
	{
		string now = Time.GetDatetimeStringFromSystem();

		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();
				string sql = @"
					INSERT INTO game_sessions (character, current_level, total_score, collectables, created_at, last_played)
					VALUES (@char, 1, 0, 0, @created, @last)
					RETURNING id;";

				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("char", characterName);
				cmd.Parameters.AddWithValue("created", now);
				cmd.Parameters.AddWithValue("last", now);

				int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
				CurrentSessionId = newId;
				CurrentSessionData = await GetSessionAsync(newId);
				GD.Print($"SessionManager: Created new PostgreSQL session ID {newId}");
				return newId;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error creating PostgreSQL session: {ex.Message}");
			}
		}

		return CreateJsonSession(characterName, now);
	}

	public async Task<Godot.Collections.Array> GetAllSessionsAsync()
	{
		var result = new Godot.Collections.Array();

		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();
				string sql = "SELECT * FROM game_sessions ORDER BY last_played DESC;";
				using var cmd = new NpgsqlCommand(sql, conn);
				using var reader = await cmd.ExecuteReaderAsync();

				while (await reader.ReadAsync())
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

	public async Task<Dictionary> GetSessionAsync(int sessionId)
	{
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();
				string sql = "SELECT * FROM game_sessions WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("id", sessionId);
				using var reader = await cmd.ExecuteReaderAsync();

				if (await reader.ReadAsync())
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

	public async Task<bool> LoadSessionAsync(int sessionId)
	{
		var data = await GetSessionAsync(sessionId);
		if (data.Count == 0) return false;

		CurrentSessionId = sessionId;
		CurrentSessionData = data;
		await UpdateLastPlayedAsync(sessionId);
		return true;
	}

	public async Task UpdateSessionAsync(int sessionId, int level, int scoreIncrement, int collectablesIncrement)
	{
		string now = Time.GetDatetimeStringFromSystem();
		int totalScore = 0;
		int totalCollectables = 0;

		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();

				string fetchSql = "SELECT total_score, collectables FROM game_sessions WHERE id = @id;";
				using var fetchCmd = new NpgsqlCommand(fetchSql, conn);
				fetchCmd.Parameters.AddWithValue("id", sessionId);
				using var reader = await fetchCmd.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					totalScore = reader.GetInt32(0);
					totalCollectables = reader.GetInt32(1);
				}
				await reader.DisposeAsync();

				string sql = @"
					UPDATE game_sessions
					SET current_level = @level, total_score = @score, collectables = @coll, last_played = @last
					WHERE id = @id;";

				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("level", level);
				cmd.Parameters.AddWithValue("score", totalScore + scoreIncrement);
				cmd.Parameters.AddWithValue("coll", totalCollectables + collectablesIncrement);
				cmd.Parameters.AddWithValue("last", now);
				cmd.Parameters.AddWithValue("id", sessionId);

				await cmd.ExecuteNonQueryAsync();

				if (sessionId == CurrentSessionId)
					CurrentSessionData = await GetSessionAsync(sessionId);
				return;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error updating PostgreSQL session: {ex.Message}");
			}
		}

		UpdateJsonSession(sessionId, level, scoreIncrement, collectablesIncrement, now);
	}

	public async Task UpdateLastPlayedAsync(int sessionId)
	{
		string now = Time.GetDatetimeStringFromSystem();
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();
				string sql = "UPDATE game_sessions SET last_played = @last WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("last", now);
				cmd.Parameters.AddWithValue("id", sessionId);
				await cmd.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				GD.PrintErr($"SessionManager: Error updating PostgreSQL last_played: {ex.Message}");
			}
		}
	}

	public async Task DeleteSessionAsync(int sessionId)
	{
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();
				string sql = "DELETE FROM game_sessions WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("id", sessionId);
				await cmd.ExecuteNonQueryAsync();

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

	public async Task DeleteAllSessionsAsync()
	{
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync();
				string sql = "DELETE FROM game_sessions;";
				using var cmd = new NpgsqlCommand(sql, conn);
				await cmd.ExecuteNonQueryAsync();

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

	public bool HasSessions() => GetAllSessionsAsync().GetAwaiter().GetResult().Count > 0;

	public string FormatDatetime(string datetimeStr)
	{
		if (string.IsNullOrEmpty(datetimeStr)) return "Unknown";
		var formatted = datetimeStr.Replace("T", " ");
		var parts = formatted.Split(":");
		return parts.Length >= 2 ? parts[0] + ":" + parts[1] : formatted;
	}

	// ─── JSON Local Storage Fallback Helpers ─────────────────────────────────

	private int ToInt(object val)
	{
		if (val == null) return 0;
		if (val is System.Text.Json.JsonElement elem)
		{
			if (elem.ValueKind == System.Text.Json.JsonValueKind.Number && elem.TryGetInt32(out int i))
				return i;
			if (int.TryParse(elem.ToString(), out int parsed))
				return parsed;
			return 0;
		}
		try { return Convert.ToInt32(val); } catch { return 0; }
	}

	private string ToStringVal(object val)
	{
		if (val == null) return "";
		if (val is System.Text.Json.JsonElement elem) return elem.ToString() ?? "";
		return val.ToString() ?? "";
	}

	private int CreateJsonSession(string characterName, string now)
	{
		var sessions = LoadJsonList();
		int maxId = 0;
		foreach (var s in sessions)
		{
			if (s.ContainsKey("id") && ToInt(s["id"]) > maxId)
				maxId = ToInt(s["id"]);
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
				{ "id", ToInt(s["id"]) },
				{ "character", ToStringVal(s["character"]) },
				{ "current_level", ToInt(s["current_level"]) },
				{ "total_score", ToInt(s["total_score"]) },
				{ "collectables", ToInt(s["collectables"]) },
				{ "created_at", ToStringVal(s["created_at"]) },
				{ "last_played", ToStringVal(s["last_played"]) }
			});
		}
		return result;
	}

	private Dictionary GetJsonSession(int sessionId)
	{
		var sessions = LoadJsonList();
		foreach (var s in sessions)
		{
			if (ToInt(s["id"]) == sessionId)
			{
				return new Dictionary
				{
					{ "id", ToInt(s["id"]) },
					{ "character", ToStringVal(s["character"]) },
					{ "current_level", ToInt(s["current_level"]) },
					{ "total_score", ToInt(s["total_score"]) },
					{ "collectables", ToInt(s["collectables"]) },
					{ "created_at", ToStringVal(s["created_at"]) },
					{ "last_played", ToStringVal(s["last_played"]) }
				};
			}
		}
		return new Dictionary();
	}

	private void UpdateJsonSession(int sessionId, int level, int scoreIncrement, int collectablesIncrement, string now)
	{
		var sessions = LoadJsonList();
		foreach (var s in sessions)
		{
			if (ToInt(s["id"]) == sessionId)
			{
				int currentScore = ToInt(s["total_score"]);
				int currentColl = ToInt(s["collectables"]);
				s["current_level"] = level;
				s["total_score"] = currentScore + scoreIncrement;
				s["collectables"] = currentColl + collectablesIncrement;
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
		sessions.RemoveAll(s => ToInt(s["id"]) == sessionId);
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
	public int create_new_session(string character_name) => CreateNewSessionAsync(character_name).GetAwaiter().GetResult();
	public Godot.Collections.Array get_all_sessions() => GetAllSessionsAsync().GetAwaiter().GetResult();
	public Dictionary get_session(int session_id) => GetSessionAsync(session_id).GetAwaiter().GetResult();
	public bool load_session(int session_id) => LoadSessionAsync(session_id).GetAwaiter().GetResult();
	public void update_session(int session_id, int level, int score, int collectables)
		=> UpdateSessionAsync(session_id, level, score, collectables).Wait();
	public void delete_session(int session_id) => DeleteSessionAsync(session_id).Wait();
	public void delete_all_sessions() => DeleteAllSessionsAsync().Wait();
	public bool has_sessions() => GetAllSessionsAsync().GetAwaiter().GetResult().Count > 0;
	public string format_datetime(string datetime_str) => FormatDatetime(datetime_str);
}
