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
@onready var title_container: HBoxContainer = $TitleContainer


# Cloud animation variables
var cloud_scene: Texture2D = preload("res://assets/images/clouds.png")
var cloud_spawn_timer: float = 0.0
var cloud_spawn_interval: float = 1.0
var max_clouds: int = 6
var clouds: Array = []

var spawn_timer: float = 0.0
var spawn_interval_min: float = 0.2
var spawn_interval_max: float = 0.8
var next_spawn_time: float = 0.0


func _ready() -> void:
	print("main_menu: _ready() start")
	Global.play_menu_music()
	_populate_language_buttons()
	_reset_spawn_timer()
	_setup_floating_title()

	# Ensure CloudsLayer exists; create it if missing so clouds can be spawned
	if not has_node("CloudsLayer"):
		print("main_menu: CloudsLayer not found — creating runtime CloudsLayer node")
		var created_layer = Node2D.new()
		created_layer.name = "CloudsLayer"
		add_child(created_layer)
		# Place behind UI but above background (try index 1)
		var target_index = clamp(1, 0, get_child_count() - 1)
		move_child(created_layer, target_index)

	if cloud_scene == null:
		push_error("main_menu: cloud_scene texture failed to load (null).")
	else:
		_spawn_initial_clouds()

	print("main_menu: _ready() complete")

func _spawn_initial_clouds() -> void:
	if not has_node("CloudsLayer"):
		push_error("main_menu: _spawn_initial_clouds called but CloudsLayer missing. Aborting spawn.")
		return
	if cloud_scene == null:
		push_error("main_menu: _spawn_initial_clouds: cloud_scene is null. Aborting cloud spawn.")
		return

	var clouds_layer = $CloudsLayer
	var screen_width = get_viewport().get_visible_rect().size.x
	var screen_height = get_viewport().get_visible_rect().size.y
	for i in range(max_clouds):
		var sprite = Sprite2D.new()
		sprite.texture = cloud_scene
		sprite.position = Vector2(randf_range(0, screen_width), randf_range(40, screen_height * 0.5))
		sprite.scale = Vector2(randf_range(0.7, 1.2), randf_range(0.7, 1.2))
		sprite.modulate.a = randf_range(0.7, 1.0)
		sprite.set_meta("speed", randf_range(30, 90))
		clouds_layer.add_child(sprite)
		clouds.append(sprite)


func _setup_floating_title() -> void:
	# Get translated title text
	var title_text = tr("PIXEL JUMPERS")
	
	# Load ComicNeue-Bold font
	var bold_font = load("res://assets/fonts/ComicNeue-Bold.ttf")
	
	# Clear any existing children
	for child in title_container.get_children():
		child.queue_free()
	
	# Create a wrapper control to handle scaling without container interference
	var wrapper = Control.new()
	wrapper.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	wrapper.custom_minimum_size = Vector2(0, 120)
	title_container.add_child(wrapper)
	
	# Create a single label for the entire title
	var label = Label.new()
	label.text = title_text
	label.add_theme_color_override("font_color", Color(0, 0.6, 0, 1))
	label.add_theme_font_override("font", bold_font)
	label.add_theme_font_size_override("font_size", 80)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.anchor_left = 0.5
	label.anchor_right = 0.5
	label.anchor_top = 0.5
	label.anchor_bottom = 0.5
	label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	label.grow_vertical = Control.GROW_DIRECTION_BOTH
	
	wrapper.add_child(label)
	
	# Need to wait a frame for the label to get its proper size
	await get_tree().process_frame
	
	# Set pivot offset to center of label for proper scaling
	label.pivot_offset = label.size / 2
	
	# Animate the entire title to float
	_animate_floating_title(label)


func _animate_floating_title(label: Label) -> void:
	# Initial zoom-in animation when game launches
	label.scale = Vector2(0.1, 0.1)  # Start very small
	label.modulate.a = 0.0  # Start invisible
	
	var zoom_tween = create_tween()
	zoom_tween.set_parallel(true)  # Run scale and fade in parallel
	zoom_tween.tween_property(label, "scale", Vector2(1.0, 1.0), 1.5).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	zoom_tween.tween_property(label, "modulate:a", 1.0, 1.0).set_ease(Tween.EASE_OUT)
	
	# Wait for zoom to complete, then start floating animation
	await zoom_tween.finished
	
	# Create continuous vertical floating animation (no rotation)
	var float_tween = create_tween()
	float_tween.set_loops()
	
	# Store original position
	var original_y = label.position.y
	
	# Float up and down smoothly
	float_tween.tween_property(label, "position:y", original_y - 15, 2.0).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	float_tween.tween_property(label, "position:y", original_y + 15, 2.0).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)


func _process(delta: float) -> void:
	# Animate clouds
	var screen_width = get_viewport().get_visible_rect().size.x
	var screen_height = get_viewport().get_visible_rect().size.y
	for cloud in clouds:
		cloud.position.x -= cloud.get_meta("speed") * delta
		if cloud.texture and cloud.position.x < -cloud.texture.get_size().x * cloud.scale.x:
			# Recycle cloud to right
			cloud.position.x = screen_width + randf_range(0, 200)
			cloud.position.y = randf_range(40, screen_height * 0.5)
			cloud.scale = Vector2(randf_range(0.7, 1.2), randf_range(0.7, 1.2))
			cloud.modulate.a = randf_range(0.7, 1.0)
			cloud.set_meta("speed", randf_range(30, 90))
		else:
			# If texture missing, skip recycling logic for this cloud
			continue

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
	
	# Update title with new language
	_setup_floating_title()
	
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
