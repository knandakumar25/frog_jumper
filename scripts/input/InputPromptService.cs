using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum PromptInputKind { Keyboard, MouseButton, ControllerButton, ControllerAxis }
public enum PromptControllerFamily { Unknown, Xbox, PlayStation, Nintendo }

/// <summary>
/// A binding's physical identity, current localized text and, for controller bindings when an image
/// set is active, the button image path (<see cref="GlyphPath"/>; null means draw <see cref="Label"/>).
/// </summary>
public sealed record InputBindingToken(PromptInputKind Kind, int PhysicalCode, float AxisDirection,
    bool Shift, bool Ctrl, bool Alt, bool Meta, string Label, string? GlyphPath = null);

/// <summary>Owns prompt presentation, never input consumption or gameplay functions.</summary>
public partial class InputPromptService : Node
{
    public static InputPromptService? Instance { get; private set; }
    [Signal] public delegate void PromptsChangedEventHandler();
    public bool UsingController { get; private set; }
    public bool IsGamepad => UsingController && Style != ControllerGlyphStyle.Keyboard;
    public int ActiveController { get; private set; } = -1;
    public ControllerGlyphStyle Style { get; private set; } = ControllerGlyphStyle.AutoDetect;
    public string Context { get; private set; } = "menu";
    public string[] ContextActions => _actions.ToArray();
    private string[] _actions = { "ui_up", "ui_down", "ui_accept", "ui_cancel" };
    private string _locale = "";

