extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

@onready var scores_table = $VBoxContainer/ScrollContainer/ScoresTable

func _ready() -> void:
	Global.stop_menu_music()
	load_scores()

func load_scores() -> void:
	# Clear existing rows
	for child in scores_table.get_children():
		child.queue_free()
	
	# Add header row
	add_row(tr("Operative"), tr("Total Score"), true)
	
	# Query sessions from SessionManager
	var sessions = SessionManager.get_all_sessions()
	
	if sessions.is_empty():
		add_row("NO DATA", "0", false)
		return
		
	# Add data rows
	for session in sessions:
		var character_name = session.get("character", "Operative")
		var score = str(session.get("total_score", 0))
		add_row(character_name, score, false)

func add_row(level: String, score: String, is_header: bool) -> void:
	var row_container = HBoxContainer.new()
	row_container.custom_minimum_size = Vector2(0, 40)
	
	var level_label = Label.new()
	level_label.text = level
	level_label.custom_minimum_size = Vector2(300, 0)
	level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if is_header:
		level_label.add_theme_color_override("font_color", Color(0, 0.94, 1, 1))
		level_label.add_theme_font_size_override("font_size", 24)
	
	var score_label = Label.new()
	score_label.text = score
	score_label.custom_minimum_size = Vector2(300, 0)
	score_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if is_header:
		score_label.add_theme_color_override("font_color", Color(0, 0.94, 1, 1))
		score_label.add_theme_font_size_override("font_size", 24)
	
	row_container.add_child(level_label)
	row_container.add_child(score_label)
	
	scores_table.add_child(row_container)

func _on_back_button_pressed() -> void:
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/main_menu.tscn")
