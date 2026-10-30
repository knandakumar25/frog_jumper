class_name TestTokyoRemembers
extends GdUnitTestSuite
## Extraction integration and terminal lifecycle tests. Every instantiated node is explicitly freed.

const OVERLAY := "res://scripts/ui/TokyoRemembers.cs"
const SCORECARD := "res://scenes/ui/cyber_victory_scorecard.tscn"


func _extract(mission: Node) -> void:
	## Complete the real beacon/parry/blade/extraction sequence without bypassing objective prerequisites.
	var player := mission.get_node("Actors/CyberShogun")
	var beacon := mission.get_node("Actors/DemolitionBeacon")
	player.global_position = beacon.global_position + Vector2(-200, -50)
	player.emit_signal("ParryDeflected", player)
	player.emit_signal("ParryDeflected", player)
	beacon.call("TakeDamage", 1000.0)
	player.global_position = mission.get_node("Actors/DataBlade").global_position + Vector2(0, -60)
	mission.call("CollectNearbyObjectives")
	player.global_position = mission.get_node("Actors/ExtractionGate").global_position + Vector2(0, -80)
	mission.call("CollectNearbyObjectives")


func test_extraction_freezes_mission_and_results_use_snapshot() -> void:
	## Extraction unlocks Level 2, freezes physics/input, snapshots results and cannot create duplicate overlays.
	var mission: Node = scene_runner("res://scenes/gameplay/level1.tscn").scene()
	var points := mission.get_node("GameManager")
	points.set("Points", 150)
	mission.get_node("Actors/CyberShogun").emit_signal("ComboChanged", 4)
	_extract(mission)
	await get_tree().process_frame
	var overlay := mission.get_node("TokyoRemembers")
	overlay.set_process(false)
	assert_bool(mission.get("MissionComplete")).is_true()
	assert_bool(mission.get_node("Actors/CyberShogun").can_process()).is_false()
	assert_bool(mission.get_node("CanvasLayer/PauseMenu").can_process()).is_false()
	assert_int(mission.get_node("/root/Global").get("unlocked_levels")).is_greater_equal(2)
	var position: Vector2 = mission.get_node("Actors/CyberShogun").position
	var health: float = mission.get_node("Actors/CyberShogun").get("CurrentHealth")
	for _frame in range(5):
		await get_tree().physics_frame
	assert_vector(mission.get_node("Actors/CyberShogun").position).is_equal(position)
	assert_int(points.get("Points")).is_equal(150)
	assert_float(mission.get_node("Actors/CyberShogun").get("CurrentHealth")).is_equal(health)
	# Deliberate external mutation proves the terminal uses captured values, not live state.
	points.set("Points", 999)
	mission.get_node("Actors/CyberShogun").emit_signal("ComboChanged", 20)
	mission.call("CollectNearbyObjectives")
	assert_int(mission.find_children("TokyoRemembers", "", false, false).size()).is_equal(1)
	overlay.call("Finish")
	overlay.call("Finish")
	var card := mission.get_node("CyberVictoryScorecard")
	assert_bool(card.get_node("RootPanel").visible).is_true()
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ScoreLabel").text).contains("150 PTS")
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ComboLabel").text).contains("4x STRIKE")
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/OperativeLabel").text).contains("KAITO")
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_true()
	mission.free()


func test_early_middle_late_and_natural_finish_emit_once() -> void:
	## Seeking is deterministic; missing speech still allows every completion path and stops ambience before results.
	for seconds in [0.1, 20.0, 39.8, 40.0]:
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
		if seconds == 40.0:
			overlay.call("_Process", 0.1)
		elif seconds < 1:
			var key := InputEventKey.new()
			key.pressed = true
			key.keycode = KEY_ESCAPE
			overlay.call("_Input", key)
		elif seconds < 30:
			var start := InputEventJoypadButton.new()
			start.pressed = true
			start.button_index = JOY_BUTTON_START
			overlay.call("_Input", start)
		else:
			overlay.get_node("UI/Skip").pressed.emit()
		overlay.call("Finish")
		assert_int(count[0]).is_equal(1)
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		overlay.free()


func test_missing_art_converges_on_results() -> void:
	## Missing essential artwork must not strand a completed mission.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("ArtPath", "res://missing-tokyo-art/")
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
	card.call("ShowMissionScorecard", 120, 3, "KAITO", 2)
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_true()
	assert_str(card.get_node("RootPanel/MainVBox/RankPanel/RankLabel").text).contains("S RANK")
	card.call("ShowScorecard", 49, 1, "KAITO")
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_false()
	assert_str(card.get_node("RootPanel/MainVBox/RankPanel/RankLabel").text).contains("B RANK")
	card.free()
