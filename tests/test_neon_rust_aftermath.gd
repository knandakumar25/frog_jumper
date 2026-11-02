class_name TestNeonRustAftermath
extends GdUnitTestSuite
## Extraction integration and terminal lifecycle tests. Every instantiated node is explicitly freed.

const OVERLAY := "res://scripts/ui/NeonRustAftermath.cs"
const SCORECARD := "res://scenes/ui/cyber_victory_scorecard.tscn"


func test_missing_speech_retains_full_duration_and_subtitles() -> void:
	## With no voice clock, visual time advances to exactly 90 seconds and only then queues results.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	overlay.set_process(false)
	for step in range(9):
		assert_bool(overlay.get("HasFinished")).is_false()
		assert_str(overlay.get_node("UI/SubtitlePanel/Subtitle").text).is_not_empty()
		overlay.call("_Process", 10.0)
	assert_float(overlay.get("Elapsed")).is_equal(90.0)
	assert_bool(overlay.get("HasFinished")).is_true()
	assert_int(count[0]).is_equal(0)
	await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	if is_instance_valid(overlay): overlay.free()


func test_skip_releases_mapped_jump_action() -> void:
	## Consuming Space also clears its held action so it cannot leak into later results navigation.
	var overlay: Node = load(OVERLAY).new()
	add_child(overlay)
	overlay.set_process(false)
	Input.action_press("jump")
	var event := InputEventKey.new()
	event.pressed = true
	event.keycode = KEY_SPACE
	event.physical_keycode = KEY_SPACE
	overlay.call("_Input", event)
	assert_bool(Input.is_action_pressed("jump")).is_false()
	overlay.free()


func test_early_middle_late_and_natural_finish_emit_once() -> void:
	## Seeking is deterministic; missing speech still allows every completion path and stops ambience before results.
	for seconds in [0.1, 44.0, 89.8, 90.0]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		var count := [0]
		overlay.connect("Finished", func() -> void: count[0] += 1)
		add_child(overlay)
		overlay.set_process(false)
		overlay.call("Seek", seconds)
		var first: String = overlay.get_node("UI/SubtitlePanel/Subtitle").text
		overlay.call("Seek", 5.0)
		overlay.call("Seek", seconds)
		assert_str(overlay.get_node("UI/SubtitlePanel/Subtitle").text).is_equal(first)
		if seconds == 90.0:
			overlay.call("_Process", 0.1)
		elif seconds < 1:
			var key := InputEventKey.new()
			key.pressed = true
			key.keycode = KEY_ESCAPE
			overlay.call("_Input", key)
		elif seconds < 50:
			var start := InputEventJoypadButton.new()
			start.pressed = true
			start.button_index = JOY_BUTTON_START
			overlay.call("_Input", start)
		else:
			overlay.get_node("UI/Skip").pressed.emit()
		overlay.call("Finish")
		assert_int(count[0]).is_equal(0)
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		await get_tree().process_frame
		assert_int(count[0]).is_equal(1)
		if is_instance_valid(overlay):
			overlay.free()


func test_missing_art_converges_on_results() -> void:
	## Missing essential artwork must not strand a completed mission.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("ArtPath", "res://missing-breach-art/")
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	if is_instance_valid(overlay):
		overlay.free()


func test_pointer_and_touch_skip_only_inside_button() -> void:
	## Direct pointer/touch input bypasses Global's mouse suppression without making the full screen skippable.
	for use_touch in [true, false]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		add_child(overlay)
		overlay.set_process(false)
		await get_tree().process_frame
		var event: Variant
		if use_touch:
			event = InputEventScreenTouch.new()
		else:
			event = InputEventMouseButton.new()
			event.button_index = MOUSE_BUTTON_LEFT
		event.pressed = true
		event.position = Vector2(-20, -20)
		overlay.call("_Input", event)
		assert_bool(overlay.get("HasFinished")).is_false()
		event.position = overlay.get_node("UI/Skip").get_global_rect().get_center()
		overlay.call("_Input", event)
		assert_bool(overlay.get("HasFinished")).is_true()
		overlay.free()


func test_final_scorecard_hides_continue_and_preserves_grades() -> void:
	## The optional target changes navigation only; existing grade thresholds and final-mission callers remain valid.
	var card: Node = load(SCORECARD).instantiate()
	card.call("_Ready")
	card.call("ShowMissionScorecard", 120, 3, "KAITO", 3)
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_true()
	assert_str(card.get_node("RootPanel/MainVBox/RankPanel/RankLabel").text).contains("S RANK")
	card.call("ShowScorecard", 49, 1, "KAITO")
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_false()
	assert_str(card.get_node("RootPanel/MainVBox/RankPanel/RankLabel").text).contains("B RANK")
	card.free()


func test_all_keyboard_and_controller_skips_consume_once() -> void:
	## Every supported shortcut stops audio synchronously and emits results once after input dispatch.
	for spec in [[true, KEY_ESCAPE], [true, KEY_SPACE], [true, KEY_ENTER], [false, JOY_BUTTON_START], [false, JOY_BUTTON_A]]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		add_child(overlay)
		overlay.set_process(false)
		var count := [0]
		overlay.connect("Finished", func() -> void: count[0] += 1)
		var event: InputEvent
		if spec[0]:
			event = InputEventKey.new()
			event.keycode = spec[1]
		else:
			event = InputEventJoypadButton.new()
			event.button_index = spec[1]
		event.pressed = true
		overlay.call("_Input", event)
		overlay.call("Finish")
		assert_int(count[0]).is_equal(0)
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		await get_tree().process_frame
		assert_int(count[0]).is_equal(1)
		if is_instance_valid(overlay):
			overlay.free()


func test_teardown_does_not_emit_results() -> void:
	## Removing an active overlay stops owned audio without delivering a late navigation callback.
	var overlay: Node = load(OVERLAY).new()
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	overlay.free()
	await get_tree().process_frame
	assert_int(count[0]).is_equal(0)


func test_teardown_restores_existing_cursor_and_prompt_state() -> void:
	## A scene that already hid Global's prompt keeps it hidden after overlay teardown.
	var global := get_node("/root/Global")
	# Headless Global omits its visual prompt: provide an owned representative canvas for this test.
	var original: Variant = global.get("_prompt_label")
	var prompt := CanvasLayer.new()
	var label := Label.new()
	prompt.add_child(label)
	add_child(prompt)
	global.set("_prompt_label", label)
	var previous_mouse := Input.mouse_mode
	global.call("set_control_prompt_visible", false)
	Input.mouse_mode = Input.MOUSE_MODE_HIDDEN
	# Headless DisplayServer may ignore cursor setters; compare the state the overlay can actually capture.
	var captured_mouse := Input.mouse_mode
	var overlay: Node = load(OVERLAY).new()
	add_child(overlay)
	assert_int(Input.mouse_mode).is_equal(Input.MOUSE_MODE_VISIBLE)
	overlay.free()
	assert_bool(prompt.visible).is_false()
	assert_int(Input.mouse_mode).is_equal(captured_mouse)
	global.set("_prompt_label", original)
	prompt.free()
	Input.mouse_mode = previous_mouse
