extends Area2D

@export var target_level : PackedScene
@export var current_level_number : int = 1  # Set this in the editor for each level
@onready var finish_audio: AudioStreamPlayer = $FinishAudio
@onready var audio_stream_player: AudioStreamPlayer = $"../../AudioStreamPlayer"

func _on_body_entered(body):
	if body is CharacterBody2D:
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
		
		# Advance the saved mission if in a session. The score was already added by save_score
		# above, so only the level moves here (adding it again counted it twice; fixed 2026-11-18).
		if SessionManager.CurrentSessionId > 0:
			var next_level = current_level_number + 1
			SessionManager.update_session(
				SessionManager.CurrentSessionId,
				next_level,
				0,  # score already saved
				0  # collectables
			)
			print("Session updated: Level ", next_level)

		# Check for CyberVictoryScorecard in current level
		var scorecard = get_tree().current_scene.get_node_or_null("CyberVictoryScorecard")
		if scorecard and scorecard.has_method("ShowScorecard"):
			var total_points = 0
			if game_manager:
				total_points = game_manager.Points
			var hud = get_tree().current_scene.get_node_or_null("CyberCombatHud")
			var max_combo = 1
			if hud and "ComboCount" in hud:
				max_combo = hud.ComboCount
			var op_title = "KAITO KUROGANE // CYBER SHOGUN"
			var player = get_tree().current_scene.find_child("CyberShogun", true, false)
			if player and "ActiveProfile" in player and player.ActiveProfile != null:
				op_title = str(player.ActiveProfile.CivilianName).to_upper() + " // " + str(player.ActiveProfile.DisplayName).to_upper()
			scorecard.ShowScorecard(total_points, max_combo, op_title)
			return

		# Create and start a timer
		var timer = Timer.new()
		timer.wait_time = 3
		add_child(timer)
		timer.start()

		# Wait for the timer to timeout
		await timer.timeout
		timer.queue_free()

		# Change the scene
		Global.reached_finish = false
		get_tree().change_scene_to_packed(target_level)
