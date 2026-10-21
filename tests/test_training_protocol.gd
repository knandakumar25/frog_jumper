extends GdUnitTestSuite
## Exercises real training physics, artwork selection, hits, retries, and isolation from campaign state.

const SCENE = "res://scenes/gameplay/training_protocol.tscn"
const ROSTER = ["Cyber Shogun", "Surya Prime", "Oceanus", "Mirage Blade", "Apex Titan", "Aura Phoenicia"]
var original_character: String
var original_gamepad_mode: bool

## Preserve the user's operative selection while tests create isolated training sessions.
func before_test() -> void:
	original_character = Global.selected_character
	original_gamepad_mode = Global.is_gamepad_mode

## Release simulated input and restore selection even when a test fails.
func after_test() -> void:
	for action in ["left", "right", "jump"]:
		Input.action_release(action)
	Global.selected_character = original_character
	Global.is_gamepad_mode = original_gamepad_mode
	Global.update_prompt_text()

## Wait for deterministic physics steps instead of using wall-clock sleeps.
func ticks(count: int) -> void:
	for i in range(count):
		await get_tree().physics_frame

## Queue an actual keyboard combat event through the engine input system.
func combat_key(code: Key) -> void:
	var event = InputEventKey.new()
	event.keycode = code
	event.pressed = true
	Input.parse_input_event(event)

## Every operative receives its own hand-drawn visual and unchanged profile health.
func test_all_six_operatives_use_hand_drawn_art_without_changing_campaign_progress() -> void:
	var unlocked = Global.unlocked_levels
	for operative in ROSTER:
		Global.selected_character = operative
		var runner = scene_runner(SCENE)
		var course = runner.scene()
		await ticks(3)
		var player = course.get_node("Room/Player")
		assert_str(player.get("OperativeName")).is_equal(operative)
		assert_bool(player.get_node("Sprite2D").visible).is_false()
		assert_object(player.get_node("HandDrawnArtwork")).is_not_null()
		assert_bool(player.get("ExternalRespawn")).is_true()
		assert_int(course.get("Lesson")).is_equal(0)
		assert_int(Global.unlocked_levels).is_equal(unlocked)
		# Release each runner before creating the next camera/scene.
		course.queue_free()
		await ticks(2)

## Moving to the exit without the required demonstration cannot unlock a lesson.
func test_exit_cannot_bypass_objective_and_retry_preserves_checkpoint() -> void:
	var runner = scene_runner(SCENE)
	var course = runner.scene()
	var player = course.get_node("Room/Player")
	player.position = Vector2(1170, 550)
	await ticks(15)
	assert_bool(course.get("DoorOpen")).is_false()
	assert_int(course.get("Lesson")).is_equal(0)
	player.position = Vector2(960, 550)
	await ticks(15)
	assert_bool(course.get("DoorOpen")).is_true()
	player.position = Vector2(1170, 550)
	await ticks(15)
	assert_int(course.get("Lesson")).is_equal(1)
	course.call("RetryRoom")
	await ticks(4)
	assert_int(course.get("Lesson")).is_equal(1)
	assert_bool(course.get("DoorOpen")).is_false()
	assert_float(course.get_node("Room/Player").position.x).is_equal(140.0)

## Dash must travel physically, and repeated overlap during one dash must damage a target only once.
func test_dash_displaces_player_and_deduplicates_target_damage() -> void:
	var runner = scene_runner(SCENE)
	var course = runner.scene()
	var player = course.get_node("Room/Player")
	var target = StaticBody2D.new()
	target.set_script(load("res://scripts/actors/TrainingTarget.cs"))
	target.set("Drone", true)
	target.position = Vector2(215, 557)
	course.get_node("Room").add_child(target)
	await ticks(15)
	var start = player.position.x
	combat_key(KEY_SHIFT)
	await ticks(20)
	assert_float(player.position.x).is_greater(start + 100)
	assert_int(player.get("ComboCount")).is_equal(1)
	assert_float(target.get("Health")).is_greater(0)
	assert_float(target.get("Health")).is_less(60)

