class_name TestCyberCombatHudAndMechanics
extends GdUnitTestSuite

## GdUnit4 Test Suite for Phase 3 Cyber Combat HUD, Player Damage/Shields, and Multi-Level Mechanics.

const HUD_PATH := "res://scenes/gameplay/cyber_combat_hud.tscn"
const SHOGUN_PATH := "res://scenes/gameplay/cyber_shogun.tscn"
const LEVEL1_PATH := "res://scenes/gameplay/level1.tscn"
const LEVEL2_PATH := "res://scenes/gameplay/level2.tscn"
const LEVEL3_PATH := "res://scenes/gameplay/level3.tscn"
const LEVEL4_PATH := "res://scenes/gameplay/level4.tscn"

func test_cyber_combat_hud_scene_loads() -> void:
	assert_bool(FileAccess.file_exists(HUD_PATH)).is_true()
	var scene := load(HUD_PATH) as PackedScene
	assert_that(scene).is_not_null()
	var hud: Node = scene.instantiate()
	assert_that(hud).is_not_null()

	assert_that(hud.get_node_or_null("RootControl/PlayerStatusContainer/VBox/HealthBar")).is_not_null()
	assert_that(hud.get_node_or_null("RootControl/PlayerStatusContainer/VBox/ShieldBar")).is_not_null()
	assert_that(hud.get_node_or_null("RootControl/PlayerStatusContainer/VBox/StatusLabel")).is_not_null()
	assert_that(hud.get_node_or_null("RootControl/ScorePanel/PointsLabel")).is_not_null()
	assert_that(hud.get_node_or_null("RootControl/ComboContainer/ComboLabel")).is_not_null()

	hud.free()

func test_cyber_shogun_damage_and_shield_mechanics() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	assert_that(scene).is_not_null()
	var player: Node = scene.instantiate()
	assert_that(player).is_not_null()

	# Initial values
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	assert_float(player.get("CurrentShield")).is_equal(50.0)

	# Take 30 damage: absorbed entirely by shield (Shield: 50 -> 20, Health: 100)
	player.call("TakeDamage", 30.0, Vector2.ZERO)
	assert_float(player.get("CurrentShield")).is_equal(20.0)
	assert_float(player.get("CurrentHealth")).is_equal(100.0)

	# Clear invulnerability for unit testing next hit
	player.set("_invulnerabilityTimer", 0.0)

	# Take 30 damage: shield absorbs 20 (Shield -> 0), remaining 10 hits health (Health: 100 -> 90)
	player.call("TakeDamage", 30.0, Vector2.ZERO)
	assert_float(player.get("CurrentShield")).is_equal(0.0)
	assert_float(player.get("CurrentHealth")).is_equal(90.0)

	# Heal 10 health, 30 shield (Health: 90 -> 100, Shield: 0 -> 30)
	player.call("Heal", 10.0, 30.0)
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	assert_float(player.get("CurrentShield")).is_equal(30.0)

	player.free()

func test_cyber_shogun_combo_system() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	var player: Node = scene.instantiate()
	assert_that(player).is_not_null()

	assert_int(player.get("ComboCount")).is_equal(0)

	player.call("RegisterHit")
	assert_int(player.get("ComboCount")).is_equal(1)

	player.call("RegisterHit")
	assert_int(player.get("ComboCount")).is_equal(2)

	player.call("RegisterHit")
	assert_int(player.get("ComboCount")).is_equal(3)

	player.free()

func test_all_gameplay_levels_have_cyber_combat_hud_and_enemies() -> void:
	var level_paths := [LEVEL1_PATH, LEVEL2_PATH, LEVEL3_PATH, LEVEL4_PATH]

	for path in level_paths:
		var scene := load(path) as PackedScene
		assert_that(scene).is_not_null()
		var level: Node = scene.instantiate()
		assert_that(level).is_not_null()
		if path == LEVEL1_PATH:
			add_child(level)
			await get_tree().process_frame

		# Verify CyberCombatHud exists
		var hud = level.get_node_or_null("CyberCombatHud")
		assert_that(hud).is_not_null()

		# Level 1 builds its isometric actors dynamically; later levels use authored SceneObjects.
		var enemies: Node
		if path == LEVEL1_PATH:
			var actors = level.get_node_or_null("Actors")
			assert_that(actors).is_not_null()
			var crew_count := 0
			for actor in actors.get_children():
				if actor is CharacterBody2D and actor.is_in_group("enemies"):
					crew_count += 1
			assert_int(crew_count).is_greater_equal(2)
		else:
			enemies = level.get_node_or_null("SceneObjects/Enemies")
			assert_that(enemies).is_not_null()
			assert_int(enemies.get_child_count()).is_greater_equal(2)

		level.free()
