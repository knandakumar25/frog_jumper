class_name TestPhase4HazardsAndAudio
extends GdUnitTestSuite

## GdUnit4 Test Suite for Phase 4: Combat Audio, Wall Mechanics, and 2D Interactive Hazards.

const LASER_GRID_PATH := "res://scenes/gameplay/cyber_laser_grid_2d.tscn"
const HAZARD_FLOOR_PATH := "res://scenes/gameplay/cyber_hazard_floor_2d.tscn"
const SHOGUN_PATH := "res://scenes/gameplay/cyber_shogun.tscn"
const LEVEL2_PATH := "res://scenes/gameplay/level2.tscn"
const LEVEL3_PATH := "res://scenes/gameplay/level3.tscn"
const LEVEL4_PATH := "res://scenes/gameplay/level4.tscn"

func test_laser_grid_scene_structure_and_properties() -> void:
	assert_bool(FileAccess.file_exists(LASER_GRID_PATH)).is_true()
	var scene := load(LASER_GRID_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var laser: Node = scene.instantiate()
	assert_that(laser).is_not_null()

	# Verify child nodes
	assert_that(laser.get_node_or_null("BeamLine")).is_not_null()
	assert_that(laser.get_node_or_null("HazardArea")).is_not_null()
	assert_that(laser.get_node_or_null("HazardArea/CollisionShape2D")).is_not_null()
	assert_that(laser.get_node_or_null("GlowLight")).is_not_null()
	assert_that(laser.get_node_or_null("HumPlayer")).is_not_null()

	# Verify default properties
	assert_float(laser.get("BeamLength")).is_equal(240.0)
	assert_float(laser.get("Damage")).is_equal(25.0)
	assert_bool(laser.get("IsLaserActive")).is_true()

	laser.free()

func test_hazard_floor_scene_structure_and_properties() -> void:
	assert_bool(FileAccess.file_exists(HAZARD_FLOOR_PATH)).is_true()
	var scene := load(HAZARD_FLOOR_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var floor_node: Node = scene.instantiate()
	assert_that(floor_node).is_not_null()

	# Verify child nodes
	assert_that(floor_node.get_node_or_null("FloorVisual")).is_not_null()
	assert_that(floor_node.get_node_or_null("HazardArea")).is_not_null()
	assert_that(floor_node.get_node_or_null("HazardArea/CollisionShape2D")).is_not_null()
	assert_that(floor_node.get_node_or_null("Sparks")).is_not_null()

	# Verify default properties
	assert_float(floor_node.get("FloorWidth")).is_equal(200.0)
	assert_float(floor_node.get("Damage")).is_equal(20.0)
	assert_bool(floor_node.get("IsSurging")).is_true()

	floor_node.free()

func test_cyber_shogun_has_audio_and_combat_setup() -> void:
	var scene := load(SHOGUN_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var shogun: Node = scene.instantiate()
	assert_that(shogun).is_not_null()
	shogun.call("_Ready")

	# Verify audio streams are loaded and assigned to child players
	var audio_players := shogun.find_children("", "AudioStreamPlayer2D", false, false)
	assert_int(audio_players.size()).is_greater_equal(4)

	var has_slash := false
	var has_clash := false
	var has_dash := false
	var has_shield := false

	for p in audio_players:
		var asp := p as AudioStreamPlayer2D
		if asp != null and asp.stream != null:
			var path: String = asp.stream.resource_path
			if path.contains("katana_slash"):
				has_slash = true
			elif path.contains("blade_clash"):
				has_clash = true
			elif path.contains("blade_dash"):
				has_dash = true
			elif path.contains("shield_hit"):
				has_shield = true

	assert_bool(has_slash).is_true()
	assert_bool(has_clash).is_true()
	assert_bool(has_dash).is_true()
	assert_bool(has_shield).is_true()

	shogun.free()

func test_levels_contain_hazards_container() -> void:
	var levels := [LEVEL2_PATH, LEVEL3_PATH, LEVEL4_PATH]

	for path in levels:
		var scene := load(path) as PackedScene
		assert_that(scene).is_not_null()
		var level: Node = scene.instantiate()
		assert_that(level).is_not_null()

		# Missions build their hazards in code under Actors: laser grids, surge floors, or operative-gated vents/walls.
		add_child(level)
		# Gameplay structure checks finish the separately covered arrival overlay.
		if path == LEVEL2_PATH:
			level.get_node("ConcreteWildfireIntro").call("Finish")
		if path == LEVEL3_PATH:
			level.get_node("NeonRustIntro").call("Finish")
		await get_tree().process_frame
		var actors := level.get_node_or_null("Actors")
		assert_that(actors).is_not_null()
		var hazard_count := 0
		for node in actors.get_children():
			if node.get_script() != null and (
					"CyberLaserGrid2D" in str(node.get_script().resource_path)
					or "CyberHazardFloor2D" in str(node.get_script().resource_path)
					or "AbilityGate" in str(node.get_script().resource_path)):
				hazard_count += 1
		assert_int(hazard_count).is_greater_equal(2)

		level.free()
