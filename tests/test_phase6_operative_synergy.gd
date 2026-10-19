class_name TestPhase6OperativeSynergy
extends GdUnitTestSuite

## GdUnit4 Test Suite for Phase 6: Multi-Operative Deployment & Character Synergy.

const SHOGUN_PATH := "res://scenes/gameplay/cyber_shogun.tscn"
const HUD_PATH := "res://scenes/gameplay/cyber_combat_hud.tscn"
const SCORECARD_PATH := "res://scenes/ui/cyber_victory_scorecard.tscn"

func test_shogun_default_operative_profile() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var shogun: CharacterBody2D = scene.instantiate()
	assert_that(shogun).is_not_null()
	shogun.call("_Ready")

	var prof: RefCounted = shogun.get("ActiveProfile")
	assert_that(prof).is_not_null()
	assert_str(prof.get("DisplayName")).is_equal("Cyber Shogun")
	assert_str(prof.get("CivilianName")).is_equal("Kaito Kurogane")
	assert_str(prof.get("ClassTag")).is_equal("VANGUARD // STRIKER")
	assert_float(shogun.get("MaxHealth")).is_equal_approx(100.0, 0.01)
	assert_float(shogun.get("MaxShield")).is_equal_approx(50.0, 0.01)
	assert_float(shogun.get("DashCooldown")).is_equal_approx(1.0, 0.01)

	shogun.free()

func test_switching_to_surya_prime_solar_tank() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	var shogun: CharacterBody2D = scene.instantiate()
	assert_that(shogun).is_not_null()
	shogun.call("_Ready")

	shogun.call("ApplyOperativeProfile", "Surya Prime")

	var prof: RefCounted = shogun.get("ActiveProfile")
	assert_that(prof).is_not_null()
	assert_str(prof.get("DisplayName")).is_equal("Surya Prime")
	assert_str(prof.get("CivilianName")).is_equal("Arjun Varma")
	assert_str(prof.get("PrimaryAbilityName")).is_equal("Solar Shield Bash")
	assert_float(shogun.get("MaxHealth")).is_equal_approx(140.0, 0.01)
	assert_float(shogun.get("CurrentHealth")).is_equal_approx(140.0, 0.01)
	assert_float(shogun.get("MaxShield")).is_equal_approx(80.0, 0.01)
	assert_float(shogun.get("CurrentShield")).is_equal_approx(80.0, 0.01)
	assert_float(shogun.get("DashCooldown")).is_equal_approx(1.4, 0.01)
	assert_float(prof.get("DamageReduction")).is_equal_approx(0.15, 0.01)

	shogun.free()

func test_switching_to_mirage_blade_phantom_assassin() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	var shogun: CharacterBody2D = scene.instantiate()
	assert_that(shogun).is_not_null()
	shogun.call("_Ready")

	shogun.call("ApplyOperativeProfile", "Mirage Blade")

	var prof: RefCounted = shogun.get("ActiveProfile")
	assert_that(prof).is_not_null()
	assert_str(prof.get("DisplayName")).is_equal("Mirage Blade")
	assert_str(prof.get("CivilianName")).is_equal("Zayd Al-Mansoor")
	assert_float(shogun.get("MaxHealth")).is_equal_approx(80.0, 0.01)
	assert_float(shogun.get("MaxShield")).is_equal_approx(40.0, 0.01)
	assert_float(shogun.get("DashCooldown")).is_equal_approx(0.6, 0.01)
	assert_float(prof.get("DashSpeed")).is_equal_approx(1000.0, 0.01)

	shogun.free()

func test_apex_titan_armor_mitigation_on_damage() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	var shogun: CharacterBody2D = scene.instantiate()
	assert_that(shogun).is_not_null()
	shogun.call("_Ready")

	# Apply Apex Titan (20% armor mitigation)
	shogun.call("ApplyOperativeProfile", "Apex Titan")
	assert_float(shogun.get("CurrentShield")).is_equal_approx(60.0, 0.01)

	# Deal 50 raw damage -> Net damage = 50 * (1 - 0.20) = 40.
	# Titan absorbs 40 with 60 shield -> remaining shield is 20!
	shogun.call("TakeDamage", 50.0, Vector2.ZERO)
	assert_float(shogun.get("CurrentShield")).is_equal_approx(20.0, 0.01)
	assert_float(shogun.get("CurrentHealth")).is_equal_approx(160.0, 0.01)

	shogun.free()

func test_combat_hud_reflects_operative_abilities_and_styling() -> void:
	var hud_scene := load(HUD_PATH) as PackedScene
	var shogun_scene := load(SHOGUN_PATH) as PackedScene
	assert_that(hud_scene).is_not_null()
	assert_that(shogun_scene).is_not_null()

	var hud: CanvasLayer = hud_scene.instantiate()
	var shogun: CharacterBody2D = shogun_scene.instantiate()
	assert_that(hud).is_not_null()
	assert_that(shogun).is_not_null()

	hud.call("_Ready")
	shogun.call("_Ready")

	# Set operative to Oceanus
	shogun.call("ApplyOperativeProfile", "Oceanus")
	hud.call("BindPlayer", shogun)

	var katana_badge: Label = hud.get_node_or_null("RootControl/PlayerStatusContainer/VBox/AbilitiesHBox/KatanaBadge")
	var dash_badge: Label = hud.get_node_or_null("RootControl/PlayerStatusContainer/VBox/AbilitiesHBox/DashBadge")
	var status_label: Label = hud.get_node_or_null("RootControl/PlayerStatusContainer/VBox/StatusLabel")

	assert_that(katana_badge).is_not_null()
	assert_that(dash_badge).is_not_null()
	assert_that(status_label).is_not_null()

	assert_bool(katana_badge.text.contains("TIDAL TORRENT")).is_true()
	assert_bool(dash_badge.text.contains("AQUA JET")).is_true()
	assert_bool(status_label.text.contains("OCEANUS")).is_true()

	shogun.free()
	hud.free()

func test_scorecard_evaluates_active_operative_title() -> void:
	var scene := load(SCORECARD_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var scorecard: CanvasLayer = scene.instantiate()
	assert_that(scorecard).is_not_null()
	scorecard.call("_Ready")

	var op_label: Label = scorecard.get_node_or_null("RootPanel/MainVBox/StatsGrid/OperativeLabel")
	assert_that(op_label).is_not_null()

	scorecard.call("ShowScorecard", 160, 5, "ARJUN VARMA // SURYA PRIME")
	assert_bool(op_label.text.contains("ARJUN VARMA // SURYA PRIME")).is_true()

	scorecard.free()
