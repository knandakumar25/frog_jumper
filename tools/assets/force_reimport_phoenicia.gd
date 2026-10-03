extends SceneTree

func _initialize() -> void:
	print("--- Starting Force Reimport of phoenicia.glb ---")
	var p = "res://assets/models/hq/phoenicia.glb"
	var res = ResourceLoader.load(p, "PackedScene", ResourceLoader.CACHE_MODE_IGNORE)
	if res == null:
		print("FAILED to load res://assets/models/hq/phoenicia.glb via ResourceLoader. Trying GltfDocument...")
		var doc = GLTFDocument.new()
		var state = GLTFState.new()
		var err = doc.append_from_file("D:/Projects/frog_jumper/assets/models/hq/phoenicia.glb", state)
		if err == OK:
			var node = doc.generate_scene(state)
			print("GltfDocument successfully loaded phoenicia.glb!")
			for child in node.get_children():
				print("  Child: ", child.name, " type: ", child.get_class())
			# Save as PackedScene to .godot/imported/phoenicia.glb-9f903cd202d48873475d0bd380801bb0.scn!
			var packed = PackedScene.new()
			packed.pack(node)
			var save_err = ResourceSaver.save(packed, "res://.godot/imported/phoenicia.glb-9f903cd202d48873475d0bd380801bb0.scn")
			print("Saved to .godot/imported/... result: ", save_err)
		else:
			print("GltfDocument failed with error: ", err)
	else:
		print("ResourceLoader loaded successfully!")
		var inst = res.instantiate()
		for child in inst.get_children():
			print("  Child: ", child.name, " type: ", child.get_class())
			if child is MeshInstance3D:
				print("    Mesh surfaces: ", child.mesh.get_surface_count())
				for s in range(child.mesh.get_surface_count()):
					print("      Surface ", s, " mat: ", child.mesh.surface_get_material(s))
		inst.queue_free()
	print("--- Done ---")
	quit()