## Defeat is handled locally and restores health without reloading earlier lessons or recording a death.
func test_defeat_rebuilds_current_room_with_full_health() -> void:
	var runner = scene_runner(SCENE)
	var course = runner.scene()
	await ticks(3)
	var old_player = course.get_node("Room/Player")
	var old_id = old_player.get_instance_id()
	old_player.call("TakeDamage", 9999.0, Vector2.ZERO)
	await ticks(5)
	var player = course.get_node("Room/Player")
	assert_int(player.get_instance_id()).is_not_equal(old_id)
	assert_float(player.get("CurrentHealth")).is_equal(player.get("MaxHealth"))
	assert_int(course.get("Lesson")).is_equal(0)

## Escape pauses physics and places keyboard focus on Resume; closing it returns gameplay control.
func test_pause_restores_keyboard_control() -> void:
	var runner = scene_runner(SCENE)
	var course = runner.scene()
	await ticks(5)
	combat_key(KEY_ESCAPE)
	await ticks(2)
	assert_bool(course.get_node("TrainingUI/PauseOptions").visible).is_true()
	assert_str(str(get_viewport().gui_get_focus_owner().name)).is_equal("Resume")
	course.get_node("TrainingUI/PauseOptions/Options/Resume").emit_signal("pressed")
	Input.action_press("right")
	await ticks(10)
	Input.action_release("right")
	assert_bool(course.get_node("TrainingUI/PauseOptions").visible).is_false()
	assert_float(course.get_node("Room/Player").position.x).is_greater(140.0)

## Place the actor at a fixture and settle collision state before testing a real action.
func settle(player: Node2D, position: Vector2) -> void:
	player.position = position
	player.velocity = Vector2.ZERO
	await ticks(12)

## Crossing the exit after an objective should reconstruct exactly the next checkpoint.
func exit_room(course: Node, expected: int) -> void:
	var player = course.get_node("Room/Player")
	await settle(player, Vector2(1180, 300 if expected == 3 else 550))
	await ticks(5)
	assert_int(course.get("Lesson")).is_equal(expected)

## Reproduce a real wall-slide and outward jump; fixture placement isolates the wall interaction.
func wall_drill(course: Node) -> void:
	var player = course.get_node("Room/Player")
	player.position = Vector2(590, 410)
	player.velocity = Vector2(100, 80)
	Input.action_press("right")
	await ticks(12)
	Input.action_press("jump")
	await ticks(2)
	Input.action_release("jump")
	Input.action_release("right")
	assert_float(player.velocity.x).is_less(0.0)
	await ticks(15)
	await settle(player, Vector2(750, 300))

## Each profile must satisfy every lesson with real jump, wall, melee, dash and shield events.
## Positions are fixtures, not a substitute for manual traversal/visual review.
func test_all_profiles_complete_the_seven_lesson_objectives() -> void:
	for operative in ROSTER:
		Global.selected_character = operative
		var runner = scene_runner(SCENE)
		var course = runner.scene()
		await settle(course.get_node("Room/Player"), Vector2(960, 550))
		await exit_room(course, 1)
		var player = course.get_node("Room/Player")
		await settle(player, Vector2(800, 260))
		Input.action_press("jump")
		await ticks(2)
		Input.action_release("jump")
		await ticks(65)
		assert_bool(course.get("DoorOpen")).is_true()
		await exit_room(course, 2)
		await wall_drill(course)
		assert_bool(course.get("DoorOpen")).is_true()
		await exit_room(course, 3)
		player = course.get_node("Room/Player")
		await settle(player, Vector2(475, 550))
		combat_key(KEY_F)
		await ticks(25)
		combat_key(KEY_Q)
		await ticks(25)
		assert_bool(course.get("DoorOpen")).is_true()
		await exit_room(course, 4)
		player = course.get_node("Room/Player")
		await settle(player, Vector2(420, 550))
		combat_key(KEY_SHIFT)
		await ticks(100)
		await settle(player, Vector2(850, 550))
		combat_key(KEY_SHIFT)
		await ticks(22)
		assert_bool(course.get("DoorOpen")).is_true()
		await exit_room(course, 5)
		player = course.get_node("Room/Player")
		await settle(player, Vector2(500, 550))
		await ticks(135)
		assert_float(player.get("CurrentShield")).is_less(player.get("MaxShield"))
		await settle(player, Vector2(900, 550))
		await ticks(300)
		assert_bool(course.get("DoorOpen")).is_true()
		await exit_room(course, 6)
		await wall_drill(course)
		player = course.get_node("Room/Player")
		await settle(player, Vector2(820, 550))
		player.get_node("Sprite2D").flip_h = false
		combat_key(KEY_SHIFT)
		await ticks(25)
		# Use actual hurtbox positions because assessment drones patrol.
		for target in course.get_node("Room").get_children():
			if not target is StaticBody2D or target.get_script() != load("res://scripts/actors/TrainingTarget.cs"):
				continue
			if not target.get("Drone"):
				continue
			for strike in range(3):
				if not is_instance_valid(target):
					break
				await settle(player, target.position - Vector2(48, 0))
				player.get_node("Sprite2D").flip_h = false
				combat_key(KEY_F)
				await ticks(25)
		assert_bool(course.get("DoorOpen")).is_true()
		await settle(player, Vector2(1180, 550))
		await ticks(10)
		assert_bool(course.get_node("TrainingUI/PauseOptions").visible).is_true()
		assert_str(course.get_node("TrainingUI/PauseOptions/Options/Continue").text).is_equal("Begin Mission 1")
		course.queue_free()
		await ticks(2)

