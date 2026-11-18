using Godot;

/// <summary>
/// The controller button-image sets the game can show. <see cref="None"/> means "no images":
/// keyboard prompts and unrecognized controllers keep their text labels.
/// </summary>
public enum ControllerGlyphSet { None, Xbox360, XboxOne, XboxSeries, PS5, Switch2 }

/// <summary>
/// Engine-light lookup from a physical controller input to its button image, and from a reported
/// controller identity to an image set. The images are Kenney "Input Prompts" 1.5A (CC0), copied to
/// <c>res://assets/ui/input_prompts/&lt;set&gt;/&lt;button&gt;.png</c> at 128x128; see that folder's README
/// for the source-file mapping and the owner's 2026-11-17 exception to the SpriteCook-only art rule.
/// Nothing here touches the scene tree, so NUnit can call every method.
/// </summary>
public static class ControllerGlyphCatalog
{
    /// <summary>Root folder holding one subfolder per image set.</summary>
    public const string Root = "res://assets/ui/input_prompts/";

    /// <summary>USB vendor IDs used to pick a set before falling back to the reported name.</summary>
    private const int Microsoft = 0x045E, Sony = 0x054C, Nintendo = 0x057E;

    /// <summary>Folder name for a set, or null for <see cref="ControllerGlyphSet.None"/>.</summary>
    public static string? Folder(ControllerGlyphSet set) => set switch
    {
        ControllerGlyphSet.Xbox360 => "xbox_360",
        ControllerGlyphSet.XboxOne => "xbox_one",
        ControllerGlyphSet.XboxSeries => "xbox_series",
        ControllerGlyphSet.PS5 => "ps5",
        ControllerGlyphSet.Switch2 => "switch2",
        _ => null,
    };

    /// <summary>The text-label family that matches a set (used for the accessible text beside an image).</summary>
    public static PromptControllerFamily FamilyOf(ControllerGlyphSet set) => set switch
    {
        ControllerGlyphSet.Xbox360 or ControllerGlyphSet.XboxOne or ControllerGlyphSet.XboxSeries => PromptControllerFamily.Xbox,
        ControllerGlyphSet.PS5 => PromptControllerFamily.PlayStation,
        ControllerGlyphSet.Switch2 => PromptControllerFamily.Nintendo,
        _ => PromptControllerFamily.Unknown,
    };

    /// <summary>
    /// Returns the image path for one controller binding, or null when the set has no image for it
    /// (keyboard/mouse input, <see cref="ControllerGlyphSet.None"/>, an unmapped button such as PS Home,
    /// or an unusual axis). Face buttons follow Godot's physical positions (south, east, west, north),
    /// so a Nintendo pad's south button shows "B", matching <see cref="InputPromptService.ButtonLabel"/>.
    /// </summary>
    /// <param name="set">The image set to draw from.</param>
    /// <param name="kind">The binding kind; only controller buttons and axes have images.</param>
    /// <param name="code">The <see cref="JoyButton"/> or <see cref="JoyAxis"/> number.</param>
    /// <param name="direction">The sign of an axis binding (-1 or +1); ignored for buttons.</param>
    public static string? Path(ControllerGlyphSet set, PromptInputKind kind, int code, float direction)
    {
        string? folder = Folder(set);
        if (folder == null) return null;
        string? name = kind switch
        {
            PromptInputKind.ControllerButton => ButtonName((JoyButton)code, set),
            PromptInputKind.ControllerAxis => AxisName((JoyAxis)code, direction),
            _ => null,
        };
        return name == null ? null : Root + folder + "/" + name + ".png";
    }

    /// <summary>Every button file name a set provides (PS5 has no Guide image); used by tests to check the files.</summary>
    public static string[] FileNames(ControllerGlyphSet set)
    {
        var names = new System.Collections.Generic.List<string> {
            "face_south", "face_east", "face_west", "face_north", "back", "start",
            "shoulder_left", "shoulder_right", "trigger_left", "trigger_right", "stick_left_press", "stick_right_press" };
        if (set != ControllerGlyphSet.PS5) names.Add("guide");
        foreach (string d in new[] { "up", "down", "left", "right" })
        {
            names.Add("dpad_" + d);
            names.Add("stick_left_" + d);
            names.Add("stick_right_" + d);
        }
        return names.ToArray();
    }

    /// <summary>Maps a Godot button index to its file name; PS5 has no Home image, so Guide stays text there.</summary>
    private static string? ButtonName(JoyButton button, ControllerGlyphSet set) => button switch
    {
        JoyButton.A => "face_south", JoyButton.B => "face_east", JoyButton.X => "face_west", JoyButton.Y => "face_north",
        JoyButton.Back => "back", JoyButton.Start => "start",
        JoyButton.Guide => set == ControllerGlyphSet.PS5 ? null : "guide",
        JoyButton.LeftShoulder => "shoulder_left", JoyButton.RightShoulder => "shoulder_right",
        JoyButton.LeftStick => "stick_left_press", JoyButton.RightStick => "stick_right_press",
        JoyButton.DpadUp => "dpad_up", JoyButton.DpadDown => "dpad_down",
        JoyButton.DpadLeft => "dpad_left", JoyButton.DpadRight => "dpad_right",
        _ => null,
    };

    /// <summary>Maps a signed stick direction or a trigger to its file name. Godot's Y axes are negative upward.</summary>
    private static string? AxisName(JoyAxis axis, float direction) => axis switch
    {
        JoyAxis.LeftX => direction < 0 ? "stick_left_left" : "stick_left_right",
        JoyAxis.LeftY => direction < 0 ? "stick_left_up" : "stick_left_down",
        JoyAxis.RightX => direction < 0 ? "stick_right_left" : "stick_right_right",
        JoyAxis.RightY => direction < 0 ? "stick_right_up" : "stick_right_down",
        JoyAxis.TriggerLeft => "trigger_left",
        JoyAxis.TriggerRight => "trigger_right",
        _ => null,
    };

