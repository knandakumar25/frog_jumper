extends Control

# RTL languages that need right alignment
const RTL_LOCALES = ["ar", "he", "fa", "ur"]

func _ready() -> void:
	var current_locale = TranslationServer.get_locale()
	var locale_code = current_locale.split("_")[0]  # Get language code (e.g., "ar" from "ar_SA")
	
	if locale_code in RTL_LOCALES:
		# Set right alignment for all labels
		for child in $VBoxContainer.get_children():
			if child is Label:
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
