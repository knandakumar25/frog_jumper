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

	private Control? _bossStatusContainer;
	private ProgressBar? _bossHealthBar;
	private Label? _bossPhaseBadge;

	private CyberShogun? _player;
	private CyberBossMech2D? _boss;

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

		_bossStatusContainer = GetNodeOrNull<Control>("RootControl/BossStatusContainer");
		_bossHealthBar = GetNodeOrNull<ProgressBar>("RootControl/BossStatusContainer/VBox/BossHealthBar");
		_bossPhaseBadge = GetNodeOrNull<Label>("RootControl/BossStatusContainer/VBox/PhaseBadge");

		if (_comboContainer != null)
		{
			_comboContainer.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		// Find the active player and boss in the scene
		BindPlayer();
		BindBoss();
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
		else if (IsInsideTree())
		{
			_player = GetTree()?.Root?.FindChild("CyberShogun", true, false) as CyberShogun;
		}

		if (_player != null)
		{
			_player.HealthChanged += OnHealthChanged;
			_player.DashCooldownUpdated += OnDashCooldownUpdated;
			_player.ComboChanged += OnComboChanged;
			_player.PlayerDefeated += OnPlayerDefeated;
			_player.OperativeProfileChanged += OnOperativeProfileChanged;

			// Initialize with current values
			if (_player.ActiveProfile != null)
			{
				OnOperativeProfileChanged(_player.ActiveProfile);
			}
			OnHealthChanged(_player.CurrentHealth, _player.MaxHealth, _player.CurrentShield, _player.MaxShield);
			OnDashCooldownUpdated(_player.DashCooldownTimer, _player.DashCooldown);
		}
	}

	private void OnOperativeProfileChanged(OperativeProfile profile)
	{
		if (profile == null) return;

		if (_katanaBadge != null)
		{
			_katanaBadge.Text = $"[F] {profile.PrimaryAbilityName.ToUpper()}";
			_katanaBadge.Modulate = profile.ThemeColor;
		}

		if (_dashBadge != null)
		{
			_dashBadge.Text = $"[SHIFT] {profile.DashAbilityName.ToUpper()} // READY";
			_dashBadge.Modulate = profile.ThemeColor;
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
			string opTag = _player?.ActiveProfile != null
				? $"{_player.ActiveProfile.DisplayName.ToUpper()} // "
				: "";
			_statusLabel.Text = $"{opTag}SHIELD: {(int)currentShield}/{(int)maxShield} | HP: {(int)currentHealth}/{(int)maxHealth}";
		}
	}

	private void OnDashCooldownUpdated(float remaining, float maxDuration)
	{
		if (_dashBadge == null) return;
		string dashName = _player?.ActiveProfile?.DashAbilityName.ToUpper() ?? "BLADE DASH";
		var themeColor = _player?.ActiveProfile?.ThemeColor ?? new Color(0f, 0.94f, 1f, 1f);

		if (remaining > 0.05f)
		{
			_dashBadge.Text = $"[SHIFT] {dashName} ({remaining:0.0}s)";
			_dashBadge.Modulate = new Color(0.6f, 0.6f, 0.6f, 0.8f);
		}
		else
		{
			_dashBadge.Text = $"[SHIFT] {dashName} // READY";
			_dashBadge.Modulate = themeColor;
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

	/// <summary>
	/// Locates and binds to the active level boss if present.
	/// </summary>
	public void BindBoss(CyberBossMech2D? boss = null)
	{
		if (boss != null)
		{
			_boss = boss;
		}
		else if (IsInsideTree())
		{
			_boss = GetTree()?.Root?.FindChild("CyberBossMech2D", true, false) as CyberBossMech2D;
		}

		if (_boss != null)
		{
			if (_bossStatusContainer != null) _bossStatusContainer.Visible = true;
			_boss.BossHealthChanged += OnBossHealthChanged;
			_boss.BossDefeated += OnBossDefeated;
			OnBossHealthChanged(_boss.CurrentHealth, _boss.MaxHealth, _boss.CurrentPhase);
		}
		else
		{
			if (_bossStatusContainer != null) _bossStatusContainer.Visible = false;
		}
	}

	private void OnBossHealthChanged(float current, float max, int phase)
	{
		if (_bossHealthBar != null)
		{
			_bossHealthBar.MaxValue = max;
			if (IsInsideTree())
			{
				var tween = CreateTween();
				tween?.TweenProperty(_bossHealthBar, "value", current, 0.2f);
			}
			else
			{
				_bossHealthBar.Value = current;
			}
		}

		if (_bossPhaseBadge != null)
		{
			if (phase == 2)
			{
				_bossPhaseBadge.Text = "PHASE 2 // OVERDRIVE RAGE";
				_bossPhaseBadge.Modulate = new Color(1.0f, 0.25f, 0.25f, 1f);
			}
			else
			{
				_bossPhaseBadge.Text = "PHASE 1 // TACTICAL HOVER";
				_bossPhaseBadge.Modulate = new Color(1.0f, 0.85f, 0.2f, 0.95f);
			}
		}
	}

	private void OnBossDefeated()
	{
		if (_bossPhaseBadge != null)
		{
			_bossPhaseBadge.Text = "MECH NEUTRALIZED // EXTRACTION GATE OPEN";
			_bossPhaseBadge.Modulate = new Color(0f, 0.94f, 1f, 1f);
		}

		if (_bossStatusContainer != null && IsInsideTree())
		{
			var tween = CreateTween();
			tween?.TweenInterval(1.5f);
			tween?.TweenProperty(_bossStatusContainer, "modulate:a", 0.0f, 0.8f);
		}
	}
}