    /// <summary>
    /// Picks the closest image set for a connected controller (owner decision, 2026-11-17):
    /// Xbox 360, Xbox One and Xbox Series pads get their own set and any other Microsoft or XInput pad
    /// gets Series; every Sony pad (DualShock 4 included) gets PS5; every Nintendo pad gets Switch 2;
    /// anything else gets <see cref="ControllerGlyphSet.None"/> and keeps text.
    /// USB vendor/product IDs win when the platform reports them; the reported name is the fallback.
    /// </summary>
    /// <param name="name">The name from <c>Input.GetJoyName</c>; may be empty.</param>
    /// <param name="vendor">USB vendor ID, or 0 when unknown.</param>
    /// <param name="product">USB product ID, or 0 when unknown.</param>
    public static ControllerGlyphSet Detect(string name, int vendor, int product)
    {
        switch (vendor)
        {
            case Microsoft:
                return product switch
                {
                    // Wired, wireless-receiver and Play & Charge Xbox 360 pads.
                    0x028E or 0x028F or 0x0291 or 0x02A1 or 0x0719 => ControllerGlyphSet.Xbox360,
                    // Xbox One, One S and Elite pads (USB and Bluetooth).
                    0x02D1 or 0x02DD or 0x02E0 or 0x02E3 or 0x02EA or 0x02FD or 0x0B00 or 0x0B05 or 0x0B20 or 0x0B22 => ControllerGlyphSet.XboxOne,
                    // Xbox Series X|S pads; other Microsoft pads fall through to the name check below.
                    0x0B12 or 0x0B13 => ControllerGlyphSet.XboxSeries,
                    _ => FromName(name, ControllerGlyphSet.XboxSeries),
                };
            case Sony: return ControllerGlyphSet.PS5;
            case Nintendo: return ControllerGlyphSet.Switch2;
            default: return FromName(name, ControllerGlyphSet.None);
        }
    }

    /// <summary>
    /// Reads a USB vendor or product ID that Godot reported either as a number or as decimal text
    /// (for example "1118"); "0x"-prefixed hexadecimal is accepted too. Returns 0 when unreadable.
    /// </summary>
    public static int ParseId(string? text)
    {
        string value = (text ?? "").Trim();
        if (value.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
            return int.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out int hex) ? hex : 0;
        // A float-typed Variant prints as "1118" or "1118.0"; drop the fraction.
        int dot = value.IndexOf('.');
        if (dot >= 0) value = value[..dot];
        return int.TryParse(value, out int number) && number > 0 ? number : 0;
    }

    /// <summary>Name-only detection; <paramref name="fallback"/> is used when no family word matches.</summary>
    private static ControllerGlyphSet FromName(string name, ControllerGlyphSet fallback)
    {
        string lower = (name ?? "").ToLowerInvariant();
        // Generation words only count next to an Xbox word, so "Series" on another brand is not an Xbox pad.
        bool xbox = lower.Contains("xbox") || lower.Contains("x-box") || lower.Contains("microsoft");
        if (xbox && lower.Contains("360")) return ControllerGlyphSet.Xbox360;
        if (xbox && System.Text.RegularExpressions.Regex.IsMatch(lower, @"\bone\b")) return ControllerGlyphSet.XboxOne;
        if (xbox && lower.Contains("series")) return ControllerGlyphSet.XboxSeries;
        return InputPromptService.DetectFamily(lower) switch
        {
            PromptControllerFamily.Xbox => ControllerGlyphSet.XboxSeries,
            PromptControllerFamily.PlayStation => ControllerGlyphSet.PS5,
            PromptControllerFamily.Nintendo => ControllerGlyphSet.Switch2,
            _ => fallback,
        };
    }

    /// <summary>
    /// Maps the saved prompt-style number to a forced set. Old saves keep working: Xbox (1) now means
    /// Xbox Series X|S, PlayStation (2) means PS5 and Nintendo (3) means Switch 2. Automatic and
    /// Keyboard return null (no forced set).
    /// </summary>
    public static ControllerGlyphSet? ForcedSet(ControllerGlyphStyle style) => style switch
    {
        ControllerGlyphStyle.Xbox => ControllerGlyphSet.XboxSeries,
        ControllerGlyphStyle.PlayStation => ControllerGlyphSet.PS5,
        ControllerGlyphStyle.NintendoSwitch => ControllerGlyphSet.Switch2,
        ControllerGlyphStyle.Xbox360 => ControllerGlyphSet.Xbox360,
        ControllerGlyphStyle.XboxOne => ControllerGlyphSet.XboxOne,
        _ => null,
    };

    /// <summary>
    /// The set a console build always uses, from its export feature tags; null on PC, macOS and Linux.
    /// The "Xbox / Windows GDK" preset sets <c>xbox</c>. The <c>ps5</c> and <c>switch2</c> tags are for
    /// future licensed console presets and do nothing until such a preset exists.
    /// </summary>
    public static ControllerGlyphSet? PlatformSet(System.Func<string, bool> hasFeature)
    {
        if (hasFeature("xbox")) return ControllerGlyphSet.XboxSeries;
        if (hasFeature("ps5")) return ControllerGlyphSet.PS5;
        if (hasFeature("switch2")) return ControllerGlyphSet.Switch2;
        return null;
    }
}
