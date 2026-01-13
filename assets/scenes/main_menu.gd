extends Node

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")
const FallingCharacter = preload("res://assets/scenes/falling_character.tscn")

var db = null

@onready var quit_popup: Panel = $QuitPopup
@onready var quit_background: ColorRect = $QuitBackground
@onready var settings_popup: Panel = $SettingsPopup
@onready var instructions_popup: Panel = $InstructionsPopup
@onready var leaderboard_popup: Panel = $LeaderboardPopup
@onready var language_popup: Panel = $LanguagePopup
@onready var language_grid: GridContainer = $LanguagePopup/VBoxContainer/ScrollContainer/LanguageGrid
@onready var scores_table: VBoxContainer = $LeaderboardPopup/VBoxContainer/ScrollContainer/ScoresTable

var spawn_timer: float = 0.0
var spawn_interval_min: float = 0.2
var spawn_interval_max: float = 0.8
var next_spawn_time: float = 0.0


func _ready() -> void:
	Global.play_menu_music()
	_populate_language_buttons()
	_reset_spawn_timer()


func _process(delta: float) -> void:
	# Spawn falling characters
	spawn_timer += delta
	if spawn_timer >= next_spawn_time:
		_spawn_falling_character()
		_reset_spawn_timer()


func _reset_spawn_timer() -> void:
	spawn_timer = 0.0
	next_spawn_time = randf_range(spawn_interval_min, spawn_interval_max)


func _spawn_falling_character() -> void:
	var character = FallingCharacter.instantiate()
	
	# Random character selection
	var characters = ["Ninja Frog", "Mask Dude", "Pink Man", "Virtual Guy"]
	character.character_name = characters[randi() % characters.size()]
	
	# Random horizontal position across the screen
	var screen_width = get_viewport().get_visible_rect().size.x
	character.position = Vector2(randf_range(50, screen_width - 50), -50)
	
	# Random scale for variety
	var scale_factor = randf_range(0.8, 1.5)
	character.scale = Vector2(scale_factor, scale_factor)
	
	# Random fall speed
	character.fall_speed = randf_range(150, 300)
	
	# Add to scene (behind UI elements)
	add_child(character)
	move_child(character, 1)  # Move behind other UI elements


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
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/character_selection.tscn")


func _on_instructions_pressed() -> void:
	instructions_popup.visible = true


func _on_leaderboard_pressed() -> void:
	if db == null:
		_init_database()
	_load_scores()
	leaderboard_popup.visible = true


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


func _on_close_instructions_pressed() -> void:
	instructions_popup.visible = false


func _on_close_leaderboard_pressed() -> void:
	leaderboard_popup.visible = false


func _init_database() -> void:
	db = SQLite.new()
	db.path = "user://scores.db"
	db.open_db()
	
	var table_dict = {
		"id": {"data_type": "INTEGER", "primary_key": true, "auto_increment": true},
		"level_name": {"data_type": "TEXT", "not_null": true},
		"score": {"data_type": "INTEGER", "not_null": true}
	}
	db.create_table("scores", table_dict)


func _load_scores() -> void:
	# Clear existing rows
	for child in scores_table.get_children():
		child.queue_free()
	
	# Query database sorted by highest score first
	db.query("SELECT level_name, score FROM scores ORDER BY score DESC")
	var results = db.query_result
	
	# Add header row
	_add_row(tr("Level"), tr("Points"), true)
	
	# Add data rows
	for row in results:
		_add_row(row["level_name"], str(row["score"]), false)


func _add_row(level: String, score: String, is_header: bool) -> void:
	var row_container = HBoxContainer.new()
	row_container.custom_minimum_size = Vector2(0, 40)
	
	var level_label = Label.new()
	level_label.text = level
	level_label.custom_minimum_size = Vector2(300, 0)
	level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if is_header:
		level_label.add_theme_color_override("font_color", Color(0, 0.8, 0, 1))
		level_label.add_theme_font_size_override("font_size", 24)
		level_label.add_theme_font_override("font", load("res://assets/fonts/ComicNeue-Bold.ttf"))
	else:
		level_label.add_theme_color_override("font_color", Color(1, 1, 1, 1))
		level_label.add_theme_font_size_override("font_size", 20)
		level_label.add_theme_font_override("font", load("res://assets/fonts/ComicNeue-Regular.ttf"))
	
	var score_label = Label.new()
	score_label.text = score
	score_label.custom_minimum_size = Vector2(300, 0)
	score_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if is_header:
		score_label.add_theme_color_override("font_color", Color(0, 0.8, 0, 1))
		score_label.add_theme_font_size_override("font_size", 24)
		score_label.add_theme_font_override("font", load("res://assets/fonts/ComicNeue-Bold.ttf"))
	else:
		score_label.add_theme_color_override("font_color", Color(1, 1, 1, 1))
		score_label.add_theme_font_size_override("font_size", 20)
		score_label.add_theme_font_override("font", load("res://assets/fonts/ComicNeue-Regular.ttf"))
	
	row_container.add_child(level_label)
	row_container.add_child(score_label)
	
	scores_table.add_child(row_container)
