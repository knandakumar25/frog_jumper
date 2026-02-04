extends Node

@onready var points_label: Label = %PointsLabel

var points = 0
var is_reloading = false
var db: SQLite
var db_path = "user://scores.db"

func _ready():
	points_label.text = tr("Points") + ": " + str(points)
	init_database()

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

func save_score(level_name: String) -> void:
	if db == null:
		return
	
	# Insert score into database
	var data_dict = {
		"level_name": level_name,
		"score": points
	}
	db.insert_row("scores", data_dict)
	
	# Also update the session if one is active
	if SessionManager.current_session_id > 0:
		# Extract level number from level_name (e.g., "level1" -> 1)
		var level_num = 1
		if level_name.begins_with("level"):
			level_num = int(level_name.substr(5))
		
		# Update session with current progress
		SessionManager.update_session(
			SessionManager.current_session_id,
			level_num,
			points,
			0  # collectables - can be updated if tracking this
		)

func add_point():
	points += 1
	print(points)
	points_label.text = tr("Points") + ": " + str(points)


func _on_water_body_entered(body: Node2D) -> void:
	if is_reloading:
		return
	
	if body is CharacterBody2D:
		is_reloading = true
		get_tree().call_deferred("reload_current_scene")
