extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

func _ready() -> void:
	Global.play_menu_music()
	
	var current_locale = TranslationServer.get_locale()
	var locale_code = current_locale.split("_")[0]
	
	# Set alignment based on language - Arabic gets right alignment, others left
	for child in $VBoxContainer.get_children():
		if child is Label and child.name != "Title":
			if locale_code == "arabic":
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			else:
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT

func _on_back_button_pressed() -> void:
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/main_menu.tscn")
