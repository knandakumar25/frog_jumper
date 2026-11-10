using Godot;
using System;
using System.Collections.Generic;

/// <summary>One mission owns one FIFO speech player; completion, pause and cancellation share the mission lifecycle.</summary>
public partial class MissionSpeechQueue : Node
{
    /// <summary>Mission supplies subtitles and suspension state; assigned before entering the tree.</summary>
    public CodeLevel Mission { get; set; } = null!;
    /// <summary>Optional resource root for offline fallback testing; empty uses each catalog recording path.</summary>
    public string AudioDirectoryOverride { get; set; } = "";
    /// <summary>Mission speech player volume (-2 dB, unchanged from before level matching); each line's gain is added to it.</summary>
    public const float BaseVolumeDb = -2f;
    private readonly Queue<EnglishSpeechCatalog.Cue> _pending = new();
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private AudioStreamPlayer? _player;
    private EnglishSpeechCatalog.Cue? _current;
    private float _fallbackRemaining;
    /// <summary>Whether a story line owns the subtitle strip, including a missing-resource fallback.</summary>
    public bool Busy => _current != null || _pending.Count > 0;
    /// <summary>Current stable cue ID for runtime verification.</summary>
    public string CurrentCueId => _current?.Id ?? "";
    /// <summary>Queued event count for integration checks without exposing mutable state.</summary>
    public int PendingCount => _pending.Count;

    /// <summary>Always processes so explicit arrival/recovery suspension can pause and resume audio at the same sample.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        // Voice bus (limited, sends into SFX); each line's level-matching gain is added on play.
        _player = new AudioStreamPlayer { Name = "EnglishSpeech", Bus = VoiceGain.Bus, VolumeDb = BaseVolumeDb };
        AddChild(_player);
        _player.Finished += OnFinished;
    }

    /// <summary>Adds each story event once in trigger order; duplicate pending or completed triggers cannot overlap.</summary>
    public void Enqueue(string id)
    {
        var cue = EnglishSpeechCatalog.Get(id);
        if (cue == null || cue.Classification != "speech" || !_seen.Add(id)) return;
        _pending.Enqueue(cue);
    }

    /// <summary>Allows attempt-owned boss phases to be spoken again after a retry.</summary>
    public void Forget(string id) => _seen.Remove(id);

    /// <summary>Advances on actual stream completion; only unavailable resources use a reading-time fallback.</summary>
    public override void _Process(double delta)
    {
        if (_player == null || !IsInsideTree()) return;
        _player.StreamPaused = Mission.SpeechSuspended;
        if (Mission.SpeechSuspended) return;
        if (_current != null)
        {
            if (_player.Stream == null)
            {
                _fallbackRemaining -= (float)delta;
                if (_fallbackRemaining <= 0) _current = null;
            }
            if (_current != null)
            {
                float remaining = _player.Stream == null ? _fallbackRemaining :
                    Math.Max(.1f, (float)(_player.Stream.GetLength() - _player.GetPlaybackPosition()));
                Mission.ShowSpeechSubtitle(EnglishSpeechCatalog.Subtitle(_current), remaining + .2f);
                return;
            }
        }
        if (_pending.Count == 0) return;
        _current = _pending.Dequeue();
        string path = string.IsNullOrEmpty(AudioDirectoryOverride) ? _current.AudioPath :
            AudioDirectoryOverride.TrimEnd('/') + "/" + _current.AudioPath[(_current.AudioPath.LastIndexOf('/') + 1)..];
        _player.Stream = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
        // Mission recordings differ by up to 10 LU; the per-line gain evens them out (0 dB if unlisted).
        _player.VolumeDb = BaseVolumeDb + VoiceGain.GainDb(path);
        _fallbackRemaining = Math.Max(4, _current.Text.Length / 14f);
        Mission.ShowSpeechSubtitle(EnglishSpeechCatalog.Subtitle(_current),
            _player.Stream == null ? _fallbackRemaining : (float)_player.Stream.GetLength() + .2f);
        if (_player.Stream != null) _player.Play();
    }

    /// <summary>Signals completion without starting the next recording inside the audio callback.</summary>
    private void OnFinished() => _current = null;

    /// <summary>Stops stale speech immediately on death, recovery, results and teardown; pending events can retrigger.</summary>
    public void Cancel()
    {
        foreach (var cue in _pending) _seen.Remove(cue.Id);
        _pending.Clear();
        _current = null;
        _fallbackRemaining = 0;
        _player?.Stop();
    }

    /// <summary>Releases the sole speech lane when the owning scene leaves the tree.</summary>
    public override void _ExitTree() => Cancel();
}
