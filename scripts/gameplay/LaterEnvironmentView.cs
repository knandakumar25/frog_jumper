using Godot;

/// <summary>Aspect-preserving decorative scenery on its own canvas layer. The camera introduces modest parallax;
/// plates never create or imply authoritative collision.</summary>
public partial class LaterEnvironmentView : Control
{
    /// <summary>Reviewed environment family corresponding to this mission.</summary>
    public string Family { get; set; } = "cyber_grave";
    /// <summary>Mission camera sample used to shift architecture independently of the base.</summary>
    public float CameraX { get; set; }
    private float _seconds;

    /// <summary>Animates independent emissive layers without allocating nodes or changing game physics.</summary>
    public override void _Process(double delta) { _seconds += (float)delta; QueueRedraw(); }

    /// <summary>Scales uniformly to cover wide and tall displays, keeping the central landmark readable.</summary>
    public override void _Draw()
    {
        float fit = Mathf.Max(Size.X / 1920, Size.Y / 1080) * 1.1f;
        Vector2 margin = (Size - new Vector2(1920, 1080) * fit) / 2;
        margin.X -= Mathf.Sin(CameraX / 28000 * Mathf.Pi) * 55;
        DrawSetTransform(margin, 0, Vector2.One * fit);
        CampaignArtwork.DrawEnvironment(this, Family, _seconds);
    }
}
