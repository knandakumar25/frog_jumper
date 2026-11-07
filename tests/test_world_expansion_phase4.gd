extends GdUnitTestSuite

# Test suite for Neo-Kyoto Living Resistance World Phase 4: Endgame and Final Acceptance.
# Verifies post-Mission 12 liberated state variants, reconstruction tasks, remnant incidents,
# archive simulations, operation challenge modifiers, mastery evaluations, and accessibility profiles.

func test_world_inspection_exposes_accessibility_and_endgame_state() -> void:
	var world_scene = load("res://scenes/world/neo_kyoto.tscn")
	assert_object(world_scene).is_not_null()
	
	var world = world_scene.instantiate()
	add_child(world)
	
	await get_tree().process_frame
	
	var inspection = world.call("InspectionState")
	assert_object(inspection).is_not_null()
	
	# Verify accessibility diagnostics
	assert_bool(inspection.has("accessibility_high_contrast")).is_true()
	assert_bool(inspection.has("accessibility_parry_scale")).is_true()
	assert_bool(inspection.has("accessibility_puzzle_assist")).is_true()
	assert_float(float(inspection.get("accessibility_parry_scale"))).is_greater_equal(1.0)
	
	# Verify endgame content catalog counts
	assert_bool(inspection.has("reconstruction_tasks_count")).is_true()
	assert_int(int(inspection.get("reconstruction_tasks_count"))).is_equal(16)
	
	assert_bool(inspection.has("remnant_incidents_count")).is_true()
	assert_int(int(inspection.get("remnant_incidents_count"))).is_equal(8)
	
	assert_bool(inspection.has("archive_simulations_count")).is_true()
	assert_int(int(inspection.get("archive_simulations_count"))).is_equal(6)
	
	assert_bool(inspection.has("operation_modifiers_count")).is_true()
	assert_int(int(inspection.get("operation_modifiers_count"))).is_equal(5)
	
	world.free()

func test_accessibility_profile_scaling_logic() -> void:
	var world_scene = load("res://scenes/world/neo_kyoto.tscn")
	assert_object(world_scene).is_not_null()
	
	var world = world_scene.instantiate()
	add_child(world)
	
	await get_tree().process_frame
	
	# Test parry timing scale clamping
	world.call("SetParryTimingScale", 2.0)
	var scaled_parry = world.call("CalculateParryWindow", 0.2)
	assert_float(float(scaled_parry)).is_equal_approx(0.4, 0.01)
	
	# Test screen shake scaling
	var scaled_shake = world.call("CalculateScreenShake", 15.0)
	assert_float(float(scaled_shake)).is_equal_approx(15.0, 0.01)
	
	world.free()

func test_operation_mastery_evaluator() -> void:
	var world_scene = load("res://scenes/world/neo_kyoto.tscn")
	assert_object(world_scene).is_not_null()
	
	var world = world_scene.instantiate()
	add_child(world)
	
	await get_tree().process_frame
	
	var modifiers: Array = ["op_mod_ironman", "op_mod_glass_vanguard"]
	var result = world.call("EvaluateOperationMastery", "side_op_harbor_slipway", 95.0, 120.0, modifiers, false)
	assert_object(result).is_not_null()
	
	# Rank 4 corresponds to Platinum in OperationMasteryRank enum (None=0, Bronze=1, Silver=2, Gold=3, Platinum=4)
	assert_int(int(result.get("rank"))).is_equal(4)
	assert_int(int(result.get("credits"))).is_equal(1200)
	assert_str(str(result.get("unlocked_cosmetic"))).starts_with("skin_platinum_")
	
	world.free()
