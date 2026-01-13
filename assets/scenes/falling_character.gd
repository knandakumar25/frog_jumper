extends AnimatedSprite2D

var fall_speed: float = 200.0
var rotation_speed: float = 2.0
var character_name: String = "Ninja Frog"

func _ready() -> void:
	_load_character_sprite()
	play("idle")
	# Random rotation direction
	rotation_speed *= 1 if randf() > 0.5 else -1


func _load_character_sprite() -> void:
	var base_path = "res://assets/images/" + character_name + "/"
	var frames = SpriteFrames.new()
	
	# Load idle animation
	var idle_texture = load(base_path + "Idle (32x32).png")
	frames.add_animation("idle")
	frames.set_animation_loop("idle", true)
	frames.set_animation_speed("idle", 10.0)
	
	for i in range(11):
		var atlas = AtlasTexture.new()
		atlas.atlas = idle_texture
		atlas.region = Rect2(i * 32, 0, 32, 32)
		frames.add_frame("idle", atlas)
	
	self.sprite_frames = frames


func _process(delta: float) -> void:
	# Fall down
	position.y += fall_speed * delta
	
	# Rotate while falling
	rotation += rotation_speed * delta
	
	# Remove when off screen (below viewport)
	if position.y > 800:
		queue_free()
