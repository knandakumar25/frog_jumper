extends SceneTree

func _initialize() -> void:
	print("--- Starting Force Reimport of cyber_shogun.glb ---")
	var doc = GLTFDocument.new()
	var state = GLTFState.new()
	var glb_path = "D:/Projects/frog_jumper/assets/models/hq/cyber_shogun.glb"
	var err = doc.append_from_file(glb_path, state)
	if err == OK:
		var node = doc.generate_scene(state)
		print("GltfDocument successfully loaded cyber_shogun.glb!")
		for child in node.get_children():
			print("  Child: ", child.name, " type: ", child.get_class())
		var packed = PackedScene.new()
		packed.pack(node)
		var target_scn = "res://.godot/imported/cyber_shogun.glb-d355f5f45418ace73ed8935d3131045b.scn"
		var save_err = ResourceSaver.save(packed, target_scn)
		print("Saved to ", target_scn, " result: ", save_err)
	else:
		print("GltfDocument failed with error: ", err)
	print("--- Done ---")
	quit()
