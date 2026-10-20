extends SceneTree
## Capture the newly authored flat artwork in its actual runtime UI.

func _initialize() -> void:
	_capture.call_deferred()

func _capture() -> void:
	root.mode = Window.MODE_WINDOWED
	root.size = Vector2i(1280, 720)
	root.content_scale_size = Vector2i(1920, 1080)
	root.content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
	var output := ProjectSettings.globalize_path("res://.validation/flat_characters/screens")
	DirAccess.make_dir_recursive_absolute(output)
	var menu = load("res://scenes/main_menu/character_selection.tscn").instantiate()
	root.add_child(menu)
	current_scene = menu
	for operative in ["Cyber Shogun", "Surya Prime", "Oceanus", "Mirage Blade", "Apex Titan", "Aura Phoenicia"]:
		menu.call("SelectOperative", operative)
		for step in range(3):
			await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(output.path_join(operative.replace(" ", "_") + ".png"))
	menu.queue_free()
	await process_frame
	current_scene = null
	var level = load("res://scenes/gameplay/level1.tscn").instantiate()
	root.add_child(level)
	current_scene = level
	for step in range(60):
		await process_frame
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(output.path_join("level1.png"))
	level.queue_free()
	await process_frame
	current_scene = null
	print("FLAT_CHARACTER_CAPTURES_COMPLETE")
	quit()
