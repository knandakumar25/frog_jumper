extends GdUnitTestSuite
## Real-engine prompt tests. InputMap, device state and locale are restored after each case;
## no test writes the owner's saved input preferences or requires a physical controller.

const SCRATCH := "__prompt_test_action"
const AUTO := 0
const XBOX := 1
const PLAYSTATION := 2
const NINTENDO := 3
const KEYBOARD := 4

var _service: Node
var _bindings: Dictionary = {}
var _locale: String
var _style: int
var _mouse_mode: int
var _context: String
var _context_actions: Array[String] = []
var _changes := 0


func before_test() -> void:
	_service = get_node("/root/InputPromptService")
	_locale = TranslationServer.get_locale()
	_style = int(_service.get("Style"))
	_mouse_mode = Input.mouse_mode
	_context = _service.get("Context")
	_context_actions.clear()
	for action: String in _service.get("ContextActions"):
		_context_actions.append(action)
	get_node("/root/LocaleService").call("ApplyLocale", "english")
	_bindings.clear()
	for action: StringName in InputMap.get_actions():
		_bindings[action] = {
			"events": InputMap.action_get_events(action).duplicate(),
			"deadzone": InputMap.action_get_deadzone(action),
		}
	_service.call("SetStyle", AUTO, false)
	_service.call("ObserveInput", _key(KEY_A))
	InputMap.add_action(SCRATCH)
	_service.connect("PromptsChanged", _on_prompts_changed)
	_changes = 0


func after_test() -> void:
	_service.disconnect("PromptsChanged", _on_prompts_changed)
	for action: StringName in InputMap.get_actions():
		if not _bindings.has(action):
			InputMap.erase_action(action)
	for action: StringName in _bindings:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
		InputMap.action_erase_events(action)
		InputMap.action_set_deadzone(action, _bindings[action]["deadzone"])
		for event: InputEvent in _bindings[action]["events"]:
			InputMap.action_add_event(action, event)
	_service.call("SetStyle", _style, false)
	_service.call("ObserveInput", _key(KEY_A))
	get_node("/root/LocaleService").call("ApplyLocale", _locale)
	_service.call("SetPromptContext", _context, _context_actions)
	_service.call("NotifyBindingsChanged")
	Input.mouse_mode = _mouse_mode


func _on_prompts_changed() -> void:
	_changes += 1


func _key(code: Key, shifted := false) -> InputEventKey:
	var event := InputEventKey.new()
	event.physical_keycode = code
	event.shift_pressed = shifted
	event.pressed = true
	return event


func _button(index: JoyButton, device := 23) -> InputEventJoypadButton:
	var event := InputEventJoypadButton.new()
	event.button_index = index
	event.device = device
	event.pressed = true
	return event


func _axis(axis: JoyAxis, value: float) -> InputEventJoypadMotion:
	var event := InputEventJoypadMotion.new()
	event.axis = axis
	event.axis_value = value
	event.device = 23
	return event


func test_face_positions_change_labels_without_changing_binding() -> void:
	var south := _button(JOY_BUTTON_A)
	InputMap.action_add_event(SCRATCH, south)
	_service.call("ObserveInput", south)
	for example: Array in [[XBOX, "A"], [PLAYSTATION, "Cross"], [NINTENDO, "B"]]:
		_service.call("SetStyle", example[0], false)
		assert_str(_service.call("GetBinding", SCRATCH)).is_equal(example[1])
		assert_int(InputMap.action_get_events(SCRATCH)[0].button_index).is_equal(JOY_BUTTON_A)
	# An unrecognized device uses physical position, not an assumed Xbox letter.
	_service.call("SetStyle", AUTO, false)
	assert_str(_service.call("GetBinding", SCRATCH)).contains("South")


func test_keyboard_override_keeps_controller_input_active() -> void:
	InputMap.action_add_event(SCRATCH, _key(KEY_R))
	InputMap.action_add_event(SCRATCH, _button(JOY_BUTTON_A))
	_service.call("ObserveInput", _button(JOY_BUTTON_A))
	_service.call("SetStyle", KEYBOARD, false)
	assert_bool(_service.get("UsingController")).is_true()
	assert_bool(_service.get("IsGamepad")).is_false()
	assert_str(_service.call("GetBinding", SCRATCH)).is_equal("R")
	# A controller label override does not force pad labels during keyboard use.
	_service.call("SetStyle", PLAYSTATION, false)
	_service.call("ObserveInput", _key(KEY_R))
	assert_str(_service.call("GetBinding", SCRATCH)).is_equal("R")