## Steer with genuine input until the target X is reached; allow ordinary braking afterward.
func steer_to(player: Node2D, x: float) -> void:
	var direction = "right" if player.position.x < x else "left"
	Input.action_press(direction)
	for frame in range(180):
		if (direction == "right" and player.position.x >= x) or (direction == "left" and player.position.x <= x):
			break
		await ticks(1)
	Input.action_release(direction)
	await ticks(10)
	assert_float(absf(player.position.x - x)).is_less(50.0)

## Start a jump on a new press; holding a button must not create repeated automatic jumps.
func jump_press() -> void:
	Input.action_press("jump")
	await ticks(2)
	Input.action_release("jump")

## The actual geometry must be traversable by every operative, without teleporting between platforms.
func test_all_profiles_traverse_jump_and_wall_courses_using_real_inputs() -> void:
	for operative in ROSTER:
		Global.selected_character = operative
		var runner = scene_runner(SCENE)
		var course = runner.scene()
		await settle(course.get_node("Room/Player"), Vector2(960, 550))
		await exit_room(course, 1)
		var player = course.get_node("Room/Player")
		await steer_to(player, 215)
		for landing in [335.0, 545.0, 795.0]:
			await jump_press()
			await steer_to(player, landing)
			await ticks(45)
			assert_bool(player.is_on_floor()).is_true()
		assert_bool(course.get("DoorOpen")).is_true()
		await exit_room(course, 2)
		player = course.get_node("Room/Player")
		await steer_to(player, 545)
		await jump_press()
		Input.action_press("right")
		await ticks(38)
		Input.action_release("right")
		await jump_press()
		await steer_to(player, 490)
		await ticks(45)
		assert_bool(player.is_on_floor()).is_true()
		assert_float(player.position.y).is_less(450.0)
		await jump_press()
		await steer_to(player, 650)
		await ticks(40)
		await steer_to(player, 755)
		assert_bool(course.get("DoorOpen")).is_true()
		course.queue_free()
		await ticks(2)

## Sends a controller button press/release through both gameplay and focused Godot controls.
func pad_button(button: JoyButton, pressed: bool) -> void:
	var event = InputEventJoypadButton.new()
	event.device = 0
	event.button_index = button
	event.pressed = pressed
	Input.parse_input_event(event)

## Controller pause/accept returns to gameplay, where RB must execute a physical dash.
func test_controller_pause_resume_and_dash() -> void:
	var runner = scene_runner(SCENE)
	var course = runner.scene()
	await ticks(15)
	pad_button(JOY_BUTTON_START, true)
	await ticks(2)
	pad_button(JOY_BUTTON_START, false)
	assert_bool(course.get_node("TrainingUI/PauseOptions").visible).is_true()
	pad_button(JOY_BUTTON_A, true)
	await ticks(2)
	pad_button(JOY_BUTTON_A, false)
	await ticks(2)
	assert_bool(course.get_node("TrainingUI/PauseOptions").visible).is_false()
	var player = course.get_node("Room/Player")
	await ticks(60)
	var start = player.position.x
	pad_button(JOY_BUTTON_RIGHT_SHOULDER, true)
	await ticks(20)
	pad_button(JOY_BUTTON_RIGHT_SHOULDER, false)
	assert_float(player.position.x).is_greater(start + 100)
