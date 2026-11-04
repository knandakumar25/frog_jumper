extends Node

# Reference to the LoadingScreen helper for scene transitions.
const LoadingScreen = preload("res://scripts/ui/loading_screen.gd")

## Mission list lives in the C# LevelCatalog; this bridge exposes it to GDScript so scenes, saves and
## menus all agree on mission numbers and paths. There is no training level: abilities are taught in-mission.
var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()

## Initializes catalog-backed mission locking and keyboard/controller focus.
func _ready() -> void:
	Global.stop_menu_music()
	_update_level_buttons()

	# Focus the first available level.
	var first_btn = get_node_or_null("MainLayout/SectorGrid/Sector1/VBox/Level1")
	if first_btn:
		first_btn.call_deferred("grab_focus")

## Greys out missions the player has not reached yet.
func _update_level_buttons() -> void:
	var unlocked = Global.unlocked_levels

	# Dynamically update buttons if they exist in the UI.
	for lvl_idx in range(1, catalog.Count() + 1):
		var btn = find_level_button("Level" + str(lvl_idx))
		if btn is Button:
			btn.disabled = (lvl_idx > unlocked)
			if btn.disabled:
				btn.modulate = Color(0.5, 0.5, 0.5, 0.6)
			else:
				btn.modulate = Color(1, 1, 1, 1)

## Finds a campaign card button by name.
func find_level_button(btn_name: String) -> Node:
	var grid = get_node_or_null("MainLayout/SectorGrid")
	if grid == null: return null
	return _find_node_recursive(grid, btn_name)

## Searches nested sector cards without relying on a fixed card count.
func _find_node_recursive(node: Node, target_name: String) -> Node:
	if node.name == target_name:
		return node
	for child in node.get_children():
		var found = _find_node_recursive(child, target_name)
		if found: return found
	return null

## Launches a mission with the squad the player had at that point of the story.
func _start_level(level_idx: int) -> void:
	if level_idx < 1 or level_idx > catalog.Count():
		printerr("Level " + str(level_idx) + " not defined in catalog!")
		return

	var scene_path = catalog.ScenePath(level_idx)
	catalog.PrepareMission(level_idx)
	GameManager.set_game_state(GameManager.State.GAMEPLAY)
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), scene_path)

# Button callbacks mapped to mission numbers.
## Launches campaign mission 1.
func _on_level_1_pressed() -> void: _start_level(1)
## Launches campaign mission 2.
func _on_level_2_pressed() -> void: _start_level(2)
## Launches campaign mission 3.
func _on_level_3_pressed() -> void: _start_level(3)
## Launches campaign mission 4.
func _on_level_4_pressed() -> void: _start_level(4)

## Enters Mission 5 directly; the Act II opening belongs to Level 4 Continue.
func _on_level_5_pressed() -> void: _start_level(5)

## Enters Mission 6 directly; the Cyber-Grave cinematic belongs to Level 5 Continue.
func _on_level_6_pressed() -> void: _start_level(6)

## Enters Mission 7 directly; the Foundry Forge cinematic belongs to Level 6 Continue.
func _on_level_7_pressed() -> void: _start_level(7)

## Enters Mission 8 directly; the Stratos Docks cinematic belongs to Level 7 Continue / Act III start.
func _on_level_8_pressed() -> void: _start_level(8)

## Enters Mission 9 directly; the Spire Outer Hull cinematic belongs to Level 8 Continue.
func _on_level_9_pressed() -> void: _start_level(9)

## Enters Mission 10 directly; the Cyber-Nexus cinematic belongs to Level 9 Continue.
func _on_level_10_pressed() -> void: _start_level(10)

## Returns to the main menu through the existing loading screen.
func _on_back_button_pressed() -> void:
	GameManager.set_game_state(GameManager.State.MAIN_MENU)
	Global.reached_finish = false
	LoadingScreen.load_scene(get_tree(), "res://scenes/main_menu/main_menu.tscn")
