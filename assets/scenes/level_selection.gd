extends Node


func _on_level_1_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/level1.tscn")


func _on_level_2_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/level2.tscn")


func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
