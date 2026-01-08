extends Control

# RTL languages that need right alignment
const RTL_LOCALES = ["ar", "he", "fa", "ur"]

func _ready() -> void:
	var current_locale = TranslationServer.get_locale()
	var locale_code = current_locale.split("_")[0]  # Get language code (e.g., "ar" from "ar_SA")
	
	if locale_code in RTL_LOCALES:
		# Set right alignment for all labels (2 = Right alignment)
		for child in $VBoxContainer.get_children():
			if child is Label and child.name != "Title":
				child.horizontal_alignment = 2

func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
