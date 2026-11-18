using Godot;
using System.Collections.Generic;

/// <summary>
/// On-screen touch controls for one play mode: a GoHUD virtual joystick at the bottom left and round
/// action buttons at the bottom right (see <see cref="TouchLayout"/>).
///
/// <para><b>Input path.</b> Everything is sent as InputMap actions through
/// <see cref="Input.ParseInputEvent"/>, the same layer keyboards and controllers use, so no gameplay
/// code changes. The joystick sends per-direction strengths and only re-sends a direction whose value
/// changed.</para>
///
/// <para><b>Visibility</b> (owner decision, 2026-11-17; rule in <see cref="TouchVisibility"/>):
/// shown from the start on iOS/Android, otherwise only after a touch; hidden by a deliberate key,
/// controller or real mouse press; shown again by the next touch. Also hidden while the game is
/// paused and while the host suppresses it (for example while the world map is open). Hiding
/// releases every held action.</para>
///
/// <para><b>Layout</b> follows the viewport size and the device's safe area (notches, rounded
/// corners), and is recomputed when the window changes size.</para>
/// </summary>
public partial class TouchControls : CanvasLayer
{
    /// <summary>The joystick widget from the GoHUD add-on (MIT), drawn in code.</summary>
    private const string JoystickScript = "res://addons/gohud/widgets/go_joystick.gd";

    private static TouchControls? _current;

    /// <summary>
    /// True while touch controls are on screen in the running scene. Gameplay that would otherwise
    /// follow the mouse (top-down aim) checks this, because touches move the emulated mouse.
    /// </summary>
    public static bool Active => _current != null && GodotObject.IsInstanceValid(_current) && _current.Shown;

    /// <summary>The layout this instance shows.</summary>
    public TouchLayout Layout { get; private set; } = TouchLayout.Platformer;
    /// <summary>True when the controls are currently drawn.</summary>
    public bool Shown => _root != null && _root.Visible;
    /// <summary>The joystick node (a GoHUD <c>GoJoystick</c>), exposed for tests.</summary>
    public Control? Joystick => _joystick;

    private Control _root = null!;
    private Control? _joystick;
    private readonly Dictionary<string, TouchActionButton> _buttons = new();
    private readonly Dictionary<string, float> _stick = new();
    private bool _wanted;
    private bool _suppressed;

    /// <summary>Raised with the new state whenever the controls appear or disappear (hosts swap their text hints).</summary>
    public event System.Action<bool>? ShownChanged;

    /// <summary>Creates touch controls for a layout; add the result to the scene (it is its own CanvasLayer).</summary>
    public static TouchControls Create(TouchLayout layout) =>
        new() { Layout = layout, Name = "TouchControls", Layer = 20, ProcessMode = ProcessModeEnum.Always };

