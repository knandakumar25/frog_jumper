using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot.Collections;

/// <summary>
/// Level3DManager handles the logic for a 3D prototype level.
/// It manages the 3D HUD, level-specific events, and connects to the
/// SessionManager to persist 3D progress.
/// </summary>
public partial class Level3DManager : Node
{
	[Export] public int LevelNumber = 1;
	[Export] public int PointsPerCollectable = 10;
	[Export] public int RequiredCollectables = 2;

	private int _currentPoints = 0;
	private int _collectedCount = 0;
	private Label _pointsLabel;
	private Label _collectableLabel;
	private DialogueUI _dialogueUI;
	private int _currentDialogueIndex = 0;

	public override async void _Ready()
	{
		// Find the HUD labels if they exist in the scene.
		_pointsLabel = GetNodeOrNull<Label>("%PointsLabel");
		_collectableLabel = GetNodeOrNull<Label>("%CollectableLabel");
		UpdateUI();

		// Initialize Dialogue UI.
		_dialogueUI = GetTree().Root.FindChild("DialogueUI", true, false) as DialogueUI;

		// Start the narrative briefing.
		await StartBriefing();
	}

	public int CurrentPoints => _currentPoints;
	public int CollectedCount => _collectedCount;

	public void AddPoint()
	{
		_currentPoints += PointsPerCollectable;
		UpdateUI();
	}

	public void IncrementCollectable()
	{
		_collectedCount++;
		UpdateUI();
	}

	public bool IsObjectiveComplete()
	{
		return _collectedCount >= RequiredCollectables;
	}

	private void UpdateUI()
	{
		if (_pointsLabel != null)
		{
			_pointsLabel.Text = $"SCORE: {_currentPoints}";
		}

		if (_collectableLabel != null)
		{
			_collectableLabel.Text = $"CHERRYS: {_collectedCount} / {RequiredCollectables}";
		}
	}

	/// <summary>
	/// Loads dialogue from english.json and presents it via DialogueUI.
	/// Freezes player movement until the briefing is complete.
	/// </summary>
	private async Task StartBriefing()
	{
		if (_dialogueUI == null)
		{
			GD.PrintErr("Level3DManager: DialogueUI not found! Skipping briefing.");
			return;
		}

		// 1. Freeze Player.
		var player = GetTree().Root.FindChild("Player3D", true, false) as Player3D;
		player?.StopMovement();

		// 2. Load Narrative.
		string path = "res://assets/narrative/english.json";
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"Level3DManager: Narrative file not found at {path}");
			player?.ResumeMovement();
			return;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string jsonText = file.GetAsText();
		var data = Json.ParseString(jsonText).AsGodotDictionary();

		// 3. Extract Level Dialogue.
		var levels = data["levels"].AsGodotDictionary();
		string lvlKey = LevelNumber.ToString();

		if (!levels.ContainsKey(lvlKey))
		{
			GD.PrintErr($"Level3DManager: No dialogue found for level {lvlKey}");
			player?.ResumeMovement();
			return;
		}

		var levelData = levels[lvlKey].AsGodotDictionary();
		var dialogueList = levelData["dialogue"].AsGodotArray();

		// 4. Present Dialogue.
		GD.Print($"Level3DManager: Starting briefing for Level {LevelNumber}.");
		foreach (var entry in dialogueList)
		{
			var line = entry.AsGodotDictionary();
			string speaker = line["speaker"].ToString();
			string text = line["text"].ToString();
			string type = line["type"].ToString();

			await _dialogueUI.DisplayLine(speaker, text, type);
			await Task.Delay(1000); // Brief pause between lines.
		}

		_dialogueUI.HideUI();

		// 5. Unfreeze Player.
		player?.ResumeMovement();
		GD.Print("Level3DManager: Briefing complete. Gameplay started.");
	}

	/// <summary>
	/// Called when the player reaches the finish zone.
	/// </summary>
	public async void OnLevelComplete()
	{
		GD.Print($"Level {LevelNumber} Complete! Final Score: {_currentPoints}");

		// 1. Unlock the next level in Global progression.
		var global = GetNode<Node>("/root/Global");
		if (global != null)
		{
			global.Call("unlock_next_level", LevelNumber);
			GD.Print($"Progression: Level {LevelNumber} cleared. Next level unlocked.");
		}

		// 2. Port the "Golden Path" save logic to 3D.
		var sm = SessionManager.Instance;
		if (sm != null && sm.CurrentSessionId > 0)
		{
			await sm.UpdateSessionAsync(sm.CurrentSessionId, LevelNumber, _currentPoints, 0);
			GD.Print("3D Session progress saved.");
		}
	}
}
