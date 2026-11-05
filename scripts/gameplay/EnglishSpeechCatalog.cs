using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>Offline English recordings shared by all playable missions; subtitles retain their current locale.</summary>
public static class EnglishSpeechCatalog
{
    /// <summary>Stable authored cue, its localized display key and immutable English recording resource.</summary>
    public sealed record Cue(string Id, string Text, string SubtitleKey, string AudioPath, string Classification);
    private static readonly Dictionary<string, Cue> Cues = Load();

    /// <summary>Loads the production inventory once; absent data leaves visual dialogue fully functional.</summary>
    private static Dictionary<string, Cue> Load()
    {
        var result = new Dictionary<string, Cue>(StringComparer.Ordinal);
        const string path = "res://assets/audio/english/cues.json";
        if (!FileAccess.FileExists(path)) return result;
        using var document = JsonDocument.Parse(FileAccess.GetFileAsString(path));
        foreach (var row in document.RootElement.EnumerateArray())
        {
            string id = row.GetProperty("id").GetString()!;
            result[id] = new Cue(id, row.GetProperty("display_text").GetString()!,
                row.GetProperty("subtitle_key").GetString() ?? "", "res://" + row.GetProperty("audio_path").GetString(),
                row.GetProperty("classification").GetString()!);
        }
        return result;
    }

    /// <summary>Returns a cue by stable ID without depending on the selected subtitle language.</summary>
    public static Cue? Get(string id) => Cues.GetValueOrDefault(id);

    /// <summary>Adapts legacy story callers using their English or localized display text; status strings do not match.</summary>
    public static Cue? Find(string text)
    {
        foreach (var cue in Cues.Values)
            if (cue.Classification == "speech" && (text == cue.Text || text == Subtitle(cue))) return cue;
        return null;
    }

    /// <summary>Resolves the existing subtitle translation while keeping recording selection in English.</summary>
    public static string Subtitle(Cue cue) => string.IsNullOrEmpty(cue.SubtitleKey)
        ? cue.Text : WildfireText.Get(cue.SubtitleKey, cue.Text);
}
