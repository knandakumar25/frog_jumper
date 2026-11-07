extends GdUnitTestSuite

# Test suite for Neo-Kyoto Living Resistance World Phase 2: Urban Expansion.
# Verifies Utility Boat water traversal, urban safehouse vitals restoration,
# and inspection state fields.

func test_player_boat_mounting_and_water_restrictions() -> void:
	var player: CharacterBody2D = preload("res://scripts/world/ExplorationPlayer.cs").new()
	add_child(player)
	
	assert_bool(player.get("IsRidingBoat")).is_false()
	
	# Dry land location: mounting boat must fail
	var land_toggled = player.call("ToggleBoatAt", 44000.0, 44000.0, 0)
	assert_bool(land_toggled).is_false()
	assert_bool(player.get("IsRidingBoat")).is_false()
	
	# In interior: mounting boat must fail
	player.set("InInterior", true)
	var interior_toggled = player.call("ToggleBoatAt", 126000.0, 50000.0, 0)
	assert_bool(interior_toggled).is_false()
	assert_bool(player.get("IsRidingBoat")).is_false()
	player.set("InInterior", false)
	
	# Water location (East coastal waters: X > 124000): mounting boat must succeed
	var water_toggled = player.call("ToggleBoatAt", 126000.0, 50000.0, 0)
	assert_bool(water_toggled).is_true()
	assert_bool(player.get("IsRidingBoat")).is_true()
	
	# Cannot mount bike while riding boat
	var bike_while_boat = player.call("ToggleBikeAt", 126000.0, 50000.0, 0)
	assert_bool(bike_while_boat).is_false()
	assert_bool(player.get("IsRidingBike")).is_false()
	
	# Dismounting boat returns player to foot
	var dismounted = player.call("ToggleBoat")
	assert_bool(dismounted).is_true()
	assert_bool(player.get("IsRidingBoat")).is_false()
	
	player.free()

func test_world_inspection_has_phase2_fields() -> void:
	var world_scene = load("res://scenes/world/neo_kyoto.tscn")
	assert_object(world_scene).is_not_null()
	
	var world = world_scene.instantiate()
	add_child(world)
	
	await get_tree().process_frame
	
	var inspection = world.call("InspectionState")
	assert_object(inspection).is_not_null()
	assert_bool(inspection.has("riding_boat")).is_true()
	assert_bool(inspection.has("riding_bike")).is_true()
	assert_bool(inspection.has("alert_state")).is_true()
	assert_bool(inspection.has("world_time")).is_true()
	
	world.free()

func test_urban_safehouse_vitals_restoration() -> void:
	var player: CharacterBody2D = preload("res://scripts/world/ExplorationPlayer.cs").new()
	add_child(player)
	
	# Inflict damage
	player.call("TakeDamage", 75.0)
	assert_float(player.get("CurrentShield")).is_equal(0.0)
	assert_float(player.get("CurrentHealth")).is_less(100.0)
	
	# Enter safehouse / clinic restoration
	player.call("RestoreFullVitals")
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	assert_float(player.get("CurrentShield")).is_equal(50.0)
	
	player.free()
