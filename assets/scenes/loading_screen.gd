# loading_screen.gd
# Compatibility stub — real implementation is in LoadingScreen.cs.
# GDScript files that do:  const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")
# and call LoadingScreen.load_scene(get_tree(), path) continue to work unchanged.

extends RefCounted

static func load_scene(tree: SceneTree, scene_path: String) -> void:
	var loading = load("res://assets/scenes/loading_screen.tscn").instantiate()
	loading.target_scene_path = scene_path          # [Export] property on LoadingScreen.cs
	tree.get_root().add_child(loading)
	tree.current_scene.queue_free()
	tree.current_scene = loading
