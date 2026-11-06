using Godot;
using System;

/// <summary>Menu-owned browser sign-in controls; guest navigation stays available while waiting.</summary>
public partial class BrowserSignInUI : VBoxContainer
{
    private Button _login, _signup, _reopen, _cancel;
    private Label _status;
    private string _statusKey = "";
    private AuthManager _auth;

    /// <summary>Create a compact panel below the existing profile frame and subscribe once.</summary>
    public override void _Ready()
    {
        Name = "BrowserSignInUI";
        var frame = GetParent().GetNodeOrNull<Control>("ProfileFrame");
        if (frame != null)
        {
            // Font and locale changes can grow the profile; keep the sign-in panel below it.
            frame.Resized += PlaceBelowProfile;
            PlaceBelowProfile();
        }
        CustomMinimumSize = new Vector2(320, 0);
        var actions = new HBoxContainer();
        AddChild(actions);
        _login = MakeButton(actions, "AUTH_LOGIN", () => Begin("login"));
        _signup = MakeButton(actions, "AUTH_SIGNUP", () => Begin("signup"));
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(320, 0) };
        AddChild(_status);
        var waiting = new HBoxContainer();
        AddChild(waiting);
        _reopen = MakeButton(waiting, "AUTH_REOPEN", () => _auth?.OpenBrowserAgain());
        _cancel = MakeButton(waiting, "AUTH_CANCEL", Cancel);
        _auth = AuthManager.Instance;
        if (_auth != null) { _auth.OnProfileUpdated += Refresh; _auth.OnBrowserStatus += Status; }
        Refresh();
    }

    /// <summary>Create native focusable buttons with translation keys retained for locale changes.</summary>
    private Button MakeButton(Node parent, string key, Action action)
    {
        var button = new Button { Name = key, Text = Tr(key), CustomMinimumSize = new Vector2(145, 42) };
        button.SetMeta("translation_key", key);
        button.AddThemeFontSizeOverride("font_size", 16);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    /// <summary>Follow the profile's measured size instead of overlapping its translated labels.</summary>
    private void PlaceBelowProfile()
    {
        if (!IsInsideTree()) return;
        var frame = GetParent().GetNodeOrNull<Control>("ProfileFrame");
        if (frame != null) Position = new Vector2(0, frame.Size.Y + 12);
    }

    /// <summary>Await the autoload on the main synchronization context, checking lifetime after completion.</summary>
    private async void Begin(string intent)
    {
        if (_auth == null || _auth.IsWaiting) return;
        var task = _auth.StartBrowserLoginAsync(intent, TranslationServer.GetLocale());
        Refresh();
        bool signedIn = await task;
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        Refresh();
        if (signedIn)
        {
            _statusKey = "";
            _status.Text = "";
            var window = GetWindow();
            if (window.Mode == Window.ModeEnum.Minimized) window.Mode = Window.ModeEnum.Windowed;
            window.GrabFocus();
            if (!window.HasFocus()) DisplayServer.WindowRequestAttention();
            GetParent().GetParent().GetNodeOrNull<Control>("ButtonContainer/Play")?.GrabFocus();
        }
    }

    /// <summary>Dismiss the current attempt without affecting guest play or the browser session.</summary>
    private void Cancel()
    {
        _statusKey = "";
        _auth?.CancelBrowserLogin();
        _status.Text = "";
    }

    /// <summary>Translate status keys without exposing transport payloads or secret values.</summary>
    private void Status(string key)
    {
        if (!IsInsideTree()) return;
        _statusKey = key;
        _status.Text = Tr(key);
    }

    /// <summary>Hide both entry points for an authenticated profile and disable duplicate attempts.</summary>
    private void Refresh()
    {
        bool signedIn = _auth?.CurrentUser != null;
        _login.Visible = _signup.Visible = !signedIn;
        _login.Disabled = _signup.Disabled = _auth?.IsWaiting ?? false;
        _reopen.Visible = _cancel.Visible = _auth?.IsWaiting ?? false;
    }

    /// <summary>Refresh translated controls in place when Settings changes the locale.</summary>
    public override void _Notification(int what)
    {
        if (what != NotificationTranslationChanged || _status == null) return;
        foreach (var button in new[] { _login, _signup, _reopen, _cancel })
            button.Text = Tr(button.GetMeta("translation_key").AsString());
        _status.Text = string.IsNullOrEmpty(_statusKey) ? "" : Tr(_statusKey);
    }

    /// <summary>Leaving the menu cancels polling and detaches every autoload subscription.</summary>
    public override void _ExitTree()
    {
        var frame = GetParent()?.GetNodeOrNull<Control>("ProfileFrame");
        if (frame != null) frame.Resized -= PlaceBelowProfile;
        if (_auth == null) return;
        _auth.OnProfileUpdated -= Refresh;
        _auth.OnBrowserStatus -= Status;
        _auth.CancelBrowserLogin();
    }
}
