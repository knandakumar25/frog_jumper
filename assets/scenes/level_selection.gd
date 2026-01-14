extends Node

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

@onready var level_1_button: Button = $VBoxContainer/HBoxContainer/Level1
@onready var level_2_button: Button = $VBoxContainer/HBoxContainer/Level2
@onready var level_3_button: Button = $VBoxContainer/HBoxContainer/Level3
@onready var level_4_button: Button = $VBoxContainer/HBoxContainer/Level4


func _ready() -> void:
	Global.play_menu_music()
	_update_level_buttons()


func _update_level_buttons() -> void:
	# Enable/disable buttons based on unlocked levels
	var unlocked = Global.unlocked_levels
	
	level_1_button.disabled = false
	level_2_button.disabled = unlocked < 2
	level_3_button.disabled = unlocked < 3
	level_4_button.disabled = unlocked < 4
	
	# Visual feedback for locked levels
	if level_2_button.disabled:
		level_2_button.modulate = Color(0.5, 0.5, 0.5, 0.6)
	if level_3_button.disabled:
		level_3_button.modulate = Color(0.5, 0.5, 0.5, 0.6)
	if level_4_button.disabled:
		level_4_button.modulate = Color(0.5, 0.5, 0.5, 0.6)


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
