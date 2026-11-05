class_name TestSpireOuterHullAftermath
extends GdUnitTestSuite
## Mission 9 post-extraction epilogue cinematic cleanup and exactly-once results handoff contracts.

const OVERLAY := "res://scripts/ui/SpireOuterHullAftermath.cs"


func test_skip_and_natural_finish_stop_owned_audio_and_emit_once() -> void:
	## The timed clock and each skip input converge on a single completion signal.
	for natural in [false, true]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
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


func test_missing_art_uses_results_fallback() -> void:
	## Missing essential art reveals results once and removes the unusable overlay.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("ArtPath", "res://missing-art-dir/")
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	for frame in 4:
		await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	if is_instance_valid(overlay):
		overlay.free()


func test_missing_speech_keeps_the_complete_subtitle_timeline() -> void:
	## Each authored cue remains readable and the clock reaches its 90-second ending without a voice stream.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	add_child(overlay)
	overlay.set_process(false)
	var samples := [
		[0.1, "AXIOM-7"],
		[11.0, "TITAN"],
		[21.0, "OCEANUS"],
		[31.0, "SURYA"],
		[41.0, "AURA"],
		[51.0, "MIRAGE"],
		[61.0, "AXIOM-7"],
		[71.0, "MIRAGE"],
		[81.0, "KAITO"]
	]
	for sample in samples:
		overlay.call("Seek", sample[0])
		assert_str(overlay.get_node("UI/SubtitlePanel/Subtitle").text).contains(sample[1])
	assert_bool(overlay.get("HasFinished")).is_false()
	overlay.call("Seek", 90.0)
	assert_bool(overlay.get("HasFinished")).is_false()
	# Natural _Process completion owns the handoff.
	overlay.call("_Process", 0.1)
	assert_bool(overlay.get("HasFinished")).is_true()
	if is_instance_valid(overlay):
		overlay.free()


func test_independent_removal_stops_audio_and_completes_once() -> void:
	## Teardown while the owning tree remains active stops tracks cleanly.
	var overlay: Node = load(OVERLAY).new()
	var count := [0]
	var audio_stopped := [false]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	overlay.tree_exiting.connect(func() -> void: audio_stopped[0] = not overlay.get_node("Atmosphere").playing)
	add_child(overlay)
	overlay.queue_free()
	await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	assert_bool(audio_stopped[0]).is_true()
	assert_bool(is_instance_valid(overlay)).is_false()
