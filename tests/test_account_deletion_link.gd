extends GdUnitTestSuite
## Account deletion in the game (2026-11-18): the menu's "Delete account" button sits in the
## signed-in profile row (hidden for guests) and opens the website's deletion page.

const MENU_AUTH := "res://scripts/main_menu/MenuAuthUI.cs"


func test_delete_button_lives_in_the_signed_in_profile_row() -> void:
	var holder := Control.new()
	add_child(holder)
	var ui: Node = load(MENU_AUTH).new()
	holder.add_child(ui)
	ui.call("Initialize", holder)
	var button: Node = holder.find_child("DeleteAccountButton", true, false)
	assert_object(button).is_not_null()
	# The profile row is shown only while a game account is signed in; guests never see the button.
	var row: Control = button.get_parent()
	if get_node("/root/AuthManager").get("CurrentUser") == null:
		assert_bool(row.visible).is_false()
	holder.free()


func test_delete_page_address_follows_the_game_language() -> void:
	var locale: Node = get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	locale.call("ApplyLocale", "german")
	var ui: Node = load(MENU_AUTH).new()
	assert_str(ui.call("GetDeleteAccountUrl")).ends_with("/de/account/delete")
	locale.call("ApplyLocale", "traditionalchinese")
	assert_str(ui.call("GetDeleteAccountUrl")).ends_with("/zh_tw/account/delete")
	locale.call("ApplyLocale", previous)
	ui.free()
