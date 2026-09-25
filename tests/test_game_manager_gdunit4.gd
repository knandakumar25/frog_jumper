class_name TestGameManagerGdUnit4
extends GdUnitTestSuite

## GdUnit4 Test Suite for GameManager
const GameManagerScript = preload("res://scripts/managers/GameManager.gd")
var _manager: Node = null


func before_test() -> void:
	_manager = auto_free(GameManagerScript.new())
	add_child(_manager)


func test_initial_state() -> void:
	assert_that(_manager.current_state).is_equal(GameManagerScript.State.MAIN_MENU)
	assert_that(_manager.player_username).is_equal("")
	assert_that(_manager.player_level).is_equal(1)


func test_valid_state_transitions() -> void:
	# Story intro transition
	_manager.set_game_state(GameManagerScript.State.STORY_INTRO)
	assert_that(_manager.current_state).is_equal(GameManagerScript.State.STORY_INTRO)

	# Gameplay transition
	_manager.set_game_state(GameManagerScript.State.GAMEPLAY)
	assert_that(_manager.current_state).is_equal(GameManagerScript.State.GAMEPLAY)

	# Pause transition
	_manager.set_game_state(GameManagerScript.State.PAUSED)
	assert_that(_manager.current_state).is_equal(GameManagerScript.State.PAUSED)

	# Game over transition
	_manager.set_game_state(GameManagerScript.State.GAME_OVER)
	assert_that(_manager.current_state).is_equal(GameManagerScript.State.GAME_OVER)


func test_player_data_persistence_across_states() -> void:
	_manager.player_username = "CyberShogun"
	_manager.player_level = 9

	_manager.set_game_state(GameManagerScript.State.GAMEPLAY)
	assert_that(_manager.player_username).is_equal("CyberShogun")
	assert_that(_manager.player_level).is_equal(9)

	_manager.set_game_state(GameManagerScript.State.PAUSED)
	assert_that(_manager.player_username).is_equal("CyberShogun")
	assert_that(_manager.player_level).is_equal(9)


func test_invalid_state_transition_is_rejected() -> void:
	var initial_state = _manager.current_state
	# Invalid dynamic calls with out-of-range state numbers
	_manager.call("set_game_state", -1)
	assert_that(_manager.current_state).is_equal(initial_state)

	_manager.call("set_game_state", 999)
	assert_that(_manager.current_state).is_equal(initial_state)
