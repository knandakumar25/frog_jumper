extends GdUnitTestSuite

const MENU = preload("res://scenes/main_menu/level_selection.tscn")


func test_campaign_maps_only_to_four_2d_missions() -> void:
	var menu = MENU.instantiate()
	assert_int(menu.level_map.size()).is_equal(5)
	for index in range(1, 5):
		var path = "res://scenes/gameplay/level%d.tscn" % index
		assert_str(menu.level_map[index]).is_equal(path)
		var mission = load(path).instantiate()
		assert_bool(mission.find_child("CyberShogun", true, false) is CharacterBody2D).is_true()
		mission.free()
	menu.free()


func test_sector_buttons_launch_the_matching_mission() -> void:
	var menu = MENU.instantiate()
	for index in range(1, 5):
		var button: Button = menu.find_level_button("Level%d" % index)
		assert_that(button).is_not_null()
		assert_bool(button.pressed.is_connected(
			Callable(menu, "_on_level_%d_pressed" % index))).is_true()
	menu.free()


func test_progression_enables_only_unlocked_missions() -> void:
	var menu = MENU.instantiate()
	var previous_unlocked = Global.unlocked_levels
	for unlocked in [1, 2, 4, 9]:
		Global.unlocked_levels = unlocked
		menu._update_level_buttons()
		for index in range(1, 5):
			var button: Button = menu.find_level_button("Level%d" % index)
			assert_bool(button.disabled).is_equal(index > unlocked)
	Global.unlocked_levels = previous_unlocked
	menu.free()


func test_legacy_training_index_redirects_to_first_mission() -> void:
	var menu = MENU.instantiate()
	assert_str(menu.level_map[0]).is_equal("res://scenes/gameplay/level1.tscn")
	menu.free()
