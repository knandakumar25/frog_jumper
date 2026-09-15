extends Area2D

# Reference to the GameManager to update the global score.
@onready var game_manager: Node = %GameManager
# Audio player for the collection sound effect.
@onready var coin_sound: AudioStreamPlayer = $CoinSound

# Signal handler for when a physics body enters the collectable area.
# If the body is a character, it triggers the collection sequence.
func _on_body_entered(body):
	if body is CharacterBody2D:
		# Play the collection sound effect.
		coin_sound.play()

		# Create a short delay timer before deleting the object and adding the point.
		# This ensures the sound can finish playing.
		var timer = Timer.new()
		timer.wait_time = 0.2
		add_child(timer)
		timer.start()

		# Wait for the timer to timeout asynchronously.
		await timer.timeout

		# Remove the collectable from the scene.
		queue_free()

		# Update the global point counter in the GameManager.
		game_manager.add_point()
