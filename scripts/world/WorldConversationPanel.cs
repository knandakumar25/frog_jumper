using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Bottom-of-screen conversation box for the open world. Shows lines one at a time, then
/// optional consequence choices. All actions are focusable buttons, so keyboard and
/// controller work through normal UI navigation. Nothing is saved here: the owner applies
/// the result in its callback, and cancelling changes nothing.
/// </summary>
public partial class WorldConversationPanel : PanelContainer
{
    private Label _speaker = null!, _text = null!;
    private HBoxContainer _actions = null!;
    private IReadOnlyList<string> _lines = Array.Empty<string>();
    private IReadOnlyList<QuestBranchChoice>? _choices;
    private Action<string?>? _finished;
    private Action? _cancelled;
    private string? _extraLabel;
    private Action? _extraAction;
    private int _line;

    /// <summary>True while a conversation is on screen; the world pauses play during that time.</summary>
    public bool IsOpen => Visible;

    /// <summary>Builds the controls once. The panel starts hidden.</summary>
    public override void _Ready()
    {
        Name = "WorldConversationPanel";
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        OffsetLeft = 120; OffsetRight = -120; OffsetTop = -250; OffsetBottom = -24;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.05f, 0.09f, 0.15f, 0.95f), ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 16, ContentMarginBottom = 16 });
        var box = new VBoxContainer();
        AddChild(box);
        _speaker = new Label { Modulate = new Color("ffea75") };
        _speaker.AddThemeFontSizeOverride("font_size", 20);
        box.AddChild(_speaker);
        _text = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = SizeFlags.ExpandFill };
        box.AddChild(_text);
        _actions = new HBoxContainer { Name = "Actions" };
        box.AddChild(_actions);
    }

    /// <summary>
    /// Opens a conversation.
    /// <paramref name="finished"/> receives the chosen branch ID, or null when no choice was offered.
    /// <paramref name="extraLabel"/> adds a second action, used when a resident stands at a door
    /// so the same interaction can still reach the building.
    /// </summary>
    public void Open(string speaker, IReadOnlyList<string> lines, IReadOnlyList<QuestBranchChoice>? choices, Action<string?> finished, Action? cancelled = null, string? extraLabel = null, Action? extraAction = null)
    {
        _lines = lines; _choices = choices; _finished = finished; _cancelled = cancelled;
        _extraLabel = extraLabel; _extraAction = extraAction; _line = 0;
        _speaker.Text = WorldText.Get(speaker);
        Visible = true;
        ShowLine();
    }

    /// <summary>Closes without applying anything, as when the player presses Cancel.</summary>
    public void Cancel()
    {
        if (!Visible) return;
        var cancelled = _cancelled;
        Close();
        cancelled?.Invoke();
    }

    /// <summary>Shows the current line and the buttons valid for it.</summary>
    private void ShowLine()
    {
        ClearActions();
        _text.Text = _lines.Count > 0 ? WorldText.Get(_lines[Math.Min(_line, _lines.Count - 1)]) : "";
        bool last = _line >= _lines.Count - 1;
        if (!last) { AddAction("ConversationContinue", "Continue", () => { _line++; ShowLine(); }); }
        else if (_choices is { Count: > 0 }) { AddAction("ConversationContinue", "Decide", ShowChoices); }
        else { AddAction("ConversationContinue", "Continue", () => Finish(null)); }
        if (last && _extraLabel != null && _extraAction != null)
        {
            var extra = _extraAction;
            AddAction("ConversationExtra", _extraLabel, () => { Close(); extra(); });
        }
        AddAction("ConversationCancel", "Leave", Cancel);
        FocusFirst();
    }

    /// <summary>Lists each option with its consequence, so the tradeoff is visible before committing.</summary>
    private void ShowChoices()
    {
        ClearActions();
        var parts = new List<string>();
        foreach (var choice in _choices!)
        {
            parts.Add($"{WorldText.Get(choice.Title)}: {WorldText.Get(choice.Description)} {WorldText.Get(choice.ConsequenceSummary)}");
            string id = choice.ChoiceId;
            AddAction($"Choice_{id}", choice.Title, () => Finish(id));
        }
        _text.Text = string.Join("\n", parts);
        AddAction("ConversationCancel", "Leave", Cancel);
        FocusFirst();
    }

    /// <summary>Closes, then reports the result. The order lets the callback open another panel.</summary>
    private void Finish(string? choice)
    {
        var finished = _finished;
        Close();
        finished?.Invoke(choice);
    }

    /// <summary>Hides the panel and drops callbacks so they can't fire twice.</summary>
    private void Close()
    {
        Visible = false;
        _finished = null; _cancelled = null; _extraAction = null;
        ClearActions();
    }

    /// <summary>Removes the old buttons. QueueFree is needed because the press handler may belong to one of them.</summary>
    private void ClearActions()
    {
        foreach (var child in _actions.GetChildren())
        {
            if (child is Node node) { node.Name = $"Old{node.GetInstanceId()}"; node.QueueFree(); }
        }
    }

    /// <summary>Adds one localized button with a stable node name for tests.</summary>
    private void AddAction(string name, string text, Action action)
    {
        var button = new Button { Name = name, Text = WorldText.Get(text) };
        button.Pressed += action;
        _actions.AddChild(button);
    }

    /// <summary>Puts focus on the newest primary action, skipping buttons waiting to be freed.</summary>
    private void FocusFirst()
    {
        if (!IsInsideTree()) return;
        foreach (var child in _actions.GetChildren())
        {
            if (child is Button button && !button.IsQueuedForDeletion()) { button.GrabFocus(); return; }
        }
    }
}
