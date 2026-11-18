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
## Shows the global leaderboard (2026-11-18): each account's best campaign score with its
## username, from the public API endpoint GET /api/game/leaderboard/. No sign-in is needed and
## no ids or emails are returned.
func load_scores() -> void:
	# Clear any existing rows from previous loads to avoid duplicates.
	for child in scores_table.get_children():
		child.queue_free()
	add_row(tr("Player"), tr("Best Score"), true)
	var request := HTTPRequest.new()
	request.timeout = 10.0
	add_child(request)
	request.request_completed.connect(_on_scores_received.bind(request))
	var url: String = AuthManager.GetApiBaseUrl() + "/game/leaderboard/"
	if request.request(url) != OK:
		_on_scores_received(HTTPRequest.RESULT_CANT_CONNECT, 0, PackedStringArray(), PackedByteArray(), request)

## Fills the table from the API answer, or shows why it could not load.
func _on_scores_received(result: int, code: int, _headers: PackedStringArray, body: PackedByteArray, request: HTTPRequest) -> void:
	request.queue_free()
	if not is_inside_tree():
		return
	if result != HTTPRequest.RESULT_SUCCESS or code != 200:
		add_row(tr("Leaderboard unavailable"), "-", false)
		return
	var rows = JSON.parse_string(body.get_string_from_utf8())
	if not (rows is Array) or rows.is_empty():
		add_row(tr("No scores yet"), "0", false)
		return
	for row in rows:
		if row is Dictionary:
			add_row("%d. %s" % [int(row.get("rank", 0)), str(row.get("username", "?"))], str(int(row.get("score", 0))), false)

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
