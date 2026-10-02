using Godot;
using System;

/// <summary>
/// OperativeUpgradeScreen presents the cyberpunk augmentation and level-up matrix for an operative:
/// - Visualizes the current character-specific level and Sparks progression meter
/// - Allows leveling up when sufficient Sparks have been harvested
/// - Provides 3 hero-specific augmentation tracks with rank indicators and unlock buttons
/// - Shows real-time stat bonuses (Health, Speed, Damage, Cooldown Reduction)
/// - Provides full keyboard / controller and mouse navigation
/// </summary>
public partial class OperativeUpgradeScreen : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	[Signal]
	public delegate void UpgradesChangedEventHandler(string operativeName);

	private string _currentOperative = "Cyber Shogun";
	private bool _nodesInitialized = false;

	// UI References
	private Label? _screenTitleLabel;
	private Label? _operativeNameLabel;
	private Label? _operativeClassLabel;
	private Label? _levelBadgeLabel;
	private Label? _sparksProgressLabel;
	private ProgressBar? _sparksProgressBar;
	private Button? _levelUpBtn;
	private Button? _harvestSparksBtn;
	private Label? _skillPointsLabel;
	private Label? _bankSparksLabel;

	// Stat Labels
	private Label? _healthStatLabel;
	private Label? _speedStatLabel;
	private Label? _damageStatLabel;
	private Label? _cdrStatLabel;

	// Upgrade Track Cards (3 tracks)
	private PanelContainer[] _trackPanels = new PanelContainer[3];
	private Label[] _trackNameLabels = new Label[3];
	private Label[] _trackRankLabels = new Label[3];
	private Label[] _trackDescLabels = new Label[3];
	private Label[] _trackNextLabels = new Label[3];
	private Button[] _trackUpgradeBtns = new Button[3];

	private Button? _backBtn;
	private Button? _respecBtn;

	public override void _Ready()
	{
		EnsureNodeReferences();
		RefreshUI();
		UISoundManager.AutoWireButtons(this);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape))
		{
			OnBackBtnPressed();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void EnsureNodeReferences()
	{
		if (_nodesInitialized) return;
		_nodesInitialized = true;

		_screenTitleLabel     = GetNodeOrNull<Label>("Header/Title");
		_operativeNameLabel   = GetNodeOrNull<Label>("MainVBox/TopCard/VBox/OperativeName");
		_operativeClassLabel  = GetNodeOrNull<Label>("MainVBox/TopCard/VBox/OperativeClass");
		_levelBadgeLabel      = GetNodeOrNull<Label>("MainVBox/TopCard/VBox/LevelRow/LevelBadge");
		_sparksProgressLabel  = GetNodeOrNull<Label>("MainVBox/TopCard/VBox/SparksBox/SparksProgressLabel");
		_sparksProgressBar   = GetNodeOrNull<ProgressBar>("MainVBox/TopCard/VBox/SparksBox/SparksProgressBar");
		_levelUpBtn           = GetNodeOrNull<Button>("MainVBox/TopCard/VBox/ActionRow/LevelUpBtn");
		_harvestSparksBtn     = GetNodeOrNull<Button>("MainVBox/TopCard/VBox/ActionRow/HarvestSparksBtn");
		_skillPointsLabel     = GetNodeOrNull<Label>("MainVBox/TopCard/VBox/ActionRow/SkillPointsLabel");
		_bankSparksLabel      = GetNodeOrNull<Label>("Header/BankSparksLabel");

		_healthStatLabel      = GetNodeOrNull<Label>("MainVBox/StatsPanel/HBox/HealthBox/Value");
		_speedStatLabel       = GetNodeOrNull<Label>("MainVBox/StatsPanel/HBox/SpeedBox/Value");
		_damageStatLabel      = GetNodeOrNull<Label>("MainVBox/StatsPanel/HBox/DamageBox/Value");
		_cdrStatLabel         = GetNodeOrNull<Label>("MainVBox/StatsPanel/HBox/CdrBox/Value");

		_backBtn              = GetNodeOrNull<Button>("Header/BackBtn");
		_respecBtn            = GetNodeOrNull<Button>("MainVBox/TopCard/VBox/ActionRow/RespecBtn");

		for (int i = 0; i < 3; i++)
		{
			int idx = i;
			_trackPanels[i]      = GetNodeOrNull<PanelContainer>($"MainVBox/TracksContainer/TrackCard_{i}");
			_trackNameLabels[i]  = GetNodeOrNull<Label>($"MainVBox/TracksContainer/TrackCard_{i}/VBox/HeaderRow/Name");
			_trackRankLabels[i]  = GetNodeOrNull<Label>($"MainVBox/TracksContainer/TrackCard_{i}/VBox/HeaderRow/Rank");
			_trackDescLabels[i]  = GetNodeOrNull<Label>($"MainVBox/TracksContainer/TrackCard_{i}/VBox/Desc");
			_trackNextLabels[i]  = GetNodeOrNull<Label>($"MainVBox/TracksContainer/TrackCard_{i}/VBox/NextRankPreview");
			_trackUpgradeBtns[i] = GetNodeOrNull<Button>($"MainVBox/TracksContainer/TrackCard_{i}/VBox/UpgradeBtn");

			if (_trackUpgradeBtns[i] != null)
			{
				_trackUpgradeBtns[i].Pressed += () => OnUpgradeTrackPressed(idx);
			}
		}

		if (_levelUpBtn != null) _levelUpBtn.Pressed += OnLevelUpBtnPressed;
		if (_harvestSparksBtn != null) _harvestSparksBtn.Pressed += OnHarvestSparksBtnPressed;
		if (_respecBtn != null) _respecBtn.Pressed += OnRespecBtnPressed;
		if (_backBtn != null) _backBtn.Pressed += OnBackBtnPressed;
	}

	/// <summary>
	/// Opens the upgrade screen configured for the given operative.
	/// </summary>
	public void OpenForOperative(string operativeName)
	{
		_currentOperative = operativeName;
		EnsureNodeReferences();
		RefreshUI();
		Show();

		// Default focus to Level Up button or first upgrade button
		if (_levelUpBtn != null && !_levelUpBtn.Disabled)
		{
			_levelUpBtn.GrabFocus();
		}
		else if (_trackUpgradeBtns[0] != null)
		{
			_trackUpgradeBtns[0].GrabFocus();
		}
	}

	/// <summary>
	/// Refreshes all labels, progress bars, and button states based on current progression.
	/// </summary>
	public void RefreshUI()
	{
		var mgr = CharacterProgressionManager.Instance;
		if (mgr == null) return;

		var hero = mgr.GetOperativeData(_currentOperative);
		var defs = CharacterProgressionManager.GetUpgradeDefs(_currentOperative);

		// Operative Name & Class
		if (_operativeNameLabel != null) _operativeNameLabel.Text = _currentOperative.ToUpperInvariant();
		if (_operativeClassLabel != null)
		{
			_operativeClassLabel.Text = $"// LEVEL {hero.Level} AUGMENTATION PROFILE";
		}

		// Level & Sparks Progress
		int requiredSparks = CharacterProgressionManager.GetRequiredSparksForLevel(hero.Level);
		bool isMaxLevel = hero.Level >= CharacterProgressionManager.MaxLevel;

		if (_levelBadgeLabel != null)
		{
			_levelBadgeLabel.Text = isMaxLevel ? "LVL 10 [MAX]" : $"LVL {hero.Level:D2}";
		}

		if (_sparksProgressLabel != null)
		{
			_sparksProgressLabel.Text = isMaxLevel
				? "⚡ MAXIMUM AUGMENTATION ACHIEVED"
				: $"⚡ {hero.CurrentSparks} / {requiredSparks} SPARKS";
		}

		if (_sparksProgressBar != null)
		{
			_sparksProgressBar.MinValue = 0;
			_sparksProgressBar.MaxValue = isMaxLevel ? 1 : requiredSparks;
			_sparksProgressBar.Value = isMaxLevel ? 1 : Math.Min(hero.CurrentSparks, requiredSparks);
		}

		// Level Up Button
		if (_levelUpBtn != null)
		{
			bool canLevel = mgr.CanLevelUp(_currentOperative);
			_levelUpBtn.Disabled = !canLevel;
			_levelUpBtn.Text = isMaxLevel
				? "MAX LEVEL"
				: (canLevel ? $"⚡ LEVEL UP ({requiredSparks} SPARKS)" : $"LEVEL UP ({requiredSparks} SPARKS)");
		}

		// Skill Points & Bank
		if (_skillPointsLabel != null)
		{
			_skillPointsLabel.Text = $"SKILL POINTS: {hero.SkillPoints}";
			_skillPointsLabel.Modulate = hero.SkillPoints > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.6f, 0.7f);
		}

		if (_bankSparksLabel != null)
		{
			_bankSparksLabel.Text = $"⚡ BANK: {mgr.GlobalSparksBank} SPARKS";
		}

		// Effective Stats
		var mods = mgr.GetEffectiveModifiers(_currentOperative);
		if (_healthStatLabel != null) _healthStatLabel.Text = $"+{mods.healthBonusPct * 100f:F0}% HP";
		if (_speedStatLabel != null)  _speedStatLabel.Text  = $"+{mods.speedBonusPct * 100f:F1}% SPD";
		if (_damageStatLabel != null) _damageStatLabel.Text = $"+{mods.damageBonusPct * 100f:F0}% ATK";
		if (_cdrStatLabel != null)    _cdrStatLabel.Text    = $"-{mods.cooldownReductionPct * 100f:F0}% CDR";

		// Upgrade Tracks (3 tracks)
		for (int i = 0; i < 3 && i < defs.Length; i++)
		{
			var def = defs[i];
			int rank = hero.UpgradeRanks[i];
			bool isMaxRank = rank >= CharacterProgressionManager.MaxUpgradeRank;

			if (_trackNameLabels[i] != null)
			{
				_trackNameLabels[i].Text = $"{def.Icon} {def.Name}";
			}

			if (_trackRankLabels[i] != null)
			{
				string pips = "";
				for (int p = 1; p <= CharacterProgressionManager.MaxUpgradeRank; p++)
				{
					pips += (p <= rank) ? "■ " : "□ ";
				}
				_trackRankLabels[i].Text = $"[ {pips.Trim()} ]  RANK {rank}/{CharacterProgressionManager.MaxUpgradeRank}";
				_trackRankLabels[i].Modulate = isMaxRank ? new Color(1f, 0.84f, 0.2f) : new Color(0f, 0.94f, 1f);
			}

			if (_trackDescLabels[i] != null)
			{
				string currentDesc = (rank > 0 && rank < def.RankDescriptions.Length)
					? def.RankDescriptions[rank]
					: def.Tagline;
				_trackDescLabels[i].Text = currentDesc;
			}

			if (_trackNextLabels[i] != null)
			{
				if (isMaxRank)
				{
					_trackNextLabels[i].Text = "✔ MAXIMUM AUGMENTATION RANK REACHED";
					_trackNextLabels[i].Modulate = new Color(0.2f, 0.9f, 0.5f);
				}
				else
				{
					string nextDesc = (rank + 1 < def.RankDescriptions.Length)
						? def.RankDescriptions[rank + 1]
						: $"+{def.StatBonusPerRank * 100f:F0}% {def.StatLabel}";
					_trackNextLabels[i].Text = $"NEXT: {nextDesc}";
					_trackNextLabels[i].Modulate = new Color(1f, 0.75f, 0.2f);
				}
			}

			if (_trackUpgradeBtns[i] != null)
			{
				bool canUpgrade = hero.SkillPoints > 0 && !isMaxRank;
				_trackUpgradeBtns[i].Disabled = !canUpgrade;
				_trackUpgradeBtns[i].Text = isMaxRank ? "MAXED" : "UPGRADE [ 1 SP ]";
			}
		}
	}

	private void OnLevelUpBtnPressed()
	{
		var mgr = CharacterProgressionManager.Instance;
		if (mgr == null) return;

		if (mgr.LevelUp(_currentOperative))
		{
			UISoundManager.Instance?.PlayTransitionSound();
			RefreshUI();
			EmitSignal(SignalName.UpgradesChanged, _currentOperative);
		}
	}

	private void OnHarvestSparksBtnPressed()
	{
		var mgr = CharacterProgressionManager.Instance;
		if (mgr == null) return;

		mgr.AddSparks(_currentOperative, 100);
		UISoundManager.Instance?.PlayClickSound();
		RefreshUI();
		EmitSignal(SignalName.UpgradesChanged, _currentOperative);
	}

	private void OnUpgradeTrackPressed(int trackIndex)
	{
		var mgr = CharacterProgressionManager.Instance;
		if (mgr == null) return;

		if (mgr.PurchaseUpgrade(_currentOperative, trackIndex))
		{
			UISoundManager.Instance?.PlayClickSound();
			RefreshUI();
			EmitSignal(SignalName.UpgradesChanged, _currentOperative);
		}
	}

	private void OnRespecBtnPressed()
	{
		var mgr = CharacterProgressionManager.Instance;
		if (mgr == null) return;

		mgr.RespecUpgrades(_currentOperative);
		UISoundManager.Instance?.PlayTransitionSound();
		RefreshUI();
		EmitSignal(SignalName.UpgradesChanged, _currentOperative);
	}

	private void OnBackBtnPressed()
	{
		UISoundManager.Instance?.PlayClickSound();
		Hide();
		EmitSignal(SignalName.Closed);
	}
}
