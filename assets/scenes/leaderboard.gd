extends Control

var db: SQLite
var db_path = "user://scores.db"

@onready var scores_table = $VBoxContainer/ScrollContainer/ScoresTable

func _ready() -> void:
	init_database()
	load_scores()

func init_database() -> void:
	db = SQLite.new()
	db.path = db_path
	db.open_db()
	
	# Create scores table if it doesn't exist
	var table_dict = {
		"id": {"data_type": "INTEGER", "primary_key": true, "auto_increment": true},
		"level_name": {"data_type": "TEXT", "not_null": true},
		"score": {"data_type": "INTEGER", "not_null": true}
	}
	db.create_table("scores", table_dict)

func load_scores() -> void:
	# Clear existing rows
	for child in scores_table.get_children():
		child.queue_free()
	
	# Query database sorted by highest score first
	db.query("SELECT level_name, score FROM scores ORDER BY score DESC")
	var results = db.query_result
	
	# Add header row
	add_row(tr("Level"), tr("Points"), true)
	
	# Add data rows
	for row in results:
		add_row(row["level_name"], str(row["score"]), false)

func add_row(level: String, score: String, is_header: bool) -> void:
	var row_container = HBoxContainer.new()
	row_container.custom_minimum_size = Vector2(0, 40)
	
	var level_label = Label.new()
	level_label.text = level
	level_label.custom_minimum_size = Vector2(300, 0)
	level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if is_header:
		level_label.add_theme_color_override("font_color", Color(0, 0.4, 0, 1))
		level_label.add_theme_font_size_override("font_size", 24)
	
	var score_label = Label.new()
	score_label.text = score
	score_label.custom_minimum_size = Vector2(300, 0)
	score_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if is_header:
		score_label.add_theme_color_override("font_color", Color(0, 0.4, 0, 1))
		score_label.add_theme_font_size_override("font_size", 24)
	
	row_container.add_child(level_label)
	row_container.add_child(score_label)
	
	scores_table.add_child(row_container)

func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://assets/scenes/main_menu.tscn")
