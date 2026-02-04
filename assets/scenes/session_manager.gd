extends Node

# Singleton for managing game sessions using SQLite
var db: SQLite = null
var db_path: String = "user://game_sessions.db"

# Current active session
var current_session_id: int = -1
var current_session_data: Dictionary = {}

func _ready() -> void:
	init_database()

func init_database() -> void:
	db = SQLite.new()
	db.path = db_path
	db.open_db()
	
	# Create game_sessions table
	var table_dict = {
		"id": {"data_type": "INTEGER", "primary_key": true, "auto_increment": true},
		"character": {"data_type": "TEXT", "not_null": true},
		"current_level": {"data_type": "INTEGER", "not_null": true, "default": 1},
		"total_score": {"data_type": "INTEGER", "not_null": true, "default": 0},
		"collectables": {"data_type": "INTEGER", "not_null": true, "default": 0},
		"created_at": {"data_type": "TEXT", "not_null": true},
		"last_played": {"data_type": "TEXT", "not_null": true}
	}
	db.create_table("game_sessions", table_dict)
	print("SessionManager: Database initialized at ", db_path)

func create_new_session(character_name: String) -> int:
	if db == null:
		init_database()
	
	var current_time = Time.get_datetime_string_from_system()
	
	var data_dict = {
		"character": character_name,
		"current_level": 1,
		"total_score": 0,
		"collectables": 0,
		"created_at": current_time,
		"last_played": current_time
	}
	
	db.insert_row("game_sessions", data_dict)
	
	# Get the ID of the newly created session
	var query = "SELECT id FROM game_sessions ORDER BY id DESC LIMIT 1"
	var result = db.select_rows("game_sessions", "id = (SELECT MAX(id) FROM game_sessions)", ["id"])
	
	if result.size() > 0:
		current_session_id = result[0]["id"]
		current_session_data = get_session(current_session_id)
		print("SessionManager: Created new session with ID ", current_session_id)
		return current_session_id
	
	return -1

func get_all_sessions() -> Array:
	if db == null:
		init_database()
	
	# Get all sessions ordered by last_played descending
	var result = db.select_rows("game_sessions", "", ["*"])
	
	# Sort by last_played descending (most recent first)
	result.sort_custom(func(a, b): return a["last_played"] > b["last_played"])
	
	return result

func get_session(session_id: int) -> Dictionary:
	if db == null:
		init_database()
	
	var result = db.select_rows("game_sessions", "id = " + str(session_id), ["*"])
	
	if result.size() > 0:
		return result[0]
	
	return {}

func load_session(session_id: int) -> bool:
	var session_data = get_session(session_id)
	
	if session_data.is_empty():
		print("SessionManager: Session ", session_id, " not found")
		return false
	
	current_session_id = session_id
	current_session_data = session_data
	
	# Update last_played time
	update_last_played(session_id)
	
	print("SessionManager: Loaded session ", session_id)
	return true

func update_session(session_id: int, level: int, score: int, collectables: int) -> void:
	if db == null:
		init_database()
	
	var current_time = Time.get_datetime_string_from_system()
	
	var update_dict = {
		"current_level": level,
		"total_score": score,
		"collectables": collectables,
		"last_played": current_time
	}
	
	db.update_rows("game_sessions", "id = " + str(session_id), update_dict)
	
	# Update current session data if this is the active session
	if session_id == current_session_id:
		current_session_data = get_session(session_id)
	
	print("SessionManager: Updated session ", session_id)

func update_last_played(session_id: int) -> void:
	if db == null:
		init_database()
	
	var current_time = Time.get_datetime_string_from_system()
	
	var update_dict = {
		"last_played": current_time
	}
	
	db.update_rows("game_sessions", "id = " + str(session_id), update_dict)

func delete_session(session_id: int) -> void:
	if db == null:
		init_database()
	
	db.delete_rows("game_sessions", "id = " + str(session_id))
	
	if session_id == current_session_id:
		current_session_id = -1
		current_session_data = {}
	
	print("SessionManager: Deleted session ", session_id)

func has_sessions() -> bool:
	var sessions = get_all_sessions()
	return sessions.size() > 0

func format_datetime(datetime_str: String) -> String:
	# Convert ISO datetime to readable format
	# Input: "2026-02-04T14:30:45"
	# Output: "Feb 4, 2026 2:30 PM"
	
	if datetime_str.is_empty():
		return "Unknown"
	
	var parts = datetime_str.split("T")
	if parts.size() != 2:
		return datetime_str
	
	var date_parts = parts[0].split("-")
	var time_parts = parts[1].split(":")
	
	if date_parts.size() != 3 or time_parts.size() < 2:
		return datetime_str
	
	var year = date_parts[0]
	var month = int(date_parts[1])
	var day = date_parts[2]
	var hour = int(time_parts[0])
	var minute = time_parts[1]
	
	var month_names = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
	var month_name = month_names[month - 1] if month >= 1 and month <= 12 else str(month)
	
	var am_pm = "AM"
	var display_hour = hour
	if hour >= 12:
		am_pm = "PM"
		if hour > 12:
			display_hour = hour - 12
	elif hour == 0:
		display_hour = 12
	
	return month_name + " " + day + ", " + year + " " + str(display_hour) + ":" + minute + " " + am_pm
