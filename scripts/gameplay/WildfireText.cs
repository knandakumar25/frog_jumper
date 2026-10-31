using Godot;

/// <summary>Gameplay localization keys with English fallback, plus prompts derived from actual InputMap bindings.</summary>
public static class WildfireText
{
    /// <summary>Returns the registered translation or the supplied English text when the key is not translated.</summary>
    public static string Get(string key, string fallback)
    {
        string translated = TranslationServer.Translate(key);
        return translated == key ? fallback : translated;
    }
    /// <summary>Returns the current keyboard or controller binding for a gameplay action.</summary>
    public static string Binding(string action, bool pad)
    {
        foreach (var input in InputMap.ActionGetEvents(action))
        {
            if (pad && input is InputEventJoypadButton button) return button.ButtonIndex.ToString();
            if (!pad && input is InputEventKey key)
                return OS.GetKeycodeString(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode);
        }
        return Get("WF_TOUCH_" + action.ToUpperInvariant(), action);
    }
}
