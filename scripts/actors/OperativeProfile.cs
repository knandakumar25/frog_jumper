using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Defines tactical statistics, passives, mobility profiles, and neon visual styling
/// for each playable operative in Axiom Force.
/// </summary>
public partial class OperativeProfile : RefCounted
{
	public string Id { get; set; } = "cyber_shogun";
	public string DisplayName { get; set; } = "Cyber Shogun";
	public string CivilianName { get; set; } = "Kaito Kurogane";
	public string ClassTag { get; set; } = "VANGUARD // STRIKER";
	public string PrimaryAbilityName { get; set; } = "Shogun's Flash";
	public string SecondaryAbilityName { get; set; } = "Kinetic Parry";
	public string DashAbilityName { get; set; } = "Blade Dash";

	// Core Gameplay Attributes & Passives
	public float MaxHealth { get; set; } = 100.0f;
	public float MaxShield { get; set; } = 50.0f;
	public float MoveSpeed { get; set; } = 400.0f;
	public float JumpVelocity { get; set; } = -1000.0f;
	public float DashCooldown { get; set; } = 1.0f;
	public float DashSpeed { get; set; } = 800.0f;
	public float MeleePrimaryDamage { get; set; } = 35.0f;
	public float MeleeSecondaryDamage { get; set; } = 25.0f;
	public float ShieldRegenRate { get; set; } = 15.0f; // HP/sec after delay
	public float DamageReduction { get; set; } = 0.0f; // Armor mitigation %

	// Visual Styling & Aura
	public Color ThemeColor { get; set; } = new Color(0f, 0.94f, 1f, 1f); // Cyan
	public Color AuraModulate { get; set; } = new Color(1f, 1f, 1f, 1f);

	public static OperativeProfile GetProfile(string? name) => OperativeRegistry.GetProfile(name);
}