func test_bindings_show_all_alternatives_and_preserve_modifiers() -> void:
	var physical := _key(KEY_A)
	var logical := InputEventKey.new()
	logical.keycode = KEY_A
	InputMap.action_add_event(SCRATCH, physical)
	InputMap.action_add_event(SCRATCH, logical)
	InputMap.action_add_event(SCRATCH, _key(KEY_TAB, true))
	var label: String = _service.call("GetBindingForDevice", SCRATCH, false)
	assert_str(label).contains("A")
	assert_str(label).contains("Shift")
	assert_str(label).contains("Tab")
	assert_int(label.split(" / ").count("A")).is_equal(1)


func test_unbound_is_explicit_for_missing_action_and_device() -> void:
	assert_str(_service.call("GetBindingForDevice", SCRATCH, false)).is_equal(tr("Unbound"))
	assert_str(_service.call("GetBinding", "__no_such_action")).is_equal(tr("Unbound"))
	InputMap.action_add_event(SCRATCH, _key(KEY_A))
	assert_str(_service.call("GetBindingForDevice", SCRATCH, true)).is_equal(tr("Unbound"))


func test_mouse_wheel_sticks_and_triggers_have_readable_labels() -> void:
	var wheel := InputEventMouseButton.new()
	wheel.button_index = MOUSE_BUTTON_WHEEL_UP
	InputMap.action_add_event(SCRATCH, wheel)
	assert_str(_service.call("GetBindingForDevice", SCRATCH, false)).contains("Wheel")
	InputMap.action_add_event(SCRATCH, _axis(JOY_AXIS_RIGHT_X, -1.0))
	InputMap.action_add_event(SCRATCH, _axis(JOY_AXIS_TRIGGER_LEFT, 1.0))
	_service.call("SetStyle", XBOX, false)
	var label: String = _service.call("GetBindingForDevice", SCRATCH, true)
	assert_str(label.to_lower()).contains("right stick")
	assert_str(label).contains("LT")


func test_drift_releases_key_echo_and_small_mouse_motion_do_not_switch_device() -> void:
	var actions: Array[String] = ["left", "right"]
	_service.call("SetPromptContext", "platformer", actions)
	# All mappings of this stick share the same threshold for this test.
	for action: StringName in InputMap.get_actions():
		for event: InputEvent in InputMap.action_get_events(action):
			if event is InputEventJoypadMotion and event.axis == JOY_AXIS_LEFT_X:
				InputMap.action_set_deadzone(action, 0.7)
	_service.call("ObserveInput", _axis(JOY_AXIS_LEFT_X, 0.6))
	assert_bool(_service.get("UsingController")).is_false()
	_service.call("ObserveInput", _axis(JOY_AXIS_LEFT_X, 0.9))
	assert_bool(_service.get("UsingController")).is_true()
	var echo := _key(KEY_A)
	echo.echo = true
	_service.call("ObserveInput", echo)
	var release := _key(KEY_A)
	release.pressed = false
	_service.call("ObserveInput", release)
	var motion := InputEventMouseMotion.new()
	motion.relative = Vector2(0.5, 0.5)
	_service.call("ObserveInput", motion)
	assert_bool(_service.get("UsingController")).is_true()
	motion.relative = Vector2(16.0, 2.0)
	_service.call("ObserveInput", motion)
	assert_bool(_service.get("UsingController")).is_false()


func test_trigger_uses_positive_press_threshold_and_ignores_neutral() -> void:
	_service.call("ObserveInput", _axis(JOY_AXIS_TRIGGER_RIGHT, -1.0))
	_service.call("ObserveInput", _axis(JOY_AXIS_TRIGGER_RIGHT, 0.5))
	assert_bool(_service.get("UsingController")).is_false()
	_service.call("ObserveInput", _axis(JOY_AXIS_TRIGGER_RIGHT, 0.75))
	assert_bool(_service.get("UsingController")).is_true()
	_service.call("ObserveInput", _axis(JOY_AXIS_TRIGGER_RIGHT, 0.0))
	assert_bool(_service.get("UsingController")).is_true()


func test_mouse_click_and_controller_disconnect_follow_active_device() -> void:
	_service.call("ObserveInput", _button(JOY_BUTTON_A, 23))
	assert_int(_service.get("ActiveController")).is_equal(23)
	_service.call("ControllerDisconnected", 24)
	assert_bool(_service.get("UsingController")).is_true()
	_service.call("ControllerDisconnected", 23)
	assert_bool(_service.get("UsingController")).is_false()
	_service.call("ObserveInput", _button(JOY_BUTTON_A, 24))
	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	_service.call("ObserveInput", click)
	assert_bool(_service.get("UsingController")).is_false()


