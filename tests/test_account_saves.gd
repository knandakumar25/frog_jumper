extends GdUnitTestSuite
## Account-linked saves (2026-11-18): SessionManager over the save store, and the main menu's
## sign-in gate. Uses an in-memory store with the server's commit rules, so no network, no
## database and no real account is touched.

var _sessions: Node


func before_test() -> void:
	_sessions = get_node("/root/SessionManager")
	_sessions.call("UseInMemoryStoreForTests")


func after_test() -> void:
	_sessions.set("CurrentSessionId", -1)
	_sessions.call("UseServiceStore")


## A slot round-trips across manager instances, and progress adds up without lowering the level.
func test_campaign_round_trip_across_managers() -> void:
	var script = load("res://scripts/managers/SessionManager.cs")
	var writer = script.new()
	var reader = script.new()
	var id: int = writer.create_new_session("store-integration")
	assert_int(id).is_greater(0)
	writer.update_session(id, 2, 40, 3)
	writer.update_session(id, 1, 10, 1)
	var saved: Dictionary = reader.get_session(id)
	assert_int(int(saved.get("current_level", 0))).is_equal(2)
	assert_int(int(saved.get("total_score", 0))).is_equal(50)
	assert_int(int(saved.get("collectables", 0))).is_equal(4)
	assert_int(writer.get_all_sessions().size()).is_equal(1)
	writer.delete_session(id)
	assert_dict(reader.get_session(id)).is_empty()
	writer.free()
	reader.free()


## Settings are not written anywhere while no account is signed in.
func test_settings_are_not_saved_without_an_account() -> void:
	assert_bool(_sessions.get("AccountReady")).is_false()
	assert_bool(_sessions.call("WriteState", "user_settings", {"display_mode": "windowed"})).is_false()
	assert_dict(_sessions.call("ReadState", "user_settings")).is_empty()


## No guest play: signed out, New Game and Squad are disabled, Continue is hidden, and a hint explains why.
func test_main_menu_requires_sign_in() -> void:
	var runner = scene_runner("res://scenes/main_menu/main_menu.tscn")
	var menu = runner.scene()
	await get_tree().process_frame
	assert_bool(menu.get_node("ButtonContainer/Play").get("IsDisabled")).is_true()
	assert_bool(menu.get_node("ButtonContainer/Roster").get("IsDisabled")).is_true()
	assert_bool(menu.get_node("ButtonContainer/ContinueButton").visible).is_false()
	var hint: Label = menu.get_node("ButtonContainer/SignInHint")
	assert_bool(hint.visible).is_true()
	assert_str(hint.text).is_not_empty()
	# Pressing New Game while signed out opens nothing.
	menu.call("_on_play_pressed")
	await get_tree().process_frame
	assert_bool(menu.get_node("NewGamePopup").visible).is_false()
