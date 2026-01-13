extends CharacterBody2D

const SPEED = 400.0
const JUMP_VELOCITY = -1000.0
var is_moving = true  # Flag to control movement
@onready var sprite_2d: AnimatedSprite2D = $Sprite2D
@onready var jump_sound: AudioStreamPlayer = $JumpSound


func _ready() -> void:
	_load_character_sprites()


func _load_character_sprites() -> void:
	var character = Global.selected_character
	var base_path = "res://assets/images/" + character + "/"
	
	# Create new sprite frames
	var sprite_frames = SpriteFrames.new()
	
	# Load idle animation
	var idle_texture = load(base_path + "Idle (32x32).png")
	_add_animation_frames(sprite_frames, "default", idle_texture, 11, 10.0)
	_add_animation_frames(sprite_frames, "idle", idle_texture, 11, 10.0)
	_add_animation_frames(sprite_frames, "finished", idle_texture, 11, 10.0)
	
	# Load jump animation
	var jump_texture = load(base_path + "Jump (32x32).png")
	_add_animation_frames(sprite_frames, "jumping", jump_texture, 1, 10.0)
	
	# Load run animation
	var run_texture = load(base_path + "Run (32x32).png")
	_add_animation_frames(sprite_frames, "running", run_texture, 12, 10.0)
	
	# Load fall animation
	var fall_texture = load(base_path + "Fall (32x32).png")
	_add_animation_frames(sprite_frames, "falling", fall_texture, 1, 10.0)
	
	# Load double jump animation
	var double_jump_texture = load(base_path + "Double Jump (32x32).png")
	_add_animation_frames(sprite_frames, "double_jump", double_jump_texture, 6, 10.0)
	
	# Load hit animation
	var hit_texture = load(base_path + "Hit (32x32).png")
	_add_animation_frames(sprite_frames, "hit", hit_texture, 7, 10.0)
	
	# Apply the new sprite frames
	sprite_2d.sprite_frames = sprite_frames
	sprite_2d.play("default")


func _add_animation_frames(sprite_frames: SpriteFrames, anim_name: String, texture: Texture2D, frame_count: int, speed: float) -> void:
	sprite_frames.add_animation(anim_name)
	sprite_frames.set_animation_loop(anim_name, true)
	sprite_frames.set_animation_speed(anim_name, speed)
	
	for i in range(frame_count):
		var atlas = AtlasTexture.new()
		atlas.atlas = texture
		atlas.region = Rect2(i * 32, 0, 32, 32)
		sprite_frames.add_frame(anim_name, atlas)

func _physics_process(delta: float) -> void:
	if Global.reached_finish:
		sprite_2d.animation = "idle"
	
	if not is_moving:
		sprite_2d.animation = "finished"
		return  # Do not move if it's set to stop

	# Animations
	if (velocity.x > 1 or velocity.x < -1):
		sprite_2d.animation = "running"
	else:
		sprite_2d.animation = "default"

	# Add the gravity.
	if not is_on_floor():
		velocity += get_gravity() * delta
		sprite_2d.animation = "jumping"

	# Handle jump.
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY
		jump_sound.play()

	# Get the input direction and handle the movement/deceleration.
	var direction := Input.get_axis("left", "right")
	if direction:
		velocity.x = direction * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, 12)

	move_and_slide()

	var isLeft = velocity.x < 0
	sprite_2d.flip_h = isLeft

func stop_movement():
	is_moving = false  # Stop the movement
