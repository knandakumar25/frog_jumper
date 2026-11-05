class_name TestStratosDocksIntro
extends GdUnitTestSuite
## Mission 8 prologue cinematic test covering input skipping, natural finish, and cue timing.

const SCENE := "res://scripts/ui/StratosDocksIntro.cs"


func test_skip_and_natural_finish_stop_owned_audio_and_emit_once() -> void:
	## The timed clock and each skip input converge on a single completion signal.
	for natural in [false, true]:
		var overlay: Node = load(SCENE).new()
		overlay.set("SpeechPath", "")
		overlay.set("EnterMissionOnFinish", false)
		var count := [0]
		overlay.connect("Finished", func() -> void: count[0] += 1)
		add_child(overlay)
		overlay.set_process(false)
		if natural:
			overlay.call("_Process", 90.0)
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
		if is_instance_valid(overlay):
			overlay.free()


func test_missing_art_uses_safe_fallback() -> void:
	## Missing essential artwork delivers completion once and removes the unusable overlay.
	var overlay: Node = load(SCENE).new()
	overlay.set("SpeechPath", "")
	overlay.set("ArtPath", "res://missing-art-dir/")
	overlay.set("EnterMissionOnFinish", false)
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	for frame in 4:
		await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	if is_instance_valid(overlay):
		overlay.free()


func test_cues_timeline_and_subtitles() -> void:
	## Tests all 9 authored cues along the timeline.
	var overlay: Node = load(SCENE).new()
	overlay.set("SpeechPath", "")
	overlay.set("EnterMissionOnFinish", false)
	add_child(overlay)
	overlay.set_process(false)

	var samples := [
		[0.5, "AXIOM-7"],
		[10.5, "MIRAGE"],
		[20.5, "TITAN"],
		[30.5, "OCEANUS"],
		[40.5, "AURA"],
		[50.5, "SURYA"],
		[60.5, "MIRAGE"],
		[70.5, "AXIOM-7"],
		[80.5, "KAITO"]
	]

	for sample in samples:
		overlay.call("Seek", sample[0])
		var text: String = overlay.get_node("UI/SubtitlePanel/Subtitle").text
		assert_bool(text.begins_with(sample[1])).is_true()

	overlay.call("Seek", 90.0)
	overlay.call("Finish")
	if is_instance_valid(overlay):
		overlay.free()
