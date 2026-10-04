extends SceneTree

func _initialize() -> void:
	print("--- Running Character Selection Ability Preview Screenshot Test ---")
	var scene_res = load("res://scenes/main_menu/character_selection.tscn") as PackedScene
	var inst = scene_res.instantiate()
	root.add_child(inst)

	for i in range(20):
		await process_frame

	var dir = DirAccess.open("res://")
	if not dir.dir_exists("scratch"):
		dir.make_dir("scratch")

	var tests = [
		{"hero": "Oceanus", "ability": 0, "name": "oceanus_primary_torrent", "wait": 18},
		{"hero": "Oceanus", "ability": 1, "name": "oceanus_secondary_vortex", "wait": 20},
		{"hero": "Oceanus", "ability": 3, "name": "oceanus_slam_crash", "wait": 22},
		{"hero": "Surya Prime", "ability": 0, "name": "surya_primary_bash", "wait": 18},
		{"hero": "Surya Prime", "ability": 1, "name": "surya_secondary_guard", "wait": 18},
		{"hero": "Surya Prime", "ability": 3, "name": "surya_slam_cosmic", "wait": 24},
		{"hero": "Apex Titan", "ability": 0, "name": "apex_primary_slam", "wait": 20},
		{"hero": "Apex Titan", "ability": 1, "name": "apex_secondary_pull", "wait": 18},
		{"hero": "Aura Phoenicia", "ability": 0, "name": "phoenicia_primary_pulse", "wait": 18},
		{"hero": "Aura Phoenicia", "ability": 1, "name": "phoenicia_secondary_reverb", "wait": 18},
		{"hero": "Cyber Shogun", "ability": 0, "name": "shogun_primary_slash", "wait": 18},
		{"hero": "Mirage Blade", "ability": 0, "name": "mirage_primary_spin", "wait": 18}
	]

	for t in tests:
		print("Testing ", t["hero"], " ability ", t["ability"], " (", t["name"], ")...")
		inst.call("SelectOperative", t["hero"])
		for f in range(15):
			await process_frame
		
		inst.call("PreviewAbility", t["ability"])
		var wait_frames = t.get("wait", 18)
		for f in range(wait_frames):
			await process_frame

		var vp = root.get_viewport()
		if vp:
			var tex = vp.get_texture()
			if tex:
				var img = tex.get_image()
				if img:
					var out_path = "D:/Projects/frog_jumper/scratch/" + t["name"] + ".png"
					img.save_png(out_path)
					print("Saved screenshot to ", out_path)
				else:
					print("img is null for ", t["name"])
			else:
				print("tex is null for ", t["name"])

		# Wait for preview to settle
		for f in range(25):
			await process_frame

	print("--- Finished Ability Preview Screenshot Test ---")
	quit()