func test_changes_publish_for_context_bindings_and_live_locale() -> void:
	var actions: Array[String] = ["attack", "jump"]
	_service.call("SetPromptContext", "platformer", actions)
	assert_str(_service.get("Context")).is_equal("platformer")
	var before := _changes
	_service.call("NotifyBindingsChanged")
	assert_int(_changes).is_greater(before)
	before = _changes
	get_node("/root/LocaleService").call("ApplyLocale", "arabic")
	await get_tree().process_frame
	assert_int(_changes).is_greater(before)
	var wheel := InputEventMouseButton.new()
	wheel.button_index = MOUSE_BUTTON_WHEEL_UP
	assert_str(_service.call("FormatBindingEvent", wheel)).is_equal(tr("Wheel") + " " + tr("Up"))


func test_reset_restores_runtime_actions_and_all_controller_aliases() -> void:
	for action in ["karate", "dash", "iso_interact", "world_map"]:
		if InputMap.has_action(action):
			InputMap.erase_action(action)
	InputMap.action_erase_events("attack")
	_service.call("ResetDefaults")
	for action in ["karate", "dash", "iso_interact", "world_map"]:
		assert_bool(InputMap.has_action(action)).is_true()
	var buttons: Array[int] = []
	for event: InputEvent in InputMap.action_get_events("attack"):
		if event is InputEventJoypadButton:
			buttons.append(event.button_index)
	assert_bool(buttons.has(JOY_BUTTON_X)).is_true()
	assert_bool(buttons.has(JOY_BUTTON_B)).is_true()
	assert_str(_service.call("GetBindingForDevice", "attack", false)).contains("F")


func test_keyboard_capture_json_reload_preserves_pad_bindings_and_modifiers() -> void:
	var original_pad: Array[InputEvent] = []
	for event: InputEvent in InputMap.action_get_events("jump"):
		if event is InputEventJoypadButton or event is InputEventJoypadMotion:
			original_pad.append(event)
	var replacement := _key(KEY_R, true)
	replacement.ctrl_pressed = true
	_service.call("ReplaceKeyboardBinding", "jump", replacement)
	for event: InputEvent in original_pad:
		assert_bool(InputMap.action_has_event("jump", event)).is_true()
	var saved: Dictionary = _service.call("CaptureKeyboardBindings")
	# SessionManager stores JSON, so exercise that wire format without writing owner state.
	var reloaded: Dictionary = JSON.parse_string(JSON.stringify(saved))
	_service.call("ResetDefaults")
	assert_str(_service.call("GetBindingForDevice", "jump", false)).not_contains("Ctrl+Shift+R")
	_service.call("ApplyKeyboardBindings", reloaded)
	assert_str(_service.call("GetBindingForDevice", "jump", false)).is_equal("Ctrl+Shift+R")
	for event: InputEvent in original_pad:
		assert_bool(InputMap.action_has_event("jump", event)).is_true()
	# The previous numeric-array save format remains readable.
	_service.call("ApplyKeyboardBindings", {"jump": [KEY_T]})
	assert_str(_service.call("GetBindingForDevice", "jump", false)).is_equal("T")
	for event: InputEvent in original_pad:
		assert_bool(InputMap.action_has_event("jump", event)).is_true()


func test_invalid_keyboard_override_does_not_erase_working_bindings() -> void:
	var previous: String = _service.call("GetBindingForDevice", "jump", false)
	_service.call("ApplyKeyboardBindings", {"jump": ["not a key"]})
	assert_str(_service.call("GetBindingForDevice", "jump", false)).is_equal(previous)
	_service.call("ApplyKeyboardBindings", {"jump": []})
	assert_str(_service.call("GetBindingForDevice", "jump", false)).is_equal(tr("Unbound"))


func test_invalid_style_defaults_to_automatic_without_persisting_device() -> void:
	_service.call("SetStyle", 999, false)
	assert_int(_service.get("Style")).is_equal(AUTO)
	_service.call("SetStyle", -1, false)
	assert_int(_service.get("Style")).is_equal(AUTO)
	_service.call("ObserveInput", _button(JOY_BUTTON_X))
	assert_int(_service.get("ActiveController")).is_equal(23)


