extends Control

func _ready() -> void:
	var current_locale = TranslationServer.get_locale()
	print("Current locale: ", current_locale)
	var locale_code = current_locale.split("_")[0]  # Get language code (e.g., "ar" from "ar_SA")
	print("Locale code: ", locale_code)
	print("Is Arabic? ", locale_code == "ar")
	
	# Set alignment based on language - Arabic gets right alignment, others left
	for child in $VBoxContainer.get_children():
		print("Checking child: ", child.name, " Type: ", child.get_class())
		if child is Label and child.name != "Title":
			print("Processing label: ", child.name)
			if locale_code == "ar":
				print("Setting RIGHT alignment for: ", child.name)
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			else:
				print("Setting LEFT alignment for: ", child.name)
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
			print("Final alignment value: ", child.horizontal_alignment)

func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