    /// <summary>Registers defaults before Global can restore keyboard overrides.</summary>
    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        InputActionDefaults.Initialize();
    }

    /// <summary>Observes hardware connection changes and defers persisted settings until all autoloads exist.</summary>
    public override void _Ready()
    {
        Input.JoyConnectionChanged += OnConnectionChanged;
        _locale = TranslationServer.GetLocale();
        Callable.From(LoadSettings).CallDeferred();
    }

    /// <summary>Releases the native signal subscription and singleton reference.</summary>
    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnConnectionChanged;
        if (Instance == this) Instance = null;
    }

    /// <summary>Refreshes bound controls when Godot changes translations in place.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && IsInsideTree()) NotifyBindingsChanged();
    }

    /// <summary>Catches direct locale changes made outside LocaleService without replaying gameplay.</summary>
    public override void _Process(double delta)
    {
        // Also catches a direct TranslationServer change made outside LocaleService.
        string locale = TranslationServer.GetLocale();
        if (_locale != locale) { _locale = locale; NotifyBindingsChanged(); }
    }

    /// <summary>Tracks events that scene owners have not already observed.</summary>
    public override void _Input(InputEvent input) => ObserveInput(input);

    /// <summary>Idempotently selects a device from deliberate input; never consumes an event.</summary>
    public void ObserveInput(InputEvent input)
    {
        bool controller;
        switch (input)
        {
            case InputEventKey key when key.Pressed && !key.Echo: controller = false; break;
            case InputEventMouseButton mouse when mouse.Pressed: controller = false; break;
            case InputEventMouseMotion mouse when mouse.Relative.LengthSquared() >= 16f: controller = false; break;
            case InputEventJoypadButton button when button.Pressed: controller = true; break;
            case InputEventJoypadMotion motion when MeaningfulMotion(motion): controller = true; break;
            default: return;
        }
        int device = controller ? input.Device : ActiveController;
        bool changed = controller != UsingController || (controller && device != ActiveController);
        UsingController = controller;
        if (controller) ActiveController = device;
        UpdatePointer();
        if (changed) NotifyBindingsChanged();
    }

    /// <summary>Filters resting sticks by action deadzones and triggers by their positive press threshold.</summary>
    private bool MeaningfulMotion(InputEventJoypadMotion motion)
    {
        if (motion.Axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight) return motion.AxisValue > 0.5f;
        float threshold = 0.4f;
        foreach (string action in _actions)
        {
            if (!InputMap.HasAction(action)) continue;
            foreach (var ev in InputMap.ActionGetEvents(action))
                if (ev is InputEventJoypadMotion axis && axis.Axis == motion.Axis &&
                    Math.Sign(axis.AxisValue) == Math.Sign(motion.AxisValue))
                    threshold = Math.Max(threshold, InputMap.ActionGetDeadzone(action));
        }
        return Math.Abs(motion.AxisValue) > threshold;
    }

    /// <summary>Updates presentation only for changes to the active controller.</summary>
    private void OnConnectionChanged(long device, bool connected)
    {
        if (!connected) ControllerDisconnected((int)device);
        else if (device == ActiveController) NotifyBindingsChanged();
    }

    /// <summary>Falls back to keyboard labels when the active pad is unplugged; another pad must send input first.</summary>
    public void ControllerDisconnected(int device)
    {
        if (device != ActiveController) return;
        ActiveController = -1;
        UsingController = false;
        UpdatePointer();
        NotifyBindingsChanged();
    }

    /// <summary>Shows pointers for menu/world/isometric use and hides them during pad or platformer use.</summary>
    private void UpdatePointer()
    {
        if (DisplayServer.GetName() == "headless") return;
        Input.MouseMode = UsingController || Context == "platformer" ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
    }

    /// <summary>Replaces the visible screen's explicit actions and updates pointer policy.</summary>
    public void SetContext(string context, params string[] actions)
    {
        if (context == Context && _actions.SequenceEqual(actions)) return;
        Context = context;
        _actions = actions.ToArray();
        UpdatePointer();
        NotifyBindingsChanged();
    }

    /// <summary>Variant-compatible context entry point for GDScript screens.</summary>
    public void SetPromptContext(string context, Godot.Collections.Array<string> actions) => SetContext(context, actions.ToArray());
    /// <summary>Formats the current screen's action list for the compatibility banner.</summary>
    public string GetContextText() => string.Join("   |   ", _actions.Select(a => Describe(a, ActionLabel(a))));

    /// <summary>Changes labels without disabling devices; optionally persists only the style, never device history.</summary>
    public void SetStyle(int value, bool persist = true)
    {
        Style = Enum.IsDefined(typeof(ControllerGlyphStyle), value) ? (ControllerGlyphStyle)value : ControllerGlyphStyle.AutoDetect;
        if (persist && IsInsideTree())
        {
            var manager = GetNodeOrNull<SessionManager>("/root/SessionManager");
            if (manager != null && !manager.WriteState("input_prompt_settings", new Godot.Collections.Dictionary { ["glyph_style"] = (int)Style }))
                GD.PushWarning("Input prompt settings were not saved. PostgreSQL must be available.");
        }
        NotifyBindingsChanged();
    }

    /// <summary>Loads the separate settings document; absent or malformed styles resolve to Automatic.</summary>
    private void LoadSettings()
    {
        var data = GetNodeOrNull<SessionManager>("/root/SessionManager")?.ReadState("input_prompt_settings");
        int value = 0;
        if (data != null && data.TryGetValue("glyph_style", out var saved) &&
            (saved.VariantType == Variant.Type.Int || saved.VariantType == Variant.Type.Float) &&
            double.IsInteger(saved.AsDouble()) && saved.AsDouble() >= 0 && saved.AsDouble() <= (int)ControllerGlyphStyle.XboxOne)
            value = saved.AsInt32();
        SetStyle(value, false);
    }

    /// <summary>Publishes a presentation invalidation after a complete mutation batch.</summary>
    public void NotifyBindingsChanged()
    {
        if (IsInsideTree()) EmitSignal(SignalName.PromptsChanged);
    }

    /// <summary>Reconstructs project and runtime actions before notifying screens; does not write storage.</summary>
    public void ResetDefaults() { InputActionDefaults.Reset(); NotifyBindingsChanged(); }
    /// <summary>Replaces keyboard events only, retaining all controller and mouse bindings.</summary>
    public void ReplaceKeyboardBinding(string action, InputEventKey key)
    {
        if (!InputMap.HasAction(action)) return;
        foreach (var ev in InputMap.ActionGetEvents(action))
            if (ev is InputEventKey) InputMap.ActionEraseEvent(action, ev);
        InputMap.ActionAddEvent(action, (InputEventKey)key.Duplicate());
        NotifyBindingsChanged();
    }

    /// <summary>Serializes the four remappable keyboard actions including chord modifiers.</summary>
    public Godot.Collections.Dictionary CaptureKeyboardBindings()
    {
        var result = new Godot.Collections.Dictionary();
        foreach (string action in new[] { "left", "right", "jump", "escape" })
        {
            var keys = new Godot.Collections.Array();
            foreach (var ev in InputMap.ActionGetEvents(action))
                if (ev is InputEventKey key) keys.Add(new Godot.Collections.Dictionary {
                    ["physical"] = (long)key.PhysicalKeycode, ["keycode"] = (long)key.Keycode,
                    ["shift"] = key.ShiftPressed, ["ctrl"] = key.CtrlPressed,
                    ["alt"] = key.AltPressed, ["meta"] = key.MetaPressed });
            result[action] = keys;
        }
        return result;
    }

    /// <summary>Restores current chord records or legacy numeric keys without removing controller events.</summary>
    public void ApplyKeyboardBindings(Godot.Collections.Dictionary data)
    {
        foreach (string action in new[] { "left", "right", "jump", "escape" })
        {
            if (!data.TryGetValue(action, out var saved) || saved.VariantType != Variant.Type.Array) continue;
            var keys = new List<InputEventKey>();
            foreach (var item in saved.AsGodotArray())
            {
                if (item.VariantType == Variant.Type.Dictionary)
                {
                    var fields = item.AsGodotDictionary();
                    var key = new InputEventKey {
                        PhysicalKeycode = (Key)fields.GetValueOrDefault("physical", 0).AsInt64(),
                        Keycode = (Key)fields.GetValueOrDefault("keycode", 0).AsInt64(),
                        ShiftPressed = fields.GetValueOrDefault("shift", false).AsBool(),
                        CtrlPressed = fields.GetValueOrDefault("ctrl", false).AsBool(),
                        AltPressed = fields.GetValueOrDefault("alt", false).AsBool(),
                        MetaPressed = fields.GetValueOrDefault("meta", false).AsBool() };
                    if (key.PhysicalKeycode != Key.None || key.Keycode != Key.None) keys.Add(key);
                }
                else if (item.VariantType is Variant.Type.Int or Variant.Type.Float && item.AsInt64() > 0)
                    keys.Add(new InputEventKey { PhysicalKeycode = (Key)item.AsInt64() });
            }
            // An explicit empty list is an unbound keyboard action; malformed nonempty data is ignored.
            if (keys.Count == 0 && saved.AsGodotArray().Count != 0) continue;
            foreach (var ev in InputMap.ActionGetEvents(action))
                if (ev is InputEventKey) InputMap.ActionEraseEvent(action, ev);
            foreach (var key in keys) InputMap.ActionAddEvent(action, key);
        }
        NotifyBindingsChanged();
    }
    /// <summary>Variant-compatible active-device binding lookup.</summary>
    public string GetBinding(string action) => Binding(action);
    /// <summary>Variant-compatible lookup for an explicit device family, used by the keyboard remapper.</summary>
    public string GetBindingForDevice(string action, bool controller) => Binding(action, controller);
    /// <summary>Shows every distinct current binding, or a localized Unbound label.</summary>
    public string Binding(string action, bool? controller = null) => string.Join(" / ", Tokens(action, controller).Select(t => t.Label).Distinct());

    /// <summary>
    /// Like <see cref="Binding"/>, but controller bindings that have an image come back as
    /// <see cref="InputGlyphText"/> markers. Only send the result to a label through
    /// <see cref="InputGlyphText.Set"/> or <see cref="InputGlyphText.SetButton"/>.
    /// </summary>
    public string MarkedBinding(string action, bool? controller = null) => string.Join(" / ",
        Tokens(action, controller).Select(t => t.GlyphPath == null ? t.Label : InputGlyphText.Mark(t.GlyphPath, t.Label)).Distinct());

    /// <summary>Marked version of <see cref="Describe"/> ("Label: [ image ]").</summary>
    public string MarkedDescribe(string action, string label) => $"{label}: [ {MarkedBinding(action)} ]";

    /// <summary>
    /// BBCode for one action's bindings, for GDScript labels that set <c>bbcode_enabled</c>.
    /// Images are <paramref name="height"/> pixels tall; text is escaped.
    /// </summary>
    public string GetRichBinding(string action, int height) => InputGlyphText.ToBbcode(MarkedBinding(action), height);

    /// <summary>
    /// Shows "label: [ bindings ]" on a GDScript-owned button, drawing controller buttons as images
    /// (see <see cref="InputGlyphText.SetButton"/>). <paramref name="label"/> must already be translated.
    /// </summary>
    public void SetActionButton(Button button, string label, string action) =>
        InputGlyphText.SetButton(button, $"{label}: [ {MarkedBinding(action)} ]");

    /// <summary>BBCode version of <see cref="GetContextText"/> for the shared prompt banner.</summary>
    public string GetRichContextText(int height) => InputGlyphText.ToBbcode(
        string.Join("   |   ", _actions.Select(a => MarkedDescribe(a, ActionLabel(a)))), height);

    /// <summary>
    /// The button-image set used for controller prompts right now: none for Keyboard style or while
    /// the keyboard is in use; otherwise a style forced in the Controls menu, then the console the
    /// game was exported for, then the connected controller (closest set; unknown pads get none).
    /// </summary>
    public ControllerGlyphSet ActiveGlyphSet => GlyphSetFor(IsGamepad);

    /// <summary>Resolves the image set for a controller (pad) lookup; keyboard lookups never get images.</summary>
    private ControllerGlyphSet GlyphSetFor(bool pad)
    {
        if (!pad || Style == ControllerGlyphStyle.Keyboard) return ControllerGlyphSet.None;
        if (ControllerGlyphCatalog.ForcedSet(Style) is { } forced) return forced;
        if (ControllerGlyphCatalog.PlatformSet(f => OS.HasFeature(f)) is { } platform) return platform;
        return DetectActiveSet();
    }

    /// <summary>Reads the active pad's name and, where the platform reports them, its USB vendor/product IDs.</summary>
    private ControllerGlyphSet DetectActiveSet()
    {
        if (ActiveController < 0) return ControllerGlyphSet.None;
        int vendor = 0, product = 0;
        var info = Input.GetJoyInfo(ActiveController);
        if (info.TryGetValue("vendor_id", out var v) && v.VariantType is Variant.Type.Int or Variant.Type.Float) vendor = v.AsInt32();
        if (info.TryGetValue("product_id", out var p) && p.VariantType is Variant.Type.Int or Variant.Type.Float) product = p.AsInt32();
        return ControllerGlyphCatalog.Detect(Input.GetJoyName(ActiveController), vendor, product);
    }
    /// <summary>Pairs a localized action title with readable bracketed bindings.</summary>
    public string Describe(string action, string label) => $"{label}: [ {Binding(action)} ]";
    /// <summary>Joins a screen-owned action list with spacing suitable for wrapping labels.</summary>
    public string Join(params (string Action, string Label)[] actions) => string.Join("   |   ", actions.Select(a => Describe(a.Action, a.Label)));

    /// <summary>Returns physical binding data independently of how a future renderer draws it.</summary>
    public IReadOnlyList<InputBindingToken> Tokens(string action, bool? controller = null)
    {
        bool pad = controller ?? IsGamepad;
        var glyphs = GlyphSetFor(pad);
        var tokens = new List<InputBindingToken>();
        // StoryIntro also accepts the remappable pause and UI-cancel actions.
        var sources = action switch {
            "story_skip" => new[] { action, "escape", "ui_cancel" },
            "deployment_skip" => new[] { action, "ui_cancel", "ui_accept" },
            _ => new[] { action } };
        foreach (string source in sources)
        if (InputMap.HasAction(source))
            foreach (var input in InputMap.ActionGetEvents(source))
            {
                bool isPad = input is InputEventJoypadButton or InputEventJoypadMotion;
                if (pad != isPad) continue;
                var token = Token(input, glyphs);
                if (token != null && !tokens.Any(t => t == token)) tokens.Add(token);
            }
        if (tokens.Count == 0) tokens.Add(new(pad ? PromptInputKind.ControllerButton : PromptInputKind.Keyboard, -1, 0, false, false, false, false, T("Unbound")));
        return tokens;
    }

    /// <summary>Formats a captured event using the current keyboard layout and controller presentation.</summary>
    public string FormatBindingEvent(InputEvent input) => Token(input, ControllerGlyphSet.None)?.Label ?? T("Unbound");

    /// <summary>
    /// Converts supported engine events into stable physical tokens with localized display text and,
    /// for controller events, the image from <paramref name="glyphs"/> when that set has one.
    /// </summary>
    private InputBindingToken? Token(InputEvent input, ControllerGlyphSet glyphs)
    {
        string label;
        int code;
        float direction = 0;
        PromptInputKind kind;
        bool shift = false, ctrl = false, alt = false, meta = false;
        switch (input)
        {
            case InputEventKey key:
                kind = PromptInputKind.Keyboard;
                code = (int)(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode);
                var display = key.PhysicalKeycode != Key.None && DisplayServer.GetName() != "headless"
                    ? DisplayServer.KeyboardGetKeycodeFromPhysical(key.PhysicalKeycode) : (Key)code;
                label = T(OS.GetKeycodeString(display));
                shift = key.ShiftPressed; ctrl = key.CtrlPressed; alt = key.AltPressed; meta = key.MetaPressed;
                var modifiers = new List<string>();
                if (ctrl && display != Key.Ctrl) modifiers.Add(T("Ctrl"));
                if (alt && display != Key.Alt) modifiers.Add(T("Alt"));
                if (shift && display != Key.Shift) modifiers.Add(T("Shift"));
                if (meta && display != Key.Meta) modifiers.Add(T("Meta"));
                modifiers.Add(label);
                label = string.Join("+", modifiers);
                break;
            case InputEventMouseButton mouse:
                kind = PromptInputKind.MouseButton; code = (int)mouse.ButtonIndex;
                shift = mouse.ShiftPressed; ctrl = mouse.CtrlPressed; alt = mouse.AltPressed; meta = mouse.MetaPressed;
                label = T(mouse.ButtonIndex switch {
                    MouseButton.Left => "Left mouse", MouseButton.Right => "Right mouse", MouseButton.Middle => "Middle mouse",
                    MouseButton.WheelUp => "Wheel up", MouseButton.WheelDown => "Wheel down",
                    MouseButton.WheelLeft => "Wheel left", MouseButton.WheelRight => "Wheel right", _ => $"Mouse {code}" });
                if (meta) label = T("Meta") + "+" + label;
                if (shift) label = T("Shift") + "+" + label;
                if (alt) label = T("Alt") + "+" + label;
                if (ctrl) label = T("Ctrl") + "+" + label;
                break;
            case InputEventJoypadButton button:
                kind = PromptInputKind.ControllerButton; code = (int)button.ButtonIndex;
                label = T(ButtonLabel(code, Family()));
                break;
            case InputEventJoypadMotion axis:
                kind = PromptInputKind.ControllerAxis; code = (int)axis.Axis; direction = Math.Sign(axis.AxisValue);
                label = T(AxisLabel(axis.Axis, direction, Family()));
                break;
            default: return null;
        }
        return new(kind, code, direction, shift, ctrl, alt, meta, label,
            ControllerGlyphCatalog.Path(glyphs, kind, code, direction));
    }

    /// <summary>Combines a manual family preference, the console build and the active controller's identity.</summary>
    private PromptControllerFamily Family()
    {
        if (ControllerGlyphCatalog.ForcedSet(Style) is { } forced) return ControllerGlyphCatalog.FamilyOf(forced);
        if (ControllerGlyphCatalog.PlatformSet(f => OS.HasFeature(f)) is { } platform) return ControllerGlyphCatalog.FamilyOf(platform);
        return DetectFamily(ActiveController >= 0 ? Input.GetJoyName(ActiveController) : "");
    }

    /// <summary>Maps recognized reported identities, leaving unknown pads with neutral position labels.</summary>
    public static PromptControllerFamily DetectFamily(string identity)
    {
        string name = identity.ToLowerInvariant();
        if (name.Contains("playstation") || name.Contains("dualshock") || name.Contains("dualsense") || name.Contains("sony") || name.Contains("ps4") || name.Contains("ps5")) return PromptControllerFamily.PlayStation;
        if (name.Contains("nintendo") || name.Contains("switch") || name.Contains("joy-con")) return PromptControllerFamily.Nintendo;
        if (name.Contains("xbox") || name.Contains("xinput") || name.Contains("microsoft")) return PromptControllerFamily.Xbox;
        return PromptControllerFamily.Unknown;
    }

    /// <summary>Names physical positions without changing the button's gameplay assignment.</summary>
    public static string ButtonLabel(int code, PromptControllerFamily family)
    {
        if (code < 4 && code >= 0) return family switch {
            PromptControllerFamily.Xbox => new[] { "A", "B", "X", "Y" }[code],
            PromptControllerFamily.PlayStation => new[] { "Cross", "Circle", "Square", "Triangle" }[code],
            PromptControllerFamily.Nintendo => new[] { "B", "A", "Y", "X" }[code],
            _ => new[] { "South face", "East face", "West face", "North face" }[code] };
        return (JoyButton)code switch {
            JoyButton.Back => family == PromptControllerFamily.PlayStation ? "Create / Share" : family == PromptControllerFamily.Nintendo ? "Minus" : "Back / View",
            JoyButton.Start => family == PromptControllerFamily.PlayStation ? "Options" : family == PromptControllerFamily.Nintendo ? "Plus" : "Start / Menu",
            JoyButton.Guide => "Guide", JoyButton.LeftStick => "Left stick press", JoyButton.RightStick => "Right stick press",
            JoyButton.LeftShoulder => family == PromptControllerFamily.PlayStation ? "L1" : family == PromptControllerFamily.Nintendo ? "L" : family == PromptControllerFamily.Xbox ? "LB" : "Left shoulder",
            JoyButton.RightShoulder => family == PromptControllerFamily.PlayStation ? "R1" : family == PromptControllerFamily.Nintendo ? "R" : family == PromptControllerFamily.Xbox ? "RB" : "Right shoulder",
            JoyButton.DpadUp => "D-pad up", JoyButton.DpadDown => "D-pad down", JoyButton.DpadLeft => "D-pad left", JoyButton.DpadRight => "D-pad right",
            _ => $"Button {code}" };
    }

    /// <summary>Names signed stick directions and controller-family trigger positions.</summary>
    private static string AxisLabel(JoyAxis axis, float direction, PromptControllerFamily family) => axis switch {
        JoyAxis.LeftX => direction < 0 ? "Left stick left" : "Left stick right",
        JoyAxis.LeftY => direction < 0 ? "Left stick up" : "Left stick down",
        JoyAxis.RightX => direction < 0 ? "Right stick left" : "Right stick right",
        JoyAxis.RightY => direction < 0 ? "Right stick up" : "Right stick down",
        JoyAxis.TriggerLeft => family == PromptControllerFamily.PlayStation ? "L2" : family == PromptControllerFamily.Nintendo ? "ZL" : family == PromptControllerFamily.Xbox ? "LT" : "Left trigger",
        JoyAxis.TriggerRight => family == PromptControllerFamily.PlayStation ? "R2" : family == PromptControllerFamily.Nintendo ? "ZR" : family == PromptControllerFamily.Xbox ? "RT" : "Right trigger",
        _ => $"Axis {(int)axis} {direction:+0;-0;0}" };

    /// <summary>Localizes labels and short direction combinations with English fallback.</summary>
    private static string T(string source)
    {
        string translated = TranslationServer.Translate(source);
        if (translated != source) return translated;
        // Stick and wheel labels are short combinations of independently localized terms.
        foreach (string stem in new[] { "Left stick", "Right stick", "Wheel", "D-pad" })
            if (source.StartsWith(stem + " ", StringComparison.Ordinal))
            {
                string suffix = source.Substring(stem.Length + 1);
                suffix = char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
                return TranslationServer.Translate(stem) + " " + TranslationServer.Translate(suffix);
            }
        return source;
    }
    /// <summary>Provides user-facing action titles for the compatibility banner.</summary>
    private static string ActionLabel(string action) => T(action switch {
        "ui_up" => "Up", "ui_down" => "Down", "ui_left" => "Left", "ui_right" => "Right",
        "ui_accept" => "Select", "ui_cancel" => "Back", "escape" => "Pause", "jump" => "Jump",
        "home_start" => "Select", "story_advance" => "Advance", "story_skip" or "cinematic_skip" or "cinematic_skip_back" or "deployment_skip" => "Skip",
        "iso_primary_attack" or "world_attack" => "Attack", "iso_special_ability" => "Ability",
        "iso_dash" => "Dash", "iso_pause" => "Pause", "iso_interact" or "world_interact" => "Interact",
        "world_map" => "Map", "squad_next" or "world_swap" => "Switch Operative", "squad_previous" => "Previous operative",
        "world_zoom_in" => "Zoom in", "world_zoom_out" => "Zoom out", "world_run" => "Run",
        "iso_move_left" or "world_left" => "Left", "iso_move_right" or "world_right" => "Right",
        "iso_move_up" or "world_up" => "Up", "iso_move_down" or "world_down" => "Down",
        "world_bike" => "Bike", "world_dodge" => "Dodge", "world_defend" => "Guard",
        "attack" => "Attack", "karate" => "Ability", "dash" => "Dash", "left" => "Left", "right" => "Right", _ => action });
}
