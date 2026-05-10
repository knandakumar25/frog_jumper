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
	var frames = SpriteFrames.new()
	
	if character_name == "Cyber Shogun":
		var idle_texture = load("res://assets/images/A-16-bit-pixel-art-2D-side-scroller-sprite-of-a-fu-64px-9.png")
		if idle_texture == null:
			print("Error: Cyber Shogun idle texture not found.")
			return
		frames.add_animation("idle")
		frames.set_animation_loop("idle", true)
		frames.set_animation_speed("idle", 10.0)
		var frame_width = 61
		var frame_height = 71
		var columns = 3
		var frame_count = 9
		for i in range(frame_count):
			var atlas = AtlasTexture.new()
			atlas.atlas = idle_texture
			var x = (i % columns) * frame_width
			var y = int(i / columns) * frame_height
			atlas.region = Rect2(x, y, frame_width, frame_height)
			frames.add_frame("idle", atlas)
	else:
		var base_path = "res://assets/images/" + character_name + "/"
		# Load idle animation
		var idle_texture = load(base_path + "Idle (32x32).png")
		if idle_texture == null:
			print("Error: Idle texture not found for ", character_name)
			return
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
	var viewport_height = get_viewport_rect().size.y
	if position.y > viewport_height + 50:
		queue_free()
