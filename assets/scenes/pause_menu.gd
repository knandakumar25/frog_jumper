extends Control

func _ready() -> void:
	get_tree().paused = false
	$PanelContainer.visible = false

func resume():
	get_tree().paused = false
	$PanelContainer.visible = false

func pause():
	get_tree().paused = true
	$PanelContainer.visible = true

func _process(delta: float) -> void:
	if Input.is_action_just_pressed("escape") and !get_tree().paused:
		pause()
	elif Input.is_action_just_pressed("escape") and get_tree().paused:
		resume()


func _on_resume_pressed() -> void:
	resume()


func _on_restart_pressed() -> void:
	get_tree().reload_current_scene()


func _on_quit_pressed() -> void:
	get_tree().quit()
