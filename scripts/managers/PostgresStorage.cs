using System;
using System.Collections.Generic;
using System.IO;
using Npgsql;
using NpgsqlTypes;

/// <summary>PostgreSQL-only application state. JSON is stored in JSONB, never in local save files.</summary>
public static class PostgresStorage
{
    /// <summary>Builds the host client's connection from environment or the development .env; secrets are never logged.</summary>
    public static string ConnectionString
    {
        get
        {
            var values = new Dictionary<string, string>();
            // Configuration is read-only; exported games should receive credentials through their environment.
            string path = Path.Combine(AppContext.BaseDirectory, ".env");
            if (!File.Exists(path)) path = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(path))
                foreach (string line in File.ReadLines(path))
                {
                    string[] pair = line.Split('=', 2);
                    if (pair.Length == 2 && !pair[0].TrimStart().StartsWith('#'))
                        values[pair[0].Trim()] = pair[1].Trim().Trim('"', '\'');
                }
            // Environment values take priority over the read-only development configuration.
            string Read(string fallback, params string[] names)
            {
                foreach (string name in names)
                    if (Environment.GetEnvironmentVariable(name) is string value) return value;
                foreach (string name in names)
                    if (values.TryGetValue(name, out string value)) return value;
                return fallback;
            }
            return new NpgsqlConnectionStringBuilder
            {
                Host = Read("127.0.0.1", "PGHOST"),
                Port = int.Parse(Read("5432", "PGPORT")),
                Database = Read("accounts_db", "PGDATABASE", "POSTGRES_DB", "DB_NAME"),
                Username = Read("postgres", "PGUSER", "POSTGRES_USER", "DB_USER"),
                Password = Read("postgres", "PGPASSWORD", "POSTGRES_PASSWORD", "DB_PASSWORD"),
                Timeout = 3, CommandTimeout = 5
            }.ConnectionString;
        }
    }

    /// <summary>Opens the state store and creates its versioned document table. Failures propagate to callers.</summary>
    private static NpgsqlConnection Open()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            connection.Open();
            using var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS game_state (state_key text PRIMARY KEY, schema_version integer NOT NULL DEFAULT 1, value jsonb NOT NULL)", connection);
            command.ExecuteNonQuery();
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    /// <summary>Returns a stored JSON document, or null only when the key has never been saved.</summary>
    public static string? Read(string key)
    {
        using var connection = Open();
        using var command = new NpgsqlCommand("SELECT schema_version, value::text FROM game_state WHERE state_key=@key", connection);
        command.Parameters.AddWithValue("key", key);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        if (reader.GetInt32(0) != 1) throw new InvalidOperationException("Unsupported database state version.");
        return reader.GetString(1);
    }

    /// <summary>Atomically replaces one state document; database failure never triggers a disk fallback.</summary>
    public static void Write(string key, string json)
    {
        using var connection = Open();
        using var command = new NpgsqlCommand("INSERT INTO game_state(state_key,value) VALUES (@key,@value) ON CONFLICT(state_key) DO UPDATE SET value=EXCLUDED.value WHERE game_state.schema_version=1", connection);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("value", NpgsqlDbType.Jsonb, json);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Unsupported database state version; document was not overwritten.");
    }
}
