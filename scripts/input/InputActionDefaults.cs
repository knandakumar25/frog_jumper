using Godot;

/// <summary>Owns project and runtime defaults. Scene entry never adds bindings back after a remap.</summary>
public static class InputActionDefaults
{
    private static bool _initialized;

    /// <summary>Runs before saved keyboard overrides; later scene calls are inert.</summary>
    public static void Initialize()
    {
        if (_initialized) return;
        RegisterDefaults();
        _initialized = true;
    }

    /// <summary>Reconstructs project and runtime actions together, including actions from exited scenes.</summary>
    public static void Reset()
    {
        InputMap.LoadFromProjectSettings();
        RegisterDefaults();
        _initialized = true;
    }

    /// <summary>Matches a nonrepeating press, preferring specific chords while allowing combat during held modifiers.</summary>
    public static bool Matches(InputEvent input, string action)
    {
        if (!InputMap.HasAction(action) || input.IsEcho()) return false;
        if (input is not InputEventKey key) return input.IsActionPressed(action, false, true);
        if (!key.Pressed) return false;
        foreach (var candidate in InputMap.ActionGetEvents(action))
        {
            if (candidate is not InputEventKey binding) continue;
            Key code = binding.PhysicalKeycode != Key.None ? binding.PhysicalKeycode : binding.Keycode;
            Key actual = binding.PhysicalKeycode != Key.None && key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            if (actual != code) continue;
            // OS events mark a modifier pressed when that modifier itself goes down.
            if (binding.ShiftPressed && !key.ShiftPressed) continue;
            if (binding.CtrlPressed && !key.CtrlPressed) continue;
            if (binding.AltPressed && !key.AltPressed) continue;
            if (binding.MetaPressed && !key.MetaPressed) continue;
            // Tab has a more-specific reverse-cycle chord. Other unmodified combat
            // aliases retain the former raw-key behavior while Shift is held after a dash.
            if (code == Key.Tab && key.ShiftPressed != binding.ShiftPressed) continue;
            return true;
        }
        return false;
    }

    /// <summary>Adds all shipped aliases once and makes controller bindings device independent.</summary>
    /// <summary>Installs the shared runtime action catalog, preserving shipped aliases and normalizing pad device IDs.</summary>
    private static void RegisterDefaults()
    {
        // Old project pad IDs must not restrict gameplay to pad zero.
        foreach (StringName action in InputMap.GetActions())
        foreach (var input in InputMap.ActionGetEvents(action))
        {
            if (input is not (InputEventJoypadButton or InputEventJoypadMotion) || input.Device == -1) continue;
            InputMap.ActionEraseEvent(action, input);
            var wildcard = (InputEvent)input.Duplicate();
            wildcard.Device = -1;
            Add(action, wildcard);
        }
        Direction("left", Key.A, JoyAxis.LeftX, -1, JoyButton.DpadLeft);
        Add("left", Keyboard(Key.Left));
        Direction("right", Key.D, JoyAxis.LeftX, 1, JoyButton.DpadRight);
        Add("right", Keyboard(Key.Right));
        Direction("forward", Key.W, JoyAxis.LeftY, -1);
        Direction("backward", Key.S, JoyAxis.LeftY, 1);
        Buttons("jump", Key.Space, JoyButton.A, JoyButton.DpadUp);
        Add("jump", Keyboard(Key.Up));
        Add("jump", Axis(JoyAxis.LeftY, -1));
        Buttons("attack", Key.F, JoyButton.X, JoyButton.B);
        Buttons("karate", Key.Q, JoyButton.Y, JoyButton.LeftShoulder);
        Buttons("dash", Key.Shift, JoyButton.RightShoulder);
        Add("dash", Keyboard(Key.C));
        Buttons("escape", Key.Escape, JoyButton.Start, JoyButton.Back);
        Buttons("squad_next", Key.Tab, JoyButton.DpadDown);
        Add("squad_previous", Keyboard(Key.Tab, shift: true));
        Add("squad_previous", Button(JoyButton.Back));
        for (int slot = 1; slot <= 6; slot++) Add("squad_slot_" + slot, Keyboard((Key)((int)Key.Key1 + slot - 1)));

        Direction("iso_move_left", Key.A, JoyAxis.LeftX, -1, deadzone: .25f);
        Direction("iso_move_right", Key.D, JoyAxis.LeftX, 1, deadzone: .25f);
        Direction("iso_move_up", Key.W, JoyAxis.LeftY, -1, deadzone: .25f);
        Direction("iso_move_down", Key.S, JoyAxis.LeftY, 1, deadzone: .25f);
        Direction("iso_aim_left", Key.None, JoyAxis.RightX, -1, deadzone: .25f);
        Direction("iso_aim_right", Key.None, JoyAxis.RightX, 1, deadzone: .25f);
        Direction("iso_aim_up", Key.None, JoyAxis.RightY, -1, deadzone: .25f);
        Direction("iso_aim_down", Key.None, JoyAxis.RightY, 1, deadzone: .25f);
        Add("iso_primary_attack", Keyboard(Key.F), .25f);
        Add("iso_primary_attack", new InputEventMouseButton { ButtonIndex = MouseButton.Left, Device = -1 });
        Add("iso_primary_attack", Axis(JoyAxis.TriggerLeft, 1));
        Add("iso_special_ability", Keyboard(Key.Q), .25f);
        Add("iso_special_ability", new InputEventMouseButton { ButtonIndex = MouseButton.Right, Device = -1 });
        Add("iso_special_ability", Axis(JoyAxis.TriggerRight, 1));
        Buttons("iso_dash", Key.Shift, JoyButton.RightShoulder);
        Buttons("iso_interact", Key.E, JoyButton.A);
        Buttons("iso_pause", Key.Escape, JoyButton.Start);

        Direction("world_left", Key.A, JoyAxis.LeftX, -1, JoyButton.DpadLeft, .2f);
        Direction("world_right", Key.D, JoyAxis.LeftX, 1, JoyButton.DpadRight, .2f);
        Direction("world_up", Key.W, JoyAxis.LeftY, -1, JoyButton.DpadUp, .2f);
        Direction("world_down", Key.S, JoyAxis.LeftY, 1, JoyButton.DpadDown, .2f);
        Buttons("world_run", Key.Shift, JoyButton.LeftStick);
        Buttons("world_map", Key.M, JoyButton.Back);
        Buttons("world_interact", Key.E, JoyButton.A);
        Buttons("world_swap", Key.Tab, JoyButton.Y);
        Buttons("world_zoom_in", Key.Equal, JoyButton.RightShoulder);
        Buttons("world_zoom_out", Key.Minus, JoyButton.LeftShoulder);
        Buttons("world_bike", Key.V, JoyButton.X);
        Buttons("world_attack", Key.F, JoyButton.B);
        Buttons("world_dodge", Key.Space, JoyButton.RightShoulder);
        Buttons("world_defend", Key.Q, JoyButton.LeftShoulder);

        Buttons("cinematic_skip", Key.Escape, JoyButton.Start, JoyButton.A);
        Add("cinematic_skip", Keyboard(Key.Space));
        Add("cinematic_skip", Keyboard(Key.Enter));
        Buttons("cinematic_skip_back", Key.Escape, JoyButton.Start, JoyButton.A, JoyButton.B);
        Add("cinematic_skip_back", Keyboard(Key.Space));
        Add("cinematic_skip_back", Keyboard(Key.Enter));
        Buttons("story_advance", Key.Space, JoyButton.A, JoyButton.X);
        Add("story_advance", Keyboard(Key.Enter));
        Buttons("story_skip", Key.Escape, JoyButton.Start, JoyButton.Back, JoyButton.Y, JoyButton.B);
        Buttons("home_start", Key.Enter, JoyButton.A, JoyButton.Start);
        Add("home_start", Keyboard(Key.Space));
        Add("home_start", new InputEventMouseButton { ButtonIndex = MouseButton.Left, Device = -1 });
        Add("deployment_skip", Button(JoyButton.Start));
    }

