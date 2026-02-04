extends Area2D

@export var target_level : PackedScene
@export var current_level_number : int = 1  # Set this in the editor for each level
@onready var finish_audio: AudioStreamPlayer = $FinishAudio
@onready var audio_stream_player: AudioStreamPlayer = $"../../AudioStreamPlayer"

func _on_body_entered(body):
	if (body.name == "CharacterBody2D"):
		# Stop the background music
		audio_stream_player.stop()
		Global.reached_finish = true
		
		# Unlock next level
		Global.unlock_next_level(current_level_number)
		
		# Stop character movement
		if body.has_method("stop_movement"):
			body.stop_movement()

		# Play finish audio
		finish_audio.play()
		
		# Save score to database
		var level_name = get_tree().current_scene.name
		var game_manager = get_node_or_null("/root/" + get_tree().current_scene.name + "/GameManager")
		if game_manager and game_manager.has_method("save_score"):
			game_manager.save_score(level_name)
		
		# Update session progress to next level if in a session
		if SessionManager.current_session_id > 0:
			var next_level = current_level_number + 1
			var total_score = 0
			if game_manager:
				total_score = game_manager.points
			
			SessionManager.update_session(
				SessionManager.current_session_id,
				next_level,
				total_score,
				0  # collectables
			)
			print("Session updated: Level ", next_level, " Score: ", total_score)

		# Create and start a timer
		var timer = Timer.new()
		timer.wait_time = 3
		add_child(timer)
		timer.start()

		# Wait for the timer to timeout
		await timer.timeout

		# Change the scene
		Global.reached_finish = false
		# If returning to level selection, restart menu music
		if target_level.resource_path == "res://assets/scenes/level_selection.tscn":
			Global.play_menu_music()
		get_tree().change_scene_to_packed(target_level)
