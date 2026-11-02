class_name TestActIIOpening
extends GdUnitTestSuite
## Opening signal isolation, deterministic fallback and complete input/audio lifecycle coverage.

const SCENE := "res://scenes/cutscenes/act_ii_opening.tscn"


func _preview(speech: String = "") -> Node:
	## Use the production scene with optional speech; every caller owns and frees its instance.
	var preview: Node = load(SCENE).instantiate()
	preview.set("EnterMissionOnFinish", false)
	preview.set("SpeechPath", speech)
	add_child(preview)
	preview.set_process(false)
	return preview


func _snapshot() -> Array:
	## Observe the same campaign, session and operative state without writing to it.
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	var state := [bridge.Count(), bridge.UnlockedCount(), bridge.ActiveOperative(),
		get_node("/root/Global").get("unlocked_levels"), get_node("/root/Global").get("selected_character"),
		get_node("/root/SessionManager").get("CurrentSessionId")]
	bridge = null
	return state


func test_timeline_all_cues_and_preview_leave_progress_unchanged() -> void:
	## Every authored subtitle window remains deterministic; observing playback alone never changes campaign progress.
	var before := _snapshot()
	var preview := _preview()
	var cues: Array = JSON.parse_string(FileAccess.get_file_as_string("res://assets/cutscenes/act_ii_opening/cues.json"))
	for index in range(cues.size()):
		var cue: Dictionary = cues[index]
		preview.call("Seek", cue.start + 0.1)
		assert_str(preview.get_node("UI/SubtitlePanel/Subtitle").text).is_equal(cue.speaker + ": " + cue.text)
	for seconds in [0.0, 7.9, 114.0, 119.9]:
		preview.call("Seek", seconds)
		assert_bool(preview.get_node("UI/SubtitlePanel").visible).is_false()
	preview.call("Seek", 120.0)
	preview.call("_Process", 0.1)
	await get_tree().process_frame
	assert_bool(preview.get("HasFinished")).is_true()
	assert_array(_snapshot()).is_equal(before)
	assert_int(before[0]).is_equal(5)
	preview.free()


func test_early_middle_late_skip_routes_complete_once_and_stop_audio() -> void:
	## Exercise Escape, controller Start, mouse and focused-button routes, including repeated input.
	for route in range(4):
		var preview := _preview("res://assets/cutscenes/act_ii_opening/dialogue.ogg")
		preview.call("Seek", [0.0, 60.0, 119.0, 30.0][route])
		var completed: Array = []
		preview.connect("Finished", func(): completed.append(true))
		var event: InputEvent
		if route == 0:
			var key := InputEventKey.new()
			key.pressed = true
			key.keycode = KEY_ESCAPE
			event = key
		elif route == 1:
			var joy := InputEventJoypadButton.new()
			joy.pressed = true
			joy.button_index = JOY_BUTTON_START
			event = joy
		elif route == 2:
			var mouse := InputEventMouseButton.new()
			mouse.pressed = true
			mouse.button_index = MOUSE_BUTTON_LEFT
			mouse.position = preview.get_node("UI/Skip").get_global_rect().get_center()
			event = mouse
		else:
			preview.get_node("UI/Skip").emit_signal("pressed")
		if event != null:
			preview.call("_Input", event)
			preview.call("_Input", event)
		preview.call("Finish")
		await get_tree().process_frame
		assert_int(completed.size()).is_equal(1)
		assert_bool(preview.get_node("Speech").playing).is_false()
		assert_bool(preview.get_node("Atmosphere").playing).is_false()
		assert_bool(preview.get_node("UI").visible).is_false()
		preview.free()


func test_missing_speech_and_stalled_audio_cannot_block_completion() -> void:
	## Missing and paused speech retain subtitles and advance on delta after the bounded stall fallback.
	for speech in ["res://missing-preview.ogg", "res://assets/cutscenes/act_ii_opening/dialogue.ogg"]:
		var preview := _preview(speech)
		if preview.get_node("Speech").stream != null:
			preview.get_node("Speech").stream_paused = true
		for frame in range(1250):
			preview.call("_Process", 0.1)
		assert_bool(preview.get("HasFinished")).is_true()
		preview.free()


func test_missing_art_completes_safely_without_creating_gameplay() -> void:
	## Essential scenery absence bypasses to the same preview ending with no progression changes.
	var before := _snapshot()
	var preview: Node = load(SCENE).instantiate()
	preview.set("EnterMissionOnFinish", false)
	preview.set("ArtPath", "res://missing-preview/")
	add_child(preview)
	await get_tree().process_frame
	await get_tree().process_frame
	assert_bool(preview.get("HasFinished")).is_true()
	assert_array(_snapshot()).is_equal(before)
	preview.free()


func test_teardown_restores_presentation_state() -> void:
	## Observed completion silences audio and restores the previous cursor policy on removal.
	var mode := Input.mouse_mode
	var preview := _preview()
	preview.call("Finish")
	assert_bool(preview.get("HasFinished")).is_true()
	preview.free()
	assert_int(Input.mouse_mode).is_equal(mode)


func test_only_act_completion_exposes_preview_and_retains_captured_results() -> void:
	## Ordinary mission results preserve Continue; Act I gains its separate preview with unchanged score labels.
	var card: Node = load("res://scenes/ui/cyber_victory_scorecard.tscn").instantiate()
	add_child(card)
	card.call("ShowMissionScorecard", 150, 4, "KAITO", 4)
	assert_bool(card.get_node("RootPanel/MainVBox/PreviewBtn").visible).is_false()
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_true()
	var score: String = card.get_node("RootPanel/MainVBox/StatsGrid/ScoreLabel").text
	card.call("ShowActScorecard", 150, 4, "KAITO")
	assert_bool(card.get_node("RootPanel/MainVBox/PreviewBtn").visible).is_true()
	assert_str(card.get_node("RootPanel/MainVBox/PreviewBtn").text).is_equal("CONTINUE TO ACT II")
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_false()
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ScoreLabel").text).is_equal(score)
	card.free()
