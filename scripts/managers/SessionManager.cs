using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Npgsql;

using System.Threading.Tasks;

/// <summary>
/// SessionManager is a singleton autoload that manages game sessions (user progress, scores, and character data).
/// PostgreSQL is the only persistent store; database failures are reported without a local fallback.
/// </summary>
public partial class SessionManager : Node
{
	public static SessionManager Instance { get; private set; }

	private static string PgConnectionString => PostgresStorage.ConnectionString;

	/// <summary>Most recent database failure; empty after a successful initialization.</summary>
	public string LastStorageError { get; private set; } = "";

	private bool _isPostgresAvailable = false;

	[Export] public int CurrentSessionId { get; set; } = -1;
	public Dictionary CurrentSessionData { get; set; } = new Dictionary();

	/// <summary>Registers the singleton and verifies storage before other autoloads read settings.</summary>
	public override void _Ready()
	{
		Instance = this;
		InitDatabaseAsync().GetAwaiter().GetResult();
	}

	/// <summary>Verifies the database and creates the session table; failures disable storage until a retry.</summary>
	public async Task InitDatabaseAsync()
	{
		GD.Print("SessionManager: Testing PostgreSQL connection...");
		try
		{
			using var conn = new NpgsqlConnection(PgConnectionString);
			await conn.OpenAsync().ConfigureAwait(false);
			_isPostgresAvailable = true;
			LastStorageError = "";
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
			await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
			GD.Print("SessionManager: PostgreSQL table 'game_sessions' verified/created.");
		}
		catch (Exception ex)
		{
			_isPostgresAvailable = false;
			LastStorageError = "PostgreSQL is unavailable. Start the server and try again.";
			GD.PrintErr($"SessionManager: {LastStorageError} ({ex.GetType().Name})");
		}
	}

	/// <summary>Creates a committed campaign save and selects it; returns -1 on database failure.</summary>
	public async Task<int> CreateNewSessionAsync(string characterName)
	{
		string now = Time.GetDatetimeStringFromSystem();

		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);
				string sql = @"
					INSERT INTO game_sessions (character, current_level, total_score, collectables, created_at, last_played)
					VALUES (@char, 1, 0, 0, @created, @last)
					RETURNING id;";

				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("char", characterName);
				cmd.Parameters.AddWithValue("created", now);
				cmd.Parameters.AddWithValue("last", now);

