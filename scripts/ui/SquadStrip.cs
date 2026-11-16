using Godot;

/// <summary>
/// A compact HUD row showing the six operatives in roster order: the active one highlighted, unlocked ones dimmed and
/// locked ones as "?" placeholders, each labelled with the number key that selects it. It rebuilds itself whenever the
/// active operative changes or a new operative joins, so recruit beats are visible immediately.
/// </summary>
public partial class SquadStrip : HBoxContainer
{
    private CyberShogun? _player;
    private InputPromptService? _prompts;
    private OperativeRoster _roster = OperativeRoster.Current;

    /// <summary>Short on-screen names so six slots fit in a row.</summary>
    private static string ShortName(string operative) => operative switch
    {
        "Cyber Shogun" => "SHOGUN", "Apex Titan" => "TITAN", "Aura Phoenicia" => "AURA",
        "Mirage Blade" => "MIRAGE", "Surya Prime" => "SURYA", _ => operative.ToUpperInvariant()
    };

    /// <summary>Binds to the player (profile changes) and the current roster (new unlocks) and draws the slots.</summary>
    public void Bind(CyberShogun player)
    {
        _player = player;
        _roster = OperativeRoster.Current;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 6);
        player.OperativeProfileChanged += OnProfileChanged;
        _roster.OperativeUnlocked += OnUnlocked;
        // Rebuild owns every child slot, so the listener must stay on this node
        // rather than being a child that the first rebuild would remove.
        _prompts = InputPromptService.Instance;
        if (_prompts != null) _prompts.PromptsChanged += Rebuild;
        Rebuild();
    }

    /// <summary>Refreshes active highlighting after a fielded operative changes.</summary>
    private void OnProfileChanged(OperativeProfile _) => Rebuild();
    /// <summary>Refreshes ownership and availability when a story recruit joins.</summary>
    private void OnUnlocked(string _) => Rebuild();

    /// <summary>Unsubscribes from the long-lived roster/player events so a freed strip is never called back.</summary>
    public override void _ExitTree()
    {
        if (_prompts != null && GodotObject.IsInstanceValid(_prompts)) _prompts.PromptsChanged -= Rebuild;
        _roster.OperativeUnlocked -= OnUnlocked;
        if (_player != null && GodotObject.IsInstanceValid(_player)) _player.OperativeProfileChanged -= OnProfileChanged;
    }

    /// <summary>Recreates the slot panels from the roster state.</summary>
    private void Rebuild()
    {
        if (!IsInsideTree() && GetParent() == null) return;
        // These passive panels own no input callbacks. Free them immediately so an
        // in-place prompt refresh during scene teardown cannot leave detached queued nodes.
        foreach (Node child in GetChildren()) child.Free();
        string active = _player?.ActiveProfile.DisplayName ?? _roster.Active;
        for (int i = 0; i < OperativeRoster.RosterOrder.Count; i++)
        {
            string name = OperativeRoster.RosterOrder[i];
            bool unlocked = _roster.IsUnlocked(name), isActive = name == active;
            var style = new StyleBoxFlat
            {
                BgColor = isActive ? new Color(.02f, .12f, .2f, .94f) : new Color(.015f, .025f, .06f, .78f),
                BorderColor = isActive ? new Color(.47f, .94f, 1f) : unlocked ? new Color(.1f, .6f, .75f, .8f) : new Color(.25f, .28f, .35f, .8f),
                ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 4, ContentMarginBottom = 4
            };
            style.SetBorderWidthAll(isActive ? 3 : 1);
            style.SetCornerRadiusAll(6);
            var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", style);
            var label = new Label
            {
                Text = unlocked ? (_roster.IsAvailable(name) ? $"{SlotBinding(i)}{ShortName(name)}" : "SURYA / " + WildfireText.Get("NAMES_SURYA_QUEUE", "Protecting the queue")) : $"{SlotBinding(i)}???",
                MouseFilter = MouseFilterEnum.Ignore
            };
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", isActive ? Colors.White : unlocked ? new Color(.6f, .85f, .95f) : new Color(.45f, .48f, .55f));
            var font = GD.Load<FontFile>("res://assets/fonts/Orbitron-VariableFont.ttf");
            if (font != null) label.AddThemeFontOverride("font", font);
            panel.AddChild(label);
            AddChild(panel);
        }
    }

    /// <summary>Shows direct-slot keys only for keyboard prompts; pads cycle the squad through the HUD's named actions.</summary>
    private static string SlotBinding(int index) => InputPromptService.Instance?.IsGamepad == true ? "" : InputPromptBinding.Binding("squad_slot_" + (index + 1)) + " ";
}
