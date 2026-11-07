class_name TestWorldExploration
extends GdUnitTestSuite
## Exercises the actual exploration scene in unsaved preview mode and frees all instantiated nodes.


func test_world_streams_nine_chunks_and_map_blocks_movement() -> void:
	## The preview never creates a campaign or touches player saves; restore autoload selection after inspection.
	var sessions: Node = get_node("/root/SessionManager")
	var previous: int = sessions.get("CurrentSessionId")
	var global: Node = get_node("/root/Global")
	var previous_progress: int = global.get("unlocked_levels")
	var previous_operative: String = global.get("selected_character")
	sessions.set("CurrentSessionId", -1)
	global.set("unlocked_levels", 12)
	var world: Node = load("res://scenes/world/neo_kyoto.tscn").instantiate()
	add_child(world)
	for frame in 120:
		await get_tree().process_frame
		if int(world.get("ResidentChunks")) == 9:
			break
	assert_int(world.get("ResidentChunks")).is_equal(9)
	assert_int(world.get("CachedChunks")).is_equal(25)
	var player: Node = world.get_node("ExplorationPlayer")
	await get_tree().process_frame
	assert_bool(player.get("MovementEnabled")).is_true()
	# The Blender atlas rotates its south-facing rig in the opposite row order to screen-space angles.
	for pair in [["world_right", 6], ["world_left", 2], ["world_up", 4], ["world_down", 0]]:
		Input.action_press(pair[0])
		await get_tree().physics_frame
		await get_tree().process_frame
		Input.action_release(pair[0])
		assert_int(player.get("Facing")).is_equal(pair[1])
	var event := InputEventAction.new()
	event.action = "world_map"
	event.pressed = true
	world.call("_UnhandledInput", event)
	await get_tree().process_frame
	assert_bool(player.get("MovementEnabled")).is_false()
	var lists := world.find_children("*", "ItemList", true, false)
	assert_int(lists.size()).is_equal(1)
	assert_int(lists[0].item_count).is_equal(316)
	world.free()
	await get_tree().process_frame
	sessions.set("CurrentSessionId", previous)
	global.set("unlocked_levels", previous_progress)
	global.set("selected_character", previous_operative)
	var bridge: RefCounted = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	bridge.call("PrepareMission", previous_progress)


func test_all_mission_results_expose_extraction_including_finale() -> void:
	## Results preserve Replay/Menu and the shared extraction action regardless of act or finale presentation.
	var card: Node = load("res://scenes/ui/cyber_victory_scorecard.tscn").instantiate()
	card.call("_Ready")
	for method in ["ShowScorecard", "ShowActScorecard", "ShowActIIScorecard", "ShowCampaignCompleteScorecard"]:
		card.call(method, 150, 4, "Cyber Shogun")
		var box: Node = card.get_node("RootPanel/MainVBox")
		assert_bool(box.get_node("ContinueBtn").visible).is_true()
		assert_str(box.get_node("ContinueBtn").text).is_equal(tr("Return to Neo-Los Angeles"))
		assert_bool(box.get_node("PreviewBtn").visible).is_false()
		assert_bool(box.get_node("ButtonsHBox/ReplayBtn").visible).is_true()
	card.free()
