using Godot;

/// <summary>Integration fixture injecting mixed processing, hidden HUD and prepaused audio before the real Level 4 arrival snapshot; never used by campaign scenes.</summary>
public partial class AegisArrivalSnapshotProbe : ShatteredAegisMission
{
    /// <summary>Seeds states that differ from defaults so restoration tests detect accidental blanket enabling or unpausing.</summary>
    protected override void OnLevelReady()
    {
        GetNode<CanvasLayer>("MissionHUD").Visible = false;
        GetNode<AudioStreamPlayer>("AudioStreamPlayer").StreamPaused = true;
        var always = new Node { Name = "ExplicitAlways", ProcessMode = ProcessModeEnum.Always };
        always.AddChild(new Node { Name = "ExplicitDisabled", ProcessMode = ProcessModeEnum.Disabled });
        AddChild(always);
        AddChild(new Node { Name = "ExplicitPausable", ProcessMode = ProcessModeEnum.Pausable });
        base.OnLevelReady();
    }
}
