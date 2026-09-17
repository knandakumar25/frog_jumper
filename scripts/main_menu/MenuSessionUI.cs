using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// MenuSessionUI handles saved game session management, "New Game" flow,
/// and session deletion/confirmation.
/// </summary>
public partial class MenuSessionUI : Node
{
	private Panel           _newGamePopup;
	private Panel           _continuePopup;
	private VBoxContainer   _sessionsContainer;
	private Control         _continueButton;
	private Panel           _confirmPopup;
	private Label           _confirmLabel;

	public void Initialize(Panel newGamePopup, Panel continuePopup, VBoxContainer sessionsContainer, Control continueButton, Panel confirmPopup, Label confirmLabel)
	{
		_newGamePopup = newGamePopup;
		_continuePopup = continuePopup;
		_sessionsContainer = sessionsContainer;
		_continueButton = continueButton;
		_confirmPopup = confirmPopup;
		_confirmLabel = confirmLabel;
	}

	public void OpenNewGame()
	{
		_newGamePopup.Visible = true;
		_newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ConfirmButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public async void OpenContinue()
	{
		await PopulateSessions();
		_continuePopup.Visible = true;
		FocusFirstSessionButton();
	}

	public void CloseAll()
	{
		if (_newGamePopup != null) _newGamePopup.Visible = false;
		if (_continuePopup != null) _continuePopup.Visible = false;
		if (_confirmPopup != null) _confirmPopup.Visible = false;
	}

	private async Task PopulateSessions()
	{
		foreach (Node child in _sessionsContainer.GetChildren())
			child.QueueFree();

		var sessions = await SessionManager.Instance.GetAllSessionsAsync();
		if (sessions.Count == 0)
		{
			var empty = new Label
			{
				Text                = Tr("No saved games found"),
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			empty.AddThemeFontSizeOverride("font_size", 24);
			empty.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Rajdhani-Medium.ttf"));
			empty.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.95f, 0.8f));
			_sessionsContainer.AddChild(empty);
			return;
		}

		var actionRow    = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		var deleteAllBtn = MakeDeleteButton(Tr("// PURGE ALL RECORDS"));
		deleteAllBtn.Pressed += OnDeleteAllPressed;
		actionRow.AddChild(deleteAllBtn);
		_sessionsContainer.AddChild(actionRow);

		_sessionsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

		var orbitronFont = GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf");
		var rajdhaniFont = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");

		int idx = 1;
		foreach (var item in sessions)
		{
			var session    = (Godot.Collections.Dictionary)item;
			int  sessionId = session["id"].AsInt32();
			string character  = (string)session["character"];
			int  level     = session["current_level"].AsInt32();
			int  score     = session["total_score"].AsInt32();
			string lastPlayed = SessionManager.Instance.FormatDatetime((string)session["last_played"]);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);

			string cardTitle = $"[{idx:D2}]  {Tr(character).ToUpper()}   |   {Tr("Level")} {level}   |   {Tr("Score")}: {score}   |   {lastPlayed}";
			var sessionBtn = new Button
			{
				Text                = cardTitle,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize   = new Vector2(0, 68),
				Alignment           = HorizontalAlignment.Left
			};
			sessionBtn.AddThemeFontSizeOverride("font_size", 19);
			sessionBtn.AddThemeFontOverride("font", rajdhaniFont);

			var normalBox = new StyleBoxFlat
			{
				BgColor = new Color(0.04f, 0.08f, 0.18f, 0.92f),
				BorderColor = new Color(0f, 0.94f, 1f, 0.70f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
				ContentMarginLeft = 20, ContentMarginRight = 20
			};
			normalBox.SetCornerRadiusAll(8);

			var hoverBox = new StyleBoxFlat
			{
				BgColor = new Color(0.08f, 0.14f, 0.32f, 0.98f),
				BorderColor = new Color(1f, 0f, 0.5f, 1.0f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
				ContentMarginLeft = 20, ContentMarginRight = 20
			};
			hoverBox.SetCornerRadiusAll(8);

			sessionBtn.AddThemeStyleboxOverride("normal",  normalBox);
			sessionBtn.AddThemeStyleboxOverride("hover",   hoverBox);
			sessionBtn.AddThemeStyleboxOverride("pressed", hoverBox);
			sessionBtn.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1f, 1f));

			var capturedId   = sessionId;
			var capturedSess = session;
			sessionBtn.Pressed += () => OnSessionSelected(capturedId, capturedSess);
			row.AddChild(sessionBtn);

			var delBtn = new Button
			{
				Text              = "✕",
				CustomMinimumSize = new Vector2(60, 68),
			};
			delBtn.AddThemeFontSizeOverride("font_size", 22);
			delBtn.AddThemeFontOverride("font", orbitronFont);

			var delNormal = new StyleBoxFlat
			{
				BgColor = new Color(0.18f, 0.04f, 0.08f, 0.90f),
				BorderColor = new Color(1f, 0f, 0.35f, 0.75f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2
			};
			delNormal.SetCornerRadiusAll(8);

			var delHover = new StyleBoxFlat
			{
				BgColor = new Color(0.35f, 0.06f, 0.12f, 0.98f),
				BorderColor = new Color(1f, 0.2f, 0.45f, 1.0f),
				BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2
			};
			delHover.SetCornerRadiusAll(8);

			delBtn.AddThemeStyleboxOverride("normal",  delNormal);
			delBtn.AddThemeStyleboxOverride("hover",   delHover);
			delBtn.AddThemeStyleboxOverride("pressed", delHover);
			delBtn.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.45f, 1f));
			delBtn.Pressed += () => OnDeleteOnePressed(capturedId);
			row.AddChild(delBtn);

