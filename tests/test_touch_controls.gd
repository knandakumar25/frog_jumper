extends GdUnitTestSuite
## On-screen touch controls: buttons and the joystick press real InputMap actions, several fingers
## work at once, hiding releases everything, and the show/hide rule follows touches and keys.
## Every node created here is freed; no test needs a phone or a touchscreen.

const CONTROLS := "res://scripts/ui/touch/TouchControls.cs"


func _controls(layout: String) -> CanvasLayer:
	var controls: CanvasLayer = load(CONTROLS).new()
	controls.call("UseLayout", layout)
	add_child(controls)
	controls.call("SetWanted", true)
	return controls


func _touch(index: int, pressed: bool) -> InputEventScreenTouch:
	var event := InputEventScreenTouch.new()
	event.index = index
	event.pressed = pressed
	return event


func _settle() -> void:
	# parse_input_event may buffer; flushing applies the action state now.
	Input.flush_buffered_events()


func test_button_touch_holds_its_action_until_release() -> void:
	var controls := _controls("platformer")
	var jump: Control = controls.call("ButtonFor", "jump")
	jump.call("_GuiInput", _touch(0, true))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_true()
	jump.call("_GuiInput", _touch(0, false))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_false()
	controls.free()


func test_two_fingers_hold_two_actions() -> void:
	var controls := _controls("platformer")
	var jump: Control = controls.call("ButtonFor", "jump")
	var attack: Control = controls.call("ButtonFor", "attack")
	jump.call("_GuiInput", _touch(0, true))
	attack.call("_GuiInput", _touch(1, true))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_true()
	assert_bool(Input.is_action_pressed("attack")).is_true()
	# Lifting the other finger's index does not release this button.
	jump.call("_GuiInput", _touch(1, false))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_true()
	controls.free()
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_false()
	assert_bool(Input.is_action_pressed("attack")).is_false()


func test_joystick_sets_direction_strength_and_release_clears_it() -> void:
	var controls := _controls("world")
	var stick: Control = controls.get("Joystick")
	assert_object(stick).is_not_null()
	stick.emit_signal("moved", Vector2(-0.8, 0.0))
	_settle()
	assert_float(Input.get_action_strength("world_left")).is_equal_approx(0.8, 0.01)
	assert_float(Input.get_action_strength("world_right")).is_equal(0.0)
	stick.emit_signal("moved", Vector2.ZERO)
	_settle()
	assert_float(Input.get_action_strength("world_left")).is_equal(0.0)
	controls.free()


func test_platformer_stick_up_never_jumps() -> void:
	var controls := _controls("platformer")
	var stick: Control = controls.get("Joystick")
	stick.emit_signal("moved", Vector2(0.0, -1.0))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_false()
	stick.emit_signal("moved", Vector2.ZERO)
	controls.free()


func test_hiding_releases_held_actions() -> void:
	var controls := _controls("isometric")
	var dash: Control = controls.call("ButtonFor", "iso_dash")
	var stick: Control = controls.get("Joystick")
	dash.call("_GuiInput", _touch(0, true))
	stick.emit_signal("moved", Vector2(0.0, 1.0))
	_settle()
	assert_bool(Input.is_action_pressed("iso_dash")).is_true()
	assert_bool(Input.is_action_pressed("iso_move_down")).is_true()
	controls.call("SetSuppressed", true)
	_settle()
	assert_bool(controls.get("Shown")).is_false()
	assert_bool(Input.is_action_pressed("iso_dash")).is_false()
	assert_bool(Input.is_action_pressed("iso_move_down")).is_false()
	controls.free()


func test_touch_shows_and_keys_hide_but_emulated_mouse_does_not() -> void:
	var controls: CanvasLayer = load(CONTROLS).new()
	controls.call("UseLayout", "platformer")
	add_child(controls)
	# Desktop/headless builds start hidden.
	assert_bool(controls.get("Shown")).is_false()
	controls.call("_Input", _touch(0, true))
	assert_bool(controls.get("Shown")).is_true()
	# A mouse click copied from that touch keeps the controls on screen.
	var copied := InputEventMouseButton.new()
	copied.button_index = MOUSE_BUTTON_LEFT
	copied.pressed = true
	copied.device = InputEvent.DEVICE_ID_EMULATION
	controls.call("_Input", copied)
	assert_bool(controls.get("Shown")).is_true()
	var key := InputEventKey.new()
	key.physical_keycode = KEY_A
	key.pressed = true
	controls.call("_Input", key)
	assert_bool(controls.get("Shown")).is_false()
	controls.free()


func test_squad_button_hides_and_shows() -> void:
	var controls := _controls("platformer")
	var squad: Control = controls.call("ButtonFor", "squad_next")
	controls.call("SetButtonVisible", "squad_next", false)
	assert_bool(squad.visible).is_false()
	controls.call("SetButtonVisible", "squad_next", true)
	assert_bool(squad.visible).is_true()
	controls.free()


