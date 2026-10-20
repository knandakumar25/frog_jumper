extends GdUnitTestSuite


func test_empty_history_returns_keyboard_focus_to_play() -> void:
	var runner = scene_runner("res://scenes/main_menu/main_menu.tscn")
	var menu = runner.scene()
	await get_tree().process_frame
	var confirm = menu.get_node("ConfirmPopup")
	confirm.show()
	confirm.get_node("VBoxContainer/ButtonContainer/YesButton").grab_focus()
	# Exercise the shared completion path without deleting real player saves.
	menu.get_node("MenuSessionUI").call("CloseEmptyHistory")
	await get_tree().process_frame
	var play: Control = menu.get_node("ButtonContainer/Play")
	assert_bool(confirm.visible).is_false()
	assert_bool(menu.get_node("ContinuePopup").visible).is_false()
	assert_bool(menu.get_node("ButtonContainer/ContinueButton").visible).is_false()
	assert_that(menu.get_viewport().gui_get_focus_owner()).is_equal(play)
	assert_that(play.find_valid_focus_neighbor(SIDE_BOTTOM)).is_not_null()


func test_cancel_deletion_focuses_custom_session_button() -> void:
	var runner = scene_runner("res://scenes/main_menu/main_menu.tscn")
	var menu = runner.scene()
	await get_tree().process_frame
	var sessions = menu.get_node("ContinuePopup/VBoxContainer/ScrollContainer/SessionsContainer")
	var row = HBoxContainer.new()
	var button = load("res://scripts/ui/CyberButton.cs").new()
	row.add_child(button)
	sessions.add_child(row)
	menu.get_node("ConfirmPopup").show()
	menu.get_node("MenuSessionUI").call("CancelAction")
	await get_tree().process_frame
	assert_that(menu.get_viewport().gui_get_focus_owner()).is_equal(button)


func test_empty_session_list_focuses_back_button() -> void:
	var runner = scene_runner("res://scenes/main_menu/main_menu.tscn")
	var menu = runner.scene()
	await get_tree().process_frame
	menu.get_node("MenuSessionUI").call("CancelAction")
	await get_tree().process_frame
	assert_that(menu.get_viewport().gui_get_focus_owner()).is_equal(
		menu.get_node("ContinuePopup/VBoxContainer/BackButton"))
