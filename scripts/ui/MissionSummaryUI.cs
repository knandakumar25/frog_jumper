using Godot;
using System;

/// <summary>
/// MissionSummaryUI provides a final report after completing a level.
/// </summary>
public partial class MissionSummaryUI : Control
{
	private Label _scoreLabel;
	private Label _cherriesLabel;
	private Button _continueBtn;

	public override void _Ready()
	{
		Visible = false;
		_scoreLabel = GetNode<Label>("Panel/VBox/ScoreLabel");
		_cherriesLabel = GetNode<Label>("Panel/VBox/CherriesLabel");
		_continueBtn = GetNode<Button>("Panel/VBox/ContinueBtn");

		_continueBtn.Pressed += OnContinuePressed;
	}

	public void ShowSummary(int score, int cherries)
	{
		Visible = true;
		_scoreLabel.Text = $"FINAL SCORE: {score}";
		_cherriesLabel.Text = $"CHERRYS RECOVERED: {cherries}";
	}

	private void OnContinuePressed()
	{
		// Transition to main menu or next level.
		// For now, return to main menu.
		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/main_menu.tscn");
	}
}
