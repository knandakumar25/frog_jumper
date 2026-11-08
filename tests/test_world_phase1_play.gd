class_name TestWorldPhase1Play
extends GdUnitTestSuite
## Plays the open-world quest slice through the real scene and input handler, in unsaved preview mode.

var _previous_session: int
var _previous_progress: int
var _previous_operative: String


func _open_world(progress: int = 12) -> Node:
	## Opens the world with no campaign selected, so no player save can be touched.
	var sessions: Node = get_node("/root/SessionManager")
	var global: Node = get_node("/root/Global")
	_previous_session = sessions.get("CurrentSessionId")
	_previous_progress = global.get("unlocked_levels")
	_previous_operative = global.get("selected_character")
	sessions.set("CurrentSessionId", -1)
	global.set("unlocked_levels", progress)
	var world: Node = load("res://scenes/world/neo_kyoto.tscn").instantiate()
	add_child(world)
	await _wait_ready(world)
	return world


func _close_world(world: Node) -> void:
	## Frees the scene and restores the global selection for the next suite.
	world.free()
	await get_tree().process_frame
	get_node("/root/SessionManager").set("CurrentSessionId", _previous_session)
	var global: Node = get_node("/root/Global")
	global.set("unlocked_levels", _previous_progress)
	global.set("selected_character", _previous_operative)
	load("res://scripts/managers/LevelCatalogBridge.cs").new().call("PrepareMission", _previous_progress)


func _wait_ready(world: Node) -> void:
	## Waits until all nine chunks around the player exist and movement is enabled.
	for frame in 240:
		await get_tree().process_frame
		if int(world.get("ResidentChunks")) == 9 and world.get_node("ExplorationPlayer").get("MovementEnabled"):
			return


func _place(world: Node, x: float, y: float) -> void:
	## Moves the player using the preview-only hook and lets the area stream in.
	world.call("PreviewPlace", x, y)
	await _wait_ready(world)


func _press(world: Node, action: String) -> void:
	## Sends one press through the same handler real input uses.
	var event := InputEventAction.new()
	event.action = action
	event.pressed = true
	world.call("_UnhandledInput", event)
	await get_tree().process_frame


func _button(world: Node, node_name: String) -> Button:
	## Finds a live conversation button by its stable name.
	var found := world.find_children(node_name, "Button", true, false)
	return null if found.is_empty() else found[0]


func _read_conversation(world: Node) -> void:
	## Clicks Continue until the current conversation ends.
	for step in 12:
		var next := _button(world, "ConversationContinue")
		if next == null or not world.call("InspectionState")["conversation_open"]:
			return
		next.pressed.emit()
		await get_tree().process_frame


func _face(world: Node, target_x: float, target_y: float) -> void:
	## Turns the player toward a point with real movement actions. Four directions cover the 90-degree arc.
	var state: Dictionary = world.call("InspectionState")
	var dx: float = target_x - float(state["x"])
	var dy: float = target_y - float(state["y"])
	var action := ""
	if absf(dx) > absf(dy):
		action = "world_right" if dx > 0 else "world_left"
	else:
		action = "world_down" if dy > 0 else "world_up"
	Input.action_press(action)
	await get_tree().physics_frame
	await get_tree().process_frame
	Input.action_release(action)


func _fight_wave(world: Node) -> void:
	## Attacks the nearest drone with real input until none remain.
	var encounters: Node = world.get_node("WorldEncounters")
	for swing in 900:
		var live := encounters.get_children().filter(func(n): return n is Node2D and not n.is_queued_for_deletion())
		if live.is_empty():
			return
		var state: Dictionary = world.call("InspectionState")
		live.sort_custom(func(a, b):
			return Vector2(a.get("XMetres") - state["x"], a.get("YMetres") - state["y"]).length_squared() < Vector2(b.get("XMetres") - state["x"], b.get("YMetres") - state["y"]).length_squared())
		await _face(world, live[0].get("XMetres"), live[0].get("YMetres"))
		await _press(world, "world_attack")
		await get_tree().physics_frame


func test_resident_step_completes_by_day() -> void:
	## Talking to the target resident by day advances the quest; the HUD names the objective.
	var world: Node = await _open_world()
	await _place(world, 43820.0, 44210.0)
	await _press(world, "world_interact")
	assert_bool(world.call("InspectionState")["conversation_open"]).is_true()
	await _read_conversation(world)
	assert_int(world.call("ObjectiveIndex", "quest_ok_01")).is_equal(1)
	await _close_world(world)


func test_resident_is_found_at_evening_position() -> void:
	## The same objective follows the resident's schedule to the evening location.
	var world: Node = await _open_world()
	world.call("PreviewSetWorldMinutes", 30.0)
	await _place(world, 44025.0, 44018.0)
	await _press(world, "world_interact")
	await _read_conversation(world)
	assert_int(world.call("ObjectiveIndex", "quest_ok_01")).is_equal(1)
	await _close_world(world)


func test_leaving_a_conversation_changes_nothing() -> void:
	## Cancelling keeps the objective, and the panel closes.
	var world: Node = await _open_world()
	await _place(world, 43820.0, 44210.0)
	await _press(world, "world_interact")
	_button(world, "ConversationCancel").pressed.emit()
	await get_tree().process_frame
	assert_bool(world.call("InspectionState")["conversation_open"]).is_false()
	assert_int(world.call("ObjectiveIndex", "quest_ok_01")).is_equal(0)
	await _close_world(world)


