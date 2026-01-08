extends Control

func _ready() -> void:
	push_warning("=== INSTRUCTIONS SCREEN _ready() CALLED ===")
	var current_locale = TranslationServer.get_locale()
	push_warning("Current locale: " + current_locale)
	var locale_code = current_locale.split("_")[0]  # Get language code (e.g., "ar" from "ar_SA")
	push_warning("Locale code: " + locale_code)
	push_warning("Is Arabic? " + str(locale_code == "ar"))
	
	# Set alignment based on language - Arabic gets right alignment, others left
	for child in $VBoxContainer.get_children():
		push_warning("Checking child: " + child.name + " Type: " + child.get_class())
		if child is Label and child.name != "Title":
			push_warning("Processing label: " + child.name)
			if locale_code == "ar":
				push_warning("Setting RIGHT alignment for: " + child.name)
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			else:
				push_warning("Setting LEFT alignment for: " + child.name)
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
			push_warning("Final alignment value: " + str(child.horizontal_alignment))

func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
