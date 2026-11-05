using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Physical action evidence shared by Missions 6–12. Ledgers still own ordering and defender clearance;
/// gates and protected spatial crossings supply independent evidence that the player performed the required action.</summary>
public abstract partial class LaterCodeLevel : CodeLevel
{
    /// <summary>The mission's engine-free ledger; no persistent campaign state is stored here.</summary>
    protected abstract ILaterMissionProgress Ledger { get; }
    private readonly HashSet<string> _requiredActions = new(), _actions = new();
    private readonly List<(string Id, string Tag, ProtectedCrossing Crossing)> _crossings = new();

    /// <summary>Closes out-of-bounds overhead bypasses and seals sector exits after their reachable defenders.</summary>
    protected override void OnLevelReady()
    {
        AddPlatform(0, WorldBounds.Position.Y - 60, WorldBounds.Size.X, 60, PalaceSurface.Wall);
        var boundaries = MissionNumber == 6
            ? new[] { (8400f, "incubation_cleared"), (12900f, "coolant_override"), (17400f, "grid_phase"), (22400f, "siphon_contain") }
            : new[] { (4000f, Ledger.ObjectiveIds[0]), (9800f, Ledger.ObjectiveIds[3]),
                (15800f, Ledger.ObjectiveIds[6]), (20300f, Ledger.ObjectiveIds[8]), (24600f, Ledger.ObjectiveIds[11]) };
        foreach (var (x, objective) in boundaries)
            Actors.AddChild(new MissionInterlock2D { Name = "Interlock_" + objective.Replace(':', '_'),
                Position = new Vector2(x, 600), Height = 600 - WorldBounds.Position.Y, Accepted = () => Ledger.Has(objective) });
    }

    private LaterEnvironmentView? _environment;
    /// <summary>Reviewed environment family, shared with the matching prologue and aftermath.</summary>
    protected string EnvironmentFamily => MissionNumber switch
    {
        6 => "cyber_grave", 7 => "foundry_forge", 8 => "stratos_docks", 9 => "spire_outer_hull",
        10 => "cyber_nexus", 11 => "apex_bastion", _ => "judgment_day"
    };

