extends SceneTree
## Run after an editor import: godot --headless --path . --script tests/test_infrastructure.gd

var _failures: int = 0
var _surface_count: int = 0


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	_check(ProjectSettings.get_setting("application/config/name") == "Axiom Force", "Project identity")
	_check(ProjectSettings.get_setting("autoload/GameManager") == "*res://scripts/managers/GameManager.gd", "Autoload registration")
	_check(root.get_node_or_null("GameManager") != null, "Global manager instantiated at startup")
	var environment := load("res://assets/themes/cyber_glow.tres") as Environment
	_check(environment != null, "Shared environment loads")
	if environment != null:
		_check(environment.glow_enabled, "Glow enabled")
		_check(environment.glow_blend_mode == Environment.GLOW_BLEND_MODE_ADDITIVE, "Additive glow")
		_check(environment.tonemap_mode == Environment.TONE_MAPPER_ACES, "ACES tonemapping")
		_check(is_equal_approx(environment.glow_hdr_threshold, 0.8), "Neon glow threshold")
	_check(ProjectSettings.get_setting("rendering/renderer/rendering_method") in ["mobile", "forward_plus"], "Renderer supports additive glow")

	for folder in ["res://scenes/main_menu", "res://scenes/gameplay"]:
		for file_name in DirAccess.get_files_at(folder):
			if file_name.ends_with(".tscn"):
				_check(load(folder.path_join(file_name)) is PackedScene, "Migrated scene loads: " + file_name)

	for scene_name in ["3d_menu_experience", "character_selection"]:
		var packed := load("res://scenes/main_menu/%s.tscn" % scene_name) as PackedScene
		_check(packed != null, "%s loads" % scene_name)
		if packed == null:
			continue
		var state := packed.get_state()
		var found_environment := false
		for index in state.get_node_count():
			var node_path := String(state.get_node_path(index)).trim_prefix("./")
			if node_path != "SubViewportContainer/SubViewport3D/World3D/WorldEnvironment3D":
				continue
			for property_index in state.get_node_property_count(index):
				if state.get_node_property_name(index, property_index) == &"environment":
					found_environment = state.get_node_property_value(index, property_index) == environment
		_check(found_environment, "%s uses the shared environment inside its 3D viewport" % scene_name)

	var model_count := 0
	for file_name in DirAccess.get_files_at("res://assets/models/hq"):
		if not file_name.ends_with(".glb"):
			continue
		model_count += 1
		var packed := load("res://assets/models/hq/" + file_name) as PackedScene
		_check(packed != null, "Imported model loads: " + file_name)
		if packed == null:
			continue
		var model := packed.instantiate()
		var previous_count := _surface_count
		_inspect_model(model, file_name)
		_check(_surface_count > previous_count, "Model has lit mesh surfaces: " + file_name)
		model.free()
	_check(model_count == 50, "All 50 migrated GLBs are available")
	if _failures == 0:
		print("Infrastructure checks passed: %d models, %d lit material surfaces, ACES, Additive glow, and 3D viewport wiring." % [model_count, _surface_count])
	quit(0 if _failures == 0 else 1)


func _inspect_model(node: Node, model_name: String) -> void:
	if node is MeshInstance3D and node.mesh != null:
		for index in node.mesh.get_surface_count():
			_surface_count += 1
			var material := node.get_active_material(index) as BaseMaterial3D
			_check(material != null, "PBR material: " + model_name)
			if material == null:
				continue
			_check(material.shading_mode == BaseMaterial3D.SHADING_MODE_PER_PIXEL, "Per-pixel lighting: " + model_name)
			_check(material.specular_mode == BaseMaterial3D.SPECULAR_SCHLICK_GGX, "Specular lighting: " + model_name)
			_check(material.metallic_specular >= 0.5, "Specular response: " + model_name)
			if model_name == "cyber_shogun.glb" or model_name.contains("Axiom_Force_HQ_Buildi"):
				_check(material.albedo_texture != null, "Hero/citadel albedo preserved: " + model_name)
				_check(material.emission_texture != null, "Hero/citadel emission preserved: " + model_name)
			if model_name.contains("Axiom_Force_HQ_Buildi"):
				_check(material.metallic_texture != null, "Citadel metallic map preserved")
				_check(material.normal_texture != null, "Citadel normal map preserved")
	for child in node.get_children():
		_inspect_model(child, model_name)


func _check(condition: bool, message: String) -> void:
	if not condition:
		_failures += 1
		push_error(message)
