extends Control


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass


func _on_confirm_pressed() -> void:
	AudioServer.set_bus_volume_db(0, linear_to_db($AudioOptions/VBoxContainer/MasterSlider.value))
	$AudioOptions.apply_language_change()


func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
