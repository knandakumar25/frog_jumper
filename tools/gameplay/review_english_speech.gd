extends SceneTree
## Bounded natural-playback review using real collectible callbacks, mixer capture and the production speech owner.

var _observations: Array[Dictionary] = []
var _failed := false
var _capture := AudioEffectCapture.new()


func _initialize() -> void:
	## Defer until the SceneTree root and autoloads are inside the active tree.
	_run.call_deferred()


func _run() -> void:
	## Capture only speech on SFX while freezing combat; never seek or synthesize a finished signal.
	var bus := AudioServer.get_bus_index("SFX")
	if bus < 0:
		_failed = true
		_finish()
		return
	_capture.buffer_length = 1.0
	AudioServer.add_bus_effect(bus, _capture)
	await _review_collectibles(5, 3)
	await _review_collectibles(12, 1)
	await _review_cinematic()
	AudioServer.remove_bus_effect(bus, AudioServer.get_bus_effect_count(bus) - 1)
	var master := AudioServer.get_bus_index("Master")
	AudioServer.add_bus_effect(master, _capture)
	await _review_briefing()
	AudioServer.remove_bus_effect(master, AudioServer.get_bus_effect_count(master) - 1)
	_finish()


func _review_collectibles(number: int, count: int) -> void:
	## Trigger actual collection in order, then observe full clips completing through the audio mixer.
	var mission: Node = load("res://scenes/gameplay/level%d.tscn" % number).instantiate()
	root.add_child(mission)
	await process_frame
	for child in mission.get_children():
		if child.has_method("Finish") and child.name != "CyberVictoryScorecard":
			child.call("Finish")
	mission.set_physics_process(false)
	mission.get_node("Actors").process_mode = Node.PROCESS_MODE_DISABLED
	var queue: Node = mission.get_node("MissionSpeechQueue")
	queue.call("Cancel")
	var player: Node2D = mission.get_node("Actors/CyberShogun")
	var collected := 0
	for child in mission.get_node("Actors").get_children():
		if child.has_method("SetCollected") and str(child.get("Kind")) == "Fragment" and collected < count:
			player.global_position = child.global_position + Vector2(0, -40)
			mission.call("CollectFragments")
			collected += 1
	var speech: AudioStreamPlayer = queue.get_node("EnglishSpeech")
	var started := Time.get_ticks_msec()
	var seen: Array[String] = []
	var max_position := 0.0
	var captured_frames := 0
	var peak := 0.0
	var paused_checked := false
	while Time.get_ticks_msec() - started < 90000:
		await create_timer(0.05).timeout
		var cue := str(queue.get("CurrentCueId"))
		if not cue.is_empty() and not seen.has(cue):
			seen.append(cue)
		max_position = maxf(max_position, speech.get_playback_position())
		var frames := _capture.get_frames_available()
		if frames > 0:
			var buffer := _capture.get_buffer(frames)
			captured_frames += buffer.size()
			for sample in buffer:
				peak = maxf(peak, maxf(absf(sample.x), absf(sample.y)))
		if not paused_checked and speech.get_playback_position() > 0.5:
			paused_checked = true
			paused = true
			await create_timer(0.15, true).timeout
			var before := speech.get_playback_position()
			await create_timer(0.2, true).timeout
			var after := speech.get_playback_position()
			if absf(after - before) > 0.05 or not speech.stream_paused:
				_failed = true
			paused = false
		if not queue.get("Busy"):
			break
	var expected: Array[String] = []
	for index in range(1, count + 1):
		expected.append("m%02d.fragment%d" % [number, index])
	var passed: bool = seen == expected and not queue.get("Busy") and max_position > 1.0 and captured_frames > 0 and peak > 0.001
	_failed = _failed or not passed
	_observations.append({"mission": number, "cues": seen, "expected": expected, "natural_completion": not queue.get("Busy"),
		"max_playback_position": max_position, "captured_frames": captured_frames, "peak": peak,
		"pause_checked": paused_checked, "passed": passed})
	mission.free()
	await process_frame


func _review_cinematic() -> void:
	## Play the repaired Foundry epilogue through its natural transition and verify real mixed-track progress.
	var overlay: Node = load("res://scripts/ui/FoundryForgeAftermath.cs").new()
	root.add_child(overlay)
	var speech: AudioStreamPlayer = overlay.get_node("Speech")
	var began := Time.get_ticks_msec()
	var furthest := 0.0
	while Time.get_ticks_msec() - began < 100000 and not overlay.get("HasFinished"):
		await create_timer(0.1).timeout
		furthest = maxf(furthest, speech.get_playback_position())
		_capture.clear_buffer()
	var passed: bool = overlay.get("HasFinished") and not speech.playing and furthest > 85.0
	_failed = _failed or not passed
	_observations.append({"cinematic": "foundry_forge_aftermath", "natural_completion": overlay.get("HasFinished"),
		"max_playback_position": furthest, "passed": passed})
	overlay.free()
	await process_frame


func _review_briefing() -> void:
	## Let each replacement line finish, then use the production Continue button to advance without changing navigation.
	var briefing: Node = load("res://scenes/main_menu/story_intro.tscn").instantiate()
	root.add_child(briefing)
	var voice: AudioStreamPlayer
	for child in briefing.get_children():
		if child is AudioStreamPlayer:
			voice = child
	if voice == null:
		_failed = true
		briefing.free()
		return
	for index in range(1, 5):
		var began := Time.get_ticks_msec()
		var furthest := 0.0
		var peak := 0.0
		while Time.get_ticks_msec() - began < 60000 and voice.playing:
			await create_timer(0.05).timeout
			furthest = maxf(furthest, voice.get_playback_position())
			var frames := _capture.get_frames_available()
			if frames > 0:
				for sample in _capture.get_buffer(frames):
					peak = maxf(peak, maxf(absf(sample.x), absf(sample.y)))
		var passed := not voice.playing and furthest > 1.0 and peak > 0.001
		_failed = _failed or not passed
		_observations.append({"briefing_line": index, "max_playback_position": furthest,
			"natural_completion": not voice.playing, "captured_peak": peak, "passed": passed})
		if index < 4:
			briefing.get_node("HBoxContainer/CyberDialogueBubble").call("CompleteTyping")
			briefing.get_node("ContinueButton").pressed.emit()
	briefing.free()
	await process_frame


func _finish() -> void:
	## Persist measurable playback evidence; subjective listening and campaign progression are outside this harness.
	var file := FileAccess.open("res://docs/voice-coverage/playback.json", FileAccess.WRITE)
	file.store_string(JSON.stringify({"passed": not _failed, "observations": _observations}, "  ") + "\n")
	file.close()
	quit(1 if _failed else 0)
