extends GdUnitTestSuite
## Exercise the live clock, cues, transition guard and node lifetime without decoding video.

const SCENE := preload('res://scenes/main_menu/deployment_cutscene.tscn')


func test_standalone_finish_does_not_access_scene_tree() -> void:
	## Standalone nodes support cleanup/seek without triggering engine lifecycle errors.
	var scene = SCENE.instantiate()
	scene.seek_visuals(14.0)
	scene.finish()
	scene._stop_audio()
	assert_bool(scene.transitioning).is_false()
	scene.free()


func test_subtitles_fit_inside_the_forty_second_film() -> void:
	## Original cues retain order, half-open boundaries and their unchanged duration.
	var scene = SCENE.instantiate()
	var previous_end := 0.0
	for cue in scene.CUES:
		assert_float(float(cue[0])).is_greater_equal(previous_end)
		assert_float(float(cue[1])).is_greater(float(cue[0]))
		assert_float(float(cue[1])).is_less_equal(40.0)
		assert_str(scene.subtitle_at(cue[0])).is_not_empty()
		assert_str(scene.subtitle_at(cue[1])).is_empty()
		previous_end = cue[1]
	scene.free()


func test_missing_voice_reaches_natural_completion() -> void:
	## Absent audio still advances the entire timeline and finishes exactly once.
	var scene = SCENE.instantiate()
	scene.narration_path = ''
	scene.transition_to_briefing = false
	add_child(scene)
	scene.set_process(false)
	scene.seek_visuals(39.9)
	scene._process(0.2)
	assert_float(scene.elapsed).is_equal(40.0)
	assert_bool(scene.transitioning).is_true()
	assert_bool(scene.narrator.playing).is_false()
	scene.free()


func test_early_mid_late_and_repeated_skip() -> void:
	## Skip input and natural completion share an idempotent transition and stop audio.
	for time in [0.2, 22.0, 38.5]:
		var scene = SCENE.instantiate()
		scene.transition_to_briefing = false
		add_child(scene)
		scene.set_process(false)
		scene.seek_visuals(time)
		var calls := [0]
		scene.completed.connect(func(): calls[0] += 1)
		if time < 1.0:
			var key := InputEventKey.new()
			key.keycode = KEY_ESCAPE
			key.pressed = true
			scene._unhandled_input(key)
		elif time < 30.0:
			var start := InputEventJoypadButton.new()
			start.button_index = JOY_BUTTON_START
			start.pressed = true
			scene._unhandled_input(start)
		else:
			scene.get_node('UI/Skip').pressed.emit()
		scene.finish()
		assert_int(calls[0]).is_equal(1)
		assert_bool(scene.narrator.playing).is_false()
		scene.free()


func test_exit_tree_stops_narration() -> void:
	## Removing a running scene stops its owned audio before the node is freed.
	var scene = SCENE.instantiate()
	scene.transition_to_briefing = false
	add_child(scene)
	remove_child(scene)
	assert_bool(scene.narrator.playing).is_false()
	scene.free()


func test_four_shots_have_readable_action_states() -> void:
	## Seek across the run, airborne arc, weighted landing, dash and transmission.
	var scene = SCENE.instantiate()
	scene.narration_path = ''
	scene.transition_to_briefing = false
	add_child(scene)
	scene.set_process(false)
	for time in [5.0, 15.0, 22.0, 25.4, 27.1, 28.7, 35.0]:
		scene.seek_visuals(time)
		assert_bool(scene.stage.hero.visible).is_equal(time >= 10 and time < 32)
		assert_bool(scene.stage.transmission.visible).is_equal(time >= 32)
		if time == 25.4:
			assert_float(scene.stage.hero.position.y).is_less(600.0)
		if time == 28.7:
			assert_bool(scene.stage.ghost.visible).is_true()
	scene.free()


func test_touch_skips_only_inside_the_button() -> void:
	## Direct touch works even when Global consumes emulated mouse events.
	var scene = SCENE.instantiate()
	scene.narration_path = ''
	scene.transition_to_briefing = false
	add_child(scene)
	scene.set_process(false)
	var touch := InputEventScreenTouch.new()
	touch.pressed = true
	touch.position = Vector2(700, 500)
	scene._input(touch)
	assert_bool(scene.transitioning).is_false()
	touch.position = scene.get_node('UI/Skip').get_global_rect().get_center()
	scene._input(touch)
	scene._input(touch)
	assert_bool(scene.transitioning).is_true()
	scene.free()


func test_narration_plays_on_voice_bus_with_its_table_gain() -> void:
	## The narration path (VOICE, duplicated from SpeechPaths.DeploymentNarration) must be in the
	## gain table, or it would play about 7 dB under the briefing that follows it.
	var table: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/audio/voice_gain.json"))
	var voice: String = preload("res://scripts/main_menu/deployment_cutscene.gd").VOICE
	assert_bool((table["files"] as Dictionary).has(voice)).is_true()
	var scene = SCENE.instantiate()
	scene.transition_to_briefing = false
	add_child(scene)
	var gain: float = table["files"][scene.narration_path]["gain_db"]
	assert_float(absf(gain)).is_greater(0.01)
	assert_str(String(scene.narrator.bus)).is_equal("Voice")
	assert_float(scene.narrator.volume_db).is_equal_approx(gain, 0.01)
	remove_child(scene)
	scene.free()
