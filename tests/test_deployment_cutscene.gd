extends GdUnitTestSuite
## Verify lifecycle safety without starting the decoder or changing scenes.


func test_standalone_finish_does_not_access_scene_tree() -> void:
	## Standalone nodes must remain safe and be freed by the owning test.
	var scene = load("res://scenes/main_menu/deployment_cutscene.tscn").instantiate()
	scene.finish()
	assert_bool(scene.transitioning).is_false()
	scene.free()


func test_subtitles_fit_inside_the_forty_second_film() -> void:
	## Each cue must be ordered, non-overlapping, and end before playback does.
	var scene = load("res://scenes/main_menu/deployment_cutscene.tscn").instantiate()
	var previous_end := 0.0
	for cue in scene.CUES:
		assert_float(float(cue[0])).is_greater_equal(previous_end)
		assert_float(float(cue[1])).is_greater(float(cue[0]))
		assert_float(float(cue[1])).is_less_equal(40.0)
		previous_end = cue[1]
	scene.free()
