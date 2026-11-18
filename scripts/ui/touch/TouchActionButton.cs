using Godot;

/// <summary>
/// One round on-screen button that holds an InputMap action down while a finger is on it.
///
/// <para>It tracks its own touch index, so several fingers work at once (stick plus jump). Godot's
/// built-in <see cref="Button"/> only follows one pointer, which is why this is a plain Control.
/// Presses go through <see cref="Input.ParseInputEvent"/> as <see cref="InputEventAction"/>s, so
/// gameplay code that reads events (<c>InputActionDefaults.Matches</c>) and code that polls
/// (<c>Input.IsActionPressed</c>, <c>IsActionJustPressed</c>) both see them, exactly as for a key.</para>
///
/// <para>A left mouse press is handled the same way (touch index -2), so the controls can be tried
/// on a desktop. Mouse events Godot copies from touches are ignored, because the touch itself
/// already pressed the button.</para>
///
/// <para>The action is always released when the button is hidden, disabled by pause, or freed,
/// so an action can never stay stuck down.</para>
/// </summary>
public partial class TouchActionButton : Control
{
    /// <summary>The InputMap action this button presses.</summary>
    public string Action { get; private set; } = "";
    /// <summary>Source text of the label; translated when drawn.</summary>
    public string Label { get; private set; } = "";
    /// <summary>True while a finger (or the mouse) holds the button.</summary>
    public bool Held => _pointer != NoPointer;

    private const int NoPointer = -1, MousePointer = -2;
    private int _pointer = NoPointer;
    private static Font? _font;

    /// <summary>Accent colours, matching the cyan mission HUD.</summary>
    private static readonly Color Ring = new("00f0ff"), Fill = new(0.02f, 0.05f, 0.09f, 0.55f), HeldFill = new(0f, 0.94f, 1f, 0.45f);

    /// <summary>Creates a button for <paramref name="action"/> labelled with <paramref name="label"/> (a translation key).</summary>
    public static TouchActionButton Create(string action, string label)
    {
        var button = new TouchActionButton { Action = action, Label = label, Name = "Touch_" + action };
        button.MouseFilter = MouseFilterEnum.Stop;
        // A button press is a physical place on the screen, so right-to-left languages do not mirror it.
        button.LayoutDirection = LayoutDirectionEnum.Ltr;
        return button;
    }

    /// <summary>Redraws on translation changes and releases the action when hidden or removed.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged) QueueRedraw();
        else if (what == NotificationVisibilityChanged && !IsVisibleInTree()) Release();
        else if (what == NotificationExitTree) Release();
    }

    /// <summary>Starts or ends a hold for touches by index and for the left mouse button.</summary>
    public override void _GuiInput(InputEvent input)
    {
        switch (input)
        {
            case InputEventScreenTouch touch when touch.Pressed && _pointer == NoPointer:
                Press(touch.Index);
                AcceptEvent();
                break;
            case InputEventScreenTouch touch when !touch.Pressed && touch.Index == _pointer:
                Release();
                AcceptEvent();
                break;
            case InputEventMouseButton mouse when mouse.ButtonIndex == MouseButton.Left:
                // Copies of touches are swallowed so they never reach gameplay as a click.
                if (mouse.Device != InputEvent.DeviceIdEmulation)
                {
                    if (mouse.Pressed && _pointer == NoPointer) Press(MousePointer);
                    else if (!mouse.Pressed && _pointer == MousePointer) Release();
                }
                AcceptEvent();
                break;
        }
    }

    /// <summary>Presses the action once and remembers which pointer owns the hold.</summary>
    private void Press(int pointer)
    {
        _pointer = pointer;
        Send(true);
        QueueRedraw();
    }

    /// <summary>Releases the action if this button is holding it; safe to call repeatedly.</summary>
    public void Release()
    {
        if (_pointer == NoPointer) return;
        _pointer = NoPointer;
        Send(false);
        if (IsInsideTree()) QueueRedraw();
    }

    /// <summary>Sends the action press or release through the normal input pipeline.</summary>
    private void Send(bool pressed)
    {
        if (string.IsNullOrEmpty(Action) || !InputMap.HasAction(Action)) return;
        Input.ParseInputEvent(new InputEventAction { Action = Action, Pressed = pressed, Strength = pressed ? 1f : 0f });
    }

    /// <summary>Draws a filled circle with a cyan ring and the translated label, brighter while held.</summary>
    public override void _Draw()
    {
        float radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 2f;
        Vector2 centre = Size * 0.5f;
        DrawCircle(centre, radius, Held ? HeldFill : Fill);
        DrawArc(centre, radius, 0, Mathf.Tau, 48, Ring, Held ? 4f : 2.5f, true);
        _font ??= GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf") ?? ThemeDB.FallbackFont;
        // Non-Latin labels fall back to the theme font, which carries the locale's glyphs.
        Font font = IsAscii(Tr(Label)) ? _font : GetThemeFont("font");
        string text = Tr(Label).ToUpper();
        int fontSize = Mathf.Clamp(Mathf.RoundToInt(radius * 0.38f), 10, 22);
        Vector2 extent = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        // Shrink long translations to fit inside the circle.
        if (extent.X > radius * 1.7f && extent.X > 0)
        {
            fontSize = Mathf.Max(8, Mathf.FloorToInt(fontSize * radius * 1.7f / extent.X));
            extent = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        }
        var origin = new Vector2(centre.X - extent.X * 0.5f, centre.Y + font.GetAscent(fontSize) * 0.5f - font.GetDescent(fontSize) * 0.5f);
        DrawString(font, origin, text, HorizontalAlignment.Left, -1, fontSize, Held ? Colors.White : Ring);
    }

    /// <summary>True when every character is basic Latin, which the HUD font can draw.</summary>
    private static bool IsAscii(string text)
    {
        foreach (char c in text) if (c > 127) return false;
        return true;
    }

    /// <summary>
    /// The hit area is the full circle (to the button's edge, slightly past the drawn ring). It is round,
    /// not square, because neighbouring buttons on the arc sit diagonally and their square corners overlap.
    /// </summary>
    public override bool _HasPoint(Vector2 point) => point.DistanceTo(Size * 0.5f) <= Mathf.Min(Size.X, Size.Y) * 0.5f;
}
