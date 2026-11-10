using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// A generated table of per-file playback gains: <c>{"files": {"res://path": {"gain_db": g, ...}}}</c>.
/// Used for voice lines (<see cref="VoiceGain"/>) and mission music (<see cref="MusicGain"/>), whose
/// tables are written by <c>tools/assets/voice_gain.py</c> and <c>tools/assets/music_gain.py</c>.
/// Files are never re-encoded; players add the gain to their volume. A missing table or entry gives
/// 0 dB, so playback never depends on the table.
/// </summary>
public sealed class GainTable
{
    /// <summary>Upper bound on any gain magnitude; same value as MAX_GAIN_DB in the generators.</summary>
    public const float MaxGainDb = 30f;

    private readonly string _path;
    private readonly float _limit;
    private Dictionary<string, float>? _gains;

    /// <summary>Creates a lazily loaded table for the <c>res://</c> JSON file at <paramref name="path"/>, clamping gains to +/- <paramref name="limit"/>.</summary>
    public GainTable(string path, float limit = MaxGainDb)
    {
        _path = path;
        _limit = limit;
    }

    /// <summary>
    /// Parses table JSON into resource path -> gain in dB, clamped to +/- <paramref name="limit"/>.
    /// Pure C# so it can be unit-tested without the engine. Wrong shapes (non-object root, "files"
    /// or an entry not an object, non-numeric gain) give no gain; invalid JSON throws
    /// <see cref="JsonException"/>.
    /// </summary>
    public static Dictionary<string, float> Parse(string json, float limit = MaxGainDb)
    {
        var result = new Dictionary<string, float>(StringComparer.Ordinal);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
            return result;
        foreach (var entry in files.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.Object
                && entry.Value.TryGetProperty("gain_db", out var gain) && gain.ValueKind == JsonValueKind.Number)
                result[entry.Name] = Math.Clamp(gain.GetSingle(), -limit, limit);
        }
        return result;
    }

    /// <summary>Gain in dB for a resource path (already clamped to the table's limit); 0 when absent.</summary>
    public float GainDb(string resPath)
    {
        _gains ??= Load();
        return _gains.GetValueOrDefault(resPath, 0f);
    }

    /// <summary>Whether the table has an entry for <paramref name="resPath"/>.</summary>
    public bool Contains(string resPath)
    {
        _gains ??= Load();
        return _gains.ContainsKey(resPath);
    }

    /// <summary>Loads the table once through Godot's file system; a read or parse failure gives an empty table.</summary>
    private Dictionary<string, float> Load()
    {
        try
        {
            return FileAccess.FileExists(_path) ? Parse(FileAccess.GetFileAsString(_path), _limit) : new();
        }
        // Parse already returns an empty table for wrong shapes; this catches invalid JSON and,
        // defensively, any accessor error a shape check missed.
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            GD.PushWarning($"GainTable: ignoring unreadable {_path}: {error.Message}");
            return new();
        }
    }
}
