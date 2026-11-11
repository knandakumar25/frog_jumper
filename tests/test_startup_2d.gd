extends GdUnitTestSuite


func test_default_launch_uses_2d_menu() -> void:
	assert_str(ProjectSettings.get_setting("application/run/main_scene")).is_equal(
		"res://scenes/main_menu/main_menu.tscn")


func test_briefing_creates_no_3d_nodes_at_runtime() -> void:
	var runner = scene_runner("res://scenes/main_menu/story_intro.tscn")
	var briefing = runner.scene()
	assert_int(briefing.find_children("*", "Node3D", true, false).size()).is_equal(0)
	# The AXIOM-7 portrait art awaits SpriteCook regeneration; the slot must still exist.
	var portrait: TextureRect = briefing.get_node("HBoxContainer/RobotPortrait")
	assert_that(portrait).is_not_null()
