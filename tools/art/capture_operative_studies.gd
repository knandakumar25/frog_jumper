extends SceneTree
## Captures each operative's movement and loadout poses in the actual selection UI.

## Defers until autoloads are ready so the capture follows the normal scene lifecycle.
func _initialize() -> void:
	capture.call_deferred()

## Saves review screenshots to the ignored validation folder and releases the menu on exit.
func capture() -> void:
	root.mode = Window.MODE_WINDOWED
	root.size = Vector2i(1280, 720)
	root.content_scale_size = Vector2i(1920, 1080)
	root.content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
	var output = ProjectSettings.globalize_path("res://.validation/operative_studies")
	DirAccess.make_dir_recursive_absolute(output)
	var menu = load("res://scenes/main_menu/character_selection.tscn").instantiate()
	root.add_child(menu)
	current_scene = menu
	for operative in ["Cyber Shogun", "Surya Prime", "Oceanus", "Mirage Blade", "Apex Titan", "Aura Phoenicia"]:
		menu.call("SelectOperative", operative)
		for action in ["Idle", "Walk", "Run", "Jump", "0", "1", "2", "3"]:
			if action.is_valid_int():
				menu.call("PreviewAbility", action.to_int())
			else:
				menu.call("PreviewStudyAction", action)
			await create_timer(0.7).timeout
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png(output.path_join(operative.replace(" ", "_") + "_" + action + ".png"))
	menu.queue_free()
	await process_frame
	current_scene = null
	print("OPERATIVE_STUDY_CAPTURES_COMPLETE")
	quit()
