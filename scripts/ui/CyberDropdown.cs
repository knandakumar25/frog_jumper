using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// CyberDropdown is a high-fidelity, custom-drawn dropdown selector that replaces
/// the standard Godot OptionButton. It features a cyberpunk aesthetic with
/// chamfered corners, neon borders, and a floating custom popover menu.
///
/// Unity Migration Note:
/// 1. Visuals: Use a custom UI Mesh or a Sliced Sprite (9-slice) for the main
///    dropdown body.
/// 2. Popup Menu: Instead of a CanvasLayer, use a Unity Canvas with a 'Sorting Order'
///    higher than the rest of the UI, or a 'World Space' canvas anchored to the
///    dropdown's position.
/// 3. List Generation: Use a 'Vertical Layout Group' and 'Content Size Fitter'
///    component to dynamically instantiate item buttons from a Prefab.
/// 4. Interaction: Use the IPointerClickHandler interface to toggle the popup.
/// </summary>
public partial class CyberDropdown : Control
{
	[Signal] public delegate void ItemSelectedEventHandler(int index, string value);

	// Internal state
	private List<string> _items = new List<string>();
	private int _selectedIndex = -1;
	private bool _isHovered = false;
	private bool _isOpen = false;

	// Cyberpunk Color Palette
	private Color _bgNormal      = new Color(0.04f, 0.08f, 0.16f, 0.92f);
	private Color _bgHover       = new Color(0.08f, 0.18f, 0.32f, 0.98f);
	private Color _borderCyan    = new Color(0.00f, 0.94f, 1.00f, 0.90f);
	private Color _borderMagenta = new Color(1.00f, 0.00f, 0.50f, 0.95f);
	private Color _textColor     = new Color(0.95f, 0.98f, 1.00f, 1.00f);

	private Font _font;
	private CanvasLayer _popupLayer;
	private PanelContainer _popupPanel;
	private VBoxContainer _popupVBox;

	public int SelectedIndex
	{
		get => _selectedIndex;
		set
		{
			if (value >= 0 && value < _items.Count)
			{
				_selectedIndex = value;
				QueueRedraw();
			}
		}
	}

	public string SelectedValue => (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : "";

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(320, 52);
		MouseFilter = MouseFilterEnum.Ignore; // Completely eliminate cursor compatibility
		FocusMode = FocusModeEnum.All;
		_font = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf") ?? ThemeDB.FallbackFont;

		FocusEntered += () => { _isHovered = true;  QueueRedraw(); };
		FocusExited  += () => { _isHovered = false; QueueRedraw(); };

		CreatePopupLayer();
	}

	/// <summary>
	/// Clears all items from the dropdown and resets the selection.
	/// </summary>
	public void Clear()
	{
		_items.Clear();
		_selectedIndex = -1;
		QueueRedraw();
	}

	/// <summary>
	/// Adds a new selectable item to the dropdown.
	/// </summary>
	public void AddItem(string item)
	{
		_items.Add(item);
		if (_selectedIndex == -1) _selectedIndex = 0;
		QueueRedraw();
	}

	/// <summary>
	/// Sets the current selection based on a matching text value.
	/// </summary>
	public void SelectItemByText(string text)
	{
		int idx = _items.IndexOf(text);
		if (idx >= 0)
		{
			_selectedIndex = idx;
			QueueRedraw();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouse)
		{
			AcceptEvent();
			return;
		}

		bool isAccept = @event.IsActionPressed("ui_accept") ||
			(@event is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == JoyButton.A) ||
			(@event is InputEventKey k && k.Pressed && !k.Echo && (k.Keycode == Key.Enter || k.Keycode == Key.Space));

		if (isAccept)
		{
			TogglePopup();
			AcceptEvent();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_isOpen && @event.IsActionPressed("ui_cancel"))
		{
			ClosePopup();
			GetViewport()?.SetInputAsHandled();
		}
	}

