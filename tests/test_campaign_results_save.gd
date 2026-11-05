class_name TestCampaignResultsSave
extends GdUnitTestSuite
## Real results UI awaits settlement and main-thread completion before enabling forward actions.


func test_invalid_settlement_keeps_retry_and_menu_usable_without_advancing() -> void:
	## An invalid mission deliberately fails before database access, so no existing campaign can be touched.
	var card: Node = load("res://scenes/ui/cyber_victory_scorecard.tscn").instantiate()
	add_child(card)
	card.call("ShowMissionScorecard", 60, 2, "KAITO", 7)
	card.call("ConfigureCompletion", 13, 60, "00000000-0000-0000-0000-000000000001", 1)
	var box: Control = card.get_node("RootPanel/MainVBox")
	assert_str(box.get_node("SaveStatus").text).contains("SAVING")
	assert_bool(box.get_node("ContinueBtn").disabled).is_true()
	for frame in 8:
		await get_tree().process_frame
	assert_str(box.get_node("SaveStatus").text).contains("SAVE NOT CONFIRMED")
	assert_bool(box.get_node("RetrySave").visible).is_true()
	for path in ["ContinueBtn", "ButtonsHBox/ReplayBtn", "ButtonsHBox/LevelSelectBtn"]:
		assert_bool(box.get_node(path).disabled).is_true()
	assert_bool(box.get_node("ButtonsHBox/MainMenuBtn").disabled).is_false()
	box.get_node("RetrySave").emit_signal("pressed")
	for frame in 8:
		await get_tree().process_frame
	assert_bool(box.get_node("RetrySave").visible).is_true()
	assert_bool(box.get_node("ContinueBtn").disabled).is_true()
	card.free()
	await get_tree().process_frame


func test_direct_play_without_campaign_confirms_status_and_enables_continue() -> void:
	## No-active-campaign completion performs no save; restore the shared runtime unlock after this isolated UI check.
	var global: Node = get_node("/root/Global")
	var previous: int = global.get("unlocked_levels")
	var card: Node = load("res://scenes/ui/cyber_victory_scorecard.tscn").instantiate()
	add_child(card)
	card.call("ShowMissionScorecard", 60, 2, "KAITO", 7)
	card.call("ConfigureCompletion", 6, 60, "00000000-0000-0000-0000-000000000002", -1)
	for frame in 8:
		await get_tree().process_frame
	var box: Control = card.get_node("RootPanel/MainVBox")
	assert_str(box.get_node("SaveStatus").text).contains("NO ACTIVE CAMPAIGN")
	assert_bool(box.get_node("ContinueBtn").disabled).is_false()
	assert_bool(box.get_node("RetrySave").visible).is_false()
	card.free()
	await get_tree().process_frame
	global.set("unlocked_levels", previous)
