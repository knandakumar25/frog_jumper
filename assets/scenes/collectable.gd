extends Area2D

@onready var game_manager: Node = %GameManager
@onready var coin_sound: AudioStreamPlayer = $CoinSound

func _on_body_entered(body):
	if body is CharacterBody2D:
		coin_sound.play()
		# Create and start a timer
		var timer = Timer.new()
		timer.wait_time = 0.2
		add_child(timer)
		timer.start()

		# Wait for the timer to timeout
		await timer.timeout
		queue_free()
		game_manager.add_point()
