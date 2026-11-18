using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// CharacterProgressionManager is a singleton that manages character-specific progression:
/// - Independent character levels (Level 1 to 10)
/// - Hero Sparks currency and level-up progress meters
/// - Skill Points and 3 unique hero-specific Augmentation Tracks (Tiers 0 to 5)
/// - Real-time stat multipliers applied to gameplay and showcased in UI
/// - Persistent progression stored on the signed-in account (account document operative_progression)
/// </summary>
public partial class CharacterProgressionManager : Node
{
	public static CharacterProgressionManager Instance { get; private set; } = null!;

	public const int MaxLevel = 10;
	public const int MaxUpgradeRank = 5;
	/// <summary>Account document key of the shared operative progression.</summary>
	public const string StorageKey = "operative_progression";
	private bool _storageLoaded;

	/// <summary>Describes an operative augmentation and its per-rank bonuses.</summary>
	public class UpgradeTrackDef
	{
		public string Name { get; set; } = "";
		public string Icon { get; set; } = "⚡";
		public string Tagline { get; set; } = "";
		public string[] RankDescriptions { get; set; } = Array.Empty<string>();
		public float StatBonusPerRank { get; set; } = 0.10f; // e.g. +10% per rank
		public string StatLabel { get; set; } = "DMG";
	}

	/// <summary>Serializable level, currency, and upgrade ranks for one operative.</summary>
	public class HeroProgressionData
	{
		public int Level { get; set; } = 1;
		public int CurrentSparks { get; set; } = 0;
		public int SkillPoints { get; set; } = 0;
		public int[] UpgradeRanks { get; set; } = new int[3]; // Ranks 0..5 for 3 tracks
	}

	/// <summary>Database document containing the shared bank and operative progression.</summary>
	public class SaveDataModel
	{
		public int GlobalSparksBank { get; set; } = 250; // Starter reserve
		public Dictionary<string, HeroProgressionData> Operatives { get; set; } = new();
	}

	private SaveDataModel _data = new();

