extends CharacterBody2D

const SPEED = 400.0
const JUMP_VELOCITY = -1000.0
var is_moving = true  # Flag to control movement
var is_attacking = false  # Flag to track attack animations
var mouse_clicked = false  # Track mouse click
var q_pressed = false  # Track Q key press
@onready var sprite_2d: AnimatedSprite2D = $Sprite2D
@onready var jump_sound: AudioStreamPlayer = $JumpSound


func _ready() -> void:
	_load_character_sprites()


func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			mouse_clicked = true
	elif event is InputEventKey:
		if event.keycode == KEY_Q and event.pressed and not event.echo:
			q_pressed = true


func _load_character_sprites() -> void:
	var character = Global.selected_character
	var base_path = "res://assets/images/" + character + "/"
	
	# Create new sprite frames
	var sprite_frames = SpriteFrames.new()
	
	# Remove the default animation that comes with new SpriteFrames
	if sprite_frames.has_animation("default"):
		sprite_frames.remove_animation("default")
	
	# Load idle animation
	var idle_texture = load(base_path + "Idle (32x32).png")
	_add_animation_frames(sprite_frames, "default", idle_texture, 10.0)
	_add_animation_frames(sprite_frames, "idle", idle_texture, 10.0)
	_add_animation_frames(sprite_frames, "finished", idle_texture, 10.0)
	
	# Load jump animation
	var jump_texture = load(base_path + "Jump (32x32).png")
	_add_animation_frames(sprite_frames, "jumping", jump_texture, 10.0)
	
	# Load run animation
	var run_texture = load(base_path + "Run (32x32).png")
	_add_animation_frames(sprite_frames, "running", run_texture, 10.0)
	
	# Load fall animation
	var fall_texture = load(base_path + "Fall (32x32).png")
	_add_animation_frames(sprite_frames, "falling", fall_texture, 10.0)
	
	# Load double jump animation
	var double_jump_texture = load(base_path + "Double Jump (32x32).png")
	_add_animation_frames(sprite_frames, "double_jump", double_jump_texture, 10.0)
	
	# Load hit animation
	var hit_texture = load(base_path + "Hit (32x32).png")
	_add_animation_frames(sprite_frames, "hit", hit_texture, 10.0)
	
	# Load sword fight animation (if exists)
	var sword_fight_path = base_path + "Sword Fight.png"
	if ResourceLoader.exists(sword_fight_path):
		var sword_fight_texture = load(sword_fight_path)
		_add_animation_frames(sprite_frames, "sword_attack", sword_fight_texture, 15.0, false, 6)
	
	# Load karate animation (if exists)
	var karate_path = base_path + "Karate.png"
	if ResourceLoader.exists(karate_path):
		var karate_texture = load(karate_path)
		_add_animation_frames(sprite_frames, "karate_attack", karate_texture, 15.0, false, 6)
	
	# Apply the new sprite frames
	sprite_2d.sprite_frames = sprite_frames
	sprite_2d.play("default")


func _add_animation_frames(sprite_frames: SpriteFrames, anim_name: String, texture: Texture2D, speed: float, should_loop: bool = true, manual_frame_count: int = -1, frames_per_row: int = -1) -> void:
	if texture == null:
		print("Error: Texture is null for animation: ", anim_name)
		return
	
	sprite_frames.add_animation(anim_name)
	sprite_frames.set_animation_loop(anim_name, should_loop)
	sprite_frames.set_animation_speed(anim_name, speed)
	
	# Use manual frame count if provided, otherwise calculate automatically
	var frame_count = manual_frame_count if manual_frame_count > 0 else texture.get_width() / 32
	
	# If frames_per_row is not specified, assume all frames are in one row
	var columns = frames_per_row if frames_per_row > 0 else frame_count
	
	print("Loading animation '", anim_name, "' with ", frame_count, " frames (", columns, " per row)")
	
	for i in range(frame_count):
		var atlas = AtlasTexture.new()
		atlas.atlas = texture
		var x = (i % columns) * 32
		var y = int(i / columns) * 32
		atlas.region = Rect2(x, y, 32, 32)
		sprite_frames.add_frame(anim_name, atlas)

func _physics_process(delta: float) -> void:
	if Global.reached_finish:
		sprite_2d.animation = "idle"
	
	if not is_moving:
		sprite_2d.animation = "finished"
		return  # Do not move if it's set to stop

	# Handle attack inputs (only when on floor and not already attacking)
	if is_on_floor() and not is_attacking:
		if mouse_clicked and sprite_2d.sprite_frames.has_animation("sword_attack"):
			mouse_clicked = false
			is_attacking = true
			velocity.x = 0  # Stop horizontal movement during attack
			sprite_2d.animation = "sword_attack"
			sprite_2d.play("sword_attack")
			await sprite_2d.animation_finished
			is_attacking = false
			return
		elif q_pressed and sprite_2d.sprite_frames.has_animation("karate_attack"):
			q_pressed = false
			is_attacking = true
			velocity.x = 0  # Stop horizontal movement during attack
			sprite_2d.animation = "karate_attack"
			sprite_2d.play("karate_attack")
			await sprite_2d.animation_finished
			is_attacking = false
			return
	
	# Reset input flags if attacks didn't trigger
	if mouse_clicked:
		mouse_clicked = false
	if q_pressed:
		q_pressed = false
	
	# Don't process movement if attacking
	if is_attacking:
		return

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
