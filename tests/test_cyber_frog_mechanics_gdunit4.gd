class_name TestCyberFrogMechanicsGdUnit4
extends GdUnitTestSuite

## GdUnit4 Test Suite for Core Gameplay and Cyber Frog Physics Calculations


func test_jump_velocity_calculation() -> void:
	var jump_height := 4.0
	var gravity := 9.8
	var velocity := sqrt(2.0 * gravity * jump_height)

	# sqrt(2 * 9.8 * 4.0) = sqrt(78.4) ~= 8.854
	assert_float(velocity).is_between(8.85, 8.86)


func test_time_to_apex() -> void:
	var jump_velocity := 10.0
	var gravity := 20.0
	var time_to_apex := jump_velocity / gravity
	var total_air_time := time_to_apex * 2.0

	assert_float(time_to_apex).is_equal_approx(0.5, 0.001)
	assert_float(total_air_time).is_equal_approx(1.0, 0.001)


func test_horizontal_distance() -> void:
	var speed := 8.0
	var air_time := 1.5
	var distance := speed * air_time

	assert_float(distance).is_equal_approx(12.0, 0.001)


func test_health_damage_clamping() -> void:
	var max_health := 100.0
	var current_health := max_health

	# Normal damage
	current_health = max(0.0, current_health - 35.0)
	assert_float(current_health).is_equal_approx(65.0, 0.001)

	# Lethal overkill damage should clamp to 0
	current_health = max(0.0, current_health - 120.0)
	assert_float(current_health).is_equal_approx(0.0, 0.001)


func test_health_heal_clamping() -> void:
	var max_health := 100.0
	var current_health := 80.0

	# Over-heal should clamp to max_health
	current_health = min(max_health, current_health + 50.0)
	assert_float(current_health).is_equal_approx(100.0, 0.001)
