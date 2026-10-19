extends GdUnitTestSuite


func test_both_menus_have_original_layered_2d_art_behind_ui() -> void:
	for name in ["main_menu", "character_selection"]:
		var menu = load("res://scenes/main_menu/%s.tscn" % name).instantiate()
		var backdrop = menu.get_node("AnimatedBackdrop")
		assert_bool(backdrop is Control).is_true()
		assert_int(backdrop.get_index()).is_equal(0)
		assert_int(backdrop.mouse_filter).is_equal(Control.MOUSE_FILTER_IGNORE)
		assert_int(backdrop.focus_mode).is_equal(Control.FOCUS_NONE)
		for texture in [backdrop.foundation, backdrop.architecture, backdrop.illumination]:
			assert_that(texture).is_not_null()
			assert_vector(texture.get_size()).is_equal(Vector2(1920, 1080))
		assert_int(menu.find_children("*", "Node3D", true, false).size()).is_equal(0)
		menu.free()


func test_animation_advances_and_can_be_disabled() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var backdrop = runner.scene().get_node("AnimatedBackdrop")
	var start: float = backdrop.elapsed
	backdrop._process(0.5)
	assert_float(backdrop.elapsed).is_greater(start)
	backdrop.motion_enabled = false
	var frozen: float = backdrop.elapsed
	backdrop._process(0.5)
	assert_float(backdrop.elapsed).is_equal(frozen)
	assert_bool(backdrop.is_processing()).is_false()
