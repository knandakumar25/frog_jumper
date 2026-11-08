using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Owns the drones of the current quest encounter: spawning under the shared hostile budget,
/// the player's melee arc, kill counting and the local alert level. Only quest objectives
/// start encounters, so safe pockets and streets stay free of roaming spawns.
/// </summary>
public partial class WorldEncounterController : Node2D
{
    /// <summary>A wave ends this far from its centre, which resets it for the next approach.</summary>
    public const double LeashMetres = 70;
    // Reach must exceed the distance at which drones hold position (WorldHostile.AttackRange - 0.4),
    // or a drone could hit the player from a spot the player can't hit back.
    private const double AttackReachMetres = 3.4, AttackHalfArcRadians = Math.PI / 4;

    private readonly List<WorldHostile> _live = new();
    private WorldPoint _center;
    private int _required, _kills;

    /// <summary>Raised once when the current wave's kill target is met, with the wave key.</summary>
    public event Action<string>? WaveCleared;

    /// <summary>The objective this wave belongs to, or null when none is running.</summary>
    public string? WaveKey { get; private set; }
    public int LiveCount => _live.Count;
    public int Kills => _kills;

    /// <summary>The current local alert, taken from what the live drones are doing.</summary>
    public NeoKyotoWorld.AlertState Alert =>
        _live.Count == 0 ? NeoKyotoWorld.AlertState.None
        : _live.Any(h => h.State == WorldHostile.Awareness.Engaged) ? NeoKyotoWorld.AlertState.Pursuit
        : _live.Any(h => h.State == WorldHostile.Awareness.Investigating) ? NeoKyotoWorld.AlertState.Search
        : NeoKyotoWorld.AlertState.Suspicion;

    /// <summary>Starts a wave around a point. Calling again for the running wave does nothing.</summary>
    public void StartWave(string key, WorldPoint center, int required)
    {
        if (WaveKey == key) return;
        Clear();
        WaveKey = key; _center = center; _required = required; _kills = 0;
        for (int i = 0; i < required; i++)
        {
            double angle = Mathf.Tau * i / required;
            TrySpawn(center.X + Math.Cos(angle) * 8, center.Y + Math.Sin(angle) * 8);
        }
    }

    /// <summary>Adds one drone only if the shared budget still allows it. Returns false when refused.</summary>
    public bool TrySpawn(double x, double y)
    {
        if (!WorldPerformanceBudget.IsWithinBudget(0, _live.Count + 1, 0)) return false;
        var hostile = new WorldHostile();
        hostile.Spawn(x, y);
        AddChild(hostile);
        _live.Add(hostile);
        return true;
    }

    /// <summary>Advances every drone, and ends the wave if the player has left its area.</summary>
    public void Tick(double delta, WorldPoint player, ChunkAddress origin, Action<float> strike)
    {
        if (WaveKey != null && _center.Distance(player) > LeashMetres) { Clear(); return; }
        // A strike can defeat the player, which clears this list, so loop over a copy and stop if that happens.
        foreach (var hostile in _live.ToArray())
        {
            if (!_live.Contains(hostile)) break;
            hostile.Tick(delta, player, origin, strike);
        }
    }

    /// <summary>
    /// Resolves a melee swing. The arc is checked in metres, undoing the camera squash.
    /// Facing index F points along (-sin a, cos a) with a = F * 45 degrees, matching
    /// how the player controller derives it. Returns how many drones were hit.
    /// </summary>
    public int ResolveAttack(WorldPoint player, int facing, float damage)
    {
        double angle = facing * Math.PI / 4;
        double aimX = -Math.Sin(angle), aimY = Math.Cos(angle);
        int hits = 0;
        foreach (var hostile in _live.ToArray())
        {
            double dx = hostile.XMetres - player.X, dy = hostile.YMetres - player.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > AttackReachMetres) continue;
            // Within a metre the direction flips with tiny movements, so a drone that close is always in the arc.
            if (distance > 1.0)
            {
                double dot = Math.Clamp((dx * aimX + dy * aimY) / distance, -1, 1);
                if (Math.Acos(dot) > AttackHalfArcRadians) continue;
            }
            hits++;
            if (hostile.Hit(damage)) Remove(hostile);
        }
        return hits;
    }

    /// <summary>Removes all drones and forgets the wave, as on defeat, escape or a scene move.</summary>
    public void Clear()
    {
        foreach (var hostile in _live) { RemoveChild(hostile); hostile.QueueFree(); }
        _live.Clear();
        WaveKey = null; _kills = 0; _required = 0;
    }

    /// <summary>Removes one destroyed drone and reports the wave when its target is met.</summary>
    private void Remove(WorldHostile hostile)
    {
        _live.Remove(hostile);
        RemoveChild(hostile);
        hostile.QueueFree();
        _kills++;
        if (WaveKey != null && _kills >= _required)
        {
            string key = WaveKey;
            Clear();
            WaveCleared?.Invoke(key);
        }
    }
}
