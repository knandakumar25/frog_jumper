extends Node


func _ready() -> void:
	Global.play_menu_music()


func _on_level_1_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	get_tree().change_scene_to_file("res://assets/scenes/level1.tscn")


func _on_level_2_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	get_tree().change_scene_to_file("res://assets/scenes/level2.tscn")


func _on_level_3_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	get_tree().change_scene_to_file("res://assets/scenes/level3.tscn")


func _on_level_4_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	get_tree().change_scene_to_file("res://assets/scenes/level4.tscn")


func _on_back_button_pressed() -> void:
	Global.reached_finish = false
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
