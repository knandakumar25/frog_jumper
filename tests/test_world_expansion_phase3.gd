extends GdUnitTestSuite

# Test suite for Neo-Kyoto Living Resistance World Phase 3: Natural & Coastal Expansion.
# Verifies dynamic 4-state regional weather inspection, natural safehouse vitals restoration,
# and reservoir water traversal.

func test_world_inspection_has_dynamic_weather() -> void:
	var world_scene = load("res://scenes/world/neo_kyoto.tscn")
	assert_object(world_scene).is_not_null()
	
	var world = world_scene.instantiate()
	add_child(world)
	
	await get_tree().process_frame
	
	var inspection = world.call("InspectionState")
	assert_object(inspection).is_not_null()
	assert_bool(inspection.has("weather")).is_true()
	
	var weather_str = str(inspection.get("weather"))
	var valid_weathers = ["Clear", "Rain", "Fog", "RegionalStorm"]
	assert_bool(valid_weathers.has(weather_str)).is_true()
	
	world.free()

func test_natural_safehouse_vitals_restoration() -> void:
	var player: CharacterBody2D = preload("res://scripts/world/ExplorationPlayer.cs").new()
	add_child(player)
	
	# Inflict heavy damage in wilderness
	player.call("TakeDamage", 85.0)
	assert_float(player.get("CurrentShield")).is_equal(0.0)
	assert_float(player.get("CurrentHealth")).is_less(100.0)
	
	# Rest at a natural district safehouse
	player.call("RestoreFullVitals")
	assert_float(player.get("CurrentHealth")).is_equal(100.0)
	assert_float(player.get("CurrentShield")).is_equal(50.0)
	
	player.free()

func test_reservoir_boat_traversal() -> void:
	var player: CharacterBody2D = preload("res://scripts/world/ExplorationPlayer.cs").new()
	add_child(player)
	
	# Cloudstep Reservoir center (Region 1 water geometry)
	# Coordinates: X ~ 48000, Y ~ 16000
	var reservoir_mounted = player.call("ToggleBoatAt", 48000.0, 16000.0, 0)
	assert_bool(reservoir_mounted).is_true()
	assert_bool(player.get("IsRidingBoat")).is_true()
	
	# Dismount boat
	var dismounted = player.call("ToggleBoat")
	assert_bool(dismounted).is_true()
	assert_bool(player.get("IsRidingBoat")).is_false()
	
	player.free()
