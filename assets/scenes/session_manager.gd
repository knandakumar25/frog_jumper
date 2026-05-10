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

func delete_sessions(session_ids: Array) -> void:
	for session_id in session_ids:
		delete_session(session_id)

func delete_all_sessions() -> void:
	if db == null:
		init_database()
	db.delete_rows("game_sessions", "")
	current_session_id = -1
	current_session_data = {}
	print("SessionManager: Deleted all sessions")

func has_sessions() -> bool:
	var sessions = get_all_sessions()
	return sessions.size() > 0

func format_datetime(datetime_str: String) -> String:
	# Return ISO format for universal compatibility
	# Input: "2026-02-04T14:30:45"
	# Output: "2026-02-04 14:30"
	
	if datetime_str.is_empty():
		return "Unknown"
	
	# Replace T with space and remove seconds
	var formatted = datetime_str.replace("T", " ")
	
	# Remove seconds if present (keep only YYYY-MM-DD HH:MM)
	var parts = formatted.split(":")
	if parts.size() >= 2:
		return parts[0] + ":" + parts[1]
	
	return formatted
