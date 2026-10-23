using Godot;

/// <summary>
/// A story beat that adds an operative to the squad and teaches their signature ability on the spot — the replacement
/// for the old separate training level. Walking into the beat's trigger zone unlocks the operative (if they are not
/// already unlocked), shows the story lines, and displays a prompt explaining the swap key and the ability. The lesson
/// completes the first time that operative's ability is used near the beat, after which the prompt disappears and the
/// lesson never shows again this session. Nothing about a beat ever blocks progress.
/// </summary>
public partial class RecruitBeat : Node2D
{
    /// <summary>Display name of the operative who joins (registry name).</summary>
    public string Operative { get; set; } = "Oceanus";
    /// <summary>Story lines shown one after another when the beat triggers.</summary>
    public string[] Lines { get; set; } = System.Array.Empty<string>();
    /// <summary>Horizontal distance (px) at which the beat triggers.</summary>
    public float TriggerRadius { get; set; } = 140f;
    /// <summary>True after the beat has triggered.</summary>
    public bool Triggered { get; private set; }
    /// <summary>True after the ability lesson has been completed.</summary>
    public bool Completed { get; private set; }
    /// <summary>The level that shows dialogue and the prompt; assigned by the level before the beat enters the tree.</summary>
    public CodeLevel Level { get; set; } = null!;
    /// <summary>Raised once when the lesson completes.</summary>
    [Signal] public delegate void LessonCompletedEventHandler();

    private float _time, _lineClock, _promptTimeout;
    private int _lineIndex;
    private CyberShogun? _player;

    /// <summary>Name, one-line instruction and ability-use radius for each signature ability tag.</summary>
    private static (string Name, string Hint) Describe(string tag) => tag switch
    {
        LevelCatalog.Parry => ("KINETIC PARRY", "press Q just as an enemy shot lands to throw it back"),
        LevelCatalog.Douse => ("TIDAL TORRENT", "face the flames and press Q to put them out"),
        LevelCatalog.Slam => ("TECTONIC SLAM", "stand beside heavy rubble and press Q to shatter it"),
        LevelCatalog.Resonate => ("HARMONIC PULSE", "press Q near a sonic lock to open it"),
        LevelCatalog.Phase => ("PHASE BLINK", "press Q to slip through lasers and hazards"),
        _ => ("SOLAR BULWARK", "press Q to absorb heat and incoming fire")
    };

    /// <summary>Skips the beat entirely if this operative's lesson was already shown (replaying a later mission).</summary>
    public override void _Ready()
    {
        _player = Level.Player;
        if (OperativeRoster.Current.LessonSeen(Operative)) { Triggered = Completed = true; Visible = false; }
        else _player.AbilityUsed += OnAbilityUsed;
    }

    /// <summary>Triggers the beat when the player is close, advances story lines, and expires a stale prompt.</summary>
    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
        if (Completed || _player == null) return;
        if (!Triggered)
        {
            Vector2 offset = _player.GlobalPosition - GlobalPosition;
            if (Mathf.Abs(offset.X) <= TriggerRadius && Mathf.Abs(offset.Y) <= 400f) Trigger();
            return;
        }
        _lineClock -= (float)delta;
        if (_lineIndex < Lines.Length && _lineClock <= 0f)
        {
            Level.ShowDialogue(Lines[_lineIndex++], 6f);
            _lineClock = 6.5f;
        }
        _promptTimeout -= (float)delta;
        RefreshPrompt();
        if (_promptTimeout <= 0f) Level.HidePrompt();
    }

    /// <summary>Unlocks the operative and starts the story lines and prompt.</summary>
    public void Trigger()
    {
        if (Triggered) return;
        Triggered = true;
        OperativeRoster.Current.Unlock(Operative);
        _promptTimeout = 45f;
        RefreshPrompt();
        GD.Print($"RecruitBeat: {Operative} joins the squad");
    }

    /// <summary>Shows either the swap instruction or the ability instruction depending on who is active.</summary>
    private void RefreshPrompt()
    {
        string tag = OperativeRoster.AbilityTagOf(Operative) ?? "";
        var (name, hint) = Describe(tag);
        string active = _player!.ActiveProfile.DisplayName;
        if (active == OperativeRoster.Normalize(Operative))
            Level.ShowPrompt($"{name}: {hint}");
        else
        {
            int slot = System.Linq.Enumerable.ToList(OperativeRoster.RosterOrder).IndexOf(OperativeRoster.Normalize(Operative)) + 1;
            Level.ShowPrompt($"{Operative.ToUpperInvariant()} JOINED  -  press {slot} or Tab to swap, then {name}: {hint}");
        }
    }

    /// <summary>Completes the lesson when the joined operative uses their own ability.</summary>
    private void OnAbilityUsed(string tag)
    {
        if (!Triggered || Completed) return;
        if (_player!.ActiveProfile.DisplayName != OperativeRoster.Normalize(Operative) || tag != OperativeRoster.AbilityTagOf(Operative)) return;
        Completed = true;
        OperativeRoster.Current.MarkLessonSeen(Operative);
        Level.HidePrompt();
        Level.ShowDialogue($"{Describe(tag).Name} mastered. Swap any time with Tab or the number keys.", 5f);
        EmitSignal(SignalName.LessonCompleted);
        Visible = false;
    }

    /// <summary>Draws a pulsing light pillar so the recruit location is visible before the player reaches it.</summary>
    public override void _Draw()
    {
        if (Triggered) return;
        var colour = OperativeRegistry.GetProfile(Operative).ThemeColor;
        float pulse = 1f + Mathf.Sin(_time * 3f) * .12f;
        for (int i = 0; i < 4; i++)
            DrawRect(new Rect2(-14 - i * 6, -420, 28 + i * 12, 420), new Color(colour, .07f * pulse));
        DrawCircle(new Vector2(0, -60), 26f * pulse, new Color(colour, .35f));
        DrawArc(new Vector2(0, -60), 40f * pulse, 0, Mathf.Tau, 36, new Color(colour, .8f), 3f);
    }
}
