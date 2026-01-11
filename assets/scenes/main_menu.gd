extends Node


@onready var quit_popup: Panel = $QuitPopup
@onready var quit_background: ColorRect = $QuitBackground
@onready var settings_popup: Panel = $SettingsPopup
@onready var language_popup: Panel = $LanguagePopup
@onready var language_grid: GridContainer = $LanguagePopup/VBoxContainer/ScrollContainer/LanguageGrid


func _ready() -> void:
	Global.play_menu_music()
	_populate_language_buttons()


func _populate_language_buttons() -> void:
	var audio_options = $SettingsPopup/VBoxContainer/AudioOptions
	var languages = audio_options.languages
	
	# Clear existing buttons
	for child in language_grid.get_children():
		child.queue_free()
	
	# Add English first
	_create_language_button("English (English)", languages["English (English)"])
	
	# Sort remaining languages alphabetically
	var sorted_langs = []
	for lang_name in languages.keys():
		if lang_name != "English (English)":
			sorted_langs.append(lang_name)
	sorted_langs.sort()
	
	# Add sorted languages
	for lang_name in sorted_langs:
		_create_language_button(lang_name, languages[lang_name])


func _create_language_button(lang_name: String, _locale: String) -> void:
	var button = Button.new()
	button.text = lang_name
	button.custom_minimum_size = Vector2(370, 50)
	button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	button.clip_text = false
	button.text_overrun_behavior = TextServer.OVERRUN_NO_TRIMMING
	button.add_theme_color_override("font_color", Color(1, 1, 1, 1))
	button.add_theme_color_override("font_hover_color", Color(1, 1, 1, 1))
	button.add_theme_color_override("font_pressed_color", Color(0.9, 0.9, 0.9, 1))
	button.add_theme_font_size_override("font_size", 20)
	
	# Get StyleBox from scene
	var normal_style = $ButtonContainer/Row2/Settings.get_theme_stylebox("normal", "Button")
	var hover_style = $ButtonContainer/Row2/Settings.get_theme_stylebox("hover", "Button")
	button.add_theme_stylebox_override("normal", normal_style)
	button.add_theme_stylebox_override("hover", hover_style)
	button.add_theme_stylebox_override("pressed", normal_style)
	
	button.pressed.connect(_on_language_selected.bind(lang_name))
	language_grid.add_child(button)


func _on_language_selected(lang_name: String) -> void:
	var audio_options = $SettingsPopup/VBoxContainer/AudioOptions
	audio_options.selected_language = lang_name
	$SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageButton.text = lang_name
	language_popup.visible = false


func _on_language_button_pressed() -> void:
	language_popup.visible = true


func _on_close_language_popup_pressed() -> void:
	language_popup.visible = false


func _on_play_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/level_selection.tscn")


func _on_instructions_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/instructions.tscn")


func _on_leaderboard_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/leaderboard.tscn")


func _on_settings_pressed() -> void:
	settings_popup.visible = true


func _on_apply_settings_pressed() -> void:
	# Apply volume settings
	var slider = $SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/MasterSlider
	AudioServer.set_bus_volume_db(0, linear_to_db(slider.value))
	
	# Apply language settings
	var audio_options = $SettingsPopup/VBoxContainer/AudioOptions
	audio_options.apply_language_change()
	
	# Close the popup
	settings_popup.visible = false


func _on_close_settings_pressed() -> void:
	settings_popup.visible = false


func _on_yes_pressed() -> void:
	get_tree().quit()


func _on_no_pressed() -> void:
	quit_popup.visible = false
	quit_background.visible = false


func _on_quit_button_pressed() -> void:
	quit_background.visible = true
	quit_popup.visible = true
