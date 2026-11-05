class_name TestNamesWithoutNumbers
extends GdUnitTestSuite
## Mission 5 runtime contracts: deployment, protected terminals, checkpoint recovery, pause and final results.

const SCENE := "res://scenes/gameplay/level5.tscn"


func _mission() -> Node:
	## Each test owns a fresh scene, with no saved session and no shared mission ledger.
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func _advance_to_core(mission: Node) -> void:
	## Simulate designated defeat notifications and completed operative devices before testing the core lifecycle.
	for id in ["service1", "service2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "phase")
	for id in ["hall1", "hall2", "hall3"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "signal")
	for id in ["utility1", "utility2", "utility3"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "coolant")
	mission.call("CommitObjective", "maintenance")
	for record in range(1, 4):
		for suffix in ["a", "b"]:
			mission.call("CompleteCombatEvent", "encounter:archive%d%s" % [record, suffix])
		mission.call("CommitObjective", "record:%d" % record)


func test_surya_cannot_be_fielded_and_records_have_no_hurtbox() -> void:
	## Both player direct swaps and the visible squad strip must respect mission deployment.
	var mission := _mission()
	var player: Node = mission.get_node("Actors/CyberShogun")
	assert_bool(player.call("SwapOperative", "Surya Prime")).is_false()
	for index in range(1, 4):
		var terminal: Node = mission.get_node("Actors/Terminalrecord_%d" % index)
		assert_bool(terminal.has_method("TakeDamage")).is_false()
		assert_bool(terminal.call("TryInteract")).is_false()
	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("ActHeading").text).contains("ACT II")
	assert_str(title_card.get_node("MissionHeading").text).contains("NAMES WITHOUT NUMBERS")
	mission.free()


func test_context_terminals_require_proximity_and_completed_defenders() -> void:
	## An authenticated terminal accepts once in range; remote and premature requests never advance recovery.
	var mission := _mission()
	var terminal: Node2D = mission.get_node("Actors/Terminalisolate")
	assert_bool(terminal.call("TryInteract")).is_false()
	_advance_to_core(mission)
	assert_bool(terminal.call("TryInteract")).is_false()
	var player: Node2D = mission.get_node("Actors/CyberShogun")
	player.position = terminal.position + Vector2(0, -46)
	assert_bool(terminal.call("TryInteract")).is_true()
	assert_bool(terminal.call("TryInteract")).is_false()
	assert_bool(mission.call("CommitObjective", "defense:2")).is_false()
	assert_bool(mission.call("CommitObjective", "extract")).is_false()
	mission.free()


func test_pointer_and_touch_operate_only_a_ready_nearby_console() -> void:
	## Synthetic pointer events follow the real terminal hit region; distant or locked consoles cannot be tapped.
	for use_touch in [false, true]:
		var mission := _mission()
		_advance_to_core(mission)
		var terminal: Node2D = mission.get_node("Actors/Terminalisolate")
		var point := terminal.get_global_transform_with_canvas() * Vector2(0, -60)
		var input: InputEvent
		if use_touch:
			var touch := InputEventScreenTouch.new()
			touch.pressed = true
			touch.position = point
			input = touch
		else:
			var mouse := InputEventMouseButton.new()
			mouse.pressed = true
			mouse.button_index = MOUSE_BUTTON_LEFT
			mouse.position = point
			input = mouse
		terminal.call("_Input", input)
		assert_bool(mission.call("HasObjective", "isolate")).is_false()
		mission.get_node("Actors/CyberShogun").position = terminal.position + Vector2(0, -46)
		terminal.call("_Input", input)
		terminal.call("_Input", input)
		assert_bool(mission.call("HasObjective", "isolate")).is_true()
		mission.free()


func test_each_defense_stage_resets_incomplete_attempt_and_retains_prior_stages() -> void:
	## Defeat recovery clears transient actors and rebuilds only the interrupted finite stage, preserving completed records.
	for stage in range(1, 4):
		var mission := _mission()
		_advance_to_core(mission)
		var player: Node2D = mission.get_node("Actors/CyberShogun")
		player.position = Vector2(19800, 554)
		mission.call("_PhysicsProcess", 0.016)
		mission.call("CommitObjective", "isolate")
		for previous in range(1, stage):
			mission.call("CompleteCombatEvent", "encounter:core%d" % previous)
			mission.call("CommitObjective", "defense:%d" % previous)
		mission.call("_PhysicsProcess", 0.016)
		var formation: Node = mission.get_node("Actors/Formationcore%d" % stage)
		assert_bool(formation.get("Active")).is_true()
		player.call("TakeDamage", 99999.0, Vector2.ZERO)
		await await_millis(1200)
		assert_bool(mission.call("HasObjective", "record:3")).is_true()
		for previous in range(1, stage):
			assert_bool(mission.call("HasObjective", "defense:%d" % previous)).is_true()
		assert_bool(mission.call("HasObjective", "defense:%d" % stage)).is_false()
		assert_vector(player.position).is_equal_approx(Vector2(19800, 550), Vector2(8, 10))
		assert_int(formation.get("LivingMembers")).is_equal(3)
		mission.free()


