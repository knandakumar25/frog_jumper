using Godot;
using System;

/// <summary>
/// Subterranean bio-synthetic gene-warping incubation pod.
/// Renders a pressurized glass stasis cylinder with rising bio-nutrient bubbles,
/// captive silhouette, and high-voltage containment rings.
/// Interaction or kinetic impact frees the captive and advances the mission objective.
/// </summary>
public partial class GenePod : Node2D
{
    /// <summary>Stable ledger objective identifier (e.g. "pod:1", "pod:2").</summary>
    public string Objective { get; set; } = "";

    /// <summary>Owning mission supplying player state and ledger access.</summary>
    public CyberGraveMission Mission { get; set; } = null!;

    /// <summary>Readable interaction caption.</summary>
    public string Caption { get; set; } = "Liberate captive operative";

    /// <summary>Proximity threshold for player interaction.</summary>
    public bool InRange => Mission != null && Mission.Player != null &&
        Mission.Player.GlobalPosition.DistanceTo(GlobalPosition + new Vector2(0, -46)) < 140;

    private float _timer;
    private readonly float[] _bubbleYs = { -30f, -60f, -90f, -120f };
    private readonly float[] _bubbleXs = { -10f, 8f, -6f, 12f };

    /// <summary>Advances procedural nutrient bubble animation.</summary>
    public override void _Process(double delta)
    {
        _timer += (float)delta;
        for (int i = 0; i < _bubbleYs.Length; i++)
        {
            _bubbleYs[i] -= (float)delta * (25f + i * 10f);
            if (_bubbleYs[i] < -135f) _bubbleYs[i] = -25f;
        }
        QueueRedraw();
    }

    /// <summary>Attempts to liberate the pod if in range and prerequisites are met.</summary>
    public bool TryLiberate()
    {
        if (Mission == null || Mission.GameplaySuspended || !InRange) return false;
        return Mission.CommitObjective(Objective);
    }

    /// <summary>Handles pointer/touch and context interactions.</summary>
    public override void _Input(InputEvent input)
    {
        if (!IsInsideTree() || Mission == null || Mission.GameplaySuspended || !InRange) return;
        Vector2? point = input is InputEventScreenTouch { Pressed: true } touch ? touch.Position :
            input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse ? mouse.Position : null;
        if (!point.HasValue) return;

        Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * point.Value;
        if (new Rect2(-45, -150, 90, 160).HasPoint(local) && TryLiberate())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Draws the industrial pod, glowing nutrient fluid, containment rings, and captive silhouette.</summary>
    public override void _Draw()
    {
        bool done = Mission != null && Mission.Progress.Has(Objective);
        bool ready = Mission != null && Mission.Progress.CanComplete(Objective);

        Color fluidColor = done ? new Color(0.2f, 0.9f, 0.7f, 0.35f) :
            ready ? new Color(0.0f, 1.0f, 0.6f, 0.55f) :
            new Color(0.9f, 0.4f, 0.1f, 0.45f);

        Color metalColor = new("14222e");
        Color rimColor = done ? new Color("85dacb") : ready ? new Color("00ff99") : new Color("e6a650");

        // Base flange and floor mount
        DrawRect(new Rect2(-42, -16, 84, 16), metalColor);
        DrawLine(new Vector2(-46, 0), new Vector2(46, 0), rimColor, 4);

        // Glass cylinder container
        Rect2 glassRect = new(-32, -135, 64, 120);
        DrawRect(glassRect, new Color(0.05f, 0.15f, 0.18f, 0.75f));
        DrawRect(glassRect, fluidColor);

        // Top cap and manifold
        DrawRect(new Rect2(-38, -150, 76, 16), metalColor);
        DrawLine(new Vector2(-36, -150), new Vector2(36, -150), rimColor, 2);

        // High-voltage reinforcement rings
        for (int y = -110; y <= -50; y += 30)
        {
            DrawLine(new Vector2(-34, y), new Vector2(34, y), new Color(rimColor, 0.7f), 2.5f);
        }

        if (!done)
        {
            // Rising biological fluid bubbles
            for (int i = 0; i < _bubbleYs.Length; i++)
            {
                DrawCircle(new Vector2(_bubbleXs[i], _bubbleYs[i]), 2.5f, new Color(1f, 1f, 1f, 0.6f));
            }

            // Captive humanoid silhouette inside
            Vector2 headPos = new(0, -98);
            DrawCircle(headPos, 9, new Color(0.08f, 0.12f, 0.15f, 0.85f));
            DrawRect(new Rect2(-8, -88, 16, 26), new Color(0.08f, 0.12f, 0.15f, 0.85f));
            DrawLine(new Vector2(-8, -75), new Vector2(-16, -58), new Color(0.08f, 0.12f, 0.15f, 0.85f), 3);
            DrawLine(new Vector2(8, -75), new Vector2(16, -58), new Color(0.08f, 0.12f, 0.15f, 0.85f), 3);
        }
        else
        {
            // Shattered glass cracks and open safety valves
            DrawLine(new Vector2(-18, -120), new Vector2(12, -70), new Color(1f, 1f, 1f, 0.8f), 1.5f);
            DrawLine(new Vector2(12, -70), new Vector2(-4, -40), new Color(1f, 1f, 1f, 0.7f), 1.5f);
            DrawArc(new Vector2(0, -75), 18, 0, Mathf.Tau, 20, new Color("85dacb"), 2f);
        }

        // Context prompt indicator when in range and ready
        if (InRange && ready && !done)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(_timer * 8f);
            DrawArc(new Vector2(0, -170), 12, 0, Mathf.Tau, 16, new Color(0f, 1f, 0.8f, pulse), 2f);
        }
    }
}
