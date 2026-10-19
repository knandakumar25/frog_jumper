class_name TestLevel12DCyberShogun
extends GdUnitTestSuite

## GdUnit4 Test Suite for Phase 2 2D Cyber Shogun Combat and Level 1 Modernization.

const LEVEL1_PATH := "res://scenes/gameplay/level1.tscn"
const LEVEL2_PATH := "res://scenes/gameplay/level2.tscn"
const LEVEL3_PATH := "res://scenes/gameplay/level3.tscn"
const LEVEL4_PATH := "res://scenes/gameplay/level4.tscn"

func test_level1_scene_loads() -> void:
	assert_bool(FileAccess.file_exists(LEVEL1_PATH)).is_true()
	var scene := load(LEVEL1_PATH) as PackedScene
	assert_that(scene).is_not_null()
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()
	instance.free()

func test_cyber_shogun_nodes_and_attack_hitbox() -> void:
	var scene := load(LEVEL1_PATH) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	var player = instance.find_child("CyberShogun", true, false)
	assert_that(player).is_not_null()

	var attack_area = player.get_node_or_null("AttackArea")
	assert_that(attack_area).is_not_null()

	var anim_sprite = player.get_node_or_null("Sprite2D") as AnimatedSprite2D
	assert_that(anim_sprite).is_not_null()
	assert_that(anim_sprite.sprite_frames).is_not_null()
	assert_bool(anim_sprite.sprite_frames.has_animation("sword_attack")).is_true()
	assert_bool(anim_sprite.sprite_frames.has_animation("blade_dash")).is_true()
	assert_bool(anim_sprite.sprite_frames.has_animation("karate_attack")).is_true()

	instance.free()

func test_cyber_drone_enemies() -> void:
	var scene := load(LEVEL1_PATH) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	var enemies_node = instance.get_node_or_null("SceneObjects/Enemies")
	assert_that(enemies_node).is_not_null()
	assert_int(enemies_node.get_child_count()).is_greater_equal(2)

	for drone in enemies_node.get_children():
		assert_bool(drone.is_in_group("enemies")).is_true()
		assert_that(drone.get_node_or_null("CollisionShape2D")).is_not_null()
		assert_that(drone.get_node_or_null("Sprite2D")).is_not_null()
		assert_that(drone.get_node_or_null("EyeLight")).is_not_null()

	instance.free()

func test_cyber_city_visuals_and_lighting() -> void:
	var scene := load(LEVEL1_PATH) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	assert_that(instance.get_node_or_null("SceneObjects/TextureRect")).is_null()

	var cyan_light = instance.get_node_or_null("SceneObjects/CyanAccentLight")
	assert_that(cyan_light).is_not_null()
	assert_float(cyan_light.energy).is_greater(0.0)

	var magenta_light = instance.get_node_or_null("SceneObjects/MagentaAccentLight")
	assert_that(magenta_light).is_not_null()
	assert_float(magenta_light.energy).is_greater(0.0)

	instance.free()

func test_level_progression_chain_loads_cleanly() -> void:
	var l1 = load(LEVEL1_PATH) as PackedScene
	assert_that(l1).is_not_null()

	var l2 = load(LEVEL2_PATH) as PackedScene
	assert_that(l2).is_not_null()

	var l3 = load(LEVEL3_PATH) as PackedScene
	assert_that(l3).is_not_null()

	var l4 = load(LEVEL4_PATH) as PackedScene
	assert_that(l4).is_not_null()
