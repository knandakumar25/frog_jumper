extends Node
## Persistent application state. Register this script as the GameManager autoload.

enum State {
	MAIN_MENU,
	STORY_INTRO,
	GAMEPLAY,
	PAUSED,
	GAME_OVER,
}

signal state_changed(new_state: State)

var current_state: State = State.MAIN_MENU
var player_username: String = ""
var player_level: int = 1


func _init() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS


func set_game_state(new_state: State) -> void:
	if not State.values().has(new_state):
		push_warning("GameManager ignored unknown game state: %s" % new_state)
		return
	if current_state == new_state:
		return
	current_state = new_state
	state_changed.emit(current_state)