    /// <summary>Builds a physical keyboard binding with its required modifiers.</summary>
    /// <summary>Builds a layout-independent physical key binding, optionally a Shift chord.</summary>
    private static InputEventKey Keyboard(Key key, bool shift = false) => new() { PhysicalKeycode = key, ShiftPressed = shift, Device = -1 };
    /// <summary>Builds a button binding usable by every connected pad.</summary>
    /// <summary>Builds a controller button binding accepted from any device.</summary>
    private static InputEventJoypadButton Button(JoyButton button) => new() { ButtonIndex = button, Device = -1 };
    /// <summary>Builds a signed axis binding accepted from any device.</summary>
    /// <summary>Builds one signed axis binding usable by every connected pad.</summary>
    private static InputEventJoypadMotion Axis(JoyAxis axis, float value) => new() { Axis = axis, AxisValue = value, Device = -1 };
    /// <summary>Adds a keyboard default and every pad alias to an action.</summary>
    /// <summary>Registers a keyboard binding and all controller button aliases.</summary>
    private static void Buttons(string action, Key key, params JoyButton[] buttons)
    {
        Add(action, Keyboard(key));
        foreach (var button in buttons) Add(action, Button(button));
    }
    /// <summary>Registers one signed direction with optional key and D-pad bindings.</summary>
    /// <summary>Adds an optional key, signed stick direction and optional D-pad alternative.</summary>
    private static void Direction(string action, Key key, JoyAxis axis, float value, JoyButton button = JoyButton.Invalid, float deadzone = .5f)
    {
        if (key != Key.None) Add(action, Keyboard(key), deadzone);
        Add(action, Axis(axis, value), deadzone);
        if (button != JoyButton.Invalid) Add(action, Button(button), deadzone);
    }
    /// <summary>Creates a missing action and avoids duplicate default events.</summary>
    /// <summary>Creates missing actions and adds only binding events that are not already present.</summary>
    private static void Add(StringName action, InputEvent input, float deadzone = .5f)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action, deadzone);
        if (!InputMap.ActionHasEvent(action, input)) InputMap.ActionAddEvent(action, input);
    }
}
