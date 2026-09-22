using Godot;

/// <summary>
/// LoadingScreen manages the asynchronous transition between game scenes.
/// It leverages Godot's threaded loading system to prevent UI freezes while
/// large assets are loaded into memory, providing the user with a real-time
/// progress bar and atmospheric fade transitions.
///
/// Unity Migration Note:
/// 1. Async Loading: Replace ResourceLoader with Unity's `SceneManager.LoadSceneAsync`.
/// 2. Progress Tracking: Use the `AsyncOperation.progress` value to update the UI.
/// 3. Global Helper: Implement a 'LoadingManager' singleton (DontDestroyOnLoad) with a
///    static LoadScene method to manage transitions globally.
/// 4. Fading: Use a simple CanvasGroup or a black UI panel with a Tween to handle
///    the fade-in/fade-out effects.
/// </summary>
public partial class LoadingScreen : Control
{
	// ─── Node references ─────────────────────────────────────────────────────
	private ProgressBar _progressBar;
	private Label       _statusLabel;
	private ColorRect   _fadeOverlay;

	// ─── State ───────────────────────────────────────────────────────────────
	/// <summary>
	/// The path to the scene that needs to be loaded. Set via the static LoadScene helper.
	/// </summary>
	[Export] public string TargetScenePath { get; set; } = "";
	private int   _loadingStatus  = 0;
	private float _fadeDuration   = 0.3f;
	private bool  _loadingStarted = false;

	// ─── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// Resolve UI components for progress reporting.
		_progressBar  = GetNode<ProgressBar>("CenterContainer/MainPanel/VBoxContainer/ProgressBar");
		_statusLabel  = GetNode<Label>("CenterContainer/MainPanel/VBoxContainer/StatusLabel");
		_fadeOverlay  = GetNode<ColorRect>("FadeOverlay");

		var loadingLabel = GetNodeOrNull<Label>("CenterContainer/MainPanel/VBoxContainer/LoadingLabel");
		if (loadingLabel != null)
		{
			loadingLabel.Text = Tr("INITIALIZING SYSTEM...");
		}

		// Apply the localized branding logo to the loading banner.
		if (HasNode("CenterContainer/MainPanel/VBoxContainer/TitleLogoBanner"))
		{
			var logoBanner = GetNode<TextureRect>("CenterContainer/MainPanel/VBoxContainer/TitleLogoBanner");
			string resPath = GetLocalizedTitleLogoPath();
			string globalPath = ProjectSettings.GlobalizePath(resPath);
			if (System.IO.File.Exists(globalPath))
			{
				var img = Image.LoadFromFile(globalPath);
				if (img != null) logoBanner.Texture = ImageTexture.CreateFromImage(img);
			}
		}

		if (string.IsNullOrEmpty(TargetScenePath))
		{
			GD.PushError("No target scene path set for loading screen");
			return;
		}

		// Start with a black screen and fade in to reveal the progress bar.
		_fadeOverlay.Modulate = _fadeOverlay.Modulate with { A = 1.0f };
		FadeIn();

		// Initiate the background load request.
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
	/// <summary>
	/// Fades the screen from black to transparent.
	/// </summary>
	private void FadeIn()
	{
		_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		var tween = CreateTween();
		tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, _fadeDuration);
		tween.TweenCallback(Callable.From(() =>
		{
			if (_fadeOverlay != null)
			{
				_fadeOverlay.Visible = false;
				_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			}
		}));
	}

	/// <summary>
	/// Fades the screen from transparent to black.
	/// </summary>
	private async System.Threading.Tasks.Task FadeOutAsync()
	{
		_fadeOverlay.Visible = true;
		_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
		var tween = CreateTween();
		tween.TweenProperty(_fadeOverlay, "modulate:a", 1.0f, _fadeDuration);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	// ─── Progress poll ───────────────────────────────────────────────────────
	public override void _Process(double delta)
	{
		if (string.IsNullOrEmpty(TargetScenePath) || !_loadingStarted) return;

		// Poll the threaded loader for the current progress.
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
				// Sequence the final scene transition.
				_ = OnLoadingCompleteAsync();
				break;
		}
	}

	/// <summary>
	/// Finalizes the loading process by retrieving the packed scene and performing a fade-out transition.
	/// </summary>
	private async System.Threading.Tasks.Task OnLoadingCompleteAsync()
	{
		var packed = ResourceLoader.LoadThreadedGet(TargetScenePath) as PackedScene;
		if (packed != null)
		{
			// Brief pause for visual polish before fading out.
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
	/// Global utility method to initiate a scene transition via the LoadingScreen.
	/// Instantiate the loading screen, set its target, and replace the current scene.
	/// </summary>
	/// <param name="tree">The current SceneTree instance.</param>
	/// <param name="scenePath">The path to the scene that should be loaded asynchronously.</param>
	public static void LoadScene(SceneTree tree, string scenePath)
	{
		var screen = GD.Load<PackedScene>("res://scenes/main_menu/loading_screen.tscn")
						.Instantiate<LoadingScreen>();
		screen.TargetScenePath = scenePath;
		tree.Root.AddChild(screen);
		tree.CurrentScene.QueueFree();
		tree.CurrentScene = screen;
	}

	/// <summary>
	/// Determines the correct title logo path based on the current translation locale.
	/// </summary>
	private string GetLocalizedTitleLogoPath()
	{
		return ThreeDMenuExperience.GetLocalizedTitle2DPath();
	}

	private string Tr(string text)
	{
		return TranslationServer.Translate(text);
	}
}
