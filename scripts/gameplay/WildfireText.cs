using Godot;

/// <summary>Gameplay localization keys with English fallback, plus prompts derived from actual InputMap bindings.</summary>
public static class WildfireText
{
    /// <summary>Retains a canonical display identity and its English-only tactical styling across locale changes.</summary>
    private sealed record DisplayArgument(string Source, bool EnglishUppercase);
    /// <summary>Retains a semantic text key instead of capturing its current translated value.</summary>
    private sealed record KeyArgument(string Key, string Fallback);
    /// <summary>Retains a nested formatted display fragment so numbers stay literal during later locale changes.</summary>
    private sealed record TemplateArgument(string Key, System.FormattableString Source);
    private sealed record BindingRecord(System.WeakReference<Node> Target, string Property, string Key, System.FormattableString Source);
    private static readonly System.Collections.Generic.Dictionary<(ulong, string), BindingRecord> Bindings = new();

    /// <summary>Marks a schema-owned display name for retranslation; arbitrary user data never receives this marker.</summary>
    public static object DisplayValue(string source, bool englishUppercase = false) => new DisplayArgument(source, englishUppercase);

    /// <summary>Marks a known interface key for lookup whenever its parent binding is refreshed.</summary>
    public static object KeyValue(string key, string fallback) => new KeyArgument(key, fallback);

    /// <summary>Marks a nested display template; ordinary external interpolation arguments remain untranslated.</summary>
    public static object TemplateValue(string key, System.FormattableString source) => new TemplateArgument(key, source);

    /// <summary>Restores the English tactical display style while preserving every other locale's authored letter case.</summary>
    public static string EnglishUppercase(string text) => LocaleCatalog.Resolve(TranslationServer.GetLocale()).Key == "english"
        ? text.ToUpperInvariant() : text;

    /// <summary>Assigns and tracks a translated display property so locale changes can refresh it without rerunning gameplay.</summary>
    public static string SetText(Node target, string property, string key, System.FormattableString source)
    {
        string nativeProperty = property switch { "Text" => "text", "TooltipText" => "tooltip_text", "PlaceholderText" => "placeholder_text", _ => property };
        string text = Interpolate(key, source);
        target.Set(nativeProperty, text);
        Bindings[(target.GetInstanceId(), nativeProperty)] = new(new(target), nativeProperty, key, source);
        return text;
    }

    /// <summary>Binds an initializer-created control while preserving its exact Godot type and all other initializer properties.</summary>
    public static T Bind<T>(T control, string property, string key, System.FormattableString source) where T : Control
    {
        SetText(control, property, key, source);
        return control;
    }

    /// <summary>Refreshes only explicitly bound controls and removes freed targets; never touches audio or external text.</summary>
    public static void RefreshBindings()
    {
        var dead = new System.Collections.Generic.List<(ulong, string)>();
        foreach (var pair in Bindings)
        {
            var binding = pair.Value;
            if (!binding.Target.TryGetTarget(out var target) || !GodotObject.IsInstanceValid(target)) dead.Add(pair.Key);
            else target.Set(binding.Property, Interpolate(binding.Key, binding.Source));
        }
        foreach (var key in dead) Bindings.Remove(key);
    }

    /// <summary>Translates the static template of a display interpolation; values, including usernames, remain literal data.</summary>
    public static string Interpolate(string key, System.FormattableString source)
    {
        string template = Get(key, source.Format);
        object?[] values = source.GetArguments();
        // Work on a fresh array: the retained source must keep canonical display
        // identities for later locale changes. Ordinary strings stay literal.
        var localized = new object?[values.Length];
        for (int i = 0; i < values.Length; i++) localized[i] = values[i] switch
        {
            DisplayArgument name => name.EnglishUppercase ? EnglishUppercase(TranslateDisplayName(name.Source)) : TranslateDisplayName(name.Source),
            KeyArgument keyed => Get(keyed.Key, keyed.Fallback),
            TemplateArgument nested => Interpolate(nested.Key, nested.Source),
            _ => values[i]
        };
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, localized);
    }

    /// <summary>Translates schema-owned text first as a whole, then falls back to composite operative identity components.</summary>
    private static string TranslateDisplayName(string source)
    {
        string whole = TranslationServer.Translate(source);
        if (whole != source) return whole;
        string[] components = source.Split(" // ");
        for (int i = 0; i < components.Length; i++) components[i] = TranslationServer.Translate(components[i]);
        return string.Join(" // ", components);
    }
    /// <summary>Formats a translated sentence after lookup, inserting values literally rather than translating external data.</summary>
    public static string Format(string key, string fallback, params (string Name, object Value)[] values)
    {
        var fields = new System.Collections.Generic.Dictionary<string, object>();
        foreach (var pair in values) fields[pair.Name] = pair.Value;
        return LocalizedTemplate.Fill(Get(key, fallback), fields);
    }
    /// <summary>Returns the registered translation or the supplied English text when the key is not translated.</summary>
    public static string Get(string key, string fallback)
    {
        string translated = TranslationServer.Translate(key);
        return translated == key || string.IsNullOrWhiteSpace(translated) ? TranslationServer.Translate(fallback) : translated;
    }
    /// <summary>Returns the current keyboard or controller binding for a gameplay action.</summary>
    public static string Binding(string action, bool pad)
    {
        return InputPromptService.Instance?.Binding(action, pad) ?? TranslationServer.Translate("Unbound");
    }
}
