extends Node


@onready var quit_dialog: ConfirmationDialog = $Control/QuitDialog


func _on_play_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/level_selection.tscn")


func _on_instructions_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/instructions.tscn")


func _on_settings_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/options_menu.tscn")


func _on_yes_pressed() -> void:
	get_tree().quit()


func _on_no_pressed() -> void:
	$VBoxContainer.hide()
	$ColorRect.hide()


func _on_quit_button_pressed() -> void:
	quit_dialog.popup_centered()
	quit_dialog.dialog_text = tr("Are you sure you want to quit?")


func _on_quit_dialog_confirmed() -> void:
	get_tree().quit()