    /// <summary>
    /// GDScript-callable setup (C# statics are not reachable from GDScript): picks "platformer",
    /// "isometric" or "world" before the node enters the tree. Unknown names keep the platformer layout.
    /// </summary>
    public void UseLayout(string name)
    {
        Layout = name switch { "isometric" => TouchLayout.Isometric, "world" => TouchLayout.World, _ => TouchLayout.Platformer };
        Name = "TouchControls";
        Layer = 20;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>Builds the joystick and buttons, sets the starting visibility and follows window resizes.</summary>
    public override void _Ready()
    {
        _current = this;
        _root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        BuildJoystick();
        foreach (var spec in Layout.Buttons)
        {
            var button = TouchActionButton.Create(spec.Action, spec.Label);
            _buttons[spec.Action] = button;
            _root.AddChild(button);
        }
        _wanted = TouchVisibility.Initial(TouchVisibility.IsMobileBuild());
        GetViewport().SizeChanged += ApplyLayout;
        ApplyLayout();
        UpdateShown();
    }

    /// <summary>Stops following the window, releases held actions and clears the shared reference.</summary>
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(GetViewport())) GetViewport().SizeChanged -= ApplyLayout;
        ReleaseAll();
        if (_current == this) _current = null;
    }

    /// <summary>Shows on touch and hides on deliberate key, pad or mouse use; never consumes the event.</summary>
    public override void _Input(InputEvent input)
    {
        bool next = TouchVisibility.Next(_wanted, TouchVisibility.Classify(input));
        if (next == _wanted) return;
        _wanted = next;
        UpdateShown();
    }

    /// <summary>Hides the controls while the game is paused, so they never cover the pause menu.</summary>
    public override void _Process(double delta)
    {
        bool paused = IsInsideTree() && GetTree().Paused;
        if (Shown == (_wanted && !_suppressed && !paused)) return;
        UpdateShown();
    }

    /// <summary>Lets the host hide the controls for a while (for example the world map); hiding releases actions.</summary>
    public void SetSuppressed(bool suppressed)
    {
        _suppressed = suppressed;
        UpdateShown();
    }

    /// <summary>Shows or hides one button (for example Squad before a second operative joins) and re-lays out.</summary>
    public void SetButtonVisible(string action, bool visible)
    {
        if (!_buttons.TryGetValue(action, out var button)) return;
        button.Visible = visible;
        ApplyLayout();
    }

    /// <summary>The button for an action, or null; used by tests.</summary>
    public TouchActionButton? ButtonFor(string action) => _buttons.GetValueOrDefault(action);

    /// <summary>Forces the visibility wish, as a touch or a key would; used by tests and the smoke harness.</summary>
    public void SetWanted(bool wanted)
    {
        _wanted = wanted;
        UpdateShown();
    }

    /// <summary>Applies the combined visibility and releases everything when hidden.</summary>
    private void UpdateShown()
    {
        if (_root == null) return;
        bool paused = IsInsideTree() && GetTree().Paused;
        bool show = _wanted && !_suppressed && !paused;
        if (!show) ReleaseAll();
        if (_root.Visible == show) return;
        _root.Visible = show;
        ShownChanged?.Invoke(show);
    }

    /// <summary>Creates the GoHUD joystick: it appears where the thumb lands inside its zone (FOLLOW mode).</summary>
    private void BuildJoystick()
    {
        var script = GD.Load<GDScript>(JoystickScript);
        if (script == null) { GD.PushWarning("TouchControls: GoHUD joystick script is missing."); return; }
        _joystick = (Control)script.New().AsGodotObject();
        _joystick.Name = "Joystick";
        _joystick.Set("mode", 1);            // GoJoystick.Mode.FOLLOW
        _joystick.Set("ink", new Color("00f0ff"));
        _joystick.Set("dead_zone", 0.12f);
        _joystick.Connect("moved", Callable.From<Vector2>(OnStick));
        _root.AddChild(_joystick);
    }

    /// <summary>Turns the joystick vector into action strengths, sending only the directions that changed.</summary>
    private void OnStick(Vector2 vector)
    {
        foreach (var (action, strength) in Layout.StickStrengths(vector))
        {
            float previous = _stick.GetValueOrDefault(action);
            if (Mathf.IsEqualApprox(previous, strength)) continue;
            _stick[action] = strength;
            if (InputMap.HasAction(action))
                Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = strength > 0f, Strength = strength });
        }
    }

    /// <summary>Releases every joystick direction and button, so nothing stays held after hiding.</summary>
    private void ReleaseAll()
    {
        if (_stick.Count > 0) OnStick(Vector2.Zero);
        foreach (var button in _buttons.Values)
            if (GodotObject.IsInstanceValid(button)) button.Release();
    }

    /// <summary>
    /// Places the joystick zone and the buttons for the current viewport size inside the safe area.
    /// Main buttons: the first sits in the bottom-right corner and the rest on a quarter circle
    /// around it. Secondary buttons (Pause, Map, Squad): a smaller row above the arc, from the right edge.
    /// </summary>
    private void ApplyLayout()
    {
        if (_root == null) return;
        Vector2 view = GetViewport().GetVisibleRect().Size;
        Rect2 safe = SafeArea(view);
        float unit = Mathf.Clamp(view.Y * 0.105f, 64f, 132f);          // main button diameter
        float small = unit * 0.72f;                                    // secondary button diameter
        float gap = unit * 0.18f;

        if (_joystick != null)
        {
            float radius = unit * 0.95f;
            _joystick.Set("radius", radius);
            _joystick.Set("knob_radius", radius * 0.42f);
            // A generous zone: the stick follows the thumb anywhere in the lower-left area.
            var zone = new Vector2(Mathf.Max(radius * 2.6f, view.X * 0.32f), Mathf.Max(radius * 2.6f, view.Y * 0.45f));
            _joystick.Position = new Vector2(safe.Position.X, safe.End.Y - zone.Y);
            _joystick.Size = zone;
        }

        var arc = new List<TouchActionButton>();
        var column = new List<TouchActionButton>();
        foreach (var spec in Layout.Buttons)
        {
            var button = _buttons[spec.Action];
            if (!button.Visible) continue;
            (spec.Zone == TouchButtonZone.Arc ? arc : column).Add(button);
        }

        Vector2 corner = new(safe.End.X - unit * 0.5f - gap, safe.End.Y - unit * 0.5f - gap);
        // Ring radius: far enough from the corner button, and wide enough that neighbours on the ring
        // (spaced 90° / (n-1) apart) are at least one button plus a gap apart: chord = 2·R·sin(step/2).
        int ringCount = Mathf.Min(Mathf.Max(arc.Count - 1, 1), 4);
        float step = ringCount <= 1 ? Mathf.Pi / 2f : Mathf.DegToRad(90f / (ringCount - 1));
        float ring = Mathf.Max(unit + gap * 1.6f, (unit + gap) / (2f * Mathf.Sin(step / 2f)));
        for (int i = 0; i < arc.Count; i++)
        {
            Vector2 centre = corner;
            if (i > 0)
            {
                // Spread the rest from straight left (180°) to straight up (270°); a second ring takes overflow.
                int onRing = Mathf.Min(arc.Count - 1, 4);
                int index = (i - 1) % 4;
                float distance = ring * (1 + (i - 1) / 4);
                float angle = Mathf.DegToRad(180f + (onRing == 1 ? 45f : 90f * index / (onRing - 1)));
                centre = corner + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            }
            Place(arc[i], centre, unit);
        }

        float top = corner.Y - ring * (arc.Count > 5 ? 2f : 1f) - unit * 0.5f - gap * 2f - small * 0.5f;
        // A row (right to left) rather than a column, so the secondary buttons stay below the
        // top-right HUD (objective panel, world minimap) even when the arc has two rings.
        for (int i = 0; i < column.Count; i++)
            Place(column[i], new Vector2(safe.End.X - small * 0.5f - gap - i * (small + gap), top), small);
    }

    /// <summary>Centres a button of the given diameter on a point.</summary>
    private static void Place(Control button, Vector2 centre, float diameter)
    {
        button.Size = new Vector2(diameter, diameter);
        button.Position = centre - button.Size * 0.5f;
        button.QueueRedraw();
    }

    /// <summary>
    /// The usable part of the viewport: the OS safe area (notches, home indicator) mapped from screen
    /// pixels to viewport coordinates, plus a small margin. Falls back to the whole viewport.
    /// </summary>
    private Rect2 SafeArea(Vector2 view)
    {
        const float margin = 16f;
        var full = new Rect2(Vector2.Zero, view);
        if (DisplayServer.GetName() == "headless") return full.Grow(-margin);
        Vector2I window = DisplayServer.WindowGetSize();
        Rect2I safe = DisplayServer.GetDisplaySafeArea();
        if (window.X <= 0 || window.Y <= 0 || safe.Size.X <= 0 || safe.Size.Y <= 0) return full.Grow(-margin);
        Vector2 scale = view / new Vector2(window.X, window.Y);
        Vector2I origin = DisplayServer.WindowGetPosition();
        var local = new Rect2((safe.Position - origin) * scale, safe.Size * scale).Intersection(full);
        return (local.Size.X > view.X * 0.5f && local.Size.Y > view.Y * 0.5f ? local : full).Grow(-margin);
    }
}
