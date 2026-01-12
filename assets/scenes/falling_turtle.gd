extends AnimatedSprite2D

var fall_speed: float = 200.0
var rotation_speed: float = 2.0

func _ready() -> void:
	play("idle")
	# Random rotation direction
	rotation_speed *= 1 if randf() > 0.5 else -1

func _process(delta: float) -> void:
	# Fall down
	position.y += fall_speed * delta
	
	# Rotate while falling
	rotation += rotation_speed * delta
	
	# Remove when off screen (below viewport)
	if position.y > 800:
		queue_free()
