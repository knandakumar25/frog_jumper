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

	private void ShowPopupAnimated(Panel popup)
	{
		if (popup == null) return;

		popup.Visible = true;
		popup.Modulate = new Color(1, 1, 1, 0);
		popup.Scale = new Vector2(0.95f, 0.95f);
		popup.PivotOffset = popup.Size / 2;

		var tween = GetTree().CreateTween().SetParallel(true);
		tween.TweenProperty(popup, "modulate", new Color(1, 1, 1, 1), 0.2f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(popup, "scale", new Vector2(1, 1), 0.2f).SetTrans(Tween.TransitionType.Sine);
	}

	public void OpenNewGame()
	{
		ShowPopupAnimated(_newGamePopup);
		_newGamePopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/ConfirmButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	public async void OpenContinue()
	{
		await PopulateSessions();
		ShowPopupAnimated(_continuePopup);
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
		{
			_sessionsContainer.RemoveChild(child);
			child.QueueFree();
		}

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

			// Session Card using CyberButton
			var sessionBtn = new CyberButton
			{
				MainText = $"[{idx:D2}] {Tr(character).ToUpper()}",
				SubText = $"{Tr("Level")} {level}   |   {Tr("Score")} {score}   |   {lastPlayed}",
				IndexText = "00",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize   = new Vector2(0, 68),
			};

			var capturedId   = sessionId;
			var capturedSess = session;
			sessionBtn.Pressed += () => OnSessionSelected(capturedId, capturedSess);
			row.AddChild(sessionBtn);

			// Deletion Button (Small CyberButton variant)
			var delBtn = new CyberButton
			{
				MainText = "✕",
				SubText = "// DEL",
				IndexText = "!",
				CustomMinimumSize = new Vector2(70, 68),
			};
			// Override some colors for the delete button to make it look dangerous
			delBtn.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.45f, 1f));

			delBtn.Pressed += () => OnDeleteOnePressed(capturedId);
			row.AddChild(delBtn);

			_sessionsContainer.AddChild(row);
			_sessionsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
			idx++;
		}
	}

	private static CyberButton MakeDeleteButton(string text)
	{
		var btn = new CyberButton
		{
			MainText = text,
			SubText = "// SYSTEM PURGE",
			IndexText = "!!",
			CustomMinimumSize = new Vector2(200, 44)
		};
		btn.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.5f, 1f));
		return btn;
	}

	private void OnDeleteOnePressed(int sessionId)
	{
		_confirmLabel.Text = Tr("Delete this saved game?") + "\n" + Tr("This cannot be undone.");
		_confirmPopup.SetMeta("delete_mode",       "one");
		_confirmPopup.SetMeta("delete_session_id", sessionId);
		_continuePopup.Visible = false;
		ShowPopupAnimated(_confirmPopup);
		_confirmPopup.GetNodeOrNull<Button>("VBoxContainer/ButtonContainer/NoButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void OnDeleteAllPressed()
	{
		_confirmLabel.Text = Tr("Delete ALL saved games?") + "\n" + Tr("This cannot be undone.");
		_confirmPopup.SetMeta("delete_mode", "all");
		_continuePopup.Visible = false;
		ShowPopupAnimated(_confirmPopup);
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
		ShowPopupAnimated(_confirmPopup);
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
				CloseEmptyHistory();
			}
			return;
		}

		if (deleteMode == "all")
		{
			await SessionManager.Instance.DeleteAllSessionsAsync();
			CloseEmptyHistory();
			return;
		}

		// Continue session
		int sessionId   = _confirmPopup.GetMeta("session_id").AsInt32();
		var sessionData = (Godot.Collections.Dictionary)_confirmPopup.GetMeta("session_data");
		await SessionManager.Instance.LoadSessionAsync(sessionId);

		// Older sessions may point at the removed tutorial (0) or retired missions; the catalog clamps them.
		int levelNum = LevelCatalog.Clamp(sessionData["current_level"].AsInt32());
		// The squad is rebuilt from progress, so the saved character is only honoured if it is unlocked by this mission.
		CampaignProgress.PrepareMission(levelNum, sessionData["character"].AsString());
		string levelScene = LevelCatalog.ScenePath(levelNum);

		_confirmPopup.Visible = false;
		LoadingScreen.LoadScene(GetTree(), levelScene);
	}

	public void CancelAction()
	{
		_confirmPopup.Visible  = false;
		_continuePopup.Visible = true;
		FocusFirstSessionButton();
	}

	// Both deleting the final save and purging all saves return to this state.
	public void CloseEmptyHistory()
	{
		CloseAll();
		if (_continueButton != null) _continueButton.Visible = false;
		if (IsInsideTree() && GetParent() is MainMenu menu)
			menu.RestoreMainMenuFocus();
	}

	/// <summary>
	/// Starts a new campaign with the starter operative, then plays deployment before the AXIOM-7 briefing.
	/// The remaining operatives are unlocked through play; the roster screen is reachable from the menus instead.
	/// </summary>
	public async void ConfirmNewGame()
	{
		_newGamePopup.Visible = false;
		int sessionId = await CampaignProgress.StartNewCampaignAsync();
		GD.Print(sessionId > 0 ? $"Created new session: {sessionId}" : "Error creating session; continuing unsaved.");
		LoadingScreen.LoadScene(GetTree(), "res://scenes/main_menu/deployment_cutscene.tscn");
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
					if (c is Control btn && CanFocusSessionControl(btn))
					{
						btn.CallDeferred(Control.MethodName.GrabFocus);
						return;
					}
				}
			}
			else if (child is Control b && CanFocusSessionControl(b))
			{
				b.CallDeferred(Control.MethodName.GrabFocus);
				return;
			}
		}
		_continuePopup?.GetNodeOrNull<Button>("VBoxContainer/BackButton")?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private static bool CanFocusSessionControl(Control control)
	{
		if (!control.IsInsideTree() || !control.IsVisibleInTree() ||
			control.IsQueuedForDeletion() || control.FocusMode == Control.FocusModeEnum.None)
			return false;
		return control is CyberButton cyber ? !cyber.IsDisabled : control is Button button && !button.Disabled;
	}

	private string Tr(string text) => MainMenu.Tr(text);
}
