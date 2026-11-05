using Godot;

/// <summary>Captures cursor and menu-help state for one cinematic and restores it on every exit path.</summary>
public sealed class CinematicOwnership
{
    private readonly Node _owner;
    private readonly Input.MouseModeEnum _mouse;
    private readonly bool _promptVisible;
    private bool _restored;

    /// <summary>Captures before presentation mutates global UI. Gameplay's own controls remain independently owned.</summary>
    public CinematicOwnership(Node owner)
    {
        _owner = owner;
        _mouse = Input.MouseMode;
        var global = owner.GetNodeOrNull<Node>("/root/Global");
        var prompt = global?.Get("_prompt_label").AsGodotObject() as CanvasItem;
        _promptVisible = prompt?.IsVisibleInTree() ?? true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        global?.Call("set_control_prompt_visible", false);
    }

    /// <summary>Restores exactly the state this presentation captured; repeated teardown cannot overwrite a later owner.</summary>
    public void Restore()
    {
        if (_restored) return;
        _restored = true;
        Input.MouseMode = _mouse;
        if (GodotObject.IsInstanceValid(_owner) && _owner.IsInsideTree())
            _owner.GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible", _promptVisible);
    }

    /// <summary>Shared responsive subtitle bounds leave room for shot composition, letterboxing and small windows.</summary>
    public static void LayoutSubtitles(PanelContainer? panel)
    {
        if (panel == null) return;
        panel.CustomMinimumSize = Vector2.Zero;
        panel.AnchorLeft = .08f; panel.AnchorRight = .92f;
        panel.AnchorTop = .77f; panel.AnchorBottom = .96f;
        panel.OffsetLeft = panel.OffsetRight = panel.OffsetTop = panel.OffsetBottom = 0;
    }

    /// <summary>Preserves at least fourteen physical pixels after canvas stretching in a small desktop window.</summary>
    public static int SubtitleFont(Control owner)
    {
        float canvasWidth = owner.GetViewportRect().Size.X;
        float windowWidth = Mathf.Max(1, owner.GetWindow().Size.X);
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(22, 14 * canvasWidth / windowWidth)), 22, 56);
    }
}
