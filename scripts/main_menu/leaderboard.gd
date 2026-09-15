extends Control

# Reference to the LoadingScreen helper for scene transitions.
const LoadingScreen = preload("res://scripts/ui/loading_screen.gd")

# Container where score rows will be dynamically instantiated.
@onready var scores_table = $VBoxContainer/ScrollContainer/ScoresTable

func _ready() -> void:
	# Stop background music as the leaderboard is a static data screen.
	Global.stop_menu_music()
	load_scores()

## Purpose:
## Fetches all recorded game sessions from the SessionManager and populates
## the leaderboard table with the operative's name and total score.
func load_scores() -> void:
	# Clear any existing rows from previous loads to avoid duplicates.
	for child in scores_table.get_children():
		child.queue_free()

	# Add the table header.
	add_row(tr("Operative"), tr("Total Score"), true)

	# Query all session data from the SessionManager.
	var sessions = SessionManager.get_all_sessions()

	if sessions.is_empty():
		# Display a fallback row if no game data exists.
		add_row("NO DATA", "0", false)
		return

	# Iterate through sessions and add a row for each recorded score.
	for session in sessions:
		var character_name = session.get("character", "Operative")
		var score = str(session.get("total_score", 0))
		add_row(character_name, score, false)

# Creates a visual row in the leaderboard.
# @param level: The operative's name or header label.
# @param score: The score value or header label.
# @param is_header: Whether to apply header styling (Cyan color, larger font).
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
	# Return to the main menu via the loading screen transition.
	LoadingScreen.load_scene(get_tree(), "res://scenes/main_menu/main_menu.tscn")
