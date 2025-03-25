extends Node

func _on_level_1_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/level1.tscn")

func _on_level_2_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/level2.tscn")

func _on_settings_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/options_menu.tscn")
