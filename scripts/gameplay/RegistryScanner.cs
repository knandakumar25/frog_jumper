using Godot;

/// <summary>
/// Telegraphed local cyber scanner hazard: pulsating amber warning sweep, intense white/cyan laser beam,
/// and generous dark crossing window. Renders volumetric projection lines and ground ionization sparks.
/// </summary>
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
        _cue = new AudioStreamPlayer2D { Name = "ScannerCue", Bus = "SFX", VolumeDb = -16, MaxDistance = 900 };
        AddChild(_cue);
    }

    /// <summary>Damage is limited to a narrow floor lane; safe waiting areas lie on either side.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Mission.GameplaySuspended || Mission.Progress.Has("signal")) return;
        _clock += (float)delta;
        _cooldown -= (float)delta;
        float phase = (_clock + Offset) % 6;
        int state = phase < 1.5f ? 0 : phase < 2.7f ? 1 : 2;

        if (state != _lastPhase)
        {
            _lastPhase = state;
            if (state < 2 && IsInsideTree())
            {
                string path = "res://assets/audio/names/scanner_" + (state == 0 ? "warning" : "live") + ".ogg";
                if (ResourceLoader.Exists(path))
                {
                    _cue.Stream = GD.Load<AudioStream>(path);
                    _cue.PitchScale = state == 0 ? 0.95f : 1.1f;
                    _cue.Play();
                }
            }
        }

        var point = Mission.Player.GlobalPosition - GlobalPosition;
        if (phase >= 1.5f && phase < 2.7f && _cooldown <= 0 && Mathf.Abs(point.X) < 42 && point.Y > -430 && point.Y < 30)
        {
            Mission.Player.TakeDamage(18, new Vector2(-Mission.Player.FacingSign * 140, -80));
            _cooldown = 0.8f;
        }
        QueueRedraw();
    }

    /// <summary>Renders cyber projector housing, volumetric warning beams, glowing laser core, and safe status markers.</summary>
    public override void _Draw()
    {
        float phase = (_clock + Offset) % 6;
        bool safe = Mission.Progress.Has("signal") || phase >= 2.7f;
        Color baseColor = safe ? new Color("85dacb") : phase < 1.5f ? new Color("e6a650") : Colors.White;

        // Projector emitter housing at the ceiling (-450)
        DrawRect(new Rect2(-60, -450, 120, 22), new Color("141e28"));
        DrawRect(new Rect2(-45, -446, 90, 14), baseColor);
        DrawLine(new Vector2(-65, -428), new Vector2(65, -428), baseColor, 2);

        if (!safe)
        {
            if (phase < 1.5f)
            {
                // Amber Warning State: Pulsing guide line and downward tracking frequency pips
                float warningPulse = 0.35f + 0.25f * Mathf.Sin(_clock * 12.0f);
                DrawRect(new Rect2(-4, -428, 8, 428), new Color(baseColor, warningPulse));

                // Downward animated frequency chevrons
                for (int pip = 0; pip < 4; pip++)
                {
                    float y = -428 + Mathf.PosMod(_clock * 180 + pip * 110, 428);
                    DrawLine(new Vector2(-16, y), new Vector2(16, y), new Color(baseColor, 0.7f), 2);
                }

                // Ground threshold warning indicators
                DrawLine(new Vector2(-70, 0), new Vector2(70, 0), new Color(baseColor, warningPulse), 3);
                DrawLine(new Vector2(-70, -10), new Vector2(-70, 0), baseColor, 2);
                DrawLine(new Vector2(70, -10), new Vector2(70, 0), baseColor, 2);
            }
            else
            {
                // Active State: Intense laser beam with dual glow falloff and ionization lines
                // Outer glow aura
                DrawRect(new Rect2(-24, -428, 48, 428), new Color(0, 0.94f, 1f, 0.25f));
                // Core beam
                DrawRect(new Rect2(-10, -428, 20, 428), new Color(1, 1, 1, 0.9f));
                // Center filament
                DrawRect(new Rect2(-3, -428, 6, 428), Colors.White);

                // Containment ionization rails
                DrawLine(new Vector2(-28, -428), new Vector2(-28, 0), new Color(0, 0.94f, 1f, 0.6f), 1.5f);
                DrawLine(new Vector2(28, -428), new Vector2(28, 0), new Color(0, 0.94f, 1f, 0.6f), 1.5f);

                // Floor contact blast / spark ring
                float floorPulse = 0.7f + 0.3f * Mathf.Sin(_clock * 24.0f);
                DrawLine(new Vector2(-80, 0), new Vector2(80, 0), new Color(1, 1, 1, floorPulse), 4);
                DrawArc(new Vector2(0, 0), 22, -Mathf.Pi, 0, 16, new Color(0, 0.94f, 1f, 0.8f), 2);
            }
        }
        else
        {
            // Safe / Neutralized State: Soft emerald baseline indicating safe passage
            DrawLine(new Vector2(-70, 0), new Vector2(70, 0), baseColor, 3);
            DrawCircle(new Vector2(0, -428), 4, baseColor);
        }
    }
}
