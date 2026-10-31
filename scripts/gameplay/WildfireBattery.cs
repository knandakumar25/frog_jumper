using Godot;
using System.Linq;

/// <summary>Stationary siege emplacement with two separately destructible relays and a shielded melee core.</summary>
public partial class WildfireBattery : Node2D
{
    /// <summary>Mission owns artillery, reinforcement activation and extraction state.</summary>
    public ConcreteWildfireMission Mission { get; set; } = null!;
    /// <summary>Finite reinforcement group triggered once by the first relay.</summary>
    public WildfireEncounter Reinforcements { get; set; } = null!;
    /// <summary>Exposed core uses the existing player's real melee query.</summary>
    public PalaceObjective Core { get; private set; } = null!;
    /// <summary>Two independently tracked power relays; completed ones remain destroyed across recovery.</summary>
    public PalaceObjective[] Relays { get; private set; } = System.Array.Empty<PalaceObjective>();
    /// <summary>Permanent destruction state for the mission instance.</summary>
    public bool Destroyed => Mission.Progress.Has("battery:core");
    /// <summary>Local HUD appears only while the player is in this encounter.</summary>
    public bool EncounterActive => !Destroyed && Mathf.Abs(Mission.Player.GlobalPosition.X - GlobalPosition.X) < 1350;
    private Label _status = null!;

    /// <summary>Installs the shield and relays before arrival captures the descendant processing state.</summary>
    public override void _Ready()
    {
        WildfireVisual.AddSprite(this, "battery", new Vector2(400, 230), new Vector2(0, -108));
        Core = new PalaceObjective { Name = "Core", Kind = "Beacon", ShieldCharges = 2 };
        AddChild(Core);
        AddWorldLabel(Core, "WF_BATTERY_CORE", "CORE / DESTROY BOTH POWER RELAYS");
        Core.Neutralized += OnCoreDestroyed;
        Relays = new[] { CreateRelay("west", -460), CreateRelay("east", 460) };
    }

    /// <summary>Registers an individually identified relay and its one-time shield decrement.</summary>
    private PalaceObjective CreateRelay(string id, float x)
    {
        var relay = new PalaceObjective { Name = "Relay_" + id, Kind = "Beacon", Position = new Vector2(x, 0) };
        relay.Neutralized += () =>
        {
            if (!Mission.Progress.Complete("relay:" + id)) return;
            Core.HitShield();
            if (!Core.Shielded) Core.GetNode<Label>("RoleLabel").Text = WildfireText.Get("WF_CORE_EXPOSED", "CORE EXPOSED / ATTACK");
            Reinforcements.Activate();
        };
        AddChild(relay);
        AddWorldLabel(relay, "WF_BATTERY_RELAY", "POWER RELAY");
        return relay;
    }

    /// <summary>Labels relay roles so the core's shield and otherwise similar machines are readable at combat scale.</summary>
    private static void AddWorldLabel(Node2D target, string key, string fallback)
    {
        var label = new Label { Name = "RoleLabel", Text = WildfireText.Get(key, fallback), Position = new Vector2(-180, -170),
            Size = new Vector2(360, 60), AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color("#ffcf8a") };
        label.AddThemeFontSizeOverride("font_size", 18);
        target.AddChild(label);
    }

    /// <summary>Places a compact local indicator below the lesson prompt, anchored for wide and tall viewports.</summary>
    public void BindHud(Control root)
    {
        _status = new Label { Name = "BatteryStatus", HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_status);
        _status.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _status.OffsetLeft = -350; _status.OffsetRight = 350; _status.OffsetTop = 175; _status.OffsetBottom = 220;
        _status.AddThemeFontSizeOverride("font_size", 20);
        _status.AddThemeColorOverride("font_color", new Color("#ffcf8a"));
    }

    /// <summary>Recovery can restart an unfinished reinforcement group without spawning a new wave.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Mission.GameplaySuspended) return;
        if (EncounterActive && Relays.Any(relay => relay.Collected) && !Reinforcements.Completed) Reinforcements.Activate();
        if (_status == null) return;
        _status.Visible = EncounterActive;
        if (!EncounterActive) return;
        string phase = Mission.Artillery.WarningRemaining > 0
            ? WildfireText.Get("WF_BATTERY_SHELLING", "SHELLING — leave the orange markers")
            : WildfireText.Get("WF_BATTERY_RECOVERY", "RECOVERY — attack the relays / exposed core");
        _status.Text = $"{WildfireText.Get("WF_BATTERY", "SIEGE BATTERY")}  {Core.CurrentHealth:0}/210  |  " +
            $"{WildfireText.Get("WF_RELAYS", "RELAYS")} {Relays.Count(relay => relay.Collected)}/2\n{phase}";
    }

    /// <summary>Destruction ends local artillery permanently; extraction still requires all three civilian groups.</summary>
    private void OnCoreDestroyed()
    {
        if (!Mission.Progress.Complete("battery:core")) return;
        Mission.Artillery.StopForever();
        Modulate = new Color(.4f, .45f, .5f);
        Mission.ShowDialogue(WildfireText.Get("WF_BATTERY_DOWN", "Battery destroyed. Break the final barrier and secure the last evacuation route."), 7);
    }
}
