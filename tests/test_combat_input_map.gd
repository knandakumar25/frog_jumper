extends GdUnitTestSuite


func test_player_actions_exist_without_runtime_bootstrap() -> void:
	for action in ["attack", "left", "right", "jump", "escape"]:
		assert_bool(ProjectSettings.has_setting("input/" + action)).is_true()
		assert_bool(InputMap.has_action(action)).is_true()


func test_attack_defaults_match_keyboard_and_controller() -> void:
	var config: Dictionary = ProjectSettings.get_setting("input/attack", {})
	assert_bool(config.has("events")).is_true()
	var keyboard = InputEventKey.new()
	keyboard.physical_keycode = KEY_F
	keyboard.pressed = true
	var controller = InputEventJoypadButton.new()
	controller.button_index = JOY_BUTTON_X
	controller.pressed = true
	var has_keyboard := false
	var has_controller := false
	for binding: InputEvent in config.get("events", []):
		has_keyboard = has_keyboard or binding.is_match(keyboard)
		has_controller = has_controller or binding.is_match(controller)
	assert_bool(has_keyboard).is_true()
	assert_bool(has_controller).is_true()


func test_other_keys_can_query_attack_without_input_map_errors() -> void:
	# The controller evaluates this query for every pressed key except F.
	for code in [KEY_Q, KEY_SHIFT, KEY_C, KEY_ESCAPE]:
		var key = InputEventKey.new()
		key.physical_keycode = code
		key.keycode = code
		key.pressed = true
		assert_bool(key.is_action_pressed("attack")).is_false()
