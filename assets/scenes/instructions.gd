extends Control

func _ready() -> void:
	var current_locale = TranslationServer.get_locale()
	var locale_code = current_locale.split("_")[0]  # Get language code (e.g., "ar" from "ar_SA")
	
	# Set alignment based on language - Arabic gets right alignment, others left
	for child in $VBoxContainer.get_children():
		if child is Label and child.name != "Title":
			if locale_code == "ar":
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			else:
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT

func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