    /// <summary>Replaces the generic city skyline with the mission's original layered environment family.</summary>
    protected override void BuildBackdrop()
    {
        var layer = new CanvasLayer { Name = "BackdropLayer", Layer = -10 };
        AddChild(layer);
        _environment = new LaterEnvironmentView { Name = "EnvironmentArtwork", Family = EnvironmentFamily,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_environment);
        _environment.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Feeds the retained mission camera into decorative parallax without moving gameplay geometry.</summary>
    protected override void UpdateBackdrop() { if (_environment != null) _environment.CameraX = Camera.GlobalPosition.X; }

    /// <summary>True for combat-only milestones or an ability milestone backed by an accepted physical action.</summary>
    public bool HasActionEvidence(string id) => !_requiredActions.Contains(id) || _actions.Contains(id);

    /// <summary>Consumes a one-shot terminal only after the mission accepts its ledger event; rejected attempts remain usable.</summary>
    protected bool AcceptTerminal(PalaceObjective terminal, string objective, Func<string, bool> commit)
    {
        if (!commit(objective))
        {
            ShowDialogue("TERMINAL LOCKED // " + ComputeObjective(), 2);
            return false;
        }
        terminal.SetCollected();
        return true;
    }

    /// <summary>Allows a gate action after prior milestones, independently of defenders that may be beyond that gate.</summary>
    private bool ActionReady(string id)
    {
        if (MissionComplete || Player == null || Player.CurrentHealth <= 0 || GetTree().Paused) return false;
        int index = Ledger.ObjectiveIds.ToList().IndexOf(id);
        return index >= 0 && Ledger.ObjectiveIds.Take(index).All(Ledger.Has);
    }

    /// <summary>Builds a full-height mandatory gate, a readable ability target and independent action evidence.</summary>
    protected AbilityGate AddObjectiveGate(string name, GateKind kind, Vector2 position, string objective)
    {
        _requiredActions.Add(objective);
        var size = new Vector2(kind is GateKind.RubbleWall or GateKind.SonicLock ? 120 : 320,
            position.Y - WorldBounds.Position.Y);
        var gate = AddGate(name, kind, position, size);
        gate.ObjectiveId = objective;
        gate.Acceptance = () => ActionReady(objective);
        var target = new Label { Name = "AbilityTarget", Position = new Vector2(-160, -180),
            Text = kind switch { GateKind.FireVent => "OCEANUS // DOUSE", GateKind.RubbleWall => "TITAN // SLAM",
                GateKind.SonicLock => "AURA // RESONATE", _ => "SURYA // CROSS WITH BULWARK" } };
        target.AddThemeColorOverride("font_color", new Color("77efff"));
        gate.AddChild(target);
        if (kind == GateKind.HeatVent)
            _crossings.Add((objective, LevelCatalog.Bulwark, new ProtectedCrossing(position.X - 184,
                position.X + 184, WorldBounds.Position.Y + 46, position.Y - 30, 80)));
        else gate.GateCleared += () => { _actions.Add(objective); if (kind == GateKind.FireVent) gate.SecureForEvacuation(); };
        return gate;
    }

    /// <summary>Turns an authored laser into an always-on full-height field and tracks Mirage's complete crossing.</summary>
    protected void TrackPhaseBoundary(CyberLaserGrid2D laser, string objective)
    {
        _requiredActions.Add(objective);
        laser.Position = new Vector2(laser.Position.X, WorldBounds.Position.Y);
        laser.BeamLength = 600 - WorldBounds.Position.Y;
        laser.AlwaysActive = true;
        laser.Damage = 999;
        _crossings.Add((objective, LevelCatalog.Phase, new ProtectedCrossing(laser.Position.X - 30,
            laser.Position.X + 30, WorldBounds.Position.Y + 46, 570, 240)));
        var label = new Label { Position = new Vector2(laser.Position.X - 165, 380), Text = "MIRAGE // PHASE ACROSS LASER" };
        label.AddThemeColorOverride("font_color", new Color("34d399"));
        World.AddChild(label);
    }

    /// <summary>Samples actual motion before each mission reconciles ability evidence with defender clearance.</summary>
    protected override void UpdateMission()
    {
        foreach (var crossing in _crossings)
        {
            bool protectedNow = Player.AbilityTag == crossing.Tag &&
                (crossing.Tag == LevelCatalog.Phase ? Player.IsPhased : Player.IsBulwarked);
            bool crossed = crossing.Crossing.Sample(Player.GlobalPosition.X, Player.GlobalPosition.Y, protectedNow);
            if (crossed && ActionReady(crossing.Id)) _actions.Add(crossing.Id);
        }
    }

    /// <summary>Removes transient rounds and resets crossing samples when recovery abandons an attempt.</summary>
    protected void ClearTransientCombat()
    {
        foreach (var crossing in _crossings) crossing.Crossing.Reset();
        foreach (Node child in Actors.GetChildren())
        {
            if (child is ICampaignProjectile projectile) projectile.Cancel();
            else if (child is WildfireRound) { child.ProcessMode = ProcessModeEnum.Disabled; child.QueueFree(); }
        }
    }

    /// <summary>Destroys only an unaccepted boss attempt. Accepted defeat stays durable through extraction recovery.</summary>
    protected void ResetArenaAttempt<T>(ref T? boss, ref bool spawned, string prefix, string[] formations)
        where T : CharacterBody2D
    {
        ClearTransientCombat();
        if (Ledger.Has(prefix + ":3")) return;
        Ledger.ResetArena(prefix, formations);
        if (GodotObject.IsInstanceValid(boss))
        {
            boss!.ProcessMode = ProcessModeEnum.Disabled;
            boss.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
            boss.QueueFree();
        }
        boss = null;
        spawned = false;
        GetNodeOrNull<CyberCombatHud>("CyberCombatHud")?.HideCampaignBoss();
    }
}