func test_shift_tab_matches_reverse_only_with_exact_modifiers() -> void:
	var reverse := _key(KEY_TAB, true)
	assert_bool(reverse.is_action_pressed("squad_previous", false, true)).is_true()
	assert_bool(reverse.is_action_pressed("squad_next", false, true)).is_false()
	var forward := _key(KEY_TAB)
	assert_bool(forward.is_action_pressed("squad_previous", false, true)).is_false()
	assert_bool(forward.is_action_pressed("squad_next", false, true)).is_true()


func test_repeated_runtime_bootstraps_never_restore_remapped_keyboard_defaults() -> void:
	_service.call("ReplaceKeyboardBinding", "jump", _key(KEY_R))
	for transition in 3:
		var bootstrap: Node = load("res://scripts/ui/InputBootstrap.cs").new()
		add_child(bootstrap)
		assert_str(_service.call("GetBindingForDevice", "jump", false)).is_equal("R")
		bootstrap.free()
	_service.call("ResetDefaults")
	var label: String = _service.call("GetBindingForDevice", "jump", false)
	assert_str(label).contains("Space")
	assert_str(label).not_contains("R")


func test_incidental_shift_keeps_attack_ability_and_dash_aliases_working() -> void:
	for example: Array in [[KEY_F, "primary"], [KEY_Q, "secondary"], [KEY_C, "dash"]]:
		var player: Node = load("res://scenes/gameplay/cyber_shogun.tscn").instantiate()
		add_child(player)
		player.set_physics_process(false)
		player.call("_Input", _key(example[0], true))
		player.call("_PhysicsProcess", 0.016)
		assert_str(player.get("ActiveAction")).is_equal(example[1])
		player.free()


func test_world_motion_uses_current_context_deadzone_without_platformer_interference() -> void:
	InputMap.action_set_deadzone("left", 0.7)
	InputMap.action_set_deadzone("world_left", 0.2)
	var actions: Array[String] = ["world_left", "world_right"]
	_service.call("SetPromptContext", "world", actions)
	_service.call("ObserveInput", _axis(JOY_AXIS_LEFT_X, 0.45))
	assert_bool(_service.get("UsingController")).is_true()


func test_mouse_binding_preserves_modifier_labels() -> void:
	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.ctrl_pressed = true
	click.shift_pressed = true
	InputMap.action_add_event(SCRATCH, click)
	var label: String = _service.call("GetBindingForDevice", SCRATCH, false)
	assert_str(label).contains("Ctrl")
	assert_str(label).contains("Shift")
	assert_str(label.to_lower()).contains("mouse")


func test_squad_strip_survives_repeated_live_refresh_and_teardown_without_orphans() -> void:
	var orphans := int(Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT))
	var holder := Node.new()
	add_child(holder)
	var player: Node = load("res://scenes/gameplay/cyber_shogun.tscn").instantiate()
	holder.add_child(player)
	player.set_physics_process(false)
	var strip: Control = load("res://scripts/ui/SquadStrip.cs").new()
	holder.add_child(strip)
	strip.call("Bind", player)
	InputMap.action_erase_events("squad_slot_1")
	InputMap.action_add_event("squad_slot_1", _key(KEY_R))
	_service.call("NotifyBindingsChanged")
	for language: String in ["english", "arabic", "german"]:
		get_node("/root/LocaleService").call("ApplyLocale", language)
		for style in [XBOX, PLAYSTATION, NINTENDO]:
			_service.call("SetStyle", style, false)
			_service.call("ObserveInput", _key(KEY_R))
			_service.call("NotifyBindingsChanged")
			assert_int(strip.get_child_count()).is_equal(6)
			assert_str(strip.get_child(0).get_child(0).text).starts_with("R ")
			_service.call("ObserveInput", _button(JOY_BUTTON_X))
			_service.call("NotifyBindingsChanged")
			assert_int(strip.get_child_count()).is_equal(6)
			assert_str(strip.get_child(0).get_child(0).text).not_contains("R ")
	holder.free()
	_service.call("NotifyBindingsChanged")
	assert_int(int(Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT))).is_equal(orphans)


const XBOX_360 := 5
const XBOX_ONE := 6


func test_forced_console_styles_draw_their_button_images() -> void:
	## Each console style draws its own image for the south face button; the plain label stays text.
	InputMap.action_add_event(SCRATCH, _button(JOY_BUTTON_A))
	_service.call("ObserveInput", _button(JOY_BUTTON_A))
	for example: Array in [[XBOX_360, "xbox_360"], [XBOX_ONE, "xbox_one"], [XBOX, "xbox_series"], [PLAYSTATION, "ps5"], [NINTENDO, "switch2"]]:
		_service.call("SetStyle", example[0], false)
		var rich: String = _service.call("GetRichBinding", SCRATCH, 20)
		assert_str(rich).is_equal("[img height=20]res://assets/ui/input_prompts/%s/face_south.png[/img]" % example[1])
		assert_bool(ResourceLoader.exists("res://assets/ui/input_prompts/%s/face_south.png" % example[1])).is_true()
		assert_str(_service.call("GetBinding", SCRATCH)).not_contains("[img")


