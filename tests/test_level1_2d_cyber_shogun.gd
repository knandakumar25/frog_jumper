class_name TestLevel12DCyberShogun
extends GdUnitTestSuite

## Integration coverage for the side-scrolling Level 1 platformer and the unchanged later campaign scenes.

const LEVEL1_PATH := "res://scenes/gameplay/level1.tscn"
const LEVEL2_PATH := "res://scenes/gameplay/level2.tscn"
const LEVEL3_PATH := "res://scenes/gameplay/level3.tscn"
const LEVEL4_PATH := "res://scenes/gameplay/level4.tscn"

## Level 1 builds a platformer: operative with camera, painted backdrop, HUD, geometry and objectives.
func test_level1_loads_platformer_world() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	await get_tree().process_frame
	var player := mission.get_node("Actors/CyberShogun")
	assert_that(player).is_not_null()
	assert_that(player.get_node("Camera2D")).is_not_null()
	assert_str(player.get_class()).is_not_equal("IsometricOperative")
	assert_that(mission.get_node("BackdropLayer/PalaceGardenBackdrop").texture).is_not_null()
	assert_that(mission.get_node("CyberCombatHud")).is_not_null()
	assert_that(mission.get_node("CanvasLayer/PauseMenu")).is_not_null()
	assert_that(mission.get_node("PalaceGeometry/DojoGate")).is_not_null()
	assert_that(mission.get_node("PalaceGeometry/SkyFerry")).is_not_null()
	assert_that(mission.get_node("Actors/DemolitionBeacon")).is_not_null()
	assert_that(mission.get_node("Actors/DataBlade")).is_not_null()
	assert_that(mission.get_node("Actors/ExtractionGate")).is_not_null()
	assert_int(mission.get("FragmentTotal")).is_equal(3)
	mission.free()

## The operative falls onto the garden ground under real gravity and can leave it with a jump.
func test_player_lands_and_jumps() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	for _frame in range(30):
		await get_tree().physics_frame
	assert_bool(player.is_on_floor()).is_true()
	var ground_y: float = player.global_position.y
	Input.action_press("jump")
	for _frame in range(6):
		await get_tree().physics_frame
	Input.action_release("jump")
	assert_float(player.global_position.y).is_less(ground_y - 20.0)
	mission.free()

## Every selected operative keeps its registry profile in the platformer controller.
func test_all_six_operatives_keep_their_profiles() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	for operative in ["Cyber Shogun", "Oceanus", "Surya Prime", "Mirage Blade", "Apex Titan", "Aura Phoenicia"]:
		player.call("ApplyOperativeProfile", operative)
		assert_str(player.get("ActiveProfile").get("DisplayName")).is_equal(operative)
	mission.free()

## The beacon is shielded: damage is ignored until parried sentry fire overloads it; then the gate and Blade unlock.
func test_beacon_opens_gate_and_unlocks_data_blade() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	var beacon := mission.get_node("Actors/DemolitionBeacon")
	var blade := mission.get_node("Actors/DataBlade")
	var gate := mission.get_node("PalaceGeometry/DojoGate")
	assert_bool(gate.get("IsOpen")).is_false()
	player.global_position = blade.global_position + Vector2(0, -60)
	mission.call("CollectNearbyObjectives")
	assert_bool(mission.get("DataBladeRecovered")).is_false()
	# Shield up: strikes do nothing.
	beacon.call("TakeDamage", 1000.0)
	assert_bool(mission.get("BeaconNeutralized")).is_false()
	assert_bool(beacon.get("Shielded")).is_true()
	# Two parried shots overload the shield.
	player.global_position = beacon.global_position + Vector2(-200, -50)
	player.emit_signal("ParryDeflected", player)
	player.emit_signal("ParryDeflected", player)
	assert_bool(beacon.get("Shielded")).is_false()
	beacon.call("TakeDamage", 1000.0)
	assert_bool(mission.get("BeaconNeutralized")).is_true()
	assert_bool(gate.get("IsOpen")).is_true()
	player.global_position = blade.global_position + Vector2(0, -60)
	mission.call("CollectNearbyObjectives")
	assert_bool(mission.get("DataBladeRecovered")).is_true()
	await get_tree().physics_frame
	assert_str(mission.get("CurrentObjective")).contains("extraction")
	mission.free()

## A parry too far from the beacon does not count toward its shield.
func test_distant_parry_does_not_overload_shield() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	var beacon := mission.get_node("Actors/DemolitionBeacon")
	player.global_position = Vector2(300, 550)
	player.emit_signal("ParryDeflected", player)
	assert_bool(beacon.get("Shielded")).is_true()
	mission.free()

## The Level 1 squad is Shogun alone, and his parry lesson exists and completes once he uses the ability.
func test_level1_teaches_parry_with_a_solo_squad() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	var lesson := mission.get_node("Actors/ParryLesson")
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(catalog.UnlockedCount()).is_equal(1)
	assert_bool(lesson.get("Triggered")).is_false()
	player.global_position = lesson.global_position + Vector2(0, -50)
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_bool(lesson.get("Triggered")).is_true()
	assert_str(mission.get("PromptText")).contains("KINETIC PARRY")
	player.call("TriggerSignatureAbility")
	assert_bool(lesson.get("Completed")).is_true()
	assert_str(mission.get("PromptText")).is_equal("")
	mission.free()

## Falling into a chasm returns the operative to the active checkpoint instead of leaving them out of bounds.
func test_chasm_fall_recovers_at_checkpoint() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	var checkpoint: Vector2 = mission.get("CheckpointPosition")
	player.global_position = Vector2(1300.0, 1200.0)
	for _frame in range(3):
		await get_tree().physics_frame
	assert_float(player.global_position.y).is_less(900.0)
	assert_float(player.global_position.distance_to(checkpoint)).is_less(200.0)
	mission.free()

## Reaching a shrine stores it as the new respawn point.
func test_shrine_updates_checkpoint() -> void:
	var runner := scene_runner(LEVEL1_PATH)
	var mission := runner.scene()
	var player := mission.get_node("Actors/CyberShogun")
	var shrine := mission.get_node("Actors/ShrineGorge")
	player.global_position = shrine.global_position + Vector2(0, -50)
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_bool(shrine.get("Activated")).is_true()
	var checkpoint: Vector2 = mission.get("CheckpointPosition")
	assert_float(absf(checkpoint.x - shrine.global_position.x)).is_less(5.0)
	mission.free()

## The new mission route does not remove the existing level 2-4 scenes.
func test_level_progression_chain_still_loads_cleanly() -> void:
	for path in [LEVEL2_PATH, LEVEL3_PATH, LEVEL4_PATH]:
		var scene := load(path) as PackedScene
		assert_that(scene).is_not_null()
		var instance := scene.instantiate()
		assert_that(instance).is_not_null()
		instance.free()
