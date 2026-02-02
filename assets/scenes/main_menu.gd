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


var cloud_scene: Texture2D = preload("res://assets/images/clouds.png")
var max_clouds: int = 8
var clouds: Array = []

# Cloud animation enhancement variables
var cloud_vertical_drift: Dictionary = {}
var cloud_drift_speed: Dictionary = {}
var cloud_drift_offset: Dictionary = {}

# Falling character spawn variables
var spawn_timer: float = 0.0
var spawn_interval_min: float = 0.5
var spawn_interval_max: float = 2.0
var next_spawn_time: float = 0.0






func _ready() -> void:
	print("main_menu: _ready() start")
	Global.play_menu_music()
	_populate_language_buttons()
	_setup_floating_title()
	_reset_spawn_timer()

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
		# Clouds are only spawned at random positions at the start, never at random times.
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
		
		# Assign cloud to a layer (front, middle, or back) for parallax
		var layer = i % 3
		var y_pos = randf_range(40 + layer * 60, screen_height * 0.5)
		sprite.position = Vector2(randf_range(0, screen_width), y_pos)
		
		# Scale and opacity based on depth (further = smaller and more transparent)
		var depth_factor = 1.0 - (layer * 0.2)
		var base_scale = randf_range(0.7, 1.3) * depth_factor
		sprite.scale = Vector2(base_scale, base_scale)
		sprite.modulate.a = randf_range(0.5, 0.8) * depth_factor
		
		# Speed based on parallax layer (closer = faster)
		var base_speed = 80 - (layer * 25)
		sprite.set_meta("speed", randf_range(base_speed - 15, base_speed + 15))
		sprite.set_meta("layer", layer)
		sprite.set_meta("base_y", y_pos)
		
		# Setup vertical drift
		cloud_drift_offset[sprite] = randf_range(0, TAU)
		cloud_drift_speed[sprite] = randf_range(0.3, 0.7)
		cloud_vertical_drift[sprite] = randf_range(5, 12)
		
		# Fade in smoothly
		var start_alpha = sprite.modulate.a
		sprite.modulate.a = 0.0
		var tween = create_tween()
		if tween:
			tween.tween_property(sprite, "modulate:a", start_alpha, randf_range(1.5, 3.0))
		
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
	label.add_theme_color_override("font_color", Color(1, 1, 1, 1))
	label.add_theme_color_override("font_outline_color", Color(0, 0.4, 0, 1))
	label.add_theme_constant_override("outline_size", 12)
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
	# Animate clouds with parallax and vertical drift
	var screen_width = get_viewport().get_visible_rect().size.x
	var screen_height = get_viewport().get_visible_rect().size.y
	var time = Time.get_ticks_msec() / 1000.0
	
	for cloud in clouds:
		# Horizontal movement (parallax)
		cloud.position.x -= cloud.get_meta("speed") * delta
		
		# Gentle vertical drift
		if cloud in cloud_vertical_drift:
			var base_y = cloud.get_meta("base_y")
			var drift_amount = cloud_vertical_drift[cloud]
			var drift_speed = cloud_drift_speed[cloud]
			var offset = cloud_drift_offset[cloud]
			cloud.position.y = base_y + sin(time * drift_speed + offset) * drift_amount
		
		# Recycle clouds that move off screen
		if cloud.texture and cloud.position.x < -cloud.texture.get_size().x * cloud.scale.x:
			# Reposition to the right
			cloud.position.x = screen_width + randf_range(50, 300)
			
			# Reassign layer and properties
			var layer = cloud.get_meta("layer")
			var y_pos = randf_range(40 + layer * 60, screen_height * 0.5)
			cloud.set_meta("base_y", y_pos)
			
			# Update scale and opacity based on depth
			var depth_factor = 1.0 - (layer * 0.2)
			var base_scale = randf_range(0.7, 1.3) * depth_factor
			cloud.scale = Vector2(base_scale, base_scale)
			
			# Update speed for this layer
			var base_speed = 80 - (layer * 25)
			cloud.set_meta("speed", randf_range(base_speed - 15, base_speed + 15))
			
			# Reset drift parameters
			cloud_drift_offset[cloud] = randf_range(0, TAU)
			cloud_drift_speed[cloud] = randf_range(0.3, 0.7)
			cloud_vertical_drift[cloud] = randf_range(5, 12)
			
			# Smooth fade in
			var target_alpha = randf_range(0.5, 0.8) * depth_factor
			cloud.modulate.a = 0.0
			var tween = create_tween()
			if tween:
				tween.tween_property(cloud, "modulate:a", target_alpha, randf_range(2.0, 4.0))

	# Spawn falling characters at random intervals
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
	
	# Random scale for variety - increased size
	var scale_factor = randf_range(1.5, 2.5)
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
	
	# Add English first by finding the locale code 'english' in the languages map
	var english_key: String = ""
	for key in languages.keys():
		if languages[key] == "english":
			english_key = key
			break

	if english_key != "":
		_create_language_button(english_key, languages[english_key])

	# Sort remaining languages alphabetically (excluding the English entry we used)
	var sorted_langs: Array = []
	for lang_name in languages.keys():
		if lang_name != english_key:
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
	
	# If the language uses a right-to-left script, set text_direction so it renders correctly
	var rtl_locales = ["arabic", "hebrew"]
	var is_rtl = _locale != null and _locale.to_lower() in rtl_locales

	button.pressed.connect(_on_language_selected.bind(lang_name))
	language_grid.add_child(button)

	# Wait a frame so the Button's internal children (Label) are created, then apply RTL
	await get_tree().process_frame

	# Debug info to help diagnose RTL issues
	print("create_language_button: added", lang_name, "locale=", _locale, "is_rtl=", is_rtl)
	for child in button.get_children():
		print("  child: ", child, typeof(child))
		if child is Label:
			print("    before: text_direction=", child.text_direction, "h_align=", child.horizontal_alignment)
			if is_rtl:
				child.text_direction = 2 # TEXT_DIRECTION_RTL
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			print("    after: text_direction=", child.text_direction, "h_align=", child.horizontal_alignment)
		elif typeof(child) == TYPE_OBJECT and child.has_method("set_text_direction"):
			if is_rtl:
				child.text_direction = 2


func _on_language_selected(lang_name: String) -> void:
	var audio_options = $SettingsPopup/VBoxContainer/AudioOptions
	audio_options.selected_language = lang_name
	var lang_button = $SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageButton
	lang_button.text = lang_name
	# Set settings language button to RTL if needed
	var rtl_locales = ["arabic", "hebrew"]
	var locale = ""
	if audio_options.languages.has(lang_name):
		locale = audio_options.languages[lang_name]
	if locale != "" and locale.to_lower() in rtl_locales:
		# Attempt to set internal label direction
		for child in lang_button.get_children():
			if child is Label:
				child.text_direction = 2
				child.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			elif typeof(child) == TYPE_OBJECT and child.has_method("set_text_direction"):
				child.text_direction = 2
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
