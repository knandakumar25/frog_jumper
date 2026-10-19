using Godot;

/// <summary>
/// CyberCombatHud provides a high-fidelity 2D cybernetic tactical interface.
/// It renders dynamic shield/health bars, dash cooldown meters, score,
/// and responsive combo strike notifications.
/// </summary>
public partial class CyberCombatHud : CanvasLayer
{
	// ─── UI References ────────────────────────────────────────────────────────
	private ProgressBar? _healthBar;
	private ProgressBar? _shieldBar;
	private Label? _statusLabel;
	private Label? _pointsLabel;
	private Label? _dashBadge;
	private Label? _katanaBadge;
	private Control? _comboContainer;
	private Label? _comboLabel;

	private CyberShogun? _player;

	public override void _Ready()
	{
		_healthBar = GetNodeOrNull<ProgressBar>("RootControl/PlayerStatusContainer/VBox/HealthBar");
		_shieldBar = GetNodeOrNull<ProgressBar>("RootControl/PlayerStatusContainer/VBox/ShieldBar");
		_statusLabel = GetNodeOrNull<Label>("RootControl/PlayerStatusContainer/VBox/StatusLabel");
		_pointsLabel = GetNodeOrNull<Label>("%PointsLabel");
		_dashBadge = GetNodeOrNull<Label>("RootControl/PlayerStatusContainer/VBox/AbilitiesHBox/DashBadge");
		_katanaBadge = GetNodeOrNull<Label>("RootControl/PlayerStatusContainer/VBox/AbilitiesHBox/KatanaBadge");
		_comboContainer = GetNodeOrNull<Control>("RootControl/ComboContainer");
		_comboLabel = GetNodeOrNull<Label>("RootControl/ComboContainer/ComboLabel");

		if (_comboContainer != null)
		{
			_comboContainer.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		// Find the active player in the scene
		BindPlayer();
	}

	/// <summary>
	/// Locates and binds to the active player's combat signals.
	/// </summary>
	public void BindPlayer(CyberShogun? player = null)
	{
		if (player != null)
		{
			_player = player;
		}
		else
		{
			_player = GetTree()?.Root?.FindChild("CyberShogun", true, false) as CyberShogun;
		}

		if (_player != null)
		{
			_player.HealthChanged += OnHealthChanged;
			_player.DashCooldownUpdated += OnDashCooldownUpdated;
			_player.ComboChanged += OnComboChanged;
			_player.PlayerDefeated += OnPlayerDefeated;

			// Initialize with current values
			OnHealthChanged(_player.CurrentHealth, _player.MaxHealth, _player.CurrentShield, _player.MaxShield);
			OnDashCooldownUpdated(_player.DashCooldownTimer, _player.DashCooldown);
		}
	}

	private void OnHealthChanged(float currentHealth, float maxHealth, float currentShield, float maxShield)
	{
		if (_healthBar != null)
		{
			_healthBar.MaxValue = maxHealth;
			if (IsInsideTree())
			{
				var tween = CreateTween();
				tween?.TweenProperty(_healthBar, "value", currentHealth, 0.2f);
			}
			else
			{
				_healthBar.Value = currentHealth;
			}
		}

		if (_shieldBar != null)
		{
			_shieldBar.MaxValue = maxShield;
			if (IsInsideTree())
			{
				var tween = CreateTween();
				tween?.TweenProperty(_shieldBar, "value", currentShield, 0.2f);
			}
			else
			{
				_shieldBar.Value = currentShield;
			}
		}

		if (_statusLabel != null)
		{
			_statusLabel.Text = $"SHIELD: {(int)currentShield}/{(int)maxShield} | HP: {(int)currentHealth}/{(int)maxHealth}";
		}
	}

	private void OnDashCooldownUpdated(float remaining, float maxDuration)
	{
		if (_dashBadge == null) return;

		if (remaining > 0.05f)
		{
			_dashBadge.Text = $"[SHIFT] DASH ({remaining:0.0}s)";
			_dashBadge.Modulate = new Color(0.6f, 0.6f, 0.6f, 0.8f);
		}
		else
		{
			_dashBadge.Text = "[SHIFT] BLADE DASH // READY";
			_dashBadge.Modulate = new Color(0f, 0.94f, 1f, 1f);
		}
	}

	private void OnComboChanged(int comboCount)
	{
		if (_comboContainer == null || _comboLabel == null) return;

		if (comboCount > 1)
		{
			string title = comboCount switch
			{
				2 => "DOUBLE STRIKE!",
				3 => "TRIPLE COMBO!",
				4 => "QUAD BLADE!",
				5 => "TACTICAL OVERDRIVE!",
				_ => "HYPER OVERDRIVE!"
			};

			_comboLabel.Text = $"{comboCount}x COMBO // {title}";
			if (IsInsideTree())
			{
				var tween = CreateTween();
				tween?.TweenProperty(_comboContainer, "modulate:a", 1.0f, 0.12f);
				tween?.Parallel().TweenProperty(_comboContainer, "scale", new Vector2(1.15f, 1.15f), 0.1f);
				tween?.TweenProperty(_comboContainer, "scale", new Vector2(1.0f, 1.0f), 0.15f);
			}
			else
			{
				_comboContainer.Modulate = new Color(1f, 1f, 1f, 1f);
				_comboContainer.Scale = Vector2.One;
			}
		}
		else
		{
			if (IsInsideTree())
			{
				var tween = CreateTween();
				tween?.TweenProperty(_comboContainer, "modulate:a", 0.0f, 0.35f);
			}
			else
			{
				_comboContainer.Modulate = new Color(1f, 1f, 1f, 0f);
			}
		}
	}

	private void OnPlayerDefeated()
	{
		if (_statusLabel != null)
		{
			_statusLabel.Text = "OPERATIVE CRITICAL // SYSTEM REBOOTING...";
			_statusLabel.Modulate = new Color(1f, 0.2f, 0.2f, 1f);
		}
	}
}
