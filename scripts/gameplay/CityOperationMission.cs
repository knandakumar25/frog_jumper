using Godot;
using System;

/// <summary>
/// Base for optional side-scrolling operations launched from a city. Reuses <see cref="CodeLevel"/>'s level
/// building but finishes on its own results panel: the reward is stored on the account's save slot and the player
/// returns to the world scene, which reopens the city they left. Campaign progress is never touched.
/// </summary>
public abstract partial class CityOperationMission : CodeLevel
{
    private Label _resultText = null!;
    private Button _continue = null!;
    // One token for the whole finish, so pressing Retry can never pay the reward twice.
    private readonly string _token = Guid.NewGuid().ToString();
    private bool _saved;

    /// <summary>ID of this operation in its city's content.</summary>
    protected abstract string OperationId { get; }
    /// <inheritdoc/>
    protected override bool IsCityOperation => true;
    /// <summary>Operations have no campaign slot; 0 is never a valid <see cref="LevelCatalog"/> entry.</summary>
    protected override int MissionNumber => 0;

    /// <summary>Freezes the level and shows the results panel, then stores the reward.</summary>
    protected override void OnCityOperationFinished(int score)
    {
        Actors.ProcessMode = ProcessModeEnum.Disabled;
        World.ProcessMode = ProcessModeEnum.Disabled;
        Callable.From(() => { if (IsInsideTree()) { BuildResults(); SaveReward(); } }).CallDeferred();
    }

    /// <summary>Builds a centred panel. Its one button is Retry after a failed save and Return after success.</summary>
    private void BuildResults()
    {
        var layer = new CanvasLayer { Name = "OperationResults", Layer = 40 };
        AddChild(layer);
        var panel = new PanelContainer { Name = "ResultsPanel" };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.OffsetLeft = -320; panel.OffsetRight = 320; panel.OffsetTop = -110; panel.OffsetBottom = 110;
        layer.AddChild(panel);
        var box = new VBoxContainer();
        panel.AddChild(box);
        _resultText = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        box.AddChild(_resultText);
        _continue = new Button { Name = "OperationContinue", Disabled = true };
        _continue.Pressed += OnContinue;
        box.AddChild(_continue);
    }

    /// <summary>Stores the clear. With no active campaign (a direct scene preview) nothing is written.</summary>
    private async void SaveReward()
    {
        _continue.Disabled = true;
        _continue.Text = WorldText.Get("Saving campaign…");
        _resultText.Text = WorldText.Get("Operation complete");
        int session = SessionManager.Instance?.CurrentSessionId ?? -1;
        bool ok = session <= 0;
        if (!ok)
        {
            // A database outage throws before the service can return a receipt; it must still end on Retry.
            try
            {
                var receipt = await WorldTransactionService.CompleteOperationAsync(session, OperationId, _token, OperativeRoster.Current.Active);
                ok = receipt.Status is TransactionStatus.Success or TransactionStatus.AlreadyCommitted;
            }
            catch (Exception) { ok = false; }
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        }
        _saved = ok;
        _continue.Disabled = false;
        _continue.Text = WorldText.Get(ok ? "Continue" : "Retry Save");
        if (!ok) _resultText.Text = WorldText.Get("Save failed. Retry to continue.");
        _continue.GrabFocus();
    }

    /// <summary>Returns to the city once saved, otherwise repeats the save with the same token.</summary>
    private void OnContinue()
    {
        if (!_saved) { SaveReward(); return; }
        LoadingScreen.LoadScene(GetTree(), WorldCatalog.Scene);
    }
}
