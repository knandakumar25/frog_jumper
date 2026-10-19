extends SceneTree
## Run after an editor import: godot --headless --path . --script tests/test_infrastructure.gd

var _failures: int = 0


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

	if _failures == 0:
		print("Infrastructure checks passed: 2D scenes and shared glow environment.")
	quit(0 if _failures == 0 else 1)


func _check(condition: bool, message: String) -> void:
	if not condition:
		_failures += 1
		push_error(message)
