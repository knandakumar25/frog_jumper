using Godot;
using System.Collections.Generic;

/// <summary>Where a touch button sits: in the main arc at the bottom right, or in the small row of secondary buttons above the arc.</summary>
public enum TouchButtonZone { Arc, TopRow }

/// <summary>One on-screen button: the InputMap action it presses, its translation key and where it sits.</summary>
/// <param name="Action">InputMap action pressed while the button is held.</param>
/// <param name="Label">Source text, translated at draw time (for example "Jump").</param>
/// <param name="Zone">Placement zone.</param>
public sealed record TouchButtonSpec(string Action, string Label, TouchButtonZone Zone = TouchButtonZone.Arc);

/// <summary>
/// The on-screen controls for one play mode: the four joystick actions (any may be null when that
/// direction is not used) and the buttons. Every action is an existing InputMap action, so the touch
/// controls feed the same action layer as the keyboard and controllers.
/// Engine-free apart from <see cref="Vector2"/>, so NUnit can test it.
/// </summary>
public sealed class TouchLayout
{
    /// <summary>Short name used in node names and tests.</summary>
    public string Name { get; }
    /// <summary>Joystick action for left, right, up and down; null means that direction does nothing.</summary>
    public string? Left { get; }
    /// <inheritdoc cref="Left"/>
    public string? Right { get; }
    /// <inheritdoc cref="Left"/>
    public string? Up { get; }
    /// <inheritdoc cref="Left"/>
    public string? Down { get; }
    /// <summary>The buttons in placement order (the arc fills from the bottom-right corner outward).</summary>
    public IReadOnlyList<TouchButtonSpec> Buttons { get; }

    private TouchLayout(string name, string? left, string? right, string? up, string? down, params TouchButtonSpec[] buttons)
    {
        Name = name; Left = left; Right = right; Up = up; Down = down; Buttons = buttons;
    }

    /// <summary>
    /// Platformer missions 1-12. The stick only moves sideways: "jump" is also bound to stick-up for
    /// controllers, but a thumb drifting upward on a touch stick must not jump, so Up/Down are unused.
    /// </summary>
    public static readonly TouchLayout Platformer = new("platformer", "left", "right", null, null,
        new TouchButtonSpec("jump", "Jump"), new TouchButtonSpec("attack", "Attack"),
        new TouchButtonSpec("karate", "Ability"), new TouchButtonSpec("dash", "Dash"),
        new TouchButtonSpec("escape", "Pause", TouchButtonZone.TopRow),
        new TouchButtonSpec("squad_next", "Squad", TouchButtonZone.TopRow));

    /// <summary>The top-down mission: eight-way movement; aim follows movement while touch is in use.</summary>
    public static readonly TouchLayout Isometric = new("isometric", "iso_move_left", "iso_move_right", "iso_move_up", "iso_move_down",
        new TouchButtonSpec("iso_primary_attack", "Attack"), new TouchButtonSpec("iso_special_ability", "Ability"),
        new TouchButtonSpec("iso_dash", "Dash"), new TouchButtonSpec("iso_interact", "Use"),
        new TouchButtonSpec("iso_pause", "Pause", TouchButtonZone.TopRow));

    /// <summary>The open world (Little Tokyo / Concrete Wards). Guard and Run are held, like their keys.</summary>
    public static readonly TouchLayout World = new("world", "world_left", "world_right", "world_up", "world_down",
        new TouchButtonSpec("world_attack", "Attack"), new TouchButtonSpec("world_dodge", "Dodge"),
        new TouchButtonSpec("world_defend", "Guard"), new TouchButtonSpec("world_interact", "Interact"),
        new TouchButtonSpec("world_run", "Run"), new TouchButtonSpec("world_bike", "Bike"),
        new TouchButtonSpec("world_map", "Map", TouchButtonZone.TopRow),
        new TouchButtonSpec("world_swap", "Squad", TouchButtonZone.TopRow));

    /// <summary>Every action this layout can press (joystick directions first, then buttons).</summary>
    public IEnumerable<string> Actions()
    {
        foreach (string? a in new[] { Left, Right, Up, Down }) if (a != null) yield return a;
        foreach (var b in Buttons) yield return b.Action;
    }

    /// <summary>
    /// Splits a joystick vector (length 0-1, +Y down as in Godot) into action strengths. Each used
    /// direction gets the positive part of its axis; unused directions are left out, so an unused
    /// axis never presses anything. A vector shorter than <paramref name="deadZone"/> releases all.
    /// </summary>
    public Dictionary<string, float> StickStrengths(Vector2 stick, float deadZone = 0.15f)
    {
        var result = new Dictionary<string, float>();
        bool idle = stick.Length() < deadZone;
        void Put(string? action, float value) { if (action != null) result[action] = idle ? 0f : Mathf.Clamp(value, 0f, 1f); }
        Put(Left, -stick.X);
        Put(Right, stick.X);
        Put(Up, -stick.Y);
        Put(Down, stick.Y);
        return result;
    }
}

/// <summary>What kind of input just happened, for deciding whether touch controls show.</summary>
public enum TouchInputKind { None, Touch, KeyOrPadOrMouse }

/// <summary>
/// Show/hide rule for the on-screen controls (owner decision, 2026-11-17): on iOS/Android they start
/// visible; elsewhere they start hidden. A touch shows them; a deliberate key, controller or real mouse
/// press hides them. Mouse events copied from touches (Godot's touch-to-mouse emulation) count as
/// nothing, otherwise every tap would hide the controls it just showed.
/// </summary>
public static class TouchVisibility
{
    /// <summary>Initial visibility: true on mobile builds.</summary>
    public static bool Initial(bool mobile) => mobile;

    /// <summary>Next visibility after an input of the given kind.</summary>
    public static bool Next(bool visible, TouchInputKind kind) => kind switch
    {
        TouchInputKind.Touch => true,
        TouchInputKind.KeyOrPadOrMouse => false,
        _ => visible,
    };

    /// <summary>
    /// Classifies a live input event. Touches show the controls; presses from a keyboard, a controller
    /// or a real mouse hide them (using the same deliberate-input filter as the prompt service, which
    /// already ignores emulated mouse events, resting sticks and small mouse jitter).
    /// </summary>
    public static TouchInputKind Classify(InputEvent input) => input switch
    {
        InputEventScreenTouch { Pressed: true } => TouchInputKind.Touch,
        _ when InputPromptBinding.DeliberateNonTouch(input) => TouchInputKind.KeyOrPadOrMouse,
        _ => TouchInputKind.None,
    };

    /// <summary>True on iOS and Android builds (Godot's "android" and "ios" feature tags).</summary>
    public static bool IsMobileBuild() => OS.HasFeature("android") || OS.HasFeature("ios");
}