/// <summary>
/// Central registry providing operative profiles and query lookups for in-game deployment.
/// </summary>
public static class OperativeRegistry
{
	private static readonly Dictionary<string, OperativeProfile> _profiles = new(StringComparer.OrdinalIgnoreCase)
	{
		["Cyber Shogun"] = new OperativeProfile
		{
			Id = "cyber_shogun",
			DisplayName = "Cyber Shogun",
			CivilianName = "Kaito Kurogane",
			ClassTag = "VANGUARD // STRIKER",
			PrimaryAbilityName = "Shogun's Flash",
			SecondaryAbilityName = "Kinetic Parry",
			DashAbilityName = "Blade Dash",
			MaxHealth = 100.0f,
			MaxShield = 50.0f,
			MoveSpeed = 400.0f,
			JumpVelocity = -1000.0f,
			DashCooldown = 1.0f,
			DashSpeed = 800.0f,
			MeleePrimaryDamage = 35.0f,
			MeleeSecondaryDamage = 25.0f,
			ShieldRegenRate = 15.0f,
			DamageReduction = 0.0f,
			ThemeColor = new Color(0f, 0.94f, 1f, 1f), // Radiant Cyan
			AuraModulate = new Color(1.0f, 1.0f, 1.0f, 1f)
		},
		["Surya Prime"] = new OperativeProfile
		{
			Id = "surya_prime",
			DisplayName = "Surya Prime",
			CivilianName = "Arjun Varma",
			ClassTag = "SOLAR BULWARK // TANK",
			PrimaryAbilityName = "Solar Shield Bash",
			SecondaryAbilityName = "Aegis of the Sun",
			DashAbilityName = "Vanguard Dash",
			MaxHealth = 140.0f, // Reinforced hull
			MaxShield = 80.0f,  // Solar Bulwark
			MoveSpeed = 360.0f, // Heavy armored mobility
			JumpVelocity = -960.0f,
			DashCooldown = 1.4f,
			DashSpeed = 700.0f,
			MeleePrimaryDamage = 45.0f, // Crushing solar impact
			MeleeSecondaryDamage = 30.0f,
			ShieldRegenRate = 20.0f,
			DamageReduction = 0.15f, // 15% armor reduction
			ThemeColor = new Color(1.0f, 0.84f, 0.1f, 1f), // Solar Amber Gold
			AuraModulate = new Color(1.2f, 1.05f, 0.7f, 1f)
		},
		["Oceanus"] = new OperativeProfile
		{
			Id = "oceanus",
			DisplayName = "Oceanus",
			CivilianName = "Kiana Vaea",
			ClassTag = "HYDRO VANGUARD // TACTICIAN",
			PrimaryAbilityName = "Tidal Torrent",
			SecondaryAbilityName = "Maelstrom Vortex",
			DashAbilityName = "Aqua Jet",
			MaxHealth = 90.0f,
			MaxShield = 70.0f,  // High shield with rapid hydro regen
			MoveSpeed = 430.0f, // Fluid aquatic movement
			JumpVelocity = -1020.0f,
			DashCooldown = 0.8f, // Rapid slide cooldown
			DashSpeed = 860.0f,
			MeleePrimaryDamage = 30.0f,
			MeleeSecondaryDamage = 22.0f,
			ShieldRegenRate = 25.0f, // Fast hydro shield recharge
			DamageReduction = 0.05f,
			ThemeColor = new Color(0.0f, 0.92f, 0.85f, 1f), // Deep Oceanic Teal
			AuraModulate = new Color(0.8f, 1.15f, 1.1f, 1f)
		},
		["Mirage Blade"] = new OperativeProfile
		{
			Id = "mirage_blade",
			DisplayName = "Mirage Blade",
			CivilianName = "Zayd Al-Mansoor",
			ClassTag = "PHANTOM INFILTRATOR // ASSASSIN",
			PrimaryAbilityName = "Phase Blink",
			SecondaryAbilityName = "Holographic Decoy",
			DashAbilityName = "Phase Dash",
			MaxHealth = 80.0f,  // Agile glass-cannon
			MaxShield = 40.0f,
			MoveSpeed = 460.0f, // Fastest ground speed
			JumpVelocity = -1060.0f,
			DashCooldown = 0.6f, // Rapid phase blink
			DashSpeed = 1000.0f, // Extreme phase velocity
			MeleePrimaryDamage = 44.0f, // High-crit phase edge
			MeleeSecondaryDamage = 28.0f,
			ShieldRegenRate = 12.0f,
			DamageReduction = 0.0f,
			ThemeColor = new Color(0.1f, 1.0f, 0.5f, 1f), // Emerald Phantom
			AuraModulate = new Color(0.75f, 1.25f, 0.85f, 1f)
		},
		["Apex Titan"] = new OperativeProfile
		{
			Id = "apex_titan",
			DisplayName = "Apex Titan",
			CivilianName = "King Tariq Nzinga",
			ClassTag = "ELECTRO-MAGNETIC // JUGGERNAUT",
			PrimaryAbilityName = "Tectonic Slam",
			SecondaryAbilityName = "Magnetic Tether",
			DashAbilityName = "Titan Surge",
			MaxHealth = 160.0f, // Massive health pool
			MaxShield = 60.0f,
			MoveSpeed = 340.0f, // Heavy frame
			JumpVelocity = -940.0f,
			DashCooldown = 1.6f,
			DashSpeed = 750.0f,
			MeleePrimaryDamage = 52.0f, // Devastating seismic damage
			MeleeSecondaryDamage = 35.0f,
			ShieldRegenRate = 16.0f,
			DamageReduction = 0.20f, // 20% kinetic resistance
			ThemeColor = new Color(0.7f, 0.2f, 1.0f, 1f), // Electric Violet
			AuraModulate = new Color(1.15f, 0.85f, 1.3f, 1f)
		},
		["Aura Phoenicia"] = new OperativeProfile
		{
			Id = "phoenicia",
			DisplayName = "Aura Phoenicia",
			CivilianName = "Maya Khoury",
			ClassTag = "PRISMATIC RESONANCE // SOUND WEAVER",
			PrimaryAbilityName = "Harmonic Pulse",
			SecondaryAbilityName = "Prism Reverberation",
			DashAbilityName = "Wave Glide",
			MaxHealth = 95.0f,
			MaxShield = 55.0f,
			MoveSpeed = 420.0f,
			JumpVelocity = -1010.0f,
			DashCooldown = 0.85f,
			DashSpeed = 830.0f,
			MeleePrimaryDamage = 34.0f, // Ultrasonic resonance
			MeleeSecondaryDamage = 24.0f,
			ShieldRegenRate = 18.0f,
			DamageReduction = 0.08f,
			ThemeColor = new Color(1.0f, 0.2f, 0.65f, 1f), // Prismatic Magenta
			AuraModulate = new Color(1.2f, 0.85f, 1.15f, 1f)
		}
	};

	public static OperativeProfile GetProfile(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return _profiles["Cyber Shogun"];
		}

		string trimmed = name.Trim();
		if (_profiles.TryGetValue(trimmed, out var exact))
		{
			return exact;
		}

		// Alias / partial matching
		if (trimmed.Contains("shogun", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("kaito", StringComparison.OrdinalIgnoreCase))
			return _profiles["Cyber Shogun"];
		if (trimmed.Contains("surya", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("arjun", StringComparison.OrdinalIgnoreCase))
			return _profiles["Surya Prime"];
		if (trimmed.Contains("oceanus", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("kiana", StringComparison.OrdinalIgnoreCase))
			return _profiles["Oceanus"];
		if (trimmed.Contains("mirage", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("zayd", StringComparison.OrdinalIgnoreCase))
			return _profiles["Mirage Blade"];
		if (trimmed.Contains("titan", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("tariq", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("apex", StringComparison.OrdinalIgnoreCase))
			return _profiles["Apex Titan"];
		if (trimmed.Contains("phoenicia", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("maya", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("aura", StringComparison.OrdinalIgnoreCase))
			return _profiles["Aura Phoenicia"];

		return _profiles["Cyber Shogun"];
	}

	public static IEnumerable<OperativeProfile> AllProfiles => _profiles.Values;
}
