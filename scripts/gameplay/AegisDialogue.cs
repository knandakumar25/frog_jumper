using Godot;

/// <summary>Compatibility adapter forwarding mission events to the single shared English speech owner.</summary>
public partial class AegisDialogue : Node
{
    /// <summary>Mission owning subtitle presentation and speech cancellation.</summary>
    public ShatteredAegisMission Mission { get; set; } = null!;
    /// <summary>Retained scene API; recording paths now come from the verified shared catalog.</summary>
    public string VoiceDirectory { get; set; } = "res://assets/audio/aegis/";
    /// <summary>Retains the mission adapter's gameplay pause contract; only the shared owner processes during pause.</summary>
    public override void _Ready() => ProcessMode = ProcessModeEnum.Pausable;
    /// <summary>Queues an authored mission event in trigger order without a competing audio player.</summary>
    public void Enqueue(string id) => Mission.PlayStoryCue("aegis." + id);
    /// <summary>Boss phase lines belong to the current attempt and may replay after recovery.</summary>
    public void ResetBossAttempt()
    {
        foreach (string id in new[] { "arena", "phase2", "phase3" }) Mission.Speech.Forget("aegis." + id);
    }
    /// <summary>Recovery and results cancel the shared owner immediately.</summary>
    public void Cancel() => Mission.Speech?.Cancel();
}
