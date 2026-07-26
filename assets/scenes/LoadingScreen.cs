using Godot;

/// <summary>
/// Loading screen with a threaded ResourceLoader progress bar and fade transitions.
/// Also exposes a static LoadScene() helper used throughout the project.
/// Converted from loading_screen.gd.
/// </summary>
public partial class LoadingScreen : Control
{
    // ─── Node references ─────────────────────────────────────────────────────
    private ProgressBar _progressBar;
    private Label       _statusLabel;
    private ColorRect   _fadeOverlay;

    // ─── State ───────────────────────────────────────────────────────────────
    [Export] public string TargetScenePath { get; set; } = "";
    private int   _loadingStatus  = 0;
    private float _fadeDuration   = 0.3f;
    private bool  _loadingStarted = false;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        _progressBar  = GetNode<ProgressBar>("CenterContainer/MainPanel/VBoxContainer/ProgressBar");
        _statusLabel  = GetNode<Label>("CenterContainer/MainPanel/VBoxContainer/StatusLabel");
        _fadeOverlay  = GetNode<ColorRect>("FadeOverlay");

        if (HasNode("CenterContainer/MainPanel/VBoxContainer/TitleLogoBanner"))
        {
            var logoBanner = GetNode<TextureRect>("CenterContainer/MainPanel/VBoxContainer/TitleLogoBanner");
            if (logoBanner.Texture == null)
            {
                string resPath = "res://assets/images/axiom_force_title_logo.jpg";
                string globalPath = ProjectSettings.GlobalizePath(resPath);
                if (System.IO.File.Exists(globalPath))
                {
                    var img = Image.LoadFromFile(globalPath);
                    if (img != null) logoBanner.Texture = ImageTexture.CreateFromImage(img);
                }
            }
        }

        if (string.IsNullOrEmpty(TargetScenePath))
        {
            GD.PushError("No target scene path set for loading screen");
            return;
        }

        _fadeOverlay.Modulate = _fadeOverlay.Modulate with { A = 1.0f };
        FadeIn();

        var error = ResourceLoader.LoadThreadedRequest(TargetScenePath);
        if (error == Error.Ok)
        {
            _loadingStarted = true;
            _statusLabel.Text = Tr("Loading scene...");
        }
        else
        {
            GD.PushError("Failed to start loading: " + TargetScenePath);
            _statusLabel.Text = Tr("Error: Failed to start loading");
        }
    }

    // ─── Fade helpers ────────────────────────────────────────────────────────
    private void FadeIn()
    {
        var tween = CreateTween();
        tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, _fadeDuration);
    }

    private async System.Threading.Tasks.Task FadeOutAsync()
    {
        var tween = CreateTween();
        tween.TweenProperty(_fadeOverlay, "modulate:a", 1.0f, _fadeDuration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    // ─── Progress poll ───────────────────────────────────────────────────────
    public override void _Process(double delta)
    {
        if (string.IsNullOrEmpty(TargetScenePath) || !_loadingStarted) return;

        var progress = new Godot.Collections.Array();
        _loadingStatus = (int)ResourceLoader.LoadThreadedGetStatus(TargetScenePath, progress);

        switch ((ResourceLoader.ThreadLoadStatus)_loadingStatus)
        {
            case ResourceLoader.ThreadLoadStatus.InvalidResource:
                _statusLabel.Text = Tr("Error: Invalid resource");
                GD.PushError("Failed to load scene: " + TargetScenePath);
                _loadingStarted = false;
                break;

            case ResourceLoader.ThreadLoadStatus.InProgress:
                if (progress.Count > 0)
                {
                    float p = (float)progress[0];
                    _progressBar.Value = p * 100.0f;
                    _statusLabel.Text  = Tr("Loading... ") + (int)(p * 100) + "%";
                }
                break;

            case ResourceLoader.ThreadLoadStatus.Failed:
                _statusLabel.Text = Tr("Error: Loading failed");
                GD.PushError("Failed to load scene: " + TargetScenePath);
                _loadingStarted = false;
                break;

            case ResourceLoader.ThreadLoadStatus.Loaded:
                _progressBar.Value = 100.0f;
                _statusLabel.Text  = Tr("Complete!");
                _loadingStarted    = false;
                _ = OnLoadingCompleteAsync();
                break;
        }
    }

    private async System.Threading.Tasks.Task OnLoadingCompleteAsync()
    {
        var packed = ResourceLoader.LoadThreadedGet(TargetScenePath) as PackedScene;
        if (packed != null)
        {
            await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
            await FadeOutAsync();
            GetTree().ChangeSceneToPacked(packed);
        }
        else
        {
            GD.PushError("Failed to retrieve loaded scene: " + TargetScenePath);
        }
    }

    // ─── Static transition helper ─────────────────────────────────────────────
    /// <summary>
    /// Transitions to a loading screen that will load <paramref name="scenePath"/>.
    /// Can be called from C# code; GDScript callers should use loading_screen.gd stub.
    /// </summary>
    public static void LoadScene(SceneTree tree, string scenePath)
    {
        var screen = GD.Load<PackedScene>("res://assets/scenes/loading_screen.tscn")
                        .Instantiate<LoadingScreen>();
        screen.TargetScenePath = scenePath;
        tree.Root.AddChild(screen);
        tree.CurrentScene.QueueFree();
        tree.CurrentScene = screen;
    }
}

