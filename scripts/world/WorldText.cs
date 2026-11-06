using Godot;

/// <summary>Localized world labels use stable English-source aliases; proper names have authored catalog entries.</summary>
public static class WorldText
{
    /// <summary>Resolves an English source phrase through the existing seventeen-language translation server.</summary>
    public static string Get(string source) => TranslationServer.Translate(source);
}
