extends Node

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")


func _ready() -> void:
	Global.play_menu_music()


func _on_level_1_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level1.tscn")


func _on_level_2_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level2.tscn")


func _on_level_3_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level3.tscn")


func _on_level_4_pressed() -> void:
	Global.reached_finish = false
	Global.stop_menu_music()
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level4.tscn")


func _on_back_button_pressed() -> void:
	Global.reached_finish = false
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/main_menu.tscn")
