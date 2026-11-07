using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

/// <summary>Versioned PostgreSQL exploration snapshot. Campaign ID stays in the database key, never shared static state.</summary>
public sealed record WorldSave(int Version, string World, WorldPoint Position, WorldPoint? Waypoint,
    WorldPoint LastExtraction, string Resume, int Mission, string Operative,
    Dictionary<string, string?>? CompletedQuests = null,
    Dictionary<string, int>? ActiveQuests = null,
    Dictionary<int, string>? DistrictChoices = null,
    HashSet<string>? UnlockedVehicles = null,
    string? ActiveVehicle = null,
    double WorldTimeMinutes = 0.0,
    Dictionary<string, int>? FactionReputation = null,
    HashSet<string>? UnlockedHqAmenities = null) : IEquatable<WorldSave>
{
    /// <summary>Creates a safe post-mission arrival; completion, including Mission 12, always resumes exploration.</summary>
    public static WorldSave AfterMission(int mission, string operative=OperativeRoster.Starter)
    {
        var entrance=WorldCatalog.Entrances[LevelCatalog.Clamp(mission)-1].Position;
        var safe=WorldCatalog.NearestStation(entrance).Position;
        return new(1,WorldCatalog.Identity,safe,null,safe,"world",LevelCatalog.Clamp(mission),OperativeRoster.Normalize(operative),
            new Dictionary<string, string?>(), new Dictionary<string, int>(), new Dictionary<int, string>(), new HashSet<string> { "veh_starter_bike" }, null, 0.0,
            new Dictionary<string, int>(), new HashSet<string>());
    }

    /// <summary>Rejects unsupported identities, unsafe coordinates and unknown modes instead of overwriting future saves.</summary>
    public bool Valid() => Version==1 && World==WorldCatalog.Identity && WorldCatalog.InBounds(Position)
        && WorldCatalog.InBounds(LastExtraction) && (Waypoint==null || WorldCatalog.InBounds(Waypoint.Value))
        && Resume is "world" or "mission" && Mission>=1 && Mission<=LevelCatalog.Count
        && OperativeRoster.RosterOrder.Contains(Operative);

    /// <summary>Returns a copy of the state with a quest marked as completed, updating any chosen branch.</summary>
    public WorldSave WithQuestCompleted(string questId, string? branchChoice)
    {
        var completed = CompletedQuests != null ? new Dictionary<string, string?>(CompletedQuests) : new();
        var active = ActiveQuests != null ? new Dictionary<string, int>(ActiveQuests) : new();
        completed[questId] = branchChoice;
        active.Remove(questId);

        var choices = DistrictChoices != null ? new Dictionary<int, string>(DistrictChoices) : new();
        if (!string.IsNullOrEmpty(branchChoice))
        {
            var quest = Array.Find(WorldContentCatalog.Quests as QuestDefinition[] ?? Array.Empty<QuestDefinition>(), q => q.Id == questId);
            if (quest != null) choices[quest.RegionId] = branchChoice;
        }

        return this with { CompletedQuests = completed, ActiveQuests = active, DistrictChoices = choices };
    }

    /// <summary>Value-based equality for records with nested collections.</summary>
    public bool Equals(WorldSave? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Version != other.Version || World != other.World || Position != other.Position ||
            Waypoint != other.Waypoint || LastExtraction != other.LastExtraction ||
            Resume != other.Resume || Mission != other.Mission || Operative != other.Operative ||
            Math.Abs(WorldTimeMinutes - other.WorldTimeMinutes) > 0.0001 || ActiveVehicle != other.ActiveVehicle)
            return false;

        if (!DictEqual(CompletedQuests, other.CompletedQuests)) return false;
        if (!DictEqual(ActiveQuests, other.ActiveQuests)) return false;
        if (!DictEqual(DistrictChoices, other.DistrictChoices)) return false;
        if (!SetEqual(UnlockedVehicles, other.UnlockedVehicles)) return false;
        if (!DictEqual(FactionReputation, other.FactionReputation)) return false;
        if (!SetEqual(UnlockedHqAmenities, other.UnlockedHqAmenities)) return false;

        return true;
    }

    public override int GetHashCode() => HashCode.Combine(Version, World, Position, Waypoint, LastExtraction, Resume, Mission, Operative);

    private static bool DictEqual<K, V>(Dictionary<K, V>? a, Dictionary<K, V>? b) where K : notnull
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return (a == null || a.Count == 0) && (b == null || b.Count == 0);
        if (a.Count != b.Count) return false;
        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out var bVal) || !EqualityComparer<V>.Default.Equals(kvp.Value, bVal)) return false;
        }
        return true;
    }

    private static bool SetEqual<T>(HashSet<T>? a, HashSet<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return (a == null || a.Count == 0) && (b == null || b.Count == 0);
        return a.SetEquals(b);
    }
}

