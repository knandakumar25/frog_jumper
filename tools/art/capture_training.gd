extends SceneTree
## Captures the live trainer at desktop and compact resolutions without editing assets or saves.

## Wait for autoload initialization before constructing real training scenes.
func _initialize() -> void:
	capture.call_deferred()

## Save per-operative runtime screenshots under the ignored validation directory.
func capture() -> void:
	root.mode = Window.MODE_WINDOWED
	root.size = Vector2i(1280, 720)
	var output = ProjectSettings.globalize_path("res://.validation/training")
	DirAccess.make_dir_recursive_absolute(output)
	var global = root.get_node("Global")
	var original = global.selected_character
	for operative in ["Cyber Shogun", "Surya Prime", "Oceanus", "Mirage Blade", "Apex Titan", "Aura Phoenicia"]:
		global.selected_character = operative
		var course = load("res://scenes/gameplay/training_protocol.tscn").instantiate()
		root.add_child(course)
		current_scene = course
		await create_timer(0.7).timeout
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(output.path_join(operative.replace(" ", "_") + ".png"))
		course.queue_free()
		await process_frame
	global.selected_character = original
	current_scene = null
	print("TRAINING_CAPTURES_COMPLETE")
	quit()
