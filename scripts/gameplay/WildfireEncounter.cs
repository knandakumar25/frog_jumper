using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// One finite authored group. Crossing its local activation rectangle starts it once; a completed group stays dead.
/// Recovery rebuilds only incomplete groups, and the mission ledger prevents duplicate kill rewards.
/// </summary>
public partial class WildfireEncounter : Node2D
{
    /// <summary>Stable encounter identifier, independent of generated node names.</summary>
    public string Id { get; set; } = "";
    /// <summary>World-space activation rectangle, including the route's available jump height.</summary>
    public Rect2 Activation { get; set; }
    /// <summary>Authored member positions; true selects a grounded burst gunner, false a hovering sentry.</summary>
    public (Vector2 Position, bool Gunner)[] Members { get; set; } = Array.Empty<(Vector2, bool)>();
    /// <summary>Explicit targeting and scene-local objective state supplied by the mission.</summary>
    public IFiniteCombatHost Mission { get; set; } = null!;
    /// <summary>Reinforcements are started by the battery rather than by proximity.</summary>
    public bool ManualActivation { get; set; }
    /// <summary>True while at least one member in this attempt remains alive.</summary>
    public bool Active { get; private set; }
    /// <summary>Completed groups never respawn during this scene instance.</summary>
    public bool Completed => Mission.HasCombatEvent("encounter:" + Id);
    /// <summary>Remaining live slots contribute to the host's simultaneous-enemy budget.</summary>
    public int LivingMembers => Active ? Members.Length - _dead.Count : 0;
    /// <summary>Optional prerequisite for sequential reinforcements; ordinary encounters accept proximity as before.</summary>
    public Func<bool>? Acceptance { get; set; }
    private readonly HashSet<int> _dead = new();
    private int _generation;

    /// <summary>Activates only on the local route, after arrival and outside recovery.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (!ManualActivation && !Active && !Completed && !Mission.GameplaySuspended &&
            Activation.HasPoint(Mission.CombatPlayer.GlobalPosition)) Activate();
    }

    /// <summary>Creates the finite group. Repeated triggers cannot add extra members.</summary>
    public void Activate()
    {
        if (Active || Completed || Mission.GameplaySuspended || Acceptance?.Invoke() == false || !Mission.CanActivateEncounter(Members.Length)) return;
        Active = true;
        int generation = ++_generation;
        for (int i = 0; i < Members.Length; i++)
        {
            int index = i;
            var spec = Members[i];
            if (spec.Gunner)
            {
                var gunner = new WildfireGunner { Name = $"Gunner{i}", Position = spec.Position,
                    Target = Mission.CombatPlayer, Mission = Mission };
                gunner.Defeated += () => MemberDefeated(index, generation);
                AddChild(gunner);
            }
            else
            {
                var sentry = new PalaceSentry { Name = $"Sentry{i}", Position = spec.Position,
                    Target = Mission.CombatPlayer, MaxHealth = 70 };
                sentry.Defeated += () => MemberDefeated(index, generation);
                AddChild(sentry);
            }
        }
    }

    /// <summary>Accepts each member once and rejects callbacks belonging to an abandoned recovery attempt.</summary>
    private void MemberDefeated(int index, int generation)
    {
        if (generation != _generation || !Active || !_dead.Add(index)) return;
        Mission.RewardMember($"{Id}:{index}");
        if (_dead.Count != Members.Length) return;
        Active = false;
        Mission.CompleteCombatEvent("encounter:" + Id);
    }

    /// <summary>Disables abandoned actors immediately; deferred deletion keeps physics callbacks safe.</summary>
    public void ResetIncomplete()
    {
        if (Completed) return;
        ++_generation;
        Active = false;
        _dead.Clear();
        foreach (Node child in GetChildren())
        {
            child.ProcessMode = ProcessModeEnum.Disabled;
            if (child is CollisionObject2D body) body.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
            child.QueueFree();
        }
    }
}
