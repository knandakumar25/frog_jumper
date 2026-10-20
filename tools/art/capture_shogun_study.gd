extends SceneTree

func _initialize() -> void:
	capture.call_deferred()

func capture() -> void:
	root.mode = Window.MODE_WINDOWED
	root.size = Vector2i(1280, 720)
	root.content_scale_size = Vector2i(1920, 1080)
	root.content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
	var output = ProjectSettings.globalize_path("res://.validation/shogun_study")
	DirAccess.make_dir_recursive_absolute(output)
	var menu = load("res://scenes/main_menu/character_selection.tscn").instantiate()
	root.add_child(menu)
	current_scene = menu
	for action in ["Idle", "Run", "Jump", "Katana", "Kinetic Parry", "Ground Slam"]:
		menu.call("PreviewStudyAction", action)
		await create_timer(0.7).timeout
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(output.path_join(action.replace(" ", "_") + ".png"))
	menu.queue_free()
	await process_frame
	current_scene = null
	print("SHOGUN_STUDY_CAPTURES_COMPLETE")
	quit()
