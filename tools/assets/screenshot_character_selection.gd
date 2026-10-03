extends SceneTree

func _initialize() -> void:
	print("--- Running In-Game Phoenicia Character Selection Screenshot Test ---")
	var scene_res = load("res://scenes/main_menu/character_selection.tscn") as PackedScene
	var inst = scene_res.instantiate()
	root.add_child(inst)
	
	await process_frame
	await process_frame
	
	var phoenicia_btn = inst.find_child("Phoenicia", true, false) as Button
	if phoenicia_btn:
		print("Emitting pressed signal on Phoenicia button...")
		phoenicia_btn.emit_signal("pressed")
	
	# Wait 45 frames for animation and shaders to settle
	for i in range(45):
		await process_frame
	
	var img = root.get_viewport().get_texture().get_image()
	img.save_png("D:/Projects/frog_jumper/scratch/godot_phoenicia_ingame_screenshot2.png")
	print("Saved screenshot to D:/Projects/frog_jumper/scratch/godot_phoenicia_ingame_screenshot2.png")
	quit()
