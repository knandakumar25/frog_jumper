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


func test_real_cue_plays_on_voice_bus_with_its_table_gain() -> void:
	## End to end: the path the catalog builds must hit the gain table, or every line silently gets 0 dB.
	var mission := _mission()
	var queue: Node = mission.get_node("MissionSpeechQueue")
	mission.call("PlayStoryCue", "m05.fragment1")
	queue.call("_Process", 0.0)
	var player: AudioStreamPlayer = queue.get_node("EnglishSpeech")
	var table: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/audio/voice_gain.json"))
	var gain: float = table["files"]["res://assets/audio/english/m05.fragment1.mp3"]["gain_db"]
	# A nonzero gain is needed to tell a table hit from the 0 dB fallback.
	assert_float(absf(gain)).is_greater(0.01)
	assert_str(String(player.bus)).is_equal("Voice")
	assert_float(player.volume_db).is_equal_approx(-2.0 + gain, 0.01)
	queue.call("Cancel")
	mission.free()


func test_mission_music_plays_on_music_bus_with_its_table_gain() -> void:
	## Mission 4 uses the default track, mission 5 its own atmosphere (which used to force -21 dB);
	## both must take the measured level from the music table instead of the old +24 dB on Master.
	var table: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/audio/music_gain.json"))
	for number in [4, 5]:
		var mission := _mission(number)
		var music: AudioStreamPlayer = mission.get_node("AudioStreamPlayer")
		var gain: float = table["files"][music.stream.resource_path]["gain_db"]
		assert_str(String(music.bus)).is_equal("Music")
		assert_float(music.volume_db).is_equal_approx(gain, 0.01)
		assert_float(music.volume_db).is_less(0.0)
		mission.free()


func test_every_mission_scene_track_has_a_music_gain() -> void:
	## Reads each scene's real MusicPath (not a regex over the sources), so a track written in any
	## form must be in the music table; otherwise it would play at its raw level.
	var table: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/audio/music_gain.json"))
	var scenes: Array[String] = []
	for number in range(1, 13):
		scenes.append("res://scenes/gameplay/level%d.tscn" % number)
	for city in ["beirut_broadcast_tower", "dubai_salt_plant", "honolulu_pumping_station", "kyoto_kamo_locks",
			"luanda_power_station", "mumbai_viaduct_grid"]:
		scenes.append("res://scenes/gameplay/%s.tscn" % city)
	for path in scenes:
		var scene: Node = load(path).instantiate()
		var track := String(scene.get("MusicPath"))
		assert_str(track).is_not_empty()
		assert_bool((table["files"] as Dictionary).has(track)).override_failure_message("%s: %s has no gain" % [path, track]).is_true()
		scene.free()
