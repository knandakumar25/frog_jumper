/// <summary>
/// Engine-free rules for where voice recordings live, shared by the players and by the unit tests
/// that check every played recording has a level-matching gain (see <see cref="VoiceGain"/>).
/// Kept separate from the Godot classes so NUnit can call it without starting the engine.
/// </summary>
public static class SpeechPaths
{
    /// <summary>Number of AXIOM-7 briefing lines <c>StoryIntro</c> shows and voices.</summary>
    public const int BriefingLineCount = 4;

    /// <summary>The deployment cinematic's AXIOM-7 narration, which leads straight into the briefing.</summary>
    public const string DeploymentNarration = "res://assets/cutscenes/deployment/narration.ogg";

    /// <summary>Resource path of a mission speech cue from its <c>audio_path</c> in <c>cues.json</c>.</summary>
    public static string CueResource(string audioPath) => "res://" + audioPath;

    /// <summary>English recording of briefing line <paramref name="line"/> (1-based); voice stays English in every locale.</summary>
    public static string Briefing(int line) => $"res://assets/speech/AXIOM-7/english/AXIOM-7-{line}.mp3";
}
