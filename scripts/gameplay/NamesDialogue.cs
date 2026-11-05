using Godot;

/// <summary>Compatibility adapter forwarding mission events to the single shared English speech owner.</summary>
public partial class NamesDialogue : Node
{
    /// <summary>Mission owning subtitle presentation and speech cancellation.</summary>
    public NamesWithoutNumbersMission Mission { get; set; } = null!;
    /// <summary>Retained scene API; recording paths now come from the verified shared catalog.</summary>
    public string VoiceDirectory { get; set; } = "res://assets/audio/names/";
    /// <summary>Retains the mission adapter's gameplay pause contract; only the shared owner processes during pause.</summary>
    public override void _Ready() => ProcessMode = ProcessModeEnum.Pausable;
    /// <summary>Queues an authored mission event in trigger order without a competing audio player.</summary>
    public void Enqueue(string id) => Mission.PlayStoryCue("names." + id);
    /// <summary>Legacy testimony callers resolve to the mission's stable collectible cue.</summary>
    public void EnqueueText(string id, string text) => Mission.PlayStoryCue("m05.fragment" + id.Replace("testimony", ""));
    /// <summary>Recovery and results cancel the shared owner immediately.</summary>
    public void Cancel() => Mission.Speech?.Cancel();
}
