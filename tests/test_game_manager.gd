extends SceneTree
## Run with: godot --headless --path . --script tests/test_game_manager.gd
## Exercises a fresh manager; configured project autoloads also run on startup.

var _failures: int = 0
var _emitted_states: Array[int] = []


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var manager_script: GDScript = load("res://scripts/managers/GameManager.gd")
	if manager_script == null or not manager_script.can_instantiate():
		push_error("GameManager script could not be loaded.")
		quit(1)
		return
	var manager = manager_script.new()
	root.add_child(manager)
	manager.state_changed.connect(func(new_state: int) -> void:
		_emitted_states.append(new_state)
		_check(manager.current_state == new_state, "State must update before signal emission.")
	)

	_check(manager.current_state == manager_script.State.MAIN_MENU, "Default state must be MAIN_MENU.")
	_check(manager.player_username == "", "Default username must be empty.")
	_check(manager.player_level == 1, "Default player level must be 1.")
	manager.player_username = "CyberShogun"
	manager.player_level = 7
	manager.set_game_state(manager_script.State.MAIN_MENU)
	_check(_emitted_states.is_empty(), "Setting the initial state again must not emit a change.")

	var expected_states: Array[int] = [
		manager_script.State.STORY_INTRO,
		manager_script.State.GAMEPLAY,
		manager_script.State.PAUSED,
		manager_script.State.GAMEPLAY,
		manager_script.State.GAME_OVER,
		manager_script.State.MAIN_MENU,
	]
	for next_state in expected_states:
		manager.set_game_state(next_state)
		_check(manager.current_state == next_state, "Requested state must become current.")
		var event_count: int = _emitted_states.size()
		manager.set_game_state(next_state)
		_check(_emitted_states.size() == event_count, "Repeated states must not emit duplicate events.")
	_check(_emitted_states == expected_states, "Transitions must emit once, in order.")
	_check(manager.player_username == "CyberShogun", "Username must persist across transitions.")
	_check(manager.player_level == 7, "Player level must persist across transitions.")

	# Dynamic calls exercise invalid enum input from external callers.
	manager.call("set_game_state", -1)
	manager.call("set_game_state", 999)
	_check(manager.current_state == manager_script.State.MAIN_MENU, "Unknown states must not corrupt current state.")
	_check(_emitted_states == expected_states, "Unknown states must not emit state changes.")

	paused = true
	_check(manager.can_process(), "Manager must remain available while the scene tree is paused.")
	manager.set_game_state(manager_script.State.PAUSED)
	manager.set_game_state(manager_script.State.GAMEPLAY)
	_check(manager.current_state == manager_script.State.GAMEPLAY, "Manager must accept transitions while paused.")
	paused = false
	manager.free()
	if _failures == 0:
		print("GameManager checks passed.")
	quit(0 if _failures == 0 else 1)


func _check(condition: bool, message: String) -> void:
	if not condition:
		_failures += 1
		push_error(message)
