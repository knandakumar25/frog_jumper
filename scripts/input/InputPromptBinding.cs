using Godot;
using System;

/// <summary>Owns a prompt subscription for exactly as long as its control lives.</summary>
public partial class InputPromptBinding : Node
{
    private Action? _refresh;
    private InputPromptService? _service;
    private string? _cinematicAction;
    private string? _previousContext;
    private string[] _previousActions = Array.Empty<string>();

    /// <summary>Creates an owner-scoped listener; freeing the owner disconnects its callback.</summary>
    public static void Track(Node owner, Action refresh)
    {
        owner.AddChild(new InputPromptBinding { _refresh = refresh });
    }

    /// <summary>Captures cinematic context when needed, subscribes, and fills the initial label.</summary>
    public override void _Ready()
    {
        _service = InputPromptService.Instance;
        if (_service != null && _cinematicAction != null)
        {
            _previousContext = _service.Context;
            _previousActions = _service.ContextActions;
            _service.SetContext("cinematic", _cinematicAction);
        }
        if (_service != null) _service.PromptsChanged += Refresh;
        Refresh();
    }

    /// <summary>Refreshes retained presentation when translations change in place.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && IsInsideTree()) Refresh();
    }

    /// <summary>Invokes only the display callback owned by this listener.</summary>
    private void Refresh() => _refresh?.Invoke();

    /// <summary>Disconnects before restoring a cinematic's previous prompt context.</summary>
    public override void _ExitTree()
    {
        if (_service != null && GodotObject.IsInstanceValid(_service)) _service.PromptsChanged -= Refresh;
        if (_service != null && GodotObject.IsInstanceValid(_service) && _previousContext != null &&
            _service.Context == "cinematic" && Array.IndexOf(_service.ContextActions, _cinematicAction) >= 0)
            _service.SetContext(_previousContext, _previousActions);
        _refresh = null;
    }

    /// <summary>Returns the active presentation's bindings with an explicit missing-service fallback.</summary>
    public static string Binding(string action) => InputPromptService.Instance?.Binding(action) ?? TranslationServer.Translate("Unbound");
    /// <summary>Pairs a translated action title with current binding alternatives.</summary>
    public static string Text(string action, string label) => $"{Binding(action)}: {TranslationServer.Translate(label)}";

    /// <summary>Like <see cref="Binding"/>, with button-image markers for <see cref="InputGlyphText.Set"/>.</summary>
    public static string MarkedBinding(string action) => InputPromptService.Instance?.MarkedBinding(action) ?? TranslationServer.Translate("Unbound");
    /// <summary>Like <see cref="Text"/>, with button-image markers for <see cref="InputGlyphText.Set"/>.</summary>
    public static string Marked(string action, string label) => $"{MarkedBinding(action)}: {TranslationServer.Translate(label)}";

    /// <summary>
    /// Keeps touch instructions until a deliberate desktop/controller event arrives. Mouse events that
    /// Godot copies from touches (device <see cref="InputEvent.DeviceIdEmulation"/>) are not deliberate
    /// mouse use; counting them would switch back to keyboard hints right after every tap.
    /// </summary>
    public static bool DeliberateNonTouch(InputEvent input) =>
        !(input is InputEventMouse && input.Device == InputEvent.DeviceIdEmulation) &&
        (input is InputEventKey { Pressed: true, Echo: false }
        or InputEventMouseButton { Pressed: true } or InputEventJoypadButton { Pressed: true } ||
        input is InputEventMouseMotion mouse && mouse.Relative.LengthSquared() >= 16 ||
        input is InputEventJoypadMotion axis && (axis.Axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight
            ? axis.AxisValue > .5f : Math.Abs(axis.AxisValue) > .5f));

    /// <summary>Uses each cinematic's existing accepted shortcuts; playback and Skip handling are unchanged.</summary>
    public static void Cinematic(Button button, bool allowBack)
    {
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        button.OffsetLeft = -420; button.OffsetRight = -24;
        button.OffsetTop = 24; button.OffsetBottom = 104;
        string action = allowBack ? "cinematic_skip_back" : "cinematic_skip";
        button.AddChild(new InputPromptBinding { _cinematicAction = action,
            _refresh = () => InputGlyphText.SetButton(button, Marked(action, "Skip")) });
    }
}