func test_resident_and_safehouse_are_both_reachable() -> void:
	## With no open quest, a resident at a door still offers the building, which heals.
	var world: Node = await _open_world(1)
	world.call("PreviewSetWorldMinutes", 30.0)
	await _place(world, 44025.0, 44018.0)
	await _press(world, "world_interact")
	assert_bool(world.call("InspectionState")["conversation_open"]).is_true()
	_button(world, "ConversationExtra").pressed.emit()
	await get_tree().process_frame
	assert_bool(world.call("InspectionState")["conversation_open"]).is_false()
	assert_str(str(world.call("InspectionState")["last_toast"])).contains("Lantern Hearth")
	await _close_world(world)


func test_branch_choice_is_picked_by_button_and_completes_quest() -> void:
	## The real choice buttons set the outcome; the quest finishes on the last step.
	var world: Node = await _open_world()
	world.call("PreviewCompleteQuest", "quest_ok_01")
	world.call("PreviewCompleteQuest", "quest_ok_02")
	await _place(world, 44150.0, 44350.0)
	await _press(world, "world_interact")
	await _read_conversation(world)
	assert_int(world.call("ObjectiveIndex", "quest_ok_03")).is_equal(1)
	await _press(world, "world_interact")
	_button(world, "ConversationContinue").pressed.emit()
	await get_tree().process_frame
	assert_object(_button(world, "Choice_choice_wall_defenses")).is_not_null()
	_button(world, "Choice_choice_artisan_guild").pressed.emit()
	await get_tree().process_frame
	assert_int(world.call("ObjectiveIndex", "quest_ok_03")).is_equal(2)
	await _press(world, "world_interact")
	await _read_conversation(world)
	assert_bool(world.call("IsQuestComplete", "quest_ok_03")).is_true()
	await _close_world(world)


func test_all_six_operatives_can_destroy_a_drone() -> void:
	## Each operative attacks with its own damage value through the real input.
	var world: Node = await _open_world()
	await _place(world, 43600.0, 44000.0)
	for operative in 6:
		var state: Dictionary = world.call("InspectionState")
		assert_bool(world.call("PreviewSpawnHostile", float(state["x"]), float(state["y"]) + 2.0)).is_true()
		await get_tree().process_frame
		for swing in 8:
			if world.call("InspectionState")["live_hostiles"] == 0:
				break
			await _press(world, "world_attack")
			for wait in 32:
				await get_tree().physics_frame
		assert_int(world.call("InspectionState")["live_hostiles"]).is_equal(0)
		await _press(world, "world_swap")
	await _close_world(world)


func test_combat_objective_counts_real_kills() -> void:
	## Walking into the objective area starts a wave; clearing it advances the quest.
	var world: Node = await _open_world()
	await _place(world, 43820.0, 44210.0)
	await _press(world, "world_interact")
	await _read_conversation(world)
	await _place(world, 43860.0, 44240.0)
	await _press(world, "world_interact")
	await _read_conversation(world)
	assert_int(world.call("ObjectiveIndex", "quest_ok_01")).is_equal(2)
	await _place(world, 44200.0, 43950.0)
	for frame in 5:
		await get_tree().process_frame
	assert_int(world.call("InspectionState")["live_hostiles"]).is_equal(3)
	await _fight_wave(world)
	assert_bool(world.call("IsQuestComplete", "quest_ok_01")).is_true()
	await _close_world(world)


func test_defeat_returns_to_safe_arrival_and_keeps_progress() -> void:
	## Lethal damage ends the encounter and moves the player, without losing quest steps.
	var world: Node = await _open_world()
	await _place(world, 43820.0, 44210.0)
	await _press(world, "world_interact")
	await _read_conversation(world)
	await _place(world, 44200.0, 43950.0)
	world.get_node("ExplorationPlayer").call("TakeDamage", 99999.0)
	await _wait_ready(world)
	var state: Dictionary = world.call("InspectionState")
	assert_float(Vector2(float(state["x"]) - 44200.0, float(state["y"]) - 43950.0).length()).is_greater(50.0)
	assert_int(state["live_hostiles"]).is_equal(0)
	assert_float(float(state["health"])).is_greater(0.0)
	assert_int(world.call("ObjectiveIndex", "quest_ok_01")).is_equal(1)
	await _close_world(world)


func test_pursuit_blocks_transit_until_escape() -> void:
	## An engaged drone sets Pursuit and disables travel; leaving the area clears both.
	var world: Node = await _open_world()
	var lists := world.find_children("*", "ItemList", true, false)
	lists[0].select(lists[0].item_count - 1)
	var transit: Button = null
	for button in world.find_children("*", "Button", true, false):
		if button.text == tr("Free transit"):
			transit = button
	await _place(world, 43600.0, 44000.0)
	await get_tree().process_frame
	assert_bool(transit.disabled).is_false()
	var state: Dictionary = world.call("InspectionState")
	world.call("PreviewSpawnHostile", float(state["x"]) + 3.0, float(state["y"]))
	await get_tree().process_frame
	await get_tree().process_frame
	assert_str(str(world.call("InspectionState")["alert_state"])).is_equal("Pursuit")
	assert_bool(transit.disabled).is_true()
	await _place(world, 43400.0, 44000.0)
	assert_str(str(world.call("InspectionState")["alert_state"])).is_equal("None")
	assert_bool(transit.disabled).is_false()
	await _close_world(world)


func test_ninth_hostile_is_refused_by_the_budget() -> void:
	## The shared budget caps live drones at eight.
	var world: Node = await _open_world()
	for index in 8:
		assert_bool(world.call("PreviewSpawnHostile", 43000.0 + index, 43000.0)).is_true()
	assert_bool(world.call("PreviewSpawnHostile", 43010.0, 43000.0)).is_false()
	await _close_world(world)