	// Hero-specific augmentation definitions
	private static readonly Dictionary<string, UpgradeTrackDef[]> HeroUpgradeDefs = new()
	{
		["Cyber Shogun"] = new UpgradeTrackDef[]
		{
			new()
			{
				Name = "Plasma Nanite Edge",
				Icon = "⚔️",
				Tagline = "Amplifies katana cutting energy and critical slice damage.",
				StatLabel = "+12% Slash Damage",
				StatBonusPerRank = 0.12f,
				RankDescriptions = new[]
				{
					"Base edge: standard nanite coating.",
					"Rank 1: +12% Katana & Dash Slash DMG.",
					"Rank 2: +24% Slash DMG; adds cyan burn trail.",
					"Rank 3: +36% Slash DMG; critical strikes pierce shields.",
					"Rank 4: +48% Slash DMG; attack speed +15%.",
					"Rank 5: +60% Slash DMG; finisher creates plasma shockwave."
				}
			},
			new()
			{
				Name = "Kinetic Battery",
				Icon = "🛡️",
				Tagline = "Reinforces Kinetic Parry absorption and counter-pulse explosion.",
				StatLabel = "+15% Counter Force",
				StatBonusPerRank = 0.15f,
				RankDescriptions = new[]
				{
					"Standard kinetic absorption mesh.",
					"Rank 1: +15% Parry absorption window & shockwave radius.",
					"Rank 2: +30% Counter force; reflects light ranged projectiles.",
					"Rank 3: +45% Counter force; restores 10 shield HP on successful parry.",
					"Rank 4: +60% Counter force; parry cooldown reduced by 20%.",
					"Rank 5: +75% Counter force; triggers radial EMP burst stunning foes."
				}
			},
			new()
			{
				Name = "Shadow Overdrive Dash",
				Icon = "💨",
				Tagline = "Boosts Cyber Dash velocity and drops electrifying trail sparks.",
				StatLabel = "-8% Dash Cooldown",
				StatBonusPerRank = 0.08f,
				RankDescriptions = new[]
				{
					"Standard single-phase dash thruster.",
					"Rank 1: Dash cooldown -8%; dash distance +10%.",
					"Rank 2: Dash cooldown -16%; drops electric spark trail.",
					"Rank 3: Dash cooldown -24%; gains brief invulnerability frames.",
					"Rank 4: Dash cooldown -32%; trail electrifies enemies for 3s.",
					"Rank 5: Dash cooldown -40%; grants secondary dash charge."
				}
			}
		},

		["Oceanus"] = new UpgradeTrackDef[]
		{
			new()
			{
				Name = "Hydro-Kinetic Surge",
				Icon = "🌊",
				Tagline = "Intensifies Tidal Torrent high-pressure water cannons.",
				StatLabel = "+14% Torrent Damage",
				StatBonusPerRank = 0.14f,
				RankDescriptions = new[]
				{
					"Standard hydro-plasma nozzle.",
					"Rank 1: +14% Tidal Torrent damage; knockback +15%.",
					"Rank 2: +28% Torrent damage; water stream pierces heavy armor.",
					"Rank 3: +42% Torrent damage; creates slick puddle speeding allies.",
					"Rank 4: +56% Torrent damage; knockback distance doubled.",
					"Rank 5: +70% Torrent damage; floods stage with crashing tsunami."
				}
			},
			new()
			{
				Name = "Maelstrom Vortex",
				Icon = "🌀",
				Tagline = "Deepens the gravitational pull of hydro vortex anomalies.",
				StatLabel = "+15% Vortex Pull",
				StatBonusPerRank = 0.15f,
				RankDescriptions = new[]
				{
					"Basic miniature water cyclone.",
					"Rank 1: +15% Vortex pull strength; duration +1.0s.",
					"Rank 2: +30% Vortex pull; pulls flying obstacles inward.",
					"Rank 3: +45% Vortex pull; swirling debris deals continuous damage.",
					"Rank 4: +60% Vortex pull; vortex cooldown -20%.",
					"Rank 5: +75% Vortex pull; vortex implodes dealing massive finishing burst."
				}
			},
			new()
			{
				Name = "Liquid Agility",
				Icon = "💧",
				Tagline = "Alters bodily density to flow effortlessly across hazards.",
				StatLabel = "+5% Movement Speed",
				StatBonusPerRank = 0.05f,
				RankDescriptions = new[]
				{
					"Standard surface tension resistance.",
					"Rank 1: Base movement speed +5%; jump height +8%.",
					"Rank 2: Base speed +10%; immune to slowing hazard traps.",
					"Rank 3: Base speed +15%; Aqua Jet dash leaves speed-boost trail.",
					"Rank 4: Base speed +20%; water mantle reduces incoming damage by 12%.",
					"Rank 5: Base speed +25%; liquefies upon taking fatal hit, reviving with 25% HP."
				}
			}
		},

		["Surya Prime"] = new UpgradeTrackDef[]
		{
			new()
			{
				Name = "Solar Aegis Bulwark",
				Icon = "🛡️",
				Tagline = "Reinforces solar bulwark plating and reflects blocked energy.",
				StatLabel = "+15% Shield HP",
				StatBonusPerRank = 0.15f,
				RankDescriptions = new[]
				{
					"Standard energy buckler.",
					"Rank 1: +15% Shield capacity; absorbs 20% more kinetic impact.",
					"Rank 2: +30% Shield capacity; reflects 25% of blocked projectiles.",
					"Rank 3: +45% Shield capacity; shield bash deals +30% damage.",
					"Rank 4: +60% Shield capacity; shield regenerates 50% faster.",
					"Rank 5: +75% Shield capacity; perfect block detonates blinding flash."
				}
			},
			new()
			{
				Name = "Tectonic Shockwave",
				Icon = "🌋",
				Tagline = "Increases ground slam seismic magnitude and crater fragmentation.",
				StatLabel = "+16% Shockwave Radius",
				StatBonusPerRank = 0.16f,
				RankDescriptions = new[]
				{
					"Basic seismic impact damper.",
					"Rank 1: +16% Seismic crater radius; knockup height +20%.",
					"Rank 2: +32% Crater radius; fractures terrain creating lava fissures.",
					"Rank 3: +48% Crater radius; shreds enemy armor by 30% for 5s.",
					"Rank 4: +64% Crater radius; cooldown reduced by 25%.",
					"Rank 5: +80% Crater radius; triggers secondary aftershock 1.5s later."
				}
			},
			new()
			{
				Name = "Cosmic Vanguard Plating",
				Icon = "🥋",
				Tagline = "Heavy gold alloy nano-armor resisting kinetic and thermal impacts.",
				StatLabel = "+7% Damage Resistance",
				StatBonusPerRank = 0.07f,
				RankDescriptions = new[]
				{
					"Factory standard exo-suit.",
					"Rank 1: Passive damage reduction +7%; knockback resistance +15%.",
					"Rank 2: Passive damage reduction +14%; max health +15%.",
					"Rank 3: Passive damage reduction +21%; immune to stun effects.",
					"Rank 4: Passive damage reduction +28%; health regenerates when stationary.",
					"Rank 5: Passive damage reduction +35%; absorbs lethal hit once per deploy."
				}
			}
		},

		["Mirage Blade"] = new UpgradeTrackDef[]
		{
			new()
			{
				Name = "Phase Shift Matrix",
				Icon = "🌌",
				Tagline = "Refines quantum phase teleportation speed and distance.",
				StatLabel = "-10% Blink Cooldown",
				StatBonusPerRank = 0.10f,
				RankDescriptions = new[]
				{
					"Short-range unstable blink module.",
					"Rank 1: Phase Blink cooldown -10%; blink distance +15%.",
					"Rank 2: Phase Blink cooldown -20%; leaves decoy at departure point.",
					"Rank 3: Phase Blink cooldown -30%; blinking passes through solid walls.",
					"Rank 4: Phase Blink cooldown -40%; grants 2 seconds stealth after blink.",
					"Rank 5: Phase Blink cooldown -50%; store up to 3 rapid blink charges."
				}
			},
			new()
			{
				Name = "Crystalline Decoy Splinter",
				Icon = "👥",
				Tagline = "Infuses holographic decoys with volatile explosive crystal resonance.",
				StatLabel = "+20% Decoy Shard DMG",
				StatBonusPerRank = 0.20f,
				RankDescriptions = new[]
				{
					"Standard passive holographic projection.",
					"Rank 1: Decoys explode on expiration dealing +20% shard DMG.",
					"Rank 2: Decoys taunt and redirect enemy aim for 3.0s.",
					"Rank 3: Exploding decoy shards inflict -30% enemy speed debuff.",
					"Rank 4: Summons 2 mirrored decoys simultaneously.",
					"Rank 5: Decoys mimic primary attack, dealing 50% real damage."
				}
			},
			new()
			{
				Name = "Phantom Ambush Edge",
				Icon = "🗡️",
				Tagline = "Deadly assassination strikes when attacking from stealth or behind.",
				StatLabel = "+25% Ambush Critical DMG",
				StatBonusPerRank = 0.25f,
				RankDescriptions = new[]
				{
					"Standard twin energy daggers.",
					"Rank 1: +25% Critical damage within 2s of exiting stealth or blink.",
					"Rank 2: +50% Ambush critical damage; melee strike speed +20%.",
					"Rank 3: +75% Ambush critical damage; resets Phase Blink on kill.",
					"Rank 4: +100% Ambush critical damage; attacks apply bleed damage.",
					"Rank 5: +125% Ambush critical damage; guaranteed 100% critical strike from stealth."
				}
			}
		},

		["Apex Titan"] = new UpgradeTrackDef[]
		{
			new()
			{
				Name = "Electromagnetic Coil",
				Icon = "⚡",
				Tagline = "Amplifies ground slam voltage, arcing chain lightning to nearby targets.",
				StatLabel = "+14% Electric Slam DMG",
				StatBonusPerRank = 0.14f,
				RankDescriptions = new[]
				{
					"Standard hydraulic ground driver.",
					"Rank 1: +14% Slam damage; arcs electricity to 2 nearby foes.",
					"Rank 2: +28% Slam damage; lightning chains up to 4 targets.",
					"Rank 3: +42% Slam damage; electromagnetically disables mechanical traps.",
					"Rank 4: +56% Slam damage; electrified enemies are magnetized together.",
					"Rank 5: +70% Slam damage; slam triggers devastating 360-degree EMP."
				}
			},
			new()
			{
				Name = "Magnetic Harpoon Tether",
				Icon = "🧲",
				Tagline = "Increases magnetic tether tractor-beam strength and grab range.",
				StatLabel = "+20% Tether Reach",
				StatBonusPerRank = 0.20f,
				RankDescriptions = new[]
				{
					"Short-range electromagnetic winch.",
					"Rank 1: Tether reach +20%; pull velocity +25%.",
					"Rank 2: Tether can pull heavy concrete debris and obstacles.",
					"Rank 3: Tether yanks aerial flying enemies directly into melee range.",
					"Rank 4: Tether cooldown -30%; can tether to terrain to sling forward.",
					"Rank 5: Tether grabs up to 3 objects simultaneously, smashing them together."
				}
			},
			new()
			{
				Name = "Titan Overdrive Core",
				Icon = "🔋",
				Tagline = "Emergency survival overdrive when structural health falls below 30%.",
				StatLabel = "+10% Overdrive Power",
				StatBonusPerRank = 0.10f,
				RankDescriptions = new[]
				{
					"Standard low-battery warning sensor.",
					"Rank 1: When HP < 30%, gain 20% kinetic shield and +10% speed for 6s.",
					"Rank 2: Overdrive shield +40%; melee damage +25% during overdrive.",
					"Rank 3: Overdrive cooldown reduced to 30s; immune to crowd control in overdrive.",
					"Rank 4: Overdrive shield +60%; releases periodic kinetic shockwaves.",
					"Rank 5: Titan form: grows 25% larger, unlimited sprint, and double damage during overdrive."
				}
			}
		},

		["Aura Phoenicia"] = new UpgradeTrackDef[]
		{
			new()
			{
				Name = "Acoustic Harmonic Core",
				Icon = "🔊",
				Tagline = "Supercharges ultrasonic acoustic resonance and pulse stagger radius.",
				StatLabel = "+12% Sonic Pulse DMG",
				StatBonusPerRank = 0.12f,
				RankDescriptions = new[]
				{
					"Standard ultrasonic emitter.",
					"Rank 1: +12% Harmonic Pulse damage; shockwave radius +15%.",
					"Rank 2: +24% Harmonic Pulse damage; sonic waves deflect physical projectiles.",
					"Rank 3: +36% Harmonic Pulse damage; stuns lesser adversaries for 1.5s.",
					"Rank 4: +48% Harmonic Pulse damage; pulse cooldown reduced by 20%.",
					"Rank 5: +60% Harmonic Pulse damage; emits destructive twin reverberation rings."
				}
			},
			new()
			{
				Name = "Prism Reverberation Field",
				Icon = "🔮",
				Tagline = "Deepens crystal harmonic lattice to amplify area reverberation.",
				StatLabel = "+15% Prismatic Field Duration",
				StatBonusPerRank = 0.15f,
				RankDescriptions = new[]
				{
					"Basic acoustic harmonic field.",
					"Rank 1: +15% Prism field duration; periodic damage tick rate +15%.",
					"Rank 2: +30% Prism duration; enemies trapped within suffer -25% move speed.",
					"Rank 3: +45% Prism duration; converts absorbed projectile energy into team shields.",
					"Rank 4: +60% Prism duration; field radius expanded by 35%.",
					"Rank 5: +75% Prism duration; field shatter triggers blinding prismatic supernova."
				}
			},
			new()
			{
				Name = "Tyrian Glide Wings",
				Icon = "🕊️",
				Tagline = "Deploys shimmering harmonic light-wings for extended aerial glide.",
				StatLabel = "+6% Movement Speed",
				StatBonusPerRank = 0.06f,
				RankDescriptions = new[]
				{
					"Standard Phoenician combat boots.",
					"Rank 1: Base speed +6%; Wave Glide dash distance +15%.",
					"Rank 2: Base speed +12%; double jump unlocks aerodynamic sonic glide.",
					"Rank 3: Base speed +18%; Cedars' Echo ground slam radius +25%.",
					"Rank 4: Base speed +24%; gliding leaves sonic wake speeding up allies.",
					"Rank 5: Base speed +30%; gain instant triple-jump and unlimited air glide."
				}
			}
		}
	};

