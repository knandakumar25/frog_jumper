using Godot;

// Add Ink runtime namespace
using Ink.Runtime;

public partial class InkManager : Node
{
    public static InkManager? Instance { get; private set; }
    private string _inkJson = "";
    private string _loadedLocale = "";
    private const string NarrativeDir = "res://assets/narrative/";
    private const string FallbackLocale = "english";
    private Story? _story = null;

    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            QueueFree();
            return;
        }
        LoadForLocale(TranslationServer.GetLocale());
    }

    public void LoadForLocale(string locale)
    {
        string jsonPath = NarrativeDir + locale + ".json";
        if (Godot.FileAccess.FileExists(jsonPath))
        {
            using var file = Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read);
            _inkJson = file.GetAsText();
            _loadedLocale = locale;
            GD.Print($"InkManager: Loaded Ink JSON narrative for locale '{locale}'.");
            _story = new Story(_inkJson);
            return;
        }
        // Fallback
        jsonPath = NarrativeDir + FallbackLocale + ".json";
        if (Godot.FileAccess.FileExists(jsonPath))
        {
            using var file = Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read);
            _inkJson = file.GetAsText();
            _loadedLocale = FallbackLocale;
            GD.Print($"InkManager: Loaded fallback Ink JSON narrative for locale '{FallbackLocale}'.");
            _story = new Story(_inkJson);
        }
        else
        {
            _inkJson = "";
            _loadedLocale = "";
            _story = null;
            GD.PrintErr("InkManager: No Ink JSON narrative file found.");
        }
    }

    public string GetInkJson() => _inkJson;
    public string GetLoadedLocale() => _loadedLocale;

    public bool HasStory() => _story != null;

    public bool CanContinue() => _story != null && _story.canContinue;

    public string Continue()
    {
        if (_story == null) return "";
        return _story.Continue().Trim();
    }

    public int GetChoiceCount()
    {
        if (_story == null) return 0;
        return _story.currentChoices.Count;
    }

    public string GetChoiceText(int index)
    {
        if (_story == null || index < 0 || index >= _story.currentChoices.Count) return "";
        return _story.currentChoices[index].text.Trim();
    }

    public void ChooseChoiceIndex(int index)
    {
        if (_story == null || index < 0 || index >= _story.currentChoices.Count) return;
        _story.ChooseChoiceIndex(index);
    }
}