				int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync().ConfigureAwait(false));
				CurrentSessionId = newId;
				CurrentSessionData = await GetSessionAsync(newId).ConfigureAwait(false);
				GD.Print($"SessionManager: Created new PostgreSQL session ID {newId}");
				return newId;
			}
			catch (Exception ex)
			{

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error creating PostgreSQL session: {ex.GetType().Name}");
			}
		}

		return -1;
	}

	/// <summary>Lists persisted campaigns, newest first; returns an empty array on failure.</summary>
	public async Task<Godot.Collections.Array> GetAllSessionsAsync()
	{
		var result = new Godot.Collections.Array();

		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);
				string sql = "SELECT * FROM game_sessions ORDER BY last_played DESC;";
				using var cmd = new NpgsqlCommand(sql, conn);
				using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

				while (await reader.ReadAsync().ConfigureAwait(false))
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

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error fetching PostgreSQL sessions: {ex.GetType().Name}");
			}
		}

		return result;
	}

	/// <summary>Loads one campaign by id; returns an empty dictionary when absent or unavailable.</summary>
	public async Task<Dictionary> GetSessionAsync(int sessionId)
	{
		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);
				string sql = "SELECT * FROM game_sessions WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("id", sessionId);
				using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

				if (await reader.ReadAsync().ConfigureAwait(false))
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

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error getting PostgreSQL session: {ex.GetType().Name}");
			}
		}

		return new Dictionary();
	}

	/// <summary>Selects an existing saved campaign and updates its last-played timestamp.</summary>
	public async Task<bool> LoadSessionAsync(int sessionId)
	{
		var data = await GetSessionAsync(sessionId).ConfigureAwait(false);
		if (data.Count == 0) return false;

		CurrentSessionId = sessionId;
		CurrentSessionData = data;
		await UpdateLastPlayedAsync(sessionId).ConfigureAwait(false);
		return true;
	}

	/// <summary>Atomically adds score and collectibles while advancing the saved mission.</summary>
	public async Task UpdateSessionAsync(int sessionId, int level, int scoreIncrement, int collectablesIncrement)
	{
		string now = Time.GetDatetimeStringFromSystem();

		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);

				string sql = @"
					UPDATE game_sessions
					SET current_level = GREATEST(current_level, @level), total_score = total_score + @score, collectables = collectables + @coll, last_played = @last
					WHERE id = @id;";

				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("level", level);
				cmd.Parameters.AddWithValue("score", scoreIncrement);
				cmd.Parameters.AddWithValue("coll", collectablesIncrement);
				cmd.Parameters.AddWithValue("last", now);
				cmd.Parameters.AddWithValue("id", sessionId);

				await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

				if (sessionId == CurrentSessionId)
					CurrentSessionData = await GetSessionAsync(sessionId).ConfigureAwait(false);
				return;
			}
			catch (Exception ex)
			{

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error updating PostgreSQL session: {ex.GetType().Name}");
			}
		}


	}

	/// <summary>Touches the saved campaign timestamp without modifying progress.</summary>
	public async Task UpdateLastPlayedAsync(int sessionId)
	{
		string now = Time.GetDatetimeStringFromSystem();
		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);
				string sql = "UPDATE game_sessions SET last_played = @last WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("last", now);
				cmd.Parameters.AddWithValue("id", sessionId);
				await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error updating PostgreSQL last_played: {ex.GetType().Name}");
			}
		}
	}

	/// <summary>Deletes a campaign and clears the active selection only after the database confirms deletion.</summary>
	public async Task DeleteSessionAsync(int sessionId)
	{
		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);
				string sql = "DELETE FROM game_sessions WHERE id = @id;";
				using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("id", sessionId);
				await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

				if (sessionId == CurrentSessionId)
				{
					CurrentSessionId = -1;
					CurrentSessionData = new Dictionary();
				}
				return;
			}
			catch (Exception ex)
			{

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error deleting PostgreSQL session: {ex.GetType().Name}");
			}
		}


	}

	/// <summary>Deletes all campaigns and clears the active runtime selection after success.</summary>
	public async Task DeleteAllSessionsAsync()
	{
		if (!_isPostgresAvailable) await InitDatabaseAsync().ConfigureAwait(false);
		if (_isPostgresAvailable)
		{
			try
			{
				using var conn = new NpgsqlConnection(PgConnectionString);
				await conn.OpenAsync().ConfigureAwait(false);
				string sql = "DELETE FROM game_sessions;";
				using var cmd = new NpgsqlCommand(sql, conn);
				await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

				CurrentSessionId = -1;
				CurrentSessionData = new Dictionary();
				return;
			}
			catch (Exception ex)
			{

				_isPostgresAvailable = false;
				LastStorageError = "PostgreSQL operation failed; no changes were saved locally.";
				GD.PrintErr($"SessionManager: Error deleting all PostgreSQL sessions: {ex.GetType().Name}");
			}
		}


	}

	/// <summary>Checks whether PostgreSQL has any campaigns for the Continue menu.</summary>
	public bool HasSessions() => GetAllSessionsAsync().GetAwaiter().GetResult().Count > 0;

	/// <summary>Formats a saved ISO timestamp to minute precision for the menu.</summary>
	public string FormatDatetime(string datetimeStr)
	{
		if (string.IsNullOrEmpty(datetimeStr)) return "Unknown";
		var formatted = datetimeStr.Replace("T", " ");
		var parts = formatted.Split(":");
		return parts.Length >= 2 ? parts[0] + ":" + parts[1] : formatted;
	}

	/// <summary>Loads a settings document for GDScript; failures are surfaced without reading a local file.</summary>
	public Dictionary ReadState(string key)
	{
		try { return Json.ParseString(PostgresStorage.Read(key) ?? "{}").AsGodotDictionary(); }
		catch (Exception ex) { GD.PrintErr($"PostgreSQL settings load failed ({ex.GetType().Name})."); return new Dictionary(); }
	}

	/// <summary>Commits settings to PostgreSQL; returns false when they could not be saved.</summary>
	public bool WriteState(string key, Dictionary value)
	{
		try { PostgresStorage.Write(key, Json.Stringify(value)); return true; }
		catch (Exception ex) { GD.PrintErr($"PostgreSQL settings save failed ({ex.GetType().Name})."); return false; }
	}

	// ─── GDScript snake_case aliases ─────────────────────────────────────────
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public int create_new_session(string character_name) => CreateNewSessionAsync(character_name).GetAwaiter().GetResult();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public Godot.Collections.Array get_all_sessions() => GetAllSessionsAsync().GetAwaiter().GetResult();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public Dictionary get_session(int session_id) => GetSessionAsync(session_id).GetAwaiter().GetResult();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public bool load_session(int session_id) => LoadSessionAsync(session_id).GetAwaiter().GetResult();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public void update_session(int session_id, int level, int score, int collectables)
		=> UpdateSessionAsync(session_id, level, score, collectables).Wait();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public void delete_session(int session_id) => DeleteSessionAsync(session_id).Wait();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public void delete_all_sessions() => DeleteAllSessionsAsync().Wait();
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public bool has_sessions() => GetAllSessionsAsync().GetAwaiter().GetResult().Count > 0;
	/// <summary>GDScript compatibility entry point for PostgreSQL session operations.</summary>
	public string format_datetime(string datetime_str) => FormatDatetime(datetime_str);
}
