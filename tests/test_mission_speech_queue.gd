class_name TestMissionSpeechQueue
extends GdUnitTestSuite
## Production mission integration: FIFO ownership, duplicate events, localized fallback and lifecycle cancellation.


func _mission(number: int = 5) -> Node:
	## Each test owns a fresh mission and finishes only its arrival overlay; no campaign progress is saved.
	var mission: Node = load("res://scenes/gameplay/level%d.tscn" % number).instantiate()
	add_child(mission)
	for child in mission.get_children():
		if child.has_method("Finish") and child.name != "CyberVictoryScorecard":
			child.call("Finish")
	mission.get_node("MissionSpeechQueue").call("Cancel")
	return mission


func test_fifo_duplicate_and_status_ownership() -> void:
	## Three testimonies retain trigger order and duplicate pickups or status updates cannot replace them.
	var mission := _mission()
	var queue: Node = mission.get_node("MissionSpeechQueue")
	for id in ["m05.fragment1", "m05.fragment2", "m05.fragment3", "m05.fragment2"]:
		mission.call("PlayStoryCue", id)
	assert_int(queue.get("PendingCount")).is_equal(3)
	queue.call("_Process", 0.0)
	assert_str(queue.get("CurrentCueId")).is_equal("m05.fragment1")
	mission.call("ShowDialogue", "Ordinary status", 1.0)
	assert_str(queue.get("CurrentCueId")).is_equal("m05.fragment1")
	var player: AudioStreamPlayer = queue.get_node("EnglishSpeech")
	player.stop()
	player.finished.emit()
	queue.call("_Process", 0.0)
	assert_str(queue.get("CurrentCueId")).is_equal("m05.fragment2")
	mission.free()


func test_pause_preserves_playback_and_cancel_clears_pending() -> void:
	## The Always-processing owner explicitly freezes audio with gameplay and cancels stale queued events.
	var mission := _mission()
	var queue: Node = mission.get_node("MissionSpeechQueue")
	mission.call("PlayStoryCue", "m05.fragment1")
	mission.call("PlayStoryCue", "m05.fragment2")
	queue.call("_Process", 0.0)
	get_tree().paused = true
	queue.call("_Process", 0.1)
	var player: AudioStreamPlayer = queue.get_node("EnglishSpeech")
	assert_bool(player.stream_paused).is_true()
	assert_str(queue.get("CurrentCueId")).is_equal("m05.fragment1")
	get_tree().paused = false
	queue.call("_Process", 0.0)
	assert_bool(player.stream_paused).is_false()
	queue.call("Cancel")
	assert_bool(player.playing).is_false()
	assert_int(queue.get("PendingCount")).is_equal(0)
	assert_str(queue.get("CurrentCueId")).is_empty()
	mission.free()


func test_missing_audio_keeps_full_subtitle_and_progresses() -> void:
	## Missing resources use bounded reading time; neither objectives nor queue advancement depend on an imported clip.
	var mission := _mission()
	var queue: Node = mission.get_node("MissionSpeechQueue")
	queue.set("AudioDirectoryOverride", "res://missing-speech")
	mission.call("PlayStoryCue", "m05.fragment1")
	mission.call("PlayStoryCue", "m05.fragment2")
	queue.call("_Process", 0.0)
	assert_bool(queue.get_node("EnglishSpeech").playing).is_false()
	assert_str(queue.get("CurrentCueId")).is_equal("m05.fragment1")
	queue.call("_Process", 60.0)
	assert_str(queue.get("CurrentCueId")).is_equal("m05.fragment2")
	mission.free()


func test_existing_scheduler_uses_only_shared_player() -> void:
	## Legacy Mission 4/5 adapters never allocate a second speech lane.
	for number in [4, 5]:
		var mission := _mission(number)
		var adapter: Node = mission.get_node("AegisDialogue" if number == 4 else "NamesDialogue")
		assert_bool(adapter.has_node("Speech")).is_false()
		adapter.call("Enqueue", "practice" if number == 4 else "sector0")
		assert_bool(mission.get_node("MissionSpeechQueue").get("Busy")).is_true()
		mission.free()


func test_later_collectible_triggers_shared_english_speech() -> void:
	## Actual collectible callbacks enqueue the correct stable log cue across all later mission scripts.
	for number in range(6, 13):
		var mission := _mission(number)
		var actors := mission.get_node("Actors")
		var player: Node2D = actors.get_node("CyberShogun")
		var fragment: Node2D = mission.find_child("MemoryFragment*", true, false)
		if fragment == null:
			for child in actors.get_children():
				if "Fragment" in str(child.name):
					fragment = child
					break
		assert_object(fragment).is_not_null()
		if fragment != null:
			player.global_position = fragment.global_position + Vector2(0, -40)
			mission.call("CollectFragments")
			var queue: Node = mission.get_node("MissionSpeechQueue")
			queue.call("_Process", 0.0)
			assert_str(queue.get("CurrentCueId")).is_equal("m%02d.fragment1" % number)
		mission.free()


func test_production_fall_recovery_cancels_active_and_pending_speech() -> void:
	## The real chasm recovery callback silences stale story beats before restoring the player checkpoint.
	var mission := _mission()
	var queue: Node = mission.get_node("MissionSpeechQueue")
	mission.call("PlayStoryCue", "m05.fragment1")
	mission.call("PlayStoryCue", "m05.fragment2")
	queue.call("_Process", 0.0)
	var player: Node2D = mission.get_node("Actors/CyberShogun")
	player.position.y = 3000
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(queue.get("Busy")).is_false()
	assert_bool(queue.get_node("EnglishSpeech").playing).is_false()
	assert_float(player.position.y).is_less(3000.0)
	var owner: WeakRef = weakref(queue)
	mission.free()
	assert_object(owner.get_ref()).is_null()
