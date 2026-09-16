extends Node

# Reference to the LoadingScreen helper for scene transitions.
const LoadingScreen = preload("res://scripts/ui/loading_screen.gd")

# Level selection mapping from index to scene path.
# This supports the full 10-level campaign (0-9).
var level_map = {
	0: "res://scenes/gameplay/training_protocol.tscn",
	1: "res://scenes/gameplay/prototype_3d.tscn", # Using prototype for Lvl 1
	2: "res://scenes/gameplay/level2.tscn",
	3: "res://scenes/gameplay/level3.tscn",
	4: "res://scenes/gameplay/level4.tscn",
	5: "res://scenes/gameplay/level5.tscn",
	6: "res://scenes/gameplay/level6.tscn",
	7: "res://scenes/gameplay/level7.tscn",
	8: "res://scenes/gameplay/level8.tscn",
	9: "res://scenes/gameplay/level9.tscn"
}

func _ready() -> void:
	Global.stop_menu_music()
	_update_level_buttons()

	# Focus the first available level.
	var first_btn = get_node_or_null("MainLayout/SectorGrid/Sector1/VBox/Level1")
	if first_btn:
		first_btn.call_deferred("grab_focus")

func _update_level_buttons() -> void:
	var unlocked = Global.unlocked_levels

	# Dynamically update buttons if they exist in the UI.
	for lvl_idx in level_map.keys():
		var btn_name = "Level" + str(lvl_idx)
		var btn = find_level_button(btn_name)
		if btn is Button:
			btn.disabled = (lvl_idx > unlocked)
			if btn.disabled:
				btn.modulate = Color(0.5, 0.5, 0.5, 0.6)
			else:
				btn.modulate = Color(1, 1, 1, 1)

func find_level_button(btn_name: String) -> Node:
	var grid = get_node_or_null("MainLayout/SectorGrid")
	if grid == null: return null
	return _find_node_recursive(grid, btn_name)

func _find_node_recursive(node: Node, target_name: String) -> Node:
	if node.name == target_name:
		return node
	for child in node.get_children():
		var found = _find_node_recursive(child, target_name)
		if found: return found
	return null

func _start_level(level_idx: int) -> void:
	if not level_map.has(level_idx):
		printerr("Level " + str(level_idx) + " not defined in map!")
		return

	var scene_path = level_map[level_idx]
	GameManager.set_game_state(GameManager.State.GAMEPLAY)
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), scene_path)

# Button callbacks mapped to indices.
func _on_level_0_pressed() -> void: _start_level(0)
func _on_level_1_pressed() -> void: _start_level(1)
func _on_level_2_pressed() -> void: _start_level(2)
func _on_level_3_pressed() -> void: _start_level(3)
func _on_level_4_pressed() -> void: _start_level(4)
func _on_level_5_pressed() -> void: _start_level(5)
func _on_level_6_pressed() -> void: _start_level(6)
func _on_level_7_pressed() -> void: _start_level(7)
func _on_level_8_pressed() -> void: _start_level(8)
func _on_level_9_pressed() -> void: _start_level(9)

func _on_back_button_pressed() -> void:
	GameManager.set_game_state(GameManager.State.MAIN_MENU)
	Global.reached_finish = false
	LoadingScreen.load_scene(get_tree(), "res://scenes/main_menu/main_menu.tscn")
