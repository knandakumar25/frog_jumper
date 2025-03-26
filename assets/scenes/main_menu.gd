extends Node


func _on_play_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/level_selection.tscn")


func _on_settings_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/options_menu.tscn")
