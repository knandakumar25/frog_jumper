extends GdUnitTestSuite

const MENU = preload("res://scenes/main_menu/level_selection.tscn")


func test_campaign_maps_to_five_2d_missions() -> void:
	var menu = MENU.instantiate()
	assert_int(menu.catalog.Count()).is_equal(9)
	for index in range(1, 8):
		var path = "res://scenes/gameplay/level%d.tscn" % index
		assert_str(menu.catalog.ScenePath(index)).is_equal(path)
		var mission = load(path).instantiate()
		add_child(mission)
		await get_tree().process_frame
		assert_bool(mission.find_child("CyberShogun", true, false) is CharacterBody2D).is_true()
		mission.free()
	menu.free()


func test_sector_buttons_launch_the_matching_mission() -> void:
	var menu = MENU.instantiate()
	for index in range(1, 8):
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
		for index in range(1, 8):
			var button: Button = menu.find_level_button("Level%d" % index)
			assert_bool(button.disabled).is_equal(index > unlocked)
	Global.unlocked_levels = previous_unlocked
	menu.free()


## The tutorial level is gone: there is no replay button and no index-0 mission.
func test_training_level_is_removed() -> void:
	var menu = MENU.instantiate()
	assert_that(menu.find_level_button("Level0")).is_null()
	assert_bool(ResourceLoader.exists("res://scenes/gameplay/training_protocol.tscn")).is_false()
	menu.free()


## Launching a mission rebuilds the squad for that point in the story.
func test_prepare_mission_sets_squad_for_that_level() -> void:
	var menu = MENU.instantiate()
	var previous_unlocked = Global.unlocked_levels
	var previous_character = Global.selected_character
	menu.catalog.PrepareMission(1)
	assert_str(Global.selected_character).is_equal("Cyber Shogun")
	menu.catalog.PrepareMission(3)
	assert_int(Global.unlocked_levels).is_greater_equal(3)
	menu.catalog.PrepareMission(7)
	assert_int(Global.unlocked_levels).is_greater_equal(7)
	Global.unlocked_levels = previous_unlocked
	Global.selected_character = previous_character
	menu.free()
