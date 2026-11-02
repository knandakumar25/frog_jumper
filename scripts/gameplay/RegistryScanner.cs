using Godot;

/// <summary>Telegraphed local scanner hazard: amber warning, white active beam, then a generous dark crossing window. No detection system.</summary>
public partial class RegistryScanner : Node2D
{
    /// <summary>Owning mission supplies pause/recovery and permanent scanner shutdown state.</summary>
    public NamesWithoutNumbersMission Mission { get; set; } = null!;
    /// <summary>Authored phase offset; the isolated tutorial uses zero.</summary>
    public float Offset { get; set; }
    private float _clock, _cooldown;
    private int _lastPhase = -1;
    private AudioStreamPlayer2D _cue = null!;
    /// <summary>One spatial player per scanner limits overlap and routes cues through the existing SFX bus.</summary>
    public override void _Ready()
    {
        _cue=new AudioStreamPlayer2D { Name="ScannerCue",Bus="SFX",VolumeDb=-16,MaxDistance=900 };
        AddChild(_cue);
    }
    /// <summary>Damage is limited to a narrow floor lane; safe waiting areas lie on either side.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Mission.GameplaySuspended || Mission.Progress.Has("signal")) return;
        _clock += (float)delta;
        _cooldown -= (float)delta;
        float phase = (_clock + Offset) % 6;
        int state=phase<1.5f?0:phase<2.7f?1:2;
        if(state!=_lastPhase)
        {
            _lastPhase=state;
            if(state<2 && IsInsideTree())
            {
                string path="res://assets/audio/names/scanner_"+(state==0?"warning":"live")+".ogg";
                if(ResourceLoader.Exists(path)){_cue.Stream=GD.Load<AudioStream>(path);_cue.Play();}
            }
        }
        var point = Mission.Player.GlobalPosition - GlobalPosition;
        if (phase >= 1.5f && phase < 2.7f && _cooldown <= 0 && Mathf.Abs(point.X) < 42 && point.Y > -430 && point.Y < 30)
        {
            Mission.Player.TakeDamage(18, new Vector2(-Mission.Player.FacingSign*140,-80));
            _cooldown = .8f;
        }
        QueueRedraw();
    }
    /// <summary>Warning and beam differ in width/brightness as well as color; shutdown leaves a visible accepted marker.</summary>
    public override void _Draw()
    {
        float phase = (_clock + Offset) % 6;
        bool safe = Mission.Progress.Has("signal") || phase >= 2.7f;
        Color color = safe ? new Color("85dacb") : phase < 1.5f ? new Color("e6a650") : Colors.White;
        DrawRect(new Rect2(-60,-450,120,22),color);
        if (!safe) DrawRect(new Rect2(phase < 1.5f ? -3 : -22,-428,phase < 1.5f ? 6 : 44,428),new Color(color,phase < 1.5f ? .3f : .85f));
        DrawLine(new Vector2(-70,0),new Vector2(70,0),color,4);
    }
}
