using Godot;
using System;

/// <summary>
/// GameOverUI provides a simple overlay that appears when the player's health reaches zero.
/// </summary>
public partial class GameOverUI : Control
{
	private Label _statusLabel;
	private Button _restartBtn;
	private Button _menuBtn;

	public override void _Ready()
	{
		Visible = false;
		_statusLabel = GetNode<Label>("Panel/VBox/StatusLabel");
		_restartBtn = GetNode<Button>("Panel/VBox/RestartBtn");
		_menuBtn = GetNode<Button>("Panel/VBox/MenuBtn");

		_restartBtn.Pressed += OnRestartPressed;
		_menuBtn.Pressed += OnMenuPressed;
	}

	public void ShowScreen()
	{
		Visible = true;
		_statusLabel.Text = "SYSTEM CRITICAL\nOPERATIVE TERMINATED";
	}

	private void OnRestartPressed()
	{
		// Reload the current scene.
		GetTree().ReloadCurrentScene();
	}

	private void OnMenuPressed()
	{
		// Transition back to main menu.
		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/main_menu.tscn");
	}
}