func test_keyboard_use_keyboard_style_and_unknown_pads_keep_text() -> void:
	InputMap.action_add_event(SCRATCH, _key(KEY_R))
	InputMap.action_add_event(SCRATCH, _button(JOY_BUTTON_A))
	# Keyboard in use: no images even with a console style chosen.
	_service.call("SetStyle", PLAYSTATION, false)
	_service.call("ObserveInput", _key(KEY_R))
	assert_str(_service.call("GetRichBinding", SCRATCH, 20)).is_equal("R")
	# Keyboard style while a pad is in use: text only.
	_service.call("ObserveInput", _button(JOY_BUTTON_A))
	_service.call("SetStyle", KEYBOARD, false)
	assert_str(_service.call("GetRichBinding", SCRATCH, 20)).is_equal("R")
	# Automatic with an unrecognized test pad (no name): position text, no image.
	_service.call("SetStyle", AUTO, false)
	assert_int(_service.get("ActiveGlyphSet")).is_equal(0)
	var rich: String = _service.call("GetRichBinding", SCRATCH, 20)
	assert_str(rich).not_contains("[img")
	assert_str(rich).contains("South")


func test_new_style_numbers_load_and_out_of_range_falls_back() -> void:
	_service.call("SetStyle", XBOX_ONE, false)
	assert_int(_service.get("Style")).is_equal(XBOX_ONE)
	_service.call("SetStyle", 7, false)
	assert_int(_service.get("Style")).is_equal(AUTO)


func test_rich_context_text_escapes_brackets_around_images() -> void:
	var actions: Array[String] = ["jump"]
	_service.call("SetPromptContext", "platformer", actions)
	_service.call("SetStyle", XBOX, false)
	_service.call("ObserveInput", _button(JOY_BUTTON_A))
	var rich: String = _service.call("GetRichContextText", 18)
	assert_str(rich).contains("[lb] ")
	assert_str(rich).contains(" [rb]")
	assert_str(rich).contains("[img height=18]res://assets/ui/input_prompts/xbox_series/face_south.png[/img]")
	assert_str(_service.call("GetContextText")).contains("/ A ]")


func test_action_button_switches_between_images_and_text() -> void:
	## Buttons cannot draw BBCode, so an overlay draws the images; keyboard use restores plain text.
	InputMap.action_add_event(SCRATCH, _key(KEY_R))
	InputMap.action_add_event(SCRATCH, _button(JOY_BUTTON_START))
	var button := Button.new()
	add_child(button)
	_service.call("SetStyle", PLAYSTATION, false)
	_service.call("ObserveInput", _button(JOY_BUTTON_START))
	_service.call("SetActionButton", button, "Skip", SCRATCH)
	var overlay: RichTextLabel = button.get_node("PromptGlyphs")
	assert_str(button.text).is_equal("")
	assert_bool(overlay.visible).is_true()
	assert_str(overlay.text).contains("ps5/start.png")
	assert_str(button.tooltip_text).is_equal("Skip: [ Options ]")
	_service.call("ObserveInput", _key(KEY_R))
	_service.call("SetActionButton", button, "Skip", SCRATCH)
	assert_str(button.text).is_equal("Skip: [ R ]")
	assert_bool(overlay.visible).is_false()
	button.free()


func test_mouse_clicks_copied_from_touches_do_not_switch_prompts_to_keyboard() -> void:
	## Review cb-M6: only real mouse use switches prompts away from the controller.
	_service.call("ObserveInput", _button(JOY_BUTTON_A))
	assert_bool(_service.get("UsingController")).is_true()
	var copied := InputEventMouseButton.new()
	copied.button_index = MOUSE_BUTTON_LEFT
	copied.pressed = true
	copied.device = InputEvent.DEVICE_ID_EMULATION
	_service.call("ObserveInput", copied)
	assert_bool(_service.get("UsingController")).is_true()
	var real := InputEventMouseButton.new()
	real.button_index = MOUSE_BUTTON_LEFT
	real.pressed = true
	_service.call("ObserveInput", real)
	assert_bool(_service.get("UsingController")).is_false()
