extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

func _ready() -> void:
	get_tree().paused = false
	$ColorRect2.visible = false

func resume():
	get_tree().paused = false
	$ColorRect2.visible = false

func pause():
	get_tree().paused = true
	$ColorRect2.visible = true

func _process(_delta: float) -> void:
	if Input.is_action_just_pressed("escape") and !get_tree().paused:
		pause()
	elif Input.is_action_just_pressed("escape") and get_tree().paused:
		resume()


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
