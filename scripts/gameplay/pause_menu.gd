extends Control

# Reference to the LoadingScreen helper for scene transitions.
const LoadingScreen = preload("res://scripts/ui/loading_screen.gd")
var _previous_prompt_context: String = "platformer"
var _previous_prompt_actions: Array[String] = []

## Purpose:
## This script manages the game's Pause Menu. It controls the freezing and unfreezing
## of the game world (SceneTree pausing) and provides navigation options to resume,
## restart, or return to previous menu screens.
##
## Translation Target:
## Unity Translation: Use a separate 'PauseMenu' UI Panel. Use 'Time.timeScale = 0'
## to freeze the game world. Ensure the PauseMenu GameObject is not affected by
## Time.timeScale (handled automatically by Unity's UI system).

func _ready() -> void:
	# Ensure the pause menu continues to process input and logic even when the tree is paused.
	process_mode = Node.PROCESS_MODE_ALWAYS

	# Initialize game state as unpaused.
	get_tree().paused = false

	# Hide the pause panel by default.
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = false

	_translate_ui()

# Updates all menu labels to the current localized language.
func _translate_ui() -> void:
	var title = get_node_or_null("ColorRect2/VBoxContainer/Title")
	if title != null:
		title.text = tr("SYSTEM PAUSED")

	var resume_btn = get_node_or_null("ColorRect2/VBoxContainer/Resume")
	if resume_btn != null:
		resume_btn.text = tr("Resume Mission")

	var restart_btn = get_node_or_null("ColorRect2/VBoxContainer/Restart")
	if restart_btn != null:
		restart_btn.text = tr("Restart Level")

	var lvl_btn = get_node_or_null("ColorRect2/VBoxContainer/LevelSelection")
	if lvl_btn != null:
		lvl_btn.text = tr("Return to Level Selection")

	var menu_btn = get_node_or_null("ColorRect2/VBoxContainer/MainMenu")
	if menu_btn != null:
		menu_btn.text = tr("Return to Main Menu")

# Unfreezes the game world and hides the pause UI.
func resume() -> void:
	InputPromptService.SetPromptContext(_previous_prompt_context, _previous_prompt_actions)
	GameManager.set_game_state(GameManager.State.GAMEPLAY)
	get_tree().paused = false
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = false

	# Release focus from the pause menu to return control to the game world.
	var vp = get_viewport()
	if vp != null:
		vp.gui_release_focus()

# Freezes the game world and displays the pause UI.
func pause() -> void:
	_previous_prompt_context = InputPromptService.Context
	_previous_prompt_actions.assign(InputPromptService.ContextActions)
	InputPromptService.SetPromptContext("menu", ["ui_up", "ui_down", "ui_accept", "escape"])
	GameManager.set_game_state(GameManager.State.PAUSED)
	get_tree().paused = true
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = true

	# Focus the Resume button immediately for gamepad navigation.
	var resume_btn = get_node_or_null("ColorRect2/VBoxContainer/Resume")
	if resume_btn != null:
		resume_btn.call_deferred("grab_focus")

func _unhandled_input(event: InputEvent) -> void:
	# Toggle pause state when 'escape' or 'ui_cancel' is pressed.
	if event.is_action_pressed("escape") or event.is_action_pressed("ui_cancel"):
		if get_tree().paused:
			resume()
		else:
			pause()
		# Prevent the input from propagating to other game systems.
		get_viewport().set_input_as_handled()

func _on_resume_pressed() -> void:
	resume()

func _on_restart_pressed() -> void:
	# Unpause and reload the current scene to reset the level.
	get_tree().paused = false
	get_tree().reload_current_scene()

func _on_level_selection_pressed() -> void:
	GameManager.set_game_state(GameManager.State.MAIN_MENU)
	# Stop menu music and transition back to level selection via loading screen.
	Global.stop_menu_music()
	get_tree().paused = false
	LoadingScreen.load_scene(get_tree(), "res://scenes/main_menu/level_selection.tscn")

func _on_main_menu_pressed() -> void:
	GameManager.set_game_state(GameManager.State.MAIN_MENU)
	# Unpause and transition back to the main menu via loading screen.
	get_tree().paused = false
	LoadingScreen.load_scene(get_tree(), "res://scenes/main_menu/main_menu.tscn")