func test_buttons_stay_inside_the_viewport_without_overlapping() -> void:
	for layout: String in ["platformer", "isometric", "world"]:
		var controls := _controls(layout)
		var view := Rect2(Vector2.ZERO, get_viewport().get_visible_rect().size)
		var rects: Array[Rect2] = []
		for child: Node in controls.get_node("Root").get_children():
			if child is Control and child.name.begins_with("Touch_") and child.visible:
				var rect := Rect2((child as Control).position, (child as Control).size)
				assert_bool(view.encloses(rect)).override_failure_message("%s %s outside view" % [layout, child.name]).is_true()
				# Buttons and their hit areas are circles: centres must be at least the two radii apart.
				for other: Rect2 in rects:
					var apart := rect.get_center().distance_to(other.get_center())
					assert_bool(apart >= (rect.size.x + other.size.x) * 0.5).override_failure_message("%s %s overlaps %s vs %s" % [layout, child.name, rect, other]).is_true()
				rects.append(rect)
		controls.free()


func test_emulated_mouse_is_not_deliberate_mouse_use() -> void:
	## Touch-copied clicks must not switch mission hints back to keyboard text.
	var binding: Script = load("res://scripts/input/InputPromptBinding.cs")
	var copied := InputEventMouseButton.new()
	copied.button_index = MOUSE_BUTTON_LEFT
	copied.pressed = true
	copied.device = InputEvent.DEVICE_ID_EMULATION
	var real := InputEventMouseButton.new()
	real.button_index = MOUSE_BUTTON_LEFT
	real.pressed = true
	var probe: Node = load("res://scripts/ui/touch/TouchControls.cs").new()
	# The visibility classifier wraps DeliberateNonTouch, so check it through a controls instance.
	probe.call("UseLayout", "platformer")
	add_child(probe)
	probe.call("SetWanted", true)
	probe.call("_Input", copied)
	assert_bool(probe.get("Shown")).is_true()
	probe.call("_Input", real)
	assert_bool(probe.get("Shown")).is_false()
	assert_object(binding).is_not_null()
	probe.free()


func test_mission_tap_keeps_touch_hint_and_shows_controls() -> void:
	## A touch followed by its copied mouse click keeps the touch hint (the emulated-mouse fix).
	var mission: Node = scene_runner("res://scenes/gameplay/level1.tscn").scene()
	var controls: CanvasLayer = mission.get("TouchControlsNode")
	assert_object(controls).is_not_null()
	mission.call("_Input", _touch(0, true))
	controls.call("_Input", _touch(0, true))
	var copied := InputEventMouseButton.new()
	copied.button_index = MOUSE_BUTTON_LEFT
	copied.pressed = true
	copied.device = InputEvent.DEVICE_ID_EMULATION
	mission.call("_Input", copied)
	controls.call("_Input", copied)
	assert_str(mission.get("ControlsHintText")).contains("touch")
	assert_bool(controls.get("Shown")).is_true()
	controls.call("SetWanted", false)


func test_hidden_controls_ignore_held_thumb_and_reset_the_joystick() -> void:
	## Review cb-M1/ir-M3: a thumb left on the stick when the controls hide presses nothing, and
	## the joystick lets go of that finger so a new touch works when the controls return.
	var controls := _controls("world")
	var stick: Control = controls.get("Joystick")
	stick.call("_begin", Vector2(40, 40))
	assert_bool(stick.call("is_active")).is_true()
	controls.call("SetSuppressed", true)
	assert_bool(stick.call("is_active")).is_false()
	stick.emit_signal("moved", Vector2(1.0, 0.0))
	_settle()
	assert_bool(Input.is_action_pressed("world_right")).is_false()
	controls.call("SetSuppressed", false)
	stick.emit_signal("moved", Vector2(1.0, 0.0))
	_settle()
	assert_bool(Input.is_action_pressed("world_right")).is_true()
	controls.free()
	_settle()
	assert_bool(Input.is_action_pressed("world_right")).is_false()


func test_focus_loss_releases_everything() -> void:
	var controls := _controls("platformer")
	var jump: Control = controls.call("ButtonFor", "jump")
	jump.call("_GuiInput", _touch(0, true))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_true()
	controls.notification(Node.NOTIFICATION_APPLICATION_FOCUS_OUT)
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_false()
	# The button forgot the lost finger, so a new touch presses it again.
	jump.call("_GuiInput", _touch(2, true))
	_settle()
	assert_bool(Input.is_action_pressed("jump")).is_true()
	controls.free()


func test_isometric_pause_has_a_tappable_resume() -> void:
	## Review cb-2: with the touch controls hidden by the pause, the panel's Resume button unpauses.
	var mission: Node = load("res://scripts/gameplay/IsometricMission.cs").new()
	add_child(mission)
	var pause := InputEventAction.new()
	pause.action = "iso_pause"
	pause.pressed = true
	mission.call("_Input", pause)
	assert_bool(mission.get("IsPaused")).is_true()
	var resume: Button = mission.find_child("ResumeButton", true, false)
	assert_object(resume).is_not_null()
	assert_bool(resume.is_visible_in_tree()).is_true()
	resume.emit_signal("pressed")
	assert_bool(mission.get("IsPaused")).is_false()
	assert_bool(get_tree().paused).is_false()
	mission.free()
	get_tree().paused = false
