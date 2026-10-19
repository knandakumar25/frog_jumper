extends SceneTree
## Run with a real rendering driver (not --headless) to capture the actual UI.


func _initialize() -> void:
	_capture.call_deferred()


func _capture() -> void:
	root.mode = Window.MODE_WINDOWED
	root.size = Vector2i(1280, 720)
	root.content_scale_size = Vector2i(1920, 1080)
	root.content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
	var output := ProjectSettings.globalize_path("res://.validation/menu-backdrops")
	DirAccess.make_dir_recursive_absolute(output)
	for name in ["main_menu", "character_selection"]:
		var scene: Node = load("res://scenes/main_menu/%s.tscn" % name).instantiate()
		root.add_child(scene)
		current_scene = scene
		for i in range(30):
			await process_frame
		var backdrop = scene.get_node("AnimatedBackdrop")
		backdrop.set_process(false)
		for time in [0.0, 4.0]:
			backdrop.elapsed = time
			backdrop.queue_redraw()
			await process_frame
			await RenderingServer.frame_post_draw
			var frame := root.get_texture().get_image()
			frame.save_png(output.path_join("%s_t%d.png" % [name, int(time)]))
		for sample in range(24):
			backdrop.elapsed = sample / 6.0
			backdrop.queue_redraw()
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png(output.path_join("%s_frame_%02d.png" % [name, sample]))
		scene.queue_free()
		await process_frame
	current_scene = null
	print("MENU_BACKDROP_CAPTURES_COMPLETE")
	quit()