			_sessionsContainer.AddChild(row);
			_sessionsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
			idx++;
		}
	}

	private static Button MakeDeleteButton(string text)
	{
		var btn = new Button { Text = text };
		btn.AddThemeFontSizeOverride("font_size", 15);
		btn.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Orbitron-VariableFont.ttf"));

		var style = new StyleBoxFlat
		{
			BgColor = new Color(0.20f, 0.04f, 0.08f, 0.90f),
			BorderColor = new Color(1f, 0f, 0.35f, 0.85f),
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8
		};
		style.SetCornerRadiusAll(6);

		btn.AddThemeStyleboxOverride("normal", style);
		btn.AddThemeStyleboxOverride("hover",  style);
		btn.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.5f, 1f));
		return btn;
	}

	private void OnDeleteOnePressed(int sessionId)
	{
		_confirmLabel.Text = Tr("Delete this saved game?") + "\n" + Tr("This cannot be undone.");
		_confirmPopup.SetMeta("delete_mode",       "one");
		_confirmPopup.SetMeta("delete_session_id", sessionId);
		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
		_confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void OnDeleteAllPressed()
	{
		_confirmLabel.Text = Tr("Delete ALL saved games?") + "\n" + Tr("This cannot be undone.");
		_confirmPopup.SetMeta("delete_mode", "all");
		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
		_confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void OnSessionSelected(int sessionId, Godot.Collections.Dictionary sessionData)
	{
		string character  = (string)sessionData["character"];
		int    level      = sessionData["current_level"].AsInt32();
		string lastPlayed = SessionManager.Instance.FormatDatetime((string)sessionData["last_played"]);

		_confirmLabel.Text = character + "  |  " + Tr("Level") + " " + level + "\n" +
								 Tr("Continue this game?") + "\n" + Tr("Last played") + ": " + lastPlayed;

		_confirmPopup.SetMeta("delete_mode",   "none");
		_confirmPopup.SetMeta("session_id",    sessionId);
		_confirmPopup.SetMeta("session_data",  sessionData);

		_continuePopup.Visible = false;
		_confirmPopup.Visible  = true;
		_confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/YesButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public async void ConfirmAction()
	{
		string deleteMode = (string)_confirmPopup.GetMeta("delete_mode", "none");

		if (deleteMode == "one")
		{
			int delId = _confirmPopup.GetMeta("delete_session_id").AsInt32();
			await SessionManager.Instance.DeleteSessionAsync(delId);
			_confirmPopup.Visible = false;
			if (SessionManager.Instance.HasSessions())
			{
				await PopulateSessions();
				_continuePopup.Visible = true;
				FocusFirstSessionButton();
			}
			else
			{
				_continuePopup.Visible = _continueButton.Visible = false;
			}
			return;
		}

		if (deleteMode == "all")
		{
			await SessionManager.Instance.DeleteAllSessionsAsync();
			_confirmPopup.Visible  = false;
			_continuePopup.Visible = false;
			_continueButton.Visible = false;
			return;
		}

		// Continue session
		int sessionId   = _confirmPopup.GetMeta("session_id").AsInt32();
		var sessionData = (Godot.Collections.Dictionary)_confirmPopup.GetMeta("session_data");
		await SessionManager.Instance.LoadSessionAsync(sessionId);
		GetNode<Node>("/root/Global").Set("selected_character", sessionData["character"]);

		int  levelNum   = sessionData["current_level"].AsInt32();
		string levelScene = "res://scenes/gameplay/level" + levelNum + ".tscn";

		_confirmPopup.Visible = false;
		LoadingScreen.LoadScene(GetTree(), levelScene);
	}

	public void CancelAction()
	{
		_confirmPopup.Visible  = false;
		_continuePopup.Visible = true;
		FocusFirstSessionButton();
	}

	public void ConfirmNewGame()
	{
		_newGamePopup.Visible = false;
		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/character_selection.tscn");
	}

	public void CancelNewGame()
	{
		_newGamePopup.Visible = false;
	}

	private void FocusFirstSessionButton()
	{
		if (_sessionsContainer == null) return;
		foreach (Node child in _sessionsContainer.GetChildren())
		{
			if (child is HBoxContainer row)
			{
				foreach (Node c in row.GetChildren())
				{
					if (c is Button btn && btn.Visible && !btn.Disabled)
					{
						btn.CallDeferred(Control.MethodName.GrabFocus);
						return;
					}
				}
			}
			else if (child is Button b && b.Visible && !b.Disabled)
			{
				b.CallDeferred(Control.MethodName.GrabFocus);
				return;
			}
		}
		_continuePopup?.GetNodeOrNull<Button>("VBoxContainer/BackButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private string Tr(string text) => MainMenu.Tr(text);
}