	/// <summary>Registers the singleton and restores progression from the signed-in account.</summary>
	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
		LoadProgression();
		// Progression is an account document (2026-11-18): reload when the account signs in or out.
		Callable.From(() =>
		{
			if (SessionManager.Instance != null && IsInstanceValid(this))
				SessionManager.Instance.AccountLoaded += _ => LoadProgression();
		}).CallDeferred();
	}

	/// <summary>
	/// Returns the required sparks to level up from currentLevel to currentLevel + 1.
	/// If at or above MaxLevel, returns 0.
	/// </summary>
	public static int GetRequiredSparksForLevel(int currentLevel)
	{
		if (currentLevel >= MaxLevel) return 0;
		// Progression curve: 100, 260, 440, 640, 860, 1100, 1360, 1640, 1940
		return 100 + (currentLevel - 1) * 140 + (currentLevel - 1) * (currentLevel - 1) * 10;
	}

	/// <summary>
	/// Returns the upgrade track definitions for the specified operative.
	/// </summary>
	public static UpgradeTrackDef[] GetUpgradeDefs(string operativeName)
	{
		if (operativeName == "Phoenicia") operativeName = "Aura Phoenicia";
		if (HeroUpgradeDefs.TryGetValue(operativeName, out var defs))
		{
			return defs;
		}
		// Fallback generic definition if not found
		return HeroUpgradeDefs["Cyber Shogun"];
	}

	/// <summary>
	/// Retrieves the progression data for an operative. Automatically creates default if missing.
	/// </summary>
	public HeroProgressionData GetOperativeData(string operativeName)
	{
		if (operativeName == "Phoenicia") operativeName = "Aura Phoenicia";
		if (!_data.Operatives.TryGetValue(operativeName, out var heroData))
		{
			heroData = new HeroProgressionData
			{
				Level = 1,
				CurrentSparks = 0,
				SkillPoints = 0,
				UpgradeRanks = new int[3]
			};
			_data.Operatives[operativeName] = heroData;
		}
		return heroData;
	}

	public int GlobalSparksBank
	{
		get => _data.GlobalSparksBank;
		set
		{
			_data.GlobalSparksBank = Math.Max(0, value);
			SaveProgression();
		}
	}

	/// <summary>
	/// Adds sparks to an operative's level-up meter.
	/// </summary>
	public void AddSparks(string operativeName, int amount)
	{
		if (amount <= 0) return;
		var hero = GetOperativeData(operativeName);
		hero.CurrentSparks += amount;
		_data.GlobalSparksBank += amount;
		SaveProgression();
	}

	/// <summary>
	/// Returns true if the operative has enough sparks to level up.
	/// </summary>
	public bool CanLevelUp(string operativeName)
	{
		var hero = GetOperativeData(operativeName);
		if (hero.Level >= MaxLevel) return false;
		int needed = GetRequiredSparksForLevel(hero.Level);
		return hero.CurrentSparks >= needed;
	}

	/// <summary>
	/// Levels up the operative, deducting the required sparks and awarding 1 Skill Point.
	/// </summary>
	public bool LevelUp(string operativeName)
	{
		if (!CanLevelUp(operativeName)) return false;

		var hero = GetOperativeData(operativeName);
		int needed = GetRequiredSparksForLevel(hero.Level);
		hero.CurrentSparks -= needed;
		hero.Level += 1;
		hero.SkillPoints += 1;
		SaveProgression();
		return true;
	}

	/// <summary>
	/// Purchases an upgrade tier for the given track index (0, 1, or 2) using 1 Skill Point.
	/// </summary>
	public bool PurchaseUpgrade(string operativeName, int trackIndex)
	{
		if (trackIndex < 0 || trackIndex >= 3) return false;

		var hero = GetOperativeData(operativeName);
		if (hero.SkillPoints <= 0) return false;
		if (hero.UpgradeRanks[trackIndex] >= MaxUpgradeRank) return false;

		hero.SkillPoints -= 1;
		hero.UpgradeRanks[trackIndex] += 1;
		SaveProgression();
		return true;
	}

	/// <summary>
	/// Resets all purchased upgrades for an operative and refunds all spent skill points.
	/// </summary>
	public void RespecUpgrades(string operativeName)
	{
		var hero = GetOperativeData(operativeName);
		int refunded = 0;
		for (int i = 0; i < 3; i++)
		{
			refunded += hero.UpgradeRanks[i];
			hero.UpgradeRanks[i] = 0;
		}
		hero.SkillPoints += refunded;
		SaveProgression();
	}

	/// <summary>
	/// Computes the effective gameplay modifiers for a character based on level and upgrade ranks.
	/// </summary>
	public (float healthBonusPct, float speedBonusPct, float damageBonusPct, float cooldownReductionPct) GetEffectiveModifiers(string operativeName)
	{
		var hero = GetOperativeData(operativeName);

		// Baseline level scaling: +3% health, +1.5% speed, +2.5% damage per level above 1
		int bonusLevels = Math.Max(0, hero.Level - 1);
		float healthPct = bonusLevels * 0.03f;
		float speedPct = bonusLevels * 0.015f;
		float dmgPct = bonusLevels * 0.025f;
		float cdrPct = 0f;

		var defs = GetUpgradeDefs(operativeName);
		for (int i = 0; i < 3 && i < defs.Length; i++)
		{
			int rank = hero.UpgradeRanks[i];
			if (rank <= 0) continue;

			float trackBonus = rank * defs[i].StatBonusPerRank;
			if (defs[i].StatLabel.Contains("DMG") || defs[i].StatLabel.Contains("Damage"))
			{
				dmgPct += trackBonus;
			}
			else if (defs[i].StatLabel.Contains("Cooldown"))
			{
				cdrPct += trackBonus;
			}
			else if (defs[i].StatLabel.Contains("Speed"))
			{
				speedPct += trackBonus;
			}
			else if (defs[i].StatLabel.Contains("HP") || defs[i].StatLabel.Contains("Shield") || defs[i].StatLabel.Contains("Resistance"))
			{
				healthPct += trackBonus;
			}
			else
			{
				dmgPct += trackBonus * 0.5f;
			}
		}

		return (healthPct, speedPct, dmgPct, Math.Min(0.50f, cdrPct));
	}

	/// <summary>
	/// Saves progression to the signed-in account, only after it loaded from that account, so
	/// defaults shown before sign-in can never overwrite a real save.
	/// </summary>
	public void SaveProgression()
	{
		// Standalone test nodes model runtime state without creating persistent player data.
		if (!IsInsideTree() || !_storageLoaded) return;
		if (SessionManager.Instance?.WriteStateJson(StorageKey, JsonSerializer.Serialize(_data)) != true)
			GD.PrintErr("Progression was not saved: no account is signed in.");
	}

	/// <summary>
	/// Loads progression from the signed-in account's documents. Before sign-in (or after sign-out)
	/// it shows defaults with saving disabled.
	/// </summary>
	public void LoadProgression()
	{
		if (!IsInsideTree()) return;
		if (SessionManager.Instance?.AccountReady != true)
		{
			_data = new SaveDataModel();
			_storageLoaded = false;
			return;
		}
		try
		{
			string? json = AccountDocs.Get(StorageKey);
			_data = json == null ? new SaveDataModel() : JsonSerializer.Deserialize<SaveDataModel>(json)
				?? throw new InvalidOperationException("Invalid progression document.");
			_storageLoaded = true;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			_storageLoaded = false;
			GD.PrintErr($"Account progression could not be read ({ex.GetType().Name}).");
		}
	}
}
