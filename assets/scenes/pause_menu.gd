extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	get_tree().paused = false
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = false

func resume() -> void:
	get_tree().paused = false
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = false

func pause() -> void:
	get_tree().paused = true
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = true

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("escape") or event.is_action_pressed("ui_cancel"):
		if get_tree().paused:
			resume()
		else:
			pause()
		get_viewport().set_input_as_handled()

func _on_resume_pressed() -> void:
	resume()

func _on_restart_pressed() -> void:
	get_tree().paused = false
	get_tree().reload_current_scene()

func _on_level_selection_pressed() -> void:
	Global.play_menu_music()
	get_tree().paused = false
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level_selection.tscn")

func _on_main_menu_pressed() -> void:
	Global.play_menu_music()
	get_tree().paused = false
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/main_menu.tscn")
