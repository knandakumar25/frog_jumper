class_name TestKyotoCity
extends GdUnitTestSuite
## Plays Kyoto through the real world scene in unsaved preview mode, and checks its side-scrolling mission loads.

var _previous_session: int
var _previous_progress: int
var _previous_operative: String


func _open_world() -> Node:
	## Opens the world with no campaign selected, so no player save can be touched.
	var sessions: Node = get_node("/root/SessionManager")
	var global: Node = get_node("/root/Global")
	_previous_session = sessions.get("CurrentSessionId")
	_previous_progress = global.get("unlocked_levels")
	_previous_operative = global.get("selected_character")
	sessions.set("CurrentSessionId", -1)
	global.set("unlocked_levels", 3)
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


func _press_interact(world: Node) -> void:
	## Sends one interact press through the same handler real input uses.
	var event := InputEventAction.new()
	event.action = "world_interact"
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


func test_skyport_flies_to_kyoto_and_back() -> void:
	var world := await _open_world()
	world.call("PreviewPlace", 44000.0, 44060.0)
	await _wait_ready(world)
	await _press_interact(world)
	assert_bool(world.call("InspectionState")["conversation_open"]).is_true()
	_button(world, "ConversationContinue").pressed.emit()
	await get_tree().process_frame
	var choice := _button(world, "Choice_kyoto")
	assert_object(choice).is_not_null()
	choice.pressed.emit()
	await _wait_ready(world)
	var state: Dictionary = world.call("InspectionState")
	assert_str(state["city"]).is_equal("kyoto")
	assert_float(state["x"]).is_equal_approx(20000.0, 1.0)
	assert_float(state["y"]).is_equal_approx(20060.0, 1.0)
	assert_int(int(state["resident"])).is_equal(9)
	assert_bool(world.call("PreviewTravel", "los-angeles")).is_true()
	await _wait_ready(world)
	assert_str(world.call("InspectionState")["city"]).is_equal("los-angeles")
	await _close_world(world)


func test_kyoto_quest_advances_and_progress_survives_a_round_trip() -> void:
	var world := await _open_world()
	world.call("PreviewTravel", "kyoto")
	await _wait_ready(world)
	world.call("PreviewPlace", 20140.0, 20110.0)
	await _wait_ready(world)
	await _press_interact(world)
	await _read_conversation(world)
	await get_tree().process_frame
	assert_int(world.call("ObjectiveIndex", "kq_gion_01")).is_equal(1)
	world.call("PreviewTravel", "los-angeles")
	await _wait_ready(world)
	assert_int(world.call("ObjectiveIndex", "kq_gion_01")).is_equal(0)
	world.call("PreviewTravel", "kyoto")
	await _wait_ready(world)
	assert_int(world.call("ObjectiveIndex", "kq_gion_01")).is_equal(1)
	await _close_world(world)


## Kyoto art awaits SpriteCook regeneration (docs/ART_REGENERATION.md): all 16 files or none.
func test_kyoto_art_is_complete_or_absent() -> void:
	var present := 0
	for index in 8:
		if ResourceLoader.exists("res://assets/world/kyoto/module_%d.png" % index):
			present += 1
		if ResourceLoader.exists("res://assets/world/kyoto/landmark_%d.png" % index):
			present += 1
	assert_bool(present == 0 or present == 16).is_true()


func test_kamo_locks_builds_without_touching_campaign_progress() -> void:
	var global: Node = get_node("/root/Global")
	var before: int = global.get("unlocked_levels")
	var mission: Node = load("res://scenes/gameplay/kyoto_kamo_locks.tscn").instantiate()
	add_child(mission)
	for frame in 5:
		await get_tree().process_frame
	assert_object(mission.find_child("LockBeacon", true, false)).is_not_null()
	assert_object(mission.find_child("ExtractionGate", true, false)).is_not_null()
	assert_object(mission.find_child("SluiceGate", true, false)).is_not_null()
	assert_bool(mission.get("BeaconDestroyed")).is_false()
	assert_int(global.get("unlocked_levels")).is_equal(before)
	mission.free()
	await get_tree().process_frame
