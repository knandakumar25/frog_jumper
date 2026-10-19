extends GdUnitTestSuite


func test_all_scenes_load_without_3d_nodes_or_background_photos() -> void:
	for folder in ["res://scenes/main_menu", "res://scenes/gameplay", "res://scenes/ui"]:
		for file_name in DirAccess.get_files_at(folder):
			if not file_name.ends_with(".tscn"):
				continue
			print("Checking scene: ", folder.path_join(file_name))
			var scene: PackedScene = load(folder.path_join(file_name))
			assert_that(scene).is_not_null()
			if scene == null:
				continue
			var instance = scene.instantiate()
			assert_int(instance.find_children("*", "Node3D", true, false).size()).is_equal(0)
			for texture in instance.find_children("*", "TextureRect", true, false):
				assert_bool(str(texture.name) in ["Background", "BackgroundTexture", "TextureRect2", "TextureRect"]).is_false()
			instance.free()
