extends SceneTree

func _initialize() -> void:
	print("--- Running In-Game Cyber Shogun Character Selection Screenshot Test ---")
	var scene_res = load("res://scenes/main_menu/character_selection.tscn") as PackedScene
	var inst = scene_res.instantiate()
	root.add_child(inst)
	
	# Wait 45 frames for animation and shaders to settle on Cyber Shogun (default operative)
	for i in range(45):
		await process_frame
	
	var img = root.get_viewport().get_texture().get_image()
	var out_path = "C:/Users/karthiknk21/.gemini/antigravity-cli/brain/51c70920-0a37-4082-8177-2c677a76cad3/cyber_shogun_ingame_podium.png"
	img.save_png(out_path)
	print("Saved screenshot to ", out_path)
	quit()
