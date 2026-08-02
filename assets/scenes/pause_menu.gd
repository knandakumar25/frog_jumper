extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	get_tree().paused = false
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = false
	_translate_ui()

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

func resume() -> void:
	get_tree().paused = false
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = false
	var vp = get_viewport()
	if vp != null:
		vp.gui_release_focus()

func pause() -> void:
	get_tree().paused = true
	var panel = get_node_or_null("ColorRect2")
	if panel != null:
		panel.visible = true
	var resume_btn = get_node_or_null("ColorRect2/VBoxContainer/Resume")
	if resume_btn != null:
		resume_btn.call_deferred("grab_focus")

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
	Global.stop_menu_music()
	get_tree().paused = false
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level_selection.tscn")

func _on_main_menu_pressed() -> void:
	get_tree().paused = false
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/main_menu.tscn")