/// <summary>Database-only world persistence; transaction helpers allow mission settlement and extraction to commit together.</summary>
public static class WorldStorage
{
    /// <summary>Preserves a committed waypoint while settling the mission's safe return in the same transaction.</summary>
    public static async Task SettleReturnAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,int session,int mission,string operative)
    {
        var state=WorldSave.AfterMission(mission,operative);
        await using(var read=new NpgsqlCommand("SELECT value::text FROM game_state WHERE state_key=@key",connection,transaction))
        {
            read.Parameters.AddWithValue("key",$"world:{session}");
            await using var reader=await read.ExecuteReaderAsync();
            if(await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                var existing=JsonSerializer.Deserialize<WorldSave>(reader.GetString(0));
                if(existing!=null && existing.Valid() && existing.Waypoint!=null) state=state with { Waypoint=existing.Waypoint };
            }
        }
        await SaveAsync(connection,transaction,session,state);
    }

    /// <summary>Single-row atomic save with standard retry at callsite on transient errors.</summary>
    public static async Task SaveAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,int session,WorldSave state)
    {
        if(!state.Valid()) throw new InvalidOperationException("Invalid exploration snapshot state rejected.");
        await EnsureAsync(connection,transaction).ConfigureAwait(false);
        await WriteAsync(connection,transaction,session,state).ConfigureAwait(false);
    }

    /// <summary>Reads one campaign's exploration snapshot, populating unvisited campaigns at their latest safe mission terminal.</summary>
    public static async Task<WorldSave?> LoadAsync(int session,int progress,string? connectionString=null,string? preferredOperative=null)
    {
        if(session<=0) return null;
        await using var connection=new NpgsqlConnection(connectionString??PostgresStorage.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await EnsureAsync(connection,null).ConfigureAwait(false);
        await using var command=new NpgsqlCommand("SELECT schema_version,value::text FROM game_state WHERE state_key=@key",connection);
        command.Parameters.AddWithValue("key",$"world:{session}");
        await using var reader=await command.ExecuteReaderAsync().ConfigureAwait(false);
        if(!await reader.ReadAsync().ConfigureAwait(false)) return progress>1?WorldSave.AfterMission(progress-1,preferredOperative??OperativeRoster.Starter):null;
        if(reader.GetInt32(0)!=1) throw new InvalidOperationException("Unsupported world version.");
        var state=JsonSerializer.Deserialize<WorldSave>(reader.GetString(1));
        if(state?.Valid()!=true) throw new InvalidOperationException("Invalid world state.");
        return state;
    }

    /// <summary>Locks the selected existing campaign before writing, serializing entry, transit and mission settlement.</summary>
    public static async Task SaveAsync(int session,WorldSave state,string? connectionString=null)
    {
        if(session<=0 || !state.Valid()) throw new InvalidOperationException("An active campaign and valid world state are required.");
        await using var connection=new NpgsqlConnection(connectionString??PostgresStorage.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction=await connection.BeginTransactionAsync().ConfigureAwait(false);
        await using(var selected=new NpgsqlCommand("SELECT id FROM game_sessions WHERE id=@id FOR UPDATE",connection,transaction))
        {
            selected.Parameters.AddWithValue("id",session);
            if(await selected.ExecuteScalarAsync().ConfigureAwait(false)==null) throw new InvalidOperationException("Campaign no longer exists.");
        }
        await EnsureAsync(connection,transaction).ConfigureAwait(false);
        await WriteAsync(connection,transaction,session,state).ConfigureAwait(false);
        await using(var touched=new NpgsqlCommand("UPDATE game_sessions SET character=@operative,last_played=@last WHERE id=@id",connection,transaction))
        {
            touched.Parameters.AddWithValue("operative",state.Operative); touched.Parameters.AddWithValue("last",DateTime.UtcNow.ToString("O")); touched.Parameters.AddWithValue("id",session);
            await touched.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    /// <summary>Creates the existing generic JSONB store without replacing any document or schema version.</summary>
    private static async Task EnsureAsync(NpgsqlConnection connection,NpgsqlTransaction? transaction)
    {
        await using var command=new NpgsqlCommand("CREATE TABLE IF NOT EXISTS game_state (state_key text PRIMARY KEY, schema_version integer NOT NULL DEFAULT 1, value jsonb NOT NULL)",connection,transaction);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>Writes inside an already locked campaign transaction; future versions cannot be silently downgraded.</summary>
    public static async Task WriteAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,int session,WorldSave state)
    {
        if(!state.Valid()) throw new InvalidOperationException("Invalid world state.");
        await using var command=new NpgsqlCommand("INSERT INTO game_state(state_key,value) VALUES (@key,@value) ON CONFLICT(state_key) DO UPDATE SET value=EXCLUDED.value WHERE game_state.schema_version=1 AND (game_state.value->>'Version')::integer=1",connection,transaction);
        command.Parameters.AddWithValue("key",$"world:{session}");
        command.Parameters.AddWithValue("value",NpgsqlDbType.Jsonb,JsonSerializer.Serialize(state));
        if(await command.ExecuteNonQueryAsync().ConfigureAwait(false)!=1) throw new InvalidOperationException("Unsupported world save version.");
    }
}

/// <summary>Single launch catalog preserves mission-owned arrivals and the existing Act II/III introduction chain.</summary>
public static class WorldMissionRouting
{
    /// <summary>Returns the story entry scene. Act III end/start screens themselves continue into Stratos Docks intro.</summary>
    public static string LaunchPath(int mission) => mission switch
    {
        5=>"res://scenes/cutscenes/act_ii_opening.tscn",
        6=>"res://scenes/cutscenes/cyber_grave_intro.tscn",
        7=>"res://scenes/cutscenes/foundry_forge_intro.tscn",
        8=>"res://scenes/ui/act_ii_end_screen.tscn",
        9=>"res://scenes/cutscenes/spire_outer_hull_intro.tscn",
        10=>"res://scenes/cutscenes/cyber_nexus_intro.tscn",
        11=>"res://scenes/cutscenes/apex_bastion_intro.tscn",
        12=>"res://scenes/cutscenes/judgment_day_intro.tscn",
        _=>LevelCatalog.ScenePath(mission)
    };
}
