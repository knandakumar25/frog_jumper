using Godot;
using System;
using System.Text.Json;

/// <summary>Shared GDScript/C# locale bridge. Changes presentation only, without replacing scenes or touching speech players.</summary>
public partial class LocaleService : Node
{
    private static readonly System.Collections.Generic.Dictionary<string, string> SourceAliases = LoadSourceAliases();

    /// <summary>Loads reviewed English-source aliases; arbitrary external text is never added to this table.</summary>
    private static System.Collections.Generic.Dictionary<string, string> LoadSourceAliases()
    {
        const string path = "res://assets/translations/source_aliases.json";
        return FileAccess.FileExists(path)
            ? JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(FileAccess.GetFileAsString(path))! : new();
    }

    /// <summary>Adds only available translations as native English-source aliases so static labels refresh in place.</summary>
    public void RegisterSourceAliases(Translation translation)
    {
        // CSV resources may be OptimizedTranslation, which is immutable. Keep
        // aliases in a separate normal Translation rather than mutating imports.
        var aliases = new Translation { Locale = Iso(translation.Locale) };
        foreach (var pair in SourceAliases)
        {
            string value = translation.GetMessage(pair.Value);
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrEmpty(translation.GetMessage(pair.Key)))
                aliases.AddMessage(pair.Key, value);
        }
        TranslationServer.AddTranslation(aliases);
    }

    /// <summary>Raised after fonts and narrative metadata have changed; listeners may refresh text without replaying events.</summary>
    [Signal] public delegate void LanguageChangedEventHandler(string catalogName);

    /// <summary>Returns identical selector contents to GDScript settings and C# login.</summary>
    public Godot.Collections.Dictionary GetLanguages()
    {
        var result = new Godot.Collections.Dictionary();
        foreach (var language in LocaleCatalog.All) result[language.DisplayName] = language.Key;
        return result;
    }

    /// <summary>Returns the canonical narrative/catalog filename for either a legacy or ISO locale.</summary>
    public string Canonical(string locale) => LocaleCatalog.Resolve(locale).Key;

    /// <summary>Returns the ISO alias used by Godot's text shaping and translation matching.</summary>
    public string Iso(string locale) => LocaleCatalog.Resolve(locale).Iso;

    /// <summary>Returns the bundled font for an explicit language identity, avoiding substring collisions such as Mandarin/Arabic.</summary>
    public string FontPath(string locale) => "res://assets/fonts/" + LocaleCatalog.Resolve(locale).Font;

    /// <summary>Allows GDScript layout code to use the same explicit direction table.</summary>
    public bool IsRtl(string locale) => LocaleCatalog.Resolve(locale).Rtl;

    /// <summary>Applies ISO locale, fonts and narrative in place. Cue IDs, audio position and campaign state are untouched.</summary>
    public void ApplyLocale(string locale)
    {
        var language = LocaleCatalog.Resolve(locale);
        TranslationServer.SetLocale(language.Iso);
        if (!IsInsideTree()) return;
        GetNodeOrNull<Node>("/root/Global")?.Call("update_font_for_locale", language.Key);
        GetNodeOrNull<Node>("/root/NarrativeManager")?.Call("reload_for_locale", language.Key);
        GetNodeOrNull<Node>("/root/Global")?.Call("update_prompt_text");
        WildfireText.RefreshBindings();
        EmitSignal(SignalName.LanguageChanged, language.Key);
    }
}
