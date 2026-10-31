using Npgsql;

/// <summary>Checks outage behavior without requiring a second database or touching player saves.</summary>
[NonParallelizable]
public class PostgresStorageTests
{
    /// <summary>JSONB survives connection changes; a newer document cannot be read as defaults or overwritten.</summary>
    [Test]
    public void JsonStateRoundTripsAndRejectsUnknownVersions()
    {
        string previousDirectory = Directory.GetCurrentDirectory();
        var project = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (project != null && !File.Exists(Path.Combine(project.FullName, "project.godot")))
            project = project.Parent;
        Assert.That(project, Is.Not.Null);
        Directory.SetCurrentDirectory(project!.FullName);
        string key = "integration:" + Guid.NewGuid();
        try
        {
            PostgresStorage.Write(key, "{\"display_mode\":\"Windowed\",\"keys\":[65,68]}");
            using var document = System.Text.Json.JsonDocument.Parse(PostgresStorage.Read(key)!);
            Assert.That(document.RootElement.GetProperty("display_mode").GetString(), Is.EqualTo("Windowed"));
            Assert.That(document.RootElement.GetProperty("keys")[1].GetInt32(), Is.EqualTo(68));
            using var connection = new NpgsqlConnection(PostgresStorage.ConnectionString);
            connection.Open();
            using var command = new NpgsqlCommand("UPDATE game_state SET schema_version=2 WHERE state_key=@key", connection);
            command.Parameters.AddWithValue("key", key);
            command.ExecuteNonQuery();
            Assert.Throws<InvalidOperationException>(() => PostgresStorage.Read(key));
            Assert.Throws<InvalidOperationException>(() => PostgresStorage.Write(key, "{}"));
        }
        finally
        {
            try
            {
                using var connection = new NpgsqlConnection(PostgresStorage.ConnectionString);
                connection.Open();
                using var command = new NpgsqlCommand("DELETE FROM game_state WHERE state_key=@key", connection);
                command.Parameters.AddWithValue("key", key);
                command.ExecuteNonQuery();
            }
            finally { Directory.SetCurrentDirectory(previousDirectory); }
        }
    }

    /// <summary>Connection refusal propagates for reads and writes instead of producing a local save.</summary>
    [Test]
    public void DatabaseOutageNeverBecomesSuccessfulLocalPersistence()
    {
        string? host = Environment.GetEnvironmentVariable("PGHOST");
        string? port = Environment.GetEnvironmentVariable("PGPORT");
        try
        {
            Environment.SetEnvironmentVariable("PGHOST", "127.0.0.1");
            Environment.SetEnvironmentVariable("PGPORT", "1");
            Assert.Throws<NpgsqlException>(() => PostgresStorage.Read("outage-test"));
            Assert.Throws<NpgsqlException>(() => PostgresStorage.Write("outage-test", "{}"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PGHOST", host);
            Environment.SetEnvironmentVariable("PGPORT", port);
        }
    }
}
