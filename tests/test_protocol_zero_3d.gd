class_name TestProtocolZero3d
extends GdUnitTestSuite

## GdUnit4 Test Suite for the Protocol Zero 3D training level scene contract.
## Covers what TrainingManager.cs binds to by NAME, since the tutorial flow is
## wired through FindChild lookups rather than serialized signal connections.
## C# classes are compared by script resource rather than by GDScript cast --
## C# class names are not registered as GDScript globals.

const SCENE_PATH := "res://scenes/gameplay/training_protocol_3d.tscn"

var _scene: PackedScene


func before_test() -> void:
	assert_bool(FileAccess.file_exists(SCENE_PATH)).is_true()
	_scene = load(SCENE_PATH) as PackedScene
	assert_that(_scene).is_not_null()


func test_training_scene_loads() -> void:
	var instance: Node = _scene.instantiate()
	assert_that(instance).is_not_null()
	instance.free()


func test_root_is_level_manager_with_training_exports() -> void:
	var instance: Node = _scene.instantiate()
	assert_that(instance.get_script()).is_equal(load("res://scripts/gameplay/Level3DManager.cs"))
	assert_int(instance.get("LevelNumber")).is_equal(0)
	assert_int(instance.get("RequiredCollectables")).is_equal(1)
	assert_that(instance.get_node("TrainingManager")).is_not_null()
	instance.free()


func test_tutorial_binds_five_targets() -> void:
	var instance: Node = _scene.instantiate()
	for index in range(1, 6):
		var target: Node = instance.get_node_or_null("TrainingManager/Target%d" % index)
		assert_that(target).is_not_null()
		assert_that(target.get_script()).is_equal(load("res://scripts/gameplay/TutorialTarget.cs"))
	assert_that(instance.get_node("TrainingManager").get_script()).is_equal(
		load("res://scripts/gameplay/TrainingManager.cs"))
	instance.free()


func test_drone_hitbox_has_killable_health() -> void:
	var instance: Node = _scene.instantiate()
	var health: Node = instance.get_node_or_null("TrainingDrone/Hitbox/Health")
	assert_that(health).is_not_null()
	assert_that(health.get_script()).is_equal(load("res://scripts/gameplay/Health.cs"))
	assert_float(health.get("MaxHealth")).is_equal(60.0)
	# WeaponArea3D and the ability shape-queries only hit layer-1 colliders, so
	# the hitbox must sit there for the melee/AoE damage path to reach it.
	var hitbox := instance.get_node("TrainingDrone/Hitbox") as PhysicsBody3D
	assert_int(hitbox.collision_layer).is_equal(1)
	assert_that(instance.get_node("TrainingDrone").get_script()).is_equal(
		load("res://scripts/gameplay/TrainingDrone.cs"))
	instance.free()


func test_fragment_pickup_detects_the_player() -> void:
	# The prototype left its pickups on default layer/mask 1 while the player is
	# on layer 2, so they never detected anything. Regression-guard the mask here.
	var instance: Node = _scene.instantiate()
	var pickup := instance.get_node("FragmentPickup") as Area3D
	assert_int(pickup.collision_mask).is_equal(2)
	assert_that(pickup.get("PointValue")).is_equal(50)
	assert_that(instance.get_node("FragmentPickup/FragmentModel")).is_not_null()
	instance.free()


func test_tutorial_targets_detect_the_player() -> void:
	var instance: Node = _scene.instantiate()
	for index in range(1, 6):
		var target := instance.get_node("TrainingManager/Target%d" % index) as Area3D
		assert_int(target.collision_mask).is_equal(2)
	instance.free()


func test_dialogue_and_hud_wire_the_named_lookups() -> void:
	# TrainingManager / Level3DManager resolve DialogueUI via
	# Root.FindChild("DialogueUI"), points and collectable HUD via %-unique names.
	var instance: Node = _scene.instantiate()
	assert_that(instance.get_node("UI/DialogueUI")).is_not_null()
	assert_that(instance.get_node("UI/DialogueUI/Panel")).is_not_null()
	assert_that(instance.get_node("UI/DialogueUI/Panel/VBox/TextLabel")).is_not_null()
	assert_that(instance.get_node("UI/DialogueUI/Panel/VBox/SpeakerLabel")).is_not_null()
	assert_that(instance.get_node("%PointsLabel")).is_not_null()
	assert_that(instance.get_node("%CollectableLabel")).is_not_null()
	assert_that(instance.get_node("UI/MissionSummaryUI")).is_not_null()
	assert_that(instance.get_node("UI/GameOverUI")).is_not_null()
	assert_that(instance.get_node("UI/PauseMenu")).is_not_null()
	assert_that(instance.get_node("InputBootstrap")).is_not_null()
	instance.free()


func test_level_select_maps_sector_01_to_the_training_level() -> void:
	var script: GDScript = load("res://scripts/main_menu/level_selection.gd")
	# Keep and free the holder: GdUnit4 counts an undisposed node as an orphan,
	# which fails the whole suite even when every assertion passes.
	var holder: Node = script.new()
	var map: Dictionary = holder.level_map
	assert_str(map[0]).is_equal(SCENE_PATH)
	holder.free()