func test_archive_defenders_spawn_above_solid_stairs_and_galleries() -> void:
	## Actual enemy bodies must stay outside structural collision so real melee can reach every mandatory defender.
	var mission := _mission()
	for id in ["service1", "service2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "phase")
	for id in ["hall1", "hall2", "hall3"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "signal")
	for id in ["utility1", "utility2", "utility3"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "coolant")
	mission.call("CommitObjective", "maintenance")
	for record in range(1, 4):
		for suffix in ["a", "b"]:
			var formation: Node = mission.get_node("Actors/Formationarchive%d%s" % [record, suffix])
			formation.call("Activate")
			assert_int(formation.get("LivingMembers")).is_equal(3)
			for enemy in formation.get_children():
				for platform in mission.get_node("PalaceGeometry").get_children():
					if not platform is StaticBody2D or platform.get("OneWay") == true:
						continue
					var bounds := Rect2(platform.position, platform.get("Size"))
					assert_bool(bounds.has_point(enemy.global_position + Vector2(0, -1))).is_false()
				enemy.call("TakeDamage", 1000.0)
		assert_bool(mission.call("CommitObjective", "record:%d" % record)).is_true()
	mission.free()


func test_pause_blocks_context_and_mission_progress() -> void:
	## Paused gameplay cannot consume fresh context input or advance a recovery stage.
	var mission := _mission()
	get_tree().paused = true
	assert_bool(mission.call("CommitObjective", "phase")).is_false()
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(mission.call("HasObjective", "phase")).is_false()
	get_tree().paused = false
	mission.free()


func test_mission_five_epilogue_precedes_results_without_new_progression() -> void:
	## Extraction freezes the mission and captures its score; the epilogue hands off once to the existing terminal card.
	var mission := _mission()
	_advance_to_core(mission)
	mission.call("CommitObjective", "isolate")
	for stage in range(1, 4):
		mission.call("CompleteCombatEvent", "encounter:core%d" % stage)
		mission.call("CommitObjective", "defense:%d" % stage)
	assert_bool(mission.call("CommitObjective", "commit")).is_true()
	assert_bool(mission.call("CommitObjective", "extract")).is_true()
	assert_bool(mission.call("CommitObjective", "extract")).is_false()
	await get_tree().process_frame
	await get_tree().process_frame
	var overlay: Node = mission.get_node("NamesWithoutNumbersAftermath")
	assert_bool(mission.get_node("Actors/CyberShogun").can_process()).is_false()
	assert_bool(mission.get_node("CyberVictoryScorecard/RootPanel").visible).is_false()
	var captured_points: int = mission.get_node("GameManager").get("Points")
	overlay.call("Finish")
	await get_tree().process_frame
	await get_tree().process_frame
	var card: Node = mission.get_node("CyberVictoryScorecard/RootPanel/MainVBox")
	assert_bool(mission.get_node("CyberVictoryScorecard/RootPanel").visible).is_true()
	assert_str(card.get_node("TitleLabel").text).is_equal("MISSION COMPLETE")
	assert_str(card.get_node("StatsGrid/ScoreLabel").text).is_equal("FINAL SCORE: %d PTS" % captured_points)
	assert_bool(card.get_node("ContinueBtn").visible).is_true()
	assert_str(card.get_node("ContinueBtn").text).contains("6")
	assert_bool(card.get_node("PreviewBtn").visible).is_false()
	assert_bool(card.get_node("ButtonsHBox/ReplayBtn").visible).is_true()
	assert_bool(card.get_node("ButtonsHBox/LevelSelectBtn").visible).is_true()
	assert_bool(card.get_node("ButtonsHBox/MainMenuBtn").visible).is_true()
	assert_str(mission.get_node("PalaceGeometry/AccessStatus").text).contains("ACCEPTED")
	mission.free()
