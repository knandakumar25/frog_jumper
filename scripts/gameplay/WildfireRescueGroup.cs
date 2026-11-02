using Godot;
using System;
using System.Linq;

/// <summary>
/// Invulnerable local evacuation vignette: wait behind cover, lock all route fires safe, then walk into shelter.
/// Civilians never follow the player or navigate a platform gap. The ledger counts arrival, not the trigger.
/// </summary>
public partial class WildfireRescueGroup : Node2D
{
    /// <summary>Stable group identifier shared by the mission's extraction requirements.</summary>
    public string Id { get; set; } = "";
    /// <summary>Mission supplies the ledger and localized status messages.</summary>
    public ConcreteWildfireMission Mission { get; set; } = null!;
    /// <summary>Local enemies and any permanent obstacle that must be cleared first.</summary>
    public string[] Conditions { get; set; } = Array.Empty<string>();
    /// <summary>All these vents must be doused simultaneously before the authored route is secured.</summary>
    public AbilityGate[] RouteFires { get; set; } = Array.Empty<AbilityGate>();
    /// <summary>Short same-floor route endpoint, relative to this group's origin.</summary>
    public Vector2 ShelterOffset { get; set; } = new(440, 0);
    /// <summary>True only after the family has arrived; repeated callbacks do not increment progress.</summary>
    public bool Evacuated => Mission.Progress.Has("rescue:" + Id);
    /// <summary>True once local safety has been established, including permanent fire suppression.</summary>
    public bool RouteSecured { get; private set; }
    /// <summary>First unmet local prerequisite, evaluated from current state after recovery; only arrival reports evacuation.</summary>
    public string StatusText
    {
        get
        {
            if (Evacuated) return WildfireText.Get("WF_SHELTER_SAFE", "EVACUATED / SHELTER REACHED");
            if (RouteSecured) return WildfireText.Get("WF_SHELTER_MOVING", "CIVILIANS MOVING / ENTERING SHELTER");
            string? defense = Conditions.FirstOrDefault(id => id.StartsWith("encounter:") && !Mission.Progress.Has(id));
            if (defense != null) return WildfireText.Get("WF_RESCUE_DEFENDERS", "{count} LOCAL DEFENDERS / {group}")
                .Replace("{count}", Mission.RemainingDefenders(defense).ToString()).Replace("{group}", defense[10..]);
            if (Conditions.Any(id => id.StartsWith("barrier:") && !Mission.Progress.Has(id)))
                return WildfireText.Get("WF_RESCUE_BARRIER", "ROUTE BLOCKED / APEX TITAN: SLAM / {ability}").Replace("{ability}", Mission.AbilityControl);
            if (Conditions.Any(id => !Mission.Progress.Has(id)))
                return WildfireText.Get("WF_RESCUE_BATTERY", "ROUTE UNDER FIRE / DESTROY THE SIEGE BATTERY CORE");
            if (RouteFires.Any(fire => !fire.Cleared))
                return WildfireText.Get("WF_RESCUE_FIRE", "ROUTE FIRE / OCEANUS: TIDAL TORRENT / {ability}").Replace("{ability}", Mission.AbilityControl);
            return WildfireText.Get("WF_RESCUE_READY", "ROUTE SAFE / PREPARING EVACUATION");
        }
    }
    private Node2D _family = null!;
    private Label _instructions = null!;
    private float _travel;

    /// <summary>Creates separate family and shelter art; neither has a combat hurtbox.</summary>
    public override void _Ready()
    {
        _family = new Node2D { Name = "Family" };
        AddChild(_family);
        WildfireVisual.AddSprite(_family, "civilians", new Vector2(160, 110), new Vector2(0, -50));
        var shelter = new Node2D { Name = "Shelter", Position = ShelterOffset };
        AddChild(shelter);
        WildfireVisual.AddSprite(shelter, "shelter", new Vector2(240, 210), new Vector2(0, -95));
        _instructions = new Label { Name = "RouteInstructions", Position = new Vector2(-160, -220),
            Size = new Vector2(420, 100), HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color("#77efff") };
        _instructions.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_instructions);
    }

    /// <summary>Checks explicit local prerequisites, then advances a short authored route with no damage or escort AI.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Mission.GameplaySuspended) return;
        _instructions.Text = StatusText;
        if (Evacuated) return;
        if (!RouteSecured)
        {
            if (!Conditions.All(Mission.Progress.Has) || !RouteFires.All(fire => fire.Cleared)) return;
            foreach (var fire in RouteFires) fire.SecureForEvacuation();
            RouteSecured = true;
            Mission.Progress.Complete("route:" + Id);
            QueueRedraw();
        }
        _travel = Mathf.MoveToward(_travel, ShelterOffset.Length(), 80 * (float)delta);
        _family.Position = ShelterOffset.LengthSquared() > 0 ? ShelterOffset.Normalized() * _travel : Vector2.Zero;
        if (_travel < ShelterOffset.Length()) return;
        _family.Visible = false;
        if (Mission.Progress.Complete("rescue:" + Id))
            Mission.ShowDialogue(WildfireText.Get("WF_GROUP_SAFE", "Civilian group evacuated. Shelter route secured."), 5);
        QueueRedraw();
        _instructions.Text = StatusText;
    }

    /// <summary>Protected cyan route markings contrast with orange fire and shelling zones.</summary>
    public override void _Draw()
    {
        DrawLine(new Vector2(-60, 2), ShelterOffset + new Vector2(60, 2),
            RouteSecured ? new Color("#77efff") : new Color("#77868f"), 4);
        DrawCircle(ShelterOffset + new Vector2(0, -160), 11, Evacuated ? new Color("#77ffba") : new Color("#ffcf8a"));
    }
}
