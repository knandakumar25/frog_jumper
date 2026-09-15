extends Node

# Reference to the LoadingScreen helper for scene transitions.
const LoadingScreen = preload("res://scripts/ui/loading_screen.gd")

# Level selection buttons mapped to specific game sectors.
@onready var level_1_button: Button = $MainLayout/SectorGrid/Sector1/VBox/Level1
@onready var level_2_button: Button = $MainLayout/SectorGrid/Sector2/VBox/Level2
@onready var level_3_button: Button = $MainLayout/SectorGrid/Sector3/VBox/Level3
@onready var level_4_button: Button = $MainLayout/SectorGrid/Sector4/VBox/Level4

func _ready() -> void:
	# Stop background menu music when entering level selection.
	Global.stop_menu_music()
	_update_level_buttons()

	# Ensure the first available level is focused for gamepad navigation.
	if level_1_button != null:
		level_1_button.call_deferred("grab_focus")

# Evaluates the user's progression and enables/disables level buttons accordingly.
func _update_level_buttons() -> void:
	# Read the number of unlocked levels from the Global singleton.
	var unlocked = Global.unlocked_levels

	# Level 1 is always available.
	level_1_button.disabled = false
	# Levels 2-4 are disabled unless the user has reached the corresponding progress threshold.
	level_2_button.disabled = unlocked < 2
	level_3_button.disabled = unlocked < 3
	level_4_button.disabled = unlocked < 4

	# Apply a desaturated visual style (greyed out) to locked buttons.
	if level_2_button.disabled:
		level_2_button.modulate = Color(0.5, 0.5, 0.5, 0.6)
	if level_3_button.disabled:
		level_3_button.modulate = Color(0.5, 0.5, 0.5, 0.6)
	if level_4_button.disabled:
		level_4_button.modulate = Color(0.5, 0.5, 0.5, 0.6)

func _on_level_1_pressed() -> void:
	_start_level("res://scenes/gameplay/level1.tscn")

func _on_level_2_pressed() -> void:
	_start_level("res://scenes/gameplay/level2.tscn")

func _on_level_3_pressed() -> void:
	_start_level("res://scenes/gameplay/level3.tscn")

func _on_level_4_pressed() -> void:
	_start_level("res://scenes/gameplay/level4.tscn")

# Internal helper to reset game state and transition to the chosen level.
func _start_level(scene_path: String) -> void:
	# Reset the finish flag so the level doesn't start as completed.
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), scene_path)

func _on_back_button_pressed() -> void:
	# Ensure game state is clean and return to the main menu.
	Global.reached_finish = false
	LoadingScreen.load_scene(get_tree(), "res://scenes/main_menu/main_menu.tscn")
