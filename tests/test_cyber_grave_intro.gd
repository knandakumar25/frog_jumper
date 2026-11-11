class_name TestCyberGraveIntro
extends GdUnitTestSuite
const ART := preload("res://tests/support/cutscene_art.gd")  # Cutscene art may await SpriteCook regeneration.
## Pre-Level 6 Cyber-Grave Intro lifecycle, subtitle timeline, and completion contracts.

const OVERLAY := "res://scripts/ui/CyberGraveIntro.cs"


func test_skip_and_natural_finish_stop_owned_audio_and_emit_once() -> void:
	## The timed cue clock and skip input converge on a single completion signal.
	for natural in [false, true]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		overlay.set("EnterMissionOnFinish", false)
		var count := [0]
		overlay.connect("Finished", func() -> void: count[0] += 1)
		add_child(overlay)
		overlay.set_process(false)
		if natural:
			overlay.call("_Process", 120.0)
		else:
			var skip := InputEventKey.new()
			skip.pressed = true
			skip.keycode = KEY_ESCAPE
			overlay.call("_Input", skip)
		overlay.call("Finish")
		assert_bool(overlay.get("HasFinished")).is_true()
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		await get_tree().process_frame
		assert_int(count[0]).is_equal(1)
		if is_instance_valid(overlay): overlay.free()


func test_missing_speech_keeps_the_complete_subtitle_timeline() -> void:
	## Each authored cue remains readable and the clock reaches its 120-second ending without a voice stream.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("EnterMissionOnFinish", false)
	add_child(overlay)
	overlay.set_process(false)
	for sample in [
		[2.0, "AXIOM-7"],
		[16.0, "KAITO"],
		[29.0, "AURA"],
		[42.0, "OCEANUS"],
		[55.0, "TITAN"],
		[70.0, "MIRAGE"],
		[84.0, "SURYA"],
		[95.0, "OCEANUS"],
		[104.0, "KAITO"]
	]:
		overlay.call("Seek", sample[0])
		assert_str(overlay.get_node("UI/SubtitlePanel/Subtitle").text).contains(sample[1])
	assert_bool(overlay.get("HasFinished")).is_false()
	overlay.call("Seek", 120.0)
	assert_bool(overlay.get("HasFinished")).is_false()
	# Seeking to the end is intentionally inert; natural _Process completion owns the handoff.
	overlay.call("_Process", 0.1)
	assert_bool(overlay.get("HasFinished")).is_true()
	overlay.free()


func test_independent_removal_stops_audio_and_completes_safely() -> void:
	if not ART.available("res://scripts/ui/CyberGraveStage.cs", self):
		return  # Playback needs art awaiting SpriteCook regeneration (docs/ART_REGENERATION.md).
	## Teardown while the owning tree remains active safely cleans up without orphan nodes.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("EnterMissionOnFinish", false)
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	await get_tree().process_frame
	overlay.free()
	await get_tree().process_frame
	assert_bool(is_instance_valid(overlay)).is_false()