	/// <summary>
	/// Initializes the floating popup layer.
	/// Uses a separate CanvasLayer to ensure the menu is always rendered on top of other UI.
	/// </summary>
	private void CreatePopupLayer()
	{
		_popupLayer = new CanvasLayer { Layer = 150 };
		AddChild(_popupLayer);

		_popupPanel = new PanelContainer
		{
			Visible = false,
			ZIndex = 200,
		};

		var stylebox = new StyleBoxFlat
		{
			BgColor = new Color(0.03f, 0.06f, 0.12f, 0.98f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			BorderColor = _borderCyan,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ShadowColor = new Color(0, 0.94f, 1f, 0.35f),
			ShadowSize = 10,
			ContentMarginLeft = 8,
			ContentMarginTop = 8,
			ContentMarginRight = 8,
			ContentMarginBottom = 8,
		};
		_popupPanel.AddThemeStyleboxOverride("panel", stylebox);

		var scroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(340, 260),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		_popupPanel.AddChild(scroll);

		_popupVBox = new VBoxContainer();
		_popupVBox.AddThemeConstantOverride("separation", 4);
		scroll.AddChild(_popupVBox);

		_popupLayer.AddChild(_popupPanel);
	}

	private void TogglePopup()
	{
		if (_isOpen)
		{
			ClosePopup();
		}
		else
		{
			OpenPopup();
		}
	}

	/// <summary>
	/// Dynamically populates the popup menu with buttons for each item in the list.
	/// </summary>
	private void OpenPopup()
	{
		if (_items.Count == 0) return;

		foreach (Node child in _popupVBox.GetChildren())
			child.QueueFree();

		for (int i = 0; i < _items.Count; i++)
		{
			int index = i;
			string itemText = _items[i];

			var btn = new Button
			{
				Text = itemText,
				CustomMinimumSize = new Vector2(320, 42),
				Alignment = HorizontalAlignment.Left,
			};
			btn.AddThemeFontOverride("font", _font);
			btn.AddThemeFontSizeOverride("font_size", 20);

			var normalStyle = new StyleBoxFlat
			{
				BgColor = (index == _selectedIndex) ? new Color(0f, 0.4f, 0.6f, 0.4f) : new Color(0.06f, 0.12f, 0.22f, 0.8f),
				BorderWidthLeft = (index == _selectedIndex) ? 4 : 0,
				BorderColor = _borderMagenta,
				ContentMarginLeft = 14,
				ContentMarginRight = 14
			};
			var hoverStyle = new StyleBoxFlat
			{
				BgColor = new Color(0f, 0.6f, 0.9f, 0.5f),
				BorderWidthLeft = 4,
				BorderColor = _borderCyan,
				ContentMarginLeft = 18,
				ContentMarginRight = 14
			};

			btn.AddThemeStyleboxOverride("normal", normalStyle);
			btn.AddThemeStyleboxOverride("hover", hoverStyle);
			btn.AddThemeStyleboxOverride("focus", hoverStyle);
			btn.AddThemeColorOverride("font_color", _textColor);
			btn.AddThemeColorOverride("font_hover_color", new Color(0f, 1f, 1f, 1f));

			btn.Pressed += () =>
			{
				_selectedIndex = index;
				QueueRedraw();
				EmitSignal(SignalName.ItemSelected, index, itemText);
				ClosePopup();
			};

			_popupVBox.AddChild(btn);
		}

		// Position the popup below the dropdown body, with screen boundary protection.
		Vector2 globalPos = GlobalPosition;
		Vector2 size = Size;
		float viewportHeight = GetViewportRect().Size.Y;
		float popupHeight = _popupPanel.Size.Y > 0 ? _popupPanel.Size.Y : 276f;
		float posY = globalPos.Y + size.Y + 6;
		if (posY + popupHeight > viewportHeight && globalPos.Y - popupHeight - 6 >= 0)
		{
			posY = globalPos.Y - popupHeight - 6;
		}
		_popupPanel.Position = new Vector2(globalPos.X, posY);
		_popupPanel.Visible = true;
		_isOpen = true;
		QueueRedraw();

		if (_popupVBox.GetChildCount() > 0)
		{
			int focusIdx = Mathf.Clamp(_selectedIndex, 0, _popupVBox.GetChildCount() - 1);
			var targetBtn = _popupVBox.GetChild<Button>(focusIdx);
			targetBtn?.CallDeferred(Control.MethodName.GrabFocus);
		}
	}

	public void ClosePopup()
	{
		if (_popupPanel != null) _popupPanel.Visible = false;
		_isOpen = false;
		QueueRedraw();
	}

	/// <summary>
	/// Renders the main dropdown body with chamfered corners and state-dependent borders.
	/// </summary>
	public override void _Draw()
	{
		Vector2 size = Size;
		float cut = 12.0f; // Size of the 45 degree corner cuts.

		// Polygon Points for the Octagonal-style body.
		Vector2[] points = new Vector2[]
		{
			new Vector2(cut, 0),
			new Vector2(size.X - cut, 0),
			new Vector2(size.X, cut),
			new Vector2(size.X, size.Y - cut),
			new Vector2(size.X - cut, size.Y),
			new Vector2(cut, size.Y),
			new Vector2(0, size.Y - cut),
			new Vector2(0, cut)
		};

		Color fill = (_isHovered || HasFocus()) ? _bgHover : _bgNormal;
		Color stroke = (_isHovered || _isOpen || HasFocus()) ? _borderMagenta : _borderCyan;

		// Render the main background.
		DrawPolygon(points, new Color[] { fill, fill, fill, fill, fill, fill, fill, fill });

		// Render the outer border.
		Vector2[] outline = new Vector2[points.Length + 1];
		Array.Copy(points, outline, points.Length);
		outline[points.Length] = points[0];

		DrawPolyline(outline, stroke, (_isHovered || _isOpen) ? 2.5f : 1.8f, true);

		// Corner tech reticles for visual detail.
		float rLen = 6.0f;
		DrawLine(new Vector2(cut, 0), new Vector2(cut + rLen, 0), stroke, 2.0f);
		DrawLine(new Vector2(size.X - cut - rLen, 0), new Vector2(size.X - cut, 0), stroke, 2.0f);

		// Render the current selection text.
		string text = SelectedValue;
		if (string.IsNullOrEmpty(text)) text = "Select Language";

		Vector2 textPos = new Vector2(16, size.Y / 2.0f + 6);
		DrawString(_font, textPos, text, HorizontalAlignment.Left, -1, 20, _textColor);

		// Render the animated dropdown arrow indicator (▼ / ▲).
		string arrow = _isOpen ? "▲" : "▼";
		Vector2 arrowPos = new Vector2(size.X - 38, size.Y / 2.0f + 6);
		DrawString(_font, arrowPos, arrow, HorizontalAlignment.Center, 24, 18, stroke);
	}
}
