using System;
using System.Collections.Generic;

/// <summary>Stable language identities shared by login, settings, fonts and narrative data; never persisted as operative IDs.</summary>
public static class LocaleCatalog
{
    /// <summary>Catalog column, Godot ISO locale, selector label and script-specific font. RTL applies to written layout only.</summary>
    public sealed record Language(string Key, string Iso, string DisplayName, bool Rtl = false, string Font = "NotoSans-Regular.ttf");

    /// <summary>The existing seventeen languages, in catalog order. Native names remain readable before selecting a language.</summary>
    public static IReadOnlyList<Language> All { get; } = new Language[]
    {
        new("english", "en", "English"),
        new("spanish", "es", "Spanish (Español)"),
        new("eurofrench", "fr", "French (Français)"),
        new("brazilianportuguese", "pt_BR", "Portuguese (Português)"),
        new("german", "de", "German (Deutsch)"),
        new("italian", "it", "Italian (Italiano)"),
        new("polish", "pl", "Polish (Polski)"),
        new("arabic", "ar", "Arabic (العربية)", true, "Cairo.ttf"),
        new("turkish", "tr", "Turkish (Türkçe)"),
        new("japanese", "ja", "Japanese (日本語)", false, "NotoSansCJKjp-Regular.otf"),
        new("korean", "ko", "Korean (한국어)", false, "NotoSansCJKkr-Regular.otf"),
        new("mandarin", "zh_CN", "Simplified Chinese (简体中文)", false, "NotoSansCJKsc-Regular.otf"),
        new("hebrew", "he", "Hebrew (עברית)", true, "NotoSansHebrew.ttf"),
        new("traditionalchinese", "zh_TW", "Traditional Chinese (繁體中文)", false, "NotoSansCJKtc-Regular.otf"),
        new("indonesian", "id", "Indonesian (Bahasa Indonesia)"),
        new("vietnamese", "vi", "Vietnamese (Tiếng Việt)"),
        new("thai", "th", "Thai (ไทย)", false, "NotoSansThai.ttf")
    };

    /// <summary>Normalizes legacy catalog names and ISO/script/region aliases; unsupported locales resolve to English.</summary>
    public static Language Resolve(string? locale)
    {
        string value = (locale ?? "").Trim().Replace('-', '_').ToLowerInvariant();
        foreach (var language in All)
            if (value == language.Key || value == language.Iso.ToLowerInvariant()) return language;
        if (value == "french") return All[2];
        if (value == "portuguese") return All[3];
        string code = value.Split('_')[0];
        if (code is "zh") return value.Contains("hant") || value.Contains("tw") || value.Contains("hk") || value.Contains("mo")
            ? All[13] : All[11];
        if (code is "iw") return All[12];
        if (code is "in") return All[14];
        foreach (var language in All)
            if (code == language.Iso.Split('_')[0]) return language;
        return All[0];
    }
}
