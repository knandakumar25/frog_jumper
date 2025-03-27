extends Node


func _on_play_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/level_selection.tscn")


func _on_settings_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/options_menu.tscn")


func _on_quit_pressed() -> void:
	$ColorRect.show()
	$VBoxContainer.show()


func _on_yes_pressed() -> void:
	get_tree().quit()


func _on_no_pressed() -> void:
	$VBoxContainer.hide()
	$ColorRect.hide()
