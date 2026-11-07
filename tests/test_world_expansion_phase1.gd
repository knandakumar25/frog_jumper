extends GdUnitTestSuite

# Test suite for Neo-Kyoto Living Resistance World Phase 0 & Phase 1.
# Verifies bike traversal, top-down combat actions, resident presence, and safehouse interiors.

func test_player_bike_mounting_and_restrictions() -> void:
	var player: CharacterBody2D = preload("res://scripts/world/ExplorationPlayer.cs").new()
	add_child(player)
	
	assert_bool(player.get("IsRidingBike")).is_false()
	
	# Normal ground point: bike can mount
	var toggled = player.call("ToggleBikeAt", 44000.0, 44000.0, 0)
	assert_bool(toggled).is_true()
	assert_bool(player.get("IsRidingBike")).is_true()
	
	# Dismounting
	player.call("ToggleBike")
	assert_bool(player.get("IsRidingBike")).is_false()
	
	# In interior: cannot mount bike
	player.set("InInterior", true)
	var interior_toggled = player.call("ToggleBikeAt", 44000.0, 44000.0, 0)
	assert_bool(interior_toggled).is_false()
	assert_bool(player.get("IsRidingBike")).is_false()
	
	# In water: cannot mount bike (river spine at 62000, 64000)
	player.set("InInterior", false)
	var water_toggled = player.call("ToggleBikeAt", 62000.0, 64000.0, 0)
	assert_bool(water_toggled).is_false()
	assert_bool(player.get("IsRidingBike")).is_false()
	
	player.free()

func test_player_topdown_combat_actions_and_mitigation() -> void:
	var player: CharacterBody2D = preload("res://scripts/world/ExplorationPlayer.cs").new()
	add_child(player)
	
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	assert_float(player.get("CurrentShield")).is_equal(50.0)
	
	# Attack execution
	var attacked: bool = player.call("ExecuteAttack")
	assert_bool(attacked).is_true()
	assert_bool(player.get("IsAttacking")).is_true()
	
	# Dodge execution
	var dodged: bool = player.call("ExecuteDodge")
	assert_bool(dodged).is_true()
	assert_float(player.get("DodgeCooldown")).is_greater(0.0)
	
	# Damage mitigation: 30 damage absorbed by shield (50 -> 20)
	player.call("TakeDamage", 30.0)
	assert_float(player.get("CurrentShield")).is_equal(20.0)
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	
	# Heavy damage piercing remaining shield
	player.call("TakeDamage", 40.0)
	assert_float(player.get("CurrentShield")).is_equal(0.0)
	assert_float(player.get("CurrentHealth")).is_less(100.0)
	
	# Safehouse restoration
	player.call("RestoreFullVitals")
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	assert_float(player.get("CurrentShield")).is_equal(50.0)
	
	player.free()

func test_world_scene_has_safehouse_and_resident_markers() -> void:
	var world_scene = load("res://scenes/world/neo_kyoto.tscn")
	assert_object(world_scene).is_not_null()
	
	var world = world_scene.instantiate()
	add_child(world)
	
	# Allow node lifecycle to settle
	await get_tree().process_frame
	
	var inspection = world.call("InspectionState")
	assert_object(inspection).is_not_null()
	assert_bool(inspection.has("resident")).is_true()
	assert_bool(inspection.has("alert_state")).is_true()
	assert_bool(inspection.has("riding_bike")).is_true()
	
	world.free()
