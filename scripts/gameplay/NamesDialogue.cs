using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>One mission-owned speech lane. Critical beats outrank expiring route hints; missing audio retains complete subtitles.</summary>
public partial class NamesDialogue : Node
{
    /// <summary>Owning mission supplies subtitles and exact objective state.</summary>
    public NamesWithoutNumbersMission Mission { get; set; } = null!;
    /// <summary>Optional speech directory; absent resources keep the full subtitle fallback and never gate objectives.</summary>
    public string VoiceDirectory { get; set; } = "res://assets/audio/names/";
    private sealed record Beat(string Id, string Text, int Priority, double Expires);
    private readonly List<Beat> _queue = new();
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private AudioStreamPlayer _speech = null!;
    private Beat? _current;
    private float _remaining;
    private double _clock;
    private Dictionary<string, string> _lines = new();
    /// <summary>Loads the same English cue manifest used by cached voice production, then creates the sole player.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        string path = "res://assets/audio/names/cues.json";
        if (FileAccess.FileExists(path))
        {
            using var json = JsonDocument.Parse(FileAccess.GetFileAsString(path));
            foreach (var cue in json.RootElement.EnumerateArray())
                _lines[cue.GetProperty("id").GetString()!] = cue.GetProperty("speaker").GetString() + ": " + cue.GetProperty("text").GetString();
        }
        _speech = new AudioStreamPlayer { Name = "Speech", Bus = "SFX", VolumeDb = -2 };
        AddChild(_speech);
    }
    /// <summary>Adds an optional testimony subtitle to the same serialized lane, so speech cannot overwrite it.</summary>
    public void EnqueueText(string id,string text)
    {
        _lines.TryAdd(id,text);
        Enqueue(id);
    }
    /// <summary>Deduplicates event beats; hints expire in twelve gameplay seconds and cannot interrupt critical speech.</summary>
    public void Enqueue(string id)
    {
        if (!_lines.TryGetValue(id, out string? text) || !_seen.Add(id)) return;
        int priority = id is "hint" ? 0 : 10;
        if (priority > 0)
        {
            _queue.RemoveAll(b => b.Priority == 0);
            if (_current?.Priority == 0) { _speech.Stop(); _current = null; _remaining = 0; }
        }
        _queue.Add(new Beat(id, text, priority, priority == 0 ? _clock + 12 : double.PositiveInfinity));
    }
    /// <summary>Serializes complete words and subtitles; paused gameplay pauses both the timer and the stream.</summary>
    public override void _Process(double delta)
    {
        _speech.StreamPaused = Mission.GameplaySuspended;
        if (Mission.GameplaySuspended) return;
        _clock += delta;
        _remaining -= (float)delta;
        if (_current != null && _remaining > 0)
        {
            // Mission checkpoints and recruitment prompts share the strip; active speech owns its matching subtitle.
            Mission.ShowDialogue(WildfireText.Get("NAMES_SPEECH_" + _current.Id.ToUpperInvariant(), _current.Text), _remaining);
            return;
        }
        _current = null;
        _queue.RemoveAll(b => b.Expires < _clock);
        var next = _queue.OrderByDescending(b => b.Priority).FirstOrDefault();
        if (next == null) return;
        _queue.Remove(next);
        _current = next;
        string audio = VoiceDirectory + next.Id + ".ogg";
        _speech.Stop();
        _speech.Stream = ResourceLoader.Exists(audio) ? GD.Load<AudioStream>(audio) : null;
        _remaining = _speech.Stream != null ? (float)_speech.Stream.GetLength() + .35f : Math.Max(4, next.Text.Length / 14f);
        Mission.ShowDialogue(WildfireText.Get("NAMES_SPEECH_" + next.Id.ToUpperInvariant(), next.Text), _remaining);
        if (_speech.Stream != null && IsInsideTree()) _speech.Play();
    }
    /// <summary>Recovery cancels stale speech/hints without replaying completed story events; teardown/results silence immediately.</summary>
    public void Cancel()
    {
        _queue.Clear();
        _current = null;
        _remaining = 0;
        _speech?.Stop();
    }
    /// <summary>Stops owned speech if the mission or scheduler is independently removed.</summary>
    public override void _ExitTree() => Cancel();
}
