class_name TestLaterCinematicLifecycle
extends GdUnitTestSuite
## Every later presentation shares exactly-once deferred completion, all supported Skip routes and owned teardown.

const PREFIXES := ["CyberGrave", "FoundryForge", "StratosDocks", "SpireOuterHull", "CyberNexus", "ApexBastion", "JudgmentDay"]


func _overlay(name: String, missing_art := false) -> Node:
	## Disable automatic navigation and speech to test lifecycle without changing campaigns or waiting for recordings.
	var overlay: Node = load("res://scripts/ui/%s.cs" % name).new()
	overlay.set("SpeechPath", "")
	if name.ends_with("Intro"):
		overlay.set("EnterMissionOnFinish", false)
	if missing_art:
		overlay.set("ArtPath", "res://missing-essential-art/")
	return overlay


func test_every_later_overlay_consumes_keyboard_pad_pointer_and_touch_skip() -> void:
	## Invoke the production input handler; completion is deferred and the overlay is actually removed.
	for prefix in PREFIXES:
		for suffix in ["Intro", "Aftermath"]:
			for route in ["escape", "space", "enter", "start", "a", "b", "mouse", "touch"]:
				var overlay := _overlay(prefix + suffix)
				var count := [0]
				overlay.connect("Finished", func(): count[0] += 1)
				add_child(overlay)
				overlay.set_process(false)
				var event: InputEvent
				if route in ["escape", "space", "enter"]:
					var key := InputEventKey.new()
					key.pressed = true
					key.keycode = {"escape": KEY_ESCAPE, "space": KEY_SPACE, "enter": KEY_ENTER}[route]
					event = key
				elif route in ["start", "a", "b"]:
					var pad := InputEventJoypadButton.new()
					pad.pressed = true
					pad.button_index = {"start": JOY_BUTTON_START, "a": JOY_BUTTON_A, "b": JOY_BUTTON_B}[route]
					event = pad
				else:
					var button: Button = overlay.find_child("Skip", true, false)
					if button == null:
						button = overlay.find_child("SkipButton", true, false)
					assert_that(button).is_not_null()
					await get_tree().process_frame
					if route == "mouse":
						var mouse := InputEventMouseButton.new()
						mouse.pressed = true
						mouse.button_index = MOUSE_BUTTON_LEFT
						mouse.position = button.get_global_rect().get_center()
						event = mouse
					else:
						var touch := InputEventScreenTouch.new()
						touch.pressed = true
						touch.position = button.get_global_rect().get_center()
						event = touch
				Input.action_press("attack")
				overlay.call("_Input", event)
				assert_bool(overlay.get("HasFinished")).is_true()
				assert_bool(Input.is_action_pressed("attack")).is_false()
				overlay.call("Finish")
				await get_tree().process_frame
				await get_tree().process_frame
				assert_int(count[0]).is_equal(1)
				assert_bool(is_instance_valid(overlay)).is_false()


func test_missing_essential_art_falls_back_and_independent_removal_delivers_once() -> void:
	## Independent removal must release retained results just as natural or Skip completion does.
	for prefix in PREFIXES:
		for suffix in ["Intro", "Aftermath"]:
			for missing_art in [false, true]:
				var overlay := _overlay(prefix + suffix, missing_art)
				var count := [0]
				overlay.connect("Finished", func(): count[0] += 1)
				add_child(overlay)
				if not missing_art:
					overlay.queue_free()
				for frame in 4:
					await get_tree().process_frame
				assert_int(count[0]).is_equal(1)
				assert_bool(is_instance_valid(overlay)).is_false()


func test_visual_sampling_does_not_restart_natural_speech_playback() -> void:
	## Real mounted audio remains at its current time when visuals sample; explicit playback scrubbing moves it.
	for name in ["SpireOuterHullIntro", "CyberNexusAftermath"]:
		var overlay: Node = load("res://scripts/ui/%s.cs" % name).new()
		if name.ends_with("Intro"):
			overlay.set("EnterMissionOnFinish", false)
		add_child(overlay)
		overlay.set_process(false)
		var speech: AudioStreamPlayer
		for child in overlay.get_children():
			if child is AudioStreamPlayer and child.bus == &"SFX":
				speech = child
		assert_that(speech).is_not_null()
		assert_bool(speech.playing).is_true()
		overlay.call("Seek", 40.0)
		await get_tree().create_timer(0.05).timeout
		assert_float(speech.get_playback_position()).is_less(2.0)
		overlay.call("SeekPlayback", 40.0)
		await get_tree().create_timer(0.05).timeout
		assert_float(speech.get_playback_position()).is_greater(39.0)
		overlay.free()
		await get_tree().process_frame
